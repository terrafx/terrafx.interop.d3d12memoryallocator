// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>Thread-safe wrapper over PoolAllocator free list, for allocation of Allocation objects.</summary>
    internal unsafe struct AllocationObjectAllocator
    {
        private D3D12MA_MUTEX m_Mutex;
        private PoolAllocator<Allocation> m_Allocator;

        public AllocationObjectAllocator(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            D3D12MA_MUTEX.Init(out m_Mutex);
            m_Allocator = new PoolAllocator<Allocation>(allocationCallbacks, 1024);
        }

        public Allocation* Allocate(AllocatorPimpl* allocator, ulong size, int wasZeroInitialized)
        {
            using MutexLock mutexLock = new((D3D12MA_MUTEX*)Unsafe.AsPointer(ref m_Mutex));

            return m_Allocator.Alloc(allocator, size, wasZeroInitialized);
        }

        public void Free([NativeTypeName("Allocation*")] ref Allocation alloc)
        {
            using MutexLock mutexLock = new((D3D12MA_MUTEX*)Unsafe.AsPointer(ref m_Mutex));
            m_Allocator.Free((Allocation*)Unsafe.AsPointer(ref alloc));
        }
    }
}
