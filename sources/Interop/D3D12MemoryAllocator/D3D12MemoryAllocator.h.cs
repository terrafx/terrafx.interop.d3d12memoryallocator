// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

using UINT = System.UInt32;
using uint64_t = System.UInt64;
using size_t = nint;
using BOOL = System.Int32;

namespace TerraFX.Interop.D3D12MA
{
    /// <summary>Custom callbacks to CPU memory allocation functions.</summary>
    public unsafe struct ALLOCATION_CALLBACKS
    {
        /// <summary>Allocation function. The parameters are the size, alignment and `pUserData`.</summary>
        public delegate*<size_t, size_t, void*, void*> pAllocate;

        /// <summary>Dellocation function. The parameters are `pMemory` and `pUserData`.</summary>
        public delegate*<void*, void*, void> pFree;

        /// <summary>Custom data that will be passed to allocation and deallocation functions as `pUserData` parameter.</summary>
        public void* pUserData;
    }

    /// <summary>Bit flags to be used with ALLOCATION_DESC::Flags.</summary>
    public enum ALLOCATION_FLAGS
    {
        /// <summary>Zero.</summary>
        ALLOCATION_FLAG_NONE = 0,

        /// <summary>
        /// Set this flag if the allocation should have its own dedicated memory allocation (committed resource with implicit heap).
        /// Use it for special, big resources, like fullscreen textures used as render targets.
        /// </summary>
        ALLOCATION_FLAG_COMMITTED = 0x1,

        /// <summary>
        /// Set this flag to only try to allocate from existing memory heaps and never create new such heap.
        /// <para>If new allocation cannot be placed in any of the existing heaps, allocation fails with `E_OUTOFMEMORY` error.</para>
        /// <para>You should not use D3D12MA::ALLOCATION_FLAG_COMMITTED and D3D12MA::ALLOCATION_FLAG_NEVER_ALLOCATE at the same time. It makes no sense.</para>
        /// </summary>
        ALLOCATION_FLAG_NEVER_ALLOCATE = 0x2,

        /// <summary>Create allocation only if additional memory required for it, if any, won't exceed memory budget. Otherwise return `E_OUTOFMEMORY`.</summary>
        ALLOCATION_FLAG_WITHIN_BUDGET = 0x4,
    }

    /// <summary>Parameters of created D3D12MA::Allocation object. To be used with Allocator::CreateResource.</summary>
    public unsafe struct ALLOCATION_DESC
    {
        /// <summary>Flags.</summary>
        public ALLOCATION_FLAGS Flags;

        /// <summary>
        /// The type of memory heap where the new allocation should be placed.
        /// <para>It must be one of: `D3D12_HEAP_TYPE_DEFAULT`, `D3D12_HEAP_TYPE_UPLOAD`, `D3D12_HEAP_TYPE_READBACK`.</para>
        /// <para>When D3D12MA::ALLOCATION_DESC::CustomPool != NULL this member is ignored.</para>
        /// </summary>
        public D3D12_HEAP_TYPE HeapType;

        /// <summary>
        /// Additional heap flags to be used when allocating memory.
        /// <para>In most cases it can be 0.</para>
        /// <para>
        /// - If you use D3D12MA::Allocator::CreateResource(), you don't need to care.
        /// Necessary flag `D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS`, `D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES`, or `D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES` is added automatically.
        /// </para>
        /// <para>
        /// - If you use D3D12MA::Allocator::AllocateMemory(), you should specify one of those `ALLOW_ONLY` flags.
        /// Except when you validate that D3D12MA::Allocator::GetD3D12Options()`.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER_1` - then you can leave it 0.
        /// </para>
        /// <para>
        /// - You can specify additional flags if needed. Then the memory will always be allocated as separate block using `D3D12Device::CreateCommittedResource` or `CreateHeap`, not as part of an existing larget block.
        /// </para>
        /// <para>When D3D12MA::ALLOCATION_DESC::CustomPool != NULL this member is ignored.</para>
        /// </summary>
        public D3D12_HEAP_FLAGS ExtraHeapFlags;

        /// <summary>
        /// Custom pool to place the new resource in. Optional.
        /// <para>When not NULL, the resource will be created inside specified custom pool. It will then never be created as committed.</para>
        /// </summary>
        void* CustomPool; // TODO
    }

    /// <summary>
    /// Represents single memory allocation.
    /// <para>It may be either implicit memory heap dedicated to a single resource or a specific region of a bigger heap plus unique offset.</para>
    /// <para>To create such object, fill structure D3D12MA::ALLOCATION_DESC and call function Allocator::CreateResource.</para>
    /// <para>The object remembers size and some other information. To retrieve this information, use methods of this class.</para>
    /// <para>The object also remembers `ID3D12Resource` and "owns" a reference to it, so it calls `Release()` on the resource when destroyed.</para>
    /// </summary>
    public unsafe partial struct Allocation : IDisposable
    {
        public void Dispose() { }

        /// <summary>
        /// Deletes this object.
        /// <para>This function must be used instead of destructor, which is private. There is no reference counting involved.</para>
        /// </summary>
        public partial void Release();

        /// <summary>
        /// Returns offset in bytes from the start of memory heap.
        /// <para>
        /// You usually don't need to use this offset. If you create a buffer or a texture together with the allocation using function
        /// D3D12MA::Allocator::CreateResource, functions that operate on that resource refer to the beginning of the resource, not entire memory heap.
        /// </para>
        /// </summary>
        /// <returns>If the Allocation represents committed resource with implicit heap, returns 0.</returns>
        public partial uint64_t GetOffset();

        /// <summary>
        /// Returns size in bytes of the allocation.
        /// <para>- If you created a buffer or a texture together with the allocation using function D3D12MA::Allocator::CreateResource, this is the size of the resource returned by `ID3D12Device::GetResourceAllocationInfo`.</para>
        /// <para>- For allocations made out of bigger memory blocks, this also is the size of the memory region assigned exclusively to this allocation.</para>
        /// <para>
        /// - For resources created as committed, this value may not be accurate. DirectX implementation may optimize memory usage internally so that you may
        /// even observe regions of `ID3D12Resource::GetGPUVirtualAddress()` + Allocation::GetSize() to overlap in memory and still work correctly.
        /// </para>
        /// </summary>
        /// <returns>The size in bytes of the allocation.</returns>
        public uint64_t GetSize() { return m_Size; }

        /// <summary>
        /// Returns D3D12 resource associated with this object.
        /// <para>Calling this method doesn't increment resource's reference counter.</para>
        /// </summary>
        /// <returns>The D3D12 resource.</returns>
        public ID3D12Resource* GetResource() { return m_Resource; }

        /// <summary>
        /// Returns memory heap that the resource is created in.
        /// <para>If the Allocation represents committed resource with implicit heap, returns NULL.</para>
        /// </summary>
        /// <returns>The memory heap that the resource is created in.</returns>
        public partial ID3D12Heap* GetHeap();

        /// <summary>
        /// Associates a name with the allocation object. This name is for use in debug diagnostics and tools.
        /// <para>Internal copy of the string is made, so the memory pointed by the argument can be changed of freed immediately after this call.</para>
        /// </summary>
        /// <param name="Name">`Name` can be null.</param>
        public partial void SetName(char* Name);

        /// <summary>
        /// Returns the name associated with the allocation object.
        /// <para>Returned string points to an internal copy.</para>
        /// <para>If no name was associated with the allocation, returns null.</para>
        /// </summary>
        /// <returns>The name associated with the allocation object.</returns>
        public char* GetName() { return m_Name; }

        /// <summary>
        /// Returns `TRUE` if the memory of the allocation was filled with zeros when the allocation was created.
        /// <para>Returns `TRUE` only if the allocator is sure that the entire memory where the allocation was created was filled with zeros at the moment the allocation was made.</para>
        /// <para>
        /// Returns `FALSE` if the memory could potentially contain garbage data.
        /// If it's a render-target or depth-stencil texture, it then needs proper initialization with `ClearRenderTargetView`, `ClearDepthStencilView`, `DiscardResource`,
        /// or a copy operation, as described on page: [ID3D12Device::CreatePlacedResource method - Notes on the required resource initialization] (https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-createplacedresource#notes-on-the-required-resource-initialization).
        /// Please note that rendering a fullscreen triangle or quad to the texture as a render target is not a proper way of initialization!
        /// </para>
        /// <para>
        /// See also articles:
        /// ["Coming to DirectX 12: More control over memory allocation"] (https://devblogs.microsoft.com/directx/coming-to-directx-12-more-control-over-memory-allocation/),
        /// ["Initializing DX12 Textures After Allocation and Aliasing"] (https://asawicki.info/news_1724_initializing_dx12_textures_after_allocation_and_aliasing).
        /// </para>
        /// </summary>
        /// <returns>Whether the memory of the allocation was filled with zeros.</returns>
        public BOOL WasZeroInitialized() { return m_PackedData.WasZeroInitialized(); }

        enum Type
        {
            TYPE_COMMITTED,
            TYPE_PLACED,
            TYPE_HEAP,
            TYPE_COUNT
        }

        void* m_Allocator;
        uint64_t m_Size;
        ID3D12Resource* m_Resource;
        UINT m_CreationFrameIndex;
        char* m_Name;

        [StructLayout(LayoutKind.Explicit)]
        struct _Anonymous_e__Union
        {
            [FieldOffset(0)] public m_Committed_t m_Committed;
            [FieldOffset(0)] public m_Placed_t m_Placed;
            [FieldOffset(0)] public m_Heap_t m_Heap;

            public struct m_Committed_t
            {
                public D3D12_HEAP_TYPE heapType;
            }

            public struct m_Placed_t
            {
                uint64_t offset;
                void* block;
            }

            public struct m_Heap_t
            {
                D3D12_HEAP_TYPE heapType;
                ID3D12Heap* heap;
            }
        }

        _Anonymous_e__Union m_Union;

        partial struct PackedData
        {
            public new Type GetType() { return (Type)m_Type; }
            public D3D12_RESOURCE_DIMENSION GetResourceDimension() { return (D3D12_RESOURCE_DIMENSION)m_ResourceDimension; }
            public D3D12_RESOURCE_FLAGS GetResourceFlags() { return (D3D12_RESOURCE_FLAGS)m_ResourceFlags; }
            public D3D12_TEXTURE_LAYOUT GetTextureLayout() { return (D3D12_TEXTURE_LAYOUT)m_TextureLayout; }
            public BOOL WasZeroInitialized() { return (BOOL)m_WasZeroInitialized; }

            public partial void SetType(Type type);
            public partial void SetResourceDimension(D3D12_RESOURCE_DIMENSION resourceDimension);
            public partial void SetResourceFlags(D3D12_RESOURCE_FLAGS resourceFlags);
            public partial void SetTextureLayout(D3D12_TEXTURE_LAYOUT textureLayout);
            void SetWasZeroInitialized(BOOL wasZeroInitialized) { m_WasZeroInitialized = wasZeroInitialized > 0 ? 1 : 0; }

            uint64_t __value;

            UINT m_Type
            {
                get => (UINT)BitHelper.ExtractRange(__value, 0, 2);
                set => __value = BitHelper.SetRange(__value, 0, 2, value);
            }

            UINT m_ResourceDimension
            {
                get => (UINT)BitHelper.ExtractRange(__value, 2, 3);
                set => __value = BitHelper.SetRange(__value, 2, 3, value);
            }

            UINT m_ResourceFlags
            {
                get => (UINT)BitHelper.ExtractRange(__value, 5, 24);
                set => __value = BitHelper.SetRange(__value, 5, 24, value);
            }

            UINT m_TextureLayout
            {
                get => (UINT)BitHelper.ExtractRange(__value, 29, 9);
                set => __value = BitHelper.SetRange(__value, 29, 9, value);
            }

            UINT m_WasZeroInitialized
            {
                get => (UINT)BitHelper.ExtractRange(__value, 38, 1);
                set => __value = BitHelper.SetRange(__value, 38, 1, value);
            }
        }

        PackedData m_PackedData;

        // TODO
    }

    /// <summary>Parameters of created D3D12MA::Pool object. To be used with D3D12MA::Allocator::CreatePool.</summary>
    public struct POOL_DESC
    {
        /// <summary>
        /// The type of memory heap where allocations of this pool should be placed.
        /// <para>It must be one of: `D3D12_HEAP_TYPE_DEFAULT`, `D3D12_HEAP_TYPE_UPLOAD`, `D3D12_HEAP_TYPE_READBACK`.</para>
        /// </summary>
        public D3D12_HEAP_TYPE HeapType;

        /// <summary>
        /// Heap flags to be used when allocating heaps of this pool.
        /// <para>
        /// It should contain one of these values, depending on type of resources you are going to create in this heap:
        /// `D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS`,
        /// `D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES`,
        /// `D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES`.
        /// Except if ResourceHeapTier = 2, then it may be `D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES` = 0.
        /// </para>
        /// <para>You can specify additional flags if needed.</para>
        /// </summary>
        public D3D12_HEAP_FLAGS HeapFlags;

        /// <summary>
        /// Size of a single heap (memory block) to be allocated as part of this pool, in bytes. Optional.
        /// <para>
        /// Specify nonzero to set explicit, constant size of memory blocks used by this pool.
        /// Leave 0 to use default and let the library manage block sizes automatically.
        /// Then sizes of particular blocks may vary.
        /// </para>
        /// </summary>
        public uint64_t BlockSize;

        /// <summary>
        /// Minimum number of heaps (memory blocks) to be always allocated in this pool, even if they stay empty. Optional.
        /// <para>Set to 0 to have no preallocated blocks and allow the pool be completely empty.</para>
        /// </summary>
        public UINT MinBlockCount;

        /// <summary>
        /// Maximum number of heaps (memory blocks) that can be allocated in this pool. Optional.
        /// <para>Set to 0 to use default, which is `UINT64_MAX`, which means no limit.</para>
        /// <para>Set to same value as D3D12MA::POOL_DESC::MinBlockCount to have fixed amount of memory allocated throughout whole lifetime of this pool.</para>
        /// </summary>
        public UINT MaxBlockCount;
    }

    /// <summary>Bit flags to be used with ALLOCATOR_DESC::Flags.</summary>
    public enum ALLOCATOR_FLAGS
    {
        /// <summary>Zero</summary>
        ALLOCATOR_FLAG_NONE = 0,

        /// <summary>
        /// Allocator and all objects created from it will not be synchronized internally, so you
        /// must guarantee they are used from only one thread at a time or synchronized by you.
        /// <para>
        ///  Using this flag may increase performance because internal mutexes are not used.
        /// </para>
        /// </summary>
        ALLOCATOR_FLAG_SINGLETHREADED = 0x1,

        /// <summary>Every allocation will have its own memory block. To be used for debugging purposes.</summary>
        ALLOCATOR_FLAG_ALWAYS_COMMITTED = 0x2,
    }

    /// <summary>Parameters of created Allocator object. To be used with CreateAllocator().</summary>
    public unsafe struct ALLOCATOR_DESC
    {
        /// <summary>Flags</summary>
        public ALLOCATOR_FLAGS Flags;

        /// <summary>
        /// Direct3D device object that the allocator should be attached to.
        /// <para>Allocator is doing `AddRef`/`Release` on this object.</para>
        /// </summary>
        public ID3D12Device* pDevice;

        /// <summary>
        /// Preferred size of a single `ID3D12Heap` block to be allocated.
        /// <para>Set to 0 to use default, which is currently 256 MiB.</para>
        /// </summary>
        public uint64_t PreferredBlockSize;

        /// <summary>
        /// Custom CPU memory allocation callbacks. Optional.
        /// <para>Optional, can be null. When specified, will be used for all CPU-side memory allocations.</para>
        /// </summary>
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;

        /// <summary>
        /// DXGI Adapter object that you use for D3D12 and this allocator.
        /// <para>Allocator is doing `AddRef`/`Release` on this object.</para>
        /// </summary>
        public IDXGIAdapter* pAdapter;
    }

    public static unsafe partial class D3D12MemAllocH
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

    /// <summary>Calculated statistics of memory usage in entire allocator.</summary>
    public struct StatInfo
    {
        /// <summary>Number of memory blocks (heaps) allocated.</summary>
        public UINT BlockCount;

        /// <summary>Number of D3D12MA::Allocation objects allocated.</summary>
        public UINT AllocationCount;

        /// <summary>Number of free ranges of memory between allocations.</summary>
        public UINT UnusedRangeCount;

        /// <summary>Total number of bytes occupied by all allocations.</summary>
        public uint64_t UsedBytes;

        /// <summary>Total number of bytes occupied by unused ranges.</summary>
        public uint64_t UnusedBytes;
        public uint64_t AllocationSizeMin;
        public uint64_t AllocationSizeAvg;
        public uint64_t AllocationSizeMax;
        public uint64_t UnusedRangeSizeMin;
        public uint64_t UnusedRangeSizeAvg;
        public uint64_t UnusedRangeSizeMax;
    }

    /// <summary>General statistics from the current state of the allocator.</summary>
    public struct Stats
    {
        /// <summary>Total statistics from all heap types.</summary>
        public StatInfo Total;

        /// <summary>
        /// One StatInfo for each type of heap located at the following indices:
        /// 0 - DEFAULT, 1 - UPLOAD, 2 - READBACK.
        /// </summary>
        public __Stats_Values HeapType;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __Stats_Values
        {
            private StatInfo _HeapType0;
            private StatInfo _HeapType1;
            private StatInfo _HeapType2;

            public unsafe ref StatInfo this[int index]
            {
                get
                {
                    fixed (__Stats_Values* p = &this)
                    {
                        switch (index)
                        {
                            case 0:
                                return ref p->_HeapType0;
                            case 1:
                                return ref p->_HeapType1;
                            case 2:
                                return ref p->_HeapType2;
                            default:
                                return ref Unsafe.NullRef<StatInfo>();
                        }
                    }
                }
            }
        }
    }

    /// <summary>Statistics of current memory usage and available budget, in bytes, for GPU or CPU memory.</summary>
    public struct Budget
    {
        /// <summary>Sum size of all memory blocks allocated from particular heap type, in bytes.</summary>
        public uint64_t BlockBytes;

        /// <summary>
        /// Sum size of all allocations created in particular heap type, in bytes.
        /// <para>
        /// Always less or equal than `BlockBytes`.
        /// Difference `BlockBytes - AllocationBytes` is the amount of memory allocated but unused -
        /// available for new allocations or wasted due to fragmentation.
        /// </para>
        /// </summary>
        public uint64_t AllocationBytes;

        /// <summary>
        /// Estimated current memory usage of the program, in bytes.
        /// <para>Fetched from system using `IDXGIAdapter3::QueryVideoMemoryInfo` if enabled.</para>
        /// <para>
        /// It might be different than `BlockBytes` (usually higher) due to additional implicit objects
        /// also occupying the memory, like swapchain, pipeline state objects, descriptor heaps, command lists, or
        /// memory blocks allocated outside of this library, if any.
        /// </para>
        /// </summary>
        public uint64_t UsageBytes;

        /// <summary>
        /// Estimated amount of memory available to the program, in bytes.
        /// <para>Fetched from system using `IDXGIAdapter3::QueryVideoMemoryInfo` if enabled.</para>
        /// <para>
        /// It might be different (most probably smaller) than memory sizes reported in `DXGI_ADAPTER_DESC` due to factors
        /// external to the program, like other programs also consuming system resources.
        /// Difference `BudgetBytes - UsageBytes` is the amount of additional memory that can probably
        /// be allocated without problems. Exceeding the budget may result in various problems.
        /// </para>
        /// </summary>
        public uint64_t BudgetBytes;
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
    }

    /// <summary>Parameters of created D3D12MA::VirtualBlock object to be passed to CreateVirtualBlock().</summary>
    public unsafe struct VIRTUAL_BLOCK_DESC
    {
        /// <summary>
        /// Total size of the block.
        /// <para>
        /// Sizes can be expressed in bytes or any units you want as long as you are consistent in using them.
        /// For example, if you allocate from some array of structures, 1 can mean single instance of entire structure.
        /// </para>
        /// </summary>
        public uint64_t Size;

        /// <summary>
        /// Custom CPU memory allocation callbacks. Optional.
        /// <para>Optional, can be null. When specified, will be used for all CPU-side memory allocations.</para>
        /// </summary>
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;
    }

    /// <summary>Parameters of created virtual allocation to be passed to VirtualBlock::Allocate().</summary>
    public unsafe struct VIRTUAL_ALLOCATION_DESC
    {
        /// <summary>
        /// Size of the allocation.
        /// <para>Cannot be zero.</para>
        /// </summary>
        public uint64_t Size;

        /// <summary>
        /// Required alignment of the allocation.
        /// <para>Must be power of two. Special value 0 has the same meaning as 1 - means no special alignment is required, so allocation can start at any offset.</para>
        /// </summary>
        public uint64_t Alignment;

        /// <summary>
        /// Custom pointer to be associated with the allocation.
        /// <para>It can be fetched or changed later.</para>
        /// </summary>
        public void* pUserData;
    }

    /// <summary>Parameters of an existing virtual allocation, returned by VirtualBlock::GetAllocationInfo().</summary>
    public unsafe struct VIRTUAL_ALLOCATION_INFO
    {
        /// <summary>
        /// Size of the allocation.
        /// <para>Same value as passed in VIRTUAL_ALLOCATION_DESC::Size.</para>
        /// </summary>
        public uint64_t Size;

        /// <summary>
        /// Custom pointer associated with the allocation.
        /// <para>Same value as passed in VIRTUAL_ALLOCATION_DESC::pUserData or VirtualBlock::SetAllocationUserData().</para>
        /// </summary>
        public void* pUserData;
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

    /// <summary>
    /// Helpers to perform bit operations on numeric types.
    /// Ported from Microsoft.Toolkit.HighPerformance:
    /// <a href="https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/master/Microsoft.Toolkit.HighPerformance/Helpers/BitHelper.cs"/>
    /// </summary>
    internal static class BitHelper
    {
        /// <summary>
        /// Extracts a bit field range from a given value.
        /// </summary>
        /// <param name="value">The input <see cref="uint64_t"/> value.</param>
        /// <param name="start">The initial index of the range to extract (in [0, 63] range).</param>
        /// <param name="length">The length of the range to extract (depends on <paramref name="start"/>).</param>
        /// <returns>The value of the extracted range within <paramref name="value"/>.</returns>
        /// <remarks>
        /// This method doesn't validate <paramref name="start"/> and <paramref name="length"/>.
        /// If either parameter is not valid, the result will just be inconsistent. The method
        /// should not be used to set all the bits at once, and it is not guaranteed to work in
        /// that case, which would just be equivalent to assigning the <see cref="uint64_t"/> value.
        /// Additionally, no conditional branches are used to retrieve the range.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint64_t ExtractRange(uint64_t value, byte start, byte length)
        {
            if (Bmi1.X64.IsSupported)
            {
                return Bmi1.X64.BitFieldExtract(value, start, length);
            }

            return (value >> start) & ((1ul << length) - 1ul);
        }

        /// <summary>
        /// Sets a bit field range within a target value.
        /// </summary>
        /// <param name="value">The initial <see cref="uint64_t"/> value.</param>
        /// <param name="start">The initial index of the range to extract (in [0, 63] range).</param>
        /// <param name="length">The length of the range to extract (depends on <paramref name="start"/>).</param>
        /// <param name="flags">The input flags to insert in the target range.</param>
        /// <returns>The updated bit field value after setting the specified range.</returns>
        /// <remarks>
        /// Just like <see cref="ExtractRange(uint64_t,byte,byte)"/>, this method doesn't validate the parameters
        /// and does not contain branching instructions, so it's well suited for use in tight loops as well.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint64_t SetRange(uint64_t value, byte start, byte length, uint64_t flags)
        {
            uint64_t
                highBits = (1ul << length) - 1ul,
                loadMask = highBits << start,
                storeMask = (flags & highBits) << start;

            if (Bmi1.X64.IsSupported)
            {
                return Bmi1.X64.AndNot(loadMask, value) | storeMask;
            }

            return (~loadMask & value) | storeMask;
        }
    }
}
