// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

public static unsafe partial class D3D12MemAlloc
{
    /// <summary>To be used with <see cref="MAKE_HRESULT" /> to define custom error codes.</summary>
    public const uint FACILITY_D3D12MA = 3542;

    /// <summary>Creates new main <see cref="D3D12MA_Allocator" /> object and returns it through <paramref name="ppAllocator" />.</summary>
    /// <remarks>You normally only need to call it once and keep a single <see cref="D3D12MA_Allocator" /> object for your <see cref="ID3D12Device" />.</remarks>
    public static HRESULT D3D12MA_CreateAllocator([NativeTypeName("const D3D12MA::ALLOCATOR_DESC *")] D3D12MA_ALLOCATOR_DESC* pDesc, D3D12MA_Allocator** ppAllocator)
    {
        if ((pDesc == null) || (ppAllocator == null) || (pDesc->pDevice == null) || (pDesc->pAdapter == null) || !((pDesc->PreferredBlockSize == 0) || ((pDesc->PreferredBlockSize >= 16) && (pDesc->PreferredBlockSize < 0x10000000000UL))))
        {
            D3D12MA_FAIL("Invalid arguments passed to CreateAllocator.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        D3D12MA_SetupAllocationCallbacks(out D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, pDesc->pAllocationCallbacks);
        *ppAllocator = D3D12MA_Allocator.Create(allocationCallbacks, allocationCallbacks, *pDesc);

        HRESULT hr = (*ppAllocator)->m_Pimpl->Init(*pDesc);

        if (FAILED(hr))
        {
            D3D12MA_DELETE(allocationCallbacks, *ppAllocator);
            *ppAllocator = null;
        }

        return hr;
    }

    /// <summary>Creates new <see cref="D3D12MA_VirtualBlock" /> object and returns it through <paramref name="ppVirtualBlock" />.</summary>
    /// <remarks>Note you don't need to create <see cref="D3D12MA_Allocator" /> to use virtual blocks.</remarks>
    public static HRESULT D3D12MA_CreateVirtualBlock([NativeTypeName("const D3D12MA::VIRTUAL_BLOCK_DESC *")] D3D12MA_VIRTUAL_BLOCK_DESC* pDesc, D3D12MA_VirtualBlock** ppVirtualBlock)
    {
        if ((pDesc == null) || (ppVirtualBlock == null))
        {
            D3D12MA_FAIL("Invalid arguments passed to CreateVirtualBlock.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        D3D12MA_SetupAllocationCallbacks(out D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, pDesc->pAllocationCallbacks);
        *ppVirtualBlock = D3D12MA_VirtualBlock.Create(allocationCallbacks, allocationCallbacks, *pDesc);

        return S_OK;
    }
}
