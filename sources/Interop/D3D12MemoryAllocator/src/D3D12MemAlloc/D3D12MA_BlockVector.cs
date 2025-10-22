// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).


using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Sequence of <see cref="D3D12MA_NormalBlock" />. Represents memory blocks allocated for a specific heap type and possibly resource type(if only Tier 1 is supported).</summary>
/// <remarks>Synchronized internally with a mutex.</remarks>
internal unsafe partial struct D3D12MA_BlockVector : IDisposable
{
    internal D3D12MA_AllocatorPimpl* m_hAllocator;

    private D3D12_HEAP_PROPERTIES m_HeapProps;

    private D3D12_HEAP_FLAGS m_HeapFlags;

    [NativeTypeName("UINT64")]
    private ulong m_PreferredBlockSize;

    [NativeTypeName("size_t")]
    private nuint m_MinBlockCount;

    [NativeTypeName("size_t")]
    private nuint m_MaxBlockCount;

    private bool m_ExplicitBlockSize;

    [NativeTypeName("UINT64")]
    private ulong m_MinAllocationAlignment;

    [NativeTypeName("UINT32")]
    private uint m_Algorithm;

    private bool m_DenyMsaaTextures;

    private ID3D12ProtectedResourceSession* m_ProtectedSession;

    // There can be at most one allocation that is completely empty - a hysteresis to avoid pessimistic case of alternating creation and destruction of a ID3D12Heap
    private bool m_HasEmptyBlock;

    private D3D12MA_RW_MUTEX m_Mutex;

    // Incrementally sorted by sumFreeSize, ascending.
    internal D3D12MA_Vector<Pointer<D3D12MA_NormalBlock>> m_Blocks;

    [NativeTypeName("UINT")]
    private uint m_NextBlockId;

    private bool m_IncrementalSort;

    public static D3D12MA_BlockVector* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, D3D12MA_AllocatorPimpl* hAllocator, [NativeTypeName("const D3D12_HEAP_PROPERTIES &")] in D3D12_HEAP_PROPERTIES heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong preferredBlockSize, [NativeTypeName("size_t")] nuint minBlockCount, [NativeTypeName("size_t")] nuint maxBlockCount, bool explicitBlockSize, [NativeTypeName("UINT64")] ulong minAllocationAlignment, [NativeTypeName("UINT32")] uint algorithm, bool denyMsaaTextures, ID3D12ProtectedResourceSession* pProtectedSession)
    {
        D3D12MA_BlockVector* result = D3D12MA_NEW<D3D12MA_BlockVector>(allocs);
        result->_ctor(hAllocator, heapProps, heapFlags, preferredBlockSize, minBlockCount, maxBlockCount, explicitBlockSize, minAllocationAlignment, algorithm, denyMsaaTextures, pProtectedSession);
        return result;
    }

    private void _ctor()
    {
        m_Mutex = new D3D12MA_RW_MUTEX();
        m_IncrementalSort = true;
    }

    private void _ctor(D3D12MA_AllocatorPimpl* hAllocator, [NativeTypeName("const D3D12_HEAP_PROPERTIES &")] in D3D12_HEAP_PROPERTIES heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong preferredBlockSize, [NativeTypeName("size_t")] nuint minBlockCount, [NativeTypeName("size_t")] nuint maxBlockCount, bool explicitBlockSize, [NativeTypeName("UINT64")] ulong minAllocationAlignment, [NativeTypeName("UINT32")] uint algorithm, bool denyMsaaTextures, ID3D12ProtectedResourceSession* pProtectedSession)
    {
        _ctor();

        m_hAllocator = hAllocator;
        m_HeapProps = heapProps;
        m_HeapFlags = heapFlags;
        m_PreferredBlockSize = preferredBlockSize;
        m_MinBlockCount = minBlockCount;
        m_MaxBlockCount = maxBlockCount;
        m_ExplicitBlockSize = explicitBlockSize;
        m_MinAllocationAlignment = minAllocationAlignment;
        m_Algorithm = algorithm;
        m_DenyMsaaTextures = denyMsaaTextures;
        m_ProtectedSession = pProtectedSession;
        m_HasEmptyBlock = false;
        m_Blocks = new D3D12MA_Vector<Pointer<D3D12MA_NormalBlock>>(hAllocator->GetAllocs());
        m_NextBlockId = 0;
    }

    public void Dispose()
    {
        for (nuint i = m_Blocks.size(); i-- != 0;)
        {
            D3D12MA_DELETE(m_hAllocator->GetAllocs(), m_Blocks[i].Value);
        }

        m_Blocks.Dispose();
    }

    [UnscopedRef]
    [return: NativeTypeName("const D3D12_HEAP_PROPERTIES &")]
    public readonly ref readonly D3D12_HEAP_PROPERTIES GetHeapProperties()
    {
        return ref m_HeapProps;
    }

    public readonly D3D12_HEAP_FLAGS GetHeapFlags()
    {
        return m_HeapFlags;
    }

    [return: NativeTypeName("UINT64")]
    public readonly ulong GetPreferredBlockSize()
    {
        return m_PreferredBlockSize;
    }

    [return: NativeTypeName("UINT32")]
    public readonly uint GetAlgorithm()
    {
        return m_Algorithm;
    }

    public readonly bool DeniesMsaaTextures()
    {
        return m_DenyMsaaTextures;
    }

    // To be used only while the m_Mutex is locked. Used during defragmentation.
    [return: NativeTypeName("size_t")]
    public readonly nuint GetBlockCount()
    {
        return m_Blocks.size();
    }

    // To be used only while the m_Mutex is locked. Used during defragmentation.
    public readonly D3D12MA_NormalBlock* GetBlock([NativeTypeName("size_t")] nuint index)
    {
        return m_Blocks[index].Value;
    }

    [UnscopedRef]
    [return: NativeTypeName("D3D12MA_RW_MUTEX &")]
    public ref D3D12MA_RW_MUTEX GetMutex()
    {
        return ref m_Mutex;
    }

    public HRESULT CreateMinBlocks()
    {
        for (nuint i = 0; i < m_MinBlockCount; ++i)
        {
            HRESULT hr = CreateBlock(m_PreferredBlockSize, null);

            if (FAILED(hr))
            {
                return hr;
            }
        }

        return S_OK;
    }

    public bool IsEmpty()
    {
        using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());
        return m_Blocks.empty();
    }

    public HRESULT Allocate([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA::ALLOCATION_DESC &")] in D3D12MA_ALLOCATION_DESC allocDesc, [NativeTypeName("size_t")] nuint allocationCount, D3D12MA_Allocation** pAllocations)
    {
        nuint allocIndex;
        HRESULT hr = S_OK;

        using (D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_Mutex, m_hAllocator->UseMutex()))
        {
            for (allocIndex = 0; allocIndex < allocationCount; ++allocIndex)
            {
                hr = AllocatePage(size, alignment, allocDesc, pAllocations + allocIndex);

                if (FAILED(hr))
                {
                    break;
                }
            }
        }

        if (FAILED(hr))
        {
            // Free all already created allocations.
            while (allocIndex-- != 0)
            {
                Free(pAllocations[allocIndex]);
            }
            ZeroMemory(pAllocations, __sizeof<Pointer<D3D12MA_Allocation>>() * allocationCount);
        }

        return hr;
    }

    public void Free(D3D12MA_Allocation* hAllocation)
    {
        D3D12MA_NormalBlock* pBlockToDelete = null;
        bool budgetExceeded = false;

        if (D3D12MA_IsHeapTypeStandard(m_HeapProps.Type))
        {
            D3D12MA_Budget budget = new D3D12MA_Budget();
            m_hAllocator->GetBudgetForHeapType(out budget, m_HeapProps.Type);
            budgetExceeded = budget.UsageBytes >= budget.BudgetBytes;
        }

        // Scope for lock.
        using (D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_Mutex, m_hAllocator->UseMutex()))
        {
            D3D12MA_NormalBlock* pBlock = hAllocation->Anonymous.m_Placed.block;

            pBlock->m_pMetadata->Free(hAllocation->GetAllocHandle());
            D3D12MA_HEAVY_ASSERT(pBlock->Validate());

            nuint blockCount = m_Blocks.size();

            // pBlock became empty after this deallocation.
            if (pBlock->m_pMetadata->IsEmpty())
            {
                // Already has empty Allocation. We don't want to have two, so delete this one.
                if ((m_HasEmptyBlock || budgetExceeded) && (blockCount > m_MinBlockCount))
                {
                    pBlockToDelete = pBlock;
                    Remove(pBlock);
                }
                else
                {
                    // We now have first empty block.
                    m_HasEmptyBlock = true;
                }
            }
            else if (m_HasEmptyBlock && blockCount > m_MinBlockCount)
            {
                // pBlock didn't become empty, but we have another empty block - find and free that one. (This is optional, heuristics.)
                D3D12MA_NormalBlock* pLastBlock = m_Blocks.back().Value;

                if (pLastBlock->m_pMetadata->IsEmpty())
                {
                    pBlockToDelete = pLastBlock;
                    m_Blocks.pop_back();
                    m_HasEmptyBlock = false;
                }
            }

            IncrementallySortBlocks();
        }

        // Destruction of a free Allocation. Deferred until this point, outside of mutex lock, for performance reason.
        if (pBlockToDelete != null)
        {
            D3D12MA_DELETE(m_hAllocator->GetAllocs(), pBlockToDelete);
        }
    }

    public HRESULT CreateResource([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA::ALLOCATION_DESC &")] in D3D12MA_ALLOCATION_DESC allocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC &")] in D3D12_RESOURCE_DESC resourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        HRESULT hr = Allocate(size, alignment, allocDesc, 1, ppAllocation);

        if (SUCCEEDED(hr))
        {
            ID3D12Resource* res = null;
            hr = m_hAllocator->GetDevice()->CreatePlacedResource((*ppAllocation)->Anonymous.m_Placed.block->GetHeap(), (*ppAllocation)->GetOffset(), (D3D12_RESOURCE_DESC*)(Unsafe.AsPointer(ref Unsafe.AsRef(in resourceDesc))), InitialResourceState, pOptimizedClearValue, __uuidof<ID3D12Resource>(), (void**)(&res));

            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    (*ppAllocation)->SetResourcePointer(res, (D3D12_RESOURCE_DESC*)(Unsafe.AsPointer(ref Unsafe.AsRef(in resourceDesc))));
                }
                else
                {
                    _ = res->Release();
                    D3D12MA_SAFE_RELEASE(ref *ppAllocation);
                }
            }
            else
            {
                D3D12MA_SAFE_RELEASE(ref *ppAllocation);
            }
        }
        return hr;
    }

    public HRESULT CreateResource2([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA::ALLOCATION_DESC &")] in D3D12MA_ALLOCATION_DESC allocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 &")] in D3D12_RESOURCE_DESC1 resourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
    {
        ID3D12Device8* device8 = m_hAllocator->GetDevice8();

        if (device8 == null)
        {
            return E_NOINTERFACE;
        }
        Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0));

        HRESULT hr = Allocate(size, alignment, allocDesc, 1, ppAllocation);

        if (SUCCEEDED(hr))
        {
            ID3D12Resource* res = null;
            hr = device8->CreatePlacedResource1((*ppAllocation)->Anonymous.m_Placed.block->GetHeap(), (*ppAllocation)->GetOffset(), (D3D12_RESOURCE_DESC1*)(Unsafe.AsPointer(ref Unsafe.AsRef(in resourceDesc))), InitialResourceState, pOptimizedClearValue, __uuidof<ID3D12Resource>(), (void**)(&res));

            if (SUCCEEDED(hr))
            {
                if (ppvResource != null)
                {
                    hr = res->QueryInterface(riidResource, ppvResource);
                }

                if (SUCCEEDED(hr))
                {
                    (*ppAllocation)->SetResourcePointer(res, (D3D12_RESOURCE_DESC1*)(Unsafe.AsPointer(ref Unsafe.AsRef(in resourceDesc))));
                }
                else
                {
                    _ = res->Release();
                    D3D12MA_SAFE_RELEASE(ref *ppAllocation);
                }
            }
            else
            {
                D3D12MA_SAFE_RELEASE(ref *ppAllocation);
            }
        }
        return hr;
    }

    public void AddStatistics([NativeTypeName("D3D12MA::Statistics &")] ref D3D12MA_Statistics inoutStats)
    {
        using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());

        for (nuint i = 0; i < m_Blocks.size(); ++i)
        {
            D3D12MA_NormalBlock* pBlock = m_Blocks[i].Value;

            D3D12MA_ASSERT(pBlock != null);
            D3D12MA_HEAVY_ASSERT(pBlock->Validate());

            pBlock->m_pMetadata->AddStatistics((D3D12MA_Statistics*)(Unsafe.AsPointer(ref inoutStats)));
        }
    }

    public void AddDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] ref D3D12MA_DetailedStatistics inoutStats)
    {
        using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());

        for (nuint i = 0; i < m_Blocks.size(); ++i)
        {
            D3D12MA_NormalBlock* pBlock = m_Blocks[i].Value;

            D3D12MA_ASSERT(pBlock != null);
            D3D12MA_HEAVY_ASSERT(pBlock->Validate());

            pBlock->m_pMetadata->AddDetailedStatistics((D3D12MA_DetailedStatistics*)(Unsafe.AsPointer(ref inoutStats)));
        }
    }

    public void WriteBlockInfoToJson([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json)
    {
        using D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());

        json.BeginObject();

        for (nuint i = 0, count = m_Blocks.size(); i < count; ++i)
        {
            D3D12MA_NormalBlock* pBlock = m_Blocks[i].Value;

            D3D12MA_ASSERT(pBlock != null);
            D3D12MA_HEAVY_ASSERT(pBlock->Validate());

            json.BeginString();
            json.ContinueString(pBlock->GetId());
            json.EndString();

            json.BeginObject();
            pBlock->m_pMetadata->WriteAllocationInfoToJson((D3D12MA_JsonWriter*)(Unsafe.AsPointer(ref json)));
            json.EndObject();
        }

        json.EndObject();
    }

    // Disable incremental sorting when freeing allocations
    internal void SetIncrementalSort(bool val)
    {
        m_IncrementalSort = val;
    }

    [return: NativeTypeName("UINT64")]
    private readonly ulong CalcSumBlockSize()
    {
        ulong result = 0;

        for (nuint i = m_Blocks.size(); i-- != 0;)
        {
            result += m_Blocks[i].Value->m_pMetadata->GetSize();
        }
        return result;
    }

    [return: NativeTypeName("UINT64")]
    private readonly ulong CalcMaxBlockSize()
    {
        ulong result = 0;

        for (nuint i = m_Blocks.size(); i-- != 0;)
        {
            result = D3D12MA_MAX(result, m_Blocks[i].Value->m_pMetadata->GetSize());

            if (result >= m_PreferredBlockSize)
            {
                break;
            }
        }
        return result;
    }

    // Finds and removes given block from vector.
    private void Remove(D3D12MA_NormalBlock* pBlock)
    {
        for (nuint blockIndex = 0; blockIndex < m_Blocks.size(); ++blockIndex)
        {
            if (m_Blocks[blockIndex].Value == pBlock)
            {
                m_Blocks.remove(blockIndex);
                return;
            }
        }
        D3D12MA_FAIL();
    }

    // Performs single step in sorting m_Blocks. They may not be fully sorted after this call.
    private void IncrementallySortBlocks()
    {
        if (!m_IncrementalSort)
        {
            return;
        }

        // Bubble sort only until first swap.
        for (nuint i = 1; i < m_Blocks.size(); ++i)
        {
            if (m_Blocks[i - 1].Value->m_pMetadata->GetSumFreeSize() > m_Blocks[i].Value->m_pMetadata->GetSumFreeSize())
            {
                D3D12MA_SWAP(ref m_Blocks[i - 1], ref m_Blocks[i]);
                return;
            }
        }
    }

    internal readonly void SortByFreeSize()
    {
        D3D12MA_SORT(m_Blocks.begin(), m_Blocks.end(), new @cmp());
    }

    private HRESULT AllocatePage([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA::ALLOCATION_DESC &")] in D3D12MA_ALLOCATION_DESC allocDesc, D3D12MA_Allocation** pAllocation)
    {
        // Early reject: requested allocation size is larger that maximum block size for this block vector.
        if ((size + D3D12MA_DEBUG_MARGIN) > m_PreferredBlockSize)
        {
            return E_OUTOFMEMORY;
        }

        ulong freeMemory = ulong.MaxValue;

        if (D3D12MA_IsHeapTypeStandard(m_HeapProps.Type))
        {
            D3D12MA_Budget budget = new D3D12MA_Budget();
            m_hAllocator->GetBudgetForHeapType(out budget, m_HeapProps.Type);
            freeMemory = (budget.UsageBytes < budget.BudgetBytes) ? (budget.BudgetBytes - budget.UsageBytes) : 0;
        }

        // Even if we don't have to stay within budget with this allocation, when the budget would be exceeded, we don't want to allocate new blocks, but always create resources as committed.
        bool canCreateNewBlock = ((allocDesc.Flags & D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE) == 0) && (m_Blocks.size() < m_MaxBlockCount) && (freeMemory >= size);

        // 1. Search existing allocations
        {
            // Forward order in m_Blocks - prefer blocks with smallest amount of free space.
            for (nuint blockIndex = 0; blockIndex < m_Blocks.size(); ++blockIndex)
            {
                D3D12MA_NormalBlock* pCurrBlock = m_Blocks[blockIndex].Value;
                D3D12MA_ASSERT(pCurrBlock != null);

                HRESULT hr = AllocateFromBlock(pCurrBlock, size, alignment, allocDesc.Flags, allocDesc.pPrivateData, (uint)(allocDesc.Flags & D3D12MA_ALLOCATION_FLAG_STRATEGY_MASK), pAllocation);

                if (SUCCEEDED(hr))
                {
                    return hr;
                }
            }
        }

        // 2. Try to create new block.
        if (canCreateNewBlock)
        {
            // Calculate optimal size for new block.
            ulong newBlockSize = m_PreferredBlockSize;
            uint newBlockSizeShift = 0;

            if (!m_ExplicitBlockSize)
            {
                // Allocate 1/8, 1/4, 1/2 as first blocks.
                ulong maxExistingBlockSize = CalcMaxBlockSize();

                for (uint i = 0; i < D3D12MA_NEW_BLOCK_SIZE_SHIFT_MAX; ++i)
                {
                    ulong smallerNewBlockSize = newBlockSize / 2;

                    if ((smallerNewBlockSize > maxExistingBlockSize) && (smallerNewBlockSize >= (size * 2)))
                    {
                        newBlockSize = smallerNewBlockSize;
                        ++newBlockSizeShift;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            nuint newBlockIndex = 0;
            HRESULT hr = (newBlockSize <= freeMemory) ? CreateBlock(newBlockSize, &newBlockIndex) : E_OUTOFMEMORY;

            // Allocation of this size failed? Try 1/2, 1/4, 1/8 of m_PreferredBlockSize.
            if (!m_ExplicitBlockSize)
            {
                while (FAILED(hr) && (newBlockSizeShift < D3D12MA_NEW_BLOCK_SIZE_SHIFT_MAX))
                {
                    ulong smallerNewBlockSize = newBlockSize / 2;

                    if (smallerNewBlockSize >= size)
                    {
                        newBlockSize = smallerNewBlockSize;
                        ++newBlockSizeShift;
                        hr = (newBlockSize <= freeMemory) ? CreateBlock(newBlockSize, &newBlockIndex) : E_OUTOFMEMORY;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (SUCCEEDED(hr))
            {
                D3D12MA_NormalBlock* pBlock = m_Blocks[newBlockIndex].Value;
                D3D12MA_ASSERT(pBlock->m_pMetadata->GetSize() >= size);

                hr = AllocateFromBlock(pBlock, size, alignment, allocDesc.Flags, allocDesc.pPrivateData, (uint)(allocDesc.Flags & D3D12MA_ALLOCATION_FLAG_STRATEGY_MASK), pAllocation);

                if (SUCCEEDED(hr))
                {
                    return hr;
                }
                else
                {
                    // Allocation from new block failed, possibly due to D3D12MA_DEBUG_MARGIN or alignment.
                    return E_OUTOFMEMORY;
                }
            }
        }

        return E_OUTOFMEMORY;
    }

    internal HRESULT AllocateFromBlock(D3D12MA_NormalBlock* pBlock, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, D3D12MA_ALLOCATION_FLAGS allocFlags, void* pPrivateData, [NativeTypeName("UINT32")] uint strategy, D3D12MA_Allocation** pAllocation)
    {
        alignment = D3D12MA_MAX(alignment, m_MinAllocationAlignment);

        D3D12MA_AllocationRequest currRequest = new D3D12MA_AllocationRequest();

        if (pBlock->m_pMetadata->CreateAllocationRequest(size, alignment, (allocFlags & D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS) != 0, strategy, &currRequest))
        {
            return CommitAllocationRequest(currRequest, pBlock, size, alignment, pPrivateData, pAllocation);
        }
        return E_OUTOFMEMORY;
    }

    internal HRESULT CommitAllocationRequest([NativeTypeName("D3D12MA::AllocationRequest &")] in D3D12MA_AllocationRequest allocRequest, D3D12MA_NormalBlock* pBlock, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, void* pPrivateData, D3D12MA_Allocation** ppAllocation)
    {
        // We no longer have an empty Allocation.
        if (pBlock->m_pMetadata->IsEmpty())
        {
            m_HasEmptyBlock = false;
        }

        *ppAllocation = m_hAllocator->GetAllocationObjectAllocator().Allocate(m_hAllocator, size, alignment, allocRequest.zeroInitialized);
        pBlock->m_pMetadata->Alloc((D3D12MA_AllocationRequest*)(Unsafe.AsPointer(ref Unsafe.AsRef(in allocRequest))), size, *ppAllocation);

        (*ppAllocation)->InitPlaced(allocRequest.allocHandle, pBlock);
        (*ppAllocation)->SetPrivateData(pPrivateData);

        D3D12MA_HEAVY_ASSERT(pBlock->Validate());
        m_hAllocator->m_Budget.AddAllocation(m_hAllocator->HeapPropertiesToMemorySegmentGroup(m_HeapProps), size);

        return S_OK;
    }

    private HRESULT CreateBlock([NativeTypeName("UINT64")] ulong blockSize, [NativeTypeName("size_t *")] nuint* pNewBlockIndex)
    {
        D3D12MA_NormalBlock* pBlock = D3D12MA_NormalBlock.Create(m_hAllocator->GetAllocs(), m_hAllocator, (D3D12MA_BlockVector*)(Unsafe.AsPointer(ref this)), m_HeapProps, m_HeapFlags, blockSize, m_NextBlockId++);
        HRESULT hr = pBlock->Init(m_Algorithm, m_ProtectedSession, m_DenyMsaaTextures);

        if (FAILED(hr))
        {
            D3D12MA_DELETE(m_hAllocator->GetAllocs(), pBlock);
            return hr;
        }

        m_Blocks.push_back(new Pointer<D3D12MA_NormalBlock>(pBlock));

        if (pNewBlockIndex != null)
        {
            *pNewBlockIndex = m_Blocks.size() - 1;
        }

        return hr;
    }

    private unsafe struct @cmp
        : D3D12MA_CmpLess<Pointer<D3D12MA_NormalBlock>>
    {
        public readonly int Compare(Pointer<D3D12MA_NormalBlock> lhs, Pointer<D3D12MA_NormalBlock> rhs)
        {
            return lhs.Value->m_pMetadata->GetSumFreeSize().CompareTo(rhs.Value->m_pMetadata->GetSumFreeSize());
        }

        public readonly bool Invoke(in Pointer<D3D12MA_NormalBlock> lhs, in Pointer<D3D12MA_NormalBlock> rhs)
        {
            return lhs.Value->m_pMetadata->GetSumFreeSize() < rhs.Value->m_pMetadata->GetSumFreeSize();
        }
    }
}
