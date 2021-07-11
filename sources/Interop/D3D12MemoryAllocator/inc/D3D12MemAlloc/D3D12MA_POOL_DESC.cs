// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;

namespace TerraFX.Interop
{
    /// <summary>Parameters of created <see cref="D3D12MA_Pool"/> object. To be used with <see cref="D3D12MA_Allocator.CreatePool"/>.</summary>
    public struct D3D12MA_POOL_DESC
    {
        /// <summary>
        /// The parameters of memory heap where allocations of this pool should be placed.
        /// <para>In the simplest case, just fill it with zeros and set <see cref="D3D12_HEAP_PROPERTIES.Type"/> to one of: <see cref="D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT"/>, <see cref="D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD"/>, <see cref="D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_READBACK"/>. Additional parameters can be used e.g.to utilize UMA.</para>
        /// </summary>
        public D3D12_HEAP_PROPERTIES HeapProperties;

        /// <summary>
        /// Heap flags to be used when allocating heaps of this pool.
        /// <para>
        /// It should contain one of these values, depending on type of resources you are going to create in this heap:
        /// <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS"/>,
        /// <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES"/>,
        /// <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES"/>.
        /// Except if <c>ResourceHeapTier = 2</c>, then it may be <see cref="D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES"/> <c>= 0</c>.
        /// </para>
        /// <para>You can specify additional flags if needed.</para>
        /// </summary>
        public D3D12_HEAP_FLAGS HeapFlags;

        /// <summary>
        /// Size of a single heap (memory block) to be allocated as part of this pool, in bytes. Optional.
        /// <para>
        /// Specify nonzero to set explicit, constant size of memory blocks used by this pool.
        /// Leave 0 to use default and let the library manage block sizes automatically.
        /// Then sizes of particular blocks may vary.
        /// </para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong BlockSize;

        /// <summary>
        /// Minimum number of heaps (memory blocks) to be always allocated in this pool, even if they stay empty. Optional.
        /// <para>Set to 0 to have no preallocated blocks and allow the pool be completely empty.</para>
        /// </summary>
        [NativeTypeName("UINT")]
        public uint MinBlockCount;

        /// <summary>
        /// Maximum number of heaps (memory blocks) that can be allocated in this pool. Optional.
        /// <para>Set to 0 to use default, which is <see cref="UINT64_MAX"/>, which means no limit.</para>
        /// <para>Set to same value as <see cref="MinBlockCount"/> to have fixed amount of memory allocated throughout whole lifetime of this pool.</para>
        /// </summary>
        [NativeTypeName("UINT")]
        public uint MaxBlockCount;

        /// <summary>
        /// Additional minimum alignment to be used for all allocations created from this pool. Can be 0.
        /// <para>Leave 0 (default) not to impose any additional alignment. If not 0, it must be a power of two.</para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong MinAllocationAlignment;
    }
}
