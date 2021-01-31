// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.ALLOCATION_FLAGS;
using static TerraFX.Interop.D3D12_HEAP_TYPE;

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents main object of this library initialized for particular <see cref="ID3D12Device"/>.
    /// <para>
    /// Fill structure <see cref="ALLOCATOR_DESC"/> and call function <see cref="CreateAllocator"/> to create it.
    /// Call method <see cref="Release"/> to destroy it.
    /// </para>
    /// <para>
    /// It is recommended to create just one object of this type per <see cref="ID3D12Device"/> object,
    /// right after Direct3D 12 is initialized and keep it alive until before Direct3D device is destroyed.
    /// </para>
    /// </summary>
    public unsafe struct Allocator : IDisposable
    {
        internal AllocatorPimpl* m_Pimpl;

        /// <summary>
        /// Deletes this object.
        /// <para>
        /// This function must be used instead of destructor, which is private.
        /// There is no reference counting involved.
        /// </para>
        /// </summary>
        public void Release()
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            // Copy is needed because otherwise we would call destructor and invalidate the structure with callbacks before using it to free memory.
            ALLOCATION_CALLBACKS allocationCallbacksCopy = *m_Pimpl->GetAllocs();
            D3D12MA_DELETE(&allocationCallbacksCopy, ref this);
        }

        /// <summary>Returns cached options retrieved from D3D12 device.</summary>
        /// <returns>The cached options retrieved from D3D12 device.</returns>
        [return: NativeTypeName("const D3D12_FEATURE_DATA_D3D12_OPTIONS&")]
        public readonly D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options()
        {
            return m_Pimpl->GetD3D12Options();
        }

        /// <summary>
        /// Allocates memory and creates a D3D12 resource (buffer or texture). This is the main allocation function.
        /// <para>
        /// The function is similar to <see cref="ID3D12Device.CreateCommittedResource"/>, but it may
        /// really call <see cref="ID3D12Device.CreatePlacedResource"/> to assign part of a larger,
        /// existing memory heap to the new resource, which is the main purpose of this whole library.
        /// </para>
        /// <para>
        /// If <paramref name="ppvResource"/> is null, you receive only <paramref name="ppAllocation"/> object from this function.
        /// It holds pointer to <see cref="ID3D12Resource"/> that can be queried using function <see cref="Allocation.GetResource"/>.
        /// Reference count of the resource object is 1. It is automatically destroyed when you destroy the allocation object.
        /// </para>
        /// <para>
        /// If <paramref name="ppvResource"/> is not null, you receive pointer to the resource next to allocation object. Reference count of the resource object is
        /// then increased by calling <see cref="IUnknown.QueryInterface"/>, so you need to manually <see cref="IUnknown.Release"/> it along with the allocation.
        /// </para>
        /// </summary>
        /// <param name="pAllocDesc">Parameters of the allocation.</param>
        /// <param name="pResourceDesc">Description of created resource.</param>
        /// <param name="InitialResourceState">Initial resource state.</param>
        /// <param name="pOptimizedClearValue">Optional. Either null or optimized clear value.</param>
        /// <param name="ppAllocation">Filled with pointer to new allocation object created.</param>
        /// <param name="riidResource">IID of a resource to be returned via <paramref name="ppvResource"/>.</param>
        /// <param name="ppvResource">Optional. If not null, filled with pointer to new resouce created.</param>
        /// <remarks>
        /// This function creates a new resource. Sub-allocation of parts of one large buffer,
        /// although recommended as a good practice, is out of scope of this library and could be implemented
        /// by the user as a higher-level logic on top of it, e.g. using the virtual_allocator feature.
        /// </remarks>
        [return: NativeTypeName("HRESULT")]
        public int CreateResource([NativeTypeName("const ALLOCATION_DESC*")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateResource."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Pimpl->CreateResource(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, ppAllocation, riidResource, ppvResource);
        }

        /// <summary>
        /// Similar to <see cref="CreateResource"/>, but supports additional parameter <paramref name="pProtectedSession"/>.
        /// <para>If <paramref name="pProtectedSession"/> is not null, current implementation always creates the resource as committed using <see cref="ID3D12Device4.CreateCommittedResource1"/>.</para>
        /// <para>To work correctly, <see cref="ID3D12Device4"/> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE"/></para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public int CreateResource1([NativeTypeName("const ALLOCATION_DESC*")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession* pProtectedSession, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateResource1."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Pimpl->CreateResource1(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, pProtectedSession, ppAllocation, riidResource, ppvResource);
        }

        /// <summary>
        /// Similar to <see cref="CreateResource1"/>, but supports new structure <see cref="D3D12_RESOURCE_DESC1"/>.
        /// <para>It internally uses <see cref="ID3D12Device8.CreateCommittedResource2"/> or <see cref="ID3D12Device8.CreatePlacedResource1"/>.</para>
        /// <para>To work correctly, <see cref="ID3D12Device8"/> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE"/> is returned.</para>
        /// </summary>
        /// <param name="pAllocDesc"></param>
        /// <param name="pResourceDesc"></param>
        /// <param name="InitialResourceState"></param>
        /// <param name="pOptimizedClearValue"></param>
        /// <param name="pProtectedSession"></param>
        /// <param name="ppAllocation"></param>
        /// <param name="riidResource"></param>
        /// <param name="ppvResource"></param>
        [return: NativeTypeName("HRESULT")]
        public int CreateResource2([NativeTypeName("const ALLOCATION_DESC*")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1*")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession* pProtectedSession, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateResource2."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Pimpl->CreateResource2(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, pProtectedSession, ppAllocation, riidResource, ppvResource);
        }

        /// <summary>
        /// Allocates memory without creating any resource placed in it.
        /// <para>This function is similar to <see cref="ID3D12Device.CreateHeap"/>, but it may really assign part of a larger, existing heap to the allocation.</para>
        /// </summary>
        /// <param name="pAllocDesc">
        /// <c>pAllocDesc->heapFlags</c> should contain one of these values, depending on type of resources you are going to create in this memory:
        /// <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS"/>,
        /// <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES"/>,
        /// <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES"/>.
        /// Except if you validate that <c>ResourceHeapTier = 2</c> - then <c>heapFlags</c>
        /// may be <see cref="D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES"/> <c>= 0</c>.
        /// Additional flags in <c>heapFlags</c> are allowed as well.
        /// </param>
        /// <param name="pAllocInfo"><c>pAllocInfo->SizeInBytes</c> must be multiply of 64KB.</param>
        /// <param name="ppAllocation"><c>pAllocInfo->Alignment</c> must be one of the legal values as described in documentation of <see cref="D3D12_HEAP_DESC"/>.</param>
        /// <remarks>If you use <see cref="ALLOCATION_FLAG_COMMITTED"/> you will get a separate memory block - a heap that always has offset 0.</remarks>
        [return: NativeTypeName("HRESULT")]
        public int AllocateMemory([NativeTypeName("const ALLOCATION_DESC*")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO*")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, Allocation** ppAllocation)
        {
            if (!ValidateAllocateMemoryParameters(pAllocDesc, pAllocInfo, ppAllocation))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::AllocateMemory."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Pimpl->AllocateMemory(pAllocDesc, pAllocInfo, ppAllocation);
        }

        /// <summary>
        /// Similar to <see cref="AllocateMemory"/>, but supports additional parameter <paramref name="pProtectedSession"/>.
        /// <para>If <paramref name="pProtectedSession"/> is not null, current implementation always creates separate heap using <see cref="ID3D12Device4.CreateHeap1"/>.</para>
        /// <para>To work correctly, <see cref="ID3D12Device4"/> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE"/> is returned.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public int AllocateMemory1([NativeTypeName("const ALLOCATION_DESC*")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO*")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, ID3D12ProtectedResourceSession* pProtectedSession, Allocation** ppAllocation)
        {
            if (!ValidateAllocateMemoryParameters(pAllocDesc, pAllocInfo, ppAllocation))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::AllocateMemory1."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Pimpl->AllocateMemory1(pAllocDesc, pAllocInfo, pProtectedSession, ppAllocation);
        }

        /// <summary>Creates a new resource in place of an existing allocation. This is useful for memory aliasing.</summary>
        /// <param name="pAllocation">
        /// Existing allocation indicating the memory where the new resource should be created.
        /// It can be created using <see cref="CreateResource"/> and already have a resource bound to it,
        /// or can be a raw memory allocated with <see cref="AllocateMemory"/>.
        /// It must not be created as committed so that <see cref="ID3D12Heap"/> is available and not implicit.
        /// </param>
        /// <param name="AllocationLocalOffset">
        /// Additional offset in bytes to be applied when allocating the resource.
        /// Local from the start of <paramref name="pAllocation"/>, not the beginning of the whole <see cref="ID3D12Heap"/>!
        /// If the new resource should start from the beginning of the <paramref name="pAllocation"/> it should be 0.
        /// </param>
        /// <param name="pResourceDesc">Description of the new resource to be created.</param>
        /// <param name="InitialResourceState"></param>
        /// <param name="pOptimizedClearValue"></param>
        /// <param name="riidResource"></param>
        /// <param name="ppvResource">
        /// Returns pointer to the new resource.
        /// The resource is not bound with <paramref name="pAllocation"/>.
        /// This pointer must not be null - you must get the resource pointer and `Release` it when no longer needed.
        /// </param>
        /// <remarks>
        /// Memory requirements of the new resource are checked for validation.
        /// If its size exceeds the end of <paramref name="pAllocation"/> or required alignment is not fulfilled
        /// considering <c>pAllocation->GetOffset() + AllocationLocalOffset</c>, the function
        /// returns <see cref="E_INVALIDARG"/>.
        /// </remarks>
        [return: NativeTypeName("HRESULT")]
        public int CreateAliasingResource(Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocation == null ) || (pResourceDesc == null) || (ppvResource == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateAliasingResource."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Pimpl->CreateAliasingResource(pAllocation, AllocationLocalOffset, pResourceDesc, InitialResourceState, pOptimizedClearValue, riidResource, ppvResource);
        }

        /// <summary>Creates custom pool.</summary>
        [return: NativeTypeName("HRESULT")]
        public int CreatePool([NativeTypeName("const POOL_DESC*")] POOL_DESC* pPoolDesc, Pool** ppPool)
        {
            if ((pPoolDesc == null) || (ppPool == null) || !IsHeapTypeValid(pPoolDesc->HeapType) || ((pPoolDesc->MaxBlockCount > 0) && (pPoolDesc->MaxBlockCount < pPoolDesc->MinBlockCount)))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreatePool."
                return E_INVALIDARG;
            }

            if (!m_Pimpl->HeapFlagsFulfillResourceHeapTier(pPoolDesc->HeapFlags))
            {
                D3D12MA_ASSERT(false); // "Invalid pPoolDesc->HeapFlags passed to Allocator::CreatePool. Did you forget to handle ResourceHeapTier=1?"
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            *ppPool = D3D12MA_NEW<Pool>(m_Pimpl->GetAllocs());
            **ppPool = new Pool((Allocator*)Unsafe.AsPointer(ref this), pPoolDesc);
            HRESULT hr = (*ppPool)->m_Pimpl->Init();

            if (SUCCEEDED(hr))
            {
                m_Pimpl->RegisterPool(*ppPool, pPoolDesc->HeapType);
            }
            else
            {
                D3D12MA_DELETE(m_Pimpl->GetAllocs(), *ppPool);
                *ppPool = null;
            }

            return hr;
        }

        /// <summary>Sets the minimum number of bytes that should always be allocated (reserved) in a specific default pool.</summary>
        /// <param name="heapType">Must be one of: <see cref="D3D12_HEAP_TYPE_DEFAULT"/>, <see cref="D3D12_HEAP_TYPE_UPLOAD"/>, <see cref="D3D12_HEAP_TYPE_READBACK"/>.</param>
        /// <param name="heapFlags">
        /// Must be one of: <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS"/>, <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES"/>,
        /// <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES"/>. If <c>ResourceHeapTier = 2</c>, it can also be <see cref="D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES"/>.
        /// </param>
        /// <param name="minBytes">Minimum number of bytes to keep allocated.</param>
        /// <remarks>See also: reserving_memory.</remarks>
        [return: NativeTypeName("HRESULT")]
        public int SetDefaultHeapMinBytes(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong minBytes)
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Pimpl->SetDefaultHeapMinBytes(heapType, heapFlags, minBytes);
        }

        /// <summary>
        /// Sets the index of the current frame.
        /// <para>This function is used to set the frame index in the allocator when a new game frame begins.</para>
        /// </summary>
        public void SetCurrentFrameIndex([NativeTypeName("UINT")] uint frameIndex)
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            m_Pimpl->SetCurrentFrameIndex(frameIndex);
        }

        /// <summary>Retrieves statistics from the current state of the allocator.</summary>
        public void CalculateStats(Stats* pStats)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pStats != null));
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            m_Pimpl->CalculateStats(pStats);
        }

        /// <summary>Retrieves information about current memory budget.</summary>
        /// <param name="pGpuBudget">Optional, can be null.</param>
        /// <param name="pCpuBudget">Optional, can be null.</param>
        /// <remarks>
        /// This function is called "get" not "calculate" because it is very fast, suitable to be called
        /// every frame or every allocation.For more detailed statistics use <see cref="CalculateStats"/>.
        /// <para>Note that when using allocator from multiple threads, returned information may immediately become outdated.</para>
        /// </remarks>
        public void GetBudget(Budget* pGpuBudget, Budget* pCpuBudget)
        {
            if ((pGpuBudget == null) && (pCpuBudget == null))
            {
                return;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            m_Pimpl->GetBudget(pGpuBudget, pCpuBudget);
        }

        /// <summary>Builds and returns statistics as a string in JSON format.</summary>
        /// <param name="ppStatsString">Must be freed using <see cref="FreeStatsString"/>.</param>
        /// <param name="DetailedMap"><see langword="true"/> to include full list of allocations (can make the string quite long), <see langword="false"/> to only return statistics.</param>
        public void BuildStatsString([NativeTypeName("WCHAR**")] ushort** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (ppStatsString != null));
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            m_Pimpl->BuildStatsString(ppStatsString, DetailedMap);
        }

        /// <summary>Frees memory of a string returned from <see cref="BuildStatsString"/>.</summary>
        public void FreeStatsString([NativeTypeName("WCHAR*")] ushort* pStatsString)
        {
            if (pStatsString != null)
            {
                using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
                m_Pimpl->FreeStatsString(pStatsString);
            }
        }

        internal Allocator([NativeTypeName("const ALLOCATION_CALLBACKS&")] ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("const ALLOCATOR_DESC&")] ALLOCATOR_DESC* desc)
        {
            m_Pimpl = D3D12MA_NEW<AllocatorPimpl>(allocationCallbacks);
            m_Pimpl->Ctor(allocationCallbacks, desc);
        }

        void IDisposable.Dispose()
        {
            D3D12MA_DELETE(m_Pimpl->GetAllocs(), m_Pimpl);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ValidateAllocateMemoryParameters(ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, Allocation** ppAllocation)
        {
            return (pAllocDesc != null)
                && (pAllocInfo != null)
                && (ppAllocation != null)
                && ((pAllocInfo->Alignment == 0) || (pAllocInfo->Alignment == D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT) || (pAllocInfo->Alignment == D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT))
                && (pAllocInfo->SizeInBytes != 0)
                && ((pAllocInfo->SizeInBytes % (64UL * 1024)) == 0);
        }
    }
}
