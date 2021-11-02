// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop
{
    public static unsafe partial class D3D12MemAlloc
    {
        internal const uint D3D12MA_STANDARD_HEAP_TYPE_COUNT = 3; // Only DEFAULT, UPLOAD, READBACK.
        internal const uint D3D12MA_DEFAULT_POOL_MAX_COUNT = 9;
        internal const D3D12_HEAP_FLAGS RESOURCE_CLASS_HEAP_FLAGS = D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;

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

        ////////////////////////////////////////////////////////////////////////////////
        // Private globals - CPU memory allocation

        internal static void* DefaultAllocate([NativeTypeName("size_t")] nuint Size, [NativeTypeName("size_t")] nuint Alignment, void* pUserData)
        {
            return _aligned_malloc(Size, Alignment);
        }

        internal static void DefaultFree(void* pMemory, void* pUserData)
        {
            _aligned_free(pMemory);
        }

        internal static void* Malloc([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs, [NativeTypeName("size_t")] nuint size, [NativeTypeName("size_t")] nuint alignment)
        {
            void* result = allocs->pAllocate(size, alignment, allocs->pUserData);
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (result != null));
            return result;
        }

        internal static void Free([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs, void* memory)
        {
            allocs->pFree(memory, allocs->pUserData);
        }

        internal static void Free([NativeTypeName("const ALLOCATION_CALLBACKS&")] ref D3D12MA_ALLOCATION_CALLBACKS allocs, void* memory)
        {
            allocs.pFree(memory, allocs.pUserData);
        }

        internal static T* Allocate<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, (nuint)sizeof(T), __alignof<T>());
        }

        internal static T* AllocateArray<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs, [NativeTypeName("size_t")] nuint count)
            where T : unmanaged
        {
            return (T*)Malloc(allocs, (nuint)sizeof(T) * count, __alignof<T>());
        }

        internal static T* AllocateArray<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] ref D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("size_t")] nuint count)
            where T : unmanaged
        {
            return (T*)Malloc((D3D12MA_ALLOCATION_CALLBACKS*)Unsafe.AsPointer(ref allocs), (nuint)sizeof(T) * count, __alignof<T>());
        }

        internal static T* D3D12MA_NEW<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs)
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

        internal static T* D3D12MA_NEW_ARRAY<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs, nuint count)
            where T : unmanaged
        {
            T* p = AllocateArray<T>(allocs, count);

            if (p != null)
            {
                ZeroMemory(p, (nuint)sizeof(T) * count);
                return p;
            }

            return TRY_D3D12MA_NEW_ARRAY<T>(allocs, count);
        }

        internal static void D3D12MA_DELETE<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs, ref T memory)
            where T : unmanaged, IDisposable
        {
            D3D12MA_DELETE(allocs, (T*)Unsafe.AsPointer(ref memory));
        }

        internal static void D3D12MA_DELETE<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs, T* memory)
            where T : unmanaged, IDisposable
        {
            if (memory != null)
            {
                memory->Dispose();
                Free(allocs, memory);
            }
        }

        internal static void D3D12MA_DELETE_ARRAY<T>([NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocs, T* memory, [NativeTypeName("size_t")] nuint count)
            where T : unmanaged
        {
            if (memory != null)
            {
                Free(allocs, memory);
            }
        }

        internal static void SetupAllocationCallbacks([NativeTypeName("ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* outAllocs, [NativeTypeName("const ALLOCATION_CALLBACKS&")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks)
        {
            if (allocationCallbacks != null)
            {
                *outAllocs = *allocationCallbacks;
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (outAllocs->pAllocate != null) && (outAllocs->pFree != null));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SAFE_RELEASE(ref D3D12MA_Allocator* ptr)
        {
            if (ptr != null)
            {
                _ = ptr->Release();
                ptr = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SAFE_RELEASE<T>(ref T* ptr)
            where T : unmanaged
        {
            if (ptr != null)
            {
                _ = ((IUnknown*)ptr)->Release();
                ptr = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        /// <summary>Returns true if given number is a power of two. T must be unsigned integer number or signed integer but always nonnegative. For 0 returns true.</summary>
        internal static bool IsPow2(ulong x)
        {
            return unchecked(x & (x - 1)) == 0;
        }

        /// <summary>Aligns given value up to nearest multiply of align value. For example: AlignUp(11, 8) = 16.</summary>
        internal static nuint AlignUp(nuint val, nuint alignment)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && IsPow2(alignment));
            return (val + alignment - 1) & ~(alignment - 1);
        }

        /// <summary>Aligns given value up to nearest multiply of align value. For example: AlignUp(11, 8) = 16.</summary>
        internal static ulong AlignUp(ulong val, ulong alignment)
        {
            D3D12MA_HEAVY_ASSERT((D3D12MA_DEBUG_LEVEL > 1) && IsPow2(alignment));
            return (val + alignment - 1) & ~(alignment - 1);
        }

        // TODO: AlignDown
        // TODO: RoundDiv

        internal static uint DivideRoudingUp(uint x, uint y)
        {
            return (x + y - 1) / y;
        }

        /// <summary>
        /// Performs binary search and returns iterator to first element that is greater or equal to <paramref name="key"/>, according to a standard comparison.
        /// <para>Returned value is the found element, if present in the collection or place where new element with value (<paramref name="key"/>) should be inserted.</para>
        /// </summary>
        internal static IterT* BinaryFindFirstNotLess<CmpLess, IterT, KeyT>([NativeTypeName("IterT")] IterT* beg, [NativeTypeName("IterT")] IterT* end, [NativeTypeName("const KeyT&")] in KeyT key, [NativeTypeName("const CmpLess&")] in CmpLess cmp)
            where CmpLess : ICmpLess<IterT, KeyT>
            where IterT : unmanaged
            where KeyT : unmanaged
        {
            nuint down = 0, up = (nuint)(end - beg);

            while (down < up)
            {
                nuint mid = (down + up) / 2;

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
        /// Performs binary search and returns iterator to an element that is equal to <paramref name="value"/>, according to comparison <paramref name="cmp"/>.
        /// <para><paramref name="cmp"/> should return true if first argument is less than second argument.</para>
        /// <para>Returned value is the found element, if present in the collection or end if not found.</para>
        /// </summary>
        internal static IterT* BinaryFindSorted<CmpLess, IterT, KeyT>([NativeTypeName("const IterT&")] in IterT* beg, [NativeTypeName("const IterT&")] in IterT* end, [NativeTypeName("const KeyT&")] in KeyT value, [NativeTypeName("const CmpLess&")] in CmpLess cmp)
            where CmpLess : ICmpLess<IterT, KeyT>, ICmpLess<KeyT, IterT>
            where IterT : unmanaged
            where KeyT : unmanaged
        {
            IterT* it = BinaryFindFirstNotLess(beg, end, in value, in cmp);

            if ((it == end) || (!cmp.Invoke(*it, value) && !cmp.Invoke(value, *it)))
            {
                return it;
            }

            return end;
        }

        [return: NativeTypeName("UINT")]
        internal static uint HeapTypeToIndex(D3D12_HEAP_TYPE type)
        {
            switch (type)
            {
                case D3D12_HEAP_TYPE_DEFAULT:
                {
                    return 0;
                }

                case D3D12_HEAP_TYPE_UPLOAD:
                {
                    return 1;
                }

                case D3D12_HEAP_TYPE_READBACK:
                {
                    return 2;
                }

                case D3D12_HEAP_TYPE_CUSTOM:
                {
                    return 3;
                }

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
            "READBACK",
            "CUSTOM"
        };

        // Stat helper functions

        internal static void AddStatInfo([NativeTypeName("StatInfo&")] ref D3D12MA_StatInfo dst, [NativeTypeName("StatInfo&")] ref D3D12MA_StatInfo src)
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

        internal static void PostProcessStatInfo(ref D3D12MA_StatInfo statInfo)
        {
            statInfo.AllocationSizeAvg = statInfo.AllocationCount > 0 ? statInfo.UsedBytes / statInfo.AllocationCount : 0;
            statInfo.UnusedRangeSizeAvg = statInfo.UnusedRangeCount > 0 ? statInfo.UnusedBytes / statInfo.UnusedRangeCount : 0;
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
            return canContainAnyTextures ? (ulong)D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT : (ulong)D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
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

        internal static bool IsHeapTypeStandard(D3D12_HEAP_TYPE type)
        {
            return type == D3D12_HEAP_TYPE_DEFAULT ||
                type == D3D12_HEAP_TYPE_UPLOAD ||
                type == D3D12_HEAP_TYPE_READBACK;
        }

        internal static D3D12_HEAP_PROPERTIES StandardHeapTypeToHeapProperties(D3D12_HEAP_TYPE type)
        {
            D3D12MA_ASSERT(IsHeapTypeStandard(type));
            D3D12_HEAP_PROPERTIES result = default;
            result.Type = type;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static D3D12MA_ResourceClass ResourceDescToResourceClass([NativeTypeName("const D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC* resDesc)
        {
            if(resDesc->Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return D3D12MA_ResourceClass.Buffer;
            }

            // Else: it's surely a texture.
            bool isRenderTargetOrDepthStencil =
                (resDesc->Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0;
            return isRenderTargetOrDepthStencil ? D3D12MA_ResourceClass.RT_DS_Texture : D3D12MA_ResourceClass.Non_RT_DS_Texture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static D3D12MA_ResourceClass ResourceDescToResourceClass([NativeTypeName("const D3D12_RESOURCE_DESC_T&")] D3D12_RESOURCE_DESC1* resDesc)
        {
            if (resDesc->Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return D3D12MA_ResourceClass.Buffer;
            }

            // Else: it's surely a texture.
            bool isRenderTargetOrDepthStencil =
                (resDesc->Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0;
            return isRenderTargetOrDepthStencil ? D3D12MA_ResourceClass.RT_DS_Texture : D3D12MA_ResourceClass.Non_RT_DS_Texture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static D3D12MA_ResourceClass HeapFlagsToResourceClass(D3D12_HEAP_FLAGS heapFlags)
        {
            bool allowBuffers = (heapFlags & D3D12_HEAP_FLAG_DENY_BUFFERS) == 0;
            bool allowRtDsTextures = (heapFlags & D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES) == 0;
            bool allowNonRtDsTextures = (heapFlags & D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES) == 0;

            int allowedGroupCount = (allowBuffers ? 1 : 0) + (allowRtDsTextures ? 1 : 0) + (allowNonRtDsTextures ? 1 : 0);
            if (allowedGroupCount != 1)
            {
                return D3D12MA_ResourceClass.Unknown;
            }
            if (allowRtDsTextures)
            {
                return D3D12MA_ResourceClass.RT_DS_Texture;
            }
            if (allowNonRtDsTextures)
            {
                return D3D12MA_ResourceClass.Non_RT_DS_Texture;
            }

            return D3D12MA_ResourceClass.Buffer;
        }

        internal static void AddStatInfoToJson(D3D12MA_JsonWriter* json, D3D12MA_StatInfo* statInfo)
        {
            json->BeginObject();
            json->WriteString("Blocks");
            json->WriteNumber(statInfo->BlockCount);
            json->WriteString("Allocations");
            json->WriteNumber(statInfo->AllocationCount);
            json->WriteString("UnusedRanges");
            json->WriteNumber(statInfo->UnusedRangeCount);
            json->WriteString("UsedBytes");
            json->WriteNumber(statInfo->UsedBytes);
            json->WriteString("UnusedBytes");
            json->WriteNumber(statInfo->UnusedBytes);

            json->WriteString("AllocationSize");
            json->BeginObject(true);
            json->WriteString("Min");
            json->WriteNumber(statInfo->AllocationSizeMin);
            json->WriteString("Avg");
            json->WriteNumber(statInfo->AllocationSizeAvg);
            json->WriteString("Max");
            json->WriteNumber(statInfo->AllocationSizeMax);
            json->EndObject();

            json->WriteString("UnusedRangeSize");
            json->BeginObject(true);
            json->WriteString("Min");
            json->WriteNumber(statInfo->UnusedRangeSizeMin);
            json->WriteString("Avg");
            json->WriteNumber(statInfo->UnusedRangeSizeAvg);
            json->WriteString("Max");
            json->WriteNumber(statInfo->UnusedRangeSizeMax);
            json->EndObject();

            json->EndObject();
        }

        internal static readonly string[] heapSubTypeName = new[]
        {
            " + buffer",
            " + texture",
            " + texture RT or DS",
        };

        public static partial int D3D12MA_CreateAllocator(D3D12MA_ALLOCATOR_DESC* pDesc, D3D12MA_Allocator** ppAllocator)
        {
            if ((pDesc == null) || (ppAllocator == null) || (pDesc->pDevice == null) || (pDesc->pAdapter == null) ||
                !((pDesc->PreferredBlockSize == 0) || ((pDesc->PreferredBlockSize >= 16) && (pDesc->PreferredBlockSize < 0x10000000000UL))))
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to CreateAllocator."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks;
            SetupAllocationCallbacks(&allocationCallbacks, pDesc->pAllocationCallbacks);

            var allocator = D3D12MA_NEW<D3D12MA_Allocator>(&allocationCallbacks); 
            D3D12MA_Allocator._ctor(ref *allocator, &allocationCallbacks, pDesc);
            *ppAllocator = allocator;

            HRESULT hr = (*ppAllocator)->Init(pDesc);

            if (FAILED(hr))
            {
                D3D12MA_DELETE(&allocationCallbacks, *ppAllocator);
                *ppAllocator = null;
            }

            return hr;
        }

        public static partial int D3D12MA_CreateVirtualBlock(D3D12MA_VIRTUAL_BLOCK_DESC* pDesc, D3D12MA_VirtualBlock** ppVirtualBlock)
        {
            if (pDesc == null || ppVirtualBlock == null)
            {
                D3D12MA_ASSERT(false); // "Invalid arguments passed to CreateVirtualBlock."
                return E_INVALIDARG;
            }

            using var debugGlobalMutexLock = D3D12MA_DEBUG_GLOBAL_MUTEX_LOCK();

            D3D12MA_ALLOCATION_CALLBACKS allocationCallbacks;
            SetupAllocationCallbacks(&allocationCallbacks, pDesc->pAllocationCallbacks);

            var virtualBlock = D3D12MA_NEW<D3D12MA_VirtualBlock>(&allocationCallbacks);
            D3D12MA_VirtualBlock._ctor(ref *virtualBlock, &allocationCallbacks, pDesc);
            *ppVirtualBlock = virtualBlock;

            return S_OK;
        }
    }
}
