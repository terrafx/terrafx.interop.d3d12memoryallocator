// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop.Windows.D3D12MA.UnitTests
{
    internal unsafe struct Pointer
    {
        public void* Value;

        public Pointer(void* value)
        {
            Value = value;
        }

        public static implicit operator Pointer(void* value) => new Pointer(value);

        public static implicit operator void*(Pointer value) => value.Value;
    }
}
