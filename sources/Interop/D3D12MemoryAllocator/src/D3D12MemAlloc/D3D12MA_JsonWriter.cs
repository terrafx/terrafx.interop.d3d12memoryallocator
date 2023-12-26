// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12MA_JsonWriter.CollectionType;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Allows to conveniently build a correct JSON document to be written to the StringBuilder passed to the constructor.</summary>
internal unsafe partial struct D3D12MA_JsonWriter : IDisposable
{
    [NativeTypeName("WCHAR * const")]
    private const string INDENT = "  ";

    private readonly D3D12MA_StringBuilder* m_SB;

    private D3D12MA_Vector<StackItem> m_Stack;

    private bool m_InsideString;

    // stringBuilder - string builder to write the document to. Must remain alive for the whole lifetime of this object.
    public D3D12MA_JsonWriter([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks, [NativeTypeName("D3D12MA::StringBuilder &")] ref D3D12MA_StringBuilder stringBuilder)
    {
        m_SB = (D3D12MA_StringBuilder*)(Unsafe.AsPointer(ref stringBuilder));
        m_Stack = new D3D12MA_Vector<StackItem>(allocationCallbacks);
        m_InsideString = false;
    }

    public void Dispose()
    {
        D3D12MA_ASSERT(!m_InsideString);
        D3D12MA_ASSERT(m_Stack.empty());
        m_Stack.Dispose();
    }

    // Begins object by writing "{".
    // Inside an object, you must call pairs of WriteString and a value, e.g.:
    // j.BeginObject(true); j.WriteString("A"); j.WriteNumber(1); j.WriteString("B"); j.WriteNumber(2); j.EndObject();
    // Will write: { "A": 1, "B": 2 }
    public void BeginObject(bool singleLine = false)
    {
        D3D12MA_ASSERT(!m_InsideString);

        BeginValue(false);
        m_SB->Add('{');

        StackItem stackItem;
        stackItem.type = COLLECTION_TYPE_OBJECT;
        stackItem.valueCount = 0;
        stackItem.singleLineMode = singleLine;
        m_Stack.push_back(stackItem);
    }

    // Ends object by writing "}".
    public void EndObject()
    {
        D3D12MA_ASSERT(!m_InsideString);
        D3D12MA_ASSERT(!m_Stack.empty() && m_Stack.back().type == COLLECTION_TYPE_OBJECT);
        D3D12MA_ASSERT(m_Stack.back().valueCount % 2 == 0);

        WriteIndent(true);
        m_SB->Add('}');

        m_Stack.pop_back();
    }

    // Begins array by writing "[".
    // Inside an array, you can write a sequence of any values.
    public void BeginArray(bool singleLine = false)
    {
        D3D12MA_ASSERT(!m_InsideString);

        BeginValue(false);
        m_SB->Add('[');

        StackItem stackItem;
        stackItem.type = COLLECTION_TYPE_ARRAY;
        stackItem.valueCount = 0;
        stackItem.singleLineMode = singleLine;
        m_Stack.push_back(stackItem);
    }

    // Ends array by writing "[".
    public void EndArray()
    {
        D3D12MA_ASSERT(!m_InsideString);
        D3D12MA_ASSERT(!m_Stack.empty() && m_Stack.back().type == COLLECTION_TYPE_ARRAY);

        WriteIndent(true);
        m_SB->Add(']');

        m_Stack.pop_back();
    }

    // Writes a string value inside "".
    // pStr can contain any UTF-16 characters, including '"', new line etc. - they will be properly escaped.
    public void WriteString([NativeTypeName("LPCWSTR")] char* pStr)
    {
        BeginString(pStr);
        EndString();
    }

    // Begins writing a string value.
    // Call BeginString, ContinueString, ContinueString, ..., EndString instead of
    // WriteString to conveniently build the string content incrementally, made of
    // parts including numbers.
    public void BeginString([NativeTypeName("LPCWSTR")] char* pStr = null)
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

    // Posts next part of an open string.
    public void ContinueString([NativeTypeName("LPCWSTR")] char* pStr)
    {
        D3D12MA_ASSERT(m_InsideString);
        D3D12MA_ASSERT(pStr != null);

        for (char* p = pStr; *p != '\0'; ++p)
        {
            // the strings we encode are assumed to be in UTF-16LE format, the native
            // windows wide character Unicode format. In this encoding Unicode code
            // points U+0000 to U+D7FF and U+E000 to U+FFFF are encoded in two bytes,
            // and everything else takes more than two bytes. We will reject any
            // multi wchar character encodings for simplicity.
            uint val = (uint)(*p);

            D3D12MA_ASSERT(((val <= 0xD7FF) || (0xE000 <= val && val <= 0xFFFF)), "Character not currently supported.");

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
                    if ((32 <= val) && (val < 256))
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
                                m_SB->Add((char)('0' + (char)(hexDigit)));
                            }   
                            else
                            {   
                                m_SB->Add((char)('A' + (char)(hexDigit)));
                            }
                        }
                    }
                    break;
                }
            }
        }
    }

    // Posts next part of an open string. The number is converted to decimal characters.
    public void ContinueString([NativeTypeName("UINT")] uint num)
    {
        D3D12MA_ASSERT(m_InsideString);
        m_SB->AddNumber(num);
    }

    public void ContinueString([NativeTypeName("UINT64")] ulong num)
    {
        D3D12MA_ASSERT(m_InsideString);
        m_SB->AddNumber(num);
    }

    public void ContinueString_Pointer([NativeTypeName("const void *")] void* ptr)
    {
        D3D12MA_ASSERT(m_InsideString);
        m_SB->AddPointer(ptr);
    }

    // Posts next part of an open string. Pointer value is converted to characters
    // using "%p" formatting - shown as hexadecimal number, e.g.: 000000081276Ad00
    // void ContinueString_Pointer(const void* ptr);
    // Ends writing a string value by writing '"'.
    public void EndString([NativeTypeName("LPCWSTR")] char* pStr = null)
    {
        D3D12MA_ASSERT(m_InsideString);

        if (pStr != null)
        {
            ContinueString(pStr);
        }

        m_SB->Add('"');
        m_InsideString = false;
    }

    // Writes a number value.
    public void WriteNumber([NativeTypeName("UINT")] uint num)
    {
        D3D12MA_ASSERT(!m_InsideString);

        BeginValue(false);
        m_SB->AddNumber(num);
    }

    public void WriteNumber([NativeTypeName("UINT64")] ulong num)
    {
        D3D12MA_ASSERT(!m_InsideString);

        BeginValue(false);
        m_SB->AddNumber(num);
    }

    // Writes a boolean value - false or true.
    public void WriteBool(bool b)
    {
        D3D12MA_ASSERT(!m_InsideString);
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

    // Writes a null value.
    public void WriteNull()
    {
        D3D12MA_ASSERT(!m_InsideString);

        BeginValue(false);
        m_SB->Add("null");
    }

    public void AddAllocationToObject([NativeTypeName("const D3D12MA::Allocation &")] in D3D12MA_Allocation alloc)
    {
        WriteString("Type");

        switch (alloc.m_PackedData.GetResourceDimension())
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
                D3D12MA_FAIL();
                break;
            }
        }

        WriteString("Size");
        WriteNumber(alloc.GetSize());

        WriteString("Usage");
        WriteNumber((uint)(alloc.m_PackedData.GetResourceFlags()));

        void* privateData = alloc.GetPrivateData();

        if (privateData != null)
        {
            WriteString("CustomData");
            BeginString();
            ContinueString_Pointer(privateData);
            EndString();
        }

        char* name = alloc.GetName();

        if (name != null)
        {
            WriteString("Name");
            WriteString(name);
        }

        if (alloc.m_PackedData.GetTextureLayout() != D3D12_TEXTURE_LAYOUT_UNKNOWN)
        {
            WriteString("Layout");
            WriteNumber((uint)(alloc.m_PackedData.GetTextureLayout()));
        }
    }

    public void AddDetailedStatisticsInfoObject([NativeTypeName("const D3D12MA::DetailedStatistics &")] in D3D12MA_DetailedStatistics stats)
    {
        BeginObject();

        WriteString("BlockCount");
        WriteNumber(stats.Stats.BlockCount);

        WriteString("BlockBytes");
        WriteNumber(stats.Stats.BlockBytes);

        WriteString("AllocationCount");
        WriteNumber(stats.Stats.AllocationCount);

        WriteString("AllocationBytes");
        WriteNumber(stats.Stats.AllocationBytes);

        WriteString("UnusedRangeCount");
        WriteNumber(stats.UnusedRangeCount);

        if (stats.Stats.AllocationCount > 1)
        {
            WriteString("AllocationSizeMin");
            WriteNumber(stats.AllocationSizeMin);

            WriteString("AllocationSizeMax");
            WriteNumber(stats.AllocationSizeMax);
        }

        if (stats.UnusedRangeCount > 1)
        {
            WriteString("UnusedRangeSizeMin");
            WriteNumber(stats.UnusedRangeSizeMin);

            WriteString("UnusedRangeSizeMax");
            WriteNumber(stats.UnusedRangeSizeMax);
        }

        EndObject();
    }

    private void BeginValue(bool isString)
    {
        if (!m_Stack.empty())
        {
            ref StackItem currItem = ref m_Stack.back();

            if ((currItem.type == COLLECTION_TYPE_OBJECT) && ((currItem.valueCount % 2) == 0))
            {
                D3D12MA_ASSERT(isString);
            }

            if ((currItem.type == COLLECTION_TYPE_OBJECT) && ((currItem.valueCount % 2) == 1))
            {
                m_SB->Add(':');
                m_SB->Add(' ');
            }
            else if (currItem.valueCount > 0)
            {
                m_SB->Add(',');
                m_SB->Add(' ');
                WriteIndent();
            }
            else
            {
                WriteIndent();
            }

            ++currItem.valueCount;
        }
    }

    private void WriteIndent(bool oneLess = false)
    {
        if (!m_Stack.empty() && !m_Stack.back().singleLineMode)
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
    }

    internal partial struct StackItem
    {
        public CollectionType type;

        [NativeTypeName("UINT")]
        public uint valueCount;

        public bool singleLineMode;
    }
}
