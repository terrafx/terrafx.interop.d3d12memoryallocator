// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Doubly linked list, with elements allocated out of PoolAllocator. Has custom interface, as well as STL-style interface, including iterator and const_iterator.</summary>
/// <typeparam name="T"></typeparam>
internal unsafe partial struct D3D12MA_List<T> : IDisposable
    where T : unmanaged, IDisposable
{
    [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")]
    private readonly D3D12MA_ALLOCATION_CALLBACKS* m_AllocationCallbacks;

    private D3D12MA_PoolAllocator<Item> m_ItemAllocator;

    private Item* m_pFront;

    private Item* m_pBack;

    [NativeTypeName("size_t")]
    private nuint m_Count;

    // allocationCallbacks externally owned, must outlive this object.
    public D3D12MA_List([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks)
    {
        m_AllocationCallbacks = (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(in allocationCallbacks));
        m_ItemAllocator = new D3D12MA_PoolAllocator<Item>(allocationCallbacks, 128);
    }

    // Intentionally not calling Clear, because that would be unnecessary
    // computations to return all items to m_ItemAllocator as free.
    public void Dispose()
    {
        m_ItemAllocator.Dispose();
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
    
    public readonly Item* Front()
    {
        return m_pFront;
    }

    public readonly Item* Back()
    {
        return m_pBack;
    }

    public readonly bool empty()
    {
        return IsEmpty();
    }

    [return: NativeTypeName("size_t")]
    public readonly nuint size()
    {
        return GetCount();
    }

    public void push_back([NativeTypeName("const T &")] in T value)
    {
        _ = PushBack(value);
    }

    public iterator insert(iterator it, [NativeTypeName("const T &")] in T value)
    {
        return new iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref this)), InsertBefore(it.m_pItem, value));
    }

    public void clear()
    {
        Clear();
    }

    public void erase(iterator it)
    {
        Remove(it.m_pItem);
    }
    
    public iterator begin()
    {
        return new iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref this)), Front());
    }

    public iterator end()
    {
        return new iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref this)), null);
    }

    public reverse_iterator rbegin()
    {
        return new reverse_iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref this)), Back());
    }

    public reverse_iterator rend()
    {
        return new reverse_iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref this)), null);
    }
    
    public readonly const_iterator cbegin()
    {
        return new const_iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref Unsafe.AsRef(in this))), Front());
    }

    public readonly const_iterator cend()
    {
        return new const_iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref Unsafe.AsRef(in this))), null);
    }

    public readonly const_reverse_iterator crbegin()
    {
        return new const_reverse_iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref Unsafe.AsRef(in this))), Back());
    }

    public readonly const_reverse_iterator crend()
    {
        return new const_reverse_iterator((D3D12MA_List<T>*)(Unsafe.AsPointer(ref Unsafe.AsRef(in this))), null);
    }

    public Item* PushBack()
    {
        Item* pNewItem = m_ItemAllocator.Alloc();
        pNewItem->pNext = null;

        if (IsEmpty())
        {
            pNewItem->pPrev = null;
            m_pFront = pNewItem;
            m_pBack = pNewItem;
            m_Count = 1;
        }
        else
        {
            pNewItem->pPrev = m_pBack;
            m_pBack->pNext = pNewItem;
            m_pBack = pNewItem;
            ++m_Count;
        }

        return pNewItem;
    }

    public Item* PushFront()
    {
        Item* pNewItem = m_ItemAllocator.Alloc();
        pNewItem->pPrev = null;

        if (IsEmpty())
        {
            pNewItem->pNext = null;
            m_pFront = pNewItem;
            m_pBack = pNewItem;
            m_Count = 1;
        }
        else
        {
            pNewItem->pNext = m_pFront;
            m_pFront->pPrev = pNewItem;
            m_pFront = pNewItem;
            ++m_Count;
        }

        return pNewItem;
    }

    public Item* PushBack([NativeTypeName("const T &")] in T value)
    {
        Item* pNewItem = PushBack();
        pNewItem->Value = value;
        return pNewItem;
    }

    public Item* PushFront([NativeTypeName("const T &")] in T value)
    {
        Item* pNewItem = PushFront();
        pNewItem->Value = value;
        return pNewItem;
    }

    public void PopBack()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);

        Item* pBackItem = m_pBack;
        Item* pPrevItem = pBackItem->pPrev;

        if (pPrevItem != null)
        {
            pPrevItem->pNext = null;
        }

        m_pBack = pPrevItem;
        m_ItemAllocator.Free(pBackItem);
        --m_Count;
    }

    public void PopFront()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);

        Item* pFrontItem = m_pFront;
        Item* pNextItem = pFrontItem->pNext;

        if (pNextItem != null)
        {
            pNextItem->pPrev = null;
        }

        m_pFront = pNextItem;
        m_ItemAllocator.Free(pFrontItem);
        --m_Count;
    }


    // Item can be null - it means PushBack.
    public Item* InsertBefore(Item* pItem)
    {
        if (pItem != null)
        {
            Item* prevItem = pItem->pPrev;
            Item* newItem = m_ItemAllocator.Alloc();

            newItem->pPrev = prevItem;
            newItem->pNext = pItem;
            pItem->pPrev = newItem;

            if (prevItem != null)
            {
                prevItem->pNext = newItem;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_pFront == pItem);
                m_pFront = newItem;
            }

            ++m_Count;
            return newItem;
        }
        else
        {
            return PushBack();
        }
    }

    // Item can be null - it means PushFront.
    public Item* InsertAfter(Item* pItem)
    {
        if (pItem != null)
        {
            Item* nextItem = pItem->pNext;
            Item* newItem = m_ItemAllocator.Alloc();

            newItem->pNext = nextItem;
            newItem->pPrev = pItem;
            pItem->pNext = newItem;

            if (nextItem != null)
            {
                nextItem->pPrev = newItem;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_pBack == pItem);
                m_pBack = newItem;
            }

            ++m_Count;
            return newItem;
        }
        else
        {
            return PushFront();
        }
    }

    public Item* InsertBefore(Item* pItem, [NativeTypeName("const T &")] in T value)
    {
        Item* newItem = InsertBefore(pItem);
        newItem->Value = value;
        return newItem;
    }

    public Item* InsertAfter(Item* pItem, [NativeTypeName("const T &")] in T value)
    {
        Item* newItem = InsertAfter(pItem);
        newItem->Value = value;
        return newItem;
    }

    public void Clear()
    {
        if (!IsEmpty())
        {
            Item* pItem = m_pBack;

            while (pItem != null)
            {
                Item* pPrevItem = pItem->pPrev;
                m_ItemAllocator.Free(pItem);
                pItem = pPrevItem;
            }

            m_pFront = null;
            m_pBack = null;
            m_Count = 0;
        }
    }

    public void Remove(Item* pItem)
    {
        D3D12MA_HEAVY_ASSERT(pItem != null);
        D3D12MA_HEAVY_ASSERT(m_Count > 0);

        if (pItem->pPrev != null)
        {
            pItem->pPrev->pNext = pItem->pNext;
        }
        else
        {
            D3D12MA_HEAVY_ASSERT(m_pFront == pItem);
            m_pFront = pItem->pNext;
        }

        if (pItem->pNext != null)
        {
            pItem->pNext->pPrev = pItem->pPrev;
        }
        else
        {
            D3D12MA_HEAVY_ASSERT(m_pBack == pItem);
            m_pBack = pItem->pPrev;
        }

        m_ItemAllocator.Free(pItem);
        --m_Count;
    }

    public partial struct Item : IDisposable
    {
        public Item* pPrev;

        public Item* pNext;

        public T Value;

        internal void _ctor()
        {
            pPrev = null;
            pNext = null;
            Value = default;
        }

        public void Dispose()
        {
            Value.Dispose();
        }
    }

    public partial struct @iterator
    {
        internal D3D12MA_List<T>* m_pList;

        internal Item* m_pItem;

        public iterator([NativeTypeName("const reverse_iterator &")] in reverse_iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        internal iterator(D3D12MA_List<T>* pList, Item* pItem)
        {
            m_pList = pList;
            m_pItem = pItem;
        }

        // T& operator*() const;
        // T* operator->() const;
        // 
        // iterator& operator++();
        // iterator& operator--();
        // iterator operator ++(int);
        // iterator operator --(int);
        // 
        // bool operator ==(const iterator& rhs) const;
        // bool operator !=(const iterator& rhs) const;
    }

    public partial struct reverse_iterator
    {
        internal D3D12MA_List<T>* m_pList;

        internal Item* m_pItem;

        public reverse_iterator([NativeTypeName("const iterator &")] in iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        internal reverse_iterator(D3D12MA_List<T>* pList, Item* pItem)
        {
            m_pList = pList;
            m_pItem = pItem;
        }

        // T& operator*() const;
        // T* operator->() const;
        // 
        // reverse_iterator& operator++();
        // reverse_iterator& operator--();
        // reverse_iterator operator ++(int);
        // reverse_iterator operator --(int);
        // 
        // bool operator ==(const reverse_iterator& rhs) const;
        // bool operator !=(const reverse_iterator& rhs) const;
    }

    public partial struct const_iterator
    {
        internal D3D12MA_List<T>* m_pList;

        internal Item* m_pItem;

        public const_iterator([NativeTypeName("const iterator &")] in iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        public const_iterator([NativeTypeName("const reverse_iterator &")] in reverse_iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        public const_iterator([NativeTypeName("const const_reverse_iterator &")] in const_reverse_iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        internal const_iterator([NativeTypeName("const D3D12MA::List<T> *")] D3D12MA_List<T>* pList, [NativeTypeName("const Item *")] Item* pItem)
        {
            m_pList = pList;
            m_pItem = pItem;
        }

        // iterator dropConst() const;
        // const T& operator*() const;
        // const T* operator->() const;
        // 
        // const_iterator& operator++();
        // const_iterator& operator--();
        // const_iterator operator ++(int);
        // const_iterator operator --(int);
        // 
        // bool operator ==(const const_iterator& rhs) const;
        // bool operator !=(const const_iterator& rhs) const;

    }

    public partial struct const_reverse_iterator
    {
        internal D3D12MA_List<T>* m_pList;

        internal Item* m_pItem;

        public const_reverse_iterator([NativeTypeName("const iterator &")] in iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        public const_reverse_iterator([NativeTypeName("const reverse_iterator &")] in reverse_iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        public const_reverse_iterator([NativeTypeName("const const_iterator &")] in const_iterator src)
        {
            m_pList = src.m_pList;
            m_pItem = src.m_pItem;
        }

        internal const_reverse_iterator([NativeTypeName("const D3D12MA::List<T> *")] D3D12MA_List<T>* pList, [NativeTypeName("const Item *")] Item* pItem)
        {
            m_pList = pList;
            m_pItem = pItem;
        }

        // reverse_iterator dropConst() const;
        // const T& operator*() const;
        // const T* operator->() const;
        // 
        // const_reverse_iterator& operator++();
        // const_reverse_iterator& operator--();
        // const_reverse_iterator operator ++(int);
        // const_reverse_iterator operator --(int);
        // 
        // bool operator ==(const const_reverse_iterator& rhs) const;
        // bool operator !=(const const_reverse_iterator& rhs) const;
    }
}
