// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct Pointer
{
    public void* Value;

    public Pointer(void* value)
    {
        Value = value;
    }
}
