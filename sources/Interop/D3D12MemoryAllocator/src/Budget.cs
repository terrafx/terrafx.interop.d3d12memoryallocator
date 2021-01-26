// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>Statistics of current memory usage and available budget, in bytes, for GPU or CPU memory.</summary>
    public struct Budget
    {
        /// <summary>Sum size of all memory blocks allocated from particular heap type, in bytes.</summary>
        [NativeTypeName("UINT64")] public ulong BlockBytes;

        /// <summary>
        /// Sum size of all allocations created in particular heap type, in bytes.
        /// <para>
        /// Always less or equal than `BlockBytes`.
        /// Difference `BlockBytes - AllocationBytes` is the amount of memory allocated but unused -
        /// available for new allocations or wasted due to fragmentation.
        /// </para>
        /// </summary>
        [NativeTypeName("UINT64")] public ulong AllocationBytes;

        /// <summary>
        /// Estimated current memory usage of the program, in bytes.
        /// <para>Fetched from system using `IDXGIAdapter3::QueryVideoMemoryInfo` if enabled.</para>
        /// <para>
        /// It might be different than `BlockBytes` (usually higher) due to additional implicit objects
        /// also occupying the memory, like swapchain, pipeline state objects, descriptor heaps, command lists, or
        /// memory blocks allocated outside of this library, if any.
        /// </para>
        /// </summary>
        [NativeTypeName("UINT64")] public ulong UsageBytes;

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
        [NativeTypeName("UINT64")] public ulong BudgetBytes;
    }
}
