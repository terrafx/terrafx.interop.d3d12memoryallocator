// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX.UnitTests;

internal partial struct Vertex(float x, float y, float z, float tx, float ty)
{
    public vec3 pos = new vec3(x, y, z);

    public vec2 texCoord = new vec2(tx, ty);
}
