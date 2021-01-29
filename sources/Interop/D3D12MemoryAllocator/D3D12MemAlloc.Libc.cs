// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.InteropServices;

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemoryAllocator
    {
        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void* _aligned_malloc([NativeTypeName("size_t")] nuint _Size, [NativeTypeName("size_t")] nuint _Alignment);

        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void _aligned_free(void* _Block);

        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void* memset(void* _Dst, int _Val, [NativeTypeName("size_t")] nuint _Size);

        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void* memcpy(void* _Dst, [NativeTypeName("void const*")] void* _Src, [NativeTypeName("size_t")] nuint _Size);

        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        internal static extern nuint wcslen([NativeTypeName("wchar_t const*")] ushort* _String);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?get_new_handler@std@@YAP6AXXZXZ", ExactSpelling = true)]
        [return: NativeTypeName("std::new_handler")]
        private static extern delegate* unmanaged[Cdecl]<void> win32_std_get_new_handler();
    }
}
