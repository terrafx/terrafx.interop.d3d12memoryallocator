// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class PoolPimpl

    internal unsafe partial struct PoolPimpl : IDisposable
    {
        public AllocatorPimpl* m_Allocator; // Externally owned object
        public POOL_DESC m_Desc;
        public BlockVector* m_BlockVector; // Owned object
        [NativeTypeName("wchar_t*")] public char* m_Name;

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
            *m_BlockVector = new(
                allocator, desc->HeapType, heapFlags,
                preferredBlockSize,
                desc->MinBlockCount, maxBlockCount,
                explicitBlockSize);
        }

        [return: NativeTypeName("HRESULT")]
        public partial int Init();
        public partial void Dispose();

        public readonly AllocatorPimpl* GetAllocator() { return m_Allocator; }
        public readonly POOL_DESC* GetDesc() { return (POOL_DESC*)Unsafe.AsPointer(ref Unsafe.AsRef(m_Desc)); }
        public BlockVector* GetBlockVector() { return m_BlockVector; }

        [return: NativeTypeName("HRESULT")]
        public int SetMinBytes([NativeTypeName("UINT64")] ulong minBytes) { return m_BlockVector->SetMinBytes(minBytes); }

        public partial void CalculateStats(StatInfo* outStats);

        public partial void SetName([NativeTypeName("LPCWSTR")] char* Name);
        [return: NativeTypeName("LPCWSTR")]
        public char* GetName() { return m_Name; }

        public partial void FreeName();
    }

    internal unsafe partial struct PoolPimpl : IDisposable
    {
        public partial int Init()
        {
            return m_BlockVector->CreateMinBlocks();
        }

        public partial void Dispose()
        {
            FreeName();
            D3D12MA_DELETE(m_Allocator->GetAllocs(), m_BlockVector);
        }

        public partial void CalculateStats(StatInfo* outStats)
        {
            ZeroMemory(&outStats, (nuint)sizeof(StatInfo));
            outStats->AllocationSizeMin = ulong.MaxValue;
            outStats->UnusedRangeSizeMin = ulong.MaxValue;

            m_BlockVector->AddStats(outStats);

            PostProcessStatInfo(ref *outStats);
        }

        public partial void SetName(char* Name)
        {
            FreeName();

            if (Name != null)
            {
                nuint nameCharCount = wcslen(Name) + 1;
                m_Name = D3D12MA_NEW_ARRAY<char>(m_Allocator->GetAllocs(), nameCharCount);
                memcpy(m_Name, Name, nameCharCount * sizeof(char));
            }
        }

        public partial void FreeName()
        {
            if (m_Name != null)
            {
                nuint nameCharCount = wcslen(m_Name) + 1;
                D3D12MA_DELETE_ARRAY_NO_DISPOSE(m_Allocator->GetAllocs(), m_Name, nameCharCount);
                m_Name = null;
            }
        }
    }
}
