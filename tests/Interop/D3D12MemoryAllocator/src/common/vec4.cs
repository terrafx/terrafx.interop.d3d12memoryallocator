// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Common.h and Common.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop.DirectX.UnitTests;

public partial struct vec4(float x, float y, float z, float w)
{
    public float x = x;

    public float y = y;

    public float z = z;

    public float w = w;

    [UnscopedRef]
    [NativeTypeName("float &")]
    public ref float this[[NativeTypeName("uint32_t")] uint index] => ref Unsafe.Add(ref x, index);

    public static vec4 operator +([NativeTypeName("const vec4 &")] in vec4 lhs, [NativeTypeName("const vec4 &")] in vec4 rhs) => new vec4(
        lhs.x + rhs.x,
        lhs.y + rhs.y,
        lhs.z + rhs.z,
        lhs.w + rhs.w
    );

    public static vec4 operator -([NativeTypeName("const vec4 &")] in vec4 lhs, [NativeTypeName("const vec4 &")] in vec4 rhs) => new vec4(
        lhs.x - rhs.x,
        lhs.y - rhs.y,
        lhs.z - rhs.z,
        lhs.w - rhs.w
    );

    public static vec4 operator *([NativeTypeName("const vec4 &")] in vec4 lhs, float s) => new vec4(
        lhs.x * s,
        lhs.y * s,
        lhs.z * s,
        lhs.w * s
    );

    public readonly vec4 Normalized()
    {
        return this * (1.0f / MathF.Sqrt((x * x) + (y * y) + (z * z) + (w * w)));
    }
}
