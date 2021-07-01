// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit febc1c2cf66eb9211f36da800b50cf73dfde8d68
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.D3D12_HEAP_FLAGS;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_CommittedAllocationParameters
    {
        public D3D12MA_CommittedAllocationList* m_List;
        public D3D12_HEAP_PROPERTIES m_HeapProperties;
        public D3D12_HEAP_FLAGS m_HeapFlags;

        public static void _ctor(out D3D12MA_CommittedAllocationParameters pThis)
        {
            pThis.m_List = null;
            pThis.m_HeapProperties = default;
            pThis.m_HeapFlags = D3D12_HEAP_FLAG_NONE;
        }

        public readonly bool IsValid() => m_List != null;
    }
}
