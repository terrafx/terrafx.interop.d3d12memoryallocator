// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12MA_DEFRAGMENTATION_MOVE_OPERATION;

namespace TerraFX.Interop.DirectX;

/// <summary>Single move of an allocation to be done for defragmentation.</summary>
public unsafe partial struct D3D12MA_DEFRAGMENTATION_MOVE
{
    /// <summary>Operation to be performed on the allocation by <see cref="D3D12MA_DefragmentationContext.EndPass" />. Default value is <see cref="D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY" />. You can modify it.</summary>
    public D3D12MA_DEFRAGMENTATION_MOVE_OPERATION Operation;

    /// <summary>Allocation that should be moved.</summary>
    public D3D12MA_Allocation* pSrcAllocation;

    /// <summary>Temporary allocation pointing to destination memory that will replace <see cref="pSrcAllocation" />.</summary>
    /// <remarks>
    ///   <para>Use it to retrieve new <see cref="ID3D12Heap" /> and offset to create new <see cref="ID3D12Resource" /> and then store it here via <see cref="D3D12MA_Allocation.SetResource" />.</para>
    ///   <para>WARNING: Do not store this allocation in your data structures! It exists only temporarily, for the duration of the defragmentation pass, to be used for storing newly created <c>resource.DefragmentationContext.EndPass</c> will destroy it and make <see cref="pSrcAllocation" /> point to this memory.</para>
    /// </remarks>
    public D3D12MA_Allocation* pDstTmpAllocation;
}
