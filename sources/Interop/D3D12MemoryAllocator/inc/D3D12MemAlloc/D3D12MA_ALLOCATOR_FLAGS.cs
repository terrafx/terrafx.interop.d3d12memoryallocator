// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;

namespace TerraFX.Interop
{
    /// <summary>Bit flags to be used with <see cref="D3D12MA_ALLOCATOR_DESC.Flags"/>.</summary>
    [Flags]
    public enum D3D12MA_ALLOCATOR_FLAGS
    {
        /// <summary>Zero</summary>
        D3D12MA_ALLOCATOR_FLAG_NONE = 0,

        /// <summary>
        /// Allocator and all objects created from it will not be synchronized internally, so you
        /// must guarantee they are used from only one thread at a time or synchronized by you.
        /// <para>Using this flag may increase performance because internal mutexes are not used.</para>
        /// </summary>
        D3D12MA_ALLOCATOR_FLAG_SINGLETHREADED = 0x1,

        /// <summary>Every allocation will have its own memory block. To be used for debugging purposes.</summary>
        D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED = 0x2,
    }
}
