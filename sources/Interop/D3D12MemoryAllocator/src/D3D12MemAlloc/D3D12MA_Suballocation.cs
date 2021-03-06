// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;

namespace TerraFX.Interop
{
    /// <summary>Represents a region of <see cref="D3D12MA_NormalBlock"/> that is either assigned and returned as allocated memory block or free.</summary>
    internal unsafe struct D3D12MA_Suballocation : IDisposable
    {
        [NativeTypeName("UINT64")]
        public ulong offset;

        [NativeTypeName("UINT64")]
        public ulong size;

        public void* userData;

        public D3D12MA_SuballocationType type;

        public void Dispose() { }
    };
}
