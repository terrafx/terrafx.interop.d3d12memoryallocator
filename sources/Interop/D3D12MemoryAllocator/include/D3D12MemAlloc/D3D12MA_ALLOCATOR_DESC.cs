// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Parameters of created <see cref="D3D12MA_Allocator" /> object. To be used with <see cref="D3D12MA_CreateAllocator" />.</summary>
public unsafe partial struct D3D12MA_ALLOCATOR_DESC
{
    /// <summary>Flags for the entire allocator.</summary>
    /// <remarks>It is recommended to use <see cref="D3D12MA_RECOMMENDED_ALLOCATOR_FLAGS" />.</remarks>
    public D3D12MA_ALLOCATOR_FLAGS Flags;

    /// <summary>Direct3D device object that the allocator should be attached to.</summary>
    /// <remarks>Allocator is doing <see cref="ID3D12Device.AddRef" />/<see cref="ID3D12Device.Release" /> on this object.</remarks>
    public ID3D12Device* pDevice;

    /// <summary>Preferred size of a single <see cref="ID3D12Heap" /> block to be allocated.</summary>
    /// <remarks>Set to 0 to use default, which is currently 64 MiB.</remarks>
    [NativeTypeName("UINT64")]
    public ulong PreferredBlockSize;

    /// <summary>Custom CPU memory allocation callbacks. Optional.</summary>
    /// <remarks>Optional, can be null. When specified, will be used for all CPU-side memory allocations.</remarks>
    [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")]
    public D3D12MA_ALLOCATION_CALLBACKS* pAllocationCallbacks;

    /// <summary><see cref="IDXGIAdapter" /> object that you use for D3D12 and this allocator.</summary>
    /// <remarks>Allocator is doing <see cref="IDXGIAdapter.AddRef" />/<see cref="IDXGIAdapter.Release" /> on this object.</remarks>
    public IDXGIAdapter* pAdapter;
}
