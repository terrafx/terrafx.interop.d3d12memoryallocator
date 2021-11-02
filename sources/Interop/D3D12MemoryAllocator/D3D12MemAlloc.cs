// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemAlloc
    {
        internal const uint UINT32_MAX = uint.MaxValue;

        private static void D3D12MA_ASSERT_FAIL(string assertion, string fname, uint line, string func)
        {
            throw new Exception($"D3D12MemoryAllocator: assertion failed.\n at \"{fname}\":{line}, \"{func ?? ""}\"\n assertion: \"{assertion}\"");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nuint __alignof<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                return 1;
            }

            if ((typeof(T) == typeof(short)) || (typeof(T) == typeof(ushort)) || (typeof(T) == typeof(char)))
            {
                return 2;
            }

            if ((typeof(T) == typeof(int)) || (typeof(T) == typeof(uint)) || (typeof(T) == typeof(float)))
            {
                return 4;
            }

            if ((typeof(T) == typeof(long)) || (typeof(T) == typeof(ulong)) || (typeof(T) == typeof(double)))
            {
                return 4;
            }

            if ((typeof(T) == typeof(nint)) || (typeof(T) == typeof(nuint)))
            {
                return (nuint)sizeof(nint);
            }

            if (typeof(T) == typeof(D3D12MA_JsonWriter.StackItem))
            {
                return 4;
            }

            if ((typeof(T) == typeof(D3D12MA_Allocation)) ||
                (typeof(T) == typeof(D3D12MA_Allocator)) ||
                (typeof(T) == typeof(D3D12MA_BlockVector)) ||
                (typeof(T) == typeof(D3D12MA_NormalBlock)) ||
                (typeof(T) == typeof(D3D12MA_BlockMetadata_Generic)) ||
                (typeof(T) == typeof(D3D12MA_PoolAllocator<D3D12MA_Allocation>.Item)) ||
                (typeof(T) == typeof(D3D12MA_PoolAllocator<D3D12MA_List<D3D12MA_Suballocation>.Item>.Item)) ||
                (typeof(T) == typeof(D3D12MA_VirtualBlock)) ||
                (typeof(T) == typeof(D3D12MA_Pool)))
            {
                return 8;
            }

            if ((typeof(T) == typeof(D3D12MA_Vector<Pointer<D3D12MA_Allocation>>)) ||
                (typeof(T) == typeof(D3D12MA_Vector<Pointer<D3D12MA_Pool>>)) ||
                (typeof(T) == typeof(D3D12MA_PoolAllocator<D3D12MA_Allocation>.ItemBlock)) ||
                (typeof(T) == typeof(D3D12MA_PoolAllocator<D3D12MA_List<D3D12MA_Suballocation>.Item>.ItemBlock)) ||
                (typeof(T) == typeof(D3D12MA_List<D3D12MA_Suballocation>.iterator)) ||
                (typeof(T) == typeof(Pointer<D3D12MA_NormalBlock>)) ||
                (typeof(T) == typeof(Pointer<D3D12MA_Allocation>)) ||
                (typeof(T) == typeof(Pointer<D3D12MA_Pool>)))
            {
                return (nuint)sizeof(void*);
            }

            throw new NotSupportedException("Invalid __alignof<T> type");
        }

        // out of memory
        private const int ENOMEM = 12;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T* TRY_D3D12MA_NEW<T>(D3D12MA_ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            T* p = null;

            while (p == null)
            {
                delegate* unmanaged[Cdecl]<void> h = win32_std_get_new_handler();

                if (h == null)
                {
                    Environment.Exit(ENOMEM);
                }

                h();
                p = Allocate<T>(allocs);
            }

            *p = default;
            return p;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T* TRY_D3D12MA_NEW_ARRAY<T>(D3D12MA_ALLOCATION_CALLBACKS* allocs, nuint count)
            where T : unmanaged
        {
            T* p = null;

            while (p == null)
            {
                delegate* unmanaged[Cdecl]<void> h = win32_std_get_new_handler();

                if (h == null)
                {
                    Environment.Exit(ENOMEM);
                }

                h();
                p = AllocateArray<T>(allocs, count);
            }

            Unsafe.InitBlock(p, 0, (uint)(sizeof(T) * (int)count));
            return p;
        }

        internal static void ZeroMemory(void* dst, [NativeTypeName("size_t")] nuint size)
        {
            _ = memset(dst, 0, size);
        }

        private static uint get_app_context_data(string name, uint defaultValue)
        {
            var data = AppContext.GetData(name);

            if (data is uint value)
            {
                return value;
            }
            else if ((data is string s) && uint.TryParse(s, out var result))
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
            var data = AppContext.GetData(name);

            if (data is ulong value)
            {
                return value;
            }
            else if ((data is string s) && ulong.TryParse(s, out var result))
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
                D3D12MA_MUTEX* pMutex = (D3D12MA_MUTEX*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAlloc), sizeof(D3D12MA_MUTEX));
                D3D12MA_MUTEX._ctor(ref *pMutex);
                return pMutex;
            }
            else
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static D3D12MA_MutexLock D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK()
        {
            return new D3D12MA_MutexLock(g_DebugGlobalMutex, true);
        }
    }
}
