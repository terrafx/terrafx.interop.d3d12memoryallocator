// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using TerraFX.Interop.Windows;

namespace TerraFX.Interop.DirectX;

internal static unsafe partial class D3D12MA_PoolAllocator
{
    private static T* Allocate<T>(ref this D3D12MA_PoolAllocator<T> self)
        where T : unmanaged, IDisposable
    {
        for (nuint i = self.m_ItemBlocks.size(); i-- != 0;)
        {
            ref D3D12MA_PoolAllocator<T>.ItemBlock block = ref self.m_ItemBlocks[i];

            // This block has some free items: Use first one.
            if (block.FirstFreeIndex != uint.MaxValue)
            {
                D3D12MA_PoolAllocator<T>.Item* pItem = &block.pItems[block.FirstFreeIndex];
                block.FirstFreeIndex = pItem->NextFreeIndex;

                T* result = (T*)(&pItem->Value);
                return result;
            }
        }

        {
            // No block has free item: Create new one and use it.
            ref D3D12MA_PoolAllocator<T>.ItemBlock newBlock = ref self.CreateNewBlock();

            D3D12MA_PoolAllocator<T>.Item* pItem = &newBlock.pItems[0];
            newBlock.FirstFreeIndex = pItem->NextFreeIndex;

            T* result = &pItem->Value;
            return result;
        }
    }

    public static D3D12MA_Allocation* Alloc(ref this D3D12MA_PoolAllocator<D3D12MA_Allocation> self, D3D12MA_AllocatorPimpl* allocator, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment, BOOL wasZeroInitialized)
    {
        D3D12MA_Allocation* result = self.Allocate();
        result->_ctor(allocator, size, alignment, wasZeroInitialized);
        return result;
    }

    public static D3D12MA_BlockMetadata_TLSF.Block* Alloc(ref this D3D12MA_PoolAllocator<D3D12MA_BlockMetadata_TLSF.Block> self)
    {
        D3D12MA_BlockMetadata_TLSF.Block* result = self.Allocate();
        result->_ctor();
        return result;
    }

    public static D3D12MA_List<T>.Item* Alloc<T>(ref this D3D12MA_PoolAllocator<D3D12MA_List<T>.Item> self)
        where T : unmanaged, IDisposable
    {
        D3D12MA_List<T>.Item* result = self.Allocate();
        result->_ctor();
        return result;
    }
}
