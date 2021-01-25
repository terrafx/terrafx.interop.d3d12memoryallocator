// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

using size_t = nuint;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class Vector

    /// <summary>
    /// Dynamically resizing continuous array. Class with interface similar to std::vector.
    /// T must be POD because constructors and destructors are not called and memcpy is
    /// used for these objects.
    /// </summary>
    internal unsafe struct Vector<T> : IDisposable
        where T : unmanaged
    {
        // allocationCallbacks externally owned, must outlive this object.
        public Vector(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_pArray = null;
            m_Count = 0;
            m_Capacity = 0;
        }

        public Vector(size_t count, ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_pArray = count > 0 ? AllocateArray<T>(allocationCallbacks, count) : null;
            m_Count = count;
            m_Capacity = count;
        }

        public Vector(in Vector<T> src)
        {
            m_AllocationCallbacks = src.m_AllocationCallbacks;
            m_pArray = src.m_Count > 0 ? AllocateArray<T>(src.m_AllocationCallbacks, src.m_Count) : null;
            m_Count = src.m_Count;
            m_Capacity = src.m_Count;

            if (m_Count > 0)
            {
                memcpy(m_pArray, src.m_pArray, m_Count * (size_t)sizeof(T));
            }
        }

        public void Dispose()
        {
            Free(m_AllocationCallbacks, m_pArray);
        }

        public static void Copy(in Vector<T> rhs, ref Vector<T> lhs)
        {
            if (!Unsafe.AreSame(ref Unsafe.AsRef(rhs), ref lhs))
            {
                lhs.resize(rhs.m_Count);
                if (lhs.m_Count != 0)
                {
                    memcpy(lhs.m_pArray, rhs.m_pArray, lhs.m_Count * (size_t)sizeof(T));
                }
            }
        }

        public bool empty() { return m_Count == 0; }
        public size_t size() { return m_Count; }
        public T* data() { return m_pArray; }

        public T* this[size_t index]
        {
            get
            {
                D3D12MA_HEAVY_ASSERT(index < m_Count);
                return m_pArray + index;
            }
        }

        public T* front()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            return m_pArray;
        }

        public T* back()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            return m_pArray + m_Count - 1;
        }

        public void reserve(size_t newCapacity, bool freeMemory = false)
        {
            newCapacity = D3D12MA_MAX(newCapacity, m_Count);

            if ((newCapacity < m_Capacity) && !freeMemory)
            {
                newCapacity = m_Capacity;
            }

            if (newCapacity != m_Capacity)
            {
                T* newArray = newCapacity > 0 ? AllocateArray<T>(m_AllocationCallbacks, newCapacity) : null;
                if (m_Count != 0)
                {
                    memcpy(newArray, m_pArray, m_Count * (size_t)sizeof(T));
                }
                Free(m_AllocationCallbacks, m_pArray);
                m_Capacity = newCapacity;
                m_pArray = newArray;
            }
        }

        public void resize(size_t newCount, bool freeMemory = false)
        {
            size_t newCapacity = m_Capacity;
            if (newCount > m_Capacity)
            {
                newCapacity = D3D12MA_MAX(newCount, D3D12MA_MAX(m_Capacity * 3 / 2, (size_t)8));
            }
            else if (freeMemory)
            {
                newCapacity = newCount;
            }

            if (newCapacity != m_Capacity)
            {
                T* newArray = newCapacity > 0 ? AllocateArray<T>(m_AllocationCallbacks, newCapacity) : null;
                size_t elementsToCopy = D3D12MA_MIN(m_Count, newCount);
                if (elementsToCopy != 0)
                {
                    memcpy(newArray, m_pArray, elementsToCopy * (size_t)sizeof(T));
                }
                Free(m_AllocationCallbacks, m_pArray);
                m_Capacity = newCapacity;
                m_pArray = newArray;
            }

            m_Count = newCount;
        }

        public void clear(bool freeMemory = false)
        {
            resize(0, freeMemory);
        }

        public void insert(size_t index, T* src)
        {
            D3D12MA_HEAVY_ASSERT(index <= m_Count);
            size_t oldCount = size();
            resize(oldCount + 1);
            if (index < oldCount)
            {
                memcpy(m_pArray + (index + 1), m_pArray + index, (oldCount - index) * (size_t)sizeof(T));
            }
            m_pArray[index] = *src;
        }

        public void remove(size_t index)
        {
            D3D12MA_HEAVY_ASSERT(index < m_Count);
            size_t oldCount = size();
            if (index < oldCount - 1)
            {
                memcpy(m_pArray + index, m_pArray + (index + 1), (oldCount - index - 1) * (size_t)sizeof(T));
            }
            resize(oldCount - 1);
        }

        public void push_back(T* src)
        {
            size_t newIndex = size();
            resize(newIndex + 1);
            m_pArray[newIndex] = *src;
        }

        public void pop_back()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            resize(size() - 1);
        }

        public void push_front(T* src)
        {
            insert(0, src);
        }

        public void pop_front()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            remove(0);
        }

        public T* begin() { return m_pArray; }
        public T* end() { return m_pArray + m_Count; }

        public size_t InsertSorted<CmpLess>(T* value, in CmpLess cmp)
            where CmpLess : struct, ICmp<T>
        {
            size_t indexToInsert = (size_t)(BinaryFindFirstNotLess(
                m_pArray,
                m_pArray + m_Count,
                value,
                cmp) - m_pArray);
            insert(indexToInsert, value);
            return indexToInsert;
        }

        public bool RemoveSorted<CmpLess>(T* value, in CmpLess cmp)
            where CmpLess : struct, ICmp<T>
        {
            T* it = BinaryFindFirstNotLess(
                m_pArray,
                m_pArray + m_Count,
                value,
                cmp);
            if ((it != end()) && !cmp.Invoke(it, value) && !cmp.Invoke(value, it))
            {
                size_t indexToRemove = (size_t)(it - begin());
                remove(indexToRemove);
                return true;
            }
            return false;
        }

        private readonly ALLOCATION_CALLBACKS* m_AllocationCallbacks;
        private T* m_pArray;
        private size_t m_Count;
        private size_t m_Capacity;
    }
}
