// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;

namespace TerraFX.Interop.DirectX;

/// <summary>Calculated statistics of memory usage e.g. in a specific memory heap type, memory segment group, custom pool, or total.</summary>
/// <remarks>These are fast to calculate. See functions: <see cref="D3D12MA_Allocator.GetBudget" />, <see cref="D3D12MA_Pool.GetStatistics" />.</remarks>
public partial struct D3D12MA_Statistics : IEquatable<D3D12MA_Statistics>
{
    public static bool operator ==(in D3D12MA_Statistics lhs, in D3D12MA_Statistics rhs)
    {
        return (lhs.BlockCount == rhs.BlockCount) &&
               (lhs.AllocationCount == rhs.AllocationCount) &&
               (lhs.BlockBytes == rhs.BlockBytes) &&
               (lhs.AllocationBytes == rhs.AllocationBytes);
    }

    public static bool operator !=(in D3D12MA_Statistics lhs, in D3D12MA_Statistics rhs) => !(lhs == rhs);

    public override bool Equals([NotNullWhen(true)] object? obj) => (obj is D3D12MA_Statistics other) && Equals(other);

    public bool Equals(D3D12MA_Statistics other) => (this == other);

    public override int GetHashCode() => HashCode.Combine(BlockCount, AllocationCount, BlockBytes, AllocationBytes);
}
