// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12MA_Allocation.Type;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Stores linked list of Allocation objects that are of <see cref="TYPE_COMMITTED" /> or <see cref="TYPE_HEAP" />. Thread-safe, synchronized internally.</summary>
internal unsafe partial struct D3D12MA_CommittedAllocationListItemTraits : D3D12MA_ItemTypeTraits<D3D12MA_CommittedAllocationListItemTraits, D3D12MA_Allocation>
{
    public static D3D12MA_Allocation* GetPrev([NativeTypeName("const ItemType *")] D3D12MA_Allocation* item)
    {
        D3D12MA_ASSERT((item->m_PackedData.GetType() == TYPE_COMMITTED) || (item->m_PackedData.GetType() == TYPE_HEAP));
        return item->Anonymous.m_Committed.prev;
    }

    public static D3D12MA_Allocation* GetNext([NativeTypeName("const ItemType *")] D3D12MA_Allocation* item)
    {
        D3D12MA_ASSERT((item->m_PackedData.GetType() == TYPE_COMMITTED) || (item->m_PackedData.GetType() == TYPE_HEAP));
        return item->Anonymous.m_Committed.next;
    }

    [return: NativeTypeName("ItemType *&")]
    public static ref D3D12MA_Allocation* AccessPrev([NativeTypeName("ItemType *")] D3D12MA_Allocation* item)
    {
        D3D12MA_ASSERT((item->m_PackedData.GetType() == TYPE_COMMITTED) || (item->m_PackedData.GetType() == TYPE_HEAP));
        return ref item->Anonymous.m_Committed.prev;
    }

    [return: NativeTypeName("ItemType *&")]
    public static ref D3D12MA_Allocation* AccessNext([NativeTypeName("ItemType *")] D3D12MA_Allocation* item)
    {
        D3D12MA_ASSERT((item->m_PackedData.GetType() == TYPE_COMMITTED) || (item->m_PackedData.GetType() == TYPE_HEAP));
        return ref item->Anonymous.m_Committed.next;
    }
}
