// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;

#pragma warning disable CS1573

namespace TerraFX.Interop
{
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
    public unsafe partial struct VirtualBlock : IDisposable
    {
        VirtualBlockPimpl* m_Pimpl;

        /// <summary>
        /// Destroys this object and frees it from memory.
        /// <para>You need to free all the allocations within this block or call Clear() before destroying it.</para>
        /// </summary>
        public partial void Release();

        /// <summary>Returns true if the block is empty - contains 0 allocations.</summary>
        [return: NativeTypeName("BOOL")]
        public readonly partial int IsEmpty();

        /// <summary>Returns information about an allocation at given offset - its size and custom pointer.</summary>
        public readonly partial void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, VIRTUAL_ALLOCATION_INFO* pInfo);

        /// <summary>Creates new allocation.</summary>
        /// <param name="pOffset">Offset of the new allocation, which can also be treated as an unique identifier of the allocation within this block. `UINT64_MAX` if allocation failed.</param>
        /// <returns>`S_OK` if allocation succeeded, `E_OUTOFMEMORY` if it failed.</returns>
        [return: NativeTypeName("HRESULT")]
        public partial int Allocate(VIRTUAL_ALLOCATION_DESC* pDesc, [NativeTypeName("UINT64*")] ulong* pOffset);

        /// <summary>Frees the allocation at given offset.</summary>
        public partial void FreeAllocation([NativeTypeName("UINT64")] ulong offset);

        /// <summary>Frees all the allocations.</summary>
        public partial void Clear();

        /// <summary>Changes custom pointer for an allocation at given offset to a new value.</summary>
        public partial void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* pUserData);

        /// <summary>Retrieves statistics from the current state of the block.</summary>
        public partial void CalculateStats(StatInfo* pInfo);

        /// <summary>Builds and returns statistics as a string in JSON format, including the list of allocations with their parameters.</summary>
        /// <param name="ppStatsString">Must be freed using VirtualBlock::FreeStatsString.</param>
        public partial void BuildStatsString([NativeTypeName("WCHAR**")] char** ppStatsString);

        /// <summary>Frees memory of a string returned from VirtualBlock::BuildStatsString.</summary>
        public partial void FreeStatsString([NativeTypeName("WCHAR*")] char* pStatsString);

        internal VirtualBlock(ALLOCATION_CALLBACKS* allocationCallbacks, VIRTUAL_BLOCK_DESC* desc)
        {
            m_Pimpl = D3D12MA_NEW<VirtualBlockPimpl>(allocationCallbacks);
            *m_Pimpl = new(allocationCallbacks, desc->Size);
        }

        public partial void Dispose();
    }

    public unsafe partial struct VirtualBlock
    {
        public partial void Dispose()
        {
            // THIS IS AN IMPORTANT ASSERT!
            // Hitting it means you have some memory leak - unreleased allocations in this virtual block.
            D3D12MA_ASSERT(m_Pimpl->m_Metadata.IsEmpty());

            D3D12MA_DELETE(&m_Pimpl->m_AllocationCallbacks, m_Pimpl);
        }

        public partial void Release()
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            // Copy is needed because otherwise we would call destructor and invalidate the structure with callbacks before using it to free memory.
            ALLOCATION_CALLBACKS allocationCallbacksCopy = m_Pimpl->m_AllocationCallbacks;
            D3D12MA_DELETE(&allocationCallbacksCopy, (VirtualBlock*)Unsafe.AsPointer(ref this));
        }

        public readonly partial int IsEmpty()
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            return m_Pimpl->m_Metadata.IsEmpty() ? TRUE : FALSE;
        }

        public readonly partial void GetAllocationInfo(ulong offset, VIRTUAL_ALLOCATION_INFO* pInfo)
        {
            D3D12MA_ASSERT(offset != UINT64_MAX && pInfo != null);

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            m_Pimpl->m_Metadata.GetAllocationInfo(offset, pInfo);
        }

        public partial int Allocate(VIRTUAL_ALLOCATION_DESC* pDesc, ulong* pOffset)
        {
            if (pDesc == null || pOffset == null || pDesc->Size == 0 || !IsPow2(pDesc->Alignment))
            {
                D3D12MA_ASSERT(false);
                return E_INVALIDARG;
            }

            *pOffset = UINT64_MAX;

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            ulong alignment = pDesc->Alignment != 0 ? pDesc->Alignment : 1;
            AllocationRequest allocRequest = default;
            if (m_Pimpl->m_Metadata.CreateAllocationRequest(pDesc->Size, alignment, &allocRequest))
            {
                m_Pimpl->m_Metadata.Alloc(&allocRequest, pDesc->Size, pDesc->pUserData);
                D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
                *pOffset = allocRequest.offset;
                return S_OK;
            }
            else
            {
                return E_OUTOFMEMORY;
            }
        }

        public partial void FreeAllocation(ulong offset)
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            D3D12MA_ASSERT(offset != UINT64_MAX);

            m_Pimpl->m_Metadata.FreeAtOffset(offset);
            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
        }

        public partial void Clear()
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            m_Pimpl->m_Metadata.Clear();
            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
        }

        public partial void SetAllocationUserData(ulong offset, void* pUserData)
        {
            D3D12MA_ASSERT(offset != UINT64_MAX);

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            m_Pimpl->m_Metadata.SetAllocationUserData(offset, pUserData);
        }

        public partial void CalculateStats(StatInfo* pInfo)
        {
            D3D12MA_ASSERT(pInfo != null);

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
            m_Pimpl->m_Metadata.CalcAllocationStatInfo(pInfo);
        }

        public partial void BuildStatsString(char** ppStatsString)
        {
            D3D12MA_ASSERT(ppStatsString != null);

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            using StringBuilder sb = new(&m_Pimpl->m_AllocationCallbacks);
            {
                using JsonWriter json = new(&m_Pimpl->m_AllocationCallbacks, &sb);
                D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
                m_Pimpl->m_Metadata.WriteAllocationInfoToJson(&json);
            } // Scope for JsonWriter

            nuint length = sb.GetLength();
            char* result = AllocateArray<char>(&m_Pimpl->m_AllocationCallbacks, length + 1);
            memcpy(result, sb.GetData(), length * sizeof(char));
            result[length] = '\0';
            *ppStatsString = result;
        }

        public partial void FreeStatsString(char* pStatsString)
        {
            if (pStatsString != null)
            {
                //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
                Free(&m_Pimpl->m_AllocationCallbacks, pStatsString);
            }
        }
    }
}
