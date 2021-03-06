// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    /// <summary>Parameters of planned allocation inside a NormalBlock.</summary>
    internal struct D3D12MA_AllocationRequest
    {
        [NativeTypeName("UINT64")]
        public ulong offset;

        [NativeTypeName("UINT64")]
        public ulong sumFreeSize; // Sum size of free items that overlap with proposed allocation.

        [NativeTypeName("UINT64")]
        public ulong sumItemSize; // Sum size of items to make lost that overlap with proposed allocation.

        public D3D12MA_List<D3D12MA_Suballocation>.iterator item;

        [NativeTypeName("BOOL")]
        public int zeroInitialized;
    };
}
