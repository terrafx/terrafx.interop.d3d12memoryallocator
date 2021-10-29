// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 49fb8acf85d038afbbd5f532ab964fa0835ce819
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop
{
    internal enum D3D12MA_ResourceClass
    {
        Unknown,
        Buffer,
        Non_RT_DS_Texture,
        RT_DS_Texture
    }
}
