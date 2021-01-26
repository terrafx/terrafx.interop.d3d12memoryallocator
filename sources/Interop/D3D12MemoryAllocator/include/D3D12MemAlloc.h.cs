// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

using UINT = System.UInt32;
using uint64_t = System.UInt64;
using size_t = nuint;
using BOOL = System.Int32;

namespace TerraFX.Interop
{
    public static partial class D3D12MemoryAllocator
    {
        /*
        When defined to value other than 0, the library will try to use
        D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT or D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT
        for created textures when possible, which can save memory because some small textures
        may get their alignment 4K and their size a multiply of 4K instead of 64K.

        #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 0
            Disables small texture alignment.
        #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 1
            Enables conservative algorithm that will use small alignment only for some textures
            that are surely known to support it.
        #define D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT 2
            Enables query for small alignment to D3D12 (based on Microsoft sample) which will
            enable small alignment for more textures, but will also generate D3D Debug Layer
            error #721 on call to ID3D12Device::GetResourceAllocationInfo, which you should just
            ignore.
        */
        public static readonly int D3D12MA_USE_SMALL_RESOURCE_PLACEMENT_ALIGNMENT = 1;
    }

    public static unsafe partial class D3D12MemoryAllocator
    {
        /// <summary>Number of D3D12 memory heap types supported.</summary>
        public const UINT HEAP_TYPE_COUNT = 3;

        /// <summary>
        /// Creates new main D3D12MA::Allocator object and returns it through `ppAllocator`.
        /// <para>You normally only need to call it once and keep a single Allocator object for your `ID3D12Device`.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateAllocator(ALLOCATOR_DESC* pDesc, Allocator** ppAllocator);

        /// <summary>
        /// Creates new D3D12MA::VirtualBlock object and returns it through `ppVirtualBlock`.
        /// <para>Note you don't need to create D3D12MA::Allocator to use virtual blocks.</para>
        /// </summary>
        public static HRESULT CreateVirtualBlock(VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);
    }

    /// <summary>
    /// Represents pure allocation algorithm and a data structure with allocations in some memory block, without actually allocating any GPU memory.
    /// <para>
    /// This class allows to use the core algorithm of the library custom allocations e.g. CPU memory or
    /// sub-allocation regions inside a single GPU buffer.
    /// </para>
    /// <para>
    /// To create this object, fill in D3D12MA::VIRTUAL_BLOCK_DESC and call CreateVirtualBlock().
    /// To destroy it, call its method VirtualBlock::Release().
    /// </para>
    /// </summary>
    public unsafe struct VirtualBlock
    {
        /// <summary>
        /// Destroys this object and frees it from memory.
        /// <para>You need to free all the allocations within this block or call Clear() before destroying it.</para>
        /// </summary>
        public partial void Release();

        /// <summary>Returns true if the block is empty - contains 0 allocations.</summary>
        public partial BOOL IsEmpty();

        /// <summary>Returns information about an allocation at given offset - its size and custom pointer.</summary>
        public partial void GetAllocationInfo(uint64_t offset, VIRTUAL_ALLOCATION_INFO* pInfo);

        /// <summary>Creates new allocation.</summary>
        /// <param name="pOffset">Offset of the new allocation, which can also be treated as an unique identifier of the allocation within this block. `UINT64_MAX` if allocation failed.</param>
        /// <returns>`S_OK` if allocation succeeded, `E_OUTOFMEMORY` if it failed.</returns>
        public partial HRESULT Allocate(VIRTUAL_ALLOCATION_DESC* pDesc, uint64_t* pOffset);

        /// <summary>Frees the allocation at given offset.</summary>
        public partial void FreeAllocation(uint64_t offset);

        /// <summary>Frees all the allocations.</summary>
        public partial void Clear();

        /// <summary>Changes custom pointer for an allocation at given offset to a new value.</summary>
        public partial void SetAllocationUserData(uint64_t offset, void* pUserData);

        /// <summary>Retrieves statistics from the current state of the block.</summary>
        public partial void CalculateStats(StatInfo* pInfo);

        /// <summary>Builds and returns statistics as a string in JSON format, including the list of allocations with their parameters.</summary>
        /// <param name="ppStatsString">Must be freed using VirtualBlock::FreeStatsString.</param>
        public partial void BuildStatsString(char** ppStatsString);

        /// <summary>Frees memory of a string returned from VirtualBlock::BuildStatsString.</summary>
        public partial void FreeStatsString(char* pStatsString);
    }
}
