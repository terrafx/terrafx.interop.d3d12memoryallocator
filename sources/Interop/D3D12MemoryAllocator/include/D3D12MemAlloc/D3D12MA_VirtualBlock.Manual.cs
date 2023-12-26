// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_VirtualBlock
{
    private static readonly void** VtblInstance = InitVtblInstance();

    private static void** InitVtblInstance()
    {
        void** lpVtbl = (void**)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_VirtualBlock), 5 * sizeof(void*)));

        lpVtbl[0] = (delegate* unmanaged[MemberFunction]<D3D12MA_VirtualBlock*, Guid*, void**, int>)(D3D12MA_IUnknownImpl.VtblInstance[0]);
        lpVtbl[1] = (delegate* unmanaged[MemberFunction]<D3D12MA_VirtualBlock*, uint>)(D3D12MA_IUnknownImpl.VtblInstance[1]);
        lpVtbl[2] = (delegate* unmanaged[MemberFunction]<D3D12MA_VirtualBlock*, uint>)(D3D12MA_IUnknownImpl.VtblInstance[2]);
        lpVtbl[3] = (delegate* unmanaged[MemberFunction]<D3D12MA_VirtualBlock*, void>)(&Dispose);
        lpVtbl[4] = (delegate* unmanaged[MemberFunction]<D3D12MA_VirtualBlock*, void>)(&ReleaseThis);

        return lpVtbl;
    }

    [VtblIndex(3)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Dispose(D3D12MA_VirtualBlock* pThis)
    {
        // THIS IS AN IMPORTANT ASSERT!
        // Hitting it means you have some memory leak - unreleased allocations in this virtual block.

        D3D12MA_ASSERT(pThis->m_Pimpl->m_Metadata->IsEmpty(), "Some allocations were not freed before destruction of this virtual block!");
        D3D12MA_DELETE(pThis->m_Pimpl->m_AllocationCallbacks, pThis->m_Pimpl);

        ((delegate* unmanaged[MemberFunction]<D3D12MA_IUnknownImpl*, void>)(D3D12MA_IUnknownImpl.VtblInstance[3]))(&pThis->Base);
    }

    [VtblIndex(4)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void ReleaseThis(D3D12MA_VirtualBlock* pThis)
    {
        // Copy is needed because otherwise we would call destructor and invalidate the structure with callbacks before using it to free memory.
        D3D12MA_ALLOCATION_CALLBACKS allocationCallbacksCopy = pThis->m_Pimpl->m_AllocationCallbacks;
        D3D12MA_DELETE(allocationCallbacksCopy, pThis);
    }
}
