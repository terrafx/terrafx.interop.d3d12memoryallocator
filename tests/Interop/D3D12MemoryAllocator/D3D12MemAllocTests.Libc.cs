// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace TerraFX.Interop.DirectX.UnitTests;

public static unsafe partial class D3D12MemAllocTests
{
    // out of memory
    private const int ENOMEM = 12;

    internal static T* cxx_new<T>()
        where T : unmanaged
    {
        T* p = (T*)(NativeMemory.Alloc(__sizeof<T>()));

        while (p == null)
        {
            delegate* unmanaged[Cdecl]<void> new_handler = win32_std_get_new_handler();

            if (new_handler == null)
            {
                Environment.Exit(ENOMEM);
            }

            new_handler();
            p = (T*)(NativeMemory.Alloc(__sizeof<T>()));
        }

        *p = default;
        return p;
    }

    internal static int fprintf(TextWriter stream, string format)
    {
        stream.Write(format);
        return format.Length;
    }

    internal static int fprintf(TextWriter stream, string format, object arg0)
    {
        return fprintf(stream, string.Format(CultureInfo.InvariantCulture, format, arg0));
    }

    internal static int fprintf(TextWriter stream, string format, object arg0, object arg1)
    {
        return fprintf(stream, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1));
    }

    internal static int fprintf(TextWriter stream, string format, object arg0, object arg1, object arg2)
    {
        return fprintf(stream, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2));
    }

    internal static int fprintf(TextWriter stream, string format, params object[] args)
    {
        return fprintf(stream, string.Format(CultureInfo.InvariantCulture, format, args));
    }

    internal static int fwprintf(TextWriter stream, string format)
    {
        stream.Write(format);
        return format.Length;
    }

    internal static int fwprintf(TextWriter stream, string format, object arg0)
    {
        return fwprintf(stream, string.Format(CultureInfo.InvariantCulture, format, arg0));
    }

    internal static int fwprintf(TextWriter stream, string format, object arg0, object arg1)
    {
        return fwprintf(stream, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1));
    }

    internal static int fwprintf(TextWriter stream, string format, object arg0, object arg1, object arg2)
    {
        return fwprintf(stream, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2));
    }

    internal static int fwprintf(TextWriter stream, string format, params object[] args)
    {
        return fwprintf(stream, string.Format(CultureInfo.InvariantCulture, format, args));
    }

    internal static int memcmp([NativeTypeName("const void *")] void* s1, [NativeTypeName("const void *")] void* s2, [NativeTypeName("size_t")] nuint n)
    {
        ReadOnlySpan<byte> tmp1 = new ReadOnlySpan<byte>(s1, (int)(n));
        ReadOnlySpan<byte> tmp2 = new ReadOnlySpan<byte>(s2, (int)(n));
        return tmp1.SequenceCompareTo(tmp2);
    }

    internal static int printf(string format)
    {
        return fprintf(Console.Out, format);
    }

    internal static int printf(string format, object arg0)
    {
        return printf(string.Format(CultureInfo.InvariantCulture, format, arg0));
    }

    internal static int printf(string format, object arg0, object arg1)
    {
        return printf(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1));
    }

    internal static int printf(string format, object arg0, object arg1, object arg2)
    {
        return printf(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2));
    }

    internal static int printf(string format, params object[] args)
    {
        return printf(string.Format(CultureInfo.InvariantCulture, format, args));
    }

    internal static int swprintf_s(out string s, string format)
    {
        s = format;
        return format.Length;
    }

    internal static int swprintf_s(out string s, string format, object arg0)
    {
        return swprintf_s(out s, string.Format(CultureInfo.InvariantCulture, format, arg0));
    }

    internal static int swprintf_s(out string s, string format, object arg0, object arg1)
    {
        return swprintf_s(out s, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1));
    }

    internal static int swprintf_s(out string s, string format, object arg0, object arg1, object arg2)
    {
        return swprintf_s(out s, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2));
    }

    internal static int swprintf_s(out string s, string format, params object[] args)
    {
        return swprintf_s(out s, string.Format(CultureInfo.InvariantCulture, format, args));
    }

    internal static int wcscmp([NativeTypeName("const wchar_t *")] char* s1, string? s2)
    {
        ReadOnlySpan<char> tmp = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)(s1));
        return tmp.CompareTo(s2, StringComparison.Ordinal);
    }

    [return: NativeTypeName("wchar_t *")]
    internal static char* wcsstr([NativeTypeName("const wchar_t *")] char* s1, string s2)
    {
        ReadOnlySpan<char> tmp = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)(s1));
        int index = tmp.IndexOf(s2, StringComparison.Ordinal);
        return (index != -1) ? (s1 + index) : null;
    }

    internal static int wprintf(string format)
    {
        Console.Write(format);
        return format.Length;
    }

    internal static int wprintf(string format, object arg0)
    {
        return wprintf(string.Format(CultureInfo.InvariantCulture, format, arg0));
    }

    internal static int wprintf(string format, object arg0, object arg1)
    {
        return wprintf(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1));
    }

    internal static int wprintf(string format, object arg0, object arg1, object arg2)
    {
        return wprintf(string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2));
    }

    internal static int wprintf(string format, object[] args)
    {
        return wprintf(string.Format(CultureInfo.InvariantCulture, format, args));
    }

    [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?get_new_handler@std@@YAP6AXXZXZ", ExactSpelling = true)]
    [return: NativeTypeName("std::new_handler")]
    private static extern delegate* unmanaged[Cdecl]<void> win32_std_get_new_handler();
}
