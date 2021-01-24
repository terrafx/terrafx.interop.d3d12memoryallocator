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
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MA.D3D12MemAllocH;

using UINT = System.UInt32;
using uint64_t = System.UInt64;
using UINT64 = System.UInt64;
using size_t = nint;
using BOOL = System.Int32;

using SuballocationList = TerraFX.Interop.D3D12MA.List<TerraFX.Interop.D3D12MA.Suballocation>;
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
        internal const uint64_t D3D12MA_DEFAULT_BLOCK_SIZE = (256UL * 1024 * 1024);

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        //
        // Configuration End
        //
        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////
        // Private globals - CPU memory allocation

        internal static void* DefaultAllocate(size_t Size, size_t Alignment, void* _  /*pUserData*/)
        {
            return (void*)Marshal.AllocHGlobal(Size);
        }
        internal static void DefaultFree(void* pMemory, void* _ /*pUserData*/)
        {
            Marshal.FreeHGlobal((IntPtr)pMemory);
        }

        internal static void* Malloc(ALLOCATION_CALLBACKS* allocs, size_t size, size_t alignment)
        {
            void* result = allocs->pAllocate(size, alignment, allocs->pUserData);
            D3D12MA_ASSERT((IntPtr)result);
            return result;
        }
        internal static void Free(ALLOCATION_CALLBACKS* allocs, void* memory)
        {
            allocs->pFree(memory, allocs->pUserData);
        }

        internal static T* Allocate<T>(ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, sizeof(T), __alignof<T>());
        }
        internal static T* AllocateArray<T>(ALLOCATION_CALLBACKS* allocs, size_t count)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, sizeof(T) * count, __alignof<T>());
        }

        private static size_t __alignof<T>() where T : unmanaged => 8;

        internal static T* D3D12MA_NEW<T>(ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            T* p = Allocate<T>(allocs);
            *p = default;
            return p;
        }
        internal static T* D3D12MA_NEW_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, size_t count)
            where T : unmanaged
        {
            T* p = AllocateArray<T>(allocs, count);
            Unsafe.InitBlock(p, 0, (UINT)(sizeof(T) * count));
            return p;
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
        internal static void D3D12MA_DELETE_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, T* memory, size_t count)
            where T : unmanaged, IDisposable
        {
            if (memory != null)
            {
                for (size_t i = count; i > 0; i--)
                {
                    memory[i].Dispose();
                }
                Free(allocs, memory);
            }
        }

        internal static void SetupAllocationCallbacks(ALLOCATION_CALLBACKS* outAllocs, ALLOCATION_CALLBACKS* allocationCallbacks)
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

        internal static void memcpy(void* dst, void* src, size_t size)
        {
            Buffer.MemoryCopy(src, dst, size, size);
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

        internal static bool D3D12MA_VALIDATE(bool cond)
        {
            if (!cond)
                D3D12MA_ASSERT(0);
            return cond;
        }

        internal const UINT NEW_BLOCK_SIZE_SHIFT_MAX = 3;

        internal static T D3D12MA_MIN<T>(T a, T b)
            where T : unmanaged, IComparable<T>
        {
            return a.CompareTo(b) <= 0 ? a : b;
        }

        internal static T D3D12MA_MAX<T>(T a, T b)
            where T : unmanaged, IComparable<T>
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }

        internal static void D3D12MA_SWAP<T>(T* a, T* b)
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
            if (typeof(T) == typeof(UINT))
                Interlocked.Increment(ref Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(uint64_t))
                Interlocked.Increment(ref Unsafe.As<T, long>(ref value));
            throw null!;
        }

        public T Load()
        {
            if (typeof(T) == typeof(UINT))
                Interlocked.Increment(ref Unsafe.As<T, int>(ref value));
            else if (typeof(T) == typeof(uint64_t))
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
        internal static bool IsPow2(size_t x)
        {
            return (x & (x - 1)) == 0;
        }

        // Aligns given value up to nearest multiply of align value. For example: AlignUp(11, 8) = 16.
        // Use types like UINT, uint64_t as T.
        internal static size_t AlignUp(size_t val, size_t alignment)
        {
            D3D12MA_HEAVY_ASSERT(IsPow2(alignment));
            return (val + alignment - 1) & ~(alignment - 1);
        }
        // Aligns given value down to nearest multiply of align value. For example: AlignUp(11, 8) = 8.
        // Use types like UINT, uint64_t as T.
        internal static size_t AlignDown(size_t val, size_t alignment)
        {
            D3D12MA_HEAVY_ASSERT(IsPow2(alignment));
            return val & ~(alignment - 1);
        }

        // Division with mathematical rounding to nearest number.
        internal static UINT RoundDiv(UINT x, UINT y)
        {
            return (x + (y / (UINT)2)) / y;
        }
        internal static UINT DivideRoudingUp(UINT x, UINT y)
        {
            return (x + y - 1) / y;
        }

        // Returns smallest power of 2 greater or equal to v.
        internal static UINT NextPow2(UINT v)
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
        internal static uint64_t NextPow2(uint64_t v)
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
        internal static UINT PrevPow2(UINT v)
        {
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v = v ^ (v >> 1);
            return v;
        }
        internal static uint64_t PrevPow2(uint64_t v)
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
        internal const uint64_t MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER = 16;

        internal interface ICmp<T>
            where T : unmanaged
        {
            bool Invoke(T* lhs, T* rhs);
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
        internal static KeyT* BinaryFindFirstNotLess<CmpLess, KeyT>(KeyT* beg, KeyT* end, KeyT* key, in CmpLess cmp)
            where CmpLess : struct, ICmp<KeyT>
            where KeyT : unmanaged
        {
            size_t down = 0, up = (size_t)end - (size_t)beg;
            while (down < up)
            {
                size_t mid = (down + up) / 2;
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
        /// Performs binary search and returns iterator to an element that is equal to `key`,
        /// according to comparison `cmp`.
        /// <para>Cmp should return true if first argument is less than second argument.</para>
        /// <para>Returned value is the found element, if present in the collection or end if not
        /// found.</para>
        /// </summary>
        internal static KeyT* BinaryFindSorted<CmpLess, KeyT>(KeyT* beg, KeyT* end, KeyT* value, in CmpLess cmp)
            where CmpLess : struct, ICmp<KeyT>
            where KeyT : unmanaged
        {
            KeyT* it = BinaryFindFirstNotLess(beg, end, value, cmp);
            if (it == end ||
                (!cmp.Invoke(it, value) && !cmp.Invoke(value, it)))
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
        }

        internal static UINT HeapTypeToIndex(D3D12_HEAP_TYPE type)
        {
            switch (type)
            {
                case D3D12_HEAP_TYPE_DEFAULT: return 0;
                case D3D12_HEAP_TYPE_UPLOAD: return 1;
                case D3D12_HEAP_TYPE_READBACK: return 2;
                default: D3D12MA_ASSERT(0); return UINT.MaxValue;
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

        static uint64_t HeapFlagsToAlignment(D3D12_HEAP_FLAGS flags)
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
        static UINT GetBitsPerPixel(DXGI_FORMAT format)
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
        static bool CanUseSmallAlignment(in D3D12_RESOURCE_DESC resourceDesc)
        {
            if(resourceDesc.Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE2D)
                return false;
            if((resourceDesc.Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0)
                return false;
            if(resourceDesc.SampleDesc.Count > 1)
                return false;
            if(resourceDesc.DepthOrArraySize != 1)
                return false;

            UINT sizeX = (UINT)resourceDesc.Width;
            UINT sizeY = resourceDesc.Height;
            UINT bitsPerPixel = GetBitsPerPixel(resourceDesc.Format);
            if(bitsPerPixel == 0)
                return false;

            if(IsFormatCompressed(resourceDesc.Format))
            {
                sizeX = DivideRoudingUp(sizeX / 4, 1u);
                sizeY = DivideRoudingUp(sizeY / 4, 1u);
                bitsPerPixel *= 16;
            }

            UINT tileSizeX = 0, tileSizeY = 0;
            switch(bitsPerPixel)
            {
            case   8: tileSizeX = 64; tileSizeY = 64; break;
            case  16: tileSizeX = 64; tileSizeY = 32; break;
            case  32: tileSizeX = 32; tileSizeY = 32; break;
            case  64: tileSizeX = 32; tileSizeY = 16; break;
            case 128: tileSizeX = 16; tileSizeY = 16; break;
            default: return false;
            }

            UINT tileCount = DivideRoudingUp(sizeX, tileSizeX) * DivideRoudingUp(sizeY, tileSizeY);
            return tileCount <= 16;
        }

        static D3D12_HEAP_FLAGS GetExtraHeapFlagsToIgnore()
        {
            D3D12_HEAP_FLAGS result =
                D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;
            return result;
        }

        static bool IsHeapTypeValid(D3D12_HEAP_TYPE type)
        {
            return type == D3D12_HEAP_TYPE_DEFAULT ||
                type == D3D12_HEAP_TYPE_UPLOAD ||
                type == D3D12_HEAP_TYPE_READBACK;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Private class Vector

    /// <summary>
    /// Dynamically resizing continuous array. Class with interface similar to std::vector.
    /// T must be POD because constructors and destructors are not called and memcpy is
    /// used for these objects.
    /// </summary>
    internal unsafe struct Vector<T> : IDisposable
        where T : unmanaged
    {
        // allocationCallbacks externally owned, must outlive this object.
        public Vector(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_pArray = null;
            m_Count = 0;
            m_Capacity = 0;
        }

        public Vector(size_t count, ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_pArray = count > 0 ? AllocateArray<T>(allocationCallbacks, count) : null;
            m_Count = count;
            m_Capacity = count;
        }

        public Vector(in Vector<T> src)
        {
            m_AllocationCallbacks = src.m_AllocationCallbacks;
            m_pArray = src.m_Count > 0 ? AllocateArray<T>(src.m_AllocationCallbacks, src.m_Count) : null;
            m_Count = src.m_Count;
            m_Capacity = src.m_Count;

            if (m_Count > 0)
            {
                memcpy(m_pArray, src.m_pArray, m_Count * sizeof(T));
            }
        }

        public void Dispose()
        {
            Free(m_AllocationCallbacks, m_pArray);
        }

        public static void Copy(in Vector<T> rhs, ref Vector<T> lhs)
        {
            if (!Unsafe.AreSame(ref Unsafe.AsRef(rhs), ref lhs))
            {
                lhs.resize(rhs.m_Count);
                if (lhs.m_Count != 0)
                {
                    memcpy(lhs.m_pArray, rhs.m_pArray, lhs.m_Count * sizeof(T));
                }
            }
        }

        public bool empty() { return m_Count == 0; }
        public size_t size() { return m_Count; }
        public T* data() { return m_pArray; }

        public T* this[size_t index]
        {
            get
            {
                D3D12MA_HEAVY_ASSERT(index < m_Count);
                return m_pArray + index;
            }
        }

        public T* front()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            return m_pArray;
        }

        public T* back()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            return m_pArray + m_Count - 1;
        }

        public void reserve(size_t newCapacity, bool freeMemory = false)
        {
            newCapacity = D3D12MA_MAX(newCapacity, m_Count);

            if ((newCapacity < m_Capacity) && !freeMemory)
            {
                newCapacity = m_Capacity;
            }

            if (newCapacity != m_Capacity)
            {
                T* newArray = newCapacity > 0 ? AllocateArray<T>(m_AllocationCallbacks, newCapacity) : null;
                if (m_Count != 0)
                {
                    memcpy(newArray, m_pArray, m_Count * sizeof(T));
                }
                Free(m_AllocationCallbacks, m_pArray);
                m_Capacity = newCapacity;
                m_pArray = newArray;
            }
        }

        public void resize(size_t newCount, bool freeMemory = false)
        {
            size_t newCapacity = m_Capacity;
            if (newCount > m_Capacity)
            {
                newCapacity = D3D12MA_MAX(newCount, D3D12MA_MAX(m_Capacity * 3 / 2, (size_t)8));
            }
            else if (freeMemory)
            {
                newCapacity = newCount;
            }

            if (newCapacity != m_Capacity)
            {
                T* newArray = newCapacity > 0 ? AllocateArray<T>(m_AllocationCallbacks, newCapacity) : null;
                size_t elementsToCopy = D3D12MA_MIN(m_Count, newCount);
                if (elementsToCopy != 0)
                {
                    memcpy(newArray, m_pArray, elementsToCopy * sizeof(T));
                }
                Free(m_AllocationCallbacks, m_pArray);
                m_Capacity = newCapacity;
                m_pArray = newArray;
            }

            m_Count = newCount;
        }

        public void clear(bool freeMemory = false)
        {
            resize(0, freeMemory);
        }

        public void insert(size_t index, T* src)
        {
            D3D12MA_HEAVY_ASSERT(index <= m_Count);
            size_t oldCount = size();
            resize(oldCount + 1);
            if(index<oldCount)
            {
                memcpy(m_pArray + (index + 1), m_pArray + index, (oldCount - index) * sizeof(T));
            }
            m_pArray[index] = *src;
        }

        public void remove(size_t index)
        {
            D3D12MA_HEAVY_ASSERT(index < m_Count);
            size_t oldCount = size();
            if (index < oldCount - 1)
            {
                memcpy(m_pArray + index, m_pArray + (index + 1), (oldCount - index - 1) * sizeof(T));
            }
            resize(oldCount - 1);
        }

        public void push_back(T* src)
        {
            size_t newIndex = size();
            resize(newIndex + 1);
            m_pArray[newIndex] = *src;
        }

        public void pop_back()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            resize(size() - 1);
        }

        public void push_front(T* src)
        {
            insert(0, src);
        }

        public void pop_front()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            remove(0);
        }

        public T* begin() { return m_pArray; }
        public T* end() { return m_pArray + m_Count; }

        public size_t InsertSorted<CmpLess>(T* value, in CmpLess cmp)
            where CmpLess : struct, ICmp<T>
        {
            size_t indexToInsert = (size_t)(BinaryFindFirstNotLess(
                m_pArray,
                m_pArray + m_Count,
                value,
                cmp) - m_pArray);
            insert(indexToInsert, value);
            return indexToInsert;
        }

        public bool RemoveSorted<CmpLess>(T* value, in CmpLess cmp)
            where CmpLess : struct, ICmp<T>
        {
            T* it = BinaryFindFirstNotLess(
                m_pArray,
                m_pArray + m_Count,
                value,
                cmp);
            if((it != end()) && !cmp.Invoke(it, value) && !cmp.Invoke(value, it))
            {
                size_t indexToRemove = (size_t)(it - begin());
                remove(indexToRemove);
                return true;
            }
            return false;
        }

        private readonly ALLOCATION_CALLBACKS* m_AllocationCallbacks;
        private T* m_pArray;
        private size_t m_Count;
        private size_t m_Capacity;
    }

    /// <summary>
    /// Allocator for objects of type T using a list of arrays (pools) to speed up
    /// allocation.Number of elements that can be allocated is not bounded because
    /// allocator can create multiple blocks.
    /// T should be POD because constructor and destructor is not called in Alloc or
    /// Free.
    /// </summary>
    internal unsafe partial struct PoolAllocator<T> : IDisposable
        where T : unmanaged, IDisposable
    {
        // allocationCallbacks externally owned, must outlive this object.
        public PoolAllocator(ALLOCATION_CALLBACKS* allocationCallbacks, UINT firstBlockCapacity)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_FirstBlockCapacity = firstBlockCapacity;
            m_ItemBlocks = new(allocationCallbacks);

            D3D12MA_ASSERT(m_FirstBlockCapacity > 1);
        }

        public void Dispose() { Clear(); }
        public partial void Clear();
        public partial T* Alloc();
        public partial T* Alloc<T0>(T0 arg0, delegate*<T0, T> cctor)
            where T0 : unmanaged;
        public partial T* Alloc<T0, T1>(T0 arg0, T1 arg1, delegate*<T0, T1, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged;
        public partial T* Alloc<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2, delegate*<T0, T1, T2, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged;
        public partial void Free(T* ptr);

        [StructLayout(LayoutKind.Explicit)]
        private struct Item : IDisposable
        {
            [FieldOffset(0)] public UINT NextFreeIndex; // UINT32_MAX means end of list.
            [FieldOffset(0)] private T __Value_Data;

            public byte* Value => (byte*)Unsafe.AsPointer(ref __Value_Data);

            public void Dispose() { }
        }

        private struct ItemBlock
        {
            public Item* pItems;
            public UINT Capacity;
            public UINT FirstFreeIndex;
        }

        private readonly ALLOCATION_CALLBACKS* m_AllocationCallbacks;
        private readonly UINT m_FirstBlockCapacity;
        private Vector<ItemBlock> m_ItemBlocks;

        private partial ItemBlock* CreateNewBlock();
    }

    internal unsafe partial struct PoolAllocator<T>
    {
        public partial void Clear()
        {
            for (size_t i = m_ItemBlocks.size(); i > 0; i--)
            {
                D3D12MA_DELETE_ARRAY(m_AllocationCallbacks, m_ItemBlocks[i]->pItems, (size_t)m_ItemBlocks[i]->Capacity);
            }
            m_ItemBlocks.clear(true);
        }

        public partial T* Alloc()
        {
            static T Cctor(void** args, void* f)
            {
                return default;
            }

            return Alloc(null, null, &Cctor);
        }

        public partial T* Alloc<T0>(T0 arg0, delegate*<T0, T> cctor)
            where T0 : unmanaged
        {
            void* args = &arg0;

            static T Cctor(void** args, void* f)
            {
                return ((delegate*<T0, T>)f)(*(T0*)args[0]);
            }

            return Alloc(&args, cctor, &Cctor);
        }

        public partial T* Alloc<T0, T1>(T0 arg0, T1 arg1, delegate*<T0, T1, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged
        {
            void* args = stackalloc void*[2] { &arg0, &arg1 };

            static T Cctor(void** args, void* f)
            {
                return ((delegate*<T0, T1, T>)f)(*(T0*)args[0], *(T1*)args[1]);
            }

            return Alloc(&args, cctor, &Cctor);
        }

        public partial T* Alloc<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2, delegate*<T0, T1, T2, T> cctor)
            where T0 : unmanaged
            where T1 : unmanaged
            where T2 : unmanaged
        {
            void* args = stackalloc void*[3] { &arg0, &arg1, &arg2 };

            static T Cctor(void** args, void* f)
            {
                return ((delegate*<T0, T1, T2, T>)f)(*(T0*)args[0], *(T1*)args[1], *(T2*)args[2]);
            }

            return Alloc(&args, cctor, &Cctor);
        }

        private T* Alloc(void** args, void* f, delegate*<void**, void*, T> cctor)
        {
            for (size_t i = m_ItemBlocks.size(); i > 0; i--)
            {
                ItemBlock* block = m_ItemBlocks[i];
                // This block has some free items: Use first one.
                if (block->FirstFreeIndex != UINT.MaxValue)
                {
                    Item* pItem = &block->pItems[block->FirstFreeIndex];
                    block->FirstFreeIndex = pItem->NextFreeIndex;
                    T* result = (T*)pItem->Value;
                    *result = cctor(args, f); // Explicit constructor call.
                    return result;
                }
            }

            {
                // No block has free item: Create new one and use it.
                ItemBlock* newBlock = CreateNewBlock();
                Item* pItem = &newBlock->pItems[0];
                newBlock->FirstFreeIndex = pItem->NextFreeIndex;
                T* result = (T*)pItem->Value;
                *result = cctor(args, f); // Explicit constructor call.
                return result;
            }
        }

        public partial void Free(T* ptr)
        {
            // Search all memory blocks to find ptr.
            for (size_t i = m_ItemBlocks.size(); i > 0; i--)
            {
                ItemBlock* block = m_ItemBlocks[i];

                Item* pItemPtr;
                memcpy(&pItemPtr, &ptr, sizeof(Item));

                // Check if pItemPtr is in address range of this block.
                if ((pItemPtr >= block->pItems) && (pItemPtr < block->pItems + block->Capacity))
                {
                    ptr->Dispose(); // Explicit destructor call.
                    UINT index = (UINT)(pItemPtr - block->pItems);
                    pItemPtr->NextFreeIndex = block->FirstFreeIndex;
                    block->FirstFreeIndex = index;
                    return;
                }
            }
            D3D12MA_ASSERT(0);
        }

        private partial ItemBlock* CreateNewBlock()
        {
            UINT newBlockCapacity = m_ItemBlocks.empty() ?
                m_FirstBlockCapacity : m_ItemBlocks.back()->Capacity * 3 / 2;

            ItemBlock newBlock = new()
            {
                pItems = D3D12MA_NEW_ARRAY<Item>(m_AllocationCallbacks, (size_t)newBlockCapacity),
                Capacity = newBlockCapacity,
                FirstFreeIndex = 0
            };

            m_ItemBlocks.push_back(&newBlock);

            // Setup singly-linked list of all free items in this block.
            for (UINT i = 0; i < newBlockCapacity - 1; ++i)
            {
                newBlock.pItems[i].NextFreeIndex = i + 1;
            }
            newBlock.pItems[newBlockCapacity - 1].NextFreeIndex = UINT32_MAX;
            return m_ItemBlocks.back();
        }
    }

    internal unsafe partial struct List<T> : IDisposable
        where T : unmanaged
    {
        public struct Item : IDisposable
        {
            public Item* pPrev;
            public Item* pNext;
            public T Value;

            public void Dispose() { }
        }

        // allocationCallbacks externally owned, must outlive this object.
        public List(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            m_AllocationCallbacks = allocationCallbacks;
            m_ItemAllocator = new(allocationCallbacks, 128);
            m_pFront = null;
            m_pBack = null;
            m_Count = 0;
        }

        public partial void Dispose();
        public partial void Clear();

        public size_t GetCount() { return m_Count; }
        public bool IsEmpty() { return m_Count == 0; }
        public Item* Front() { return m_pFront; }
        public Item* Back() { return m_pBack; }

        public partial Item* PushBack();
        public partial Item* PushFront();
        public partial Item* PushBack(T* value);
        public partial Item* PushFront(T* value);
        public partial void PopBack();
        public partial void PopFront();

        // Item can be null - it means PushBack.
        public partial Item* InsertBefore(Item* pItem);
        // Item can be null - it means PushFront.
        public partial Item* InsertAfter(Item* pItem);

        public partial Item* InsertBefore(Item* pItem, T* value);
        public partial Item* InsertAfter(Item* pItem, T* value);

        public partial void Remove(Item* pItem);

#pragma warning disable CS0660, CS0661
        public struct iterator
#pragma warning restore CS0660, CS0661
        {
            public T* op_Arrow()
            {
                D3D12MA_HEAVY_ASSERT(m_pItem != null);
                return &m_pItem->Value;
            }

            public void op_MoveNext()
            {
                D3D12MA_HEAVY_ASSERT(m_pItem != null);
                m_pItem = m_pItem->pNext;
            }

            public void op_MoveBack()
            {
                if (m_pItem != null)
                {
                    m_pItem = m_pItem->pPrev;
                }
                else
                {
                    D3D12MA_HEAVY_ASSERT(!m_pList->IsEmpty());
                    m_pItem = m_pList->Back();
                }
            }

            public static bool operator ==(iterator lhs, iterator rhs)
            {
                D3D12MA_HEAVY_ASSERT(lhs.m_pList == rhs.m_pList);
                return lhs.m_pItem == rhs.m_pItem;
            }

            public static bool operator !=(iterator lhs, iterator rhs)
            {
                D3D12MA_HEAVY_ASSERT(lhs.m_pList == rhs.m_pList);
                return lhs.m_pItem != rhs.m_pItem;
            }

            private List<T>* m_pList;
            internal Item* m_pItem;

            public iterator(List<T>* pList, Item* pItem)
            {
                m_pList = pList;
                m_pItem = pItem;
            }
        }

        public bool empty() { return IsEmpty(); }
        public size_t size() { return GetCount(); }

        public iterator begin() { return new((List<T>*)Unsafe.AsPointer(ref this), Front()); }
        public iterator end() { return new((List<T>*)Unsafe.AsPointer(ref this), null); }

        public void clear() { Clear(); }
        public void push_back(T* value) { PushBack(value); }
        public void erase(iterator it) { Remove(it.m_pItem); }
        public iterator insert(iterator it, T* value) { return new((List<T>*)Unsafe.AsPointer(ref this), InsertBefore(it.m_pItem, value)); }

        private readonly ALLOCATION_CALLBACKS* m_AllocationCallbacks;
        private PoolAllocator<Item> m_ItemAllocator;
        private Item* m_pFront;
        private Item* m_pBack;
        private size_t m_Count;
    }

    internal unsafe partial struct List<T>
    {
        public partial void Dispose()
        {
            // Intentionally not calling Clear, because that would be unnecessary
            // computations to return all items to m_ItemAllocator as free.
        }

        public partial void Clear()
        {
            if (!IsEmpty())
            {
                Item* pItem = m_pBack;
                while (pItem != null)
                {
                    Item* pPrevItem = pItem->pPrev;
                    m_ItemAllocator.Free(pItem);
                    pItem = pPrevItem;
                }
                m_pFront = null;
                m_pBack = null;
                m_Count = 0;
            }
        }

        public partial Item* PushBack()
        {
            Item* pNewItem = m_ItemAllocator.Alloc();
            pNewItem->pNext = null;
            if (IsEmpty())
            {
                pNewItem->pPrev = null;
                m_pFront = pNewItem;
                m_pBack = pNewItem;
                m_Count = 1;
            }
            else
            {
                pNewItem->pPrev = m_pBack;
                m_pBack->pNext = pNewItem;
                m_pBack = pNewItem;
                ++m_Count;
            }
            return pNewItem;
        }

        public partial Item* PushFront()
        {
            Item* pNewItem = m_ItemAllocator.Alloc();
            pNewItem->pPrev = null;
            if (IsEmpty())
            {
                pNewItem->pNext = null;
                m_pFront = pNewItem;
                m_pBack = pNewItem;
                m_Count = 1;
            }
            else
            {
                pNewItem->pNext = m_pFront;
                m_pFront->pPrev = pNewItem;
                m_pFront = pNewItem;
                ++m_Count;
            }
            return pNewItem;
        }

        public partial Item* PushBack(T* value)
        {
            Item* pNewItem = PushBack();
            pNewItem->Value = *value;
            return pNewItem;
        }

        public partial Item* PushFront(T* value)
        {
            Item* pNewItem = PushFront();
            pNewItem->Value = *value;
            return pNewItem;
        }

        public partial void PopBack()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            Item* pBackItem = m_pBack;
            Item* pPrevItem = pBackItem->pPrev;
            if (pPrevItem != null)
            {
                pPrevItem->pNext = null;
            }
            m_pBack = pPrevItem;
            m_ItemAllocator.Free(pBackItem);
            --m_Count;
        }

        public partial void PopFront()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            Item* pFrontItem = m_pFront;
            Item* pNextItem = pFrontItem->pNext;
            if (pNextItem != null)
            {
                pNextItem->pPrev = null;
            }
            m_pFront = pNextItem;
            m_ItemAllocator.Free(pFrontItem);
            --m_Count;
        }

        public partial void Remove(Item* pItem)
        {
            D3D12MA_HEAVY_ASSERT(pItem != null);
            D3D12MA_HEAVY_ASSERT(m_Count > 0);

            if (pItem->pPrev != null)
            {
                pItem->pPrev->pNext = pItem->pNext;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_pFront == pItem);
                m_pFront = pItem->pNext;
            }

            if (pItem->pNext != null)
            {
                pItem->pNext->pPrev = pItem->pPrev;
            }
            else
            {
                D3D12MA_HEAVY_ASSERT(m_pBack == pItem);
                m_pBack = pItem->pPrev;
            }

            m_ItemAllocator.Free(pItem);
            --m_Count;
        }

        public partial Item* InsertBefore(Item* pItem)
        {
            if (pItem != null)
            {
                Item* prevItem = pItem->pPrev;
                Item* newItem = m_ItemAllocator.Alloc();
                newItem->pPrev = prevItem;
                newItem->pNext = pItem;
                pItem->pPrev = newItem;
                if (prevItem != null)
                {
                    prevItem->pNext = newItem;
                }
                else
                {
                    D3D12MA_HEAVY_ASSERT(m_pFront == pItem);
                    m_pFront = newItem;
                }
                ++m_Count;
                return newItem;
            }
            else
            {
                return PushBack();
            }
        }

        public partial Item* InsertAfter(Item* pItem)
        {
            if (pItem != null)
            {
                Item* nextItem = pItem->pNext;
                Item* newItem = m_ItemAllocator.Alloc();
                newItem->pNext = nextItem;
                newItem->pPrev = pItem;
                pItem->pNext = newItem;
                if (nextItem != null)
                {
                    nextItem->pPrev = newItem;
                }
                else
                {
                    D3D12MA_HEAVY_ASSERT(m_pBack == pItem);
                    m_pBack = newItem;
                }
                ++m_Count;
                return newItem;
            }
            else
                return PushFront();
        }

        public partial Item* InsertBefore(Item* pItem, T* value)
        {
            Item* newItem = InsertBefore(pItem);
            newItem->Value = *value;
            return newItem;
        }

        public partial Item* InsertAfter(Item* pItem, T* value)
        {
            Item* newItem = InsertAfter(pItem);
            newItem->Value = *value;
            return newItem;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Private class AllocationObjectAllocator definition

    /// <summary>Thread-safe wrapper over PoolAllocator free list, for allocation of Allocation objects.</summary>
    internal unsafe struct AllocationObjectAllocator
    {
        public AllocationObjectAllocator(ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            D3D12MA_MUTEX.Init(out m_Mutex);
            m_Allocator = new(allocationCallbacks, 1024);
        }

        private D3D12MA_MUTEX m_Mutex;
        private PoolAllocator<Allocation> m_Allocator;
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Private class BlockMetadata and derived classes - declarations

    internal enum SuballocationType
    {
        SUBALLOCATION_TYPE_FREE = 0,
        SUBALLOCATION_TYPE_ALLOCATION = 1,
    };

    /// <summary>
    /// Represents a region of NormalBlock that is either assigned and returned as
    /// allocated memory block or free.
    /// </summary>
    internal unsafe struct Suballocation
    {
        public UINT64 offset;
        public UINT64 size;
        public void* userData;
        public SuballocationType type;
    };

    // Comparator for offsets.
    internal unsafe struct SuballocationOffsetLess : ICmp<Suballocation>
    {
        public bool Invoke(Suballocation* lhs, Suballocation* rhs)
        {
            return lhs->offset < rhs->offset;
        }
    }

    internal unsafe struct SuballocationOffsetGreater : ICmp<Suballocation>
    {
        public bool Invoke(Suballocation* lhs, Suballocation* rhs)
        {
            return lhs->offset > rhs->offset;
        }
    }

    internal unsafe struct SuballocationItemSizeLess : ICmp<SuballocationList.iterator>
    {
        public bool Invoke(SuballocationList.iterator* lhs, SuballocationList.iterator* rhs)
        {
            return lhs->op_Arrow()->size < rhs->op_Arrow()->size;
        }
    }

    /// <summary>Parameters of planned allocation inside a NormalBlock.</summary>
    internal struct AllocationRequest
    {
        public UINT64 offset;
        public UINT64 sumFreeSize; // Sum size of free items that overlap with proposed allocation.
        public UINT64 sumItemSize; // Sum size of items to make lost that overlap with proposed allocation.
        public SuballocationList.iterator item;
        public BOOL zeroInitialized;
    };
}
