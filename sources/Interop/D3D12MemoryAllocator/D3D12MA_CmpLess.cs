// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Collections.Generic;
namespace TerraFX.Interop.DirectX;

internal unsafe interface D3D12MA_CmpLess<T> : D3D12MA_CmpLess<T, T>, IComparer<T>
    where T : unmanaged
{
}

internal unsafe interface D3D12MA_CmpLess<TLeft, TRight>
    where TLeft : unmanaged
    where TRight : unmanaged
{
    bool Invoke(in TLeft lhs, in TRight rhs);
}
