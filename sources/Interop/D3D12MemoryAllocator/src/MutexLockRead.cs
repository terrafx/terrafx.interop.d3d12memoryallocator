// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    // Helper RAII class to lock a RW mutex in constructor and unlock it in destructor (at the end of scope), for reading.
    internal readonly unsafe ref struct MutexLockRead
    {
        public MutexLockRead(D3D12MA_RW_MUTEX mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            m_pMutex?.LockRead();
        }

        public void Dispose()
        {
            m_pMutex?.UnlockRead();
        }

        readonly D3D12MA_RW_MUTEX? m_pMutex;
    }
}