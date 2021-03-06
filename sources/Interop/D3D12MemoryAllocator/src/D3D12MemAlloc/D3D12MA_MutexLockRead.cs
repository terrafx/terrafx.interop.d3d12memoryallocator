// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    // Helper RAII class to lock a RW mutex in constructor and unlock it in destructor (at the end of scope), for reading.
    internal readonly unsafe ref struct D3D12MA_MutexLockRead
    {
        private readonly D3D12MA_RW_MUTEX* m_pMutex;

        public D3D12MA_MutexLockRead([NativeTypeName("D3D12MA_RW_MUTEX&")] ref D3D12MA_RW_MUTEX mutex, bool useMutex = true)
            : this((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref mutex), useMutex)
        {
        }

        public D3D12MA_MutexLockRead([NativeTypeName("D3D12MA_RW_MUTEX&")] D3D12MA_RW_MUTEX* mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            if (m_pMutex != null)
            {
                m_pMutex->LockRead();
            }
        }

        public void Dispose()
        {
            if (m_pMutex != null)
            {
                m_pMutex->UnlockRead();
            }
        }
    }
}
