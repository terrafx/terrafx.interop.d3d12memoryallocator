// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit e7e5c2a4fee52f9c4e29a16f5b108c86b00914a0
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_PoolListItemTraits : D3D12MA_IItemTypeTraits<D3D12MA_PoolPimpl>
    {
        public D3D12MA_PoolPimpl* GetPrev(D3D12MA_PoolPimpl* item) => item->m_PrevPool;

        public D3D12MA_PoolPimpl* GetNext(D3D12MA_PoolPimpl* item) => item->m_NextPool;

        public D3D12MA_PoolPimpl** AccessPrev(D3D12MA_PoolPimpl* item) => &item->m_PrevPool;

        public D3D12MA_PoolPimpl** AccessNext(D3D12MA_PoolPimpl* item) => &item->m_NextPool;
    }
}
