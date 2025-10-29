// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12MA_Allocation.Type;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_Allocation
{
    private static readonly void** VtblInstance = InitVtblInstance();

    private static void** InitVtblInstance()
    {
        void** lpVtbl = (void**)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_Allocation), 5 * sizeof(void*)));

        lpVtbl[0] = (delegate* unmanaged[MemberFunction]<D3D12MA_Allocation*, Guid*, void**, int>)(D3D12MA_IUnknownImpl.VtblInstance[0]);
        lpVtbl[1] = (delegate* unmanaged[MemberFunction]<D3D12MA_Allocation*, uint>)(D3D12MA_IUnknownImpl.VtblInstance[1]);
        lpVtbl[2] = (delegate* unmanaged[MemberFunction]<D3D12MA_Allocation*, uint>)(D3D12MA_IUnknownImpl.VtblInstance[2]);
        lpVtbl[3] = (delegate* unmanaged[MemberFunction]<D3D12MA_Allocation*, void>)(D3D12MA_IUnknownImpl.VtblInstance[3]);
        lpVtbl[4] = (delegate* unmanaged[MemberFunction]<D3D12MA_Allocation*, void>)(&ReleaseThis);

        return lpVtbl;
    }

    [VtblIndex(4)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void ReleaseThis(D3D12MA_Allocation* pThis)
    {
        D3D12MA_SAFE_RELEASE(ref pThis->m_Resource);

        switch (pThis->m_PackedData.GetType())
        {
            case TYPE_COMMITTED:
            {
                pThis->m_Allocator->FreeCommittedMemory(pThis);
                break;
            }

            case TYPE_PLACED:
            {
                pThis->m_Allocator->FreePlacedMemory(pThis);
                break;
            }

            case TYPE_HEAP:
            {
                pThis->m_Allocator->FreeHeapMemory(pThis);
                break;
            }
        }

        pThis->FreeName();
        pThis->m_Allocator->GetAllocationObjectAllocator().Free(pThis);
    }
}
