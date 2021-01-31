// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    internal unsafe struct PoolPimpl : IDisposable
    {
        public AllocatorPimpl* m_Allocator; // Externally owned object
        public POOL_DESC m_Desc;
        public BlockVector* m_BlockVector; // Owned object

        [NativeTypeName("wchar_t*")]
        public ushort* m_Name;

        public PoolPimpl(AllocatorPimpl* allocator, POOL_DESC* desc)
        {
            m_Allocator = allocator;
            m_Desc = *desc;
            m_BlockVector = null;
            m_Name = null;

            bool explicitBlockSize = desc->BlockSize != 0;
            ulong preferredBlockSize = explicitBlockSize ? desc->BlockSize : D3D12MA_DEFAULT_BLOCK_SIZE;

            D3D12_HEAP_FLAGS heapFlags = desc->HeapFlags;

            uint maxBlockCount = desc->MaxBlockCount != 0 ? desc->MaxBlockCount : uint.MaxValue;

            m_BlockVector = D3D12MA_NEW<BlockVector>(allocator->GetAllocs());
            *m_BlockVector = new BlockVector(
                allocator, desc->HeapType, heapFlags,
                preferredBlockSize,
                desc->MinBlockCount, maxBlockCount,
                explicitBlockSize);
        }

        [return: NativeTypeName("HRESULT")]
        public int Init()
        {
            return m_BlockVector->CreateMinBlocks();
        }

        public void Dispose()
        {
            FreeName();
            D3D12MA_DELETE(m_Allocator->GetAllocs(), m_BlockVector);
        }

        public readonly AllocatorPimpl* GetAllocator() => m_Allocator;

        public readonly POOL_DESC* GetDesc() => (POOL_DESC*)Unsafe.AsPointer(ref Unsafe.AsRef(m_Desc));

        public BlockVector* GetBlockVector() => m_BlockVector;

        [return: NativeTypeName("HRESULT")]
        public int SetMinBytes([NativeTypeName("UINT64")] ulong minBytes) => m_BlockVector->SetMinBytes(minBytes);

        public void CalculateStats(StatInfo* outStats)
        {
            ZeroMemory(outStats, (nuint)sizeof(StatInfo));
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
        public ushort* GetName() => m_Name;

        public void FreeName()
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
