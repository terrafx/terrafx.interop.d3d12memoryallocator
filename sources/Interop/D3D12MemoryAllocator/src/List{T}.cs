// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    internal unsafe partial struct List<T> : IDisposable
        where T : unmanaged
    {
        private readonly ALLOCATION_CALLBACKS* m_AllocationCallbacks;
        private PoolAllocator<Item> m_ItemAllocator;
        private Item* m_pFront;
        private Item* m_pBack;
        [NativeTypeName("size_t")] private nuint m_Count;

        public struct Item : IDisposable
        {
            public Item* pPrev;
            public Item* pNext;
            public T Value;

            public void Dispose() { }
        }

        // allocationCallbacks externally owned, must outlive this object.
        public List(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_ItemAllocator = new(allocationCallbacks, 128);
            m_pFront = null;
            m_pBack = null;
            m_Count = 0;
        }

        public partial void Dispose();
        public partial void Clear();

        [return: NativeTypeName("size_t")]
        public readonly nuint GetCount() { return m_Count; }
        public readonly bool IsEmpty() { return m_Count == 0; }
        public readonly Item* Front() { return m_pFront; }
        public readonly Item* Back() { return m_pBack; }

        public partial Item* PushBack();
        public partial Item* PushFront();
        public partial Item* PushBack(T* value);
        public partial Item* PushFront(T* value);
        public partial void PopBack();
        public partial void PopFront();

        // Item can be null - it means PushBack.
        public partial Item* InsertBefore(Item* pItem);
        // Item can be null - it means PushFront.
        public partial Item* InsertAfter(Item* pItem);

        public partial Item* InsertBefore(Item* pItem, T* value);
        public partial Item* InsertAfter(Item* pItem, T* value);

        public partial void Remove(Item* pItem);

#pragma warning disable CS0660, CS0661
        public struct iterator
#pragma warning restore CS0660, CS0661
        {
            private List<T>* m_pList;
            internal Item* m_pItem;

            public T* op_Arrow()
            {
                D3D12MA_HEAVY_ASSERT(m_pItem != null);
                return &m_pItem->Value;
            }

            public void op_MoveNext()
            {
                D3D12MA_HEAVY_ASSERT(m_pItem != null);
                m_pItem = m_pItem->pNext;
            }

            public void op_MoveBack()
            {
                if (m_pItem != null)
                {
                    m_pItem = m_pItem->pPrev;
                }
                else
                {
                    D3D12MA_HEAVY_ASSERT(!m_pList->IsEmpty());
                    m_pItem = m_pList->Back();
                }
            }

            public static bool operator ==(iterator lhs, iterator rhs)
            {
                D3D12MA_HEAVY_ASSERT(lhs.m_pList == rhs.m_pList);
                return lhs.m_pItem == rhs.m_pItem;
            }

            public static bool operator !=(iterator lhs, iterator rhs)
            {
                D3D12MA_HEAVY_ASSERT(lhs.m_pList == rhs.m_pList);
                return lhs.m_pItem != rhs.m_pItem;
            }

            public iterator(List<T>* pList, Item* pItem)
            {
                m_pList = pList;
                m_pItem = pItem;
            }
        }

        public readonly bool empty() { return IsEmpty(); }

        [return: NativeTypeName("size_t")]
        public readonly nuint size() { return GetCount(); }

        public iterator begin() { return new((List<T>*)Unsafe.AsPointer(ref this), Front()); }
        public iterator end() { return new((List<T>*)Unsafe.AsPointer(ref this), null); }

        public void clear() { Clear(); }
        public void push_back(T* value) { PushBack(value); }
        public void erase(iterator it) { Remove(it.m_pItem); }
        public iterator insert(iterator it, T* value) { return new((List<T>*)Unsafe.AsPointer(ref this), InsertBefore(it.m_pItem, value)); }
    }

    internal unsafe partial struct List<T>
    {
        public partial void Dispose()
        {
            // Intentionally not calling Clear, because that would be unnecessary
            // computations to return all items to m_ItemAllocator as free.
        }

        public partial void Clear()
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

        public partial Item* PushBack()
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

        public partial Item* PushFront()
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

        public partial Item* PushBack(T* value)
        {
            Item* pNewItem = PushBack();
            pNewItem->Value = *value;
            return pNewItem;
        }

        public partial Item* PushFront(T* value)
        {
            Item* pNewItem = PushFront();
            pNewItem->Value = *value;
            return pNewItem;
        }

        public partial void PopBack()
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

        public partial void PopFront()
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

        public partial void Remove(Item* pItem)
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

        public partial Item* InsertBefore(Item* pItem)
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

        public partial Item* InsertAfter(Item* pItem)
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
                return PushFront();
        }

        public partial Item* InsertBefore(Item* pItem, T* value)
        {
            Item* newItem = InsertBefore(pItem);
            newItem->Value = *value;
            return newItem;
        }

        public partial Item* InsertAfter(Item* pItem, T* value)
        {
            Item* newItem = InsertAfter(pItem);
            newItem->Value = *value;
            return newItem;
        }
    }
}
