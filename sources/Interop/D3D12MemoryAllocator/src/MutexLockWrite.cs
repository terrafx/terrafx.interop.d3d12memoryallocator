// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    // Helper RAII class to lock a RW mutex in constructor and unlock it in destructor (at the end of scope), for writing.
    internal readonly unsafe ref struct MutexLockWrite
    {
        public MutexLockWrite(D3D12MA_RW_MUTEX* mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            if (m_pMutex != null)
                m_pMutex->LockWrite();
        }

        public void Dispose()
        {
            if (m_pMutex != null)
                m_pMutex->UnlockWrite();
        }

        readonly D3D12MA_RW_MUTEX* m_pMutex;
    }
}
