// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_CurrentBudgetData
    {
        public _D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer m_BlockBytes;

        public _D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer m_AllocationBytes;

        public volatile uint m_OperationsSinceBudgetFetch;

        public D3D12MA_RW_MUTEX m_BudgetMutex;

        [NativeTypeName("UINT64")]
        public ulong m_D3D12UsageLocal, m_D3D12UsageNonLocal;

        [NativeTypeName("UINT64")]
        public ulong m_D3D12BudgetLocal, m_D3D12BudgetNonLocal;

        [NativeTypeName("UINT64")]
        public _D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer m_BlockBytesAtBudgetFetch;

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

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct _D3D12MA_HEAP_TYPE_COUNT_e__FixedBuffer
        {
            public ulong e0;
            public ulong e1;
            public ulong e2;

            public ref ulong this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return ref AsSpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<ulong> AsSpan() => MemoryMarshal.CreateSpan(ref e0, (int)D3D12MA_HEAP_TYPE_COUNT);
        }
    }
}
