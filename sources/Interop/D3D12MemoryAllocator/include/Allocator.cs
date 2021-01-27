// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.Windows;

#pragma warning disable CS1573

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents main object of this library initialized for particular `ID3D12Device`.
    /// <para>
    /// Fill structure D3D12MA::ALLOCATOR_DESC and call function CreateAllocator() to create it.
    /// Call method Allocator::Release to destroy it.
    /// </para>
    /// <para>
    /// It is recommended to create just one object of this type per `ID3D12Device` object,
    /// right after Direct3D 12 is initialized and keep it alive until before Direct3D device is destroyed.
    /// </para>
    /// </summary>
    public unsafe partial struct Allocator : IDisposable
    {
        internal AllocatorPimpl* m_Pimpl;

        /// <summary>
        /// Deletes this object.
        /// <para>
        /// This function must be used instead of destructor, which is private.
        /// There is no reference counting involved.
        /// </para>
        /// </summary>
        public partial void Release();

        /// <summary>
        /// Returns cached options retrieved from D3D12 device.
        /// </summary>
        /// <returns>The cached options retrieved from D3D12 device.</returns>
        public readonly partial D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options();

        /// <summary>
        /// Allocates memory and creates a D3D12 resource (buffer or texture). This is the main allocation function.
        /// <para>
        /// The function is similar to `ID3D12Device::CreateCommittedResource`, but it may
        /// really call `ID3D12Device::CreatePlacedResource` to assign part of a larger,
        /// existing memory heap to the new resource, which is the main purpose of this
        /// whole library.
        /// </para>
        /// <para>
        /// If `ppvResource` is null, you receive only `ppAllocation` object from this function.
        /// It holds pointer to `ID3D12Resource` that can be queried using function D3D12MA::Allocation::GetResource().
        /// Reference count of the resource object is 1.
        /// It is automatically destroyed when you destroy the allocation object.
        /// </para>
        /// <para>
        /// If `ppvResource` is not null, you receive pointer to the resource next to allocation object.
        /// Reference count of the resource object is then increased by calling `QueryInterface`, so you need to manually `Release` it
        /// along with the allocation.
        /// </para>
        /// </summary>
        /// <param name="pAllocDesc">Parameters of the allocation.</param>
        /// <param name="pResourceDesc">Description of created resource.</param>
        /// <param name="InitialResourceState">Initial resource state.</param>
        /// <param name="pOptimizedClearValue">Optional. Either null or optimized clear value.</param>
        /// <param name="ppAllocation">Filled with pointer to new allocation object created.</param>
        /// <param name="riidResource">IID of a resource to be returned via `ppvResource`.</param>
        /// <param name="ppvResource">Optional. If not null, filled with pointer to new resouce created.</param>
        /// <remarks>
        /// This function creates a new resource. Sub-allocation of parts of one large buffer,
        /// although recommended as a good practice, is out of scope of this library and could be implemented
        /// by the user as a higher-level logic on top of it, e.g. using the \ref virtual_allocator feature.
        /// </remarks>
        [return: NativeTypeName("HRESULT")]
        public partial int CreateResource(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Allocation** ppAllocation,
            [NativeTypeName("REFIID")] Guid* riidResource,
            void** ppvResource);

        /// <summary>
        /// Similar to Allocator::CreateResource, but supports additional parameter `pProtectedSession`.
        /// <para>
        /// If `pProtectedSession` is not null, current implementation always creates the resource as committed
        /// using `ID3D12Device4::CreateCommittedResource1`.
        /// </para>
        /// <para>To work correctly, `ID3D12Device4` interface must be available in the current system. Otherwise, `E_NOINTERFACE`</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public partial int CreateResource1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation,
            [NativeTypeName("REFIID")] Guid* riidResource,
            void** ppvResource);

        /// <summary>
        /// Similar to Allocator::CreateResource1, but supports new structure `D3D12_RESOURCE_DESC1`.
        /// <para>It internally uses `ID3D12Device8::CreateCommittedResource2` or `ID3D12Device8::CreatePlacedResource1`.</para>
        /// <para>To work correctly, `ID3D12Device8` interface must be available in the current system. Otherwise, `E_NOINTERFACE` is returned.</para>
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
        public partial int CreateResource2(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC1* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation,
            [NativeTypeName("REFIID")] Guid* riidResource,
            void** ppvResource);

        /// <summary>
        /// Allocates memory without creating any resource placed in it.
        /// <para>
        /// This function is similar to `ID3D12Device::CreateHeap`, but it may really assign
        /// part of a larger, existing heap to the allocation.
        /// </para>
        /// </summary>
        /// <param name="pAllocDesc">
        /// `pAllocDesc->heapFlags` should contain one of these values, depending on type of resources you are going to create in this memory:
        /// `D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS`,
        /// `D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES`,
        /// `D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES`.
        /// Except if you validate that ResourceHeapTier = 2 - then `heapFlags`
        /// may be `D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES` = 0.
        /// Additional flags in `heapFlags` are allowed as well.
        /// </param>
        /// <param name="pAllocInfo">`pAllocInfo->SizeInBytes` must be multiply of 64KB.</param>
        /// <param name="ppAllocation">`pAllocInfo->Alignment` must be one of the legal values as described in documentation of `D3D12_HEAP_DESC`.</param>
        /// <remarks>
        /// If you use D3D12MA::ALLOCATION_FLAG_COMMITTED you will get a separate memory block -
        /// a heap that always has offset 0.
        /// </remarks>
        [return: NativeTypeName("HRESULT")]
        public partial int AllocateMemory(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo,
            Allocation** ppAllocation);

        /// <summary>
        /// Similar to Allocator::AllocateMemory, but supports additional parameter `pProtectedSession`.
        /// <para>
        /// If `pProtectedSession` is not null, current implementation always creates separate heap
        /// using `ID3D12Device4::CreateHeap1`.
        /// </para>
        /// <para>To work correctly, `ID3D12Device4` interface must be available in the current system. Otherwise, `E_NOINTERFACE` is returned.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public partial int AllocateMemory1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation);

        /// <summary>
        /// Creates a new resource in place of an existing allocation. This is useful for memory aliasing.
        /// </summary>
        /// <param name="pAllocation">
        /// Existing allocation indicating the memory where the new resource should be created.
        /// It can be created using D3D12MA::Allocator::CreateResource and already have a resource bound to it,
        /// or can be a raw memory allocated with D3D12MA::Allocator::AllocateMemory.
        /// It must not be created as committed so that `ID3D12Heap` is available and not implicit.
        /// </param>
        /// <param name="AllocationLocalOffset">
        /// Additional offset in bytes to be applied when allocating the resource.
        /// Local from the start of `pAllocation`, not the beginning of the whole `ID3D12Heap`!
        /// If the new resource should start from the beginning of the `pAllocation` it should be 0.
        /// </param>
        /// <param name="pResourceDesc">Description of the new resource to be created.</param>
        /// <param name="ppvResource">
        /// Returns pointer to the new resource.
        /// The resource is not bound with `pAllocation`.
        /// This pointer must not be null - you must get the resource pointer and `Release` it when no longer needed.
        /// </param>
        /// <remarks>
        /// Memory requirements of the new resource are checked for validation.
        /// If its size exceeds the end of `pAllocation` or required alignment is not fulfilled
        /// considering `pAllocation->GetOffset() + AllocationLocalOffset`, the function
        /// returns `E_INVALIDARG`.
        /// </remarks>
        [return: NativeTypeName("HRESULT")]
        public partial int CreateAliasingResource(
            Allocation* pAllocation,
            [NativeTypeName("UINT64")] ulong AllocationLocalOffset,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            [NativeTypeName("REFIID")] Guid* riidResource,
            void** ppvResource);

        /// <summary>Creates custom pool.</summary>
        [return: NativeTypeName("HRESULT")]
        public partial int CreatePool(
            POOL_DESC* pPoolDesc,
            Pool** ppPool);

        /// <summary>Sets the minimum number of bytes that should always be allocated (reserved) in a specific default pool.</summary>
        /// <param name="heapType">Must be one of: `D3D12_HEAP_TYPE_DEFAULT`, `D3D12_HEAP_TYPE_UPLOAD`, `D3D12_HEAP_TYPE_READBACK`.</param>
        /// <param name="heapFlags">
        /// Must be one of: `D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS`, `D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES`,
        /// `D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES`. If ResourceHeapTier = 2, it can also be `D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES`.
        /// </param>
        /// <param name="minBytes">Minimum number of bytes to keep allocated.</param>
        /// <remarks>See also: \subpage reserving_memory.</remarks>
        [return: NativeTypeName("HRESULT")]
        public partial int SetDefaultHeapMinBytes(
            D3D12_HEAP_TYPE heapType,
            D3D12_HEAP_FLAGS heapFlags,
            [NativeTypeName("UINT64")] ulong minBytes);

        /// <summary>
        /// Sets the index of the current frame.
        /// <para>This function is used to set the frame index in the allocator when a new game frame begins.</para>
        /// </summary>
        public partial void SetCurrentFrameIndex([NativeTypeName("UINT")] uint frameIndex);

        /// <summary>Retrieves statistics from the current state of the allocator.</summary>
        public partial void CalculateStats(Stats* pStats);

        /// <summary>Retrieves information about current memory budget.</summary>
        /// <param name="pGpuBudget">Optional, can be null.</param>
        /// <param name="pCpuBudget">Optional, can be null.</param>
        /// <remarks>
        /// This function is called "get" not "calculate" because it is very fast, suitable to be called
        /// every frame or every allocation.For more detailed statistics use CalculateStats().
        /// <para>
        /// Note that when using allocator from multiple threads, returned information may immediately
        /// become outdated.
        /// </para>
        /// </remarks>
        public partial void GetBudget(Budget* pGpuBudget, Budget* pCpuBudget);

        /// <summary>
        /// Builds and returns statistics as a string in JSON format.
        /// </summary>
        /// <param name="ppStatsString">Must be freed using Allocator::FreeStatsString.</param>
        /// <param name="DetailedMap">`TRUE` to include full list of allocations (can make the string quite long), `FALSE` to only return statistics.</param>
        public partial void BuildStatsString([NativeTypeName("WCHAR**")] ushort** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap);

        /// <summary>Frees memory of a string returned from Allocator::BuildStatsString.</summary>
        public partial void FreeStatsString([NativeTypeName("WCHAR*")] ushort* pStatsString);

        internal Allocator(ALLOCATION_CALLBACKS* allocationCallbacks, ALLOCATOR_DESC* desc)
        {
            m_Pimpl = D3D12MA_NEW<AllocatorPimpl>(allocationCallbacks);
            *m_Pimpl = new(allocationCallbacks, desc);
        }

        public partial void Dispose();
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Public class Allocator implementation

    public unsafe partial struct Allocator
    {
        public partial void Dispose()
        {
            D3D12MA_DELETE(m_Pimpl->GetAllocs(), m_Pimpl);
        }

        public partial void Release()
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            // Copy is needed because otherwise we would call destructor and invalidate the structure with callbacks before using it to free memory.
            ALLOCATION_CALLBACKS allocationCallbacksCopy = *m_Pimpl->GetAllocs();
            D3D12MA_DELETE(&allocationCallbacksCopy, (Allocator*)Unsafe.AsPointer(ref this));
        }

        public readonly partial D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options()
        {
            return m_Pimpl->GetD3D12Options();
        }

        public partial int CreateResource(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource)
        {
            if (pAllocDesc == null || pResourceDesc == null || ppAllocation == null)
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            return m_Pimpl->CreateResource(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, ppAllocation, riidResource, ppvResource);
        }

        public partial int CreateResource1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource)
        {
            if (pAllocDesc == null || pResourceDesc == null || ppAllocation == null)
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            return m_Pimpl->CreateResource1(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, pProtectedSession, ppAllocation, riidResource, ppvResource);
        }

        public partial int CreateResource2(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC1* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource)
        {
            if (pAllocDesc == null || pResourceDesc == null || ppAllocation == null)
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            return m_Pimpl->CreateResource2(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, pProtectedSession, ppAllocation, riidResource, ppvResource);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ValidateAllocateMemoryParameters(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo,
            Allocation** ppAllocation)
        {
            return pAllocDesc != null &&
                pAllocInfo != null &&
                ppAllocation != null &&
                (pAllocInfo->Alignment == 0 ||
                    pAllocInfo->Alignment == D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT ||
                    pAllocInfo->Alignment == D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT) &&
                pAllocInfo->SizeInBytes != 0 &&
                pAllocInfo->SizeInBytes % (64UL * 1024) == 0;
        }

        public partial int AllocateMemory(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo,
            Allocation** ppAllocation)
        {
            if (!ValidateAllocateMemoryParameters(pAllocDesc, pAllocInfo, ppAllocation))
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            return m_Pimpl->AllocateMemory(pAllocDesc, pAllocInfo, ppAllocation);
        }

        public partial int AllocateMemory1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation)
        {
            if (!ValidateAllocateMemoryParameters(pAllocDesc, pAllocInfo, ppAllocation))
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            return m_Pimpl->AllocateMemory1(pAllocDesc, pAllocInfo, pProtectedSession, ppAllocation);
        }

        public partial int CreateAliasingResource(
            Allocation* pAllocation,
            ulong AllocationLocalOffset,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Guid* riidResource,
            void** ppvResource)
        {
            if (pAllocation == null || pResourceDesc == null || ppvResource == null)
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            return m_Pimpl->CreateAliasingResource(pAllocation, AllocationLocalOffset, pResourceDesc, InitialResourceState, pOptimizedClearValue, riidResource, ppvResource);
        }

        public partial int CreatePool(
            POOL_DESC* pPoolDesc,
            Pool** ppPool)
        {
            if (pPoolDesc == null || ppPool == null ||
                !IsHeapTypeValid(pPoolDesc->HeapType) ||
                (pPoolDesc->MaxBlockCount > 0 && pPoolDesc->MaxBlockCount < pPoolDesc->MinBlockCount))
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            if (!m_Pimpl->HeapFlagsFulfillResourceHeapTier(pPoolDesc->HeapFlags))
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            *ppPool = D3D12MA_NEW<Pool>(m_Pimpl->GetAllocs());
            **ppPool = new((Allocator*)Unsafe.AsPointer(ref this), pPoolDesc);
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

        public partial int SetDefaultHeapMinBytes(
            D3D12_HEAP_TYPE heapType,
            D3D12_HEAP_FLAGS heapFlags,
            ulong minBytes)
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            return m_Pimpl->SetDefaultHeapMinBytes(heapType, heapFlags, minBytes);
        }

        public partial void SetCurrentFrameIndex(uint frameIndex)
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            m_Pimpl->SetCurrentFrameIndex(frameIndex);
        }

        public partial void CalculateStats(Stats* pStats)
        {
            D3D12MA_ASSERT(pStats != null);
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            m_Pimpl->CalculateStats(pStats);
        }

        public partial void GetBudget(Budget* pGpuBudget, Budget* pCpuBudget)
        {
            if (pGpuBudget == null && pCpuBudget == null)
            {
                return;
            }
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            m_Pimpl->GetBudget(pGpuBudget, pCpuBudget);
        }

        public partial void BuildStatsString(ushort** ppStatsString, int DetailedMap)
        {
            D3D12MA_ASSERT(ppStatsString != null);
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            m_Pimpl->BuildStatsString(ppStatsString, DetailedMap);
        }

        public partial void FreeStatsString(ushort* pStatsString)
        {
            if (pStatsString != null)
            {
                //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
                m_Pimpl->FreeStatsString(pStatsString);
            }
        }
    }
}
