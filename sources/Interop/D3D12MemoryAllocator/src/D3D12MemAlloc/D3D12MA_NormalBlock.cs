// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MA_POOL_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Represents a single block of device memory (heap) with all the data about its regions (aka suballocations, Allocation), assigned and free. Thread-safety: This class must be externally synchronized.</summary>
[NativeTypeName("class D3D12MA::NormalBlock : D3D12MA::MemoryBlock")]
[NativeInheritance("D3D12MA::MemoryBlock")]
internal unsafe partial struct D3D12MA_NormalBlock : IDisposable
{
    public D3D12MA_MemoryBlock Base;

    public D3D12MA_BlockMetadata* m_pMetadata;

    private D3D12MA_BlockVector* m_BlockVector;

    public static D3D12MA_NormalBlock* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, D3D12MA_AllocatorPimpl* allocator, D3D12MA_BlockVector* blockVector, [NativeTypeName("const D3D12_HEAP_PROPERTIES &")] in D3D12_HEAP_PROPERTIES heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT")] uint id)
    {
        D3D12MA_NormalBlock* result = D3D12MA_NEW<D3D12MA_NormalBlock>(allocs);
        result->_ctor(allocator, blockVector, heapProps, heapFlags, size, id);
        return result;
    }

    private void _ctor(D3D12MA_AllocatorPimpl* allocator, D3D12MA_BlockVector* blockVector, [NativeTypeName("const D3D12_HEAP_PROPERTIES &")] in D3D12_HEAP_PROPERTIES heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT")] uint id)
    {
        Base = new D3D12MA_MemoryBlock(allocator, heapProps, heapFlags, size, id);

        m_pMetadata = null;
        m_BlockVector = blockVector;
    }

    public void Dispose()
    {
        if (m_pMetadata != null)
        {
            // THIS IS THE MOST IMPORTANT ASSERT IN THE ENTIRE LIBRARY!
            // Hitting it means you have some memory leak - unreleased Allocation objects.
            D3D12MA_ASSERT(m_pMetadata->IsEmpty(), "Some allocations were not freed before destruction of this memory block!");

            D3D12MA_DELETE(Base.m_Allocator->GetAllocs(), m_pMetadata);
        }

        Base.Dispose();
    }

    [UnscopedRef]
    [return: NativeTypeName("const D3D12_HEAP_PROPERTIES &")]
    public readonly ref readonly D3D12_HEAP_PROPERTIES GetHeapProperties()
    {
        return ref Base.GetHeapProperties();
    }

    public readonly D3D12_HEAP_FLAGS GetHeapFlags()
    {
        return Base.GetHeapFlags();
    }

    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSize()
    {
        return Base.GetSize();
    }

    [return: NativeTypeName("UINT")]
    public readonly uint GetId()
    {
        return Base.GetId();
    }

    public readonly ID3D12Heap* GetHeap()
    {
        return Base.GetHeap();
    }

    public readonly D3D12MA_BlockVector* GetBlockVector()
    {
        return m_BlockVector;
    }

    // 'algorithm' should be one of the *_ALGORITHM_* flags in enums POOL_FLAGS or VIRTUAL_BLOCK_FLAGS
    public HRESULT Init([NativeTypeName("UINT32")] uint algorithm, ID3D12ProtectedResourceSession* pProtectedSession, bool denyMsaaTextures)
    {
        HRESULT hr = Base.Init(pProtectedSession, denyMsaaTextures);

        if (FAILED(hr))
        {
            return hr;
        }

        switch (algorithm)
        {
            case (uint)(D3D12MA_POOL_FLAG_ALGORITHM_LINEAR):
            {
                D3D12MA_BlockMetadata_Linear* pMetadata = D3D12MA_BlockMetadata_Linear.Create(Base.m_Allocator->GetAllocs(), (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(ref Unsafe.AsRef(in Base.m_Allocator->GetAllocs()))), false);
                m_pMetadata = (D3D12MA_BlockMetadata*)(pMetadata);
                break;
            }

            default:
            {
                D3D12MA_FAIL();
                break;
            }

            case 0:
            {
                D3D12MA_BlockMetadata_TLSF* pMetadata = D3D12MA_BlockMetadata_TLSF.Create(Base.m_Allocator->GetAllocs(), (D3D12MA_ALLOCATION_CALLBACKS*)(Unsafe.AsPointer(ref Unsafe.AsRef(in Base.m_Allocator->GetAllocs()))), false);
                m_pMetadata = (D3D12MA_BlockMetadata*)(pMetadata);
                break;
            }
        }
        m_pMetadata->Init(Base.m_Size);

        return hr;
    }

    // Validates all data structures inside this object. If not valid, returns false.
    public readonly bool Validate()
    {
        D3D12MA_VALIDATE((Base.GetHeap() != null) && (m_pMetadata != null) && (m_pMetadata->GetSize() != 0) && (m_pMetadata->GetSize() == Base.GetSize()));
        return m_pMetadata->Validate();
    }
}
