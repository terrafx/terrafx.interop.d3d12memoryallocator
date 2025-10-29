// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of created <see cref="D3D12MA_Allocation"/> object. To be used with <see cref="D3D12MA_Allocator.CreateResource"/>.</summary>
public unsafe partial struct D3D12MA_ALLOCATION_DESC
{
    /// <summary>Flags for the allocation.</summary>
    public D3D12MA_ALLOCATION_FLAGS Flags;

    /// <summary>The type of memory heap where the new allocation should be placed.</summary>
    /// <remarks>
    ///   <para>It must be one of: <see cref="D3D12_HEAP_TYPE_DEFAULT" />, <see cref="D3D12_HEAP_TYPE_UPLOAD" />, <see cref="D3D12_HEAP_TYPE_READBACK" />.</para>
    ///   <para>When <c>D3D12MA_ALLOCATION_DESC.CustomPool != null</c> this member is ignored.</para>
    /// </remarks>
    public D3D12_HEAP_TYPE HeapType;

    /// <summary>Additional heap flags to be used when allocating memory.</summary>
    /// <remarks>
    ///   <para>In most cases it can be 0.</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>If you use <see cref="D3D12MA_Allocator.CreateResource" />, you don't need to care. Necessary flag <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS" />, <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES" />, or <see cref="D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES" /> is added automatically.</description>
    ///     </item>
    ///     <item>
    ///       <description>If you use <see cref="D3D12MA_Allocator.AllocateMemory" />, you should specify one of those <c>ALLOW_ONLY</c> flags. Except when you validate that <c>D3D12MA_Allocator.GetD3D12Options().ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER_1</c> - then you can leave it 0.</description>
    ///     </item>
    ///     <item>
    ///       <description>You can specify additional flags if needed. Then the memory will always be allocated as separate block using <see cref="ID3D12Device.CreateCommittedResource" /> or <see cref="ID3D12Device.CreateHeap" />, not as part of an existing larget block.</description>
    ///     </item>
    ///   </list>
    ///   <para>When <c>D3D12MA_ALLOCATION_DESC.CustomPool != null</c> this member is ignored.</para>
    /// </remarks>
    public D3D12_HEAP_FLAGS ExtraHeapFlags;

    /// <summary>Custom pool to place the new resource in. Optional.</summary>
    /// <remarks>When not <c>null</c>, the resource will be created inside specified custom pool. Members <see cref="HeapType" />, <see cref="ExtraHeapFlags" /> are then ignored.</remarks>
    public D3D12MA_Pool* CustomPool;

    /// <summary>Custom general-purpose pointer that will be stored in <see cref="D3D12MA_Allocation" />.</summary>
    public void* pPrivateData;
}
