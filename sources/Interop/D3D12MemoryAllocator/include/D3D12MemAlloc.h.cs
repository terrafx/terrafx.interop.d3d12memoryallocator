// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

using UINT = System.UInt32;
using uint64_t = System.UInt64;
using size_t = nuint;
using BOOL = System.Int32;

namespace TerraFX.Interop
{
    public static partial class D3D12MemoryAllocator
    {
        /*
        When defined to value other than 0, the library will try to use
        D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT or D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT
        for created textures when possible, which can save memory because some small textures
        may get their alignment 4K and their size a multiply of 4K instead of 64K.

        #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 0
            Disables small texture alignment.
        #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 1
            Enables conservative algorithm that will use small alignment only for some textures
            that are surely known to support it.
        #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 2
            Enables query for small alignment to D3D12 (based on Microsoft sample) which will
            enable small alignment for more textures, but will also generate D3D Debug Layer
            error #721 on call to ID3D12Device::GetResourceAllocationInfo, which you should just
            ignore.
        */
        public static readonly int D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT = 1;
    }

    public static unsafe partial class D3D12MemoryAllocator
    {
        /// <summary>Number of D3D12 memory heap types supported.</summary>
        public const UINT HEAP_TYPE_COUNT = 3;

        /// <summary>
        /// Creates new main D3D12MA::Allocator object and returns it through `ppAllocator`.
        /// <para>You normally only need to call it once and keep a single Allocator object for your `ID3D12Device`.</para>
        /// </summary>
        public static HRESULT CreateAllocator(ALLOCATOR_DESC* pDesc, Allocator** ppAllocator);

        /// <summary>
        /// Creates new D3D12MA::VirtualBlock object and returns it through `ppVirtualBlock`.
        /// <para>Note you don't need to create D3D12MA::Allocator to use virtual blocks.</para>
        /// </summary>
        public static HRESULT CreateVirtualBlock(VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);
    }

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
    public partial unsafe struct Allocator
    {
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
        public partial D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options();

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
        public partial HRESULT CreateResource(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Allocation** ppAllocation,
            Guid* riidResource,
            void** ppvResource);

        /// <summary>
        /// Similar to Allocator::CreateResource, but supports additional parameter `pProtectedSession`.
        /// <para>
        /// If `pProtectedSession` is not null, current implementation always creates the resource as committed
        /// using `ID3D12Device4::CreateCommittedResource1`.
        /// </para>
        /// <para>To work correctly, `ID3D12Device4` interface must be available in the current system. Otherwise, `E_NOINTERFACE`</para>
        /// </summary>
        public partial HRESULT CreateResource1(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
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
        public partial HRESULT CreateResource2(
            ALLOCATION_DESC* pAllocDesc,
            D3D12_RESOURCE_DESC1* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            ID3D12ProtectedResourceSession* pProtectedSession,
            Allocation** ppAllocation,
            Guid* riidResource,
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
        public partial HRESULT AllocateMemory(
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
        public partial HRESULT AllocateMemory1(
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
        public partial HRESULT CreateAliasingResource(
            Allocation* pAllocation,
            uint64_t AllocationLocalOffset,
            D3D12_RESOURCE_DESC* pResourceDesc,
            D3D12_RESOURCE_STATES InitialResourceState,
            D3D12_CLEAR_VALUE* pOptimizedClearValue,
            Guid* riidResource,
            void** ppvResource);

        /// <summary>Creates custom pool.</summary>
        public partial HRESULT CreatePool(
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
        public partial HRESULT SetDefaultHeapMinBytes(
            D3D12_HEAP_TYPE heapType,
            D3D12_HEAP_FLAGS heapFlags,
            uint64_t minBytes);

        /// <summary>
        /// Sets the index of the current frame.
        /// <para>This function is used to set the frame index in the allocator when a new game frame begins.</para>
        /// </summary>
        public partial void SetCurrentFrameIndex(UINT frameIndex);

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
        public partial void BuildStatsString(char** ppStatsString, BOOL DetailedMap);

        /// <summary>Frees memory of a string returned from Allocator::BuildStatsString.</summary>
        public partial void FreeStatsString(char* pStatsString);

        internal long CreateAllocator(ALLOCATION_DESC* desc, Allocator** pResult);
        internal partial void D3D12MA_DELETE<T>(ALLOCATION_CALLBACKS* callbacks, T* data)
            where T : unmanaged;

        internal AllocatorPimpl* m_Pimpl;
    }

    /// <summary>
    /// Represents pure allocation algorithm and a data structure with allocations in some memory block, without actually allocating any GPU memory.
    /// <para>
    /// This class allows to use the core algorithm of the library custom allocations e.g. CPU memory or
    /// sub-allocation regions inside a single GPU buffer.
    /// </para>
    /// <para>
    /// To create this object, fill in D3D12MA::VIRTUAL_BLOCK_DESC and call CreateVirtualBlock().
    /// To destroy it, call its method VirtualBlock::Release().
    /// </para>
    /// </summary>
    public unsafe struct VirtualBlock
    {
        /// <summary>
        /// Destroys this object and frees it from memory.
        /// <para>You need to free all the allocations within this block or call Clear() before destroying it.</para>
        /// </summary>
        public partial void Release();

        /// <summary>Returns true if the block is empty - contains 0 allocations.</summary>
        public partial BOOL IsEmpty();

        /// <summary>Returns information about an allocation at given offset - its size and custom pointer.</summary>
        public partial void GetAllocationInfo(uint64_t offset, VIRTUAL_ALLOCATION_INFO* pInfo);

        /// <summary>Creates new allocation.</summary>
        /// <param name="pOffset">Offset of the new allocation, which can also be treated as an unique identifier of the allocation within this block. `UINT64_MAX` if allocation failed.</param>
        /// <returns>`S_OK` if allocation succeeded, `E_OUTOFMEMORY` if it failed.</returns>
        public partial HRESULT Allocate(VIRTUAL_ALLOCATION_DESC* pDesc, uint64_t* pOffset);

        /// <summary>Frees the allocation at given offset.</summary>
        public partial void FreeAllocation(uint64_t offset);

        /// <summary>Frees all the allocations.</summary>
        public partial void Clear();

        /// <summary>Changes custom pointer for an allocation at given offset to a new value.</summary>
        public partial void SetAllocationUserData(uint64_t offset, void* pUserData);

        /// <summary>Retrieves statistics from the current state of the block.</summary>
        public partial void CalculateStats(StatInfo* pInfo);

        /// <summary>Builds and returns statistics as a string in JSON format, including the list of allocations with their parameters.</summary>
        /// <param name="ppStatsString">Must be freed using VirtualBlock::FreeStatsString.</param>
        public partial void BuildStatsString(char** ppStatsString);

        /// <summary>Frees memory of a string returned from VirtualBlock::BuildStatsString.</summary>
        public partial void FreeStatsString(char* pStatsString);
    }
}
