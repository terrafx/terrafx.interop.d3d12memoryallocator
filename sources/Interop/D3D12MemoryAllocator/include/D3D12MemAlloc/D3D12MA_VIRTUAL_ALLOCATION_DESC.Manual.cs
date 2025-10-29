// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12MA_VIRTUAL_ALLOCATION_FLAGS;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_VIRTUAL_ALLOCATION_DESC
{
    /// Constructor initializing description of a virtual allocation with given parameters.
    public D3D12MA_VIRTUAL_ALLOCATION_DESC(ulong size, ulong alignment, D3D12MA_VIRTUAL_ALLOCATION_FLAGS flags = D3D12MA_VIRTUAL_ALLOCATION_FLAG_NONE, void* privateData = null)
    {
        Flags = flags;
        Size = size;
        Alignment = alignment;
        pPrivateData = privateData;
    }
}
