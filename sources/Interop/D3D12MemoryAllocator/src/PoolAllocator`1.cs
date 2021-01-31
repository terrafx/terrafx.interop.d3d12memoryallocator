// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    /// <summary>Allocator for objects of type T using a list of arrays (pools) to speed up allocation.Number of elements that can be allocated is not bounded because allocator can create multiple blocks.</summary>
    /// <typeparam name="T">Should be POD because constructor and destructor is not called in Alloc (see extension) or <see cref="Free"/>.</typeparam>
    internal unsafe struct PoolAllocator<T> : IDisposable
        where T : unmanaged, IDisposable
    {
        internal readonly ALLOCATION_CALLBACKS* m_AllocationCallbacks;

        [NativeTypeName("UINT")]
        internal readonly uint m_FirstBlockCapacity;

        internal Vector<ItemBlock> m_ItemBlocks;

        // allocationCallbacks externally owned, must outlive this object.
        public PoolAllocator(ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("UINT")] uint firstBlockCapacity)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_FirstBlockCapacity = firstBlockCapacity;
            m_ItemBlocks = new Vector<ItemBlock>(allocationCallbacks);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_FirstBlockCapacity > 1));
        }

        public void Dispose()
        {
            if (typeof(T) == typeof(Allocation))
            {
                PoolAllocator_Allocation.Clear(ref Unsafe.As<PoolAllocator<T>, PoolAllocator<Allocation>>(ref this));
            }
            else if (typeof(T) == typeof(List<Suballocation>.Item))
            {
                PoolAllocator_SuballocationListItem.Clear(ref Unsafe.As<PoolAllocator<T>, PoolAllocator<List<Suballocation>.Item>>(ref this));
            }
            else
            {
                throw new ArgumentException("Invalid pool allocator type");
            }
        }

        public void Clear()
        {
            if (typeof(T) == typeof(Allocation))
            {
                PoolAllocator_Allocation.Clear(ref Unsafe.As<PoolAllocator<T>, PoolAllocator<Allocation>>(ref this));
            }
            else if (typeof(T) == typeof(List<Suballocation>.Item))
            {
                PoolAllocator_SuballocationListItem.Clear(ref Unsafe.As<PoolAllocator<T>, PoolAllocator<List<Suballocation>.Item>>(ref this));
            }
            else
            {
                throw new ArgumentException("Invalid pool allocator type");
            }
        }

        public T* Alloc()
        {
            if (typeof(T) == typeof(Allocation))
            {
                throw new NotImplementedException();
            }
            else if (typeof(T) == typeof(List<Suballocation>.Item))
            {
                return (T*)PoolAllocator_SuballocationListItem.Alloc(ref Unsafe.As<PoolAllocator<T>, PoolAllocator<List<Suballocation>.Item>>(ref this));
            }
            else
            {
                throw new ArgumentException("Invalid pool allocator type");
            }
        }

        public void Free(T* ptr)
        {
            if (typeof(T) == typeof(Allocation))
            {
                PoolAllocator_Allocation.Free(ref Unsafe.As<PoolAllocator<T>, PoolAllocator<Allocation>>(ref this), (Allocation*)ptr);
            }
            else if (typeof(T) == typeof(List<Suballocation>.Item))
            {
                PoolAllocator_SuballocationListItem.Free(ref Unsafe.As<PoolAllocator<T>, PoolAllocator<List<Suballocation>.Item>>(ref this), (List<Suballocation>.Item*)ptr);
            }
            else
            {
                throw new ArgumentException("Invalid pool allocator type");
            }
        }

        internal struct ItemBlock
        {
            /// <summary>Can be either <see cref="PoolAllocator_Allocation.Item"/>* or <see cref="PoolAllocator_SuballocationListItem.Item"/>*.</summary>
            public void* pItems;

            [NativeTypeName("UINT")]
            public uint Capacity;

            [NativeTypeName("UINT")]
            public uint FirstFreeIndex;
        }
    }

    internal unsafe static class PoolAllocator_Allocation
    {
        public static void Clear(this ref PoolAllocator<Allocation> @this)
        {
            for (nuint i = @this.m_ItemBlocks.size(); unchecked(i-- > 0);)
            {
                D3D12MA_DELETE_ARRAY(@this.m_AllocationCallbacks, (Item*)@this.m_ItemBlocks[i]->pItems, (nuint)@this.m_ItemBlocks[i]->Capacity);
            }

            @this.m_ItemBlocks.clear(true);
        }

        public static Allocation* Alloc(this ref PoolAllocator<Allocation> @this, AllocatorPimpl* allocator, ulong size, int wasZeroInitialized)
        {
            for (nuint i = @this.m_ItemBlocks.size(); unchecked(i-- > 0);)
            {
                PoolAllocator<Allocation>.ItemBlock* block = @this.m_ItemBlocks[i];
                // This block has some free items: Use first one.
                if (block->FirstFreeIndex != uint.MaxValue)
                {
                    Item* pItem = &((Item*)block->pItems)[block->FirstFreeIndex];
                    block->FirstFreeIndex = pItem->NextFreeIndex;
                    Allocation* result = (Allocation*)pItem->Value;
                    result->Ctor(allocator, size, wasZeroInitialized);
                    return result;
                }
            }

            {
                // No block has free item: Create new one and use it.
                PoolAllocator<Allocation>.ItemBlock* newBlock = @this.CreateNewBlock();
                Item* pItem = &((Item*)newBlock->pItems)[0];
                newBlock->FirstFreeIndex = pItem->NextFreeIndex;
                Allocation* result = (Allocation*)pItem->Value;
                result->Ctor(allocator, size, wasZeroInitialized);
                return result;
            }
        }

        public static void Free(this ref PoolAllocator<Allocation> @this, Allocation* ptr)
        {
            // Search all memory blocks to find ptr.
            for (nuint i = @this.m_ItemBlocks.size(); unchecked(i-- > 0);)
            {
                PoolAllocator<Allocation>.ItemBlock* block = @this.m_ItemBlocks[i];

                Item* pItemPtr = (Item*)ptr; // memcpy(&pItemPtr, &ptr, (nuint)sizeof(Item*));

                // Check if pItemPtr is in address range of this block.
                if ((pItemPtr >= block->pItems) && (pItemPtr < (Item*)block->pItems + block->Capacity))
                {
                    // ptr->Dispose(); // Explicit destructor call (skipped, Allocation has an empty destructor)
                    uint index = (uint)(pItemPtr - (Item*)block->pItems);
                    pItemPtr->NextFreeIndex = block->FirstFreeIndex;
                    block->FirstFreeIndex = index;
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Pointer doesn't belong to this memory pool."
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct Item
        {
            [FieldOffset(0), NativeTypeName("UINT")]
            public uint NextFreeIndex;

            [FieldOffset(0)]
            private Allocation __Value_Data;

            public byte* Value => (byte*)Unsafe.AsPointer(ref __Value_Data);
        }

        private static PoolAllocator<Allocation>.ItemBlock* CreateNewBlock(this ref PoolAllocator<Allocation> @this)
        {
            uint newBlockCapacity = @this.m_ItemBlocks.empty() ?
                @this.m_FirstBlockCapacity : @this.m_ItemBlocks.back()->Capacity * 3 / 2;

            PoolAllocator<Allocation>.ItemBlock newBlock = new() {
                pItems = D3D12MA_NEW_ARRAY<Item>(@this.m_AllocationCallbacks, (nuint)newBlockCapacity),
                Capacity = newBlockCapacity,
                FirstFreeIndex = 0
            };

            @this.m_ItemBlocks.push_back(&newBlock);

            // Setup singly-linked list of all free items in this block.
            for (uint i = 0; i < newBlockCapacity - 1; ++i)
            {
                ((Item*)newBlock.pItems)[i].NextFreeIndex = i + 1;
            }

            ((Item*)newBlock.pItems)[newBlockCapacity - 1].NextFreeIndex = uint.MaxValue;
            return @this.m_ItemBlocks.back();
        }
    }

    internal unsafe static class PoolAllocator_SuballocationListItem
    {
        public static void Clear(this ref PoolAllocator<List<Suballocation>.Item> @this)
        {
            for (nuint i = @this.m_ItemBlocks.size(); unchecked(i-- > 0);)
            {
                D3D12MA_DELETE_ARRAY(@this.m_AllocationCallbacks, (Item*)@this.m_ItemBlocks[i]->pItems, (nuint)@this.m_ItemBlocks[i]->Capacity);
            }

            @this.m_ItemBlocks.clear(true);
        }

        public static List<Suballocation>.Item* Alloc(this ref PoolAllocator<List<Suballocation>.Item> @this)
        {
            for (nuint i = @this.m_ItemBlocks.size(); unchecked(i-- > 0);)
            {
                PoolAllocator<List<Suballocation>.Item>.ItemBlock* block = @this.m_ItemBlocks[i];
                // This block has some free items: Use first one.
                if (block->FirstFreeIndex != uint.MaxValue)
                {
                    Item* pItem = &((Item*)block->pItems)[block->FirstFreeIndex];
                    block->FirstFreeIndex = pItem->NextFreeIndex;
                    List<Suballocation>.Item* result = (List<Suballocation>.Item*)pItem->Value;
                    *result = default;
                    return result;
                }
            }

            {
                // No block has free item: Create new one and use it.
                PoolAllocator<List<Suballocation>.Item>.ItemBlock* newBlock = @this.CreateNewBlock();
                Item* pItem = &((Item*)newBlock->pItems)[0];
                newBlock->FirstFreeIndex = pItem->NextFreeIndex;
                List<Suballocation>.Item* result = (List<Suballocation>.Item*)pItem->Value;
                *result = default;
                return result;
            }
        }

        public static void Free(this ref PoolAllocator<List<Suballocation>.Item> @this, List<Suballocation>.Item* ptr)
        {
            // Search all memory blocks to find ptr.
            for (nuint i = @this.m_ItemBlocks.size(); unchecked(i-- > 0);)
            {
                PoolAllocator<List<Suballocation>.Item>.ItemBlock* block = @this.m_ItemBlocks[i];

                Item* pItemPtr = (Item*)ptr; // memcpy(&pItemPtr, &ptr, (nuint)sizeof(Item*));

                // Check if pItemPtr is in address range of this block.
                if ((pItemPtr >= block->pItems) && (pItemPtr < (Item*)block->pItems + block->Capacity))
                {
                    ptr->Dispose(); // Explicit destructor call.
                    uint index = (uint)(pItemPtr - (Item*)block->pItems);
                    pItemPtr->NextFreeIndex = block->FirstFreeIndex;
                    block->FirstFreeIndex = index;
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Pointer doesn't belong to this memory pool."
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct Item
        {
            [FieldOffset(0), NativeTypeName("UINT")]
            public uint NextFreeIndex;

            [FieldOffset(0)]
            private List<Suballocation>.Item __Value_Data;

            public byte* Value => (byte*)Unsafe.AsPointer(ref __Value_Data);
        }

        private static PoolAllocator<List<Suballocation>.Item>.ItemBlock* CreateNewBlock(this ref PoolAllocator<List<Suballocation>.Item> @this)
        {
            uint newBlockCapacity = @this.m_ItemBlocks.empty() ?
                @this.m_FirstBlockCapacity : @this.m_ItemBlocks.back()->Capacity * 3 / 2;

            PoolAllocator<List<Suballocation>.Item>.ItemBlock newBlock = new() {
                pItems = D3D12MA_NEW_ARRAY<Item>(@this.m_AllocationCallbacks, (nuint)newBlockCapacity),
                Capacity = newBlockCapacity,
                FirstFreeIndex = 0
            };

            @this.m_ItemBlocks.push_back(&newBlock);

            // Setup singly-linked list of all free items in this block.
            for (uint i = 0; i < newBlockCapacity - 1; ++i)
            {
                ((Item*)newBlock.pItems)[i].NextFreeIndex = i + 1;
            }

            ((Item*)newBlock.pItems)[newBlockCapacity - 1].NextFreeIndex = uint.MaxValue;
            return @this.m_ItemBlocks.back();
        }
    }
}
