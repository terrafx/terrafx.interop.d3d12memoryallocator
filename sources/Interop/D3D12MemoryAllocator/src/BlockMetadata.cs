// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

using UINT64 = System.UInt64;
using size_t = nuint;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class BlockMetadata and derived classes - declarations

    /// <summary>
    /// Data structure used for bookkeeping of allocations and unused ranges of memory
    /// in a single ID3D12Heap memory block.
    /// </summary>
    internal unsafe struct BlockMetadata : IDisposable
    {
        private static void** SharedLpVtbl = InitLpVtbl();

        private static void** InitLpVtbl()
        {
            SharedLpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(BlockMetadata), sizeof(void*) * 15);
            SharedLpVtbl[0] = (delegate*<BlockMetadata*, void>)&Dispose;
            SharedLpVtbl[1] = (delegate*<BlockMetadata*, UINT64, void>)&Init;
            SharedLpVtbl[2] = null;
            SharedLpVtbl[3] = null;
            SharedLpVtbl[4] = null;
            SharedLpVtbl[5] = null;
            SharedLpVtbl[6] = null;
            SharedLpVtbl[7] = null;
            SharedLpVtbl[8] = null;
            SharedLpVtbl[9] = null;
            SharedLpVtbl[10] = null;
            SharedLpVtbl[11] = null;
            SharedLpVtbl[12] = null;
            SharedLpVtbl[13] = null;
            SharedLpVtbl[14] = null;
            return SharedLpVtbl;
        }

        public void** lpVtbl;

        public BlockMetadata(ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
        {
            lpVtbl = SharedLpVtbl;

            m_Size = 0;
            m_IsVirtual = isVirtual;
            m_pAllocationCallbacks = allocationCallbacks;

            D3D12MA_ASSERT(allocationCallbacks);
        }

        public void Dispose()
        {
            ((delegate*<BlockMetadata*, void>)lpVtbl[0])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }
        public void Init(UINT64 size)
        {
            ((delegate*<BlockMetadata*, UINT64, void>)lpVtbl[1])((BlockMetadata*)Unsafe.AsPointer(ref this), size);
        }

        // Validates all data structures inside this object. If not valid, returns false.
        public bool Validate()
        {
            return ((delegate*<BlockMetadata*, bool>)lpVtbl[2])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }
        public UINT64 GetSize() { return m_Size; }
        public bool IsVirtual() { return m_IsVirtual; }
        public size_t GetAllocationCount()
        {
            return ((delegate*<BlockMetadata*, size_t>)lpVtbl[3])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }
        public UINT64 GetSumFreeSize()
        {
            return ((delegate*<BlockMetadata*, UINT64>)lpVtbl[4])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }
        public UINT64 GetUnusedRangeSizeMax()
        {
            return ((delegate*<BlockMetadata*, UINT64>)lpVtbl[5])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }
        // Returns true if this block is empty - contains only single free suballocation.
        public bool IsEmpty() => ((delegate*<BlockMetadata*, bool>)lpVtbl[6])((BlockMetadata*)Unsafe.AsPointer(ref this));

        public void GetAllocationInfo(UINT64 offset, VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            ((delegate*<BlockMetadata*, UINT64, VIRTUAL_ALLOCATION_INFO*, void>)lpVtbl[7])((BlockMetadata*)Unsafe.AsPointer(ref this), offset, outInfo);
        }

        // Tries to find a place for suballocation with given parameters inside this block.
        // If succeeded, fills pAllocationRequest and returns true.
        // If failed, returns false.
        public void CreateAllocationRequest(
            UINT64 allocSize,
            UINT64 allocAlignment,
            AllocationRequest* pAllocationRequest)
        {
            ((delegate*<BlockMetadata*, UINT64, UINT64, AllocationRequest*, void>)lpVtbl[8])((BlockMetadata*)Unsafe.AsPointer(ref this), allocSize, allocAlignment, pAllocationRequest);
        }

        // Makes actual allocation based on request. Request must already be checked and valid.
        public void Alloc(
            AllocationRequest* request,
            UINT64 allocSize,
            void* userData)
        {
            ((delegate*<BlockMetadata*, AllocationRequest*, UINT64, void*, void>)lpVtbl[9])((BlockMetadata*)Unsafe.AsPointer(ref this), request, allocSize, userData);
        }

        public void FreeAtOffset(UINT64 offset)
        {
            ((delegate*<BlockMetadata*, UINT64, void>)lpVtbl[10])((BlockMetadata*)Unsafe.AsPointer(ref this), offset);
        }
        // Frees all allocations.
        // Careful! Don't call it if there are Allocation objects owned by pUserData of of cleared allocations!
        public void Clear()
        {
            ((delegate*<BlockMetadata*, void>)lpVtbl[11])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }

        public void SetAllocationUserData(UINT64 offset, void* userData)
        {
            ((delegate*<BlockMetadata*, UINT64, void*, void>)lpVtbl[12])((BlockMetadata*)Unsafe.AsPointer(ref this), offset, userData);
        }

        public void CalcAllocationStatInfo(StatInfo* outInfo)
        {
            ((delegate*<BlockMetadata*, StatInfo*, void>)lpVtbl[13])((BlockMetadata*)Unsafe.AsPointer(ref this), outInfo);
        }

        public void WriteAllocationInfoToJson(JsonWriter* json)
        {
            ((delegate*<BlockMetadata*, JsonWriter*, void>)lpVtbl[14])((BlockMetadata*)Unsafe.AsPointer(ref this), json);
        }

        public static void Dispose(BlockMetadata* @this) { }
        public static void Init(BlockMetadata* @this, UINT64 size) { @this->m_Size = size; }

        public ALLOCATION_CALLBACKS* GetAllocs() { return m_pAllocationCallbacks; }

        public UINT64 m_Size;
        public bool m_IsVirtual;
        public readonly ALLOCATION_CALLBACKS* m_pAllocationCallbacks;
    }
}
