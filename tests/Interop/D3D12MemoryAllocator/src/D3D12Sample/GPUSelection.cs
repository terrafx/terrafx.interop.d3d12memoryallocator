// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX.UnitTests;

internal partial struct GPUSelection
{
    [NativeTypeName("UINT32")]
    public uint Index;

    [NativeTypeName("std::wstring")]
    public string Substring;

    public GPUSelection()
    {
        Index = uint.MaxValue;
        Substring = "";
    }
}
