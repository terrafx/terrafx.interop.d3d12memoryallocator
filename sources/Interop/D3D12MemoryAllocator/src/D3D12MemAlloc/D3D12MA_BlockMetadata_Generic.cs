// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemAlloc;
using static TerraFX.Interop.D3D12MA_SuballocationType;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_BlockMetadata_Generic : IDisposable
    {
        private static readonly void** Vtbl = InitVtbl();

        private static void** InitVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_BlockMetadata_Generic), sizeof(void*) * 15);

            /* Finalizer                 */ lpVtbl[0] = (delegate*<ref D3D12MA_BlockMetadata_Generic, void>)&Dispose;
            /* Init                      */ lpVtbl[1] = (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, void>)&Init;
            /* Validate                  */ lpVtbl[2] = (delegate*<in D3D12MA_BlockMetadata_Generic, bool>)&Validate;
            /* GetAllocationCount        */ lpVtbl[3] = (delegate*<in D3D12MA_BlockMetadata_Generic, nuint>)&GetAllocationCount;
            /* GetSumFreeSize            */ lpVtbl[4] = (delegate*<in D3D12MA_BlockMetadata_Generic, ulong>)&GetSumFreeSize;
            /* GetUnusedRangeSizeMax     */ lpVtbl[5] = (delegate*<in D3D12MA_BlockMetadata_Generic, ulong>)&GetUnusedRangeSizeMax;
            /* IsEmpty                   */ lpVtbl[6] = (delegate*<in D3D12MA_BlockMetadata_Generic, bool>)&IsEmpty;
            /* GetAllocationInfo         */ lpVtbl[7] = (delegate*<in D3D12MA_BlockMetadata_Generic, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)&GetAllocationInfo;
            /* CreateAllocationRequest   */ lpVtbl[8] = (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, ulong, D3D12MA_AllocationRequest*, bool>)&CreateAllocationRequest;
            /* Alloc                     */ lpVtbl[9] = (delegate*<ref D3D12MA_BlockMetadata_Generic, D3D12MA_AllocationRequest*, ulong, void*, void>)&Alloc;
            /* FreeAtOffset              */ lpVtbl[10] = (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, void>)&FreeAtOffset;
            /* Clear                     */ lpVtbl[11] = (delegate*<ref D3D12MA_BlockMetadata_Generic, void>)&Clear;
            /* SetAllocationUserData     */ lpVtbl[12] = (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, void*, void>)&SetAllocationUserData;
            /* CalcAllocationStatInfo    */ lpVtbl[13] = (delegate*<in D3D12MA_BlockMetadata_Generic, D3D12MA_StatInfo*, void>)&CalcAllocationStatInfo;
            /* WriteAllocationInfoToJson */ lpVtbl[14] = (delegate*<in D3D12MA_BlockMetadata_Generic, D3D12MA_JsonWriter*, void>)&WriteAllocationInfoToJson;

            return lpVtbl;
        }

        private D3D12MA_BlockMetadata Base;

        [NativeTypeName("UINT")]
        private uint m_FreeCount;

        [NativeTypeName("UINT")]
        private ulong m_SumFreeSize;

        internal D3D12MA_List<D3D12MA_Suballocation> m_Suballocations;

        // Suballocations that are free and have size greater than certain threshold.
        // Sorted by size, ascending.
        private D3D12MA_Vector<D3D12MA_List<D3D12MA_Suballocation>.iterator> m_FreeSuballocationsBySize;

        private D3D12MA_ZeroInitializedRange m_ZeroInitializedRange;

        internal static void _ctor(ref D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
        {
            pThis.Base = new D3D12MA_BlockMetadata(allocationCallbacks, isVirtual);
            pThis.Base.lpVtbl = Vtbl;

            pThis.m_FreeCount = 0;
            pThis.m_SumFreeSize = 0;
            D3D12MA_List<D3D12MA_Suballocation>._ctor(ref pThis.m_Suballocations, allocationCallbacks);
            D3D12MA_Vector<D3D12MA_List<D3D12MA_Suballocation>.iterator>._ctor(ref pThis.m_FreeSuballocationsBySize, allocationCallbacks);
            Unsafe.SkipInit(out pThis.m_ZeroInitializedRange);

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocationCallbacks != null));
        }

        public void Dispose()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[0] == (delegate*<ref D3D12MA_BlockMetadata_Generic, void>)&Dispose));

            Dispose(ref this);

            m_FreeSuballocationsBySize.Dispose();
            m_Suballocations.Dispose();
        }

        public void Init([NativeTypeName("UINT64")] ulong size)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[1] == (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, void>)&Init));

            Init(ref this, size);
        }

        public readonly bool Validate()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[2] == (delegate*<in D3D12MA_BlockMetadata_Generic, bool>)&Validate));

            return Validate(in this);
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => Base.GetSize();

        public readonly bool IsVirtual() => Base.IsVirtual();

        [return: NativeTypeName("size_t")]
        public readonly nuint GetAllocationCount()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[3] == (delegate*<in D3D12MA_BlockMetadata_Generic, nuint>)&GetAllocationCount));

            return GetAllocationCount(in this);
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSumFreeSize()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[4] == (delegate*<in D3D12MA_BlockMetadata_Generic, ulong>)&GetSumFreeSize));

            return GetSumFreeSize(in this);
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetUnusedRangeSizeMax()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[5] == (delegate*<in D3D12MA_BlockMetadata_Generic, ulong>)&GetUnusedRangeSizeMax));

            return GetUnusedRangeSizeMax(in this);
        }

        public readonly bool IsEmpty()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[6] == (delegate*<in D3D12MA_BlockMetadata_Generic, bool>)&IsEmpty));

            return IsEmpty(in this);
        }

        public readonly void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, [NativeTypeName("D3D12MA_VIRTUAL_ALLOCATION_INFO&")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[7] == (delegate*<in D3D12MA_BlockMetadata_Generic, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)&GetAllocationInfo));

            GetAllocationInfo(in this, offset, outInfo);
        }

        public bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, D3D12MA_AllocationRequest* pAllocationRequest)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[8] == (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, ulong, D3D12MA_AllocationRequest*, bool>)&CreateAllocationRequest));

            return CreateAllocationRequest(ref this, allocSize, allocAlignment, pAllocationRequest);
        }

        public void Alloc([NativeTypeName("const D3D12MA_AllocationRequest&")] D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* userData)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[9] == (delegate*<ref D3D12MA_BlockMetadata_Generic, D3D12MA_AllocationRequest*, ulong, void*, void>)&Alloc));

            Alloc(ref this, request, allocSize, userData);
        }

        public void FreeAtOffset([NativeTypeName("UINT64")] ulong offset)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[10] == (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, void>)&FreeAtOffset));

            FreeAtOffset(ref this, offset);
        }

        public void Clear()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[11] == (delegate*<ref D3D12MA_BlockMetadata_Generic, void>)&Clear));

            Clear(ref this);
        }

        public void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* userData)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[12] == (delegate*<ref D3D12MA_BlockMetadata_Generic, ulong, void*, void>)&SetAllocationUserData));

            SetAllocationUserData(ref this, offset, userData);
        }

        public readonly void CalcAllocationStatInfo([NativeTypeName("StatInfo&")] D3D12MA_StatInfo* outInfo)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[13] == (delegate*<in D3D12MA_BlockMetadata_Generic, D3D12MA_StatInfo*, void>)&CalcAllocationStatInfo));

            CalcAllocationStatInfo(in this, outInfo);
        }

        public readonly void WriteAllocationInfoToJson([NativeTypeName("JsonWriter&")] D3D12MA_JsonWriter* json)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (Base.lpVtbl[14] == (delegate*<in D3D12MA_BlockMetadata_Generic, D3D12MA_JsonWriter*, void>)&WriteAllocationInfoToJson));

            WriteAllocationInfoToJson(in this, json);
        }

        public readonly bool ValidateFreeSuballocationList()
        {
            ulong lastSize = 0;

            for (nuint i = 0, count = m_FreeSuballocationsBySize.size(); i < count; ++i)
            {
                D3D12MA_List<D3D12MA_Suballocation>.iterator it = *m_FreeSuballocationsBySize[i];

                _ = D3D12MA_VALIDATE(it.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE);
                _ = D3D12MA_VALIDATE(it.Get()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER);
                _ = D3D12MA_VALIDATE(it.Get()->size >= lastSize);

                lastSize = it.Get()->size;
            }

            return true;
        }

        /// <summary>Checks if requested suballocation with given parameters can be placed in given pFreeSuballocItem. If yes, fills pOffset and returns true. If no, returns false.</summary>
        public readonly bool CheckAllocation([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem, [NativeTypeName("UINT64*")] ulong* pOffset, [NativeTypeName("UINT64*")] ulong* pSumFreeSize, [NativeTypeName("UINT64*")] ulong* pSumItemSize, [NativeTypeName("BOOL")] int* pZeroInitialized)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocSize > 0));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballocItem != m_Suballocations.end()));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pOffset != null) && (pZeroInitialized != null));

            *pSumFreeSize = 0;
            *pSumItemSize = 0;
            *pZeroInitialized = FALSE;

            D3D12MA_Suballocation* suballoc = suballocItem.Get();
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballoc->type == D3D12MA_SUBALLOCATION_TYPE_FREE));

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
            *pOffset = AlignUp(*pOffset, allocAlignment);

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
        public void MergeFreeWithNext(D3D12MA_List<D3D12MA_Suballocation>.iterator item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item != m_Suballocations.end()));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE));

            D3D12MA_List<D3D12MA_Suballocation>.iterator nextItem = item;
            nextItem = nextItem.MoveNext();

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (nextItem != m_Suballocations.end()));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (nextItem.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE));

            item.Get()->size += nextItem.Get()->size;
            --m_FreeCount;

            m_Suballocations.erase(nextItem);
        }

        /// <summary>Releases given suballocation, making it free. Merges it with adjacent free suballocations if applicable. Returns iterator to new free suballocation at this place.</summary>
        public D3D12MA_List<D3D12MA_Suballocation>.iterator FreeSuballocation(D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem)
        {
            // Change this suballocation to be marked as free.
            D3D12MA_Suballocation* suballoc = suballocItem.Get();
            suballoc->type = D3D12MA_SUBALLOCATION_TYPE_FREE;
            suballoc->userData = null;

            // Update totals.
            ++m_FreeCount;
            m_SumFreeSize += suballoc->size;

            // Merge with previous and/or next suballocation if it's also free.
            bool mergeWithNext = false;
            bool mergeWithPrev = false;

            D3D12MA_List<D3D12MA_Suballocation>.iterator nextItem = suballocItem;
            nextItem = nextItem.MoveNext();

            if ((nextItem != m_Suballocations.end()) && (nextItem.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE))
            {
                mergeWithNext = true;
            }

            D3D12MA_List<D3D12MA_Suballocation>.iterator prevItem = suballocItem;

            if (suballocItem != m_Suballocations.begin())
            {
                prevItem = prevItem.MoveBack();

                if (prevItem.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE)
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
        public void RegisterFreeSuballocation(D3D12MA_List<D3D12MA_Suballocation>.iterator item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.Get()->size > 0));

            // You may want to enable this validation at the beginning or at the end of
            // this function, depending on what do you want to check.
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && ValidateFreeSuballocationList());

            if (item.Get()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER)
            {
                if (m_FreeSuballocationsBySize.empty())
                {
                    m_FreeSuballocationsBySize.push_back(in item);
                }
                else
                {
                    _ = m_FreeSuballocationsBySize.InsertSorted(in item, new D3D12MA_SuballocationItemSizeLess());
                }
            }

            // D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && ValidateFreeSuballocatioNList());
        }

        /// <summary>Given free suballocation, it removes it from sorted list of <see cref="m_FreeSuballocationsBySize"/> if it's suitable.</summary>
        public void UnregisterFreeSuballocation(D3D12MA_List<D3D12MA_Suballocation>.iterator item)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (item.Get()->size > 0));

            // You may want to enable this validation at the beginning or at the end of
            // this function, depending on what do you want to check.
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && ValidateFreeSuballocationList());

            if (item.Get()->size >= MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER)
            {
                D3D12MA_List<D3D12MA_Suballocation>.iterator* it = BinaryFindFirstNotLess(
                     m_FreeSuballocationsBySize.data(),
                     m_FreeSuballocationsBySize.data() + m_FreeSuballocationsBySize.size(),
                     in item,
                     new D3D12MA_SuballocationItemSizeLess()
                );

                for (nuint index = (nuint)(it - m_FreeSuballocationsBySize.data()); index < m_FreeSuballocationsBySize.size(); ++index)
                {
                    if (*m_FreeSuballocationsBySize[index] == item)
                    {
                        m_FreeSuballocationsBySize.remove(index);
                        return;
                    }

                    D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_FreeSuballocationsBySize[index]->Get()->size == item.Get()->size)); // "Not found!"
                }

                D3D12MA_ASSERT(false); // "Not found!"
            }

            // D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && ValidateFreeSuballocationList());
        }

        public static void Dispose(ref D3D12MA_BlockMetadata_Generic pThis)
        {
            D3D12MA_BlockMetadata.Dispose(ref pThis.Base);
        }

        public static void Init(ref D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("UINT64")] ulong size)
        {
            D3D12MA_BlockMetadata.Init(ref pThis.Base, size);
            pThis.m_ZeroInitializedRange.Reset(size);

            pThis.m_FreeCount = 1;
            pThis.m_SumFreeSize = size;

            D3D12MA_Suballocation suballoc = default;
            suballoc.offset = 0;
            suballoc.size = size;
            suballoc.type = D3D12MA_SUBALLOCATION_TYPE_FREE;
            suballoc.userData = null;

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (size > MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER));
            pThis.m_Suballocations.push_back(in suballoc);

            D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = pThis.m_Suballocations.end();
            suballocItem = suballocItem.MoveBack();

            pThis.m_FreeSuballocationsBySize.push_back(in suballocItem);
        }

        public static bool Validate(in D3D12MA_BlockMetadata_Generic pThis)
        {
            _ = D3D12MA_VALIDATE(!pThis.m_Suballocations.empty());

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

            for (D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = pThis.m_Suballocations.begin(); suballocItem != pThis.m_Suballocations.end(); suballocItem = suballocItem.MoveNext())
            {
                D3D12MA_Suballocation* subAlloc = suballocItem.Get();

                // Actual offset of this suballocation doesn't match expected one.
                _ = D3D12MA_VALIDATE(subAlloc->offset == calculatedOffset);

                bool currFree = subAlloc->type == D3D12MA_SUBALLOCATION_TYPE_FREE;

                // Two adjacent free suballocations are invalid. They should be merged.
                _ = D3D12MA_VALIDATE(!prevFree || !currFree);

                D3D12MA_Allocation* alloc = (D3D12MA_Allocation*)subAlloc->userData;

                if (!pThis.IsVirtual())
                {
                    _ = D3D12MA_VALIDATE(currFree == (alloc == null));
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
                    _ = D3D12MA_VALIDATE(subAlloc->size >= D3D12MA_DEBUG_MARGIN);
                }
                else
                {
                    if (!pThis.IsVirtual())
                    {
                        _ = D3D12MA_VALIDATE(alloc->GetOffset() == subAlloc->offset);
                        _ = D3D12MA_VALIDATE(alloc->GetSize() == subAlloc->size);
                    }

                    // Margin required between allocations - previous allocation must be free.
                    _ = D3D12MA_VALIDATE(D3D12MA_DEBUG_MARGIN == 0 || prevFree);
                }

                calculatedOffset += subAlloc->size;
                prevFree = currFree;
            }

            // Number of free suballocations registered in m_FreeSuballocationsBySize doesn't
            // match expected one.
            _ = D3D12MA_VALIDATE(pThis.m_FreeSuballocationsBySize.size() == freeSuballocationsToRegister);

            ulong lastSize = 0;
            for (nuint i = 0; i < pThis.m_FreeSuballocationsBySize.size(); ++i)
            {
                D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = *pThis.m_FreeSuballocationsBySize[i];

                // Only free suballocations can be registered in m_FreeSuballocationsBySize.
                _ = D3D12MA_VALIDATE(suballocItem.Get()->type == D3D12MA_SUBALLOCATION_TYPE_FREE);

                // They must be sorted by size ascending.
                _ = D3D12MA_VALIDATE(suballocItem.Get()->size >= lastSize);

                lastSize = suballocItem.Get()->size;
            }

            // Check if totals match calculacted values.
            _ = D3D12MA_VALIDATE(pThis.ValidateFreeSuballocationList());
            _ = D3D12MA_VALIDATE(calculatedOffset == pThis.GetSize());
            _ = D3D12MA_VALIDATE(calculatedSumFreeSize == pThis.m_SumFreeSize);
            _ = D3D12MA_VALIDATE(calculatedFreeCount == pThis.m_FreeCount);

            return true;
        }

        public static nuint GetAllocationCount(in D3D12MA_BlockMetadata_Generic pThis)
        {
            return pThis.m_Suballocations.size() - pThis.m_FreeCount;
        }

        public static ulong GetSumFreeSize(in D3D12MA_BlockMetadata_Generic pThis) => pThis.m_SumFreeSize;

        public static ulong GetUnusedRangeSizeMax(in D3D12MA_BlockMetadata_Generic pThis)
        {
            if (!pThis.m_FreeSuballocationsBySize.empty())
            {
                return pThis.m_FreeSuballocationsBySize.back()->Get()->size;
            }
            else
            {
                return 0;
            }
        }

        public static bool IsEmpty(in D3D12MA_BlockMetadata_Generic pThis)
        {
            return (pThis.m_Suballocations.size() == 1) && (pThis.m_FreeCount == 1);
        }

        public static void GetAllocationInfo(in D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("UINT64")] ulong offset, [NativeTypeName("D3D12MA_VIRTUAL_ALLOCATION_INFO&")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            for (D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = pThis.m_Suballocations.begin(); suballocItem != pThis.m_Suballocations.end(); suballocItem = suballocItem.MoveNext())
            {
                D3D12MA_Suballocation* suballoc = suballocItem.Get();

                if (suballoc->offset == offset)
                {
                    outInfo->size = suballoc->size;
                    outInfo->pUserData = suballoc->userData;
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Not found!"
        }

        public static bool CreateAllocationRequest(ref D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, [NativeTypeName("AllocationRequest&")] D3D12MA_AllocationRequest* pAllocationRequest)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocSize > 0));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pAllocationRequest != null));
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && pThis.Validate());

            // There is not enough total free space in this block to fullfill the request: Early return.
            if (pThis.m_SumFreeSize < allocSize + (2 * D3D12MA_DEBUG_MARGIN))
            {
                return false;
            }

            // New algorithm, efficiently searching freeSuballocationsBySize.
            nuint freeSuballocCount = pThis.m_FreeSuballocationsBySize.size();

            if (freeSuballocCount > 0)
            {
                // Find first free suballocation with size not less than allocSize + 2 * D3D12MA_DEBUG_MARGIN.
                D3D12MA_List<D3D12MA_Suballocation>.iterator* it = BinaryFindFirstNotLess(
                    pThis.m_FreeSuballocationsBySize.data(),
                    pThis.m_FreeSuballocationsBySize.data() + freeSuballocCount,
                    allocSize + (2 * D3D12MA_DEBUG_MARGIN),
                    new D3D12MA_SuballocationItemSizeLess()
                );

                nuint index = (nuint)(it - pThis.m_FreeSuballocationsBySize.data());

                for (; index < freeSuballocCount; ++index)
                {
                    if (pThis.CheckAllocation(
                        allocSize,
                        allocAlignment,
                        *pThis.m_FreeSuballocationsBySize[index],
                        &pAllocationRequest->offset,
                        &pAllocationRequest->sumFreeSize,
                        &pAllocationRequest->sumItemSize,
                        &pAllocationRequest->zeroInitialized))
                    {
                        pAllocationRequest->item = *pThis.m_FreeSuballocationsBySize[index];
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Alloc(ref D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("const D3D12MA_AllocationRequest&")] D3D12MA_AllocationRequest* request, ulong allocSize, void* userData)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (request->item != pThis.m_Suballocations.end()));
            D3D12MA_Suballocation* suballoc = request->item.Get();

            // Given suballocation is a free block.
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballoc->type == D3D12MA_SUBALLOCATION_TYPE_FREE));

            // Given offset is inside this suballocation.
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (request->offset >= suballoc->offset));

            ulong paddingBegin = request->offset - suballoc->offset;
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (suballoc->size >= paddingBegin + allocSize));
            ulong paddingEnd = suballoc->size - paddingBegin - allocSize;

            // Unregister this free suballocation from m_FreeSuballocationsBySize and update
            // it to become used.
            pThis.UnregisterFreeSuballocation(request->item);

            suballoc->offset = request->offset;
            suballoc->size = allocSize;
            suballoc->type = D3D12MA_SUBALLOCATION_TYPE_ALLOCATION;
            suballoc->userData = userData;

            // If there are any free bytes remaining at the end, insert new free suballocation after current one.
            if (paddingEnd != 0)
            {
                D3D12MA_Suballocation paddingSuballoc = default;
                paddingSuballoc.offset = request->offset + allocSize;
                paddingSuballoc.size = paddingEnd;
                paddingSuballoc.type = D3D12MA_SUBALLOCATION_TYPE_FREE;

                D3D12MA_List<D3D12MA_Suballocation>.iterator next = request->item;
                next = next.MoveNext();

                D3D12MA_List<D3D12MA_Suballocation>.iterator paddingEndItem = pThis.m_Suballocations.insert(next, in paddingSuballoc);
                pThis.RegisterFreeSuballocation(paddingEndItem);
            }

            // If there are any free bytes remaining at the beginning, insert new free suballocation before current one.
            if (paddingBegin != 0)
            {
                D3D12MA_Suballocation paddingSuballoc = default;
                paddingSuballoc.offset = request->offset - paddingBegin;
                paddingSuballoc.size = paddingBegin;
                paddingSuballoc.type = D3D12MA_SUBALLOCATION_TYPE_FREE;

                D3D12MA_List<D3D12MA_Suballocation>.iterator paddingBeginItem = pThis.m_Suballocations.insert(request->item, in paddingSuballoc);
                pThis.RegisterFreeSuballocation(paddingBeginItem);
            }

            // Update totals.
            pThis.m_FreeCount = pThis.m_FreeCount - 1;

            if (paddingBegin > 0)
            {
                ++pThis.m_FreeCount;
            }

            if (paddingEnd > 0)
            {
                ++pThis.m_FreeCount;
            }

            pThis.m_SumFreeSize -= allocSize;
            pThis.m_ZeroInitializedRange.MarkRangeAsUsed(request->offset, request->offset + allocSize);
        }

        public static void FreeAtOffset(ref D3D12MA_BlockMetadata_Generic pThis, ulong offset)
        {
            for (D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = pThis.m_Suballocations.begin(); suballocItem != pThis.m_Suballocations.end(); suballocItem = suballocItem.MoveNext())
            {
                D3D12MA_Suballocation* suballoc = suballocItem.Get();

                if (suballoc->offset == offset)
                {
                    _ = pThis.FreeSuballocation(suballocItem);
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Not found!"
        }

        public static void Clear(ref D3D12MA_BlockMetadata_Generic pThis)
        {
            pThis.m_FreeCount = 1;
            pThis.m_SumFreeSize = pThis.GetSize();

            pThis.m_Suballocations.clear();

            D3D12MA_Suballocation suballoc = default;
            suballoc.offset = 0;
            suballoc.size = pThis.GetSize();
            suballoc.type = D3D12MA_SUBALLOCATION_TYPE_FREE;

            pThis.m_Suballocations.push_back(in suballoc);
            pThis.m_FreeSuballocationsBySize.clear();

            D3D12MA_List<D3D12MA_Suballocation>.iterator it = pThis.m_Suballocations.begin();
            pThis.m_FreeSuballocationsBySize.push_back(in it);
        }

        public static void SetAllocationUserData(ref D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("UINT64")] ulong offset, void* userData)
        {
            for (D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = pThis.m_Suballocations.begin(); suballocItem != pThis.m_Suballocations.end(); suballocItem = suballocItem.MoveNext())
            {
                D3D12MA_Suballocation* suballoc = suballocItem.Get();

                if (suballoc->offset == offset)
                {
                    suballoc->userData = userData;
                    return;
                }
            }

            D3D12MA_ASSERT(false); // "Not found!"
        }

        public static void CalcAllocationStatInfo(in D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("StatInfo&")] D3D12MA_StatInfo* outInfo)
        {
            outInfo->BlockCount = 1;

            uint rangeCount = (uint)pThis.m_Suballocations.size();
            outInfo->AllocationCount = rangeCount - pThis.m_FreeCount;
            outInfo->UnusedRangeCount = pThis.m_FreeCount;

            outInfo->UsedBytes = pThis.GetSize() - pThis.m_SumFreeSize;
            outInfo->UnusedBytes = pThis.m_SumFreeSize;

            outInfo->AllocationSizeMin = UINT64_MAX;
            outInfo->AllocationSizeMax = 0;

            outInfo->UnusedRangeSizeMin = UINT64_MAX;
            outInfo->UnusedRangeSizeMax = 0;

            for (D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = pThis.m_Suballocations.begin(); suballocItem != pThis.m_Suballocations.end(); suballocItem = suballocItem.MoveNext())
            {
                D3D12MA_Suballocation* suballoc = suballocItem.Get();

                if (suballoc->type == D3D12MA_SUBALLOCATION_TYPE_FREE)
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

        public static void WriteAllocationInfoToJson(in D3D12MA_BlockMetadata_Generic pThis, [NativeTypeName("JsonWriter&")] D3D12MA_JsonWriter* json)
        {
            json->BeginObject();

            json->WriteString("TotalBytes");
            json->WriteNumber(pThis.GetSize());

            json->WriteString("UnusuedBytes");
            json->WriteNumber(pThis.GetSumFreeSize());

            json->WriteString("Allocations");
            json->WriteNumber(pThis.GetAllocationCount());

            json->WriteString("UnusedRanges");
            json->WriteNumber(pThis.m_FreeCount);

            json->WriteString("Suballocations");
            json->BeginArray();

            for (D3D12MA_List<D3D12MA_Suballocation>.iterator suballocItem = pThis.m_Suballocations.begin(); suballocItem != pThis.m_Suballocations.end(); suballocItem = suballocItem.MoveNext())
            {
                D3D12MA_Suballocation* suballoc = suballocItem.Get();

                json->BeginObject(true);

                json->WriteString("Offset");
                json->WriteNumber(suballoc->offset);

                if (suballoc->type == D3D12MA_SUBALLOCATION_TYPE_FREE)
                {
                    json->WriteString("Type");
                    json->WriteString("FREE");
                    json->WriteString("Size");
                    json->WriteNumber(suballoc->size);
                }
                else if (pThis.IsVirtual())
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
                    D3D12MA_Allocation* alloc = (D3D12MA_Allocation*)suballoc->userData;
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
