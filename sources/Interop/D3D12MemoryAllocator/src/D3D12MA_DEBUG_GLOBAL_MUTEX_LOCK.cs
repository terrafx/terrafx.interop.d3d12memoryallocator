// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    internal readonly unsafe ref struct D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK Get()
        {
            if (D3D12MA_DEBUG_GLOBAL_MUTEX > 0)
                g_DebugGlobalMutex.Lock();

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (D3D12MA_DEBUG_GLOBAL_MUTEX > 0)
                g_DebugGlobalMutex.Unlock();
        }
    }
}
