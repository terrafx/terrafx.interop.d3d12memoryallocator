// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

internal unsafe struct D3D12MA_SuballocationItemSizeLess
    : D3D12MA_CmpLess<D3D12MA_List<D3D12MA_Suballocation>.iterator>,
      D3D12MA_CmpLess<D3D12MA_List<D3D12MA_Suballocation>.iterator, ulong>
{
    public readonly int Compare(D3D12MA_List<D3D12MA_Suballocation>.iterator lhs, D3D12MA_List<D3D12MA_Suballocation>.iterator rhs)
    {
        return lhs.m_pItem->Value.size.CompareTo(rhs.m_pItem->Value.size);
    }

    public readonly bool Invoke([NativeTypeName("const D3D12MA::SuballocationList::iterator")] in D3D12MA_List<D3D12MA_Suballocation>.iterator lhs, [NativeTypeName("const D3D12MA::SuballocationList::iterator")] in D3D12MA_List<D3D12MA_Suballocation>.iterator rhs)
    {
        return lhs.m_pItem->Value.size < rhs.m_pItem->Value.size;
    }

    public readonly bool Invoke([NativeTypeName("const D3D12MA::SuballocationList::iterator")] in D3D12MA_List<D3D12MA_Suballocation>.iterator lhs, [NativeTypeName("UINT64")] in ulong rhsSize)
    {
        return lhs.m_pItem->Value.size < rhsSize;
    }
}
