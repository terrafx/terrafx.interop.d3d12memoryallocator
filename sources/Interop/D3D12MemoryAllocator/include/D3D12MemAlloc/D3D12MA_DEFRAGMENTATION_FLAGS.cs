// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;

namespace TerraFX.Interop.DirectX;

/// <summary>Flags to be passed as <see cref="D3D12MA_DEFRAGMENTATION_DESC.Flags" />.</summary>
[Flags]
public enum D3D12MA_DEFRAGMENTATION_FLAGS
{
    /// <summary>Use simple but fast algorithm for defragmentation. May not achieve best results but will require least time to compute and least allocations to copy.</summary>
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST = 0x1,

    /// <summary>Default defragmentation algorithm, applied also when no <c>ALGORITHM</c> flag is specified. Offers a balance between defragmentation quality and the amount of allocations and bytes that need to be moved.</summary>
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED = 0x2,

    /// <summary>Perform full defragmentation of memory. Can result in notably more time to compute and allocations to copy, but will achieve best memory packing.</summary>
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL = 0x4,

    /// <summary>A bit mask to extract only <c>ALGORITHM</c> bits from entire set of flags.</summary>
    D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_MASK = D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST | D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED | D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL,
}
