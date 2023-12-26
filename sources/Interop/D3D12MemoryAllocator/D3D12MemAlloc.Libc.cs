// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TerraFX.Interop.DirectX;

public static unsafe partial class D3D12MemAlloc
{
    internal static void* memset(void* s, int c, [NativeTypeName("size_t")] nuint n)
    {
        Unsafe.InitBlock(s, (byte)(c), (uint)(n));
        return s;
    }

    internal static void* memcpy(void* s1, [NativeTypeName("const void *")] void* s2, [NativeTypeName("size_t")] nuint n)
    {
        Unsafe.CopyBlock(s1, s2, (uint)(n));
        return s1;
    }

    internal static void* memmove(void* s1, [NativeTypeName("const void *")] void* s2, [NativeTypeName("size_t")] nuint n)
    {
        Unsafe.CopyBlock(s1, s2, (uint)(n));
        return s1;
    }

    [return: NativeTypeName("size_t")]
    internal static nuint wcslen([NativeTypeName("const wchar_t *")] char* s)
    {
        return (uint)(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)(s)).Length);
    }

    [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?get_new_handler@std@@YAP6AXXZXZ", ExactSpelling = true)]
    [return: NativeTypeName("std::new_handler")]
    private static extern delegate* unmanaged[Cdecl]<void> win32_std_get_new_handler();
}
