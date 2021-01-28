// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>Parameters of created virtual allocation to be passed to VirtualBlock::Allocate().</summary>
    public unsafe struct VIRTUAL_ALLOCATION_DESC
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
