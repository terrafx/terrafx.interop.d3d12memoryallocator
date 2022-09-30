// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of created <see cref="D3D12MA_Pool" /> object. To be used with <see cref="D3D12MA_Allocator.CreatePool" />.</summary>
public unsafe partial struct D3D12MA_POOL_DESC
{
    /// <summary>Flags.</summary>
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
    ///   <para>You can specify additional flags if needed.</para>
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
}
