// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of an existing virtual allocation, returned by <see cref="D3D12MA_VirtualBlock.GetAllocationInfo" />.</summary>
public unsafe partial struct D3D12MA_VIRTUAL_ALLOCATION_INFO
{
    /// <summary>Offset of the allocation.</summary>
    [NativeTypeName("UINT64")]
    public ulong Offset;

    /// <summary>Size of the allocation.</summary>
    /// <remarks>Same value as passed in <see cref="D3D12MA_VIRTUAL_ALLOCATION_DESC.Size" />.</remarks>
    [NativeTypeName("UINT64")]
    public ulong Size;

    /// <summary>Custom pointer associated with the allocation.</summary>
    /// <remarks>Same value as passed in <see cref="D3D12MA_VIRTUAL_ALLOCATION_DESC.pPrivateData" /> or <see cref="D3D12MA_VirtualBlock.SetAllocationPrivateData" />.</remarks>
    public void* pPrivateData;
}
