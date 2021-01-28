// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>Thin wrapper for pointer types to allow using them as generic arguments.</summary>
    internal unsafe readonly struct Ptr<T>
        where T : unmanaged
    {
        private readonly T* ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ptr(T* ptr)
        {
            this.ptr = ptr;
        }

        public T* Value => ptr;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref ptr[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Ptr<T>(T* ptr) => new(ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(Ptr<T> ptr) => ptr.ptr;
    }
}
