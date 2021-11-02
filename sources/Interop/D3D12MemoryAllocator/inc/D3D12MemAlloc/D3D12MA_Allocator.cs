// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.D3D12MA_ALLOCATION_FLAGS;
using System.Runtime.InteropServices;

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents main object of this library initialized for particular <see cref="ID3D12Device"/>.
    /// <para>
    /// Fill structure <see cref="D3D12MA_ALLOCATOR_DESC"/> and call function <see cref="D3D12MA_CreateAllocator"/> to create it.
    /// Call method <see cref="Release"/> to destroy it.
    /// </para>
    /// <para>
    /// It is recommended to create just one object of this type per <see cref="ID3D12Device"/> object,
    /// right after Direct3D 12 is initialized and keep it alive until before Direct3D device is destroyed.
    /// </para>
    /// </summary>
    public unsafe partial struct D3D12MA_Allocator : IDisposable
    {
        private static readonly void** Vtbl = InitVtbl();

        private static void** InitVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_Allocator), sizeof(void*) * 4);

            /* QueryInterface */ lpVtbl[0] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, Guid*, void**, int>)&D3D12MA_IUnknownImpl.QueryInterface;
            /* AddRef         */ lpVtbl[1] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, uint>)&D3D12MA_IUnknownImpl.AddRef;
            /* Release        */ lpVtbl[2] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, uint>)&D3D12MA_IUnknownImpl.Release;
            /* ReleaseThis    */ lpVtbl[3] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, void>)&ReleaseThis;

            return lpVtbl;
        }

        /// <summary>
        /// Implements <c>IUnknown.Release()</c>.
        /// </summary>
        public uint Release()
        {
            return m_IUnknownImpl.Release();
        }

        /// <summary>
        /// Deletes this object.
        /// <para>
        /// This function must be used instead of destructor, which is private.
        /// There is no reference counting involved.
        /// </para>
        /// </summary>
        private void ReleaseThis()
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            // Copy is needed because otherwise we would call destructor and invalidate the structure with callbacks before using it to free memory.
            D3D12MA_ALLOCATION_CALLBACKS allocationCallbacksCopy = *GetAllocs();
            D3D12MA_DELETE(&allocationCallbacksCopy, ref this);
        }

        [UnmanagedCallersOnly]
        private static void ReleaseThis(D3D12MA_IUnknownImpl* pThis)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pThis->lpVtbl == Vtbl));
            ((D3D12MA_Allocator*)pThis)->ReleaseThis();
        }

        /// <summary>Returns cached options retrieved from D3D12 device.</summary>
        /// <returns>The cached options retrieved from D3D12 device.</returns>
        [return: NativeTypeName("const D3D12_FEATURE_DATA_D3D12_OPTIONS&")]
        public readonly D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options()
        {
            return (D3D12_FEATURE_DATA_D3D12_OPTIONS*)Unsafe.AsPointer(ref Unsafe.AsRef(in m_D3D12Options));
        }

        /// <summary>
        /// Returns true if <see cref="D3D12_FEATURE_DATA_ARCHITECTURE1.UMA"/> was found to be true.
        /// <para>
        /// For more information about how to use it, see articles in Microsoft Docs:
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/direct3d12/default-texture-mapping"/>
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/api/d3d12/ns-d3d12-d3d12_feature_data_architecture"/>
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-getcustomheapproperties"/>
        /// </para>
        /// </summary>
        /// <returns>Whether <see cref="D3D12_FEATURE_DATA_ARCHITECTURE1.UMA"/> was found to be true.</returns>
        [return: NativeTypeName("BOOL")]
        public readonly int IsUMA()
        {
            return m_D3D12Architecture.UMA;
        }

        /// <summary>
        /// Returns true if <see cref="D3D12_FEATURE_DATA_ARCHITECTURE1.CacheCoherentUMA"/> was found to be true.
        /// <para>
        /// For more information about how to use it, see articles in Microsoft Docs:
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/direct3d12/default-texture-mapping"/>
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/api/d3d12/ns-d3d12-d3d12_feature_data_architecture"/>
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-getcustomheapproperties"/>
        /// </para>
        /// </summary>
        /// <returns>Whether <see cref="D3D12_FEATURE_DATA_ARCHITECTURE1.CacheCoherentUMA"/> was found to be true.</returns>
        [return: NativeTypeName("BOOL")]
        public readonly int IsCacheCoherentUMA()
        {
            return m_D3D12Architecture.CacheCoherentUMA;
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
        /// It holds pointer to <see cref="ID3D12Resource"/> that can be queried using function <see cref="D3D12MA_Allocation.GetResource"/>.
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
        public int CreateResource([NativeTypeName("const ALLOCATION_DESC*")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateResource."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return CreateResourcePimpl(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, ppAllocation, riidResource, ppvResource);
        }

        /// <summary>
        /// Similar to <see cref="CreateResource"/>, but supports additional parameter <paramref name="pProtectedSession"/>.
        /// <para>If <paramref name="pProtectedSession"/> is not null, current implementation always creates the resource as committed using <see cref="ID3D12Device4.CreateCommittedResource1"/>.</para>
        /// <para>To work correctly, <see cref="ID3D12Device4"/> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE"/></para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public int CreateResource1([NativeTypeName("const ALLOCATION_DESC*")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession* pProtectedSession, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateResource1."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return CreateResource1Pimpl(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, pProtectedSession, ppAllocation, riidResource, ppvResource);
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
        public int CreateResource2([NativeTypeName("const ALLOCATION_DESC*")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1*")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession* pProtectedSession, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateResource2."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return CreateResource2Pimpl(pAllocDesc, pResourceDesc, InitialResourceState, pOptimizedClearValue, pProtectedSession, ppAllocation, riidResource, ppvResource);
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
        /// <remarks>If you use <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED"/> you will get a separate memory block - a heap that always has offset 0.</remarks>
        [return: NativeTypeName("HRESULT")]
        public int AllocateMemory([NativeTypeName("const ALLOCATION_DESC*")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO*")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation)
        {
            if (!ValidateAllocateMemoryParameters(pAllocDesc, pAllocInfo, ppAllocation))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::AllocateMemory."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return AllocateMemoryPimpl(pAllocDesc, pAllocInfo, ppAllocation);
        }

        /// <summary>
        /// Similar to <see cref="AllocateMemory"/>, but supports additional parameter <paramref name="pProtectedSession"/>.
        /// <para>If <paramref name="pProtectedSession"/> is not null, current implementation always creates separate heap using <see cref="ID3D12Device4.CreateHeap1"/>.</para>
        /// <para>To work correctly, <see cref="ID3D12Device4"/> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE"/> is returned.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public int AllocateMemory1([NativeTypeName("const ALLOCATION_DESC*")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO*")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, ID3D12ProtectedResourceSession* pProtectedSession, D3D12MA_Allocation** ppAllocation)
        {
            if (!ValidateAllocateMemoryParameters(pAllocDesc, pAllocInfo, ppAllocation))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::AllocateMemory1."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return AllocateMemory1Pimpl(pAllocDesc, pAllocInfo, pProtectedSession, ppAllocation);
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
        public int CreateAliasingResource(D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC*")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE*")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            if ((pAllocation == null ) || (pResourceDesc == null) || (ppvResource == null))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreateAliasingResource."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return CreateAliasingResourcePimpl(pAllocation, AllocationLocalOffset, pResourceDesc, InitialResourceState, pOptimizedClearValue, riidResource, ppvResource);
        }

        /// <summary>Creates custom pool.</summary>
        [return: NativeTypeName("HRESULT")]
        public int CreatePool([NativeTypeName("const POOL_DESC*")] D3D12MA_POOL_DESC* pPoolDesc, D3D12MA_Pool** ppPool)
        {
            if ((pPoolDesc == null) || (ppPool == null) ||
                ((pPoolDesc->MaxBlockCount > 0) && (pPoolDesc->MaxBlockCount < pPoolDesc->MinBlockCount)) ||
                ((pPoolDesc->MinAllocationAlignment > 0) && !IsPow2(pPoolDesc->MinAllocationAlignment)))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to Allocator::CreatePool."
                return E_INVALIDARG;
            }

            if (!HeapFlagsFulfillResourceHeapTier(pPoolDesc->HeapFlags))
            {
                D3D12MA_ASSERT(false); // "Invalid pPoolDesc->HeapFlags passed to Allocator::CreatePool. Did you forget to handle ResourceHeapTier=1?"
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            *ppPool = D3D12MA_NEW<D3D12MA_Pool>(GetAllocs());
            D3D12MA_Pool._ctor(ref **ppPool, ref this, pPoolDesc);

            HRESULT hr = (*ppPool)->Init();

            if (SUCCEEDED(hr))
            {
                RegisterPool(*ppPool, pPoolDesc->HeapProperties.Type);
            }
            else
            {
                D3D12MA_DELETE(GetAllocs(), *ppPool);
                *ppPool = null;
            }

            return hr;
        }

        /// <summary>
        /// Sets the index of the current frame.
        /// <para>This function is used to set the frame index in the allocator when a new game frame begins.</para>
        /// </summary>
        public void SetCurrentFrameIndex([NativeTypeName("UINT")] uint frameIndex)
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            SetCurrentFrameIndexPimpl(frameIndex);
        }

        /// <summary>Retrieves statistics from the current state of the allocator.</summary>
        public void CalculateStats(D3D12MA_Stats* pStats)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pStats != null));
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            CalculateStatsPimpl(pStats);
        }

        /// <summary>Retrieves information about current memory budget.</summary>
        /// <param name="pGpuBudget">Optional, can be null.</param>
        /// <param name="pCpuBudget">Optional, can be null.</param>
        /// <remarks>
        /// This function is called "get" not "calculate" because it is very fast, suitable to be called
        /// every frame or every allocation.For more detailed statistics use <see cref="CalculateStats"/>.
        /// <para>Note that when using allocator from multiple threads, returned information may immediately become outdated.</para>
        /// </remarks>
        public void GetBudget(D3D12MA_Budget* pGpuBudget, D3D12MA_Budget* pCpuBudget)
        {
            if ((pGpuBudget == null) && (pCpuBudget == null))
            {
                return;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            GetBudgetPimpl(pGpuBudget, pCpuBudget);
        }

        /// <summary>Builds and returns statistics as a string in JSON format.</summary>
        /// <param name="ppStatsString">Must be freed using <see cref="FreeStatsString"/>.</param>
        /// <param name="DetailedMap"><see langword="true"/> to include full list of allocations (can make the string quite long), <see langword="false"/> to only return statistics.</param>
        public void BuildStatsString([NativeTypeName("WCHAR**")] ushort** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (ppStatsString != null));
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            BuildStatsStringPimpl(ppStatsString, DetailedMap);
        }

        /// <summary>Frees memory of a string returned from <see cref="BuildStatsString"/>.</summary>
        public void FreeStatsString([NativeTypeName("WCHAR*")] ushort* pStatsString)
        {
            if (pStatsString != null)
            {
                using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
                FreeStatsStringPimpl(pStatsString);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ValidateAllocateMemoryParameters(D3D12MA_ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation)
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
