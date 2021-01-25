// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_RW_MUTEX
    {
        public void LockRead() { AcquireSRWLockShared((RTL_SRWLOCK*)Unsafe.AsPointer(ref m_Lock)); }
        public void UnlockRead() { ReleaseSRWLockShared((RTL_SRWLOCK*)Unsafe.AsPointer(ref m_Lock)); }
        public void LockWrite() { AcquireSRWLockExclusive((RTL_SRWLOCK*)Unsafe.AsPointer(ref m_Lock)); }
        public void UnlockWrite() { ReleaseSRWLockExclusive((RTL_SRWLOCK*)Unsafe.AsPointer(ref m_Lock)); }

        public static void Init(out D3D12MA_RW_MUTEX mutex)
        {
            mutex = default;
            InitializeSRWLock((RTL_SRWLOCK*)Unsafe.AsPointer(ref mutex.m_Lock));
        }

        [NativeTypeName("SRWLOCK")]
        private RTL_SRWLOCK m_Lock;
    }
}
