// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.JsonWriter.CollectionType;

namespace TerraFX.Interop
{
    ////////////////////////////////////////////////////////////////////////////////
    // Private class JsonWriter
    internal unsafe partial struct JsonWriter : IDisposable
    {
        private StringBuilder* m_SB;
        private Vector<StackItem> m_Stack;
        private bool m_InsideString;

        public JsonWriter(ALLOCATION_CALLBACKS* allocationCallbacks, StringBuilder* stringBuilder)
        {
            m_SB = stringBuilder;
            m_Stack = new(allocationCallbacks);
            m_InsideString = false;
        }

        public partial void Dispose();

        public partial void BeginObject(bool singleLine = false);

        public partial void EndObject();

        public partial void BeginArray(bool singleLine = false);

        public partial void EndArray();

        public partial void WriteString([NativeTypeName("LPCWSTR")] char* pStr);

        public void WriteString(string str) { fixed (char* p = str) WriteString(p); }

        public partial void BeginString([NativeTypeName("LPCWSTR")] char* pStr = null);

        public partial void ContinueString([NativeTypeName("LPCWSTR")] char* pStr);

        public void ContinueString(string str) { fixed (char* p = str) ContinueString(p); }

        public partial void ContinueString([NativeTypeName("UINT")] uint num);

        public partial void ContinueString([NativeTypeName("UINT64")] ulong num);

        public partial void AddAllocationToObject(Allocation* alloc);

        // void ContinueString_Pointer(const void* ptr);
        public partial void EndString([NativeTypeName("LPCWSTR")] char* pStr = null);

        public partial void WriteNumber([NativeTypeName("UINT")] uint num);

        public partial void WriteNumber([NativeTypeName("UINT64")] ulong num);

        public partial void WriteBool(bool b);

        public partial void WriteNull();

        private const string INDENT = "  ";

        internal enum CollectionType
        {
            COLLECTION_TYPE_OBJECT,
            COLLECTION_TYPE_ARRAY,
        };

        struct StackItem
        {
            public CollectionType type;
            [NativeTypeName("UINT")] public uint valueCount;
            public bool singleLineMode;
        };

        private partial void BeginValue(bool isString);

        private partial void WriteIndent(bool oneLess = false);
    }

    internal unsafe partial struct JsonWriter
    {
        public partial void Dispose()
        {
            D3D12MA_ASSERT(!m_InsideString);
            D3D12MA_ASSERT(m_Stack.empty());
        }

        public partial void BeginObject(bool singleLine)
        {
            D3D12MA_ASSERT(!m_InsideString);

            BeginValue(false);
            m_SB->Add('{');

            StackItem stackItem;
            stackItem.type = COLLECTION_TYPE_OBJECT;
            stackItem.valueCount = 0;
            stackItem.singleLineMode = singleLine;
            m_Stack.push_back(&stackItem);
        }

        public partial void EndObject()
        {
            D3D12MA_ASSERT(!m_InsideString);
            D3D12MA_ASSERT(!m_Stack.empty() && m_Stack.back()->type == COLLECTION_TYPE_OBJECT);
            D3D12MA_ASSERT(m_Stack.back()->valueCount % 2 == 0);

            WriteIndent(true);
            m_SB->Add('}');

            m_Stack.pop_back();
        }

        public partial void BeginArray(bool singleLine)
        {
            D3D12MA_ASSERT(!m_InsideString);

            BeginValue(false);
            m_SB->Add('[');

            StackItem stackItem;
            stackItem.type = COLLECTION_TYPE_ARRAY;
            stackItem.valueCount = 0;
            stackItem.singleLineMode = singleLine;
            m_Stack.push_back(&stackItem);
        }

        public partial void EndArray()
        {
            D3D12MA_ASSERT(!m_InsideString);
            D3D12MA_ASSERT(!m_Stack.empty() && m_Stack.back()->type == COLLECTION_TYPE_ARRAY);

            WriteIndent(true);
            m_SB->Add(']');

            m_Stack.pop_back();
        }

        public partial void WriteString(char* pStr)
        {
            BeginString(pStr);
            EndString();
        }

        public partial void BeginString(char* pStr)
        {
            D3D12MA_ASSERT(!m_InsideString);

            BeginValue(true);
            m_InsideString = true;
            m_SB->Add('"');
            if (pStr != null)
            {
                ContinueString(pStr);
            }
        }

        public partial void ContinueString(char* pStr)
        {
            D3D12MA_ASSERT(m_InsideString);
            D3D12MA_ASSERT(pStr);

            for (char* p = pStr; *p != 0; ++p)
            {
                // the strings we encode are assumed to be in UTF-16LE format, the native
                // windows wide character unicode format. In this encoding unicode code
                // points U+0000 to U+D7FF and U+E000 to U+FFFF are encoded in two bytes,
                // and everything else takes more than two bytes. We will reject any
                // multi wchar character encodings for simplicity.
                uint val = *p;
                D3D12MA_ASSERT((val <= 0xD7FF) || (0xE000 <= val && val <= 0xFFFF));
                switch (*p)
                {
                    case '"': m_SB->Add('\\'); m_SB->Add('"'); break;
                    case '\\': m_SB->Add('\\'); m_SB->Add('\\'); break;
                    case '/':  m_SB->Add('\\'); m_SB->Add('/'); break;
                    case '\b': m_SB->Add('\\'); m_SB->Add('b'); break;
                    case '\f': m_SB->Add('\\'); m_SB->Add('f'); break;
                    case '\n': m_SB->Add('\\'); m_SB->Add('n'); break;
                    case '\r': m_SB->Add('\\'); m_SB->Add('r'); break;
                    case '\t': m_SB->Add('\\'); m_SB->Add('t'); break;
                    default:
                        // conservatively use encoding \uXXXX for any unicode character
                        // requiring more than one byte.
                        if (32 <= val && val < 256)
                            m_SB->Add(*p);
                        else
                        {
                            m_SB->Add('\\');
                            m_SB->Add('u');
                            for (uint i = 0; i < 4; ++i)
                            {
                                uint hexDigit = (val & 0xF000) >> 12;
                                val <<= 4;
                                if (hexDigit < 10)
                                    m_SB->Add((char)('0' + hexDigit));
                                else
                                    m_SB->Add((char)('A' + hexDigit));
                            }
                        }
                        break;
                }
            }
        }

        public partial void ContinueString(uint num)
        {
            D3D12MA_ASSERT(m_InsideString);
            m_SB->AddNumber(num);
        }

        public partial void ContinueString(ulong num)
        {
            D3D12MA_ASSERT(m_InsideString);
            m_SB->AddNumber(num);
        }

        public partial void EndString(char* pStr)
        {
            D3D12MA_ASSERT(m_InsideString);

            if (pStr != null)
                ContinueString(pStr);
            m_SB->Add('"');
            m_InsideString = false;
        }

        public partial void WriteNumber(uint num)
        {
            D3D12MA_ASSERT(!m_InsideString);
            BeginValue(false);
            m_SB->AddNumber(num);
        }

        public partial void WriteNumber(ulong num)
        {
            D3D12MA_ASSERT(!m_InsideString);
            BeginValue(false);
            m_SB->AddNumber(num);
        }

        public partial void WriteBool(bool b)
        {
            D3D12MA_ASSERT(!m_InsideString);
            BeginValue(false);
            if (b)
                m_SB->Add("true");
            else
                m_SB->Add("false");
        }

        public partial void WriteNull()
        {
            D3D12MA_ASSERT(!m_InsideString);
            BeginValue(false);
            m_SB->Add("null");
        }

        private partial void BeginValue(bool isString)
        {
            if (!m_Stack.empty())
            {
                StackItem* currItem = m_Stack.back();
                if (currItem->type == COLLECTION_TYPE_OBJECT && currItem->valueCount % 2 == 0)
                {
                    D3D12MA_ASSERT(isString);
                }

                if (currItem->type == COLLECTION_TYPE_OBJECT && currItem->valueCount % 2 == 1)
                {
                    m_SB->Add(':');
                    m_SB->Add(' ');
                }
                else if (currItem->valueCount > 0)
                {
                    m_SB->Add(',');
                    m_SB->Add(' ');
                    WriteIndent();
                }
                else
                {
                    WriteIndent();
                }
                ++currItem->valueCount;
            }
        }

        private partial void WriteIndent(bool oneLess)
        {
            if (!m_Stack.empty() && !m_Stack.back()->singleLineMode)
            {
                m_SB->AddNewLine();

                nuint count = m_Stack.size();
                if (count > 0 && oneLess)
                {
                    --count;
                }
                for (nuint i = 0; i < count; ++i)
                {
                    m_SB->Add(INDENT);
                }
            }
        }

        public partial void AddAllocationToObject(Allocation* alloc)
        {
            WriteString("Type");
            switch (alloc->m_PackedData.GetResourceDimension())
            {
                case D3D12_RESOURCE_DIMENSION_UNKNOWN:
                    WriteString("UNKNOWN");
                    break;
                case D3D12_RESOURCE_DIMENSION_BUFFER:
                    WriteString("BUFFER");
                    break;
                case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                    WriteString("TEXTURE1D");
                    break;
                case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                    WriteString("TEXTURE2D");
                    break;
                case D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                    WriteString("TEXTURE3D");
                    break;
                default:
                    D3D12MA_ASSERT(0);
                    break;
            }
            WriteString("Size");
            WriteNumber(alloc->GetSize());
            char* name = alloc->GetName();
            if (name != null)
            {
                WriteString("Name");
                WriteString(name);
            }
            if (alloc->m_PackedData.GetResourceFlags() != 0)
            {
                WriteString("Flags");
                WriteNumber((uint)alloc->m_PackedData.GetResourceFlags());
            }
            if (alloc->m_PackedData.GetTextureLayout() != 0)
            {
                WriteString("Layout");
                WriteNumber((uint)alloc->m_PackedData.GetTextureLayout());
            }
            if (alloc->m_CreationFrameIndex != 0)
            {
                WriteString("CreationFrameIndex");
                WriteNumber(alloc->m_CreationFrameIndex);
            }
        }
    }
}
