// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.S;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_CurrentBudgetData
{
    [NativeTypeName("D3D12MA_ATOMIC_UINT32[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private _m_BlockCount_e__FixedBuffer m_BlockCount;

    [NativeTypeName("D3D12MA_ATOMIC_UINT32[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private _m_AllocationCount_e__FixedBuffer m_AllocationCount;

    [NativeTypeName("D3D12MA_ATOMIC_UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private _m_BlockBytes_e__FixedBuffer m_BlockBytes;

    [NativeTypeName("D3D12MA_ATOMIC_UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private _m_AllocationBytes_e__FixedBuffer m_AllocationBytes;

    [NativeTypeName("D3D12MA_ATOMIC_UINT32")]
    private volatile uint m_OperationsSinceBudgetFetch;

    private D3D12MA_RW_MUTEX m_BudgetMutex;

    [NativeTypeName("UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private _m_D3D12Usage_e__FixedBuffer m_D3D12Usage;

    [NativeTypeName("UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private _m_D3D12Budget_e__FixedBuffer m_D3D12Budget;

    [NativeTypeName("UINT64[DXGI_MEMORY_SEGMENT_GROUP_COUNT]")]
    private _m_BlockBytesAtD3D12Fetch_e__FixedBuffer m_BlockBytesAtD3D12Fetch;

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
        outStats.BlockCount = Volatile.Read(in m_BlockCount[(int)(group)]);
        outStats.AllocationCount = Volatile.Read(in m_AllocationCount[(int)(group)]);

        outStats.BlockBytes = Volatile.Read(in m_BlockBytes[(int)(group)]);
        outStats.AllocationBytes = Volatile.Read(in m_AllocationBytes[(int)(group)]);
    }

    public void GetBudget(bool useMutex, [NativeTypeName("UINT64 *")] ulong* outLocalUsage, [NativeTypeName("UINT64 *")] ulong* outLocalBudget, [NativeTypeName("UINT64 *")] ulong* outNonLocalUsage, [NativeTypeName("UINT64 *")] ulong* outNonLocalBudget)
    {
        using D3D12MA_MutexLockRead lockRead = new D3D12MA_MutexLockRead(ref m_BudgetMutex, useMutex);

        if (outLocalUsage != null)
        {
            ulong D3D12Usage = m_D3D12Usage[(int)(DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY)];
            ulong blockBytes = Volatile.Read(ref m_BlockBytes[(int)(DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY)]);
            ulong blockBytesAtD3D12Fetch = m_BlockBytesAtD3D12Fetch[(int)(DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY)];
            *outLocalUsage = ((D3D12Usage + blockBytes) > blockBytesAtD3D12Fetch) ? (D3D12Usage + blockBytes - blockBytesAtD3D12Fetch) : 0;
        }

        if (outLocalBudget != null)
        {
            *outLocalBudget = m_D3D12Budget[(int)(DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY)];
        }

        if (outNonLocalUsage != null)
        {
            ulong D3D12Usage = m_D3D12Usage[(int)(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY)];
            ulong blockBytes = Volatile.Read(ref m_BlockBytes[(int)(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY)]);
            ulong blockBytesAtD3D12Fetch = m_BlockBytesAtD3D12Fetch[(int)(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY)];
            *outNonLocalUsage = ((D3D12Usage + blockBytes) > blockBytesAtD3D12Fetch) ? (D3D12Usage + blockBytes - blockBytesAtD3D12Fetch) : 0;
        }

        if (outNonLocalBudget != null)
        {
            *outNonLocalBudget = m_D3D12Budget[(int)(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY)];
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

        if (FAILED(hrLocal))
        {
            return hrLocal;
        }

        HRESULT hrNonLocal = adapter3->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL, &infoNonLocal);

        if (FAILED(hrNonLocal))
        {
            return hrNonLocal;
        }

        using D3D12MA_MutexLockWrite lockWrite = new D3D12MA_MutexLockWrite(ref m_BudgetMutex, useMutex);

        m_D3D12Usage[0] = infoLocal.CurrentUsage;
        m_D3D12Budget[0] = infoLocal.Budget;

        m_D3D12Usage[1] = infoNonLocal.CurrentUsage;
        m_D3D12Budget[1] = infoNonLocal.Budget;

        m_BlockBytesAtD3D12Fetch[0] = Volatile.Read(ref m_BlockBytes[0]);
        m_BlockBytesAtD3D12Fetch[1] = Volatile.Read(ref m_BlockBytes[1]);
        m_OperationsSinceBudgetFetch = 0;

        return S_OK;
    }

    public void AddAllocation([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong allocationBytes)
    {
        _ = Interlocked.Increment(ref m_AllocationCount[(int)(group)]);
        _ = Interlocked.Add(ref m_AllocationBytes[(int)(group)], allocationBytes);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }

    public void RemoveAllocation([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong allocationBytes)
    {
        D3D12MA_ASSERT(Volatile.Read(ref m_AllocationBytes[(int)(group)]) >= allocationBytes);
        D3D12MA_ASSERT(Volatile.Read(ref m_AllocationCount[(int)(group)]) > 0);

        _ = Interlocked.Add(ref m_AllocationBytes[(int)(group)], unchecked(0 - allocationBytes));
        _ = Interlocked.Decrement(ref m_AllocationCount[(int)(group)]);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }

    public void AddBlock([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong blockBytes)
    {
        _ = Interlocked.Increment(ref m_BlockCount[(int)(group)]);
        _ = Interlocked.Add(ref m_BlockBytes[(int)(group)], blockBytes);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }

    public void RemoveBlock([NativeTypeName("UINT")] uint group, [NativeTypeName("UINT64")] ulong blockBytes)
    {
        D3D12MA_ASSERT(Volatile.Read(ref m_BlockBytes[(int)(group)]) >= blockBytes);
        D3D12MA_ASSERT(Volatile.Read(ref m_BlockCount[(int)(group)]) > 0);

        _ = Interlocked.Add(ref m_BlockBytes[(int)(group)], unchecked(0 - blockBytes));
        _ = Interlocked.Decrement(ref m_BlockCount[(int)(group)]);
        _ = Interlocked.Increment(ref m_OperationsSinceBudgetFetch);
    }

    [InlineArray((int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT))]
    public partial struct _m_BlockCount_e__FixedBuffer
    {
        public uint e0;
    }

    [InlineArray((int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT))]
    public partial struct _m_AllocationCount_e__FixedBuffer
    {
        public uint e0;
    }

    [InlineArray((int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT))]
    public partial struct _m_BlockBytes_e__FixedBuffer
    {
        public ulong e0;
    }

    [InlineArray((int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT))]
    public partial struct _m_AllocationBytes_e__FixedBuffer
    {
        public ulong e0;
    }

    [InlineArray((int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT))]
    public partial struct _m_D3D12Usage_e__FixedBuffer
    {
        public ulong e0;
    }

    [InlineArray((int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT))]
    public partial struct _m_D3D12Budget_e__FixedBuffer
    {
        public ulong e0;
    }

    [InlineArray((int)(DXGI_MEMORY_SEGMENT_GROUP_COUNT))]
    public partial struct _m_BlockBytesAtD3D12Fetch_e__FixedBuffer
    {
        public ulong e0;
    }
}
