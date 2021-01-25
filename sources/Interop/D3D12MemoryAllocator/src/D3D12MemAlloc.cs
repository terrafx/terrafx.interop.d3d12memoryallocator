// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;

using UINT = System.UInt32;
using uint64_t = System.UInt64;
using UINT64 = System.UInt64;
using size_t = nuint;

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

        [Conditional("DEBUG")]
        internal static void D3D12MA_ASSERT<T>(T cond)
            where T : unmanaged
            => Debug.Assert(!EqualityComparer<T>.Default.Equals(cond, default));

        [Conditional("DEBUG")]
        internal static void D3D12MA_ASSERT<T>(T* cond)
            where T : unmanaged
            => Debug.Assert(cond != null);

        // Assert that will be called very often, like inside data structures e.g. operator[].
        // Making it non-empty can make program slow.
        [Conditional("DEBUG")]
        internal static void D3D12MA_HEAVY_ASSERT<T>(T expr)
            where T : unmanaged
            => Debug.Assert(!EqualityComparer<T>.Default.Equals(expr, default));

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
            [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            static extern unsafe void* _aligned_malloc(size_t _Size, size_t _Alignment);

            return _aligned_malloc(Size, Alignment);
        }
        internal static void DefaultFree(void* pMemory, void* _ /*pUserData*/)
        {
            [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            static extern unsafe void _aligned_free(void* _Block);

            _aligned_free(pMemory);
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
            return (T*)Malloc(allocs, (size_t)sizeof(T), __alignof<T>());
        }
        internal static T* AllocateArray<T>(ALLOCATION_CALLBACKS* allocs, size_t count)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, (size_t)sizeof(T) * count, __alignof<T>());
        }

        private static size_t __alignof<T>() where T : unmanaged => 8;

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?get_new_handler@std@@YAP6AXXZXZ", ExactSpelling = true)]
        private static extern delegate* unmanaged[Cdecl]<void> win32_std_get_new_handler();

        // out of memory
        private const int ENOMEM = 12;

        internal static T* D3D12MA_NEW<T>(ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            T* p = Allocate<T>(allocs);

            if (p != null)
            {
                *p = default;
                return p;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static T* TRY_D3D12MA_NEW(ALLOCATION_CALLBACKS* allocs)
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

            return TRY_D3D12MA_NEW(allocs);
        }
        internal static T* D3D12MA_NEW_ARRAY<T>(ALLOCATION_CALLBACKS* allocs, size_t count)
            where T : unmanaged
        {
            T* p = AllocateArray<T>(allocs, count);

            if (p != null)
            {
                Unsafe.InitBlock(p, 0, (UINT)(sizeof(T) * (int)count));
                return p;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static T* TRY_D3D12MA_NEW_ARRAY(ALLOCATION_CALLBACKS* allocs, size_t count)
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

                Unsafe.InitBlock(p, 0, (UINT)(sizeof(T) * (int)count));
                return p;
            }

            return TRY_D3D12MA_NEW_ARRAY(allocs, count);
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

        internal static size_t D3D12MA_MIN(size_t a, size_t b)
        {
            return a <= b ? a : b;
        }

        internal static uint64_t D3D12MA_MIN(uint64_t a, uint64_t b)
        {
            return a <= b ? a : b;
        }

        internal static size_t D3D12MA_MAX(size_t a, size_t b)
        {
            return a >= b ? a : b;
        }

        internal static uint64_t D3D12MA_MAX(uint64_t a, uint64_t b)
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
            return (pStr == null) || (*pStr == '\0');
        }

        // Minimum size of a free suballocation to register it in the free suballocation collection.
        internal const uint64_t MIN_FREE_SUBALLOCATION_SIZE_TO_REGISTER = 16;

        internal interface ICmp<T>
            where T : unmanaged
        {
            bool Invoke(T* lhs, T* rhs);
        }

        internal interface ICmp64<T>
            where T : unmanaged
        {
            bool Invoke(T* lhs, UINT64 rhs);
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

        /// <summary>Overload of <see cref="BinaryFindFirstNotLess{CmpLess,KeyT}(KeyT*,KeyT*,KeyT*,in CmpLess)"/> to work around lack of templates.</summary>
        internal static KeyT* BinaryFindFirstNotLess<CmpLess, KeyT>(KeyT* beg, KeyT* end, UINT64 key, in CmpLess cmp)
            where CmpLess : struct, ICmp64<KeyT>
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
                case D3D12_HEAP_TYPE_DEFAULT:
                    return 0;
                case D3D12_HEAP_TYPE_UPLOAD:
                    return 1;
                case D3D12_HEAP_TYPE_READBACK:
                    return 2;
                default:
                    D3D12MA_ASSERT(0);
                    return UINT.MaxValue;
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
            if (resourceDesc.Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE2D)
                return false;
            if ((resourceDesc.Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0)
                return false;
            if (resourceDesc.SampleDesc.Count > 1)
                return false;
            if (resourceDesc.DepthOrArraySize != 1)
                return false;

            UINT sizeX = (UINT)resourceDesc.Width;
            UINT sizeY = resourceDesc.Height;
            UINT bitsPerPixel = GetBitsPerPixel(resourceDesc.Format);
            if (bitsPerPixel == 0)
                return false;

            if (IsFormatCompressed(resourceDesc.Format))
            {
                sizeX = DivideRoudingUp(sizeX / 4, 1u);
                sizeY = DivideRoudingUp(sizeY / 4, 1u);
                bitsPerPixel *= 16;
            }

            UINT tileSizeX = 0, tileSizeY = 0;
            switch (bitsPerPixel)
            {
                case 8:
                    tileSizeX = 64;
                    tileSizeY = 64;
                    break;
                case 16:
                    tileSizeX = 64;
                    tileSizeY = 32;
                    break;
                case 32:
                    tileSizeX = 32;
                    tileSizeY = 32;
                    break;
                case 64:
                    tileSizeX = 32;
                    tileSizeY = 16;
                    break;
                case 128:
                    tileSizeX = 16;
                    tileSizeY = 16;
                    break;
                default:
                    return false;
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

    internal unsafe struct SuballocationItemSizeLess : ICmp<SuballocationList.iterator>, ICmp64<SuballocationList.iterator>
    {
        public bool Invoke(SuballocationList.iterator* lhs, SuballocationList.iterator* rhs)
        {
            return lhs->op_Arrow()->size < rhs->op_Arrow()->size;
        }

        public bool Invoke(SuballocationList.iterator* lhs, UINT64 rhs)
        {
            return lhs->op_Arrow()->size < rhs;
        }
    }
}
