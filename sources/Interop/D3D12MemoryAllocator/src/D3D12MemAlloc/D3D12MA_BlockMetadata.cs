// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    /// <summary>Data structure used for bookkeeping of allocations and unused ranges of memory in a single ID3D12Heap memory block.</summary>
    internal unsafe struct D3D12MA_BlockMetadata : IDisposable
    {
        private static readonly void** Vtbl = InitVtbl();

        private static void** InitVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_BlockMetadata), sizeof(void*) * 15);

            /* Finalizer                 */ lpVtbl[0] = (delegate*<ref D3D12MA_BlockMetadata, void>)&Dispose;
            /* Init                      */ lpVtbl[1] = (delegate*<ref D3D12MA_BlockMetadata, ulong, void>)&Init;
            /* Validate                  */ lpVtbl[2] = null;
            /* GetAllocationCount        */ lpVtbl[3] = null;
            /* GetSumFreeSize            */ lpVtbl[4] = null;
            /* GetUnusedRangeSizeMax     */ lpVtbl[5] = null;
            /* IsEmpty                   */ lpVtbl[6] = null;
            /* GetAllocationInfo         */ lpVtbl[7] = null;
            /* CreateAllocationRequest   */ lpVtbl[8] = null;
            /* Alloc                     */ lpVtbl[9] = null;
            /* FreeAtOffset              */ lpVtbl[10] = null;
            /* Clear                     */ lpVtbl[11] = null;
            /* SetAllocationUserData     */ lpVtbl[12] = null;
            /* CalcAllocationStatInfo    */ lpVtbl[13] = null;
            /* WriteAllocationInfoToJson */ lpVtbl[14] = null;

            return lpVtbl;
        }

        internal void** lpVtbl;

        [NativeTypeName("UINT64")]
        public ulong m_Size;

        public byte m_IsVirtual;

        public readonly D3D12MA_ALLOCATION_CALLBACKS* m_pAllocationCallbacks;

        public D3D12MA_BlockMetadata([NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual)
        {
            lpVtbl = Vtbl;

            m_Size = 0;
            m_IsVirtual = (byte)(isVirtual ? 1 : 0);
            m_pAllocationCallbacks = allocationCallbacks;

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocationCallbacks != null));
        }

        public void Dispose()
        {
            ((delegate*<ref D3D12MA_BlockMetadata, void>)lpVtbl[0])(ref this);
        }

        public void Init([NativeTypeName("UINT64")] ulong size)
        {
            ((delegate*<ref D3D12MA_BlockMetadata, ulong, void>)lpVtbl[1])(ref this, size);
        }

        /// <summary>Validates all data structures inside this object. If not valid, returns false.</summary>
        public readonly bool Validate()
        {
            return ((delegate*<in D3D12MA_BlockMetadata, bool>)lpVtbl[2])(in this);
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => m_Size;

        public readonly bool IsVirtual() => m_IsVirtual != 0;

        [return: NativeTypeName("size_t")]
        public readonly nuint GetAllocationCount()
        {
            return ((delegate*<in D3D12MA_BlockMetadata, nuint>)lpVtbl[3])(in this);
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSumFreeSize()
        {
            return ((delegate*<in D3D12MA_BlockMetadata, ulong>)lpVtbl[4])(in this);
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetUnusedRangeSizeMax()
        {
            return ((delegate*<in D3D12MA_BlockMetadata, ulong>)lpVtbl[5])(in this);
        }

        /// <summary>Returns true if this block is empty - contains only single free suballocation.</summary>
        public readonly bool IsEmpty() => ((delegate*<in D3D12MA_BlockMetadata, bool>)lpVtbl[6])(in this);

        public readonly void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, [NativeTypeName("D3D12MA_VIRTUAL_ALLOCATION_INFO&")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
        {
            ((delegate*<in D3D12MA_BlockMetadata, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)lpVtbl[7])(in this, offset, outInfo);
        }

        /// <summary>Tries to find a place for suballocation with given parameters inside this block. If succeeded, fills pAllocationRequest and returns true. If failed, returns false.</summary>
        public bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, D3D12MA_AllocationRequest* pAllocationRequest)
        {
            return ((delegate*<ref D3D12MA_BlockMetadata, ulong, ulong, D3D12MA_AllocationRequest*, bool>)lpVtbl[8])(ref this, allocSize, allocAlignment, pAllocationRequest);
        }

        /// <summary>Makes actual allocation based on request. Request must already be checked and valid.</summary>
        public void Alloc([NativeTypeName("const D3D12MA_AllocationRequest&")] D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* userData)
        {
            ((delegate*<ref D3D12MA_BlockMetadata, D3D12MA_AllocationRequest*, ulong, void*, void>)lpVtbl[9])(ref this, request, allocSize, userData);
        }

        public void FreeAtOffset([NativeTypeName("UINT64")] ulong offset)
        {
            ((delegate*<ref D3D12MA_BlockMetadata, ulong, void>)lpVtbl[10])(ref this, offset);
        }

        /// <summary>Frees all allocations. Careful! Don't call it if there are Allocation objects owned by pUserData of of cleared allocations!</summary>
        public void Clear()
        {
            ((delegate*<ref D3D12MA_BlockMetadata, void>)lpVtbl[11])(ref this);
        }

        public void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* userData)
        {
            ((delegate*<ref D3D12MA_BlockMetadata, ulong, void*, void>)lpVtbl[12])(ref this, offset, userData);
        }

        public readonly void CalcAllocationStatInfo([NativeTypeName("StatInfo&")] D3D12MA_StatInfo* outInfo)
        {
            ((delegate*<in D3D12MA_BlockMetadata, D3D12MA_StatInfo*, void>)lpVtbl[13])(in this, outInfo);
        }

        public readonly void WriteAllocationInfoToJson([NativeTypeName("JsonWriter&")] D3D12MA_JsonWriter* json)
        {
            ((delegate*<in D3D12MA_BlockMetadata, D3D12MA_JsonWriter*, void>)lpVtbl[14])(in this, json);
        }

        internal static void Dispose(ref D3D12MA_BlockMetadata pThis) { }

        internal static void Init(ref D3D12MA_BlockMetadata pThis, ulong size) => pThis.m_Size = size;

        internal D3D12MA_ALLOCATION_CALLBACKS* GetAllocs() => m_pAllocationCallbacks;
    }
}
