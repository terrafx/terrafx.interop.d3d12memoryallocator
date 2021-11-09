// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.InteropServices;

namespace TerraFX.Interop.Windows.D3D12MA.UnitTests
{
    internal struct vec2
    {
        public float x;

        public float y;

        public vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public ref float this[[NativeTypeName("uint32_t")] uint index] => ref MemoryMarshal.CreateSpan(ref x, 2)[(int)index];

        public static vec2 operator +([NativeTypeName("const vec2&")] in vec2 lhs, [NativeTypeName("const vec2&")] in vec2 rhs)
        {
            return new vec2(
                lhs.x + rhs.x,
                lhs.y + rhs.y
            );
        }

        public static vec2 operator -([NativeTypeName("const vec2&")] in vec2 lhs, [NativeTypeName("const vec2&")] in vec2 rhs)
        {
            return new vec2(
                lhs.x - rhs.x,
                lhs.y - rhs.y
            );
        }

        public static vec2 operator *([NativeTypeName("const vec2&")] in vec2 lhs, float s)
        {
            return new vec2(
                lhs.x * s,
                lhs.y * s
            );
        }

        public readonly vec2 Normalized()
        {
            return this * (1.0f / MathF.Sqrt((x * x) + (y * y)));
        }
    }
}
