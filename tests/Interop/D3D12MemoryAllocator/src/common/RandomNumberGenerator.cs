// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Common.h and Common.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX.UnitTests;

public partial struct RandomNumberGenerator
{
    [NativeTypeName("uint32_t")]
    private uint m_Value;

    public RandomNumberGenerator()
    {
        m_Value = GetTickCount();
    }

    public RandomNumberGenerator([NativeTypeName("uint32_t")] uint seed)
    {
        m_Value = seed;
    }

    public void Seed([NativeTypeName("uint32_t")] uint seed)
    {
        m_Value = seed;
    }

    [return: NativeTypeName("uint32_t")]
    public uint Generate()
    {
        return GenerateFast() ^ (GenerateFast() >> 7);
    }

    public bool GenerateBool() { return (GenerateFast() & 0x4) != 0; }

    [return: NativeTypeName("uint32_t")]
    private uint GenerateFast()
    {
        return m_Value = ((m_Value * 196314165) + 907633515);
    }
}
