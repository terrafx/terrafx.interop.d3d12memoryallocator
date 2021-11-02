// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    /// <summary>Dynamically resizing continuous array. Class with interface similar to <c>std::vector</c>.</summary>
    /// <typeparam name="T">Must be POD because constructors and destructors are not called and <see cref="memcpy"/> is used for these objects.</typeparam>
    internal unsafe struct D3D12MA_Vector<T> : IDisposable
        where T : unmanaged
    {
        private D3D12MA_ALLOCATION_CALLBACKS* m_AllocationCallbacks;

        internal T* m_pArray;

        [NativeTypeName("size_t")]
        private nuint m_Count;

        [NativeTypeName("size_t")]
        private nuint m_Capacity;

        // allocationCallbacks externally owned, must outlive this object.
        public static void _ctor(ref D3D12MA_Vector<T> pThis, [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            pThis.m_AllocationCallbacks = allocationCallbacks;
            pThis.m_pArray = null;
            pThis.m_Count = 0;
            pThis.m_Capacity = 0;
        }

        public static void _ctor(ref D3D12MA_Vector<T> pThis, [NativeTypeName("size_t")] nuint count, [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            pThis.m_AllocationCallbacks = allocationCallbacks;
            pThis.m_pArray = (count != 0) ? AllocateArray<T>(allocationCallbacks, count) : null;
            pThis.m_Count = count;
            pThis.m_Capacity = count;
        }

        public static void _ctor(ref D3D12MA_Vector<T> pThis, [NativeTypeName("const D3D12MA_Vector<T>&")] in D3D12MA_Vector<T> src)
        {
            pThis.m_AllocationCallbacks = src.m_AllocationCallbacks;
            pThis.m_pArray = (src.m_Count != 0) ? AllocateArray<T>(src.m_AllocationCallbacks, src.m_Count) : null;
            pThis.m_Count = src.m_Count;
            pThis.m_Capacity = src.m_Count;

            if (pThis.m_Count > 0)
            {
                _ = memcpy(pThis.m_pArray, src.m_pArray, pThis.m_Count * (nuint)sizeof(T));
            }
        }

        public void Dispose()
        {
            Free(m_AllocationCallbacks, m_pArray);
        }

        public static ref D3D12MA_Vector<T> Assign(ref D3D12MA_Vector<T> lhs, in D3D12MA_Vector<T> rhs)
        {
            if (!Unsafe.AreSame(ref Unsafe.AsRef(rhs), ref lhs))
            {
                lhs.resize(rhs.m_Count);

                if (lhs.m_Count != 0)
                {
                    _ = memcpy(lhs.m_pArray, rhs.m_pArray, lhs.m_Count * (nuint)sizeof(T));
                }
            }

            return ref lhs;
        }

        public readonly bool empty() => m_Count == 0;

        [return: NativeTypeName("size_t")]
        public readonly nuint size() => m_Count;

        public readonly T* data() => m_pArray;

        public readonly T* this[[NativeTypeName("size_t")] nuint index]
        {
            get
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (index < m_Count));
                return m_pArray + index;
            }
        }

        public readonly T* front()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            return m_pArray;
        }

        public readonly T* back()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            return m_pArray + m_Count - 1;
        }

        public void reserve([NativeTypeName("size_t")] nuint newCapacity, bool freeMemory = false)
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
                    _ = memcpy(newArray, m_pArray, m_Count * (nuint)sizeof(T));
                }

                Free(m_AllocationCallbacks, m_pArray);

                m_Capacity = newCapacity;
                m_pArray = newArray;
            }
        }

        public void resize([NativeTypeName("size_t")] nuint newCount, bool freeMemory = false)
        {
            nuint newCapacity = m_Capacity;

            if (newCount > m_Capacity)
            {
                newCapacity = D3D12MA_MAX(newCount, D3D12MA_MAX(m_Capacity * 3 / 2, (nuint)8));
            }
            else if (freeMemory)
            {
                newCapacity = newCount;
            }

            if (newCapacity != m_Capacity)
            {
                T* newArray = newCapacity > 0 ? AllocateArray<T>(m_AllocationCallbacks, newCapacity) : null;
                nuint elementsToCopy = D3D12MA_MIN(m_Count, newCount);

                if (elementsToCopy != 0)
                {
                    _ = memcpy(newArray, m_pArray, elementsToCopy * (nuint)sizeof(T));
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

        public void insert([NativeTypeName("size_t")] nuint index, [NativeTypeName("const T&")] in T src)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (index <= m_Count));

            nuint oldCount = size();
            resize(oldCount + 1);

            if (index < oldCount)
            {
                _ = memcpy(m_pArray + (index + 1), m_pArray + index, (oldCount - index) * (nuint)sizeof(T));
            }

            m_pArray[index] = src;
        }

        public void remove([NativeTypeName("size_t")] nuint index)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (index < m_Count));

            nuint oldCount = size();

            if (index < oldCount - 1)
            {
                _ = memcpy(m_pArray + index, m_pArray + (index + 1), (oldCount - index - 1) * (nuint)sizeof(T));
            }

            resize(oldCount - 1);
        }

        public void push_back([NativeTypeName("const T&")] in T src)
        {
            nuint newIndex = size();
            resize(newIndex + 1);
            m_pArray[newIndex] = src;
        }

        public void pop_back()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            resize(size() - 1);
        }

        public void push_front([NativeTypeName("const T&")] in T src)
        {
            insert(0, in src);
        }

        public void pop_front()
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (m_Count > 0));
            remove(0);
        }

        public T* begin() => m_pArray;

        public T* end() => m_pArray + m_Count;

        [return: NativeTypeName("size_t")]
        public nuint InsertSorted<CmpLess>([NativeTypeName("const T&")] in T value, [NativeTypeName("const CmpLess&")] in CmpLess cmp)
            where CmpLess : unmanaged, ICmpLess<T, T>
        {
            nuint indexToInsert = (nuint)(BinaryFindFirstNotLess(m_pArray, m_pArray + m_Count, value, cmp) - m_pArray);
            insert(indexToInsert, value);
            return indexToInsert;
        }

        public bool RemoveSorted<CmpLess>([NativeTypeName("const T&")] in T value, [NativeTypeName("const CmpLess&")] in CmpLess cmp)
            where CmpLess : unmanaged, ICmpLess<T, T>
        {
            T* it = BinaryFindFirstNotLess(m_pArray, m_pArray + m_Count, value, cmp);

            if ((it != end()) && !cmp.Invoke(*it, value) && !cmp.Invoke(value, *it))
            {
                nuint indexToRemove = (nuint)(it - begin());
                remove(indexToRemove);
                return true;
            }

            return false;
        }
    }
}
