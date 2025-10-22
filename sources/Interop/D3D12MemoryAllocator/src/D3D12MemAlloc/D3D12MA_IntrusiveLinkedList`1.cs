// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 3a335d55c99e605775bbe9fe9c01ee6212804bed
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_IntrusiveLinkedList<ItemTypeTraits, ItemType> : IDisposable
    where ItemTypeTraits : unmanaged, D3D12MA_ItemTypeTraits<ItemTypeTraits, ItemType>
    where ItemType : unmanaged
{
    private ItemType* m_Front;

    private ItemType* m_Back;

    private nuint m_Count;

    public static ItemType* GetPrev([NativeTypeName("const ItemType*")] ItemType* item)
    {
        return ItemTypeTraits.GetPrev(item);
    }

    public static ItemType* GetNext([NativeTypeName("const ItemType*")] ItemType* item)
    {
        return ItemTypeTraits.GetNext(item);
    }

    public D3D12MA_IntrusiveLinkedList([NativeTypeName("IntrusiveLinkedList<ItemTypeTraits>&&")] ref D3D12MA_IntrusiveLinkedList<ItemTypeTraits, ItemType>* src)
    {
        m_Front = (*src).m_Front;
        m_Back = (*src).m_Back;
        m_Count = (*src).m_Count;

        (*src).m_Front = (*src).m_Back = null;
        (*src).m_Count = 0;
    }

    public readonly void Dispose()
    {
        D3D12MA_HEAVY_ASSERT(IsEmpty());
    }

    [return: NativeTypeName("size_t")]
    public readonly nuint GetCount()
    {
        return m_Count;
    }

    public readonly bool IsEmpty()
    {
        return m_Count == 0;
    }

    public readonly ItemType* Front()
    {
        return m_Front;
    }

    public readonly ItemType* Back()
    {
        return m_Back;
    }

    public void PushBack(ItemType* item)
    {
        D3D12MA_HEAVY_ASSERT((ItemTypeTraits.GetPrev(item) == null) && (ItemTypeTraits.GetNext(item) == null));

        if (IsEmpty())
        {
            m_Front = item;
            m_Back = item;
            m_Count = 1;
        }
        else
        {
            ItemTypeTraits.AccessPrev(item) = m_Back;
            ItemTypeTraits.AccessNext(m_Back) = item;

            m_Back = item;
            ++m_Count;
        }
    }

    public void PushFront(ItemType* item)
    {
        D3D12MA_HEAVY_ASSERT((ItemTypeTraits.GetPrev(item) == null) && (ItemTypeTraits.GetNext(item) == null));

        if (IsEmpty())
        {
            m_Front = item;
            m_Back = item;
            m_Count = 1;
        }
        else
        {
            ItemTypeTraits.AccessNext(item) = m_Front;
            ItemTypeTraits.AccessPrev(m_Front) = item;

            m_Front = item;
            ++m_Count;
        }
    }

    public ItemType* PopBack()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);

        ItemType* backItem = m_Back;
        ItemType* prevItem = ItemTypeTraits.GetPrev(backItem);

        if (prevItem != null)
        {
            ItemTypeTraits.AccessNext(prevItem) = null;
        }

        m_Back = prevItem;
        --m_Count;

        ItemTypeTraits.AccessPrev(backItem) = null;
        ItemTypeTraits.AccessNext(backItem) = null;

        return backItem;
    }

    public ItemType* PopFront()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);

        ItemType* frontItem = m_Front;
        ItemType* nextItem = ItemTypeTraits.GetNext(frontItem);

        if (nextItem != null)
        {
            ItemTypeTraits.AccessPrev(nextItem) = null;
        }

        m_Front = nextItem;
        --m_Count;

        ItemTypeTraits.AccessPrev(frontItem) = null;
        ItemTypeTraits.AccessNext(frontItem) = null;

        return frontItem;
    }

    // MyItem can be null - it means PushBack.
    public void InsertBefore(ItemType* existingItem, ItemType* newItem)
    {
        D3D12MA_HEAVY_ASSERT((newItem != null) && (ItemTypeTraits.GetPrev(newItem) == null) && (ItemTypeTraits.GetNext(newItem) == null));

        if (existingItem != null)
        {
            ItemType* prevItem = ItemTypeTraits.GetPrev(existingItem);

            ItemTypeTraits.AccessPrev(newItem) = prevItem;
            ItemTypeTraits.AccessNext(newItem) = existingItem;
            ItemTypeTraits.AccessPrev(existingItem) = newItem;

            if (prevItem != null)
            {
                ItemTypeTraits.AccessNext(prevItem) = newItem;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_Front == existingItem);
                m_Front = newItem;
            }

            ++m_Count;
        }
        else
        {
            PushBack(newItem);
        }
    }

    // MyItem can be null - it means PushFront.
    public void InsertAfter(ItemType* existingItem, ItemType* newItem)
    {
        D3D12MA_HEAVY_ASSERT((newItem != null) && (ItemTypeTraits.GetPrev(newItem) == null) && (ItemTypeTraits.GetNext(newItem) == null));

        if (existingItem != null)
        {
            ItemType* nextItem = ItemTypeTraits.GetNext(existingItem);

            ItemTypeTraits.AccessNext(newItem) = nextItem;
            ItemTypeTraits.AccessPrev(newItem) = existingItem;
            ItemTypeTraits.AccessNext(existingItem) = newItem;

            if (nextItem != null)
            {
                ItemTypeTraits.AccessPrev(nextItem) = newItem;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_Back == existingItem);
                m_Back = newItem;
            }

            ++m_Count;
        }
        else
        {
            PushFront(newItem);
        }
    }

    public void Remove(ItemType* item)
    {
        D3D12MA_HEAVY_ASSERT((item != null) && (m_Count > 0));

        if (ItemTypeTraits.GetPrev(item) != null)
        {
            ItemTypeTraits.AccessNext(ItemTypeTraits.AccessPrev(item)) = ItemTypeTraits.GetNext(item);
        }
        else
        {
            D3D12MA_HEAVY_ASSERT(m_Front == item);
            m_Front = ItemTypeTraits.GetNext(item);
        }

        if (ItemTypeTraits.GetNext(item) != null)
        {
            ItemTypeTraits.AccessPrev(ItemTypeTraits.AccessNext(item)) = ItemTypeTraits.GetPrev(item);
        }
        else
        {
            D3D12MA_HEAVY_ASSERT(m_Back == item);
            m_Back = ItemTypeTraits.GetPrev(item);
        }

        ItemTypeTraits.AccessPrev(item) = null;
        ItemTypeTraits.AccessNext(item) = null;

        --m_Count;
    }

    public void RemoveAll()
    {
        if (!IsEmpty())
        {
            ItemType* item = m_Back;

            while (item != null)
            {
                ItemType* prevItem = ItemTypeTraits.AccessPrev(item);

                ItemTypeTraits.AccessPrev(item) = null;
                ItemTypeTraits.AccessNext(item) = null;

                item = prevItem;
            }

            m_Front = null;
            m_Back = null;
            m_Count = 0;
        }
    }
}
