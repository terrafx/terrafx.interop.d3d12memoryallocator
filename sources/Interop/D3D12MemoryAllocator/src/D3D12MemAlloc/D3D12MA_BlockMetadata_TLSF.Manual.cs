// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_BlockMetadata_TLSF
{
    internal static readonly void** VtblInstance = InitVtblInstance();

    private static void** InitVtblInstance()
    {
        void** lpVtbl = (void**)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_BlockMetadata_TLSF), 21 * sizeof(void*)));

        lpVtbl[0] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, void>)(&Dispose);
        lpVtbl[1] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, void>)(&Init);
        lpVtbl[2] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, byte>)(&Validate);
        lpVtbl[3] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, nuint>)(&GetAllocationCount);
        lpVtbl[4] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, nuint>)(&GetFreeRegionsCount);
        lpVtbl[5] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong>)(&GetSumFreeSize);
        lpVtbl[6] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, ulong>)(&GetAllocationOffset);
        lpVtbl[7] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, byte>)(&IsEmpty);
        lpVtbl[8] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)(&GetAllocationInfo);
        lpVtbl[9] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, ulong, byte, uint, D3D12MA_AllocationRequest*, byte>)(&CreateAllocationRequest);
        lpVtbl[10] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, D3D12MA_AllocationRequest*, ulong, void*, void>)(&Alloc);
        lpVtbl[11] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, void>)(&Free);
        lpVtbl[12] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, void>)(&Clear);
        lpVtbl[13] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong>)(&GetAllocationListBegin);
        lpVtbl[14] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, ulong>)(&GetNextAllocation);
        lpVtbl[15] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, ulong>)(&GetNextFreeRegionSize);
        lpVtbl[16] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, void*>)(&GetAllocationPrivateData);
        lpVtbl[17] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, ulong, void*, void>)(&SetAllocationPrivateData);
        lpVtbl[18] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, D3D12MA_Statistics*, void>)(&AddStatistics);
        lpVtbl[19] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, D3D12MA_DetailedStatistics*, void>)(&AddDetailedStatistics);
        lpVtbl[20] = (delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_TLSF*, D3D12MA_JsonWriter*, void>)(&WriteAllocationInfoToJson);

        return lpVtbl;
    }

    [VtblIndex(0)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Dispose(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        D3D12MA_DELETE_ARRAY(*pThis->GetAllocs(), pThis->m_FreeList);
        pThis->m_BlockAllocator.Dispose();
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata*, void>)(D3D12MA_BlockMetadata.VtblInstance[0]))(&pThis->Base);
    }

    [VtblIndex(1)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Init(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("ulong")] ulong size)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata*, ulong, void>)(D3D12MA_BlockMetadata.VtblInstance[1]))(&pThis->Base, size);

        pThis->m_NullBlock = pThis->m_BlockAllocator.Alloc();
        pThis->m_NullBlock->size = size;
        pThis->m_NullBlock->offset = 0;
        pThis->m_NullBlock->prevPhysical = null;
        pThis->m_NullBlock->nextPhysical = null;
        pThis->m_NullBlock->MarkFree();
        pThis->m_NullBlock->NextFree() = null;
        pThis->m_NullBlock->PrevFree() = null;

        byte memoryClass = SizeToMemoryClass(size);
        ushort sli = pThis->SizeToSecondIndex(size, memoryClass);

        pThis->m_ListsCount = ((memoryClass == 0u) ? 0u : ((memoryClass - 1u) * (1u << SECOND_LEVEL_INDEX) + sli)) + 1u;

        if (pThis->IsVirtual())
        {
            pThis->m_ListsCount += 1u << SECOND_LEVEL_INDEX;
        }
        else
        {
            pThis->m_ListsCount += 4;
        }

        pThis->m_MemoryClasses = (byte)(memoryClass + 2);
        _ = memset(pThis->m_InnerIsFreeBitmap, 0, MAX_MEMORY_CLASSES * sizeof(uint));

        pThis->m_FreeList = D3D12MA_NEW_ARRAY<Pointer<Block>>(*pThis->GetAllocs(), pThis->m_ListsCount);
        _ = memset(pThis->m_FreeList, 0, pThis->m_ListsCount * __sizeof<Pointer<Block>>());
    }

    [VtblIndex(2)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static byte Validate(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        D3D12MA_VALIDATE(pThis->GetSumFreeSize() <= pThis->GetSize());

        ulong calculatedSize = pThis->m_NullBlock->size;
        ulong calculatedFreeSize = pThis->m_NullBlock->size;

        nuint allocCount = 0;
        nuint freeCount = 0;

        // Check integrity of free lists
        for (uint list = 0; list < pThis->m_ListsCount; ++list)
        {
            Block* block = pThis->m_FreeList[list].Value;

            if (block != null)
            {
                D3D12MA_VALIDATE(block->IsFree());
                D3D12MA_VALIDATE(block->PrevFree() == null);

                while (block->NextFree() != null)
                {
                    D3D12MA_VALIDATE(block->NextFree()->IsFree());
                    D3D12MA_VALIDATE(block->NextFree()->PrevFree() == block);
                    block = block->NextFree();
                }
            }
        }

        D3D12MA_VALIDATE(pThis->m_NullBlock->nextPhysical == null);

        if (pThis->m_NullBlock->prevPhysical != null)
        {
            D3D12MA_VALIDATE(pThis->m_NullBlock->prevPhysical->nextPhysical == pThis->m_NullBlock);
        }

        // Check all blocks
        ulong nextOffset = pThis->m_NullBlock->offset;

        for (Block* prev = pThis->m_NullBlock->prevPhysical; prev != null; prev = prev->prevPhysical)
        {
            D3D12MA_VALIDATE(prev->offset + prev->size == nextOffset);

            nextOffset = prev->offset;
            calculatedSize += prev->size;

            uint listIndex = pThis->GetListIndex(prev->size);

            if (prev->IsFree())
            {
                ++freeCount;

                // Check if free block belongs to free list
                Block* freeBlock = pThis->m_FreeList[listIndex].Value;

                D3D12MA_VALIDATE(freeBlock != null);

                bool found = false;

                do
                {
                    if (freeBlock == prev)
                    {
                        found = true;
                    }

                    freeBlock = freeBlock->NextFree();
                }
                while (!found && freeBlock != null);

                D3D12MA_VALIDATE(found);
                calculatedFreeSize += prev->size;
            }
            else
            {
                ++allocCount;

                // Check if taken block is not on a free list
                Block* freeBlock = pThis->m_FreeList[listIndex].Value;

                while (freeBlock != null)
                {
                    D3D12MA_VALIDATE(freeBlock != prev);
                    freeBlock = freeBlock->NextFree();
                }
            }

            if (prev->prevPhysical != null)
            {
                D3D12MA_VALIDATE(prev->prevPhysical->nextPhysical == prev);
            }
        }

        D3D12MA_VALIDATE(nextOffset == 0);
        D3D12MA_VALIDATE(calculatedSize == pThis->GetSize());
        D3D12MA_VALIDATE(calculatedFreeSize == pThis->GetSumFreeSize());
        D3D12MA_VALIDATE(allocCount == pThis->m_AllocCount);
        D3D12MA_VALIDATE(freeCount == pThis->m_BlocksFreeCount);

        return 1;
    }

    [VtblIndex(3)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("nuint")]
    internal static nuint GetAllocationCount(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        return pThis->m_AllocCount;
    }

    [VtblIndex(4)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("nuint")]
    internal static nuint GetFreeRegionsCount(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        return pThis->m_BlocksFreeCount + 1;
    }

    [VtblIndex(5)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("ulong")]
    internal static ulong GetSumFreeSize(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        return pThis->m_BlocksFreeSize + pThis->m_NullBlock->size;
    }

    [VtblIndex(6)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("ulong")]
    internal static ulong GetAllocationOffset(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return ((Block*)allocHandle)->offset;
    }

    [VtblIndex(7)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static byte IsEmpty(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        return (byte)((pThis->m_NullBlock->offset == 0) ? 1 : 0);
    }

    [VtblIndex(8)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void GetAllocationInfo(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO &")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
    {
        Block* block = (Block*)allocHandle;
        D3D12MA_ASSERT(!block->IsFree(), "Cannot get allocation info for free block!");

        outInfo->Offset = block->offset;
        outInfo->Size = block->size;
        outInfo->pPrivateData = block->PrivateData();
    }

    [VtblIndex(9)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static byte CreateAllocationRequest(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("ulong")] ulong allocSize, [NativeTypeName("ulong")] ulong allocAlignment, byte upperAddress, [NativeTypeName("uint")] uint strategy, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        D3D12MA_ASSERT(allocSize > 0, "Cannot allocate empty block!");
        D3D12MA_ASSERT(upperAddress == 0, "D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS can be used only with linear algorithm.");
        D3D12MA_ASSERT(pAllocationRequest != null);
        D3D12MA_HEAVY_ASSERT(pThis->Validate());

        allocSize += pThis->GetDebugMargin();

        // Quick check for too small pool
        if (allocSize > pThis->GetSumFreeSize())
        {
            return 0;
        }

        // If no free blocks in pool then check only null block
        if (pThis->m_BlocksFreeCount == 0)
        {
            return (byte)(pThis->CheckBlock(ref *pThis->m_NullBlock, pThis->m_ListsCount, allocSize, allocAlignment, pAllocationRequest) ? 1 : 0);
        }

        // Round up to the next block
        ulong sizeForNextList = allocSize;
        ulong smallSizeStep = SMALL_BUFFER_SIZE / (pThis->IsVirtual() ? (1u << SECOND_LEVEL_INDEX) : 4u);

        if (allocSize > SMALL_BUFFER_SIZE)
        {
            sizeForNextList += (1ul << (D3D12MA_BitScanMSB(allocSize) - SECOND_LEVEL_INDEX));
        }
        else if (allocSize > SMALL_BUFFER_SIZE - smallSizeStep)
        {
            sizeForNextList = SMALL_BUFFER_SIZE + 1;
        }
        else
        {
            sizeForNextList += smallSizeStep;
        }

        uint nextListIndex = 0;
        uint prevListIndex = 0;

        Block* nextListBlock = null;
        Block* prevListBlock = null;

        // Check blocks according to strategies
        if ((strategy & (uint)(D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME)) != 0)
        {
            // Quick check for larger block first
            nextListBlock = pThis->FindFreeBlock(sizeForNextList, ref nextListIndex);

            if (nextListBlock != null && pThis->CheckBlock(ref *nextListBlock, nextListIndex, allocSize, allocAlignment, pAllocationRequest))
            {
                return 1;
            }

            // If not fitted then null block
            if (pThis->CheckBlock(ref *pThis->m_NullBlock, pThis->m_ListsCount, allocSize, allocAlignment, pAllocationRequest))
            {
                return 1;
            }

            // Null block failed, search larger bucket
            while (nextListBlock != null)
            {
                if (pThis->CheckBlock(ref *nextListBlock, nextListIndex, allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
                nextListBlock = nextListBlock->NextFree();
            }

            // Failed again, check best fit bucket
            prevListBlock = pThis->FindFreeBlock(allocSize, ref prevListIndex);

            while (prevListBlock != null)
            {
                if (pThis->CheckBlock(ref *prevListBlock, prevListIndex, allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
                prevListBlock = prevListBlock->NextFree();
            }
        }
        else if ((strategy & (uint)(D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY)) != 0)
        {
            // Check best fit bucket
            prevListBlock = pThis->FindFreeBlock(allocSize, ref prevListIndex);

            while (prevListBlock != null)
            {
                if (pThis->CheckBlock(ref *prevListBlock, prevListIndex, allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
                prevListBlock = prevListBlock->NextFree();
            }

            // If failed check null block
            if (pThis->CheckBlock(ref *pThis->m_NullBlock, pThis->m_ListsCount, allocSize, allocAlignment, pAllocationRequest))
            {
                return 1;
            }

            // Check larger bucket
            nextListBlock = pThis->FindFreeBlock(sizeForNextList, ref nextListIndex);

            while (nextListBlock != null)
            {
                if (pThis->CheckBlock(ref *nextListBlock, nextListIndex, allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
                nextListBlock = nextListBlock->NextFree();
            }
        }
        else if ((strategy & (uint)(D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET)) != 0)
        {
            // Perform search from the start
            using D3D12MA_Vector<Pointer<Block>> blockList = new D3D12MA_Vector<Pointer<Block>>(pThis->m_BlocksFreeCount, *pThis->GetAllocs());

            nuint i = pThis->m_BlocksFreeCount;

            for (Block* block = pThis->m_NullBlock->prevPhysical; block != null; block = block->prevPhysical)
            {
                if (block->IsFree() && block->size >= allocSize)
                {
                    blockList[--i].Value = block;
                }
            }

            for (; i < pThis->m_BlocksFreeCount; ++i)
            {
                ref Block block = ref *blockList[i].Value;

                if (pThis->CheckBlock(ref block, pThis->GetListIndex(block.size), allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
            }

            // If failed check null block
            if (pThis->CheckBlock(ref *pThis->m_NullBlock, pThis->m_ListsCount, allocSize, allocAlignment, pAllocationRequest))
            {
                return 1;
            }

            // Whole range searched, no more memory
            return 0;
        }
        else
        {
            // Check larger bucket
            nextListBlock = pThis->FindFreeBlock(sizeForNextList, ref nextListIndex);

            while (nextListBlock != null)
            {
                if (pThis->CheckBlock(ref *nextListBlock, nextListIndex, allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
                nextListBlock = nextListBlock->NextFree();
            }

            // If failed check null block
            if (pThis->CheckBlock(ref *pThis->m_NullBlock, pThis->m_ListsCount, allocSize, allocAlignment, pAllocationRequest))
            {
                return 1;
            }

            // Check best fit bucket
            prevListBlock = pThis->FindFreeBlock(allocSize, ref prevListIndex);

            while (prevListBlock != null)
            {
                if (pThis->CheckBlock(ref *prevListBlock, prevListIndex, allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
                prevListBlock = prevListBlock->NextFree();
            }
        }

        // Worst case, full search has to be done
        while (++nextListIndex < pThis->m_ListsCount)
        {
            nextListBlock = pThis->m_FreeList[nextListIndex].Value;

            while (nextListBlock != null)
            {
                if (pThis->CheckBlock(ref *nextListBlock, nextListIndex, allocSize, allocAlignment, pAllocationRequest))
                {
                    return 1;
                }
                nextListBlock = nextListBlock->NextFree();
            }
        }

        // No more memory sadly
        return 0;
    }

    [VtblIndex(10)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Alloc(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("const D3D12MA::AllocationRequest &")] D3D12MA_AllocationRequest* request, [NativeTypeName("ulong")] ulong allocSize, void* PrivateData)
    {
        // Get block and pop it from the free list
        Block* currentBlock = (Block*)(request->allocHandle);
        ulong offset = request->algorithmData;

        D3D12MA_ASSERT(currentBlock != null);
        D3D12MA_ASSERT(currentBlock->offset <= offset);

        if (currentBlock != pThis->m_NullBlock)
        {
            pThis->RemoveFreeBlock(currentBlock);
        }

        // Append missing alignment to prev block or create new one
        ulong misssingAlignment = offset - currentBlock->offset;

        if (misssingAlignment != 0)
        {
            Block* prevBlock = currentBlock->prevPhysical;
            D3D12MA_ASSERT(prevBlock != null, "There should be no missing alignment at offset 0!");

            if (prevBlock->IsFree() && prevBlock->size != pThis->GetDebugMargin())
            {
                uint oldList = pThis->GetListIndex(prevBlock->size);
                prevBlock->size += misssingAlignment;

                // Check if new size crosses list bucket
                if (oldList != pThis->GetListIndex(prevBlock->size))
                {
                    prevBlock->size -= misssingAlignment;
                    pThis->RemoveFreeBlock(prevBlock);

                    prevBlock->size += misssingAlignment;
                    pThis->InsertFreeBlock(prevBlock);
                }
                else
                {
                    pThis->m_BlocksFreeSize += misssingAlignment;
                }
            }
            else
            {
                Block* newBlock = pThis->m_BlockAllocator.Alloc();

                currentBlock->prevPhysical = newBlock;
                prevBlock->nextPhysical = newBlock;
                newBlock->prevPhysical = prevBlock;
                newBlock->nextPhysical = currentBlock;
                newBlock->size = misssingAlignment;
                newBlock->offset = currentBlock->offset;
                newBlock->MarkTaken();

                pThis->InsertFreeBlock(newBlock);
            }

            currentBlock->size -= misssingAlignment;
            currentBlock->offset += misssingAlignment;
        }

        ulong size = request->size + pThis->GetDebugMargin();

        if (currentBlock->size == size)
        {
            if (currentBlock == pThis->m_NullBlock)
            {
                // Setup new null block
                pThis->m_NullBlock = pThis->m_BlockAllocator.Alloc();
                pThis->m_NullBlock->size = 0;
                pThis->m_NullBlock->offset = currentBlock->offset + size;
                pThis->m_NullBlock->prevPhysical = currentBlock;
                pThis->m_NullBlock->nextPhysical = null;
                pThis->m_NullBlock->MarkFree();
                pThis->m_NullBlock->PrevFree() = null;
                pThis->m_NullBlock->NextFree() = null;
                currentBlock->nextPhysical = pThis->m_NullBlock;
                currentBlock->MarkTaken();
            }
        }
        else
        {
            D3D12MA_ASSERT(currentBlock->size > size, "Proper block already found, shouldn't find smaller one!");

            // Create new free block
            Block* newBlock = pThis->m_BlockAllocator.Alloc();

            newBlock->size = currentBlock->size - size;
            newBlock->offset = currentBlock->offset + size;
            newBlock->prevPhysical = currentBlock;
            newBlock->nextPhysical = currentBlock->nextPhysical;
            currentBlock->nextPhysical = newBlock;
            currentBlock->size = size;

            if (currentBlock == pThis->m_NullBlock)
            {
                pThis->m_NullBlock = newBlock;
                pThis->m_NullBlock->MarkFree();
                pThis->m_NullBlock->NextFree() = null;
                pThis->m_NullBlock->PrevFree() = null;
                currentBlock->MarkTaken();
            }
            else
            {
                newBlock->nextPhysical->prevPhysical = newBlock;
                newBlock->MarkTaken();
                pThis->InsertFreeBlock(newBlock);
            }
        }
        currentBlock->PrivateData() = PrivateData;

        if (pThis->GetDebugMargin() > 0)
        {
            currentBlock->size -= pThis->GetDebugMargin();
            Block* newBlock = pThis->m_BlockAllocator.Alloc();

            newBlock->size = pThis->GetDebugMargin();
            newBlock->offset = currentBlock->offset + currentBlock->size;
            newBlock->prevPhysical = currentBlock;
            newBlock->nextPhysical = currentBlock->nextPhysical;
            newBlock->MarkTaken();
            currentBlock->nextPhysical->prevPhysical = newBlock;
            currentBlock->nextPhysical = newBlock;

            pThis->InsertFreeBlock(newBlock);
        }

        ++pThis->m_AllocCount;
    }

    [VtblIndex(11)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Free(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        Block* block = (Block*)allocHandle;
        Block* next = block->nextPhysical;
        D3D12MA_ASSERT(!block->IsFree(), "Block is already free!");

        --pThis->m_AllocCount;

        if (pThis->GetDebugMargin() > 0)
        {
            pThis->RemoveFreeBlock(next);
            pThis->MergeBlock(next, block);

            block = next;
            next = next->nextPhysical;
        }

        // Try merging
        Block* prev = block->prevPhysical;

        if ((prev != null) && prev->IsFree() && (prev->size != pThis->GetDebugMargin()))
        {
            pThis->RemoveFreeBlock(prev);
            pThis->MergeBlock(block, prev);
        }

        if (!next->IsFree())
        {
            pThis->InsertFreeBlock(block);
        }
        else if (next == pThis->m_NullBlock)
        {
            pThis->MergeBlock(pThis->m_NullBlock, block);
        }
        else
        {
            pThis->RemoveFreeBlock(next);
            pThis->MergeBlock(next, block);
            pThis->InsertFreeBlock(next);
        }
    }

    [VtblIndex(12)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void Clear(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        pThis->m_AllocCount = 0;
        pThis->m_BlocksFreeCount = 0;
        pThis->m_BlocksFreeSize = 0;
        pThis->m_IsFreeBitmap = 0;
        pThis->m_NullBlock->offset = 0;
        pThis->m_NullBlock->size = pThis->GetSize();

        Block* block = pThis->m_NullBlock->prevPhysical;
        pThis->m_NullBlock->prevPhysical = null;

        while (block != null)
        {
            Block* prev = block->prevPhysical;
            pThis->m_BlockAllocator.Free(block);
            block = prev;
        }

        _ = memset(pThis->m_FreeList, 0, pThis->m_ListsCount * __sizeof<Pointer<Block>>());
        _ = memset(pThis->m_InnerIsFreeBitmap, 0, (uint)(pThis->m_MemoryClasses) * sizeof(uint));
    }

    [VtblIndex(13)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    internal static ulong GetAllocationListBegin(D3D12MA_BlockMetadata_TLSF* pThis)
    {
        if (pThis->m_AllocCount == 0)
        {
            return (ulong)(0);
        }

        for (Block* block = pThis->m_NullBlock->prevPhysical; block != null; block = block->prevPhysical)
        {
            if (!block->IsFree())
            {
                return (ulong)(block);
            }
        }

        D3D12MA_FAIL("If m_AllocCount > 0 then should find any allocation!");
        return (ulong)(0);
    }

    [VtblIndex(14)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    internal static ulong GetNextAllocation(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong prevAlloc)
    {
        Block* startBlock = (Block*)prevAlloc;
        D3D12MA_ASSERT(!startBlock->IsFree(), "Incorrect block!");

        for (Block* block = startBlock->prevPhysical; block != null; block = block->prevPhysical)
        {
            if (!block->IsFree())
            {
                return (ulong)(block);
            }
        }
        return (ulong)(0);
    }

    [VtblIndex(15)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    [return: NativeTypeName("ulong")]
    internal static ulong GetNextFreeRegionSize(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong alloc)
    {
        Block* block = (Block*)alloc;
        D3D12MA_ASSERT(!block->IsFree(), "Incorrect block!");

        if (block->prevPhysical != null)
        {
            return block->prevPhysical->IsFree() ? block->prevPhysical->size : 0;
        }
        return 0;
    }

    [VtblIndex(16)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void* GetAllocationPrivateData(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        Block* block = (Block*)allocHandle;
        D3D12MA_ASSERT(!block->IsFree(), "Cannot get user data for free block!");
        return block->PrivateData();
    }

    [VtblIndex(17)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void SetAllocationPrivateData(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, void* privateData)
    {
        Block* block = (Block*)allocHandle;
        D3D12MA_ASSERT(!block->IsFree(), "Trying to set user data for not allocated block!");
        block->PrivateData() = privateData;
    }

    [VtblIndex(18)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void AddStatistics(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::Statistics &")] D3D12MA_Statistics* inoutStats)
    {
        inoutStats->BlockCount++;
        inoutStats->AllocationCount += (uint)(pThis->m_AllocCount);
        inoutStats->BlockBytes += pThis->GetSize();
        inoutStats->AllocationBytes += pThis->GetSize() - pThis->GetSumFreeSize();
    }

    [VtblIndex(19)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void AddDetailedStatistics(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::DetailedStatistics &")] D3D12MA_DetailedStatistics* inoutStats)
    {
        inoutStats->Stats.BlockCount++;
        inoutStats->Stats.BlockBytes += pThis->GetSize();

        for (Block* block = pThis->m_NullBlock->prevPhysical; block != null; block = block->prevPhysical)
        {
            if (block->IsFree())
            {
                D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, block->size);
            }
            else
            {
                D3D12MA_AddDetailedStatisticsAllocation(ref *inoutStats, block->size);
            }
        }

        if (pThis->m_NullBlock->size > 0)
        {
            D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, pThis->m_NullBlock->size);
        }
    }

    [VtblIndex(20)]
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static void WriteAllocationInfoToJson(D3D12MA_BlockMetadata_TLSF* pThis, [NativeTypeName("D3D12MA::JsonWriter &")] D3D12MA_JsonWriter* json)
    {
        nuint blockCount = pThis->m_AllocCount + pThis->m_BlocksFreeCount;
        using D3D12MA_Vector<Pointer<Block>> blockList = new D3D12MA_Vector<Pointer<Block>>(blockCount, *pThis->GetAllocs());

        nuint i = blockCount;

        if (pThis->m_NullBlock->size > 0)
        {
            ++blockCount;
            blockList.push_back(new Pointer<Block>(pThis->m_NullBlock));
        }

        for (Block* block = pThis->m_NullBlock->prevPhysical; block != null; block = block->prevPhysical)
        {
            blockList[--i].Value = block;
        }
        D3D12MA_ASSERT(i == 0);

        pThis->PrintDetailedMap_Begin(ref *json, pThis->GetSumFreeSize(), pThis->GetAllocationCount(), pThis->m_BlocksFreeCount + ((pThis->m_NullBlock->size != 0u) ? 1u : 0u));

        for (; i < blockCount; ++i)
        {
            Block* block = blockList[i].Value;

            if (block->IsFree())
            {
                PrintDetailedMap_UnusedRange(ref *json, block->offset, block->size);
            }
            else
            {
                pThis->PrintDetailedMap_Allocation(ref *json, block->offset, block->size, block->PrivateData());
            }
        }

        PrintDetailedMap_End(ref *json);
    }
}
