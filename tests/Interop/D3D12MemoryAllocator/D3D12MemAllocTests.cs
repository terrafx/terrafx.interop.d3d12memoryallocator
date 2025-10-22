// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Internal;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]

namespace TerraFX.Interop.DirectX.UnitTests;

public static unsafe partial class D3D12MemAllocTests
{
    internal static readonly Guid* IID_NULL = (Guid*)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAllocTests), sizeof(Guid)));

    internal static void D3D12MA_SORT<KeyT>(in KeyT* beg, in KeyT* end, Comparison<KeyT> cmp)
        where KeyT : unmanaged
    {
        Span<KeyT> items = new Span<KeyT>(beg, (int)(end - beg));
        items.Sort(cmp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe uint __sizeof<T>()
        where T : unmanaged
    {
        return (uint)(sizeof(T));
    }

    [Test]
    [Explicit]
    public static void D3D12Sample()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            string[] args = [];
            Assert.That(wmain(args), Is.Zero);
        }
        else
        {
            Assert.Inconclusive();
        }
    }

    [Test]
    public static void Tests()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            string[] args = ["--Test"];
            Assert.That(wmain(args), Is.Zero);
        }
        else
        {
            Assert.Inconclusive();
        }
    }

    [Test]
    [Explicit]
    public static void Benchmarks()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            string[] args = ["--Benchmark"];
            Assert.That(wmain(args), Is.Zero);
        }
        else
        {
            Assert.Inconclusive();
        }
    }
}
