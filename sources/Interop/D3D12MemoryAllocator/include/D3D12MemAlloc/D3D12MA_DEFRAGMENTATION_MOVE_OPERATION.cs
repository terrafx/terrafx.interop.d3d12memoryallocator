// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Operation performed on single defragmentation move.</summary>
public enum D3D12MA_DEFRAGMENTATION_MOVE_OPERATION
{
    /// <summary>Resource has been recreated at `pDstTmpAllocation`, data has been copied, old resource has been destroyed. <c>pSrcAllocation</c> will be changed to point to the new place. This is the default value set by <see cref="D3D12MA_DefragmentationContext.BeginPass" />.</summary>
    D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY = 0,

    /// <summary>Set this value if you cannot move the allocation. New place reserved at <c>pDstTmpAllocation</c> will be freed. <c>pSrcAllocation</c> will remain unchanged.</summary>
    D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE = 1,

    /// <summary>Set this value if you decide to abandon the allocation and you destroyed the resource. New place reserved <c>pDstTmpAllocation</c> will be freed, along with <c>pSrcAllocation</c>.</summary>
    D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_DESTROY = 2,
}
