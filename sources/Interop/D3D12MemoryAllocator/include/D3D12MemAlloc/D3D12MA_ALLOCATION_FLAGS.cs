// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.DirectX.D3D12MA_POOL_FLAGS;
using static TerraFX.Interop.Windows.E;

namespace TerraFX.Interop.DirectX;

/// <summary>Bit flags to be used with <see cref="D3D12MA_ALLOCATION_DESC.Flags" />.</summary>
[Flags]
public enum D3D12MA_ALLOCATION_FLAGS
{
    /// <summary>Zero</summary>
    D3D12MA_ALLOCATION_FLAG_NONE = 0,

    /// <summary>Set this flag if the allocation should have its own dedicated memory allocation (committed resource with implicit heap).</summary>
    /// <remarks>
    ///   <para>Use it for special, big resources, like fullscreen textures used as render targets.</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>When used with functions like <see cref="D3D12MA_Allocator.CreateResource" />, it will use <see cref="ID3D12Device.CreateCommittedResource" />, so the created allocation will contain a resource (<c>D3D12MA_Allocation.GetResource() != null</c>) but will not have a heap (<c>D3D12MA_Allocation.GetHeap() == null</c>), as the heap is implicit.</description>
    ///     </item>
    ///     <item>
    ///       <description>When used with raw memory allocation like <see cref="D3D12MA_Allocator.AllocateMemory" />, it will use <see cref="ID3D12Device.CreateHeap" />, so the created allocation will contain a heap (<c>D3D12MA_Allocation.GetHeap() != null</c>) and its offset will always be 0.</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    D3D12MA_ALLOCATION_FLAG_COMMITTED = 0x1,

    /// <summary>Set this flag to only try to allocate from existing memory heaps and never create new such heap.</summary>
    /// <remarks>
    ///   <para>If new allocation cannot be placed in any of the existing heaps, allocation fails with <see cref="E_OUTOFMEMORY" /> error.</para>
    ///   <para>You should not use <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED" /> and <see cref="D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE" /> at the same time. It makes no sense.</para>
    /// </remarks>
    D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE = 0x2,

    /// <summary>Create allocation only if additional memory required for it, if any, won't exceed memory budget. Otherwise return <see cref="E_OUTOFMEMORY" />.</summary>
    D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET = 0x4,

    /// <summary>Allocation will be created from upper stack in a double stack pool.</summary>
    /// <remarks>This flag is only allowed for custom pools created with <see cref="D3D12MA_POOL_FLAG_ALGORITHM_LINEAR" /> flag.</remarks>
    D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS = 0x8,

    /// <summary>Set this flag if the allocated memory will have aliasing resources.</summary>
    /// <remarks>Use this when calling <see cref="D3D12MA_Allocator.CreateResource" /> and similar to guarantee creation of explicit heap for desired allocation and prevent it from using <see cref="ID3D12Device.CreateCommittedResource" />, so that new allocation object will always have <c>allocation->GetHeap() != null</c>.</remarks>
    D3D12MA_ALLOCATION_FLAG_CAN_ALIAS = 0x10,

    /// <summary>Allocation strategy that chooses smallest possible free range for the allocation to minimize memory usage and fragmentation, possibly at the expense of allocation time.</summary>
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY = 0x00010000,

    /// <summary>Allocation strategy that chooses first suitable free range for the allocation - not necessarily in terms of the smallest offset but the one that is easiest and fastest to find to minimize allocation time, possibly at the expense of allocation quality.</summary>
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME = 0x00020000,

    /// <summary>Allocation strategy that chooses always the lowest offset in available space. This is not the most efficient strategy but achieves highly packed data. Used internally by defragmentation, not recomended in typical usage.</summary>
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET = 0x0004000,

    /// <summary>Alias to <see cref="D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY" />.</summary>
    D3D12MA_ALLOCATION_FLAG_STRATEGY_BEST_FIT = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY,

    /// <summary>Alias to <see cref="D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME" />.</summary>
    D3D12MA_ALLOCATION_FLAG_STRATEGY_FIRST_FIT = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME,

    /// <summary>A bit mask to extract only <c>STRATEGY</c> bits from entire set of flags.</summary>
    D3D12MA_ALLOCATION_FLAG_STRATEGY_MASK = D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY | D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_TIME | D3D12MA_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET,
}
