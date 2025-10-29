// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters for defragmentation.</summary>
/// <remarks>To be used with functions <see cref="D3D12MA_Allocator.BeginDefragmentation" /> and <see cref="D3D12MA_Pool.BeginDefragmentation" />.</remarks>
public partial struct D3D12MA_DEFRAGMENTATION_DESC
{
    /// <summary>Flags.</summary>
    public D3D12MA_DEFRAGMENTATION_FLAGS Flags;

    /// <summary>Maximum numbers of bytes that can be copied during single pass, while moving allocations to different places.</summary>
    /// <remarks>0 means no limit.</remarks>
    [NativeTypeName("UINT64")]
    public ulong MaxBytesPerPass;

    /// <summary>Maximum number of allocations that can be moved during single pass to a different place.</summary>
    /// <remarks>0 means no limit.</remarks>
    [NativeTypeName("UINT32")]
    public uint MaxAllocationsPerPass;
}
