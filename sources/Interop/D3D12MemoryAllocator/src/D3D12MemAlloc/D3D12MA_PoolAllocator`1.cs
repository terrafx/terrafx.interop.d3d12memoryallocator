// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Allocator for objects of type T using a list of arrays (pools) to speed up allocation.Number of elements that can be allocated is not bounded because allocator can create multiple blocks.</summary>
/// <typeparam name="T">T should be POD because constructor and destructor is not called in Alloc or Free.</typeparam>
internal unsafe partial struct D3D12MA_PoolAllocator<T> : IDisposable
    where T : unmanaged, IDisposable
{
    [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")]
    private readonly D3D12MA_ALLOCATION_CALLBACKS* m_AllocationCallbacks;

    [NativeTypeName("UINT")]
    private readonly uint m_FirstBlockCapacity;

    internal D3D12MA_Vector<ItemBlock> m_ItemBlocks;

    // allocationCallbacks externally owned, must outlive this object.
    public D3D12MA_PoolAllocator([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("UINT")] uint firstBlockCapacity)
    {
        m_AllocationCallbacks = (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(in allocationCallbacks));
        m_FirstBlockCapacity = firstBlockCapacity;
        m_ItemBlocks = new D3D12MA_Vector<ItemBlock>(allocationCallbacks);

        D3D12MA_ASSERT(m_FirstBlockCapacity > 1);
    }

    public void Dispose()
    {
        Clear();
        m_ItemBlocks.Dispose();
    }

    public void Clear()
    {
        for (nuint i = m_ItemBlocks.size(); i-- != 0;)
        {
            D3D12MA_DELETE_ARRAY(*m_AllocationCallbacks, m_ItemBlocks[i].pItems);
        }
        m_ItemBlocks.clear(true);
    }

    public void Free(T* ptr)
    {
        // Search all memory blocks to find ptr.

        for (nuint i = m_ItemBlocks.size(); i-- != 0;)
        {
            ref ItemBlock block = ref m_ItemBlocks[i];

            Item* pItemPtr;
            _ = memcpy(&pItemPtr, &ptr, __sizeof<nuint>());

            // Check if pItemPtr is in address range of this block.
            if ((pItemPtr >= block.pItems) && (pItemPtr < block.pItems + block.Capacity))
            {
                ptr->Dispose(); // Explicit destructor call.
                uint index = (uint)(pItemPtr - block.pItems);

                pItemPtr->NextFreeIndex = block.FirstFreeIndex;
                block.FirstFreeIndex = index;

                return;
            }
        }

        D3D12MA_FAIL("Pointer doesn't belong to this memory pool.");
    }

    [return: NativeTypeName("D3D12MA::PoolAllocator<T>::ItemBlock &")]
    internal ref ItemBlock CreateNewBlock()
    {
        uint newBlockCapacity = m_ItemBlocks.empty() ? m_FirstBlockCapacity : (m_ItemBlocks.back().Capacity * 3 / 2);

        ItemBlock newBlock = new ItemBlock() {
            pItems = D3D12MA_NEW_ARRAY<Item>(*m_AllocationCallbacks, newBlockCapacity),
            Capacity = newBlockCapacity,
            FirstFreeIndex = 0,
        };

        m_ItemBlocks.push_back(newBlock);

        // Setup singly-linked list of all free items in this block.
        for (uint i = 0; i < newBlockCapacity - 1; ++i)
        {
            newBlock.pItems[i].NextFreeIndex = i + 1;
        }

        newBlock.pItems[newBlockCapacity - 1].NextFreeIndex = uint.MaxValue;
        return ref m_ItemBlocks.back();
    }

    internal partial struct Item
    {
        [UnscopedRef]
        [NativeTypeName("UINT")]
        public ref uint NextFreeIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // uint.MaxValue means end of list.
                return ref Unsafe.As<T, uint>(ref Value);
            }
        }

        [NativeTypeName("char[sizeof(T)]")]
        public T Value;
    }

    internal partial struct ItemBlock
    {
        public Item* pItems;

        [NativeTypeName("UINT")]
        public uint Capacity;

        [NativeTypeName("UINT")]
        public uint FirstFreeIndex;
    }
}
