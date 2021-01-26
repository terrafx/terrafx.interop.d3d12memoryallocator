// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

using UINT = System.UInt32;
using uint64_t = System.UInt64;

namespace TerraFX.Interop
{
    // Minimal port of std::atomic<T> for the purposes of this project
    internal struct atomic<T>
        where T : unmanaged
    {
        private T value;

        public void Add(T value)
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Add(ref Unsafe.As<T, UINT>(ref this.value), Unsafe.As<T, UINT>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Add(ref Unsafe.As<T, uint64_t>(ref this.value), Unsafe.As<T, uint64_t>(ref value));
            throw null!;
        }

        public void Subtract(T value)
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Add(ref Unsafe.As<T, int>(ref this.value), -Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Add(ref Unsafe.As<T, long>(ref this.value), -Unsafe.As<T, long>(ref value));
            throw null!;
        }

        public void Increment()
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Increment(ref Unsafe.As<T, UINT>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Increment(ref Unsafe.As<T, uint64_t>(ref value));
            throw null!;
        }

        public T Load()
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Increment(ref Unsafe.As<T, UINT>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Increment(ref Unsafe.As<T, uint64_t>(ref value));
            throw null!;
        }

        public T Store(T value)
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Exchange(ref Unsafe.As<T, UINT>(ref this.value), Unsafe.As<T, UINT>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Exchange(ref Unsafe.As<T, uint64_t>(ref this.value), Unsafe.As<T, uint64_t>(ref value));
            throw null!;
        }
    }
}
