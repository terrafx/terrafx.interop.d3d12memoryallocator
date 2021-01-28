// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    internal unsafe struct StringBuilder : IDisposable
    {
        [NativeTypeName("Vector<WCHAR>")]
        private Vector<ushort> m_Data;

        public StringBuilder(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_Data = new Vector<ushort>(allocationCallbacks);
        }

        public void Dispose() => m_Data.Dispose();

        [return: NativeTypeName("size_t")]
        public readonly nuint GetLength() => m_Data.size();

        [return: NativeTypeName("LPCWSTR")]
        public readonly ushort* GetData() => m_Data.data();

        public void Add([NativeTypeName("WCHAR")] ushort ch) => m_Data.push_back(&ch);

        public void Add([NativeTypeName("LPCWSTR")] ushort* str)
        {
            nuint len = wcslen(str);
            if (len > 0)
            {
                nuint oldCount = m_Data.size();
                m_Data.resize(oldCount + len);
                memcpy(m_Data.data() + oldCount, str, len * sizeof(ushort));
            }
        }

        public void Add(string str) { fixed (void* p = str) Add((ushort*)p); }

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
