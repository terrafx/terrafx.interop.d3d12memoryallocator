// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace TerraFX.Interop
{
    internal static class atomic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(this ref atomic<ulong> pThis, ulong value)
        {
            Interlocked.Add(ref pThis.value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(this ref atomic<ulong> pThis, ulong value)
        {
            Interlocked.Add(ref Unsafe.As<ulong, long>(ref pThis.value), -(long)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Increment(this ref atomic<uint> pThis)
        {
            Interlocked.Increment(ref pThis.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Load(this in atomic<uint> pThis)
        {
            return pThis.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Load(this in atomic<ulong> pThis)
        {
            return Interlocked.Read(ref Unsafe.AsRef(in pThis).value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(this ref atomic<uint> pThis, uint value)
        {
            pThis.value = value;
        }
    }
}
