// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 3a335d55c99e605775bbe9fe9c01ee6212804bed
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class IntrusiveLinkedList

    /*
    Expected interface of ItemTypeTraits:
    struct MyItemTypeTraits
    {
        typedef MyItem ItemType;
        static ItemType* GetPrev(const ItemType* item) { return item->myPrevPtr; }
        static ItemType* GetNext(const ItemType* item) { return item->myNextPtr; }
        static ItemType*& AccessPrev(ItemType* item) { return item->myPrevPtr; }
        static ItemType*& AccessNext(ItemType* item) { return item->myNextPtr; }
    };
    */
    internal unsafe struct D3D12MA_IntrusiveLinkedList<TItemType, TItemTypeTraits> : IDisposable
        where TItemType : unmanaged
        where TItemTypeTraits : unmanaged, D3D12MA_IItemTypeTraits<TItemType>
    {
        private TItemType* m_Front;
        private TItemType* m_Back;
        private nuint m_Count;

        public static TItemType* GetPrev([NativeTypeName("const ItemType*")] TItemType* item)
        {
            return default(TItemTypeTraits).GetPrev(item);
        }

        public static TItemType* GetNext([NativeTypeName("const ItemType*")] TItemType* item)
        {
            return default(TItemTypeTraits).GetNext(item);
        }

        public static void _ctor(ref D3D12MA_IntrusiveLinkedList<TItemType, TItemTypeTraits> pThis)
        {
            pThis.m_Front = null;
            pThis.m_Back = null;
            pThis.m_Count = 0;
        }

        public static void _ctor(ref D3D12MA_IntrusiveLinkedList<TItemType, TItemTypeTraits> pThis, [NativeTypeName("IntrusiveLinkedList<ItemTypeTraits>&&")] D3D12MA_IntrusiveLinkedList<TItemType, TItemTypeTraits>** src)
        {
            pThis.m_Front = (*src)->m_Front;
            pThis.m_Back = (*src)->m_Back;
            pThis.m_Count = (*src)->m_Count;

            (*src)->m_Front = (*src)->m_Back = null;
            (*src)->m_Count = 0;
        }

        public void Dispose()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (IsEmpty()));
        }

        [return: NativeTypeName("IntrusiveLinkedList<ItemTypeTraits>&")]
        public D3D12MA_IntrusiveLinkedList<TItemType, TItemTypeTraits>* op_Assignment([NativeTypeName("IntrusiveLinkedList<ItemTypeTraits>&&")] D3D12MA_IntrusiveLinkedList<TItemType, TItemTypeTraits>** src)
        {
            if (*src != Unsafe.AsPointer(ref this))
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (IsEmpty()));
                m_Front = (*src)->m_Front;
                m_Back = (*src)->m_Back;
                m_Count = (*src)->m_Count;
                (*src)->m_Front = (*src)->m_Back = null;
                (*src)->m_Count = 0;
            }

            return (D3D12MA_IntrusiveLinkedList<TItemType, TItemTypeTraits>*)Unsafe.AsPointer(ref this);
        }

        public void RemoveAll()
        {
            if (!IsEmpty())
            {
                TItemType* item = m_Back;
                while (item != null)
                {
                    TItemType* prevItem = *default(TItemTypeTraits).AccessPrev(item);
                    *default(TItemTypeTraits).AccessPrev(item) = null;
                    *default(TItemTypeTraits).AccessNext(item) = null;
                    item = prevItem;
                }
                m_Front = null;
                m_Back = null;
                m_Count = 0;
            }
        }

        [return: NativeTypeName("size_t")]
        public readonly nuint GetCount() => m_Count;

        public readonly bool IsEmpty() => m_Count == 0;

        public readonly TItemType* Front() => m_Front;

        public readonly TItemType* Back() => m_Back;

        public void PushBack(TItemType* item)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (default(TItemTypeTraits).GetPrev(item) == null) && (default(TItemTypeTraits).GetNext(item) == null));
            if (IsEmpty())
            {
                m_Front = item;
                m_Back = item;
                m_Count = 1;
            }
            else
            {
                *default(TItemTypeTraits).AccessPrev(item) = m_Back;
                *default(TItemTypeTraits).AccessNext(m_Back) = item;
                m_Back = item;
                ++m_Count;
            }
        }

        public void PushFront(TItemType* item)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (default(TItemTypeTraits).GetPrev(item) == null) && (default(TItemTypeTraits).GetNext(item) == null));
            if (IsEmpty())
            {
                m_Front = item;
                m_Back = item;
                m_Count = 1;
            }
            else
            {
                *default(TItemTypeTraits).AccessNext(item) = m_Front;
                *default(TItemTypeTraits).AccessPrev(m_Front) = item;
                m_Front = item;
                ++m_Count;
            }
        }

        public TItemType* PopBack()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            TItemType* backItem = m_Back;
            TItemType* prevItem = default(TItemTypeTraits).GetPrev(backItem);
            if (prevItem != null)
            {
                *default(TItemTypeTraits).AccessNext(prevItem) = null;
            }
            m_Back = prevItem;
            --m_Count;
            *default(TItemTypeTraits).AccessPrev(backItem) = null;
            *default(TItemTypeTraits).AccessNext(backItem) = null;
            return backItem;
        }

        public TItemType* PopFront()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            TItemType* frontItem = m_Front;
            TItemType* nextItem = default(TItemTypeTraits).GetNext(frontItem);
            if (nextItem != null)
            {
                *default(TItemTypeTraits).AccessPrev(nextItem) = null;
            }
            m_Front = nextItem;
            --m_Count;
            *default(TItemTypeTraits).AccessPrev(frontItem) = null;
            *default(TItemTypeTraits).AccessNext(frontItem) = null;
            return frontItem;
        }

        // MyItem can be null - it means PushBack.
        public void InsertBefore(TItemType* existingItem, TItemType* newItem)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (newItem != null) && (default(TItemTypeTraits).GetPrev(newItem) == null) && (default(TItemTypeTraits).GetNext(newItem) == null));
            if (existingItem != null)
            {
                TItemType* prevItem = default(TItemTypeTraits).GetPrev(existingItem);
                *default(TItemTypeTraits).AccessPrev(newItem) = prevItem;
                *default(TItemTypeTraits).AccessNext(newItem) = existingItem;
                *default(TItemTypeTraits).AccessPrev(existingItem) = newItem;
                if (prevItem != null)
                {
                    *default(TItemTypeTraits).AccessNext(prevItem) = newItem;
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
        public void InsertAfter(TItemType* existingItem, TItemType* newItem)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (newItem != null) && (default(TItemTypeTraits).GetPrev(newItem) == null) && (default(TItemTypeTraits).GetNext(newItem) == null));
            if (existingItem != null)
            {
                TItemType* nextItem = default(TItemTypeTraits).GetNext(existingItem);
                *default(TItemTypeTraits).AccessNext(newItem) = nextItem;
                *default(TItemTypeTraits).AccessPrev(newItem) = existingItem;
                *default(TItemTypeTraits).AccessNext(existingItem) = newItem;
                if (nextItem != null)
                {
                    *default(TItemTypeTraits).AccessPrev(nextItem) = newItem;
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

        public void Remove(TItemType* item)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (item != null) && (m_Count > 0));
            if (default(TItemTypeTraits).GetPrev(item) != null)
            {
                *default(TItemTypeTraits).AccessNext(*default(TItemTypeTraits).AccessPrev(item)) = default(TItemTypeTraits).GetNext(item);
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_Front == item);
                m_Front = default(TItemTypeTraits).GetNext(item);
            }

            if (default(TItemTypeTraits).GetNext(item) != null)
            {
                *default(TItemTypeTraits).AccessPrev(*default(TItemTypeTraits).AccessNext(item)) = default(TItemTypeTraits).GetPrev(item);
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_Back == item);
                m_Back = default(TItemTypeTraits).GetPrev(item);
            }
            *default(TItemTypeTraits).AccessPrev(item) = null;
            *default(TItemTypeTraits).AccessNext(item) = null;
            --m_Count;
        }
    }
}
