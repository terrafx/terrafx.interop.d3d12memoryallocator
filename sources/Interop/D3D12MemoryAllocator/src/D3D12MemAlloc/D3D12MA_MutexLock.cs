// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    // Helper RAII class to lock a mutex in constructor and unlock it in destructor (at the end of scope).
    internal readonly unsafe ref struct D3D12MA_MutexLock
    {
        readonly D3D12MA_MUTEX* m_pMutex;

        public D3D12MA_MutexLock(D3D12MA_MUTEX* mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            if (m_pMutex != null)
                m_pMutex->Lock();
        }

        public void Dispose()
        {
            if (m_pMutex != null)
                m_pMutex->Unlock();
        }
    }
}
