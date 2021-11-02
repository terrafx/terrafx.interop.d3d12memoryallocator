// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemAlloc;
using static TerraFX.Interop.D3D12_RESOURCE_HEAP_TIER;
using static TerraFX.Interop.D3D12MA_ALLOCATOR_FLAGS;
using static TerraFX.Interop.D3D12_FEATURE;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DXGI_MEMORY_SEGMENT_GROUP;
using System.Runtime.InteropServices;
using System.Threading;

namespace TerraFX.Interop
{
    public unsafe partial struct D3D12MA_Allocator
    {
        internal D3D12MA_IUnknownImpl m_IUnknownImpl;

        internal D3D12MA_CurrentBudgetData m_Budget;

        private byte m_UseMutex;

        private byte m_AlwaysCommitted;

        private ID3D12Device* m_Device; // AddRef

        private ID3D12Device4* m_Device4; // AddRef, optional

        private ID3D12Device8* m_Device8; // AddRef, optional

        private IDXGIAdapter* m_Adapter; // AddRef

        private IDXGIAdapter3* m_Adapter3; //AddRef, optional

        [NativeTypeName("UINT64")]
        private ulong m_PreferredBlockSize;

        private D3D12MA_ALLOCATION_CALLBACKS m_AllocationCallbacks;

        [NativeTypeName("D3D12MA_ATOMIC_UINT32")]
        private volatile uint m_CurrentFrameIndex;

        private DXGI_ADAPTER_DESC m_AdapterDesc;

        private D3D12_FEATURE_DATA_D3D12_OPTIONS m_D3D12Options;

        private D3D12_FEATURE_DATA_ARCHITECTURE m_D3D12Architecture;

        private D3D12MA_AllocationObjectAllocator m_AllocationObjectAllocator;

        [NativeTypeName("IntrusiveLinkedList<PoolListItemTraits> m_pPools[HEAP_TYPE_COUNT]")]
        private _D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer<D3D12MA_IntrusiveLinkedList<D3D12MA_Pool>> m_Pools;

        [NativeTypeName("D3D12MA_RW_MUTEX m_PoolsMutex[HEAP_TYPE_COUNT]")]
        private _D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer<D3D12MA_RW_MUTEX> m_PoolsMutex;

        // Default pools.
        [NativeTypeName("BlockVector* m_BlockVectors[DEFAULT_POOL_MAX_COUNT]")]
        private _D3D12MA_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<Pointer<D3D12MA_BlockVector>> m_BlockVectors;

        [NativeTypeName("CommittedAllocationList m_CommittedAllocations[STANDARD_HEAP_TYPE_COUNT]")]
        private _D3D12MA_STANDARD_HEAP_TYPE_COUNT_e__FixedBuffer<D3D12MA_CommittedAllocationList> m_CommittedAllocations;

        // Explicit constructor as a normal instance method: this is needed to ensure the code is executed in-place over the
        // AllocatorPimpl instance being initialized, and not on a local variable which is then copied over to the
        // target memory location, which would break the references to self fields being used in the code below.
        internal static void _ctor(ref D3D12MA_Allocator pThis, [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("const D3D12MA_ALLOCATOR_DESC&")] D3D12MA_ALLOCATOR_DESC* desc)
        {
            D3D12MA_IUnknownImpl._ctor(ref pThis.m_IUnknownImpl, Vtbl);
            D3D12MA_CurrentBudgetData._ctor(ref pThis.m_Budget);

            for (uint i = 0; i < D3D12MA_HEAP_TYPE_COUNT; ++i)
            {
                D3D12MA_RW_MUTEX._ctor(ref pThis.m_PoolsMutex[(int)i]);
            }

            pThis.m_UseMutex = (byte)((((int)desc->Flags & (int)D3D12MA_ALLOCATOR_FLAG_SINGLETHREADED) == 0) ? 1 : 0);
            pThis.m_AlwaysCommitted = (byte)((((int)desc->Flags & (int)D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED) != 0) ? 1 : 0);
            pThis.m_Device = desc->pDevice;
            pThis.m_Device4 = null;
            pThis.m_Device8 = null;
            pThis.m_Adapter = desc->pAdapter;
            pThis.m_Adapter3 = null;
            pThis.m_PreferredBlockSize = desc->PreferredBlockSize != 0 ? desc->PreferredBlockSize : D3D12MA_DEFAULT_BLOCK_SIZE;
            pThis.m_AllocationCallbacks = *allocationCallbacks;
            pThis.m_CurrentFrameIndex = default;

            // Below this line don't use allocationCallbacks but m_AllocationCallbacks!!!
            D3D12MA_AllocationObjectAllocator._ctor(ref pThis.m_AllocationObjectAllocator, ref pThis.m_AllocationCallbacks);

            // desc.pAllocationCallbacks intentionally ignored here, preprocessed by CreateAllocator.
            ZeroMemory(Unsafe.AsPointer(ref pThis.m_D3D12Options), (nuint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS));
            ZeroMemory(Unsafe.AsPointer(ref pThis.m_D3D12Architecture), (nuint)sizeof(D3D12_FEATURE_DATA_ARCHITECTURE));

            foreach (ref D3D12MA_IntrusiveLinkedList<D3D12MA_Pool> pool in pThis.m_Pools.AsSpan())
            {
                D3D12MA_IntrusiveLinkedList<D3D12MA_Pool>._ctor(ref pool);
            }

            ZeroMemory(Unsafe.AsPointer(ref pThis.m_BlockVectors), (nuint)sizeof(_D3D12MA_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<Pointer<D3D12MA_BlockVector>>));

            for (uint i = 0; i < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++i)
            {
                ref D3D12MA_CommittedAllocationList committedAllocations = ref pThis.m_CommittedAllocations[(int)i];

                D3D12MA_CommittedAllocationList._ctor(ref committedAllocations);

                committedAllocations.Init(
                    pThis.m_UseMutex != 0,
                    (D3D12_HEAP_TYPE)(D3D12_HEAP_TYPE_DEFAULT + (int)i),
                    null); // pool
            }

            _ = pThis.m_Device->AddRef();
            _ = pThis.m_Adapter->AddRef();
        }

        [return: NativeTypeName("HRESULT")]
        internal int Init(D3D12MA_ALLOCATOR_DESC* desc)
        {
            D3D12MA_Allocator* pThis = (D3D12MA_Allocator*)Unsafe.AsPointer(ref this);

            if (D3D12MA_DXGI_1_4 != 0)
            {
                _ = desc->pAdapter->QueryInterface(__uuidof<IDXGIAdapter3>(), (void**)&pThis->m_Adapter3);
            }

            _ = m_Device->QueryInterface(__uuidof<ID3D12Device4>(), (void**)&pThis->m_Device4);
            _ = m_Device->QueryInterface(__uuidof<ID3D12Device8>(), (void**)&pThis->m_Device8);

            HRESULT hr = m_Adapter->GetDesc(&pThis->m_AdapterDesc);

            if (FAILED(hr))
            {
                return hr;
            }

            hr = m_Device->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, &pThis->m_D3D12Options, (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS));

            if (FAILED(hr))
            {
                return hr;
            }

            if (D3D12MA_FORCE_RESOURCE_HEAP_TIER != 0)
            {
                m_D3D12Options.ResourceHeapTier = (D3D12_RESOURCE_HEAP_TIER)D3D12MA_FORCE_RESOURCE_HEAP_TIER;
            }

            hr = m_Device->CheckFeatureSupport(D3D12_FEATURE_ARCHITECTURE, &pThis->m_D3D12Architecture, (uint)sizeof(D3D12_FEATURE_DATA_ARCHITECTURE));
            if (FAILED(hr))
            {
                m_D3D12Architecture.UMA = FALSE;
                m_D3D12Architecture.CacheCoherentUMA = FALSE;
            }

            D3D12_HEAP_PROPERTIES heapProps = default;
            uint defaultPoolCount = CalcDefaultPoolCount();

            for (uint i = 0; i < defaultPoolCount; ++i)
            {
                D3D12_HEAP_FLAGS heapFlags;
                CalcDefaultPoolParams(&heapProps.Type, &heapFlags, i);

                var blockVector = D3D12MA_NEW<D3D12MA_BlockVector>(GetAllocs());
                D3D12MA_BlockVector._ctor(
                    ref *blockVector,
                    pThis, // hAllocator
                    &heapProps, // heapType
                    heapFlags, // heapFlags
                    m_PreferredBlockSize,
                    0, // minBlockCount
                    nuint.MaxValue, // maxBlockCount
                    false, // explicitBlockSize
                    D3D12MA_DEBUG_ALIGNMENT // minAllocationAlignment
                );
                m_BlockVectors[(int)i] = blockVector;

                // No need to call m_pBlockVectors[i]->CreateMinBlocks here, becase minBlockCount is 0.
            }

            if ((D3D12MA_DXGI_1_4 != 0) && (m_Adapter3 != null))
            {
                _ = UpdateD3D12Budget();
            }

            return S_OK;
        }

        void IDisposable.Dispose()
        {
            SAFE_RELEASE(ref m_Device8);
            SAFE_RELEASE(ref m_Device4);

            if (D3D12MA_DXGI_1_4 != 0)
            {
                SAFE_RELEASE(ref m_Adapter3);
            }

            SAFE_RELEASE(ref m_Adapter);
            SAFE_RELEASE(ref m_Device);

            for (uint i = D3D12MA_DEFAULT_POOL_MAX_COUNT; unchecked(i-- > 0);)
            {
                D3D12MA_DELETE<D3D12MA_BlockVector>(GetAllocs(), m_BlockVectors[(int)i]);
            }

            for (uint i = D3D12MA_HEAP_TYPE_COUNT; unchecked(i-- > 0);)
            {
                if (!m_Pools[(int)i].IsEmpty())
                {
                    D3D12MA_ASSERT(false); // "Unfreed pools found!"
                }

                m_Pools[(int)i].Dispose();
            }

            m_AllocationObjectAllocator.Dispose();
        }

        internal readonly ID3D12Device* GetDevice() => m_Device;

        internal readonly ID3D12Device4* GetDevice4() => m_Device4;

        internal readonly ID3D12Device8* GetDevice8() => m_Device8;

        // Shortcut for "Allocation Callbacks", because this function is called so often.
        [return: NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")]
        internal readonly D3D12MA_ALLOCATION_CALLBACKS* GetAllocs() => (D3D12MA_ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref Unsafe.AsRef(in m_AllocationCallbacks));

        private readonly bool SupportsResourceHeapTier2() => m_D3D12Options.ResourceHeapTier >= D3D12_RESOURCE_HEAP_TIER_2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly bool UseMutex() => m_UseMutex != 0;

        internal D3D12MA_AllocationObjectAllocator* GetAllocationObjectAllocator() => (D3D12MA_AllocationObjectAllocator*)Unsafe.AsPointer(ref m_AllocationObjectAllocator);

        private readonly bool HeapFlagsFulfillResourceHeapTier(D3D12_HEAP_FLAGS flags)
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

        [return: NativeTypeName("HRESULT")]
        private int CreateResourcePimpl(D3D12MA_ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pAllocDesc != null) && (pResourceDesc != null) && (ppAllocation != null));

            *ppAllocation = null;

            if (ppvResource != null)
            {
                *ppvResource = null;
            }

            D3D12_RESOURCE_DESC finalResourceDesc = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&finalResourceDesc);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (resAllocInfo.SizeInBytes > 0));

            D3D12MA_BlockVector* blockVector = null;
            D3D12MA_CommittedAllocationParameters committedAllocationParams;
            D3D12MA_CommittedAllocationParameters._ctor(out committedAllocationParams);
            bool preferCommitted = false;
            int hr = CalcAllocationParams(
                pAllocDesc,
                resAllocInfo.SizeInBytes,
                pResourceDesc,
                out blockVector,
                out committedAllocationParams,
                out preferCommitted);

            if (FAILED(hr))
            {
                return hr;
            }

            bool withinBudget = (pAllocDesc->Flags & D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET) != 0;
            hr = E_INVALIDARG;
            if ((committedAllocationParams.IsValid()) && preferCommitted)
            {
                hr = AllocateCommittedResource(
                    &committedAllocationParams,
                    resAllocInfo.SizeInBytes,
                    withinBudget,
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
            }
            if (blockVector != null)
            {
                hr = blockVector->CreateResource(
                    resAllocInfo.SizeInBytes,
                    resAllocInfo.Alignment,
                    pAllocDesc,
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
            }
            if ((committedAllocationParams.IsValid()) && !preferCommitted)
            {
                hr = AllocateCommittedResource(
                    &committedAllocationParams,
                    resAllocInfo.SizeInBytes,
                    withinBudget,
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
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int CreateResource1Pimpl(D3D12MA_ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
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

            D3D12_RESOURCE_DESC finalResourceDesc = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&finalResourceDesc);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (resAllocInfo.SizeInBytes > 0));

            D3D12MA_BlockVector* blockVector = null;
            D3D12MA_CommittedAllocationParameters committedAllocationParams;
            D3D12MA_CommittedAllocationParameters._ctor(out committedAllocationParams);
            bool preferCommitted = false;
            int hr = CalcAllocationParams(
                pAllocDesc,
                resAllocInfo.SizeInBytes,
                pResourceDesc,
                out blockVector,
                out committedAllocationParams,
                out preferCommitted);

            if (FAILED(hr))
            {
                return hr;
            }

            bool withinBudget = (pAllocDesc->Flags & D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET) != 0;
            // In current implementation it must always be allocated as committed.
            if (committedAllocationParams.IsValid())
            {
                return AllocateCommittedResource1(
                    &committedAllocationParams,
                    resAllocInfo.SizeInBytes,
                    withinBudget,
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
                return E_INVALIDARG;
            }
        }

        [return: NativeTypeName("HRESULT")]
        private int CreateResource2Pimpl(D3D12MA_ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pAllocDesc != null) && (pResourceDesc != null) && (ppAllocation != null));

            *ppAllocation = null;

            if (ppvResource != null)
            {
                *ppvResource = null;
            }

            if (m_Device8 == null)
            {
                return E_NOINTERFACE;
            }

            D3D12_RESOURCE_DESC1 finalResourceDesc = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&finalResourceDesc);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (resAllocInfo.SizeInBytes > 0));

            D3D12MA_BlockVector* blockVector = null;
            D3D12MA_CommittedAllocationParameters committedAllocationParams;
            D3D12MA_CommittedAllocationParameters._ctor(out committedAllocationParams);
            bool preferCommitted = false;
            int hr = CalcAllocationParams(
                pAllocDesc,
                resAllocInfo.SizeInBytes,
                (D3D12_RESOURCE_DESC*)pResourceDesc,
                out blockVector,
                out committedAllocationParams,
                out preferCommitted);

            if (FAILED(hr))
            {
                return hr;
            }

            if (pProtectedSession != null)
            {
                blockVector = null; // Must be committed allocation.
            }

            bool withinBudget = (pAllocDesc->Flags & D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET) != 0;
            hr = E_INVALIDARG;
            if ((committedAllocationParams.IsValid()) && preferCommitted)
            {
                hr = AllocateCommittedResource2(
                    &committedAllocationParams,
                    resAllocInfo.SizeInBytes,
                    withinBudget,
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
            }
            if (blockVector != null)
            {
                hr = blockVector->CreateResource2(
                    resAllocInfo.SizeInBytes,
                    resAllocInfo.Alignment,
                    pAllocDesc,
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
            }
            if ((committedAllocationParams.IsValid()) && !preferCommitted)
            {
                hr = AllocateCommittedResource2(
                    &committedAllocationParams,
                    resAllocInfo.SizeInBytes,
                    withinBudget,
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
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateMemoryPimpl(D3D12MA_ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation)
        {
            *ppAllocation = null;

            D3D12MA_BlockVector* blockVector = null;
            D3D12MA_CommittedAllocationParameters committedAllocationParams;
            D3D12MA_CommittedAllocationParameters._ctor(out committedAllocationParams);
            bool preferCommitted = false;
            int hr = CalcAllocationParams(
                pAllocDesc,
                pAllocInfo->SizeInBytes,
                null, // pResDesc
                out blockVector,
                out committedAllocationParams,
                out preferCommitted);

            if (FAILED(hr))
            {
                return hr;
            }

            bool withinBudget = (pAllocDesc->Flags & D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET) != 0;
            hr = E_INVALIDARG;
            if (committedAllocationParams.IsValid() && preferCommitted)
            {
                hr = AllocateHeap(&committedAllocationParams, pAllocInfo, withinBudget, ppAllocation);
                if (SUCCEEDED(hr))
                {
                    return hr;
                }
            }
            if (blockVector != null)
            {
                hr = blockVector->Allocate(pAllocInfo->SizeInBytes, pAllocInfo->Alignment, pAllocDesc, 1, ppAllocation);
                if (SUCCEEDED(hr))
                {
                    return hr;
                }
            }
            if (committedAllocationParams.IsValid() && !preferCommitted)
            {
                hr = AllocateHeap(&committedAllocationParams, pAllocInfo, withinBudget, ppAllocation);
                if (SUCCEEDED(hr))
                {
                    return hr;
                }
            }
            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateMemory1Pimpl(D3D12MA_ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, ID3D12ProtectedResourceSession *pProtectedSession, D3D12MA_Allocation** ppAllocation)
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

            D3D12MA_BlockVector* blockVector = null;
            D3D12MA_CommittedAllocationParameters committedAllocationParams;
            D3D12MA_CommittedAllocationParameters._ctor(out committedAllocationParams);
            bool preferCommitted = false;
            int hr = CalcAllocationParams(
                pAllocDesc,
                pAllocInfo->SizeInBytes,
                null, // pResDesc
                out blockVector,
                out committedAllocationParams,
                out preferCommitted);

            if (FAILED(hr))
            {
                return hr;
            }

            bool withinBudget = (pAllocDesc->Flags & D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET) != 0;
            // In current implementation it must always be allocated as separate CreateHeap1.
            if (committedAllocationParams.IsValid())
            {
                return AllocateHeap1(&committedAllocationParams, pAllocInfo, withinBudget, pProtectedSession, ppAllocation);
            }
            else
            {
                return E_INVALIDARG;
            }
        }

        [return: NativeTypeName("HRESULT")]
        private int CreateAliasingResourcePimpl(D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE&")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            *ppvResource = null;

            D3D12_RESOURCE_DESC resourceDesc2 = *pResourceDesc;
            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = GetResourceAllocationInfo(&resourceDesc2);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && IsPow2(resAllocInfo.Alignment));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (resAllocInfo.SizeInBytes > 0));

            ID3D12Heap* existingHeap = pAllocation->GetHeap();
            ulong existingOffset = pAllocation->GetOffset();
            ulong existingSize = pAllocation->GetSize();
            ulong newOffset = existingOffset + AllocationLocalOffset;

            if ((existingHeap == null) || ((AllocationLocalOffset + resAllocInfo.SizeInBytes) > existingSize) || ((newOffset % resAllocInfo.Alignment) != 0))
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
                ppvResource
            );
        }

        /// <summary>
        /// Unregisters allocation from the collection of dedicated allocations.
        /// Allocation object must be deleted externally afterwards.
        /// </summary>
        internal void FreeCommittedMemory([NativeTypeName("Allocation*")] ref D3D12MA_Allocation allocation)
        {
            FreeCommittedMemory((D3D12MA_Allocation*)Unsafe.AsPointer(ref allocation));
        }

        private void FreeCommittedMemory([NativeTypeName("Allocation*")] D3D12MA_Allocation* allocation)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocation != null) && (allocation->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_COMMITTED));

            D3D12MA_CommittedAllocationList* allocList = allocation->m_Committed.list;
            allocList->Unregister(allocation);

            ulong allocationSize = allocation->GetSize();
            uint heapTypeIndex = HeapTypeToIndex(allocList->GetHeapType());
            m_Budget.RemoveCommittedAllocation(heapTypeIndex, allocationSize);
        }

        /// <summary>
        /// Unregisters allocation from the collection of placed allocations.
        /// Allocation object must be deleted externally afterwards.
        /// </summary>
        internal void FreePlacedMemory([NativeTypeName("Allocation*")] ref D3D12MA_Allocation allocation)
        {
            FreePlacedMemory((D3D12MA_Allocation*)Unsafe.AsPointer(ref allocation));
        }

        private void FreePlacedMemory([NativeTypeName("Allocation*")] D3D12MA_Allocation* allocation)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && allocation != null && allocation->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_PLACED);
            D3D12MA_NormalBlock* block = allocation->m_Placed.block;

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (block != null));
            D3D12MA_BlockVector* blockVector = block->GetBlockVector();

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (blockVector != null));
            m_Budget.RemoveAllocation(HeapTypeToIndex(block->GetHeapProperties()->Type), allocation->GetSize());

            blockVector->Free(allocation);
        }

        /// <summary>
        /// Unregisters allocation from the collection of dedicated allocations and destroys associated heap.
        /// Allocation object must be deleted externally afterwards.
        /// </summary>
        internal void FreeHeapMemory([NativeTypeName("Allocation*")] ref D3D12MA_Allocation allocation)
        {
            FreeHeapMemory((D3D12MA_Allocation*)Unsafe.AsPointer(ref allocation));
        }

        private void FreeHeapMemory([NativeTypeName("Allocation*")] D3D12MA_Allocation* allocation)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && allocation != null && allocation->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_HEAP);

            D3D12MA_CommittedAllocationList* allocList = allocation->m_Committed.list;
            allocList->Unregister(allocation);

            SAFE_RELEASE(ref allocation->m_Union.m_Heap.heap);

            uint heapTypeIndex = HeapTypeToIndex(allocList->GetHeapType());
            ulong allocationSize = allocation->GetSize();
            m_Budget.RemoveCommittedAllocation(heapTypeIndex, allocationSize);
        }

        private void SetCurrentFrameIndexPimpl([NativeTypeName("UINT")] uint frameIndex)
        {
            m_CurrentFrameIndex = frameIndex;

            if ((D3D12MA_DXGI_1_4 != 0) && (m_Adapter3 != null))
            {
                _ = UpdateD3D12Budget();
            }
        }

        [return: NativeTypeName("UINT")]
        internal readonly uint GetCurrentFrameIndex() => m_CurrentFrameIndex;

        private void CalculateStatsPimpl(D3D12MA_Stats* outStats)
        {
            // Init stats
            ZeroMemory(outStats, (uint)sizeof(D3D12MA_Stats));

            outStats->Total.AllocationSizeMin = UINT64_MAX;
            outStats->Total.UnusedRangeSizeMin = UINT64_MAX;

            for (nuint i = 0; i < D3D12MA_HEAP_TYPE_COUNT; i++)
            {
                outStats->HeapType[(int)i].AllocationSizeMin = UINT64_MAX;
                outStats->HeapType[(int)i].UnusedRangeSizeMin = UINT64_MAX;
            }

            // Process deafult pools.
            if (SupportsResourceHeapTier2())
            {
                for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapTypeIndex)
                {
                    D3D12MA_BlockVector* pBlockVector = m_BlockVectors[(int)heapTypeIndex];
                    D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pBlockVector != null));
                    pBlockVector->AddStats(outStats);
                }
            }
            else
            {
                for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapTypeIndex)
                {
                    for (nuint heapSubType = 0; heapSubType < 3; ++heapSubType)
                    {
                        D3D12MA_BlockVector* pBlockVector = m_BlockVectors[(int)((heapTypeIndex * 3) + heapSubType)];
                        D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pBlockVector != null));
                        pBlockVector->AddStats(outStats);
                    }
                }
            }

            // Process custom pools
            for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_HEAP_TYPE_COUNT; ++heapTypeIndex)
            {
                using var @lock = new D3D12MA_MutexLockRead(ref m_PoolsMutex[(int)heapTypeIndex], m_UseMutex != 0);

                D3D12MA_IntrusiveLinkedList<D3D12MA_Pool>* poolList =
                    (D3D12MA_IntrusiveLinkedList<D3D12MA_Pool>*)Unsafe.AsPointer(ref m_Pools[(int)heapTypeIndex]);
                for (D3D12MA_Pool* pool = poolList->Front(); pool != null; pool = D3D12MA_IntrusiveLinkedList<D3D12MA_Pool>.GetNext(pool))
                {
                    pool->AddStats(outStats);
                }
            }

            // Process committed allocations.
            for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapTypeIndex)
            {
                Unsafe.SkipInit(out D3D12MA_StatInfo statInfo); // Uninitialized.
                m_CommittedAllocations[(int)heapTypeIndex].CalculateStats(ref statInfo);
                AddStatInfo(ref outStats->Total, ref statInfo);
                AddStatInfo(ref outStats->HeapType[(int)heapTypeIndex], ref statInfo);
            }

            // Post process
            PostProcessStatInfo(ref outStats->Total);
            for (nuint i = 0; i < D3D12MA_HEAP_TYPE_COUNT; ++i)
                PostProcessStatInfo(ref outStats->HeapType[(int)i]);
        }

        private void GetBudgetPimpl(D3D12MA_Budget* outGpuBudget, D3D12MA_Budget* outCpuBudget)
        {
            if (outGpuBudget != null)
            {
                // Taking DEFAULT.
                outGpuBudget->BlockBytes = Volatile.Read(ref m_Budget.m_BlockBytes[0]);
                outGpuBudget->AllocationBytes = Volatile.Read(ref m_Budget.m_AllocationBytes[0]);
            }

            if (outCpuBudget != null)
            {
                // Taking UPLOAD + READBACK.
                outCpuBudget->BlockBytes = Volatile.Read(ref m_Budget.m_BlockBytes[1]) + Volatile.Read(ref m_Budget.m_BlockBytes[2]);
                outCpuBudget->AllocationBytes = Volatile.Read(ref m_Budget.m_AllocationBytes[1]) + Volatile.Read(ref m_Budget.m_AllocationBytes[2]);
            }
            // TODO: What to do with CUSTOM?

            if (D3D12MA_DXGI_1_4 != 0)
            {
                if (m_Adapter3 != null)
                {
                    if (m_Budget.m_OperationsSinceBudgetFetch < 30)
                    {
                        using var @lock = new D3D12MA_MutexLockRead(ref m_Budget.m_BudgetMutex, m_UseMutex != 0);

                        if (outGpuBudget != null)
                        {
                            if (m_Budget.m_D3D12UsageLocal + outGpuBudget->BlockBytes > m_Budget.m_BlockBytesAtBudgetFetch[0])
                            {
                                outGpuBudget->UsageBytes = m_Budget.m_D3D12UsageLocal + outGpuBudget->BlockBytes - m_Budget.m_BlockBytesAtBudgetFetch[0];
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
                                outCpuBudget->UsageBytes = m_Budget.m_D3D12UsageNonLocal + outCpuBudget->BlockBytes - (m_Budget.m_BlockBytesAtBudgetFetch[1] + m_Budget.m_BlockBytesAtBudgetFetch[2]);
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
                        _ = UpdateD3D12Budget(); // Outside of mutex lock
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

        internal void GetBudgetForHeapType(D3D12MA_Budget* outBudget, D3D12_HEAP_TYPE heapType)
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

        private void BuildStatsStringPimpl([NativeTypeName("WCHAR**")] ushort** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap)
        {
            using var sb = new D3D12MA_StringBuilder(GetAllocs());

            using (var json = new D3D12MA_JsonWriter(GetAllocs(), &sb))
            {
                D3D12MA_Budget gpuBudget = default, cpuBudget = default;
                GetBudget(&gpuBudget, &cpuBudget);

                D3D12MA_Stats stats;
                CalculateStats(&stats);

                json.BeginObject();

                json.WriteString("Total");
                AddStatInfoToJson(&json, &stats.Total);

                for (nuint heapType = 0; heapType < D3D12MA_HEAP_TYPE_COUNT; ++heapType)
                {
                    json.WriteString(HeapTypeNames[heapType]);
                    AddStatInfoToJson(&json, (D3D12MA_StatInfo*)Unsafe.AsPointer(ref stats.HeapType[(int)heapType]));
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

                if (DetailedMap != 0)
                {
                    json.WriteString("DetailedMap");
                    json.BeginObject();

                    json.WriteString("DefaultPools");
                    json.BeginObject();

                    if (SupportsResourceHeapTier2())
                    {
                        for (nuint heapType = 0; heapType < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapType)
                        {
                            json.WriteString(HeapTypeNames[heapType]);
                            json.BeginObject();

                            json.WriteString("Blocks");

                            D3D12MA_BlockVector* blockVector = m_BlockVectors[(int)heapType];
                            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (blockVector != null));
                            blockVector->WriteBlockInfoToJson(&json);

                            json.EndObject(); // heap name
                        }
                    }
                    else
                    {
                        for (nuint heapType = 0; heapType < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapType)
                        {
                            for (nuint heapSubType = 0; heapSubType < 3; ++heapSubType)
                            {
                                json.BeginString();
                                json.ContinueString(HeapTypeNames[heapType]);
                                json.ContinueString(heapSubTypeName[heapSubType]);
                                json.EndString();
                                json.BeginObject();

                                json.WriteString("Blocks");

                                D3D12MA_BlockVector* blockVector = m_BlockVectors[(int)((heapType * 3) + heapSubType)];
                                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (blockVector != null));
                                blockVector->WriteBlockInfoToJson(&json);

                                json.EndObject(); // heap name
                            }
                        }
                    }

                    json.EndObject(); // DefaultPools

                    json.WriteString("CommittedAllocations");
                    json.BeginObject();

                    for (nuint heapTypeIndex = 0; heapTypeIndex < D3D12MA_STANDARD_HEAP_TYPE_COUNT; ++heapTypeIndex)
                    {
                        json.WriteString(HeapTypeNames[(int)heapTypeIndex]);
                        m_CommittedAllocations[(int)heapTypeIndex].BuildStatsString(ref *&json);
                    }

                    json.EndObject(); // CommittedAllocations

                    json.EndObject(); // DetailedMap
                }

                json.EndObject();
            }

            nuint length = sb.GetLength();
            ushort* result = AllocateArray<ushort>(GetAllocs(), length + 1);

            _ = memcpy(result, sb.GetData(), length * sizeof(ushort));

            result[length] = '\0';
            *ppStatsString = result;
        }

        private void FreeStatsStringPimpl([NativeTypeName("WCHAR*")] ushort* pStatsString)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pStatsString != null));
            Free(GetAllocs(), pStatsString);
        }

        /// <summary>
        /// Heuristics that decides whether a resource should better be placed in its own,
        /// dedicated allocation(committed resource rather than placed resource).
        /// </summary>
        private static bool PrefersCommittedAllocation<TD3D12_RESOURCE_DESC>(TD3D12_RESOURCE_DESC* resourceDesc)
            where TD3D12_RESOURCE_DESC : unmanaged
        {
            // Intentional. It may change in the future.
            return false;
        }

        // Allocates and registers new committed resource with implicit heap, as dedicated allocation.
        // Creates and returns Allocation object and optionally D3D12 resource.
        [return: NativeTypeName("HRESULT")]
        private int AllocateCommittedResource([NativeTypeName("const CommittedAllocationParameters&")] D3D12MA_CommittedAllocationParameters* committedAllocParams, [NativeTypeName("UINT64")] ulong resourceSize, bool withinBudget, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (committedAllocParams->IsValid()));

            if (withinBudget &&
                !NewAllocationWithinBudget(committedAllocParams->m_HeapProperties.Type, resourceSize))
            {
                return E_OUTOFMEMORY;
            }

            ID3D12Resource* res = null;
            HRESULT hr = m_Device->CreateCommittedResource(
                &committedAllocParams->m_HeapProperties,
                committedAllocParams->m_HeapFlags & ~RESOURCE_CLASS_HEAP_FLAGS, // D3D12 ERROR: ID3D12Device::CreateCommittedResource: When creating a committed resource, D3D12_HEAP_FLAGS must not have either D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES, D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES, nor D3D12_HEAP_FLAG_DENY_BUFFERS set. These flags will be set automatically to correspond with the committed resource type. [ STATE_CREATION ERROR #640: CREATERESOURCEANDHEAP_INVALIDHEAPMISCFLAGS]
                pResourceDesc,
                InitialResourceState,
                pOptimizedClearValue,
                __uuidof<ID3D12Resource>(),
                (void**)&res);

            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    const int wasZeroInitialized = 1;                    
                    D3D12MA_Allocation* alloc = m_AllocationObjectAllocator.Allocate((D3D12MA_Allocator*)Unsafe.AsPointer(ref this), resourceSize, wasZeroInitialized);
                    alloc->InitCommitted(ref *committedAllocParams->m_List);
                    alloc->SetResource(res, pResourceDesc);

                    *ppAllocation = alloc;

                    committedAllocParams->m_List->Register(alloc);

                    uint heapTypeIndex = HeapTypeToIndex(committedAllocParams->m_HeapProperties.Type);
                    m_Budget.AddCommittedAllocation(heapTypeIndex, resourceSize);
                }
                else
                {
                    _ = res->Release();
                }
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateCommittedResource1([NativeTypeName("const CommittedAllocationParameters&")] D3D12MA_CommittedAllocationParameters* committedAllocParams, [NativeTypeName("UINT64")] ulong resourceSize, bool withinBudget, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (committedAllocParams->IsValid()));

            if (m_Device4 == null)
            {
                return E_NOINTERFACE;
            }

            if (withinBudget &&
                !NewAllocationWithinBudget(committedAllocParams->m_HeapProperties.Type, resourceSize))
            {
                return E_OUTOFMEMORY;
            }

            ID3D12Resource* res = null;
            HRESULT hr = m_Device4->CreateCommittedResource1(
                &committedAllocParams->m_HeapProperties,
                committedAllocParams->m_HeapFlags & ~RESOURCE_CLASS_HEAP_FLAGS, // D3D12 ERROR: ID3D12Device::CreateCommittedResource: When creating a committed resource, D3D12_HEAP_FLAGS must not have either D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES, D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES, nor D3D12_HEAP_FLAG_DENY_BUFFERS set. These flags will be set automatically to correspond with the committed resource type. [ STATE_CREATION ERROR #640: CREATERESOURCEANDHEAP_INVALIDHEAPMISCFLAGS]
                pResourceDesc,
                InitialResourceState,
                pOptimizedClearValue,
                pProtectedSession,
                __uuidof<ID3D12Resource>(),
                (void**)&res);

            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    const int wasZeroInitialized = 1;
                    D3D12MA_Allocation* alloc = m_AllocationObjectAllocator.Allocate((D3D12MA_Allocator*)Unsafe.AsPointer(ref this), resourceSize, wasZeroInitialized);
                    alloc->InitCommitted(ref *committedAllocParams->m_List);
                    alloc->SetResource(res, pResourceDesc);

                    *ppAllocation = alloc;

                    committedAllocParams->m_List->Register(alloc);

                    uint heapTypeIndex = HeapTypeToIndex(committedAllocParams->m_HeapProperties.Type);
                    m_Budget.AddCommittedAllocation(heapTypeIndex, resourceSize);
                }
                else
                {
                    _ = res->Release();
                }
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateCommittedResource2([NativeTypeName("const CommittedAllocationParameters&")] D3D12MA_CommittedAllocationParameters* committedAllocParams, [NativeTypeName("UINT64")] ulong resourceSize, bool withinBudget, [NativeTypeName("const D3D12_RESOURCE_DESC1*")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession* pProtectedSession, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (committedAllocParams->IsValid()));

            if (m_Device8 == null)
            {
                return E_NOINTERFACE;
            }

            if (withinBudget &&
                !NewAllocationWithinBudget(committedAllocParams->m_HeapProperties.Type, resourceSize))
            {
                return E_OUTOFMEMORY;
            }

            ID3D12Resource* res = null;
            HRESULT hr = m_Device8->CreateCommittedResource2(
                &committedAllocParams->m_HeapProperties,
                committedAllocParams->m_HeapFlags & ~RESOURCE_CLASS_HEAP_FLAGS, // D3D12 ERROR: ID3D12Device::CreateCommittedResource: When creating a committed resource, D3D12_HEAP_FLAGS must not have either D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES, D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES, nor D3D12_HEAP_FLAG_DENY_BUFFERS set. These flags will be set automatically to correspond with the committed resource type. [ STATE_CREATION ERROR #640: CREATERESOURCEANDHEAP_INVALIDHEAPMISCFLAGS]
                pResourceDesc,
                InitialResourceState,
                pOptimizedClearValue,
                pProtectedSession,
                __uuidof<ID3D12Resource>(),
                (void**)&res);

            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    const int wasZeroInitialized = 1;
                    D3D12MA_Allocation* alloc = m_AllocationObjectAllocator.Allocate((D3D12MA_Allocator*)Unsafe.AsPointer(ref this), resourceSize, wasZeroInitialized);

                    alloc->InitCommitted(ref *committedAllocParams->m_List);
                    alloc->SetResource(res, pResourceDesc);

                    *ppAllocation = alloc;

                    committedAllocParams->m_List->Register(alloc);

                    uint heapTypeIndex = HeapTypeToIndex(committedAllocParams->m_HeapProperties.Type);
                    m_Budget.AddCommittedAllocation(heapTypeIndex, resourceSize);
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
        [return: NativeTypeName("HRESULT")]
        private int AllocateHeap([NativeTypeName("const CommittedAllocationParameters&")] D3D12MA_CommittedAllocationParameters* committedAllocParams, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO&")] D3D12_RESOURCE_ALLOCATION_INFO* allocInfo, bool withinBudget, D3D12MA_Allocation** ppAllocation)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (committedAllocParams->IsValid()));

            *ppAllocation = null;

            if (withinBudget &&
                !NewAllocationWithinBudget(committedAllocParams->m_HeapProperties.Type, allocInfo->SizeInBytes))
            {
                return E_OUTOFMEMORY;
            }

            D3D12_HEAP_DESC heapDesc = default;
            heapDesc.SizeInBytes = allocInfo->SizeInBytes;
            heapDesc.Properties = committedAllocParams->m_HeapProperties;
            heapDesc.Alignment = allocInfo->Alignment;
            heapDesc.Flags = committedAllocParams->m_HeapFlags;

            ID3D12Heap* heap = null;
            HRESULT hr = m_Device->CreateHeap(&heapDesc, __uuidof<ID3D12Heap>(), (void**)&heap);
            if (SUCCEEDED(hr))
            {
                const int wasZeroInitialized = 1;
                *ppAllocation = m_AllocationObjectAllocator.Allocate((D3D12MA_Allocator*)Unsafe.AsPointer(ref this), allocInfo->SizeInBytes, wasZeroInitialized);
                (*ppAllocation)->InitHeap(ref *committedAllocParams->m_List, heap);
                committedAllocParams->m_List->Register(*ppAllocation);

                uint heapTypeIndex = HeapTypeToIndex(committedAllocParams->m_HeapProperties.Type);
                m_Budget.AddCommittedAllocation(heapTypeIndex, allocInfo->SizeInBytes);
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocateHeap1([NativeTypeName("const CommittedAllocationParameters&")] D3D12MA_CommittedAllocationParameters* committedAllocParams, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO&")] D3D12_RESOURCE_ALLOCATION_INFO* allocInfo, bool withinBudget, ID3D12ProtectedResourceSession* pProtectedSession, D3D12MA_Allocation** ppAllocation)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (committedAllocParams->IsValid()));

            *ppAllocation = null;

            if (m_Device4 == null)
            {
                return E_NOINTERFACE;
            }

            if (withinBudget &&
                !NewAllocationWithinBudget(committedAllocParams->m_HeapProperties.Type, allocInfo->SizeInBytes))
            {
                return E_OUTOFMEMORY;
            }

            D3D12_HEAP_DESC heapDesc = default;
            heapDesc.SizeInBytes = allocInfo->SizeInBytes;
            heapDesc.Properties = committedAllocParams->m_HeapProperties;
            heapDesc.Alignment = allocInfo->Alignment;
            heapDesc.Flags = committedAllocParams->m_HeapFlags;

            ID3D12Heap* heap = null;
            HRESULT hr = m_Device4->CreateHeap1(&heapDesc, pProtectedSession, __uuidof<ID3D12Heap>(), (void**)&heap);
            if (SUCCEEDED(hr))
            {
                const int wasZeroInitialized = 1;
                *ppAllocation = m_AllocationObjectAllocator.Allocate((D3D12MA_Allocator*)Unsafe.AsPointer(ref this), allocInfo->SizeInBytes, wasZeroInitialized);
                (*ppAllocation)->InitHeap(ref *committedAllocParams->m_List, heap);
                committedAllocParams->m_List->Register(*ppAllocation);

                uint heapTypeIndex = HeapTypeToIndex(committedAllocParams->m_HeapProperties.Type);
                m_Budget.AddCommittedAllocation(heapTypeIndex, allocInfo->SizeInBytes);
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        private int CalcAllocationParams([NativeTypeName("const ALLOCATION_DESC&")] D3D12MA_ALLOCATION_DESC* allocDesc, ulong allocSize, [NativeTypeName("const D3D12_RESOURCE_DESC_T*")] D3D12_RESOURCE_DESC* resDesc, [NativeTypeName("BlockVector*&")] out D3D12MA_BlockVector* outBlockVector, [NativeTypeName("CommittedAllocationParameters&")] out D3D12MA_CommittedAllocationParameters outCommittedAllocationParams, [NativeTypeName("bool&")] out bool outPreferCommitted)
        {
            outBlockVector = null;
            D3D12MA_CommittedAllocationParameters._ctor(out outCommittedAllocationParams);
            outPreferCommitted = false;

            if (allocDesc->CustomPool != null)
            {
                D3D12MA_Pool* pool = allocDesc->CustomPool;

                outBlockVector = pool->GetBlockVector();

                outCommittedAllocationParams.m_HeapProperties = pool->GetDesc().HeapProperties;
                outCommittedAllocationParams.m_HeapFlags = pool->GetDesc().HeapFlags;
                outCommittedAllocationParams.m_List = pool->GetCommittedAllocationList();
            }
            else
            {
                if (!IsHeapTypeStandard(allocDesc->HeapType))
                {
                    return E_INVALIDARG;
                }

                outCommittedAllocationParams.m_HeapProperties = StandardHeapTypeToHeapProperties(allocDesc->HeapType);
                outCommittedAllocationParams.m_HeapFlags = allocDesc->ExtraHeapFlags;
                outCommittedAllocationParams.m_List = (D3D12MA_CommittedAllocationList*)Unsafe.AsPointer(ref m_CommittedAllocations[(int)HeapTypeToIndex(allocDesc->HeapType)]);

                D3D12MA_ResourceClass resourceClass = (resDesc != null) ?
                    ResourceDescToResourceClass(resDesc) : HeapFlagsToResourceClass(allocDesc->ExtraHeapFlags);
                uint defaultPoolIndex = CalcDefaultPoolIndex(allocDesc, resourceClass);
                if (defaultPoolIndex != Windows.UINT32_MAX)
                {
                    outBlockVector = m_BlockVectors[(int)defaultPoolIndex].Value;
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

                D3D12_HEAP_FLAGS extraHeapFlags = allocDesc->ExtraHeapFlags & ~RESOURCE_CLASS_HEAP_FLAGS;
                if ((outBlockVector != null) && (extraHeapFlags != 0))
                {
                    outBlockVector = null;
                }
            }

            if (((allocDesc->Flags & D3D12MA_ALLOCATION_FLAG_COMMITTED) != 0) ||
                (m_AlwaysCommitted != 0))
            {
                outBlockVector = null;
            }

            if ((allocDesc->Flags & D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE) != 0)
            {
                outCommittedAllocationParams.m_List = null;
            }

            if ((resDesc != null) && (!outPreferCommitted) && PrefersCommittedAllocation(resDesc))
            {
                outPreferCommitted = true;
            }

            return ((outBlockVector != null) || (outCommittedAllocationParams.m_List != null)) ? S_OK : E_INVALIDARG;
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
        private readonly uint CalcDefaultPoolCount()
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

        // Returns UINT32_MAX if index cannot be calculcated.
        [return: NativeTypeName("UINT")]
        private readonly uint CalcDefaultPoolIndex([NativeTypeName("const ALLOCATION_DESC&")] D3D12MA_ALLOCATION_DESC* allocDesc, D3D12MA_ResourceClass resourceClass)
        {
            D3D12_HEAP_FLAGS extraHeapFlags = allocDesc->ExtraHeapFlags & ~RESOURCE_CLASS_HEAP_FLAGS;
            if (extraHeapFlags != 0)
            {
                return Windows.UINT32_MAX;
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
                        return Windows.UINT32_MAX;
                    }
                }
            }
        }

        private readonly void CalcDefaultPoolParams(D3D12_HEAP_TYPE* outHeapType, D3D12_HEAP_FLAGS* outHeapFlags, [NativeTypeName("UINT")] uint index)
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

        /// <summary>Registers Pool object in m_pPools.</summary>
        private void RegisterPool(D3D12MA_Pool* pool, D3D12_HEAP_TYPE heapType)
        {
            uint heapTypeIndex = HeapTypeToIndex(heapType);

            using var @lock = new D3D12MA_MutexLockWrite(ref m_PoolsMutex[(int)heapTypeIndex], m_UseMutex != 0);

            m_Pools[(int)heapTypeIndex].PushBack(pool);
        }

        /// <summary>Unregisters Pool object from m_pPools.</summary>
        internal void UnregisterPool([NativeTypeName("Pool*")] ref D3D12MA_Pool pool, D3D12_HEAP_TYPE heapType)
        {
            UnregisterPool((D3D12MA_Pool*)Unsafe.AsPointer(ref pool), heapType);
        }

        private void UnregisterPool([NativeTypeName("Pool*")] D3D12MA_Pool* pool, D3D12_HEAP_TYPE heapType)
        {
            uint heapTypeIndex = HeapTypeToIndex(heapType);

            using var @lock = new D3D12MA_MutexLockWrite(ref m_PoolsMutex[(int)heapTypeIndex], m_UseMutex != 0);

            m_Pools[(int)heapTypeIndex].Remove(pool);
        }

        [return: NativeTypeName("HRESULT")]
        private int UpdateD3D12Budget()
        {
            if (D3D12MA_DXGI_1_4 != 0)
            {
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_Adapter3 != null));

                DXGI_QUERY_VIDEO_MEMORY_INFO infoLocal = default;
                DXGI_QUERY_VIDEO_MEMORY_INFO infoNonLocal = default;

                HRESULT hrLocal = m_Adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &infoLocal);
                HRESULT hrNonLocal = m_Adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL, &infoNonLocal);

                {
                    using var lockWrite = new D3D12MA_MutexLockWrite(ref m_Budget.m_BudgetMutex, m_UseMutex != 0);

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

                    for (uint i = 0; i < D3D12MA_HEAP_TYPE_COUNT; ++i)
                    {
                        m_Budget.m_BlockBytesAtBudgetFetch[(int)i] = Volatile.Read(ref m_Budget.m_BlockBytes[(int)i]);
                    }

                    m_Budget.m_OperationsSinceBudgetFetch = 0;
                }

                return FAILED(hrLocal) ? hrLocal : hrNonLocal;
            }
            else
            {
                return S_OK;
            }
        }

        private readonly D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfoNative(D3D12_RESOURCE_DESC* resourceDesc)
        {
            return m_Device->GetResourceAllocationInfo(0, 1, resourceDesc);
        }

        private readonly D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfoNative(D3D12_RESOURCE_DESC1* resourceDesc)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_Device8 != null));
            D3D12_RESOURCE_ALLOCATION_INFO1 info1Unused;
            return m_Device8->GetResourceAllocationInfo2(0, 1, resourceDesc, &info1Unused);
        }

        private readonly D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfo([NativeTypeName("D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC* inOutResourceDesc)
        {
            /* Optional optimization: Microsoft documentation says:
            https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-getresourceallocationinfo
    
            Your application can forgo using GetResourceAllocationInfo for buffer resources
            (D3D12_RESOURCE_DIMENSION_BUFFER). Buffers have the same size on all adapters,
            which is merely the smallest multiple of 64KB that's greater or equal to
            D3D12_RESOURCE_DESC::Width.
            */

            if (inOutResourceDesc->Alignment == 0 && inOutResourceDesc->Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return new D3D12_RESOURCE_ALLOCATION_INFO(
                    AlignUp(inOutResourceDesc->Width, D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT), // SizeInBytes
                    D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT // Alignment
                );
            }

            if (D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT != 0)
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
                        (ulong)D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT :
                        (ulong)D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT;

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

        private readonly D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfo([NativeTypeName("D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC1* inOutResourceDesc)
        {
            return GetResourceAllocationInfo((D3D12_RESOURCE_DESC*)inOutResourceDesc);
        }

        private bool NewAllocationWithinBudget(D3D12_HEAP_TYPE heapType, [NativeTypeName("UINT64")] ulong size)
        {
            D3D12MA_Budget budget = default;
            GetBudgetForHeapType(&budget, heapType);
            return budget.UsageBytes + size <= budget.BudgetBytes;
        }

        /// <summary>Writes object { } with data of given budget.</summary>
        private static void WriteBudgetToJson(D3D12MA_JsonWriter* json, D3D12MA_Budget* budget)
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
        private struct _D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer<T>
            where T : unmanaged
        {
#pragma warning disable CS0649
            public T e0;
            public T e1;
            public T e2;
            public T e3;
#pragma warning restore CS0649

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return ref AsSpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan()
            {
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((sizeof(_D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer<T>) / sizeof(T)) == (int)D3D12MA_HEAP_TYPE_COUNT) && ((sizeof(_D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer<T>) % sizeof(T)) == 0));

                return MemoryMarshal.CreateSpan(ref e0, (int)D3D12MA_HEAP_TYPE_COUNT);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private struct _D3D12MA_STANDARD_HEAP_TYPE_COUNT_e__FixedBuffer<T>
            where T : unmanaged
        {
#pragma warning disable CS0649
            public T e0;
            public T e1;
            public T e2;
#pragma warning restore CS0649

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return ref AsSpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref e0, (int)D3D12MA_STANDARD_HEAP_TYPE_COUNT);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private struct _D3D12MA_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<T>
            where T : unmanaged
        {
#pragma warning disable CS0649
            public T e0;
            public T e1;
            public T e2;
            public T e3;
            public T e4;
            public T e5;
            public T e6;
            public T e7;
            public T e8;
#pragma warning restore CS0649

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return ref AsSpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan()
            {
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((sizeof(_D3D12MA_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<T>) / sizeof(T)) == (int)D3D12MA_DEFAULT_POOL_MAX_COUNT) && ((sizeof(_D3D12MA_DEFAULT_POOL_MAX_COUNT_e__FixedBuffer<T>) % sizeof(T)) == 0));

                return MemoryMarshal.CreateSpan(ref e0, (int)D3D12MA_DEFAULT_POOL_MAX_COUNT);
            }
        }
    }
}
