// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

public unsafe partial struct D3D12MA_ALLOCATION_DESC
{
    /// <summary>Constructor initializing description of an allocation to be created in a specific custom pool.</summary>
    /// <param name="customPool"></param>
    /// <param name="flags"></param>
    /// <param name="privateData"></param>
    public D3D12MA_ALLOCATION_DESC(D3D12MA_Pool* customPool, D3D12MA_ALLOCATION_FLAGS flags = D3D12MA_ALLOCATION_FLAG_NONE, void* privateData = null)
    {
        Flags = flags;
        HeapType = default;
        ExtraHeapFlags = D3D12_HEAP_FLAG_NONE;
        CustomPool = customPool;
        pPrivateData = privateData;
    }

    /// <summary>Constructor initializing description of an allocation to be created in a default pool of a specific <see cref="D3D12_HEAP_TYPE" />.</summary>
    /// <param name="heapType"></param>
    /// <param name="flags"></param>
    /// <param name="privateData"></param>
    /// <param name="extraHeapFlags"></param>
    public D3D12MA_ALLOCATION_DESC(D3D12_HEAP_TYPE heapType, D3D12MA_ALLOCATION_FLAGS flags = D3D12MA_ALLOCATION_FLAG_NONE, void* privateData = null)
    {
        Flags = flags;
        HeapType = heapType;
        ExtraHeapFlags = D3D12MA_RECOMMENDED_HEAP_FLAGS;
        CustomPool = null;
        pPrivateData = privateData;
    }

    /// <summary>Constructor initializing description of an allocation to be created in a default pool of a specific <see cref="D3D12_HEAP_TYPE" />.</summary>
    /// <param name="heapType"></param>
    /// <param name="flags"></param>
    /// <param name="privateData"></param>
    /// <param name="extraHeapFlags"></param>
    public D3D12MA_ALLOCATION_DESC(D3D12_HEAP_TYPE heapType, D3D12MA_ALLOCATION_FLAGS flags, void* privateData, D3D12_HEAP_FLAGS extraHeapFlags)
    {
        Flags = flags;
        HeapType = heapType;
        ExtraHeapFlags = extraHeapFlags;
        CustomPool = null;
        pPrivateData = privateData;
    }
}
