// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Calculated statistics of memory usage e.g. in a specific memory heap type, memory segment group, custom pool, or total.</summary>
/// <remarks>These are fast to calculate. See functions: <see cref="D3D12MA_Allocator.GetBudget" />, <see cref="D3D12MA_Pool.GetStatistics" />.</remarks>
public partial struct D3D12MA_Statistics
{
    /// <summary>Number of D3D12 memory blocks allocated - <see cref="ID3D12Heap" /> objects and committed resources.</summary>
    [NativeTypeName("UINT")]
    public uint BlockCount;

    /// <summary>Number of <see cref="D3D12MA_Allocation" /> objects allocated.</summary>
    /// <remarks>Committed allocations have their own blocks, so each one adds 1 to <see cref="AllocationCount" /> as well as <see cref="BlockCount" />.</remarks>
    [NativeTypeName("UINT")]
    public uint AllocationCount;

    /// <summary>Number of bytes allocated in memory blocks.</summary>
    [NativeTypeName("UINT64")]
    public ulong BlockBytes;

    /// <summary>Total number of bytes occupied by all <see cref="D3D12MA_Allocation" /> objects.</summary>
    /// <remarks>Always less or equal than <see cref="BlockBytes" />. Difference <c>(BlockBytes - AllocationBytes)</c> is the amount of memory allocated from D3D12 but unused by any <see cref="D3D12MA_Allocation" />.</remarks>
    [NativeTypeName("UINT64")]
    public ulong AllocationBytes;
}
