// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_RW_MUTEX
    {
        [NativeTypeName("SRWLOCK")]
        private SRWLOCK m_Lock;

        public void LockRead() => AcquireSRWLockShared((SRWLOCK*)Unsafe.AsPointer(ref m_Lock));

        public void UnlockRead() => ReleaseSRWLockShared((SRWLOCK*)Unsafe.AsPointer(ref m_Lock));

        public void LockWrite() => AcquireSRWLockExclusive((SRWLOCK*)Unsafe.AsPointer(ref m_Lock));

        public void UnlockWrite() => ReleaseSRWLockExclusive((SRWLOCK*)Unsafe.AsPointer(ref m_Lock));

        public static void _ctor(ref D3D12MA_RW_MUTEX mutex)
        {
            InitializeSRWLock((SRWLOCK*)Unsafe.AsPointer(ref mutex.m_Lock));
        }
    }
}
