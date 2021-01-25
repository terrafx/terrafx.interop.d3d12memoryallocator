// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class AllocationObjectAllocator definition

    /// <summary>Thread-safe wrapper over PoolAllocator free list, for allocation of Allocation objects.</summary>
    internal unsafe partial struct AllocationObjectAllocator
    {
        public AllocationObjectAllocator(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            D3D12MA_MUTEX.Init(out m_Mutex);
            m_Allocator = new(allocationCallbacks, 1024);
        }
        public partial Allocation* Allocate(AllocatorPimpl* allocator, ulong size, int wasZeroInitialized);
        public partial void Free(Allocation* alloc);

        private D3D12MA_MUTEX m_Mutex;
        private PoolAllocator<Allocation> m_Allocator;
    }

    internal unsafe partial struct AllocationObjectAllocator
    {
        public partial Allocation* Allocate(AllocatorPimpl* allocator, ulong size, int wasZeroInitialized)
        {
            using MutexLock mutexLock = new((D3D12MA_MUTEX*)Unsafe.AsPointer(ref m_Mutex));
            static Allocation Cctor(Ptr<AllocatorPimpl> allocator, ulong size, int wasZeroInitialized)
            {
                return new(allocator, size, wasZeroInitialized);
            }
            return m_Allocator.Alloc((Ptr<AllocatorPimpl>)allocator, size, wasZeroInitialized, &Cctor);
        }

        public partial void Free(Allocation* alloc)
        {
            using MutexLock mutexLock = new((D3D12MA_MUTEX*)Unsafe.AsPointer(ref m_Mutex));
            m_Allocator.Free(alloc);
        }
    }
}
