// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using static TerraFX.Interop.Windows;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_MUTEX
    {
        [NativeTypeName("std::mutex")] private HANDLE m_Mutex;

        public void Lock() { WaitForSingleObject(m_Mutex, INFINITE); }
        public void Unlock() { ReleaseMutex(m_Mutex); }

        public static void Init(out D3D12MA_MUTEX mutex)
        {
            mutex = default;
            mutex.m_Mutex = CreateMutex(null, 0, null);
        }
    }
}
