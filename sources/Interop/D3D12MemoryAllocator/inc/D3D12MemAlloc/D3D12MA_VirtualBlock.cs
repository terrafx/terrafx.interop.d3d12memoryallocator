// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemAlloc;
using System.Runtime.InteropServices;

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents pure allocation algorithm and a data structure with allocations in some memory block, without actually allocating any GPU memory.
    /// <para>This class allows to use the core algorithm of the library custom allocations e.g. CPU memory or sub-allocation regions inside a single GPU buffer.</para>
    /// <para>
    /// To create this object, fill in <see cref="D3D12MA_VIRTUAL_BLOCK_DESC"/> and call <see cref="D3D12MA_CreateVirtualBlock"/>.
    /// To destroy it, call its method <see cref="Release"/>.
    /// </para>
    /// </summary>
    public unsafe partial struct D3D12MA_VirtualBlock : IDisposable
    {
        private static readonly void** Vtbl = InitVtbl();

        private static void** InitVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_VirtualBlock), sizeof(void*) * 4);

            /* QueryInterface */ lpVtbl[0] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, Guid*, void**, int>)&D3D12MA_IUnknownImpl.QueryInterface;
            /* AddRef         */ lpVtbl[1] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, uint>)&D3D12MA_IUnknownImpl.AddRef;
            /* Release        */ lpVtbl[2] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, uint>)&D3D12MA_IUnknownImpl.Release;
            /* ReleaseThis    */ lpVtbl[3] = (delegate* unmanaged<D3D12MA_IUnknownImpl*, void>)&ReleaseThis;

            return lpVtbl;
        }

        /// <summary>
        /// Implements <c>IUnknown.Release()</c>.
        /// </summary>
        public uint Release()
        {
            return m_IUnknownImpl.Release();
        }

        /// <summary>
        /// Destroys this object and frees it from memory.
        /// <para>You need to free all the allocations within this block or call <see cref="Clear"/> before destroying it.</para>
        /// </summary>
        private void ReleaseThis()
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            // Copy is needed because otherwise we would call destructor and invalidate the structure with callbacks before using it to free memory.
            D3D12MA_ALLOCATION_CALLBACKS allocationCallbacksCopy = m_AllocationCallbacks;
            D3D12MA_DELETE(&allocationCallbacksCopy, ref this);
        }

        [UnmanagedCallersOnly]
        private static void ReleaseThis(D3D12MA_IUnknownImpl* pThis)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pThis->lpVtbl == Vtbl));
            ((D3D12MA_VirtualBlock*)pThis)->ReleaseThis();
        }

        /// <summary>Returns true if the block is empty - contains 0 allocations.</summary>
        [return: NativeTypeName("BOOL")]
        public readonly int IsEmpty()
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            return m_Metadata.IsEmpty() ? TRUE : FALSE;
        }

        /// <summary>Returns information about an allocation at given offset - its size and custom pointer.</summary>
        public readonly void GetAllocationInfo([NativeTypeName("UINT64")] ulong offset, D3D12MA_VIRTUAL_ALLOCATION_INFO* pInfo)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (offset != UINT64_MAX) && (pInfo != null));

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            m_Metadata.GetAllocationInfo(offset, pInfo);
        }

        /// <summary>Creates new allocation.</summary>
        /// <param name="pDesc"></param>
        /// <param name="pOffset">Offset of the new allocation, which can also be treated as an unique identifier of the allocation within this block. <see cref="UINT64_MAX"/> if allocation failed.</param>
        /// <returns><see cref="S_OK"/> if allocation succeeded, <see cref="E_OUTOFMEMORY"/> if it failed.</returns>
        [return: NativeTypeName("HRESULT")]
        public int Allocate([NativeTypeName("const VIRTUAL_ALLOCATION_DESC*")] D3D12MA_VIRTUAL_ALLOCATION_DESC* pDesc, [NativeTypeName("UINT64 *")] ulong* pOffset)
        {
            if ((pDesc == null) || (pOffset == null) || (pDesc->Size == 0) || !IsPow2(pDesc->Alignment))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to VirtualBlock::Allocate."
                return E_INVALIDARG;
            }

            *pOffset = UINT64_MAX;

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            ulong alignment = pDesc->Alignment != 0 ? pDesc->Alignment : 1;
            D3D12MA_AllocationRequest allocRequest = default;

            if (m_Metadata.CreateAllocationRequest(pDesc->Size, alignment, &allocRequest))
            {
                m_Metadata.Alloc(&allocRequest, pDesc->Size, pDesc->pUserData);
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && m_Metadata.Validate());
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
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (offset != UINT64_MAX));

            m_Metadata.FreeAtOffset(offset);
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && m_Metadata.Validate());
        }

        /// <summary>Frees all the allocations.</summary>
        public void Clear()
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            m_Metadata.Clear();
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && m_Metadata.Validate());
        }

        /// <summary>Changes custom pointer for an allocation at given offset to a new value.</summary>
        public void SetAllocationUserData([NativeTypeName("UINT64")] ulong offset, void* pUserData)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (offset != UINT64_MAX));

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            m_Metadata.SetAllocationUserData(offset, pUserData);
        }

        /// <summary>Retrieves statistics from the current state of the block.</summary>
        public void CalculateStats(D3D12MA_StatInfo* pInfo)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pInfo != null));
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && m_Metadata.Validate());
            m_Metadata.CalcAllocationStatInfo(pInfo);
        }

        /// <summary>Builds and returns statistics as a string in JSON format, including the list of allocations with their parameters.</summary>
        /// <param name="ppStatsString">Must be freed using <see cref="FreeStatsString"/>.</param>
        public void BuildStatsString([NativeTypeName("WCHAR**")] ushort** ppStatsString)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (ppStatsString != null));

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            using var sb = new D3D12MA_StringBuilder(ref m_AllocationCallbacks);

            using (var json = new D3D12MA_JsonWriter(ref m_AllocationCallbacks, &sb))
            {
                D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && m_Metadata.Validate());
                m_Metadata.WriteAllocationInfoToJson(&json);
            }

            nuint length = sb.GetLength();
            ushort* result = AllocateArray<ushort>(ref m_AllocationCallbacks, length + 1);
            _ = memcpy(result, sb.GetData(), length * sizeof(ushort));
            result[length] = '\0';
            *ppStatsString = result;
        }

        /// <summary>Frees memory of a string returned from <see cref="BuildStatsString"/>.</summary>
        public void FreeStatsString([NativeTypeName("WCHAR*")] ushort* pStatsString)
        {
            if (pStatsString != null)
            {
                using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
                Free(ref m_AllocationCallbacks, pStatsString);
            }
        }

        internal static void _ctor(ref D3D12MA_VirtualBlock pThis, [NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("const VIRTUAL_BLOCK_DESC&")] D3D12MA_VIRTUAL_BLOCK_DESC* desc)
        {
            _ctor(ref pThis, allocationCallbacks, desc->Size);
        }

        void IDisposable.Dispose()
        {
            // THIS IS AN IMPORTANT ASSERT!
            // Hitting it means you have some memory leak - unreleased allocations in this virtual block.

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && m_Metadata.IsEmpty()); // "Some allocations were not freed before destruction of this virtual block!"

            m_Metadata.Dispose();
        }
    }
}
