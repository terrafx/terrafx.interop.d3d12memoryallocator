// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
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

    /// <summary>Every allocation will have its own memory block. To be used for debugging purposes.</summary>
    D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED = 0x2,

    /// <summary>Heaps created for the default pools will be created with flag <see cref="D3D12_HEAP_FLAG_CREATE_NOT_ZEROED" />, allowing for their memory to be not zeroed by the system if possible, which can speed up allocation.</summary>
    /// <remarks>
    ///   <para>Only affects default pools. To use the flag with custom_pools, you need to add it manually:</para>
    ///   <code>poolDesc.heapFlags |= D3D12_HEAP_FLAG_CREATE_NOT_ZEROED;</code>
    ///   <para>Only avaiable if <see cref="ID3D12Device8" /> is present. Otherwise, the flag is ignored.</para>
    /// </remarks>
    D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED = 0x4,

    /// <summary>Optimization, allocate MSAA textures as committed resources always.</summary>
    /// <remarks>Specify this flag to create MSAA textures with implicit heaps, as if they were created with flag <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED" />. Usage of this flags enables all default pools to create its heaps on smaller alignment not suitable for MSAA textures.</remarks>
    D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED = 0x8,
}
