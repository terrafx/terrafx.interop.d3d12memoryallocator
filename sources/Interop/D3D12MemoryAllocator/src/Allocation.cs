// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.Allocation.Type;
using System.Runtime.Intrinsics.X86;

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents single memory allocation.
    /// <para>It may be either implicit memory heap dedicated to a single resource or a specific region of a bigger heap plus unique offset.</para>
    /// <para>To create such object, fill structure D3D12MA::ALLOCATION_DESC and call function Allocator::CreateResource.</para>
    /// <para>The object remembers size and some other information. To retrieve this information, use methods of this class.</para>
    /// <para>The object also remembers `ID3D12Resource` and "owns" a reference to it, so it calls `Release()` on the resource when destroyed.</para>
    /// </summary>
    public unsafe partial struct Allocation : IDisposable
    {
        internal AllocatorPimpl* m_Allocator;
        [NativeTypeName("UINT64")] internal ulong m_Size;
        internal ID3D12Resource* m_Resource;
        [NativeTypeName("UINT")] internal uint m_CreationFrameIndex;
        [NativeTypeName("wchar_t*")] internal char* m_Name;
        internal _Anonymous_e__Union m_Union;
        internal PackedData m_PackedData;

        /// <summary>
        /// Deletes this object.
        /// <para>This function must be used instead of destructor, which is private. There is no reference counting involved.</para>
        /// </summary>
        public partial void Release();

        /// <summary>
        /// Returns offset in bytes from the start of memory heap.
        /// <para>
        /// You usually don't need to use this offset. If you create a buffer or a texture together with the allocation using function
        /// D3D12MA::Allocator::CreateResource, functions that operate on that resource refer to the beginning of the resource, not entire memory heap.
        /// </para>
        /// </summary>
        /// <returns>If the Allocation represents committed resource with implicit heap, returns 0.</returns>
        [return: NativeTypeName("UINT64")]
        public readonly partial ulong GetOffset();

        /// <summary>
        /// Returns size in bytes of the allocation.
        /// <para>- If you created a buffer or a texture together with the allocation using function D3D12MA::Allocator::CreateResource, this is the size of the resource returned by `ID3D12Device::GetResourceAllocationInfo`.</para>
        /// <para>- For allocations made out of bigger memory blocks, this also is the size of the memory region assigned exclusively to this allocation.</para>
        /// <para>
        /// - For resources created as committed, this value may not be accurate. DirectX implementation may optimize memory usage internally so that you may
        /// even observe regions of `ID3D12Resource::GetGPUVirtualAddress()` + Allocation::GetSize() to overlap in memory and still work correctly.
        /// </para>
        /// </summary>
        /// <returns>The size in bytes of the allocation.</returns>
        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize() { return m_Size; }

        /// <summary>
        /// Returns D3D12 resource associated with this object.
        /// <para>Calling this method doesn't increment resource's reference counter.</para>
        /// </summary>
        /// <returns>The D3D12 resource.</returns>
        public readonly ID3D12Resource* GetResource() { return m_Resource; }

        /// <summary>
        /// Returns memory heap that the resource is created in.
        /// <para>If the Allocation represents committed resource with implicit heap, returns NULL.</para>
        /// </summary>
        /// <returns>The memory heap that the resource is created in.</returns>
        public readonly partial ID3D12Heap* GetHeap();

        /// <summary>
        /// Associates a name with the allocation object. This name is for use in debug diagnostics and tools.
        /// <para>Internal copy of the string is made, so the memory pointed by the argument can be changed of freed immediately after this call.</para>
        /// </summary>
        /// <param name="Name">`Name` can be null.</param>
        public partial void SetName([NativeTypeName("LPCWSTR")] char* Name);

        /// <summary>
        /// Returns the name associated with the allocation object.
        /// <para>Returned string points to an internal copy.</para>
        /// <para>If no name was associated with the allocation, returns null.</para>
        /// </summary>
        /// <returns>The name associated with the allocation object.</returns>
        [return: NativeTypeName("LPCWSTR")]
        public readonly char* GetName() { return m_Name; }

        /// <summary>
        /// Returns `TRUE` if the memory of the allocation was filled with zeros when the allocation was created.
        /// <para>Returns `TRUE` only if the allocator is sure that the entire memory where the allocation was created was filled with zeros at the moment the allocation was made.</para>
        /// <para>
        /// Returns `FALSE` if the memory could potentially contain garbage data.
        /// If it's a render-target or depth-stencil texture, it then needs proper initialization with `ClearRenderTargetView`, `ClearDepthStencilView`, `DiscardResource`,
        /// or a copy operation, as described on page: [ID3D12Device::CreatePlacedResource method - Notes on the required resource initialization] (https://docs.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12device-createplacedresource#notes-on-the-required-resource-initialization).
        /// Please note that rendering a fullscreen triangle or quad to the texture as a render target is not a proper way of initialization!
        /// </para>
        /// <para>
        /// See also articles:
        /// ["Coming to DirectX 12: More control over memory allocation"] (https://devblogs.microsoft.com/directx/coming-to-directx-12-more-control-over-memory-allocation/),
        /// ["Initializing DX12 Textures After Allocation and Aliasing"] (https://asawicki.info/news_1724_initializing_dx12_textures_after_allocation_and_aliasing).
        /// </para>
        /// </summary>
        /// <returns>Whether the memory of the allocation was filled with zeros.</returns>
        [return: NativeTypeName("BOOL")]
        public readonly int WasZeroInitialized() { return m_PackedData.WasZeroInitialized(); }

        internal enum Type
        {
            TYPE_COMMITTED,
            TYPE_PLACED,
            TYPE_HEAP,
            TYPE_COUNT
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct _Anonymous_e__Union
        {
            [FieldOffset(0)] public m_Committed_t m_Committed;
            [FieldOffset(0)] public m_Placed_t m_Placed;
            [FieldOffset(0)] public m_Heap_t m_Heap;

            public struct m_Committed_t
            {
                public D3D12_HEAP_TYPE heapType;
            }

            public struct m_Placed_t
            {
                [NativeTypeName("UINT64")] public ulong offset;
                public NormalBlock* block;
            }

            public struct m_Heap_t
            {
                public D3D12_HEAP_TYPE heapType;
                public ID3D12Heap* heap;
            }
        }

        internal partial struct PackedData
        {
            ulong __value;

            public readonly new Type GetType() { return (Type)m_Type; }

            public readonly D3D12_RESOURCE_DIMENSION GetResourceDimension() { return (D3D12_RESOURCE_DIMENSION)m_ResourceDimension; }

            public readonly D3D12_RESOURCE_FLAGS GetResourceFlags() { return (D3D12_RESOURCE_FLAGS)m_ResourceFlags; }

            public readonly D3D12_TEXTURE_LAYOUT GetTextureLayout() { return (D3D12_TEXTURE_LAYOUT)m_TextureLayout; }

            [return: NativeTypeName("BOOL")]
            public readonly int WasZeroInitialized() { return (int)m_WasZeroInitialized; }

            public partial void SetType(Type type);

            public partial void SetResourceDimension(D3D12_RESOURCE_DIMENSION resourceDimension);

            public partial void SetResourceFlags(D3D12_RESOURCE_FLAGS resourceFlags);

            public partial void SetTextureLayout(D3D12_TEXTURE_LAYOUT textureLayout);

            public void SetWasZeroInitialized([NativeTypeName("BOOL")] int wasZeroInitialized) { m_WasZeroInitialized = wasZeroInitialized > 0 ? 1 : 0; }

            [NativeTypeName("UINT")]
            uint m_Type
            {
                readonly get => (uint)BitHelper.ExtractRange(__value, 0, 2);
                set => __value = BitHelper.SetRange(__value, 0, 2, value);
            }

            [NativeTypeName("UINT")]
            uint m_ResourceDimension
            {
                readonly get => (uint)BitHelper.ExtractRange(__value, 2, 3);
                set => __value = BitHelper.SetRange(__value, 2, 3, value);
            }

            [NativeTypeName("UINT")]
            uint m_ResourceFlags
            {
                readonly get => (uint)BitHelper.ExtractRange(__value, 5, 24);
                set => __value = BitHelper.SetRange(__value, 5, 24, value);
            }

            [NativeTypeName("UINT")]
            uint m_TextureLayout
            {
                readonly get => (uint)BitHelper.ExtractRange(__value, 29, 9);
                set => __value = BitHelper.SetRange(__value, 29, 9, value);
            }

            [NativeTypeName("UINT")]
            uint m_WasZeroInitialized
            {
                readonly get => (uint)BitHelper.ExtractRange(__value, 38, 1);
                set => __value = BitHelper.SetRange(__value, 38, 1, value);
            }
        }

        internal Allocation(AllocatorPimpl* allocator, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("BOOL")] int wasZeroInitialized)
        {
            Unsafe.SkipInit(out this);

            m_Allocator = allocator;
            m_Size = size;
            m_Resource = null;
            m_CreationFrameIndex = allocator->GetCurrentFrameIndex();
            m_Name = null;

            D3D12MA_ASSERT(allocator);

            m_PackedData.SetType(TYPE_COUNT);
            m_PackedData.SetResourceDimension(D3D12_RESOURCE_DIMENSION_UNKNOWN);
            m_PackedData.SetResourceFlags(D3D12_RESOURCE_FLAG_NONE);
            m_PackedData.SetTextureLayout(D3D12_TEXTURE_LAYOUT_UNKNOWN);
            m_PackedData.SetWasZeroInitialized(wasZeroInitialized);
        }

        public partial void Dispose();

        internal partial void InitCommitted(D3D12_HEAP_TYPE heapType);

        internal partial void InitPlaced([NativeTypeName("UINT64")] ulong offset, [NativeTypeName("UINT64")] ulong alignment, NormalBlock* block);

        internal partial void InitHeap(D3D12_HEAP_TYPE heapType, ID3D12Heap* heap);

        internal partial void SetResource<TD3D12_RESOURCE_DESC>(ID3D12Resource* resource, TD3D12_RESOURCE_DESC* pResourceDesc)
            where TD3D12_RESOURCE_DESC : unmanaged;

        internal partial void FreeName();
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Public class Allocation implementation

    public unsafe partial struct Allocation
    {
        internal partial struct PackedData
        {
            public partial void SetType(Type type)
            {
                uint u = (uint)type;
                D3D12MA_ASSERT(u < (1u << 2));
                m_Type = u;
            }

            public partial void SetResourceDimension(D3D12_RESOURCE_DIMENSION resourceDimension)
            {
                uint u = (uint)resourceDimension;
                D3D12MA_ASSERT(u < (1u << 3));
                m_ResourceDimension = u;
            }

            public partial void SetResourceFlags(D3D12_RESOURCE_FLAGS resourceFlags)
            {
                uint u = (uint)resourceFlags;
                D3D12MA_ASSERT(u < (1u << 24));
                m_ResourceFlags = u;
            }

            public partial void SetTextureLayout(D3D12_TEXTURE_LAYOUT textureLayout)
            {
                uint u = (uint)textureLayout;
                D3D12MA_ASSERT(u < (1u << 9));
                m_TextureLayout = u;
            }
        }

        public partial void Release()
        {
            if (Unsafe.IsNullRef(ref this))
            {
                return;
            }

            //D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK

            SAFE_RELEASE(&((Allocation*)Unsafe.AsPointer(ref this))->m_Resource);

            switch (m_PackedData.GetType())
            {
                case TYPE_COMMITTED:
                    m_Allocator->FreeCommittedMemory((Allocation*)Unsafe.AsPointer(ref this));
                    break;
                case TYPE_PLACED:
                    m_Allocator->FreePlacedMemory((Allocation*)Unsafe.AsPointer(ref this));
                    break;
                case TYPE_HEAP:
                    m_Allocator->FreeHeapMemory((Allocation*)Unsafe.AsPointer(ref this));
                    break;
            }

            FreeName();

            m_Allocator->GetAllocationObjectAllocator()->Free((Allocation*)Unsafe.AsPointer(ref this));
        }

        public readonly partial ulong GetOffset()
        {
            switch (m_PackedData.GetType())
            {
                case TYPE_COMMITTED:
                case TYPE_HEAP:
                    return 0;
                case TYPE_PLACED:
                    return m_Union.m_Placed.offset;
                default:
                    D3D12MA_ASSERT(0);
                    return 0;
            }
        }

        public readonly partial ID3D12Heap* GetHeap()
        {
            switch (m_PackedData.GetType())
            {
                case TYPE_COMMITTED:
                    return null;
                case TYPE_PLACED:
                    return m_Union.m_Placed.block->@base.GetHeap();
                case TYPE_HEAP:
                    return m_Union.m_Heap.heap;
                default:
                    D3D12MA_ASSERT(0);
                    return null;
            }
        }

        public partial void SetName(char* Name)
        {
            FreeName();

            if (Name != null)
            {
                nuint nameCharCount = wcslen(Name) + 1;
                m_Name = D3D12MA_NEW_ARRAY<char>(m_Allocator->GetAllocs(), nameCharCount);
                memcpy(m_Name, Name, nameCharCount * sizeof(char));
            }
        }

        public partial void Dispose()
        {
            // Nothing here, everything already done in Release.
        }

        internal partial void InitCommitted(D3D12_HEAP_TYPE heapType)
        {
            m_PackedData.SetType(TYPE_COMMITTED);
            m_Union.m_Committed.heapType = heapType;
        }

        internal partial void InitPlaced(ulong offset, ulong alignment, NormalBlock* block)
        {
            m_PackedData.SetType(TYPE_PLACED);
            m_Union.m_Placed.offset = offset;
            m_Union.m_Placed.block = block;
        }

        internal partial void InitHeap(D3D12_HEAP_TYPE heapType, ID3D12Heap* heap)
        {
            m_PackedData.SetType(TYPE_HEAP);
            m_Union.m_Heap.heapType = heapType;
            m_Union.m_Heap.heap = heap;
        }

        internal partial void SetResource<TD3D12_RESOURCE_DESC>(ID3D12Resource* resource, TD3D12_RESOURCE_DESC* pResourceDesc)
            where TD3D12_RESOURCE_DESC : unmanaged
        {
            D3D12MA_ASSERT(m_Resource == null && pResourceDesc != null);
            m_Resource = resource;
            m_PackedData.SetResourceDimension(((D3D12_RESOURCE_DESC*)pResourceDesc)->Dimension);
            m_PackedData.SetResourceFlags(((D3D12_RESOURCE_DESC*)pResourceDesc)->Flags);
            m_PackedData.SetTextureLayout(((D3D12_RESOURCE_DESC*)pResourceDesc)->Layout);
        }

        internal partial void FreeName()
        {
            if (m_Name != null)
            {
                nuint nameCharCount = wcslen(m_Name) + 1;
                D3D12MA_DELETE_ARRAY_NO_DISPOSE(m_Allocator->GetAllocs(), m_Name, nameCharCount);
                m_Name = null;
            }
        }

        internal partial struct PackedData
        {
            /// <summary>
            /// Helpers to perform bit operations on numeric types.
            /// Ported from Microsoft.Toolkit.HighPerformance:
            /// <a href="https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/master/Microsoft.Toolkit.HighPerformance/Helpers/BitHelper.cs"/>
            /// </summary>
            private static class BitHelper
            {
                /// <summary>
                /// Extracts a bit field range from a given value.
                /// </summary>
                /// <param name="value">The input <see cref="ulong"/> value.</param>
                /// <param name="start">The initial index of the range to extract (in [0, 63] range).</param>
                /// <param name="length">The length of the range to extract (depends on <paramref name="start"/>).</param>
                /// <returns>The value of the extracted range within <paramref name="value"/>.</returns>
                /// <remarks>
                /// This method doesn't validate <paramref name="start"/> and <paramref name="length"/>.
                /// If either parameter is not valid, the result will just be inconsistent. The method
                /// should not be used to set all the bits at once, and it is not guaranteed to work in
                /// that case, which would just be equivalent to assigning the <see cref="ulong"/> value.
                /// Additionally, no conditional branches are used to retrieve the range.
                /// </remarks>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static ulong ExtractRange(ulong value, byte start, byte length)
                {
                    if (Bmi1.X64.IsSupported)
                    {
                        return Bmi1.X64.BitFieldExtract(value, start, length);
                    }

                    return (value >> start) & ((1ul << length) - 1ul);
                }

                /// <summary>
                /// Sets a bit field range within a target value.
                /// </summary>
                /// <param name="value">The initial <see cref="ulong"/> value.</param>
                /// <param name="start">The initial index of the range to extract (in [0, 63] range).</param>
                /// <param name="length">The length of the range to extract (depends on <paramref name="start"/>).</param>
                /// <param name="flags">The input flags to insert in the target range.</param>
                /// <returns>The updated bit field value after setting the specified range.</returns>
                /// <remarks>
                /// Just like <see cref="ExtractRange(ulong,byte,byte)"/>, this method doesn't validate the parameters
                /// and does not contain branching instructions, so it's well suited for use in tight loops as well.
                /// </remarks>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static ulong SetRange(ulong value, byte start, byte length, ulong flags)
                {
                    ulong
                        highBits = (1ul << length) - 1ul,
                        loadMask = highBits << start,
                        storeMask = (flags & highBits) << start;

                    if (Bmi1.X64.IsSupported)
                    {
                        return Bmi1.X64.AndNot(loadMask, value) | storeMask;
                    }

                    return (~loadMask & value) | storeMask;
                }
            }
        }
    }
}
