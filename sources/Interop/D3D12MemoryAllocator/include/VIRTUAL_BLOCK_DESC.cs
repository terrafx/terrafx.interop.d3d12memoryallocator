// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>Parameters of created D3D12MA::VirtualBlock object to be passed to CreateVirtualBlock().</summary>
    public unsafe struct VIRTUAL_BLOCK_DESC
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
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;
    }
}
