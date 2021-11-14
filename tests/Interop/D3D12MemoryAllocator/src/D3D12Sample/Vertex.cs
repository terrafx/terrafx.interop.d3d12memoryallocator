// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX.UnitTests
{
    internal struct Vertex
    {
        public vec3 pos;

        public vec2 texCoord;

        public Vertex(float x, float y, float z, float tx, float ty)
        {
            pos = new vec3(x, y, z);
            texCoord = new vec2(tx, ty);
        }
    }
}
