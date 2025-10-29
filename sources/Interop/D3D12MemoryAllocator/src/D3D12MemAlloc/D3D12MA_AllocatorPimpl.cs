// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.D3D12_CPU_PAGE_PROPERTY;
using static TerraFX.Interop.DirectX.D3D12_FEATURE;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_MEMORY_POOL;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_HEAP_TIER;
using static TerraFX.Interop.DirectX.D3D12MA_Allocation.Type;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATOR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_AllocatorPimpl : IDisposable
{
    [NativeTypeName("std::atomic_uint32_t")]
    public volatile uint m_RefCount;

    public D3D12MA_CurrentBudgetData m_Budget;

    private bool m_UseMutex;

    private bool m_AlwaysCommitted;

    private bool m_MsaaAlwaysCommitted;

    private bool m_PreferSmallBuffersCommitted;

    private bool m_DefaultPoolsNotZeroed;

    private ID3D12Device* m_Device; // AddRef

    private ID3D12Device1* m_Device1; // AddRef, optional

    private ID3D12Device4* m_Device4; // AddRef, optional

    private ID3D12Device8* m_Device8; // AddRef, optional

    private ID3D12Device10* m_Device10; // AddRef, optional

    private ID3D12Device12* m_Device12; // AddRef, optional

    private IDXGIAdapter* m_Adapter; // AddRef

    private IDXGIAdapter3* m_Adapter3; // AddRef, optional

    [NativeTypeName("UINT64")]
    private ulong m_PreferredBlockSize;

    private D3D12MA_ALLOCATION_CALLBACKS m_AllocationCallbacks;

    [NativeTypeName("D3D12MA_ATOMIC_UINT32")]
    private volatile uint m_CurrentFrameIndex;

    private DXGI_ADAPTER_DESC m_AdapterDesc;

    private D3D12_FEATURE_DATA_D3D12_OPTIONS m_D3D12Options;

    private BOOL m_GPUUploadHeapSupported;

    private D3D12_FEATURE_DATA_ARCHITECTURE m_D3D12Architecture;

    private D3D12MA_AllocationObjectAllocator m_AllocationObjectAllocator;

    [NativeTypeName("D3D12MA_RW_MUTEX[D3D12MA::HEAP_TYPE_COUNT]")]
    private _m_PoolsMutex_e__FixedBuffer m_PoolsMutex;

    [NativeTypeName("D3D12MA::PoolList[D3D12MA::HEAP_TYPE_COUNT]")]
    private _m_Pools_e__FixedBuffer m_Pools;

    // Default pools.
    [NativeTypeName("D3D12MA::BlockVector*[D3D12MA::DEFAULT_POOL_MAX_COUNT]")]
    private _m_BlockVectors_e__FixedBuffer m_BlockVectors;

    [NativeTypeName("D3D12MA::CommittedAllocationList[D3D12MA::STANDARD_HEAP_TYPE_COUNT]")]
    private _m_CommittedAllocations_e__FixedBuffer m_CommittedAllocations;

    public static D3D12MA_AllocatorPimpl* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::ALLOCATOR_DESC &")] in D3D12MA_ALLOCATOR_DESC desc)
    {
        D3D12MA_AllocatorPimpl* result = D3D12MA_NEW<D3D12MA_AllocatorPimpl>(allocs);
        result->_ctor(allocationCallbacks, desc);
        return result;
    }

    private void _ctor()
    {
        m_RefCount = 1;
        m_Budget = new D3D12MA_CurrentBudgetData();
        m_Device4 = null;
        m_Device8 = null;
        m_Adapter3 = null;
        m_AdapterDesc = new DXGI_ADAPTER_DESC();
        m_PoolsMutex = new _m_PoolsMutex_e__FixedBuffer();
        m_Pools = new _m_Pools_e__FixedBuffer();
        m_CommittedAllocations = new _m_CommittedAllocations_e__FixedBuffer();
    }

    private void _ctor([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::ALLOCATOR_DESC &")] in D3D12MA_ALLOCATOR_DESC desc)
    {
        _ctor();

        m_UseMutex = (desc.Flags & D3D12MA_ALLOCATOR_FLAG_SINGLETHREADED) == 0;
        m_AlwaysCommitted = (desc.Flags & D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED) != 0;
        m_MsaaAlwaysCommitted = (desc.Flags & D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED) != 0;
        m_PreferSmallBuffersCommitted = (desc.Flags & D3D12MA_ALLOCATOR_FLAG_DONT_PREFER_SMALL_BUFFERS_COMMITTED) == 0;
        m_Device = desc.pDevice;
        m_Adapter = desc.pAdapter;
        m_PreferredBlockSize = (desc.PreferredBlockSize != 0) ? desc.PreferredBlockSize : D3D12MA_DEFAULT_BLOCK_SIZE;
        m_AllocationCallbacks = allocationCallbacks;
        m_CurrentFrameIndex = 0;

        // Below this line don't use allocationCallbacks but m_AllocationCallbacks!!!
        m_AllocationObjectAllocator = new D3D12MA_AllocationObjectAllocator(m_AllocationCallbacks, m_UseMutex);

        // desc.pAllocationCallbacks intentionally ignored here, preprocessed by CreateAllocator.
        ZeroMemory(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_D3D12Options, __sizeof<D3D12_FEATURE_DATA_D3D12_OPTIONS>());
        ZeroMemory(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_D3D12Architecture, __sizeof<D3D12_FEATURE_DATA_ARCHITECTURE>());

        ZeroMemory(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_BlockVectors, __sizeof<_m_BlockVectors_e__FixedBuffer>());

        for (uint i = 0; i < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++i)
        {
            m_CommittedAllocations[(int)(i)].Init(m_UseMutex, D3D12MA_IndexToStandardHeapType(i), null); // pool
        }

        _ = m_Device->AddRef();
        _ = m_Adapter->AddRef();
    }

    public void Dispose()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0))
        {
            D3D12MA_SAFE_RELEASE(ref m_Device12);
            D3D12MA_SAFE_RELEASE(ref m_Device10);
            D3D12MA_SAFE_RELEASE(ref m_Device8);
            D3D12MA_SAFE_RELEASE(ref m_Device4);
            D3D12MA_SAFE_RELEASE(ref m_Device1);
        }
        D3D12MA_SAFE_RELEASE(ref m_Adapter3);
        D3D12MA_SAFE_RELEASE(ref m_Adapter);
        D3D12MA_SAFE_RELEASE(ref m_Device);

        for (uint i = D3D12MA_DEFAULT_POOL_MAX_COUNT; i-- != 0;)
        {
            D3D12MA_DELETE(GetAllocs(), m_BlockVectors[(int)(i)].Value);
        }

        for (uint i = D3D12MA_HEAP_TYPE_COUNT; i-- != 0;)
        {
            if (!m_Pools[(int)(i)].IsEmpty())
            {
                D3D12MA_FAIL("Unfreed pools found!");
            }
        }

        m_AllocationObjectAllocator.Dispose();
        m_Pools.Dispose();
        m_CommittedAllocations.Dispose();
    }

    public readonly ID3D12Device* GetDevice()
    {
        return m_Device;
    }

    public readonly ID3D12Device1* GetDevice1()
    {
        return m_Device1;
    }

    public readonly ID3D12Device4* GetDevice4()
    {
        return m_Device4;
    }

    public readonly ID3D12Device8* GetDevice8()
    {
        return m_Device8;
    }

    public readonly ID3D12Device10* GetDevice10()
    {
        return m_Device10;
    }

    public readonly ID3D12Device12* GetDevice12()
    {
        return m_Device12;
    }

    // Shortcut for "Allocation Callbacks", because this function is called so often.

    [UnscopedRef]
    [return: NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")]
    public readonly ref readonly D3D12MA_ALLOCATION_CALLBACKS GetAllocs()
    {
        return ref m_AllocationCallbacks;
    }

    [UnscopedRef]
    [return: NativeTypeName("const D3D12_FEATURE_DATA_D3D12_OPTIONS &")]
    public readonly ref readonly D3D12_FEATURE_DATA_D3D12_OPTIONS GetD3D12Options()
    {
        return ref m_D3D12Options;
    }

    public readonly BOOL IsUMA()
    {
        return m_D3D12Architecture.UMA;
    }

    public readonly BOOL IsCacheCoherentUMA()
    {
        return m_D3D12Architecture.CacheCoherentUMA;
    }

    public readonly bool SupportsResourceHeapTier2()
    {
        return m_D3D12Options.ResourceHeapTier >= D3D12_RESOURCE_HEAP_TIER_2;
    }

    public readonly bool IsGPUUploadHeapSupported()
    {
        return m_GPUUploadHeapSupported != FALSE;
    }

    public readonly bool UseMutex()
    {
        return m_UseMutex;
    }

    [UnscopedRef]
    [return: NativeTypeName("D3D12MA::AllocationObjectAllocator &")]
    public ref D3D12MA_AllocationObjectAllocator GetAllocationObjectAllocator()
    {
        return ref m_AllocationObjectAllocator;
    }

    [return: NativeTypeName("UINT")]
    public readonly uint GetCurrentFrameIndex()
    {
        return m_CurrentFrameIndex;
    }

    [return: NativeTypeName("UINT")]
    public readonly uint GetDefaultPoolCount()
    {
        // If SupportsResourceHeapTier2():
        //     0: D3D12_HEAP_TYPE_DEFAULT
        //     1: D3D12_HEAP_TYPE_UPLOAD
        //     2: D3D12_HEAP_TYPE_READBACK
        //     3: D3D12_HEAP_TYPE_GPU_UPLOAD
        // else:
        //     0: D3D12_HEAP_TYPE_DEFAULT + buffer
        //     1: D3D12_HEAP_TYPE_DEFAULT + texture
        //     2: D3D12_HEAP_TYPE_DEFAULT + texture RT or DS
        //     3: D3D12_HEAP_TYPE_UPLOAD + buffer
        //     4: D3D12_HEAP_TYPE_UPLOAD + texture
        //     5: D3D12_HEAP_TYPE_UPLOAD + texture RT or DS
        //     6: D3D12_HEAP_TYPE_READBACK + buffer
        //     7: D3D12_HEAP_TYPE_READBACK + texture
        //     8: D3D12_HEAP_TYPE_READBACK + texture RT or DS
        //     9: D3D12_HEAP_TYPE_GPU_UPLOAD + buffer
        //     10: D3D12_HEAP_TYPE_GPU_UPLOAD + texture
        //     11: D3D12_HEAP_TYPE_GPU_UPLOAD + texture RT or DS

        return SupportsResourceHeapTier2() ? 4u : 12u;
    }

    public Pointer<D3D12MA_BlockVector>* GetDefaultPools()
    {
        return &((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_BlockVectors.e0;
    }

    public HRESULT Init([NativeTypeName("const D3D12MA::ALLOCATOR_DESC &")] in D3D12MA_ALLOCATOR_DESC desc)
    {
        _ = desc.pAdapter->QueryInterface(__uuidof<IDXGIAdapter3>(), (void**)(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_Adapter3));
        _ = m_Device->QueryInterface(__uuidof<ID3D12Device1>(), (void**)(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_Device1));

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0))
        {
            _ = m_Device->QueryInterface(__uuidof<ID3D12Device4>(), (void**)(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_Device4));
            _ = m_Device->QueryInterface(__uuidof<ID3D12Device8>(), (void**)(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_Device8));

            if ((desc.Flags & D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED) != 0)
            {
                D3D12_FEATURE_DATA_D3D12_OPTIONS7 options7 = default;

                if (SUCCEEDED(m_Device->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS7, &options7, (uint)(sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS7)))))
                {
                    // DEFAULT_POOLS_NOT_ZEROED both supported and enabled by the user.
                    m_DefaultPoolsNotZeroed = true;
                }
            }

            _ = m_Device->QueryInterface(__uuidof<ID3D12Device10>(), (void**)(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_Device10));
            _ = m_Device->QueryInterface(__uuidof<ID3D12Device12>(), (void**)(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_Device12));
        }

        HRESULT hr = m_Adapter->GetDesc(&((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_AdapterDesc);

        if (FAILED(hr))
        {
            return hr;
        }

        hr = m_Device->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, &((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_D3D12Options, __sizeof<D3D12_FEATURE_DATA_D3D12_OPTIONS>());

        if (FAILED(hr))
        {
            return hr;
        }

        if (D3D12MA_FORCE_RESOURCE_HEAP_TIER != 0)
        {
            m_D3D12Options.ResourceHeapTier = (D3D12_RESOURCE_HEAP_TIER)(D3D12MA_FORCE_RESOURCE_HEAP_TIER);
        }

        D3D12_FEATURE_DATA_D3D12_OPTIONS16 options16 = default;
        hr = m_Device->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS16, &options16, (uint)(sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS16)));

        if (SUCCEEDED(hr))
        {
            m_GPUUploadHeapSupported = options16.GPUUploadHeapSupported;
        }

        hr = m_Device->CheckFeatureSupport(D3D12_FEATURE_ARCHITECTURE, &((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)))->m_D3D12Architecture, __sizeof<D3D12_FEATURE_DATA_ARCHITECTURE>());

        if (FAILED(hr))
        {
            m_D3D12Architecture.UMA = FALSE;
            m_D3D12Architecture.CacheCoherentUMA = FALSE;
        }

        D3D12_HEAP_PROPERTIES heapProps = new D3D12_HEAP_PROPERTIES();
        uint defaultPoolCount = GetDefaultPoolCount();

        for (uint i = 0; i < defaultPoolCount; ++i)
        {
            CalcDefaultPoolParams(out heapProps.Type, out D3D12_HEAP_FLAGS heapFlags, i);

            if (m_DefaultPoolsNotZeroed)
            {
                heapFlags |= D3D12_HEAP_FLAG_CREATE_NOT_ZEROED;
            }

            D3D12MA_BlockVector* blockVector = D3D12MA_BlockVector.Create(GetAllocs(), (D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)), heapProps, heapFlags, m_PreferredBlockSize, 0, nuint.MaxValue, false, D3D12MA_DEBUG_ALIGNMENT, 0, m_MsaaAlwaysCommitted, null, D3D12_RESIDENCY_PRIORITY_NONE);
            m_BlockVectors[(int)(i)] = new Pointer<D3D12MA_BlockVector>(blockVector);

            // No need to call m_pBlockVectors[i]->CreateMinBlocks here, becase minBlockCount is 0.
        }

        if (D3D12MA_DXGI_1_4 != 0)
        {
            _ = UpdateD3D12Budget();
        }
        return S_OK;
    }

    public readonly bool HeapFlagsFulfillResourceHeapTier(D3D12_HEAP_FLAGS flags)
    {
        if (SupportsResourceHeapTier2())
        {
            return true;
        }
        else
        {
            bool allowBuffers = (flags & D3D12_HEAP_FLAG_DENY_BUFFERS) == 0;
            bool allowRtDsTextures = (flags & D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES) == 0;
            bool allowNonRtDsTextures = (flags & D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES) == 0;

            byte allowedGroupCount = (byte)((allowBuffers ? 1 : 0) + (allowRtDsTextures ? 1 : 0) + (allowNonRtDsTextures ? 1 : 0));
            return allowedGroupCount == 1;
        }
    }

    [return: NativeTypeName("UINT")]
    public readonly uint StandardHeapTypeToMemorySegmentGroup(D3D12_HEAP_TYPE heapType)
    {
        D3D12MA_ASSERT(D3D12MA_IsHeapTypeStandard(heapType));

        if (IsUMA())
        {
            return DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY;
        }

        return ((heapType == D3D12_HEAP_TYPE_DEFAULT) || (heapType == D3D12_HEAP_TYPE_GPU_UPLOAD_COPY)) ? DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY : DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY;
    }

    [return: NativeTypeName("UINT")]
    public readonly uint HeapPropertiesToMemorySegmentGroup([NativeTypeName("const D3D12_HEAP_PROPERTIES &")] in D3D12_HEAP_PROPERTIES heapProps)
    {
        if (IsUMA())
        {
            return DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY;
        }

        if (heapProps.MemoryPoolPreference == D3D12_MEMORY_POOL_UNKNOWN)
        {
            return StandardHeapTypeToMemorySegmentGroup(heapProps.Type);
        }

        return (heapProps.MemoryPoolPreference == D3D12_MEMORY_POOL_L1) ? DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY : DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY;
    }

    [return: NativeTypeName("UINT64")]
    public readonly ulong GetMemoryCapacity([NativeTypeName("UINT")] uint memorySegmentGroup)
    {
        switch (memorySegmentGroup)
        {
            case DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY:
            {
                return IsUMA() ? (m_AdapterDesc.DedicatedVideoMemory + m_AdapterDesc.SharedSystemMemory) : m_AdapterDesc.DedicatedVideoMemory;
            }

            case DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY:
            {
                return IsUMA() ? 0 : m_AdapterDesc.SharedSystemMemory;
            }

            default:
            {
                D3D12MA_FAIL();
                return ulong.MaxValue;
            }
        }
    }

    public HRESULT CreatePlacedResourceWrap(ID3D12Heap* pHeap, [NativeTypeName("UINT64")] ulong HeapOffset, [NativeTypeName("const D3D12MA_CREATE_RESOURCE_PARAMS &")] in D3D12MA_CREATE_RESOURCE_PARAMS createParams, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_LAYOUT)
        {
            if (m_Device10 == null)
            {
                return E_NOINTERFACE;
            }

            // Microsoft defined pCastableFormats parameter as pointer to non-const and only fixed it in later Agility SDK, thus we need const_cast.
            return m_Device10->CreatePlacedResource2(pHeap, HeapOffset, createParams.GetResourceDesc1(), createParams.GetInitialLayout(), createParams.GetOptimizedClearValue(), createParams.GetNumCastableFormats(), (DXGI_FORMAT*)(createParams.GetCastableFormats()), riidResource, ppvResource);
        }

        if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE_AND_DESC1)
        {
            if (m_Device8 == null)
            {
                return E_NOINTERFACE;
            }
            Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));

            return m_Device8->CreatePlacedResource1(pHeap, HeapOffset, createParams.GetResourceDesc1(), createParams.GetInitialResourceState(), createParams.GetOptimizedClearValue(), riidResource, ppvResource);
        }

        if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE)
        {
            return m_Device->CreatePlacedResource(pHeap, HeapOffset, createParams.GetResourceDesc(), createParams.GetInitialResourceState(), createParams.GetOptimizedClearValue(), riidResource, ppvResource);
        }

        D3D12MA_FAIL();
        return E_INVALIDARG;
    }

    public HRESULT CreateResource([NativeTypeName("const D3D12MA::ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12MA_CREATE_RESOURCE_PARAMS &")] in D3D12MA_CREATE_RESOURCE_PARAMS createParams, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        D3D12MA_ASSERT((pAllocDesc != null) && (createParams.GetBaseResourceDesc() != null) && (ppAllocation != null));

        *ppAllocation = null;

        if (ppvResource != null)
        {
            *ppvResource = null;
        }

        HRESULT hr = E_NOINTERFACE;
        D3D12MA_CREATE_RESOURCE_PARAMS finalCreateParams = createParams;
        D3D12_RESOURCE_DESC finalResourceDesc;
        D3D12_RESOURCE_DESC1 finalResourceDesc1;
        Unsafe.SkipInit(out D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo);

        if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE)
        {
            finalResourceDesc = *createParams.GetResourceDesc();
            finalCreateParams.AccessResourceDesc() = &finalResourceDesc;
            hr = GetResourceAllocationInfo(&finalResourceDesc, 0, null, out resAllocInfo);
        }
        else if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE_AND_DESC1)
        {
            if (m_Device8 != null)
            {
                finalResourceDesc1 = *createParams.GetResourceDesc1();
                finalCreateParams.AccessResourceDesc1() = &finalResourceDesc1;
                hr = GetResourceAllocationInfo(&finalResourceDesc1, 0, null, out resAllocInfo);
            }
        }
        else if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_LAYOUT)
        {
            if (m_Device10 != null)
            {
                finalResourceDesc1 = *createParams.GetResourceDesc1();
                finalCreateParams.AccessResourceDesc1() = &finalResourceDesc1;
                hr = GetResourceAllocationInfo(&finalResourceDesc1, createParams.GetNumCastableFormats(), createParams.GetCastableFormats(), out resAllocInfo);
            }
        }
        else
        {
            D3D12MA_FAIL();
            hr = E_INVALIDARG;
        }

        if (FAILED(hr))
        {
            return hr;
        }

        D3D12MA_ASSERT(D3D12MA_IsPow2(resAllocInfo.Alignment));
        // We've seen UINT64_MAX returned when the call to GetResourceAllocationInfo was invalid.
        D3D12MA_ASSERT(resAllocInfo.SizeInBytes != UINT64_MAX);
        D3D12MA_ASSERT(resAllocInfo.SizeInBytes > 0);

        D3D12MA_BlockVector* blockVector = null;
        D3D12MA_CommittedAllocationParameters committedAllocationParams = default;
        bool preferCommitted = false;

        if (createParams.Variant >= D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE_AND_DESC1)
        {
            hr = CalcAllocationParams(*pAllocDesc, resAllocInfo.SizeInBytes, createParams.GetResourceDesc1(), out blockVector, out committedAllocationParams, out preferCommitted);
        }
        else
        {
            hr = CalcAllocationParams(*pAllocDesc, resAllocInfo.SizeInBytes, createParams.GetResourceDesc(), out blockVector, out committedAllocationParams, out preferCommitted);
        }

        if (FAILED(hr))
        {
            return hr;
        }

        bool withinBudget = (pAllocDesc->Flags & D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET) != 0;
        hr = E_INVALIDARG;

        if (committedAllocationParams.IsValid() && preferCommitted)
        {
            hr = AllocateCommittedResource(committedAllocationParams, resAllocInfo.SizeInBytes, withinBudget, pAllocDesc->pPrivateData, finalCreateParams, ppAllocation, riidResource, ppvResource);

            if (SUCCEEDED(hr))
            {
                return hr;
            }
        }

        if (blockVector != null)
        {
            hr = blockVector->CreateResource(resAllocInfo.SizeInBytes, resAllocInfo.Alignment, *pAllocDesc, finalCreateParams, committedAllocationParams.IsValid(), ppAllocation, riidResource, ppvResource);

            if (SUCCEEDED(hr))
            {
                return hr;
            }
        }

        if (committedAllocationParams.IsValid() && !preferCommitted)
        {
            hr = AllocateCommittedResource(committedAllocationParams, resAllocInfo.SizeInBytes, withinBudget, pAllocDesc->pPrivateData, finalCreateParams, ppAllocation, riidResource, ppvResource);

            if (SUCCEEDED(hr))
            {
                return hr;
            }
        }

        return hr;
    }

    public HRESULT CreateAliasingResource(D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_CREATE_RESOURCE_PARAMS &")] in D3D12MA_CREATE_RESOURCE_PARAMS createParams, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        *ppvResource = null;

        HRESULT hr = E_NOINTERFACE;
        D3D12MA_CREATE_RESOURCE_PARAMS finalCreateParams = createParams;
        D3D12_RESOURCE_DESC finalResourceDesc;
        D3D12_RESOURCE_DESC1 finalResourceDesc1;
        Unsafe.SkipInit(out D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo);

        if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE)
        {
            finalResourceDesc = *createParams.GetResourceDesc();
            finalCreateParams.AccessResourceDesc() = &finalResourceDesc;
            hr = GetResourceAllocationInfo(&finalResourceDesc, 0, null, out resAllocInfo);
        }
        else if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE_AND_DESC1)
        {
            if (m_Device8 != null)
            {
                finalResourceDesc1 = *createParams.GetResourceDesc1();
                finalCreateParams.AccessResourceDesc1() = &finalResourceDesc1;
                hr = GetResourceAllocationInfo(&finalResourceDesc1, 0, null, out resAllocInfo);
            }
        }
        else if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_LAYOUT)
        {
            if (m_Device10 != null)
            {
                finalResourceDesc1 = *createParams.GetResourceDesc1();
                finalCreateParams.AccessResourceDesc1() = &finalResourceDesc1;
                hr = GetResourceAllocationInfo(&finalResourceDesc1, createParams.GetNumCastableFormats(), createParams.GetCastableFormats(), out resAllocInfo);
            }
        }
        else
        {
            D3D12MA_FAIL();
            hr = E_INVALIDARG;
        }

        if (FAILED(hr))
        {
            return hr;
        }

        D3D12MA_ASSERT(D3D12MA_IsPow2(resAllocInfo.Alignment));
        D3D12MA_ASSERT(resAllocInfo.SizeInBytes > 0);

        ID3D12Heap* existingHeap = pAllocation->GetHeap();
        ulong existingOffset = pAllocation->GetOffset();
        ulong existingSize = pAllocation->GetSize();
        ulong newOffset = existingOffset + AllocationLocalOffset;

        if ((existingHeap == null) || ((AllocationLocalOffset + resAllocInfo.SizeInBytes) > existingSize) || ((newOffset % resAllocInfo.Alignment) != 0))
        {
            return E_INVALIDARG;
        }

        return CreatePlacedResourceWrap(existingHeap, newOffset, finalCreateParams, riidResource, ppvResource);
    }

    public HRESULT AllocateMemory([NativeTypeName("const D3D12MA::ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO *")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation)
    {
        *ppAllocation = null;

        HRESULT hr = CalcAllocationParams(*pAllocDesc, pAllocInfo->SizeInBytes, (D3D12_RESOURCE_DESC*)(null), out D3D12MA_BlockVector* blockVector, out D3D12MA_CommittedAllocationParameters committedAllocationParams, out bool preferCommitted);

        if (FAILED(hr))
        {
            return hr;
        }

        bool withinBudget = (pAllocDesc->Flags & D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET) != 0;
        hr = E_INVALIDARG;

        if (committedAllocationParams.IsValid() && preferCommitted)
        {
            hr = AllocateHeap(committedAllocationParams, *pAllocInfo, withinBudget, pAllocDesc->pPrivateData, ppAllocation);

            if (SUCCEEDED(hr))
            {
                return hr;
            }
        }

        if (blockVector != null)
        {
            hr = blockVector->Allocate(pAllocInfo->SizeInBytes, pAllocInfo->Alignment, *pAllocDesc, committedAllocationParams.IsValid(), 1, ppAllocation);

            if (SUCCEEDED(hr))
            {
                return hr;
            }
        }

        if (committedAllocationParams.IsValid() && !preferCommitted)
        {
            hr = AllocateHeap(committedAllocationParams, *pAllocInfo, withinBudget, pAllocDesc->pPrivateData, ppAllocation);

            if (SUCCEEDED(hr))
            {
                return hr;
            }
        }

        return hr;
    }

    // Unregisters allocation from the collection of dedicated allocations.
    // Allocation object must be deleted externally afterwards.
    public void FreeCommittedMemory(D3D12MA_Allocation* allocation)
    {
        D3D12MA_ASSERT((allocation != null) && (allocation->m_PackedData.GetType() == TYPE_COMMITTED));

        D3D12MA_CommittedAllocationList* allocList = allocation->Anonymous.m_Committed.list;
        allocList->Unregister(allocation);

        uint memSegmentGroup = allocList->GetMemorySegmentGroup((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)));
        ulong allocSize = allocation->GetSize();

        m_Budget.RemoveAllocation(memSegmentGroup, allocSize);
        m_Budget.RemoveBlock(memSegmentGroup, allocSize);
    }

    // Unregisters allocation from the collection of placed allocations.
    // Allocation object must be deleted externally afterwards.
    public void FreePlacedMemory(D3D12MA_Allocation* allocation)
    {
        D3D12MA_ASSERT((allocation != null) && (allocation->m_PackedData.GetType() == TYPE_PLACED));

        D3D12MA_NormalBlock* block = allocation->Anonymous.m_Placed.block;
        D3D12MA_ASSERT(block != null);

        D3D12MA_BlockVector* blockVector = block->GetBlockVector();
        D3D12MA_ASSERT(blockVector != null);

        m_Budget.RemoveAllocation(HeapPropertiesToMemorySegmentGroup(block->GetHeapProperties()), allocation->GetSize());
        blockVector->Free(allocation);
    }

    // Unregisters allocation from the collection of dedicated allocations and destroys associated heap.
    // Allocation object must be deleted externally afterwards.
    public void FreeHeapMemory(D3D12MA_Allocation* allocation)
    {
        D3D12MA_ASSERT((allocation != null) && (allocation->m_PackedData.GetType() == TYPE_HEAP));

        D3D12MA_CommittedAllocationList* allocList = allocation->Anonymous.m_Committed.list;
        allocList->Unregister(allocation);

        D3D12MA_SAFE_RELEASE(ref allocation->Anonymous.m_Heap.heap);

        uint memSegmentGroup = allocList->GetMemorySegmentGroup((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)));
        ulong allocSize = allocation->GetSize();

        m_Budget.RemoveAllocation(memSegmentGroup, allocSize);
        m_Budget.RemoveBlock(memSegmentGroup, allocSize);
    }

    public readonly void SetResidencyPriority(ID3D12Pageable* obj, D3D12_RESIDENCY_PRIORITY priority)
    {
        if ((priority != D3D12_RESIDENCY_PRIORITY_NONE) && (m_Device1 != null))
        {
            // Intentionally ignoring the result.
            _ = m_Device1->SetResidencyPriority(1, &obj, &priority);
        }
    }

    public void SetCurrentFrameIndex([NativeTypeName("UINT")] uint frameIndex)
    {
        m_CurrentFrameIndex = frameIndex;

        if (D3D12MA_DXGI_1_4 != 0)
        {
            _ = UpdateD3D12Budget();
        }
    }

    // For more deailed stats use outCustomHeaps to access statistics divided into L0 and L1 group
    public void CalculateStatistics([NativeTypeName("D3D12MA::TotalStatistics &")] out D3D12MA_TotalStatistics outStats, [NativeTypeName("D3D12MA::DetailedStatistics[2]")] D3D12MA_DetailedStatistics* outCustomHeaps = null)
    {
        Unsafe.SkipInit(out outStats);

        // Init stats
        for (nuint i = 0; i < D3D12MA_HEAP_TYPE_COUNT; i++)
        {
            D3D12MA_ClearDetailedStatistics(out outStats.HeapType[(int)(i)]);
        }

        for (nuint i = 0; i < DXGI_MEMORY_SEGMENT_GROUP_COUNT; i++)
        {
            D3D12MA_ClearDetailedStatistics(out outStats.MemorySegmentGroup[(int)(i)]);
        }

        D3D12MA_ClearDetailedStatistics(out outStats.Total);

        if (outCustomHeaps != null)
        {
            D3D12MA_ClearDetailedStatistics(out outCustomHeaps[0]);
            D3D12MA_ClearDetailedStatistics(out outCustomHeaps[1]);
        }

        // Process default pools. 4 standard heap types only. Add them to outStats.HeapType[i].
        if (SupportsResourceHeapTier2())
        {
            // DEFAULT, UPLOAD, READBACK, GPU_UPLOAD.
            for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapTypeIndex)
            {
                D3D12MA_BlockVector* pBlockVector = m_BlockVectors[(int)(heapTypeIndex)].Value;
                D3D12MA_ASSERT(pBlockVector != null);
                nuint outputIndex = (heapTypeIndex < 3) ? heapTypeIndex : 4; // GPU_UPLOAD 3 -> 4
                pBlockVector->AddDetailedStatistics(ref outStats.HeapType[(int)(outputIndex)]);
            }
        }
        else
        {
            // DEFAULT, UPLOAD, READBACK.
            for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapTypeIndex)
            {
                for (nuint heapSubType = 0; heapSubType < 3; ++heapSubType)
                {
                    D3D12MA_BlockVector* pBlockVector = m_BlockVectors[(int)((heapTypeIndex * 3) + heapSubType)].Value;
                    D3D12MA_ASSERT(pBlockVector != null);

                    nuint outputIndex = (heapTypeIndex < 3) ? heapTypeIndex : 4; // GPU_UPLOAD 3 -> 4
                    pBlockVector->AddDetailedStatistics(ref outStats.HeapType[(int)(outputIndex)]);
                }
            }
        }

        // Sum them up to memory segment groups.
        D3D12MA_AddDetailedStatistics(ref outStats.MemorySegmentGroup[(int)(StandardHeapTypeToMemorySegmentGroup(D3D12_HEAP_TYPE_DEFAULT))], outStats.HeapType[0]);
        D3D12MA_AddDetailedStatistics(ref outStats.MemorySegmentGroup[(int)(StandardHeapTypeToMemorySegmentGroup(D3D12_HEAP_TYPE_UPLOAD))], outStats.HeapType[1]);
        D3D12MA_AddDetailedStatistics(ref outStats.MemorySegmentGroup[(int)(StandardHeapTypeToMemorySegmentGroup(D3D12_HEAP_TYPE_READBACK))], outStats.HeapType[2]);
        D3D12MA_AddDetailedStatistics(ref outStats.MemorySegmentGroup[(int)(StandardHeapTypeToMemorySegmentGroup(D3D12_HEAP_TYPE_GPU_UPLOAD_COPY))], outStats.HeapType[4]);

        // Process custom pools.
        D3D12MA_DetailedStatistics tmpStats;

        for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_HEAP_TYPE_COUNT; ++heapTypeIndex)
        {
            using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_PoolsMutex[(int)(heapTypeIndex)], m_UseMutex);
            ref D3D12MA_IntrusiveLinkedList<D3D12MA_PoolListItemTraits, D3D12MA_PoolPimpl> poolList = ref m_Pools[(int)(heapTypeIndex)];

            for (D3D12MA_PoolPimpl* pool = poolList.Front(); pool != null; pool = D3D12MA_IntrusiveLinkedList<D3D12MA_PoolListItemTraits, D3D12MA_PoolPimpl>.GetNext(pool))
            {
                ref readonly D3D12_HEAP_PROPERTIES poolHeapProps = ref pool->GetDesc().HeapProperties;

                D3D12MA_ClearDetailedStatistics(out tmpStats);
                pool->AddDetailedStatistics(ref tmpStats);

                D3D12MA_AddDetailedStatistics(ref outStats.HeapType[(int)(heapTypeIndex)], tmpStats);

                uint memorySegment = HeapPropertiesToMemorySegmentGroup(poolHeapProps);
                D3D12MA_AddDetailedStatistics(ref outStats.MemorySegmentGroup[(int)(memorySegment)], tmpStats);

                if (outCustomHeaps != null)
                {
                    D3D12MA_AddDetailedStatistics(ref outCustomHeaps[memorySegment], tmpStats);
                }
            }
        }

        // Process committed allocations. standard heap types only.
        for (uint heapTypeIndex = 0; heapTypeIndex < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapTypeIndex)
        {
            D3D12MA_ClearDetailedStatistics(out tmpStats);
            m_CommittedAllocations[(int)(heapTypeIndex)].AddDetailedStatistics(ref tmpStats);

            nuint outputIndex = (heapTypeIndex < 3) ? heapTypeIndex : 4; // GPU_UPLOAD 3 -> 4

            D3D12MA_AddDetailedStatistics(ref outStats.HeapType[(int)(outputIndex)], tmpStats);
            D3D12MA_AddDetailedStatistics(ref outStats.MemorySegmentGroup[(int)(StandardHeapTypeToMemorySegmentGroup(D3D12MA_IndexToStandardHeapType(heapTypeIndex)))], tmpStats);
        }

        // Sum up memory segment groups to totals.
        D3D12MA_AddDetailedStatistics(ref outStats.Total, outStats.MemorySegmentGroup[0]);
        D3D12MA_AddDetailedStatistics(ref outStats.Total, outStats.MemorySegmentGroup[1]);

        D3D12MA_ASSERT(outStats.Total.Stats.BlockCount == (outStats.MemorySegmentGroup[0].Stats.BlockCount + outStats.MemorySegmentGroup[1].Stats.BlockCount));
        D3D12MA_ASSERT(outStats.Total.Stats.AllocationCount == (outStats.MemorySegmentGroup[0].Stats.AllocationCount + outStats.MemorySegmentGroup[1].Stats.AllocationCount));

        D3D12MA_ASSERT(outStats.Total.Stats.BlockBytes == (outStats.MemorySegmentGroup[0].Stats.BlockBytes + outStats.MemorySegmentGroup[1].Stats.BlockBytes));
        D3D12MA_ASSERT(outStats.Total.Stats.AllocationBytes == (outStats.MemorySegmentGroup[0].Stats.AllocationBytes + outStats.MemorySegmentGroup[1].Stats.AllocationBytes));

        D3D12MA_ASSERT(outStats.Total.UnusedRangeCount == (outStats.MemorySegmentGroup[0].UnusedRangeCount + outStats.MemorySegmentGroup[1].UnusedRangeCount));

        D3D12MA_ASSERT(outStats.Total.Stats.BlockCount == (outStats.HeapType[0].Stats.BlockCount + outStats.HeapType[1].Stats.BlockCount + outStats.HeapType[2].Stats.BlockCount + outStats.HeapType[3].Stats.BlockCount + outStats.HeapType[4].Stats.BlockCount));
        D3D12MA_ASSERT(outStats.Total.Stats.AllocationCount == (outStats.HeapType[0].Stats.AllocationCount + outStats.HeapType[1].Stats.AllocationCount + outStats.HeapType[2].Stats.AllocationCount + outStats.HeapType[3].Stats.AllocationCount + outStats.HeapType[4].Stats.AllocationCount));

        D3D12MA_ASSERT(outStats.Total.Stats.BlockBytes == (outStats.HeapType[0].Stats.BlockBytes + outStats.HeapType[1].Stats.BlockBytes + outStats.HeapType[2].Stats.BlockBytes + outStats.HeapType[3].Stats.BlockBytes + outStats.HeapType[4].Stats.BlockBytes));
        D3D12MA_ASSERT(outStats.Total.Stats.AllocationBytes == (outStats.HeapType[0].Stats.AllocationBytes + outStats.HeapType[1].Stats.AllocationBytes + outStats.HeapType[2].Stats.AllocationBytes + outStats.HeapType[3].Stats.AllocationBytes + outStats.HeapType[4].Stats.AllocationBytes));

        D3D12MA_ASSERT(outStats.Total.UnusedRangeCount == (outStats.HeapType[0].UnusedRangeCount + outStats.HeapType[1].UnusedRangeCount + outStats.HeapType[2].UnusedRangeCount + outStats.HeapType[3].UnusedRangeCount + outStats.HeapType[4].UnusedRangeCount));
    }

    public void GetBudget(D3D12MA_Budget* outLocalBudget, D3D12MA_Budget* outNonLocalBudget)
    {
        if (outLocalBudget != null)
        {
            m_Budget.GetStatistics(out outLocalBudget->Stats, DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY);
        }

        if (outNonLocalBudget != null)
        {
            m_Budget.GetStatistics(out outNonLocalBudget->Stats, DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY);
        }

        if ((D3D12MA_DXGI_1_4 != 0) && (m_Adapter3 != null))
        {
            if (!m_Budget.ShouldUpdateBudget())
            {
                m_Budget.GetBudget(m_UseMutex, (outLocalBudget != null) ? &outLocalBudget->UsageBytes : null, (outLocalBudget != null) ? &outLocalBudget->BudgetBytes : null, (outNonLocalBudget != null) ? &outNonLocalBudget->UsageBytes : null, (outNonLocalBudget != null) ? &outNonLocalBudget->BudgetBytes : null);
                return;
            }

            if (SUCCEEDED(UpdateD3D12Budget()))
            {
                GetBudget(outLocalBudget, outNonLocalBudget); // Recursion
                return;
            }
        }

        // Fallback path - manual calculation, not real budget.
        if (outLocalBudget != null)
        {
            outLocalBudget->UsageBytes = outLocalBudget->Stats.BlockBytes;
            outLocalBudget->BudgetBytes = GetMemoryCapacity(DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY) * 8 / 10; // 80% heuristics.
        }

        if (outNonLocalBudget != null)
        {
            outNonLocalBudget->UsageBytes = outNonLocalBudget->Stats.BlockBytes;
            outNonLocalBudget->BudgetBytes = GetMemoryCapacity(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY) * 8 / 10; // 80% heuristics.
        }
    }

    public void GetBudgetForHeapType([NativeTypeName("D3D12MA::Budget &")] out D3D12MA_Budget outBudget, D3D12_HEAP_TYPE heapType)
    {
        bool isLocal = StandardHeapTypeToMemorySegmentGroup(heapType) == DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY;

        fixed (D3D12MA_Budget* pOutBudget = &outBudget)
        {
            if (isLocal)
            {
                GetBudget(pOutBudget, null);
            }
            else
            {
                GetBudget(null, pOutBudget);
            }
        }
    }

    public void BuildStatsString([NativeTypeName("WCHAR **")] char** ppStatsString, BOOL detailedMap)
    {
        using D3D12MA_StringBuilder sb = new D3D12MA_StringBuilder(GetAllocs());
        {
            D3D12MA_Budget localBudget = new D3D12MA_Budget();
            D3D12MA_Budget nonLocalBudget = new D3D12MA_Budget();
            GetBudget(&localBudget, &nonLocalBudget);

            D3D12MA_DetailedStatistics* customHeaps = stackalloc D3D12MA_DetailedStatistics[2];
            CalculateStatistics(out D3D12MA_TotalStatistics stats, customHeaps);

            using D3D12MA_JsonWriter json = new D3D12MA_JsonWriter(GetAllocs(), &sb);

            json.BeginObject();
            {
                json.WriteString("General");
                json.BeginObject();
                {
                    json.WriteString("API");
                    json.WriteString("Direct3D 12");

                    json.WriteString("GPU");
                    json.WriteString(m_AdapterDesc.Description);

                    json.WriteString("DedicatedVideoMemory");
                    json.WriteNumber(m_AdapterDesc.DedicatedVideoMemory);

                    json.WriteString("DedicatedSystemMemory");
                    json.WriteNumber(m_AdapterDesc.DedicatedSystemMemory);

                    json.WriteString("SharedSystemMemory");
                    json.WriteNumber(m_AdapterDesc.SharedSystemMemory);

                    json.WriteString("ResourceHeapTier");
                    json.WriteNumber((uint)(m_D3D12Options.ResourceHeapTier));

                    json.WriteString("ResourceBindingTier");
                    json.WriteNumber((uint)(m_D3D12Options.ResourceBindingTier));

                    json.WriteString("TiledResourcesTier");
                    json.WriteNumber((uint)(m_D3D12Options.TiledResourcesTier));

                    json.WriteString("TileBasedRenderer");
                    json.WriteBool(m_D3D12Architecture.TileBasedRenderer);

                    json.WriteString("UMA");
                    json.WriteBool(m_D3D12Architecture.UMA);

                    json.WriteString("CacheCoherentUMA");
                    json.WriteBool(m_D3D12Architecture.CacheCoherentUMA);

                    json.WriteString("GPUUploadHeapSupported");
                    json.WriteBool(m_GPUUploadHeapSupported != FALSE);
                }
                json.EndObject();
            }

            {
                json.WriteString("Total");
                json.AddDetailedStatisticsInfoObject(stats.Total);
            }

            {
                json.WriteString("MemoryInfo");
                json.BeginObject();
                {
                    json.WriteString("L0");
                    json.BeginObject();
                    {
                        json.WriteString("Budget");
                        WriteBudgetToJson(&json, IsUMA() ? localBudget : nonLocalBudget); // When UMA device only L0 present as local

                        json.WriteString("Stats");
                        json.AddDetailedStatisticsInfoObject(stats.MemorySegmentGroup[!IsUMA() ? 1 : 0]);

                        json.WriteString("MemoryPools");
                        json.BeginObject();
                        {
                            if (IsUMA())
                            {
                                json.WriteString("DEFAULT");
                                json.BeginObject();
                                {
                                    json.WriteString("Stats");
                                    json.AddDetailedStatisticsInfoObject(stats.HeapType[0]);
                                }
                                json.EndObject();

                                if (IsGPUUploadHeapSupported())
                                {
                                    json.WriteString("GPU_UPLOAD");
                                    json.BeginObject();
                                    {
                                        json.WriteString("Stats");
                                        json.AddDetailedStatisticsInfoObject(stats.HeapType[4]);
                                    }
                                    json.EndObject();
                                }
                            }

                            json.WriteString("UPLOAD");
                            json.BeginObject();
                            {
                                json.WriteString("Stats");
                                json.AddDetailedStatisticsInfoObject(stats.HeapType[1]);
                            }
                            json.EndObject();

                            json.WriteString("READBACK");
                            json.BeginObject();
                            {
                                json.WriteString("Stats");
                                json.AddDetailedStatisticsInfoObject(stats.HeapType[2]);
                            }
                            json.EndObject();

                            json.WriteString("CUSTOM");
                            json.BeginObject();
                            {
                                json.WriteString("Stats");
                                json.AddDetailedStatisticsInfoObject(customHeaps[!IsUMA() ? 1 : 0]);
                            }
                            json.EndObject();
                        }
                        json.EndObject();
                    }
                    json.EndObject();

                    if (!IsUMA())
                    {
                        json.WriteString("L1");
                        json.BeginObject();
                        {
                            json.WriteString("Budget");
                            WriteBudgetToJson(&json, localBudget);

                            json.WriteString("Stats");
                            json.AddDetailedStatisticsInfoObject(stats.MemorySegmentGroup[0]);

                            json.WriteString("MemoryPools");
                            json.BeginObject();
                            {
                                json.WriteString("DEFAULT");
                                json.BeginObject();
                                {
                                    json.WriteString("Stats");
                                    json.AddDetailedStatisticsInfoObject(stats.HeapType[0]);
                                }
                                json.EndObject();

                                if (IsGPUUploadHeapSupported())
                                {
                                    json.WriteString("GPU_UPLOAD");
                                    json.BeginObject();
                                    {
                                        json.WriteString("Stats");
                                        json.AddDetailedStatisticsInfoObject(stats.HeapType[4]);
                                    }
                                    json.EndObject();
                                }

                                json.WriteString("CUSTOM");
                                json.BeginObject();
                                {
                                    json.WriteString("Stats");
                                    json.AddDetailedStatisticsInfoObject(customHeaps[0]);
                                }
                                json.EndObject();
                            }
                            json.EndObject();
                        }
                        json.EndObject();
                    }
                }
                json.EndObject();
            }

            if (detailedMap)
            {
                static void writeHeapInfo(D3D12MA_JsonWriter* pJson, D3D12MA_BlockVector* blockVector, D3D12MA_CommittedAllocationList* committedAllocs, bool customHeap)
                {
                    D3D12MA_ASSERT(blockVector != null);
                    ref D3D12MA_JsonWriter json = ref *pJson;

                    D3D12_HEAP_FLAGS flags = blockVector->GetHeapFlags();
                    json.WriteString("Flags");
                    json.BeginArray(true);
                    {
                        if ((flags & D3D12_HEAP_FLAG_SHARED) != 0)
                        {
                            json.WriteString("HEAP_FLAG_SHARED");
                        }

                        if ((flags & D3D12_HEAP_FLAG_ALLOW_DISPLAY) != 0)
                        {
                            json.WriteString("HEAP_FLAG_ALLOW_DISPLAY");
                        }

                        if ((flags & D3D12_HEAP_FLAG_SHARED_CROSS_ADAPTER) != 0)
                        {
                            json.WriteString("HEAP_FLAG_CROSS_ADAPTER");
                        }

                        if ((flags & D3D12_HEAP_FLAG_HARDWARE_PROTECTED) != 0)
                        {
                            json.WriteString("HEAP_FLAG_HARDWARE_PROTECTED");
                        }

                        if ((flags & D3D12_HEAP_FLAG_ALLOW_WRITE_WATCH) != 0)
                        {
                            json.WriteString("HEAP_FLAG_ALLOW_WRITE_WATCH");
                        }

                        if ((flags & D3D12_HEAP_FLAG_ALLOW_SHADER_ATOMICS) != 0)
                        {
                            json.WriteString("HEAP_FLAG_ALLOW_SHADER_ATOMICS");
                        }

                        if ((flags & D3D12_HEAP_FLAG_CREATE_NOT_RESIDENT) != 0)
                        {
                            json.WriteString("HEAP_FLAG_CREATE_NOT_RESIDENT");
                        }

                        if ((flags & D3D12_HEAP_FLAG_CREATE_NOT_ZEROED) != 0)
                        {
                            json.WriteString("HEAP_FLAG_CREATE_NOT_ZEROED");
                        }

                        if ((flags & D3D12_HEAP_FLAG_DENY_BUFFERS) != 0)
                        {
                            json.WriteString("HEAP_FLAG_DENY_BUFFERS");
                        }

                        if ((flags & D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES) != 0)
                        {
                            json.WriteString("HEAP_FLAG_DENY_RT_DS_TEXTURES");
                        }

                        if ((flags & D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES) != 0)
                        {
                            json.WriteString("HEAP_FLAG_DENY_NON_RT_DS_TEXTURES");
                        }

                        flags &= ~(D3D12_HEAP_FLAG_SHARED | D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_ALLOW_DISPLAY | D3D12_HEAP_FLAG_SHARED_CROSS_ADAPTER | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAG_HARDWARE_PROTECTED | D3D12_HEAP_FLAG_ALLOW_WRITE_WATCH | D3D12_HEAP_FLAG_ALLOW_SHADER_ATOMICS);
                        flags &= ~(D3D12_HEAP_FLAG_CREATE_NOT_RESIDENT | D3D12_HEAP_FLAG_CREATE_NOT_ZEROED);

                        if (flags != 0)
                        {
                            json.WriteNumber((uint)(flags));
                        }

                        if (customHeap)
                        {
                            ref readonly D3D12_HEAP_PROPERTIES properties = ref blockVector->GetHeapProperties();

                            switch (properties.MemoryPoolPreference)
                            {
                                default:
                                {
                                    D3D12MA_FAIL();
                                    break;
                                }

                                case D3D12_MEMORY_POOL_UNKNOWN:
                                {
                                    json.WriteString("MEMORY_POOL_UNKNOWN");
                                    break;
                                }

                                case D3D12_MEMORY_POOL_L0:
                                {
                                    json.WriteString("MEMORY_POOL_L0");
                                    break;
                                }

                                case D3D12_MEMORY_POOL_L1:
                                {
                                    json.WriteString("MEMORY_POOL_L1");
                                    break;
                                }
                            }

                            switch (properties.CPUPageProperty)
                            {
                                default:
                                {
                                    D3D12MA_FAIL();
                                    break;
                                }

                                case D3D12_CPU_PAGE_PROPERTY_UNKNOWN:
                                {
                                    json.WriteString("CPU_PAGE_PROPERTY_UNKNOWN");
                                    break;
                                }

                                case D3D12_CPU_PAGE_PROPERTY_NOT_AVAILABLE:
                                {
                                    json.WriteString("CPU_PAGE_PROPERTY_NOT_AVAILABLE");
                                    break;
                                }

                                case D3D12_CPU_PAGE_PROPERTY_WRITE_COMBINE:
                                {
                                    json.WriteString("CPU_PAGE_PROPERTY_WRITE_COMBINE");
                                    break;
                                }

                                case D3D12_CPU_PAGE_PROPERTY_WRITE_BACK:
                                {
                                    json.WriteString("CPU_PAGE_PROPERTY_WRITE_BACK");
                                    break;
                                }
                            }
                        }
                    }
                    json.EndArray();

                    json.WriteString("PreferredBlockSize");
                    json.WriteNumber(blockVector->GetPreferredBlockSize());

                    json.WriteString("Blocks");
                    blockVector->WriteBlockInfoToJson(pJson);

                    json.WriteString("DedicatedAllocations");
                    json.BeginArray();

                    if (committedAllocs != null)
                    {
                        committedAllocs->BuildStatsString(ref json);
                    }

                    json.EndArray();
                }

                json.WriteString("DefaultPools");
                json.BeginObject();
                {
                    if (SupportsResourceHeapTier2())
                    {
                        for (byte heapType = 0; heapType < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapType)
                        {
                            json.WriteString(D3D12MA_StandardHeapTypeNames[heapType]);
                            json.BeginObject();
                            writeHeapInfo(&json, m_BlockVectors[heapType].Value, (D3D12MA_CommittedAllocationList*)(Unsafe.AsPointer(ref m_CommittedAllocations[heapType])), false);
                            json.EndObject();
                        }
                    }
                    else
                    {
                        for (byte heapType = 0; heapType < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapType)
                        {
                            for (byte heapSubType = 0; heapSubType < 3; ++heapSubType)
                            {
                                json.BeginString(D3D12MA_StandardHeapTypeNames[heapType]);
                                json.EndString(D3D12MA_HeapSubTypeName[heapSubType]);

                                json.BeginObject();
                                writeHeapInfo(&json, m_BlockVectors[(int)(heapType * 3 + heapSubType)].Value, (D3D12MA_CommittedAllocationList*)(Unsafe.AsPointer(ref m_CommittedAllocations[heapType])), false);
                                json.EndObject();
                            }
                        }
                    }
                }
                json.EndObject();

                json.WriteString("CustomPools");
                json.BeginObject();

                for (byte heapTypeIndex = 0; heapTypeIndex < D3D12MA_HEAP_TYPE_COUNT; ++heapTypeIndex)
                {
                    using D3D12MA_MutexLockRead mutex = new D3D12MA_MutexLockRead(ref m_PoolsMutex[heapTypeIndex], m_UseMutex);
                    D3D12MA_PoolPimpl* item = m_Pools[heapTypeIndex].Front();

                    if (item != null)
                    {
                        nuint index = 0;
                        json.WriteString(D3D12MA_HeapTypeNames[heapTypeIndex]);
                        json.BeginArray();

                        do
                        {
                            json.BeginObject();
                            json.WriteString("Name");
                            json.BeginString();
                            json.ContinueString(index++);

                            if (item->GetName() != null)
                            {
                                json.ContinueString(" - ");
                                json.ContinueString(item->GetName());
                            }

                            json.EndString();

                            writeHeapInfo(&json, item->GetBlockVector(), item->GetCommittedAllocationList(), heapTypeIndex == 3);
                            json.EndObject();
                        }
                        while ((item = D3D12MA_IntrusiveLinkedList<D3D12MA_PoolListItemTraits, D3D12MA_PoolPimpl>.GetNext(item)) != null);

                        json.EndArray();
                    }
                }

                json.EndObject();
            }
            json.EndObject();
        }

        nuint length = sb.GetLength();
        char* result = D3D12MA_AllocateArray<char>(GetAllocs(), length + 2);

        result[0] = (char)(0xFEFF);
        _ = memcpy(result + 1, sb.GetData(), length * sizeof(char));

        result[length + 1] = '\0';
        *ppStatsString = result;
    }

    public readonly void FreeStatsString([NativeTypeName("WCHAR *")] char* pStatsString)
    {
        D3D12MA_ASSERT(pStatsString != null);
        D3D12MA_Free(GetAllocs(), pStatsString);
    }

    // Heuristics that decides whether a resource should better be placed in its own, dedicated allocation (committed resource rather than placed resource).
    private bool PrefersCommittedAllocation([NativeTypeName("const D3D12_RESOURCE_DESC_T &")] D3D12_RESOURCE_DESC* resourceDesc, D3D12MA_ALLOCATION_FLAGS strategy)
    {
        // Prefer creating small buffers <= 32 KB as committed, because drivers pack them better, while placed buffers require 64 KB alignment.
        // Creating as committed would be slower.

        if ((resourceDesc->Dimension == D3D12_RESOURCE_DIMENSION_BUFFER) && (resourceDesc->Width <= (D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT / 2)) && (strategy != D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME) && m_PreferSmallBuffersCommitted)
        {
            return true;
        }

        // Intentional. It may change in the future.
        return false;
    }

    private bool PrefersCommittedAllocation([NativeTypeName("const D3D12_RESOURCE_DESC_T &")] D3D12_RESOURCE_DESC1* resourceDesc, D3D12MA_ALLOCATION_FLAGS strategy)
    {
        return PrefersCommittedAllocation((D3D12_RESOURCE_DESC*)(resourceDesc), strategy);
    }

    // Allocates and registers new committed resource with implicit heap, as dedicated allocation.
    // Creates and returns Allocation object and optionally D3D12 resource.
    private HRESULT AllocateCommittedResource([NativeTypeName("const D3D12MA::CommittedAllocationParameters &")] in D3D12MA_CommittedAllocationParameters committedAllocParams, [NativeTypeName("UINT64")] ulong resourceSize, bool withinBudget, void* pPrivateData, [NativeTypeName("D3D12MA_CREATE_RESOURCE_PARAMS &")] in D3D12MA_CREATE_RESOURCE_PARAMS createParams, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        D3D12MA_ASSERT(committedAllocParams.IsValid());

        HRESULT hr;
        ID3D12Resource* res = null;

        // Allocate aliasing memory with explicit heap
        if (committedAllocParams.m_CanAlias)
        {
            D3D12_RESOURCE_ALLOCATION_INFO heapAllocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
                SizeInBytes = resourceSize,
                Alignment = D3D12MA_HeapFlagsToAlignment(committedAllocParams.m_HeapFlags, m_MsaaAlwaysCommitted),
            };

            hr = AllocateHeap(committedAllocParams, heapAllocInfo, withinBudget, pPrivateData, ppAllocation);

            if (SUCCEEDED(hr))
            {
                hr = CreatePlacedResourceWrap((*ppAllocation)->GetHeap(), 0, createParams, __uuidof<ID3D12Resource>(), (void**)(&res));

                if (SUCCEEDED(hr))
                {
                    if (ppvResource != null)
                    {
                        hr = res->QueryInterface(riidResource, ppvResource);
                    }

                    if (SUCCEEDED(hr))
                    {
                        (*ppAllocation)->SetResourcePointer(res, createParams.GetBaseResourceDesc());
                        return hr;
                    }

                    _ = res->Release();
                }

                FreeHeapMemory(*ppAllocation);
            }

            return hr;
        }

        if (withinBudget && !NewAllocationWithinBudget(committedAllocParams.m_HeapProperties.Type, resourceSize))
        {
            return E_OUTOFMEMORY;
        }

        fixed (D3D12_HEAP_PROPERTIES* pHeapProperties = &committedAllocParams.m_HeapProperties)
        {
            // D3D12 ERROR: ID3D12Device::CreateCommittedResource: When creating a committed resource, D3D12_HEAP_FLAGS must not have either D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES, D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES, nor D3D12_HEAP_FLAG_DENY_BUFFERS set. These flags will be set automatically to correspond with the committed resource type. [ STATE_CREATION ERROR #640: CREATERESOURCEANDHEAP_INVALIDHEAPMISCFLAGS]
            if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_LAYOUT)
            {
                if (m_Device10 == null)
                {
                    return E_NOINTERFACE;
                }

                // Microsoft defined pCastableFormats parameter as pointer to non-const and only fixed it in later Agility SDK, thus we need const_cast.
                hr = m_Device10->CreateCommittedResource3(pHeapProperties, committedAllocParams.m_HeapFlags & ~D3D12MA_RESOURCE_CLASS_HEAP_FLAGS, createParams.GetResourceDesc1(), createParams.GetInitialLayout(), createParams.GetOptimizedClearValue(), committedAllocParams.m_ProtectedSession, createParams.GetNumCastableFormats(), (DXGI_FORMAT*)(createParams.GetCastableFormats()), __uuidof<ID3D12Resource>(), (void**)(&res));
            }
            else if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE_AND_DESC1)
            {
                if (m_Device8 == null)
                {
                    return E_NOINTERFACE;
                }
                Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));

                hr = m_Device8->CreateCommittedResource2(pHeapProperties, committedAllocParams.m_HeapFlags & ~D3D12MA_RESOURCE_CLASS_HEAP_FLAGS, createParams.GetResourceDesc1(), createParams.GetInitialResourceState(), createParams.GetOptimizedClearValue(), committedAllocParams.m_ProtectedSession, __uuidof<ID3D12Resource>(), (void**)(&res));
            }
            else if (createParams.Variant == D3D12MA_CREATE_RESOURCE_PARAMS.VARIANT.WITH_STATE)
            {
                if (m_Device4 != null)
                {
                    Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));
                    hr = m_Device4->CreateCommittedResource1(pHeapProperties, committedAllocParams.m_HeapFlags & ~D3D12MA_RESOURCE_CLASS_HEAP_FLAGS, createParams.GetResourceDesc(), createParams.GetInitialResourceState(), createParams.GetOptimizedClearValue(), committedAllocParams.m_ProtectedSession, __uuidof<ID3D12Resource>(), (void**)(&res));
                }
                else
                {
                    if (committedAllocParams.m_ProtectedSession == null)
                    {
                        hr = m_Device->CreateCommittedResource(pHeapProperties, committedAllocParams.m_HeapFlags & ~D3D12MA_RESOURCE_CLASS_HEAP_FLAGS, createParams.GetResourceDesc(), createParams.GetInitialResourceState(), createParams.GetOptimizedClearValue(), __uuidof<ID3D12Resource>(), (void**)(&res));
                    }
                    else
                    {
                        hr = E_NOINTERFACE;
                    }
                }
            }
            else
            {
                D3D12MA_FAIL();
                return E_INVALIDARG;
            }
        }

        if (SUCCEEDED(hr))
        {
            SetResidencyPriority((ID3D12Pageable*)(res), committedAllocParams.m_ResidencyPriority);

            if (ppvResource != null)
            {
                hr = res->QueryInterface(riidResource, ppvResource);
            }

            if (SUCCEEDED(hr))
            {
                D3D12MA_Allocation* alloc = m_AllocationObjectAllocator.Allocate((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)), resourceSize, createParams.GetBaseResourceDesc()->Alignment);

                alloc->InitCommitted(committedAllocParams.m_List);
                alloc->SetResourcePointer(res, createParams.GetBaseResourceDesc());
                alloc->SetPrivateData(pPrivateData);

                *ppAllocation = alloc;

                committedAllocParams.m_List->Register(alloc);

                uint memSegmentGroup = HeapPropertiesToMemorySegmentGroup(committedAllocParams.m_HeapProperties);
                m_Budget.AddBlock(memSegmentGroup, resourceSize);
                m_Budget.AddAllocation(memSegmentGroup, resourceSize);
            }
            else
            {
                _ = res->Release();
            }
        }

        return hr;
    }

    // Allocates and registers new heap without any resources placed in it, as dedicated allocation.
    // Creates and returns Allocation object.
    private HRESULT AllocateHeap([NativeTypeName("const D3D12MA::CommittedAllocationParameters &")] in D3D12MA_CommittedAllocationParameters committedAllocParams, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO &")] in D3D12_RESOURCE_ALLOCATION_INFO allocInfo, bool withinBudget, void* pPrivateData, D3D12MA_Allocation** ppAllocation)
    {
        D3D12MA_ASSERT(committedAllocParams.IsValid());
        *ppAllocation = null;

        if (withinBudget && !NewAllocationWithinBudget(committedAllocParams.m_HeapProperties.Type, allocInfo.SizeInBytes))
        {
            return E_OUTOFMEMORY;
        }

        D3D12_HEAP_DESC heapDesc = new D3D12_HEAP_DESC {
            SizeInBytes = allocInfo.SizeInBytes,
            Properties = committedAllocParams.m_HeapProperties,
            Alignment = allocInfo.Alignment,
            Flags = committedAllocParams.m_HeapFlags,
        };

        HRESULT hr;
        ID3D12Heap* heap = null;

        if (m_Device4 != null)
        {
            Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));
            hr = m_Device4->CreateHeap1(&heapDesc, committedAllocParams.m_ProtectedSession, __uuidof<ID3D12Heap>(), (void**)(&heap));
        }
        else
        {
            if (committedAllocParams.m_ProtectedSession == null)
            {
                hr = m_Device->CreateHeap(&heapDesc, __uuidof<ID3D12Heap>(), (void**)(&heap));
            }
            else
            {
                hr = E_NOINTERFACE;
            }
        }

        if (SUCCEEDED(hr))
        {
            *ppAllocation = m_AllocationObjectAllocator.Allocate((D3D12MA_AllocatorPimpl*)(Unsafe.AsPointer(ref this)), allocInfo.SizeInBytes, allocInfo.Alignment);
            SetResidencyPriority((ID3D12Pageable*)(heap), committedAllocParams.m_ResidencyPriority);

            (*ppAllocation)->InitHeap(committedAllocParams.m_List, heap);
            (*ppAllocation)->SetPrivateData(pPrivateData);
            committedAllocParams.m_List->Register(*ppAllocation);

            uint memSegmentGroup = HeapPropertiesToMemorySegmentGroup(committedAllocParams.m_HeapProperties);
            m_Budget.AddBlock(memSegmentGroup, allocInfo.SizeInBytes);
            m_Budget.AddAllocation(memSegmentGroup, allocInfo.SizeInBytes);
        }

        return hr;
    }

    private HRESULT CalcAllocationParams([NativeTypeName("const D3D12MA::ALLOCATION_DESC &")] in D3D12MA_ALLOCATION_DESC allocDesc, [NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("const D3D12_RESOURCE_DESC_T *")] D3D12_RESOURCE_DESC* resDesc, [NativeTypeName("D3D12MA::BlockVector *&")] out D3D12MA_BlockVector* outBlockVector, [NativeTypeName("D3D12MA::CommittedAllocationParameters &")] out D3D12MA_CommittedAllocationParameters outCommittedAllocationParams, [NativeTypeName("bool &")] out bool outPreferCommitted)
    {
        outBlockVector = null;
        outCommittedAllocationParams = new D3D12MA_CommittedAllocationParameters();
        outPreferCommitted = false;

        if ((allocDesc.HeapType == D3D12_HEAP_TYPE_GPU_UPLOAD_COPY) && !IsGPUUploadHeapSupported())
        {
            return E_NOTIMPL;
        }

        bool msaaAlwaysCommitted;

        if (allocDesc.CustomPool != null)
        {
            D3D12MA_PoolPimpl* pool = allocDesc.CustomPool->m_Pimpl;

            msaaAlwaysCommitted = pool->GetBlockVector()->DeniesMsaaTextures();

            if (!pool->AlwaysCommitted())
            {
                outBlockVector = pool->GetBlockVector();
            }

            ref readonly var desc = ref pool->GetDesc();
            outCommittedAllocationParams.m_ProtectedSession = desc.pProtectedSession;
            outCommittedAllocationParams.m_HeapProperties = desc.HeapProperties;
            outCommittedAllocationParams.m_HeapFlags = desc.HeapFlags;
            outCommittedAllocationParams.m_List = pool->GetCommittedAllocationList();
            outCommittedAllocationParams.m_ResidencyPriority = pool->GetDesc().ResidencyPriority;
        }
        else
        {
            if (!D3D12MA_IsHeapTypeStandard(allocDesc.HeapType))
            {
                return E_INVALIDARG;
            }
            msaaAlwaysCommitted = m_MsaaAlwaysCommitted;

            outCommittedAllocationParams.m_HeapProperties = D3D12MA_StandardHeapTypeToHeapProperties(allocDesc.HeapType);
            outCommittedAllocationParams.m_HeapFlags = allocDesc.ExtraHeapFlags;
            outCommittedAllocationParams.m_List = (D3D12MA_CommittedAllocationList*)(Unsafe.AsPointer(ref m_CommittedAllocations[(int)(D3D12MA_StandardHeapTypeToIndex(allocDesc.HeapType))]));

            D3D12MA_ResourceClass resourceClass = (resDesc != null) ? D3D12MA_ResourceDescToResourceClass(*resDesc) : D3D12MA_HeapFlagsToResourceClass(allocDesc.ExtraHeapFlags);
            uint defaultPoolIndex = CalcDefaultPoolIndex(allocDesc, resourceClass);

            if (defaultPoolIndex != uint.MaxValue)
            {
                outBlockVector = m_BlockVectors[(int)(defaultPoolIndex)].Value;
                ulong preferredBlockSize = outBlockVector->GetPreferredBlockSize();

                if (allocSize > preferredBlockSize)
                {
                    outBlockVector = null;
                }
                else if (allocSize > preferredBlockSize / 2)
                {
                    // Heuristics: Allocate committed memory if requested size if greater than half of preferred block size.
                    outPreferCommitted = true;
                }
            }
        }

        if (((allocDesc.Flags & D3D12MA_ALLOCATION_FLAG_COMMITTED) != 0) || m_AlwaysCommitted)
        {
            outBlockVector = null;
        }

        if ((allocDesc.Flags & D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
        {
            outCommittedAllocationParams.m_List = null;
        }

        outCommittedAllocationParams.m_CanAlias = (allocDesc.Flags & D3D12MA_ALLOCATION_FLAG_CAN_ALIAS) != 0;

        if (resDesc != null)
        {
            if (resDesc->SampleDesc.Count > 1 && msaaAlwaysCommitted)
            {
                outBlockVector = null;
            }

            if (!outPreferCommitted && PrefersCommittedAllocation(resDesc, allocDesc.Flags & D3D12MA_ALLOCATION_FLAG_STRATEGY_MASK))
            {
                outPreferCommitted = true;
            }
        }

        return ((outBlockVector != null) || (outCommittedAllocationParams.m_List != null)) ? S_OK : E_INVALIDARG;
    }

    private HRESULT CalcAllocationParams([NativeTypeName("const D3D12MA::ALLOCATION_DESC &")] in D3D12MA_ALLOCATION_DESC allocDesc, [NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("const D3D12_RESOURCE_DESC_T *")] D3D12_RESOURCE_DESC1* resDesc, [NativeTypeName("D3D12MA::BlockVector *&")] out D3D12MA_BlockVector* outBlockVector, [NativeTypeName("D3D12MA::CommittedAllocationParameters &")] out D3D12MA_CommittedAllocationParameters outCommittedAllocationParams, [NativeTypeName("bool &")] out bool outPreferCommitted)
    {
        return CalcAllocationParams(allocDesc, allocSize, (D3D12_RESOURCE_DESC*)(resDesc), out outBlockVector, out outCommittedAllocationParams, out outPreferCommitted);
    }

    // Returns uint.MaxValue if index cannot be calculcated.
    [return: NativeTypeName("UINT")]
    private readonly uint CalcDefaultPoolIndex([NativeTypeName("const D3D12MA::ALLOCATION_DESC &")] in D3D12MA_ALLOCATION_DESC allocDesc, D3D12MA_ResourceClass resourceClass)
    {
        D3D12_HEAP_FLAGS extraHeapFlags = allocDesc.ExtraHeapFlags & ~D3D12MA_RESOURCE_CLASS_HEAP_FLAGS;

        if (D3D12MA_CREATE_NOT_ZEROED_AVAILABLE != 0)
        {
            extraHeapFlags &= ~D3D12_HEAP_FLAG_CREATE_NOT_ZEROED;
        }

        if (extraHeapFlags != 0)
        {
            return uint.MaxValue;
        }

        uint poolIndex = uint.MaxValue;

        switch (allocDesc.HeapType)
        {
            case D3D12_HEAP_TYPE_DEFAULT:
            {
                poolIndex = 0;
                break;
            }

            case D3D12_HEAP_TYPE_UPLOAD:
            {
                poolIndex = 1;
                break;
            }

            case D3D12_HEAP_TYPE_READBACK:
            {
                poolIndex = 2;
                break;
            }

            case D3D12_HEAP_TYPE_GPU_UPLOAD_COPY:
            {
                poolIndex = 3;
                break;
            }

            default:
            {
                D3D12MA_FAIL();
                break;
            }
        }

        if (SupportsResourceHeapTier2())
        {
            return poolIndex;
        }
        else
        {
            switch (resourceClass)
            {
                case D3D12MA_ResourceClass.Buffer:
                {
                    return poolIndex * 3;
                }

                case D3D12MA_ResourceClass.Non_RT_DS_Texture:
                {
                    return poolIndex * 3 + 1;
                }

                case D3D12MA_ResourceClass.RT_DS_Texture:
                {
                    return poolIndex * 3 + 2;
                }

                default:
                {
                    return uint.MaxValue;
                }
            }
        }
    }

    private readonly void CalcDefaultPoolParams([NativeTypeName("D3D12_HEAP_TYPE &")] out D3D12_HEAP_TYPE outHeapType, [NativeTypeName("D3D12_HEAP_FLAGS &")] out D3D12_HEAP_FLAGS outHeapFlags, [NativeTypeName("UINT")] uint index)
    {
        outHeapType = D3D12_HEAP_TYPE_DEFAULT;
        outHeapFlags = D3D12_HEAP_FLAG_NONE;

        if (!SupportsResourceHeapTier2())
        {
            switch (index % 3)
            {
                case 0:
                {
                    outHeapFlags = D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;
                    break;
                }
                
                case 1:
                {
                    outHeapFlags = D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES;
                    break;
                }

                case 2:
                {
                    outHeapFlags = D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;
                    break;
                }
            }

            index /= 3;
        }

        switch (index)
        {
            case 0:
            {
                outHeapType = D3D12_HEAP_TYPE_DEFAULT;
                break;
            }

            case 1:
            {
                outHeapType = D3D12_HEAP_TYPE_UPLOAD;
                break;
            }

            case 2:
            {
                outHeapType = D3D12_HEAP_TYPE_READBACK;
                break;
            }

            case 3:
            {
                outHeapType = D3D12_HEAP_TYPE_GPU_UPLOAD_COPY;
                break;
            }

            default:
            {
                D3D12MA_FAIL();
                break;
            }
        }
    }

    // Registers Pool object in m_Pools.
    internal void RegisterPool(D3D12MA_Pool* pool, D3D12_HEAP_TYPE heapType)
    {
        uint heapTypeIndex = (uint)(heapType - 1);

        using D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_PoolsMutex[(int)(heapTypeIndex)], m_UseMutex);
        m_Pools[(int)(heapTypeIndex)].PushBack(pool->m_Pimpl);
    }

    // Unregisters Pool object from m_Pools.
    internal void UnregisterPool(D3D12MA_Pool* pool, D3D12_HEAP_TYPE heapType)
    {
        uint heapTypeIndex = (uint)(heapType - 1);

        using D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_PoolsMutex[(int)(heapTypeIndex)], m_UseMutex);
        m_Pools[(int)(heapTypeIndex)].Remove(pool->m_Pimpl);
    }

    private HRESULT UpdateD3D12Budget()
    {
        if (D3D12MA_DXGI_1_4 != 0)
        {
            if (m_Adapter3 != null)
            {
                return m_Budget.UpdateBudget(m_Adapter3, m_UseMutex);
            }
            else
            {
                return E_NOINTERFACE;
            }
        }
        else
        {
            return S_OK;
        }
    }

    private readonly D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfoNative([NativeTypeName("const D3D12_RESOURCE_DESC &")] D3D12_RESOURCE_DESC* resourceDesc)
    {
        return m_Device->GetResourceAllocationInfo(0, 1, resourceDesc);
    }

    private readonly HRESULT GetResourceAllocationInfoMiddle([NativeTypeName("D3D12_RESOURCE_DESC &")] D3D12_RESOURCE_DESC* inOutResourceDesc, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("D3D12_RESOURCE_ALLOCATION_INFO &")] out D3D12_RESOURCE_ALLOCATION_INFO outAllocInfo)
    {
        if (NumCastableFormats > 0)
        {
            Unsafe.SkipInit(out outAllocInfo);
            return E_NOTIMPL;
        }

        outAllocInfo = GetResourceAllocationInfoNative(inOutResourceDesc);
        return (outAllocInfo.SizeInBytes != UINT64_MAX) ? S_OK : E_INVALIDARG;
    }

    private readonly D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfo2Native([NativeTypeName("const D3D12_RESOURCE_DESC1 &")] D3D12_RESOURCE_DESC1* resourceDesc)
    {
        D3D12MA_ASSERT(m_Device8 != null);
        Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));

        D3D12_RESOURCE_ALLOCATION_INFO1 info1Unused;
        return m_Device8->GetResourceAllocationInfo2(0, 1, resourceDesc, &info1Unused);
    }

    private readonly HRESULT GetResourceAllocationInfoMiddle([NativeTypeName("D3D12_RESOURCE_DESC1 &")] D3D12_RESOURCE_DESC1* inOutResourceDesc, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("D3D12_RESOURCE_ALLOCATION_INFO &")] out D3D12_RESOURCE_ALLOCATION_INFO outAllocInfo)
    {
        if (NumCastableFormats > 0)
        {
            if (m_Device12 != null)
            {
                outAllocInfo = GetResourceAllocationInfo3Native(inOutResourceDesc, NumCastableFormats, pCastableFormats);
                return outAllocInfo.SizeInBytes != UINT64_MAX ? S_OK : E_INVALIDARG;
            }
        }

        outAllocInfo = GetResourceAllocationInfo2Native(inOutResourceDesc);
        return outAllocInfo.SizeInBytes != UINT64_MAX ? S_OK : E_INVALIDARG;
    }

    private readonly D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfo3Native([NativeTypeName("const D3D12_RESOURCE_DESC1 &")] D3D12_RESOURCE_DESC1* resourceDesc, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats)
    {
        D3D12MA_ASSERT(m_Device12 != null);
        Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));

        D3D12_RESOURCE_ALLOCATION_INFO1 info1Unused;
        return m_Device12->GetResourceAllocationInfo3(0, 1, resourceDesc, &NumCastableFormats, &pCastableFormats, &info1Unused);
    }

    private readonly HRESULT GetResourceAllocationInfo([NativeTypeName("D3D12_RESOURCE_DESC_T &")] D3D12_RESOURCE_DESC* inOutResourceDesc, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("D3D12_RESOURCE_ALLOCATION_INFO &")] out D3D12_RESOURCE_ALLOCATION_INFO outAllocInfo)
    {
        // Optional optimization: Microsoft documentation says:
        //    https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-getresourceallocationinfo
        //
        // Your application can forgo using GetResourceAllocationInfo for buffer resources
        // (D3D12_RESOURCE_DIMENSION_BUFFER). Buffers have the same size on all adapters,
        // which is merely the smallest multiple of 64KB that's greater or equal to
        // D3D12_RESOURCE_DESC::Width.

        if ((inOutResourceDesc->Alignment == 0) && (inOutResourceDesc->Dimension == D3D12_RESOURCE_DIMENSION_BUFFER))
        {
            outAllocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
                SizeInBytes = D3D12MA_AlignUp(inOutResourceDesc->Width, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT),
                Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
            };
            return S_OK;
        }

        if (D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT != 0)
        {
            if ((inOutResourceDesc->Alignment == 0) && (inOutResourceDesc->Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE2D) && ((inOutResourceDesc->Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) == 0) && ((D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT != 1) || D3D12MA_CanUseSmallAlignment(*inOutResourceDesc)))
            {
                // The algorithm here is based on Microsoft sample: "Small Resources Sample"
                // https://github.com/microsoft/DirectX-Graphics-Samples/tree/master/Samples/Desktop/D3D12SmallResources

                ulong smallAlignmentToTry = (uint)((inOutResourceDesc->SampleDesc.Count > 1) ? D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT : D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT);
                inOutResourceDesc->Alignment = smallAlignmentToTry;

                HRESULT hr = GetResourceAllocationInfoMiddle(inOutResourceDesc, NumCastableFormats, pCastableFormats, out outAllocInfo);

                // Check if alignment requested has been granted.
                if (SUCCEEDED(hr) && (outAllocInfo.Alignment == smallAlignmentToTry))
                {
                    return S_OK;
                }

                inOutResourceDesc->Alignment = 0; // Restore original
            }
        }

        return GetResourceAllocationInfoMiddle(inOutResourceDesc, NumCastableFormats, pCastableFormats, out outAllocInfo);
    }

    private readonly HRESULT GetResourceAllocationInfo([NativeTypeName("D3D12_RESOURCE_DESC_T &")] D3D12_RESOURCE_DESC1* inOutResourceDesc, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("D3D12_RESOURCE_ALLOCATION_INFO &")] out D3D12_RESOURCE_ALLOCATION_INFO outAllocInfo)
    {
        return GetResourceAllocationInfo((D3D12_RESOURCE_DESC*)(inOutResourceDesc), NumCastableFormats, pCastableFormats, out outAllocInfo);
    }

    private bool NewAllocationWithinBudget(D3D12_HEAP_TYPE heapType, [NativeTypeName("UINT64")] ulong size)
    {
        GetBudgetForHeapType(out D3D12MA_Budget budget, heapType);
        return budget.UsageBytes + size <= budget.BudgetBytes;
    }

    // Writes object { } with data of given budget.
    private static void WriteBudgetToJson([NativeTypeName("D3D12MA::JsonWriter &")] D3D12MA_JsonWriter* pJson, [NativeTypeName("const D3D12MA::Budget &")] in D3D12MA_Budget budget)
    {
        ref D3D12MA_JsonWriter json = ref *pJson;

        json.BeginObject();
        {
            json.WriteString("BudgetBytes");
            json.WriteNumber(budget.BudgetBytes);

            json.WriteString("UsageBytes");
            json.WriteNumber(budget.UsageBytes);
        }
        json.EndObject();
    }

    [InlineArray((int)(D3D12MA_HEAP_TYPE_COUNT))]
    private partial struct _m_PoolsMutex_e__FixedBuffer
    {
        public D3D12MA_RW_MUTEX e0;
    }

    [InlineArray((int)(D3D12MA_HEAP_TYPE_COUNT))]
    private partial struct _m_Pools_e__FixedBuffer : IDisposable
    {
        public D3D12MA_IntrusiveLinkedList<D3D12MA_PoolListItemTraits, D3D12MA_PoolPimpl> e0;

        public readonly void Dispose()
        {
            this[0].Dispose();
            this[1].Dispose();
            this[2].Dispose();
            this[3].Dispose();
        }
    }

    [InlineArray((int)(D3D12MA_DEFAULT_POOL_MAX_COUNT))]
    private partial struct _m_BlockVectors_e__FixedBuffer
    {
        public Pointer<D3D12MA_BlockVector> e0;
    }

    [InlineArray((int)(D3D12MA_STANDARD_HEAP_TYPE_COUNT))]
    private partial struct _m_CommittedAllocations_e__FixedBuffer
    {
        public D3D12MA_CommittedAllocationList e0;

        public readonly void Dispose()
        {
            this[0].Dispose();
            this[1].Dispose();
            this[2].Dispose();
        }
    }
}
