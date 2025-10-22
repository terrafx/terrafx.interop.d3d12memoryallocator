// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_MUTEX : IDisposable
{
    [NativeTypeName("std::mutex")]
    private HANDLE m_Mutex;

    public static D3D12MA_MUTEX* Create()
    {
        D3D12MA_MUTEX* result = (D3D12MA_MUTEX*)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAlloc), sizeof(D3D12MA_MUTEX)));
        result->_ctor();
        return result;
    }

    public D3D12MA_MUTEX()
    {
        _ctor();
    }

    private void _ctor()
    {
        m_Mutex = CreateMutex(null, 0, null);
    }

    public readonly void Dispose()
    {
        _ = CloseHandle(m_Mutex);
    }

    public readonly void Lock()
    {
        _ = WaitForSingleObject(m_Mutex, INFINITE);
    }

    public readonly void Unlock()
    {
        _ = ReleaseMutex(m_Mutex);
    }
}
