// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MA_VIRTUAL_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Represents pure allocation algorithm and a data structure with allocations in some memory block, without actually allocating any GPU memory.</summary>
/// <remarks>
///   <para>This class allows to use the core algorithm of the library custom allocations e.g. CPU memory or sub-allocation regions inside a single GPU buffer.</para>
///   <para>To create this object, fill in <see cref="D3D12MA_VIRTUAL_BLOCK_DESC" /> and call <see cref="D3D12MA_CreateVirtualBlock" />. To destroy it, call its method <see cref="Release()" />. You need to free all the allocations within this block or call <see cref="Clear" /> before destroying it.</para>
///   <para>This object is not thread-safe - should not be used from multiple threads simultaneously, must be synchronized externally.</para>
/// </remarks>
[NativeTypeName("class D3D12MA::VirtualBlock : D3D12MA::IUnknownImpl")]
[NativeInheritance("D3D12MA::IUnknownImpl")]
public unsafe partial struct D3D12MA_VirtualBlock : D3D12MA_IUnknownImpl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));

    public D3D12MA_IUnknownImpl Base;

    private D3D12MA_VirtualBlockPimpl* m_Pimpl;

    public static D3D12MA_VirtualBlock* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::VIRTUAL_BLOCK_DESC &")] in D3D12MA_VIRTUAL_BLOCK_DESC desc)
    {
        D3D12MA_VirtualBlock* result = D3D12MA_NEW<D3D12MA_VirtualBlock>(allocs);
        result->_ctor(allocationCallbacks, desc);
        return result;
    }

    private void _ctor()
    {
        Base = new D3D12MA_IUnknownImpl {
            lpVtbl = VtblInstance,
        };
    }

    private void _ctor([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::VIRTUAL_BLOCK_DESC &")] in D3D12MA_VIRTUAL_BLOCK_DESC desc)
    {
        _ctor();
        m_Pimpl = D3D12MA_VirtualBlockPimpl.Create(allocationCallbacks, allocationCallbacks, desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged<D3D12MA_VirtualBlock*, Guid*, void**, int>)(Base.lpVtbl[0]))((D3D12MA_VirtualBlock*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged<D3D12MA_VirtualBlock*, uint>)(Base.lpVtbl[1]))((D3D12MA_VirtualBlock*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged<D3D12MA_VirtualBlock*, uint>)(Base.lpVtbl[2]))((D3D12MA_VirtualBlock*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    void IDisposable.Dispose()
    {
        ((delegate* unmanaged<D3D12MA_VirtualBlock*, void>)(Base.lpVtbl[3]))((D3D12MA_VirtualBlock*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    void D3D12MA_IUnknownImpl.Interface.ReleaseThis()
    {
        ((delegate* unmanaged<D3D12MA_VirtualBlock*, void>)(Base.lpVtbl[4]))((D3D12MA_VirtualBlock*)Unsafe.AsPointer(ref this));
    }

    /// <summary>Returns true if the block is empty - contains 0 allocations.</summary>
    /// <returns></returns>
    public readonly BOOL IsEmpty()
    {
        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->m_Metadata->IsEmpty() ? TRUE : FALSE;
    }

    /// <summary>Returns information about an allocation - its offset, size and custom pointer.</summary>
    /// <param name="allocation"></param>
    /// <param name="pInfo"></param>
    public readonly void GetAllocationInfo(D3D12MA_VirtualAllocation allocation, D3D12MA_VIRTUAL_ALLOCATION_INFO* pInfo)
    {
        D3D12MA_ASSERT((allocation.AllocHandle != 0) && (pInfo != null));

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->m_Metadata->GetAllocationInfo(allocation.AllocHandle, pInfo);
    }

    /// <summary>Creates new allocation.</summary>
    /// <param name="pDesc"></param>
    /// <param name="pAllocation">Unique indentifier of the new allocation within single block.</param>
    /// <param name="pOffset">Returned offset of the new allocation. Optional, can be null.</param>
    /// <returns><see cref="S_OK" /> if allocation succeeded, <see cref="E_OUTOFMEMORY" /> if it failed.</returns>
    /// <remarks>If the allocation failed, <c>pAllocation->AllocHandle</c> is set to 0 and <paramref name="pOffset" />, if not null, is set to <see cref="ulong.MaxValue" />.</remarks>
    public HRESULT Allocate([NativeTypeName("const D3D12MA::VIRTUAL_ALLOCATION_DESC *")] D3D12MA_VIRTUAL_ALLOCATION_DESC* pDesc, D3D12MA_VirtualAllocation* pAllocation, [NativeTypeName("UINT64 *")] ulong* pOffset)
    {
        if ((pDesc == null) || (pAllocation == null) || (pDesc->Size == 0) || !D3D12MA_IsPow2(pDesc->Alignment))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_VirtualBlock.Allocate.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);

        ulong alignment = (pDesc->Alignment != 0) ? pDesc->Alignment : 1;
        D3D12MA_AllocationRequest allocRequest = new D3D12MA_AllocationRequest();

        if (m_Pimpl->m_Metadata->CreateAllocationRequest(pDesc->Size, alignment, (pDesc->Flags & D3D12MA_VIRTUAL_ALLOCATION_FLAG_UPPER_ADDRESS) != 0, (uint)(pDesc->Flags & D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MASK), &allocRequest))
        {
            m_Pimpl->m_Metadata->Alloc(&allocRequest, pDesc->Size, pDesc->pPrivateData);
            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata->Validate());
            pAllocation->AllocHandle = allocRequest.allocHandle;

            if (pOffset != null)
            {
                *pOffset = m_Pimpl->m_Metadata->GetAllocationOffset(allocRequest.allocHandle);
            }
            return S_OK;
        }

        pAllocation->AllocHandle = 0;

        if (pOffset != null)
        {
            *pOffset = ulong.MaxValue;
        }
        return E_OUTOFMEMORY;
    }

    /// <summary>Frees the allocation.</summary>
    /// <param name="allocation"></param>
    /// <remarks>Calling this function with <c>allocation.AllocHandle == 0</c> is correct and does nothing.</remarks>
    public void FreeAllocation(D3D12MA_VirtualAllocation allocation)
    {
        if (allocation.AllocHandle == 0)
        {
            return;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);

        m_Pimpl->m_Metadata->Free(allocation.AllocHandle);
        D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata->Validate());
    }

    /// <summary>Frees all the allocations.</summary>
    public void Clear()
    {
        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);

        m_Pimpl->m_Metadata->Clear();
        D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata->Validate());
    }

    /// <summary>Changes custom pointer for an allocation to a new value.</summary>
    /// <param name="allocation"></param>
    /// <param name="pPrivateData"></param>
    public void SetAllocationPrivateData(D3D12MA_VirtualAllocation allocation, void* pPrivateData)
    {
        D3D12MA_ASSERT(allocation.AllocHandle != 0);

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->m_Metadata->SetAllocationPrivateData(allocation.AllocHandle, pPrivateData);
    }

    /// <summary>Retrieves basic statistics of the virtual block that are fast to calculate.</summary>
    /// <param name="pStats">Statistics of the virtual block.</param>
    public readonly void GetStatistics(D3D12MA_Statistics* pStats)
    {
        D3D12MA_ASSERT(pStats != null);

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata->Validate());
        D3D12MA_ClearStatistics(out *pStats);
        m_Pimpl->m_Metadata->AddStatistics(pStats);
    }

    /// <summary>Retrieves detailed statistics of the virtual block that are slower to calculate.</summary>
    /// <param name="pStats">Statistics of the virtual block.</param>
    public readonly void CalculateStatistics(D3D12MA_DetailedStatistics* pStats)
    {
        D3D12MA_ASSERT(pStats != null);

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata->Validate());
        D3D12MA_ClearDetailedStatistics(out *pStats);
        m_Pimpl->m_Metadata->AddDetailedStatistics(pStats);
    }

    /// <summary>Builds and returns statistics as a string in JSON format, including the list of allocations with their parameters.</summary>
    /// <param name="ppStatsString">Must be freed using <see cref="FreeStatsString" />.</param>
    public readonly void BuildStatsString([NativeTypeName("WCHAR **")] char** ppStatsString)
    {
        D3D12MA_ASSERT(ppStatsString != null);

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);

        using D3D12MA_StringBuilder sb = new D3D12MA_StringBuilder(m_Pimpl->m_AllocationCallbacks);
        {
            using D3D12MA_JsonWriter json = new D3D12MA_JsonWriter(m_Pimpl->m_AllocationCallbacks, ref Unsafe.AsRef(in sb));
            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata->Validate());
            m_Pimpl->m_Metadata->WriteAllocationInfoToJson(&json);
        } // Scope for JsonWriter

        nuint length = sb.GetLength();
        char* result = D3D12MA_AllocateArray<char>(m_Pimpl->m_AllocationCallbacks, length + 1);

        _ = memcpy(result, sb.GetData(), length * sizeof(char));

        result[length] = '\0';
        *ppStatsString = result;
    }

    /// <summary>Frees memory of a string returned from <see cref="BuildStatsString" />.</summary>
    /// <param name="pStatsString"></param>
    public readonly void FreeStatsString([NativeTypeName("WCHAR *")] char* pStatsString)
    {
        if (pStatsString != null)
        {
            using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
            D3D12MA_Free(m_Pimpl->m_AllocationCallbacks, pStatsString);
        }
    }
}
