// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.Windows;

using D3D12MA_ATOMIC_UINT32 = TerraFX.Interop.D3D12MA.atomic<uint>;
using D3D12MA_ATOMIC_UINT64 = TerraFX.Interop.D3D12MA.atomic<ulong>;

namespace TerraFX.Interop.D3D12MA
{
    public static unsafe partial class D3D12MemAllocH
    {
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        //
        // Configuration Begin
        //
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        [Conditional("DEBUG")]
        internal static void D3D12MA_ASSERT<T>(T cond)
            where T : unmanaged
            => Debug.Assert(EqualityComparer<T>.Default.Equals(cond, default));

        // Assert that will be called very often, like inside data structures e.g. operator[].
        // Making it non-empty can make program slow.
        [Conditional("DEBUG")]
        internal static void D3D12MA_HEAVY_ASSERT<T>(T expr)
            where T : unmanaged
            => Debug.Assert(EqualityComparer<T>.Default.Equals(expr, default));

        // Minimum margin before and after every allocation, in bytes.
        // Set nonzero for debugging purposes only.
        internal const int D3D12MA_DEBUG_MARGIN = 0;

        /// <summary>
        /// Set this to 1 for debugging purposes only, to enable single mutex protecting all
        /// entry calls to the library.Can be useful for debugging multithreading issues.
        /// </summary>
        internal const int D3D12MA_DEBUG_GLOBAL_MUTEX = 0;

        // Default size of a block allocated as single ID3D12Heap.
        internal const ulong D3D12MA_DEFAULT_BLOCK_SIZE = (256UL * 1024 * 1024);

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        //
        // Configuration End
        //
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////
        // Private globals - CPU memory allocation

        static void* DefaultAllocate(nint Size, nint Alignment, void* _  /*pUserData*/)
        {
            return (void*)Marshal.AllocHGlobal(Size);
        }
        static void DefaultFree(void* pMemory, void* _ /*pUserData*/)
        {
            Marshal.FreeHGlobal((IntPtr)pMemory);
        }

        static void* Malloc(ALLOCATION_CALLBACKS* allocs, nint size, nint alignment)
        {
            void* result = allocs->pAllocate(size, alignment, allocs->pUserData);
            D3D12MA_ASSERT((IntPtr)result);
            return result;
        }
        static void Free(ALLOCATION_CALLBACKS* allocs, void* memory)
        {
            allocs->pFree(memory, allocs->pUserData);
        }

        static T* Allocate<T>(ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, sizeof(T), __alignof<T>());
        }
        static T* AllocateArray<T>(ALLOCATION_CALLBACKS* allocs, nint count)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, sizeof(T) * count, __alignof<T>());
        }

        internal static nint __alignof<T>() where T : unmanaged => 8;

        static T* D3D12MA_NEW<T>(ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            T* p = Allocate<T>(allocs);
            *p = default;
            return p;
        }
        static T* D3D12MA_NEW_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, nint count)
            where T : unmanaged
        {
            T* p = AllocateArray<T>(allocs, count);
            Unsafe.InitBlock(p, 0, (uint)(sizeof(T) * count));
            return p;
        }

        static void D3D12MA_DELETE<T>(ALLOCATION_CALLBACKS* allocs, T* memory)
            where T : unmanaged, IDisposable
        {
            if (memory != null)
            {
                memory->Dispose();
                Free(allocs, memory);
            }
        }
        static void D3D12MA_DELETE_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, T* memory, nint count)
            where T : unmanaged, IDisposable
        {
            if (memory != null)
            {
                for (nint i = count; i > 0; i--)
                {
                    memory[i].Dispose();
                }
                Free(allocs, memory);
            }
        }

        static void SetupAllocationCallbacks(ALLOCATION_CALLBACKS* outAllocs, ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            if (allocationCallbacks is not null)
            {
                *outAllocs = *allocationCallbacks;
                D3D12MA_ASSERT(outAllocs->pAllocate is not null && outAllocs->pFree is not null);
            }
            else
            {
                outAllocs->pAllocate = &DefaultAllocate;
                outAllocs->pFree = &DefaultFree;
                outAllocs->pUserData = null;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Private globals - basic facilities

        static void SAFE_RELEASE<T>(T** ptr)
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

        static bool D3D12MA_VALIDATE(bool cond)
        {
            if (!cond)
                D3D12MA_ASSERT(0);
            return cond;
        }

        internal const uint NEW_BLOCK_SIZE_SHIFT_MAX = 3;

        static T D3D12MA_MIN<T>(T a, T b)
            where T : unmanaged, IComparable<T>
        {
            return a.CompareTo(b) <= 0 ? a : b;
        }

        static T D3D12MA_MAX<T>(T a, T b)
            where T : unmanaged, IComparable<T>
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }

        static void D3D12MA_SWAP<T>(T* a, T* b)
            where T : unmanaged
        {
            T tmp = *a;
            *a = *b;
            *b = tmp;
        }
    }

    internal readonly struct D3D12MA_MUTEX
    {
        public void Lock() => Monitor.Enter(m_Mutex);
        public void Unlock() => Monitor.Exit(m_Mutex);

        public static void Init(out D3D12MA_MUTEX mutex)
        {
            mutex = default;
            Unsafe.AsRef(mutex.m_Mutex) = new();
        }

        private readonly object m_Mutex;
    }

    internal readonly struct D3D12MA_RW_MUTEX
    {
        public void LockRead() => m_Lock.EnterReadLock();
        public void UnlockRead() => m_Lock.ExitReadLock();
        public void LockWrite() => m_Lock.EnterWriteLock();
        public void UnlockWrite() => m_Lock.ExitWriteLock();

        public static void Init(out D3D12MA_RW_MUTEX mutex)
        {
            mutex = default;
            Unsafe.AsRef(mutex.m_Lock) = new();
        }

        private readonly ReaderWriterLockSlim m_Lock;
    }

    // Minimal port of std::atomic<T> for the purposes of this project
    internal struct atomic<T>
        where T : unmanaged
    {
        private T value;

        public void Increment()
        {
            if (typeof(T) == typeof(uint))
                Interlocked.Increment(ref Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(ulong))
                Interlocked.Increment(ref Unsafe.As<T, long>(ref value));
            throw null!;
        }

        public T Load()
        {
            if (typeof(T) == typeof(uint))
                Interlocked.Increment(ref Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(ulong))
                Interlocked.Increment(ref Unsafe.As<T, long>(ref value));
            throw null!;
        }
    }

    public static unsafe partial class D3D12MemAllocH
    {
        /// <summary>
        /// Returns true if given number is a power of two.
        /// T must be unsigned integer number or signed integer but always nonnegative.
        /// For 0 returns true.
        /// </summary>
        internal static bool IsPow2(nint x)
        {
            return (x & (x - 1)) == 0;
        }

        // Aligns given value up to nearest multiply of align value. For example: AlignUp(11, 8) = 16.
        // Use types like UINT, uint64_t as T.
        internal static nint AlignUp(nint val, nint alignment)
        {
            D3D12MA_HEAVY_ASSERT(IsPow2(alignment));
            return (val + alignment - 1) & ~(alignment - 1);
        }
        // Aligns given value down to nearest multiply of align value. For example: AlignUp(11, 8) = 8.
        // Use types like UINT, uint64_t as T.
        internal static nint AlignDown(nint val, nint alignment)
        {
            D3D12MA_HEAVY_ASSERT(IsPow2(alignment));
            return val & ~(alignment - 1);
        }

        // Division with mathematical rounding to nearest number.
        internal static nint RoundDiv(nint x, nint y)
        {
            return (x + (y / (nint)2)) / y;
        }
        internal static nint DivideRoudingUp(nint x, nint y)
        {
            return (x + y - 1) / y;
        }

        // Returns smallest power of 2 greater or equal to v.
        internal static uint NextPow2(uint v)
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
        internal static ulong NextPow2(ulong v)
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

        // Returns largest power of 2 less or equal to v.
        internal static uint PrevPow2(uint v)
        {
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v = v ^ (v >> 1);
            return v;
        }
        internal static ulong PrevPow2(ulong v)
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
            return pStr is null || *pStr == '\0';
        }
    }

    // Helper RAII class to lock a mutex in constructor and unlock it in destructor (at the end of scope).
    internal readonly ref struct MutexLock
    {
        public MutexLock(D3D12MA_MUTEX mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            m_pMutex?.Lock();
        }

        public void Dispose()
        {
            m_pMutex?.Unlock();
        }

        readonly D3D12MA_MUTEX? m_pMutex;
    }

    // Helper RAII class to lock a RW mutex in constructor and unlock it in destructor (at the end of scope), for reading.
    internal readonly unsafe ref struct MutexLockRead
    {
        public MutexLockRead(D3D12MA_RW_MUTEX mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            m_pMutex?.LockRead();
        }

        public void Dispose()
        {
            m_pMutex?.UnlockRead();
        }

        readonly D3D12MA_RW_MUTEX? m_pMutex;
    }

    // Helper RAII class to lock a RW mutex in constructor and unlock it in destructor (at the end of scope), for writing.
    internal readonly unsafe ref struct MutexLockWrite
    {
        public MutexLockWrite(D3D12MA_RW_MUTEX mutex, bool useMutex = true)
        {
            m_pMutex = useMutex ? mutex : null;

            m_pMutex?.LockWrite();
        }

        public void Dispose()
        {
            m_pMutex?.UnlockWrite();
        }

        readonly D3D12MA_RW_MUTEX? m_pMutex;
    }

    public static unsafe partial class D3D12MemAllocH
    {
        // Minimum size of a free suballocation to register it in the free suballocation collection.
        internal const ulong MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER = 16;

        internal interface ICmp<T>
            where T : unmanaged
        {
            bool Invoke(T* lhs, T* rhs);
            bool Invoke(T lhs, T rhs);
        }

        /// <summary>
        /// Performs binary search and returns iterator to first element that is greater or
        /// equal to `key`, according to comparison `cmp`.
        /// <para>Cmp should return true if first argument is less than second argument.</para>
        /// <para>
        /// Returned value is the found element, if present in the collection or place where
        /// new element with value(key) should be inserted.
        /// </para>
        /// </summary>
        internal static KeyT* BinaryFindFirstNotLess<CmpLess, KeyT>(KeyT* beg, KeyT* end, KeyT key, in CmpLess cmp)
            where CmpLess : struct, ICmp<KeyT>
            where KeyT : unmanaged
        {
            nint down = 0, up = (nint)end - (nint)beg;
            while (down < up)
            {
                nint mid = (down + up) / 2;
                if (cmp.Invoke(*(beg + mid), key))
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
        /// Performs binary search and returns iterator to an element that is equal to `key`,
        /// according to comparison `cmp`.
        /// <para>Cmp should return true if first argument is less than second argument.</para>
        /// <para>Returned value is the found element, if present in the collection or end if not
        /// found.</para>
        /// </summary>
        internal static KeyT* BinaryFindSorted<CmpLess, KeyT>(KeyT* beg, KeyT* end, KeyT value, in CmpLess cmp)
            where CmpLess : struct, ICmp<KeyT>
            where KeyT : unmanaged
        {
            KeyT* it = BinaryFindFirstNotLess(beg, end, value, cmp);
            if (it == end ||
                (!cmp.Invoke(*it, value) && !cmp.Invoke(value, *it)))
            {
                return it;
            }
            return end;
        }

        internal readonly struct PointerLess : ICmp<byte>
        {
            public bool Invoke(byte* lhs, byte* rhs)
            {
                return lhs < rhs;
            }

            public bool Invoke(byte lhs, byte rhs)
                => throw new NotImplementedException();
        }

        internal static uint HeapTypeToIndex(D3D12_HEAP_TYPE type)
        {
            switch (type)
            {
                case D3D12_HEAP_TYPE_DEFAULT: return 0;
                case D3D12_HEAP_TYPE_UPLOAD: return 1;
                case D3D12_HEAP_TYPE_READBACK: return 2;
                default: D3D12MA_ASSERT(0); return uint.MaxValue;
            }
        }

        internal static char*[] HeapTypeNames = new[]
        {
            L("DEFAULT"),
            L("UPLOAD"),
            L("READBACK")
        };

        private static char* L(string text)
        {
            char* p = (char*)Marshal.AllocHGlobal(sizeof(char) * text.Length + 1);
            text.AsSpan().CopyTo(new(p, text.Length));
            p[text.Length] = '\0';
            return p;
        }

        // Stat helper functions

        static void AddStatInfo(ref StatInfo dst, ref StatInfo src)
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

        static void PostProcessStatInfo(ref StatInfo statInfo)
        {
            statInfo.AllocationSizeAvg = statInfo.AllocationCount > 0 ?
                statInfo.UsedBytes / statInfo.AllocationCount : 0;
            statInfo.UnusedRangeSizeAvg = statInfo.UnusedRangeCount > 0 ?
                statInfo.UnusedBytes / statInfo.UnusedRangeCount : 0;
        }

        static ulong HeapFlagsToAlignment(D3D12_HEAP_FLAGS flags)
        {
            /*
            Documentation of D3D12_HEAP_DESC structure says:

            - D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT   defined as 64KB.
            - D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT   defined as 4MB. An
            application must decide whether the heap will contain multi-sample
            anti-aliasing (MSAA), in which case, the application must choose [this flag].

            https://docs.microsoft.com/en-us/windows/desktop/api/d3d12/ns-d3d12-d3d12_heap_desc
            */

            const D3D12_HEAP_FLAGS denyAllTexturesFlags =
                D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES;
            bool canContainAnyTextures =
                (flags & denyAllTexturesFlags) != denyAllTexturesFlags;
            return canContainAnyTextures ?
                D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT : D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
        }

        static bool IsFormatCompressed(DXGI_FORMAT format)
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
                    return true;
                default:
                    return false;
            }
        }

        // Only some formats are supported. For others it returns 0.
        static uint GetBitsPerPixel(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT_R32G32B32A32_TYPELESS:
                case DXGI_FORMAT_R32G32B32A32_FLOAT:
                case DXGI_FORMAT_R32G32B32A32_UINT:
                case DXGI_FORMAT_R32G32B32A32_SINT:
                    return 128;
                case DXGI_FORMAT_R32G32B32_TYPELESS:
                case DXGI_FORMAT_R32G32B32_FLOAT:
                case DXGI_FORMAT_R32G32B32_UINT:
                case DXGI_FORMAT_R32G32B32_SINT:
                    return 96;
                case DXGI_FORMAT_R16G16B16A16_TYPELESS:
                case DXGI_FORMAT_R16G16B16A16_FLOAT:
                case DXGI_FORMAT_R16G16B16A16_UNORM:
                case DXGI_FORMAT_R16G16B16A16_UINT:
                case DXGI_FORMAT_R16G16B16A16_SNORM:
                case DXGI_FORMAT_R16G16B16A16_SINT:
                    return 64;
                case DXGI_FORMAT_R32G32_TYPELESS:
                case DXGI_FORMAT_R32G32_FLOAT:
                case DXGI_FORMAT_R32G32_UINT:
                case DXGI_FORMAT_R32G32_SINT:
                    return 64;
                case DXGI_FORMAT_R32G8X24_TYPELESS:
                case DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
                case DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
                case DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
                    return 64;
                case DXGI_FORMAT_R10G10B10A2_TYPELESS:
                case DXGI_FORMAT_R10G10B10A2_UNORM:
                case DXGI_FORMAT_R10G10B10A2_UINT:
                case DXGI_FORMAT_R11G11B10_FLOAT:
                    return 32;
                case DXGI_FORMAT_R8G8B8A8_TYPELESS:
                case DXGI_FORMAT_R8G8B8A8_UNORM:
                case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                case DXGI_FORMAT_R8G8B8A8_UINT:
                case DXGI_FORMAT_R8G8B8A8_SNORM:
                case DXGI_FORMAT_R8G8B8A8_SINT:
                    return 32;
                case DXGI_FORMAT_R16G16_TYPELESS:
                case DXGI_FORMAT_R16G16_FLOAT:
                case DXGI_FORMAT_R16G16_UNORM:
                case DXGI_FORMAT_R16G16_UINT:
                case DXGI_FORMAT_R16G16_SNORM:
                case DXGI_FORMAT_R16G16_SINT:
                    return 32;
                case DXGI_FORMAT_R32_TYPELESS:
                case DXGI_FORMAT_D32_FLOAT:
                case DXGI_FORMAT_R32_FLOAT:
                case DXGI_FORMAT_R32_UINT:
                case DXGI_FORMAT_R32_SINT:
                    return 32;
                case DXGI_FORMAT_R24G8_TYPELESS:
                case DXGI_FORMAT_D24_UNORM_S8_UINT:
                case DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
                case DXGI_FORMAT_X24_TYPELESS_G8_UINT:
                    return 32;
                case DXGI_FORMAT_R8G8_TYPELESS:
                case DXGI_FORMAT_R8G8_UNORM:
                case DXGI_FORMAT_R8G8_UINT:
                case DXGI_FORMAT_R8G8_SNORM:
                case DXGI_FORMAT_R8G8_SINT:
                    return 16;
                case DXGI_FORMAT_R16_TYPELESS:
                case DXGI_FORMAT_R16_FLOAT:
                case DXGI_FORMAT_D16_UNORM:
                case DXGI_FORMAT_R16_UNORM:
                case DXGI_FORMAT_R16_UINT:
                case DXGI_FORMAT_R16_SNORM:
                case DXGI_FORMAT_R16_SINT:
                    return 16;
                case DXGI_FORMAT_R8_TYPELESS:
                case DXGI_FORMAT_R8_UNORM:
                case DXGI_FORMAT_R8_UINT:
                case DXGI_FORMAT_R8_SNORM:
                case DXGI_FORMAT_R8_SINT:
                case DXGI_FORMAT_A8_UNORM:
                    return 8;
                case DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT_BC1_UNORM_SRGB:
                    return 4;
                case DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT_BC2_UNORM_SRGB:
                    return 8;
                case DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT_BC3_UNORM_SRGB:
                    return 8;
                case DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT_BC4_SNORM:
                    return 4;
                case DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT_BC5_SNORM:
                    return 8;
                case DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT_BC6H_SF16:
                    return 8;
                case DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT_BC7_UNORM_SRGB:
                    return 8;
                default:
                    return 0;
            }
        }

        // This algorithm is overly conservative.

    }
}
