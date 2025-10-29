// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.IID;
using static TerraFX.Interop.Windows.S;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_IUnknownImpl
{
    internal static readonly void** VtblInstance = InitVtblInstance();

    private static void** InitVtblInstance()
    {
        void** lpVtbl = (void**)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_IUnknownImpl), 5 * sizeof(void*)));

        lpVtbl[0] = (delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, Guid*, void**, int>)(&QueryInterface);
        lpVtbl[1] = (delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, uint>)(&AddRef);
        lpVtbl[2] = (delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, uint>)(&Release);
        lpVtbl[3] = (delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, void>)(&Dispose);
        lpVtbl[4] = (delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, void>)(&ReleaseThis);

        return lpVtbl;
    }

    [VtblIndex(0)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static int QueryInterface(D3D12MA_IUnknownImpl* pThis, [NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
    {
        if (ppvObject == null)
        {
            return E_POINTER;
        }

        if (*riid == IID_IUnknown)
        {
            _ = Interlocked.Increment(ref pThis->m_RefCount);
            *ppvObject = pThis;
            return S_OK;
        }

        *ppvObject = null;
        return E_NOINTERFACE;
    }

    [VtblIndex(1)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("ULONG")]
    internal static uint AddRef(D3D12MA_IUnknownImpl* pThis)
    {
        return Interlocked.Increment(ref pThis->m_RefCount);
    }

    [VtblIndex(2)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("ULONG")]
    internal static uint Release(D3D12MA_IUnknownImpl* pThis)
    {
        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        uint newRefCount = Interlocked.Decrement(ref pThis->m_RefCount);

        if (newRefCount == 0)
        {
            ((delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, void>)(pThis->lpVtbl[4]))(pThis);
        }
        return newRefCount;
    }

    [VtblIndex(3)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Dispose(D3D12MA_IUnknownImpl* pThis)
    {
    }

    [VtblIndex(4)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void ReleaseThis(D3D12MA_IUnknownImpl* pThis)
    {
        pThis->Dispose();
        NativeMemory.Free(pThis);
    }
}
