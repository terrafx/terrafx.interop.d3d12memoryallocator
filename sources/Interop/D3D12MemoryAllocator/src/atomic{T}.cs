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

        public void Increment()
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Increment(ref Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Increment(ref Unsafe.As<T, long>(ref value));
            throw null!;
        }

        public T Load()
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Increment(ref Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Increment(ref Unsafe.As<T, long>(ref value));
            throw null!;
        }
    }
}
