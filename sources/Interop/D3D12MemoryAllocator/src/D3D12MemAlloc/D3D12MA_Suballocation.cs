// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;

namespace TerraFX.Interop.DirectX;

/// <summary>Represents a region of NormalBlock that is either assigned and returned as allocated memory block or free.</summary>
internal unsafe partial struct D3D12MA_Suballocation : IDisposable
{
    [NativeTypeName("UINT64")]
    public ulong offset;

    [NativeTypeName("UINT64")]
    public ulong size;

    public void* privateData;

    public D3D12MA_SuballocationType type;

    readonly void IDisposable.Dispose()
    {
    }
}
