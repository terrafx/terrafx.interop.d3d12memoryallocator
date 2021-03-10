// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 3a335d55c99e605775bbe9fe9c01ee6212804bed
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_CommittedAllocationListItemTraits : D3D12MA_IItemTypeTraits<D3D12MA_Allocation>
    {
        public D3D12MA_Allocation* GetPrev(D3D12MA_Allocation* item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_COMMITTED) || (item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_HEAP)));
            return item->m_Committed.prev;
        }

        public D3D12MA_Allocation* GetNext(D3D12MA_Allocation* item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_COMMITTED) || (item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_HEAP)));
            return item->m_Committed.next;
        }

        public D3D12MA_Allocation** AccessPrev(D3D12MA_Allocation* item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_COMMITTED) || (item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_HEAP)));

            fixed (D3D12MA_Allocation** alloc = &item->m_Committed.prev)
            {
                return alloc;
            }
        }

        public D3D12MA_Allocation** AccessNext(D3D12MA_Allocation* item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_COMMITTED) || (item->m_PackedData.GetType() == D3D12MA_Allocation.Type.TYPE_HEAP)));

            fixed (D3D12MA_Allocation** alloc = &item->m_Committed.next)
            {
                return alloc;
            }
        }
    }
}
