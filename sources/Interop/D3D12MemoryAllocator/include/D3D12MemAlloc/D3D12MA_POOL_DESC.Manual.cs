// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using TerraFX.Interop.Gdiplus;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESIDENCY_PRIORITY;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_POOL_DESC
{
    /// <summary>Constructor initializing description of a custom pool created in one of the standard <see cref="D3D12_HEAP_TYPE" />.</summary>
    /// <param name="heapType"></param>
    /// <param name="heapFlags"></param>
    /// <param name="blockSize"></param>
    /// <param name="minBlockCount"></param>
    /// <param name="maxBlockCount"></param>
    /// <param name="residencyPriority"></param>
    public D3D12MA_POOL_DESC(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = uint.MaxValue, D3D12_RESIDENCY_PRIORITY residencyPriority = D3D12_RESIDENCY_PRIORITY_NORMAL)
    {
        Flags = D3D12MA_RECOMMENDED_POOL_FLAGS;
        HeapProperties = default;
        HeapProperties.Type = heapType;
        HeapFlags = heapFlags;
        BlockSize = blockSize;
        MinBlockCount = minBlockCount;
        MaxBlockCount = maxBlockCount;
        MinAllocationAlignment = 0;
        pProtectedSession = null;
        ResidencyPriority = residencyPriority;
    }

    /// <summary>Constructor initializing description of a custom pool created in one of the standard <see cref="D3D12_HEAP_TYPE" />.</summary>
    /// <param name="heapType"></param>
    /// <param name="heapFlags"></param>
    /// <param name="flags"></param>
    /// <param name="blockSize"></param>
    /// <param name="minBlockCount"></param>
    /// <param name="maxBlockCount"></param>
    /// <param name="residencyPriority"></param>
    public D3D12MA_POOL_DESC(D3D12_HEAP_TYPE heapType, D3D12_HEAP_FLAGS heapFlags, D3D12MA_POOL_FLAGS flags, ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = uint.MaxValue, D3D12_RESIDENCY_PRIORITY residencyPriority = D3D12_RESIDENCY_PRIORITY_NORMAL)
    {
        Flags = flags;
        HeapProperties = default;
        HeapProperties.Type = heapType;
        HeapFlags = heapFlags;
        BlockSize = blockSize;
        MinBlockCount = minBlockCount;
        MaxBlockCount = maxBlockCount;
        MinAllocationAlignment = 0;
        pProtectedSession = null;
        ResidencyPriority = residencyPriority;
    }

    /// <summary>Constructor initializing description of a custom pool created with custom <see cref="D3D12_HEAP_PROPERTIES" />.</summary>
    /// <param name="heapProperties"></param>
    /// <param name="heapFlags"></param>
    /// <param name="blockSize"></param>
    /// <param name="minBlockCount"></param>
    /// <param name="maxBlockCount"></param>
    /// <param name="residencyPriority"></param>
    public D3D12MA_POOL_DESC(D3D12_HEAP_PROPERTIES heapProperties, D3D12_HEAP_FLAGS heapFlags, ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = uint.MaxValue, D3D12_RESIDENCY_PRIORITY residencyPriority = D3D12_RESIDENCY_PRIORITY_NORMAL)
    {
        Flags = D3D12MA_RECOMMENDED_POOL_FLAGS;
        HeapProperties = heapProperties;
        HeapFlags = heapFlags;
        BlockSize = blockSize;
        MinBlockCount = minBlockCount;
        MaxBlockCount = maxBlockCount;
        MinAllocationAlignment = 0;
        pProtectedSession = null;
        ResidencyPriority = residencyPriority;
    }

    /// <summary>Constructor initializing description of a custom pool created with custom <see cref="D3D12_HEAP_PROPERTIES" />.</summary>
    /// <param name="heapProperties"></param>
    /// <param name="heapFlags"></param>
    /// <param name="flags"></param>
    /// <param name="blockSize"></param>
    /// <param name="minBlockCount"></param>
    /// <param name="maxBlockCount"></param>
    /// <param name="residencyPriority"></param>
    public D3D12MA_POOL_DESC(D3D12_HEAP_PROPERTIES heapProperties, D3D12_HEAP_FLAGS heapFlags, D3D12MA_POOL_FLAGS flags, ulong blockSize = 0, uint minBlockCount = 0, uint maxBlockCount = uint.MaxValue, D3D12_RESIDENCY_PRIORITY residencyPriority = D3D12_RESIDENCY_PRIORITY_NORMAL)
    {
        Flags = flags;
        HeapProperties = heapProperties;
        HeapFlags = heapFlags;
        BlockSize = blockSize;
        MinBlockCount = minBlockCount;
        MaxBlockCount = maxBlockCount;
        MinAllocationAlignment = 0;
        pProtectedSession = null;
        ResidencyPriority = residencyPriority;
    }
}
