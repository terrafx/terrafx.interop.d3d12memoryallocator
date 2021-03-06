// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    /// <summary>Parameters of created <see cref="D3D12MA_Allocator"/> object. To be used with <see cref="D3D12MemAlloc.D3D12MA_CreateAllocator"/>.</summary>
    public unsafe struct D3D12MA_ALLOCATOR_DESC
    {
        /// <summary>Flags</summary>
        public D3D12MA_ALLOCATOR_FLAGS Flags;

        /// <summary>
        /// Direct3D device object that the allocator should be attached to.
        /// <para><see cref="D3D12MA_Allocator"/> is doing <see cref="IUnknown.AddRef"/>/<see cref="IUnknown.Release"/> on this object.</para>
        /// </summary>
        public ID3D12Device* pDevice;

        /// <summary>
        /// Preferred size of a single <see cref="ID3D12Heap"/> block to be allocated.
        /// <para>Set to 0 to use default, which is currently 256 MiB.</para>
        /// </summary>
        [NativeTypeName("UINT64")]
        public ulong PreferredBlockSize;

        /// <summary>
        /// Custom CPU memory allocation callbacks. Optional.
        /// <para>Optional, can be null. When specified, will be used for all CPU-side memory allocations.</para>
        /// </summary>
        [NativeTypeName("const ALLOCATION_CALLBACKS*")]
        public D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;

        /// <summary>
        /// DXGI Adapter object that you use for D3D12 and this allocator.
        /// <para><see cref="D3D12MA_Allocator"/> is doing <see cref="IUnknown.AddRef"/>/<see cref="IUnknown.Release"/> on this object.</para>
        /// </summary>
        public IDXGIAdapter* pAdapter;
    }
}
