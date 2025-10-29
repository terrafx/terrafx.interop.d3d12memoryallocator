// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Represents a single block of device memory (heap). Base class for inheritance. Thread-safety: This class must be externally synchronized.</summary>
internal unsafe partial struct D3D12MA_MemoryBlock : IDisposable
{
    internal readonly D3D12MA_AllocatorPimpl* m_Allocator;

    internal readonly D3D12_HEAP_PROPERTIES m_HeapProps;

    internal readonly D3D12_HEAP_FLAGS m_HeapFlags;

    [NativeTypeName("UINT64")]
    internal readonly ulong m_Size;

    [NativeTypeName("UINT")]
    internal readonly uint m_Id;

    internal Pointer<ID3D12Heap> m_Heap;

    // Creates the ID3D12Heap.
    public D3D12MA_MemoryBlock(D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("const D3D12_HEAP_PROPERTIES &")] in D3D12_HEAP_PROPERTIES heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT")] uint id)
    {
        m_Allocator = allocator;
        m_HeapProps = heapProps;
        m_HeapFlags = heapFlags;
        m_Size = size;
        m_Id = id;
    }

    public void Dispose()
    {
        if (m_Heap.Value != null)
        {
            _ = m_Heap.Value->Release();
            m_Allocator->m_Budget.RemoveBlock(m_Allocator->HeapPropertiesToMemorySegmentGroup(m_HeapProps), m_Size);
        }
    }

    [UnscopedRef]
    [return: NativeTypeName("const D3D12_HEAP_PROPERTIES &")]
    public readonly ref readonly D3D12_HEAP_PROPERTIES GetHeapProperties()
    {
        return ref m_HeapProps;
    }

    public readonly D3D12_HEAP_FLAGS GetHeapFlags()
    {
        return m_HeapFlags;
    }

    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSize()
    {
        return m_Size;
    }

    [return: NativeTypeName("UINT")]
    public readonly uint GetId()
    {
        return m_Id;
    }

    public readonly ID3D12Heap* GetHeap()
    {
        return m_Heap.Value;
    }

    internal HRESULT Init(ID3D12ProtectedResourceSession* pProtectedSession, bool denyMsaaTextures)
    {
        D3D12MA_ASSERT((m_Heap.Value == null) && (m_Size > 0));

        D3D12_HEAP_DESC heapDesc = new D3D12_HEAP_DESC {
            SizeInBytes = m_Size,
            Properties = m_HeapProps,
            Alignment = D3D12MA_HeapFlagsToAlignment(m_HeapFlags, denyMsaaTextures),
            Flags = m_HeapFlags,
        };

        HRESULT hr;
        ID3D12Device4* device4 = m_Allocator->GetDevice4();

        if (device4 != null)
        {
            Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));
            hr = m_Allocator->GetDevice4()->CreateHeap1(&heapDesc, pProtectedSession, __uuidof<ID3D12Heap>(), (void**)(Unsafe.AsPointer(ref m_Heap)));
        }
        else
        {
            if (pProtectedSession == null)
            {
                hr = m_Allocator->GetDevice()->CreateHeap(&heapDesc, __uuidof<ID3D12Heap>(), (void**)(Unsafe.AsPointer(ref m_Heap)));
            }
            else
            {
                hr = E_NOINTERFACE;
            }
        }

        if (SUCCEEDED(hr))
        {
            m_Allocator->m_Budget.AddBlock(m_Allocator->HeapPropertiesToMemorySegmentGroup(m_HeapProps), m_Size);
        }
        return hr;
    }
}
