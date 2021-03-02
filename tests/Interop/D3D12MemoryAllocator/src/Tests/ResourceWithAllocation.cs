// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop.UnitTests
{
    internal unsafe struct ResourceWithAllocation : IDisposable
    {
        public ComPtr<ID3D12Resource> resource;

        public unique_ptr<D3D12MA_Allocation> allocation;

        [NativeTypeName("UINT64")]
        public ulong size;

        [NativeTypeName("UINT")]
        public uint dataSeed;

        public static void Init(out ResourceWithAllocation value)
        {
            value = default;
            value.size = UINT64_MAX;
        }

        public void Reset()
        {
            resource.Get()->Release();
            allocation.reset();
            size = UINT64_MAX;
            dataSeed = 0;
        }

        public void Dispose()
        {
            resource.Dispose();
            allocation.Dispose();
        }
    }
}
