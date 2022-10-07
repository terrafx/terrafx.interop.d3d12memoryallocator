// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;

namespace TerraFX.Interop.DirectX.UnitTests;

internal static unsafe partial class DisposeExtensions
{
    public static void Dispose<T>(this List<T> list)
        where T : IDisposable
    {
        foreach (var item in list)
        {
            item.Dispose();
        }
    }
}
