// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>Represents a single block of device memory (heap) with all the data about its regions(aka suballocations, Allocation), assigned and free. Thread-safety: This class must be externally synchronized.</summary>
    internal unsafe struct NormalBlock : /* MemoryBlock, */ IDisposable
    {
        private static readonly void** SharedLpVtbl = InitLpVtbl();

        private static void** InitLpVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NormalBlock), sizeof(void*));
            lpVtbl[0] = (delegate*<NormalBlock*, void>)&Dispose;
            return lpVtbl;
        }

        public MemoryBlock Base;
        public BlockMetadata* m_pMetadata;

        private BlockVector* m_BlockVector;

        public NormalBlock(AllocatorPimpl* allocator, BlockVector* blockVector, D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT")] uint id)
        {
            Base = new MemoryBlock(allocator, heapType, heapFlags, size, id);
            Base.lpVtbl = SharedLpVtbl;
            m_pMetadata = null;
            m_BlockVector = blockVector;
        }

        public void Dispose()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<NormalBlock*, void>)&Dispose == Base.lpVtbl[0]));
            Dispose((NormalBlock*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("HRESULT")]
        public int Init()
        {
            HRESULT hr = Base.Init();
            if (FAILED(hr))
            {
                return hr;
            }

            m_pMetadata = (BlockMetadata*)D3D12MA_NEW<BlockMetadata_Generic>(Base.m_Allocator->GetAllocs());
            *(BlockMetadata_Generic*)m_pMetadata = new BlockMetadata_Generic(Base.m_Allocator->GetAllocs(), false);
            m_pMetadata->Init(Base.m_Size);

            return hr;
        }

        public readonly D3D12_HEAP_TYPE GetHeapType() => Base.m_HeapType;

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => Base.m_Size;

        [return: NativeTypeName("UINT")]
        public readonly uint GetId() => Base.m_Id;

        public readonly ID3D12Heap* GetHeap() => Base.m_Heap;

        public readonly BlockVector* GetBlockVector() => m_BlockVector;

        /// <summary>Validates all data structures inside this object. If not valid, returns false.</summary>
        public readonly bool Validate()
        {
            D3D12MA_VALIDATE(GetHeap() != null &&
                m_pMetadata != null &&
                m_pMetadata->GetSize() != 0 &&
                m_pMetadata->GetSize() == GetSize());
            return m_pMetadata->Validate();
        }

        public static void Dispose(NormalBlock* pThis)
        {
            if (pThis->m_pMetadata != null)
            {
                // THIS IS THE MOST IMPORTANT ASSERT IN THE ENTIRE LIBRARY!
                // Hitting it means you have some memory leak - unreleased Allocation objects.
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pThis->m_pMetadata->IsEmpty())); // "Some allocations were not freed before destruction of this memory block!"

                D3D12MA_DELETE(pThis->Base.m_Allocator->GetAllocs(), pThis->m_pMetadata);
            }

            MemoryBlock.Dispose((MemoryBlock*)pThis); // base.~T();
        }
    }
}
