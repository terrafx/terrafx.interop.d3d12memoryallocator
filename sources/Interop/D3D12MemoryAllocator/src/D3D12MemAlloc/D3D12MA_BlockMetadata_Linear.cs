// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using static TerraFX.Interop.DirectX.D3D12MA_BlockMetadata_Linear.ALLOC_REQUEST_TYPE;
using static TerraFX.Interop.DirectX.D3D12MA_BlockMetadata_Linear.SECOND_VECTOR_MODE;
using static TerraFX.Interop.DirectX.D3D12MA_SuballocationType;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using System.Diagnostics.CodeAnalysis;

namespace TerraFX.Interop.DirectX;

[NativeTypeName("class D3D12MA::BlockMetadata_Linear : D3D12MA::BlockMetadata")]
[NativeInheritance("D3D12MA::BlockMetadata")]
internal unsafe partial struct D3D12MA_BlockMetadata_Linear : D3D12MA_BlockMetadata.Interface
{
    public D3D12MA_BlockMetadata Base;

    [NativeTypeName("UINT64")]
    private ulong m_SumFreeSize;

    [NativeTypeName("SuballocationVectorType")]
    private D3D12MA_Vector<D3D12MA_Suballocation> m_Suballocations0;

    [NativeTypeName("SuballocationVectorType")]
    private D3D12MA_Vector<D3D12MA_Suballocation> m_Suballocations1;

    [NativeTypeName("UINT32")]
    private uint m_1stVectorIndex;

    private SECOND_VECTOR_MODE m_2ndVectorMode;

    // Number of items in 1st vector with hAllocation = null at the beginning.
    [NativeTypeName("size_t")]
    private nuint m_1stNullItemsBeginCount;

    // Number of other items in 1st vector with hAllocation = null somewhere in the middle.
    [NativeTypeName("size_t")]
    private nuint m_1stNullItemsMiddleCount;

    // Number of items in 2nd vector with hAllocation = null.
    [NativeTypeName("size_t")]
    private nuint m_2ndNullItemsCount;

    public static D3D12MA_BlockMetadata_Linear* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
    {
        D3D12MA_BlockMetadata_Linear* result = D3D12MA_NEW<D3D12MA_BlockMetadata_Linear>(allocs);
        result->_ctor(allocationCallbacks, isVirtual);
        return result;
    }

    private void _ctor([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
    {
        Base = new D3D12MA_BlockMetadata(allocationCallbacks, isVirtual) {
            lpVtbl = VtblInstance,
        };

        m_SumFreeSize = 0;

        m_Suballocations0 = new D3D12MA_Vector<D3D12MA_Suballocation>(*allocationCallbacks);
        m_Suballocations1 = new D3D12MA_Vector<D3D12MA_Suballocation>(*allocationCallbacks);

        m_1stVectorIndex = 0;
        m_2ndVectorMode = SECOND_VECTOR_EMPTY;
        m_1stNullItemsBeginCount = 0;
        m_1stNullItemsMiddleCount = 0;
        m_2ndNullItemsCount = 0;

        D3D12MA_ASSERT(allocationCallbacks != null);
    }

    [VtblIndex(0)]
    public void Dispose()
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, void>)(Base.lpVtbl[0]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref this));
    }

    [VtblIndex(1)]
    public void Init([NativeTypeName("UINT64")] ulong size)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, void>)(Base.lpVtbl[1]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref this), size);
    }

    [VtblIndex(2)]
    public readonly bool Validate()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, byte>)(Base.lpVtbl[2]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this))) != 0;
    }

    [VtblIndex(3)]
    [return: NativeTypeName("size_t")]
    public readonly nuint GetAllocationCount()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, nuint>)(Base.lpVtbl[3]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(4)]
    [return: NativeTypeName("size_t")]
    public readonly nuint GetFreeRegionsCount()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, nuint>)(Base.lpVtbl[4]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(5)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSumFreeSize()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong>)(Base.lpVtbl[5]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(6)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetAllocationOffset([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, ulong>)(Base.lpVtbl[6]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle);
    }

    [VtblIndex(7)]
    public readonly bool IsEmpty()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, byte>)(Base.lpVtbl[7]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this))) != 0;
    }

    [VtblIndex(8)]
    public readonly void GetAllocationInfo([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO &")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)(Base.lpVtbl[8]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle, outInfo);
    }

    [VtblIndex(9)]
    public bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, bool upperAddress, [NativeTypeName("UINT32")] uint strategy, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, ulong, byte, uint, D3D12MA_AllocationRequest*, byte>)(Base.lpVtbl[9]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref this), allocSize, allocAlignment, (byte)(upperAddress ? 1 : 0), strategy, pAllocationRequest) != 0;
    }

    [VtblIndex(10)]
    public void Alloc([NativeTypeName("const D3D12MA::AllocationRequest &")] D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* PrivateData)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, D3D12MA_AllocationRequest*, ulong, void*, void>)(Base.lpVtbl[10]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref this), request, allocSize, PrivateData);
    }

    [VtblIndex(11)]
    public void Free([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, void>)(Base.lpVtbl[11]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref this), allocHandle);
    }

    [VtblIndex(12)]
    public void Clear()
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, void>)(Base.lpVtbl[12]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref this));
    }

    [VtblIndex(13)]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    public readonly ulong GetAllocationListBegin()
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong>)(Base.lpVtbl[13]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(14)]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    public readonly ulong GetNextAllocation([NativeTypeName("D3D12MA::AllocHandle")] ulong prevAlloc)
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, ulong>)(Base.lpVtbl[14]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), prevAlloc);
    }

    [VtblIndex(15)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetNextFreeRegionSize([NativeTypeName("D3D12MA::AllocHandle")] ulong alloc)
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, ulong>)(Base.lpVtbl[15]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), alloc);
    }

    [VtblIndex(16)]
    public readonly void* GetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, void*>)(Base.lpVtbl[16]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle);
    }

    [VtblIndex(17)]
    public void SetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, void* privateData)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, ulong, void*, void>)(Base.lpVtbl[17]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref this), allocHandle, privateData);
    }

    [VtblIndex(18)]
    public readonly void AddStatistics([NativeTypeName("D3D12MA::Statistics &")] D3D12MA_Statistics* inoutStats)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, D3D12MA_Statistics*, void>)(Base.lpVtbl[18]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), inoutStats);
    }

    [VtblIndex(19)]
    public readonly void AddDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] D3D12MA_DetailedStatistics* inoutStats)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, D3D12MA_DetailedStatistics*, void>)(Base.lpVtbl[19]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), inoutStats);
    }

    [VtblIndex(20)]
    public readonly void WriteAllocationInfoToJson([NativeTypeName("D3D12MA::JsonWriter &")] D3D12MA_JsonWriter* json)
    {
        ((delegate* unmanaged[MemberFunction]<D3D12MA_BlockMetadata_Linear*, D3D12MA_JsonWriter*, void>)(Base.lpVtbl[20]))((D3D12MA_BlockMetadata_Linear*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), json);
    }

    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSize()
    {
        return Base.GetSize();
    }

    public readonly bool IsVirtual()
    {
        return Base.IsVirtual();
    }

    [return: NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")]
    private readonly D3D12MA_ALLOCATION_CALLBACKS* GetAllocs()
    {
        return Base.GetAllocs();
    }

    private readonly ulong GetDebugMargin()
    {
        return Base.GetDebugMargin();
    }

    private readonly void PrintDetailedMap_Begin([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json, [NativeTypeName("UINT64")] ulong unusedBytes, [NativeTypeName("size_t")] nuint allocationCount, [NativeTypeName("size_t")] nuint unusedRangeCount)
    {
        Base.PrintDetailedMap_Begin(ref json, unusedBytes, allocationCount, unusedRangeCount);
    }

    private readonly void PrintDetailedMap_Allocation([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json, [NativeTypeName("UINT64")] ulong offset, [NativeTypeName("UINT64")] ulong size, void* privateData)
    {
        Base.PrintDetailedMap_Allocation(ref json, offset, size, privateData);
    }

    private static void PrintDetailedMap_UnusedRange([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json, [NativeTypeName("UINT64")] ulong offset, [NativeTypeName("UINT64")] ulong size)
    {
        D3D12MA_BlockMetadata.PrintDetailedMap_UnusedRange(ref json, offset, size);
    }

    private static void PrintDetailedMap_End([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json)
    {
        D3D12MA_BlockMetadata.PrintDetailedMap_End(ref json);
    }

    [UnscopedRef]
    [return: NativeTypeName("D3D12MA::SuballocationVectorType &")]
    private ref D3D12MA_Vector<D3D12MA_Suballocation> AccessSuballocations1st()
    {
        return ref ((m_1stVectorIndex != 0) ? ref m_Suballocations1 : ref m_Suballocations0);
    }

    [UnscopedRef]
    [return: NativeTypeName("D3D12MA::SuballocationVectorType &")]
    private ref D3D12MA_Vector<D3D12MA_Suballocation> AccessSuballocations2nd()
    {
        return ref ((m_1stVectorIndex != 0) ? ref m_Suballocations0 : ref m_Suballocations1);
    }

    private readonly ref D3D12MA_Suballocation FindSuballocation([NativeTypeName("UINT64")] ulong offset)
    {
        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref Unsafe.AsRef(in this).AccessSuballocations1st();
        ref readonly D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref Unsafe.AsRef(in this).AccessSuballocations2nd();

        D3D12MA_Suballocation refSuballoc = new D3D12MA_Suballocation {
            offset = offset,
        };
        // Rest of members stays uninitialized intentionally for better performance.

        // Item from the 1st vector.
        {
            D3D12MA_Suballocation* it = D3D12MA_BinaryFindSorted(suballocations1st.cbegin() + m_1stNullItemsBeginCount, suballocations1st.cend(), refSuballoc, new D3D12MA_SuballocationOffsetLess());

            if (it != suballocations1st.cend())
            {
                return ref *it;
            }
        }

        if (m_2ndVectorMode != SECOND_VECTOR_EMPTY)
        {
            // Rest of members stays uninitialized intentionally for better performance.
            D3D12MA_Suballocation* it = m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER ? D3D12MA_BinaryFindSorted(suballocations2nd.cbegin(), suballocations2nd.cend(), refSuballoc, new D3D12MA_SuballocationOffsetLess()) : D3D12MA_BinaryFindSorted(suballocations2nd.cbegin(), suballocations2nd.cend(), refSuballoc, new D3D12MA_SuballocationOffsetGreater());

            if (it != suballocations2nd.cend())
            {
                return ref *it;
            }
        }

        D3D12MA_FAIL("Allocation not found in linear allocator!");

        return ref *suballocations1st.crbegin(); // Should never occur.
    }

    private readonly bool ShouldCompact1st()
    {
        nuint nullItemCount = m_1stNullItemsBeginCount + m_1stNullItemsMiddleCount;
        nuint suballocCount = Unsafe.AsRef(in this).AccessSuballocations1st().size();

        return (suballocCount > 32) && ((nullItemCount * 2) >= ((suballocCount - nullItemCount) * 3));
    }

    private void CleanupAfterFree()
    {
        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref AccessSuballocations1st();
        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref AccessSuballocations2nd();

        if (IsEmpty())
        {
            suballocations1st.clear();
            suballocations2nd.clear();

            m_1stNullItemsBeginCount = 0;
            m_1stNullItemsMiddleCount = 0;
            m_2ndNullItemsCount = 0;

            m_2ndVectorMode = SECOND_VECTOR_EMPTY;
        }
        else
        {
            nuint suballoc1stCount = suballocations1st.size();
            nuint nullItem1stCount = m_1stNullItemsBeginCount + m_1stNullItemsMiddleCount;

            D3D12MA_ASSERT(nullItem1stCount <= suballoc1stCount);

            // Find more null items at the beginning of 1st vector.
            while (m_1stNullItemsBeginCount < suballoc1stCount &&
                suballocations1st[m_1stNullItemsBeginCount].type == D3D12MA_SUBALLOCATION_TYPE_FREE)
            {
                ++m_1stNullItemsBeginCount;
                --m_1stNullItemsMiddleCount;
            }

            // Find more null items at the end of 1st vector.
            while ((m_1stNullItemsMiddleCount > 0) && (suballocations1st.back().type == D3D12MA_SUBALLOCATION_TYPE_FREE))
            {
                --m_1stNullItemsMiddleCount;
                suballocations1st.pop_back();
            }

            // Find more null items at the end of 2nd vector.
            while ((m_2ndNullItemsCount > 0) && (suballocations2nd.back().type == D3D12MA_SUBALLOCATION_TYPE_FREE))
            {
                --m_2ndNullItemsCount;
                suballocations2nd.pop_back();
            }

            // Find more null items at the beginning of 2nd vector.
            while ((m_2ndNullItemsCount > 0) && (suballocations2nd[0].type == D3D12MA_SUBALLOCATION_TYPE_FREE))
            {
                --m_2ndNullItemsCount;
                suballocations2nd.remove(0);
            }

            if (ShouldCompact1st())
            {
                nuint nonNullItemCount = suballoc1stCount - nullItem1stCount;
                nuint srcIndex = m_1stNullItemsBeginCount;

                for (nuint dstIndex = 0; dstIndex < nonNullItemCount; ++dstIndex)
                {
                    while (suballocations1st[srcIndex].type == D3D12MA_SUBALLOCATION_TYPE_FREE)
                    {
                        ++srcIndex;
                    }

                    if (dstIndex != srcIndex)
                    {
                        suballocations1st[dstIndex] = suballocations1st[srcIndex];
                    }

                    ++srcIndex;
                }

                suballocations1st.resize(nonNullItemCount);

                m_1stNullItemsBeginCount = 0;
                m_1stNullItemsMiddleCount = 0;
            }

            // 2nd vector became empty.
            if (suballocations2nd.empty())
            {
                m_2ndVectorMode = SECOND_VECTOR_EMPTY;
            }

            // 1st vector became empty.
            if (suballocations1st.size() - m_1stNullItemsBeginCount == 0)
            {
                suballocations1st.clear();
                m_1stNullItemsBeginCount = 0;

                if (!suballocations2nd.empty() && m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER)
                {
                    // Swap 1st with 2nd. Now 2nd is empty.
                    m_2ndVectorMode = SECOND_VECTOR_EMPTY;
                    m_1stNullItemsMiddleCount = m_2ndNullItemsCount;

                    while ((m_1stNullItemsBeginCount < suballocations2nd.size()) && (suballocations2nd[m_1stNullItemsBeginCount].type == D3D12MA_SUBALLOCATION_TYPE_FREE))
                    {
                        ++m_1stNullItemsBeginCount;
                        --m_1stNullItemsMiddleCount;
                    }

                    m_2ndNullItemsCount = 0;
                    m_1stVectorIndex ^= 1;
                }
            }
        }

        D3D12MA_HEAVY_ASSERT(Validate());
    }

    private bool CreateAllocationRequest_LowerAddress([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        ulong blockSize = GetSize();

        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref AccessSuballocations1st();
        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref AccessSuballocations2nd();

        if ((m_2ndVectorMode == SECOND_VECTOR_EMPTY) || (m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK))
        {
            // Try to allocate at the end of 1st vector.
            ulong resultBaseOffset = 0;

            if (!suballocations1st.empty())
            {
                ref readonly D3D12MA_Suballocation lastSuballoc = ref suballocations1st.back();
                resultBaseOffset = lastSuballoc.offset + lastSuballoc.size + GetDebugMargin();
            }

            // Start from offset equal to beginning of free space.
            ulong resultOffset = resultBaseOffset;

            // Apply alignment.
            resultOffset = D3D12MA_AlignUp(resultOffset, allocAlignment);
            ulong freeSpaceEnd = (m_2ndVectorMode == SECOND_VECTOR_DOUBLE_STACK) ? suballocations2nd.back().offset : blockSize;

            // There is enough free space at the end after alignment.
            if ((resultOffset + allocSize + GetDebugMargin()) <= freeSpaceEnd)
            {
                // All tests passed: Success.
                pAllocationRequest->allocHandle = (ulong)(resultOffset + 1);

                // pAllocationRequest->item, customData unused.
                pAllocationRequest->algorithmData = (uint)(ALLOC_REQUEST_END_OF_1ST);
                return true;
            }
        }

        // Wrap-around to end of 2nd vector. Try to allocate there, watching for the
        // beginning of 1st vector as the end of free space.
        if ((m_2ndVectorMode == SECOND_VECTOR_EMPTY) || (m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER))
        {
            D3D12MA_ASSERT(!suballocations1st.empty());
            ulong resultBaseOffset = 0;

            if (!suballocations2nd.empty())
            {
                ref readonly D3D12MA_Suballocation lastSuballoc = ref suballocations2nd.back();
                resultBaseOffset = lastSuballoc.offset + lastSuballoc.size + GetDebugMargin();
            }

            // Start from offset equal to beginning of free space.
            ulong resultOffset = resultBaseOffset;

            // Apply alignment.
            resultOffset = D3D12MA_AlignUp(resultOffset, allocAlignment);
            nuint index1st = m_1stNullItemsBeginCount;

            // There is enough free space at the end after alignment.
            if (((index1st == suballocations1st.size()) && ((resultOffset + allocSize + GetDebugMargin()) <= blockSize)) || ((index1st < suballocations1st.size()) && ((resultOffset + allocSize + GetDebugMargin()) <= suballocations1st[index1st].offset)))
            {
                // All tests passed: Success.
                pAllocationRequest->allocHandle = (ulong)(resultOffset + 1);
                pAllocationRequest->algorithmData = (uint)(ALLOC_REQUEST_END_OF_2ND);

                // pAllocationRequest->item, customData unused.
                return true;
            }
        }

        return false;
    }

    private bool CreateAllocationRequest_UpperAddress([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        ulong blockSize = GetSize();

        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations1st = ref AccessSuballocations1st();
        ref D3D12MA_Vector<D3D12MA_Suballocation> suballocations2nd = ref AccessSuballocations2nd();

        if (m_2ndVectorMode == SECOND_VECTOR_RING_BUFFER)
        {
            D3D12MA_FAIL("Trying to use pool with linear algorithm as double stack, while it is already being used as ring buffer.");
            return false;
        }

        // Try to allocate before 2nd.back(), or end of block if 2nd.empty().
        if (allocSize > blockSize)
        {
            return false;
        }

        ulong resultBaseOffset = blockSize - allocSize;

        if (!suballocations2nd.empty())
        {
            ref readonly D3D12MA_Suballocation lastSuballoc = ref suballocations2nd.back();
            resultBaseOffset = lastSuballoc.offset - allocSize;

            if (allocSize > lastSuballoc.offset)
            {
                return false;
            }
        }

        // Start from offset equal to end of free space.
        ulong resultOffset = resultBaseOffset;

        // Apply debugMargin at the end.
        if (GetDebugMargin() > 0)
        {
            if (resultOffset < GetDebugMargin())
            {
                return false;
            }
            resultOffset -= GetDebugMargin();
        }

        // Apply alignment.
        resultOffset = D3D12MA_AlignDown(resultOffset, allocAlignment);

        // There is enough free space.
        ulong endOf1st = !suballocations1st.empty() ? (suballocations1st.back().offset + suballocations1st.back().size) : 0;

        if (endOf1st + GetDebugMargin() <= resultOffset)
        {
            // All tests passed: Success.
            pAllocationRequest->allocHandle = (ulong)(resultOffset + 1);

            // pAllocationRequest->item unused.
            pAllocationRequest->algorithmData = (uint)(ALLOC_REQUEST_UPPER_ADDRESS);
            return true;
        }

        return false;
    }

    internal enum ALLOC_REQUEST_TYPE
    {
        ALLOC_REQUEST_UPPER_ADDRESS,

        ALLOC_REQUEST_END_OF_1ST,

        ALLOC_REQUEST_END_OF_2ND,
    }

    internal enum SECOND_VECTOR_MODE
    {
        SECOND_VECTOR_EMPTY,

        // Suballocations in 2nd vector are created later than the ones in 1st, but they all have smaller offset.
        SECOND_VECTOR_RING_BUFFER,

        // Suballocations in 2nd vector are upper side of double stack.
        // They all have offsets higher than those in 1st vector.
        // Top of this stack means smaller offsets, but higher indices in this vector.
        SECOND_VECTOR_DOUBLE_STACK,
    }
}
