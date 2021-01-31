// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.Allocation.Type;
using static TerraFX.Interop.Allocation._Anonymous_e__Union;

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents single memory allocation.
    /// <para>It may be either implicit memory heap dedicated to a single resource or a specific region of a bigger heap plus unique offset.</para>
    /// <para>To create such object, fill structure <see cref="ALLOCATION_DESC"/> and call function <see cref="Allocator.CreateResource"/>.</para>
    /// <para>The object remembers size and some other information. To retrieve this information, use methods of this class.</para>
    /// <para>The object also remembers <see cref="ID3D12Resource"/> and "owns" a reference to it, so it calls <see cref="IUnknown.Release"/> on the resource when destroyed.</para>
    /// </summary>
    public unsafe struct Allocation : IDisposable
    {
        internal AllocatorPimpl* m_Allocator;

        [NativeTypeName("UINT64")]
        internal ulong m_Size;

        internal ID3D12Resource* m_Resource;

        [NativeTypeName("UINT")]
        internal uint m_CreationFrameIndex;

        [NativeTypeName("wchar_t*")]
        internal ushort* m_Name;

        internal _Anonymous_e__Union m_Union;
        internal PackedData m_PackedData;

        /// <summary>
        /// Deletes this object.
        /// <para>This function must be used instead of destructor, which is private. There is no reference counting involved.</para>
        /// </summary>
        public void Release()
        {
            if (Unsafe.IsNullRef(ref this))
            {
                return;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            SAFE_RELEASE(ref m_Resource);

            switch (m_PackedData.GetType())
            {
                case TYPE_COMMITTED:
                {
                    m_Allocator->FreeCommittedMemory(ref this);
                    break;
                }

                case TYPE_PLACED:
                {
                    m_Allocator->FreePlacedMemory(ref this);
                    break;
                }

                case TYPE_HEAP:
                {
                    m_Allocator->FreeHeapMemory(ref this);
                    break;
                }
            }

            FreeName();

            m_Allocator->GetAllocationObjectAllocator()->Free(ref this);
        }

        /// <summary>
        /// Returns offset in bytes from the start of memory heap.
        /// <para>
        /// You usually don't need to use this offset. If you create a buffer or a texture together with the allocation using function
        /// <see cref="Allocator.CreateResource"/>, functions that operate on that resource refer to the beginning of the resource, not entire memory heap.
        /// </para>
        /// </summary>
        /// <returns>If the <see cref="Allocation"/> represents committed resource with implicit heap, returns 0.</returns>
        [return: NativeTypeName("UINT64")]
        public readonly ulong GetOffset()
        {
            switch (m_PackedData.GetType())
            {
                case TYPE_COMMITTED:
                case TYPE_HEAP:
                {
                    return 0;
                }

                case TYPE_PLACED:
                {
                    return m_Placed.offset;
                }

                default:
                {
                    D3D12MA_ASSERT(false);
                    return 0;
                }
            }
        }

        /// <summary>
        /// Returns size in bytes of the allocation.
        /// <para>- If you created a buffer or a texture together with the allocation using function <see cref="Allocator.CreateResource"/>, this is the size of the resource returned by <see cref="ID3D12Device.GetResourceAllocationInfo"/>.</para>
        /// <para>- For allocations made out of bigger memory blocks, this also is the size of the memory region assigned exclusively to this allocation.</para>
        /// <para>
        /// - For resources created as committed, this value may not be accurate. DirectX implementation may optimize memory usage internally so that you may
        /// even observe regions of <see cref="ID3D12Resource.GetGPUVirtualAddress"/> + <see cref="GetSize"/> to overlap in memory and still work correctly.
        /// </para>
        /// </summary>
        /// <returns>The size in bytes of the allocation.</returns>
        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() => m_Size;

        /// <summary>
        /// Returns D3D12 resource associated with this object.
        /// <para>Calling this method doesn't increment resource's reference counter.</para>
        /// </summary>
        /// <returns>The D3D12 resource.</returns>
        public readonly ID3D12Resource* GetResource() => m_Resource;

        /// <summary>
        /// Returns memory heap that the resource is created in.
        /// <para>If the <see cref="Allocation"/> represents committed resource with implicit heap, returns <see langword="null"/>.</para>
        /// </summary>
        /// <returns>The memory heap that the resource is created in.</returns>
        public readonly ID3D12Heap* GetHeap()
        {
            switch (m_PackedData.GetType())
            {
                case TYPE_COMMITTED:
                {
                    return null;
                }

                case TYPE_PLACED:
                {
                    return m_Placed.block->GetHeap();
                }

                case TYPE_HEAP:
                {
                    return m_Heap.heap;
                }

                default:
                {
                    D3D12MA_ASSERT(false);
                    return null;
                }
            }
        }

        /// <summary>
        /// Associates a name with the allocation object. This name is for use in debug diagnostics and tools.
        /// <para>Internal copy of the string is made, so the memory pointed by the argument can be changed of freed immediately after this call.</para>
        /// </summary>
        /// <param name="Name">`Name` can be null.</param>
        public void SetName([NativeTypeName("LPCWSTR")] ushort* Name)
        {
            FreeName();

            if (Name != null)
            {
                nuint nameCharCount = wcslen(Name) + 1;
                m_Name = D3D12MA_NEW_ARRAY<ushort>(m_Allocator->GetAllocs(), nameCharCount);
                memcpy(m_Name, Name, nameCharCount * sizeof(ushort));
            }
        }

        /// <summary>
        /// Returns the name associated with the allocation object.
        /// <para>Returned string points to an internal copy.</para>
        /// <para>If no name was associated with the allocation, returns null.</para>
        /// </summary>
        /// <returns>The name associated with the allocation object.</returns>
        [return: NativeTypeName("LPCWSTR")]
        public readonly ushort* GetName() => m_Name;

        /// <summary>
        /// Returns <see langword="true"/> if the memory of the allocation was filled with zeros when the allocation was created.
        /// <para>Returns <see langword="true"/> only if the allocator is sure that the entire memory where the allocation was created was filled with zeros at the moment the allocation was made.</para>
        /// <para>
        /// Returns <see langword="false"/> if the memory could potentially contain garbage data.
        /// If it's a render-target or depth-stencil texture, it then needs proper initialization with <see cref="ID3D12GraphicsCommandList.ClearRenderTargetView"/>, <see cref="ID3D12GraphicsCommandList.ClearDepthStencilView"/>, <see cref="ID3D12GraphicsCommandList.DiscardResource"/>,
        /// or a copy operation, as described on page: <see href="https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-createplacedresource#notes-on-the-required-resource-initialization">"<see cref="ID3D12Device.CreatePlacedResource"/> method - Notes on the required resource initialization"</see>.
        /// Please note that rendering a fullscreen triangle or quad to the texture as a render target is not a proper way of initialization!
        /// </para>
        /// <para>
        /// See also articles:
        /// <see href="https://devblogs.microsoft.com/directx/coming-to-directx-12-more-control-over-memory-allocation/">"Coming to DirectX 12: More control over memory allocation"</see>
        /// <see href="https://asawicki.info/news_1724_initializing_dx12_textures_after_allocation_and_aliasing">"Initializing DX12 Textures After Allocation and Aliasing"</see>
        /// </para>
        /// </summary>
        /// <returns>Whether the memory of the allocation was filled with zeros.</returns>
        [return: NativeTypeName("BOOL")]
        public readonly int WasZeroInitialized() => m_PackedData.WasZeroInitialized();

        internal enum Type
        {
            TYPE_COMMITTED,
            TYPE_PLACED,
            TYPE_HEAP,
            TYPE_COUNT
        }

        internal readonly ref _m_Committed_e__Struct m_Committed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(m_Union.m_Committed), 1));
        }

        internal readonly ref _m_Placed_e__Struct m_Placed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(m_Union.m_Placed), 1));
        }

        internal readonly ref _m_Heap_e__Struct m_Heap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(m_Union.m_Heap), 1));
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            public _m_Committed_e__Struct m_Committed;

            [FieldOffset(0)]
            public _m_Placed_e__Struct m_Placed;

            [FieldOffset(0)]
            public _m_Heap_e__Struct m_Heap;

            public struct _m_Committed_e__Struct
            {
                public D3D12_HEAP_TYPE heapType;
            }

            public struct _m_Placed_e__Struct
            {
                [NativeTypeName("UINT64")]
                public ulong offset;

                public NormalBlock* block;
            }

            public struct _m_Heap_e__Struct
            {
                public D3D12_HEAP_TYPE heapType;
                public ID3D12Heap* heap;
            }
        }

        internal partial struct PackedData
        {
            uint _bitfield0;
            uint _bitfield1;

            public readonly new Type GetType() => (Type)m_Type;

            public readonly D3D12_RESOURCE_DIMENSION GetResourceDimension() => (D3D12_RESOURCE_DIMENSION)m_ResourceDimension;

            public readonly D3D12_RESOURCE_FLAGS GetResourceFlags() => (D3D12_RESOURCE_FLAGS)m_ResourceFlags;

            public readonly D3D12_TEXTURE_LAYOUT GetTextureLayout() => (D3D12_TEXTURE_LAYOUT)m_TextureLayout;

            [return: NativeTypeName("BOOL")]
            public readonly int WasZeroInitialized() => (int)m_WasZeroInitialized;

            public void SetType(Type type)
            {
                uint u = (uint)type;
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (u < (1u << 2)));
                m_Type = u;
            }

            public void SetResourceDimension(D3D12_RESOURCE_DIMENSION resourceDimension)
            {
                uint u = (uint)resourceDimension;
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (u < (1u << 3)));
                m_ResourceDimension = u;
            }

            public void SetResourceFlags(D3D12_RESOURCE_FLAGS resourceFlags)
            {
                uint u = (uint)resourceFlags;
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (u < (1u << 24)));
                m_ResourceFlags = u;
            }

            public void SetTextureLayout(D3D12_TEXTURE_LAYOUT textureLayout)
            {
                uint u = (uint)textureLayout;
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (u < (1u << 9)));
                m_TextureLayout = u;
            }

            public void SetWasZeroInitialized([NativeTypeName("BOOL")] int wasZeroInitialized) => m_WasZeroInitialized = wasZeroInitialized != 0 ? 1 : 0;

            [NativeTypeName("UINT : 2")]
            private uint m_Type
            {
                readonly get => _bitfield0 & 0x3u;
                set => _bitfield0 = (_bitfield0 & ~0x3u) | (value & 0x3u);
            }

            [NativeTypeName("UINT : 3")]
            private uint m_ResourceDimension
            {
                readonly get => (_bitfield0 >> 2) & 0x7u;
                set => _bitfield0 = (_bitfield0 & ~(0x7u << 2)) | ((value & 0x7u) << 2);
            }

            [NativeTypeName("UINT : 24")]
            private uint m_ResourceFlags
            {
                readonly get => (_bitfield0 >> 5) & 0xFFFFFFu;
                set => _bitfield0 = (_bitfield0 & ~(0xFFFFFFu << 5)) | ((value & 0xFFFFFFu) << 5);
            }

            [NativeTypeName("UINT : 9")]
            private uint m_TextureLayout
            {
                readonly get => _bitfield1 & 0x1FFu;
                set => _bitfield1 = (_bitfield1 & ~0x1FFu) | (value & 0x1FFu);
            }

            [NativeTypeName("UINT : 1")]
            private uint m_WasZeroInitialized
            {
                readonly get => (_bitfield1 >> 9) & 0x1u;
                set => _bitfield1 = (_bitfield1 & ~(0x1u << 9)) | ((value & 0x1u) << 9);
            }
        }

        internal void Ctor(AllocatorPimpl* allocator, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("BOOL")] int wasZeroInitialized)
        {
            m_Allocator = allocator;
            m_Size = size;
            m_Resource = null;
            m_CreationFrameIndex = allocator->GetCurrentFrameIndex();
            m_Name = null;

            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (allocator != null));

            m_PackedData.SetType(TYPE_COUNT);
            m_PackedData.SetResourceDimension(D3D12_RESOURCE_DIMENSION_UNKNOWN);
            m_PackedData.SetResourceFlags(D3D12_RESOURCE_FLAG_NONE);
            m_PackedData.SetTextureLayout(D3D12_TEXTURE_LAYOUT_UNKNOWN);
            m_PackedData.SetWasZeroInitialized(wasZeroInitialized);
        }

        void IDisposable.Dispose()
        {
            // Nothing here, everything already done in Release.
        }

        internal void InitCommitted(D3D12_HEAP_TYPE heapType)
        {
            m_PackedData.SetType(TYPE_COMMITTED);
            m_Committed.heapType = heapType;
        }

        internal void InitPlaced([NativeTypeName("UINT64")] ulong offset, [NativeTypeName("UINT64")] ulong alignment, NormalBlock* block)
        {
            m_PackedData.SetType(TYPE_PLACED);
            m_Placed.offset = offset;
            m_Placed.block = block;
        }

        internal void InitHeap(D3D12_HEAP_TYPE heapType, ID3D12Heap* heap)
        {
            m_PackedData.SetType(TYPE_HEAP);
            m_Heap.heapType = heapType;
            m_Heap.heap = heap;
        }

        internal void SetResource(ID3D12Resource* resource, [NativeTypeName("const D3D12_RESOURCE_DESC_T*")] D3D12_RESOURCE_DESC* pResourceDesc)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((m_Resource == null) && (pResourceDesc != null)));
            m_Resource = resource;
            m_PackedData.SetResourceDimension(pResourceDesc->Dimension);
            m_PackedData.SetResourceFlags(pResourceDesc->Flags);
            m_PackedData.SetTextureLayout(pResourceDesc->Layout);
        }

        internal void SetResource(ID3D12Resource* resource, [NativeTypeName("const D3D12_RESOURCE_DESC_T*")] D3D12_RESOURCE_DESC1* pResourceDesc)
        {
            SetResource(resource, (D3D12_RESOURCE_DESC*)pResourceDesc);
        }

        internal void FreeName()
        {
            if (m_Name != null)
            {
                nuint nameCharCount = wcslen(m_Name) + 1;
                D3D12MA_DELETE_ARRAY(m_Allocator->GetAllocs(), m_Name, nameCharCount);
                m_Name = null;
            }
        }
    }
}
