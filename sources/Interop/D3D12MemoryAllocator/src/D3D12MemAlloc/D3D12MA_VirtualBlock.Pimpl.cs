// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    public unsafe partial struct D3D12MA_VirtualBlock
    {
        private D3D12MA_IUnknownImpl m_IUnknownImpl;

        internal D3D12MA_ALLOCATION_CALLBACKS m_AllocationCallbacks;

        [NativeTypeName("UINT64")]
        internal ulong m_Size;

        internal D3D12MA_BlockMetadata_Generic m_Metadata;

        internal static void _ctor(ref D3D12MA_VirtualBlock pThis, D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("UINT64")] ulong size)
        {
            D3D12MA_IUnknownImpl._ctor(ref pThis.m_IUnknownImpl, Vtbl);

            pThis.m_AllocationCallbacks = *allocationCallbacks;
            pThis.m_Size = size;

            D3D12MA_BlockMetadata_Generic._ctor(ref pThis.m_Metadata, (D3D12MA_ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref pThis.m_AllocationCallbacks), true); // isVirtual

            pThis.m_Metadata.Init(pThis.m_Size);
        }
    }
}
