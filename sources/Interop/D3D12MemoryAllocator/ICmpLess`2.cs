// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    internal interface ICmpLess<T, U>
        where T : unmanaged
    {
        bool Invoke(T lhs, U rhs);
    }
}
