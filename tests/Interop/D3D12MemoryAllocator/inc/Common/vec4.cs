// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TerraFX.Interop.DirectX.UnitTests;

internal struct vec4
{
    public float x;

    public float y;

    public float z;

    public float w;

    public vec4(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    [UnscopedRef]
    public ref float this[[NativeTypeName("uint32_t")] uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref MemoryMarshal.CreateSpan(ref x, 4)[(int)index];
        }
    }

    public static vec4 operator +([NativeTypeName("const vec4&")] in vec4 lhs, [NativeTypeName("const vec4&")] in vec4 rhs)
    {
        return new vec4(
            lhs.x + rhs.x,
            lhs.y + rhs.y,
            lhs.z + rhs.z,
            lhs.w + rhs.w
        );
    }

    public static vec4 operator -([NativeTypeName("const vec4&")] in vec4 lhs, [NativeTypeName("const vec4&")] in vec4 rhs)
    {
        return new vec4(
            lhs.x - rhs.x,
            lhs.y - rhs.y,
            lhs.z - rhs.z,
            lhs.w - rhs.w
        );
    }

    public static vec4 operator *([NativeTypeName("const vec4&")] in vec4 lhs, float s)
    {
        return new vec4(
            lhs.x * s,
            lhs.y * s,
            lhs.z * s,
            lhs.w + s
        );
    }

    public readonly vec4 Normalized()
    {
        return this * (1.0f / MathF.Sqrt((x * x) + (y * y) + (z * z) + (w * w)));
    }
}
