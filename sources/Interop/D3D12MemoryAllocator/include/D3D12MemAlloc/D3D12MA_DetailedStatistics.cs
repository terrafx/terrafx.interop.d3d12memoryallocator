// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>More detailed statistics than <see cref="D3D12MA_Statistics" />.</summary>
/// <remarks>
///   <para>These are slower to calculate. Use for debugging purposes. See functions: <see cref="D3D12MA_Allocator.CalculateStatistics" />, <see cref="D3D12MA_Pool.CalculateStatistics" />.</para>
///   <para>Averages are not provided because they can be easily calculated as:</para>
///   <code>
///     ulong allocationSizeAvg = detailedStats.Statistics.AllocationBytes / detailedStats.Statistics.AllocationCount;
///     ulong unusedBytes = detailedStats.Statistics.BlockBytes - detailedStats.Statistics.AllocationBytes;
///     ulong unusedRangeSizeAvg = unusedBytes / detailedStats.UnusedRangeCount;
///   </code>
/// </remarks>
public partial struct D3D12MA_DetailedStatistics
{
    /// <summary>Basic statistics.</summary>
    public D3D12MA_Statistics Stats;

    /// <summary>Number of free ranges of memory between allocations.</summary>
    [NativeTypeName("UINT")]
    public uint UnusedRangeCount;

    /// <summary>Smallest allocation size. <see cref="ulong.MaxValue" /> if there are 0 allocations.</summary>
    [NativeTypeName("UINT64")]
    public ulong AllocationSizeMin;

    /// <summary>Largest allocation size. 0 if there are 0 allocations.</summary>
    [NativeTypeName("UINT64")]
    public ulong AllocationSizeMax;

    /// <summary>Smallest empty range size. <see cref="ulong.MaxValue" /> if there are 0 empty ranges.</summary>
    [NativeTypeName("UINT64")]
    public ulong UnusedRangeSizeMin;

    /// <summary>Largest empty range size. 0 if there are 0 empty ranges.</summary>
    [NativeTypeName("UINT64")]
    public ulong UnusedRangeSizeMax;
}
