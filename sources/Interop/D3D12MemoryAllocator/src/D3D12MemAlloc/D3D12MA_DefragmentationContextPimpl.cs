// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_DEFRAGMENTATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_DEFRAGMENTATION_MOVE_OPERATION;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_DefragmentationContextPimpl : IDisposable
{
    /// <summary>Max number of allocations to ignore due to size constraints before ending single pass.</summary>
    private const byte MAX_ALLOCS_TO_IGNORE = 16;

    [NativeTypeName("UINT64")]
    private ulong m_MaxPassBytes;

    [NativeTypeName("UINT32")]
    private uint m_MaxPassAllocations;

    private D3D12MA_Vector<D3D12MA_DEFRAGMENTATION_MOVE> m_Moves;

    [NativeTypeName("UINT8")]
    private byte m_IgnoredAllocs;

    [NativeTypeName("UINT32")]
    private uint m_Algorithm;

    [NativeTypeName("UINT32")]
    private uint m_BlockVectorCount;

    private Pointer<D3D12MA_BlockVector> m_PoolBlockVector;

    private Pointer<D3D12MA_BlockVector>* m_pBlockVectors;

    [NativeTypeName("size_t")]
    private nuint m_ImmovableBlockCount;

    private D3D12MA_DEFRAGMENTATION_STATS m_GlobalStats;

    private D3D12MA_DEFRAGMENTATION_STATS m_PassStats;

    private void* m_AlgorithmState;

    public static D3D12MA_DefragmentationContextPimpl* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, D3D12MA_AllocatorPimpl* hAllocator, [NativeTypeName("const D3D12MA_DEFRAGMENTATION_DESC &")] in D3D12MA_DEFRAGMENTATION_DESC desc, D3D12MA_BlockVector* poolVector)
    {
        D3D12MA_DefragmentationContextPimpl* result = D3D12MA_NEW<D3D12MA_DefragmentationContextPimpl>(allocs);
        result->_ctor(hAllocator, desc, poolVector);
        return result;
    }

    private void _ctor(D3D12MA_AllocatorPimpl* hAllocator, [NativeTypeName("const D3D12MA_DEFRAGMENTATION_DESC &")] in D3D12MA_DEFRAGMENTATION_DESC desc, D3D12MA_BlockVector* poolVector)
    {
        m_MaxPassBytes = (desc.MaxBytesPerPass == 0) ? ulong.MaxValue : desc.MaxBytesPerPass;
        m_MaxPassAllocations = (desc.MaxAllocationsPerPass == 0) ? uint.MaxValue : desc.MaxAllocationsPerPass;
        m_Moves = new D3D12MA_Vector<D3D12MA_DEFRAGMENTATION_MOVE>(hAllocator->GetAllocs());

        m_IgnoredAllocs = 0;

        m_Algorithm = (uint)(desc.Flags & D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_MASK);

        if (poolVector != null)
        {
            m_BlockVectorCount = 1;
            m_PoolBlockVector = new Pointer<D3D12MA_BlockVector>(poolVector);
            m_pBlockVectors = (Pointer<D3D12MA_BlockVector>*)(Unsafe.AsPointer(ref m_PoolBlockVector));

            m_PoolBlockVector.Value->SetIncrementalSort(false);
            m_PoolBlockVector.Value->SortByFreeSize();
        }
        else
        {
            m_BlockVectorCount = hAllocator->GetDefaultPoolCount();
            m_PoolBlockVector = new Pointer<D3D12MA_BlockVector>(null);
            m_pBlockVectors = hAllocator->GetDefaultPools();

            for (uint i = 0; i < m_BlockVectorCount; ++i)
            {
                D3D12MA_BlockVector* vector = m_pBlockVectors[i].Value;

                if (vector != null)
                {
                    vector->SetIncrementalSort(false);
                    vector->SortByFreeSize();
                }
            }
        }

        m_ImmovableBlockCount = 0;
        m_GlobalStats = new D3D12MA_DEFRAGMENTATION_STATS();
        m_PassStats = new D3D12MA_DEFRAGMENTATION_STATS();

        switch (m_Algorithm)
        {
            case 0: // Default algorithm
            {
                m_Algorithm = (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED);
                goto case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED);
            }

            case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED):
            {
                m_AlgorithmState = D3D12MA_NEW_ARRAY<StateBalanced>(hAllocator->GetAllocs(), m_BlockVectorCount);
                break;
            }

            default:
            {
                m_AlgorithmState = null;
                break;
            }
        }
    }

    public void Dispose()
    {
        if (m_PoolBlockVector.Value != null)
        {
            m_PoolBlockVector.Value->SetIncrementalSort(true);
        }
        else
        {
            for (uint i = 0; i < m_BlockVectorCount; ++i)
            {
                D3D12MA_BlockVector* vector = m_pBlockVectors[i].Value;

                if (vector != null)
                {
                    vector->SetIncrementalSort(true);
                }
            }
        }

        if (m_AlgorithmState != null)
        {
            switch (m_Algorithm)
            {
                case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED):
                {
                    D3D12MA_DELETE_ARRAY(m_Moves.GetAllocs(), (StateBalanced*)(m_AlgorithmState));
                    break;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }
            }
        }

        m_Moves.Dispose();
    }

    public readonly void GetStats([NativeTypeName("D3D12MA::DEFRAGMENTATION_STATS &")] out D3D12MA_DEFRAGMENTATION_STATS outStats)
    {
        outStats = m_GlobalStats;
    }

    [return: NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")]
    public readonly ref readonly D3D12MA_ALLOCATION_CALLBACKS GetAllocs()
    {
        return ref m_Moves.GetAllocs();
    }

    public HRESULT DefragmentPassBegin([NativeTypeName("D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO &")] ref D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO moveInfo)
    {
        if (m_PoolBlockVector.Value != null)
        {
            using D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_PoolBlockVector.Value->GetMutex(), m_PoolBlockVector.Value->m_hAllocator->UseMutex());

            if (m_PoolBlockVector.Value->GetBlockCount() > 1)
            {
                _ = ComputeDefragmentation(ref *m_PoolBlockVector.Value, 0);
            }
            else if (m_PoolBlockVector.Value->GetBlockCount() == 1)
            {
                _ = ReallocWithinBlock(ref *m_PoolBlockVector.Value, m_PoolBlockVector.Value->GetBlock(0));
            }

            // Setup index into block vector
            for (nuint i = 0; i < m_Moves.size(); ++i)
            {
                m_Moves[i].pDstTmpAllocation->SetPrivateData(null);
            }
        }
        else
        {
            for (uint i = 0; i < m_BlockVectorCount; ++i)
            {
                if (m_pBlockVectors[i].Value != null)
                {
                    using D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref m_pBlockVectors[i].Value->GetMutex(), m_pBlockVectors[i].Value->m_hAllocator->UseMutex());

                    bool end = false;
                    nuint movesOffset = m_Moves.size();

                    if (m_pBlockVectors[i].Value->GetBlockCount() > 1)
                    {
                        end = ComputeDefragmentation(ref *m_pBlockVectors[i].Value, i);
                    }
                    else if (m_pBlockVectors[i].Value->GetBlockCount() == 1)
                    {
                        end = ReallocWithinBlock(ref *m_pBlockVectors[i].Value, m_pBlockVectors[i].Value->GetBlock(0));
                    }

                    // Setup index into block vector
                    for (; movesOffset < m_Moves.size(); ++movesOffset)
                    {
                        m_Moves[movesOffset].pDstTmpAllocation->SetPrivateData((void*)((nuint)(i)));
                    }

                    if (end)
                    {
                        break;
                    }
                }
            }
        }

        moveInfo.MoveCount = (uint)(m_Moves.size());

        if (moveInfo.MoveCount > 0)
        {
            moveInfo.pMoves = m_Moves.data();
            return S_FALSE;
        }

        moveInfo.pMoves = null;
        return S_OK;
    }

    public HRESULT DefragmentPassEnd([NativeTypeName("D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO &")] ref D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO moveInfo)
    {
        D3D12MA_ASSERT((moveInfo.MoveCount == 0) || (moveInfo.pMoves != null));

        HRESULT result = S_OK;
        using D3D12MA_Vector<FragmentedBlock> immovableBlocks = new D3D12MA_Vector<FragmentedBlock>(m_Moves.GetAllocs());

        for (uint i = 0; i < moveInfo.MoveCount; ++i)
        {
            ref D3D12MA_DEFRAGMENTATION_MOVE move = ref moveInfo.pMoves[i];

            nuint prevCount = 0, currentCount = 0;
            ulong freedBlockSize = 0;

            uint vectorIndex;
            D3D12MA_BlockVector* vector;

            if (m_PoolBlockVector.Value != null)
            {
                vectorIndex = 0;
                vector = m_PoolBlockVector.Value;
            }
            else
            {
                vectorIndex = (uint)((nuint)(move.pDstTmpAllocation->GetPrivateData()));
                vector = m_pBlockVectors[vectorIndex].Value;
                D3D12MA_ASSERT(vector != null);
            }

            switch (move.Operation)
            {
                case D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY:
                {
                    move.pSrcAllocation->SwapBlockAllocation(move.pDstTmpAllocation);

                    // Scope for locks, Free have it's own lock
                    using (D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref vector->GetMutex(), vector->m_hAllocator->UseMutex()))
                    {
                        prevCount = vector->GetBlockCount();
                        freedBlockSize = move.pDstTmpAllocation->GetBlock()->m_pMetadata->GetSize();
                    }
                    _ = move.pDstTmpAllocation->Release();

                    using (D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref vector->GetMutex(), vector->m_hAllocator->UseMutex()))
                    {
                        currentCount = vector->GetBlockCount();
                    }

                    result = S_FALSE;
                    break;
                }

                case D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE:
                {
                    m_PassStats.BytesMoved -= move.pSrcAllocation->GetSize();
                    --m_PassStats.AllocationsMoved;
                    _ = move.pDstTmpAllocation->Release();

                    D3D12MA_NormalBlock* newBlock = move.pSrcAllocation->GetBlock();
                    bool notPresent = true;

                    for (FragmentedBlock* block = immovableBlocks.begin(); block != immovableBlocks.end(); block++)
                    {
                        if (block->block == newBlock)
                        {
                            notPresent = false;
                            break;
                        }
                    }

                    if (notPresent)
                    {
                        immovableBlocks.push_back(new FragmentedBlock {
                            data = vectorIndex,
                            block = newBlock,
                        });
                    }
                    break;
                }

                case D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_DESTROY:
                {
                    m_PassStats.BytesMoved -= move.pSrcAllocation->GetSize();
                    --m_PassStats.AllocationsMoved;

                    // Scope for locks, Free have it's own lock
                    using (D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref vector->GetMutex(), vector->m_hAllocator->UseMutex()))
                    {
                        prevCount = vector->GetBlockCount();
                        freedBlockSize = move.pSrcAllocation->GetBlock()->m_pMetadata->GetSize();
                    }
                    _ = move.pSrcAllocation->Release();

                    using (D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref vector->GetMutex(), vector->m_hAllocator->UseMutex()))
                    {
                        currentCount = vector->GetBlockCount();
                    }
                    freedBlockSize *= prevCount - currentCount;

                    ulong dstBlockSize;

                    using (D3D12MA_MutexLockRead @lock = new D3D12MA_MutexLockRead(ref vector->GetMutex(), vector->m_hAllocator->UseMutex()))
                    {
                        
                        dstBlockSize = move.pDstTmpAllocation->GetBlock()->m_pMetadata->GetSize();
                    }
                    _ = move.pDstTmpAllocation->Release();

                    using (D3D12MA_MutexLockRead  @lock = new D3D12MA_MutexLockRead(ref vector->GetMutex(), vector->m_hAllocator->UseMutex()))
                    {
                        
                        freedBlockSize += dstBlockSize * (currentCount - vector->GetBlockCount());
                        currentCount = vector->GetBlockCount();
                    }

                    result = S_FALSE;
                    break;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }
            }

            if (prevCount > currentCount)
            {
                nuint freedBlocks = prevCount - currentCount;
                m_PassStats.HeapsFreed += (uint)(freedBlocks);
                m_PassStats.BytesFreed += freedBlockSize;
            }
        }

        moveInfo.MoveCount = 0;
        moveInfo.pMoves = null;
        m_Moves.clear();

        // Update stats
        m_GlobalStats.AllocationsMoved += m_PassStats.AllocationsMoved;
        m_GlobalStats.BytesFreed += m_PassStats.BytesFreed;
        m_GlobalStats.BytesMoved += m_PassStats.BytesMoved;
        m_GlobalStats.HeapsFreed += m_PassStats.HeapsFreed;
        m_PassStats = new D3D12MA_DEFRAGMENTATION_STATS();

        // Move blocks with immovable allocations according to algorithm
        if (immovableBlocks.size() > 0)
        {
            // Move to the begining
            for (FragmentedBlock* block = immovableBlocks.begin(); block != immovableBlocks.end(); block++)
            {
                D3D12MA_BlockVector* vector = m_pBlockVectors[block->data].Value;

                using D3D12MA_MutexLockWrite @lock = new D3D12MA_MutexLockWrite(ref vector->GetMutex(), vector->m_hAllocator->UseMutex());

                for (nuint i = m_ImmovableBlockCount; i < vector->GetBlockCount(); ++i)
                {
                    if (vector->GetBlock(i) == block->block)
                    {
                        D3D12MA_SWAP(ref vector->m_Blocks[i].Value, ref vector->m_Blocks[m_ImmovableBlockCount++].Value);
                        break;
                    }
                }
            }
        }
        return result;
    }

    private static MoveAllocationData GetMoveData([NativeTypeName("D3D12MA::AllocHandle")] ulong handle, D3D12MA_BlockMetadata* metadata)
    {
        MoveAllocationData moveData = new MoveAllocationData();

        moveData.move.pSrcAllocation = (D3D12MA_Allocation*)(metadata->GetAllocationPrivateData(handle));
        moveData.size = moveData.move.pSrcAllocation->GetSize();
        moveData.alignment = moveData.move.pSrcAllocation->GetAlignment();
        moveData.flags = D3D12MA_ALLOCATION_FLAG_NONE;

        return moveData;
    }

    private CounterStatus CheckCounters([NativeTypeName("UINT64")] ulong bytes)
    {
        // Ignore allocation if will exceed max size for copy
        if (m_PassStats.BytesMoved + bytes > m_MaxPassBytes)
        {
            if (++m_IgnoredAllocs < MAX_ALLOCS_TO_IGNORE)
            {
                return CounterStatus.Ignore;
            }
            else
            {
                return CounterStatus.End;
            }
        }
        return CounterStatus.Pass;
    }

    private bool IncrementCounters([NativeTypeName("UINT64")] ulong bytes)
    {
        m_PassStats.BytesMoved += bytes;

        // Early return when max found
        if ((++m_PassStats.AllocationsMoved >= m_MaxPassAllocations) || (m_PassStats.BytesMoved >= m_MaxPassBytes))
        {
            D3D12MA_ASSERT((m_PassStats.AllocationsMoved == m_MaxPassAllocations) || (m_PassStats.BytesMoved == m_MaxPassBytes), "Exceeded maximal pass threshold!");
            return true;
        }

        return false;
    }

    private bool ReallocWithinBlock([NativeTypeName("D3D12MA::BlockVector &")] ref D3D12MA_BlockVector vector, D3D12MA_NormalBlock* block)
    {
        D3D12MA_BlockMetadata* metadata = block->m_pMetadata;

        for (ulong handle = metadata->GetAllocationListBegin(); handle != 0; handle = metadata->GetNextAllocation(handle))
        {
            MoveAllocationData moveData = GetMoveData(handle, metadata);

            // Ignore newly created allocations by defragmentation algorithm
            if (moveData.move.pSrcAllocation->GetPrivateData() == Unsafe.AsPointer(ref this))
            {
                continue;
            }

            switch (CheckCounters(moveData.move.pSrcAllocation->GetSize()))
            {
                case CounterStatus.Ignore:
                {
                    continue;
                }

                case CounterStatus.End:
                {
                    return true;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }

                case CounterStatus.Pass:
                {
                    break;
                }
            }

            ulong offset = moveData.move.pSrcAllocation->GetOffset();

            if (offset != 0 && metadata->GetSumFreeSize() >= moveData.size)
            {
                D3D12MA_AllocationRequest request = new D3D12MA_AllocationRequest();

                if (metadata->CreateAllocationRequest(moveData.size, moveData.alignment, false, (uint)(D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET), &request))
                {
                    if (metadata->GetAllocationOffset(request.allocHandle) < offset)
                    {
                        if (SUCCEEDED(vector.CommitAllocationRequest(request, block, moveData.size, moveData.alignment, Unsafe.AsPointer(ref this), &moveData.move.pDstTmpAllocation)))
                        {
                            m_Moves.push_back(moveData.move);

                            if (IncrementCounters(moveData.size))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool AllocInOtherBlock([NativeTypeName("size_t")] nuint start, [NativeTypeName("size_t")] nuint end, [NativeTypeName("D3D12MA::DefragmentationContextPimpl::MoveAllocationData &")] ref MoveAllocationData data, [NativeTypeName("D3D12MA::BlockVector &")] ref D3D12MA_BlockVector vector)
    {
        for (; start < end; ++start)
        {
            D3D12MA_NormalBlock* dstBlock = vector.GetBlock(start);

            if (dstBlock->m_pMetadata->GetSumFreeSize() >= data.size)
            {
                if (SUCCEEDED(vector.AllocateFromBlock(dstBlock, data.size, data.alignment, data.flags, Unsafe.AsPointer(ref this), 0, &((MoveAllocationData*)(Unsafe.AsPointer(ref data)))->move.pDstTmpAllocation)))
                {
                    m_Moves.push_back(data.move);

                    if (IncrementCounters(data.size))
                    {
                        return true;
                    }
                    break;
                }
            }
        }
        return false;
    }

    private bool ComputeDefragmentation([NativeTypeName("D3D12MA::BlockVector &")] ref D3D12MA_BlockVector vector, [NativeTypeName("size_t")] nuint index)
    {
        switch (m_Algorithm)
        {
            case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST):
            {
                return ComputeDefragmentation_Fast(ref vector);
            }

            default:
            {
                D3D12MA_FAIL();
                return false;
            }

            case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED):
            {
                return ComputeDefragmentation_Balanced(ref vector, index, true);
            }

            case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL):
            {
                return ComputeDefragmentation_Full(ref vector);
            }
        }
    }

    private bool ComputeDefragmentation_Fast([NativeTypeName("D3D12MA::BlockVector &")] ref D3D12MA_BlockVector vector)
    {
        // Move only between blocks

        // Go through allocations in last blocks and try to fit them inside first ones
        for (nuint i = vector.GetBlockCount() - 1; i > m_ImmovableBlockCount; --i)
        {
            D3D12MA_BlockMetadata* metadata = vector.GetBlock(i)->m_pMetadata;

            for (ulong handle = metadata->GetAllocationListBegin(); handle != 0; handle = metadata->GetNextAllocation(handle))
            {
                MoveAllocationData moveData = GetMoveData(handle, metadata);

                // Ignore newly created allocations by defragmentation algorithm
                if (moveData.move.pSrcAllocation->GetPrivateData() == Unsafe.AsPointer(ref this))
                {
                    continue;
                }

                switch (CheckCounters(moveData.move.pSrcAllocation->GetSize()))
                {
                    case CounterStatus.Ignore:
                    {
                        continue;
                    }

                    case CounterStatus.End:
                    {
                        return true;
                    }

                    default:
                    {
                        D3D12MA_FAIL();
                        break;
                    }

                    case CounterStatus.Pass:
                    {
                        break;
                    }
                }

                // Check all previous blocks for free space
                if (AllocInOtherBlock(0, i, ref moveData, ref vector))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool ComputeDefragmentation_Balanced([NativeTypeName("D3D12MA::BlockVector &")] ref D3D12MA_BlockVector vector, [NativeTypeName("size_t")] nuint index, bool update)
    {
        // Go over every allocation and try to fit it in previous blocks at lowest offsets,
        // if not possible: realloc within single block to minimize offset (exclude offset == 0),
        // but only if there are noticable gaps between them (some heuristic, ex. average size of allocation in block)
        D3D12MA_ASSERT(m_AlgorithmState != null);

        ref StateBalanced vectorState = ref ((StateBalanced*)(m_AlgorithmState))[index];

        if (update && (vectorState.avgAllocSize == ulong.MaxValue))
        {
            UpdateVectorStatistics(ref vector, ref vectorState);
        }

        nuint startMoveCount = m_Moves.size();
        ulong minimalFreeRegion = vectorState.avgFreeSize / 2;

        for (nuint i = vector.GetBlockCount() - 1; i > m_ImmovableBlockCount; --i)
        {
            D3D12MA_NormalBlock* block = vector.GetBlock(i);
            D3D12MA_BlockMetadata* metadata = block->m_pMetadata;
            ulong prevFreeRegionSize = 0;

            for (ulong handle = metadata->GetAllocationListBegin(); handle != 0; handle = metadata->GetNextAllocation(handle))
            {
                MoveAllocationData moveData = GetMoveData(handle, metadata);

                // Ignore newly created allocations by defragmentation algorithm
                if (moveData.move.pSrcAllocation->GetPrivateData() == Unsafe.AsPointer(ref this))
                {
                    continue;
                }

                switch (CheckCounters(moveData.move.pSrcAllocation->GetSize()))
                {
                    case CounterStatus.Ignore:
                    {
                        continue;
                    }

                    case CounterStatus.End:
                    {
                        return true;
                    }

                    default:
                    {
                        D3D12MA_FAIL();
                        break;
                    }

                    case CounterStatus.Pass:
                    {
                        break;
                    }
                }

                // Check all previous blocks for free space
                nuint prevMoveCount = m_Moves.size();

                if (AllocInOtherBlock(0, i, ref moveData, ref vector))
                {
                    return true;
                }

                ulong nextFreeRegionSize = metadata->GetNextFreeRegionSize(handle);

                // If no room found then realloc within block for lower offset
                ulong offset = moveData.move.pSrcAllocation->GetOffset();

                if (prevMoveCount == m_Moves.size() && offset != 0 && metadata->GetSumFreeSize() >= moveData.size)
                {
                    // Check if realloc will make sense
                    if ((prevFreeRegionSize >= minimalFreeRegion) || (nextFreeRegionSize >= minimalFreeRegion) || (moveData.size <= vectorState.avgFreeSize) || (moveData.size <= vectorState.avgAllocSize))
                    {
                        D3D12MA_AllocationRequest request = new D3D12MA_AllocationRequest();

                        if (metadata->CreateAllocationRequest(moveData.size, moveData.alignment, false, (uint)(D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET), &request))
                        {
                            if (metadata->GetAllocationOffset(request.allocHandle) < offset)
                            {
                                if (SUCCEEDED(vector.CommitAllocationRequest(request, block, moveData.size, moveData.alignment, Unsafe.AsPointer(ref this), &moveData.move.pDstTmpAllocation)))
                                {
                                    m_Moves.push_back(moveData.move);

                                    if (IncrementCounters(moveData.size))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                prevFreeRegionSize = nextFreeRegionSize;
            }
        }

        // No moves perfomed, update statistics to current vector state
        if (startMoveCount == m_Moves.size() && !update)
        {
            vectorState.avgAllocSize = ulong.MaxValue;
            return ComputeDefragmentation_Balanced(ref vector, index, false);
        }
        return false;
    }

    private bool ComputeDefragmentation_Full([NativeTypeName("D3D12MA::BlockVector &")] ref D3D12MA_BlockVector vector)
    {
        // Go over every allocation and try to fit it in previous blocks at lowest offsets,
        // if not possible: realloc within single block to minimize offset (exclude offset == 0)

        for (nuint i = vector.GetBlockCount() - 1; i > m_ImmovableBlockCount; --i)
        {
            D3D12MA_NormalBlock* block = vector.GetBlock(i);
            D3D12MA_BlockMetadata* metadata = block->m_pMetadata;

            for (ulong handle = metadata->GetAllocationListBegin(); handle != 0; handle = metadata->GetNextAllocation(handle))
            {
                MoveAllocationData moveData = GetMoveData(handle, metadata);

                // Ignore newly created allocations by defragmentation algorithm
                if (moveData.move.pSrcAllocation->GetPrivateData() == Unsafe.AsPointer(ref this))
                {
                    continue;
                }

                switch (CheckCounters(moveData.move.pSrcAllocation->GetSize()))
                {
                    case CounterStatus.Ignore:
                    {
                        continue;
                    }

                    case CounterStatus.End:
                    {
                        return true;
                    }

                    default:
                    {
                        D3D12MA_FAIL();
                        break;
                    }

                    case CounterStatus.Pass:
                    {
                        break;
                    }
                }

                // Check all previous blocks for free space
                nuint prevMoveCount = m_Moves.size();

                if (AllocInOtherBlock(0, i, ref moveData, ref vector))
                {
                    return true;
                }

                // If no room found then realloc within block for lower offset
                ulong offset = moveData.move.pSrcAllocation->GetOffset();

                if (prevMoveCount == m_Moves.size() && offset != 0 && metadata->GetSumFreeSize() >= moveData.size)
                {
                    D3D12MA_AllocationRequest request = new D3D12MA_AllocationRequest();

                    if (metadata->CreateAllocationRequest(moveData.size, moveData.alignment, false, (uint)(D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET), &request))
                    {
                        if (metadata->GetAllocationOffset(request.allocHandle) < offset)
                        {
                            if (SUCCEEDED(vector.CommitAllocationRequest(request, block, moveData.size, moveData.alignment, Unsafe.AsPointer(ref this), &moveData.move.pDstTmpAllocation)))
                            {
                                m_Moves.push_back(moveData.move);

                                if (IncrementCounters(moveData.size))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private readonly void UpdateVectorStatistics([NativeTypeName("D3D12MA::BlockVector &")] ref D3D12MA_BlockVector vector, [NativeTypeName("D3D12MA::DefragmentationContextPimpl::StateBalanced &")] ref StateBalanced state)
    {
        nuint allocCount = 0;
        nuint freeCount = 0;

        state.avgFreeSize = 0;
        state.avgAllocSize = 0;

        for (nuint i = 0; i < vector.GetBlockCount(); ++i)
        {
            D3D12MA_BlockMetadata* metadata = vector.GetBlock(i)->m_pMetadata;

            allocCount += metadata->GetAllocationCount();
            freeCount += metadata->GetFreeRegionsCount();

            state.avgFreeSize += metadata->GetSumFreeSize();
            state.avgAllocSize += metadata->GetSize();
        }

        state.avgAllocSize = (state.avgAllocSize - state.avgFreeSize) / allocCount;
        state.avgFreeSize /= freeCount;
    }

    private enum CounterStatus
    {
        Pass,

        Ignore,

        End,
    }

    private partial struct FragmentedBlock
    {
        [NativeTypeName("UINT32")]
        public uint data;

        public D3D12MA_NormalBlock* block;
    }

    internal partial struct StateBalanced
    {
        [NativeTypeName("UINT64")]
        public ulong avgFreeSize;

        [NativeTypeName("UINT64")]
        public ulong avgAllocSize;

        public StateBalanced()
        {
            avgAllocSize = ulong.MaxValue;
        }
    }

    private partial struct MoveAllocationData
    {
        [NativeTypeName("UINT64")]
        public ulong size;

        [NativeTypeName("UINT64")]
        public ulong alignment;

        public D3D12MA_ALLOCATION_FLAGS flags;

        public D3D12MA_DEFRAGMENTATION_MOVE move;
    }
}
