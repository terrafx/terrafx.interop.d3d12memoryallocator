// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace TerraFX.Interop
{
    // Minimal port of std::atomic<T> for the purposes of this project
    internal struct atomic<T>
        where T : unmanaged
    {
        internal T value;
    }

    internal static class atomic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(this ref atomic<uint> @this, uint value)
        {
            Interlocked.Add(ref @this.value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(this ref atomic<ulong> @this, ulong value)
        {
            Interlocked.Add(ref @this.value, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(this ref atomic<uint> @this, uint value)
        {
            Interlocked.Add(ref Unsafe.As<uint, int>(ref @this.value), -(int)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(this ref atomic<ulong> @this, ulong value)
        {
            Interlocked.Add(ref Unsafe.As<ulong, long>(ref @this.value), -(long)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Increment(this ref atomic<uint> @this)
        {
            Interlocked.Increment(ref @this.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Increment(this ref atomic<ulong> @this)
        {
            Interlocked.Increment(ref @this.value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Load(this in atomic<uint> @this)
        {
            return @this.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Load(this in atomic<ulong> @this)
        {
            return Interlocked.Read(ref Unsafe.AsRef(in @this).value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(this ref atomic<uint> @this, uint value)
        {
            @this.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Store(this ref atomic<ulong> @this, ulong value)
        {
            Interlocked.Exchange(ref @this.value, value);
        }
    }
}
