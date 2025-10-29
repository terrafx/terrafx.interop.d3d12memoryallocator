// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of created virtual allocation to be passed to <see cref="D3D12MA_VirtualBlock.Allocate" />.</summary>
public unsafe partial struct D3D12MA_VIRTUAL_ALLOCATION_DESC
{
    /// <summary>Flags for the virtual allocation.</summary>
    public D3D12MA_VIRTUAL_ALLOCATION_FLAGS Flags;

    /// <summary>Size of the allocation.</summary>
    /// <remarks>Cannot be zero.</remarks>
    [NativeTypeName("UINT64")]
    public ulong Size;

    /// <summary>Required alignment of the allocation.</summary>
    /// <remarks>Must be power of two. Special value 0 has the same meaning as 1 - means no special alignment is required, so allocation can start at any offset.</remarks>
    [NativeTypeName("UINT64")]
    public ulong Alignment;

    /// <summary>Custom pointer to be associated with the allocation.</summary>
    /// <remarks>It can be fetched or changed later.</remarks>
    public void* pPrivateData;
}
