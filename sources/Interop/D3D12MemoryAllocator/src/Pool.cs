// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    /// <summary>
    /// Custom memory pool
    /// <para>
    /// Represents a separate set of heaps (memory blocks) that can be used to create
    /// D3D12MA::Allocation-s and resources in it.Usually there is no need to create custom
    /// pools - creating resources in default pool is sufficient.
    /// </para>
    /// <para>To create custom pool, fill D3D12MA::POOL_DESC and call D3D12MA::Allocator::CreatePool.</para>
    /// </summary>
    public unsafe partial struct Pool : IDisposable
    {
        internal PoolPimpl* m_Pimpl;

        /// <summary>
        /// Deletes pool object, frees D3D12 heaps (memory blocks) managed by it. Allocations and resources must already be released!
        /// <para>
        /// It doesn't delete allocations and resources created in this pool. They must be all
        /// released before calling this function!
        /// </para>
        /// </summary>
        public partial void Release();

        /// <summary>
        /// Returns copy of parameters of the pool.
        /// <para>These are the same parameters as passed to D3D12MA::Allocator::CreatePool.</para>
        /// </summary>
        public readonly partial POOL_DESC GetDesc();

        /// <summary>
        /// Sets the minimum number of bytes that should always be allocated (reserved) in this pool.
        /// <para>See also: \subpage reserving_memory.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public partial int SetMinBytes([NativeTypeName("UINT64")] ulong minBytes);

        /// <summary>Retrieves statistics from the current state of this pool.</summary>
        public partial void CalculateStats(StatInfo* pStats);

        /// <summary>
        /// Associates a name with the pool. This name is for use in debug diagnostics and tools.
        /// <para>
        /// Internal copy of the string is made, so the memory pointed by the argument can be
        /// changed of freed immediately after this call.
        /// </para>
        /// </summary>
        /// <param name="Name">`Name` can be null.</param>
        public partial void SetName([NativeTypeName("LPCWSTR")] char* Name);

        /// <summary>
        /// Returns the name associated with the pool object.
        /// <para>Returned string points to an internal copy.</para>
        /// <para>If no name was associated with the allocation, returns NULL.</para>
        /// </summary>
        [return: NativeTypeName("LPCWSTR")]
        public partial char* GetName();

        internal Pool(Allocator* allocator, POOL_DESC* desc)
        {
            m_Pimpl = D3D12MA_NEW<PoolPimpl>(allocator->m_Pimpl->GetAllocs());
            *m_Pimpl = new(allocator->m_Pimpl, desc);
        }

        public partial void Dispose();
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Private class AllocatorPimpl implementation

    public unsafe partial struct Pool
    {
        public partial void Dispose()
        {
            m_Pimpl->GetAllocator()->UnregisterPool((Pool*)Unsafe.AsPointer(ref this), m_Pimpl->GetDesc()->HeapType);

            D3D12MA_DELETE(m_Pimpl->GetAllocator()->GetAllocs(), m_Pimpl);
        }

        public partial void Release()
        {
            if (Unsafe.IsNullRef(ref this))
            {
                return;
            }

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            D3D12MA_DELETE(m_Pimpl->GetAllocator()->GetAllocs(), (Pool*)Unsafe.AsPointer(ref this));
        }

        public readonly partial POOL_DESC GetDesc()
        {
            return *m_Pimpl->GetDesc();
        }

        public partial int SetMinBytes(ulong minBytes)
        {
            return m_Pimpl->SetMinBytes(minBytes);
        }

        public partial void CalculateStats(StatInfo* pStats)
        {
            D3D12MA_ASSERT(pStats);
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            m_Pimpl->CalculateStats(pStats);
        }

        public partial void SetName(char* Name)
        {
            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK
            m_Pimpl->SetName(Name);
        }

        public partial char* GetName()
        {
            return m_Pimpl->GetName();
        }
    }
}
