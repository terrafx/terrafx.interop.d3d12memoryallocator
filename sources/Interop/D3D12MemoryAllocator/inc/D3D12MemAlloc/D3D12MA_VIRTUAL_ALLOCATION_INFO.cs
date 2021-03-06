// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    /// <summary>Parameters of an existing virtual allocation, returned by <see cref="D3D12MA_VirtualBlock.GetAllocationInfo"/>.</summary>
    public unsafe struct D3D12MA_VIRTUAL_ALLOCATION_INFO
    {
        /// <summary>
        /// Size of the allocation.
        /// <para>Same value as passed in <see cref="D3D12MA_VIRTUAL_ALLOCATION_DESC.Size"/>.</para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong size;

        /// <summary>
        /// Custom pointer associated with the allocation.
        /// <para>Same value as passed in <see cref="D3D12MA_VIRTUAL_ALLOCATION_DESC.pUserData"/> or <see cref="D3D12MA_VirtualBlock.SetAllocationUserData"/>.</para>
        /// </summary>
        public void* pUserData;
    }
}
