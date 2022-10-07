// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12MA_DEFRAGMENTATION_MOVE_OPERATION;

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters for incremental defragmentation steps.</summary>
/// <remarks>To be used with function <see cref="D3D12MA_DefragmentationContext.BeginPass" />.</remarks>
public unsafe partial struct D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO
{
    /// <summary>Number of elements in the <see cref="pMoves" /> array.</summary>
    [NativeTypeName("UINT32")]
    public uint MoveCount;

    /// <summary>Array of moves to be performed by the user in the current defragmentation pass.</summary>
    /// <remarks>
    ///   <para>Pointer to an array of <see cref="MoveCount" /> elements, owned by D3D12MA, created in <see cref="D3D12MA_DefragmentationContext.BeginPass" />, destroyed in <see cref="D3D12MA_DefragmentationContext.EndPass" />.</para>
    ///   <para>For each element, you should:</para>
    ///   <list type="number">
    ///     <item>
    ///       <description>Create a new resource in the place pointed by <c>pMoves[i].pDstTmpAllocation->GetHeap() + pMoves[i].pDstTmpAllocation->GetOffset()</c>.</description>
    ///     </item>
    ///     <item>
    ///       <description>Store new resource in <c>pMoves[i].pDstTmpAllocation</c> by using <see cref="D3D12MA_Allocation.SetResource" />. It will later replace old resource from <c>pMoves[i].pSrcAllocation</c>.</description>
    ///     </item>
    ///     <item>
    ///       <description>Copy data from the <c>pMoves[i].pSrcAllocation</c> e.g. using <see cref="ID3D12GraphicsCommandList.CopyResource" />.</description>
    ///     </item>
    ///     <item>
    ///       <description>Make sure these commands finished executing on the GPU.</description>
    ///     </item>
    ///   </list>
    ///   <para>Only then you can finish defragmentation pass by calling <see cref="D3D12MA_DefragmentationContext.EndPass" />. After this call, the allocation will point to the new place in memory.</para>
    ///   <para>Alternatively, if you cannot move specific allocation, you can set <see cref="D3D12MA_DEFRAGMENTATION_MOVE.Operation" /> to <see cref="D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE" />.</para>
    ///   <para>Alternatively, if you decide you want to completely remove the allocation, set <see cref="D3D12MA_DEFRAGMENTATION_MOVE.Operation" /> to <see cref="D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_DESTROY" />. Then, after DefragmentationContext::EndPass() the allocation will be released.</para>
    /// </remarks>
    public D3D12MA_DEFRAGMENTATION_MOVE* pMoves;
}
