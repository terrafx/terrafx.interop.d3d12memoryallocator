// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12MA_VIRTUAL_BLOCK_FLAGS;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_VIRTUAL_BLOCK_DESC
{
    /// <summary>Constructor initializing description of a virtual block with given parameters.</summary>
    /// <param name="size"></param>
    /// <param name="flags"></param>
    /// <param name="allocationCallbacks"></param>
    public D3D12MA_VIRTUAL_BLOCK_DESC(ulong size, D3D12MA_VIRTUAL_BLOCK_FLAGS flags = D3D12MA_VIRTUAL_BLOCK_FLAG_NONE, D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks = null)
    {
        Flags = flags;
        Size = size;
        pAllocationCallbacks = allocationCallbacks;
    }
}
