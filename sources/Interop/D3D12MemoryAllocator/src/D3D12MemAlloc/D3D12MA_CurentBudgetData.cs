// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_CurrentBudgetData
{
    [NativeTypeName("D3D12MA_ATOMIC_UINT32[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private fixed uint m_BlockCount[(int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT)];

    [NativeTypeName("D3D12MA_ATOMIC_UINT32[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private fixed uint m_AllocationCount[(int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT)];

    [NativeTypeName("D3D12MA_ATOMIC_UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private fixed ulong m_BlockBytes[(int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT)];

    [NativeTypeName("D3D12MA_ATOMIC_UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private fixed ulong m_AllocationBytes[(int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT)];

    [NativeTypeName("D3D12MA_ATOMIC_UINT32")]
    private volatile uint m_OperationsSinceBudgetFetch;

    private D3D12MA_RW_MUTEX m_BudgetMutex;

    [NativeTypeName("UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private fixed ulong m_D3D12Usage[(int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT)];

    [NativeTypeName("UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private fixed ulong m_D3D12Budget[(int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT)];

    [NativeTypeName("UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private fixed ulong m_BlockBytesAtD3D12Fetch[(int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT)];

    public D3D12MA_CurrentBudgetData()
    {
        m_BudgetMutex = new D3D12MA_RW_MUTEX();
    }

    public readonly bool ShouldUpdateBudget()
    {
        return m_OperationsSinceBudgetFetch >= 30;
    }

    public readonly void GetStatistics([NativeTypeName("D3D12MA::Statistics &")] out D3D12MA_Statistics outStats, [NativeTypeName("UINT")] uint group)
    {
        outStats.BlockCount = Volatile.Read(ref Unsafe.AsRef(in m_BlockCount[group]));
        outStats.AllocationCount = Volatile.Read(ref Unsafe.AsRef(in m_AllocationCount[group]));

        outStats.BlockBytes = Volatile.Read(ref Unsafe.AsRef(in m_BlockBytes[group]));
        outStats.AllocationBytes = Volatile.Read(ref Unsafe.AsRef(in m_AllocationBytes[group]));
    }

    public void GetBudget(bool useMutex, [NativeTypeName("UINT64 *")] ulong* outLocalUsage, [NativeTypeName("UINT64 *")] ulong* outLocalBudget, [NativeTypeName("UINT64 *")] ulong* outNonLocalUsage, [NativeTypeName("UINT64 *")] ulong* outNonLocalBudget)
    {
        using D3D12MA_MutexLockRead lockRead = new D3D12MA_MutexLockRead(ref m_BudgetMutex, useMutex);

        if (outLocalUsage != null)
        {
            ulong D3D12Usage = m_D3D12Usage[DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY];
            ulong blockBytes = Volatile.Read(ref m_BlockBytes[DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY]);
            ulong blockBytesAtD3D12Fetch = m_BlockBytesAtD3D12Fetch[DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY];
            *outLocalUsage = ((D3D12Usage + blockBytes) > blockBytesAtD3D12Fetch) ? (D3D12Usage + blockBytes - blockBytesAtD3D12Fetch) : 0;
        }

        if (outLocalBudget != null)
        {
            *outLocalBudget = m_D3D12Budget[DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY];
        }

        if (outNonLocalUsage != null)
        {
            ulong D3D12Usage = m_D3D12Usage[DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY];
            ulong blockBytes = Volatile.Read(ref m_BlockBytes[DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY]);
            ulong blockBytesAtD3D12Fetch = m_BlockBytesAtD3D12Fetch[DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY];
            *outNonLocalUsage = ((D3D12Usage + blockBytes) > blockBytesAtD3D12Fetch) ? (D3D12Usage + blockBytes - blockBytesAtD3D12Fetch) : 0;
        }

        if (outNonLocalBudget != null)
        {
            *outNonLocalBudget = m_D3D12Budget[DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY];
        }
    }

    public HRESULT UpdateBudget(IDXGIAdapter3* adapter3, bool useMutex)
    {
        if (D3D12MA_DXGI_1_4 == 0)
        {
            D3D12MA_FAIL();
        }

        D3D12MA_ASSERT(adapter3 != null);

        DXGI_QUERY_VIDEO_MEMORY_INFO infoLocal = new DXGI_QUERY_VIDEO_MEMORY_INFO();
        DXGI_QUERY_VIDEO_MEMORY_INFO infoNonLocal = new DXGI_QUERY_VIDEO_MEMORY_INFO();

        HRESULT hrLocal = adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &infoLocal);
        HRESULT hrNonLocal = adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL, &infoNonLocal);

        if (SUCCEEDED(hrLocal) || SUCCEEDED(hrNonLocal))
        {
            using D3D12MA_MutexLockWrite lockWrite = new D3D12MA_MutexLockWrite(ref m_BudgetMutex, useMutex);

            if (SUCCEEDED(hrLocal))
            {
                m_D3D12Usage[0] = infoLocal.CurrentUsage;
                m_D3D12Budget[0] = infoLocal.Budget;
            }

            if (SUCCEEDED(hrNonLocal))
            {
                m_D3D12Usage[1] = infoNonLocal.CurrentUsage;
                m_D3D12Budget[1] = infoNonLocal.Budget;
            }

            m_BlockBytesAtD3D12Fetch[0] = Volatile.Read(ref m_BlockBytes[0]);
            m_BlockBytesAtD3D12Fetch[1] = Volatile.Read(ref m_BlockBytes[1]);
            m_OperationsSinceBudgetFetch = 0;
        }

        return FAILED(hrLocal) ? hrLocal : hrNonLocal;
    }

    public void AddAllocation([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong allocationBytes)
    {
        _ = Interlocked.Increment(ref m_AllocationCount[group]);
        _ = Interlocked.Add(ref m_AllocationBytes[group], allocationBytes);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }

    public void RemoveAllocation([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong allocationBytes)
    {
        D3D12MA_ASSERT(Volatile.Read(ref m_AllocationBytes[group]) >= allocationBytes);
        D3D12MA_ASSERT(Volatile.Read(ref m_AllocationCount[group]) > 0);

        _ = Interlocked.Add(ref m_AllocationBytes[group], unchecked(0 - allocationBytes));
        _ = Interlocked.Decrement(ref m_AllocationCount[group]);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }

    public void AddBlock([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong blockBytes)
    {
        _ = Interlocked.Increment(ref m_BlockCount[group]);
        _ = Interlocked.Add(ref m_BlockBytes[group], blockBytes);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }

    public void RemoveBlock([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong blockBytes)
    {
        D3D12MA_ASSERT(Volatile.Read(ref m_BlockBytes[group]) >= blockBytes);
        D3D12MA_ASSERT(Volatile.Read(ref m_BlockCount[group]) > 0);

        _ = Interlocked.Add(ref m_BlockBytes[group], unchecked(0 - blockBytes));
        _ = Interlocked.Decrement(ref m_BlockCount[group]);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }
}
