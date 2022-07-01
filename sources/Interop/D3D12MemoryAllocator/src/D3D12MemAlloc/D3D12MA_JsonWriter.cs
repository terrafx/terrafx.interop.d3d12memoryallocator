// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.DirectX.D3D12MA_JsonWriter.CollectionType;

namespace TerraFX.Interop.DirectX
{
    /// <summary>
    /// Allows to conveniently build a correct JSON document to be written to the <see cref="D3D12MA_StringBuilder"/> passed to the constructor.
    /// </summary>
    internal unsafe struct D3D12MA_JsonWriter : IDisposable
    {
        private const string INDENT = "  ";

        private D3D12MA_StringBuilder* m_SB;
        private D3D12MA_Vector<StackItem> m_Stack;
        private bool m_InsideString;

        /// <summary>
        /// Creates a new <see cref="D3D12MA_JsonWriter"/> instance.
        /// </summary>
        /// <param name="allocationCallbacks">The allocation callbacks</param>
        /// <param name="stringBuilder">String builder to write the document to. Must remain alive for the whole lifetime of this object.</param>
        public D3D12MA_JsonWriter([NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks, [NativeTypeName("StringBuilder&")] D3D12MA_StringBuilder* stringBuilder)
        {
            m_SB = stringBuilder;

            Unsafe.SkipInit(out m_Stack);
            D3D12MA_Vector<StackItem>._ctor(ref m_Stack, allocationCallbacks);

            m_InsideString = false;
        }

        /// <summary>
        /// Creates a new <see cref="D3D12MA_JsonWriter"/> instance.
        /// </summary>
        /// <param name="allocationCallbacks">The allocation callbacks</param>
        /// <param name="stringBuilder">String builder to write the document to. Must remain alive for the whole lifetime of this object.</param>
        public D3D12MA_JsonWriter([NativeTypeName("const D3D12MA_ALLOCATION_CALLBACKS&")] ref D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("StringBuilder&")] D3D12MA_StringBuilder* stringBuilder)
        {
            m_SB = stringBuilder;

            Unsafe.SkipInit(out m_Stack);
            D3D12MA_Vector<StackItem>._ctor(ref m_Stack, (D3D12MA_ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref allocationCallbacks));

            m_InsideString = false;
        }

        public void Dispose()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && m_Stack.empty());

            m_Stack.Dispose();
        }

        /// <summary>
        /// Begins object by writing "{".
        /// Inside an object, you must call pairs of WriteString and a value, e.g.:
        /// <c>j.BeginObject(true); j.WriteString("A"); j.WriteNumber(1); j.WriteString("B"); j.WriteNumber(2); j.EndObject();</c>
        /// Will write: <c>{ "A": 1, "B": 2 }</c>.
        /// </summary>
        public void BeginObject(bool singleLine = false)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);

            BeginValue(false);
            m_SB->Add('{');

            StackItem stackItem;
            stackItem.type = COLLECTION_TYPE_OBJECT;
            stackItem.valueCount = 0;
            stackItem.singleLineMode = singleLine;
            m_Stack.push_back(in stackItem);
        }

        /// <summary>
        /// Ends object by writing "}".
        /// </summary>
        public void EndObject()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_Stack.empty() && (m_Stack.back()->type == COLLECTION_TYPE_OBJECT));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (m_Stack.back()->valueCount % 2 == 0));

            WriteIndent(true);
            m_SB->Add('}');

            m_Stack.pop_back();
        }

        /// <summary>
        /// Begins array by writing "[".
        /// Inside an array, you can write a sequence of any values.
        /// </summary>
        public void BeginArray(bool singleLine = false)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);

            BeginValue(false);
            m_SB->Add('[');

            StackItem stackItem;
            stackItem.type = COLLECTION_TYPE_ARRAY;
            stackItem.valueCount = 0;
            stackItem.singleLineMode = singleLine;
            m_Stack.push_back(in stackItem);
        }

        /// <summary>
        /// Ends array by writing "[".
        /// </summary>
        public void EndArray()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (!m_InsideString));
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_Stack.empty() && (m_Stack.back()->type == COLLECTION_TYPE_ARRAY));

            WriteIndent(true);
            m_SB->Add(']');

            m_Stack.pop_back();
        }

        /// <summary>
        /// Writes a string value inside "".
        /// </summary>
        /// <param name="pStr">Can contain any UTF-16 characters, including '"', new line etc. - they will be properly escaped.</param>
        public void WriteString([NativeTypeName("LPCWSTR")] ushort* pStr)
        {
            BeginString(pStr);
            EndString();
        }

        /// <summary>
        /// Writes a string value inside "".
        /// </summary>
        /// <param name="str">Can contain any UTF-16 characters, including '"', new line etc. - they will be properly escaped.</param>
        public void WriteString(string str)
        {
            fixed (char* p = str)
            {
                WriteString((ushort*)p);
            }
        }

        /// <summary>
        /// Begins writing a string value.
        /// Call <see cref="BeginString(ushort*)"/>, <see cref="ContinueString(ushort*)"/>, <see cref="ContinueString(ushort*)"/>, ..., <see cref="EndString(ushort*)"/> instead of
        /// <see cref="WriteString(ushort*)"/> to conveniently build the string content incrementally, made of parts including numbers.
        /// </summary>
        public void BeginString([NativeTypeName("LPCWSTR")] ushort* pStr = null)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);

            BeginValue(true);

            m_InsideString = true;
            m_SB->Add('"');

            if (pStr != null)
            {
                ContinueString(pStr);
            }
        }

        /// <summary>
        /// Posts next part of an open string.
        /// </summary>
        public void ContinueString([NativeTypeName("LPCWSTR")] ushort* pStr)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && m_InsideString);
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (pStr != null));

            for (ushort* p = pStr; *p != 0; ++p)
            {
                // the strings we encode are assumed to be in UTF-16LE format, the native
                // windows wide character Unicode format. In this encoding Unicode code
                // points U+0000 to U+D7FF and U+E000 to U+FFFF are encoded in two bytes,
                // and everything else takes more than two bytes. We will reject any
                // multi wchar character encodings for simplicity.

                uint val = (uint)*p;
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((val <= 0xD7FF) || (0xE000 <= val && val <= 0xFFFF)));

                switch (*p)
                {
                    case '"':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('"');
                        break;
                    }

                    case '\\':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('\\');
                        break;
                    }

                    case '/':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('/');
                        break;
                    }

                    case '\b':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('b');
                        break;
                    }

                    case '\f':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('f');
                        break;
                    }

                    case '\n':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('n');
                        break;
                    }

                    case '\r':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('r');
                        break;
                    }

                    case '\t':
                    {
                        m_SB->Add('\\');
                        m_SB->Add('t');
                        break;
                    }

                    default:
                    {
                        // conservatively use encoding \uXXXX for any Unicode character
                        // requiring more than one byte.

                        if (32 <= val && val < 256)
                        {
                            m_SB->Add(*p);
                        }
                        else
                        {
                            m_SB->Add('\\');
                            m_SB->Add('u');

                            for (uint i = 0; i < 4; ++i)
                            {
                                uint hexDigit = (val & 0xF000) >> 12;
                                val <<= 4;

                                if (hexDigit < 10)
                                {
                                    m_SB->Add((ushort)('0' + hexDigit));
                                }
                                else
                                {
                                    m_SB->Add((ushort)('A' + hexDigit));
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        public void ContinueString(string str)
        {
            fixed (char* p = str)
            {
                ContinueString((ushort*)p);
            }
        }

        /// <summary>
        /// Posts next part of an open string. The number is converted to decimal characters.
        /// </summary>
        public void ContinueString([NativeTypeName("UINT")] uint num)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && m_InsideString);
            m_SB->AddNumber(num);
        }

        public void ContinueString([NativeTypeName("UINT64")] ulong num)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && m_InsideString);
            m_SB->AddNumber(num);
        }

        public void AddAllocationToObject(D3D12MA_Allocation* alloc)
        {
            WriteString("Type");

            switch (alloc->m_PackedData.GetResourceDimension())
            {
                case D3D12_RESOURCE_DIMENSION_UNKNOWN:
                {
                    WriteString("UNKNOWN");
                    break;
                }

                case D3D12_RESOURCE_DIMENSION_BUFFER:
                {
                    WriteString("BUFFER");
                    break;
                }

                case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                {
                    WriteString("TEXTURE1D");
                    break;
                }

                case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                {
                    WriteString("TEXTURE2D");
                    break;
                }

                case D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                {
                    WriteString("TEXTURE3D");
                    break;
                }

                default:
                {
                    D3D12MA_ASSERT(false);
                    break;
                }
            }

            WriteString("Size");
            WriteNumber(alloc->GetSize());

            ushort* name = alloc->GetName();

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

        // Posts next part of an open string. Pointer value is converted to characters
        // using "%p" formatting - shown as hexadecimal number, e.g.: 000000081276Ad00
        // void ContinueString_Pointer(const void* ptr);

        /// <summary>
        /// Ends writing a string value by writing '"'.
        /// </summary>
        public void EndString([NativeTypeName("LPCWSTR")] ushort* pStr = null)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && m_InsideString);

            if (pStr != null)
            {
                ContinueString(pStr);
            }

            m_SB->Add('"');
            m_InsideString = false;
        }

        /// <summary>
        /// Writes a number value.
        /// </summary>
        public void WriteNumber([NativeTypeName("UINT")] uint num)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);
            BeginValue(false);
            m_SB->AddNumber(num);
        }

        public void WriteNumber([NativeTypeName("UINT64")] ulong num)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);
            BeginValue(false);
            m_SB->AddNumber(num);
        }

        /// <summary>
        /// Writes a boolean value - false or true.
        /// </summary>
        public void WriteBool(bool b)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);
            BeginValue(false);

            if (b)
            {
                m_SB->Add("true");
            }
            else
            {
                m_SB->Add("false");
            }
        }

        /// <summary>
        /// Writes a null value.
        /// </summary>
        public void WriteNull()
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && !m_InsideString);
            BeginValue(false);
            m_SB->Add("null");
        }

        private void BeginValue(bool isString)
        {
            if (!m_Stack.empty())
            {
                StackItem* currItem = m_Stack.back();

                if ((currItem->type == COLLECTION_TYPE_OBJECT) && (currItem->valueCount % 2 == 0))
                {
                    D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && isString);
                }

                if ((currItem->type == COLLECTION_TYPE_OBJECT) && (currItem->valueCount % 2 == 1))
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

        private void WriteIndent(bool oneLess = false)
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

        internal enum CollectionType
        {
            COLLECTION_TYPE_OBJECT,
            COLLECTION_TYPE_ARRAY,
        };

        internal struct StackItem
        {
            public CollectionType type;

            [NativeTypeName("UINT")]
            public uint valueCount;

            public bool singleLineMode;
        };
    }
}
