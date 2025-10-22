// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_JsonWriter
{
    public void BeginString([NativeTypeName("LPCWSTR")] string str)
    {
        fixed (char* pStr = str)
        {
            BeginString(pStr);
        }
    }

    public readonly void ContinueString([NativeTypeName("LPCWSTR")] string str)
    {
        fixed (char* pStr = str)
        {
            ContinueString(pStr);
        }
    }

    public void EndString([NativeTypeName("LPCWSTR")] string str)
    {
        fixed (char* pStr = str)
        {
            EndString(pStr);
        }
    }

    public void WriteString([NativeTypeName("LPCWSTR")] string str)
    {
        fixed (char* pStr = str)
        {
            WriteString(pStr);
        }
    }
}
