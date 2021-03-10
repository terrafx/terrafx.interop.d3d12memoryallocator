// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_PoolPimpl : IDisposable
    {
        public D3D12MA_AllocatorPimpl* m_Allocator; // Externally owned object

        public D3D12MA_POOL_DESC m_Desc;

        public D3D12MA_BlockVector* m_BlockVector; // Owned object

        [NativeTypeName("wchar_t*")]
        public ushort* m_Name;

        public D3D12MA_PoolPimpl* m_PrevPool;

        public D3D12MA_PoolPimpl* m_NextPool;

        internal static void _ctor(ref D3D12MA_PoolPimpl pThis, D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("const D3D12MA_POOL_DESC&")] D3D12MA_POOL_DESC* desc)
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
        public int Init()
        {
            return m_BlockVector->CreateMinBlocks();
        }

        public void Dispose()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_PrevPool == null) && (m_NextPool == null));
            FreeName();
            D3D12MA_DELETE(m_Allocator->GetAllocs(), m_BlockVector);
        }

        public readonly D3D12MA_AllocatorPimpl* GetAllocator() => m_Allocator;

        public readonly D3D12MA_POOL_DESC* GetDesc() => (D3D12MA_POOL_DESC*)Unsafe.AsPointer(ref Unsafe.AsRef(m_Desc));

        public D3D12MA_BlockVector* GetBlockVector() => m_BlockVector;

        [return: NativeTypeName("HRESULT")]
        public int SetMinBytes([NativeTypeName("UINT64")] ulong minBytes) => m_BlockVector->SetMinBytes(minBytes);

        public void CalculateStats([NativeTypeName("StatInfo&")] D3D12MA_StatInfo* outStats)
        {
            ZeroMemory(outStats, (nuint)sizeof(D3D12MA_StatInfo));

            outStats->AllocationSizeMin = ulong.MaxValue;
            outStats->UnusedRangeSizeMin = ulong.MaxValue;

            m_BlockVector->AddStats(outStats);
            PostProcessStatInfo(ref *outStats);
        }

        public void SetName([NativeTypeName("LPCWSTR")] ushort* Name)
        {
            FreeName();

            if (Name != null)
            {
                nuint nameCharCount = wcslen(Name) + 1;
                m_Name = D3D12MA_NEW_ARRAY<ushort>(m_Allocator->GetAllocs(), nameCharCount);
                memcpy(m_Name, Name, nameCharCount * sizeof(ushort));
            }
        }

        [return: NativeTypeName("LPCWSTR")]
        public readonly ushort* GetName() => m_Name;

        private void FreeName()
        {
            if (m_Name != null)
            {
                nuint nameCharCount = wcslen(m_Name) + 1;
                D3D12MA_DELETE_ARRAY(m_Allocator->GetAllocs(), m_Name, nameCharCount);
                m_Name = null;
            }
        }
    }
}
