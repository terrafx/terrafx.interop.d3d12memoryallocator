// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    /// <summary>
    /// Custom memory pool
    /// <para>
    /// Represents a separate set of heaps (memory blocks) that can be used to create
    /// <see cref="D3D12MA_Allocation"/>-s and resources in it. Usually there is no need to create custom
    /// pools - creating resources in default pool is sufficient.
    /// </para>
    /// <para>To create custom pool, fill <see cref="D3D12MA_POOL_DESC"/> and call <see cref="D3D12MA_Allocator.CreatePool"/>.</para>
    /// </summary>
    public unsafe partial struct D3D12MA_Pool : IDisposable
    {
        private static readonly void** Vtbl = InitVtbl();

        private static void** InitVtbl()
        {
            void** lpVtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MA_Pool), sizeof(void*) * 4);

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
        /// Deletes pool object, frees D3D12 heaps (memory blocks) managed by it. Allocations and resources must already be released!
        /// <para>It doesn't delete allocations and resources created in this pool. They must be all released before calling this function!</para>
        /// </summary>
        private void ReleaseThis()
        {
            if (Unsafe.IsNullRef(ref this))
            {
                return;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            D3D12MA_DELETE(GetAllocator()->GetAllocs(), ref this);
        }

        [UnmanagedCallersOnly]
        private static void ReleaseThis(D3D12MA_IUnknownImpl* pThis)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pThis->lpVtbl == Vtbl));
            ((D3D12MA_Pool*)pThis)->ReleaseThis();
        }


        /// <summary>
        /// Returns copy of parameters of the pool.
        /// <para>These are the same parameters as passed to <see cref="D3D12MA_Allocator.CreatePool"/>.</para>
        /// </summary>
        public readonly D3D12MA_POOL_DESC GetDesc() => m_Desc;

        /// <summary>Retrieves statistics from the current state of this pool.</summary>
        public void CalculateStats(D3D12MA_StatInfo* pStats)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pStats != null));
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            CalculateStatsPimpl(pStats);
        }

        /// <summary>
        /// Associates a name with the pool. This name is for use in debug diagnostics and tools.
        /// <para>Internal copy of the string is made, so the memory pointed by the argument can be changed of freed immediately after this call.</para>
        /// </summary>
        /// <param name="Name">`Name` can be null.</param>
        public void SetName([NativeTypeName("LPCWSTR")] ushort* Name)
        {
            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();
            SetNamePimpl(Name);
        }

        /// <summary>
        /// Returns the name associated with the pool object.
        /// <para>Returned string points to an internal copy.</para>
        /// <para>If no name was associated with the allocation, returns <see langword="null"/>.</para>
        /// </summary>
        [return: NativeTypeName("LPCWSTR")]
        public ushort* GetName() => m_Name;
    }
}
