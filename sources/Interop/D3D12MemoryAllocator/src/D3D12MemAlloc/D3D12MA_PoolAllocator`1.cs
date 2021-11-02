// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    /// <summary>Allocator for objects of type T using a list of arrays (pools) to speed up allocation.Number of elements that can be allocated is not bounded because allocator can create multiple blocks.</summary>
    /// <typeparam name="T">Should be POD because constructor and destructor is not called in Alloc (see extension) or <see cref="Free"/>.</typeparam>
    internal unsafe struct D3D12MA_PoolAllocator<T> : IDisposable
        where T : unmanaged, IDisposable
    {
        internal D3D12MA_ALLOCATION_CALLBACKS* m_AllocationCallbacks;

        [NativeTypeName("UINT")]
        internal uint m_FirstBlockCapacity;

        internal D3D12MA_Vector<ItemBlock> m_ItemBlocks;

        // allocationCallbacks externally owned, must outlive this object.
        public static void _ctor(ref D3D12MA_PoolAllocator<T> pThis, [NativeTypeName("D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("UINT")] uint firstBlockCapacity)
        {
            pThis.m_AllocationCallbacks = allocationCallbacks;
            pThis.m_FirstBlockCapacity = firstBlockCapacity;

            D3D12MA_Vector<ItemBlock>._ctor(ref pThis.m_ItemBlocks, allocationCallbacks);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pThis.m_FirstBlockCapacity > 1));
        }

        public void Dispose()
        {
            Clear();
            m_ItemBlocks.Dispose();
        }

        public void Clear()
        {
            for (nuint i = m_ItemBlocks.size(); unchecked(i-- != 0);)
            {
                D3D12MA_DELETE_ARRAY(m_AllocationCallbacks, m_ItemBlocks[i]->pItems, m_ItemBlocks[i]->Capacity);
            }
            m_ItemBlocks.clear(true);
        }

        public T* Alloc()
        {
            T* result;
            Item* pItem;

            for (nuint i = m_ItemBlocks.size(); unchecked(i-- != 0);)
            {
                ItemBlock* block = m_ItemBlocks[i];

                // This block has some free items: Use first one.
                if (block->FirstFreeIndex != UINT32_MAX)
                {
                    pItem = &block->pItems[block->FirstFreeIndex];
                    block->FirstFreeIndex = pItem->NextFreeIndex;

                    result = (T*)&pItem->Value;
                    return result;
                }
            }

            // No block has free item: Create new one and use it.
            ItemBlock* newBlock = CreateNewBlock();
            pItem = &newBlock->pItems[0];
            newBlock->FirstFreeIndex = pItem->NextFreeIndex;

            result = (T*)&pItem->Value;
            return result;
        }

        public void Free(T* ptr)
        {
            // Search all memory blocks to find ptr.

            for (nuint i = m_ItemBlocks.size(); unchecked(i-- != 0);)
            {
                ItemBlock* block = m_ItemBlocks[i];

                Item* pItemPtr;
                _ = memcpy(&pItemPtr, &ptr, (nuint)sizeof(Item*));

                // Check if pItemPtr is in address range of this block.
                if ((pItemPtr >= block->pItems) && (pItemPtr < block->pItems + block->Capacity))
                {
                    ptr->Dispose(); // Explicit destructor call.
                    uint index = (uint)(pItemPtr - block->pItems);
                    pItemPtr->NextFreeIndex = block->FirstFreeIndex;
                    block->FirstFreeIndex = index;
                    return;
                }
            }

            D3D12MA_ASSERT(false); // Pointer doesn't belong to this memory pool
        }

        private ItemBlock* CreateNewBlock()
        {
            uint newBlockCapacity = m_ItemBlocks.empty() ? m_FirstBlockCapacity : m_ItemBlocks.back()->Capacity * 3 / 2;

            ItemBlock newBlock = new ItemBlock {
                pItems = D3D12MA_NEW_ARRAY<Item>(m_AllocationCallbacks, newBlockCapacity),
                Capacity = newBlockCapacity,
                FirstFreeIndex = 0
            };

            m_ItemBlocks.push_back(newBlock);

            // Setup singly-linked list of all free items in this block.
            for (uint i = 0; i < newBlockCapacity - 1; ++i)
            {
                newBlock.pItems[i].NextFreeIndex = i + 1;
            }

            newBlock.pItems[newBlockCapacity - 1].NextFreeIndex = UINT32_MAX;
            return m_ItemBlocks.back();
        }

        internal struct Item
        {
            // UINT32_MAX means end of list.
            public ref uint NextFreeIndex => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Unsafe.As<T, uint>(ref Value), 1));

            public T Value;
        }

        internal struct ItemBlock
        {
            public Item* pItems;

            [NativeTypeName("UINT")]
            public uint Capacity;

            [NativeTypeName("UINT")]
            public uint FirstFreeIndex;
        }
    }
}
