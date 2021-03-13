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
    internal unsafe struct D3D12MA_IntrusiveLinkedList<TItemType> : IDisposable
        where TItemType : unmanaged, D3D12MA_IItemTypeTraits<TItemType>
    {
        private TItemType* m_Front;
        private TItemType* m_Back;
        private nuint m_Count;

        public static TItemType* GetPrev([NativeTypeName("const ItemType*")] TItemType* item) => item->GetPrev();

        public static TItemType* GetNext([NativeTypeName("const ItemType*")] TItemType* item) => item->GetNext();

        public static void _ctor(ref D3D12MA_IntrusiveLinkedList<TItemType> pThis)
        {
            pThis.m_Front = null;
            pThis.m_Back = null;
            pThis.m_Count = 0;
        }

        public static void _ctor(ref D3D12MA_IntrusiveLinkedList<TItemType> pThis, [NativeTypeName("IntrusiveLinkedList<ItemTypeTraits>&&")] D3D12MA_IntrusiveLinkedList<TItemType>** src)
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
        public D3D12MA_IntrusiveLinkedList<TItemType>* Assign([NativeTypeName("IntrusiveLinkedList<ItemTypeTraits>&&")] D3D12MA_IntrusiveLinkedList<TItemType>** src)
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

            return (D3D12MA_IntrusiveLinkedList<TItemType>*)Unsafe.AsPointer(ref this);
        }

        public void RemoveAll()
        {
            if (!IsEmpty())
            {
                TItemType* item = m_Back;
                while (item != null)
                {
                    TItemType* prevItem = item->GetPrev();
                    *item->AccessPrev() = null;
                    *item->AccessNext() = null;
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
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (item->GetPrev() == null) && (item->GetNext() == null));
            if (IsEmpty())
            {
                m_Front = item;
                m_Back = item;
                m_Count = 1;
            }
            else
            {
                *item->AccessPrev() = m_Back;
                *m_Back->AccessNext() = item;
                m_Back = item;
                ++m_Count;
            }
        }

        public void PushFront(TItemType* item)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (item->GetPrev() == null) && (item->GetNext() == null));
            if (IsEmpty())
            {
                m_Front = item;
                m_Back = item;
                m_Count = 1;
            }
            else
            {
                *item->AccessNext() = m_Front;
                *m_Front->AccessPrev() = item;
                m_Front = item;
                ++m_Count;
            }
        }

        public TItemType* PopBack()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            TItemType* backItem = m_Back;
            TItemType* prevItem = backItem->GetPrev();
            if (prevItem != null)
            {
                *prevItem->AccessNext() = null;
            }
            m_Back = prevItem;
            --m_Count;
            *backItem->AccessPrev() = null;
            *backItem->AccessNext() = null;
            return backItem;
        }

        public TItemType* PopFront()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            TItemType* frontItem = m_Front;
            TItemType* nextItem = frontItem->GetNext();
            if (nextItem != null)
            {
                *nextItem->AccessPrev() = null;
            }
            m_Front = nextItem;
            --m_Count;
            *frontItem->AccessPrev() = null;
            *frontItem->AccessNext() = null;
            return frontItem;
        }

        // MyItem can be null - it means PushBack.
        public void InsertBefore(TItemType* existingItem, TItemType* newItem)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (newItem != null) && (newItem->GetPrev() == null) && (newItem->GetNext() == null));
            if (existingItem != null)
            {
                TItemType* prevItem = existingItem->GetPrev();
                *newItem->AccessPrev() = prevItem;
                *newItem->AccessNext() = existingItem;
                *existingItem->AccessPrev() = newItem;
                if (prevItem != null)
                {
                    *prevItem->AccessNext() = newItem;
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
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (newItem != null) && (newItem->GetPrev() == null) && (newItem->GetNext() == null));
            if (existingItem != null)
            {
                TItemType* nextItem = existingItem->GetNext();
                *newItem->AccessNext() = nextItem;
                *newItem->AccessPrev() = existingItem;
                *existingItem->AccessNext() = newItem;
                if (nextItem != null)
                {
                    *nextItem->AccessPrev() = newItem;
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
            if (item->GetPrev() != null)
            {
                *item->GetPrev()->AccessNext() = item->GetNext();
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_Front == item);
                m_Front = item->GetNext();
            }

            if (item->GetNext() != null)
            {
                *item->GetNext()->AccessPrev() = item->GetPrev();
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_Back == item);
                m_Back = item->GetPrev();
            }
            *item->AccessPrev() = null;
            *item->AccessNext() = null;
            --m_Count;
        }
    }
}
