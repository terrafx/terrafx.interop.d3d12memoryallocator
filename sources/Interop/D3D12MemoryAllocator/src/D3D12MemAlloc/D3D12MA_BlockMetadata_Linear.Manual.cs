// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12MA_BlockMetadata_Linear.ALLOC_REQUEST_TYPE;
using static TerraFX.Interop.DirectX.D3D12MA_BlockMetadata_Linear.SECOND_VECTOR_MODE;
using static TerraFX.Interop.DirectX.D3D12MA_SuballocationType;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_BlockMetadata_Linear
{
    internal static readonly void** VtblInstance = InitVtblInstance();

    private static void** InitVtblInstance()
    {
        void** lpVtbl = (void**)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_BlockMetadata_Linear), 21 * sizeof(void*)));

        lpVtbl[0] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, void>)(&Dispose);
        lpVtbl[1] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, void>)(&Init);
        lpVtbl[2] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, byte>)(&Validate);
        lpVtbl[3] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, nuint>)(&GetAllocationCount);
        lpVtbl[4] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, nuint>)(&GetFreeRegionsCount);
        lpVtbl[5] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong>)(&GetSumFreeSize);
        lpVtbl[6] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, ulong>)(&GetAllocationOffset);
        lpVtbl[7] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, byte>)(&IsEmpty);
        lpVtbl[8] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)(&GetAllocationInfo);
        lpVtbl[9] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, ulong, byte, uint, D3D12MA_AllocationRequest*, byte>)(&CreateAllocationRequest);
        lpVtbl[10] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, D3D12MA_AllocationRequest*, ulong, void*, void>)(&Alloc);
        lpVtbl[11] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, void>)(&Free);
        lpVtbl[12] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, void>)(&Clear);
        lpVtbl[13] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong>)(&GetAllocationListBegin);
        lpVtbl[14] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, ulong>)(&GetNextAllocation);
        lpVtbl[15] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, ulong>)(&GetNextFreeRegionSize);
        lpVtbl[16] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, void*>)(&GetAllocationPrivateData);
        lpVtbl[17] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, ulong, void*, void>)(&SetAllocationPrivateData);
        lpVtbl[18] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, D3D12MA_Statistics*, void>)(&AddStatistics);
        lpVtbl[19] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, D3D12MA_DetailedStatistics*, void>)(&AddDetailedStatistics);
        lpVtbl[20] = (delegate* unmanaged<D3D12MA_BlockMetadata_Linear*, D3D12MA_JsonWriter*, void>)(&WriteAllocationInfoToJson);

        return lpVtbl;
    }

    [VtblIndex(0)]
    [UnmanagedCallersOnly]
    internal static void Dispose(D3D12MA_BlockMetadata_Linear* pThis)
    {
        pThis->m_Suballocations0.Dispose();
        pThis->m_Suballocations1.Dispose();
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, void>)(D3D12MA_BlockMetadata.VtblInstance[0]))(&pThis->Base);
    }

    [VtblIndex(1)]
    [UnmanagedCallersOnly]
    internal static void Init(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("UINT64")] ulong size)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, void>)(D3D12MA_BlockMetadata.VtblInstance[1]))(&pThis->Base, size);
        pThis->m_SumFreeSize = size;
    }

    [VtblIndex(2)]
    [UnmanagedCallersOnly]
    internal static byte Validate(D3D12MA_BlockMetadata_Linear* pThis)
    {
        D3D12MA_VALIDATE(pThis->GetSumFreeSize() <= pThis->GetSize());

        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref pThis->AccessSuballocations1st();
        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref pThis->AccessSuballocations2nd();

        D3D12MA_VALIDATE(suballocations2nd.empty() == (pThis->m_2ndVectorMode == SECOND_VECTOR_EMPTY));
        D3D12MA_VALIDATE(!suballocations1st.empty() || suballocations2nd.empty() || (pThis->m_2ndVectorMode != SECOND_VECTOR_RING_BUFFER));

        if (!suballocations1st.empty())
        {
            // Null item at the beginning should be accounted into m_1stNullItemsBeginCount.
            D3D12MA_VALIDATE(suballocations1st[pThis->m_1stNullItemsBeginCount].type != D3D12MA_SUBALLOCATION_TYPE_FREE);
            // Null item at the end should be just pop_back().
            D3D12MA_VALIDATE(suballocations1st.back().type != D3D12MA_SUBALLOCATION_TYPE_FREE);
        }
        if (!suballocations2nd.empty())
        {
            // Null item at the end should be just pop_back().
            D3D12MA_VALIDATE(suballocations2nd.back().type != D3D12MA_SUBALLOCATION_TYPE_FREE);
        }

        D3D12MA_VALIDATE((pThis->m_1stNullItemsBeginCount + pThis->m_1stNullItemsMiddleCount) <= suballocations1st.size());
        D3D12MA_VALIDATE(pThis->m_2ndNullItemsCount <= suballocations2nd.size());

        ulong sumUsedSize = 0;
        nuint suballoc1stCount = suballocations1st.size();
        ulong offset = 0;

        if (pThis->m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER)
        {
            nuint suballoc2ndCount = suballocations2nd.size();
            nuint nullItem2ndCount = 0;

            for (nuint i = 0; i < suballoc2ndCount; ++i)
            {
                ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[i];
                bool currFree = (suballoc.type == D3D12MA_SUBALLOCATION_TYPE_FREE);

                D3D12MA_Allocation* alloc = (D3D12MA_Allocation*)(suballoc.privateData);

                if (!pThis->IsVirtual())
                {
                    D3D12MA_VALIDATE(currFree == (alloc == null));
                }
                D3D12MA_VALIDATE(suballoc.offset >= offset);

                if (!currFree)
                {
                    if (!pThis->IsVirtual())
                    {
                        D3D12MA_VALIDATE(pThis->GetAllocationOffset(alloc->GetAllocHandle()) == suballoc.offset);
                        D3D12MA_VALIDATE(alloc->GetSize() == suballoc.size);
                    }
                    sumUsedSize += suballoc.size;
                }
                else
                {
                    ++nullItem2ndCount;
                }

                offset = suballoc.offset + suballoc.size + pThis->GetDebugMargin();
            }

            D3D12MA_VALIDATE(nullItem2ndCount == pThis->m_2ndNullItemsCount);
        }

        for (nuint i = 0; i < pThis->m_1stNullItemsBeginCount; ++i)
        {
            ref readonly D3D12MA_Suballocation suballoc = ref suballocations1st[i];
            D3D12MA_VALIDATE((suballoc.type == D3D12MA_SUBALLOCATION_TYPE_FREE) && (suballoc.privateData == null));
        }

        nuint nullItem1stCount = pThis->m_1stNullItemsBeginCount;

        for (nuint i = pThis->m_1stNullItemsBeginCount; i < suballoc1stCount; ++i)
        {
            ref readonly D3D12MA_Suballocation suballoc = ref suballocations1st[i];
            bool currFree = (suballoc.type == D3D12MA_SUBALLOCATION_TYPE_FREE);

            D3D12MA_Allocation* alloc = (D3D12MA_Allocation*)(suballoc.privateData);

            if (!pThis->IsVirtual())
            {
                D3D12MA_VALIDATE(currFree == (alloc == null));
            }
            D3D12MA_VALIDATE(suballoc.offset >= offset);
            D3D12MA_VALIDATE((i >= pThis->m_1stNullItemsBeginCount) || currFree);

            if (!currFree)
            {
                if (!pThis->IsVirtual())
                {
                    D3D12MA_VALIDATE(pThis->GetAllocationOffset(alloc->GetAllocHandle()) == suballoc.offset);
                    D3D12MA_VALIDATE(alloc->GetSize() == suballoc.size);
                }
                sumUsedSize += suballoc.size;
            }
            else
            {
                ++nullItem1stCount;
            }

            offset = suballoc.offset + suballoc.size + pThis->GetDebugMargin();
        }
        D3D12MA_VALIDATE(nullItem1stCount == (pThis->m_1stNullItemsBeginCount + pThis->m_1stNullItemsMiddleCount));

        if (pThis->m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK)
        {
            nuint suballoc2ndCount = suballocations2nd.size();
            nuint  nullItem2ndCount = 0;

            for (nuint i = suballoc2ndCount; i-- != 0;)
            {
                ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[i];
                bool currFree = (suballoc.type == D3D12MA_SUBALLOCATION_TYPE_FREE);

                D3D12MA_Allocation* alloc = (D3D12MA_Allocation*)(suballoc.privateData);

                if (!pThis->IsVirtual())
                {
                    D3D12MA_VALIDATE(currFree == (alloc == null));
                }
                D3D12MA_VALIDATE(suballoc.offset >= offset);

                if (!currFree)
                {
                    if (!pThis->IsVirtual())
                    {
                        D3D12MA_VALIDATE(pThis->GetAllocationOffset(alloc->GetAllocHandle()) == suballoc.offset);
                        D3D12MA_VALIDATE(alloc->GetSize() == suballoc.size);
                    }
                    sumUsedSize += suballoc.size;
                }
                else
                {
                    ++nullItem2ndCount;
                }

                offset = suballoc.offset + suballoc.size + pThis->GetDebugMargin();
            }

            D3D12MA_VALIDATE(nullItem2ndCount == pThis->m_2ndNullItemsCount);
        }

        D3D12MA_VALIDATE(offset <= pThis->GetSize());
        D3D12MA_VALIDATE(pThis->m_SumFreeSize == (pThis->GetSize() - sumUsedSize));

        return 1;
    }

    [VtblIndex(3)]
    [UnmanagedCallersOnly]
    [return: NativeTypeName("size_t")]
    internal static nuint GetAllocationCount(D3D12MA_BlockMetadata_Linear* pThis)
    {
        return pThis->AccessSuballocations1st().size() - pThis->m_1stNullItemsBeginCount - pThis->m_1stNullItemsMiddleCount + pThis->AccessSuballocations2nd().size() - pThis->m_2ndNullItemsCount;
    }

    [VtblIndex(4)]
    [UnmanagedCallersOnly]
    [return: NativeTypeName("size_t")]
    internal static nuint GetFreeRegionsCount(D3D12MA_BlockMetadata_Linear* pThis)
    {
        // Function only used for defragmentation, which is disabled for this algorithm
        D3D12MA_FAIL();
        return nuint.MaxValue;
    }

    [VtblIndex(5)]
    [UnmanagedCallersOnly]
    [return: NativeTypeName("UINT64")]
    internal static ulong GetSumFreeSize(D3D12MA_BlockMetadata_Linear* pThis)
    {
        return pThis->m_SumFreeSize;
    }

    [VtblIndex(6)]
    [UnmanagedCallersOnly]
    [return: NativeTypeName("UINT64")]
    internal static ulong GetAllocationOffset(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return (ulong)(allocHandle - 1);
    }

    [VtblIndex(7)]
    [UnmanagedCallersOnly]
    internal static byte IsEmpty(D3D12MA_BlockMetadata_Linear* pThis)
    {
        return (byte)((pThis->GetAllocationCount() == 0) ? 1 : 0);
    }

    [VtblIndex(8)]
    [UnmanagedCallersOnly]
    internal static void GetAllocationInfo(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO &")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
    {
        ref readonly D3D12MA_Suballocation suballoc = ref pThis->FindSuballocation((ulong)(allocHandle - 1));

        outInfo->Offset = suballoc.offset;
        outInfo->Size = suballoc.size;
        outInfo->pPrivateData = suballoc.privateData;
    }

    [VtblIndex(9)]
    [UnmanagedCallersOnly]
    internal static byte CreateAllocationRequest(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, byte upperAddress, [NativeTypeName("UINT32")] uint strategy, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        D3D12MA_ASSERT(allocSize > 0, "Cannot allocate empty block!");
        D3D12MA_ASSERT(pAllocationRequest != null);
        D3D12MA_HEAVY_ASSERT(pThis->Validate());

        pAllocationRequest->size = allocSize;
        return (byte)(((upperAddress != 0) ? pThis->CreateAllocationRequest_UpperAddress(allocSize, allocAlignment, pAllocationRequest) : pThis->CreateAllocationRequest_LowerAddress(allocSize, allocAlignment, pAllocationRequest)) ? 1 : 0);
    }

    [VtblIndex(10)]
    [UnmanagedCallersOnly]
    internal static void Alloc(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("const D3D12MA::AllocationRequest &")] D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* PrivateData)
    {
        ulong offset = (ulong)(request->allocHandle - 1);

        D3D12MA_Suballocation newSuballoc = new D3D12MA_Suballocation {
            offset = offset,
            size = request->size,
            privateData = PrivateData,
            type = D3D12MA_SUBALLOCATION_TYPE_ALLOCATION
        };

        switch (request->algorithmData)
        {
            case (uint)(ALLOC_REQUEST_UPPER_ADDRESS):
            {
                D3D12MA_ASSERT(pThis->m_2ndVectorMode != SECOND_VECTOR_RING_BUFFER, "CRITICAL ERROR: Trying to use linear allocator as double stack while it was already used as ring buffer.");

                ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref pThis->AccessSuballocations2nd();
                suballocations2nd.push_back(newSuballoc);

                pThis->m_2ndVectorMode = SECOND_VECTOR_DOUBLE_STACK;
                break;
            }

            case (uint)(ALLOC_REQUEST_END_OF_1ST):
            {
                ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref pThis->AccessSuballocations1st();

                D3D12MA_ASSERT(suballocations1st.empty() || (offset >= (suballocations1st.back().offset + suballocations1st.back().size)));
                // Check if it fits before the end of the block.
                D3D12MA_ASSERT((offset + request->size) <= pThis->GetSize());

                suballocations1st.push_back(newSuballoc);
                break;
            }

            case (uint)(ALLOC_REQUEST_END_OF_2ND):
            {
                ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref pThis->AccessSuballocations1st();

                // New allocation at the end of 2-part ring buffer, so before first allocation from 1st vector.
                D3D12MA_ASSERT(!suballocations1st.empty() && ((offset + request->size) <= suballocations1st[pThis->m_1stNullItemsBeginCount].offset));

                ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref pThis->AccessSuballocations2nd();

                switch (pThis->m_2ndVectorMode)
                {
                    case SECOND_VECTOR_EMPTY:
                    {
                        // First allocation from second part ring buffer.
                        D3D12MA_ASSERT(suballocations2nd.empty());
                        pThis->m_2ndVectorMode = SECOND_VECTOR_RING_BUFFER;
                        break;
                    }

                    case SECOND_VECTOR_RING_BUFFER:
                    {
                        // 2-part ring buffer is already started.
                        D3D12MA_ASSERT(!suballocations2nd.empty());
                        break;
                    }

                    case SECOND_VECTOR_DOUBLE_STACK:
                    {
                        D3D12MA_FAIL("CRITICAL ERROR: Trying to use linear allocator as ring buffer while it was already used as double stack.");
                        break;
                    }

                    default:
                    {
                        D3D12MA_FAIL();
                        break;
                    }
                }

                suballocations2nd.push_back(newSuballoc);
                break;
            }

            default:
            {
                D3D12MA_FAIL("CRITICAL INTERNAL ERROR.");
                break;
            }
        }

        pThis->m_SumFreeSize -= newSuballoc.size;
    }

    [VtblIndex(11)]
    [UnmanagedCallersOnly]
    internal static void Free(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref pThis->AccessSuballocations1st();
        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref pThis->AccessSuballocations2nd();

        ulong offset = (ulong)(allocHandle - 1);

        if (!suballocations1st.empty())
        {
            // First allocation: Mark it as next empty at the beginning.
            ref D3D12MA_Suballocation firstSuballoc = ref suballocations1st[pThis->m_1stNullItemsBeginCount];

            if (firstSuballoc.offset == offset)
            {
                firstSuballoc.type = D3D12MA_SUBALLOCATION_TYPE_FREE;
                firstSuballoc.privateData = null;
                pThis->m_SumFreeSize += firstSuballoc.size;
                ++pThis->m_1stNullItemsBeginCount;
                pThis->CleanupAfterFree();
                return;
            }
        }

        // Last allocation in 2-part ring buffer or top of upper stack (same logic).
        if ((pThis->m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER) || (pThis->m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK))
        {
            ref D3D12MA_Suballocation lastSuballoc = ref suballocations2nd.back();

            if (lastSuballoc.offset == offset)
            {
                pThis->m_SumFreeSize += lastSuballoc.size;
                suballocations2nd.pop_back();
                pThis->CleanupAfterFree();
                return;
            }
        }
        // Last allocation in 1st vector.
        else if (pThis->m_2ndVectorMode == SECOND_VECTOR_EMPTY)
        {
            ref D3D12MA_Suballocation lastSuballoc = ref suballocations1st.back();

            if (lastSuballoc.offset == offset)
            {
                pThis->m_SumFreeSize += lastSuballoc.size;
                suballocations1st.pop_back();
                pThis->CleanupAfterFree();
                return;
            }
        }

        D3D12MA_Suballocation refSuballoc = new D3D12MA_Suballocation {
            offset = offset,
        };
        // Rest of members stays uninitialized intentionally for better performance.

        // Item from the middle of 1st vector.
        {
            D3D12MA_Suballocation* it = D3D12MA_BinaryFindSorted(suballocations1st.begin() + pThis->m_1stNullItemsBeginCount, suballocations1st.end(), refSuballoc, new D3D12MA_SuballocationOffsetLess());
            if (it != suballocations1st.end())
            {
                it->type = D3D12MA_SUBALLOCATION_TYPE_FREE;
                it->privateData = null;
                ++pThis->m_1stNullItemsMiddleCount;
                pThis->m_SumFreeSize += it->size;
                pThis->CleanupAfterFree();
                return;
            }
        }

        if (pThis->m_2ndVectorMode != SECOND_VECTOR_EMPTY)
        {
            // Item from the middle of 2nd vector.
            D3D12MA_Suballocation* it = (pThis->m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER) ? D3D12MA_BinaryFindSorted(suballocations2nd.begin(), suballocations2nd.end(), refSuballoc, new D3D12MA_SuballocationOffsetLess()) : D3D12MA_BinaryFindSorted(suballocations2nd.begin(), suballocations2nd.end(), refSuballoc, new D3D12MA_SuballocationOffsetGreater());
            if (it != suballocations2nd.end())
            {
                it->type = D3D12MA_SUBALLOCATION_TYPE_FREE;
                it->privateData = null;
                ++pThis->m_2ndNullItemsCount;
                pThis->m_SumFreeSize += it->size;
                pThis->CleanupAfterFree();
                return;
            }
        }

        D3D12MA_FAIL("Allocation to free not found in linear allocator!");
    }

    [VtblIndex(12)]
    [UnmanagedCallersOnly]
    internal static void Clear(D3D12MA_BlockMetadata_Linear* pThis)
    {
        pThis->m_SumFreeSize = pThis->GetSize();
        pThis->m_Suballocations0.clear();
        pThis->m_Suballocations1.clear();
        // Leaving m_1stVectorIndex unchanged - it doesn't matter.
        pThis->m_2ndVectorMode = SECOND_VECTOR_EMPTY;
        pThis->m_1stNullItemsBeginCount = 0;
        pThis->m_1stNullItemsMiddleCount = 0;
        pThis->m_2ndNullItemsCount = 0;
    }

    [VtblIndex(13)]
    [UnmanagedCallersOnly]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    internal static ulong GetAllocationListBegin(D3D12MA_BlockMetadata_Linear* pThis)
    {
        // Function only used for defragmentation, which is disabled for this algorithm
        D3D12MA_FAIL();
        return (ulong)(0);
    }

    [VtblIndex(14)]
    [UnmanagedCallersOnly]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    internal static ulong GetNextAllocation(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong prevAlloc)
    {
        // Function only used for defragmentation, which is disabled for this algorithm
        D3D12MA_FAIL();
        return (ulong)(0);
    }

    [VtblIndex(15)]
    [UnmanagedCallersOnly]
    [return: NativeTypeName("UINT64")]
    internal static ulong GetNextFreeRegionSize(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong alloc)
    {
        // Function only used for defragmentation, which is disabled for this algorithm
        D3D12MA_FAIL();
        return 0;
    }

    [VtblIndex(16)]
    [UnmanagedCallersOnly]
    internal static void* GetAllocationPrivateData(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return pThis->FindSuballocation((ulong)(allocHandle - 1)).privateData;
    }

    [VtblIndex(17)]
    [UnmanagedCallersOnly]
    internal static void SetAllocationPrivateData(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, void* privateData)
    {
        ref D3D12MA_Suballocation suballoc = ref pThis->FindSuballocation((ulong)(allocHandle - 1));
        suballoc.privateData = privateData;
    }

    [VtblIndex(18)]
    [UnmanagedCallersOnly]
    internal static void AddStatistics(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::Statistics &")] D3D12MA_Statistics* inoutStats)
    {
        inoutStats->BlockCount++;
        inoutStats->AllocationCount += (uint)(pThis->GetAllocationCount());
        inoutStats->BlockBytes += pThis->GetSize();
        inoutStats->AllocationBytes += pThis->GetSize() - pThis->m_SumFreeSize;
    }

    [VtblIndex(19)]
    [UnmanagedCallersOnly]
    internal static void AddDetailedStatistics(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::DetailedStatistics &")] D3D12MA_DetailedStatistics* inoutStats)
    {
        inoutStats->Stats.BlockCount++;
        inoutStats->Stats.BlockBytes += pThis->GetSize();

        ulong size = pThis->GetSize();

        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref pThis->AccessSuballocations1st();
        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref pThis->AccessSuballocations2nd();

        nuint suballoc1stCount = suballocations1st.size();
        nuint suballoc2ndCount = suballocations2nd.size();

        ulong lastOffset = 0;

        if (pThis->m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER)
        {
            ulong freeSpace2ndTo1stEnd = suballocations1st[pThis->m_1stNullItemsBeginCount].offset;
            nuint nextAlloc2ndIndex = 0;

            while (lastOffset < freeSpace2ndTo1stEnd)
            {
                // Find next non-null allocation or move nextAllocIndex to the end.
                while ((nextAlloc2ndIndex < suballoc2ndCount) && (suballocations2nd[nextAlloc2ndIndex].privateData == null))
                {
                    ++nextAlloc2ndIndex;
                }

                // Found non-null allocation.
                if (nextAlloc2ndIndex < suballoc2ndCount)
                {
                    ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[nextAlloc2ndIndex];

                    // 1. Process free space before this allocation.
                    if (lastOffset < suballoc.offset)
                    {
                        // There is free space from lastOffset to suballoc.offset.
                        ulong unusedRangeSize = suballoc.offset - lastOffset;
                        D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, unusedRangeSize);
                    }

                    // 2. Process this allocation.
                    // There is allocation with suballoc.offset, suballoc.size.
                    D3D12MA_AddDetailedStatisticsAllocation(ref *inoutStats, suballoc.size);

                    // 3. Prepare for next iteration.
                    lastOffset = suballoc.offset + suballoc.size;
                    ++nextAlloc2ndIndex;
                }
                // We are at the end.
                else
                {
                    // There is free space from lastOffset to freeSpace2ndTo1stEnd.
                    if (lastOffset < freeSpace2ndTo1stEnd)
                    {
                        ulong unusedRangeSize = freeSpace2ndTo1stEnd - lastOffset;
                        D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, unusedRangeSize);
                    }

                    // End of loop.
                    lastOffset = freeSpace2ndTo1stEnd;
                }
            }
        }

        nuint nextAlloc1stIndex = pThis->m_1stNullItemsBeginCount;
        ulong freeSpace1stTo2ndEnd = pThis->m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK ? suballocations2nd.back().offset : size;

        while (lastOffset < freeSpace1stTo2ndEnd)
        {
            // Find next non-null allocation or move nextAllocIndex to the end.
            while ((nextAlloc1stIndex < suballoc1stCount) && (suballocations1st[nextAlloc1stIndex].privateData == null))
            {
                ++nextAlloc1stIndex;
            }

            // Found non-null allocation.
            if (nextAlloc1stIndex < suballoc1stCount)
            {
                ref readonly D3D12MA_Suballocation suballoc = ref suballocations1st[nextAlloc1stIndex];

                // 1. Process free space before this allocation.
                if (lastOffset < suballoc.offset)
                {
                    // There is free space from lastOffset to suballoc.offset.
                    ulong unusedRangeSize = suballoc.offset - lastOffset;
                    D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, unusedRangeSize);
                }

                // 2. Process this allocation.
                // There is allocation with suballoc.offset, suballoc.size.
                D3D12MA_AddDetailedStatisticsAllocation(ref *inoutStats, suballoc.size);

                // 3. Prepare for next iteration.
                lastOffset = suballoc.offset + suballoc.size;
                ++nextAlloc1stIndex;
            }
            // We are at the end.
            else
            {
                // There is free space from lastOffset to freeSpace1stTo2ndEnd.
                if (lastOffset < freeSpace1stTo2ndEnd)
                {
                    ulong unusedRangeSize = freeSpace1stTo2ndEnd - lastOffset;
                    D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, unusedRangeSize);
                }

                // End of loop.
                lastOffset = freeSpace1stTo2ndEnd;
            }
        }

        if (pThis->m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK)
        {
            nuint nextAlloc2ndIndex = suballocations2nd.size() - 1;

            while (lastOffset < size)
            {
                // Find next non-null allocation or move nextAllocIndex to the end.
                while ((nextAlloc2ndIndex != nuint.MaxValue) && (suballocations2nd[nextAlloc2ndIndex].privateData == null))
                {
                    --nextAlloc2ndIndex;
                }

                // Found non-null allocation.
                if (nextAlloc2ndIndex != nuint.MaxValue)
                {
                    ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[nextAlloc2ndIndex];

                    // 1. Process free space before this allocation.
                    if (lastOffset < suballoc.offset)
                    {
                        // There is free space from lastOffset to suballoc.offset.
                        ulong unusedRangeSize = suballoc.offset - lastOffset;
                        D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, unusedRangeSize);
                    }

                    // 2. Process this allocation.
                    // There is allocation with suballoc.offset, suballoc.size.
                    D3D12MA_AddDetailedStatisticsAllocation(ref *inoutStats, suballoc.size);

                    // 3. Prepare for next iteration.
                    lastOffset = suballoc.offset + suballoc.size;
                    --nextAlloc2ndIndex;
                }
                // We are at the end.
                else
                {
                    // There is free space from lastOffset to size.
                    if (lastOffset < size)
                    {
                        ulong unusedRangeSize = size - lastOffset;
                        D3D12MA_AddDetailedStatisticsUnusedRange(ref *inoutStats, unusedRangeSize);
                    }

                    // End of loop.
                    lastOffset = size;
                }
            }
        }
    }

    [VtblIndex(20)]
    [UnmanagedCallersOnly]
    internal static void WriteAllocationInfoToJson(D3D12MA_BlockMetadata_Linear* pThis, [NativeTypeName("D3D12MA::JsonWriter &")] D3D12MA_JsonWriter* json)
    {
        ulong size = pThis->GetSize();

        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref pThis->AccessSuballocations1st();
        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref pThis->AccessSuballocations2nd();

        nuint suballoc1stCount = suballocations1st.size();
        nuint suballoc2ndCount = suballocations2nd.size();

        // FIRST PASS

        nuint unusedRangeCount = 0;
        ulong usedBytes = 0;

        ulong lastOffset = 0;
        nuint alloc2ndCount = 0;

        if (pThis->m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER)
        {
            ulong freeSpace2ndTo1stEnd = suballocations1st[pThis->m_1stNullItemsBeginCount].offset;
            nuint nextAlloc2ndIndex = 0;

            while (lastOffset < freeSpace2ndTo1stEnd)
            {
                // Find next non-null allocation or move nextAlloc2ndIndex to the end.
                while ((nextAlloc2ndIndex < suballoc2ndCount) && (suballocations2nd[nextAlloc2ndIndex].privateData == null))
                {
                    ++nextAlloc2ndIndex;
                }

                // Found non-null allocation.
                if (nextAlloc2ndIndex < suballoc2ndCount)
                {
                    ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[nextAlloc2ndIndex];

                    // 1. Process free space before this allocation.
                    if (lastOffset < suballoc.offset)
                    {
                        // There is free space from lastOffset to suballoc.offset.
                        ++unusedRangeCount;
                    }

                    // 2. Process this allocation.
                    // There is allocation with suballoc.offset, suballoc.size.
                    ++alloc2ndCount;
                    usedBytes += suballoc.size;

                    // 3. Prepare for next iteration.
                    lastOffset = suballoc.offset + suballoc.size;
                    ++nextAlloc2ndIndex;
                }
                // We are at the end.
                else
                {
                    if (lastOffset < freeSpace2ndTo1stEnd)
                    {
                        // There is free space from lastOffset to freeSpace2ndTo1stEnd.
                        ++unusedRangeCount;
                    }

                    // End of loop.
                    lastOffset = freeSpace2ndTo1stEnd;
                }
            }
        }

        nuint nextAlloc1stIndex = pThis->m_1stNullItemsBeginCount;
        nuint alloc1stCount = 0;
        ulong freeSpace1stTo2ndEnd = (pThis->m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK) ? suballocations2nd.back().offset : size;

        while (lastOffset < freeSpace1stTo2ndEnd)
        {
            // Find next non-null allocation or move nextAllocIndex to the end.
            while ((nextAlloc1stIndex < suballoc1stCount) && (suballocations1st[nextAlloc1stIndex].privateData == null))
            {
                ++nextAlloc1stIndex;
            }

            // Found non-null allocation.
            if (nextAlloc1stIndex < suballoc1stCount)
            {
                ref readonly D3D12MA_Suballocation suballoc = ref suballocations1st[nextAlloc1stIndex];

                // 1. Process free space before this allocation.
                if (lastOffset < suballoc.offset)
                {
                    // There is free space from lastOffset to suballoc.offset.
                    ++unusedRangeCount;
                }

                // 2. Process this allocation.
                // There is allocation with suballoc.offset, suballoc.size.
                ++alloc1stCount;
                usedBytes += suballoc.size;

                // 3. Prepare for next iteration.
                lastOffset = suballoc.offset + suballoc.size;
                ++nextAlloc1stIndex;
            }
            // We are at the end.
            else
            {
                if (lastOffset < size)
                {
                    // There is free space from lastOffset to freeSpace1stTo2ndEnd.
                    ++unusedRangeCount;
                }

                // End of loop.
                lastOffset = freeSpace1stTo2ndEnd;
            }
        }

        if (pThis->m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK)
        {
            nuint nextAlloc2ndIndex = suballocations2nd.size() - 1;

            while (lastOffset < size)
            {
                // Find next non-null allocation or move nextAlloc2ndIndex to the end.
                while ((nextAlloc2ndIndex != nuint.MaxValue) && (suballocations2nd[nextAlloc2ndIndex].privateData == null))
                {
                    --nextAlloc2ndIndex;
                }

                // Found non-null allocation.
                if (nextAlloc2ndIndex != nuint.MaxValue)
                {
                    ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[nextAlloc2ndIndex];

                    // 1. Process free space before this allocation.
                    if (lastOffset < suballoc.offset)
                    {
                        // There is free space from lastOffset to suballoc.offset.
                        ++unusedRangeCount;
                    }

                    // 2. Process this allocation.
                    // There is allocation with suballoc.offset, suballoc.size.
                    ++alloc2ndCount;
                    usedBytes += suballoc.size;

                    // 3. Prepare for next iteration.
                    lastOffset = suballoc.offset + suballoc.size;
                    --nextAlloc2ndIndex;
                }
                // We are at the end.
                else
                {
                    if (lastOffset < size)
                    {
                        // There is free space from lastOffset to size.
                        ++unusedRangeCount;
                    }

                    // End of loop.
                    lastOffset = size;
                }
            }
        }

        ulong unusedBytes = size - usedBytes;
        pThis->PrintDetailedMap_Begin(ref *json, unusedBytes, alloc1stCount + alloc2ndCount, unusedRangeCount);

        // SECOND PASS
        lastOffset = 0;
        if (pThis->m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER)
        {
            ulong freeSpace2ndTo1stEnd = suballocations1st[pThis->m_1stNullItemsBeginCount].offset;
            nuint nextAlloc2ndIndex = 0;

            while (lastOffset < freeSpace2ndTo1stEnd)
            {
                // Find next non-null allocation or move nextAlloc2ndIndex to the end.
                while ((nextAlloc2ndIndex < suballoc2ndCount) && (suballocations2nd[nextAlloc2ndIndex].privateData == null))
                {
                    ++nextAlloc2ndIndex;
                }

                // Found non-null allocation.
                if (nextAlloc2ndIndex < suballoc2ndCount)
                {
                    ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[nextAlloc2ndIndex];

                    // 1. Process free space before this allocation.
                    if (lastOffset < suballoc.offset)
                    {
                        // There is free space from lastOffset to suballoc.offset.
                        ulong unusedRangeSize = suballoc.offset - lastOffset;
                        pThis->PrintDetailedMap_UnusedRange(ref *json, lastOffset, unusedRangeSize);
                    }

                    // 2. Process this allocation.
                    // There is allocation with suballoc.offset, suballoc.size.
                    pThis->PrintDetailedMap_Allocation(ref *json, suballoc.offset, suballoc.size, suballoc.privateData);

                    // 3. Prepare for next iteration.
                    lastOffset = suballoc.offset + suballoc.size;
                    ++nextAlloc2ndIndex;
                }
                // We are at the end.
                else
                {
                    if (lastOffset < freeSpace2ndTo1stEnd)
                    {
                        // There is free space from lastOffset to freeSpace2ndTo1stEnd.
                        ulong unusedRangeSize = freeSpace2ndTo1stEnd - lastOffset;
                        pThis->PrintDetailedMap_UnusedRange(ref *json, lastOffset, unusedRangeSize);
                    }

                    // End of loop.
                    lastOffset = freeSpace2ndTo1stEnd;
                }
            }
        }

        nextAlloc1stIndex = pThis->m_1stNullItemsBeginCount;

        while (lastOffset < freeSpace1stTo2ndEnd)
        {
            // Find next non-null allocation or move nextAllocIndex to the end.
            while ((nextAlloc1stIndex < suballoc1stCount) && (suballocations1st[nextAlloc1stIndex].privateData == null))
            {
                ++nextAlloc1stIndex;
            }

            // Found non-null allocation.
            if (nextAlloc1stIndex < suballoc1stCount)
            {
                ref readonly D3D12MA_Suballocation suballoc = ref suballocations1st[nextAlloc1stIndex];

                // 1. Process free space before this allocation.
                if (lastOffset < suballoc.offset)
                {
                    // There is free space from lastOffset to suballoc.offset.
                    ulong unusedRangeSize = suballoc.offset - lastOffset;
                    pThis->PrintDetailedMap_UnusedRange(ref *json, lastOffset, unusedRangeSize);
                }

                // 2. Process this allocation.
                // There is allocation with suballoc.offset, suballoc.size.
                pThis->PrintDetailedMap_Allocation(ref *json, suballoc.offset, suballoc.size, suballoc.privateData);

                // 3. Prepare for next iteration.
                lastOffset = suballoc.offset + suballoc.size;
                ++nextAlloc1stIndex;
            }
            // We are at the end.
            else
            {
                if (lastOffset < freeSpace1stTo2ndEnd)
                {
                    // There is free space from lastOffset to freeSpace1stTo2ndEnd.
                    ulong unusedRangeSize = freeSpace1stTo2ndEnd - lastOffset;
                    pThis->PrintDetailedMap_UnusedRange(ref *json, lastOffset, unusedRangeSize);
                }

                // End of loop.
                lastOffset = freeSpace1stTo2ndEnd;
            }
        }

        if (pThis->m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK)
        {
            nuint nextAlloc2ndIndex = suballocations2nd.size() - 1;

            while (lastOffset < size)
            {
                // Find next non-null allocation or move nextAlloc2ndIndex to the end.
                while ((nextAlloc2ndIndex != nuint.MaxValue) && (suballocations2nd[nextAlloc2ndIndex].privateData == null))
                {
                    --nextAlloc2ndIndex;
                }

                // Found non-null allocation.
                if (nextAlloc2ndIndex != nuint.MaxValue)
                {
                    ref readonly D3D12MA_Suballocation suballoc = ref suballocations2nd[nextAlloc2ndIndex];

                    // 1. Process free space before this allocation.
                    if (lastOffset < suballoc.offset)
                    {
                        // There is free space from lastOffset to suballoc.offset.
                        ulong  unusedRangeSize = suballoc.offset - lastOffset;
                        pThis->PrintDetailedMap_UnusedRange(ref *json, lastOffset, unusedRangeSize);
                    }

                    // 2. Process this allocation.
                    // There is allocation with suballoc.offset, suballoc.size.
                    pThis->PrintDetailedMap_Allocation(ref *json, suballoc.offset, suballoc.size, suballoc.privateData);

                    // 3. Prepare for next iteration.
                    lastOffset = suballoc.offset + suballoc.size;
                    --nextAlloc2ndIndex;
                }
                // We are at the end.
                else
                {
                    if (lastOffset < size)
                    {
                        // There is free space from lastOffset to size.
                        ulong  unusedRangeSize = size - lastOffset;
                        pThis->PrintDetailedMap_UnusedRange(ref *json, lastOffset, unusedRangeSize);
                    }

                    // End of loop.
                    lastOffset = size;
                }
            }
        }

        pThis->PrintDetailedMap_End(ref *json);
    }
}
