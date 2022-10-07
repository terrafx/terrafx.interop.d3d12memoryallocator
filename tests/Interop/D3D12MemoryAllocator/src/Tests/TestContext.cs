// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX.UnitTests;

public unsafe partial struct TestContext
{
    public D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks;

    public ID3D12Device* device;

    public D3D12MA_Allocator* allocator;

    public D3D12MA_ALLOCATOR_FLAGS allocatorFlags;
}
