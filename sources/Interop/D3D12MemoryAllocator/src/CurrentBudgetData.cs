// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;

using UINT = System.UInt32;
using UINT64 = System.UInt64;
using System.ComponentModel;

namespace TerraFX.Interop
{
    public partial class D3D12MemoryAllocator
    {
        internal const uint DEFAULT_POOL_MAX_COUNT = 9;
    }

    internal unsafe struct CurrentBudgetData
    {
        public __m_Buffer64_Data<atomic<ulong>> m_BlockBytes;
        public __m_Buffer64_Data<atomic<ulong>> m_AllocationBytes;

        public atomic<uint> m_OperationsSinceBudgetFetch;
        public D3D12MA_RW_MUTEX m_BudgetMutex;
        public UINT64 m_D3D12UsageLocal, m_D3D12UsageNonLocal;
        public UINT64 m_D3D12BudgetLocal, m_D3D12BudgetNonLocal;
        public __m_Buffer64_Data<UINT64> m_BlockBytesAtBudgetFetch;

        public void AddAllocation(UINT heapTypeIndex, UINT64 allocationSize)
        {
            m_AllocationBytes[(int)heapTypeIndex].Add(allocationSize);
            m_OperationsSinceBudgetFetch.Increment();
        }

        public void RemoveAllocation(UINT heapTypeIndex, UINT64 allocationSize)
        {
            m_AllocationBytes[(int)heapTypeIndex].Add(allocationSize);
            m_OperationsSinceBudgetFetch.Increment();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __m_Buffer64_Data<T>
            where T : unmanaged
        {
            public T _0;
            public T _1;
            public T _2;

            public ref T this[int index] => ref ((T*)Unsafe.AsPointer(ref this))[index];
        }
    }
}
