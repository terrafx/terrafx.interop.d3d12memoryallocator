// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace TerraFX.Interop
{
    // Minimal port of std::atomic<T> for the purposes of this project
    internal struct atomic<T>
        where T : unmanaged
    {
        private T value;

        public void Add(T value)
        {
            if (typeof(T) == typeof(uint))
                Interlocked.Add(ref Unsafe.As<T, uint>(ref this.value), Unsafe.As<T, uint>(ref value));
            else if (typeof(T) == typeof(ulong))
                Interlocked.Add(ref Unsafe.As<T, ulong>(ref this.value), Unsafe.As<T, ulong>(ref value));
            throw null!;
        }

        public void Subtract(T value)
        {
            if (typeof(T) == typeof(uint))
                Interlocked.Add(ref Unsafe.As<T, int>(ref this.value), -Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(ulong))
                Interlocked.Add(ref Unsafe.As<T, long>(ref this.value), -Unsafe.As<T, long>(ref value));
            throw null!;
        }

        public void Increment()
        {
            if (typeof(T) == typeof(uint))
                Interlocked.Increment(ref Unsafe.As<T, uint>(ref value));
            else if (typeof(T) == typeof(ulong))
                Interlocked.Increment(ref Unsafe.As<T, ulong>(ref value));
            throw null!;
        }

        public T Load()
        {
            if (typeof(T) == typeof(uint))
                Interlocked.Increment(ref Unsafe.As<T, uint>(ref value));
            else if (typeof(T) == typeof(ulong))
                Interlocked.Increment(ref Unsafe.As<T, ulong>(ref value));
            throw null!;
        }

        public T Store(T value)
        {
            if (typeof(T) == typeof(uint))
                Interlocked.Exchange(ref Unsafe.As<T, uint>(ref this.value), Unsafe.As<T, uint>(ref value));
            else if (typeof(T) == typeof(ulong))
                Interlocked.Exchange(ref Unsafe.As<T, ulong>(ref this.value), Unsafe.As<T, ulong>(ref value));
            throw null!;
        }
    }
}
