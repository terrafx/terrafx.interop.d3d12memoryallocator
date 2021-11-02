// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemAlloc;
using static TerraFX.Interop.D3D12MA_ALLOCATION_FLAGS;

namespace TerraFX.Interop
{
    /// <summary>
    /// Sequence of NormalBlock. Represents memory blocks allocated for a specific heap type and possibly resource type(if only Tier 1 is supported).
    /// <para>Synchronized internally with a mutex.</para>
    /// </summary>
    internal unsafe struct D3D12MA_BlockVector : IDisposable
    {
        private D3D12MA_Allocator* m_hAllocator;

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

        /* There can be at most one allocation that is completely empty - a
        hysteresis to avoid pessimistic case of alternating creation and destruction
        of a VkDeviceMemory. */
        private bool m_HasEmptyBlock;

        private D3D12MA_RW_MUTEX m_Mutex;

        // Incrementally sorted by sumFreeSize, ascending.
        internal D3D12MA_Vector<Pointer<D3D12MA_NormalBlock>> m_Blocks;

        [NativeTypeName("UINT")]
        private uint m_NextBlockId;

        internal static void _ctor(ref D3D12MA_BlockVector pThis, D3D12MA_Allocator* hAllocator, [NativeTypeName("const D3D12_HEAP_PROPERTIES&")] D3D12_HEAP_PROPERTIES* heapProps, D3D12_HEAP_FLAGS heapFlags, [NativeTypeName("UINT64")] ulong preferredBlockSize, [NativeTypeName("size_t")] nuint minBlockCount, [NativeTypeName("size_t")] nuint maxBlockCount, bool explicitBlockSize, [NativeTypeName("UINT64")] ulong minAllocationAlignment)
        {
            pThis.m_hAllocator = hAllocator;
            pThis.m_HeapProps = *heapProps;
            pThis.m_HeapFlags = heapFlags;
            pThis.m_PreferredBlockSize = preferredBlockSize;
            pThis.m_MinBlockCount = minBlockCount;
            pThis.m_MaxBlockCount = maxBlockCount;
            pThis.m_ExplicitBlockSize = explicitBlockSize;
            pThis.m_MinAllocationAlignment = minAllocationAlignment;
            pThis.m_HasEmptyBlock = false;
            D3D12MA_Vector<Pointer<D3D12MA_NormalBlock>>._ctor(ref pThis.m_Blocks, hAllocator->GetAllocs());
            pThis.m_NextBlockId = 0;

            D3D12MA_RW_MUTEX._ctor(ref pThis.m_Mutex);
        }

        public void Dispose()
        {
            for (nuint i = m_Blocks.size(); unchecked(i-- != 0);)
            {
                D3D12MA_DELETE(m_hAllocator->GetAllocs(), m_Blocks[i]->Value);
            }

            m_Blocks.Dispose();
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

        [return: NativeTypeName("const D3D12_HEAP_PROPERTIES&")]
        public readonly D3D12_HEAP_PROPERTIES* GetHeapProperties() => (D3D12_HEAP_PROPERTIES*)Unsafe.AsPointer(ref Unsafe.AsRef(in m_HeapProps));

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetPreferredBlockSize() => m_PreferredBlockSize;

        public bool IsEmpty()
        {
            using var @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());
            return m_Blocks.empty();
        }

        [return: NativeTypeName("HRESULT")]
        public int Allocate([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA_ALLOCATION_DESC&")] D3D12MA_ALLOCATION_DESC* allocDesc, [NativeTypeName("size_t")] nuint allocationCount, D3D12MA_Allocation** pAllocations)
        {
            nuint allocIndex;
            HRESULT hr = S_OK;

            using (var @lock = new D3D12MA_MutexLockWrite(ref m_Mutex, m_hAllocator->UseMutex()))
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
                while (unchecked(allocIndex-- != 0))
                {
                    Free(pAllocations[allocIndex]);
                }

                ZeroMemory(pAllocations, (nuint)sizeof(D3D12MA_Allocation*) * allocationCount);
            }

            return hr;
        }

        public void Free(D3D12MA_Allocation* hAllocation)
        {
            D3D12MA_NormalBlock* pBlockToDelete = null;

            bool budgetExceeded = false;
            if (IsHeapTypeStandard(m_HeapProps.Type))
            {
                D3D12MA_Budget budget = default;
                m_hAllocator->GetBudgetForHeapType(&budget, m_HeapProps.Type);
                budgetExceeded = budget.UsageBytes >= budget.BudgetBytes;
            }

            using (var @lock = new D3D12MA_MutexLockWrite(ref m_Mutex, m_hAllocator->UseMutex()))
            {
                D3D12MA_NormalBlock* pBlock = hAllocation->m_Placed.block;

                pBlock->m_pMetadata->FreeAtOffset(hAllocation->GetOffset());
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && pBlock->Validate());

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
                    // pBlock didn't become empty, but we have another empty block - find and free that one.
                    // (This is optional, heuristics.)

                    D3D12MA_NormalBlock* pLastBlock = m_Blocks.back()->Value;

                    if (pLastBlock->m_pMetadata->IsEmpty())
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
        public int CreateResource([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA_ALLOCATION_DESC&")] D3D12MA_ALLOCATION_DESC* allocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC&")] D3D12_RESOURCE_DESC* resourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE&")] D3D12_CLEAR_VALUE* pOptimizedClearValue, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
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
                    __uuidof<ID3D12Resource>(), (void**)&res
                );

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
                        _ = res->Release();
                        SAFE_RELEASE(ref *ppAllocation);
                    }
                }
                else
                {
                    SAFE_RELEASE(ref *ppAllocation);
                }
            }

            return hr;
        }

        [return: NativeTypeName("HRESULT")]
        public int CreateResource2([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA_ALLOCATION_DESC&")] D3D12MA_ALLOCATION_DESC* allocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1&")] D3D12_RESOURCE_DESC1* resourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE&")] D3D12_CLEAR_VALUE* pOptimizedClearValue, ID3D12ProtectedResourceSession *pProtectedSession, D3D12MA_Allocation** ppAllocation, [NativeTypeName("REFIID")] Guid* riidResource, void** ppvResource)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pProtectedSession == null)); // "Should never get here. pProtectedSession != NULL currently requires committed resources."

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
                    __uuidof<ID3D12Resource>(), (void**)&res
                );

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
                        _ = res->Release();
                        SAFE_RELEASE(ref *ppAllocation);
                    }
                }
                else
                {
                    SAFE_RELEASE(ref *ppAllocation);
                }
            }

            return hr;
        }

        public void AddStats([NativeTypeName("StatInfo&")] D3D12MA_StatInfo* outStats)
        {
            using var @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());

            for (nuint i = 0; i < m_Blocks.size(); ++i)
            {
                D3D12MA_NormalBlock* pBlock = m_Blocks[i]->Value;

                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pBlock != null));
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && pBlock->Validate());

                D3D12MA_StatInfo blockStatInfo;
                pBlock->m_pMetadata->CalcAllocationStatInfo(&blockStatInfo);

                AddStatInfo(ref *outStats, ref blockStatInfo);
            }
        }

        public void AddStats([NativeTypeName("Stats&")] D3D12MA_Stats* outStats)
        {
            uint heapTypeIndex = HeapTypeToIndex(m_HeapProps.Type);
            ref D3D12MA_StatInfo pStatInfo = ref outStats->HeapType[(int)heapTypeIndex];

            using var @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());

            for (nuint i = 0; i < m_Blocks.size(); ++i)
            {
                D3D12MA_NormalBlock* pBlock = m_Blocks[i]->Value;

                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pBlock != null));
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && pBlock->Validate());

                D3D12MA_StatInfo blockStatInfo;
                pBlock->m_pMetadata->CalcAllocationStatInfo(&blockStatInfo);

                AddStatInfo(ref outStats->Total, ref blockStatInfo);
                AddStatInfo(ref pStatInfo, ref blockStatInfo);
            }
        }

        public void WriteBlockInfoToJson([NativeTypeName("JsonWriter&")] D3D12MA_JsonWriter* json)
        {
            using var @lock = new D3D12MA_MutexLockRead(ref m_Mutex, m_hAllocator->UseMutex());

            json->BeginObject();

            for (nuint i = 0, count = m_Blocks.size(); i < count; ++i)
            {
                D3D12MA_NormalBlock* pBlock = m_Blocks[i]->Value;

                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pBlock != null));
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && pBlock->Validate());

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

            for (nuint i = m_Blocks.size(); unchecked(i-- != 0);)
            {
                result += m_Blocks[i]->Value->m_pMetadata->GetSize();
            }

            return result;
        }

        [return: NativeTypeName("UINT64")]
        private readonly ulong CalcMaxBlockSize()
        {
            ulong result = 0;

            for (nuint i = m_Blocks.size(); unchecked(i-- != 0);)
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
        private void Remove(D3D12MA_NormalBlock* pBlock)
        {
            for (nuint blockIndex = 0; blockIndex < m_Blocks.size(); ++blockIndex)
            {
                if (m_Blocks[blockIndex]->Value == pBlock)
                {
                    m_Blocks.remove(blockIndex);
                    return;
                }
            }

            D3D12MA_ASSERT(false);
        }

        /// <summary>Performs single step in sorting m_Blocks. They may not be fully sorted after this call.</summary>
        private void IncrementallySortBlocks()
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
        private int AllocatePage([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, [NativeTypeName("const D3D12MA_ALLOCATION_DESC&")] D3D12MA_ALLOCATION_DESC* allocDesc, D3D12MA_Allocation** pAllocation)
        {
            // Early reject: requested allocation size is larger that maximum block size for this block vector.
            if (size + (2 * D3D12MA_DEBUG_MARGIN) > m_PreferredBlockSize)
            {
                return E_OUTOFMEMORY;
            }

            ulong freeMemory = UINT64_MAX;
            if (IsHeapTypeStandard(m_HeapProps.Type))
            {
                D3D12MA_Budget budget = default;
                m_hAllocator->GetBudgetForHeapType(&budget, m_HeapProps.Type);
                freeMemory = (budget.UsageBytes < budget.BudgetBytes) ? (budget.BudgetBytes - budget.UsageBytes) : 0;
            }

            bool canCreateNewBlock = ((allocDesc->Flags & D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE) == 0) && (m_Blocks.size() < m_MaxBlockCount);

            // Even if we don't have to stay within budget with this allocation, when the
            // budget would be exceeded, we don't want to allocate new blocks, but always
            // create resources as committed.
            canCreateNewBlock &= freeMemory >= size;

            // 1. Search existing allocations
            {
                // Forward order in m_Blocks - prefer blocks with smallest amount of free space.
                for (nuint blockIndex = 0; blockIndex < m_Blocks.size(); ++blockIndex)
                {
                    D3D12MA_NormalBlock* pCurrBlock = m_Blocks[blockIndex]->Value;
                    D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pCurrBlock != null));

                    HRESULT hr = AllocateFromBlock(
                        pCurrBlock,
                        size,
                        alignment,
                        allocDesc->Flags,
                        pAllocation
                    );

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
                HRESULT hr = newBlockSize <= freeMemory ? CreateBlock(newBlockSize, &newBlockIndex) : E_OUTOFMEMORY;

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
                            hr = newBlockSize <= freeMemory ? CreateBlock(newBlockSize, &newBlockIndex) : E_OUTOFMEMORY;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (SUCCEEDED(hr))
                {
                    D3D12MA_NormalBlock* pBlock = m_Blocks[newBlockIndex]->Value;
                    D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pBlock->m_pMetadata->GetSize() >= size));

                    hr = AllocateFromBlock(
                        pBlock,
                        size,
                        alignment,
                        allocDesc->Flags,
                        pAllocation
                    );

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
        private int AllocateFromBlock(D3D12MA_NormalBlock* pBlock, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, D3D12MA_ALLOCATION_FLAGS allocFlags, D3D12MA_Allocation** pAllocation)
        {
            alignment = D3D12MA_MAX(alignment, m_MinAllocationAlignment);

            D3D12MA_AllocationRequest currRequest = default;

            if (pBlock->m_pMetadata->CreateAllocationRequest(size, alignment, &currRequest))
            {
                // We no longer have an empty Allocation.
                if (pBlock->m_pMetadata->IsEmpty())
                {
                    m_HasEmptyBlock = false;
                }

                *pAllocation = m_hAllocator->GetAllocationObjectAllocator()->Allocate(m_hAllocator, size, currRequest.zeroInitialized);
                pBlock->m_pMetadata->Alloc(&currRequest, size, *pAllocation);
                (*pAllocation)->InitPlaced(currRequest.offset, alignment, pBlock);
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && pBlock->Validate());
                m_hAllocator->m_Budget.AddAllocation(HeapTypeToIndex(m_HeapProps.Type), size);
                return S_OK;
            }

            return E_OUTOFMEMORY;
        }

        [return: NativeTypeName("HRESULT")]
        private int CreateBlock([NativeTypeName("UINT64")] ulong blockSize, [NativeTypeName("size_t")] nuint* pNewBlockIndex)
        {
            D3D12MA_NormalBlock* pBlock = D3D12MA_NEW<D3D12MA_NormalBlock>(m_hAllocator->GetAllocs());

            D3D12MA_NormalBlock._ctor(
                ref *pBlock,
                m_hAllocator,
                ref this,
                (D3D12_HEAP_PROPERTIES*)Unsafe.AsPointer(ref m_HeapProps),
                m_HeapFlags,
                blockSize,
                m_NextBlockId++
            );

            int hr = pBlock->Init();

            if (FAILED(hr))
            {
                D3D12MA_DELETE(m_hAllocator->GetAllocs(), pBlock);
                return hr;
            }

            var block = (Pointer<D3D12MA_NormalBlock>)pBlock;
            m_Blocks.push_back(in block);

            if (pNewBlockIndex != null)
            {
                *pNewBlockIndex = m_Blocks.size() - 1;
            }

            return hr;
        }
    }
}
