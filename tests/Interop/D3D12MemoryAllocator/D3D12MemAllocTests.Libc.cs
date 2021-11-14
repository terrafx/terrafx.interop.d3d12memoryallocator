// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.InteropServices;

namespace TerraFX.Interop.DirectX.UnitTests
{
    internal unsafe static partial class D3D12MemAllocTests
    {
        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void* _aligned_malloc([NativeTypeName("size_t")] nuint _Size, [NativeTypeName("size_t")] nuint _Alignment);

        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void _aligned_free(void* _Block);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp([NativeTypeName("void const*")] void* _Buf1, [NativeTypeName("void const*")] void* _Buf2, [NativeTypeName("size_t")] nuint num);
    }
}
