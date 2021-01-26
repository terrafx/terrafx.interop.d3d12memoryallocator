// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class NormalBlock definition

    /// <summary>
    /// Represents a single block of device memory (heap) with all the data about its
    /// regions(aka suballocations, Allocation), assigned and free.
    /// Thread-safety: This class must be externally synchronized.
    /// </summary>
    internal unsafe partial struct NormalBlock : /* MemoryBlock, */ IDisposable
    {
        private static void** SharedLpVtbl = InitLpVtbl();

        private static void** InitLpVtbl()
        {
            SharedLpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NormalBlock), sizeof(void*));
            SharedLpVtbl[0] = (delegate*<NormalBlock*, void>)&Dispose;
            return SharedLpVtbl;
        }

        public MemoryBlock @base;
        public BlockMetadata* m_pMetadata;

        private BlockVector* m_BlockVector;

        public NormalBlock(
            AllocatorPimpl* allocator,
            BlockVector* blockVector,
            D3D12_HEAP_TYPE heapType,
            D3D12_HEAP_FLAGS heapFlags,
            [NativeTypeName("UINT64")] ulong size,
            [NativeTypeName("UINT")] uint id)
        {
            @base = new(allocator, heapType, heapFlags, size, id);
            @base.lpVtbl = SharedLpVtbl;
            m_pMetadata = null;
            m_BlockVector = null;
        }

        public partial void Dispose();
        [return: NativeTypeName("HRESULT")]
        public partial int Init();

        public readonly BlockVector* GetBlockVector() { return m_BlockVector; }

        // Validates all data structures inside this object. If not valid, returns false.
        public readonly partial bool Validate();
    }

    internal unsafe partial struct NormalBlock
    {
        public partial void Dispose()
        {
            Dispose((NormalBlock*)Unsafe.AsPointer(ref this));
        }

        public partial int Init()
        {
            HRESULT hr = @base.Init();
            if (FAILED(hr))
            {
                return hr;
            }

            m_pMetadata = (BlockMetadata*)D3D12MA_NEW<BlockMetadata_Generic>(@base.m_Allocator->GetAllocs());
            *(BlockMetadata_Generic*)m_pMetadata = new(@base.m_Allocator->GetAllocs(), false);
            m_pMetadata->Init(@base.m_Size);

            return hr;
        }

        public readonly partial bool Validate()
        {
            D3D12MA_VALIDATE(@base.GetHeap() != null &&
                m_pMetadata != null &&
                m_pMetadata->GetSize() != 0 &&
                m_pMetadata->GetSize() == @base.GetSize());
            return m_pMetadata->Validate();
        }

        public static void Dispose(NormalBlock* @this)
        {
            if (@this->m_pMetadata != null)
            {
                // THIS IS THE MOST IMPORTANT ASSERT IN THE ENTIRE LIBRARY!
                // Hitting it means you have some memory leak - unreleased Allocation objects.
                D3D12MA_ASSERT(@this->m_pMetadata->IsEmpty());

                D3D12MA_DELETE(@this->@base.m_Allocator->GetAllocs(), @this->m_pMetadata);
            }

            MemoryBlock.Dispose((MemoryBlock*)@this); // base.~T();
        }
    }
}
