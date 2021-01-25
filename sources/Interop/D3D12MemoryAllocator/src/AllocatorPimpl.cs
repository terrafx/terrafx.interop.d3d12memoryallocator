// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12_RESOURCE_HEAP_TIER;

using UINT = System.UInt32;
using UINT64 = System.UInt64;
using AllocationVectorType = TerraFX.Interop.Vector<TerraFX.Interop.Ptr<TerraFX.Interop.Allocation>>;
using PoolVectorType = TerraFX.Interop.Vector<TerraFX.Interop.Ptr<TerraFX.Interop.Pool>>;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class AllocatorPimpl definition

    internal unsafe partial struct AllocatorPimpl : IDisposable
    {
        public CurrentBudgetData m_Budget;

        public AllocatorPimpl(ALLOCATION_CALLBACKS* allocationCallbacks, ALLOCATION_DESC* desc)
        {
            // TODO
        }

        public partial HRESULT Init(ALLOCATION_DESC* desc);
        public partial void Dispose();

        public ID3D12Device* GetDevice() { return m_Device; }
        public ID3D12Device4* GetDevice4() { return m_Device4; }
        public ID3D12Device8* GetDevice8() { return m_Device8; }

        // Shortcut for "Allocation Callbacks", because this function is called so often.
        public ALLOCATION_CALLBACKS* GetAllocs() { return (ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref m_AllocationCallbacks); }
        public D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options() { return (D3D12_FEATURE_DATA_D3D12_OPTIONS*)Unsafe.AsPointer(ref m_D3D12Options); }
        public bool SupportsResourceHeapTier2() { return m_D3D12Options.ResourceHeapTier >= D3D12_RESOURCE_HEAP_TIER_2; }
        public bool UseMutex() { return m_UseMutex; }
        public ref AllocationObjectAllocator GetAllocationObjectAllocator() { return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref m_AllocationObjectAllocator, 1)); }
        public partial bool HeapFlagsFulfillResourceHeapTier(D3D12_HEAP_FLAGS flags);

        public partial HRESULT CreateResource(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource);

        public partial HRESULT CreateResource1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession *pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource);

        public partial HRESULT CreateResource2(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC1* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession *pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource);

        public partial HRESULT AllocateMemory(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo,
            Allocation** ppAllocation);

        public partial HRESULT AllocateMemory1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo,
            ID3D12ProtectedResourceSession *pProtectedSession,
            Allocation** ppAllocation);

        public partial HRESULT CreateAliasingResource(
            Allocation* pAllocation,
            UINT64 AllocationLocalOffset,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Guid* riidResource,
            void** ppvResource);

        public partial HRESULT SetDefaultHeapMinBytes(
            D3D12_HEAP_TYPE heapType,
            D3D12_HEAP_FLAGS heapFlags,
            UINT64 minBytes);

        // Unregisters allocation from the collection of dedicated allocations.
        // Allocation object must be deleted externally afterwards.
        public partial void FreeCommittedMemory(Allocation* allocation);
        // Unregisters allocation from the collection of placed allocations.
        // Allocation object must be deleted externally afterwards.
        public partial void FreePlacedMemory(Allocation* allocation);
        // Unregisters allocation from the collection of dedicated allocations and destroys associated heap.
        // Allocation object must be deleted externally afterwards.
        public partial void FreeHeapMemory(Allocation* allocation);

        public partial void SetCurrentFrameIndex(uint frameIndex);

        public uint GetCurrentFrameIndex() { return m_CurrentFrameIndex.Load(); }

        public partial void CalculateStats(Stats* outStats);

        public partial void GetBudget(Budget* outGpuBudget, Budget* outCpuBudget);
        public partial void GetBudgetForHeapType(Budget* outBudget, D3D12_HEAP_TYPE heapType);

        public partial void BuildStatsString(char** ppStatsString, int DetailedMap);

        public partial void FreeStatsString(char* pStatsString);

        /// <summary>
        /// Heuristics that decides whether a resource should better be placed in its own,
        /// dedicated allocation(committed resource rather than placed resource).
        /// </summary>
        internal static partial bool PrefersCommittedAllocation<D3D12_RESOURCE_DESC_T>(D3D12_RESOURCE_DESC_T* resourceDesc)
            where D3D12_RESOURCE_DESC_T : unmanaged;

        private readonly bool m_UseMutex;
        private readonly bool m_AlwaysCommitted;
        private ID3D12Device* m_Device; // AddRef
        private ID3D12Device4* m_Device4; // AddRef, optional
        private ID3D12Device8* m_Device8; // AddRef, optional
        private IDXGIAdapter* m_Adapter; // AddRef
        private IDXGIAdapter3* m_Adapter3; //AddRef, optional
        private UINT64 m_PreferredBlockSize;
        private ALLOCATION_CALLBACKS m_AllocationCallbacks;
        private atomic<uint> m_CurrentFrameIndex;
        private DXGI_ADAPTER_DESC m_AdapterDesc;
        private D3D12_FEATURE_DATA_D3D12_OPTIONS m_D3D12Options;
        AllocationObjectAllocator m_AllocationObjectAllocator;

        private __m_Buffer_HEAP_TYPE_COUNT<Ptr<AllocationVectorType>> m_pCommittedAllocations;
        private __m_Buffer_HEAP_TYPE_COUNT<D3D12MA_RW_MUTEX> m_CommittedAllocationsMutex;

        private __m_Buffer_HEAP_TYPE_COUNT<Ptr<PoolVectorType>> m_pPools;
        private __m_Buffer_HEAP_TYPE_COUNT<D3D12MA_RW_MUTEX> m_PoolsMutex;

        // Default pools.
        private __m_Buffer_DEFAULT_POOL_MAX_COUNT<Ptr<BlockVector>> m_BlockVectors;

        // # Used only when ResourceHeapTier = 1
        private __m_Buffer_DEFAULT_POOL_MAX_COUNT<UINT64> m_DefaultPoolTier1MinBytes; // Default 0
        private __m_Buffer_HEAP_TYPE_COUNT<UINT64> m_DefaultPoolHeapTypeMinBytes; // Default UINT64_MAX, meaning not set
        private D3D12MA_RW_MUTEX m_DefaultPoolMinBytesMutex;

        // Allocates and registers new committed resource with implicit heap, as dedicated allocation.
        // Creates and returns Allocation object.
        private partial HRESULT AllocateCommittedResource(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* resAllocInfo,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource);

        private partial HRESULT AllocateCommittedResource1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* resAllocInfo,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession *pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource);

        private partial HRESULT AllocateCommittedResource2(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC1* pResourceDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* resAllocInfo,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession *pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource);

        // Allocates and registers new heap without any resources placed in it, as dedicated allocation.
        // Creates and returns Allocation object.
        private partial HRESULT AllocateHeap(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* allocInfo,
            Allocation** ppAllocation);

        private partial HRESULT AllocateHeap1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* allocInfo,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation);

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
        private partial UINT CalcDefaultPoolCount();
        private partial UINT CalcDefaultPoolIndex<D3D12_RESOURCE_DESC_T>(ALLOCATION_DESC* allocDesc, D3D12_RESOURCE_DESC_T* resourceDesc)
            where D3D12_RESOURCE_DESC_T : unmanaged;
        // This one returns UINT32_MAX if nonstandard heap flags are used and index cannot be calculcated.
        private static partial UINT CalcDefaultPoolIndex(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, bool supportsResourceHeapTier2);
        private UINT CalcDefaultPoolIndex(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags)
        {
            return CalcDefaultPoolIndex(heapType, heapFlags, SupportsResourceHeapTier2());
        }
        UINT CalcDefaultPoolIndex(ALLOCATION_DESC* allocDesc)
        {
            return CalcDefaultPoolIndex(allocDesc->HeapType, allocDesc->ExtraHeapFlags);
        }
        private partial void CalcDefaultPoolParams(D3D12_HEAP_TYPE* outHeapType, D3D12_HEAP_FLAGS* outHeapFlags, UINT index);

        // Registers Allocation object in m_pCommittedAllocations.
        private partial void RegisterCommittedAllocation(Allocation* alloc, D3D12_HEAP_TYPE heapType);
        // Unregisters Allocation object from m_pCommittedAllocations.
        private partial void UnregisterCommittedAllocation(Allocation* alloc, D3D12_HEAP_TYPE heapType);

        // Registers Pool object in m_pPools.
        private partial void RegisterPool(Pool* pool, D3D12_HEAP_TYPE heapType);
        // Unregisters Pool object from m_pPools.
        private partial void UnregisterPool(Pool* pool, D3D12_HEAP_TYPE heapType);

        private partial HRESULT UpdateD3D12Budget();

        private partial D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfoNative(D3D12_RESOURCE_DESC* resourceDesc);
        private partial D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfoNative(D3D12_RESOURCE_DESC1* resourceDesc);

        private partial D3D12_RESOURCE_ALLOCATION_INFO GetResourceAllocationInfo<D3D12_RESOURCE_DESC_T>(D3D12_RESOURCE_DESC_T* inOutResourceDesc)
            where D3D12_RESOURCE_DESC_T : unmanaged;

        private partial bool NewAllocationWithinBudget(D3D12_HEAP_TYPE heapType, UINT64 size);

        // Writes object { } with data of given budget.
        private static partial void WriteBudgetToJson(JsonWriter* json, Budget* budget);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __m_Buffer_HEAP_TYPE_COUNT<T>
        {
            public T _0;
            public T _1;
            public T _2;

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref Unsafe.Add(ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref _0, 1)), index);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __m_Buffer_DEFAULT_POOL_MAX_COUNT<T>
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
                get => ref Unsafe.Add(ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref _0, 1)), index);
            }
        }
    };
}
