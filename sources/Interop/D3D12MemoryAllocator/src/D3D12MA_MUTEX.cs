// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace TerraFX.Interop
{
    internal readonly struct D3D12MA_MUTEX
    {
        public void Lock() { Monitor.Enter(m_Mutex); }
        public void Unlock() { Monitor.Exit(m_Mutex); }

        public static void Init(out D3D12MA_MUTEX mutex)
        {
            mutex = default;
            Unsafe.AsRef(mutex.m_Mutex) = new();
        }

        private readonly object m_Mutex;
    }
}
