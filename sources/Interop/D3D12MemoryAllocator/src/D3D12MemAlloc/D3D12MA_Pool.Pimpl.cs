// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    public unsafe partial struct D3D12MA_Pool : IDisposable, D3D12MA_IItemTypeTraits<D3D12MA_Pool>
    {
        internal D3D12MA_Allocator* m_Allocator; // Externally owned object

        internal D3D12MA_POOL_DESC m_Desc;

        internal D3D12MA_BlockVector* m_BlockVector; // Owned object

        [NativeTypeName("wchar_t*")]
        internal ushort* m_Name;

        internal D3D12MA_Pool* m_PrevPool;

        internal D3D12MA_Pool* m_NextPool;

        internal static void _ctor(ref D3D12MA_Pool pThis, D3D12MA_Allocator* allocator, [NativeTypeName("const D3D12MA_POOL_DESC&")] D3D12MA_POOL_DESC* desc)
        {
            pThis.m_Allocator = allocator;
            pThis.m_Desc = *desc;
            pThis.m_BlockVector = null;
            pThis.m_Name = null;
            pThis.m_PrevPool = null;
            pThis.m_NextPool = null;

            bool explicitBlockSize = desc->BlockSize != 0;
            ulong preferredBlockSize = explicitBlockSize ? desc->BlockSize : D3D12MA_DEFAULT_BLOCK_SIZE;

            D3D12_HEAP_FLAGS heapFlags = desc->HeapFlags;
            uint maxBlockCount = desc->MaxBlockCount != 0 ? desc->MaxBlockCount : uint.MaxValue;

            pThis.m_BlockVector = D3D12MA_NEW<D3D12MA_BlockVector>(allocator->GetAllocs());
            D3D12MA_BlockVector._ctor(
                ref *pThis.m_BlockVector,
                allocator, &desc->HeapProperties, heapFlags,
                preferredBlockSize,
                desc->MinBlockCount, maxBlockCount,
                explicitBlockSize
            );
        }

        [return: NativeTypeName("HRESULT")]
        internal int Init()
        {
            return m_BlockVector->CreateMinBlocks();
        }

        void IDisposable.Dispose()
        {
            GetAllocator()->UnregisterPool(ref this, m_Desc.HeapProperties.Type);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_PrevPool == null) && (m_NextPool == null));
            FreeName();
            D3D12MA_DELETE(m_Allocator->GetAllocs(), m_BlockVector);
        }

        internal readonly D3D12MA_Allocator* GetAllocator() => m_Allocator;

        internal D3D12MA_BlockVector* GetBlockVector() => m_BlockVector;

        [return: NativeTypeName("HRESULT")]
        private int SetMinBytesPimpl([NativeTypeName("UINT64")] ulong minBytes)
        {
            return m_BlockVector->SetMinBytes(minBytes);
        }

        private void CalculateStatsPimpl([NativeTypeName("StatInfo&")] D3D12MA_StatInfo* outStats)
        {
            ZeroMemory(outStats, (nuint)sizeof(D3D12MA_StatInfo));

            outStats->AllocationSizeMin = ulong.MaxValue;
            outStats->UnusedRangeSizeMin = ulong.MaxValue;

            m_BlockVector->AddStats(outStats);
            PostProcessStatInfo(ref *outStats);
        }

        private void SetNamePimpl([NativeTypeName("LPCWSTR")] ushort* Name)
        {
            FreeName();

            if (Name != null)
            {
                nuint nameCharCount = wcslen(Name) + 1;
                m_Name = D3D12MA_NEW_ARRAY<ushort>(m_Allocator->GetAllocs(), nameCharCount);
                memcpy(m_Name, Name, nameCharCount * sizeof(ushort));
            }
        }

        private void FreeName()
        {
            if (m_Name != null)
            {
                nuint nameCharCount = wcslen(m_Name) + 1;
                D3D12MA_DELETE_ARRAY(m_Allocator->GetAllocs(), m_Name, nameCharCount);
                m_Name = null;
            }
        }

        readonly D3D12MA_Pool* D3D12MA_IItemTypeTraits<D3D12MA_Pool>.GetPrev() => m_PrevPool;

        readonly D3D12MA_Pool* D3D12MA_IItemTypeTraits<D3D12MA_Pool>.GetNext() => m_NextPool;

        readonly D3D12MA_Pool** D3D12MA_IItemTypeTraits<D3D12MA_Pool>.AccessPrev()
        {
            return &((D3D12MA_Pool*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)))->m_PrevPool;
        }

        readonly D3D12MA_Pool** D3D12MA_IItemTypeTraits<D3D12MA_Pool>.AccessNext()
        {
            return &((D3D12MA_Pool*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)))->m_NextPool;
        }
    }
}
