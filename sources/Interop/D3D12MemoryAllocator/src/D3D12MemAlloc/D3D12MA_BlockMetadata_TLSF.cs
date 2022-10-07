// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

[NativeTypeName("class D3D12MA::BlockMetadata_Linear : D3D12MA::BlockMetadata")]
[NativeInheritance("D3D12MA::BlockMetadata")]
internal unsafe partial struct D3D12MA_BlockMetadata_TLSF : D3D12MA_BlockMetadata.Interface
{
    // According to original paper it should be preferable 4 or 5:
    // M. Masmano, I. Ripoll, A. Crespo, and J. Real "TLSF: a New Dynamic Memory Allocator for Real-Time Systems"
    // http://www.gii.upv.es/tlsf/files/ecrts04_tlsf.pdf

    [NativeTypeName("UINT8")]
    private const byte SECOND_LEVEL_INDEX = 5;

    [NativeTypeName("UINT16")]
    private const ushort SMALL_BUFFER_SIZE = 256;

    [NativeTypeName("UINT")]
    private const uint INITIAL_BLOCK_ALLOC_COUNT = 16;

    [NativeTypeName("UINT8")]
    private const byte MEMORY_CLASS_SHIFT = 7;

    [NativeTypeName("UINT8")]
    private const byte MAX_MEMORY_CLASSES = 65 - MEMORY_CLASS_SHIFT;

    public D3D12MA_BlockMetadata Base;

    [NativeTypeName("size_t")]
    private nuint m_AllocCount;

    // Total number of free blocks besides null block
    [NativeTypeName("size_t")]
    private nuint m_BlocksFreeCount;

    // Total size of free blocks excluding null block

    [NativeTypeName("UINT64")]
    private ulong m_BlocksFreeSize;

    [NativeTypeName("UINT32")]
    private uint m_IsFreeBitmap;

    [NativeTypeName("UINT8")]
    private byte m_MemoryClasses;

    [NativeTypeName("UINT32[MAX_MEMORY_CLASSES")]
    private fixed uint m_InnerIsFreeBitmap[MAX_MEMORY_CLASSES];

    [NativeTypeName("UINT32")]
    private uint m_ListsCount;

    // 0: 0-3 lists for small buffers
    // 1+: 0-(2^SLI-1) lists for normal buffers
    private Pointer<Block>* m_FreeList;

    private D3D12MA_PoolAllocator<Block> m_BlockAllocator;

    private Block* m_NullBlock;

    public static D3D12MA_BlockMetadata_TLSF* Create([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
    {
        D3D12MA_BlockMetadata_TLSF* result = D3D12MA_NEW<D3D12MA_BlockMetadata_TLSF>(allocs);
        result->_ctor(allocationCallbacks, isVirtual);
        return result;
    }

    private void _ctor([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
    {
        Base = new D3D12MA_BlockMetadata(allocationCallbacks, isVirtual) {
            lpVtbl = VtblInstance,
        };

        m_AllocCount = 0;
        m_BlocksFreeCount = 0;
        m_BlocksFreeSize = 0;
        m_IsFreeBitmap = 0;
        m_MemoryClasses = 0;

        MemoryMarshal.CreateSpan(ref m_InnerIsFreeBitmap[0], MAX_MEMORY_CLASSES).Clear();

        m_ListsCount = 0;
        m_FreeList = null;

        m_BlockAllocator = new D3D12MA_PoolAllocator<Block>(*allocationCallbacks, INITIAL_BLOCK_ALLOC_COUNT);

        m_NullBlock = null;
        D3D12MA_ASSERT(allocationCallbacks != null);
    }

    [VtblIndex(0)]
    public void Dispose()
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, void>)(Base.lpVtbl[0]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref this));
    }

    [VtblIndex(1)]
    public void Init([NativeTypeName("UINT64")] ulong size)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, void>)(Base.lpVtbl[1]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref this), size);
    }

    [VtblIndex(2)]
    public readonly bool Validate()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, byte>)(Base.lpVtbl[2]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this))) != 0;
    }

    [VtblIndex(3)]
    [return: NativeTypeName("size_t")]
    public readonly nuint GetAllocationCount()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, nuint>)(Base.lpVtbl[3]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(4)]
    [return: NativeTypeName("size_t")]
    public readonly nuint GetFreeRegionsCount()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, nuint>)(Base.lpVtbl[4]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(5)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSumFreeSize()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong>)(Base.lpVtbl[5]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(6)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetAllocationOffset([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, ulong>)(Base.lpVtbl[6]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle);
    }

    [VtblIndex(7)]
    public readonly bool IsEmpty()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, byte>)(Base.lpVtbl[7]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this))) != 0;
    }

    [VtblIndex(8)]
    public readonly void GetAllocationInfo([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO &")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)(Base.lpVtbl[8]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle, outInfo);
    }

    [VtblIndex(9)]
    public bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, bool upperAddress, [NativeTypeName("UINT32")] uint strategy, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, ulong, byte, uint, D3D12MA_AllocationRequest*, byte>)(Base.lpVtbl[9]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref this), allocSize, allocAlignment, (byte)(upperAddress ? 1 : 0), strategy, pAllocationRequest) != 0;
    }

    [VtblIndex(10)]
    public void Alloc([NativeTypeName("const D3D12MA::AllocationRequest &")] D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* PrivateData)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, D3D12MA_AllocationRequest*, ulong, void*, void>)(Base.lpVtbl[10]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref this), request, allocSize, PrivateData);
    }

    [VtblIndex(11)]
    public void Free([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, void>)(Base.lpVtbl[11]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref this), allocHandle);
    }

    [VtblIndex(12)]
    public void Clear()
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, void>)(Base.lpVtbl[12]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref this));
    }

    [VtblIndex(13)]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    public readonly ulong GetAllocationListBegin()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong>)(Base.lpVtbl[13]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(14)]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    public readonly ulong GetNextAllocation([NativeTypeName("D3D12MA::AllocHandle")] ulong prevAlloc)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, ulong>)(Base.lpVtbl[14]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), prevAlloc);
    }

    [VtblIndex(15)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetNextFreeRegionSize([NativeTypeName("D3D12MA::AllocHandle")] ulong alloc)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, ulong>)(Base.lpVtbl[15]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), alloc);
    }

    [VtblIndex(16)]
    public readonly void* GetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, void*>)(Base.lpVtbl[16]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle);
    }

    [VtblIndex(17)]
    public void SetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, void* privateData)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, ulong, void*, void>)(Base.lpVtbl[17]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref this), allocHandle, privateData);
    }

    [VtblIndex(18)]
    public readonly void AddStatistics([NativeTypeName("D3D12MA::Statistics &")] D3D12MA_Statistics* inoutStats)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, D3D12MA_Statistics*, void>)(Base.lpVtbl[18]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), inoutStats);
    }

    [VtblIndex(19)]
    public readonly void AddDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] D3D12MA_DetailedStatistics* inoutStats)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, D3D12MA_DetailedStatistics*, void>)(Base.lpVtbl[19]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), inoutStats);
    }

    [VtblIndex(20)]
    public readonly void WriteAllocationInfoToJson([NativeTypeName("D3D12MA::JsonWriter &")] D3D12MA_JsonWriter* json)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata_TLSF*, D3D12MA_JsonWriter*, void>)(Base.lpVtbl[20]))((D3D12MA_BlockMetadata_TLSF*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), json);
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

    private readonly void PrintDetailedMap_UnusedRange([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json, [NativeTypeName("UINT64")] ulong offset, [NativeTypeName("UINT64")] ulong size)
    {
        Base.PrintDetailedMap_UnusedRange(ref json, offset, size);
    }

    private readonly void PrintDetailedMap_End([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json)
    {
        Base.PrintDetailedMap_End(ref json);
    }

    [return: NativeTypeName("UINT8")]
    private readonly byte SizeToMemoryClass([NativeTypeName("UINT64")] ulong size)
    {
        if (size > SMALL_BUFFER_SIZE)
        {
            return (byte)(D3D12MA_BitScanMSB(size) - MEMORY_CLASS_SHIFT);
        }
        return 0;
    }

    [return: NativeTypeName("UINT16")]
    private readonly ushort SizeToSecondIndex([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT8")] byte memoryClass)
    {
        if (memoryClass == 0)
        {
            if (IsVirtual())
            {
                return (ushort)((size - 1) / 8);
            }
            else
            {
                return (ushort)((size - 1) / 64);
            }
        }
        return (ushort)((size >> (memoryClass + MEMORY_CLASS_SHIFT - SECOND_LEVEL_INDEX)) ^ (1U << SECOND_LEVEL_INDEX));
    }

    [return: NativeTypeName("UINT32")]
    private readonly uint GetListIndex([NativeTypeName("UINT8")] byte memoryClass, [NativeTypeName("UINT16")] ushort secondIndex)
    {
        if (memoryClass == 0)
        {
            return secondIndex;
        }

        uint index = (uint)(memoryClass - 1) * (1 << SECOND_LEVEL_INDEX) + secondIndex;

        if (IsVirtual())
        {
            return index + (1 << SECOND_LEVEL_INDEX);
        }
        else
        {
            return index + 4;
        }
    }

    [return: NativeTypeName("UINT32")]
    private readonly uint GetListIndex([NativeTypeName("UINT64")] ulong size)
    {
        byte memoryClass = SizeToMemoryClass(size);
        return GetListIndex(memoryClass, SizeToSecondIndex(size, memoryClass));
    }

    private void RemoveFreeBlock(Block* block)
    {
        D3D12MA_ASSERT(block != m_NullBlock);
        D3D12MA_ASSERT(block->IsFree());

        if (block->NextFree() != null)
        {
            block->NextFree()->PrevFree() = block->PrevFree();
        }

        if (block->PrevFree() != null)
        {
            block->PrevFree()->NextFree() = block->NextFree();
        }
        else
        {
            byte memClass = SizeToMemoryClass(block->size);
            ushort secondIndex = SizeToSecondIndex(block->size, memClass);

            uint index = GetListIndex(memClass, secondIndex);
            m_FreeList[index].Value = block->NextFree();

            if (block->NextFree() == null)
            {
                m_InnerIsFreeBitmap[memClass] &= ~(1u << secondIndex);

                if (m_InnerIsFreeBitmap[memClass] == 0)
                {
                    m_IsFreeBitmap &= ~(1u << memClass);
                }
            }
        }

        block->MarkTaken();
        block->PrivateData() = null;

        --m_BlocksFreeCount;
        m_BlocksFreeSize -= block->size;
    }

    private void InsertFreeBlock(Block* block)
    {
        D3D12MA_ASSERT(block != m_NullBlock);
        D3D12MA_ASSERT(!block->IsFree(), "Cannot insert block twice!");

        byte memClass = SizeToMemoryClass(block->size);
        ushort secondIndex = SizeToSecondIndex(block->size, memClass);
        uint index = GetListIndex(memClass, secondIndex);

        block->PrevFree() = null;
        block->NextFree() = m_FreeList[index].Value;

        m_FreeList[index].Value = block;

        if (block->NextFree() != null)
        {
            block->NextFree()->PrevFree() = block;
        }
        else
        {
            m_InnerIsFreeBitmap[memClass] |= 1u << secondIndex;
            m_IsFreeBitmap |= 1u << memClass;
        }

        ++m_BlocksFreeCount;
        m_BlocksFreeSize += block->size;
    }

    private void MergeBlock(Block* block, Block* prev)
    {
        D3D12MA_ASSERT(block->prevPhysical == prev, "Cannot merge seperate physical regions!");
        D3D12MA_ASSERT(!prev->IsFree(), "Cannot merge block that belongs to free list!");

        block->offset = prev->offset;
        block->size += prev->size;
        block->prevPhysical = prev->prevPhysical;

        if (block->prevPhysical != null)
        {
            block->prevPhysical->nextPhysical = block;
        }
        m_BlockAllocator.Free(prev);
    }

    private readonly Block* FindFreeBlock([NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT32 &")] ref uint listIndex)
    {
        byte memoryClass = SizeToMemoryClass(size);
        uint innerFreeMap = m_InnerIsFreeBitmap[memoryClass] & (~0U << SizeToSecondIndex(size, memoryClass));

        if (innerFreeMap == 0)
        {
            // Check higher levels for avaiable blocks
            uint freeMap = m_IsFreeBitmap & (~0u << (memoryClass + 1));

            if (freeMap == 0)
            {
                return null; // No more memory avaible
            }

            // Find lowest free region
            memoryClass = D3D12MA_BitScanLSB(freeMap);
            innerFreeMap = m_InnerIsFreeBitmap[memoryClass];

            D3D12MA_ASSERT(innerFreeMap != 0);
        }
        // Find lowest free subregion
        listIndex = GetListIndex(memoryClass, D3D12MA_BitScanLSB(innerFreeMap));
        return m_FreeList[listIndex].Value;
    }

    private bool CheckBlock(ref Block block, [NativeTypeName("UINT32")] uint listIndex, [NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        D3D12MA_ASSERT(block.IsFree(), "Block is already taken!");

        ulong alignedOffset = D3D12MA_AlignUp(block.offset, allocAlignment);

        if (block.size < (allocSize + alignedOffset - block.offset))
        {
            return false;
        }

        // Alloc successful
        pAllocationRequest->allocHandle = (ulong)(Unsafe.AsPointer(ref block));
        pAllocationRequest->size = allocSize - GetDebugMargin();
        pAllocationRequest->algorithmData = alignedOffset;

        // Place block at the start of list if it's normal block
        if ((listIndex != m_ListsCount) && (block.PrevFree() != null))
        {
            block.PrevFree()->NextFree() = block.NextFree();

            if (block.NextFree() != null)
            {
                block.NextFree()->PrevFree() = block.PrevFree();
            }

            block.PrevFree() = null;
            block.NextFree() = m_FreeList[listIndex].Value;

            m_FreeList[listIndex].Value = (Block*)(Unsafe.AsPointer(ref block));

            if (block.NextFree() != null)
            {
                block.NextFree()->PrevFree() = (Block*)(Unsafe.AsPointer(ref block));
            }
        }

        return true;
    }

    internal partial struct Block : IDisposable
    {
        [NativeTypeName("UINT64")]
        public ulong offset;

        [NativeTypeName("UINT64")]
        public ulong size;

        public Block* prevPhysical;

        public Block* nextPhysical;

        private Block* prevFree; // Address of the same block here indicates that block is taken

        private _Anonymous_e__Union Anonymous;

        internal void _ctor()
        {
            offset = 0;
            size = 0;
            prevPhysical = null;
            nextPhysical = null;
            prevFree = null;
            Anonymous = new _Anonymous_e__Union();
        }

        void IDisposable.Dispose()
        {
        }

        public void MarkFree()
        {
            prevFree = null;
        }

        public void MarkTaken()
        {
            prevFree = (Block*)(Unsafe.AsPointer(ref this));
        }

        public readonly bool IsFree()
        {
            return prevFree != (Block*)(Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
        }

        [UnscopedRef]
        [return: NativeTypeName("void *&")]
        public ref void* PrivateData()
        {
            D3D12MA_HEAVY_ASSERT(!IsFree());
            return ref Anonymous.privateData;
        }

        [UnscopedRef]
        [return: NativeTypeName("Block *&")]
        public ref Block* PrevFree()
        {
            return ref prevFree;
        }

        [UnscopedRef]
        [return: NativeTypeName("Block *&")]
        public ref Block* NextFree()
        {
            D3D12MA_HEAVY_ASSERT(IsFree());
            return ref Anonymous.nextFree;
        }

        [StructLayout(LayoutKind.Explicit)]
        private partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            public Block* nextFree;

            [FieldOffset(0)]
            public void* privateData;
        }
    }
}
