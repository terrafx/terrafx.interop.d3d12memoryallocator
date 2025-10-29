// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Statistics of current memory usage and available budget for a specific memory segment group.</summary>
/// <remarks>These are fast to calculate. See function <see cref="D3D12MA_Allocator.GetBudget" />.</remarks>
public partial struct D3D12MA_Budget
{
    /// <summary>Statistics fetched from the library.</summary>
    public D3D12MA_Statistics Stats;

    /// <summary>Estimated current memory usage of the program.</summary>
    /// <remarks>
    ///   <para>Fetched from system using <see cref="IDXGIAdapter3.QueryVideoMemoryInfo" /> if possible.</para>
    ///   <para>It might be different than <c>BlockBytes</c> (usually higher) due to additional implicit objects also occupying the memory, like swapchain, pipeline state objects, descriptor heaps, command lists, or heaps and resources allocated outside of this library, if any.</para>
    /// </remarks>
    [NativeTypeName("UINT64")]
    public ulong UsageBytes;

    /// <summary>Estimated amount of memory available to the program.</summary>
    /// <remarks>
    ///   <para>Fetched from system using <see cref="IDXGIAdapter3.QueryVideoMemoryInfo" /> if possible.</para>
    ///   <para>It might be different (most probably smaller) than memory capacity returned by <see cref="D3D12MA_Allocator.GetMemoryCapacity" /> due to factors external to the program, decided by the operating system. Difference <c>BudgetBytes - UsageBytes</c> is the amount of additional memory that can probably be allocated without problems.Exceeding the budget may result in various problems.</para>
    /// </remarks>
    [NativeTypeName("UINT64")]
    public ulong BudgetBytes;
}
