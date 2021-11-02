// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    internal unsafe struct D3D12MA_StringBuilder : IDisposable
    {
        [NativeTypeName("Vector<WCHAR>")]
        private D3D12MA_Vector<ushort> m_Data;

        public D3D12MA_StringBuilder(D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            Unsafe.SkipInit(out m_Data);
            D3D12MA_Vector<ushort>._ctor(ref m_Data, allocationCallbacks);
        }

        public D3D12MA_StringBuilder(ref D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks)
        {
            Unsafe.SkipInit(out m_Data);
            D3D12MA_Vector<ushort>._ctor(ref m_Data, (D3D12MA_ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref allocationCallbacks));
        }

        public void Dispose() => m_Data.Dispose();

        [return: NativeTypeName("size_t")]
        public readonly nuint GetLength() => m_Data.size();

        [return: NativeTypeName("LPCWSTR")]
        public readonly ushort* GetData() => m_Data.data();

        public void Add([NativeTypeName("WCHAR")] ushort ch) => m_Data.push_back(in ch);

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

        public void Add(string str)
        {
            fixed (char* p = str)
            {
                Add((ushort*)p);
            }
        }

        public void AddNewLine() => Add('\n');

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
            while (num > 0);
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
            while (num > 0);

            Add(p);
        }
    }
}
