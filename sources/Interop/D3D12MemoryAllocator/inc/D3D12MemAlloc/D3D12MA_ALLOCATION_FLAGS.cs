// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop
{
    /// <summary>Bit flags to be used with <see cref="D3D12MA_ALLOCATION_DESC.Flags"/>.</summary>
    [Flags]
    public enum D3D12MA_ALLOCATION_FLAGS
    {
        /// <summary>Zero.</summary>
        D3D12MA_ALLOCATION_FLAG_NONE = 0,

        /// <summary>Set this flag if the allocation should have its own dedicated memory allocation (committed resource with implicit heap). Use it for special, big resources, like fullscreen textures used as render targets.</summary>
        D3D12MA_ALLOCATION_FLAG_COMMITTED = 0x1,

        /// <summary>
        /// Set this flag to only try to allocate from existing memory heaps and never create new such heap.
        /// <para>If new allocation cannot be placed in any of the existing heaps, allocation fails with <see cref="E_OUTOFMEMORY"/> error.</para>
        /// <para>You should not use <see cref="D3D12MA_ALLOCATION_FLAG_COMMITTED"/> and <see cref="D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE"/> at the same time. It makes no sense.</para>
        /// </summary>
        D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE = 0x2,

        /// <summary>Create allocation only if additional memory required for it, if any, won't exceed memory budget. Otherwise return <see cref="E_OUTOFMEMORY"/>.</summary>
        D3D12MA_ALLOCATION_FLAG_WITHIN_BUDGET = 0x4,
    }
}
