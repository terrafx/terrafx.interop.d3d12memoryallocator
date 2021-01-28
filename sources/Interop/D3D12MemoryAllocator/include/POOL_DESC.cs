// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;

namespace TerraFX.Interop
{
    /// <summary>Parameters of created <see cref="Pool"/> object. To be used with <see cref="Allocator.CreatePool"/>.</summary>
    public struct POOL_DESC
    {
        /// <summary>
        /// The type of memory heap where allocations of this pool should be placed.
        /// <para>It must be one of: <see cref="D3D12_HEAP_TYPE_DEFAULT"/>, <see cref="D3D12_HEAP_TYPE_UPLOAD"/>, <see cref="D3D12_HEAP_TYPE_READBACK"/>.</para>
        /// </summary>
        public D3D12_HEAP_TYPE HeapType;

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
    }
}
