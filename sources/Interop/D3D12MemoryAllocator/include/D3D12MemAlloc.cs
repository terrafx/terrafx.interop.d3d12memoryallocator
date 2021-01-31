// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemoryAllocator
    {
        /// <summary>Number of D3D12 memory heap types supported.</summary>
        [NativeTypeName("UINT")]
        internal const uint HEAP_TYPE_COUNT = 3;

        /// <summary>To be used with MAKE_HRESULT to define custom error codes.</summary>
        internal const uint FACILITY_D3D12MA = 3542;

        /// <summary>
        /// Creates new main <see cref="Allocator"/> object and returns it through <paramref name="ppAllocator"/>.
        /// <para>You normally only need to call it once and keep a single Allocator object for your <see cref="ID3D12Device"/>.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateAllocator([NativeTypeName("const ALLOCATOR_DESC*")] ALLOCATOR_DESC* pDesc, Allocator** ppAllocator);

        /// <summary>
        /// Creates new <see cref="VirtualBlock"/> object and returns it through <paramref name="ppVirtualBlock"/>.
        /// <para>Note you don't need to create <see cref="Allocator"/> to use virtual blocks.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int CreateVirtualBlock([NativeTypeName("const VIRTUAL_BLOCK_DESC*")] VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);
    }
}
