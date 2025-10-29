// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_PoolListItemTraits : D3D12MA_ItemTypeTraits<D3D12MA_PoolListItemTraits, D3D12MA_PoolPimpl>
{
    public static D3D12MA_PoolPimpl* GetPrev([NativeTypeName("const ItemType *")] D3D12MA_PoolPimpl* item)
    {
        return item->m_PrevPool;
    }

    public static D3D12MA_PoolPimpl* GetNext([NativeTypeName("const ItemType *")] D3D12MA_PoolPimpl* item)
    {
        return item->m_NextPool;
    }

    [return: NativeTypeName("ItemType *&")]
    public static ref D3D12MA_PoolPimpl* AccessPrev([NativeTypeName("ItemType *")] D3D12MA_PoolPimpl* item)
    {
        return ref item->m_PrevPool;
    }

    [return: NativeTypeName("ItemType *&")]
    public static ref D3D12MA_PoolPimpl* AccessNext([NativeTypeName("ItemType *")] D3D12MA_PoolPimpl* item)
    {
        return ref item->m_NextPool;
    }
}
