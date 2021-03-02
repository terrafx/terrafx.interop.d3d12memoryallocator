// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>Thread-safe wrapper over PoolAllocator free list, for allocation of Allocation objects.</summary>
    internal unsafe struct D3D12MA_AllocationObjectAllocator
    {
        private D3D12MA_MUTEX m_Mutex;
        private PoolAllocator<D3D12MA_Allocation> m_Allocator;

        public D3D12MA_AllocationObjectAllocator(D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            D3D12MA_MUTEX.Init(out m_Mutex);
            m_Allocator = new PoolAllocator<D3D12MA_Allocation>(allocationCallbacks, 1024);
        }

        public D3D12MA_Allocation* Allocate(D3D12MA_AllocatorPimpl* allocator, ulong size, int wasZeroInitialized)
        {
            using var mutexLock = new D3D12MA_MutexLock((D3D12MA_MUTEX*)Unsafe.AsPointer(ref m_Mutex));

            return m_Allocator.Alloc(allocator, size, wasZeroInitialized);
        }

        public void Free([NativeTypeName("Allocation*")] ref D3D12MA_Allocation alloc)
        {
            using var mutexLock = new D3D12MA_MutexLock((D3D12MA_MUTEX*)Unsafe.AsPointer(ref m_Mutex));
            m_Allocator.Free((D3D12MA_Allocation*)Unsafe.AsPointer(ref alloc));
        }
    }
}
