// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

internal unsafe interface D3D12MA_ItemTypeTraits<ItemTypeTraits, ItemType>
    where ItemTypeTraits : unmanaged, D3D12MA_ItemTypeTraits<ItemTypeTraits, ItemType>
    where ItemType : unmanaged
{
    static abstract ItemType* GetPrev([NativeTypeName("const ItemType *")] ItemType* item);

    static abstract ItemType* GetNext([NativeTypeName("const ItemType *")] ItemType* item);

    [return: NativeTypeName("ItemType *&")]
    static abstract ref ItemType* AccessPrev(ItemType* item);

    [return: NativeTypeName("ItemType *&")]
    static abstract ref ItemType* AccessNext(ItemType* item);
}
