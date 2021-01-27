// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

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
            SharedLpVtbl[1] = (delegate*<BlockMetadata*, ulong, void>)&Init;
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

        [NativeTypeName("UINT64")] public ulong m_Size;
        public bool m_IsVirtual;
        public readonly ALLOCATION_CALLBACKS* m_pAllocationCallbacks;

        public BlockMetadata(ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
        {
            lpVtbl = SharedLpVtbl;

            m_Size = 0;
            m_IsVirtual = isVirtual;
            m_pAllocationCallbacks = allocationCallbacks;

            D3D12MA_ASSERT(allocationCallbacks != null);
        }

        public void Dispose()
        {
            ((delegate*<BlockMetadata*, void>)lpVtbl[0])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }

        public void Init([NativeTypeName("UINT64")] ulong size)
        {
            ((delegate*<BlockMetadata*, ulong, void>)lpVtbl[1])((BlockMetadata*)Unsafe.AsPointer(ref this), size);
        }

        /// <summary>Validates all data structures inside this object. If not valid, returns false.</summary>
        public readonly bool Validate()
        {
            return ((delegate*<BlockMetadata*, bool>)lpVtbl[2])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() { return m_Size; }

        public readonly bool IsVirtual() { return m_IsVirtual; }

        [return: NativeTypeName("size_t")]
        public readonly nuint GetAllocationCount()
        {
            return ((delegate*<BlockMetadata*, nuint>)lpVtbl[3])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSumFreeSize()
        {
            return ((delegate*<BlockMetadata*, ulong>)lpVtbl[4])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetUnusedRangeSizeMax()
        {
            return ((delegate*<BlockMetadata*, ulong>)lpVtbl[5])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        /// <summary>Returns true if this block is empty - contains only single free suballocation.</summary>
        public readonly bool IsEmpty() => ((delegate*<BlockMetadata*, bool>)lpVtbl[6])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));

        public readonly void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            ((delegate*<BlockMetadata*, ulong, VIRTUAL_ALLOCATION_INFO*, void>)lpVtbl[7])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), offset, outInfo);
        }

        /// <summary>
        /// Tries to find a place for suballocation with given parameters inside this block.
        /// If succeeded, fills pAllocationRequest and returns true.
        /// If failed, returns false.
        /// </summary>
        public bool CreateAllocationRequest(
            [NativeTypeName("UINT64")] ulong allocSize,
            [NativeTypeName("UINT64")] ulong allocAlignment,
            AllocationRequest* pAllocationRequest)
        {
            return ((delegate*<BlockMetadata*, ulong, ulong, AllocationRequest*, bool>)lpVtbl[8])((BlockMetadata*)Unsafe.AsPointer(ref this), allocSize, allocAlignment, pAllocationRequest);
        }

        /// <summary>Makes actual allocation based on request. Request must already be checked and valid.</summary>
        public void Alloc(
            AllocationRequest* request,
            [NativeTypeName("UINT64")] ulong allocSize,
            void* userData)
        {
            ((delegate*<BlockMetadata*, AllocationRequest*, ulong, void*, void>)lpVtbl[9])((BlockMetadata*)Unsafe.AsPointer(ref this), request, allocSize, userData);
        }

        public void FreeAtOffset([NativeTypeName("UINT64")] ulong offset)
        {
            ((delegate*<BlockMetadata*, ulong, void>)lpVtbl[10])((BlockMetadata*)Unsafe.AsPointer(ref this), offset);
        }

        /// <summary>
        /// Frees all allocations.
        /// Careful! Don't call it if there are Allocation objects owned by pUserData of of cleared allocations!
        /// </summary>
        public void Clear()
        {
            ((delegate*<BlockMetadata*, void>)lpVtbl[11])((BlockMetadata*)Unsafe.AsPointer(ref this));
        }

        public void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* userData)
        {
            ((delegate*<BlockMetadata*, ulong, void*, void>)lpVtbl[12])((BlockMetadata*)Unsafe.AsPointer(ref this), offset, userData);
        }

        public readonly void CalcAllocationStatInfo(StatInfo* outInfo)
        {
            ((delegate*<BlockMetadata*, StatInfo*, void>)lpVtbl[13])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), outInfo);
        }

        public readonly void WriteAllocationInfoToJson(JsonWriter* json)
        {
            ((delegate*<BlockMetadata*, JsonWriter*, void>)lpVtbl[14])((BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), json);
        }

        public static void Dispose(BlockMetadata* @this) { }

        public static void Init(BlockMetadata* @this, ulong size) { @this->m_Size = size; }

        public ALLOCATION_CALLBACKS* GetAllocs() { return m_pAllocationCallbacks; }
    }
}
