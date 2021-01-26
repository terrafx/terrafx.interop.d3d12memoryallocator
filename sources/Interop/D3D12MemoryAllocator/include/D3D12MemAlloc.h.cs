// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

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
        [NativeTypeName("UINT")] public const uint HEAP_TYPE_COUNT = 3;

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
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateVirtualBlock(VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);
    }
}
