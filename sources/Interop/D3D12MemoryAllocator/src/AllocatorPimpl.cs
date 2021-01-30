// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.D3D12_RESOURCE_HEAP_TIER;
using static TerraFX.Interop.ALLOCATOR_FLAGS;
using static TerraFX.Interop.D3D12_FEATURE;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.ALLOCATION_FLAGS;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DXGI_MEMORY_SEGMENT_GROUP;

using AllocationVectorType = TerraFX.Interop.Vector<TerraFX.Interop.Ptr<TerraFX.Interop.Allocation>>;
using PoolVectorType = TerraFX.Interop.Vector<TerraFX.Interop.Ptr<TerraFX.Interop.Pool>>;

namespace TerraFX.Interop
{
    internal unsafe struct AllocatorPimpl : IDisposable
    {
        public CurrentBudgetData m_Budget;

#pragma warning disable CS0649
        readonly bool m_UseMutex;
        readonly bool m_AlwaysCommitted;
#pragma warning restore CS0649
        ID3D12Device* m_Device; // AddRef
        ID3D12Device4* m_Device4; // AddRef, optional
        ID3D12Device8* m_Device8; // AddRef, optional
        IDXGIAdapter* m_Adapter; // AddRef
        IDXGIAdapter3* m_Adapter3; //AddRef, optional

        [NativeTypeName("UINT64")]
        ulong m_PreferredBlockSize;

        ALLOCATION_CALLBACKS m_AllocationCallbacks;

        [NativeTypeName("D3D12MA_ATOMIC_UINT32")]
        atomic<uint> m_CurrentFrameIndex;

        DXGI_ADAPTER_DESC m_AdapterDesc;
        D3D12_FEATURE_DATA_D3D12_OPTIONS m_D3D12Options;
        AllocationObjectAllocator m_AllocationObjectAllocator;

        [NativeTypeName("AllocationVectorType* m_pCommittedAllocations[HEAP_TYPE_COUNT]")]
        __m_Buffer_HEAP_TYPE_COUNT_e__FixedBuffer<Ptr<AllocationVectorType>> m_pCommittedAllocations;

        [NativeTypeName("D3D12MA_RW_MUTEX m_CommittedAllocationsMutex[HEAP_TYPE_COUNT]")]
        __m_Buffer_HEAP_TYPE_COUNT_e__FixedBuffer<D3D12MA_RW_MUTEX> m_CommittedAllocationsMutex;

        [NativeTypeName("PoolVectorType* m_pPools[HEAP_TYPE_COUNT]")]
        __m_Buffer_HEAP_TYPE_COUNT_e__FixedBuffer<Ptr<PoolVectorType>> m_pPools;

        [NativeTypeName("D3D12MA_RW_MUTEX m_PoolsMutex[HEAP_TYPE_COUNT]")]
        __m_Buffer_HEAP_TYPE_COUNT_e__FixedBuffer<D3D12MA_RW_MUTEX> m_PoolsMutex;

        // Default pools.
        [NativeTypeName("BlockVector* m_BlockVectors[DEFAULT_POOL_MAX_COUNT]")]
        __m_Buffer_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<Ptr<BlockVector>> m_BlockVectors;

        // # Used only when ResourceHeapTier = 1
        [NativeTypeName("UINT64 m_DefaultPoolTier1MinBytes[DEFAULT_POOL_MAX_COUNT]")]
        __m_Buffer_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<ulong> m_DefaultPoolTier1MinBytes; // Default 0

        [NativeTypeName("UINT64 m_DefaultPoolHeapTypeMinBytes[HEAP_TYPE_COUNT]")]
        __m_Buffer_HEAP_TYPE_COUNT_e__FixedBuffer<ulong> m_DefaultPoolHeapTypeMinBytes; // Default UINT64_MAX, meaning not set

        D3D12MA_RW_MUTEX m_DefaultPoolMinBytesMutex;

        // Explicit constructor as a normal instance method: this is needed to ensure the code is executed in-place over the
        // AllocatorPimpl instance being initialized, and not on a local variable which is then copied over to the
        // target memory location, which would break the references to self fields being used in the code below.
        public void Ctor(ALLOCATION_CALLBACKS* allocationCallbacks, ALLOCATOR_DESC* desc)
        {
            Unsafe.AsRef(m_UseMutex) = ((int)desc->Flags & (int)ALLOCATOR_FLAG_SINGLETHREADED) == 0;
            Unsafe.AsRef(m_AlwaysCommitted) = ((int)desc->Flags & (int)ALLOCATOR_FLAG_ALWAYS_COMMITTED) != 0;
            m_Device = desc->pDevice;
            m_Adapter = desc->pAdapter;
            m_PreferredBlockSize = desc->PreferredBlockSize != 0 ? desc->PreferredBlockSize : D3D12MA_DEFAULT_BLOCK_SIZE;
            m_AllocationCallbacks = *allocationCallbacks;
            m_CurrentFrameIndex = default;
            // Below this line don't use allocationCallbacks but m_AllocationCallbacks!!!
            m_AllocationObjectAllocator = new AllocationObjectAllocator((ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref m_AllocationCallbacks));

            // desc.pAllocationCallbacks intentionally ignored here, preprocessed by CreateAllocator.
            m_D3D12Options = default;

            m_pCommittedAllocations = default;
            m_pPools = default;
            m_BlockVectors = default;
            m_DefaultPoolTier1MinBytes = default;

            Unsafe.SkipInit(out m_DefaultPoolHeapTypeMinBytes);
            for (uint i = 0; i < HEAP_TYPE_COUNT; ++i)
            {
                m_DefaultPoolHeapTypeMinBytes[(int)i] = UINT64_MAX;
            }

            for (uint heapTypeIndex = 0; heapTypeIndex < HEAP_TYPE_COUNT; ++heapTypeIndex)
            {
                AllocationVectorType* p0 = m_pCommittedAllocations[(int)heapTypeIndex] = D3D12MA_NEW<AllocationVectorType>(GetAllocs());
                *p0 = new AllocationVectorType(GetAllocs());
                PoolVectorType* p1 = m_pPools[(int)heapTypeIndex] = D3D12MA_NEW<PoolVectorType>(GetAllocs());
                *p1 = new PoolVectorType(GetAllocs());
            }

            m_Device->AddRef();
            m_Adapter->AddRef();
        }

        [return: NativeTypeName("HRESULT")]
        public int Init(ALLOCATOR_DESC* desc)
        {
            AllocatorPimpl* @this = (AllocatorPimpl*)Unsafe.AsPointer(ref this);

            if (D3D12MA_DXGI_1_4 > 0)
            {
                desc->pAdapter->QueryInterface(__uuidof<IDXGIAdapter3>(), (void**)&@this->m_Adapter3);
            }

            m_Device->QueryInterface(__uuidof<ID3D12Device4>(), (void**)&@this->m_Device4);

            m_Device->QueryInterface(__uuidof<ID3D12Device8>(), (void**)&@this->m_Device8);

            HRESULT hr = m_Adapter->GetDesc(&@this->m_AdapterDesc);
            if (FAILED(hr))
            {
                return hr;
            }

            hr = m_Device->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, &@this->m_D3D12Options, (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS));
            if (FAILED(hr))
            {
                return hr;
            }

            uint defaultPoolCount = CalcDefaultPoolCount();
            for (uint i = 0; i < defaultPoolCount; ++i)
            {
                D3D12_HEAP_TYPE heapType;
                D3D12_HEAP_FLAGS heapFlags;
                CalcDefaultPoolParams(&heapType, &heapFlags, i);

                BlockVector* p = m_BlockVectors[(int)i] = D3D12MA_NEW<BlockVector>(GetAllocs());
                *p = new BlockVector(
                    @this, // hAllocator
                    heapType, // heapType
                    heapFlags, // heapFlags
                    m_PreferredBlockSize,
                    0, // minBlockCount
                    nuint.MaxValue, // maxBlockCount
                    false); // explicitBlockSize
                            // No need to call m_pBlockVectors[i]->CreateMinBlocks here, becase minBlockCount is 0.
            }

            if (D3D12MA_DXGI_1_4 > 0 && m_Adapter3 != null)
            {
                UpdateD3D12Budget();
            }

            return S_OK;
        }

        public void Dispose()
        {
            SAFE_RELEASE(ref m_Device8);

            SAFE_RELEASE(ref m_Device4);

            if (D3D12MA_DXGI_1_4 > 0)
            {
                SAFE_RELEASE(ref m_Adapter3);
            }

            SAFE_RELEASE(ref m_Adapter);
            SAFE_RELEASE(ref m_Device);

            for (uint i = DEFAULT_POOL_MAX_COUNT; unchecked(i-- > 0);)
            {
                D3D12MA_DELETE<BlockVector>(GetAllocs(), m_BlockVectors[(int)i]);
            }

            for (uint i = HEAP_TYPE_COUNT; unchecked(i-- > 0);)
            {
                if (m_pPools[(int)i].Value != null && !m_pPools[(int)i].Value->empty())
                {
                    D3D12MA_ASSERT(false); // "Unfreed pools found!"
                }

                D3D12MA_DELETE(GetAllocs(), m_pPools[(int)i].Value);
            }

            for (uint i = HEAP_TYPE_COUNT; unchecked(i-- > 0);)
            {
                if (m_pCommittedAllocations[(int)i].Value != null && !m_pCommittedAllocations[(int)i].Value->empty())
                {
                    D3D12MA_ASSERT(false); // "Unfreed committed allocations found!"
                }

                D3D12MA_DELETE(GetAllocs(), m_pCommittedAllocations[(int)i].Value);
            }
        }

        public ID3D12Device* GetDevice() => m_Device;

        public ID3D12Device4* GetDevice4() => m_Device4;

        public ID3D12Device8* GetDevice8() => m_Device8;

        // Shortcut for "Allocation Callbacks", because this function is called so often.
        public readonly ALLOCATION_CALLBACKS* GetAllocs() => (ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref Unsafe.AsRef(m_AllocationCallbacks));

        public readonly D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options() => (D3D12_FEATURE_DATA_D3D12_OPTIONS*)Unsafe.AsPointer(ref Unsafe.AsRef(m_D3D12Options));

        public readonly bool SupportsResourceHeapTier2() => m_D3D12Options.ResourceHeapTier >= D3D12_RESOURCE_HEAP_TIER_2;

        public readonly bool UseMutex() => m_UseMutex;

        public AllocationObjectAllocator* GetAllocationObjectAllocator() => (AllocationObjectAllocator*)Unsafe.AsPointer(ref m_AllocationObjectAllocator);

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
                int allowedGroupCount = (allowBuffers ? 1 : 0) + (allowRtDsTextures ? 1 : 0) + (allowNonRtDsTextures ? 1 : 0);
                return allowedGroupCount == 1;
            }
        }

        [return: NativeTypeName("HRESULT")]
        public int CreateResource(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            *ppAllocation = null;
            if (ppvResource != null)
            {
                *ppvResource = null;
            }

            if (pAllocDesc->CustomPool == null && !IsHeapTypeValid(pAllocDesc->HeapType))
            {
                return E_INVALIDARG;
            }

            ALLOCATION_DESC finalAllocDesc = *pAllocDesc;

            D3D12_RESOURCE_DESC finalResourceDesc = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&finalResourceDesc);
            resAllocInfo.Alignment = D3D12MA_MAX(resAllocInfo.Alignment, D3D12MA_DEBUG_ALIGNMENT);
            D3D12MA_ASSERT(IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT(resAllocInfo.SizeInBytes > 0);

            if (pAllocDesc->CustomPool != null)
            {
                if ((finalAllocDesc.Flags & ALLOCATION_FLAG_COMMITTED) != 0)
                {
                    return E_INVALIDARG;
                }

                BlockVector* blockVector = pAllocDesc->CustomPool->m_Pimpl->GetBlockVector();
                D3D12MA_ASSERT(blockVector != null);
                return blockVector->CreateResource(
                    resAllocInfo.SizeInBytes,
                    resAllocInfo.Alignment,
                    &finalAllocDesc,
                    &finalResourceDesc,
                    InitialResourceState,
                    pOptimizedClearValue,
                    ppAllocation,
                    riidResource,
                    ppvResource);
            }
            else
            {
                uint defaultPoolIndex = CalcDefaultPoolIndex(pAllocDesc, &finalResourceDesc);
                bool requireCommittedMemory = defaultPoolIndex == uint.MaxValue;
                if (requireCommittedMemory)
                {
                    return AllocateCommittedResource(
                        &finalAllocDesc,
                        &finalResourceDesc,
                        &resAllocInfo,
                        InitialResourceState,
                        pOptimizedClearValue,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                }

                BlockVector* blockVector = m_BlockVectors[(int)defaultPoolIndex];
                D3D12MA_ASSERT(blockVector != null);

                ulong preferredBlockSize = blockVector->GetPreferredBlockSize();
                bool preferCommittedMemory =
                    m_AlwaysCommitted ||
                    PrefersCommittedAllocation(&finalResourceDesc) ||
                    // Heuristics: Allocate committed memory if requested size if greater than half of preferred block size.
                    resAllocInfo.SizeInBytes > preferredBlockSize / 2;
                if (preferCommittedMemory &&
                    (finalAllocDesc.Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) == 0)
                {
                    finalAllocDesc.Flags |= ALLOCATION_FLAG_COMMITTED;
                }

                if ((finalAllocDesc.Flags & ALLOCATION_FLAG_COMMITTED) != 0)
                {
                    return AllocateCommittedResource(
                        &finalAllocDesc,
                        &finalResourceDesc,
                        &resAllocInfo,
                        InitialResourceState,
                        pOptimizedClearValue,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                }
                else
                {
                    HRESULT hr = blockVector->CreateResource(
                        resAllocInfo.SizeInBytes,
                        resAllocInfo.Alignment,
                        &finalAllocDesc,
                        &finalResourceDesc,
                        InitialResourceState,
                        pOptimizedClearValue,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                    if (SUCCEEDED(hr))
                    {
                        return hr;
                    }

                    return AllocateCommittedResource(
                        &finalAllocDesc,
                        &finalResourceDesc,
                        &resAllocInfo,
                        InitialResourceState,
                        pOptimizedClearValue,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                }
            }
        }

        [return: NativeTypeName("HRESULT")]
        public int CreateResource1(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if (m_Device4 == null)
            {
                return E_NOINTERFACE;
            }

            // Fall back to old implementation
            if (pProtectedSession == null)
            {
                return CreateResource(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, ppAllocation, riidResource, ppvResource);
            }

            *ppAllocation = null;
            if (ppvResource != null)
            {
                *ppvResource = null;
            }

            // In current implementation it must always be allocated as committed.
            if (pAllocDesc->CustomPool != null ||
                (pAllocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                return E_INVALIDARG;
            }

            D3D12_RESOURCE_DESC finalResourceDesc = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&finalResourceDesc);
            resAllocInfo.Alignment = D3D12MA_MAX(resAllocInfo.Alignment, D3D12MA_DEBUG_ALIGNMENT);
            D3D12MA_ASSERT(IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT(resAllocInfo.SizeInBytes > 0);

            return AllocateCommittedResource1(
                pAllocDesc,
                &finalResourceDesc,
                &resAllocInfo,
                InitialResourceState,
                pOptimizedClearValue,
                pProtectedSession,
                ppAllocation,
                riidResource,
                ppvResource);
        }

        [return: NativeTypeName("HRESULT")]
        public int CreateResource2(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            *ppAllocation = null;
            if (ppvResource != null)
            {
                *ppvResource = null;
            }

            if (m_Device8 == null)
            {
                return E_NOINTERFACE;
            }

            if (pAllocDesc->CustomPool == null && !IsHeapTypeValid(pAllocDesc->HeapType))
            {
                return E_INVALIDARG;
            }

            ALLOCATION_DESC finalAllocDesc = *pAllocDesc;

            D3D12_RESOURCE_DESC1 finalResourceDesc = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&finalResourceDesc);
            resAllocInfo.Alignment = D3D12MA_MAX(resAllocInfo.Alignment, D3D12MA_DEBUG_ALIGNMENT);
            D3D12MA_ASSERT(IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT(resAllocInfo.SizeInBytes > 0);

            bool requireCommittedMemory = pProtectedSession != null || (finalAllocDesc.Flags & ALLOCATION_FLAG_COMMITTED) != 0;

            if (pAllocDesc->CustomPool != null)
            {
                if (requireCommittedMemory)
                {
                    return E_INVALIDARG;
                }

                BlockVector* blockVector = pAllocDesc->CustomPool->m_Pimpl->GetBlockVector();
                D3D12MA_ASSERT(blockVector != null);
                return blockVector->CreateResource2(
                    resAllocInfo.SizeInBytes,
                    resAllocInfo.Alignment,
                    &finalAllocDesc,
                    &finalResourceDesc,
                    InitialResourceState,
                    pOptimizedClearValue,
                    pProtectedSession,
                    ppAllocation,
                    riidResource,
                    ppvResource);
            }
            else
            {
                uint defaultPoolIndex = CalcDefaultPoolIndex(pAllocDesc, &finalResourceDesc);
                requireCommittedMemory = requireCommittedMemory || defaultPoolIndex == UINT32_MAX;
                if (requireCommittedMemory)
                {
                    return AllocateCommittedResource2(
                        &finalAllocDesc,
                        &finalResourceDesc,
                        &resAllocInfo,
                        InitialResourceState,
                        pOptimizedClearValue,
                        pProtectedSession,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                }

                BlockVector* blockVector = m_BlockVectors[(int)defaultPoolIndex];
                D3D12MA_ASSERT(blockVector != null);

                ulong preferredBlockSize = blockVector->GetPreferredBlockSize();
                bool preferCommittedMemory =
                    m_AlwaysCommitted ||
                    PrefersCommittedAllocation(&finalResourceDesc) ||
                    // Heuristics: Allocate committed memory if requested size if greater than half of preferred block size.
                    resAllocInfo.SizeInBytes > preferredBlockSize / 2;
                if (preferCommittedMemory &&
                    (finalAllocDesc.Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) == 0)
                {
                    finalAllocDesc.Flags |= ALLOCATION_FLAG_COMMITTED;
                }

                if ((finalAllocDesc.Flags & ALLOCATION_FLAG_COMMITTED) != 0)
                {
                    return AllocateCommittedResource2(
                        &finalAllocDesc,
                        &finalResourceDesc,
                        &resAllocInfo,
                        InitialResourceState,
                        pOptimizedClearValue,
                        pProtectedSession,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                }
                else
                {
                    HRESULT hr = blockVector->CreateResource2(
                        resAllocInfo.SizeInBytes,
                        resAllocInfo.Alignment,
                        &finalAllocDesc,
                        &finalResourceDesc,
                        InitialResourceState,
                        pOptimizedClearValue,
                        pProtectedSession,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                    if (SUCCEEDED(hr))
                    {
                        return hr;
                    }

                    return AllocateCommittedResource2(
                        &finalAllocDesc,
                        &finalResourceDesc,
                        &resAllocInfo,
                        InitialResourceState,
                        pOptimizedClearValue,
                        pProtectedSession,
                        ppAllocation,
                        riidResource,
                        ppvResource);
                }
            }
        }

        [return: NativeTypeName("HRESULT")]
        public int AllocateMemory(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, Allocation** ppAllocation)
        {
            *ppAllocation = null;

            if (pAllocDesc->CustomPool != null)
            {
                BlockVector* blockVector = pAllocDesc->CustomPool->m_Pimpl->GetBlockVector();
                D3D12MA_ASSERT(blockVector != null);
                return blockVector->Allocate(
                    pAllocInfo->SizeInBytes,
                    pAllocInfo->Alignment,
                    pAllocDesc,
                    1,
                    (Allocation**)ppAllocation);
            }
            else
            {
                if (!IsHeapTypeValid(pAllocDesc->HeapType))
                {
                    return E_INVALIDARG;
                }

                ALLOCATION_DESC finalAllocDesc = *pAllocDesc;

                uint defaultPoolIndex = CalcDefaultPoolIndex(pAllocDesc);
                bool requireCommittedMemory = (defaultPoolIndex == UINT32_MAX);
                if (requireCommittedMemory)
                {
                    return AllocateHeap(&finalAllocDesc, pAllocInfo, ppAllocation);
                }

                BlockVector* blockVector = m_BlockVectors[(int)defaultPoolIndex];
                D3D12MA_ASSERT(blockVector != null);

                ulong preferredBlockSize = blockVector->GetPreferredBlockSize();
                bool preferCommittedMemory =
                    m_AlwaysCommitted ||
                    // Heuristics: Allocate committed memory if requested size if greater than half of preferred block size.
                    pAllocInfo->SizeInBytes > preferredBlockSize / 2;
                if (preferCommittedMemory &&
                    (finalAllocDesc.Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) == 0)
                {
                    finalAllocDesc.Flags |= ALLOCATION_FLAG_COMMITTED;
                }

                if ((finalAllocDesc.Flags & ALLOCATION_FLAG_COMMITTED) != 0)
                {
                    return AllocateHeap(&finalAllocDesc, pAllocInfo, ppAllocation);
                }
                else
                {
                    HRESULT hr = blockVector->Allocate(
                        pAllocInfo->SizeInBytes,
                        pAllocInfo->Alignment,
                        &finalAllocDesc,
                        1,
                        (Allocation**)ppAllocation);
                    if (SUCCEEDED(hr))
                    {
                        return hr;
                    }

                    return AllocateHeap(&finalAllocDesc, pAllocInfo, ppAllocation);
                }
            }
        }

        [return: NativeTypeName("HRESULT")]
        public int AllocateMemory1(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, ID3D12ProtectedResourceSession *pProtectedSession, Allocation** ppAllocation)
        {
            if (m_Device4 == null)
            {
                return E_NOINTERFACE;
            }

            // Fall back to old implementation
            if (pProtectedSession == null)
            {
                return AllocateMemory(pAllocDesc, pAllocInfo, ppAllocation);
            }

            *ppAllocation = null;

            // In current implementation it must always be allocated as separate CreateHeap1.
            if (pAllocDesc->CustomPool != null ||
                (pAllocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                return E_INVALIDARG;
            }

            if (!IsHeapTypeValid(pAllocDesc->HeapType))
            {
                return E_INVALIDARG;
            }

            return AllocateHeap1(pAllocDesc, pAllocInfo, pProtectedSession, ppAllocation);
        }

        [return: NativeTypeName("HRESULT")]
        public int CreateAliasingResource(Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            *ppvResource = null;

            D3D12_RESOURCE_DESC resourceDesc2 = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&resourceDesc2);
            resAllocInfo.Alignment = D3D12MA_MAX(resAllocInfo.Alignment, D3D12MA_DEBUG_ALIGNMENT);
            D3D12MA_ASSERT(IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT(resAllocInfo.SizeInBytes > 0);

            ID3D12Heap* existingHeap = pAllocation->GetHeap();
            ulong existingOffset = pAllocation->GetOffset();
            ulong existingSize = pAllocation->GetSize();
            ulong newOffset = existingOffset + AllocationLocalOffset;

            if (existingHeap == null ||
                AllocationLocalOffset + resAllocInfo.SizeInBytes > existingSize ||
                newOffset % resAllocInfo.Alignment != 0)
            {
                return E_INVALIDARG;
            }

            return m_Device->CreatePlacedResource(
                existingHeap,
                newOffset,
                &resourceDesc2,
                InitialResourceState,
                pOptimizedClearValue,
                riidResource,
                ppvResource);
        }

        [return: NativeTypeName("HRESULT")]
        public int SetDefaultHeapMinBytes(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong minBytes)
        {
            if (!IsHeapTypeValid(heapType))
            {
                D3D12MA_ASSERT(false); // "Allocator::SetDefaultHeapMinBytes: Invalid heapType passed."
                return E_INVALIDARG;
            }

            if (SupportsResourceHeapTier2())
            {
                if (heapFlags != D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES &&
                    heapFlags != D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS &&
                    heapFlags != D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES &&
                    heapFlags != D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES)
                {
                    D3D12MA_ASSERT(false); // "Allocator::SetDefaultHeapMinBytes: Invalid heapFlags passed."
                    return E_INVALIDARG;
                }

                ulong newMinBytes = UINT64_MAX;

                {
                    using MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_DefaultPoolMinBytesMutex), m_UseMutex);

                    if (heapFlags == D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES)
                    {
                        m_DefaultPoolHeapTypeMinBytes[(int)HeapTypeToIndex(heapType)] = minBytes;
                        newMinBytes = minBytes;
                    }
                    else
                    {
                        uint defaultPoolTier1Index = CalcDefaultPoolIndex(heapType, heapFlags, false);
                        m_DefaultPoolTier1MinBytes[(int)defaultPoolTier1Index] = minBytes;

                        newMinBytes = m_DefaultPoolHeapTypeMinBytes[(int)HeapTypeToIndex(heapType)];
                        if (newMinBytes == UINT64_MAX)
                        {
                            newMinBytes = m_DefaultPoolTier1MinBytes[(int)CalcDefaultPoolIndex(heapType, D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS, false)] +
                                m_DefaultPoolTier1MinBytes[(int)CalcDefaultPoolIndex(heapType, D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES, false)] +
                                m_DefaultPoolTier1MinBytes[(int)CalcDefaultPoolIndex(heapType, D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES, false)];
                        }
                    }
                }

                uint defaultPoolIndex = CalcDefaultPoolIndex(heapType, D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES);
                return m_BlockVectors[(int)defaultPoolIndex].Value->SetMinBytes(newMinBytes);
            }
            else
            {
                if (heapFlags != D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS &&
                    heapFlags != D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES &&
                    heapFlags != D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES)
                {
                    D3D12MA_ASSERT(false); // "Allocator::SetDefaultHeapMinBytes: Invalid heapFlags passed."
                    return E_INVALIDARG;
                }

                uint defaultPoolIndex = CalcDefaultPoolIndex(heapType, heapFlags);
                return m_BlockVectors[(int)defaultPoolIndex].Value->SetMinBytes(minBytes);
            }
        }

        /// <summary>
        /// Unregisters allocation from the collection of dedicated allocations.
        /// Allocation object must be deleted externally afterwards.
        /// </summary>
        public void FreeCommittedMemory([NativeTypeName("Allocation*")] ref Allocation allocation)
        {
            Allocation* pAllocation = (Allocation*)Unsafe.AsPointer(ref allocation);
            D3D12MA_ASSERT(pAllocation != null && pAllocation->m_PackedData.GetType() == Allocation.Type.TYPE_COMMITTED);
            UnregisterCommittedAllocation(pAllocation, pAllocation->m_Committed.heapType);

            ulong allocationSize = pAllocation->GetSize();
            uint heapTypeIndex = HeapTypeToIndex(pAllocation->m_Committed.heapType);
            m_Budget.RemoveAllocation(heapTypeIndex, allocationSize);
            m_Budget.m_BlockBytes[(int)heapTypeIndex].Subtract(allocationSize);
        }

        /// <summary>
        /// Unregisters allocation from the collection of placed allocations.
        /// Allocation object must be deleted externally afterwards.
        /// </summary>
        public void FreePlacedMemory([NativeTypeName("Allocation*")] ref Allocation allocation)
        {
            Allocation* pAllocation = (Allocation*)Unsafe.AsPointer(ref allocation);
            D3D12MA_ASSERT(pAllocation != null && pAllocation->m_PackedData.GetType() == Allocation.Type.TYPE_PLACED);

            NormalBlock* block = pAllocation->m_Placed.block;
            D3D12MA_ASSERT(block != null);
            BlockVector* blockVector = block->GetBlockVector();
            D3D12MA_ASSERT(blockVector != null);
            m_Budget.RemoveAllocation(HeapTypeToIndex(block->GetHeapType()), pAllocation->GetSize());
            blockVector->Free(pAllocation);
        }

        /// <summary>
        /// Unregisters allocation from the collection of dedicated allocations and destroys associated heap.
        /// Allocation object must be deleted externally afterwards.
        /// </summary>
        public void FreeHeapMemory([NativeTypeName("Allocation*")] ref Allocation allocation)
        {
            Allocation* pAllocation = (Allocation*)Unsafe.AsPointer(ref allocation);
            D3D12MA_ASSERT(pAllocation != null && pAllocation->m_PackedData.GetType() == Allocation.Type.TYPE_HEAP);
            UnregisterCommittedAllocation(pAllocation, pAllocation->m_Heap.heapType);
            SAFE_RELEASE(&pAllocation->m_Union.m_Heap.heap);

            uint heapTypeIndex = HeapTypeToIndex(pAllocation->m_Heap.heapType);
            ulong allocationSize = pAllocation->GetSize();
            m_Budget.m_BlockBytes[(int)heapTypeIndex].Subtract(allocationSize);
            m_Budget.RemoveAllocation(heapTypeIndex, allocationSize);
        }

        public void SetCurrentFrameIndex([NativeTypeName("UINT")] uint frameIndex)
        {
            m_CurrentFrameIndex.Store(frameIndex);

            if (D3D12MA_DXGI_1_4 > 0 && m_Adapter3 != null)
            {
                UpdateD3D12Budget();
            }
        }

        [return: NativeTypeName("UINT")]
        public readonly uint GetCurrentFrameIndex() => m_CurrentFrameIndex.Load();

        public void CalculateStats(Stats* outStats)
        {
            // Init stats
            ZeroMemory(outStats, (uint)sizeof(Stats));
            outStats->Total.AllocationSizeMin = UINT64_MAX;
            outStats->Total.UnusedRangeSizeMin = UINT64_MAX;
            for (nuint i = 0; i < HEAP_TYPE_COUNT; i++)
            {
                outStats->HeapType[(int)i].AllocationSizeMin = UINT64_MAX;
                outStats->HeapType[(int)i].UnusedRangeSizeMin = UINT64_MAX;
            }

            // Process deafult pools.
            for (nuint i = 0; i < HEAP_TYPE_COUNT; ++i)
            {
                BlockVector* pBlockVector = m_BlockVectors[(int)i];
                D3D12MA_ASSERT(pBlockVector != null);
                pBlockVector->AddStats(outStats);
            }

            // Process custom pools
            for (nuint heapTypeIndex = 0; heapTypeIndex < HEAP_TYPE_COUNT; ++heapTypeIndex)
            {
                using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_PoolsMutex[(int)heapTypeIndex]), m_UseMutex);
                PoolVectorType* poolVector = m_pPools[(int)heapTypeIndex];
                D3D12MA_ASSERT(poolVector != null);
                for (nuint poolIndex = 0, count = poolVector->size(); poolIndex < count; ++poolIndex)
                {
                    Pool* pool = *(*poolVector)[poolIndex];
                    pool->m_Pimpl->GetBlockVector()->AddStats(outStats);
                }
            }

            // Process committed allocations.
            for (nuint heapTypeIndex = 0; heapTypeIndex < HEAP_TYPE_COUNT; ++heapTypeIndex)
            {
                StatInfo* heapStatInfo = (StatInfo*)Unsafe.AsPointer(ref outStats->HeapType[(int)heapTypeIndex]);
                using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_CommittedAllocationsMutex[(int)heapTypeIndex]), m_UseMutex);
                AllocationVectorType* allocationVector = m_pCommittedAllocations[(int)heapTypeIndex];
                D3D12MA_ASSERT(allocationVector != null);
                for (nuint allocIndex = 0, count = allocationVector->size(); allocIndex < count; ++allocIndex)
                {
                    ulong size = (*allocationVector)[allocIndex]->Value->GetSize();
                    StatInfo statInfo = default;
                    statInfo.BlockCount = 1;
                    statInfo.AllocationCount = 1;
                    statInfo.UnusedRangeCount = 0;
                    statInfo.UsedBytes = size;
                    statInfo.UnusedBytes = 0;
                    statInfo.AllocationSizeMin = size;
                    statInfo.AllocationSizeMax = size;
                    statInfo.UnusedRangeSizeMin = UINT64_MAX;
                    statInfo.UnusedRangeSizeMax = 0;
                    AddStatInfo(ref outStats->Total, ref statInfo);
                    AddStatInfo(ref *heapStatInfo, ref statInfo);
                }
            }

            // Post process
            PostProcessStatInfo(ref outStats->Total);
            for (nuint i = 0; i < HEAP_TYPE_COUNT; ++i)
                PostProcessStatInfo(ref outStats->HeapType[(int)i]);
        }

        public void GetBudget(Budget* outGpuBudget, Budget* outCpuBudget)
        {
            if (outGpuBudget != null)
            {
                // Taking DEFAULT.
                outGpuBudget->BlockBytes = m_Budget.m_BlockBytes[0].Load();
                outGpuBudget->AllocationBytes = m_Budget.m_AllocationBytes[0].Load();
            }

            if (outCpuBudget != null)
            {
                // Taking UPLOAD + READBACK.
                outCpuBudget->BlockBytes = m_Budget.m_BlockBytes[1].Load() + m_Budget.m_BlockBytes[2].Load();
                outCpuBudget->AllocationBytes = m_Budget.m_AllocationBytes[1].Load() + m_Budget.m_AllocationBytes[2].Load();
            }

            if (D3D12MA_DXGI_1_4 > 0)
            {
                if (m_Adapter3 != null)
                {
                    if (m_Budget.m_OperationsSinceBudgetFetch.Load() < 30)
                    {
                        using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Budget.m_BudgetMutex), m_UseMutex);
                        if (outGpuBudget != null)
                        {

                            if (m_Budget.m_D3D12UsageLocal + outGpuBudget->BlockBytes > m_Budget.m_BlockBytesAtBudgetFetch[0])
                            {
                                outGpuBudget->UsageBytes = m_Budget.m_D3D12UsageLocal +
                                    outGpuBudget->BlockBytes - m_Budget.m_BlockBytesAtBudgetFetch[0];
                            }
                            else
                            {
                                outGpuBudget->UsageBytes = 0;
                            }

                            outGpuBudget->BudgetBytes = m_Budget.m_D3D12BudgetLocal;
                        }

                        if (outCpuBudget != null)
                        {
                            if (m_Budget.m_D3D12UsageNonLocal + outCpuBudget->BlockBytes > m_Budget.m_BlockBytesAtBudgetFetch[1] + m_Budget.m_BlockBytesAtBudgetFetch[2])
                            {
                                outCpuBudget->UsageBytes = m_Budget.m_D3D12UsageNonLocal +
                                    outCpuBudget->BlockBytes - (m_Budget.m_BlockBytesAtBudgetFetch[1] + m_Budget.m_BlockBytesAtBudgetFetch[2]);
                            }
                            else
                            {
                                outCpuBudget->UsageBytes = 0;
                            }

                            outCpuBudget->BudgetBytes = m_Budget.m_D3D12BudgetNonLocal;
                        }
                    }
                    else
                    {
                        UpdateD3D12Budget(); // Outside of mutex lock
                        GetBudget(outGpuBudget, outCpuBudget); // Recursion
                    }
                }
            }
            else
            {
                if (outGpuBudget != null)
                {
                    ulong gpuMemorySize = m_AdapterDesc.DedicatedVideoMemory + m_AdapterDesc.DedicatedSystemMemory; // TODO: Is this right?
                    outGpuBudget->UsageBytes = outGpuBudget->BlockBytes;
                    outGpuBudget->BudgetBytes = gpuMemorySize * 8 / 10; // 80% heuristics.
                }

                if (outCpuBudget != null)
                {
                    ulong cpuMemorySize = m_AdapterDesc.SharedSystemMemory; // TODO: Is this right?
                    outCpuBudget->UsageBytes = outCpuBudget->BlockBytes;
                    outCpuBudget->BudgetBytes = cpuMemorySize * 8 / 10; // 80% heuristics.
                }
            }
        }

        public void GetBudgetForHeapType(Budget* outBudget, D3D12_HEAP_TYPE heapType)
        {
            switch (heapType)
            {
                case D3D12_HEAP_TYPE_DEFAULT:
                {
                    GetBudget(outBudget, null);
                    break;
                }

                case D3D12_HEAP_TYPE_UPLOAD:
                case D3D12_HEAP_TYPE_READBACK:
                {
                    GetBudget(null, outBudget);
                    break;
                }

                default:
                {
                    D3D12MA_ASSERT(false);
                    break;
                }
            }
        }

        static void AddStatInfoToJson(JsonWriter* json, StatInfo* statInfo)
        {
            json->BeginObject();
            json->WriteString("Blocks");
            json->WriteNumber(statInfo->BlockCount);
            json->WriteString("Allocations");
            json->WriteNumber(statInfo->AllocationCount);
            json->WriteString("UnusedRanges");
            json->WriteNumber(statInfo->UnusedRangeCount);
            json->WriteString("UsedBytes");
            json->WriteNumber(statInfo->UsedBytes);
            json->WriteString("UnusedBytes");
            json->WriteNumber(statInfo->UnusedBytes);

            json->WriteString("AllocationSize");
            json->BeginObject(true);
            json->WriteString("Min");
            json->WriteNumber(statInfo->AllocationSizeMin);
            json->WriteString("Avg");
            json->WriteNumber(statInfo->AllocationSizeAvg);
            json->WriteString("Max");
            json->WriteNumber(statInfo->AllocationSizeMax);
            json->EndObject();

            json->WriteString("UnusedRangeSize");
            json->BeginObject(true);
            json->WriteString("Min");
            json->WriteNumber(statInfo->UnusedRangeSizeMin);
            json->WriteString("Avg");
            json->WriteNumber(statInfo->UnusedRangeSizeAvg);
            json->WriteString("Max");
            json->WriteNumber(statInfo->UnusedRangeSizeMax);
            json->EndObject();

            json->EndObject();
        }

        internal static readonly string[] heapSubTypeName = new[]
        {
            " + buffer",
            " + texture",
            " + texture RT or DS",
        };

        public void BuildStatsString([NativeTypeName("WCHAR**")] ushort** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap)
        {
            using StringBuilder sb = new(GetAllocs());
            {
                using JsonWriter json = new(GetAllocs(), &sb);

                Budget gpuBudget = default, cpuBudget = default;
                GetBudget(&gpuBudget, &cpuBudget);

                Stats stats;
                CalculateStats(&stats);

                json.BeginObject();

                json.WriteString("Total");
                AddStatInfoToJson(&json, &stats.Total);
                for (nuint heapType = 0; heapType < HEAP_TYPE_COUNT; ++heapType)
                {
                    json.WriteString(HeapTypeNames[heapType]);
                    AddStatInfoToJson(&json, (StatInfo*)Unsafe.AsPointer(ref stats.HeapType[(int)heapType]));
                }

                json.WriteString("Budget");
                json.BeginObject();
                {
                    json.WriteString("GPU");
                    WriteBudgetToJson(&json, &gpuBudget);
                    json.WriteString("CPU");
                    WriteBudgetToJson(&json, &cpuBudget);
                }

                json.EndObject();

                if (DetailedMap > 0)
                {
                    json.WriteString("DetailedMap");
                    json.BeginObject();

                    json.WriteString("DefaultPools");
                    json.BeginObject();

                    if (SupportsResourceHeapTier2())
                    {
                        for (nuint heapType = 0; heapType < HEAP_TYPE_COUNT; ++heapType)
                        {
                            json.WriteString(HeapTypeNames[heapType]);
                            json.BeginObject();

                            json.WriteString("Blocks");

                            BlockVector* blockVector = m_BlockVectors[(int)heapType];
                            D3D12MA_ASSERT(blockVector != null);
                            blockVector->WriteBlockInfoToJson(&json);

                            json.EndObject(); // heap name
                        }
                    }
                    else
                    {
                        for (nuint heapType = 0; heapType < HEAP_TYPE_COUNT; ++heapType)
                        {
                            for (nuint heapSubType = 0; heapSubType < 3; ++heapSubType)
                            {
                                json.BeginString();
                                json.ContinueString(HeapTypeNames[heapType]);
                                json.ContinueString(heapSubTypeName[heapSubType]);
                                json.EndString();
                                json.BeginObject();

                                json.WriteString("Blocks");

                                BlockVector* blockVector = m_BlockVectors[(int)(heapType * 3 + heapSubType)];
                                D3D12MA_ASSERT(blockVector != null);
                                blockVector->WriteBlockInfoToJson(&json);

                                json.EndObject(); // heap name
                            }
                        }
                    }

                    json.EndObject(); // DefaultPools

                    json.WriteString("CommittedAllocations");
                    json.BeginObject();

                    for (nuint heapType = 0; heapType < HEAP_TYPE_COUNT; ++heapType)
                    {
                        json.WriteString(HeapTypeNames[heapType]);
                        using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_CommittedAllocationsMutex[(int)heapType]), m_UseMutex);

                        json.BeginArray();
                        AllocationVectorType* allocationVector = m_pCommittedAllocations[(int)heapType];
                        D3D12MA_ASSERT(allocationVector != null);
                        for (nuint i = 0, count = allocationVector->size(); i < count; ++i)
                        {
                            Allocation* alloc = *(*allocationVector)[i];
                            D3D12MA_ASSERT(alloc != null);

                            json.BeginObject(true);
                            json.AddAllocationToObject(alloc);
                            json.EndObject();
                        }

                        json.EndArray();
                    }

                    json.EndObject(); // CommittedAllocations

                    json.EndObject(); // DetailedMap
                }

                json.EndObject();
            }

            nuint length = sb.GetLength();
            ushort* result = AllocateArray<ushort>(GetAllocs(), length + 1);
            memcpy(result, sb.GetData(), length * sizeof(ushort));
            result[length] = '\0';
            *ppStatsString = result;
        }

        public void FreeStatsString([NativeTypeName("WCHAR*")] ushort* pStatsString)
        {
            D3D12MA_ASSERT(pStatsString != null);
            Free(GetAllocs(), pStatsString);
        }

        /// <summary>
        /// Heuristics that decides whether a resource should better be placed in its own,
        /// dedicated allocation(committed resource rather than placed resource).
        /// </summary>
        internal static bool PrefersCommittedAllocation<TD3D12_RESOURCE_DESC>(TD3D12_RESOURCE_DESC* resourceDesc)
            where TD3D12_RESOURCE_DESC : unmanaged
        {
            // Intentional. It may change in the future.
            return false;
        }

        // Allocates and registers new committed resource with implicit heap, as dedicated allocation.
        // Creates and returns Allocation object.
        [return: NativeTypeName("HRESULT")]
        private int AllocateCommittedResource(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_ALLOCATION_INFO* resAllocInfo, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                return E_OUTOFMEMORY;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_WITHIN_BUDGET) != 0 &&
                !NewAllocationWithinBudget(pAllocDesc->HeapType, resAllocInfo->SizeInBytes))
            {
                return E_OUTOFMEMORY;
            }

            D3D12_HEAP_PROPERTIES heapProps = default;
            heapProps.Type = pAllocDesc->HeapType;

            D3D12_HEAP_FLAGS heapFlags = pAllocDesc->ExtraHeapFlags;

            ID3D12Resource* res = null;
            HRESULT hr = m_Device->CreateCommittedResource(
                &heapProps, heapFlags, pResourceDesc, InitialResourceState,
                pOptimizedClearValue, __uuidof<ID3D12Resource>(), (void**)&res);
            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    const int wasZeroInitialized = 1;
                    Allocation* alloc = m_AllocationObjectAllocator.Allocate((AllocatorPimpl*)Unsafe.AsPointer(ref this), resAllocInfo->SizeInBytes, wasZeroInitialized);
                    alloc->InitCommitted(pAllocDesc->HeapType);
                    alloc->SetResource(res, pResourceDesc);

                    *ppAllocation = alloc;

                    RegisterCommittedAllocation(*ppAllocation, pAllocDesc->HeapType);

                    uint heapTypeIndex = HeapTypeToIndex(pAllocDesc->HeapType);
                    m_Budget.AddAllocation(heapTypeIndex, resAllocInfo->SizeInBytes);
                    m_Budget.m_BlockBytes[(int)heapTypeIndex].Add(resAllocInfo->SizeInBytes);
                }
                else
                {
                    res->Release();
                }
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateCommittedResource1(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_ALLOCATION_INFO* resAllocInfo, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if (m_Device4 == null)
            {
                return E_NOINTERFACE;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                return E_OUTOFMEMORY;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_WITHIN_BUDGET) != 0 &&
                !NewAllocationWithinBudget(pAllocDesc->HeapType, resAllocInfo->SizeInBytes))
            {
                return E_OUTOFMEMORY;
            }

            D3D12_HEAP_PROPERTIES heapProps = default;
            heapProps.Type = pAllocDesc->HeapType;

            D3D12_HEAP_FLAGS heapFlags = pAllocDesc->ExtraHeapFlags;

            ID3D12Resource* res = null;
            HRESULT hr = m_Device4->CreateCommittedResource1(
                &heapProps, heapFlags, pResourceDesc, InitialResourceState,
                pOptimizedClearValue, pProtectedSession, __uuidof<ID3D12Resource>(), (void**)&res);
            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    const int wasZeroInitialized = 1;
                    Allocation* alloc = m_AllocationObjectAllocator.Allocate((AllocatorPimpl*)Unsafe.AsPointer(ref this), resAllocInfo->SizeInBytes, wasZeroInitialized);
                    alloc->InitCommitted(pAllocDesc->HeapType);
                    alloc->SetResource(res, pResourceDesc);

                    *ppAllocation = alloc;

                    RegisterCommittedAllocation(*ppAllocation, pAllocDesc->HeapType);

                    uint heapTypeIndex = HeapTypeToIndex(pAllocDesc->HeapType);
                    m_Budget.AddAllocation(heapTypeIndex, resAllocInfo->SizeInBytes);
                    m_Budget.m_BlockBytes[(int)heapTypeIndex].Add(resAllocInfo->SizeInBytes);
                }
                else
                {
                    res->Release();
                }
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateCommittedResource2(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_ALLOCATION_INFO* resAllocInfo, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if (m_Device8 == null)
            {
                return E_NOINTERFACE;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                return E_OUTOFMEMORY;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_WITHIN_BUDGET) != 0 &&
                !NewAllocationWithinBudget(pAllocDesc->HeapType, resAllocInfo->SizeInBytes))
            {
                return E_OUTOFMEMORY;
            }

            D3D12_HEAP_PROPERTIES heapProps = default;
            heapProps.Type = pAllocDesc->HeapType;

            D3D12_HEAP_FLAGS heapFlags = pAllocDesc->ExtraHeapFlags;

            ID3D12Resource* res = null;
            HRESULT hr = m_Device8->CreateCommittedResource2(
                &heapProps, heapFlags, pResourceDesc, InitialResourceState,
                pOptimizedClearValue, pProtectedSession, __uuidof<ID3D12Resource>(), (void**)&res);
            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    const int wasZeroInitialized = 1;
                    Allocation* alloc = m_AllocationObjectAllocator.Allocate((AllocatorPimpl*)Unsafe.AsPointer(ref this), resAllocInfo->SizeInBytes, wasZeroInitialized);
                    alloc->InitCommitted(pAllocDesc->HeapType);
                    alloc->SetResource(res, pResourceDesc);

                    *ppAllocation = alloc;

                    RegisterCommittedAllocation(*ppAllocation, pAllocDesc->HeapType);

                    uint heapTypeIndex = HeapTypeToIndex(pAllocDesc->HeapType);
                    m_Budget.AddAllocation(heapTypeIndex, resAllocInfo->SizeInBytes);
                    m_Budget.m_BlockBytes[(int)heapTypeIndex].Add(resAllocInfo->SizeInBytes);
                }
                else
                {
                    res->Release();
                }
            }

            return hr;
        }

        // Allocates and registers new heap without any resources placed in it, as dedicated allocation.
        // Creates and returns Allocation object.
        [return: NativeTypeName("HRESULT")]
        private int AllocateHeap(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* allocInfo, Allocation** ppAllocation)
        {
            *ppAllocation = null;

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                return E_OUTOFMEMORY;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_WITHIN_BUDGET) != 0 &&
                !NewAllocationWithinBudget(pAllocDesc->HeapType, allocInfo->SizeInBytes))
            {
                return E_OUTOFMEMORY;
            }

            D3D12_HEAP_FLAGS heapFlags = pAllocDesc->ExtraHeapFlags;

            D3D12_HEAP_DESC heapDesc = default;
            heapDesc.SizeInBytes = allocInfo->SizeInBytes;
            heapDesc.Properties.Type = pAllocDesc->HeapType;
            heapDesc.Alignment = allocInfo->Alignment;
            heapDesc.Flags = heapFlags;

            ID3D12Heap* heap = null;
            HRESULT hr = m_Device->CreateHeap(&heapDesc, __uuidof<ID3D12Heap>(), (void**)&heap);
            if (SUCCEEDED(hr))
            {
                const int wasZeroInitialized = 1;
                (*ppAllocation) = m_AllocationObjectAllocator.Allocate((AllocatorPimpl*)Unsafe.AsPointer(ref this), allocInfo->SizeInBytes, wasZeroInitialized);
                (*ppAllocation)->InitHeap(pAllocDesc->HeapType, heap);
                RegisterCommittedAllocation(*ppAllocation, pAllocDesc->HeapType);

                uint heapTypeIndex = HeapTypeToIndex(pAllocDesc->HeapType);
                m_Budget.AddAllocation(heapTypeIndex, allocInfo->SizeInBytes);
                m_Budget.m_BlockBytes[(int)heapTypeIndex].Add(allocInfo->SizeInBytes);
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateHeap1(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* allocInfo, ID3D12ProtectedResourceSession* pProtectedSession, Allocation** ppAllocation)
        {
            *ppAllocation = null;

            if (m_Device4 == null)
            {
                return E_NOINTERFACE;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                return E_OUTOFMEMORY;
            }

            if ((pAllocDesc->Flags & ALLOCATION_FLAG_WITHIN_BUDGET) != 0 &&
                !NewAllocationWithinBudget(pAllocDesc->HeapType, allocInfo->SizeInBytes))
            {
                return E_OUTOFMEMORY;
            }

            D3D12_HEAP_FLAGS heapFlags = pAllocDesc->ExtraHeapFlags;

            D3D12_HEAP_DESC heapDesc = default;
            heapDesc.SizeInBytes = allocInfo->SizeInBytes;
            heapDesc.Properties.Type = pAllocDesc->HeapType;
            heapDesc.Alignment = allocInfo->Alignment;
            heapDesc.Flags = heapFlags;

            ID3D12Heap* heap = null;
            HRESULT hr = m_Device4->CreateHeap1(&heapDesc, pProtectedSession, __uuidof<ID3D12Heap>(), (void**)&heap);
            if (SUCCEEDED(hr))
            {
                const int wasZeroInitialized = 1;
                (*ppAllocation) = m_AllocationObjectAllocator.Allocate((AllocatorPimpl*)Unsafe.AsPointer(ref this), allocInfo->SizeInBytes, wasZeroInitialized);
                (*ppAllocation)->InitHeap(pAllocDesc->HeapType, heap);
                RegisterCommittedAllocation(*ppAllocation, pAllocDesc->HeapType);

                uint heapTypeIndex = HeapTypeToIndex(pAllocDesc->HeapType);
                m_Budget.AddAllocation(heapTypeIndex, allocInfo->SizeInBytes);
                m_Budget.m_BlockBytes[(int)heapTypeIndex].Add(allocInfo->SizeInBytes);
            }

            return hr;
        }

        /// <summary>
        /// <code>
        /// If SupportsResourceHeapTier2():
        ///     0: D3D12_HEAP_TYPE_DEFAULT
        ///     1: D3D12_HEAP_TYPE_UPLOAD
        ///     2: D3D12_HEAP_TYPE_READBACK
        /// else:
        ///     0: D3D12_HEAP_TYPE_DEFAULT + buffer
        ///     1: D3D12_HEAP_TYPE_DEFAULT + texture
        ///     2: D3D12_HEAP_TYPE_DEFAULT + texture RT or DS
        ///     3: D3D12_HEAP_TYPE_UPLOAD + buffer
        ///     4: D3D12_HEAP_TYPE_UPLOAD + texture
        ///     5: D3D12_HEAP_TYPE_UPLOAD + texture RT or DS
        ///     6: D3D12_HEAP_TYPE_READBACK + buffer
        ///     7: D3D12_HEAP_TYPE_READBACK + texture
        ///     8: D3D12_HEAP_TYPE_READBACK + texture RT or DS
        /// </code>
        /// </summary>
        [return: NativeTypeName("UINT")]
        private uint CalcDefaultPoolCount()
        {
            if (SupportsResourceHeapTier2())
            {
                return 3;
            }
            else
            {
                return 9;
            }
        }

        [return: NativeTypeName("UINT")]
        private uint CalcDefaultPoolIndex([NativeTypeName("const ALLOCATION_DESC&")] ALLOCATION_DESC* allocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC* resourceDesc)
        {
            D3D12_HEAP_FLAGS extraHeapFlags = allocDesc->ExtraHeapFlags & ~GetExtraHeapFlagsToIgnore();
            if (extraHeapFlags != 0)
            {
                return UINT32_MAX;
            }

            uint poolIndex = UINT_MAX;
            switch (allocDesc->HeapType)
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

                default:
                {
                    D3D12MA_ASSERT(false);
                    break;
                }
            }

            if (!SupportsResourceHeapTier2())
            {
                poolIndex *= 3;
                if (resourceDesc->Dimension != D3D12_RESOURCE_DIMENSION_BUFFER)
                {
                    ++poolIndex;
                    bool isRenderTargetOrDepthStencil = (resourceDesc->Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0;
                    if (isRenderTargetOrDepthStencil)
                    {
                        ++poolIndex;
                    }
                }
            }

            return poolIndex;
        }

        [return: NativeTypeName("UINT")]
        private uint CalcDefaultPoolIndex([NativeTypeName("const ALLOCATION_DESC&")] ALLOCATION_DESC* allocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC1* resourceDesc)
        {
            return CalcDefaultPoolIndex(allocDesc, (D3D12_RESOURCE_DESC*)resourceDesc);
        }

        /// <summary>This one returns UINT32_MAX if nonstandard heap flags are used and index cannot be calculcated.</summary>
        [return: NativeTypeName("UINT")]
        private static uint CalcDefaultPoolIndex(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, bool supportsResourceHeapTier2)
        {
            D3D12_HEAP_FLAGS extraHeapFlags = heapFlags & ~GetExtraHeapFlagsToIgnore();
            if (extraHeapFlags != 0)
            {
                return UINT32_MAX;
            }

            uint poolIndex = UINT_MAX;
            switch (heapType)
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

                default:
                {
                    D3D12MA_ASSERT(false);
                    break;
                }
            }

            if (!supportsResourceHeapTier2)
            {
                poolIndex *= 3;

                bool allowBuffers = (heapFlags & D3D12_HEAP_FLAG_DENY_BUFFERS) == 0;
                bool allowRtDsTextures = (heapFlags & D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES) == 0;
                bool allowNonRtDsTextures = (heapFlags & D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES) == 0;

                int allowedGroupCount = (allowBuffers ? 1 : 0) + (allowRtDsTextures ? 1 : 0) + (allowNonRtDsTextures ? 1 : 0);
                if (allowedGroupCount != 1)
                {
                    return UINT32_MAX;
                }

                if (!allowBuffers)
                {
                    ++poolIndex;
                    if (allowRtDsTextures)
                    {
                        ++poolIndex;
                    }
                }
            }

            return poolIndex;
        }

        [return: NativeTypeName("UINT")]
        private uint CalcDefaultPoolIndex(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags)
        {
            return CalcDefaultPoolIndex(heapType, heapFlags, SupportsResourceHeapTier2());
        }

        [return: NativeTypeName("UINT")]
        uint CalcDefaultPoolIndex(ALLOCATION_DESC* allocDesc)
        {
            return CalcDefaultPoolIndex(allocDesc->HeapType, allocDesc->ExtraHeapFlags);
        }

        void CalcDefaultPoolParams(D3D12_HEAP_TYPE* outHeapType, D3D12_HEAP_FLAGS* outHeapFlags, [NativeTypeName("UINT")] uint index)
        {
            *outHeapType = D3D12_HEAP_TYPE_DEFAULT;
            *outHeapFlags = D3D12_HEAP_FLAG_NONE;

            if (!SupportsResourceHeapTier2())
            {
                switch (index % 3)
                {
                    case 0:
                    {
                        *outHeapFlags = D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;
                        break;
                    }

                    case 1:
                    {
                        *outHeapFlags = D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES;
                        break;
                    }

                    case 2:
                    {
                        *outHeapFlags = D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;
                        break;
                    }
                }

                index /= 3;
            }

            switch (index)
            {
                case 0:
                {
                    *outHeapType = D3D12_HEAP_TYPE_DEFAULT;
                    break;
                }

                case 1:
                {
                    *outHeapType = D3D12_HEAP_TYPE_UPLOAD;
                    break;
                }

                case 2:
                {
                    *outHeapType = D3D12_HEAP_TYPE_READBACK;
                    break;
                }

                default:
                {
                    D3D12MA_ASSERT(false);
                    break;
                }
            }
        }

        /// <summary>Registers Allocation object in m_pCommittedAllocations.</summary>
        void RegisterCommittedAllocation(Allocation* alloc, D3D12_HEAP_TYPE heapType)
        {
            uint heapTypeIndex = HeapTypeToIndex(heapType);

            using MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_CommittedAllocationsMutex[(int)heapTypeIndex]), m_UseMutex);
            AllocationVectorType* committedAllocations = m_pCommittedAllocations[(int)heapTypeIndex];
            D3D12MA_ASSERT(committedAllocations != null);
            committedAllocations->InsertSorted((Ptr<Allocation>*)&alloc, new PointerLess<Ptr<Allocation>>());
        }

        /// <summary>Unregisters Allocation object from m_pCommittedAllocations.</summary>
        void UnregisterCommittedAllocation(Allocation* alloc, D3D12_HEAP_TYPE heapType)
        {
            uint heapTypeIndex = HeapTypeToIndex(heapType);

            using MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_CommittedAllocationsMutex[(int)heapTypeIndex]), m_UseMutex);
            AllocationVectorType* committedAllocations = m_pCommittedAllocations[(int)heapTypeIndex];
            D3D12MA_ASSERT(committedAllocations != null);
            bool success = committedAllocations->RemoveSorted((Ptr<Allocation>*)&alloc, new PointerLess<Ptr<Allocation>>());
            D3D12MA_ASSERT(success);
        }

        /// <summary>Registers Pool object in m_pPools.</summary>
        internal void RegisterPool(Pool* pool, D3D12_HEAP_TYPE heapType)
        {
            uint heapTypeIndex = HeapTypeToIndex(heapType);

            using MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_PoolsMutex[(int)heapTypeIndex]), m_UseMutex);
            PoolVectorType* pools = m_pPools[(int)heapTypeIndex];
            D3D12MA_ASSERT(pools != null);
            pools->InsertSorted((Ptr<Pool>*)&pool, new PointerLess<Ptr<Pool>>());
        }

        /// <summary>Unregisters Pool object from m_pPools.</summary>
        internal void UnregisterPool([NativeTypeName("Pool*")] ref Pool pool, D3D12_HEAP_TYPE heapType)
        {
            fixed (Pool* pPool = &pool)
            {
                uint heapTypeIndex = HeapTypeToIndex(heapType);

                using MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_PoolsMutex[(int)heapTypeIndex]), m_UseMutex);
                PoolVectorType* pools = m_pPools[(int)heapTypeIndex];
                D3D12MA_ASSERT(pools != null);
                bool success = pools->RemoveSorted((Ptr<Pool>*)&pPool, new PointerLess<Ptr<Pool>>());
                D3D12MA_ASSERT(success);
            }
        }

        [return: NativeTypeName("HRESULT")]
        private int UpdateD3D12Budget()
        {
            if (D3D12MA_DXGI_1_4 > 0)
            {
                D3D12MA_ASSERT(m_Adapter3 != null);

                DXGI_QUERY_VIDEO_MEMORY_INFO infoLocal = default;
                DXGI_QUERY_VIDEO_MEMORY_INFO infoNonLocal = default;
                HRESULT hrLocal = m_Adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &infoLocal);
                HRESULT hrNonLocal = m_Adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL, &infoNonLocal);

                {
                    using MutexLockWrite lockWrite = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Budget.m_BudgetMutex), m_UseMutex);

                    if (SUCCEEDED(hrLocal))
                    {
                        m_Budget.m_D3D12UsageLocal = infoLocal.CurrentUsage;
                        m_Budget.m_D3D12BudgetLocal = infoLocal.Budget;
                    }

                    if (SUCCEEDED(hrNonLocal))
                    {
                        m_Budget.m_D3D12UsageNonLocal = infoNonLocal.CurrentUsage;
                        m_Budget.m_D3D12BudgetNonLocal = infoNonLocal.Budget;
                    }

                    for (uint i = 0; i < HEAP_TYPE_COUNT; ++i)
                    {
                        m_Budget.m_BlockBytesAtBudgetFetch[(int)i] = m_Budget.m_BlockBytes[(int)i].Load();
                    }

                    m_Budget.m_OperationsSinceBudgetFetch.Store(0);
                }

                return FAILED(hrLocal) ? hrLocal : hrNonLocal;
            }
            else
            {
                return S_OK;
            }
        }

        private D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfoNative(D3D12_RESOURCE_DESC* resourceDesc)
        {
            return m_Device->GetResourceAllocationInfo(0, 1, resourceDesc);
        }

        private D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfoNative(D3D12_RESOURCE_DESC1* resourceDesc)
        {
            D3D12MA_ASSERT(m_Device8 != null);
            D3D12_RESOURCE_ALLOCATION_INFO1 info1Unused;
            return m_Device8->GetResourceAllocationInfo2(0, 1, resourceDesc, &info1Unused);
        }

        private D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfo([NativeTypeName("D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC* inOutResourceDesc)
        {
            /* Optional optimization: Microsoft documentation says:
            https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-getresourceallocationinfo
    
            Your application can forgo using GetResourceAllocationInfo for buffer resources
            (D3D12_RESOURCE_DIMENSION_BUFFER). Buffers have the same size on all adapters,
            which is merely the smallest multiple of 64KB that's greater or equal to
            D3D12_RESOURCE_DESC::Width.
            */
            if (inOutResourceDesc->Alignment == 0 &&
                inOutResourceDesc->Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return new D3D12_RESOURCE_ALLOCATION_INFO(
                    AlignUp(inOutResourceDesc->Width, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT), // SizeInBytes
                    D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT); // Alignment
            }

            if (D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT > 0)
            {
                if (inOutResourceDesc->Alignment == 0 &&
                    inOutResourceDesc->Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE2D &&
                    (inOutResourceDesc->Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) == 0 &&
                    (D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT != 1 || CanUseSmallAlignment(inOutResourceDesc)))
                {
                    /*
                    The algorithm here is based on Microsoft sample: "Small Resources Sample"
                    https://github.com/microsoft/DirectX-Graphics-Samples/tree/master/Samples/Desktop/D3D12SmallResources
                    */
                    ulong smallAlignmentToTry = inOutResourceDesc->SampleDesc.Count > 1 ?
                        D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT :
                        D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT;
                    inOutResourceDesc->Alignment = smallAlignmentToTry;
                    D3D12_RESOURCE_ALLOCATION_INFO smallAllocInfo = GetResourceAllocationInfoNative(inOutResourceDesc);
                    // Check if alignment requested has been granted.
                    if (smallAllocInfo.Alignment == smallAlignmentToTry)
                    {
                        return smallAllocInfo;
                    }

                    inOutResourceDesc->Alignment = 0; // Restore original
                }
            }

            return GetResourceAllocationInfoNative(inOutResourceDesc);
        }

        private D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfo([NativeTypeName("D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC1* inOutResourceDesc)
        {
            return GetResourceAllocationInfo((D3D12_RESOURCE_DESC*)inOutResourceDesc);
        }

        private bool NewAllocationWithinBudget(D3D12_HEAP_TYPE heapType, [NativeTypeName("UINT64")] ulong size)
        {
            Budget budget = default;
            GetBudgetForHeapType(&budget, heapType);
            return budget.UsageBytes + size <= budget.BudgetBytes;
        }

        /// <summary>Writes object { } with data of given budget.</summary>
        private static void WriteBudgetToJson(JsonWriter* json, Budget* budget)
        {
            json->BeginObject();
            {
                json->WriteString("BlockBytes");
                json->WriteNumber(budget->BlockBytes);
                json->WriteString("AllocationBytes");
                json->WriteNumber(budget->AllocationBytes);
                json->WriteString("UsageBytes");
                json->WriteNumber(budget->UsageBytes);
                json->WriteString("BudgetBytes");
                json->WriteNumber(budget->BudgetBytes);
            }

            json->EndObject();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __m_Buffer_HEAP_TYPE_COUNT_e__FixedBuffer<T>
            where T : unmanaged
        {
            public T _0;
            public T _1;
            public T _2;

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref ((T*)Unsafe.AsPointer(ref _0))[index];
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __m_Buffer_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<T>
            where T : unmanaged
        {
            public T _0;
            public T _1;
            public T _2;
            public T _3;
            public T _4;
            public T _5;
            public T _6;
            public T _7;
            public T _8;

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref ((T*)Unsafe.AsPointer(ref _0))[index];
            }
        }
    }
}
