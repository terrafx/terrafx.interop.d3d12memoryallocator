// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESIDENCY_PRIORITY;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of created <see cref="D3D12MA_Pool" /> object. To be used with <see cref="D3D12MA_Allocator.CreatePool" />.</summary>
public unsafe partial struct D3D12MA_POOL_DESC
{
    /// <summary>Flags for the heap.</summary>
    /// <remarks>It is recommended to use <see cref="D3D12MA_RECOMMENDED_HEAP_FLAGS" />.</remarks>
    public D3D12MA_POOL_FLAGS Flags;

    /// <summary>The parameters of memory heap where allocations of this pool should be placed.</summary>
    /// <remarks>In the simplest case, just fill it with zeros and set <see cref="D3D12_HEAP_PROPERTIES.Type" /> to one of: <see cref="D3D12_HEAP_TYPE_DEFAULT" />, <see cref="D3D12_HEAP_TYPE_UPLOAD" />, <see cref="D3D12_HEAP_TYPE_READBACK" />. Additional parameters can be used e.g. to utilize UMA.</remarks>
    public D3D12_HEAP_PROPERTIES HeapProperties;

    /// <summary>Heap flags to be used when allocating heaps of this pool.</summary>
    /// <remarks>
    ///   <para>It should contain one of these values, depending on type of resources you are going to create in this heap:</para>
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
    ///   <para>Except if <c>ResourceHeapTier = 2</c>, then it may be <see cref="D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES" />.</para>
    ///   <para>It is recommended to also add <see cref="D3D12MA_RECOMMENDED_POOL_FLAGS" />. You can specify additional flags if needed.</para>
    /// </remarks>
    public D3D12_HEAP_FLAGS HeapFlags;

    /// <summary>Size of a single heap (memory block) to be allocated as part of this pool, in bytes. Optional.</summary>
    /// <remarks>Specify nonzero to set explicit, constant size of memory blocks used by this pool. Leave 0 to use default and let the library manage block sizes automatically. Then sizes of particular blocks may vary.</remarks>
    [NativeTypeName("UINT64")]
    public ulong BlockSize;

    /// <summary>Minimum number of heaps (memory blocks) to be always allocated in this pool, even if they stay empty. Optional.</summary>
    /// <remarks>Set to 0 to have no preallocated blocks and allow the pool be completely empty.</remarks>
    [NativeTypeName("UINT")]
    public uint MinBlockCount;

    /// <summary>Maximum number of heaps (memory blocks) that can be allocated in this pool. Optional.</summary>
    /// <remarks>
    ///   <para>Set to 0 to use default, which is <see cref="ulong.MaxValue" />, which means no limit.</para>
    ///   <para>Set to same value as <see cref="MinBlockCount" /> to have fixed amount of memory allocated throughout whole lifetime of this pool.</para>
    /// </remarks>
    [NativeTypeName("UINT")]
    public uint MaxBlockCount;

    /// <summary>Additional minimum alignment to be used for all allocations created from this pool. Can be 0.</summary>
    /// <remarks>Leave 0 (default) not to impose any additional alignment. If not 0, it must be a power of two.</remarks>
    [NativeTypeName("UINT64")]
    public ulong MinAllocationAlignment;

    /// <summary>Additional parameter allowing pool to create resources with passed protected session.</summary>
    /// <remarks>If not null then all the heaps and committed resources will be created with this parameter. Valid only if <see cref="ID3D12Device4" /> interface is present in current Windows SDK!</remarks>
    public ID3D12ProtectedResourceSession* pProtectedSession;

    /// <summary>Residency priority to be set for all allocations made in this pool. Optional.</summary>
    /// <remarks>
    ///   <para>Set this parameter to one of the possible enum values e.g. <see cref="D3D12_RESIDENCY_PRIORITY_HIGH" /> to apply specific residency priority to all allocations made in this pool: <see cref="ID3D12Heap" /> memory blocks used to sub-allocate for placed resources, as well as committed resources or heaps created when <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED" /> is used. This can increase/decrease chance that the memory will be pushed out from VRAM to system RAM when the system runs out of memory, which is invisible to the developer using D3D12 API while it can degrade performance.</para>
    ///   <para>Priority is set using function <see cref="ID3D12Device1.SetResidencyPriority" />. It is performed only when <see cref="ID3D12Device1" /> interface is defined and successfully obtained. Otherwise, this parameter is ignored.</para>
    ///   <para>This parameter is optional.If you set it to <c>default(<see cref="D3D12_RESIDENCY_PRIORITY" />)</c>, residency priority will not be set for allocations made in this pool.</para>
    ///   <para>There is no equivalent parameter for allocations made in default pools. If you want to set residency priority for such allocation, you need to do it manually: allocate with <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED" /> and call <see cref="ID3D12Device1.SetResidencyPriority" />, passing <see cref="D3D12MA_Allocation.GetResource" />.</para>
    /// </remarks>
    public D3D12_RESIDENCY_PRIORITY ResidencyPriority;
}
