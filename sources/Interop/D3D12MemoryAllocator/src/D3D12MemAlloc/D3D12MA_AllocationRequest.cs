// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using TerraFX.Interop.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of planned allocation inside a NormalBlock.</summary>
internal unsafe partial struct D3D12MA_AllocationRequest
{
    [NativeTypeName("D3D12MA::AllocHandle")]
    public ulong allocHandle;

    [NativeTypeName("UINT64")]
    public ulong size;

    [NativeTypeName("UINT64")]
    public ulong algorithmData;

    [NativeTypeName("UINT64")]
    public ulong sumFreeSize; // Sum size of free items that overlap with proposed allocation.

    [NativeTypeName("UINT64")]
    public ulong sumItemSize; // Sum size of items to make lost that overlap with proposed allocation.

    [NativeTypeName("D3D12MA::SuballocationList::iterator")]
    public D3D12MA_Suballocation* item;
}
