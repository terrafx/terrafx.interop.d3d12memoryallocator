
// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_VIRTUAL_BLOCK_FLAGS;

namespace TerraFX.Interop.DirectX;

/// <summary>Bit flags to be used with <see cref="D3D12MA_VIRTUAL_ALLOCATION_DESC.Flags" />.</summary>
[Flags]
public enum D3D12MA_VIRTUAL_ALLOCATION_FLAGS
{
    /// <summary>Zero</summary>
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_NONE = 0,

    /// <summary>Allocation will be created from upper stack in a double stack pool.</summary>
    /// <remarks>This flag is only allowed for virtual blocks created with <see cref="D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR" /> flag.</remarks>
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_UPPER_ADDRESS = D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS,

    /// <summary>Allocation strategy that tries to minimize memory usage.</summary>
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY,

    /// <summary>Allocation strategy that tries to minimize allocation time.</summary>
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_TIME = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME,

    /// <summary>Allocation strategy that chooses always the lowest offset in available space. This is not the most efficient strategy but achieves highly packed data.</summary>
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET,

    /// <summary>A bit mask to extract only <c>STRATEGY</c> bits from entire set of flags.</summary>
    /// <remarks>These strategy flags are binary compatible with equivalent flags in <see cref="D3D12MA_ALLOCATION_FLAGS" />.</remarks>
    D3D12MA_VIRTUAL_ALLOCATION_FLAG_STRATEGY_MASK = D3D12MA_ALLOCATION_FLAG_STRATEGY_MASK,
}
