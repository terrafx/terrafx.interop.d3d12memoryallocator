// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Data structure used for bookkeeping of allocations and unused ranges of memory in a single ID3D12Heap memory block.</summary>
internal unsafe partial struct D3D12MA_BlockMetadata : D3D12MA_BlockMetadata.Interface
{
    public void** lpVtbl;

    [NativeTypeName("UINT64")]
    private ulong m_Size;

    private readonly bool m_IsVirtual;

    [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")]
    private readonly D3D12MA_ALLOCATION_CALLBACKS* m_pAllocationCallbacks;

    public D3D12MA_BlockMetadata()
    {
        lpVtbl = VtblInstance;
    }

    public D3D12MA_BlockMetadata([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, bool isVirtual) : this()
    {
        m_IsVirtual = isVirtual;
        m_pAllocationCallbacks = allocationCallbacks;

        D3D12MA_ASSERT(allocationCallbacks != null);
    }

    [VtblIndex(0)]
    public void Dispose()
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, void>)(lpVtbl[0]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this));
    }

    [VtblIndex(1)]
    public void Init([NativeTypeName("UINT64")] ulong size)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, void>)(lpVtbl[1]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), size);
    }

    // Validates all data structures inside this object. If not valid, returns false.
    [VtblIndex(2)]
    public readonly bool Validate()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, byte>)(lpVtbl[2]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this))) != 0;
    }

    [VtblIndex(3)]
    [return: NativeTypeName("size_t")]
    public readonly nuint GetAllocationCount()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, nuint>)(lpVtbl[3]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(4)]
    [return: NativeTypeName("size_t")]
    public readonly nuint GetFreeRegionsCount()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, nuint>)(lpVtbl[4]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(5)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSumFreeSize()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong>)(lpVtbl[5]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(6)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetAllocationOffset([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, ulong>)(lpVtbl[6]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle);
    }

    // Returns true if this block is empty - contains only single free suballocation.
    [VtblIndex(7)]
    public readonly bool IsEmpty()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, byte>)(lpVtbl[7]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this))) != 0;
    }

    [VtblIndex(8)]
    public readonly void GetAllocationInfo([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO &")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void>)(lpVtbl[8]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle, outInfo);
    }

    // Tries to find a place for suballocation with given parameters inside this block.
    // If succeeded, fills pAllocationRequest and returns true.
    // If failed, returns false.
    [VtblIndex(9)]
    public bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, bool upperAddress, [NativeTypeName("UINT32")] uint strategy, D3D12MA_AllocationRequest* pAllocationRequest)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, ulong, byte, uint, D3D12MA_AllocationRequest*, byte>)(lpVtbl[9]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), allocSize, allocAlignment, (byte)(upperAddress ? 1 : 0), strategy, pAllocationRequest) != 0;
    }

    // Makes actual allocation based on request. Request must already be checked and valid.
    [VtblIndex(10)]
    public void Alloc([NativeTypeName("const D3D12MA::AllocationRequest &")] D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* PrivateData)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, D3D12MA_AllocationRequest*, ulong, void*, void>)(lpVtbl[10]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), request, allocSize, PrivateData);
    }

    [VtblIndex(11)]
    public void Free([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, void>)(lpVtbl[11]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), allocHandle);
    }

    // Frees all allocations.
    // Careful! Don't call it if there are Allocation objects owned by pPrivateData of of cleared allocations!
    [VtblIndex(12)]
    public void Clear()
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, void>)(lpVtbl[12]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this));
    }

    [VtblIndex(13)]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    public readonly ulong GetAllocationListBegin()
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong>)(lpVtbl[13]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)));
    }

    [VtblIndex(14)]
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    public readonly ulong GetNextAllocation([NativeTypeName("D3D12MA::AllocHandle")] ulong prevAlloc)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, ulong>)(lpVtbl[14]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), prevAlloc);
    }

    [VtblIndex(15)]
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetNextFreeRegionSize([NativeTypeName("D3D12MA::AllocHandle")] ulong alloc)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, ulong>)(lpVtbl[15]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), alloc);
    }

    [VtblIndex(16)]
    public readonly void* GetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle)
    {
        return ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, void*>)(lpVtbl[16]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), allocHandle);
    }

    [VtblIndex(17)]
    public void SetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, void* privateData)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, ulong, void*, void>)(lpVtbl[17]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref this), allocHandle, privateData);
    }

    [VtblIndex(18)]
    public readonly void AddStatistics([NativeTypeName("D3D12MA::Statistics &")] D3D12MA_Statistics* inoutStats)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, D3D12MA_Statistics*, void>)(lpVtbl[18]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), inoutStats);
    }

    [VtblIndex(19)]
    public readonly void AddDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] D3D12MA_DetailedStatistics* inoutStats)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, D3D12MA_DetailedStatistics*, void>)(lpVtbl[19]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), inoutStats);
    }

    [VtblIndex(20)]
    public readonly void WriteAllocationInfoToJson([NativeTypeName("D3D12MA::JsonWriter &")] D3D12MA_JsonWriter* json)
    {
        ((delegate* unmanaged<D3D12MA_BlockMetadata*, D3D12MA_JsonWriter*, void>)(lpVtbl[20]))((D3D12MA_BlockMetadata*)Unsafe.AsPointer(ref Unsafe.AsRef(in this)), json);
    }

    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSize()
    {
        return m_Size;
    }

    public readonly bool IsVirtual()
    {
        return m_IsVirtual;
    }

    [return: NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")]

    internal readonly D3D12MA_ALLOCATION_CALLBACKS* GetAllocs()
    {
        return m_pAllocationCallbacks;
    }

    [return: NativeTypeName("UINT64")]
    internal readonly ulong GetDebugMargin()
    {
        return IsVirtual() ? 0 : D3D12MA_DEBUG_MARGIN;
    }

    internal readonly void PrintDetailedMap_Begin([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json, [NativeTypeName("UINT64")] ulong unusedBytes, [NativeTypeName("size_t")] nuint allocationCount, [NativeTypeName("size_t")] nuint unusedRangeCount)
    {
        json.WriteString("TotalBytes");
        json.WriteNumber(GetSize());

        json.WriteString("UnusedBytes");
        json.WriteNumber(unusedBytes);

        json.WriteString("Allocations");
        json.WriteNumber(allocationCount);

        json.WriteString("UnusedRanges");
        json.WriteNumber(unusedRangeCount);

        json.WriteString("Suballocations");
        json.BeginArray();
    }

    internal readonly void PrintDetailedMap_Allocation([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json, [NativeTypeName("UINT64")] ulong offset, [NativeTypeName("UINT64")] ulong size, void* privateData)
    {
        json.BeginObject(true);

        json.WriteString("Offset");
        json.WriteNumber(offset);

        if (IsVirtual())
        {
            json.WriteString("Size");
            json.WriteNumber(size);

            if (privateData != null)
            {
                json.WriteString("CustomData");
                json.WriteNumber((nuint)(privateData));
            }
        }
        else
        {
            D3D12MA_Allocation* alloc = (D3D12MA_Allocation*)(privateData);
            D3D12MA_ASSERT(alloc != null);
            json.AddAllocationToObject(*alloc);
        }
        json.EndObject();
    }

    internal static void PrintDetailedMap_UnusedRange([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json, [NativeTypeName("UINT64")] ulong offset, [NativeTypeName("UINT64")] ulong size)
    {
        json.BeginObject(true);

        json.WriteString("Offset");
        json.WriteNumber(offset);

        json.WriteString("Type");
        json.WriteString("FREE");

        json.WriteString("Size");
        json.WriteNumber(size);

        json.EndObject();
    }

    internal static void PrintDetailedMap_End([NativeTypeName("D3D12MA::JsonWriter &")] ref D3D12MA_JsonWriter json)
    {
        json.EndArray();
    }

    public interface Interface : IDisposable
    {
        [VtblIndex(1)]
        void Init([NativeTypeName("UINT64")] ulong size);

        [VtblIndex(2)]
        bool Validate();

        [VtblIndex(3)]
        [return: NativeTypeName("size_t")]
        nuint GetAllocationCount();

        [VtblIndex(4)]
        [return: NativeTypeName("size_t")]
        nuint GetFreeRegionsCount();

        [VtblIndex(5)]
        [return: NativeTypeName("UINT64")]
        ulong GetSumFreeSize();

        [VtblIndex(6)]
        [return: NativeTypeName("UINT64")]
        ulong GetAllocationOffset([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle);

        [VtblIndex(7)]
        bool IsEmpty();

        [VtblIndex(8)]
        void GetAllocationInfo([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO &")] D3D12MA_VIRTUAL_ALLOCATION_INFO* outInfo);

        [VtblIndex(9)]
        bool CreateAllocationRequest([NativeTypeName("UINT64")] ulong allocSize, [NativeTypeName("UINT64")] ulong allocAlignment, bool upperAddress, [NativeTypeName("UINT32")] uint strategy, D3D12MA_AllocationRequest* pAllocationRequest);

        [VtblIndex(10)]
        void Alloc([NativeTypeName("const D3D12MA::AllocationRequest &")] D3D12MA_AllocationRequest* request, [NativeTypeName("UINT64")] ulong allocSize, void* PrivateData);

        [VtblIndex(11)]
        void Free([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle);

        [VtblIndex(12)]
        void Clear();

        [VtblIndex(13)]
        [return: NativeTypeName("D3D12MA::AllocHandle")]
        ulong GetAllocationListBegin();

        [VtblIndex(14)]
        [return: NativeTypeName("D3D12MA::AllocHandle")]
        ulong GetNextAllocation([NativeTypeName("D3D12MA::AllocHandle")] ulong prevAlloc);

        [VtblIndex(15)]
        [return: NativeTypeName("UINT64")]
        ulong GetNextFreeRegionSize([NativeTypeName("D3D12MA::AllocHandle")] ulong alloc);

        [VtblIndex(16)]
        void* GetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle);

        [VtblIndex(17)]
        void SetAllocationPrivateData([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, void* privateData);

        [VtblIndex(18)]
        void AddStatistics([NativeTypeName("D3D12MA::Statistics &")] D3D12MA_Statistics* inoutStats);

        [VtblIndex(19)]
        void AddDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] D3D12MA_DetailedStatistics* inoutStats);

        [VtblIndex(20)]
        void WriteAllocationInfoToJson([NativeTypeName("D3D12MA::JsonWriter &")] D3D12MA_JsonWriter* json);
    }

    public partial struct Vtbl<TSelf>
        where TSelf : unmanaged, Interface
    {
        [NativeTypeName("void () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, void> Dispose;

        [NativeTypeName("void (UINT64) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, void> Init;

        [NativeTypeName("bool () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, byte> Validate;

        [NativeTypeName("size_t () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, nuint> GetAllocationCount;

        [NativeTypeName("size_t () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, nuint> GetFreeRegionsCount;

        [NativeTypeName("UINT64 () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong> GetSumFreeSize;

        [NativeTypeName("UINT64 (D3D12MA::AllocHandle) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, ulong> GetAllocationOffset;

        [NativeTypeName("bool () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, byte> IsEmpty;

        [NativeTypeName("void (D3D12MA::AllocHandle, D3D12MA::VIRTUAL_ALLOCATION_INFO &) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, D3D12MA_VIRTUAL_ALLOCATION_INFO*, void> GetAllocationInfo;

        [NativeTypeName("bool (UINT64, UINT64, bool, UINT32, D3D12MA::AllocationRequest *) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, ulong, byte, uint, D3D12MA_AllocationRequest*, byte> CreateAllocationRequest;

        [NativeTypeName("void (const D3D12MA::AllocationRequest &, UINT64, void *) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, D3D12MA_AllocationRequest*, ulong, void*, void> Alloc;

        [NativeTypeName("void (D3D12MA::AllocHandle) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, void> Free;

        [NativeTypeName("void () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, void> Clear;

        [NativeTypeName("D3D12MA::AllocHandle () __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong> GetAllocationListBegin;

        [NativeTypeName("D3D12MA::AllocHandle (D3D12MA::AllocHandle) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, ulong> GetNextAllocation;

        [NativeTypeName("UINT64 (D3D12MA::AllocHandle) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, ulong> GetNextFreeRegionSize;

        [NativeTypeName("void * (D3D12MA::AllocHandle) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, void*> GetAllocationPrivateData;

        [NativeTypeName("void (D3D12MA::AllocHandle, void *) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, ulong, void*, void> SetAllocationPrivateData;

        [NativeTypeName("void (D3D12MA::Statistics &) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, D3D12MA_Statistics*, void> AddStatistics;

        [NativeTypeName("void (D3D12MA::DetailedStatistics &) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, D3D12MA_DetailedStatistics*, void> AddDetailedStatistics;

        [NativeTypeName("void (D3D12MA::JsonWriter &) __attribute__((stdcall))")]
        public delegate* unmanaged<TSelf*, D3D12MA_JsonWriter*, void> WriteAllocationInfoToJson;
    }
}
