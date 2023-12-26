// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12MA_Allocation.Type;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX;

/// <summary>Represents single memory allocation.</summary>
/// <remarks>
///   <para>It may be either implicit memory heap dedicated to a single resource or a specific region of a bigger heap plus unique offset.</para>
///   <para>To create such object, fill structure <see cref="D3D12MA_ALLOCATION_DESC" /> and call function <see cref="D3D12MA_Allocator.CreateResource" />.</para>
///   <para>The object remembers size and some other information. To retrieve this information, use methods of this class.</para>
///   <para>The object also remembers <see cref="ID3D12Resource" /> and "owns" a reference to it, so it calls <see cref="ID3D12Resource.Release" /> on the resource when destroyed.</para>
/// </remarks>
[NativeTypeName("class D3D12MA::Allocation : D3D12MA::IUnknownImpl")]
[NativeInheritance("D3D12MA::IUnknownImpl")]
public unsafe partial struct D3D12MA_Allocation : D3D12MA_IUnknownImpl.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_NULL));

    public D3D12MA_IUnknownImpl Base;

    private D3D12MA_AllocatorPimpl* m_Allocator;

    [NativeTypeName("UINT64")]
    private ulong m_Size;

    [NativeTypeName("UINT64")]
    private ulong m_Alignment;

    private ID3D12Resource* m_Resource;

    private void* m_pPrivateData;

    [NativeTypeName("wchar_t *")]
    private char* m_Name;

    internal _Anonymous_e__Union Anonymous;

    internal PackedData m_PackedData;

    private void _ctor()
    {
        Base = new D3D12MA_IUnknownImpl {
            lpVtbl = VtblInstance,
        };
    }

    internal void _ctor(D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, BOOL wasZeroInitialized)
    {
        _ctor();

        m_Allocator = allocator;
        m_Size = size;
        m_Alignment = alignment;

        D3D12MA_ASSERT(allocator != null);

        m_Resource = null;
        m_pPrivateData = null;
        m_Name = null;
        Anonymous = new _Anonymous_e__Union();

        m_PackedData.SetType(TYPE_COUNT);
        m_PackedData.SetResourceDimension(D3D12_RESOURCE_DIMENSION_UNKNOWN);
        m_PackedData.SetResourceFlags(D3D12_RESOURCE_FLAG_NONE);
        m_PackedData.SetTextureLayout(D3D12_TEXTURE_LAYOUT_UNKNOWN);
        m_PackedData.SetWasZeroInitialized(wasZeroInitialized);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("REFIID")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged<D3D12MA_Allocation*, Guid*, void**, int>)(Base.lpVtbl[0]))((D3D12MA_Allocation*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged<D3D12MA_Allocation*, uint>)(Base.lpVtbl[1]))((D3D12MA_Allocation*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged<D3D12MA_Allocation*, uint>)(Base.lpVtbl[2]))((D3D12MA_Allocation*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    void IDisposable.Dispose()
    {
        ((delegate* unmanaged<D3D12MA_Allocation*, void>)(Base.lpVtbl[3]))((D3D12MA_Allocation*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    void D3D12MA_IUnknownImpl.Interface.ReleaseThis()
    {
        ((delegate* unmanaged<D3D12MA_Allocation*, void>)(Base.lpVtbl[4]))((D3D12MA_Allocation*)Unsafe.AsPointer(ref this));
    }

    /// <summary>Returns offset in bytes from the start of memory heap.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <para>You usually don't need to use this offset. If you create a buffer or a texture together with the allocation using function <see cref="D3D12MA_Allocator.CreateResource" />, functions that operate on that resource refer to the beginning of the resource, not entire memory heap.</para>
    ///   <para>If the Allocation represents committed resource with implicit heap, returns 0.</para>
    /// </remarks>
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
                return Anonymous.m_Placed.block->m_pMetadata->GetAllocationOffset(Anonymous.m_Placed.allocHandle);
            }

            default:
            {
                D3D12MA_FAIL();
                return 0;
            }
        }
    }

    /// <summary>Returns alignment that resource was created with.</summary>
    /// <returns></returns>
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetAlignment()
    {
        return m_Alignment;
    }

    /// <summary>Returns size in bytes of the allocation.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>If you created a buffer or a texture together with the allocation using function <see cref="D3D12MA_Allocator.CreateResource" />, this is the size of the resource returned by <see cref="ID3D12Device.GetResourceAllocationInfo" />.</description>
    ///     </item>
    ///     <item>
    ///       <description>For allocations made out of bigger memory blocks, this also is the size of the memory region assigned exclusively to this allocation.</description>
    ///     </item>
    ///     <item>
    ///       <description>For resources created as committed, this value may not be accurate. DirectX implementation may optimize memory usage internally so that you may even observe regions of <c>ID3D12Resource.GetGPUVirtualAddress() + D3D12MA_Allocation.GetSize()</c> to overlap in memory and still work correctly.</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    [return: NativeTypeName("UINT64")]
    public readonly ulong GetSize()
    {
        return m_Size;
    }

    /// <summary>Returns D3D12 resource associated with this object.</summary>
    /// <returns></returns>
    /// <remarks>Calling this method doesn't increment resource's reference counter.</remarks>
    public readonly ID3D12Resource* GetResource()
    {
        return m_Resource;
    }

    /// <summary>Releases the resource currently pointed by the allocation (if any), sets it to new one, incrementing its reference counter (if not null).</summary>
    /// <param name="pResource"></param>
    public void SetResource(ID3D12Resource* pResource)
    {
        if (pResource != m_Resource)
        {
            if (m_Resource != null)
            {
                _ = m_Resource->Release();
            }

            m_Resource = pResource;

            if (m_Resource != null)
            {
                _ = m_Resource->AddRef();
            }
        }
    }

    /// <summary>Returns memory heap that the resource is created in.</summary>
    /// <returns></returns>
    /// <remarks>If the Allocation represents committed resource with implicit heap, returns <c>null</c>.</remarks>
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
                return Anonymous.m_Placed.block->GetHeap();
            }

            case TYPE_HEAP:
            {
                return Anonymous.m_Heap.heap;
            }

            default:
            {
                D3D12MA_FAIL();
                return null;
            }
        }
    }

    /// <summary>Changes custom pointer for an allocation to a new value.</summary>
    /// <param name="pPrivateData"></param>
    public void SetPrivateData(void* pPrivateData)
    {
        m_pPrivateData = pPrivateData;
    }

    /// <summary>Get custom pointer associated with the allocation.</summary>
    /// <returns></returns>
    public readonly void* GetPrivateData()
    {
        return m_pPrivateData;
    }

    /// <summary>Associates a name with the allocation object. This name is for use in debug diagnostics and tools.</summary>
    /// <param name="Name"></param>
    /// <remarks>
    ///   <para>Internal copy of the string is made, so the memory pointed by the argument can be changed of freed immediately after this call.</para>
    ///   <para><paramref name="Name" /> can be null.</para>
    /// </remarks>
    public void SetName([NativeTypeName("LPCWSTR")] char* Name)
    {
        FreeName();

        if (Name != null)
        {
            nuint nameCharCount = wcslen(Name) + 1;
            m_Name = D3D12MA_NEW_ARRAY<char>(m_Allocator->GetAllocs(), nameCharCount);
            _ = memcpy(m_Name, Name, nameCharCount * sizeof(char));
        }
    }

    /// <summary>Returns the name associated with the allocation object.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <para>Returned string points to an internal copy.</para>
    ///   <para>If no name was associated with the allocation, returns null.</para>
    /// </remarks>
    [return: NativeTypeName("LPCWSTR")]
    public readonly char* GetName()
    {
        return m_Name;
    }

    /// <summary>Returns <see cref="TRUE" /> if the memory of the allocation was filled with zeros when the allocation was created.</summary>
    /// <returns></returns>
    /// <remarks>
    ///   <para>Returns <see cref="TRUE" /> only if the allocator is sure that the entire memory where the allocation was created was filled with zeros at the moment the allocation was made.</para>
    ///   <para>Returns <see cref="FALSE" /> if the memory could potentially contain garbage data. If it's a render-target or depth-stencil texture, it then needs proper initialization with <see cref="ID3D12GraphicsCommandList.ClearRenderTargetView" />, <see cref="ID3D12GraphicsCommandList.ClearDepthStencilView" />, <see cref="ID3D12GraphicsCommandList.DiscardResource" />, or a copy operation, as described on page <see cref="ID3D12Device.CreatePlacedResource" /> method - Notes on the required resource initialization" in Microsoft documentation. Please note that rendering a fullscreen triangle or quad to the texture as a render target is not a proper way of initialization!</para>
    ///   <para>See also articles:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>"Coming to DirectX 12: More control over memory allocation" on DirectX Developer Blog</description>
    ///     </item>
    ///     <item>
    ///       <description>https://asawicki.info/news_1724_initializing_dx12_textures_after_allocation_and_aliasing</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public readonly BOOL WasZeroInitialized()
    {
        return m_PackedData.WasZeroInitialized();
    }

    internal void InitCommitted(D3D12MA_CommittedAllocationList* list)
    {
        m_PackedData.SetType(TYPE_COMMITTED);
        Anonymous.m_Committed.list = list;
        Anonymous.m_Committed.prev = null;
        Anonymous.m_Committed.next = null;
    }

    internal void InitPlaced([NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, D3D12MA_NormalBlock* block)
    {
        m_PackedData.SetType(TYPE_PLACED);
        Anonymous.m_Placed.allocHandle = allocHandle;
        Anonymous.m_Placed.block = block;
    }

    internal void InitHeap(D3D12MA_CommittedAllocationList* list, ID3D12Heap* heap)
    {
        m_PackedData.SetType(TYPE_HEAP);
        Anonymous.m_Heap.list = list;
        Anonymous.m_Committed.prev = null;
        Anonymous.m_Committed.next = null;
        Anonymous.m_Heap.heap = heap;
    }

    internal void SwapBlockAllocation(D3D12MA_Allocation* allocation)
    {
        D3D12MA_ASSERT(allocation != null);
        D3D12MA_ASSERT(m_PackedData.GetType() == TYPE_PLACED);
        D3D12MA_ASSERT(allocation->m_PackedData.GetType() == TYPE_PLACED);

        D3D12MA_SWAP(ref m_Resource, ref allocation->m_Resource);
        m_PackedData.SetWasZeroInitialized(allocation->m_PackedData.WasZeroInitialized());
        Anonymous.m_Placed.block->m_pMetadata->SetAllocationPrivateData(Anonymous.m_Placed.allocHandle, allocation);

        D3D12MA_SWAP(ref Anonymous.m_Placed, ref allocation->Anonymous.m_Placed);
        Anonymous.m_Placed.block->m_pMetadata->SetAllocationPrivateData(Anonymous.m_Placed.allocHandle, Unsafe.AsPointer(ref this));
    }

    // If the D3D12MA_Allocation represents committed resource with implicit heap, returns ulong.MaxValue.
    [return: NativeTypeName("D3D12MA::AllocHandle")]
    internal readonly ulong GetAllocHandle()
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
                return Anonymous.m_Placed.allocHandle;
            }

            default:
            {
                D3D12MA_FAIL();
                return 0;
            }
        }
    }

    internal readonly D3D12MA_NormalBlock* GetBlock()
    {
        switch (m_PackedData.GetType())
        {
            case TYPE_COMMITTED:
            case TYPE_HEAP:
            {
                return null;
            }

            case TYPE_PLACED:
            {
                return Anonymous.m_Placed.block;
            }

            default:
            {
                D3D12MA_FAIL();
                return null;
            }
        }
    }

    internal void SetResourcePointer(ID3D12Resource* resource, [NativeTypeName("const D3D12_RESOURCE_DESC_T *")] D3D12_RESOURCE_DESC* pResourceDesc)
    {
        D3D12MA_ASSERT((m_Resource == null) && (pResourceDesc != null));
        m_Resource = resource;
        m_PackedData.SetResourceDimension(pResourceDesc->Dimension);
        m_PackedData.SetResourceFlags(pResourceDesc->Flags);
        m_PackedData.SetTextureLayout(pResourceDesc->Layout);
    }

    internal void SetResourcePointer(ID3D12Resource* resource, [NativeTypeName("const D3D12_RESOURCE_DESC_T *")] D3D12_RESOURCE_DESC1* pResourceDesc)
    {
        D3D12MA_ASSERT((m_Resource == null) && (pResourceDesc != null));
        m_Resource = resource;
        m_PackedData.SetResourceDimension(pResourceDesc->Dimension);
        m_PackedData.SetResourceFlags(pResourceDesc->Flags);
        m_PackedData.SetTextureLayout(pResourceDesc->Layout);
    }

    private void FreeName()
    {
        if (m_Name != null)
        {
            nuint nameCharCount = wcslen(m_Name) + 1;
            D3D12MA_DELETE_ARRAY(m_Allocator->GetAllocs(), m_Name);
            m_Name = null;
        }
    }

    internal enum Type
    {
        TYPE_COMMITTED,

        TYPE_PLACED,

        TYPE_HEAP,

        TYPE_COUNT,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public _m_Committed_e__Struct m_Committed;

        [FieldOffset(0)]
        public _m_Placed_e__Struct m_Placed;

        [FieldOffset(0)]
        public _m_Heap_e__Struct m_Heap;

        public partial struct _m_Committed_e__Struct
        {
            public D3D12MA_CommittedAllocationList* list;

            public D3D12MA_Allocation* prev;

            public D3D12MA_Allocation* next;
        }

        public partial struct _m_Placed_e__Struct
        {
            [NativeTypeName("D3D12MA::AllocHandle")]
            public ulong allocHandle;

            public D3D12MA_NormalBlock* block;
        }

        public partial struct _m_Heap_e__Struct
        {
            public D3D12MA_CommittedAllocationList* list;

            public D3D12MA_Allocation* prev;

            public D3D12MA_Allocation* next;

            public ID3D12Heap* heap;
        }
    }

    internal partial struct PackedData
    {
        private uint _bitfield1;

        [NativeTypeName("UINT : 2")]
        private uint m_Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                return _bitfield1 & 0x3u;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _bitfield1 = (_bitfield1 & ~0x3u) | (value & 03u);
            }
        }

        [NativeTypeName("UINT : 3")]
        private uint m_ResourceDimension
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                return (_bitfield1 >> 2) & 0x7u;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _bitfield1 = (_bitfield1 & ~(0x7u << 2)) | ((value & 0x7u) << 2);
            }
        }

        [NativeTypeName("UINT : 24")]
        private uint m_ResourceFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                return (_bitfield1 >> 5) & 0xFFFFFFu;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _bitfield1 = (_bitfield1 & ~(0xFFFFFFu << 5)) | ((value & 0xFFFFFFu) << 5);
            }
        }

        private uint _bitfield2;

        [NativeTypeName("UINT : 9")]
        private uint m_TextureLayout
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                return _bitfield2 & 0x1FFu;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _bitfield2 = (_bitfield2 & ~0x1FFu) | (value & 0x1FFu);
            }
        }

        [NativeTypeName("UINT : 1")]
        private uint m_WasZeroInitialized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                return (_bitfield2 >> 9) & 0x1u;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _bitfield2 = (_bitfield2 & ~(0x1u << 9)) | ((value & 0x1u) << 9);
            }
        }

        public new readonly Type GetType()
        {
            return (Type)(m_Type);
        }

        public readonly D3D12_RESOURCE_DIMENSION GetResourceDimension()
        {
            return (D3D12_RESOURCE_DIMENSION)(m_ResourceDimension);
        }

        public readonly D3D12_RESOURCE_FLAGS GetResourceFlags()
        {
            return (D3D12_RESOURCE_FLAGS)(m_ResourceFlags);
        }

        public readonly D3D12_TEXTURE_LAYOUT GetTextureLayout()
        {
            return (D3D12_TEXTURE_LAYOUT)(m_TextureLayout);
        }

        public readonly BOOL WasZeroInitialized()
        {
            return (BOOL)(m_WasZeroInitialized);
        }

        public void SetType(Type type)
        {
            uint u = (uint)(type);
            D3D12MA_ASSERT(u < (1u << 2));
            m_Type = u;
        }

        public void SetResourceDimension(D3D12_RESOURCE_DIMENSION resourceDimension)
        {
            uint u = (uint)(resourceDimension);
            D3D12MA_ASSERT(u < (1u << 3));
            m_ResourceDimension = u;
        }

        public void SetResourceFlags(D3D12_RESOURCE_FLAGS resourceFlags)
        {
            uint u = (uint)(resourceFlags);
            D3D12MA_ASSERT(u < (1u << 24));
            m_ResourceFlags = u;
        }

        public void SetTextureLayout(D3D12_TEXTURE_LAYOUT textureLayout)
        {
            uint u = (uint)(textureLayout);
            D3D12MA_ASSERT(u < (1u << 9));
            m_TextureLayout = u;
        }

        public void SetWasZeroInitialized(BOOL wasZeroInitialized)
        {
            m_WasZeroInitialized = wasZeroInitialized ? 1u : 0u;
        }
    }
}
