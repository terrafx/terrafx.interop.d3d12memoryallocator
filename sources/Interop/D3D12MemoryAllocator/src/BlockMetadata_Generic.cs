// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.SuballocationType;

using SuballocationList = TerraFX.Interop.List<TerraFX.Interop.Suballocation>;

namespace TerraFX.Interop
{
    internal unsafe struct BlockMetadata_Generic // : BlockMetadata
    {
        private static readonly void** SharedLpVtbl = InitLpVtbl();

        private static void** InitLpVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(BlockMetadata_Generic), sizeof(void*) * 15);
            lpVtbl[0] = (delegate*<BlockMetadata_Generic*, void>)&Dispose;
            lpVtbl[1] = (delegate*<BlockMetadata_Generic*, ulong, void>)&Init;
            lpVtbl[2] = (delegate*<BlockMetadata_Generic*, bool>)&Validate;
            lpVtbl[3] = (delegate*<BlockMetadata_Generic*, nuint>)&GetAllocationCount;
            lpVtbl[4] = (delegate*<BlockMetadata_Generic*, ulong>)&GetSumFreeSize;
            lpVtbl[5] = (delegate*<BlockMetadata_Generic*, ulong>)&GetUnusedRangeSizeMax;
            lpVtbl[6] = (delegate*<BlockMetadata_Generic*, bool>)&IsEmpty;
            lpVtbl[7] = (delegate*<BlockMetadata_Generic*, ulong, VIRTUAL_ALLOCATION_INFO*, void>)&GetAllocationInfo;
            lpVtbl[8] = (delegate*<BlockMetadata_Generic*, ulong, ulong, AllocationRequest*, bool>)&CreateAllocationRequest;
            lpVtbl[9] = (delegate*<BlockMetadata_Generic*, AllocationRequest*, ulong, void*, void>)&Alloc;
            lpVtbl[10] = (delegate*<BlockMetadata_Generic*, ulong, void>)&FreeAtOffset;
            lpVtbl[11] = (delegate*<BlockMetadata_Generic*, void>)&Clear;
            lpVtbl[12] = (delegate*<BlockMetadata_Generic*, ulong, void*, void>)&SetAllocationUserData;
            lpVtbl[13] = (delegate*<BlockMetadata_Generic*, StatInfo*, void>)&CalcAllocationStatInfo;
            lpVtbl[14] = (delegate*<BlockMetadata_Generic*, JsonWriter*, void>)&WriteAllocationInfoToJson;
            return lpVtbl;
        }

        private BlockMetadata Base;

        [NativeTypeName("UINT")]
        private uint m_FreeCount;

        [NativeTypeName("UINT")]
        private ulong m_SumFreeSize;

        private SuballocationList m_Suballocations;

        // Suballocations that are free and have size greater than certain threshold.
        // Sorted by size, ascending.
        private Vector<SuballocationList.iterator> m_FreeSuballocationsBySize;
        private ZeroInitializedRange m_ZeroInitializedRange;

        public BlockMetadata_Generic(ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
        {
            Base = new BlockMetadata(allocationCallbacks, isVirtual);
            Base.lpVtbl = SharedLpVtbl;
            m_FreeCount = 0;
            m_SumFreeSize = 0;
            m_Suballocations = new SuballocationList(allocationCallbacks);
            m_FreeSuballocationsBySize = new Vector<SuballocationList.iterator>(allocationCallbacks);
            m_ZeroInitializedRange = default;

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocationCallbacks != null));
        }

        public void Dispose()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, void>)&Dispose == Base.lpVtbl[0]));
            Dispose((BlockMetadata_Generic*)Unsafe.AsPointer(ref this));
        }

        public void Init([NativeTypeName("UINT64")] ulong size)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, ulong, void>)&Init == Base.lpVtbl[1]));
            Init((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), size);
        }

        public readonly bool Validate()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, bool>)&Validate == Base.lpVtbl[2]));
            return Validate((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => Base.m_Size;

        public readonly bool IsVirtual() => Base.m_IsVirtual;

        [return: NativeTypeName("size_t")]
        public readonly nuint GetAllocationCount()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, nuint>)&GetAllocationCount == Base.lpVtbl[3]));
            return GetAllocationCount((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSumFreeSize()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, ulong>)&GetSumFreeSize == Base.lpVtbl[4]));
            return GetSumFreeSize((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetUnusedRangeSizeMax()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, ulong>)&GetUnusedRangeSizeMax == Base.lpVtbl[5]));
            return GetUnusedRangeSizeMax((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        public readonly bool IsEmpty()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, bool>)&IsEmpty == Base.lpVtbl[6]));
            return IsEmpty((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        public readonly void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, ulong, VIRTUAL_ALLOCATION_INFO*, void>)&GetAllocationInfo == Base.lpVtbl[7]));
            GetAllocationInfo((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), offset, outInfo);
        }

        public bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, AllocationRequest* pAllocationRequest)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, ulong, ulong, AllocationRequest*, bool>)&CreateAllocationRequest == Base.lpVtbl[8]));
            return CreateAllocationRequest(
                (BlockMetadata_Generic*)Unsafe.AsPointer(ref this),
                allocSize,
                allocAlignment,
                pAllocationRequest);
        }

        public void Alloc(AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* userData)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, AllocationRequest*, ulong, void*, void>)&Alloc == Base.lpVtbl[9]));
            Alloc(
                (BlockMetadata_Generic*)Unsafe.AsPointer(ref this),
                request,
                allocSize,
                userData);
        }

        public void FreeAtOffset([NativeTypeName("UINT64")] ulong offset)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, ulong, void>)&FreeAtOffset == Base.lpVtbl[10]));
            FreeAtOffset((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), offset);
        }

        public void Clear()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, void>)&Clear == Base.lpVtbl[11]));
            Clear((BlockMetadata_Generic*)Unsafe.AsPointer(ref this));
        }

        public void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* userData)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, ulong, void*, void>)&SetAllocationUserData == Base.lpVtbl[12]));
            SetAllocationUserData((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), offset, userData);
        }

        public void CalcAllocationStatInfo(StatInfo* outInfo)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, StatInfo*, void>)&CalcAllocationStatInfo == Base.lpVtbl[13]));
            CalcAllocationStatInfo((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), outInfo);
        }

        public void WriteAllocationInfoToJson(JsonWriter* json)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((delegate*<BlockMetadata_Generic*, JsonWriter*, void>)&WriteAllocationInfoToJson == Base.lpVtbl[14]));
            WriteAllocationInfoToJson((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), json);
        }

        public bool ValidateFreeSuballocationList()
        {
            ulong lastSize = 0;
            for (nuint i = 0, count = m_FreeSuballocationsBySize.size(); i < count; ++i)
            {
                SuballocationList.iterator it = *m_FreeSuballocationsBySize[i];

                D3D12MA_VALIDATE(it.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);
                D3D12MA_VALIDATE(it.op_Arrow()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER);
                D3D12MA_VALIDATE(it.op_Arrow()->size >= lastSize);
                lastSize = it.op_Arrow()->size;
            }

            return true;
        }

        /// <summary>Checks if requested suballocation with given parameters can be placed in given pFreeSuballocItem. If yes, fills pOffset and returns true. If no, returns false.</summary>
        public bool CheckAllocation([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, SuballocationList.iterator suballocItem, [NativeTypeName("UINT64*")] ulong* pOffset, [NativeTypeName("UINT64*")] ulong* pSumFreeSize, [NativeTypeName("UINT64*")] ulong* pSumItemSize, [NativeTypeName("BOOL")] int* pZeroInitialized)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocSize > 0));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballocItem != m_Suballocations.end()));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pOffset != null && pZeroInitialized != null));

            *pSumFreeSize = 0;
            *pSumItemSize = 0;
            *pZeroInitialized = FALSE;

            Suballocation* suballoc = suballocItem.op_Arrow();
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballoc->type == SUBALLOCATION_TYPE_FREE));

            *pSumFreeSize = suballoc->size;

            // Size of this suballocation is too small for this request: Early return.
            if (suballoc->size < allocSize)
            {
                return false;
            }

            // Start from offset equal to beginning of this suballocation.
            *pOffset = suballoc->offset;

            // Apply D3D12MA_DEBUG_MARGIN at the beginning.
            if (D3D12MA_DEBUG_MARGIN > 0)
            {
                *pOffset += D3D12MA_DEBUG_MARGIN;
            }

            // Apply alignment.
            *pOffset = AlignUp((nuint)(*pOffset), (nuint)allocAlignment);

            // Calculate padding at the beginning based on current offset.
            ulong paddingBegin = *pOffset - suballoc->offset;

            // Calculate required margin at the end.
            ulong requiredEndMargin = D3D12MA_DEBUG_MARGIN;

            // Fail if requested size plus margin before and after is bigger than size of this suballocation.
            if (paddingBegin + allocSize + requiredEndMargin > suballoc->size)
            {
                return false;
            }

            // All tests passed: Success. pOffset is already filled.
            *pZeroInitialized = m_ZeroInitializedRange.IsRangeZeroInitialized(*pOffset, *pOffset + allocSize);
            return true;
        }

        /// <summary>Given free suballocation, it merges it with following one, which must also be free.</summary>
        public void MergeFreeWithNext(SuballocationList.iterator item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item != m_Suballocations.end()));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.op_Arrow()->type == SUBALLOCATION_TYPE_FREE));

            SuballocationList.iterator nextItem = item;
            nextItem.op_MoveNext();
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (nextItem != m_Suballocations.end()));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (nextItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE));

            item.op_Arrow()->size += nextItem.op_Arrow()->size;
            --m_FreeCount;
            m_Suballocations.erase(nextItem);
        }

        /// <summary>Releases given suballocation, making it free. Merges it with adjacent free suballocations if applicable. Returns iterator to new free suballocation at this place.</summary>
        public SuballocationList.iterator FreeSuballocation(SuballocationList.iterator suballocItem)
        {
            // Change this suballocation to be marked as free.
            Suballocation* suballoc = suballocItem.op_Arrow();
            suballoc->type = SUBALLOCATION_TYPE_FREE;
            suballoc->userData = null;

            // Update totals.
            ++m_FreeCount;
            m_SumFreeSize += suballoc->size;

            // Merge with previous and/or next suballocation if it's also free.
            bool mergeWithNext = false;
            bool mergeWithPrev = false;

            SuballocationList.iterator nextItem = suballocItem;
            nextItem.op_MoveNext();
            if ((nextItem != m_Suballocations.end()) && (nextItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE))
            {
                mergeWithNext = true;
            }

            SuballocationList.iterator prevItem = suballocItem;
            if (suballocItem != m_Suballocations.begin())
            {
                prevItem.op_MoveBack();
                if (prevItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE)
                {
                    mergeWithPrev = true;
                }
            }

            if (mergeWithNext)
            {
                UnregisterFreeSuballocation(nextItem);
                MergeFreeWithNext(suballocItem);
            }

            if (mergeWithPrev)
            {
                UnregisterFreeSuballocation(prevItem);
                MergeFreeWithNext(prevItem);
                RegisterFreeSuballocation(prevItem);
                return prevItem;
            }
            else
            {
                RegisterFreeSuballocation(suballocItem);
                return suballocItem;
            }
        }

        /// <summary>Given free suballocation, it inserts it into sorted list of <see cref="m_FreeSuballocationsBySize"/> if it's suitable.</summary>
        public void RegisterFreeSuballocation(SuballocationList.iterator item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.op_Arrow()->type == SUBALLOCATION_TYPE_FREE));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.op_Arrow()->size > 0));

            // You may want to enable this validation at the beginning or at the end of
            // this function, depending on what do you want to check.
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (ValidateFreeSuballocationList()));

            if (item.op_Arrow()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER)
            {
                if (m_FreeSuballocationsBySize.empty())
                {
                    m_FreeSuballocationsBySize.push_back(&item);
                }
                else
                {
                    m_FreeSuballocationsBySize.InsertSorted(&item, new SuballocationItemSizeLess());
                }
            }

            //D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (ValidateFreeSuballocationList()));
        }

        /// <summary>Given free suballocation, it removes it from sorted list of <see cref="m_FreeSuballocationsBySize"/> if it's suitable.</summary>
        public void UnregisterFreeSuballocation(SuballocationList.iterator item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.op_Arrow()->type == SUBALLOCATION_TYPE_FREE));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.op_Arrow()->size > 0));

            // You may want to enable this validation at the beginning or at the end of
            // this function, depending on what do you want to check.
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (ValidateFreeSuballocationList()));

            if (item.op_Arrow()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER)
            {
                SuballocationList.iterator* it = BinaryFindFirstNotLess(
                     m_FreeSuballocationsBySize.data(),
                     m_FreeSuballocationsBySize.data() + m_FreeSuballocationsBySize.size(),
                     &item,
                     new SuballocationItemSizeLess());
                for (nuint index = (nuint)(it - m_FreeSuballocationsBySize.data());
                     index < m_FreeSuballocationsBySize.size();
                     ++index)
                {
                    if (*m_FreeSuballocationsBySize[index] == item)
                    {
                        m_FreeSuballocationsBySize.remove(index);
                        return;
                    }

                    D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_FreeSuballocationsBySize[index]->op_Arrow()->size == item.op_Arrow()->size)); // "Not found!"
                }

                D3D12MA_ASSERT(false); // "Not found!"
            }

            //D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (ValidateFreeSuballocationList()));
        }

        public static void Dispose(BlockMetadata_Generic* pThis)
        {
        }

        public static void Init(BlockMetadata_Generic* pThis, ulong size)
        {
            BlockMetadata.Init((BlockMetadata*)pThis, size);
            pThis->m_ZeroInitializedRange.Reset(size);

            pThis->m_FreeCount = 1;
            pThis->m_SumFreeSize = size;

            Suballocation suballoc = default;
            suballoc.offset = 0;
            suballoc.size = size;
            suballoc.type = SUBALLOCATION_TYPE_FREE;
            suballoc.userData = null;

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (size > MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER));
            pThis->m_Suballocations.push_back(&suballoc);
            SuballocationList.iterator suballocItem = pThis->m_Suballocations.end();
            suballocItem.op_MoveBack();
            pThis->m_FreeSuballocationsBySize.push_back(&suballocItem);
        }

        public static bool Validate(BlockMetadata_Generic* pThis)
        {
            D3D12MA_VALIDATE(!pThis->m_Suballocations.empty());

            // Expected offset of new suballocation as calculated from previous ones.
            ulong calculatedOffset = 0;
            // Expected number of free suballocations as calculated from traversing their list.
            uint calculatedFreeCount = 0;
            // Expected sum size of free suballocations as calculated from traversing their list.
            ulong calculatedSumFreeSize = 0;
            // Expected number of free suballocations that should be registered in
            // m_FreeSuballocationsBySize calculated from traversing their list.
            nuint freeSuballocationsToRegister = 0;
            // True if previous visited suballocation was free.
            bool prevFree = false;

            for (SuballocationList.iterator suballocItem = pThis->m_Suballocations.begin();
                suballocItem != pThis->m_Suballocations.end();
                suballocItem.op_MoveNext())
            {
                Suballocation* subAlloc = suballocItem.op_Arrow();

                // Actual offset of this suballocation doesn't match expected one.
                D3D12MA_VALIDATE(subAlloc->offset == calculatedOffset);

                bool currFree = (subAlloc->type == SUBALLOCATION_TYPE_FREE);
                // Two adjacent free suballocations are invalid. They should be merged.
                D3D12MA_VALIDATE(!prevFree || !currFree);

                Allocation* alloc = (Allocation*)subAlloc->userData;
                if (!pThis->IsVirtual())
                {
                    D3D12MA_VALIDATE(currFree == (alloc == null));
                }

                if (currFree)
                {
                    calculatedSumFreeSize += subAlloc->size;
                    ++calculatedFreeCount;
                    if (subAlloc->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER)
                    {
                        ++freeSuballocationsToRegister;
                    }

                    // Margin required between allocations - every free space must be at least that large.
                    D3D12MA_VALIDATE(subAlloc->size >= D3D12MA_DEBUG_MARGIN);
                }
                else
                {
                    if (!pThis->IsVirtual())
                    {
                        D3D12MA_VALIDATE(alloc->GetOffset() == subAlloc->offset);
                        D3D12MA_VALIDATE(alloc->GetSize() == subAlloc->size);
                    }

                    // Margin required between allocations - previous allocation must be free.
                    D3D12MA_VALIDATE(D3D12MA_DEBUG_MARGIN == 0 || prevFree);
                }

                calculatedOffset += subAlloc->size;
                prevFree = currFree;
            }

            // Number of free suballocations registered in m_FreeSuballocationsBySize doesn't
            // match expected one.
            D3D12MA_VALIDATE(pThis->m_FreeSuballocationsBySize.size() == freeSuballocationsToRegister);

            ulong lastSize = 0;
            for (nuint i = 0; i < pThis->m_FreeSuballocationsBySize.size(); ++i)
            {
                SuballocationList.iterator suballocItem = *pThis->m_FreeSuballocationsBySize[i];

                // Only free suballocations can be registered in m_FreeSuballocationsBySize.
                D3D12MA_VALIDATE(suballocItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);
                // They must be sorted by size ascending.
                D3D12MA_VALIDATE(suballocItem.op_Arrow()->size >= lastSize);

                lastSize = suballocItem.op_Arrow()->size;
            }

            // Check if totals match calculacted values.
            D3D12MA_VALIDATE(pThis->ValidateFreeSuballocationList());
            D3D12MA_VALIDATE(calculatedOffset == pThis->GetSize());
            D3D12MA_VALIDATE(calculatedSumFreeSize == pThis->m_SumFreeSize);
            D3D12MA_VALIDATE(calculatedFreeCount == pThis->m_FreeCount);

            return true;
        }

        public static nuint GetAllocationCount(BlockMetadata_Generic* pThis)
        {
            return pThis->m_Suballocations.size() - pThis->m_FreeCount;
        }

        public static ulong GetSumFreeSize(BlockMetadata_Generic* pThis) => pThis->m_SumFreeSize;

        public static ulong GetUnusedRangeSizeMax(BlockMetadata_Generic* pThis)
        {
            if (!pThis->m_FreeSuballocationsBySize.empty())
            {
                return pThis->m_FreeSuballocationsBySize.back()->op_Arrow()->size;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsEmpty(BlockMetadata_Generic* pThis)
        {
            return (pThis->m_Suballocations.size() == 1) && (pThis->m_FreeCount == 1);
        }

        public static void GetAllocationInfo(BlockMetadata_Generic* pThis, ulong offset, VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            for (SuballocationList.iterator suballocItem = pThis->m_Suballocations.begin();
                 suballocItem != pThis->m_Suballocations.end();
                 suballocItem.op_MoveNext())
            {
                Suballocation* suballoc = suballocItem.op_Arrow();
                if (suballoc->offset == offset)
                {
                    outInfo->size = suballoc->size;
                    outInfo->pUserData = suballoc->userData;
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Not found!"
        }

        public static bool CreateAllocationRequest(BlockMetadata_Generic* pThis, ulong allocSize, ulong allocAlignment, AllocationRequest* pAllocationRequest)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocSize > 0));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pAllocationRequest != null));
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (pThis->Validate()));

            // There is not enough total free space in this block to fullfill the request: Early return.
            if (pThis->m_SumFreeSize < allocSize + 2 * D3D12MA_DEBUG_MARGIN)
            {
                return false;
            }

            // New algorithm, efficiently searching freeSuballocationsBySize.
            nuint freeSuballocCount = pThis->m_FreeSuballocationsBySize.size();
            if (freeSuballocCount > 0)
            {
                // Find first free suballocation with size not less than allocSize + 2 * D3D12MA_DEBUG_MARGIN.
                SuballocationList.iterator* it = BinaryFindFirstNotLess(
                     pThis->m_FreeSuballocationsBySize.data(),
                     pThis->m_FreeSuballocationsBySize.data() + freeSuballocCount,
                     allocSize + 2 * D3D12MA_DEBUG_MARGIN,
                     new SuballocationItemSizeLess());
                nuint index = (nuint)(it - pThis->m_FreeSuballocationsBySize.data());
                for (; index < freeSuballocCount; ++index)
                {
                    if (pThis->CheckAllocation(
                        allocSize,
                        allocAlignment,
                        *pThis->m_FreeSuballocationsBySize[index],
                        &pAllocationRequest->offset,
                        &pAllocationRequest->sumFreeSize,
                        &pAllocationRequest->sumItemSize,
                        &pAllocationRequest->zeroInitialized))
                    {
                        pAllocationRequest->item = *pThis->m_FreeSuballocationsBySize[index];
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Alloc(BlockMetadata_Generic* pThis, AllocationRequest* request, ulong allocSize, void* userData)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (request->item != pThis->m_Suballocations.end()));
            Suballocation* suballoc = request->item.op_Arrow();
            // Given suballocation is a free block.
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballoc->type == SUBALLOCATION_TYPE_FREE));
            // Given offset is inside this suballocation.
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (request->offset >= suballoc->offset));
            ulong paddingBegin = request->offset - suballoc->offset;
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballoc->size >= paddingBegin + allocSize));
            ulong paddingEnd = suballoc->size - paddingBegin - allocSize;

            // Unregister this free suballocation from m_FreeSuballocationsBySize and update
            // it to become used.
            pThis->UnregisterFreeSuballocation(request->item);

            suballoc->offset = request->offset;
            suballoc->size = allocSize;
            suballoc->type = SUBALLOCATION_TYPE_ALLOCATION;
            suballoc->userData = userData;

            // If there are any free bytes remaining at the end, insert new free suballocation after current one.
            if (paddingEnd > 0)
            {
                Suballocation paddingSuballoc = default;
                paddingSuballoc.offset = request->offset + allocSize;
                paddingSuballoc.size = paddingEnd;
                paddingSuballoc.type = SUBALLOCATION_TYPE_FREE;
                SuballocationList.iterator next = request->item;
                next.op_MoveNext();
                SuballocationList.iterator paddingEndItem =
                    pThis->m_Suballocations.insert(next, &paddingSuballoc);
                pThis->RegisterFreeSuballocation(paddingEndItem);
            }

            // If there are any free bytes remaining at the beginning, insert new free suballocation before current one.
            if (paddingBegin > 0)
            {
                Suballocation paddingSuballoc = default;
                paddingSuballoc.offset = request->offset - paddingBegin;
                paddingSuballoc.size = paddingBegin;
                paddingSuballoc.type = SUBALLOCATION_TYPE_FREE;
                SuballocationList.iterator paddingBeginItem =
                    pThis->m_Suballocations.insert(request->item, &paddingSuballoc);
                pThis->RegisterFreeSuballocation(paddingBeginItem);
            }

            // Update totals.
            pThis->m_FreeCount = pThis->m_FreeCount - 1;
            if (paddingBegin > 0)
            {
                ++pThis->m_FreeCount;
            }

            if (paddingEnd > 0)
            {
                ++pThis->m_FreeCount;
            }

            pThis->m_SumFreeSize -= allocSize;

            pThis->m_ZeroInitializedRange.MarkRangeAsUsed(request->offset, request->offset + allocSize);
        }

        public static void FreeAtOffset(BlockMetadata_Generic* pThis, ulong offset)
        {
            for (SuballocationList.iterator suballocItem = pThis->m_Suballocations.begin();
                 suballocItem != pThis->m_Suballocations.end();
                 suballocItem.op_MoveNext())
            {
                Suballocation* suballoc = suballocItem.op_Arrow();
                if (suballoc->offset == offset)
                {
                    pThis->FreeSuballocation(suballocItem);
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Not found!"
        }

        public static void Clear(BlockMetadata_Generic* pThis)
        {
            pThis->m_FreeCount = 1;
            pThis->m_SumFreeSize = pThis->GetSize();

            pThis->m_Suballocations.clear();
            Suballocation suballoc = default;
            suballoc.offset = 0;
            suballoc.size = pThis->GetSize();
            suballoc.type = SUBALLOCATION_TYPE_FREE;
            pThis->m_Suballocations.push_back(&suballoc);

            pThis->m_FreeSuballocationsBySize.clear();
            SuballocationList.iterator it = pThis->m_Suballocations.begin();
            pThis->m_FreeSuballocationsBySize.push_back(&it);
        }

        public static void SetAllocationUserData(BlockMetadata_Generic* pThis, ulong offset, void* userData)
        {
            for (SuballocationList.iterator suballocItem = pThis->m_Suballocations.begin();
                 suballocItem != pThis->m_Suballocations.end();
                 suballocItem.op_MoveNext())
            {
                Suballocation* suballoc = suballocItem.op_Arrow();
                if (suballoc->offset == offset)
                {
                    suballoc->userData = userData;
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Not found!"
        }

        public static void CalcAllocationStatInfo(BlockMetadata_Generic* pThis, StatInfo* outInfo)
        {
            outInfo->BlockCount = 1;

            uint rangeCount = (uint)pThis->m_Suballocations.size();
            outInfo->AllocationCount = rangeCount - pThis->m_FreeCount;
            outInfo->UnusedRangeCount = pThis->m_FreeCount;

            outInfo->UsedBytes = pThis->GetSize() - pThis->m_SumFreeSize;
            outInfo->UnusedBytes = pThis->m_SumFreeSize;

            outInfo->AllocationSizeMin = UINT64_MAX;
            outInfo->AllocationSizeMax = 0;
            outInfo->UnusedRangeSizeMin = UINT64_MAX;
            outInfo->UnusedRangeSizeMax = 0;

            for (SuballocationList.iterator suballocItem = pThis->m_Suballocations.begin();
                 suballocItem != pThis->m_Suballocations.end();
                 suballocItem.op_MoveNext())
            {
                Suballocation* suballoc = suballocItem.op_Arrow();
                if (suballoc->type == SUBALLOCATION_TYPE_FREE)
                {
                    outInfo->UnusedRangeSizeMin = D3D12MA_MIN(suballoc->size, outInfo->UnusedRangeSizeMin);
                    outInfo->UnusedRangeSizeMax = D3D12MA_MAX(suballoc->size, outInfo->UnusedRangeSizeMax);
                }
                else
                {
                    outInfo->AllocationSizeMin = D3D12MA_MIN(suballoc->size, outInfo->AllocationSizeMin);
                    outInfo->AllocationSizeMax = D3D12MA_MAX(suballoc->size, outInfo->AllocationSizeMax);
                }
            }
        }

        public static void WriteAllocationInfoToJson(BlockMetadata_Generic* pThis, JsonWriter* json)
        {
            json->BeginObject();
            json->WriteString("TotalBytes");
            json->WriteNumber(pThis->GetSize());
            json->WriteString("UnusuedBytes");
            json->WriteNumber(pThis->GetSumFreeSize());
            json->WriteString("Allocations");
            json->WriteNumber(pThis->GetAllocationCount());
            json->WriteString("UnusedRanges");
            json->WriteNumber(pThis->m_FreeCount);
            json->WriteString("Suballocations");
            json->BeginArray();
            for (SuballocationList.iterator suballocItem = pThis->m_Suballocations.begin();
                 suballocItem != pThis->m_Suballocations.end();
                 suballocItem.op_MoveNext())
            {
                Suballocation* suballoc = suballocItem.op_Arrow();
                json->BeginObject(true);
                json->WriteString("Offset");
                json->WriteNumber(suballoc->offset);
                if (suballoc->type == SUBALLOCATION_TYPE_FREE)
                {
                    json->WriteString("Type");
                    json->WriteString("FREE");
                    json->WriteString("Size");
                    json->WriteNumber(suballoc->size);
                }
                else if (pThis->IsVirtual())
                {
                    json->WriteString("Type");
                    json->WriteString("ALLOCATION");
                    json->WriteString("Size");
                    json->WriteNumber(suballoc->size);
                    if (suballoc->userData != null)
                    {
                        json->WriteString("UserData");
                        json->WriteNumber((uint)suballoc->userData);
                    }
                }
                else
                {
                    Allocation* alloc = (Allocation*)suballoc->userData;
                    D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (alloc != null));
                    json->AddAllocationToObject(alloc);
                }

                json->EndObject();
            }

            json->EndArray();
            json->EndObject();
        }
    }
}
