// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_RW_MUTEX
{
    [NativeTypeName("SRWLOCK")]
    private SRWLOCK m_Lock;

    public D3D12MA_RW_MUTEX()
    {
        InitializeSRWLock((SRWLOCK*)(Unsafe.AsPointer(ref m_Lock)));
    }

    public void LockRead()
    {
        AcquireSRWLockShared((SRWLOCK*)(Unsafe.AsPointer(ref m_Lock)));
    }

    public void UnlockRead()
    {
        ReleaseSRWLockShared((SRWLOCK*)(Unsafe.AsPointer(ref m_Lock)));
    }

    public void LockWrite()
    {
        AcquireSRWLockExclusive((SRWLOCK*)(Unsafe.AsPointer(ref m_Lock)));
    }

    public void UnlockWrite()
    {
        ReleaseSRWLockExclusive((SRWLOCK*)(Unsafe.AsPointer(ref m_Lock)));
    }
}
