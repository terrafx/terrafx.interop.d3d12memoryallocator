// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Runtime.InteropServices;

namespace TerraFX.Interop.UnitTests
{
    internal unsafe static partial class D3D12MemAllocTests
    {
        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void* _aligned_malloc([NativeTypeName("size_t")] nuint _Size, [NativeTypeName("size_t")] nuint _Alignment);

        [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void _aligned_free(void* _Block);
    }
}
