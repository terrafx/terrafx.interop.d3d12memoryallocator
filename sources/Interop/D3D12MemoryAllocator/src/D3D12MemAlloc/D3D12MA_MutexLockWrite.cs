// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop.DirectX;

/// <summary>Helper RAII class to lock a RW mutex in constructor and unlock it in destructor (at the end of scope), for writing.</summary>
internal readonly unsafe partial struct D3D12MA_MutexLockWrite : IDisposable
{
    private readonly D3D12MA_RW_MUTEX* m_pMutex;

    public D3D12MA_MutexLockWrite([NativeTypeName("D3D12MA_RW_MUTEX &")] ref D3D12MA_RW_MUTEX mutex, bool useMutex = true)
    {
        m_pMutex = useMutex ? (D3D12MA_RW_MUTEX*)(Unsafe.AsPointer(ref mutex)) : null;

        if (m_pMutex != null)
        {
            m_pMutex->LockWrite();
        }
    }

    public void Dispose()
    {
        if (m_pMutex != null)
        {
            m_pMutex->UnlockWrite();
        }
    }
}
