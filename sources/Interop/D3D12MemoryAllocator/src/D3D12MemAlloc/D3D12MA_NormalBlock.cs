// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemAlloc;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>Represents a single block of device memory (heap) with all the data about its regions(aka suballocations, Allocation), assigned and free. Thread-safety: This class must be externally synchronized.</summary>
    internal unsafe struct D3D12MA_NormalBlock : IDisposable
    {
        private static readonly void** Vtbl = InitVtbl();

        private static void** InitVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_NormalBlock), sizeof(void*));
            lpVtbl[0] = (delegate*<ref D3D12MA_NormalBlock, void>)&Dispose;
            return lpVtbl;
        }

        public D3D12MA_MemoryBlock Base;

        public D3D12MA_BlockMetadata_Generic* m_pMetadata;

        private D3D12MA_BlockVector* m_BlockVector;

        public static void _ctor(ref D3D12MA_NormalBlock pThis, D3D12MA_Allocator* allocator, ref D3D12MA_BlockVector blockVector, [NativeTypeName("const D3D12_HEAP_PROPERTIES&")] D3D12_HEAP_PROPERTIES* heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT")] uint id)
        {
            pThis.Base = new D3D12MA_MemoryBlock(allocator, heapProps, heapFlags, size, id);
            pThis.Base.lpVtbl = Vtbl;

            pThis.m_pMetadata = null;
            pThis.m_BlockVector = (D3D12MA_BlockVector*)Unsafe.AsPointer(ref blockVector);
        }

        public void Dispose()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[0] == (delegate*<ref D3D12MA_NormalBlock, void>)&Dispose));

            Dispose(ref this);
        }

        [return: NativeTypeName("HRESULT")]
        public int Init()
        {
            HRESULT hr = Base.Init();

            if (FAILED(hr))
            {
                return hr;
            }

            var allocationCallbacks = Base.m_Allocator->GetAllocs();

            var metadata = D3D12MA_NEW<D3D12MA_BlockMetadata_Generic>(allocationCallbacks);
            D3D12MA_BlockMetadata_Generic._ctor(ref *metadata, allocationCallbacks, false);

            m_pMetadata = metadata;
            m_pMetadata->Init(Base.m_Size);

            return hr;
        }

        [return: NativeTypeName("const D3D12_HEAP_PROPERTIES&")]
        public readonly D3D12_HEAP_PROPERTIES* GetHeapProperties() => Base.GetHeapProperties();

        public readonly D3D12_HEAP_FLAGS GetHeapFlags() => Base.GetHeapFlags();

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => Base.GetSize();

        [return: NativeTypeName("UINT")]
        public readonly uint GetId() => Base.GetId();

        public readonly ID3D12Heap* GetHeap() => Base.GetHeap();

        public readonly D3D12MA_BlockVector* GetBlockVector() => m_BlockVector;

        /// <summary>Validates all data structures inside this object. If not valid, returns false.</summary>
        public readonly bool Validate()
        {
            _ = D3D12MA_VALIDATE(
                (GetHeap() != null) &&
                (m_pMetadata != null) &&
                (m_pMetadata->GetSize() != 0) &&
                (m_pMetadata->GetSize() == GetSize())
            );
            return m_pMetadata->Validate();
        }

        public static void Dispose(ref D3D12MA_NormalBlock pThis)
        {
            if (pThis.m_pMetadata != null)
            {
                // THIS IS THE MOST IMPORTANT ASSERT IN THE ENTIRE LIBRARY!
                // Hitting it means you have some memory leak - unreleased Allocation objects.
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && pThis.m_pMetadata->IsEmpty()); // "Some allocations were not freed before destruction of this memory block!"

                D3D12MA_DELETE(pThis.Base.m_Allocator->GetAllocs(), pThis.m_pMetadata);
            }

            D3D12MA_MemoryBlock.Dispose(ref pThis.Base);
        }
    }
}
