// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.InteropServices;

namespace TerraFX.Interop.Windows.D3D12MA.UnitTests
{
    internal struct vec3
    {
        public float x;

        public float y;

        public float z;

        public vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public ref float this[[NativeTypeName("uint32_t")] uint index] => ref MemoryMarshal.CreateSpan(ref x, 3)[(int)index];

        public static vec3 operator +([NativeTypeName("const vec3&")] in vec3 lhs, [NativeTypeName("const vec3&")] in vec3 rhs)
        {
            return new vec3(
                lhs.x + rhs.x,
                lhs.y + rhs.y,
                lhs.z + rhs.z
            );
        }

        public static vec3 operator -([NativeTypeName("const vec3&")] in vec3 lhs, [NativeTypeName("const vec3&")] in vec3 rhs)
        {
            return new vec3(
                lhs.x - rhs.x,
                lhs.y - rhs.y,
                lhs.z - rhs.z
            );
        }

        public static vec3 operator *([NativeTypeName("const vec3&")] in vec3 lhs, float s)
        {
            return new vec3(
                lhs.x * s,
                lhs.y * s,
                lhs.z * s
            );
        }

        public readonly vec3 Normalized()
        {
            return this * (1.0f / MathF.Sqrt((x * x) + (y * y) + (z * z)));
        }
    }
}
