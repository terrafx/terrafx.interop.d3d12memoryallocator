// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_HEAP_TIER;

namespace TerraFX.Interop.DirectX;

public static unsafe partial class D3D12MemAlloc
{
#pragma warning disable CA1802,CA1805
#if DEBUG
    private static readonly bool IsDebug = true;
#else
    private static readonly bool IsDebug = false;
#endif
#pragma warning restore CA1802,CA1805

    /// <summary>Define this macro to 0 to disable usage of DXGI 1.4 (needed for <see cref="IDXGIAdapter3"/> and query for memory budget).</summary>
    public static readonly uint D3D12MA_DXGI_1_4 = get_app_context_data(nameof(D3D12MA_DXGI_1_4), 1);

    /// <summary>When defined to value other than 0, the library will try to use <see cref="D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT"/> or <see cref="D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT"/> for created textures when possible, which can save memory because some small textures may get their alignment 4K and their size a multiply of 4K instead of 64K.</summary>
    /// <remarks>
    ///   <code>
    ///   #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 0
    ///       Disables small texture alignment.
    ///   #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 1
    ///       Enables conservative algorithm that will use small alignment only for some textures
    ///       that are surely known to support it.
    ///   #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 2
    ///       Enables query for small alignment to D3D12 (based on Microsoft sample) which will
    ///       enable small alignment for more textures, but will also generate D3D Debug Layer
    ///       error #721 on call to ID3D12Device::GetResourceAllocationInfo, which you should just
    ///       ignore.
    ///   </code>
    /// </remarks>
    public static readonly uint D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT = get_app_context_data(nameof(D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT), 1);

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
    public static readonly uint D3D12MA_DEBUG_LEVEL = get_app_context_data(nameof(D3D12MA_DEBUG_LEVEL), IsDebug ? 1u : 0u);

    /// <summary>Minimum alignment of all allocations, in bytes. Set to more than 1 for debugging purposes only.Must be power of two.</summary>
    public static readonly ulong D3D12MA_DEBUG_ALIGNMENT = get_app_context_data(nameof(D3D12MA_DEBUG_ALIGNMENT), 1UL);

    /// <summary>Minimum margin before and after every allocation, in bytes. Set nonzero for debugging purposes only.</summary>
    public static readonly ulong D3D12MA_DEBUG_MARGIN = get_app_context_data(nameof(D3D12MA_DEBUG_MARGIN), 0UL);

    /// <summary>Set this to 1 for debugging purposes only, to enable single mutex protecting all entry calls to the library. Can be useful for debugging multithreading issues.</summary>
    public static readonly uint D3D12MA_DEBUG_GLOBAL_MUTEX = get_app_context_data(nameof(D3D12MA_DEBUG_GLOBAL_MUTEX), 0);

    /// <summary>Define this macro for debugging purposes only to force specific <see cref="D3D12_RESOURCE_HEAP_TIER" />, especially to test compatibility with <see cref="D3D12_RESOURCE_HEAP_TIER_1" />.on modern GPUs.</summary>
    public static readonly uint D3D12MA_FORCE_RESOURCE_HEAP_TIER = get_app_context_data(nameof(D3D12MA_FORCE_RESOURCE_HEAP_TIER), 0);

    /// <summary>Default size of a block allocated as single <see cref="ID3D12Heap" />.</summary>
    public static readonly ulong D3D12MA_DEFAULT_BLOCK_SIZE = get_app_context_data(nameof(D3D12MA_DEFAULT_BLOCK_SIZE), 64UL * 1024 * 1024);

    /// <summary>Minimum size of a free suballocation to register it in the free suballocation collection.</summary>
    [NativeTypeName("UINT64")]
    public static readonly ulong D3D12MA_MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER = get_app_context_data(nameof(D3D12MA_MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER), 16UL);

    internal static D3D12MA_MUTEX* g_DebugGlobalMutex = InitDebugGlobalMutex();
}
