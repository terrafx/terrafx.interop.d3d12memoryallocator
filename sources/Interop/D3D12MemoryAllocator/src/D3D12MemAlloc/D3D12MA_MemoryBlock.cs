// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemAlloc;
using System.Runtime.CompilerServices;
using System.Threading;

namespace TerraFX.Interop
{
    /// <summary>Represents a single block of device memory (heap). Base class for inheritance. Thread-safety: This class must be externally synchronized.</summary>
    internal unsafe struct D3D12MA_MemoryBlock : IDisposable
    {
        private static readonly void** Vtbl = InitVtbl();

        private static void** InitVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_MemoryBlock), sizeof(void*));
            lpVtbl[0] = (delegate*<ref D3D12MA_MemoryBlock, void>)&Dispose;
            return lpVtbl;
        }

        internal void** lpVtbl;

        internal readonly D3D12MA_Allocator* m_Allocator;

        internal readonly D3D12_HEAP_PROPERTIES m_HeapProps;

        internal readonly D3D12_HEAP_FLAGS m_HeapFlags;

        [NativeTypeName("UINT64")]
        internal readonly ulong m_Size;

        internal readonly uint m_Id;

        internal ID3D12Heap* m_Heap;

        public D3D12MA_MemoryBlock(D3D12MA_Allocator* allocator, [NativeTypeName("const D3D12_HEAP_PROPERTIES&")] D3D12_HEAP_PROPERTIES* heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT")] uint id)
        {
            lpVtbl = Vtbl;

            m_Allocator = allocator;
            m_HeapProps = *heapProps;
            m_HeapFlags = heapFlags;
            m_Size = size;
            m_Id = id;
            m_Heap = null;
        }

        public void Dispose()
        {
            ((delegate*<ref D3D12MA_MemoryBlock, void>)lpVtbl[0])(ref this);
        }

        [return: NativeTypeName("const D3D12_HEAP_PROPERTIES&")]
        public readonly D3D12_HEAP_PROPERTIES* GetHeapProperties() => (D3D12_HEAP_PROPERTIES*)Unsafe.AsPointer(ref Unsafe.AsRef(in m_HeapProps));

        public readonly D3D12_HEAP_FLAGS GetHeapFlags() => m_HeapFlags;

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => m_Size;

        [return: NativeTypeName("UINT")]
        public readonly uint GetId() => m_Id;

        public readonly ID3D12Heap* GetHeap() => m_Heap;

        public HRESULT Init()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_Heap == null) && (m_Size > 0));

            D3D12_HEAP_DESC heapDesc = default;
            heapDesc.SizeInBytes = m_Size;
            heapDesc.Properties = m_HeapProps;
            heapDesc.Alignment = HeapFlagsToAlignment(m_HeapFlags);
            heapDesc.Flags = m_HeapFlags;

            HRESULT hr;

            fixed (ID3D12Heap** ppvHeap = &m_Heap)
            {
                hr = m_Allocator->GetDevice()->CreateHeap(&heapDesc, __uuidof<ID3D12Heap>(), (void**)ppvHeap);
            }

            if (SUCCEEDED(hr))
            {
                ref ulong blockBytes = ref m_Allocator->m_Budget.m_BlockBytes[(int)HeapTypeToIndex(m_HeapProps.Type)];
                Volatile.Write(ref blockBytes, Volatile.Read(ref blockBytes) + m_Size);
            }

            return hr;
        }

        public static void Dispose(ref D3D12MA_MemoryBlock pThis)
        {
            if (pThis.m_Heap != null)
            {
                ref ulong blockBytes = ref pThis.m_Allocator->m_Budget.m_BlockBytes[(int)HeapTypeToIndex(pThis.m_HeapProps.Type)];
                Volatile.Write(ref blockBytes, Volatile.Read(ref blockBytes) - pThis.m_Size);
                _ = pThis.m_Heap->Release();
            }
        }
    }
}
