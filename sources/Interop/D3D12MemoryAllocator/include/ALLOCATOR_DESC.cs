// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>Parameters of created Allocator object. To be used with CreateAllocator().</summary>
    public unsafe struct ALLOCATOR_DESC
    {
        /// <summary>Flags</summary>
        public ALLOCATOR_FLAGS Flags;

        /// <summary>
        /// Direct3D device object that the allocator should be attached to.
        /// <para>Allocator is doing `AddRef`/`Release` on this object.</para>
        /// </summary>
        public ID3D12Device* pDevice;

        /// <summary>
        /// Preferred size of a single `ID3D12Heap` block to be allocated.
        /// <para>Set to 0 to use default, which is currently 256 MiB.</para>
        /// </summary>
        [NativeTypeName("UINT64")] public ulong PreferredBlockSize;

        /// <summary>
        /// Custom CPU memory allocation callbacks. Optional.
        /// <para>Optional, can be null. When specified, will be used for all CPU-side memory allocations.</para>
        /// </summary>
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;

        /// <summary>
        /// DXGI Adapter object that you use for D3D12 and this allocator.
        /// <para>Allocator is doing `AddRef`/`Release` on this object.</para>
        /// </summary>
        public IDXGIAdapter* pAdapter;
    }
}
