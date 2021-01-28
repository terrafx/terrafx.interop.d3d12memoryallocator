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
    public unsafe struct VirtualBlock : IDisposable
    {
        VirtualBlockPimpl* m_Pimpl;

        /// <summary>
        /// Destroys this object and frees it from memory.
        /// <para>You need to free all the allocations within this block or call Clear() before destroying it.</para>
        /// </summary>
        public void Release()
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            // Copy is needed because otherwise we would call destructor and invalidate the structure with callbacks before using it to free memory.
            ALLOCATION_CALLBACKS allocationCallbacksCopy = m_Pimpl->m_AllocationCallbacks;
            D3D12MA_DELETE(&allocationCallbacksCopy, (VirtualBlock*)Unsafe.AsPointer(ref this));
        }

        /// <summary>Returns true if the block is empty - contains 0 allocations.</summary>
        [return: NativeTypeName("BOOL")]
        public readonly int IsEmpty()
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            return m_Pimpl->m_Metadata.IsEmpty() ? TRUE : FALSE;
        }

        /// <summary>Returns information about an allocation at given offset - its size and custom pointer.</summary>
        public readonly void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, VIRTUAL_ALLOCATION_INFO* pInfo)
        {
            D3D12MA_ASSERT(offset != UINT64_MAX && pInfo != null);

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            m_Pimpl->m_Metadata.GetAllocationInfo(offset, pInfo);
        }

        /// <summary>Creates new allocation.</summary>
        /// <param name="pOffset">Offset of the new allocation, which can also be treated as an unique identifier of the allocation within this block. `UINT64_MAX` if allocation failed.</param>
        /// <returns>`S_OK` if allocation succeeded, `E_OUTOFMEMORY` if it failed.</returns>
        [return: NativeTypeName("HRESULT")]
        public int Allocate([NativeTypeName("const VIRTUAL_ALLOCATION_DESC*")] VIRTUAL_ALLOCATION_DESC* pDesc, [NativeTypeName("UINT64 *")] ulong* pOffset)
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

        /// <summary>Frees the allocation at given offset.</summary>
        public void FreeAllocation([NativeTypeName("UINT64")] ulong offset)
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            D3D12MA_ASSERT(offset != UINT64_MAX);

            m_Pimpl->m_Metadata.FreeAtOffset(offset);
            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
        }

        /// <summary>Frees all the allocations.</summary>
        public void Clear()
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            m_Pimpl->m_Metadata.Clear();
            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
        }

        /// <summary>Changes custom pointer for an allocation at given offset to a new value.</summary>
        public void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* pUserData)
        {
            D3D12MA_ASSERT(offset != UINT64_MAX);

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            m_Pimpl->m_Metadata.SetAllocationUserData(offset, pUserData);
        }

        /// <summary>Retrieves statistics from the current state of the block.</summary>
        public void CalculateStats(StatInfo* pInfo)
        {
            D3D12MA_ASSERT(pInfo != null);

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            D3D12MA_HEAVY_ASSERT(m_Pimpl->m_Metadata.Validate());
            m_Pimpl->m_Metadata.CalcAllocationStatInfo(pInfo);
        }

        /// <summary>Builds and returns statistics as a string in JSON format, including the list of allocations with their parameters.</summary>
        /// <param name="ppStatsString">Must be freed using VirtualBlock::FreeStatsString.</param>
        public void BuildStatsString([NativeTypeName("WCHAR**")] ushort** ppStatsString)
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
            ushort* result = AllocateArray<ushort>(&m_Pimpl->m_AllocationCallbacks, length + 1);
            memcpy(result, sb.GetData(), length * sizeof(ushort));
            result[length] = '\0';
            *ppStatsString = result;
        }

        /// <summary>Frees memory of a string returned from VirtualBlock::BuildStatsString.</summary>
        public void FreeStatsString([NativeTypeName("WCHAR*")] ushort* pStatsString)
        {
            if (pStatsString != null)
            {
                //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
                Free(&m_Pimpl->m_AllocationCallbacks, pStatsString);
            }
        }

        internal VirtualBlock([NativeTypeName("const ALLOCATION_CALLBACKS&")] ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("const VIRTUAL_BLOCK_DESC&")] VIRTUAL_BLOCK_DESC* desc)
        {
            m_Pimpl = D3D12MA_NEW<VirtualBlockPimpl>(allocationCallbacks);
            *m_Pimpl = new VirtualBlockPimpl(allocationCallbacks, desc->Size);
        }

        public void Dispose()
        {
            // THIS IS AN IMPORTANT ASSERT!
            // Hitting it means you have some memory leak - unreleased allocations in this virtual block.
            D3D12MA_ASSERT(m_Pimpl->m_Metadata.IsEmpty());

            D3D12MA_DELETE(&m_Pimpl->m_AllocationCallbacks, m_Pimpl);
        }
    }
}
