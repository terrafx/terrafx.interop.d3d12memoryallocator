// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using NUnit.Framework;
using TerraFX.Interop.DirectX;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX.UnitTests
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
    }
}
