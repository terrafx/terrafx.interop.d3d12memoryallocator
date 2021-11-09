// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.D3D12MA.UnitTests
{
    internal sealed class RandomNumberGenerator
    {
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
        public uint Generate() => GenerateFast() ^ (GenerateFast() >> 7);

        public bool GenerateBool() => (GenerateFast() & 0x4) != 0;

        [NativeTypeName("uint32_t")]
        private uint m_Value;

        [return: NativeTypeName("uint32_t")]
        private uint GenerateFast()
        {
            return m_Value = unchecked((m_Value * 196314165) + 907633515);
        }
    }
}
