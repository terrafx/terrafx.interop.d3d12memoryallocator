// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Dynamically resizing continuous array. Class with interface similar to <c>std::vector</c>.</summary>
/// <typeparam name="T">T must be POD because constructors and destructors are not called and <c>memcpy</c> is used for these objects.</typeparam>
internal unsafe partial struct D3D12MA_Vector<T> : IDisposable
    where T : unmanaged
{
    [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")]
    private readonly D3D12MA_ALLOCATION_CALLBACKS* m_AllocationCallbacks;

    private T* m_pArray;

    [NativeTypeName("size_t")]
    private nuint m_Count;

    [NativeTypeName("size_t")]
    private nuint m_Capacity;

    // allocationCallbacks externally owned, must outlive this object.
    public D3D12MA_Vector([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks)
    {
        m_AllocationCallbacks = (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(ref Unsafe.AsRef(in allocationCallbacks)));
    }

    public D3D12MA_Vector([NativeTypeName("size_t")] nuint count, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks)
    {
        m_AllocationCallbacks = (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(ref Unsafe.AsRef(in allocationCallbacks)));
        m_pArray = (count != 0) ? D3D12MA_AllocateArray<T>(allocationCallbacks, count) : null;
        m_Count = count;
        m_Capacity = count;
    }

    public D3D12MA_Vector([NativeTypeName("const D3D12MA::Vector<T> &")] in D3D12MA_Vector<T> src)
    {
        m_AllocationCallbacks = src.m_AllocationCallbacks;
        m_pArray = (src.m_Count != 0) ? D3D12MA_AllocateArray<T>(*src.m_AllocationCallbacks, src.m_Count) : null;
        m_Count = src.m_Count;
        m_Capacity = src.m_Count;

        if (m_Count > 0)
        {
            _ = memcpy(m_pArray, src.m_pArray, m_Count * __sizeof<T>());
        }
    }

    public void Dispose()
    {
        D3D12MA_Free(*m_AllocationCallbacks, m_pArray);
    }

    [return: NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")]
    public readonly ref readonly D3D12MA_ALLOCATION_CALLBACKS GetAllocs()
    {
        return ref *m_AllocationCallbacks;
    }

    public readonly bool empty()
    {
        return m_Count == 0;
    }

    [return: NativeTypeName("size_t")]
    public readonly nuint size()
    {
        return m_Count;
    }

    public T* data()
    {
        return m_pArray;
    }

    public void clear(bool freeMemory = false)
    {
        resize(0, freeMemory);
    }

    [return: NativeTypeName("iterator")]
    public T* begin()
    {
        return m_pArray;
    }

    [return: NativeTypeName("iterator")]
    public T* end()
    {
        return m_pArray + m_Count;
    }

    [return: NativeTypeName("iterator")]
    public T* rend()
    {
        return begin() - 1;
    }

    [return: NativeTypeName("iterator")]
    public T* rbegin()
    {
        return end() - 1;
    }

    [return: NativeTypeName("const iterator")]
    public readonly T* cbegin()
    {
        return m_pArray;
    }

    [return: NativeTypeName("const iterator")]
    public readonly T* cend()
    {
        return m_pArray + m_Count;
    }

    [return: NativeTypeName("const iterator")]
    public readonly T* crbegin()
    {
        return cend() -1;
    }

    [return: NativeTypeName("const iterator")]
    public readonly T* crend()
    {
        return cbegin() -1;
    }

    public void push_front([NativeTypeName("const T &")] in T src)
    {
        insert(0, src);
    }

    public void push_back([NativeTypeName("const T &")] in T src)
    {
        nuint newIndex = size();
        resize(newIndex + 1);
        m_pArray[newIndex] = src;
    }

    public void pop_front()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);
        remove(0);
    }

    public void pop_back()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);
        resize(size() - 1);
    }

    [return: NativeTypeName("T &")]
    public ref T front()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);
        return ref m_pArray[0];
    }

    [return: NativeTypeName("T &")]
    public ref T back()
    {
        D3D12MA_HEAVY_ASSERT(m_Count > 0);
        return ref m_pArray[m_Count - 1];
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
            T* newArray = (newCapacity != 0) ? D3D12MA_AllocateArray<T>(*m_AllocationCallbacks, newCapacity) : null;

            if (m_Count != 0)
            {
                _ = memcpy(newArray, m_pArray, m_Count * __sizeof<T>());
            }
            D3D12MA_Free(*m_AllocationCallbacks, m_pArray);

            m_Capacity = newCapacity;
            m_pArray = newArray;
        }
    }

    public void resize([NativeTypeName("size_t")] nuint  newCount, bool freeMemory = false)
    {
        nuint newCapacity = m_Capacity;

        if (newCount > m_Capacity)
        {
            newCapacity = D3D12MA_MAX(newCount, D3D12MA_MAX(m_Capacity * 3 / 2, 8));
        }
        else if (freeMemory)
        {
            newCapacity = newCount;
        }

        if (newCapacity != m_Capacity)
        {
            T* newArray = (newCapacity != 0) ? D3D12MA_AllocateArray<T>(*m_AllocationCallbacks, newCapacity) : null;
            nuint elementsToCopy = D3D12MA_MIN(m_Count, newCount);

            if (elementsToCopy != 0)
            {
                _ = memcpy(newArray, m_pArray, elementsToCopy * __sizeof<T>());
            }
            D3D12MA_Free(*m_AllocationCallbacks, m_pArray);

            m_Capacity = newCapacity;
            m_pArray = newArray;
        }

        m_Count = newCount;
    }

    public void insert([NativeTypeName("size_t")] nuint  index, [NativeTypeName("const T &")] in T src)
    {
        D3D12MA_HEAVY_ASSERT(index <= m_Count);

        nuint oldCount = size();
        resize(oldCount + 1);

        if (index < oldCount)
        {
            _ = memmove(m_pArray + (index + 1), m_pArray + index, (oldCount - index) * __sizeof<T>());
        }
        m_pArray[index] = src;
    }

    public void remove([NativeTypeName("size_t")] nuint index)
    {
        D3D12MA_HEAVY_ASSERT(index < m_Count);
        nuint oldCount = size();

        if (index < oldCount - 1)
        {
            _ = memmove(m_pArray + index, m_pArray + (index + 1), (oldCount - index - 1) * __sizeof<T>());
        }
        resize(oldCount - 1);
    }

    // template < typename CmpLess >
    [return: NativeTypeName("size_t")]
    public nuint InsertSorted<CmpLess>([NativeTypeName("const T &")] in T value, [NativeTypeName("const CmpLess &")] in CmpLess cmp)
        where CmpLess : unmanaged, D3D12MA_CmpLess<T>
    {
        nuint indexToInsert = (nuint)(D3D12MA_BinaryFindFirstNotLess(m_pArray, m_pArray + m_Count, value, cmp) - m_pArray);
        insert(indexToInsert, value);
        return indexToInsert;
    }

    public bool RemoveSorted<CmpLess>([NativeTypeName("const T &")] in T value, [NativeTypeName("const CmpLess &")] in CmpLess cmp)
        where CmpLess : unmanaged, D3D12MA_CmpLess<T>
    {
        T* it = D3D12MA_BinaryFindFirstNotLess(m_pArray, m_pArray + m_Count, value, cmp);

        if ((it != end()) && !cmp.Invoke(*it, value) && !cmp.Invoke(value, *it))
        {
            nuint indexToRemove = (nuint)(it - begin());
            remove(indexToRemove);
            return true;
        }
        return false;
    }

    [NativeTypeName("T&")]
    public ref T this[[NativeTypeName("size_t")] nuint index]
    {
        get
        {
            D3D12MA_HEAVY_ASSERT(index < m_Count);
            return ref m_pArray[index];
        }
    }
}
