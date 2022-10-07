// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Represents single memory allocation done inside <see cref="D3D12MA_VirtualBlock" />.</summary>
public partial struct D3D12MA_VirtualAllocation
{
    /// <summary>Unique idenitfier of current allocation. 0 means null/invalid.</summary>
    [NativeTypeName("D3D12MA::AllocHandle")]
    public ulong AllocHandle;
}
