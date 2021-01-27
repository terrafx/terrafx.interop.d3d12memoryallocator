// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>Parameters of an existing virtual allocation, returned by VirtualBlock::GetAllocationInfo().</summary>
    public unsafe struct VIRTUAL_ALLOCATION_INFO
    {
        /// <summary>
        /// Size of the allocation.
        /// <para>Same value as passed in VIRTUAL_ALLOCATION_DESC::Size.</para>
        /// </summary>
        [NativeTypeName("UINT64")] public ulong size;

        /// <summary>
        /// Custom pointer associated with the allocation.
        /// <para>Same value as passed in VIRTUAL_ALLOCATION_DESC::pUserData or VirtualBlock::SetAllocationUserData().</para>
        /// </summary>
        public void* pUserData;
    }
}
