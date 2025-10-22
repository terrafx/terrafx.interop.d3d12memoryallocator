// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
[assembly: DisableRuntimeMarshalling]

namespace TerraFX.Interop.DirectX;

public static unsafe partial class D3D12MemAlloc
{
    // out of memory
    private const int ENOMEM = 12;

    internal static ref readonly Guid IID_NULL
    {
        get
        {
            ReadOnlySpan<byte> data = [
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe nuint __alignof<T>()
        where T : unmanaged
    {
        AlignOf<T> alignof = new AlignOf<T>();
        return (nuint)(nint)(Unsafe.ByteOffset(ref alignof.Origin, ref Unsafe.As<T, byte>(ref alignof.Target)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe uint __sizeof<T>()
        where T : unmanaged
    {
        return (uint)(sizeof(T));
    }

    internal static void ZeroMemory(void* dst, [NativeTypeName("size_t")] nuint size)
    {
        _ = memset(dst, 0, size);
    }

    private static uint get_app_context_data(string name, uint defaultValue)
    {
        object? data = AppContext.GetData(name);

        if (data is uint value)
        {
            return value;
        }
        else if ((data is string s) && uint.TryParse(s, out uint result))
        {
            return result;
        }
        else
        {
            return defaultValue;
        }
    }

    private static ulong get_app_context_data(string name, ulong defaultValue)
    {
        object? data = AppContext.GetData(name);

        if (data is ulong value)
        {
            return value;
        }
        else if ((data is string s) && ulong.TryParse(s, out ulong result))
        {
            return result;
        }
        else
        {
            return defaultValue;
        }
    }

    /// <summary>Creates a new <see cref="D3D12MA_MUTEX"/> when <see cref="D3D12MA_DEBUG_GLOBAL_MUTEX"/> is set, otherwise a <see langword="null"/> one.</summary>
    private static D3D12MA_MUTEX* InitDebugGlobalMutex()
    {
        if (D3D12MA_DEBUG_GLOBAL_MUTEX != 0)
        {
            return D3D12MA_MUTEX.Create();
        }
        else
        {
            return null;
        }
    }

    internal struct AlignOf<T>
        where T : unmanaged
    {
        public byte Origin;

        public T Target;
    }
}
