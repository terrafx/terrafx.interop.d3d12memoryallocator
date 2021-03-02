// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    /// <summary>Data structure used for bookkeeping of allocations and unused ranges of memory in a single ID3D12Heap memory block.</summary>
    internal unsafe struct D3D12MA_BlockMetadata : IDisposable
    {
        private static readonly void** SharedLpVtbl = InitLpVtbl();

        private static void** InitLpVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_BlockMetadata), sizeof(void*) * 15);
            lpVtbl[0] = (delegate*<D3D12MA_BlockMetadata*, void>)&Dispose;
            lpVtbl[1] = (delegate*<D3D12MA_BlockMetadata*, ulong, void>)&Init;
            lpVtbl[2] = null;
            lpVtbl[3] = null;
            lpVtbl[4] = null;
            lpVtbl[5] = null;
            lpVtbl[6] = null;
            lpVtbl[7] = null;
            lpVtbl[8] = null;
            lpVtbl[9] = null;
            lpVtbl[10] = null;
            lpVtbl[11] = null;
            lpVtbl[12] = null;
            lpVtbl[13] = null;
            lpVtbl[14] = null;
            return lpVtbl;
        }

        public void** lpVtbl;

        [NativeTypeName("UINT64")]
        public ulong m_Size;

        public bool m_IsVirtual;
        public readonly D3D12MA_ALLOCATION_CALLBACKS* m_pAllocationCallbacks;

        public D3D12MA_BlockMetadata(D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
        {
            lpVtbl = SharedLpVtbl;

            m_Size = 0;
            m_IsVirtual = isVirtual;
            m_pAllocationCallbacks = allocationCallbacks;

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocationCallbacks != null));
        }

        public void Dispose()
        {
            ((delegate*<D3D12MA_BlockMetadata*, void>)lpVtbl[0])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this));
        }

        public void Init([NativeTypeName("UINT64")] ulong size)
        {
            ((delegate*<D3D12MA_BlockMetadata*, ulong, void>)lpVtbl[1])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), size);
        }

        /// <summary>Validates all data structures inside this object. If not valid, returns false.</summary>
        public readonly bool Validate()
        {
            return ((delegate*<D3D12MA_BlockMetadata*, bool>)lpVtbl[2])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => m_Size;

        public readonly bool IsVirtual() => m_IsVirtual;

        [return: NativeTypeName("size_t")]
        public readonly nuint GetAllocationCount()
        {
            return ((delegate*<D3D12MA_BlockMetadata*, nuint>)lpVtbl[3])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSumFreeSize()
        {
            return ((delegate*<D3D12MA_BlockMetadata*, ulong>)lpVtbl[4])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetUnusedRangeSizeMax()
        {
            return ((delegate*<D3D12MA_BlockMetadata*, ulong>)lpVtbl[5])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));
        }

        /// <summary>Returns true if this block is empty - contains only single free suballocation.</summary>
        public readonly bool IsEmpty() => ((delegate*<D3D12MA_BlockMetadata*, bool>)lpVtbl[6])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)));

        public readonly void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            ((delegate*<D3D12MA_BlockMetadata*, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)lpVtbl[7])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), offset, outInfo);
        }

        /// <summary>Tries to find a place for suballocation with given parameters inside this block. If succeeded, fills pAllocationRequest and returns true. If failed, returns false.</summary>
        public bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, D3D12MA_AllocationRequest* pAllocationRequest)
        {
            return ((delegate*<D3D12MA_BlockMetadata*, ulong, ulong, D3D12MA_AllocationRequest*, bool>)lpVtbl[8])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), allocSize, allocAlignment, pAllocationRequest);
        }

        /// <summary>Makes actual allocation based on request. Request must already be checked and valid.</summary>
        public void Alloc(D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* userData)
        {
            ((delegate*<D3D12MA_BlockMetadata*, D3D12MA_AllocationRequest*, ulong, void*, void>)lpVtbl[9])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), request, allocSize, userData);
        }

        public void FreeAtOffset([NativeTypeName("UINT64")] ulong offset)
        {
            ((delegate*<D3D12MA_BlockMetadata*, ulong, void>)lpVtbl[10])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), offset);
        }

        /// <summary>Frees all allocations. Careful! Don't call it if there are Allocation objects owned by pUserData of of cleared allocations!</summary>
        public void Clear()
        {
            ((delegate*<D3D12MA_BlockMetadata*, void>)lpVtbl[11])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this));
        }

        public void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* userData)
        {
            ((delegate*<D3D12MA_BlockMetadata*, ulong, void*, void>)lpVtbl[12])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), offset, userData);
        }

        public readonly void CalcAllocationStatInfo(D3D12MA_StatInfo* outInfo)
        {
            ((delegate*<D3D12MA_BlockMetadata*, D3D12MA_StatInfo*, void>)lpVtbl[13])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), outInfo);
        }

        public readonly void WriteAllocationInfoToJson(D3D12MA_JsonWriter* json)
        {
            ((delegate*<D3D12MA_BlockMetadata*, D3D12MA_JsonWriter*, void>)lpVtbl[14])((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(this)), json);
        }

        public static void Dispose(D3D12MA_BlockMetadata* pThis) { }

        public static void Init(D3D12MA_BlockMetadata* pThis, ulong size) => pThis->m_Size = size;

        public D3D12MA_ALLOCATION_CALLBACKS* GetAllocs() => m_pAllocationCallbacks;
    }
}
