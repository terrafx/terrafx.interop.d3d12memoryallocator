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
        public partial T* Alloc<T0>(T0 arg0, delegate*<T0, T> cctor)
            where T0 : unmanaged;
        public partial T* Alloc<T0, T1>(T0 arg0, T1 arg1, delegate*<T0, T1, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged;
        public partial T* Alloc<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2, delegate*<T0, T1, T2, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged;
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
            for (nuint i = m_ItemBlocks.size(); i-- > 0; )
            {
                D3D12MA_DELETE_ARRAY_NO_DISPOSE(m_AllocationCallbacks, m_ItemBlocks[i]->pItems, (nuint)m_ItemBlocks[i]->Capacity);
            }
            m_ItemBlocks.clear(true);
        }

        public partial T* Alloc()
        {
            static T Cctor(void** args, void* f)
            {
                return default;
            }

            return Alloc(null, null, &Cctor);
        }

        public partial T* Alloc<T0>(T0 arg0, delegate*<T0, T> cctor)
            where T0 : unmanaged
        {
            void* args = &arg0;

            static T Cctor(void** args, void* f)
            {
                return ((delegate*<T0, T>)f)(*(T0*)args[0]);
            }

            return Alloc(&args, cctor, &Cctor);
        }

        public partial T* Alloc<T0, T1>(T0 arg0, T1 arg1, delegate*<T0, T1, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged
        {
            void* args = stackalloc void*[2] { &arg0, &arg1 };

            static T Cctor(void** args, void* f)
            {
                return ((delegate*<T0, T1, T>)f)(*(T0*)args[0], *(T1*)args[1]);
            }

            return Alloc(&args, cctor, &Cctor);
        }

        public partial T* Alloc<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2, delegate*<T0, T1, T2, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
        {
            void* args = stackalloc void*[3] { &arg0, &arg1, &arg2 };

            static T Cctor(void** args, void* f)
            {
                return ((delegate*<T0, T1, T2, T>)f)(*(T0*)args[0], *(T1*)args[1], *(T2*)args[2]);
            }

            return Alloc(&args, cctor, &Cctor);
        }

        private T* Alloc(void** args, void* f, delegate*<void**, void*, T> cctor)
        {
            for (nuint i = m_ItemBlocks.size(); i > 0; i--)
            {
                ItemBlock* block = m_ItemBlocks[i];
                // This block has some free items: Use first one.
                if (block->FirstFreeIndex != uint.MaxValue)
                {
                    Item* pItem = &block->pItems[block->FirstFreeIndex];
                    block->FirstFreeIndex = pItem->NextFreeIndex;
                    T* result = (T*)pItem->Value;
                    *result = cctor(args, f); // Explicit constructor call.
                    return result;
                }
            }

            {
                // No block has free item: Create new one and use it.
                ItemBlock* newBlock = CreateNewBlock();
                Item* pItem = &newBlock->pItems[0];
                newBlock->FirstFreeIndex = pItem->NextFreeIndex;
                T* result = (T*)pItem->Value;
                *result = cctor(args, f); // Explicit constructor call.
                return result;
            }
        }

        public partial void Free(T* ptr)
        {
            // Search all memory blocks to find ptr.
            for (nuint i = m_ItemBlocks.size(); i > 0; i--)
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
