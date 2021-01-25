// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class AllocationObjectAllocator definition

    /// <summary>Thread-safe wrapper over PoolAllocator free list, for allocation of Allocation objects.</summary>
    internal unsafe struct AllocationObjectAllocator
    {
        public AllocationObjectAllocator(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            D3D12MA_MUTEX.Init(out m_Mutex);
            m_Allocator = new(allocationCallbacks, 1024);
        }

        private D3D12MA_MUTEX m_Mutex;
        private PoolAllocator<Allocation> m_Allocator;
    }
}
