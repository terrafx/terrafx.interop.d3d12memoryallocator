// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using NUnit.Framework;
using TerraFX.Interop.Windows.D3D12;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.Windows.D3D12MA.UnitTests
{
    internal unsafe partial class D3D12MemAllocTests
    {
        public static void CHECK_BOOL(bool cond, [CallerFilePath] string __FILE__ = "", [CallerLineNumber] int __LINE__ = 0, [CallerArgumentExpression("cond")] string expr = "")
            => Assert.False(!cond, $"{__FILE__}({__LINE__}): !({(string.IsNullOrEmpty(expr) ? cond : expr)})");

        public static void CHECK_HR(int hr, [CallerFilePath] string __FILE__ = "", [CallerLineNumber] int __LINE__ = 0, [CallerArgumentExpression("hr")] string expr = "")
            => Assert.False(FAILED(hr), $"{__FILE__}({__LINE__}): FAILED({(string.IsNullOrEmpty(expr) ? hr.ToString("X8") : expr)})");

        public static ulong CeilDiv(ulong x, ulong y)
            => (x + y - 1) / y;

        // public static T RoundDiv(T x, T y)
        // {
        //     return (x + y / (T)2) / y;
        // }

        public static uint AlignUp(uint val, uint align)
            => (val + align - 1) / align * align;

        public static nuint AlignUp(nuint val, nuint align)
            => (val + align - 1) / align * align;

        public const float PI = 3.14159265358979323846264338327950288419716939937510582f;

        public static readonly D3D12_RANGE* EMPTY_RANGE = (D3D12_RANGE*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAllocTests), sizeof(D3D12_RANGE));

        public static float Dot([NativeTypeName("const vec3&")] in vec3 lhs, [NativeTypeName("const vec3&")] in vec3 rhs)
            => (lhs.x * rhs.x) + (lhs.y * rhs.y) + (lhs.z * rhs.z);

        public static vec3 Cross([NativeTypeName("const vec3&")] in vec3 lhs, [NativeTypeName("const vec3&")] in vec3 rhs)
        {
            return new vec3(
                (lhs.y * rhs.z) - (lhs.z * rhs.y),
                (lhs.z * rhs.x) - (lhs.x * rhs.z),
                (lhs.x * rhs.y) - (lhs.y * rhs.x)
            );
        }
    }
}
