// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    /// <summary>Statistics of current memory usage and available budget, in bytes, for GPU or CPU memory.</summary>
    public struct D3D12MA_Budget
    {
        /// <summary>Sum size of all memory blocks allocated from particular heap type, in bytes.</summary>
        [NativeTypeName("UINT64")]
        public ulong BlockBytes;

        /// <summary>
        /// Sum size of all allocations created in particular heap type, in bytes.
        /// <para>
        /// Always less or equal than <see cref="BlockBytes"/>. Difference <c>BlockBytes - AllocationBytes</c> is
        /// the amount of memory allocated but unused - available for new allocations or wasted due to fragmentation.
        /// </para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong AllocationBytes;

        /// <summary>
        /// Estimated current memory usage of the program, in bytes.
        /// <para>Fetched from system using <see cref="IDXGIAdapter3.QueryVideoMemoryInfo"/> if enabled.</para>
        /// <para>
        /// It might be different than <see cref="BlockBytes"/> (usually higher) due to additional implicit objects
        /// also occupying the memory, like swapchain, pipeline state objects, descriptor heaps, command lists, or
        /// memory blocks allocated outside of this library, if any.
        /// </para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong UsageBytes;

        /// <summary>
        /// Estimated amount of memory available to the program, in bytes.
        /// <para>Fetched from system using <see cref="IDXGIAdapter3.QueryVideoMemoryInfo"/> if enabled.</para>
        /// <para>
        /// It might be different (most probably smaller) than memory sizes reported in <see cref="DXGI_ADAPTER_DESC"/> due to factors
        /// external to the program, like other programs also consuming system resources.
        /// Difference <c>BudgetBytes - UsageBytes</c> is the amount of additional memory that can probably
        /// be allocated without problems. Exceeding the budget may result in various problems.
        /// </para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong BudgetBytes;
    }
}
