// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    /// <summary>Parameters of created <see cref="D3D12MA_VirtualBlock"/> object to be passed to <see cref="D3D12MemAlloc.D3D12MA_CreateVirtualBlock"/>.</summary>
    public unsafe struct D3D12MA_VIRTUAL_BLOCK_DESC
    {
        /// <summary>
        /// Total size of the block.
        /// <para>
        /// Sizes can be expressed in bytes or any units you want as long as you are consistent in using them.
        /// For example, if you allocate from some array of structures, 1 can mean single instance of entire structure.
        /// </para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong Size;

        /// <summary>
        /// Custom CPU memory allocation callbacks. Optional.
        /// <para>Optional, can be null. When specified, will be used for all CPU-side memory allocations.</para>
        /// </summary>
        [NativeTypeName("const ALLOCATION_CALLBACKS*")]
        public D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;
    }
}
