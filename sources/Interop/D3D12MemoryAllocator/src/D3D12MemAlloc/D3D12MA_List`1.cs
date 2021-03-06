// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_List<T> : IDisposable
        where T : unmanaged, IDisposable
    {
        private D3D12MA_ALLOCATION_CALLBACKS* m_AllocationCallbacks;

        private D3D12MA_PoolAllocator<Item> m_ItemAllocator;

        internal Item* m_pFront;

        private Item* m_pBack;

        [NativeTypeName("size_t")]
        private nuint m_Count;

        // allocationCallbacks externally owned, must outlive this object.
        public static void _ctor(ref D3D12MA_List<T> pThis, [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            pThis.m_AllocationCallbacks = allocationCallbacks;
            D3D12MA_PoolAllocator<Item>._ctor(ref pThis.m_ItemAllocator, allocationCallbacks, 128);
            pThis.m_pFront = null;
            pThis.m_pBack = null;
            pThis.m_Count = 0;
        }

        public void Dispose()
        {
            // Intentionally not calling Clear, because that would be unnecessary
            // computations to return all items to m_ItemAllocator as free.

            m_ItemAllocator.Dispose();
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

        [return: NativeTypeName("size_t")]
        public readonly nuint GetCount() => m_Count;

        public readonly bool IsEmpty() => m_Count == 0;

        public readonly Item* Front() => m_pFront;

        public readonly Item* Back() => m_pBack;

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

        public Item* PushBack([NativeTypeName("const T&")] in T value)
        {
            Item* pNewItem = PushBack();
            pNewItem->Value = value;
            return pNewItem;
        }

        public Item* PushFront([NativeTypeName("const T&")] in T value)
        {
            Item* pNewItem = PushFront();
            pNewItem->Value = value;
            return pNewItem;
        }

        public void PopBack()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));

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
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));

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
                    D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_pFront == pItem));
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
                    D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_pBack == pItem));
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

        public Item* InsertBefore(Item* pItem, [NativeTypeName("const T&")] in T value)
        {
            Item* newItem = InsertBefore(pItem);
            newItem->Value = value;
            return newItem;
        }

        public Item* InsertAfter(Item* pItem, [NativeTypeName("const T&")] in T value)
        {
            Item* newItem = InsertAfter(pItem);
            newItem->Value = value;
            return newItem;
        }

        public void Remove(Item* pItem)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (pItem != null));
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));

            if (pItem->pPrev != null)
            {
                pItem->pPrev->pNext = pItem->pNext;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_pFront == pItem));
                m_pFront = pItem->pNext;
            }

            if (pItem->pNext != null)
            {
                pItem->pNext->pPrev = pItem->pPrev;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_pBack == pItem));
                m_pBack = pItem->pPrev;
            }

            m_ItemAllocator.Free(pItem);
            --m_Count;
        }

        public struct Item : IDisposable
        {
            public Item* pPrev;

            public Item* pNext;

            public T Value;

            public void Dispose() { }
        }

#pragma warning disable CS0660, CS0661
        public struct iterator
        {
            private D3D12MA_List<T>* m_pList;

            internal Item* m_pItem;

            public readonly T* Get()
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_pItem != null));
                return &m_pItem->Value;
            }

            public readonly iterator MoveNext()
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_pItem != null));
                return new iterator {
                    m_pList = m_pList,
                    m_pItem = m_pItem->pNext
                };
            }

            public readonly iterator MoveBack()
            {
                var iterator = new iterator {
                    m_pList = m_pList
                };

                if (m_pItem != null)
                {
                    iterator.m_pItem = m_pItem->pPrev;
                }
                else
                {
                    D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (!m_pList->IsEmpty()));
                    iterator.m_pItem = m_pList->Back();
                }

                return iterator;
            }

            public static bool operator ==([NativeTypeName("const iterator&")] in iterator lhs, [NativeTypeName("const iterator&")] in iterator rhs)
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (lhs.m_pList == rhs.m_pList));
                return lhs.m_pItem == rhs.m_pItem;
            }

            public static bool operator !=([NativeTypeName("const iterator&")] in iterator lhs, [NativeTypeName("const iterator&")] in iterator rhs)
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (lhs.m_pList == rhs.m_pList));
                return lhs.m_pItem != rhs.m_pItem;
            }

            internal iterator(in D3D12MA_List<T> pList, Item* pItem)
                : this((D3D12MA_List<T>*)Unsafe.AsPointer(ref Unsafe.AsRef(in pList)), pItem)
            {
            }

            internal iterator(D3D12MA_List<T>* pList, Item* pItem)
            {
                m_pList = pList;
                m_pItem = pItem;
            }
        }
#pragma warning restore CS0660, CS0661

        public readonly bool empty() => IsEmpty();

        [return: NativeTypeName("size_t")]
        public readonly nuint size() => GetCount();

        public readonly iterator begin() => new iterator(in this, Front());

        public readonly iterator end() => new iterator(in this, null);

        public void clear() => Clear();

        public void push_back([NativeTypeName("const T&")] in T value) => PushBack(in value);

        public void erase(iterator it) => Remove(it.m_pItem);

        public iterator insert(iterator it, [NativeTypeName("const T&")] in T value)
            => new iterator(in this, InsertBefore(it.m_pItem, value));
    }
}
