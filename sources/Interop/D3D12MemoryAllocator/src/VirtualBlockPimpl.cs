// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;

namespace TerraFX.Interop
{
    internal unsafe struct VirtualBlockPimpl : IDisposable
    {
        public readonly ALLOCATION_CALLBACKS m_AllocationCallbacks;

        [NativeTypeName("UINT64")]
        public readonly ulong m_Size;

        public BlockMetadata_Generic m_Metadata;

        public VirtualBlockPimpl(ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("UINT64")] ulong size)
        {
            m_AllocationCallbacks = *allocationCallbacks;
            m_Size = size;
            m_Metadata = new(allocationCallbacks, true); // isVirtual

            m_Metadata.Init(m_Size);
        }

        public void Dispose()
        {
        }
    }
}
