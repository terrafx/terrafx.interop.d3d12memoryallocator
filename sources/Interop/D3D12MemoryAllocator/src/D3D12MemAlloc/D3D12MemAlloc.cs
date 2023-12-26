// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;

namespace TerraFX.Interop.DirectX;

public static unsafe partial class D3D12MemAlloc
{
    internal static void D3D12MA_SORT<KeyT, CmpLess>(in KeyT* beg, in KeyT* end, in CmpLess cmp)
        where KeyT : unmanaged
        where CmpLess : unmanaged, D3D12MA_CmpLess<KeyT>
    {
        Span<KeyT> items = new Span<KeyT>(beg, (int)(end - beg));
        items.Sort(cmp);
    }

    private static void D3D12MA_ASSERT_FAIL(string assertion, string fname, uint line, string func)
    {
        throw new Exception($"D3D12MemoryAllocator: assertion failed.\n at \"{fname}\":{line}, \"{func ?? ""}\"\n assertion: \"{assertion}\"");
    }

    internal static void D3D12MA_FAIL(string assertion = "", [CallerFilePath] string fname = "", [CallerLineNumber] uint line = 0, [CallerMemberName] string func = "")
    {
        D3D12MA_ASSERT_FAIL(assertion, fname, line, func);
    }

    internal static void D3D12MA_ASSERT(bool cond, [CallerArgumentExpression("cond")] string assertion = "", [CallerFilePath] string fname = "", [CallerLineNumber] uint line = 0, [CallerMemberName] string func = "")
    {
        if ((D3D12MA_DEBUG_LEVEL > 0) && !cond)
        {
            D3D12MA_ASSERT_FAIL(assertion, fname, line, func);
        }
    }

    internal static void D3D12MA_HEAVY_ASSERT(bool cond, [CallerArgumentExpression("cond")] string assertion = "", [CallerFilePath] string fname = "", [CallerLineNumber] uint line = 0, [CallerMemberName] string func = "")
    {
        if ((D3D12MA_DEBUG_LEVEL > 1) && !cond)
        {
            D3D12MA_ASSERT_FAIL(assertion, fname, line, func);
        }
    }

    [NativeTypeName("UINT")]
    internal const uint D3D12MA_HEAP_TYPE_COUNT = 4;

    [NativeTypeName("UINT")]
    internal const uint D3D12MA_STANDARD_HEAP_TYPE_COUNT = 3;

    [NativeTypeName("UINT")]
    internal const uint D3D12MA_DEFAULT_POOL_MAX_COUNT = 9;

    [NativeTypeName("UINT")]
    internal const uint D3D12MA_NEW_BLOCK_SIZE_SHIFT_MAX = 3;

    internal static readonly string[] D3D12MA_HeapTypeNames = new string[] {
        "DEFAULT",
        "UPLOAD",
        "READBACK",
        "CUSTOM",
    };

    internal static readonly string[] D3D12MA_HeapSubTypeName = new string[] {
        " - Buffers",
        " - Textures",
        " - Textures RT/DS",
    };

    internal const D3D12_HEAP_FLAGS D3D12MA_RESOURCE_CLASS_HEAP_FLAGS = D3D12_HEAP_FLAG_DENY_BUFFERS | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES;

    internal const uint DXGI_MEMORY_SEGMENT_GROUP_LOCAL_COPY = (uint)(DXGI_MEMORY_SEGMENT_GROUP_LOCAL);

    internal const uint DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY = (uint)(DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL);

    internal const uint DXGI_MEMORY_SEGMENT_GROUP_COUNT = (DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL_COPY + 1);

    [UnmanagedCallersOnly]
    internal static void* D3D12MA_DefaultAllocate([NativeTypeName("size_t")] nuint Size, [NativeTypeName("size_t")] nuint Alignment, void* pPrivateData)
    {
        return NativeMemory.AlignedAlloc(Size, Alignment);
    }

    [UnmanagedCallersOnly]
    internal static void D3D12MA_DefaultFree(void* pMemory, void* pPrivateData)
    {
        NativeMemory.AlignedFree(pMemory);
    }

    internal static void* D3D12MA_Malloc([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("size_t")] nuint size, [NativeTypeName("size_t")] nuint alignment)
    {
        void* result = allocs.pAllocate(size, alignment, allocs.pPrivateData);
        D3D12MA_ASSERT(result != null);
        return result;
    }

    internal static void D3D12MA_Free([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, void* memory)
    {
        allocs.pFree(memory, allocs.pPrivateData);
    }

    internal static T* D3D12MA_Allocate<T>([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs)
        where T : unmanaged
    {
        return (T*)(D3D12MA_Malloc(allocs, __sizeof<T>(), __alignof<T>()));
    }

    internal static T* D3D12MA_AllocateArray<T>([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("size_t")] nuint count)
        where T : unmanaged
    {
        return (T*)(D3D12MA_Malloc(allocs, __sizeof<T>() * count, __alignof<T>()));
    }

    internal static T* D3D12MA_NEW<T>([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs)
        where T : unmanaged
    {
        T* p = D3D12MA_Allocate<T>(allocs);

        while (p == null)
        {
            delegate* unmanaged[Cdecl]<void> new_handler = win32_std_get_new_handler();

            if (new_handler == null)
            {
                Environment.Exit(ENOMEM);
            }

            new_handler();
            p = D3D12MA_Allocate<T>(allocs);
        }

        *p = default;
        return p;
    }

    internal static T* D3D12MA_NEW_ARRAY<T>([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, [NativeTypeName("size_t")] nuint count)
        where T : unmanaged
    {
        T* p = D3D12MA_AllocateArray<T>(allocs, count);

        while (p == null)
        {
            delegate* unmanaged[Cdecl]<void> new_handler = win32_std_get_new_handler();

            if (new_handler == null)
            {
                Environment.Exit(ENOMEM);
            }

            new_handler();
            p = D3D12MA_AllocateArray<T>(allocs, count);
        }

        ZeroMemory(p, __sizeof<T>() * count);
        return p;
    }

    internal static void D3D12MA_DELETE<T>([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, T* memory)
        where T : unmanaged, IDisposable
    {
        if (memory != null)
        {
            memory->Dispose();
            D3D12MA_Free(allocs, memory);
        }
    }

    internal static void D3D12MA_DELETE_ARRAY<T>([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, T* memory)
        where T : unmanaged
    {
        if (memory != null)
        {
            D3D12MA_Free(allocs, memory);
        }
    }

    internal static void D3D12MA_DELETE_ARRAY<T>([NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS &")] in D3D12MA_ALLOCATION_CALLBACKS allocs, T* memory, [NativeTypeName("size_t")] nuint count)
        where T : unmanaged, IDisposable
    {
        if (memory != null)
        {
            for (nuint i = count; i-- != 0;)
            {
                memory[i].Dispose();
            }
            D3D12MA_Free(allocs, memory);
        }
    }

    internal static void D3D12MA_SetupAllocationCallbacks([NativeTypeName("D3D12MA::ALLOCATION_CALLBACKS &")] out D3D12MA_ALLOCATION_CALLBACKS outAllocs, [NativeTypeName("const D3D12MA::ALLOCATION_CALLBACKS *")] D3D12MA_ALLOCATION_CALLBACKS* allocationCallbacks)
    {
        if (allocationCallbacks != null)
        {
            outAllocs = *allocationCallbacks;
            D3D12MA_ASSERT((outAllocs.pAllocate != null) && (outAllocs.pFree != null));
        }
        else
        {
            outAllocs.pAllocate = &D3D12MA_DefaultAllocate;
            outAllocs.pFree = &D3D12MA_DefaultFree;
            outAllocs.pPrivateData = null;
        }
    }

    internal static void D3D12MA_SAFE_RELEASE<T>(ref T* ptr)
        where T : unmanaged, IUnknown.Interface
    {
        if (ptr != null)
        {
            _ = ptr->Release();
            ptr = null;
        }
    }

    internal static void D3D12MA_VALIDATE(bool cond, [CallerArgumentExpression(nameof(cond))] string message = "")
    {
        D3D12MA_ASSERT(cond, "Validation failed: " + message);
    }

    internal static uint D3D12MA_MIN([NativeTypeName("const T &")] in uint a, [NativeTypeName("const T &")] in uint b)
    {
        return (a <= b) ? a : b;
    }

    internal static nuint D3D12MA_MIN([NativeTypeName("const T &")] in nuint a, [NativeTypeName("const T &")] in nuint b)
    {
        return (a <= b) ? a : b;
    }

    internal static ulong D3D12MA_MIN([NativeTypeName("const T &")] in ulong a, [NativeTypeName("const T &")] in ulong b)
    {
        return (a <= b) ? a : b;
    }

    internal static uint D3D12MA_MAX([NativeTypeName("const T &")] in uint a, [NativeTypeName("const T &")] in uint b)
    {
        return (a <= b) ? b : a;
    }

    internal static nuint D3D12MA_MAX([NativeTypeName("const T &")] in nuint a, [NativeTypeName("const T &")] in nuint b)
    {
        return (a <= b) ? b : a;
    }

    internal static ulong D3D12MA_MAX([NativeTypeName("const T &")] in ulong a, [NativeTypeName("const T &")] in ulong b)
    {
        return (a <= b) ? b : a;
    }

    internal static void D3D12MA_SWAP<T>(ref T a, ref T b)
        where T : unmanaged
    {
        T tmp = a;
        a = b;
        b = tmp;
    }

    internal static void D3D12MA_SWAP<T>(ref T* a, ref T* b)
        where T : unmanaged
    {
        T* tmp = a;
        a = b;
        b = tmp;
    }

    /// <summary>Scans integer for index of first nonzero bit from the Least Significant Bit (LSB). If mask is 0 then returns byte.MaxValue</summary>
    /// <param name="mask"></param>
    /// <returns></returns>
    [return: NativeTypeName("UINT8")]
    internal static byte D3D12MA_BitScanLSB([NativeTypeName("UINT64")] ulong mask)
    {
        byte pos = 0;
        ulong bit = 1;

        do
        {
            if ((mask & bit) != 0)
            {
                return pos;
            }
            bit <<= 1;
        }
        while (pos++ < 63);

        return byte.MaxValue;
    }

    /// <summary>Scans integer for index of first nonzero bit from the Least Significant Bit (LSB). If mask is 0 then returns byte.MaxValue</summary>
    /// <param name="mask"></param>
    /// <returns></returns>
    [return: NativeTypeName("UINT8")]
    internal static byte D3D12MA_BitScanLSB([NativeTypeName("UINT32")] uint mask)
    {
        byte pos = 0;
        uint bit = 1;

        do
        {
            if ((mask & bit) != 0)
            {
                return pos;
            }
            bit <<= 1;
        }
        while (pos++ < 31);

        return byte.MaxValue;
    }

    /// <summary>Scans integer for index of first nonzero bit from the Most Significant Bit (MSB). If mask is 0 then returns byte.MaxValue</summary>
    /// <param name="mask"></param>
    /// <returns></returns>
    [return: NativeTypeName("UINT8")]
    internal static byte D3D12MA_BitScanMSB([NativeTypeName("UINT64")] ulong mask)
    {
        byte pos = 63;
        ulong bit = 1ul << 63;

        do
        {
            if ((mask & bit) != 0)
            {
                return pos;
            }
            bit >>= 1;
        }
        while (pos-- > 0);

        return byte.MaxValue;
    }

    /// <summary>Scans integer for index of first nonzero bit from the Most Significant Bit (MSB). If mask is 0 then returns byte.MaxValue</summary>
    /// <param name="mask"></param>
    /// <returns></returns>
    [return: NativeTypeName("UINT8")]
    internal static byte D3D12MA_BitScanMSB([NativeTypeName("UINT32")] uint mask)
    {
        byte pos = 31;
        uint bit = 1U << 31;

        do
        {
            if ((mask & bit) != 0)
            {
                return pos;
            }

            bit >>= 1;
        }
        while (pos-- > 0);

        return byte.MaxValue;
    }

    // Returns true if given number is a power of two.
    // T must be unsigned integer number or signed integer but always nonnegative.
    // For 0 returns true.
    internal static bool D3D12MA_IsPow2(uint x)
    {
        return (x & (x - 1)) == 0;
    }

    internal static bool D3D12MA_IsPow2(nuint x)
    {
        return (x & (x - 1)) == 0;
    }

    internal static bool D3D12MA_IsPow2(ulong x)
    {
        return (x & (x - 1)) == 0;
    }

    // Aligns given value up to nearest multiply of align value. For example: D3D12MA_AlignUp(11, 8) = 16.
    internal static uint D3D12MA_AlignUp(uint val, uint alignment)
    {
        D3D12MA_HEAVY_ASSERT(D3D12MA_IsPow2(alignment));
        return (val + alignment - 1) & ~(alignment - 1);
    }

    internal static nuint D3D12MA_AlignUp(nuint val, nuint alignment)
    {
        D3D12MA_HEAVY_ASSERT(D3D12MA_IsPow2(alignment));
        return (val + alignment - 1) & ~(alignment - 1);
    }

    internal static ulong D3D12MA_AlignUp(ulong val, ulong alignment)
    {
        D3D12MA_HEAVY_ASSERT(D3D12MA_IsPow2(alignment));
        return (val + alignment - 1) & ~(alignment - 1);
    }

    // Aligns given value down to nearest multiply of align value. For example: D3D12MA_AlignDown(11, 8) = 8.
    internal static uint D3D12MA_AlignDown(uint val, uint alignment)
    {
        D3D12MA_HEAVY_ASSERT(D3D12MA_IsPow2(alignment));
        return val & ~(alignment - 1);
    }

    internal static nuint D3D12MA_AlignDown(nuint val, nuint alignment)
    {
        D3D12MA_HEAVY_ASSERT(D3D12MA_IsPow2(alignment));
        return val & ~(alignment - 1);
    }

    internal static ulong D3D12MA_AlignDown(ulong val, ulong alignment)
    {
        D3D12MA_HEAVY_ASSERT(D3D12MA_IsPow2(alignment));
        return val & ~(alignment - 1);
    }

    // Division with mathematical rounding to nearest number.
    internal static uint D3D12MA_RoundDiv(uint x, uint y)
    {
        return (x + (y / 2)) / y;
    }

    internal static nuint D3D12MA_RoundDiv(nuint x, nuint y)
    {
        return (x + (y / 2)) / y;
    }

    internal static ulong D3D12MA_RoundDiv(ulong x, ulong y)
    {
        return (x + (y / 2)) / y;
    }

    internal static uint D3D12MA_DivideRoundingUp(uint x, uint y)
    {
        return (x + y - 1) / y;
    }

    internal static nuint D3D12MA_DivideRoundingUp(nuint x, nuint y)
    {
        return (x + y - 1) / y;
    }

    internal static ulong D3D12MA_DivideRoundingUp(ulong x, ulong y)
    {
        return (x + y - 1) / y;
    }

    [return: NativeTypeName("WCHAR")]
    internal static char D3D12MA_HexDigitToChar([NativeTypeName("UINT8")] byte digit)
    {
        if (digit < 10)
        {
            return (char)('0' + digit);
        }
        else
        {
            return (char)('A' + (digit - 10));
        }
    }

    // Performs binary search and returns iterator to first element that is greater or
    // equal to `key`, according to comparison `cmp`.
    //
    // Cmp should return true if first argument is less than second argument.
    //
    // Returned value is the found element, if present in the collection or place where
    // new element with value(key) should be inserted.
    [return: NativeTypeName("IterT")]
    internal static KeyT* D3D12MA_BinaryFindFirstNotLess<KeyT, CmpLess>([NativeTypeName("IterT")] KeyT* beg, [NativeTypeName("IterT")] KeyT* end, [NativeTypeName("const KeyT &")] in KeyT key, [NativeTypeName("const CmpLess &")] in CmpLess cmp)
        where KeyT : unmanaged
        where CmpLess : unmanaged, D3D12MA_CmpLess<KeyT>
    {
        nuint down = 0;
        nuint up = (nuint)(end - beg);

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

    // Performs binary search and returns iterator to an element that is equal to `key`,
    // according to comparison `cmp`.
    //
    // Cmp should return true if first argument is less than second argument.
    //
    // Returned value is the found element, if present in the collection or end if not
    // found.
    [return: NativeTypeName("IterT")]
    internal static KeyT* D3D12MA_BinaryFindSorted<KeyT, CmpLess>([NativeTypeName("const IterT &")] in KeyT* beg, [NativeTypeName("const IterT &")] in KeyT* end, [NativeTypeName("const KeyT &")] in KeyT value, [NativeTypeName("const CmpLess &")] in CmpLess cmp)
        where KeyT : unmanaged
        where CmpLess : unmanaged, D3D12MA_CmpLess<KeyT>
    {
        KeyT* it = D3D12MA_BinaryFindFirstNotLess(beg, end, value, cmp);

        if ((it == end) || (!cmp.Invoke(*it, value) && !cmp.Invoke(value, *it)))
        {
            return it;
        }

        return end;
    }

    [return: NativeTypeName("UINT")]
    internal static uint D3D12MA_HeapTypeToIndex(D3D12_HEAP_TYPE type)
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
                D3D12MA_FAIL();
                return uint.MaxValue;
            }
        }
    }

    internal static D3D12_HEAP_TYPE D3D12MA_IndexToHeapType([NativeTypeName("UINT")] uint heapTypeIndex)
    {
        D3D12MA_ASSERT(heapTypeIndex < 4);

        // D3D12_HEAP_TYPE_DEFAULT starts at 1.
        return (D3D12_HEAP_TYPE)(heapTypeIndex + 1);
    }

    [return: NativeTypeName("UINT64")]
    internal static ulong D3D12MA_HeapFlagsToAlignment(D3D12_HEAP_FLAGS flags, bool denyMsaaTextures)
    {
        // Documentation of D3D12_HEAP_DESC structure says:
        // 
        // - D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT   defined as 64KB.
        // - D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT   defined as 4MB. An
        // application must decide whether the heap will contain multi-sample
        // anti-aliasing (MSAA), in which case, the application must choose [this flag].
        // 
        // https://docs.microsoft.com/en-us/windows/desktop/api/d3d12/ns-d3d12-d3d12_heap_desc

        if (denyMsaaTextures)
        {
            return D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
        }

        D3D12_HEAP_FLAGS denyAllTexturesFlags = D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES;
        bool canContainAnyTextures = (flags & denyAllTexturesFlags) != denyAllTexturesFlags;
        return (uint)(canContainAnyTextures ? D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT : D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT);
    }

    internal static D3D12MA_ResourceClass D3D12MA_HeapFlagsToResourceClass(D3D12_HEAP_FLAGS heapFlags)
    {
        bool allowBuffers = (heapFlags & D3D12_HEAP_FLAG_DENY_BUFFERS) == 0;
        bool allowRtDsTextures = (heapFlags & D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES) == 0;
        bool allowNonRtDsTextures = (heapFlags & D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES) == 0;

        byte allowedGroupCount = (byte)((allowBuffers ? 1 : 0) + (allowRtDsTextures ? 1 : 0) + (allowNonRtDsTextures ? 1 : 0));

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

    internal static bool D3D12MA_IsHeapTypeStandard(D3D12_HEAP_TYPE type)
    {
        return (type == D3D12_HEAP_TYPE_DEFAULT) || (type == D3D12_HEAP_TYPE_UPLOAD) || (type == D3D12_HEAP_TYPE_READBACK);
    }

    internal static D3D12_HEAP_PROPERTIES D3D12MA_StandardHeapTypeToHeapProperties(D3D12_HEAP_TYPE type)
    {
        D3D12MA_ASSERT(D3D12MA_IsHeapTypeStandard(type));

        D3D12_HEAP_PROPERTIES result = new D3D12_HEAP_PROPERTIES {
            Type = type,
        };
        return result;
    }

    internal static bool D3D12MA_IsFormatCompressed(DXGI_FORMAT format)
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

    // Only some formats are supported. For others it returns 0.
    [return: NativeTypeName("UINT")]
    internal static uint D3D12MA_GetBitsPerPixel(DXGI_FORMAT format)
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
    
    internal static D3D12MA_ResourceClass D3D12MA_ResourceDescToResourceClass([NativeTypeName("const D3D12_RESOURCE_DESC_T &")] in D3D12_RESOURCE_DESC resDesc)
    {
        if (resDesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
        {
            return D3D12MA_ResourceClass.Buffer;
        }

        // Else: it's surely a texture.
        bool isRenderTargetOrDepthStencil = (resDesc.Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0;
        return isRenderTargetOrDepthStencil ? D3D12MA_ResourceClass.RT_DS_Texture : D3D12MA_ResourceClass.Non_RT_DS_Texture;
    }

    internal static D3D12MA_ResourceClass D3D12MA_ResourceDescToResourceClass([NativeTypeName("const D3D12_RESOURCE_DESC_T &")] in D3D12_RESOURCE_DESC1 resDesc)
    {
        if (resDesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
        {
            return D3D12MA_ResourceClass.Buffer;
        }

        // Else: it's surely a texture.
        bool isRenderTargetOrDepthStencil = (resDesc.Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0;
        return isRenderTargetOrDepthStencil ? D3D12MA_ResourceClass.RT_DS_Texture : D3D12MA_ResourceClass.Non_RT_DS_Texture;
    }

    // This algorithm is overly conservative.
    internal static bool D3D12MA_CanUseSmallAlignment([NativeTypeName("const D3D12_RESOURCE_DESC_T &")] in D3D12_RESOURCE_DESC resourceDesc)
    {
        if (resourceDesc.Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE2D)
        {
            return false;
        }

        if ((resourceDesc.Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0)
        {
            return false;
        }

        if (resourceDesc.SampleDesc.Count > 1)
        {
            return false;
        }

        if (resourceDesc.DepthOrArraySize != 1)
        {
            return false;
        }

        uint sizeX = (uint)(resourceDesc.Width);
        uint sizeY = resourceDesc.Height;
        uint bitsPerPixel = D3D12MA_GetBitsPerPixel(resourceDesc.Format);

        if (bitsPerPixel == 0)
        {
            return false;
        }

        if (D3D12MA_IsFormatCompressed(resourceDesc.Format))
        {
            sizeX = D3D12MA_DivideRoundingUp(sizeX, 4u);
            sizeY = D3D12MA_DivideRoundingUp(sizeY, 4u);
            bitsPerPixel *= 16;
        }

        uint tileSizeX;
        uint tileSizeY;

        switch (bitsPerPixel)
        {
            case   8:
            {
                tileSizeX = 64;
                tileSizeY = 64;
                break;
            }

            case  16:
            {
                tileSizeX = 64;
                tileSizeY = 32;
                break;
            }

            case  32:
            {
                tileSizeX = 32;
                tileSizeY = 32;
                break;
            }

            case  64:
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

            default:
            {
                return false;
            }
        }

        uint tileCount = D3D12MA_DivideRoundingUp(sizeX, tileSizeX) * D3D12MA_DivideRoundingUp(sizeY, tileSizeY);
        return tileCount <= 16;
    }

    internal static bool D3D12MA_CanUseSmallAlignment([NativeTypeName("const D3D12_RESOURCE_DESC_T &")] in D3D12_RESOURCE_DESC1 resourceDesc)
    {
        if (resourceDesc.Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE2D)
        {
            return false;
        }

        if ((resourceDesc.Flags & (D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL)) != 0)
        {
            return false;
        }

        if (resourceDesc.SampleDesc.Count > 1)
        {
            return false;
        }

        if (resourceDesc.DepthOrArraySize != 1)
        {
            return false;
        }

        uint sizeX = (uint)(resourceDesc.Width);
        uint sizeY = resourceDesc.Height;
        uint bitsPerPixel = D3D12MA_GetBitsPerPixel(resourceDesc.Format);

        if (bitsPerPixel == 0)
        {
            return false;
        }

        if (D3D12MA_IsFormatCompressed(resourceDesc.Format))
        {
            sizeX = D3D12MA_DivideRoundingUp(sizeX, 4u);
            sizeY = D3D12MA_DivideRoundingUp(sizeY, 4u);
            bitsPerPixel *= 16;
        }

        uint tileSizeX;
        uint tileSizeY;

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

            default:
            {
                return false;
            }
        }

        uint tileCount = D3D12MA_DivideRoundingUp(sizeX, tileSizeX) * D3D12MA_DivideRoundingUp(sizeY, tileSizeY);
        return tileCount <= 16;
    }

    internal static bool D3D12MA_ValidateAllocateMemoryParameters([NativeTypeName("const D3D12MA::ALLOCATION_DESC *")] D3D12MA_ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO *")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, D3D12MA_Allocation** ppAllocation)
    {
        return (pAllocDesc != null) && (pAllocInfo != null) && (ppAllocation != null) && ((pAllocInfo->Alignment == 0) || (pAllocInfo->Alignment == D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT) || (pAllocInfo->Alignment == D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT)) && (pAllocInfo->SizeInBytes != 0) && (pAllocInfo->SizeInBytes % (64ul * 1024) == 0);
    }

    internal static void D3D12MA_ClearStatistics([NativeTypeName("D3D12MA::Statistics &")] out D3D12MA_Statistics outStats)
    {
        outStats.BlockCount = 0;
        outStats.AllocationCount = 0;

        outStats.BlockBytes = 0;
        outStats.AllocationBytes = 0;
    }

    internal static void D3D12MA_ClearDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] out D3D12MA_DetailedStatistics outStats)
    {
        D3D12MA_ClearStatistics(out outStats.Stats);
        outStats.UnusedRangeCount = 0;

        outStats.AllocationSizeMin = ulong.MaxValue;
        outStats.AllocationSizeMax = 0;

        outStats.UnusedRangeSizeMin = ulong.MaxValue;
        outStats.UnusedRangeSizeMax = 0;
    }

    internal static void D3D12MA_AddStatistics([NativeTypeName("D3D12MA::Statistics &")] ref D3D12MA_Statistics inoutStats, [NativeTypeName("const D3D12MA::Statistics &")] in D3D12MA_Statistics src)
    {
        inoutStats.BlockCount += src.BlockCount;
        inoutStats.AllocationCount += src.AllocationCount;

        inoutStats.BlockBytes += src.BlockBytes;
        inoutStats.AllocationBytes += src.AllocationBytes;
    }

    internal static void D3D12MA_AddDetailedStatistics([NativeTypeName("D3D12MA::DetailedStatistics &")] ref D3D12MA_DetailedStatistics inoutStats, [NativeTypeName("const D3D12MA::DetailedStatistics &")] in D3D12MA_DetailedStatistics src)
    {
        D3D12MA_AddStatistics(ref inoutStats.Stats, src.Stats);
        inoutStats.UnusedRangeCount += src.UnusedRangeCount;

        inoutStats.AllocationSizeMin = D3D12MA_MIN(inoutStats.AllocationSizeMin, src.AllocationSizeMin);
        inoutStats.AllocationSizeMax = D3D12MA_MAX(inoutStats.AllocationSizeMax, src.AllocationSizeMax);

        inoutStats.UnusedRangeSizeMin = D3D12MA_MIN(inoutStats.UnusedRangeSizeMin, src.UnusedRangeSizeMin);
        inoutStats.UnusedRangeSizeMax = D3D12MA_MAX(inoutStats.UnusedRangeSizeMax, src.UnusedRangeSizeMax);
    }

    internal static void D3D12MA_AddDetailedStatisticsAllocation([NativeTypeName("D3D12MA::DetailedStatistics &")] ref D3D12MA_DetailedStatistics inoutStats, [NativeTypeName("UINT64")] ulong size)
    {
        inoutStats.Stats.AllocationCount++;
        inoutStats.Stats.AllocationBytes += size;

        inoutStats.AllocationSizeMin = D3D12MA_MIN(inoutStats.AllocationSizeMin, size);
        inoutStats.AllocationSizeMax = D3D12MA_MAX(inoutStats.AllocationSizeMax, size);
    }

    internal static void D3D12MA_AddDetailedStatisticsUnusedRange([NativeTypeName("D3D12MA::DetailedStatistics &")] ref D3D12MA_DetailedStatistics inoutStats, [NativeTypeName("UINT64")] ulong size)
    {
        inoutStats.UnusedRangeCount++;
        inoutStats.UnusedRangeSizeMin = D3D12MA_MIN(inoutStats.UnusedRangeSizeMin, size);
        inoutStats.UnusedRangeSizeMax = D3D12MA_MAX(inoutStats.UnusedRangeSizeMax, size);
    }
}
