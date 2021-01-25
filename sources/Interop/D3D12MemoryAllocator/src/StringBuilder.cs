// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12MemoryAllocator;

using UINT = System.UInt32;
using UINT64 = System.UInt64;
using size_t = nuint;
using WCHAR = System.Char;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class StringBuilder

    internal unsafe partial struct StringBuilder
    {
        public StringBuilder(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_Data = new(allocationCallbacks);
        }

        public size_t GetLength() { return m_Data.size(); }
        public WCHAR* GetData() { return m_Data.data(); }

        public void Add(WCHAR ch) { m_Data.push_back(&ch); }
        public partial void Add(WCHAR* str);
        public void AddNewLine() { Add('\n'); }
        public partial void AddNumber(UINT num);
        public partial void AddNumber(UINT64 num);

        private Vector<WCHAR> m_Data;
    }

    internal unsafe partial struct StringBuilder
    {
        public partial void Add(WCHAR* str)
        {
            [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            static extern size_t wcslen(WCHAR* _String);

            size_t len = wcslen(str);
            if (len > 0)
            {
                size_t oldCount = m_Data.size();
                m_Data.resize(oldCount + len);
                memcpy(m_Data.data() + oldCount, str, len * sizeof(WCHAR));
            }
        }

        public partial void AddNumber(UINT num)
        {
            WCHAR* buf = stackalloc WCHAR[11];
            buf[10] = '\0';
            WCHAR* p = &buf[10];
            do
            {
                *--p = (WCHAR)('0' + (num % 10));
                num /= 10;
            }
            while (num > 0);
            Add(p);
        }

        public partial void AddNumber(UINT64 num)
        {
            WCHAR* buf = stackalloc WCHAR[21];
            buf[20] = '\0';
            WCHAR* p = &buf[20];
            do
            {
                *--p = (WCHAR)('0' + (num % 10));
                num /= 10;
            }
            while (num > 0);
            Add(p);
        }
    }
}
