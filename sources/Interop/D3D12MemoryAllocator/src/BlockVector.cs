// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.ALLOCATION_FLAGS;

namespace TerraFX.Interop
{
    /// <summary>
    /// Sequence of NormalBlock. Represents memory blocks allocated for a specific heap type and possibly resource type(if only Tier 1 is supported).
    /// <para>Synchronized internally with a mutex.</para>
    /// </summary>
    internal unsafe struct BlockVector : IDisposable
    {
        readonly AllocatorPimpl* m_hAllocator;
        readonly D3D12_HEAP_TYPE m_HeapType;
        readonly D3D12_HEAP_FLAGS m_HeapFlags;

        [NativeTypeName("UINT64")]
        readonly ulong m_PreferredBlockSize;

        [NativeTypeName("size_t")]
        readonly nuint m_MinBlockCount;

        [NativeTypeName("size_t")]
        readonly nuint m_MaxBlockCount;

        readonly bool m_ExplicitBlockSize;

        [NativeTypeName("UINT64")]
        ulong m_MinBytes;

        /* There can be at most one allocation that is completely empty - a
        hysteresis to avoid pessimistic case of alternating creation and destruction
        of a VkDeviceMemory. */
        bool m_HasEmptyBlock;

        D3D12MA_RW_MUTEX m_Mutex;

        // Incrementally sorted by sumFreeSize, ascending.
        Vector<Ptr<NormalBlock>> m_Blocks;

        [NativeTypeName("UINT")]
        uint m_NextBlockId;

        public BlockVector(AllocatorPimpl* hAllocator, D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong preferredBlockSize, [NativeTypeName("size_t")] nuint minBlockCount, [NativeTypeName("size_t")] nuint maxBlockCount, bool explicitBlockSize)
        {
            m_hAllocator = hAllocator;
            m_HeapType = heapType;
            m_HeapFlags = heapFlags;
            m_PreferredBlockSize = preferredBlockSize;
            m_MinBlockCount = minBlockCount;
            m_MaxBlockCount = maxBlockCount;
            m_ExplicitBlockSize = explicitBlockSize;
            m_MinBytes = 0;
            m_HasEmptyBlock = false;
            m_Blocks = new Vector<Ptr<NormalBlock>>(hAllocator->GetAllocs());
            m_NextBlockId = 0;
            D3D12MA_RW_MUTEX.Init(out m_Mutex);
        }

        public void Dispose()
        {
            for (nuint i = m_Blocks.size(); unchecked(i-- > 0);)
            {
                D3D12MA_DELETE(m_hAllocator->GetAllocs(), m_Blocks[i]->Value);
            }
        }


        [return: NativeTypeName("HRESULT")]
        public int CreateMinBlocks()
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


        [return: NativeTypeName("UINT")]
        public readonly uint GetHeapType() => (uint)m_HeapType;

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetPreferredBlockSize() => m_PreferredBlockSize;

        public bool IsEmpty()
        {
            using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Mutex), m_hAllocator->UseMutex());
            return m_Blocks.empty();
        }

        [return: NativeTypeName("HRESULT")]
        public int Allocate([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, ALLOCATION_DESC* allocDesc, [NativeTypeName("size_t")] nuint allocationCount, Allocation** pAllocations)
        {
            nuint allocIndex;
            HRESULT hr = S_OK;

            {
                using MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Mutex), m_hAllocator->UseMutex());
                for (allocIndex = 0; allocIndex < allocationCount; ++allocIndex)
                {
                    hr = AllocatePage(
                        size,
                        alignment,
                        allocDesc,
                        pAllocations + allocIndex);
                    if (FAILED(hr))
                    {
                        break;
                    }
                }
            }

            if (FAILED(hr))
            {
                // Free all already created allocations.
                while (allocIndex-- > 0)
                {
                    Free(pAllocations[allocIndex]);
                }

                ZeroMemory(pAllocations, (nuint)sizeof(Allocation*) * allocationCount);
            }

            return hr;
        }

        public void Free(Allocation* hAllocation)
        {
            NormalBlock* pBlockToDelete = null;

            bool budgetExceeded = false;
            {
                Budget budget = default;
                m_hAllocator->GetBudgetForHeapType(&budget, m_HeapType);
                budgetExceeded = budget.UsageBytes >= budget.BudgetBytes;
            }

            using (MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Mutex), m_hAllocator->UseMutex()))
            {
                NormalBlock* pBlock = hAllocation->m_Placed.block;

                pBlock->m_pMetadata->FreeAtOffset(hAllocation->GetOffset());
                D3D12MA_HEAVY_ASSERT(pBlock->Validate());

                nuint blockCount = m_Blocks.size();
                ulong sumBlockSize = CalcSumBlockSize();
                // pBlock became empty after this deallocation.
                if (pBlock->m_pMetadata->IsEmpty())
                {
                    // Already has empty Allocation. We don't want to have two, so delete this one.
                    if ((m_HasEmptyBlock || budgetExceeded) &&
                        blockCount > m_MinBlockCount &&
                        sumBlockSize - pBlock->m_pMetadata->GetSize() >= m_MinBytes)
                    {
                        pBlockToDelete = pBlock;
                        Remove(pBlock);
                    }
                    // We now have first empty block.
                    else
                    {
                        m_HasEmptyBlock = true;
                    }
                }
                // pBlock didn't become empty, but we have another empty block - find and free that one.
                // (This is optional, heuristics.)
                else if (m_HasEmptyBlock && blockCount > m_MinBlockCount)
                {
                    NormalBlock* pLastBlock = m_Blocks.back()->Value;
                    if (pLastBlock->m_pMetadata->IsEmpty() &&
                        sumBlockSize - pLastBlock->m_pMetadata->GetSize() >= m_MinBytes)
                    {
                        pBlockToDelete = pLastBlock;
                        m_Blocks.pop_back();
                        m_HasEmptyBlock = false;
                    }
                }

                IncrementallySortBlocks();
            }

            // Destruction of a free Allocation. Deferred until this point, outside of mutex
            // lock, for performance reason.
            if (pBlockToDelete != null)
            {
                D3D12MA_DELETE(m_hAllocator->GetAllocs(), pBlockToDelete);
            }
        }

        [return: NativeTypeName("HRESULT")]
        public int CreateResource([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, ALLOCATION_DESC* allocDesc, D3D12_RESOURCE_DESC* resourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            HRESULT hr = Allocate(size, alignment, allocDesc, 1, ppAllocation);
            if (SUCCEEDED(hr))
            {
                ID3D12Resource* res = null;
                hr = m_hAllocator->GetDevice()->CreatePlacedResource(
                    (*ppAllocation)->m_Placed.block->GetHeap(),
                    (*ppAllocation)->GetOffset(),
                    resourceDesc,
                    InitialResourceState,
                    pOptimizedClearValue,
                    __uuidof<ID3D12Resource>(),
                    (void**)&res);
                if (SUCCEEDED(hr))
                {
                    if (ppvResource != null)
                    {
                        hr = res->QueryInterface(riidResource, ppvResource);
                    }

                    if (SUCCEEDED(hr))
                    {
                        (*ppAllocation)->SetResource(res, resourceDesc);
                    }
                    else
                    {
                        res->Release();
                        SAFE_RELEASE(ppAllocation);
                    }
                }
                else
                {
                    SAFE_RELEASE(ppAllocation);
                }
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        public int CreateResource2([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, ALLOCATION_DESC* allocDesc, D3D12_RESOURCE_DESC1* resourceDesc, D3D12_RESOURCE_STATES InitialResourceState, D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            D3D12MA_ASSERT(pProtectedSession == null); // "Should never get here. pProtectedSession != NULL currently requires committed resources."

            ID3D12Device8* device8 = m_hAllocator->GetDevice8();
            if (device8 == null)
            {
                return E_NOINTERFACE;
            }

            HRESULT hr = Allocate(size, alignment, allocDesc, 1, ppAllocation);
            if (SUCCEEDED(hr))
            {
                ID3D12Resource* res = null;
                hr = device8->CreatePlacedResource1(
                    (*ppAllocation)->m_Placed.block->GetHeap(),
                    (*ppAllocation)->GetOffset(),
                    resourceDesc,
                    InitialResourceState,
                    pOptimizedClearValue,
                    __uuidof<ID3D12Resource>(),
                    (void**)&res);
                if (SUCCEEDED(hr))
                {
                    if (ppvResource != null)
                    {
                        hr = res->QueryInterface(riidResource, ppvResource);
                    }

                    if (SUCCEEDED(hr))
                    {
                        (*ppAllocation)->SetResource(res, resourceDesc);
                    }
                    else
                    {
                        res->Release();
                        SAFE_RELEASE(ppAllocation);
                    }
                }
                else
                {
                    SAFE_RELEASE(ppAllocation);
                }
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        public int SetMinBytes([NativeTypeName("UINT64")] ulong minBytes)
        {
            using MutexLockWrite @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Mutex), m_hAllocator->UseMutex());

            if (minBytes == m_MinBytes)
            {
                return S_OK;
            }

            HRESULT hr = S_OK;
            ulong sumBlockSize = CalcSumBlockSize();
            nuint blockCount = m_Blocks.size();

            // New minBytes is smaller - may be able to free some blocks.
            if (minBytes < m_MinBytes)
            {
                m_HasEmptyBlock = false; // Will recalculate this value from scratch.
                for (nuint blockIndex = blockCount; blockIndex-- > 0;)
                {
                    NormalBlock* block = *m_Blocks[blockIndex];
                    ulong size = block->m_pMetadata->GetSize();
                    bool isEmpty = block->m_pMetadata->IsEmpty();
                    if (isEmpty &&
                        sumBlockSize - size >= minBytes &&
                        blockCount - 1 >= m_MinBlockCount)
                    {
                        D3D12MA_DELETE(m_hAllocator->GetAllocs(), block);
                        m_Blocks.remove(blockIndex);
                        sumBlockSize -= size;
                        --blockCount;
                    }
                    else
                    {
                        if (isEmpty)
                        {
                            m_HasEmptyBlock = true;
                        }
                    }
                }
            }
            // New minBytes is larger - may need to allocate some blocks.
            else
            {
                ulong minBlockSize = m_PreferredBlockSize >> (int)NEW_BLOCK_SIZE_SHIFT_MAX;
                while (SUCCEEDED(hr) && sumBlockSize < minBytes)
                {
                    if (blockCount < m_MaxBlockCount)
                    {
                        ulong newBlockSize = m_PreferredBlockSize;
                        if (!m_ExplicitBlockSize)
                        {
                            if (sumBlockSize + newBlockSize > minBytes)
                            {
                                newBlockSize = minBytes - sumBlockSize;
                            }
                            // Next one would be the last block to create and its size would be smaller than
                            // the smallest block size we want to use here, so make this one smaller.
                            else if (blockCount + 1 < m_MaxBlockCount &&
                                sumBlockSize + newBlockSize + minBlockSize > minBytes)
                            {
                                newBlockSize -= minBlockSize + sumBlockSize + m_PreferredBlockSize - minBytes;
                            }
                        }

                        hr = CreateBlock(newBlockSize, null);
                        if (SUCCEEDED(hr))
                        {
                            m_HasEmptyBlock = true;
                            sumBlockSize += newBlockSize;
                            ++blockCount;
                        }
                    }
                    else
                    {
                        hr = E_INVALIDARG;
                    }
                }
            }

            m_MinBytes = minBytes;
            return hr;
        }

        public void AddStats(StatInfo* outStats)
        {
            using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Mutex), m_hAllocator->UseMutex());

            for (nuint i = 0; i < m_Blocks.size(); ++i)
            {
                NormalBlock* pBlock = *m_Blocks[i];
                D3D12MA_ASSERT(pBlock != null);
                D3D12MA_HEAVY_ASSERT(pBlock->Validate());
                StatInfo blockStatInfo;
                pBlock->m_pMetadata->CalcAllocationStatInfo(&blockStatInfo);
                AddStatInfo(ref *outStats, ref blockStatInfo);
            }
        }

        public void AddStats(Stats* outStats)
        {
            uint heapTypeIndex = HeapTypeToIndex(m_HeapType);
            ref StatInfo pStatInfo = ref outStats->HeapType[(int)heapTypeIndex];

            using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Mutex), m_hAllocator->UseMutex());

            for (nuint i = 0; i < m_Blocks.size(); ++i)
            {
                NormalBlock* pBlock = *m_Blocks[i];
                D3D12MA_ASSERT(pBlock != null);
                D3D12MA_HEAVY_ASSERT(pBlock->Validate());
                StatInfo blockStatInfo;
                pBlock->m_pMetadata->CalcAllocationStatInfo(&blockStatInfo);
                AddStatInfo(ref outStats->Total, ref blockStatInfo);
                AddStatInfo(ref pStatInfo, ref blockStatInfo);
            }
        }

        public void WriteBlockInfoToJson(JsonWriter* json)
        {
            using MutexLockRead @lock = new((D3D12MA_RW_MUTEX*)Unsafe.AsPointer(ref m_Mutex), m_hAllocator->UseMutex());

            json->BeginObject();

            for (nuint i = 0, count = m_Blocks.size(); i < count; ++i)
            {
                NormalBlock* pBlock = *m_Blocks[i];
                D3D12MA_ASSERT(pBlock != null);
                D3D12MA_HEAVY_ASSERT(pBlock->Validate());
                json->BeginString();
                json->ContinueString(pBlock->GetId());
                json->EndString();

                pBlock->m_pMetadata->WriteAllocationInfoToJson(json);
            }

            json->EndObject();
        }

        [return: NativeTypeName("UINT64")]
        private readonly ulong CalcSumBlockSize()
        {
            ulong result = 0;
            for (nuint i = m_Blocks.size(); unchecked(i-- > 0);)
            {
                result += m_Blocks[i]->Value->m_pMetadata->GetSize();
            }

            return result;
        }

        [return: NativeTypeName("UINT64")]
        private readonly ulong CalcMaxBlockSize()
        {
            ulong result = 0;
            for (nuint i = m_Blocks.size(); unchecked(i-- > 0);)
            {
                result = D3D12MA_MAX(result, m_Blocks[i]->Value->m_pMetadata->GetSize());
                if (result >= m_PreferredBlockSize)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>Finds and removes given block from vector.</summary>
        void Remove(NormalBlock* pBlock)
        {
            for (nuint blockIndex = 0; blockIndex < m_Blocks.size(); ++blockIndex)
            {
                if (m_Blocks[blockIndex] == pBlock)
                {
                    m_Blocks.remove(blockIndex);
                    return;
                }
            }

            D3D12MA_ASSERT(false);
        }

        /// <summary>Performs single step in sorting m_Blocks. They may not be fully sorted after this call.</summary>
        void IncrementallySortBlocks()
        {
            // Bubble sort only until first swap.
            for (nuint i = 1; i < m_Blocks.size(); ++i)
            {
                if (m_Blocks[i - 1]->Value->m_pMetadata->GetSumFreeSize() > m_Blocks[i]->Value->m_pMetadata->GetSumFreeSize())
                {
                    D3D12MA_SWAP(m_Blocks[i - 1], m_Blocks[i]);
                    return;
                }
            }
        }

        [return: NativeTypeName("HRESULT")]
        private int AllocatePage([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, ALLOCATION_DESC* allocDesc, Allocation** pAllocation)
        {
            // Early reject: requested allocation size is larger that maximum block size for this block vector.
            if (size + 2 * D3D12MA_DEBUG_MARGIN > m_PreferredBlockSize)
            {
                return E_OUTOFMEMORY;
            }

            ulong freeMemory;
            {
                Budget budget = default;
                m_hAllocator->GetBudgetForHeapType(&budget, m_HeapType);
                freeMemory = (budget.UsageBytes < budget.BudgetBytes) ? (budget.BudgetBytes - budget.UsageBytes) : 0;
            }

            bool canCreateNewBlock =
                ((allocDesc->Flags & ALLOCATION_FLAG_NEVER_ALLOCATE) == 0) &&
                (m_Blocks.size() < m_MaxBlockCount) &&
                // Even if we don't have to stay within budget with this allocation, when the
                // budget would be exceeded, we don't want to allocate new blocks, but always
                // create resources as committed.
                freeMemory >= size;

            // 1. Search existing allocations
            {
                // Forward order in m_Blocks - prefer blocks with smallest amount of free space.
                for (nuint blockIndex = 0; blockIndex < m_Blocks.size(); ++blockIndex)
                {
                    NormalBlock* pCurrBlock = *m_Blocks[blockIndex];
                    D3D12MA_ASSERT(pCurrBlock != null);
                    HRESULT hr = AllocateFromBlock(
                        pCurrBlock,
                        size,
                        alignment,
                        allocDesc->Flags,
                        pAllocation);
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
                    for (uint i = 0; i < NEW_BLOCK_SIZE_SHIFT_MAX; ++i)
                    {
                        ulong smallerNewBlockSize = newBlockSize / 2;
                        if (smallerNewBlockSize > maxExistingBlockSize && smallerNewBlockSize >= size * 2)
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
                HRESULT hr = newBlockSize <= freeMemory ?
                    CreateBlock(newBlockSize, &newBlockIndex) : E_OUTOFMEMORY;
                // Allocation of this size failed? Try 1/2, 1/4, 1/8 of m_PreferredBlockSize.
                if (!m_ExplicitBlockSize)
                {
                    while (FAILED(hr) && newBlockSizeShift < NEW_BLOCK_SIZE_SHIFT_MAX)
                    {
                        ulong smallerNewBlockSize = newBlockSize / 2;
                        if (smallerNewBlockSize >= size)
                        {
                            newBlockSize = smallerNewBlockSize;
                            ++newBlockSizeShift;
                            hr = newBlockSize <= freeMemory ?
                                CreateBlock(newBlockSize, &newBlockIndex) : E_OUTOFMEMORY;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (SUCCEEDED(hr))
                {
                    NormalBlock* pBlock = *m_Blocks[newBlockIndex];
                    D3D12MA_ASSERT(pBlock->m_pMetadata->GetSize() >= size);

                    hr = AllocateFromBlock(
                        pBlock,
                        size,
                        alignment,
                        allocDesc->Flags,
                        pAllocation);
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

        [return: NativeTypeName("HRESULT")]
        private int AllocateFromBlock(NormalBlock* pBlock, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, ALLOCATION_FLAGS allocFlags, Allocation** pAllocation)
        {
            AllocationRequest currRequest = default;
            if (pBlock->m_pMetadata->CreateAllocationRequest(
                size,
                alignment,
                &currRequest))
            {
                // We no longer have an empty Allocation.
                if (pBlock->m_pMetadata->IsEmpty())
                {
                    m_HasEmptyBlock = false;
                }

                *pAllocation = m_hAllocator->GetAllocationObjectAllocator()->Allocate(m_hAllocator, size, currRequest.zeroInitialized);
                pBlock->m_pMetadata->Alloc(&currRequest, size, *pAllocation);
                (*pAllocation)->InitPlaced(currRequest.offset, alignment, pBlock);
                D3D12MA_HEAVY_ASSERT(pBlock->Validate());
                m_hAllocator->m_Budget.AddAllocation(HeapTypeToIndex(m_HeapType), size);
                return S_OK;
            }

            return E_OUTOFMEMORY;
        }

        [return: NativeTypeName("HRESULT")]
        private int CreateBlock([NativeTypeName("UINT64")] ulong blockSize, [NativeTypeName("size_t")] nuint* pNewBlockIndex)
        {
            NormalBlock* pBlock = D3D12MA_NEW<NormalBlock>(m_hAllocator->GetAllocs());
            *pBlock = new NormalBlock(
                m_hAllocator,
                (BlockVector*)Unsafe.AsPointer(ref this),
                m_HeapType,
                m_HeapFlags,
                blockSize,
                m_NextBlockId++);
            int hr = pBlock->Init();
            if (FAILED(hr))
            {
                D3D12MA_DELETE(m_hAllocator->GetAllocs(), pBlock);
                return hr;
            }

            m_Blocks.push_back((Ptr<NormalBlock>*)&pBlock);
            if (pNewBlockIndex != null)
            {
                *pNewBlockIndex = m_Blocks.size() - 1;
            }

            return hr;
        }
    }
}
