// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

internal struct D3D12MA_SuballocationOffsetLess
    : D3D12MA_CmpLess<D3D12MA_Suballocation>
{
    public int Compare(D3D12MA_Suballocation lhs, D3D12MA_Suballocation rhs)
    {
        return lhs.offset.CompareTo(rhs.offset);
    }

    public readonly bool Invoke([NativeTypeName("const D3D12MA::Suballocation &")] in D3D12MA_Suballocation lhs, [NativeTypeName("const D3D12MA::Suballocation &")] in D3D12MA_Suballocation rhs)
    {
        return lhs.offset < rhs.offset;
    }
}
