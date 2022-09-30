// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using TerraFX.Interop.Windows;

namespace TerraFX.Interop.DirectX.UnitTests;

public unsafe partial struct ResourceWithAllocation : IDisposable
{
    public ComPtr<ID3D12Resource> resource;

    public ComPtr<D3D12MA_Allocation> allocation;

    [NativeTypeName("UINT64")]
    public ulong size;

    [NativeTypeName("UINT")]
    public uint dataSeed;

    public ResourceWithAllocation()
    {
        size = ulong.MaxValue;
    }

    public void Dispose()
    {
        resource.Dispose();
        allocation.Dispose();
    }

    public void Reset()
    {
        _ = resource.Reset();
        _ = allocation.Reset();

        size = ulong.MaxValue;
        dataSeed = 0;
    }
}
