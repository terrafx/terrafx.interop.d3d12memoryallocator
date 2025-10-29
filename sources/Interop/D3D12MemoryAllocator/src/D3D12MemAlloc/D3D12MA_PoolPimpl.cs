// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MA_POOL_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_PoolPimpl : IDisposable
{
    private D3D12MA_AllocatorPimpl* m_Allocator; // Externally owned object.

    private D3D12MA_POOL_DESC m_Desc;

    private D3D12MA_BlockVector* m_BlockVector; // Owned object.

    private D3D12MA_CommittedAllocationList m_CommittedAllocations;

    [NativeTypeName("wchar_t *")]
    private char* m_Name;

    internal D3D12MA_PoolPimpl* m_PrevPool;

    internal D3D12MA_PoolPimpl* m_NextPool;

    public static D3D12MA_PoolPimpl* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("const D3D12MA::POOL_DESC &")] in D3D12MA_POOL_DESC desc)
    {
        D3D12MA_PoolPimpl* result = D3D12MA_NEW<D3D12MA_PoolPimpl>(allocs);
        result->_ctor(allocator, desc);
        return result;
    }

    private void _ctor(D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("const D3D12MA::POOL_DESC &")] in D3D12MA_POOL_DESC desc)
    {
        m_Allocator = allocator;
        m_Desc = desc;

        bool explicitBlockSize = desc.BlockSize != 0;
        ulong preferredBlockSize = explicitBlockSize ? desc.BlockSize : D3D12MA_DEFAULT_BLOCK_SIZE;
        uint maxBlockCount = desc.MaxBlockCount != 0 ? desc.MaxBlockCount : uint.MaxValue;

        m_BlockVector = D3D12MA_BlockVector.Create(allocator->GetAllocs(), allocator, desc.HeapProperties, desc.HeapFlags, preferredBlockSize, desc.MinBlockCount, maxBlockCount, explicitBlockSize, D3D12MA_MAX(desc.MinAllocationAlignment, (ulong)(D3D12MA_DEBUG_ALIGNMENT)), ((desc.Flags & D3D12MA_POOL_FLAG_ALGORITHM_MASK) != 0) ? 1u : 0u, (desc.Flags & D3D12MA_POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED) != 0, desc.pProtectedSession, desc.ResidencyPriority);

        m_CommittedAllocations = new D3D12MA_CommittedAllocationList();
        m_Name = null;
        m_PrevPool = null;
        m_NextPool = null;
    }

    public void Dispose()
    {
        D3D12MA_ASSERT((m_PrevPool == null) && (m_NextPool == null));
        FreeName();

        D3D12MA_DELETE(m_Allocator->GetAllocs(), m_BlockVector);
        m_CommittedAllocations.Dispose();
    }

    public readonly D3D12MA_AllocatorPimpl* GetAllocator()
    {
        return m_Allocator;
    }

    [UnscopedRef]
    [return: NativeTypeName("const D3D12MA::POOL_DESC &")]
    public readonly ref readonly D3D12MA_POOL_DESC GetDesc()
    {
        return ref m_Desc;
    }

    public readonly bool AlwaysCommitted()
    {
        return (m_Desc.Flags & D3D12MA_POOL_FLAG_ALWAYS_COMMITTED) != 0;
    }

    public readonly bool SupportsCommittedAllocations()
    {
        return m_Desc.BlockSize == 0;
    }

    [return: NativeTypeName("LPCWSTR")]
    public readonly char* GetName()
    {
        return m_Name;
    }

    public readonly D3D12MA_BlockVector* GetBlockVector()
    {
        return m_BlockVector;
    }

    public D3D12MA_CommittedAllocationList* GetCommittedAllocationList()
    {
        return SupportsCommittedAllocations() ? (D3D12MA_CommittedAllocationList*)(Unsafe.AsPointer(ref m_CommittedAllocations)) : null;
    }

    public HRESULT Init()
    {
        m_CommittedAllocations.Init(m_Allocator->UseMutex(), m_Desc.HeapProperties.Type, (D3D12MA_PoolPimpl*)(Unsafe.AsPointer(ref this)));
        return m_BlockVector->CreateMinBlocks();
    }

    public void GetStatistics([NativeTypeName("D3D12MA::Statistics &")] out D3D12MA_Statistics outStats)
    {
        D3D12MA_ClearStatistics(out outStats);
        m_BlockVector->AddStatistics(ref outStats);
        m_CommittedAllocations.AddStatistics(ref outStats);
    }

    public void CalculateStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] out D3D12MA_DetailedStatistics outStats)
    {
        D3D12MA_ClearDetailedStatistics(out outStats);
        AddDetailedStatistics(ref outStats);
    }

    public void AddDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] ref D3D12MA_DetailedStatistics inoutStats)
    {
        m_BlockVector->AddDetailedStatistics(ref inoutStats);
        m_CommittedAllocations.AddDetailedStatistics(ref inoutStats);
    }

    public void SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        FreeName();

        if (Name != null)
        {
            nuint nameCharCount = wcslen(Name) + 1;
            m_Name = D3D12MA_NEW_ARRAY<char>(m_Allocator->GetAllocs(), nameCharCount);
            _ = memcpy(m_Name, Name, nameCharCount * sizeof(char));
        }
    }

    private void FreeName()
    {
        if (m_Name != null)
        {
            nuint nameCharCount = wcslen(m_Name) + 1;
            D3D12MA_DELETE_ARRAY(m_Allocator->GetAllocs(), m_Name);
            m_Name = null;
        }
    }
}
