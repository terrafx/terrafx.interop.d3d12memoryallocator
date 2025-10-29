// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATOR_FLAGS;

namespace TerraFX.Interop.DirectX;

/// <summary>Bit flags to be used with <see cref="D3D12MA_POOL_DESC.Flags" />.</summary>
[Flags]
public enum D3D12MA_POOL_FLAGS
{
    /// <summary>Zero</summary>
    D3D12MA_POOL_FLAG_NONE = 0,

    /// <summary>Enables alternative, linear allocation algorithm in this pool.</summary>
    /// <remarks>
    ///   <para>Specify this flag to enable linear allocation algorithm, which always creates new allocations after last one and doesn't reuse space from allocations freed in between. It trades memory consumption for simplified algorithm and data structure, which has better performance and uses less memory for metadata.</para>
    ///   <para>By using this flag, you can achieve behavior of free-at-once, stack, ring buffer, and double stack. For details, see documentation chapter linear_algorithm.</para>
    /// </remarks>
    D3D12MA_POOL_FLAG_ALGORITHM_LINEAR = 0x1,

    /// <summary>Optimization, allocate MSAA textures as committed resources always.</summary>
    /// <remarks>
    ///   <para>Specify this flag to create MSAA textures with implicit heaps, as if they were created with flag <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED" />. Usage of this flags enables pool to create its heaps on smaller alignment not suitable for MSAA textures.</para>
    ///   <para>You should always use this flag unless you really need to create some MSAA textures in this pool as placed.</para>
    /// </remarks>
    D3D12MA_POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED = 0x2,

    /// <summary>Every allocation made in this pool will be created as a committed resource - will have its own memory block.</summary>
    /// <remarks>There is also an equivalent flag for the entire allocator: <see cref="D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED" />.</remarks>
    D3D12MA_POOL_FLAG_ALWAYS_COMMITTED = 0x4,

    /// <summary>Bit mask to extract only <c>ALGORITHM</c> bits from entire set of flags.</summary>
    D3D12MA_POOL_FLAG_ALGORITHM_MASK = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR,
}
