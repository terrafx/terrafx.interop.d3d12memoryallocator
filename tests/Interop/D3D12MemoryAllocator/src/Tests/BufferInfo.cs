// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using TerraFX.Interop.Windows;

namespace TerraFX.Interop.DirectX.UnitTests;

public unsafe partial struct BufferInfo : IDisposable
{
    public ComPtr<ID3D12Resource> Buffer;

    public ComPtr<D3D12MA_Allocation> Allocation;

    public void Dispose()
    {
        Buffer.Dispose();
        Allocation.Dispose();
    }
}
