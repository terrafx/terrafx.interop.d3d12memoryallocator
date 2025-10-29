// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;

namespace TerraFX.Interop.DirectX;

/// <summary>Bit flags to be used with <see cref="D3D12MA_ALLOCATOR_DESC.Flags"/>.</summary>
[Flags]
public enum D3D12MA_ALLOCATOR_FLAGS
{
    /// <summary>Zero</summary>
    D3D12MA_ALLOCATOR_FLAG_NONE = 0,

    /// <summary>Allocator and all objects created from it will not be synchronized internally, so you must guarantee they are used from only one thread at a time or synchronized by you.</summary>
    /// <remarks>Using this flag may increase performance because internal mutexes are not used.</remarks>
    D3D12MA_ALLOCATOR_FLAG_SINGLETHREADED = 0x1,

    /// <summary>Every allocation will be created as a committed resource - will have its own memory block.</summary>
    /// <remarks>Affects both default pools and custom pools. To be used for debugging purposes only. There is also an equivalent flag for custom pools: <see cref="D3D12MA_POOL_FLAG_ALWAYS_COMMITTED" />.</remarks>
    D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED = 0x2,

    /// <summary>Heaps created for the default pools will be created with flag <see cref="D3D12_HEAP_FLAG_CREATE_NOT_ZEROED" />, allowing for their memory to be not zeroed by the system if possible, which can speed up allocation.</summary>
    /// <remarks>
    ///   <para>Only affects default pools. To use the flag with custom_pools, you need to add it manually:</para>
    ///   <code>poolDesc.heapFlags |= D3D12_HEAP_FLAG_CREATE_NOT_ZEROED;</code>
    ///   <para>Only available if <see cref="ID3D12Device8" /> is present. Otherwise, the flag is ignored.</para>
    /// </remarks>
    D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED = 0x4,

    /// <summary>Optimization, allocate MSAA textures as committed resources always.</summary>
    /// <remarks>
    ///   <para>Specify this flag to create MSAA textures with implicit heaps, as if they were created with flag <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED" />. Usage of this flags enables all default pools to create its heaps on smaller alignment not suitable for MSAA textures.</para>
    ///  <para>You should always use this flag unless you really need to create some MSAA textures as placed.</para>
    /// </remarks>
    D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED = 0x8,

    /// <summary>Disable optimization that prefers creating small buffers as committed to avoid 64 KB alignment.</summary>
    /// <remarks>By default, the library prefers creating small buffers &lt;= 32 KB as committed, because drivers tend to pack them better, while placed buffers require 64 KB alignment. This, however, may decrease performance, as creating committed resources involves allocation of implicit heaps, which may take longer than creating placed resources in existing heaps. Passing this flag will disable this committed preference globally for the allocator. It can also be disabled for a single allocation by using <see cref="D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME" />.</remarks>
    D3D12MA_ALLOCATOR_FLAG_DONT_PREFER_SMALL_BUFFERS_COMMITTED = 0x10,
}
