// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>Parameters of created D3D12MA::Allocation object. To be used with Allocator::CreateResource.</summary>
    public unsafe struct ALLOCATION_DESC
    {
        /// <summary>Flags.</summary>
        public ALLOCATION_FLAGS Flags;

        /// <summary>
        /// The type of memory heap where the new allocation should be placed.
        /// <para>It must be one of: `D3D12_HEAP_TYPE_DEFAULT`, `D3D12_HEAP_TYPE_UPLOAD`, `D3D12_HEAP_TYPE_READBACK`.</para>
        /// <para>When D3D12MA::ALLOCATION_DESC::CustomPool != NULL this member is ignored.</para>
        /// </summary>
        public D3D12_HEAP_TYPE HeapType;

        /// <summary>
        /// Additional heap flags to be used when allocating memory.
        /// <para>In most cases it can be 0.</para>
        /// <para>
        /// - If you use D3D12MA::Allocator::CreateResource(), you don't need to care.
        /// Necessary flag `D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS`, `D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES`, or `D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES` is added automatically.
        /// </para>
        /// <para>
        /// - If you use D3D12MA::Allocator::AllocateMemory(), you should specify one of those `ALLOW_ONLY` flags.
        /// Except when you validate that D3D12MA::Allocator::GetD3D12Options()`.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER_1` - then you can leave it 0.
        /// </para>
        /// <para>
        /// - You can specify additional flags if needed. Then the memory will always be allocated as separate block using `D3D12Device::CreateCommittedResource` or `CreateHeap`, not as part of an existing larget block.
        /// </para>
        /// <para>When D3D12MA::ALLOCATION_DESC::CustomPool != NULL this member is ignored.</para>
        /// </summary>
        public D3D12_HEAP_FLAGS ExtraHeapFlags;

        /// <summary>
        /// Custom pool to place the new resource in. Optional.
        /// <para>When not NULL, the resource will be created inside specified custom pool. It will then never be created as committed.</para>
        /// </summary>
        public Pool* CustomPool;
    }
}
