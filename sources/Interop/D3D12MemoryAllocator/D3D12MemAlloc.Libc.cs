// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemAlloc
    {
        private static void* _aligned_malloc([NativeTypeName("size_t")] nuint _Size, [NativeTypeName("size_t")] nuint _Alignment)
        {
            return NativeMemory.AlignedAlloc(_Size, _Alignment);
        }

        private static void _aligned_free(void* _Block)
        {
            NativeMemory.AlignedFree(_Block);
        }

        private static void* memset(void* _Dst, int _Val, [NativeTypeName("size_t")] nuint _Size)
        {
            Unsafe.InitBlock(_Dst, (byte)_Val, (uint)_Size);
            return _Dst;
        }

        internal static void* memcpy(void* _Dst, [NativeTypeName("void const*")] void* _Src, [NativeTypeName("size_t")] nuint _Size)
        {
            Unsafe.CopyBlock(_Dst, _Src, (uint)_Size);
            return _Dst;
        }

        internal static nuint wcslen([NativeTypeName("wchar_t const*")] ushort* @_String)
        {
            return (uint)MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)@_String).Length;
        }

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?get_new_handler@std@@YAP6AXXZXZ", ExactSpelling = true)]
        [return: NativeTypeName("std::new_handler")]
        private static extern delegate* unmanaged[Cdecl]<void> win32_std_get_new_handler();
    }
}
