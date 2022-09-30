// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Common.h and Common.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop.DirectX.UnitTests;

public partial struct vec2
{
    public float x;

    public float y;

    public vec2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    [UnscopedRef]
    [NativeTypeName("float &")]
    public ref float this[[NativeTypeName("uint32_t")] uint index] => ref Unsafe.Add(ref x, index);

    public static vec2 operator +([NativeTypeName("const vec2 &")] in vec2 lhs, [NativeTypeName("const vec2 &")] in vec2 rhs) => new vec2(
        lhs.x + rhs.x,
        lhs.y + rhs.y
    );

    public static vec2 operator -([NativeTypeName("const vec2 &")] in vec2 lhs, [NativeTypeName("const vec2 &")] in vec2 rhs) => new vec2(
        lhs.x - rhs.x,
        lhs.y - rhs.y
    );

    public static vec2 operator *([NativeTypeName("const vec2 &")] in vec2 lhs, float s) => new vec2(
        lhs.x * s,
        lhs.y * s
    );

    public readonly vec2 Normalized()
    {
        return this * (1.0f / MathF.Sqrt((x * x) + (y * y)));
    }
}
