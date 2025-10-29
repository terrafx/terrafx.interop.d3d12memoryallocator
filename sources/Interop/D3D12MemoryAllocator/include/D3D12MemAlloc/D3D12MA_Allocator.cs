// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_POOL_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Represents main object of this library initialized for particular <see cref="ID3D12Device" />.</summary>
/// <remarks>
///   <para>Fill structure <see cref="D3D12MA_ALLOCATOR_DESC" /> and call function <see cref="D3D12MA_CreateAllocator" /> to create it. Call method <see cref="Release()" /> to destroy it.</para>
///   <para>It is recommended to create just one object of this type per <see cref="ID3D12Device" /> object, right after Direct3D 12 is initialized and keep it alive until before Direct3D device is destroyed.</para>
/// </remarks>
[NativeTypeName("class D3D12MA::Allocator : D3D12MA::IUnknownImpl")]
[NativeInheritance("D3D12MA::IUnknownImpl")]
public unsafe partial struct D3D12MA_Allocator : D3D12MA_IUnknownImpl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));

    public D3D12MA_IUnknownImpl Base;

    internal D3D12MA_AllocatorPimpl* m_Pimpl;

    public static D3D12MA_Allocator* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::ALLOCATOR_DESC &")] in D3D12MA_ALLOCATOR_DESC desc)
    {
        D3D12MA_Allocator* result = D3D12MA_NEW<D3D12MA_Allocator>(allocs);
        result->_ctor(allocationCallbacks, desc);
        return result;
    }

    private void _ctor()
    {
        Base = new D3D12MA_IUnknownImpl {
            lpVtbl = VtblInstance,
        };
    }

    private void _ctor([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::ALLOCATOR_DESC &")] in D3D12MA_ALLOCATOR_DESC desc)
    {
        _ctor();
        m_Pimpl = D3D12MA_AllocatorPimpl.Create(allocationCallbacks, allocationCallbacks, desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_Allocator*, Guid*, void**, int>)(Base.lpVtbl[0]))((D3D12MA_Allocator*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_Allocator*, uint>)(Base.lpVtbl[1]))((D3D12MA_Allocator*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_Allocator*, uint>)(Base.lpVtbl[2]))((D3D12MA_Allocator*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    void IDisposable.Dispose()
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_Allocator*, void>)(Base.lpVtbl[3]))((D3D12MA_Allocator*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    void D3D12MA_IUnknownImpl.Interface.ReleaseThis()
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_Allocator*, void>)(Base.lpVtbl[4]))((D3D12MA_Allocator*)Unsafe.AsPointer(ref this));
    }

    /// <summary>Returns cached options retrieved from D3D12 device.</summary>
    /// <returns></returns>
    [return: NativeTypeName("const D3D12_FEATURE_DATA_D3D12_OPTIONS &")]
    public readonly D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options()
    {
        return (D3D12_FEATURE_DATA_D3D12_OPTIONS*)(Unsafe.AsPointer(ref Unsafe.AsRef(in m_Pimpl->GetD3D12Options())));
    }

    /// <summary>Returns true if `D3D12_FEATURE_DATA_ARCHITECTURE1::UMA` was found to be true.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <para>For more information about how to use it, see articles in Microsoft Docs articles:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>"UMA Optimizations: CPU Accessible Textures and Standard Swizzle"</description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="D3D12_FEATURE_DATA_ARCHITECTURE" /> structure (d3d12.h)"</description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="ID3D12Device.GetCustomHeapProperties" /> method (d3d12.h)"</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public readonly BOOL IsUMA()
    {
        return m_Pimpl->IsUMA();
    }

    /// <summary>Returns true if `D3D12_FEATURE_DATA_ARCHITECTURE1::CacheCoherentUMA` was found to be true.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <para>For more information about how to use it, see articles in Microsoft Docs articles:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>"UMA Optimizations: CPU Accessible Textures and Standard Swizzle"</description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="D3D12_FEATURE_DATA_ARCHITECTURE" /> structure (d3d12.h)"</description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="ID3D12Device.GetCustomHeapProperties" /> method (d3d12.h)"</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public readonly BOOL IsCacheCoherentUMA()
    {
        return m_Pimpl->IsCacheCoherentUMA();
    }

    /// <summary>Returns true if GPU Upload Heaps are supported on the current system.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <para>When true, you can use <see cref="D3D12_HEAP_TYPE_GPU_UPLOAD" />.</para>
    ///   <para>This flag is fetched from <see cref="D3D12_FEATURE_D3D12_OPTIONS16.GPUUploadHeapSupported" />.</para>
    /// </remarks>
    public readonly BOOL IsGPUUploadHeapSupported()
    {
        return m_Pimpl->IsGPUUploadHeapSupported();
    }

    /// <summary>Returns total amount of memory of specific segment group, in bytes.</summary>
    /// <param name="memorySegmentGroup">Use <see cref="DXGI_MEMORY_SEGMENT_GROUP_LOCAL" /> or <see cref="DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL" />.</param>
    /// <returns></returns>
    /// <remarks>
    ///   <para>This information is taken from <see cref="DXGI_ADAPTER_DESC" />. It is not recommended to use this number. You should preferably call <see cref="GetBudget" /> and limit memory usage to <see cref="D3D12MA_Budget.BudgetBytes" /> instead.</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>
    ///         <para>When <c>IsUMA() == FALSE</c> (discrete graphics card):</para>
    ///         <list type="bullet">
    ///           <item>
    ///             <description><c>GetMemoryCapacity(DXGI_MEMORY_SEGMENT_GROUP_LOCAL)</c> returns the size of the video memory.</description>
    ///           </item>
    ///           <item>
    ///             <description><c>GetMemoryCapacity(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL)</c> returns the size of the system memory available for D3D12 resources.</description>
    ///           </item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         <para>When <c>IsUMA() == TRUE</c> (integrated graphics chip):</para>
    ///         <list type="bullet">
    ///           <item>
    ///             <description><c>GetMemoryCapacity(DXGI_MEMORY_SEGMENT_GROUP_LOCAL)</c> returns the size of the shared memory available for all D3D12 resources. All memory is considered "local".</description>
    ///           </item>
    ///           <item>
    ///             <description><c>GetMemoryCapacity(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL)</c> is not applicable and returns 0.</description>
    ///           </item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///   </list>
    /// </remarks>
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetMemoryCapacity([NativeTypeName("UINT")] uint memorySegmentGroup)
    {
        return m_Pimpl->GetMemoryCapacity(memorySegmentGroup);
    }

    /// <summary>Allocates memory and creates a D3D12 resource (buffer or texture). This is the main allocation function.</summary>
    /// <param name="pAllocDesc">Parameters of the allocation.</param>
    /// <param name="pResourceDesc">Description of created resource.</param>
    /// <param name="InitialResourceState">Initial resource state.</param>
    /// <param name="pOptimizedClearValue">Optional. Either null or optimized clear value.</param>
    /// <param name="ppAllocation">Filled with pointer to new allocation object created.</param>
    /// <param name="riidResource">IID of a resource to be returned via `ppvResource`.</param>
    /// <param name="ppvResource">Optional. If not null, filled with pointer to new resouce created.</param>
    /// <returns></returns>
    /// <remarks>
    ///   <para>The function is similar to <see cref="ID3D12Device.CreateCommittedResource" />, but it may really call <see cref="ID3D12Device.CreatePlacedResource" /> to assign part of a larger, existing memory heap to the new resource, which is the main purpose of this whole library.</para>
    ///   <para>If <paramref name="ppvResource" /> is null, you receive only <paramref name="ppAllocation" /> object from this function. It holds pointer to <see cref="ID3D12Resource" /> that can be queried using function <see cref="D3D12MA_Allocation.GetResource" />. Reference count of the resource object is 1. It is automatically destroyed when you destroy the allocation object.</para>
    ///   <para>If <paramref name="ppvResource" /> is not null, you receive pointer to the resource next to allocation object. Reference count of the resource object is then increased by calling <see cref="ID3D12Resource.QueryInterface" />, so you need to manually <see cref="ID3D12Resource.Release" /> it along with the allocation.</para>
    ///   <para>NOTE: This function creates a new resource. Sub-allocation of parts of one large buffer, although recommended as a good practice, is out of scope of this library and could be implemented by the user as a higher-level logic on top of it, e.g. using the virtual_allocator feature.</para>
    /// </remarks>
    public HRESULT CreateResource([NativeTypeName("const D3D12MA::ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.CreateResource.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->CreateResource(pAllocDesc, new D3D12MA_CREATE_RESOURCE_PARAMS(pResourceDesc, InitialResourceState, pOptimizedClearValue), ppAllocation, riidResource, ppvResource);
    }

    /// <summary>Similar to Allocator::CreateResource, but supports new structure <see cref="D3D12_RESOURCE_DESC1" />.</summary>
    /// <param name="pAllocDesc"></param>
    /// <param name="pResourceDesc"></param>
    /// <param name="InitialResourceState"></param>
    /// <param name="pOptimizedClearValue"></param>
    /// <param name="ppAllocation"></param>
    /// <param name="riidResource"></param>
    /// <param name="ppvResource"></param>
    /// <returns></returns>
    /// <remarks>
    ///   <para>It internally uses <see cref="ID3D12Device8.CreateCommittedResource2" /> or <see cref="ID3D12Device8.CreatePlacedResource1"/>.</para>
    ///   <para>To work correctly, <see cref="ID3D12Device8" />interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE" /> is returned.</para>
    /// </remarks>
    public HRESULT CreateResource2([NativeTypeName("const D3D12MA::ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.CreateResource2.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->CreateResource(pAllocDesc, new D3D12MA_CREATE_RESOURCE_PARAMS(pResourceDesc, InitialResourceState, pOptimizedClearValue), ppAllocation, riidResource, ppvResource);
    }

    /// <summary>Similar to <see cref="CreateResource2" />, but there are initial layout instead of state and castable formats list</summary>
    /// <param name="pAllocDesc"></param>
    /// <param name="pResourceDesc"></param>
    /// <param name="InitialLayout"></param>
    /// <param name="pOptimizedClearValue"></param>
    /// <param name="NumCastableFormats"></param>
    /// <param name="pCastableFormats"></param>
    /// <param name="ppAllocation"></param>
    /// <param name="riidResource"></param>
    /// <param name="ppvResource"></param>
    /// <returns></returns>
    /// <remarks>
    ///   <para>It internally uses <see cref="ID3D12Device10.CreateCommittedResource3" /> or <see cref="ID3D12Device10.CreatePlacedResource2" />.</para>
    ///   <para>To work correctly, <see cref="ID3D12Device10" /> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE" /> is returned. If you use <paramref name="pCastableFormats" />, <see cref="ID3D12Device12" /> must also be available.</para>
    /// </remarks>
    public HRESULT CreateResource3([NativeTypeName("const D3D12MA::ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        if ((pAllocDesc == null) || (pResourceDesc == null) || (ppAllocation == null))
        {
            D3D12MA_FAIL("Invalid arguments passed to Allocator::CreateResource3.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->CreateResource(pAllocDesc, new D3D12MA_CREATE_RESOURCE_PARAMS(pResourceDesc, InitialLayout, pOptimizedClearValue, NumCastableFormats, pCastableFormats), ppAllocation, riidResource, ppvResource);
    }

    /// <summary>Allocates memory without creating any resource placed in it.</summary>
    /// <param name="pAllocDesc"></param>
    /// <param name="pAllocInfo"></param>
    /// <param name="ppAllocation"></param>
    /// <returns></returns>
    /// <remarks>
    ///   <para>This function is similar to <see cref="ID3D12Device.CreateHeap" />, but it may really assign part of a larger, existing heap to the allocation.</para>
    ///   <para><c>pAllocDesc->heapFlags</c> should contain one of these values, depending on type of resources you are going to create in this memory:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description><see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS" /></description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES" /></description>
    ///     </item>
    ///     <item>
    ///       <description><see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES" /></description>
    ///     </item>
    ///   </list>
    ///   <para>Except if you validate that <c>ResourceHeapTier = 2</c> - then <c>heapFlags</c> may be <see cref="D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES" />. Additional flags in <c>heapFlags</c> are allowed as well.</para>
    ///   <para><c>pAllocInfo->SizeInBytes</c> must be multiply of 64KB. <c>pAllocInfo->Alignment</c> must be one of the legal values as described in documentation of <see cref="D3D12_HEAP_DESC" />.</para>
    ///   <para>If you use <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED" /> you will get a separate memory block - a heap that always has offset 0.</para>
    /// </remarks>
    public HRESULT AllocateMemory([NativeTypeName("const D3D12MA::ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO *")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation)
    {
        if (!D3D12MA_ValidateAllocateMemoryParameters(pAllocDesc, pAllocInfo, ppAllocation))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.AllocateMemory.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->AllocateMemory(pAllocDesc, pAllocInfo, ppAllocation);
    }

    /// <summary>Creates a new resource in place of an existing allocation. This is useful for memory aliasing.</summary>
    /// <param name="pAllocation">Existing allocation indicating the memory where the new resource should be created. It can be created using <see cref="CreateResource" /> and already have a resource bound to it, or can be a raw memory allocated with <see cref="AllocateMemory" />. It must not be created as committed so that <see cref="ID3D12Heap" /> is available and not implicit.</param>
    /// <param name="AllocationLocalOffset">Additional offset in bytes to be applied when allocating the resource. Local from the start of <paramref name="pAllocation" />, not the beginning of the whole <see cref="ID3D12Heap" />! If the new resource should start from the beginning of the `pAllocation` it should be 0.</param>
    /// <param name="pResourceDesc">Description of the new resource to be created.</param>
    /// <param name="InitialResourceState"></param>
    /// <param name="pOptimizedClearValue"></param>
    /// <param name="riidResource"></param>
    /// <param name="ppvResource">Returns pointer to the new resource. The resource is not bound with <paramref name="pAllocation" />. This pointer must not be null - you must get the resource pointer and <see cref="ID3D12Resource.Release" /> it when no longer needed.</param>
    /// <returns></returns>
    /// <remarks>Memory requirements of the new resource are checked for validation. If its size exceeds the end of <paramref name="pAllocation" /> or required alignment is not fulfilled considering <c>pAllocation->GetOffset() + AllocationLocalOffset</c>, the function returns <see cref="E_INVALIDARG" />.</remarks>
    public HRESULT CreateAliasingResource(D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        if ((pAllocation == null) || (pResourceDesc == null) || (ppvResource == null))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.CreateAliasingResource.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->CreateAliasingResource(pAllocation, AllocationLocalOffset, new D3D12MA_CREATE_RESOURCE_PARAMS(pResourceDesc, InitialResourceState, pOptimizedClearValue), riidResource, ppvResource);
    }

    /// <summary>Similar to <see cref="CreateAliasingResource" />, but supports new structure <see cref="D3D12_RESOURCE_DESC1" />.</summary>
    /// <param name="pAllocation"></param>
    /// <param name="AllocationLocalOffset"></param>
    /// <param name="pResourceDesc"></param>
    /// <param name="InitialResourceState"></param>
    /// <param name="pOptimizedClearValue"></param>
    /// <param name="riidResource"></param>
    /// <param name="ppvResource"></param>
    /// <returns></returns>
    /// <remarks>
    ///   <para>It internally uses <see cref="ID3D12Device8.CreatePlacedResource1" />.</para>
    ///   <para>To work correctly, <see cref="ID3D12Device8" /> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE" /> is returned.</para>
    /// </remarks>
    public HRESULT CreateAliasingResource1(D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        if ((pAllocation == null) || (pResourceDesc == null) || (ppvResource == null))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.CreateAliasingResource.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->CreateAliasingResource(pAllocation, AllocationLocalOffset, new D3D12MA_CREATE_RESOURCE_PARAMS(pResourceDesc, InitialResourceState, pOptimizedClearValue), riidResource, ppvResource);
    }

    /// <summary>Similar to <see cref="CreateAliasingResource1" />, but there are initial layout instead of state and castable formats list.</summary>
    /// <param name="pAllocation"></param>
    /// <param name="AllocationLocalOffset"></param>
    /// <param name="pResourceDesc"></param>
    /// <param name="InitialLayout"></param>
    /// <param name="pOptimizedClearValue"></param>
    /// <param name="NumCastableFormats"></param>
    /// <param name="pCastableFormats"></param>
    /// <param name="riidResource"></param>
    /// <param name="ppvResource"></param>
    /// <returns></returns>
    /// <remarks>
    ///   <para>It internally uses <see cref="ID3D12Device10.CreatePlacedResource2" />.</para>
    ///   <para>To work correctly, <see cref="ID3D12Device10" /> interface must be available in the current system. Otherwise, <see cref="E_NOINTERFACE" /> is returned. If you use <paramref name="pCastableFormats" />, <see cref="ID3D12Device12" /> must albo be available.</para>
    /// </remarks>
    public HRESULT CreateAliasingResource2(D3D12MA_Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        if ((pAllocation == null) || (pResourceDesc == null) || (ppvResource == null))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.CreateAliasingResource.");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        return m_Pimpl->CreateAliasingResource(pAllocation, AllocationLocalOffset, new D3D12MA_CREATE_RESOURCE_PARAMS(pResourceDesc, InitialLayout, pOptimizedClearValue, NumCastableFormats, pCastableFormats), riidResource, ppvResource);
    }

    /// <summary>Creates custom pool.</summary>
    /// <param name="pPoolDesc"></param>
    /// <param name="ppPool"></param>
    /// <returns></returns>
    public HRESULT CreatePool([NativeTypeName("const D3D12MA::POOL_DESC *")] D3D12MA_POOL_DESC* pPoolDesc, D3D12MA_Pool** ppPool)
    {
        if ((pPoolDesc == null) || (ppPool == null) || ((pPoolDesc->MaxBlockCount > 0) && (pPoolDesc->MaxBlockCount < pPoolDesc->MinBlockCount)) || ((pPoolDesc->MinAllocationAlignment > 0) && !D3D12MA_IsPow2(pPoolDesc->MinAllocationAlignment)))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.CreatePool.");
            return E_INVALIDARG;
        }

        if (((pPoolDesc->Flags & D3D12MA_POOL_FLAG_ALWAYS_COMMITTED) != 0) && ((pPoolDesc->BlockSize != 0) || (pPoolDesc->MinBlockCount > 0)))
        {
            D3D12MA_FAIL("Invalid arguments passed to D3D12MA_Allocator.CreatePool while D3D12MA_POOL_FLAG_ALWAYS_COMMITTED is specified.");
            return E_INVALIDARG;
        }

        if (!m_Pimpl->HeapFlagsFulfillResourceHeapTier(pPoolDesc->HeapFlags))
        {
            D3D12MA_FAIL("Invalid pPoolDesc->HeapFlags passed to D3D12MA_Allocator.CreatePool. Did you forget to handle ResourceHeapTier=1?");
            return E_INVALIDARG;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        *ppPool = D3D12MA_Pool.Create(m_Pimpl->GetAllocs(), (D3D12MA_Allocator*)(Unsafe.AsPointer(ref this)), *pPoolDesc);

        HRESULT hr = (*ppPool)->m_Pimpl->Init();

        if (SUCCEEDED(hr))
        {
            m_Pimpl->RegisterPool(*ppPool, pPoolDesc->HeapProperties.Type);
        }
        else
        {
            D3D12MA_DELETE(m_Pimpl->GetAllocs(), *ppPool);
            *ppPool = null;
        }
        return hr;
    }

    /// <summary>Sets the index of the current frame.</summary>
    /// <param name="frameIndex"></param>
    /// <remarks>This function is used to set the frame index in the allocator when a new game frame begins.</remarks>
    public void SetCurrentFrameIndex([NativeTypeName("UINT")] uint frameIndex)
    {
        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->SetCurrentFrameIndex(frameIndex);
    }

    /// <summary>Retrieves information about current memory usage and budget.</summary>
    /// <param name="pLocalBudget">Optional, can be null.</param>
    /// <param name="pNonLocalBudget">Optional, can be null.</param>
    /// <remarks>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>
    ///         <para>When <c>IsUMA() == FALSE</c> (discrete graphics card):</para>
    ///         <list type="bullet">
    ///           <item>
    ///             <description><paramref name="pLocalBudget"/> returns the budget of the video memory.</description>
    ///           </item>
    ///           <item>
    ///             <description><paramref name="pNonLocalBudget" /> returns the budget of the system memory available for D3D12 resources.</description>
    ///           </item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         <para>When <c>IsUMA() == TRUE</c> (integrated graphics chip):</para>
    ///         <list type="bullet">
    ///           <item>
    ///             <description><paramref name="pLocalBudget" /> returns the budget of the shared memory available for all D3D12 resources. All memory is considered "local".</description>
    ///           </item>
    ///           <item>
    ///             <description><paramref name="pNonLocalBudget" /> is not applicable and returns zeros.</description>
    ///           </item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///   </list>
    ///   <para>This function is called "get" not "calculate" because it is very fast, suitable to be called every frame or every allocation.For more detailed statistics use <see cref="CalculateStatistics" />.</para>
    ///   <para>Note that when using allocator from multiple threads, returned information may immediately become outdated.</para>
    /// </remarks>
    public void GetBudget(D3D12MA_Budget* pLocalBudget, D3D12MA_Budget* pNonLocalBudget)
    {
        if ((pLocalBudget == null) && (pNonLocalBudget == null))
        {
            return;
        }

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->GetBudget(pLocalBudget, pNonLocalBudget);
    }

    /// <summary>Retrieves statistics from current state of the allocator.</summary>
    /// <param name="pStats"></param>
    /// <remarks>
    ///   <para>This function is called "calculate" not "get" because it has to traverse all internal data structures, so it may be quite slow. Use it for debugging purposes. For faster but more brief statistics suitable to be called every frame or every allocation, use <see cref="GetBudget" />.</para>
    ///   <para>Note that when using allocator from multiple threads, returned information may immediately become outdated.</para>
    /// </remarks>
    public void CalculateStatistics(D3D12MA_TotalStatistics* pStats)
    {
        D3D12MA_ASSERT(pStats != null);

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->CalculateStatistics(out *pStats);
    }

    /// <summary>Builds and returns statistics as a string in JSON format.</summary>
    /// <param name="ppStatsString">Must be freed using <see cref="FreeStatsString" />.</param>
    /// <param name="DetailedMap"><see cref="TRUE" /> to include full list of allocations (can make the string quite long), <see cref="FALSE" /> to only return statistics.</param>
    public readonly void BuildStatsString([NativeTypeName("WCHAR **")] char** ppStatsString, BOOL DetailedMap)
    {
        D3D12MA_ASSERT(ppStatsString != null);

        using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
        m_Pimpl->BuildStatsString(ppStatsString, DetailedMap);
    }

    /// <summary>Frees memory of a string returned from Allocator::BuildStatsString.</summary>
    /// <param name="pStatsString"></param>
    public readonly void FreeStatsString([NativeTypeName("WCHAR *")] char* pStatsString)
    {
        if (pStatsString != null)
        {
            using D3D12MA_MutexLock debugGlobalMutexLock = new D3D12MA_MutexLock(ref *g_DebugGlobalMutex, true);
            m_Pimpl->FreeStatsString(pStatsString);
        }
    }

    /// <summary>Begins defragmentation process of the default pools.</summary>
    /// <param name="pDesc">Structure filled with parameters of defragmentation.</param>
    /// <param name="ppContext">Context object that will manage defragmentation.</param>
    /// <remarks>For more information about defragmentation, see documentation chapter: Defragmentation.</remarks>
    public void BeginDefragmentation([NativeTypeName("const D3D12MA::DEFRAGMENTATION_DESC *")] D3D12MA_DEFRAGMENTATION_DESC* pDesc, D3D12MA_DefragmentationContext** ppContext)
    {
        D3D12MA_ASSERT((pDesc != null) && (ppContext != null));
        *ppContext = D3D12MA_DefragmentationContext.Create(m_Pimpl->GetAllocs(), m_Pimpl, *pDesc, null);
    }
}
