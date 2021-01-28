// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents a single block of device memory (heap).
    /// Base class for inheritance.
    /// Thread-safety: This class must be externally synchronized.
    /// </summary>
    internal unsafe struct MemoryBlock : IDisposable
    {
        private static void** SharedLpVtbl = InitLpVtbl();

        private static void** InitLpVtbl()
        {
            SharedLpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(MemoryBlock), sizeof(void*));
            SharedLpVtbl[0] = (delegate*<MemoryBlock*, void>)&Dispose;
            return SharedLpVtbl;
        }

        public void** lpVtbl;

        public readonly AllocatorPimpl* m_Allocator;
        public readonly D3D12_HEAP_TYPE m_HeapType;
        public readonly D3D12_HEAP_FLAGS m_HeapFlags;

        [NativeTypeName("UINT64")]
        public readonly ulong m_Size;

        public readonly uint m_Id;

        private ID3D12Heap* m_Heap;

        public MemoryBlock(
            AllocatorPimpl* allocator,
            D3D12_HEAP_TYPE heapType,
            D3D12_HEAP_FLAGS heapFlags,
            [NativeTypeName("UINT64")] ulong size,
            [NativeTypeName("UINT")] uint id)
        {
            lpVtbl = SharedLpVtbl;

            m_Allocator = allocator;
            m_HeapType = heapType;
            m_HeapFlags = heapFlags;
            m_Size = size;
            m_Id = id;
            m_Heap = null;
        }

        public void Dispose()
        {
            Dispose((MemoryBlock*)Unsafe.AsPointer(ref this));
        }
        // Creates the ID3D12Heap.

        public D3D12_HEAP_TYPE GetHeapType() { return m_HeapType; }

        public D3D12_HEAP_FLAGS GetHeapFlags() { return m_HeapFlags; }

        [return: NativeTypeName("UINT64")]
        public ulong GetSize() { return m_Size; }

        [return: NativeTypeName("UINT")]
        public uint GetId() { return m_Id; }

        public ID3D12Heap* GetHeap() { return m_Heap; }

        public HRESULT Init()
        {
            D3D12MA_ASSERT(m_Heap == null && m_Size > 0);

            D3D12_HEAP_DESC heapDesc = default;
            heapDesc.SizeInBytes = m_Size;
            heapDesc.Properties.Type = m_HeapType;
            heapDesc.Alignment = HeapFlagsToAlignment(m_HeapFlags);
            heapDesc.Flags = m_HeapFlags;

            HRESULT hr;
            fixed (ID3D12Heap** ppvHeap = &m_Heap)
            {
                hr = m_Allocator->GetDevice()->CreateHeap(&heapDesc, __uuidof<ID3D12Heap>(), (void**)ppvHeap);
            }


            if (SUCCEEDED(hr))
            {
                m_Allocator->m_Budget.m_BlockBytes[(int)HeapTypeToIndex(m_HeapType)].Add(m_Size);
            }
            return hr;
        }

        public static void Dispose(MemoryBlock* @this)
        {
            if (@this->m_Heap != null)
            {
                @this->m_Allocator->m_Budget.m_BlockBytes[(int)HeapTypeToIndex(@this->m_HeapType)].Subtract(@this->m_Size);
                @this->m_Heap->Release();
            }
        }
    }
}
