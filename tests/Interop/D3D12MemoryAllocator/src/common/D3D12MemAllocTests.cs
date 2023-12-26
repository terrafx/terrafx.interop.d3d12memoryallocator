// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Common.h and Common.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX.UnitTests;

public static unsafe partial class D3D12MemAllocTests
{
    [NativeTypeName("uint32_t")]
    public const uint VENDOR_ID_AMD = 0x1002;

    [NativeTypeName("uint32_t")]
    public const uint VENDOR_ID_NVIDIA = 0x10DE;

    [NativeTypeName("uint32_t")]
    public const uint VENDOR_ID_INTEL = 0x8086;

    public const float PI = 3.14159265358979323846264338327950288419716939937510582f;

    public static ref readonly D3D12_RANGE EMPTY_RANGE
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Environment.Is64BitProcess)
            {
                ReadOnlySpan<byte> data = new byte[] {
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                };

                Debug.Assert(data.Length == Unsafe.SizeOf<D3D12_RANGE>());
                return ref Unsafe.As<byte, D3D12_RANGE>(ref MemoryMarshal.GetReference(data));
            }
            else
            {
                ReadOnlySpan<byte> data = new byte[] {
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                };

                Debug.Assert(data.Length == Unsafe.SizeOf<D3D12_RANGE>());
                return ref Unsafe.As<byte, D3D12_RANGE>(ref MemoryMarshal.GetReference(data));
            }
        }
    }

    public static void CHECK_BOOL(bool expr, [CallerArgumentExpression(nameof(expr))] string __EXPR__ = "", [CallerFilePath] string __FILE__ = "", [CallerLineNumber] int __LINE__ = 0)
    {
        if (!expr)
        {
            Debug.Fail(__EXPR__);
            throw new UnreachableException($"{__FILE__}({__LINE__}): ( {__EXPR__} ) == false");
        }
    }

    public static void CHECK_HR(HRESULT expr, [CallerArgumentExpression(nameof(expr))] string __EXPR__ = "", [CallerFilePath] string __FILE__ = "", [CallerLineNumber] int __LINE__ = 0)
    {
        if (FAILED(expr))
        {
            Debug.Fail(__EXPR__);
            throw new UnreachableException($"{__FILE__}({__LINE__}): FAILED( {__EXPR__} )");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CeilDiv(uint x, uint y)
    {
        return (x + y - 1) / y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint CeilDiv(nuint x, nuint y)
    {
        return (x + y - 1) / y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong CeilDiv(ulong x, ulong y)
    {
        return (x + y - 1) / y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RoundDiv(uint x, uint y)
    {
        return (x + (y / (uint)(2))) / y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint RoundDiv(nuint x, nuint y)
    {
        return (x + (y / (nuint)(2))) / y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RoundDiv(ulong x, ulong y)
    {
        return (x + (y / (ulong)(2))) / y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AlignUp(uint val, uint align)
    {
        return (val + align - 1) / align * align;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint AlignUp(nuint val, nuint align)
    {
        return (val + align - 1) / align * align;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong AlignUp(ulong val, ulong align)
    {
        return (val + align - 1) / align * align;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot([NativeTypeName("const vec3 &")] in vec3 lhs, [NativeTypeName("const vec3 &")] in vec3 rhs)
    {
        return (lhs.x * rhs.x) + (lhs.y * rhs.y) + (lhs.z * rhs.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vec3 Cross([NativeTypeName("const vec3 &")] in vec3 lhs, [NativeTypeName("const vec3 &")] in vec3 rhs)
    {
        return new vec3(
            (lhs.y * rhs.z) - (lhs.z * rhs.y),
            (lhs.z * rhs.x) - (lhs.x * rhs.z),
            (lhs.x * rhs.y) - (lhs.y * rhs.x)
        );
    }

    // void ReadFile(std::vector<char>& out, const wchar_t* fileName);

    internal static void SaveFile(string filePath, void* data, nuint dataSize)
    {
        TextWriter f = new StreamWriter(filePath);
        ReadOnlySpan<char> tmp = new ReadOnlySpan<char>(data, (int)(dataSize / sizeof(char)));

        f.Write(tmp);
        f.Close();
    }

    // void SetConsoleColor(CONSOLE_COLOR color);

    // void PrintMessage(CONSOLE_COLOR color, const char* msg);
    // void PrintMessage(CONSOLE_COLOR color, const wchar_t* msg);

    // inline void Print(const char* msg) { PrintMessage(CONSOLE_COLOR::NORMAL, msg); }
    // inline void Print(const wchar_t* msg) { PrintMessage(CONSOLE_COLOR::NORMAL, msg); }
    // inline void PrintWarning(const char* msg) { PrintMessage(CONSOLE_COLOR::WARNING, msg); }
    // inline void PrintWarning(const wchar_t* msg) { PrintMessage(CONSOLE_COLOR::WARNING, msg); }
    // inline void PrintError(const char* msg) { PrintMessage(CONSOLE_COLOR::ERROR_, msg); }
    // inline void PrintError(const wchar_t* msg) { PrintMessage(CONSOLE_COLOR::ERROR_, msg); }

    // void PrintMessageV(CONSOLE_COLOR color, const char* format, va_list argList);
    // void PrintMessageV(CONSOLE_COLOR color, const wchar_t* format, va_list argList);
    // void PrintMessageF(CONSOLE_COLOR color, const char* format, ...);
    // void PrintMessageF(CONSOLE_COLOR color, const wchar_t* format, ...);
    // void PrintWarningF(const char* format, ...);
    // void PrintWarningF(const wchar_t* format, ...);
    // void PrintErrorF(const char* format, ...);
    // void PrintErrorF(const wchar_t* format, ...);
}
