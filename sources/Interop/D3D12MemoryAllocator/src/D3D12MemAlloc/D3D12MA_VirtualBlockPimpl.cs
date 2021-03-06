// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_VirtualBlockPimpl : IDisposable
    {
        public D3D12MA_ALLOCATION_CALLBACKS m_AllocationCallbacks;

        [NativeTypeName("UINT64")]
        public ulong m_Size;

        public D3D12MA_BlockMetadata_Generic m_Metadata;

        public static void _ctor(ref D3D12MA_VirtualBlockPimpl pThis, D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("UINT64")] ulong size)
        {
            pThis.m_AllocationCallbacks = *allocationCallbacks;
            pThis.m_Size = size;

            D3D12MA_BlockMetadata_Generic._ctor(ref pThis.m_Metadata, (D3D12MA_ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref pThis.m_AllocationCallbacks), true); // isVirtual

            pThis.m_Metadata.Init(pThis.m_Size);
        }

        public void Dispose()
        {
            m_Metadata.Dispose();
        }
    }
}
