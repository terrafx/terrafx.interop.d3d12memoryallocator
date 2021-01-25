// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace TerraFX.Interop
{
    internal readonly struct D3D12MA_RW_MUTEX
    {
        public void LockRead() { m_Lock.EnterReadLock(); }
        public void UnlockRead() { m_Lock.ExitReadLock(); }
        public void LockWrite() { m_Lock.EnterWriteLock(); }
        public void UnlockWrite() { m_Lock.ExitWriteLock(); }

        public static void Init(out D3D12MA_RW_MUTEX mutex)
        {
            mutex = default;
            Unsafe.AsRef(mutex.m_Lock) = new();
        }

        private readonly ReaderWriterLockSlim m_Lock;
    }
}
