// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_MUTEX : IDisposable
    {
        [NativeTypeName("std::mutex")]
        private HANDLE m_Mutex;

        public void Lock() => WaitForSingleObject(m_Mutex, INFINITE);

        public void Unlock() => ReleaseMutex(m_Mutex);

        public static void _ctor(ref D3D12MA_MUTEX mutex)
        {
            mutex.m_Mutex = CreateMutex(null, 0, null);
        }

        public void Dispose()
        {
            _ = CloseHandle(m_Mutex);
        }
    }
}
