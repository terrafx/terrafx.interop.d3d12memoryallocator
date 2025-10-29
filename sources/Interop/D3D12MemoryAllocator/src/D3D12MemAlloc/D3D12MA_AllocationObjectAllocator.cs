// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using TerraFX.Interop.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Thread-safe wrapper over PoolAllocator free list, for allocation of Allocation objects.</summary>
internal unsafe partial struct D3D12MA_AllocationObjectAllocator : IDisposable
{
    private D3D12MA_MUTEX m_Mutex;

    private bool m_UseMutex;

    private D3D12MA_PoolAllocator<D3D12MA_Allocation> m_Allocator;

    public D3D12MA_AllocationObjectAllocator()
    {
        m_Mutex = new D3D12MA_MUTEX();
    }

    public D3D12MA_AllocationObjectAllocator([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, bool useMutex) : this()
    {
        m_UseMutex = useMutex;
        m_Allocator = new D3D12MA_PoolAllocator<D3D12MA_Allocation>(allocationCallbacks, 1024);
    }

    public void Dispose()
    {
        m_Mutex.Dispose();
        m_Allocator.Dispose();
    }

    public D3D12MA_Allocation* Allocate(D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment)
    {
        using D3D12MA_MutexLock mutexLock = new D3D12MA_MutexLock(ref m_Mutex, m_UseMutex);
        return m_Allocator.Alloc(allocator, size, alignment);
    }

    public void Free(D3D12MA_Allocation* alloc)
    {
        using D3D12MA_MutexLock mutexLock = new D3D12MA_MutexLock(ref m_Mutex, m_UseMutex);
        m_Allocator.Free(alloc);
    }
}
