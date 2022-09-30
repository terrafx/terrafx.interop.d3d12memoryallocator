// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

internal unsafe partial struct D3D12MA_StringBuilder : IDisposable
{
    [NativeTypeName("D3D12MA::Vector<WCHAR>")]
    private D3D12MA_Vector<ushort> m_Data;

    public D3D12MA_StringBuilder([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks)
    {
        m_Data = new D3D12MA_Vector<ushort>(allocationCallbacks);
    }

    public void Dispose()
    {
        m_Data.Dispose();
    }

    [return: NativeTypeName("size_t")]
    public readonly nuint GetLength()
    {
        return m_Data.size();
    }

    [return: NativeTypeName("LPCWSTR")]
    public readonly ushort* GetData()
    {
        return m_Data.data();
    }

    public void Add([NativeTypeName("WCHAR")] ushort ch)
    {
        m_Data.push_back(ch);
    }

    public void Add([NativeTypeName("LPCWSTR")] ushort* str)
    {
        nuint len = wcslen(str);

        if (len > 0)
        {
            nuint oldCount = m_Data.size();
            m_Data.resize(oldCount + len);
            _ = memcpy(m_Data.data() + oldCount, str, len * sizeof(ushort));
        }
    }

    public void AddNewLine()
    {
        Add('\n');
    }

    public void AddNumber([NativeTypeName("UINT")] uint num)
    {
        ushort* buf = stackalloc ushort[11];

        buf[10] = '\0';
        ushort* p = &buf[10];

        do
        {
            *--p = (ushort)('0' + (num % 10));
            num /= 10;
        }
        while (num != 0);

        Add(p);
    }

    public void AddNumber([NativeTypeName("UINT64")] ulong num)
    {
        ushort* buf = stackalloc ushort[21];

        buf[20] = '\0';
        ushort* p = &buf[20];

        do
        {
            *--p = (ushort)('0' + (num % 10));
            num /= 10;
        }
        while (num != 0);

        Add(p);
    }

    public void AddPointer([NativeTypeName("const void *")] void* ptr)
    {
        ushort* buf = stackalloc ushort[21];

        nuint num = (nuint)ptr;
        buf[20] = '\0';
        ushort* p = &buf[20];

        do
        {
            *--p = D3D12MA_HexDigitToChar((byte)(num & 0xF));
            num >>= 4;
        }
        while (num != 0);

        Add(p);
    }
}
