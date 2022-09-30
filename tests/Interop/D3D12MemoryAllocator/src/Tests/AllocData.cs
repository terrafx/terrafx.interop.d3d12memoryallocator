// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX.UnitTests;

public unsafe partial struct AllocData
{
    public D3D12MA_VirtualAllocation allocation;

    [NativeTypeName("UINT64")]
    public ulong allocOffset;

    [NativeTypeName("UINT64")]
    public ulong requestedSize;

    [NativeTypeName("UINT64")]
    public ulong allocationSize;
}
