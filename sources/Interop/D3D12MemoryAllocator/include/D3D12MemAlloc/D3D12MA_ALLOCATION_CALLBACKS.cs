// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

/// <summary>Custom callbacks to CPU memory allocation functions.</summary>
public unsafe partial struct D3D12MA_ALLOCATION_CALLBACKS
{
    /// <summary>Allocation function.</summary>
    [NativeTypeName("ALLOCATE_FUNC_PTR")]
    public delegate* unmanaged<nuint, nuint, void*, void*> pAllocate;

    /// <summary>Dellocation function.</summary>
    [NativeTypeName("FREE_FUNC_PTR")]
    public delegate* unmanaged<void*, void*, void> pFree;

    /// <summary>Custom data that will be passed to allocation and deallocation functions as <c>pUserData</c> parameter.</summary>
    public void* pPrivateData;
}
