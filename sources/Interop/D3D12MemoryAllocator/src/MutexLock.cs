// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    // Helper RAII class to lock a mutex in constructor and unlock it in destructor (at the end of scope).
    internal readonly ref struct MutexLock
    {
        public MutexLock(D3D12MA_MUTEX mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            m_pMutex?.Lock();
        }

        public void Dispose()
        {
            m_pMutex?.Unlock();
        }

        readonly D3D12MA_MUTEX? m_pMutex;
    }
}
