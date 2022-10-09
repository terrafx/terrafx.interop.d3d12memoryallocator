// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.S;

namespace TerraFX.Interop.DirectX;

/// <summary>Represents defragmentation process in progress.</summary>
/// <remarks>You can create this object using <see cref="D3D12MA_Allocator.BeginDefragmentation" /> (for default pools) or <see cref="D3D12MA_Pool.BeginDefragmentation" /> (for a custom pool).</remarks>
[NativeTypeName("class D3D12MA::DefragmentationContext : D3D12MA::IUnknownImpl")]
[NativeInheritance("D3D12MA::IUnknownImpl")]
public unsafe partial struct D3D12MA_DefragmentationContext : D3D12MA_IUnknownImpl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));

    public D3D12MA_IUnknownImpl Base;

    private D3D12MA_DefragmentationContextPimpl* m_Pimpl;

    internal static D3D12MA_DefragmentationContext* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("const D3D12MA::DEFRAGMENTATION_DESC &")] in D3D12MA_DEFRAGMENTATION_DESC desc, D3D12MA_BlockVector* poolVector)
    {
        D3D12MA_DefragmentationContext* result = D3D12MA_NEW<D3D12MA_DefragmentationContext>(allocs);
        result->_ctor(allocator, desc, poolVector);
        return result;
    }

    private void _ctor()
    {
        Base = new D3D12MA_IUnknownImpl {
            lpVtbl = VtblInstance,
        };
    }

    private void _ctor(D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("const D3D12MA::DEFRAGMENTATION_DESC &")] in D3D12MA_DEFRAGMENTATION_DESC desc, D3D12MA_BlockVector* poolVector)
    {
        _ctor();
        m_Pimpl = D3D12MA_DefragmentationContextPimpl.Create(allocator->GetAllocs(), allocator, desc, poolVector);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged<D3D12MA_DefragmentationContext*, Guid*, void**, int>)(Base.lpVtbl[0]))((D3D12MA_DefragmentationContext*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged<D3D12MA_DefragmentationContext*, uint>)(Base.lpVtbl[1]))((D3D12MA_DefragmentationContext*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged<D3D12MA_DefragmentationContext*, uint>)(Base.lpVtbl[2]))((D3D12MA_DefragmentationContext*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    void IDisposable.Dispose()
    {
        ((delegate* unmanaged<D3D12MA_DefragmentationContext*, void>)(Base.lpVtbl[3]))((D3D12MA_DefragmentationContext*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    void D3D12MA_IUnknownImpl.Interface.ReleaseThis()
    {
        ((delegate* unmanaged<D3D12MA_DefragmentationContext*, void>)(Base.lpVtbl[4]))((D3D12MA_DefragmentationContext*)Unsafe.AsPointer(ref this));
    }

    /// <summary>Starts single defragmentation pass.</summary>
    /// <param name="pPassInfo">Computed informations for current pass.</param>
    /// <returns>
    ///   <list type="bullet">
    ///     <item>
    ///       <description><see cref="S_OK" /> if no more moves are possible. Then you can omit call to <see cref="EndPass" /> and simply end whole defragmentation.</description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="S_FALSE" /> if there are pending moves returned in <paramref name="pPassInfo" />. You need to perform them, call <see cref="EndPass" />, and then preferably try another pass with <see cref="BeginPass" />.</description>
    ///     </item>
    ///   </list>
    /// </returns>
    public HRESULT BeginPass(D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo)
    {
        D3D12MA_ASSERT(pPassInfo != null);
        return m_Pimpl->DefragmentPassBegin(ref *pPassInfo);
    }

    /// <summary>Ends single defragmentation pass.</summary>
    /// <param name="pPassInfo">Computed informations for current pass filled by <see cref="BeginPass" /> and possibly modified by you.</param>
    /// <returns>Returns <see cref="S_OK" /> if no more moves are possible or <see cref="S_FALSE" /> if more defragmentations are possible.</returns>
    /// <remarks>
    ///   <para>Ends incremental defragmentation pass and commits all defragmentation moves from `pPassInfo`. After this call:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>Allocation at <c>pPassInfo[i].pSrcAllocation</c> that had <c>pPassInfo[i].Operation == D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY</c> (which is the default) will be pointing to the new destination place.</description>
    ///     </item>
    ///     <item>
    ///       <description>Allocation at <c>pPassInfo[i].pSrcAllocation</c> that had <c>pPassInfo[i].Operation == D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_DESTROY</c> will be released.</description>
    ///     </item>
    ///   </list>
    ///   <para>If no more moves are possible you can end whole defragmentation.</para>
    /// </remarks>
    public HRESULT EndPass(D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo)
    {
        D3D12MA_ASSERT(pPassInfo != null);
        return m_Pimpl->DefragmentPassEnd(ref *pPassInfo);
    }

    /// <summary>Returns statistics of the defragmentation performed so far.</summary>
    /// <param name="pStats"></param>
    public void GetStats(D3D12MA_DEFRAGMENTATION_STATS* pStats)
    {
        D3D12MA_ASSERT(pStats != null);
        m_Pimpl->GetStats(out *pStats);
    }
}
