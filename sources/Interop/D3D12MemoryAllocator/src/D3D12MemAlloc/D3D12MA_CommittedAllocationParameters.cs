// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_CommittedAllocationParameters
{
    public D3D12MA_CommittedAllocationList* m_List;

    public D3D12_HEAP_PROPERTIES m_HeapProperties;

    public D3D12_HEAP_FLAGS m_HeapFlags;

    public ID3D12ProtectedResourceSession* m_ProtectedSession;

    public bool m_CanAlias;

    public D3D12_RESIDENCY_PRIORITY m_ResidencyPriority;

    public readonly bool IsValid()
    {
        return m_List != null;
    }
}
