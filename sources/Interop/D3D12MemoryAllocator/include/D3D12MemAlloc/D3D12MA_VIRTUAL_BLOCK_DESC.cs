// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of created <see cref="D3D12MA_VirtualBlock" /> object to be passed to <see cref="D3D12MA_CreateVirtualBlock" />.</summary>
public unsafe partial struct D3D12MA_VIRTUAL_BLOCK_DESC
{
    /// <summary>Flags.</summary>
    public D3D12MA_VIRTUAL_BLOCK_FLAGS Flags;

    /// <summary>Total size of the block.</summary>
    /// <remarks>Sizes can be expressed in bytes or any units you want as long as you are consistent in using them. For example, if you allocate from some array of structures, 1 can mean single instance of entire structure.</remarks>
    [NativeTypeName("UINT64")]
    public ulong Size;

    /// <summary>Custom CPU memory allocation callbacks. Optional.</summary>
    /// <remarks>Optional, can be null. When specified, will be used for all CPU-side memory allocations.</remarks>
    [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")]
    public D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;
}
