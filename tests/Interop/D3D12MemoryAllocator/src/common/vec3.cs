// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Common.h and Common.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop.DirectX.UnitTests;

public partial struct vec3
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

    [UnscopedRef]
    [NativeTypeName("float &")]
    public ref float this[[NativeTypeName("uint32_t")] uint index] => ref Unsafe.Add(ref x, index);

    public static vec3 operator +([NativeTypeName("const vec3 &")] in vec3 lhs, [NativeTypeName("const vec3 &")] in vec3 rhs) => new vec3(
        lhs.x + rhs.x,
        lhs.y + rhs.y,
        lhs.z + rhs.z
    );

    public static vec3 operator -([NativeTypeName("const vec3 &")] in vec3 lhs, [NativeTypeName("const vec3 &")] in vec3 rhs) => new vec3(
        lhs.x - rhs.x,
        lhs.y - rhs.y,
        lhs.z - rhs.z
    );

    public static vec3 operator *([NativeTypeName("const vec3 &")] in vec3 lhs, float s) => new vec3(
        lhs.x * s,
        lhs.y * s,
        lhs.z * s
    );

    public readonly vec3 Normalized()
    {
        return this * (1.0f / MathF.Sqrt((x * x) + (y * y) + (z * z)));
    }
}
