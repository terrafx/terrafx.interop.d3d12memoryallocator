// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    internal unsafe struct Pointer<T>
        where T : unmanaged
    {
        public T* Value;

        public Pointer(T* value)
        {
            Value = value;
        }

        public static implicit operator Pointer<T>(T* value) => new Pointer<T>(value);

        public static implicit operator T*(Pointer<T> value) => value.Value;
    }
}
