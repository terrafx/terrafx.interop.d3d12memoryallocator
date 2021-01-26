// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.SuballocationType;

using SuballocationList = TerraFX.Interop.List<TerraFX.Interop.Suballocation>;

namespace TerraFX.Interop
{
    internal unsafe partial struct BlockMetadata_Generic // : BlockMetadata
    {
        private static void** SharedLpVtbl = InitLpVtbl();

        private static void** InitLpVtbl()
        {
            SharedLpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(BlockMetadata_Generic), sizeof(void*) * 15);
            SharedLpVtbl[0] = (delegate*<BlockMetadata_Generic*, void>)&Dispose;
            SharedLpVtbl[1] = (delegate*<BlockMetadata_Generic*, ulong, void>)&Init;
            SharedLpVtbl[2] = (delegate*<BlockMetadata_Generic*, bool>)&Validate;
            SharedLpVtbl[3] = (delegate*<BlockMetadata_Generic*, nuint>)&GetAllocationCount;
            SharedLpVtbl[4] = (delegate*<BlockMetadata_Generic*, ulong>)&GetSumFreeSize;
            SharedLpVtbl[5] = (delegate*<BlockMetadata_Generic*, ulong>)&GetUnusedRangeSizeMax;
            SharedLpVtbl[6] = (delegate*<BlockMetadata_Generic*, bool>)&IsEmpty;
            SharedLpVtbl[7] = (delegate*<BlockMetadata_Generic*, ulong, VIRTUAL_ALLOCATION_INFO*, void>)&GetAllocationInfo;
            SharedLpVtbl[8] = (delegate*<BlockMetadata_Generic*, ulong, ulong, AllocationRequest*, bool>)&CreateAllocationRequest;
            SharedLpVtbl[9] = (delegate*<BlockMetadata_Generic*, AllocationRequest*, ulong, void*, void>)&Alloc;
            SharedLpVtbl[10] = (delegate*<BlockMetadata_Generic*, ulong, void>)&FreeAtOffset;
            SharedLpVtbl[11] = (delegate*<BlockMetadata_Generic*, void>)&Clear;
            SharedLpVtbl[12] = (delegate*<BlockMetadata_Generic*, ulong, void*, void>)&SetAllocationUserData;
            SharedLpVtbl[13] = (delegate*<BlockMetadata_Generic*, StatInfo*, void>)&CalcAllocationStatInfo;
            SharedLpVtbl[14] = (delegate*<BlockMetadata_Generic*, JsonWriter*, void>)&WriteAllocationInfoToJson;
            return SharedLpVtbl;
        }

        private BlockMetadata @base;

        [NativeTypeName("UINT")] private uint m_FreeCount;
        [NativeTypeName("UINT")] private ulong m_SumFreeSize;
        private SuballocationList m_Suballocations;
        // Suballocations that are free and have size greater than certain threshold.
        // Sorted by size, ascending.
        private Vector<SuballocationList.iterator> m_FreeSuballocationsBySize;
        private ZeroInitializedRange m_ZeroInitializedRange;

        public BlockMetadata_Generic(ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
        {
            @base = new(allocationCallbacks, isVirtual);
            @base.lpVtbl = SharedLpVtbl;
            m_FreeCount = 0;
            m_SumFreeSize = 0;
            m_Suballocations = new(allocationCallbacks);
            m_FreeSuballocationsBySize = new(allocationCallbacks);
            m_ZeroInitializedRange = default;

            D3D12MA_ASSERT(allocationCallbacks);
        }

        public partial void Dispose();
        public partial void Init([NativeTypeName("UINT64")] ulong size);

        public readonly partial bool Validate();

        [return: NativeTypeName("size_t")]
        public readonly nuint GetAllocationCount() { return GetAllocationCount((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this))); }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSumFreeSize() { return GetSumFreeSize((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this))); }
        [return: NativeTypeName("UINT64")]
        public readonly partial ulong GetUnusedRangeSizeMax();
        public readonly partial bool IsEmpty();

        public readonly partial void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, VIRTUAL_ALLOCATION_INFO* outInfo);

        public partial bool CreateAllocationRequest(
            [NativeTypeName("UINT64")] ulong allocSize,
            [NativeTypeName("UINT64")] ulong allocAlignment,
            AllocationRequest* pAllocationRequest);

        public partial void Alloc(
            AllocationRequest* request,
            [NativeTypeName("UINT64")] ulong allocSize,
            void* userData);

        public partial void FreeAtOffset([NativeTypeName("UINT64")] ulong offset);
        public partial void Clear();

        public partial void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* userData);

        public partial void CalcAllocationStatInfo(StatInfo* outInfo);
        public partial void WriteAllocationInfoToJson(JsonWriter* json);

        public partial bool ValidateFreeSuballocationList();

        // Checks if requested suballocation with given parameters can be placed in given pFreeSuballocItem.
        // If yes, fills pOffset and returns true. If no, returns false.
        public partial bool CheckAllocation(
            [NativeTypeName("UINT64")] ulong allocSize,
            [NativeTypeName("UINT64")] ulong allocAlignment,
            SuballocationList.iterator suballocItem,
            [NativeTypeName("UINT64*")] ulong* pOffset,
            [NativeTypeName("UINT64*")] ulong* pSumFreeSize,
            [NativeTypeName("UINT64*")] ulong* pSumItemSize,
            [NativeTypeName("BOOL")] int* pZeroInitialized);
        // Given free suballocation, it merges it with following one, which must also be free.
        public partial void MergeFreeWithNext(SuballocationList.iterator item);
        // Releases given suballocation, making it free.
        // Merges it with adjacent free suballocations if applicable.
        // Returns iterator to new free suballocation at this place.
        public partial SuballocationList.iterator FreeSuballocation(SuballocationList.iterator suballocItem);
        // Given free suballocation, it inserts it into sorted list of
        // m_FreeSuballocationsBySize if it's suitable.
        public partial void RegisterFreeSuballocation(SuballocationList.iterator item);
        // Given free suballocation, it removes it from sorted list of
        // m_FreeSuballocationsBySize if it's suitable.
        public partial void UnregisterFreeSuballocation(SuballocationList.iterator item);
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Private class BlockMetadata_Generic implementation

    internal unsafe partial struct BlockMetadata_Generic
    {
        public partial void Dispose()
        {
            Dispose((BlockMetadata_Generic*)Unsafe.AsPointer(ref this));
        }

        public partial void Init(ulong size)
        {
            Init((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), size);
        }

        public readonly partial bool Validate()
        {
            return Validate((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        public readonly partial ulong GetUnusedRangeSizeMax()
        {
            return GetUnusedRangeSizeMax((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        public readonly partial bool IsEmpty()
        {
            return IsEmpty((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        public readonly partial void GetAllocationInfo(ulong offset, VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            GetAllocationInfo((BlockMetadata_Generic*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), offset, outInfo);
        }

        public partial bool CreateAllocationRequest(
            ulong allocSize,
            ulong allocAlignment,
            AllocationRequest* pAllocationRequest)
        {
            return CreateAllocationRequest(
                (BlockMetadata_Generic*)Unsafe.AsPointer(ref this),
                allocSize,
                allocAlignment,
                pAllocationRequest);
        }

        public partial void Alloc(
            AllocationRequest* request,
            ulong allocSize,
            void* userData)
        {
            Alloc(
                (BlockMetadata_Generic*)Unsafe.AsPointer(ref this),
                request,
                allocSize,
                userData);
        }

        public partial void FreeAtOffset(ulong offset)
        {
            FreeAtOffset((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), offset);
        }

        public partial void Clear()
        {
            Clear((BlockMetadata_Generic*)Unsafe.AsPointer(ref this));
        }

        public partial bool ValidateFreeSuballocationList()
        {
            return ValidateFreeSuballocationList((BlockMetadata_Generic*)Unsafe.AsPointer(ref this));
        }

        public partial bool CheckAllocation(
            ulong allocSize,
            ulong allocAlignment,
            SuballocationList.iterator suballocItem,
            ulong* pOffset,
            ulong* pSumFreeSize,
            ulong* pSumItemSize,
            int* pZeroInitialized)
        {
            return CheckAllocation(
                (BlockMetadata_Generic*)Unsafe.AsPointer(ref this),
                allocSize,
                allocAlignment,
                suballocItem,
                pOffset,
                pSumFreeSize,
                pSumItemSize,
                pZeroInitialized);
        }

        public partial void MergeFreeWithNext(SuballocationList.iterator item)
        {
            MergeFreeWithNext((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), item);
        }

        public partial SuballocationList.iterator FreeSuballocation(SuballocationList.iterator suballocItem)
        {
            return FreeSuballocation((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), suballocItem);
        }

        public partial void RegisterFreeSuballocation(SuballocationList.iterator item)
        {
            RegisterFreeSuballocation((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), item);
        }

        public partial void UnregisterFreeSuballocation(SuballocationList.iterator item)
        {
            UnregisterFreeSuballocation((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), item);
        }

        public partial void SetAllocationUserData(ulong offset, void* userData)
        {
            SetAllocationUserData((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), offset, userData);
        }

        public partial void CalcAllocationStatInfo(StatInfo* outInfo)
        {
            CalcAllocationStatInfo((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), outInfo);
        }

        public partial void WriteAllocationInfoToJson(JsonWriter* json)
        {
            WriteAllocationInfoToJson((BlockMetadata_Generic*)Unsafe.AsPointer(ref this), json);
        }

        public static void Dispose(BlockMetadata_Generic* @this)
        {
        }

        public static void Init(BlockMetadata_Generic* @this, ulong size)
        {
            BlockMetadata.Init((BlockMetadata*)@this, size);
            @this->m_ZeroInitializedRange.Reset(size);

            @this->m_FreeCount = 1;
            @this->m_SumFreeSize = size;

            Suballocation suballoc = default;
            suballoc.offset = 0;
            suballoc.size = size;
            suballoc.type = SUBALLOCATION_TYPE_FREE;
            suballoc.userData = null;

            D3D12MA_ASSERT(size > MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER);
            @this->m_Suballocations.push_back(&suballoc);
            SuballocationList.iterator suballocItem = @this->m_Suballocations.end();
            suballocItem.op_MoveBack();
            @this->m_FreeSuballocationsBySize.push_back(&suballocItem);
        }

        public static bool Validate(BlockMetadata_Generic* @this)
        {
            D3D12MA_VALIDATE(!@this->m_Suballocations.empty());

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

            for (SuballocationList.iterator suballocItem = @this->m_Suballocations.begin();
                suballocItem != @this->m_Suballocations.end();
                suballocItem.op_MoveNext())
            {
                Suballocation* subAlloc = suballocItem.op_Arrow();

                // Actual offset of this suballocation doesn't match expected one.
                D3D12MA_VALIDATE(subAlloc->offset == calculatedOffset);

                bool currFree = (subAlloc->type == SUBALLOCATION_TYPE_FREE);
                // Two adjacent free suballocations are invalid. They should be merged.
                D3D12MA_VALIDATE(!prevFree || !currFree);

                Allocation* alloc = (Allocation*)subAlloc->userData;
                if (!@this->@base.IsVirtual())
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
                    if (!@this->@base.IsVirtual())
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
            D3D12MA_VALIDATE(@this->m_FreeSuballocationsBySize.size() == freeSuballocationsToRegister);

            ulong lastSize = 0;
            for (nuint i = 0; i < @this->m_FreeSuballocationsBySize.size(); ++i)
            {
                SuballocationList.iterator suballocItem = *@this->m_FreeSuballocationsBySize[i];

                // Only free suballocations can be registered in m_FreeSuballocationsBySize.
                D3D12MA_VALIDATE(suballocItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);
                // They must be sorted by size ascending.
                D3D12MA_VALIDATE(suballocItem.op_Arrow()->size >= lastSize);

                lastSize = suballocItem.op_Arrow()->size;
            }

            // Check if totals match calculacted values.
            D3D12MA_VALIDATE(@this->ValidateFreeSuballocationList());
            D3D12MA_VALIDATE(calculatedOffset == @this->@base.GetSize());
            D3D12MA_VALIDATE(calculatedSumFreeSize == @this->m_SumFreeSize);
            D3D12MA_VALIDATE(calculatedFreeCount == @this->m_FreeCount);

            return true;
        }

        public static nuint GetAllocationCount(BlockMetadata_Generic* @this)
        {
            return @this->m_Suballocations.size() - @this->m_FreeCount;
        }

        public static ulong GetSumFreeSize(BlockMetadata_Generic* @this) { return @this->m_SumFreeSize; }

        public static ulong GetUnusedRangeSizeMax(BlockMetadata_Generic* @this)
        {
            if (!@this->m_FreeSuballocationsBySize.empty())
            {
                return @this->m_FreeSuballocationsBySize.back()->op_Arrow()->size;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsEmpty(BlockMetadata_Generic* @this)
        {
            return (@this->m_Suballocations.size() == 1) && (@this->m_FreeCount == 1);
        }

        public static void GetAllocationInfo(BlockMetadata_Generic* @this, ulong offset, VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            for (SuballocationList.iterator suballocItem = @this->m_Suballocations.begin();
                 suballocItem != @this->m_Suballocations.end();
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
            D3D12MA_ASSERT(0);
        }

        public static bool CreateAllocationRequest(
            BlockMetadata_Generic* @this,
            ulong allocSize,
            ulong allocAlignment,
            AllocationRequest* pAllocationRequest)
        {
            D3D12MA_ASSERT(allocSize > 0);
            D3D12MA_ASSERT(pAllocationRequest);
            D3D12MA_HEAVY_ASSERT(@this->Validate());

            // There is not enough total free space in this block to fullfill the request: Early return.
            if (@this->m_SumFreeSize < allocSize + 2 * D3D12MA_DEBUG_MARGIN)
            {
                return false;
            }

            // New algorithm, efficiently searching freeSuballocationsBySize.
            nuint freeSuballocCount = @this->m_FreeSuballocationsBySize.size();
            if (freeSuballocCount > 0)
            {
                // Find first free suballocation with size not less than allocSize + 2 * D3D12MA_DEBUG_MARGIN.
                SuballocationList.iterator* it = BinaryFindFirstNotLess(
                     @this->m_FreeSuballocationsBySize.data(),
                     @this->m_FreeSuballocationsBySize.data() + freeSuballocCount,
                     allocSize + 2 * D3D12MA_DEBUG_MARGIN,
                     new SuballocationItemSizeLess());
                nuint index = (nuint)(it - @this->m_FreeSuballocationsBySize.data());
                for (; index < freeSuballocCount; ++index)
                {
                    if (@this->CheckAllocation(
                        allocSize,
                        allocAlignment,
                        *@this->m_FreeSuballocationsBySize[index],
                        &pAllocationRequest->offset,
                        &pAllocationRequest->sumFreeSize,
                        &pAllocationRequest->sumItemSize,
                        &pAllocationRequest->zeroInitialized))
                    {
                        pAllocationRequest->item = *@this->m_FreeSuballocationsBySize[index];
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Alloc(
            BlockMetadata_Generic* @this,
            AllocationRequest* request,
            ulong allocSize,
            void* userData)
        {
            D3D12MA_ASSERT(request->item != @this->m_Suballocations.end());
            Suballocation* suballoc = request->item.op_Arrow();
            // Given suballocation is a free block.
            D3D12MA_ASSERT(suballoc->type == SUBALLOCATION_TYPE_FREE);
            // Given offset is inside this suballocation.
            D3D12MA_ASSERT(request->offset >= suballoc->offset);
            ulong paddingBegin = request->offset - suballoc->offset;
            D3D12MA_ASSERT(suballoc->size >= paddingBegin + allocSize);
            ulong paddingEnd = suballoc->size - paddingBegin - allocSize;

            // Unregister this free suballocation from m_FreeSuballocationsBySize and update
            // it to become used.
            @this->UnregisterFreeSuballocation(request->item);

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
                    @this->m_Suballocations.insert(next, &paddingSuballoc);
                @this->RegisterFreeSuballocation(paddingEndItem);
            }

            // If there are any free bytes remaining at the beginning, insert new free suballocation before current one.
            if (paddingBegin > 0)
            {
                Suballocation paddingSuballoc = default;
                paddingSuballoc.offset = request->offset - paddingBegin;
                paddingSuballoc.size = paddingBegin;
                paddingSuballoc.type = SUBALLOCATION_TYPE_FREE;
                SuballocationList.iterator paddingBeginItem =
                    @this->m_Suballocations.insert(request->item, &paddingSuballoc);
                @this->RegisterFreeSuballocation(paddingBeginItem);
            }

            // Update totals.
            @this->m_FreeCount = @this->m_FreeCount - 1;
            if (paddingBegin > 0)
            {
                ++@this->m_FreeCount;
            }
            if (paddingEnd > 0)
            {
                ++@this->m_FreeCount;
            }
            @this->m_SumFreeSize -= allocSize;

            @this->m_ZeroInitializedRange.MarkRangeAsUsed(request->offset, request->offset + allocSize);
        }

        public static void FreeAtOffset(BlockMetadata_Generic* @this, ulong offset)
        {
            for (SuballocationList.iterator suballocItem = @this->m_Suballocations.begin();
                 suballocItem != @this->m_Suballocations.end();
                 suballocItem.op_MoveNext())
            {
                Suballocation* suballoc = suballocItem.op_Arrow();
                if (suballoc->offset == offset)
                {
                    @this->FreeSuballocation(suballocItem);
                    return;
                }
            }
            D3D12MA_ASSERT(0);
        }

        public static void Clear(BlockMetadata_Generic* @this)
        {
            @this->m_FreeCount = 1;
            @this->m_SumFreeSize = @this->@base.GetSize();

            @this->m_Suballocations.clear();
            Suballocation suballoc = default;
            suballoc.offset = 0;
            suballoc.size = @this->@base.GetSize();
            suballoc.type = SUBALLOCATION_TYPE_FREE;
            @this->m_Suballocations.push_back(&suballoc);

            @this->m_FreeSuballocationsBySize.clear();
            SuballocationList.iterator it = @this->m_Suballocations.begin();
            @this->m_FreeSuballocationsBySize.push_back(&it);
        }

        public static bool ValidateFreeSuballocationList(BlockMetadata_Generic* @this)
        {
            ulong lastSize = 0;
            for (nuint i = 0, count = @this->m_FreeSuballocationsBySize.size(); i < count; ++i)
            {
                SuballocationList.iterator it = *@this->m_FreeSuballocationsBySize[i];

                D3D12MA_VALIDATE(it.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);
                D3D12MA_VALIDATE(it.op_Arrow()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER);
                D3D12MA_VALIDATE(it.op_Arrow()->size >= lastSize);
                lastSize = it.op_Arrow()->size;
            }
            return true;
        }

        public static bool CheckAllocation(
            BlockMetadata_Generic* @this,
            ulong allocSize,
            ulong allocAlignment,
            SuballocationList.iterator suballocItem,
            ulong* pOffset,
            ulong* pSumFreeSize,
            ulong* pSumItemSize,
            int* pZeroInitialized)
        {
            D3D12MA_ASSERT(allocSize > 0);
            D3D12MA_ASSERT(suballocItem != @this->m_Suballocations.end());
            D3D12MA_ASSERT(pOffset != null && pZeroInitialized != null);

            *pSumFreeSize = 0;
            *pSumItemSize = 0;
            *pZeroInitialized = FALSE;

            Suballocation* suballoc = suballocItem.op_Arrow();
            D3D12MA_ASSERT(suballoc->type == SUBALLOCATION_TYPE_FREE);

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
#pragma warning disable CS0162
                *pOffset += D3D12MA_DEBUG_MARGIN;
#pragma warning restore CS0162
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
            *pZeroInitialized = @this->m_ZeroInitializedRange.IsRangeZeroInitialized(*pOffset, *pOffset + allocSize);
            return true;
        }

        public static void MergeFreeWithNext(BlockMetadata_Generic* @this, SuballocationList.iterator item)
        {
            D3D12MA_ASSERT(item != @this->m_Suballocations.end());
            D3D12MA_ASSERT(item.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);

            SuballocationList.iterator nextItem = item;
            nextItem.op_MoveNext();
            D3D12MA_ASSERT(nextItem != @this->m_Suballocations.end());
            D3D12MA_ASSERT(nextItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);

            item.op_Arrow()->size += nextItem.op_Arrow()->size;
            --@this->m_FreeCount;
            @this->m_Suballocations.erase(nextItem);
        }

        public static SuballocationList.iterator FreeSuballocation(BlockMetadata_Generic* @this, SuballocationList.iterator suballocItem)
        {
            // Change this suballocation to be marked as free.
            Suballocation* suballoc = suballocItem.op_Arrow();
            suballoc->type = SUBALLOCATION_TYPE_FREE;
            suballoc->userData = null;

            // Update totals.
            ++@this->m_FreeCount;
            @this->m_SumFreeSize += suballoc->size;

            // Merge with previous and/or next suballocation if it's also free.
            bool mergeWithNext = false;
            bool mergeWithPrev = false;

            SuballocationList.iterator nextItem = suballocItem;
            nextItem.op_MoveNext();
            if ((nextItem != @this->m_Suballocations.end()) && (nextItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE))
            {
                mergeWithNext = true;
            }

            SuballocationList.iterator prevItem = suballocItem;
            if (suballocItem != @this->m_Suballocations.begin())
            {
                prevItem.op_MoveBack();
                if (prevItem.op_Arrow()->type == SUBALLOCATION_TYPE_FREE)
                {
                    mergeWithPrev = true;
                }
            }

            if (mergeWithNext)
            {
                @this->UnregisterFreeSuballocation(nextItem);
                @this->MergeFreeWithNext(suballocItem);
            }

            if (mergeWithPrev)
            {
                @this->UnregisterFreeSuballocation(prevItem);
                @this->MergeFreeWithNext(prevItem);
                @this->RegisterFreeSuballocation(prevItem);
                return prevItem;
            }
            else
            {
                @this->RegisterFreeSuballocation(suballocItem);
                return suballocItem;
            }
        }

        public static void RegisterFreeSuballocation(BlockMetadata_Generic* @this, SuballocationList.iterator item)
        {
            D3D12MA_ASSERT(item.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);
            D3D12MA_ASSERT(item.op_Arrow()->size > 0);

            // You may want to enable this validation at the beginning or at the end of
            // this function, depending on what do you want to check.
            D3D12MA_HEAVY_ASSERT(@this->ValidateFreeSuballocationList());

            if (item.op_Arrow()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER)
            {
                if (@this->m_FreeSuballocationsBySize.empty())
                {
                    @this->m_FreeSuballocationsBySize.push_back(&item);
                }
                else
                {
                    @this->m_FreeSuballocationsBySize.InsertSorted(&item, new SuballocationItemSizeLess());
                }
            }

            //D3D12MA_HEAVY_ASSERT(ValidateFreeSuballocationList());
        }

        public static void UnregisterFreeSuballocation(BlockMetadata_Generic* @this, SuballocationList.iterator item)
        {
            D3D12MA_ASSERT(item.op_Arrow()->type == SUBALLOCATION_TYPE_FREE);
            D3D12MA_ASSERT(item.op_Arrow()->size > 0);

            // You may want to enable this validation at the beginning or at the end of
            // this function, depending on what do you want to check.
            D3D12MA_HEAVY_ASSERT(@this->ValidateFreeSuballocationList());

            if (item.op_Arrow()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER)
            {
                SuballocationList.iterator* it = BinaryFindFirstNotLess(
                     @this->m_FreeSuballocationsBySize.data(),
                     @this->m_FreeSuballocationsBySize.data() + @this->m_FreeSuballocationsBySize.size(),
                     &item,
                     new SuballocationItemSizeLess());
                for (nuint index = (nuint)(it - @this->m_FreeSuballocationsBySize.data());
                     index < @this->m_FreeSuballocationsBySize.size();
                     ++index)
                {
                    if (*@this->m_FreeSuballocationsBySize[index] == item)
                    {
                        @this->m_FreeSuballocationsBySize.remove(index);
                        return;
                    }
                    D3D12MA_ASSERT((@this->m_FreeSuballocationsBySize[index]->op_Arrow()->size == item.op_Arrow()->size));
                }
                D3D12MA_ASSERT(0);
            }

            //D3D12MA_HEAVY_ASSERT(ValidateFreeSuballocationList());
        }

        public static void SetAllocationUserData(BlockMetadata_Generic* @this, ulong offset, void* userData)
        {
            for (SuballocationList.iterator suballocItem = @this->m_Suballocations.begin();
                 suballocItem != @this->m_Suballocations.end();
                 suballocItem.op_MoveNext())
            {
                Suballocation* suballoc = suballocItem.op_Arrow();
                if (suballoc->offset == offset)
                {
                    suballoc->userData = userData;
                    return;
                }
            }
            D3D12MA_ASSERT(0);
        }

        public static void CalcAllocationStatInfo(BlockMetadata_Generic* @this, StatInfo* outInfo)
        {
            outInfo->BlockCount = 1;

            uint rangeCount = (uint)@this->m_Suballocations.size();
            outInfo->AllocationCount = rangeCount - @this->m_FreeCount;
            outInfo->UnusedRangeCount = @this->m_FreeCount;

            outInfo->UsedBytes = @this->@base.GetSize() - @this->m_SumFreeSize;
            outInfo->UnusedBytes = @this->m_SumFreeSize;

            outInfo->AllocationSizeMin = UINT64_MAX;
            outInfo->AllocationSizeMax = 0;
            outInfo->UnusedRangeSizeMin = UINT64_MAX;
            outInfo->UnusedRangeSizeMax = 0;

            for (SuballocationList.iterator suballocItem = @this->m_Suballocations.begin();
                 suballocItem != @this->m_Suballocations.end();
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

        public static void WriteAllocationInfoToJson(BlockMetadata_Generic* @this, JsonWriter* json)
        {
            json->BeginObject();
            json->WriteString("TotalBytes");
            json->WriteNumber(@this->@base.GetSize());
            json->WriteString("UnusuedBytes");
            json->WriteNumber(@this->GetSumFreeSize());
            json->WriteString("Allocations");
            json->WriteNumber(@this->GetAllocationCount());
            json->WriteString("UnusedRanges");
            json->WriteNumber(@this->m_FreeCount);
            json->WriteString("Suballocations");
            json->BeginArray();
            for (SuballocationList.iterator suballocItem = @this->m_Suballocations.begin();
                 suballocItem != @this->m_Suballocations.end();
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
                else if (@this->@base.IsVirtual())
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
                    D3D12MA_ASSERT(alloc);
                    json->AddAllocationToObject(alloc);
                }
                json->EndObject();
            }
            json->EndArray();
            json->EndObject();
        }
    }
}
