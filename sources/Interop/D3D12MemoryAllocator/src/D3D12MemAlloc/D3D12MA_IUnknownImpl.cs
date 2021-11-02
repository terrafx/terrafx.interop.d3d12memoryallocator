// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 7642634a62a151295badb7a6e31414473df5c036
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    /// <summary>The base implementation for COM's <c>IUnknown</c> interface.</summary>
    internal unsafe struct D3D12MA_IUnknownImpl
    {
        internal void** lpVtbl;

        [NativeTypeName("std::atomic<UINT>")]
        private volatile uint m_RefCount;

        public static void _ctor(ref D3D12MA_IUnknownImpl pThis, void** lpVtbl)
        {
            pThis.lpVtbl = lpVtbl;
            pThis.m_RefCount = 1;
        }

        [return: NativeTypeName("HRESULT")]
        public int QueryInterface([NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (lpVtbl[0] == (delegate* unmanaged<D3D12MA_IUnknownImpl*, Guid*, void**, int>)&QueryInterface));

            if (ppvObject is null)
            {
                return Windows.E_POINTER;
            }

            if (riid->Equals(Windows.IID_IUnknown))
            {
                _ = Interlocked.Increment(ref m_RefCount);
                *ppvObject = Unsafe.AsPointer(ref this);
                return Windows.S_OK;
            }

            *ppvObject = null;

            return Windows.E_NOINTERFACE;
        }

        [return: NativeTypeName("ULONG")]
        public uint AddRef()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (lpVtbl[1] == (delegate* unmanaged<D3D12MA_IUnknownImpl*, uint>)&AddRef));
            return Interlocked.Increment(ref m_RefCount);
        }

        [return: NativeTypeName("ULONG")]
        public uint Release()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (lpVtbl[2] == (delegate* unmanaged<D3D12MA_IUnknownImpl*, uint>)&Release));

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            uint newRefCount = Interlocked.Decrement(ref m_RefCount);

            if (newRefCount == 0)
            {
                ReleaseThis();
            }

            return newRefCount;
        }

        public void ReleaseThis()
        {
            ((delegate* unmanaged<D3D12MA_IUnknownImpl*, void>)lpVtbl[3])((D3D12MA_IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        [UnmanagedCallersOnly]
        [return: NativeTypeName("HRESULT")]
        public static int QueryInterface(D3D12MA_IUnknownImpl* pThis, [NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
        {
            if (ppvObject is null)
            {
                return Windows.E_POINTER;
            }

            if (riid->Equals(Windows.IID_IUnknown))
            {
                _ = Interlocked.Increment(ref pThis->m_RefCount);
                *ppvObject = pThis;
                return Windows.S_OK;
            }

            *ppvObject = null;

            return Windows.E_NOINTERFACE;
        }

        [UnmanagedCallersOnly]
        [return: NativeTypeName("ULONG")]
        public static uint AddRef(D3D12MA_IUnknownImpl* pThis)
        {
            return Interlocked.Increment(ref pThis->m_RefCount);
        }

        [UnmanagedCallersOnly]
        [return: NativeTypeName("ULONG")]
        public static uint Release(D3D12MA_IUnknownImpl* pThis)
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            uint newRefCount = Interlocked.Decrement(ref pThis->m_RefCount);

            if (newRefCount == 0)
            {
                pThis->ReleaseThis();
            }

            return newRefCount;
        }
    }
}
