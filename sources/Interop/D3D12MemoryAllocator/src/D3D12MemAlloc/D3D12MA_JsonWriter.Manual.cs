// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_JsonWriter
{
    public void BeginString([NativeTypeName("LPCWSTR")] ReadOnlySpan<char> str)
    {
        fixed (char* pStr = str)
        {
            BeginString(pStr);
        }
    }

    public readonly void ContinueString([NativeTypeName("LPCWSTR")] ReadOnlySpan<char> str)
    {
        fixed (char* pStr = str)
        {
            ContinueString(pStr);
        }
    }

    public void EndString([NativeTypeName("LPCWSTR")] ReadOnlySpan<char> str)
    {
        fixed (char* pStr = str)
        {
            EndString(pStr);
        }
    }

    public void WriteString([NativeTypeName("LPCWSTR")] ReadOnlySpan<char> str)
    {
        fixed (char* pStr = str)
        {
            WriteString(pStr);
        }
    }
}
