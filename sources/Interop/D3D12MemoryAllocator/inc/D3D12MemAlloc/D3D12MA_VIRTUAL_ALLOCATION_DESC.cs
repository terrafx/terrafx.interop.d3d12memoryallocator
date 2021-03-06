// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    /// <summary>Parameters of created virtual allocation to be passed to <see cref="D3D12MA_VirtualBlock.Allocate"/>.</summary>
    public unsafe struct D3D12MA_VIRTUAL_ALLOCATION_DESC
    {
        /// <summary>
        /// Size of the allocation.
        /// <para>Cannot be zero.</para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong Size;

        /// <summary>
        /// Required alignment of the allocation.
        /// <para>Must be power of two. Special value 0 has the same meaning as 1 - means no special alignment is required, so allocation can start at any offset.</para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong Alignment;

        /// <summary>
        /// Custom pointer to be associated with the allocation.
        /// <para>It can be fetched or changed later.</para>
        /// </summary>
        public void* pUserData;
    }
}
