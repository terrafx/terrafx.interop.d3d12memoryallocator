// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 47bedc01fff10d502622d2eb7b38ae887de83897
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Threading;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_CurrentBudgetData
    {
        public fixed ulong m_BlockBytes[(int)D3D12MA_HEAP_TYPE_COUNT];

        public fixed ulong m_AllocationBytes[(int)D3D12MA_HEAP_TYPE_COUNT];

        public volatile uint m_OperationsSinceBudgetFetch;

        public D3D12MA_RW_MUTEX m_BudgetMutex;

        [NativeTypeName("UINT64")]
        public ulong m_D3D12UsageLocal, m_D3D12UsageNonLocal;

        [NativeTypeName("UINT64")]
        public ulong m_D3D12BudgetLocal, m_D3D12BudgetNonLocal;

        [NativeTypeName("UINT64")]
        public fixed ulong m_BlockBytesAtBudgetFetch[(int)D3D12MA_HEAP_TYPE_COUNT];

        public static void _ctor(ref D3D12MA_CurrentBudgetData pThis)
        {
            D3D12MA_RW_MUTEX._ctor(ref pThis.m_BudgetMutex);

            for (uint i = 0; i < D3D12MA_HEAP_TYPE_COUNT; ++i)
            {
                Volatile.Write(ref pThis.m_BlockBytes[(int)i], 0);
                Volatile.Write(ref pThis.m_AllocationBytes[(int)i], 0);
                pThis.m_BlockBytesAtBudgetFetch[(int)i] = 0;
            }

            pThis.m_D3D12UsageLocal = 0;
            pThis.m_D3D12UsageNonLocal = 0;
            pThis.m_D3D12BudgetLocal = 0;
            pThis.m_D3D12BudgetNonLocal = 0;
            pThis.m_OperationsSinceBudgetFetch = 0;
        }

        public void AddAllocation([NativeTypeName("UINT")] uint heapTypeIndex, [NativeTypeName("UINT64")] ulong allocationSize)
        {
            ref ulong allocationBytes = ref m_AllocationBytes[(int)heapTypeIndex];
            Volatile.Write(ref allocationBytes, Volatile.Read(ref allocationBytes) + allocationSize);

            ++m_OperationsSinceBudgetFetch;
        }

        public void RemoveAllocation([NativeTypeName("UINT")] uint heapTypeIndex, [NativeTypeName("UINT64")] ulong allocationSize)
        {
            ref ulong allocationBytes = ref m_AllocationBytes[(int)heapTypeIndex];
            Volatile.Write(ref allocationBytes, Volatile.Read(ref allocationBytes) - allocationSize);

            ++m_OperationsSinceBudgetFetch;
        }

        public void AddCommittedAllocation([NativeTypeName("UINT")] uint heapTypeIndex, [NativeTypeName("UINT64")] ulong allocationSize)
        {
            AddAllocation(heapTypeIndex, allocationSize);

            ref ulong blockBytes = ref m_BlockBytes[(int)heapTypeIndex];
            Volatile.Write(ref blockBytes, Volatile.Read(ref blockBytes) + allocationSize);
        }

        public void RemoveCommittedAllocation([NativeTypeName("UINT")] uint heapTypeIndex, [NativeTypeName("UINT64")] ulong allocationSize)
        {
            ref ulong blockBytes = ref m_BlockBytes[(int)heapTypeIndex];
            Volatile.Write(ref blockBytes, Volatile.Read(ref blockBytes) - allocationSize);

            RemoveAllocation(heapTypeIndex, allocationSize);
        }
    }
}
