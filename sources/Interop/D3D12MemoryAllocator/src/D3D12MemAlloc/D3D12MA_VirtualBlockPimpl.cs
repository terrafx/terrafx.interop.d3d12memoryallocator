// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MA_VIRTUAL_BLOCK_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_VirtualBlockPimpl : IDisposable
{
    public D3D12MA_ALLOCATION_CALLBACKS m_AllocationCallbacks;

    [NativeTypeName("UINT64")]
    public ulong m_Size;

    public D3D12MA_BlockMetadata* m_Metadata;

    public static D3D12MA_VirtualBlockPimpl* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::VIRTUAL_BLOCK_DESC &")] in D3D12MA_VIRTUAL_BLOCK_DESC desc)
    {
        D3D12MA_VirtualBlockPimpl* result = D3D12MA_NEW<D3D12MA_VirtualBlockPimpl>(allocs);
        result->_ctor(allocationCallbacks, desc);
        return result;
    }

    private void _ctor([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("const D3D12MA::VIRTUAL_BLOCK_DESC &")] in D3D12MA_VIRTUAL_BLOCK_DESC desc)
    {
        m_AllocationCallbacks = allocationCallbacks;
        m_Size = desc.Size;

        switch (desc.Flags & D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_MASK)
        {
            case D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR:
            {
                D3D12MA_BlockMetadata_Linear* metadata = D3D12MA_BlockMetadata_Linear.Create(allocationCallbacks, (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(in m_AllocationCallbacks)), true);
                m_Metadata = (D3D12MA_BlockMetadata*)(metadata);
                break;
            }

            case 0:
            {
                D3D12MA_BlockMetadata_TLSF* metadata = D3D12MA_BlockMetadata_TLSF.Create(allocationCallbacks, (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(in m_AllocationCallbacks)), true);
                m_Metadata = (D3D12MA_BlockMetadata*)(metadata);
                break;
            }

            default:
            {
                D3D12MA_FAIL();
                break;
            }
        }

        m_Metadata->Init(m_Size);
    }

    public readonly void Dispose()
    {
        D3D12MA_DELETE(m_AllocationCallbacks, m_Metadata);
    }
}
