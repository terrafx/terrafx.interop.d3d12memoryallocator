// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    public partial class D3D12MemoryAllocator
    {
        internal const uint DEFAULT_POOL_MAX_COUNT = 9;
    }

    internal unsafe struct CurrentBudgetData
    {
        public __m_Buffer64_e__FixedBuffer<atomic<ulong>> m_BlockBytes;
        public __m_Buffer64_e__FixedBuffer<atomic<ulong>> m_AllocationBytes;

        public atomic<uint> m_OperationsSinceBudgetFetch;
        public D3D12MA_RW_MUTEX m_BudgetMutex;

        [NativeTypeName("UINT64")]
        public ulong m_D3D12UsageLocal, m_D3D12UsageNonLocal;

        [NativeTypeName("UINT64")]
        public ulong m_D3D12BudgetLocal, m_D3D12BudgetNonLocal;

        [NativeTypeName("UINT64")]
        public __m_Buffer64_e__FixedBuffer<ulong> m_BlockBytesAtBudgetFetch;

        public void AddAllocation([NativeTypeName("UINT")] uint heapTypeIndex, [NativeTypeName("UINT64")] ulong allocationSize)
        {
            m_AllocationBytes[(int)heapTypeIndex].Add(allocationSize);
            m_OperationsSinceBudgetFetch.Increment();
        }

        public void RemoveAllocation([NativeTypeName("UINT")] uint heapTypeIndex, [NativeTypeName("UINT64")] ulong allocationSize)
        {
            m_AllocationBytes[(int)heapTypeIndex].Add(allocationSize);
            m_OperationsSinceBudgetFetch.Increment();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __m_Buffer64_e__FixedBuffer<T>
            where T : unmanaged
        {
            public T e0;
            public T e1;
            public T e2;

            public ref T this[int index] => ref ((T*)Unsafe.AsPointer(ref this))[index];
        }
    }
}
