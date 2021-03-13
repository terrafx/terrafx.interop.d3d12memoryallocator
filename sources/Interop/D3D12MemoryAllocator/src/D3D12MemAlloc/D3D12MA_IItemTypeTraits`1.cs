// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 3a335d55c99e605775bbe9fe9c01ee6212804bed
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    internal unsafe interface D3D12MA_IItemTypeTraits<TItemType>
        where TItemType : unmanaged, D3D12MA_IItemTypeTraits<TItemType>
    {
        public TItemType* GetPrev();

        public TItemType* GetNext();

        [return: NativeTypeName("ItemType*&")]
        public TItemType** AccessPrev();

        [return: NativeTypeName("ItemType*&")]
        public TItemType** AccessNext();
    }
}
