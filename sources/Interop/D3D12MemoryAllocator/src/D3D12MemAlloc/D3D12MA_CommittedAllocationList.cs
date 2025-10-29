// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12MA_Allocation.Type;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Stores linked list of Allocation objects that are of <see cref="TYPE_COMMITTED" /> or <see cref="TYPE_HEAP" />. Thread-safe, synchronized internally.</summary>
internal unsafe partial struct D3D12MA_CommittedAllocationList : IDisposable
{
    private bool m_UseMutex;

    private D3D12_HEAP_TYPE m_HeapType;

    private D3D12MA_PoolPimpl* m_Pool;

    private D3D12MA_RW_MUTEX m_Mutex;

    private D3D12MA_IntrusiveLinkedList<D3D12MA_CommittedAllocationListItemTraits, D3D12MA_Allocation> m_AllocationList;

    public D3D12MA_CommittedAllocationList()
    {
        m_UseMutex = true;
        m_HeapType = D3D12_HEAP_TYPE_CUSTOM;
        m_Mutex = new D3D12MA_RW_MUTEX();
    }

    public readonly void Dispose()
    {
        if (!m_AllocationList.IsEmpty())
        {
            D3D12MA_FAIL("Unfreed committed allocations found!");
        }
        m_AllocationList.Dispose();
    }

    public void Init(bool useMutex, D3D12_HEAP_TYPE heapType, D3D12MA_PoolPimpl* pool)
    {
        m_UseMutex = useMutex;
        m_HeapType = heapType;
        m_Pool = pool;
    }

    public readonly D3D12_HEAP_TYPE GetHeapType()
    {
        return m_HeapType;
    }

    public readonly D3D12MA_PoolPimpl* GetPool()
    {
        return m_Pool;
    }

    [return: NativeTypeName("UINT")]
    public readonly uint GetMemorySegmentGroup(D3D12MA_AllocatorPimpl* allocator)
    {
        if (m_Pool != null)
        {
            return allocator->HeapPropertiesToMemorySegmentGroup(m_Pool->GetDesc().HeapProperties);
        }
        else
        {
            return allocator->StandardHeapTypeToMemorySegmentGroup(m_HeapType);
        }
    }

    public void AddStatistics([NativeTypeName("D3D12MA_Statistics &")] ref D3D12MA_Statistics inoutStats)
    {
        using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_UseMutex);

        for (D3D12MA_Allocation* alloc = m_AllocationList.Front(); alloc != null; alloc = D3D12MA_IntrusiveLinkedList<D3D12MA_CommittedAllocationListItemTraits, D3D12MA_Allocation>.GetNext(alloc))
        {
            ulong size = alloc->GetSize();
            inoutStats.BlockCount++;
            inoutStats.AllocationCount++;
            inoutStats.BlockBytes += size;
            inoutStats.AllocationBytes += size;
        }
    }

    public void AddDetailedStatistics([NativeTypeName("D3D12MA_DetailedStatistics &")] ref D3D12MA_DetailedStatistics inoutStats)
    {
        using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_UseMutex);

        for (D3D12MA_Allocation* alloc = m_AllocationList.Front(); alloc != null; alloc = D3D12MA_IntrusiveLinkedList<D3D12MA_CommittedAllocationListItemTraits, D3D12MA_Allocation>.GetNext(alloc))
        {
            ulong size = alloc->GetSize();
            inoutStats.Stats.BlockCount++;
            inoutStats.Stats.BlockBytes += size;
            D3D12MA_AddDetailedStatisticsAllocation(ref inoutStats, size);
        }
    }

    // Writes JSON array with the list of allocations.
    public void BuildStatsString([NativeTypeName("D3D12MA_JsonWriter &")] ref D3D12MA_JsonWriter json)
    {
        using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_UseMutex);

        for (D3D12MA_Allocation* alloc = m_AllocationList.Front(); alloc != null; alloc = D3D12MA_IntrusiveLinkedList<D3D12MA_CommittedAllocationListItemTraits, D3D12MA_Allocation>.GetNext(alloc))
        {
            json.BeginObject(true);
            json.AddAllocationToObject(*alloc);
            json.EndObject();
        }
    }

    public void Register(D3D12MA_Allocation* alloc)
    {
        using D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_Mutex, m_UseMutex);
        m_AllocationList.PushBack(alloc);
    }

    public void Unregister(D3D12MA_Allocation* alloc)
    {
        using D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_Mutex, m_UseMutex);
        m_AllocationList.Remove(alloc);
    }
}
