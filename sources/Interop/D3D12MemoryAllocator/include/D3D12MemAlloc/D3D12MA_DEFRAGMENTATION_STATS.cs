// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Statistics returned for defragmentation process by function <see cref="D3D12MA_DefragmentationContext.GetStats" />.</summary>
public unsafe partial struct D3D12MA_DEFRAGMENTATION_STATS
{
    /// <summary>Total number of bytes that have been copied while moving allocations to different places.</summary>
    [NativeTypeName("UINT64")]
    public ulong BytesMoved;

    /// <summary>Total number of bytes that have been released to the system by freeing empty heaps.</summary>
    [NativeTypeName("UINT64")]
    public ulong BytesFreed;

    /// <summary>Number of allocations that have been moved to different places.</summary>
    [NativeTypeName("UINT32")]
    public uint AllocationsMoved;

    /// <summary>Number of empty <see cref="ID3D12Heap" /> objects that have been released to the system.</summary>
    [NativeTypeName("UINT32")]
    public uint HeapsFreed;
}
