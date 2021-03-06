// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    /// <summary>Custom callbacks to CPU memory allocation functions.</summary>
    public unsafe struct D3D12MA_ALLOCATION_CALLBACKS
    {
        /// <summary>Allocation function. The parameters are the size, alignment and <see cref="pUserData"/>.</summary>
        [NativeTypeName("ALLOCATE_FUNC_PTR")]
        public delegate*<nuint, nuint, void*, void*> pAllocate;

        /// <summary>Dellocation function. The parameters are `pMemory` and <see cref="pUserData"/>.</summary>
        [NativeTypeName("FREE_FUNC_PTR")]
        public delegate*<void*, void*, void> pFree;

        /// <summary>Custom data that will be passed to allocation and deallocation functions as <see cref="pUserData"/> parameter.</summary>
        public void* pUserData;
    }
}
