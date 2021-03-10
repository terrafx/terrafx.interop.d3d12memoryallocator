// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemAlloc
    {
        /// <summary>To be used with MAKE_HRESULT to define custom error codes.</summary>
        public const uint FACILITY_D3D12MA = 3542;

        /// <summary>Number of D3D12 memory heap types supported.</summary>
        [NativeTypeName("UINT")]
        public const uint D3D12MA_HEAP_TYPE_COUNT = 4;

        /// <summary>
        /// Creates new main <see cref="D3D12MA_Allocator"/> object and returns it through <paramref name="ppAllocator"/>.
        /// <para>You normally only need to call it once and keep a single Allocator object for your <see cref="ID3D12Device"/>.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int D3D12MA_CreateAllocator([NativeTypeName("const ALLOCATOR_DESC*")] D3D12MA_ALLOCATOR_DESC* pDesc, D3D12MA_Allocator** ppAllocator);

        /// <summary>
        /// Creates new <see cref="D3D12MA_VirtualBlock"/> object and returns it through <paramref name="ppVirtualBlock"/>.
        /// <para>Note you don't need to create <see cref="D3D12MA_Allocator"/> to use virtual blocks.</para>
        /// </summary>
        [return: NativeTypeName("HRESULT")]
        public static partial int D3D12MA_CreateVirtualBlock([NativeTypeName("const VIRTUAL_BLOCK_DESC*")] D3D12MA_VIRTUAL_BLOCK_DESC* pDesc, D3D12MA_VirtualBlock** ppVirtualBlock);
    }
}
