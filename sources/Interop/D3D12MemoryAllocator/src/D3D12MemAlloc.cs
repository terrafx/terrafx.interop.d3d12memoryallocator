// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;

using SuballocationList = TerraFX.Interop.List<TerraFX.Interop.Suballocation>;

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemoryAllocator
    {
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        //
        // Configuration Begin
        //
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void D3D12MA_ASSERT(bool cond, [CallerArgumentExpression("cond")] string assertion = "", [CallerFilePath] string fname = "", [CallerLineNumber] uint line = 0, [CallerMemberName] string func = "")
        {
            if ((D3D12MA_DEBUG_LEVEL > 0) && !cond)
            {
                D3D12MA_ASSERT_FAIL(assertion, fname, line, func);
            }
        }

        // Assert that will be called very often, like inside data structures e.g. operator[].
        // Making it non-empty can make program slow.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void D3D12MA_HEAVY_ASSERT(bool cond, [CallerArgumentExpression("cond")] string assertion = "", [CallerFilePath] string fname = "", [CallerLineNumber] uint line = 0, [CallerMemberName] string func = "")
        {
            if ((D3D12MA_DEBUG_LEVEL > 1) && !cond)
            {
                D3D12MA_ASSERT_FAIL(assertion, fname, line, func);
            }
        }

        private static void D3D12MA_ASSERT_FAIL(string assertion, string fname, uint line, string func)
        {
            throw new Exception($"D3D12MemoryAllocator: assertion failed.\n at \"{fname}\":{line}, \"{func ?? ""}\"\n assertion: \"{assertion}\"");
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
            if (D3D12MA_DEBUG_GLOBAL_MUTEX > 0)
            {
                D3D12MA_MUTEX* pMutex = (D3D12MA_MUTEX*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemoryAllocator), sizeof(D3D12MA_MUTEX));
                D3D12MA_MUTEX.Init(out *pMutex);
                return pMutex;
            }
            else
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static MutexLock D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK()
        {
            return new MutexLock(g_DebugGlobalMutex, true);
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        //
        // Configuration End
        //
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////
        // Private globals - CPU memory allocation

        internal static void* DefaultAllocate([NativeTypeName("size_t")] nuint Size, [NativeTypeName("size_t")] nuint Alignment, void* _  /*pUserData*/)
        {
            return _aligned_malloc(Size, Alignment);
        }

        internal static void DefaultFree(void* pMemory, void* _ /*pUserData*/)
        {
            _aligned_free(pMemory);
        }

        internal static void* Malloc(ALLOCATION_CALLBACKS* allocs, [NativeTypeName("size_t")] nuint size, [NativeTypeName("size_t")] nuint alignment)
        {
            void* result = allocs->pAllocate(size, alignment, allocs->pUserData);
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (result != null));
            return result;
        }

        internal static void Free(ALLOCATION_CALLBACKS* allocs, void* memory)
        {
            allocs->pFree(memory, allocs->pUserData);
        }

        internal static T* Allocate<T>(ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, (nuint)sizeof(T), __alignof<T>());
        }

        internal static T* AllocateArray<T>(ALLOCATION_CALLBACKS* allocs, [NativeTypeName("size_t")] nuint count)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, (nuint)sizeof(T) * count, __alignof<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nuint __alignof<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte)) return 1;
            if (typeof(T) == typeof(short) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(char)) return 2;
            if (typeof(T) == typeof(int) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(float)) return 4;
            if (typeof(T) == typeof(long) ||
                typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(double)) return 4;
            if (typeof(T) == typeof(nint) ||
                typeof(T) == typeof(nuint)) return (nuint)sizeof(nint);
            if (typeof(T) == typeof(Allocation)) return 8;
            if (typeof(T) == typeof(Allocator)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(AllocatorPimpl)) return 8;
            if (typeof(T) == typeof(Vector<Ptr<Allocation>>)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(Vector<Ptr<Pool>>)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(BlockVector)) return 8;
            if (typeof(T) == typeof(NormalBlock)) return 8;
            if (typeof(T) == typeof(BlockMetadata_Generic)) return 8;
            if (typeof(T) == typeof(PoolAllocator_Allocation.Item)) return 8;
            if (typeof(T) == typeof(PoolAllocator_SuballocationListItem.Item)) return 8;
            if (typeof(T) == typeof(PoolAllocator<Allocation>.ItemBlock)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(PoolAllocator<SuballocationList.Item>.ItemBlock)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(SuballocationList.iterator)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(Ptr<NormalBlock>)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(Ptr<Allocation>)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(VirtualBlock)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(VirtualBlockPimpl)) return 8;
            if (typeof(T) == typeof(JsonWriter.StackItem)) return 4;
            if (typeof(T) == typeof(Pool)) return (nuint)sizeof(void*);
            if (typeof(T) == typeof(PoolPimpl)) return 8;
            if (typeof(T) == typeof(Ptr<Pool>)) return (nuint)sizeof(void*);

            throw new ArgumentException("Invalid __alignof<T> type");
        }

        // out of memory
        private const int ENOMEM = 12;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T* TRY_D3D12MA_NEW<T>(ALLOCATION_CALLBACKS* allocs)
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
        private static T* TRY_D3D12MA_NEW_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, nuint count)
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

        internal static T* D3D12MA_NEW<T>(ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            T* p = Allocate<T>(allocs);

            if (p != null)
            {
                *p = default;
                return p;
            }

            return TRY_D3D12MA_NEW<T>(allocs);
        }

        internal static T* D3D12MA_NEW_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, nuint count)
            where T : unmanaged
        {
            T* p = AllocateArray<T>(allocs, count);

            if (p != null)
            {
                Unsafe.InitBlock(p, 0, (uint)(sizeof(T) * (int)count));
                return p;
            }

            return TRY_D3D12MA_NEW_ARRAY<T>(allocs, count);
        }

        internal static void D3D12MA_DELETE<T>(ALLOCATION_CALLBACKS* allocs, T* memory)
            where T : unmanaged, IDisposable
        {
            if (memory != null)
            {
                memory->Dispose();
                Free(allocs, memory);
            }
        }

        internal static void D3D12MA_DELETE<T>(ALLOCATION_CALLBACKS* allocs, ref T memory)
            where T : unmanaged, IDisposable
        {
            T* pMemory = (T*)Unsafe.AsPointer(ref memory);
            if (pMemory != null)
            {
                pMemory->Dispose();
                Free(allocs, pMemory);
            }
        }

        internal static void D3D12MA_DELETE_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, T* memory, [NativeTypeName("size_t")] nuint count)
            where T : unmanaged
        {
            if (memory != null)
            {
                // The loop to call the destructor on all target items has been removed, because it was not actually needed.
                // D3D12MA_DELETE_ARRAY is only ever called on two types: either ushort*, which has no destructor, or
                // List<Suballocation>.Item, which only contains raw values and pointers and has no desstructor either.

                Free(allocs, memory);
            }
        }

        internal static void SetupAllocationCallbacks(ALLOCATION_CALLBACKS* outAllocs, ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            if (allocationCallbacks is not null)
            {
                *outAllocs = *allocationCallbacks;
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((outAllocs->pAllocate != null) && (outAllocs->pFree != null)));
            }
            else
            {
                outAllocs->pAllocate = &DefaultAllocate;
                outAllocs->pFree = &DefaultFree;
                outAllocs->pUserData = null;
            }
        }

        internal static void ZeroMemory(void* dst, [NativeTypeName("size_t")] nuint size)
        {
            memset(dst, 0, size);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Private globals - basic facilities

        internal static void SAFE_RELEASE<T>(T** ptr)
            where T : unmanaged
        {
            if (ptr != null)
            {
                if (typeof(T) == typeof(Allocator))
                    ((Allocator*)*ptr)->Release();
                else
                    ((IUnknown*)*ptr)->Release();
                *ptr = null;
            }
        }

        internal static void SAFE_RELEASE<T>(ref T* ptr)
            where T : unmanaged
        {
            if (ptr != null)
            {
                if (typeof(T) == typeof(Allocator))
                    ((Allocator*)ptr)->Release();
                else
                    ((IUnknown*)ptr)->Release();
                ptr = null;
            }
        }

        internal static bool D3D12MA_VALIDATE(bool cond)
        {
            if (!cond)
            {
                D3D12MA_ASSERT(false);
            }

            return cond;
        }

        internal const uint NEW_BLOCK_SIZE_SHIFT_MAX = 3;

        internal static nuint D3D12MA_MIN(nuint a, nuint b)
        {
            return a <= b ? a : b;
        }

        internal static ulong D3D12MA_MIN(ulong a, ulong b)
        {
            return a <= b ? a : b;
        }

        internal static nuint D3D12MA_MAX(nuint a, nuint b)
        {
            return a >= b ? a : b;
        }

        internal static ulong D3D12MA_MAX(ulong a, ulong b)
        {
            return a >= b ? a : b;
        }

        internal static void D3D12MA_SWAP<T>(T* a, T* b)
            where T : unmanaged
        {
            T tmp = *a;
            *a = *b;
            *b = tmp;
        }

        /// <summary>Returns true if given number is a power of two. T must be unsigned integer number or signed integer but always nonnegative. For 0 returns true.</summary>
        internal static bool IsPow2(nuint x)
        {
            return unchecked(x & (x - 1)) == 0;
        }

        internal static bool IsPow2(ulong x)
        {
            return unchecked(x & (x - 1)) == 0;
        }

        /// <summary>Aligns given value up to nearest multiply of align value. For example: AlignUp(11, 8) = 16.</summary>
        internal static nuint AlignUp(nuint val, nuint alignment)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (IsPow2(alignment)));
            return (val + alignment - 1) & ~(alignment - 1);
        }

        /// <summary>Aligns given value up to nearest multiply of align value. For example: AlignUp(11, 8) = 16.</summary>
        internal static ulong AlignUp(ulong val, ulong alignment)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && (IsPow2(alignment)));
            return (val + alignment - 1) & ~(alignment - 1);
        }

        internal static uint DivideRoudingUp(uint x, uint y)
        {
            return (x + y - 1) / y;
        }

        /// <summary>Returns smallest power of 2 greater or equal to v.</summary>
        [return: NativeTypeName("UINT")]
        internal static uint NextPow2([NativeTypeName("UINT")] uint v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        /// <summary>Returns smallest power of 2 greater or equal to v.</summary>
        [return: NativeTypeName("uint64_t")]
        internal static ulong NextPow2([NativeTypeName("uint64_t")] ulong v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v |= v >> 32;
            v++;
            return v;
        }

        /// <summary>Returns largest power of 2 less or equal to v.</summary>
        [return: NativeTypeName("UINT")]
        internal static uint PrevPow2([NativeTypeName("UINT")] uint v)
        {
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v = v ^ (v >> 1);
            return v;
        }

        /// <summary>Returns largest power of 2 less or equal to v.</summary>
        [return: NativeTypeName("uint64_t")]
        internal static ulong PrevPow2([NativeTypeName("uint64_t")] ulong v)
        {
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v |= v >> 32;
            v = v ^ (v >> 1);
            return v;
        }

        internal static bool StrIsEmpty(byte* pStr)
        {
            return (pStr == null) || (*pStr == '\0');
        }

        internal interface ICmpLess<T>
            where T : unmanaged
        {
            bool Invoke(T* lhs, T* rhs);
        }

        internal interface ICmpLess64<T>
            where T : unmanaged
        {
            bool Invoke(T* lhs, ulong rhs);
        }

        /// <summary>
        /// Performs binary search and returns iterator to first element that is greater or equal to <paramref name="key"/>, according to comparison <paramref name="cmp"/>.
        /// <para><paramref name="cmp"/> should return true if first argument is less than second argument.</para>
        /// <para>Returned value is the found element, if present in the collection or place where new element with value (<paramref name="key"/>) should be inserted.</para>
        /// </summary>
        internal static TKey* BinaryFindFirstNotLess<TCmpLess, TKey>(TKey* beg, TKey* end, TKey* key, in TCmpLess cmp)
            where TCmpLess : struct, ICmpLess<TKey>
            where TKey : unmanaged
        {
            nuint down = 0, up = (nuint)(end - beg);
            while (down < up)
            {
                nuint mid = (down + up) / 2;
                if (cmp.Invoke((beg + mid), key))
                {
                    down = mid + 1;
                }
                else
                {
                    up = mid;
                }
            }

            return beg + down;
        }

        /// <summary>Overload of <see cref="BinaryFindFirstNotLess{TCmpLess,TKey}(TKey*,TKey*,TKey*,in TCmpLess)"/> to work around lack of templates.</summary>
        internal static void** BinaryFindFirstNotLess(void** beg, void** end, void** key, in PointerLess cmp)
        {
            nuint down = 0, up = (nuint)(end - beg);
            while (down < up)
            {
                nuint mid = (down + up) / 2;
                if (cmp.Invoke(*(beg + mid), *key))
                {
                    down = mid + 1;
                }
                else
                {
                    up = mid;
                }
            }

            return beg + down;
        }

        /// <summary>Overload of <see cref="BinaryFindFirstNotLess{TCmpLess,TKey}(TKey*,TKey*,TKey*,in TCmpLess)"/> to work around lack of templates.</summary>
        internal static TKey* BinaryFindFirstNotLess<TCmpLess, TKey>(TKey* beg, TKey* end, ulong key, in TCmpLess cmp)
            where TCmpLess : struct, ICmpLess64<TKey>
            where TKey : unmanaged
        {
            nuint down = 0, up = (nuint)(end - beg);
            while (down < up)
            {
                nuint mid = (down + up) / 2;
                if (cmp.Invoke((beg + mid), key))
                {
                    down = mid + 1;
                }
                else
                {
                    up = mid;
                }
            }

            return beg + down;
        }

        /// <summary>
        /// Performs binary search and returns iterator to an element that is equal to <paramref name="value"/>, according to comparison <paramref name="cmp"/>.
        /// <para><paramref name="cmp"/> should return true if first argument is less than second argument.</para>
        /// <para>Returned value is the found element, if present in the collection or end if not found.</para>
        /// </summary>
        internal static TKey* BinaryFindSorted<TCmpLess, TKey>(TKey* beg, TKey* end, TKey* value, in TCmpLess cmp)
            where TCmpLess : struct, ICmpLess<TKey>
            where TKey : unmanaged
        {
            TKey* it = BinaryFindFirstNotLess(beg, end, value, cmp);
            if (it == end ||
                (!cmp.Invoke(it, value) && !cmp.Invoke(value, it)))
            {
                return it;
            }

            return end;
        }

        internal readonly struct PointerLess
        {
            public bool Invoke(void* lhs, void* rhs)
            {
                return lhs < rhs;
            }
        }

        internal readonly struct PointerLess<T> : ICmpLess<T>
            where T : unmanaged
        {
            public bool Invoke(T* lhs, T* rhs)
            {
                return lhs < rhs;
            }
        }

        [return: NativeTypeName("UINT")]
        internal static uint HeapTypeToIndex(D3D12_HEAP_TYPE type)
        {
            switch (type)
            {
                case D3D12_HEAP_TYPE_DEFAULT: return 0;
                case D3D12_HEAP_TYPE_UPLOAD: return 1;
                case D3D12_HEAP_TYPE_READBACK: return 2;
                default:
                {
                    D3D12MA_ASSERT(false);
                    return UINT_MAX;
                }
            }
        }

        internal static readonly string[] HeapTypeNames = new[]
        {
            "DEFAULT",
            "UPLOAD",
            "READBACK"
        };

        // Stat helper functions

        internal static void AddStatInfo(ref StatInfo dst, ref StatInfo src)
        {
            dst.BlockCount += src.BlockCount;
            dst.AllocationCount += src.AllocationCount;
            dst.UnusedRangeCount += src.UnusedRangeCount;
            dst.UsedBytes += src.UsedBytes;
            dst.UnusedBytes += src.UnusedBytes;
            dst.AllocationSizeMin = D3D12MA_MIN(dst.AllocationSizeMin, src.AllocationSizeMin);
            dst.AllocationSizeMax = D3D12MA_MAX(dst.AllocationSizeMax, src.AllocationSizeMax);
            dst.UnusedRangeSizeMin = D3D12MA_MIN(dst.UnusedRangeSizeMin, src.UnusedRangeSizeMin);
            dst.UnusedRangeSizeMax = D3D12MA_MAX(dst.UnusedRangeSizeMax, src.UnusedRangeSizeMax);
        }

        internal static void PostProcessStatInfo(ref StatInfo statInfo)
        {
            statInfo.AllocationSizeAvg = statInfo.AllocationCount > 0 ?
                statInfo.UsedBytes / statInfo.AllocationCount : 0;
            statInfo.UnusedRangeSizeAvg = statInfo.UnusedRangeCount > 0 ?
                statInfo.UnusedBytes / statInfo.UnusedRangeCount : 0;
        }

        [return: NativeTypeName("UINT64")]
        internal static ulong HeapFlagsToAlignment(D3D12_HEAP_FLAGS flags)
        {
            /*
            Documentation of D3D12_HEAP_DESC structure says:

            - D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT   defined as 64KB.
            - D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT   defined as 4MB. An
            application must decide whether the heap will contain multi-sample
            anti-aliasing (MSAA), in which case, the application must choose [this flag].

            https://docs.microsoft.com/en-us/windows/desktop/api/d3d12/ns-d3d12-d3d12_heap_desc
            */

            const D3D12_HEAP_FLAGS denyAllTexturesFlags = D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES;
            bool canContainAnyTextures = (flags & denyAllTexturesFlags) != denyAllTexturesFlags;
            return canContainAnyTextures
                ? (ulong)D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT
                : (ulong)D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
        }

        internal static bool IsFormatCompressed(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT_BC4_SNORM:
                case DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT_BC7_UNORM_SRGB:
                {
                    return true;
                }

                default:
                {
                    return false;
                }
            }
        }

        /// <summary>Only some formats are supported. For others it returns 0.</summary>
        [return: NativeTypeName("UINT")]
        internal static uint GetBitsPerPixel(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT_R32G32B32A32_TYPELESS:
                case DXGI_FORMAT_R32G32B32A32_FLOAT:
                case DXGI_FORMAT_R32G32B32A32_UINT:
                case DXGI_FORMAT_R32G32B32A32_SINT:
                {
                    return 128;
                }

                case DXGI_FORMAT_R32G32B32_TYPELESS:
                case DXGI_FORMAT_R32G32B32_FLOAT:
                case DXGI_FORMAT_R32G32B32_UINT:
                case DXGI_FORMAT_R32G32B32_SINT:
                {
                    return 96;
                }

                case DXGI_FORMAT_R16G16B16A16_TYPELESS:
                case DXGI_FORMAT_R16G16B16A16_FLOAT:
                case DXGI_FORMAT_R16G16B16A16_UNORM:
                case DXGI_FORMAT_R16G16B16A16_UINT:
                case DXGI_FORMAT_R16G16B16A16_SNORM:
                case DXGI_FORMAT_R16G16B16A16_SINT:
                {
                    return 64;
                }

                case DXGI_FORMAT_R32G32_TYPELESS:
                case DXGI_FORMAT_R32G32_FLOAT:
                case DXGI_FORMAT_R32G32_UINT:
                case DXGI_FORMAT_R32G32_SINT:
                {
                    return 64;
                }

                case DXGI_FORMAT_R32G8X24_TYPELESS:
                case DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
                case DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
                case DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
                {
                    return 64;
                }

                case DXGI_FORMAT_R10G10B10A2_TYPELESS:
                case DXGI_FORMAT_R10G10B10A2_UNORM:
                case DXGI_FORMAT_R10G10B10A2_UINT:
                case DXGI_FORMAT_R11G11B10_FLOAT:
                {
                    return 32;
                }

                case DXGI_FORMAT_R8G8B8A8_TYPELESS:
                case DXGI_FORMAT_R8G8B8A8_UNORM:
                case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                case DXGI_FORMAT_R8G8B8A8_UINT:
                case DXGI_FORMAT_R8G8B8A8_SNORM:
                case DXGI_FORMAT_R8G8B8A8_SINT:
                {
                    return 32;
                }

                case DXGI_FORMAT_R16G16_TYPELESS:
                case DXGI_FORMAT_R16G16_FLOAT:
                case DXGI_FORMAT_R16G16_UNORM:
                case DXGI_FORMAT_R16G16_UINT:
                case DXGI_FORMAT_R16G16_SNORM:
                case DXGI_FORMAT_R16G16_SINT:
                {
                    return 32;
                }

                case DXGI_FORMAT_R32_TYPELESS:
                case DXGI_FORMAT_D32_FLOAT:
                case DXGI_FORMAT_R32_FLOAT:
                case DXGI_FORMAT_R32_UINT:
                case DXGI_FORMAT_R32_SINT:
                {
                    return 32;
                }

                case DXGI_FORMAT_R24G8_TYPELESS:
                case DXGI_FORMAT_D24_UNORM_S8_UINT:
                case DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
                case DXGI_FORMAT_X24_TYPELESS_G8_UINT:
                {
                    return 32;
                }

                case DXGI_FORMAT_R8G8_TYPELESS:
                case DXGI_FORMAT_R8G8_UNORM:
                case DXGI_FORMAT_R8G8_UINT:
                case DXGI_FORMAT_R8G8_SNORM:
                case DXGI_FORMAT_R8G8_SINT:
                {
                    return 16;
                }

                case DXGI_FORMAT_R16_TYPELESS:
                case DXGI_FORMAT_R16_FLOAT:
                case DXGI_FORMAT_D16_UNORM:
                case DXGI_FORMAT_R16_UNORM:
                case DXGI_FORMAT_R16_UINT:
                case DXGI_FORMAT_R16_SNORM:
                case DXGI_FORMAT_R16_SINT:
                {
                    return 16;
                }

                case DXGI_FORMAT_R8_TYPELESS:
                case DXGI_FORMAT_R8_UNORM:
                case DXGI_FORMAT_R8_UINT:
                case DXGI_FORMAT_R8_SNORM:
                case DXGI_FORMAT_R8_SINT:
                case DXGI_FORMAT_A8_UNORM:
                {
                    return 8;
                }

                case DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT_BC1_UNORM_SRGB:
                {
                    return 4;
                }

                case DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT_BC2_UNORM_SRGB:
                {
                    return 8;
                }

                case DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT_BC3_UNORM_SRGB:
                {
                    return 8;
                }

                case DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT_BC4_SNORM:
                {
                    return 4;
                }

                case DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT_BC5_SNORM:
                {
                    return 8;
                }

                case DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT_BC6H_SF16:
                {
                    return 8;
                }

                case DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT_BC7_UNORM_SRGB:
                {
                    return 8;
                }

                default:
                {
                    return 0;
                }
            }
        }

        // This algorithm is overly conservative.
        internal static bool CanUseSmallAlignment(D3D12_RESOURCE_DESC* resourceDesc)
        {
            if (resourceDesc->Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE2D)
            {
                return false;
            }

            if ((resourceDesc->Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0)
            {
                return false;
            }

            if (resourceDesc->SampleDesc.Count > 1)
            {
                return false;
            }

            if (resourceDesc->DepthOrArraySize != 1)
            {
                return false;
            }

            uint sizeX = (uint)resourceDesc->Width;
            uint sizeY = resourceDesc->Height;
            uint bitsPerPixel = GetBitsPerPixel(resourceDesc->Format);

            if (bitsPerPixel == 0)
            {
                return false;
            }

            if (IsFormatCompressed(resourceDesc->Format))
            {
                sizeX = DivideRoudingUp(sizeX / 4, 1u);
                sizeY = DivideRoudingUp(sizeY / 4, 1u);
                bitsPerPixel *= 16;
            }

            uint tileSizeX, tileSizeY;
            switch (bitsPerPixel)
            {
                case 8:
                {
                    tileSizeX = 64;
                    tileSizeY = 64;
                    break;
                }

                case 16:
                {
                    tileSizeX = 64;
                    tileSizeY = 32;
                    break;
                }

                case 32:
                {
                    tileSizeX = 32;
                    tileSizeY = 32;
                    break;
                }

                case 64:
                {
                    tileSizeX = 32;
                    tileSizeY = 16;
                    break;
                }

                case 128:
                {
                    tileSizeX = 16;
                    tileSizeY = 16;
                    break;
                }

                default: return false;
            }

            uint tileCount = DivideRoudingUp(sizeX, tileSizeX) * DivideRoudingUp(sizeY, tileSizeY);
            return tileCount <= 16;
        }

        internal static D3D12_HEAP_FLAGS GetExtraHeapFlagsToIgnore()
        {
            D3D12_HEAP_FLAGS result =
                D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;
            return result;
        }

        internal static bool IsHeapTypeValid(D3D12_HEAP_TYPE type)
        {
            return type == D3D12_HEAP_TYPE_DEFAULT ||
                type == D3D12_HEAP_TYPE_UPLOAD ||
                type == D3D12_HEAP_TYPE_READBACK;
        }

        public static partial int CreateAllocator(ALLOCATOR_DESC* pDesc, Allocator** ppAllocator)
        {
            if ((pDesc == null) || (ppAllocator == null) || (pDesc->pDevice == null) || (pDesc->pAdapter == null) ||
                !((pDesc->PreferredBlockSize == 0) || ((pDesc->PreferredBlockSize >= 16) && (pDesc->PreferredBlockSize < 0x10000000000UL))))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to CreateAllocator."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            ALLOCATION_CALLBACKS allocationCallbacks;
            SetupAllocationCallbacks(&allocationCallbacks, pDesc->pAllocationCallbacks);

            *ppAllocator = D3D12MA_NEW<Allocator>(&allocationCallbacks);
            **ppAllocator = new Allocator(&allocationCallbacks, pDesc);
            HRESULT hr = (*ppAllocator)->m_Pimpl->Init(pDesc);

            if (FAILED(hr))
            {
                D3D12MA_DELETE(&allocationCallbacks, *ppAllocator);
                *ppAllocator = null;
            }

            return hr;
        }

        public static partial int CreateVirtualBlock(VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock)
        {
            if (pDesc == null || ppVirtualBlock == null)
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to CreateVirtualBlock."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            ALLOCATION_CALLBACKS allocationCallbacks;
            SetupAllocationCallbacks(&allocationCallbacks, pDesc->pAllocationCallbacks);

            *ppVirtualBlock = D3D12MA_NEW<VirtualBlock>(&allocationCallbacks);
            **ppVirtualBlock = new VirtualBlock(&allocationCallbacks, pDesc);
            return S_OK;
        }
    }

    // Comparator for offsets.
    internal unsafe struct SuballocationOffsetLess : ICmpLess<Suballocation>
    {
        public bool Invoke(Suballocation* lhs, Suballocation* rhs)
        {
            return lhs->offset < rhs->offset;
        }
    }

    internal unsafe struct SuballocationOffsetGreater : ICmpLess<Suballocation>
    {
        public bool Invoke(Suballocation* lhs, Suballocation* rhs)
        {
            return lhs->offset > rhs->offset;
        }
    }

    internal unsafe struct SuballocationItemSizeLess : ICmpLess<SuballocationList.iterator>, ICmpLess64<SuballocationList.iterator>
    {
        public bool Invoke(SuballocationList.iterator* lhs, SuballocationList.iterator* rhs)
        {
            return lhs->op_Arrow()->size < rhs->op_Arrow()->size;
        }

        public bool Invoke(SuballocationList.iterator* lhs, ulong rhs)
        {
            return lhs->op_Arrow()->size < rhs;
        }
    }
}
