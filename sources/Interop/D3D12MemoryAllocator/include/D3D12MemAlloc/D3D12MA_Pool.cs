// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MA_POOL_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;

namespace TerraFX.Interop.DirectX;

/// <summary>Custom memory pool</summary>
/// <remarks>
///   <para>Represents a separate set of heaps (memory blocks) that can be used to create <see cref="D3D12MA_Allocation" />-s and resources in it.Usually there is no need to create custom pools - creating resources in default pool is sufficient.</para>
///   <para>To create custom pool, fill <see cref="D3D12MA_POOL_DESC" /> and call <see cref="D3D12MA_Allocator.CreatePool" />.</para>
/// </remarks>
[NativeTypeName("class D3D12MA::Pool : D3D12MA::IUnknownImpl")]
[NativeInheritance("D3D12MA::IUnknownImpl")]
public unsafe partial struct D3D12MA_Pool : D3D12MA_IUnknownImpl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));

    public D3D12MA_IUnknownImpl Base;

    internal D3D12MA_PoolPimpl* m_Pimpl;

    public static D3D12MA_Pool* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, D3D12MA_Allocator* allocator, [NativeTypeName("const D3D12MA::POOL_DESC &")] in D3D12MA_POOL_DESC desc)
    {
        D3D12MA_Pool* result = D3D12MA_NEW<D3D12MA_Pool>(allocs);
        result->_ctor(allocator, desc);
        return result;
    }

    private void _ctor()
    {
        Base = new D3D12MA_IUnknownImpl {
            lpVtbl = VtblInstance,
        };
    }

    private void _ctor(D3D12MA_Allocator* allocator, [NativeTypeName("const D3D12MA::POOL_DESC &")] in D3D12MA_POOL_DESC desc)
    {
        _ctor();
        m_Pimpl = D3D12MA_PoolPimpl.Create(allocator->m_Pimpl->GetAllocs(), allocator->m_Pimpl, desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged<D3D12MA_Pool*, Guid*, void**, int>)(Base.lpVtbl[0]))((D3D12MA_Pool*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged<D3D12MA_Pool*, uint>)(Base.lpVtbl[1]))((D3D12MA_Pool*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged<D3D12MA_Pool*, uint>)(Base.lpVtbl[2]))((D3D12MA_Pool*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    void IDisposable.Dispose()
    {
        ((delegate* unmanaged<D3D12MA_Pool*, void>)(Base.lpVtbl[3]))((D3D12MA_Pool*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    void D3D12MA_IUnknownImpl.Interface.ReleaseThis()
    {
        ((delegate* unmanaged<D3D12MA_Pool*, void>)(Base.lpVtbl[4]))((D3D12MA_Pool*)Unsafe.AsPointer(ref this));
    }

    /// <summary>Returns copy of parameters of the pool.</summary>
    /// <returns></returns>
    /// <remarks>These are the same parameters as passed to <see cref="D3D12MA_Allocator.CreatePool" />.</remarks>
    public readonly D3D12MA_POOL_DESC GetDesc()
    {
        return m_Pimpl->GetDesc();
    }

    /// <summary>Retrieves basic statistics of the custom pool that are fast to calculate.</summary>
    /// <param name="pStats">Statistics of the current pool.</param>
    public void GetStatistics(D3D12MA_Statistics* pStats)
    {
        D3D12MA_ASSERT(pStats != null);
        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->GetStatistics(out *pStats);
    }

    /// <summary>Retrieves detailed statistics of the custom pool that are slower to calculate.</summary>
    /// <param name="pStats">Statistics of the current pool.</param>
    public void CalculateStatistics(D3D12MA_DetailedStatistics* pStats)
    {
        D3D12MA_ASSERT(pStats != null);
        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->CalculateStatistics(out *pStats);
    }

    /// <summary>Associates a name with the pool. This name is for use in debug diagnostics and tools.</summary>
    /// <param name="Name"></param>
    /// <remarks>
    ///   <para>Internal copy of the string is made, so the memory pointed by the argument can be changed of freed immediately after this call.</para>
    ///   <para><paramref name="Name" /> can be <c>null</c>.</para>
    /// </remarks>
    public void SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->SetName(Name);
    }

    /// <summary>Returns the name associated with the pool object.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <para>Returned string points to an internal copy.</para>
    ///   <para>If no name was associated with the allocation, returns <c>null</c>.</para>
    /// </remarks>
    [return: NativeTypeName("LPCWSTR")]
    public readonly char* GetName()
    {
        return m_Pimpl->GetName();
    }

    /// <summary>Begins defragmentation process of the current pool.</summary>
    /// <param name="pDesc">Structure filled with parameters of defragmentation.</param>
    /// <param name="ppContext">Context object that will manage defragmentation.</param>
    /// <returns>
    ///   <list type="bullet">
    ///     <item>
    ///       <description><see cref="S_OK" /> if defragmentation can begin.</description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="E_NOINTERFACE" /> if defragmentation is not supported.</description>
    ///     </item>
    ///   </list>
    ///   <para>For more information about defragmentation, see documentation chapter: Defragmentation.</para>
    /// </returns>
    public HRESULT BeginDefragmentation([NativeTypeName("const D3D12MA::DEFRAGMENTATION_DESC *")] D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext)
    {
        D3D12MA_ASSERT((pDesc != null) && (ppContext != null));

        // Check for support
        if ((m_Pimpl->GetBlockVector()->GetAlgorithm() & (uint)(D3D12MA_POOL_FLAG_ALGORITHM_LINEAR)) != 0)
        {
            return E_NOINTERFACE;
        }

        D3D12MA_AllocatorPimpl* allocator = m_Pimpl->GetAllocator();
        *ppContext = D3D12MA_DefragmentationContext.Create(allocator->GetAllocs(), allocator, *pDesc, m_Pimpl->GetBlockVector());

        return S_OK;
    }
}
