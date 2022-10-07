// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_DefragmentationContext
{
    private static readonly void** VtblInstance = InitVtblInstance();

    private static void** InitVtblInstance()
    {
        void** lpVtbl = (void**)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_DefragmentationContext), 5 * sizeof(void*)));

        lpVtbl[0] = (delegate* unmanaged<D3D12MA_DefragmentationContext*, Guid*, void**, int>)(D3D12MA_IUnknownImpl.VtblInstance[0]);
        lpVtbl[1] = (delegate* unmanaged<D3D12MA_DefragmentationContext*, uint>)(D3D12MA_IUnknownImpl.VtblInstance[1]);
        lpVtbl[2] = (delegate* unmanaged<D3D12MA_DefragmentationContext*, uint>)(D3D12MA_IUnknownImpl.VtblInstance[2]);
        lpVtbl[3] = (delegate* unmanaged<D3D12MA_DefragmentationContext*, void>)(&Dispose);
        lpVtbl[4] = (delegate* unmanaged<D3D12MA_DefragmentationContext*, void>)(&ReleaseThis);

        return lpVtbl;
    }

    [VtblIndex(3)]
    [UnmanagedCallersOnly]
    internal static void Dispose(D3D12MA_DefragmentationContext* pThis)
    {
        D3D12MA_DELETE(pThis->m_Pimpl->GetAllocs(), pThis->m_Pimpl);
        ((delegate* unmanaged<D3D12MA_IUnknownImpl*, void>)(D3D12MA_IUnknownImpl.VtblInstance[3]))(&pThis->Base);
    }

    [VtblIndex(4)]
    [UnmanagedCallersOnly]
    internal static void ReleaseThis(D3D12MA_DefragmentationContext* pThis)
    {
        if (pThis == null)
        {
            return;
        }

        D3D12MA_DELETE(pThis->m_Pimpl->GetAllocs(), pThis);
    }
}
