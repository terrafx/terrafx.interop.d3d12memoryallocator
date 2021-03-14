// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>Thread-safe wrapper over PoolAllocator free list, for allocation of Allocation objects.</summary>
    internal unsafe struct D3D12MA_AllocationObjectAllocator : IDisposable
    {
        private D3D12MA_MUTEX m_Mutex;

        private D3D12MA_PoolAllocator<D3D12MA_Allocation> m_Allocator;

        public static void _ctor(ref D3D12MA_AllocationObjectAllocator pThis, [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] ref D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks)
        {
            D3D12MA_MUTEX._ctor(ref pThis.m_Mutex);
            D3D12MA_PoolAllocator<D3D12MA_Allocation>._ctor(ref pThis.m_Allocator, (D3D12MA_ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref allocationCallbacks), 1024);
        }

        public D3D12MA_Allocation* Allocate(D3D12MA_Allocator* allocator, ulong size, int wasZeroInitialized)
        {
            using var mutexLock = new D3D12MA_MutexLock(ref m_Mutex);

            var allocation = m_Allocator.Alloc();
            D3D12MA_Allocation._ctor(ref *allocation, allocator, size, wasZeroInitialized);

            return allocation;
        }

        public void Free(ref D3D12MA_Allocation alloc)
        {
            Free((D3D12MA_Allocation*)Unsafe.AsPointer(ref alloc));
        }

        public void Free(D3D12MA_Allocation* alloc)
        {
            using var mutexLock = new D3D12MA_MutexLock(ref m_Mutex);
            m_Allocator.Free(alloc);
        }

        public void Dispose()
        {
            m_Mutex.Dispose();
            m_Allocator.Dispose();
        }
    }
}
