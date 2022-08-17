// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using TerraFX.Interop.DirectX;

namespace TerraFX.Interop.DirectX.UnitTests;

internal unsafe struct TestContext
{
    public D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks;

    public ID3D12Device* device;

    public D3D12MA_Allocator* allocator;

    public D3D12MA_ALLOCATOR_FLAGS allocatorFlags;
}
