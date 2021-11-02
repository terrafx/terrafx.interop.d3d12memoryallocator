// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 3a335d55c99e605775bbe9fe9c01ee6212804bed
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop
{
    /// <summary>
    /// Stores linked list of Allocation objects that are of TYPE_COMMITTED or TYPE_HEAP. Thread-safe, synchronized internally.
    /// </summary>
    internal unsafe struct D3D12MA_CommittedAllocationList : IDisposable
    {
        private byte m_useMutex;
        private D3D12_HEAP_TYPE m_HeapType;
        private D3D12MA_Pool* m_Pool;

        private D3D12MA_RW_MUTEX m_Mutex;

        [NativeTypeName("typedef IntrusiveLinkedList<CommittedAllocationListItemTraits> CommittedAllocationLinkedList")]
        private D3D12MA_IntrusiveLinkedList<D3D12MA_Allocation> m_AllocationList;

        public static void _ctor(ref D3D12MA_CommittedAllocationList pThis)
        {
            pThis.m_useMutex = 1;
            pThis.m_HeapType = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_CUSTOM;
            pThis.m_Pool = null;

            D3D12MA_RW_MUTEX._ctor(ref pThis.m_Mutex);
        }

        public void Init(bool useMutex, D3D12_HEAP_TYPE heapType, D3D12MA_Pool* pool)
        {
            m_useMutex = (byte)(useMutex ? 1 : 0);
            m_HeapType = heapType;
            m_Pool = pool;
        }

        public void Dispose()
        {
            if (!m_AllocationList.IsEmpty())
            {
                D3D12MA_ASSERT(false); // "Unfreed committed allocations found!"
            }
        }

        public readonly D3D12_HEAP_TYPE GetHeapType() => m_HeapType;

        public void CalculateStats([NativeTypeName("StatInfo&")] ref D3D12MA_StatInfo outStats)
        {
            ZeroMemory(Unsafe.AsPointer(ref outStats), (nuint)sizeof(D3D12MA_StatInfo));
            outStats.AllocationSizeMin = UINT64_MAX;
            outStats.UnusedRangeSizeMin = UINT64_MAX;

            using D3D12MA_MutexLockRead @lock = new(ref m_Mutex, m_useMutex != 0);

            for (D3D12MA_Allocation* alloc = m_AllocationList.Front();
                 alloc != null; alloc = D3D12MA_IntrusiveLinkedList<D3D12MA_Allocation>.GetNext(alloc))
            {
                ulong size = alloc->GetSize();
                ++outStats.BlockCount;
                ++outStats.AllocationCount;
                outStats.UsedBytes += size;

                if (size > outStats.AllocationSizeMax)
                {
                    outStats.AllocationSizeMax = size;
                }

                if (size < outStats.AllocationSizeMin)
                {
                    outStats.AllocationSizeMin = size;
                }
            }
        }

        /// <summary>Writes JSON array with the list of allocations.</summary>
        public void BuildStatsString([NativeTypeName("JsonWriter&")] ref D3D12MA_JsonWriter json)
        {
            using D3D12MA_MutexLockRead @lock = new(ref m_Mutex, m_useMutex != 0);

            json.BeginArray();
            for (D3D12MA_Allocation* alloc = m_AllocationList.Front();
                 alloc != null; alloc = D3D12MA_IntrusiveLinkedList<D3D12MA_Allocation>.GetNext(alloc))
            {
                json.BeginObject(true);
                json.AddAllocationToObject(alloc);
                json.EndObject();
            }
            json.EndArray();
        }

        public void Register(D3D12MA_Allocation* alloc)
        {
            using D3D12MA_MutexLockRead @lock = new(ref m_Mutex, m_useMutex != 0);

            m_AllocationList.PushBack(alloc);
        }

        public void Unregister(D3D12MA_Allocation* alloc)
        {
            using D3D12MA_MutexLockRead @lock = new(ref m_Mutex, m_useMutex != 0);

            m_AllocationList.Remove(alloc);
        }
    }
}
