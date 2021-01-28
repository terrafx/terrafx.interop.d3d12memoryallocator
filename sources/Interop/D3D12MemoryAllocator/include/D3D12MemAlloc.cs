// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemoryAllocator
    {
        /// <summary>Number of D3D12 memory heap types supported.</summary>
        [NativeTypeName("UINT")]
        internal const uint HEAP_TYPE_COUNT = 3;

        /// <summary>
        /// Creates new main D3D12MA::Allocator object and returns it through `ppAllocator`.
        /// <para>You normally only need to call it once and keep a single Allocator object for your `ID3D12Device`.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateAllocator([NativeTypeName("const ALLOCATOR_DESC*")] ALLOCATOR_DESC* pDesc, Allocator** ppAllocator);

        /// <summary>
        /// Creates new D3D12MA::VirtualBlock object and returns it through `ppVirtualBlock`.
        /// <para>Note you don't need to create D3D12MA::Allocator to use virtual blocks.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateVirtualBlock([NativeTypeName("const VIRTUAL_BLOCK_DESC*")] VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);
    }
}
