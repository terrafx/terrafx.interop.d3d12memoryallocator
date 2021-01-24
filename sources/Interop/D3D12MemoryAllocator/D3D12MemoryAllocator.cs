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
using size_t = nint;

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

        void reserve(size_t newCapacity, bool freeMemory = false)
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

        void resize(size_t newCount, bool freeMemory = false)
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

        void clear(bool freeMemory = false)
        {
            resize(0, freeMemory);
        }

        void insert(size_t index, T* src)
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

        void remove(size_t index)
        {
            D3D12MA_HEAVY_ASSERT(index < m_Count);
            size_t oldCount = size();
            if (index < oldCount - 1)
            {
                memcpy(m_pArray + index, m_pArray + (index + 1), (oldCount - index - 1) * sizeof(T));
            }
            resize(oldCount - 1);
        }

        void push_back(T* src)
        {
            size_t newIndex = size();
            resize(newIndex + 1);
            m_pArray[newIndex] = *src;
        }

        void pop_back()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            resize(size() - 1);
        }

        void push_front(T* src)
        {
            insert(0, src);
        }

        void pop_front()
        {
            D3D12MA_HEAVY_ASSERT(m_Count > 0);
            remove(0);
        }

        public T* begin() { return m_pArray; }
        public T* end() { return m_pArray + m_Count; }

        size_t InsertSorted<CmpLess>(T* value, in CmpLess cmp)
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

        bool RemoveSorted<CmpLess>(T* value, in CmpLess cmp)
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
}
