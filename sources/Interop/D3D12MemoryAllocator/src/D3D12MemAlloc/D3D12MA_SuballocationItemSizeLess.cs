// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_SuballocationItemSizeLess
        : ICmpLess<D3D12MA_List<D3D12MA_Suballocation>.iterator, D3D12MA_List<D3D12MA_Suballocation>.iterator>,
          ICmpLess<D3D12MA_List<D3D12MA_Suballocation>.iterator, ulong>
    {
        public bool Invoke(D3D12MA_List<D3D12MA_Suballocation>.iterator lhs, D3D12MA_List<D3D12MA_Suballocation>.iterator rhs)
        {
            return lhs.Get()->size < rhs.Get()->size;
        }

        public bool Invoke(D3D12MA_List<D3D12MA_Suballocation>.iterator lhs, ulong rhs)
        {
            return lhs.Get()->size < rhs;
        }
    }
}
