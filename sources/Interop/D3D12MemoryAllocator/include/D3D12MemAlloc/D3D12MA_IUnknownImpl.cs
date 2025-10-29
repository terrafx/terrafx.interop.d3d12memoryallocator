// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

[NativeTypeName("class D3D12MA::IUnknownImpl : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct D3D12MA_IUnknownImpl : D3D12MA_IUnknownImpl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));

    public void** lpVtbl;

    [NativeTypeName("std::atomic<UINT>")]
    private volatile uint m_RefCount;

    public D3D12MA_IUnknownImpl()
    {
        lpVtbl = VtblInstance;
        m_RefCount = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, Guid*, void**, int>)(lpVtbl[0]))((D3D12MA_IUnknownImpl*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, uint>)(lpVtbl[1]))((D3D12MA_IUnknownImpl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, uint>)(lpVtbl[2]))((D3D12MA_IUnknownImpl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public void Dispose()
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, void>)(lpVtbl[3]))((D3D12MA_IUnknownImpl*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    void Interface.ReleaseThis()
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, void>)(lpVtbl[4]))((D3D12MA_IUnknownImpl*)Unsafe.AsPointer(ref this));
    }

    public interface Interface : IUnknown.Interface, IDisposable
    {
        [VtblIndex(4)]
        void ReleaseThis();
    }

    public partial struct Vtbl<TSelf>
        where TSelf : unmanaged, Interface
    {
        [NativeTypeName("HRESULT (const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, Guid*, void**, int> QueryInterface;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> AddRef;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> Release;

        [NativeTypeName("void () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void> Dispose;

        [NativeTypeName("void () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, void> ReleaseThis;
    }
}
