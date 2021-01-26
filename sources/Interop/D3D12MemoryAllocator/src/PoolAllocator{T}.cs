// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    /// <summary>
    /// Allocator for objects of type T using a list of arrays (pools) to speed up
    /// allocation.Number of elements that can be allocated is not bounded because
    /// allocator can create multiple blocks.
    /// T should be POD because constructor and destructor is not called in Alloc or
    /// Free.
    /// </summary>
    internal unsafe partial struct PoolAllocator<T> : IDisposable
        where T : unmanaged, IDisposable
    {
        private readonly ALLOCATION_CALLBACKS* m_AllocationCallbacks;
        [NativeTypeName("UINT")] private readonly uint m_FirstBlockCapacity;
        private Vector<ItemBlock> m_ItemBlocks;

        // allocationCallbacks externally owned, must outlive this object.
        public PoolAllocator(ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("UINT")] uint firstBlockCapacity)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_FirstBlockCapacity = firstBlockCapacity;
            m_ItemBlocks = new(allocationCallbacks);

            D3D12MA_ASSERT(m_FirstBlockCapacity > 1);
        }

        public void Dispose() { Clear(); }

        public partial void Clear();

        public partial T* Alloc();

        public partial T* Alloc(void** args, delegate*<void**, T> ctor);

        public partial void Free(T* ptr);

        [StructLayout(LayoutKind.Explicit)]
        private struct Item
        {
            [FieldOffset(0), NativeTypeName("UINT")] public uint NextFreeIndex; // UINT32_MAX means end of list.
            [FieldOffset(0)] private T __Value_Data;

            public byte* Value => (byte*)Unsafe.AsPointer(ref __Value_Data);
        }

        private struct ItemBlock
        {
            public Item* pItems;
            [NativeTypeName("UINT")] public uint Capacity;
            [NativeTypeName("UINT")] public uint FirstFreeIndex;
        }

        private partial ItemBlock* CreateNewBlock();
    }

    internal unsafe partial struct PoolAllocator<T>
    {
        public partial void Clear()
        {
            for (nuint i = m_ItemBlocks.size(); i-- > 0;)
            {
                D3D12MA_DELETE_ARRAY_NO_DISPOSE(m_AllocationCallbacks, m_ItemBlocks[i]->pItems, (nuint)m_ItemBlocks[i]->Capacity);
            }
            m_ItemBlocks.clear(true);
        }

        public partial T* Alloc()
        {
            return Alloc(null, null);
        }

        public partial T* Alloc(void** args, delegate*<void**, T> ctor)
        {
            for (nuint i = m_ItemBlocks.size(); i-- > 0;)
            {
                ItemBlock* block = m_ItemBlocks[i];
                // This block has some free items: Use first one.
                if (block->FirstFreeIndex != uint.MaxValue)
                {
                    Item* pItem = &block->pItems[block->FirstFreeIndex];
                    block->FirstFreeIndex = pItem->NextFreeIndex;
                    T* result = (T*)pItem->Value;
                    *result = ctor == null ? default : ctor(args); // Explicit constructor call.
                    return result;
                }
            }

            {
                // No block has free item: Create new one and use it.
                ItemBlock* newBlock = CreateNewBlock();
                Item* pItem = &newBlock->pItems[0];
                newBlock->FirstFreeIndex = pItem->NextFreeIndex;
                T* result = (T*)pItem->Value;
                *result = ctor == null ? default : ctor(args); // Explicit constructor call.
                return result;
            }
        }

        public partial void Free(T* ptr)
        {
            // Search all memory blocks to find ptr.
            for (nuint i = m_ItemBlocks.size(); i-- > 0;)
            {
                ItemBlock* block = m_ItemBlocks[i];

                Item* pItemPtr;
                memcpy(&pItemPtr, &ptr, (nuint)sizeof(Item));

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
            D3D12MA_ASSERT(0);
        }

        private partial ItemBlock* CreateNewBlock()
        {
            uint newBlockCapacity = m_ItemBlocks.empty() ?
                m_FirstBlockCapacity : m_ItemBlocks.back()->Capacity * 3 / 2;

            ItemBlock newBlock = new() {
                pItems = D3D12MA_NEW_ARRAY<Item>(m_AllocationCallbacks, (nuint)newBlockCapacity),
                Capacity = newBlockCapacity,
                FirstFreeIndex = 0
            };

            m_ItemBlocks.push_back(&newBlock);

            // Setup singly-linked list of all free items in this block.
            for (uint i = 0; i < newBlockCapacity - 1; ++i)
            {
                newBlock.pItems[i].NextFreeIndex = i + 1;
            }
            newBlock.pItems[newBlockCapacity - 1].NextFreeIndex = uint.MaxValue;
            return m_ItemBlocks.back();
        }
    }
}
