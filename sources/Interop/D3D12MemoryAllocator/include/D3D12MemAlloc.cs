// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemoryAllocator
    {
#if DEBUG
        private const bool IsDebug = true;
#else
        private const bool IsDebug = false;
#endif

        /// <summary>
        /// When defined to value other than 0, the library will execute debug assertions throughout
        /// the codebase. There are two different levels available, with different performance impacts.
        /// By default, RELEASE build of the library have all assertions disabled.
        /// <code>
        /// #define D3D12MA_DEBUG_LEVEL 0
        ///     All assertions are disabled.
        /// #define D3D12MA_DEBUG_LEVEL 1
        ///     Enable basic assertions.
        /// #define D3D12MA_DEBUG_LEVEL 2
        ///     Enable all assertions, including those in hot paths. This guarantees maximum assertion
        ///     coverage, but it might have a noticeable impact on performance when using the library.
        /// </code>
        /// </summary>
        public static readonly uint D3D12MA_DEBUG_LEVEL = get_app_context_data(nameof(D3D12MA_DEBUG_LEVEL), IsDebug ? 2 : 0);

        /// <summary>
        /// When defined to value other than 0, the library will try to use
        /// D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT or D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT
        /// for created textures when possible, which can save memory because some small textures
        /// may get their alignment 4K and their size a multiply of 4K instead of 64K.
        /// <code>
        /// #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 0
        ///     Disables small texture alignment.
        /// #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 1
        ///     Enables conservative algorithm that will use small alignment only for some textures
        ///     that are surely known to support it.
        /// #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 2
        ///     Enables query for small alignment to D3D12 (based on Microsoft sample) which will
        ///     enable small alignment for more textures, but will also generate D3D Debug Layer
        ///     error #721 on call to ID3D12Device::GetResourceAllocationInfo, which you should just
        ///     ignore.
        /// </code>
        /// </summary>
        public static readonly uint D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT = get_app_context_data(nameof(D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT), 1);

        /// <summary>
        /// Minimum alignment of all allocations, in bytes.
        /// Set to more than 1 for debugging purposes only.Must be power of two.
        /// </summary>
        public static readonly uint D3D12MA_DEBUG_ALIGNMENT = get_app_context_data(nameof(D3D12MA_DEBUG_ALIGNMENT), 1);

        /// <summary>
        /// Minimum margin before and after every allocation, in bytes.
        /// Set nonzero for debugging purposes only.
        /// </summary>
        public static readonly uint D3D12MA_DEBUG_MARGIN = get_app_context_data(nameof(D3D12MA_DEBUG_MARGIN), 0);

        /// <summary>
        /// Set this to 1 for debugging purposes only, to enable single mutex protecting all
        /// entry calls to the library.Can be useful for debugging multithreading issues.
        /// </summary>
        public static readonly uint D3D12MA_DEBUG_GLOBAL_MUTEX = get_app_context_data(nameof(D3D12MA_DEBUG_GLOBAL_MUTEX), 0);

        /// <summary>Define this macro to 0 to disable usage of DXGI 1.4 (needed for IDXGIAdapter3 and query for memory budget).</summary>
        public static readonly uint D3D12MA_DXGI_1_4 = get_app_context_data(nameof(D3D12MA_DXGI_1_4), 1);

        /// <summary>Number of D3D12 memory heap types supported.</summary>
        [NativeTypeName("UINT")] internal const uint HEAP_TYPE_COUNT = 3;

        /// <summary>Default size of a block allocated as single ID3D12Heap.</summary>
        internal const ulong D3D12MA_DEFAULT_BLOCK_SIZE = (256UL * 1024 * 1024);

        /// <summary>Minimum size of a free suballocation to register it in the free suballocation collection.</summary>
        internal const ulong MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER = 16;

        /// <summary>
        /// Creates new main D3D12MA::Allocator object and returns it through `ppAllocator`.
        /// <para>You normally only need to call it once and keep a single Allocator object for your `ID3D12Device`.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateAllocator(ALLOCATOR_DESC* pDesc, Allocator** ppAllocator);

        /// <summary>
        /// Creates new D3D12MA::VirtualBlock object and returns it through `ppVirtualBlock`.
        /// <para>Note you don't need to create D3D12MA::Allocator to use virtual blocks.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateVirtualBlock(VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);
    }
}
