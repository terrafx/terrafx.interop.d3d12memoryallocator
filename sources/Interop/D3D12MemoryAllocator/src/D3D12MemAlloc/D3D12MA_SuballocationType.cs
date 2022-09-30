// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

internal enum D3D12MA_SuballocationType
{
    D3D12MA_SUBALLOCATION_TYPE_FREE = 0,

    D3D12MA_SUBALLOCATION_TYPE_ALLOCATION = 1
}
