// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_BlockMetadata
{
    internal static readonly void** VtblInstance = InitVtblInstance();

    private static void** InitVtblInstance()
    {
        void** lpVtbl = (void**)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_BlockMetadata), 21 * sizeof(void*)));

        lpVtbl[0] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata*, void>)(&Dispose);
        lpVtbl[1] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata*, ulong, void>)(&Init);
        lpVtbl[2] = null;
        lpVtbl[3] = null;
        lpVtbl[4] = null;
        lpVtbl[5] = null;
        lpVtbl[6] = null;
        lpVtbl[7] = null;
        lpVtbl[8] = null;
        lpVtbl[9] = null;
        lpVtbl[10] = null;
        lpVtbl[11] = null;
        lpVtbl[12] = null;
        lpVtbl[13] = null;
        lpVtbl[14] = null;
        lpVtbl[15] = null;
        lpVtbl[16] = null;
        lpVtbl[17] = null;
        lpVtbl[18] = null;
        lpVtbl[19] = null;
        lpVtbl[20] = null;

        return lpVtbl;
    }

    [VtblIndex(0)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Dispose(D3D12MA_BlockMetadata* pThis)
    {
    }

    [VtblIndex(1)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Init(D3D12MA_BlockMetadata* pThis, [NativeTypeName("UINT64")] ulong size)
    {
        pThis->m_Size = size;
    }
}
