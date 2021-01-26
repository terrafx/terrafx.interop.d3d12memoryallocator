// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Interop.D3D12MemoryAllocator;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class StringBuilder

    internal unsafe partial struct StringBuilder : IDisposable
    {
        [NativeTypeName("Vector<WCHAR>")] private Vector<char> m_Data;

        public StringBuilder(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_Data = new(allocationCallbacks);
        }

        public void Dispose() { m_Data.Dispose(); }


        [return: NativeTypeName("size_t")]
        public readonly nuint GetLength() { return m_Data.size(); }

        [return: NativeTypeName("LPCWSTR")]
        public readonly char* GetData() { return m_Data.data(); }

        public void Add([NativeTypeName("WCHAR")] char ch) { m_Data.push_back(&ch); }
        public partial void Add([NativeTypeName("LPCWSTR")] char* str);
        public void Add(string str) { fixed (char* p = str) Add(p); }
        public void AddNewLine() { Add('\n'); }
        public partial void AddNumber([NativeTypeName("UINT")] uint num);
        public partial void AddNumber([NativeTypeName("UINT64")] ulong num);
    }

    internal unsafe partial struct StringBuilder
    {
        public partial void Add(char* str)
        {
            nuint len = wcslen(str);
            if (len > 0)
            {
                nuint oldCount = m_Data.size();
                m_Data.resize(oldCount + len);
                memcpy(m_Data.data() + oldCount, str, len * sizeof(char));
            }
        }

        public partial void AddNumber(uint num)
        {
            char* buf = stackalloc char[11];
            buf[10] = '\0';
            char* p = &buf[10];
            do
            {
                *--p = (char)('0' + (num % 10));
                num /= 10;
            }
            while (num > 0);
            Add(p);
        }

        public partial void AddNumber(ulong num)
        {
            char* buf = stackalloc char[21];
            buf[20] = '\0';
            char* p = &buf[20];
            do
            {
                *--p = (char)('0' + (num % 10));
                num /= 10;
            }
            while (num > 0);
            Add(p);
        }
    }
}
