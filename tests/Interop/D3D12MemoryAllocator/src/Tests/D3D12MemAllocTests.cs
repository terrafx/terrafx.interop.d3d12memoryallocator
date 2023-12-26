// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.h and Tests.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.D3D12_CPU_PAGE_PROPERTY;
using static TerraFX.Interop.DirectX.D3D12_FEATURE;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_MEMORY_POOL;
using static TerraFX.Interop.DirectX.D3D12_PROTECTED_RESOURCE_SESSION_SUPPORT_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_BARRIER_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_BARRIER_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_STATES;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATOR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_DEFRAGMENTATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_DEFRAGMENTATION_MOVE_OPERATION;
using static TerraFX.Interop.DirectX.D3D12MA_POOL_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_VIRTUAL_ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MA_VIRTUAL_BLOCK_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.UnitTests.CONFIG_TYPE;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX.UnitTests;

public static unsafe partial class D3D12MemAllocTests
{
    [NativeTypeName("const char *")]
    internal const string CODE_DESCRIPTION = "D3D12MA Tests";

    [NativeTypeName("UINT64")]
    internal const ulong KILOBYTE = 1024;

    [NativeTypeName("UINT64")]
    internal const ulong MEGABYTE = 1024 * KILOBYTE;

#pragma warning disable CA1802
    internal static readonly CONFIG_TYPE ConfigType = CONFIG_TYPE_AVERAGE;
#pragma warning restore CA1802

    [NativeTypeName("const char *[]")]
    internal static string[] FREE_ORDER_NAMES = {
        "FORWARD",
        "BACKWARD",
        "RANDOM",
    };

    internal const uint TestCommittedResourcesAndJson_count = 4;

    internal static readonly string?[] TestCommittedResourcesAndJson_names = new string?[(int)(TestCommittedResourcesAndJson_count)] {
        "Resource\nFoo\r\nBar",
        "Resource \"'&<>?#@!&-=_+[]{};:,./\\",
        null,
        "",
    };

    [SupportedOSPlatform("windows10.0")]
    public static void Test([NativeTypeName("const TestContext &")] in TestContext ctx, bool benchmark)
    {
        _ = wprintf("TESTS BEGIN\n");

        TestGroupVirtual(ctx, benchmark: false);
        TestGroupBasics(ctx, benchmark: false);
        TestGroupDefragmentation(ctx, benchmark: false);

        _ = wprintf("TESTS END\n");
    }

    internal static void CurrentTimeToStr([NativeTypeName("std::string &")] out string @out)
    {
        @out = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    }

    internal static float ToFloatSeconds(TimeSpan d)
    {
        return (float)(d.TotalSeconds);
    }

    [return: NativeTypeName("const char *")]
    internal static string AlgorithmToStr(D3D12MA_POOL_FLAGS algorithm)
    {
        switch (algorithm)
        {
            case D3D12MA_POOL_FLAG_ALGORITHM_LINEAR:
            {
                return "Linear";
            }

            case 0:
            {
                return "TLSF";
            }

            default:
            {
                D3D12MA_FAIL();
                return "";
            }
        }
    }

    [return: NativeTypeName("const char *")]
    internal static string VirtualAlgorithmToStr(D3D12MA_VIRTUAL_BLOCK_FLAGS algorithm)
    {
        switch (algorithm)
        {
            case D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR:
            {
                return "Linear";
            }

            case 0:
            {
                return "TLSF";
            }

            default:
            {
                D3D12MA_FAIL();
                return "";
            }
        }
    }

    [return: NativeTypeName("const wchar_t *")]
    internal static string DefragmentationAlgorithmToStr([NativeTypeName("UINT32")] uint algorithm)
    {
        switch (algorithm)
        {
            case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED):
            {
                return "Balanced";
            }

            case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST):
            {
                return "Fast";
            }

            case (uint)(D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL):
            {
                return "Full";
            }

            case 0:
            {
                return "Default";
            }

            default:
            {
                D3D12MA_FAIL();
                return "";
            }
        }
    }

    internal static void FillResourceDescForBuffer([NativeTypeName("D3D12_RESOURCE_DESC_T &")] out D3D12_RESOURCE_DESC outResourceDesc, [NativeTypeName("UINT64")] ulong size)
    {
        outResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = size,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Flags = D3D12_RESOURCE_FLAG_NONE,
        };
    }

    internal static void FillResourceDescForBuffer([NativeTypeName("D3D12_RESOURCE_DESC_T &")] out D3D12_RESOURCE_DESC1 outResourceDesc, [NativeTypeName("UINT64")] ulong size)
    {
        outResourceDesc = new D3D12_RESOURCE_DESC1 {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = size,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            Flags = D3D12_RESOURCE_FLAG_NONE,
        };
    }

    internal static void FillData(void* outPtr, [NativeTypeName("const UINT64")] ulong sizeInBytes, [NativeTypeName("UINT")] uint seed)
    {
        uint* outValues = (uint*)(outPtr);
        ulong sizeInValues = sizeInBytes / sizeof(uint);
        uint value = seed;

        for (uint i = 0; i < sizeInValues; ++i)
        {
            outValues[i] = value++;
        }
    }

    internal static void FillAllocationsData([NativeTypeName("const ComPtr<D3D12MA::Allocation> *")] ComPtr<D3D12MA_Allocation>* allocs, [NativeTypeName("size_t")] nuint allocCount, [NativeTypeName("UINT")] uint seed)
    {
        for (ComPtr<D3D12MA_Allocation>* alloc = allocs; alloc < (allocs + allocCount); alloc++)
        {
            D3D12_RANGE range = new D3D12_RANGE();

            void* ptr;
            CHECK_HR(alloc->Get()->GetResource()->Map(0, &range, &ptr));

            FillData(ptr, alloc->Get()->GetSize(), seed);
            alloc->Get()->GetResource()->Unmap(0, null);
        }
    }

    internal static void FillAllocationsDataGPU([NativeTypeName("const TestContext &")] in TestContext ctx, [NativeTypeName("const ComPtr<D3D12MA::Allocation> *")] ComPtr<D3D12MA_Allocation>* allocs, [NativeTypeName("size_t")] nuint allocCount, [NativeTypeName("UINT")] uint seed)
    {
        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
            ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
            Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
        };

        List<D3D12_RESOURCE_BARRIER> barriers = new List<D3D12_RESOURCE_BARRIER>();
        List<ComPtr<D3D12MA_Allocation>> uploadAllocs = new List<ComPtr<D3D12MA_Allocation>>();

        barriers.Capacity = (int)(allocCount);
        uploadAllocs.Capacity = (int)(allocCount);

        // Move resource into right state
        D3D12_RESOURCE_BARRIER barrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
            Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
        };

        barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
        barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;

        ID3D12GraphicsCommandList* cl = BeginCommandList();

        for (ComPtr<D3D12MA_Allocation>* alloc = allocs; alloc < (allocs + allocCount); alloc++)
        {
            // Copy only buffers for now
            D3D12_RESOURCE_DESC resDesc = alloc->Get()->GetResource()->GetDesc();

            if (resDesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                using ComPtr<D3D12MA_Allocation> uploadAlloc = new ComPtr<D3D12MA_Allocation>();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&uploadAlloc), IID_NULL, null));

                D3D12_RANGE range = new D3D12_RANGE();

                void* ptr;
                CHECK_HR(uploadAlloc.Get()->GetResource()->Map(0, &range, &ptr));

                FillData(ptr, resDesc.Width, seed);
                uploadAlloc.Get()->GetResource()->Unmap(0, null);

                cl->CopyResource(alloc->Get()->GetResource(), uploadAlloc.Get()->GetResource());

                uploadAllocs.Add(uploadAlloc);
                _ = uploadAlloc.Detach();
            }

            barrier.Transition.pResource = alloc->Get()->GetResource();
            barrier.Transition.StateAfter = (D3D12_RESOURCE_STATES)(nuint)(alloc->Get()->GetPrivateData());

            barriers.Add(barrier);
        }

        fixed (D3D12_RESOURCE_BARRIER* pBarriers = CollectionsMarshal.AsSpan(barriers))
        {
            cl->ResourceBarrier((uint)(allocCount), pBarriers);
        }
        EndCommandList(cl);

        uploadAllocs.Dispose();
    }

    internal static bool ValidateData([NativeTypeName("const void *")] void* ptr, [NativeTypeName("const UINT64")] ulong sizeInBytes, [NativeTypeName("UINT")] uint seed)
    {
        uint* values = (uint*)(ptr);
        ulong sizeInValues = sizeInBytes / sizeof(uint);
        uint value = seed;

        for (uint i = 0; i < sizeInValues; ++i)
        {
            if (values[i] != value++)
            {
                // CHECK_BOOL(false, "ValidateData failed.");
                return false;
            }
        }
        return true;
    }

    internal static bool ValidateDataZero([NativeTypeName("const void *")] void* ptr, [NativeTypeName("const UINT64")] ulong sizeInBytes)
    {
        uint* values = (uint*)(ptr);
        ulong sizeInValues = sizeInBytes / sizeof(uint);

        for (uint i = 0; i < sizeInValues; ++i)
        {
            if (values[i] != 0)
            {
                // CHECK_BOOL(false, "ValidateData failed.");
                return false;
            }
        }
        return true;
    }

    internal static void ValidateAllocationsData([NativeTypeName("const ComPtr<D3D12MA::Allocation> *")] ComPtr<D3D12MA_Allocation>* allocs, [NativeTypeName("size_t")] nuint allocCount, [NativeTypeName("UINT")] uint seed)
    {
        for (ComPtr<D3D12MA_Allocation>* alloc = allocs; alloc < (allocs + allocCount); alloc++)
        {
            D3D12_RANGE range = new D3D12_RANGE();

            void* ptr;
            CHECK_HR(alloc->Get()->GetResource()->Map(0, &range, &ptr));

            CHECK_BOOL(ValidateData(ptr, alloc->Get()->GetSize(), seed));
            alloc->Get()->GetResource()->Unmap(0, null);
        }
    }

    internal static void ValidateAllocationsDataGPU([NativeTypeName("const TestContext &")] in TestContext ctx, [NativeTypeName("const ComPtr<D3D12MA::Allocation> *")] ComPtr<D3D12MA_Allocation>* allocs, [NativeTypeName("size_t")] nuint allocCount, [NativeTypeName("UINT")] uint seed)
    {
        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_READBACK,
            ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
            Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
        };

        List<D3D12_RESOURCE_BARRIER> barriers = new List<D3D12_RESOURCE_BARRIER>();
        List<ComPtr<D3D12MA_Allocation>> downloadAllocs = new List<ComPtr<D3D12MA_Allocation>>();

        barriers.Capacity = (int)(allocCount);
        downloadAllocs.Capacity = (int)(allocCount);

        // Move resource into right state
        D3D12_RESOURCE_BARRIER barrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
            Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
        };

        barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
        barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_SOURCE;

        ID3D12GraphicsCommandList* cl = BeginCommandList();
        nuint resCount = allocCount;

        for (ComPtr<D3D12MA_Allocation>* alloc = allocs; alloc < (allocs + allocCount); alloc++)
        {
            // Check only buffers for now
            D3D12_RESOURCE_DESC resDesc = alloc->Get()->GetResource()->GetDesc();

            if (resDesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                using ComPtr<D3D12MA_Allocation> downloadAlloc = new ComPtr<D3D12MA_Allocation>();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&downloadAlloc), IID_NULL, null));

                barrier.Transition.pResource = alloc->Get()->GetResource();
                barrier.Transition.StateBefore = (D3D12_RESOURCE_STATES)(nuint)(alloc->Get()->GetPrivateData());

                barriers.Add(barrier);

                downloadAllocs.Add(downloadAlloc);
                _ = downloadAlloc.Detach();
            }
            else
            {
                --resCount;
            }
        }

        fixed (D3D12_RESOURCE_BARRIER* pBarriers = CollectionsMarshal.AsSpan(barriers))
        {
            cl->ResourceBarrier((uint)(resCount), pBarriers);
        }

        for (nuint i = 0, j = 0; i < resCount; ++j)
        {
            if (allocs[j].Get()->GetResource()->GetDesc().Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                cl->CopyResource(downloadAllocs[(int)(i)].Get()->GetResource(), allocs[j].Get()->GetResource());

                barriers[(int)(i)].Transition.StateAfter = barriers[(int)(i)].Transition.StateBefore;
                barriers[(int)(i)].Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_SOURCE;

                ++i;
            }
        }

        fixed (D3D12_RESOURCE_BARRIER* pBarriers = CollectionsMarshal.AsSpan(barriers))
        {
            cl->ResourceBarrier((uint)(resCount), pBarriers);
        }
        EndCommandList(cl);

        foreach (var alloc in downloadAllocs)
        {
            D3D12_RANGE range = new D3D12_RANGE();

            void* ptr;
            CHECK_HR(alloc.Get()->GetResource()->Map(0, &range, &ptr));

            CHECK_BOOL(ValidateData(ptr, alloc.Get()->GetResource()->GetDesc().Width, seed));
            alloc.Get()->GetResource()->Unmap(0, null);
        }

        downloadAllocs.Dispose();
    }

    internal static void SaveStatsStringToFile([NativeTypeName("const TestContext &")] in TestContext ctx, [NativeTypeName("const wchar_t *")] string dstFilePath, [Optional, DefaultParameterValue(TRUE)] BOOL detailed)
    {
        char* s = null;
        ctx.allocator->BuildStatsString(&s, detailed);
        SaveFile(dstFilePath, s, wcslen(s) * sizeof(char));
        ctx.allocator->FreeStatsString(s);
    }

    internal static void TestDebugMargin([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        if (D3D12MA_DEBUG_MARGIN == 0)
        {
            return;
        }

        _ = wprintf("Test D3D12MA_DEBUG_MARGIN = {0}\n", (uint)(D3D12MA_DEBUG_MARGIN));

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC();
        D3D12_RESOURCE_DESC resDesc = new D3D12_RESOURCE_DESC();

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;

        nuint BUF_COUNT = 10;
        ComPtr<D3D12MA_Allocation>* buffers = stackalloc ComPtr<D3D12MA_Allocation>[(int)(BUF_COUNT)];

        for (nuint algorithmIndex = 0; algorithmIndex < 2; ++algorithmIndex)
        {
            switch (algorithmIndex)
            {
                case 0:
                {
                    poolDesc.Flags = D3D12MA_POOL_FLAG_NONE;
                    break;
                }

                case 1:
                {
                    poolDesc.Flags = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR;
                    break;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }
            }

            using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
            CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

            allocDesc.CustomPool = pool.Get();

            // Create few buffers of different size.

            for (nuint allocIndex = 0; allocIndex < 10; ++allocIndex)
            {
                bool isLast = allocIndex == BUF_COUNT - 1;
                FillResourceDescForBuffer(out resDesc, (ulong)(allocIndex + 1) * 0x10000);
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&buffers[allocIndex]), IID_NULL, null));
            }

            // JSON dump
            char* json = null;
            ctx.allocator->BuildStatsString(&json, TRUE);
            // Put breakpoint here to manually inspect json in a debugger.

            // Check if their offsets preserve margin between them.
            D3D12MA_SORT(buffers, buffers + BUF_COUNT, (lhs, rhs) => {
                if (lhs.Get()->GetHeap() != rhs.Get()->GetHeap())
                {
                    return ((nuint)(lhs.Get()->GetHeap())).CompareTo((nuint)(rhs.Get()->GetHeap()));
                }

                return lhs.Get()->GetOffset().CompareTo(rhs.Get()->GetOffset());
            });

            for (nuint i = 1; i < BUF_COUNT; ++i)
            {
                if (buffers[i].Get()->GetHeap() == buffers[i - 1].Get()->GetHeap())
                {
                    ulong allocStart = buffers[i].Get()->GetOffset();
                    ulong prevAllocEnd = buffers[i - 1].Get()->GetOffset() + buffers[i - 1].Get()->GetSize();
                    CHECK_BOOL(allocStart >= prevAllocEnd + D3D12MA_DEBUG_MARGIN);
                }
            }

            ctx.allocator->FreeStatsString(json);

            for (nuint i = 0; i < BUF_COUNT; i++)
            {
                buffers[i].Dispose();
            }
        }

        for (nuint i = 0; i < BUF_COUNT; i++)
        {
            buffers[i].Dispose();
        }
    }

    internal static void TestDebugMarginNotInVirtualAllocator([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test D3D12MA_DEBUG_MARGIN not applied to virtual allocator\n");

        const nuint ALLOCATION_COUNT = 10;
        D3D12MA_VirtualAllocation* allocs = stackalloc D3D12MA_VirtualAllocation[(int)(ALLOCATION_COUNT)];

        for (nuint algorithmIndex = 0; algorithmIndex < 2; ++algorithmIndex)
        {
            D3D12MA_VIRTUAL_BLOCK_DESC blockDesc = new D3D12MA_VIRTUAL_BLOCK_DESC {
                Size = ALLOCATION_COUNT * MEGABYTE,
            };

            switch (algorithmIndex)
            {
                case 0:
                {
                    blockDesc.Flags = D3D12MA_VIRTUAL_BLOCK_FLAG_NONE;
                    break;
                }

                case 1:
                {
                    blockDesc.Flags = D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR;
                    break;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }
            }

            using ComPtr<D3D12MA_VirtualBlock> block = new ComPtr<D3D12MA_VirtualBlock>();
            CHECK_HR(D3D12MA_CreateVirtualBlock(&blockDesc, (D3D12MA_VirtualBlock**)(&block)));

            // Fill the entire block

            for (nuint i = 0; i < ALLOCATION_COUNT; ++i)
            {
                D3D12MA_VIRTUAL_ALLOCATION_DESC allocDesc = new D3D12MA_VIRTUAL_ALLOCATION_DESC {
                    Size = 1 * MEGABYTE,
                };
                CHECK_HR(block.Get()->Allocate(&allocDesc, &allocs[i], null));
            }

            block.Get()->Clear();
        }
    }

    internal static void TestJson([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test JSON\n");

        List<ComPtr<D3D12MA_Pool>> pools = new List<ComPtr<D3D12MA_Pool>>();
        List<ComPtr<D3D12MA_Allocation>> allocs = new List<ComPtr<D3D12MA_Allocation>>();

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC();
        D3D12_RESOURCE_DESC resDesc = new D3D12_RESOURCE_DESC {
            Alignment = 0,
            MipLevels = 1,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
        };

        D3D12_RESOURCE_ALLOCATION_INFO allocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
            Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
            SizeInBytes = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
        };

        // Select if using custom pool or default
        for (byte poolType = 0; poolType < 2; ++poolType)
        {
            // Select different heaps
            for (byte heapType = 0; heapType < 5; ++heapType)
            {
                Unsafe.SkipInit(out D3D12_RESOURCE_STATES state);
                D3D12_CPU_PAGE_PROPERTY cpuPageType = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
                D3D12_MEMORY_POOL memoryPool = D3D12_MEMORY_POOL_UNKNOWN;

                switch (heapType)
                {
                    case 0:
                    {
                        allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                        state = D3D12_RESOURCE_STATE_COMMON;
                        break;
                    }

                    case 1:
                    {
                        allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;
                        state = D3D12_RESOURCE_STATE_GENERIC_READ;
                        break;
                    }

                    case 2:
                    {
                        allocDesc.HeapType = D3D12_HEAP_TYPE_READBACK;
                        state = D3D12_RESOURCE_STATE_COPY_DEST;
                        break;
                    }

                    case 3:
                    {
                        allocDesc.HeapType = D3D12_HEAP_TYPE_CUSTOM;
                        state = D3D12_RESOURCE_STATE_COMMON;
                        cpuPageType = D3D12_CPU_PAGE_PROPERTY_NOT_AVAILABLE;
                        memoryPool = ctx.allocator->IsUMA() ? D3D12_MEMORY_POOL_L0 : D3D12_MEMORY_POOL_L1;
                        break;
                    }

                    case 4:
                    {
                        allocDesc.HeapType = D3D12_HEAP_TYPE_CUSTOM;
                        state = D3D12_RESOURCE_STATE_GENERIC_READ;
                        cpuPageType = D3D12_CPU_PAGE_PROPERTY_WRITE_COMBINE;
                        memoryPool = D3D12_MEMORY_POOL_L0;
                        break;
                    }
                }

                // Skip custom heaps for default pools
                if (poolType == 0 && heapType > 2)
                {
                    continue;
                }

                bool texturesPossible = heapType == 0 || heapType == 3;

                // Select different resource region types
                for (byte resType = 0; resType < 3; ++resType)
                {
                    allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
                    D3D12_RESOURCE_FLAGS resFlags = D3D12_RESOURCE_FLAG_NONE;

                    if (texturesPossible)
                    {
                        switch (resType)
                        {
                            case 1:
                            {
                                allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES;
                                break;
                            }

                            case 2:
                            {
                                allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES;
                                resFlags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
                                break;
                            }
                        }
                    }

                    switch (poolType)
                    {
                        case 0:
                        {
                            allocDesc.CustomPool = null;
                            break;
                        }

                        case 1:
                        {
                            using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();

                            D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
                                HeapFlags = allocDesc.ExtraHeapFlags,
                                HeapProperties = new D3D12_HEAP_PROPERTIES {
                                    Type = allocDesc.HeapType,
                                    CPUPageProperty = cpuPageType,
                                    MemoryPoolPreference = memoryPool,
                                },
                            };
                            CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

                            allocDesc.CustomPool = pool.Get();

                            pools.Add(pool);
                            _ = pool.Detach();

                            break;
                        }
                    }

                    // Select different allocation flags
                    for (byte allocFlag = 0; allocFlag < 2; ++allocFlag)
                    {
                        switch (allocFlag)
                        {
                            case 0:
                            {
                                allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NONE;
                                break;
                            }

                            case 1:
                            {
                                allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;
                                break;
                            }
                        }

                        // Select different alloc types (block, buffer, texture, etc.)
                        for (byte allocType = 0; allocType < 5; ++allocType)
                        {
                            // Select different data stored in the allocation
                            for (byte data = 0; data < 4; ++data)
                            {
                                using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();

                                if (texturesPossible && resType != 0)
                                {
                                    resDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
                                    resDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;

                                    switch (allocType % 3)
                                    {
                                        case 0:
                                        {
                                            resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE1D;
                                            resDesc.Width = 512;
                                            resDesc.Height = 1;
                                            resDesc.DepthOrArraySize = 1;
                                            resDesc.Flags = resFlags;

                                            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, state, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));
                                            break;
                                        }

                                        case 1:
                                        {
                                            resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                                            resDesc.Width = 1024;
                                            resDesc.Height = 512;
                                            resDesc.DepthOrArraySize = 1;
                                            resDesc.Flags = resFlags;
                                            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, state, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));
                                            break;
                                        }

                                        case 2:
                                        {
                                            resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE3D;
                                            resDesc.Width = 512;
                                            resDesc.Height = 256;
                                            resDesc.DepthOrArraySize = 128;
                                            resDesc.Flags = resFlags;
                                            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, state, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    switch (allocType % 2)
                                    {
                                        case 0:
                                        {
                                            CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &allocInfo, (D3D12MA_Allocation**)(&alloc)));
                                            break;
                                        }

                                        case 1:
                                        {
                                            FillResourceDescForBuffer(out resDesc, 1024);
                                            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, state, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));
                                            break;
                                        }
                                    }
                                }

                                switch (data)
                                {
                                    case 1:
                                    {
                                        alloc.Get()->SetPrivateData((void*)16112007);
                                        break;
                                    }

                                    case 2:
                                    {
                                        fixed (char* pName = "SHEPURD")
                                        {
                                            alloc.Get()->SetName(pName);
                                        }
                                        break;
                                    }

                                    case 3:
                                    {
                                        alloc.Get()->SetPrivateData((void*)(26012010));

                                        fixed (char* pName = "JOKER")
                                        {
                                            alloc.Get()->SetName(pName);
                                        }
                                        break;
                                    }
                                }

                                allocs.Add(alloc);
                                _ = alloc.Detach();
                            }
                        }

                    }
                }
            }
        }

        SaveStatsStringToFile(ctx, "JSON_D3D12.json");

        allocs.Dispose();
        pools.Dispose();
    }

    internal static void TestCommittedResourcesAndJson([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test committed resources and JSON\n");

        const uint count = TestCommittedResourcesAndJson_count;
        const ulong bufSize = 32ul * 1024;
        string?[] names = TestCommittedResourcesAndJson_names;

        ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)(count)];

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
            Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, bufSize);

        for (uint i = 0; i < count; ++i)
        {
            bool receiveExplicitResource = i < 2;
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&resources[i].allocation), __uuidof<ID3D12Resource>(), (receiveExplicitResource ? (void**)(&resources[i].resource) : null)));

            if (receiveExplicitResource)
            {
                ID3D12Resource* res = resources[i].resource.Get();
                CHECK_BOOL((res != null) && (res == resources[i].allocation.Get()->GetResource()));

                ulong refCountAfterAdd = res->AddRef();
                CHECK_BOOL(refCountAfterAdd == 3);

                _ = res->Release();
            }

            // Make sure it has implicit heap.
            CHECK_BOOL((resources[i].allocation.Get()->GetHeap() == null) && (resources[i].allocation.Get()->GetOffset() == 0));

            fixed (char* pName = names[i])
            {
                resources[i].allocation.Get()->SetName(pName);
            }
        }

        // Check names.
        for (uint i = 0; i < count; ++i)
        {
            char* allocName = resources[i].allocation.Get()->GetName();

            if (allocName != null)
            {
                CHECK_BOOL(wcscmp(allocName, names[i]) == 0);
            }
            else
            {
                CHECK_BOOL(names[i] == null);
            }
        }

        char* jsonString;
        ctx.allocator->BuildStatsString(&jsonString, TRUE);

        CHECK_BOOL(wcsstr(jsonString, "\"Resource\\nFoo\\r\\nBar\"") != null);
        CHECK_BOOL(wcsstr(jsonString, "\"Resource \\\"'&<>?#@!&-=_+[]{};:,.\\/\\\\\"") != null);
        CHECK_BOOL(wcsstr(jsonString, "\"\"") != null);

        ctx.allocator->FreeStatsString(jsonString);

        for (uint i = 0; i < count; i++)
        {
            resources[i].Dispose();
        }
    }

    internal static void TestCustomHeapFlags([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test custom heap flags\n");

        // 1. Just memory heap with custom flags
        {
            D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
                HeapType = D3D12_HEAP_TYPE_DEFAULT,
                ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES |
                D3D12_HEAP_FLAG_SHARED, // Extra flag.
            };

            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
                SizeInBytes = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
                Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
            };

            using ResourceWithAllocation res = new ResourceWithAllocation();
            CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &resAllocInfo, (D3D12MA_Allocation**)(&res.allocation)));

            // Must be created as separate allocation.
            CHECK_BOOL(res.allocation.Get()->GetOffset() == 0);
        }

        // 2. Committed resource with custom flags
        {
            D3D12_RESOURCE_DESC resourceDesc = new D3D12_RESOURCE_DESC {
                Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                Alignment = 0,
                Width = 1920,
                Height = 1080,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = DXGI_FORMAT_R8G8B8A8_UNORM,
                SampleDesc = new DXGI_SAMPLE_DESC {
                    Count = 1,
                    Quality = 0,
                },
                Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                Flags = D3D12_RESOURCE_FLAG_ALLOW_CROSS_ADAPTER,
            };

            D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
                HeapType = D3D12_HEAP_TYPE_DEFAULT,
                ExtraHeapFlags = D3D12_HEAP_FLAG_SHARED | D3D12_HEAP_FLAG_SHARED_CROSS_ADAPTER, // Extra flags.
            };

            using ResourceWithAllocation res = new ResourceWithAllocation();
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_COMMON, null, (D3D12MA_Allocation**)(&res.allocation), __uuidof<ID3D12Resource>(), (void**)(&res.resource)));

            // Must be created as committed.
            CHECK_BOOL(res.allocation.Get()->GetHeap() == null);
        }
    }

    internal static void TestPlacedResources([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test placed resources\n");

        bool alwaysCommitted = (ctx.allocatorFlags & D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED) != 0;

        const uint count = 4;
        const ulong bufSize = 32ul * 1024;

        ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)(count)];

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, bufSize);

        for (uint i = 0; i < count; ++i)
        {
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&resources[i].allocation), __uuidof<ID3D12Resource>(), (void**)(&resources[i].resource)));

            // Make sure it doesn't have implicit heap.
            if (!alwaysCommitted)
            {
                CHECK_BOOL(resources[i].allocation.Get()->GetHeap() != null);
            }
        }

        // Make sure at least some of the resources belong to the same heap, but their memory ranges don't overlap.
        bool sameHeapFound = false;

        for (nuint i = 0; i < count; ++i)
        {
            for (nuint j = i + 1; j < count; ++j)
            {
                ref readonly ResourceWithAllocation resI = ref resources[i];
                ref readonly ResourceWithAllocation resJ = ref resources[j];

                if ((resI.allocation.Get()->GetHeap() != null) && (resI.allocation.Get()->GetHeap() == resJ.allocation.Get()->GetHeap()))
                {
                    sameHeapFound = true;
                    CHECK_BOOL(((resI.allocation.Get()->GetOffset() + resI.allocation.Get()->GetSize()) <= resJ.allocation.Get()->GetOffset()) || ((resJ.allocation.Get()->GetOffset() + resJ.allocation.Get()->GetSize()) <= resI.allocation.Get()->GetOffset()));
                }
            }
        }

        if (!alwaysCommitted)
        {
            CHECK_BOOL(sameHeapFound);
        }

        // Additionally create a texture to see if no error occurs due to bad handling of Resource Tier.
        resourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = 0,
            Width = 1024,
            Height = 1024,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_NONE,
        };

        using ResourceWithAllocation textureRes = new ResourceWithAllocation();
        CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&textureRes.allocation), __uuidof<ID3D12Resource>(), (void**)(&textureRes.resource)));

        // Additionally create an MSAA render target to see if no error occurs due to bad handling of Resource Tier.
        resourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = 0,
            Width = 1920,
            Height = 1080,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 2,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET,
        };

        using ResourceWithAllocation renderTargetRes = new ResourceWithAllocation();
        CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_RENDER_TARGET, null, (D3D12MA_Allocation**)(&renderTargetRes.allocation), __uuidof<ID3D12Resource>(), (void**)(&renderTargetRes.resource)));

        for (uint i = 0; i < count; i++)
        {
            resources[i].Dispose();
        }
    }

    internal static void TestOtherComInterface([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test other COM interface\n");
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, 0x10000);

        for (uint i = 0; i < 2; ++i)
        {
            D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
                HeapType = D3D12_HEAP_TYPE_DEFAULT,
            };

            if (i == 1)
            {
                allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;
            }

            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
            using ComPtr<ID3D12Pageable> pageable = new ComPtr<ID3D12Pageable>();

            CHECK_HR(ctx.allocator->CreateResource(
                &allocDesc,
                &resDesc,
                D3D12_RESOURCE_STATE_COMMON,
                null,                           // pOptimizedClearValue
                (D3D12MA_Allocation**)(&alloc),
                __uuidof<ID3D12Pageable>(),
                (void**)(&pageable)
            ));

            // Do something with the interface to make sure it's valid.
            using ComPtr<ID3D12Device> device = new ComPtr<ID3D12Device>();

            CHECK_HR(pageable.Get()->GetDevice(__uuidof<ID3D12Device>(), (void**)(&device)));
            CHECK_BOOL(device.Get() == ctx.device);
        }
    }

    internal static void TestCustomPools([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test custom pools\n");

        // # Fetch global stats 1

        D3D12MA_TotalStatistics globalStatsBeg = new D3D12MA_TotalStatistics();
        ctx.allocator->CalculateStatistics(&globalStatsBeg);

        // # Create pool, 1..2 blocks of 11 MB

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
        poolDesc.BlockSize = 11 * MEGABYTE;
        poolDesc.MinBlockCount = 1;
        poolDesc.MaxBlockCount = 2;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        // # Validate stats for empty pool

        D3D12MA_DetailedStatistics poolStats = new D3D12MA_DetailedStatistics();
        pool.Get()->CalculateStatistics(&poolStats);

        CHECK_BOOL(poolStats.Stats.BlockCount == 1);
        CHECK_BOOL(poolStats.Stats.AllocationCount == 0);
        CHECK_BOOL(poolStats.Stats.AllocationBytes == 0);
        CHECK_BOOL(poolStats.Stats.BlockBytes - poolStats.Stats.AllocationBytes == poolStats.Stats.BlockCount * poolDesc.BlockSize);

        // # SetName and GetName

        const string NAME = "Custom pool name 1";

        fixed (char* pName = NAME)
        {
            pool.Get()->SetName(pName);
        }

        CHECK_BOOL(wcscmp(pool.Get()->GetName(), NAME) == 0);

        // # Create buffers 2x 5 MB

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
            ExtraHeapFlags = unchecked((D3D12_HEAP_FLAGS)(0xCDCDCDCD)), // Should be ignored.
            HeapType = unchecked((D3D12_HEAP_TYPE)(0xCDCDCDCD)),        // Should be ignored.
        };

        const ulong BUFFER_SIZE = 5 * MEGABYTE;
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, BUFFER_SIZE);

        ComPtr<D3D12MA_Allocation>* allocs = stackalloc ComPtr<D3D12MA_Allocation>[4];

        for (uint i = 0; i < 2; ++i)
        {
            CHECK_HR(ctx.allocator->CreateResource(
                &allocDesc,
                &resDesc,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                null,                               // pOptimizedClearValue
                (D3D12MA_Allocation**)(&allocs[i]),
                __uuidof<ID3D12Resource>(),         // riidResource
                null                                // ppvResource
            ));
        }

        // # Validate pool stats now

        pool.Get()->CalculateStatistics(&poolStats);

        CHECK_BOOL(poolStats.Stats.BlockCount == 1);
        CHECK_BOOL(poolStats.Stats.AllocationCount == 2);
        CHECK_BOOL(poolStats.Stats.AllocationBytes == 2 * BUFFER_SIZE);
        CHECK_BOOL((poolStats.Stats.BlockBytes - poolStats.Stats.AllocationBytes) == (poolDesc.BlockSize - poolStats.Stats.AllocationBytes));

        // # Check that global stats are updated as well

        D3D12MA_TotalStatistics globalStatsCurr = new D3D12MA_TotalStatistics();
        ctx.allocator->CalculateStatistics(&globalStatsCurr);

        CHECK_BOOL(globalStatsCurr.Total.Stats.AllocationCount == (globalStatsBeg.Total.Stats.AllocationCount + poolStats.Stats.AllocationCount));
        CHECK_BOOL(globalStatsCurr.Total.Stats.BlockCount == (globalStatsBeg.Total.Stats.BlockCount + poolStats.Stats.BlockCount));
        CHECK_BOOL(globalStatsCurr.Total.Stats.AllocationBytes == (globalStatsBeg.Total.Stats.AllocationBytes + poolStats.Stats.AllocationBytes));

        // # NEVER_ALLOCATE and COMMITTED should fail
        // (Committed allocations not allowed in this pool because BlockSize != 0.)

        for (uint i = 0; i < 2; ++i)
        {
            allocDesc.Flags = (i == 0) ? D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE : D3D12MA_ALLOCATION_FLAG_COMMITTED;
            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();

            HRESULT hr = ctx.allocator->CreateResource(&allocDesc, &resDesc,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                null,                               // pOptimizedClearValue
                (D3D12MA_Allocation**)(&alloc),
                __uuidof<ID3D12Resource>(),         // riidResource
                null                                // ppvResource
            );
            CHECK_BOOL(FAILED(hr));
        }

        // # 3 more buffers. 3rd should fail.
        allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NONE;

        for (uint i = 2; i < 5; ++i)
        {
            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();

            HRESULT hr = ctx.allocator->CreateResource(
                &allocDesc,
                &resDesc,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                null,                               // pOptimizedClearValue
                (D3D12MA_Allocation**)(&alloc),
                __uuidof<ID3D12Resource>(),         // riidResource
                null                                // ppvResource
            );

            if (i < 4)
            {
                CHECK_HR(hr);
                allocs[i] = alloc;
                _ = alloc.Detach();
            }
            else
            {
                CHECK_BOOL(FAILED(hr));
            }
        }

        pool.Get()->CalculateStatistics(&poolStats);

        CHECK_BOOL(poolStats.Stats.BlockCount == 2);
        CHECK_BOOL(poolStats.Stats.AllocationCount == 4);
        CHECK_BOOL(poolStats.Stats.AllocationBytes == 4 * BUFFER_SIZE);
        CHECK_BOOL((poolStats.Stats.BlockBytes - poolStats.Stats.AllocationBytes) == ((poolStats.Stats.BlockCount * poolDesc.BlockSize) - poolStats.Stats.AllocationBytes));

        // # Make room, AllocateMemory, CreateAliasingResource

        _ = allocs[3].Reset();
        _ = allocs[0].Reset();

        D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
            SizeInBytes = 5 * MEGABYTE,
            Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
        };

        CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &resAllocInfo, (D3D12MA_Allocation**)(&allocs[0])));

        resDesc.Width = 1 * MEGABYTE;
        using ComPtr<ID3D12Resource> res = new ComPtr<ID3D12Resource>();

        CHECK_HR(ctx.allocator->CreateAliasingResource(
            allocs[0].Get(),
            0,                                  // AllocationLocalOffset
            &resDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,                               // pOptimizedClearValue
            __uuidof<ID3D12Resource>(),
            (void**)(&res)
        ));

        // JSON dump
        char* json = null;

        ctx.allocator->BuildStatsString(&json, TRUE);
        ctx.allocator->FreeStatsString(json);

        for (int i = 0; i < 4; i++)
        {
            allocs[i].Dispose();
        }
    }

    internal static void TestPoolsAndAllocationParameters([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test pools and allocation parameters\n");

        using ComPtr<D3D12MA_Pool> pool1 = new ComPtr<D3D12MA_Pool>();
        using ComPtr<D3D12MA_Pool> pool2 = new ComPtr<D3D12MA_Pool>();

        List<ComPtr<D3D12MA_Allocation>> bufs = new List<ComPtr<D3D12MA_Allocation>>();
        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC();

        uint totalNewAllocCount = 0;
        uint totalNewBlockCount = 0;

        D3D12MA_TotalStatistics statsBeg, statsEnd;
        ctx.allocator->CalculateStatistics(&statsBeg);

        HRESULT hr;
        using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();

        // poolTypeI:
        // 0 = default pool
        // 1 = custom pool, default (flexible) block size and block count
        // 2 = custom pool, fixed block size and limited block count
        for (nuint poolTypeI = 0; poolTypeI < 3; ++poolTypeI)
        {
            if (poolTypeI == 0)
            {
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                allocDesc.CustomPool = null;
            }
            else if (poolTypeI == 1)
            {
                D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();

                poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
                poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

                hr = ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool1));
                CHECK_HR(hr);

                allocDesc.CustomPool = pool1.Get();
            }
            else if (poolTypeI == 2)
            {
                D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();

                poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
                poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
                poolDesc.MaxBlockCount = 1;
                poolDesc.BlockSize = (2 * MEGABYTE) + (MEGABYTE / 2); // 2.5 MB

                hr = ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool2));
                CHECK_HR(hr);

                allocDesc.CustomPool = pool2.Get();
            }

            uint poolAllocCount = 0, poolBlockCount = 0;
            FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, MEGABYTE);

            // Default parameters
            allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NONE;
            hr = ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null);

            CHECK_BOOL(SUCCEEDED(hr) && (alloc.Get() != null) && (alloc.Get()->GetResource() != null));

            ID3D12Heap* defaultAllocHeap = alloc.Get()->GetHeap();
            ulong defaultAllocOffset = alloc.Get()->GetOffset();

            bufs.Add(alloc);
            _ = alloc.Detach();

            ++poolAllocCount;

            // COMMITTED. Should not try pool2 as it may assert on invalid call.
            if (poolTypeI != 2)
            {
                allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;
                hr = ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null);

                CHECK_BOOL(SUCCEEDED(hr) && (alloc.Get() != null) && (alloc.Get()->GetResource() != null));
                CHECK_BOOL(alloc.Get()->GetOffset() == 0); // Committed
                CHECK_BOOL(alloc.Get()->GetHeap() == null); // Committed

                bufs.Add(alloc);
                _ = alloc.Detach();

                ++poolAllocCount;
            }

            // NEVER_ALLOCATE #1
            allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE;
            hr = ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null);

            CHECK_BOOL(SUCCEEDED(hr) && (alloc.Get() != null) && (alloc.Get()->GetResource() != null));
            CHECK_BOOL(alloc.Get()->GetHeap() == defaultAllocHeap); // Same memory block as default one.
            CHECK_BOOL(alloc.Get()->GetOffset() != defaultAllocOffset);

            bufs.Add(alloc);
            _ = alloc.Detach();

            ++poolAllocCount;

            // NEVER_ALLOCATE #2. Should fail in pool2 as it has no space.
            allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE;
            hr = ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null);

            if (poolTypeI == 2)
            {
                CHECK_BOOL(FAILED(hr));
            }
            else
            {
                CHECK_BOOL(SUCCEEDED(hr) && (alloc.Get() != null) && (alloc.Get()->GetResource() != null));

                bufs.Add(alloc);
                _ = alloc.Detach();

                ++poolAllocCount;
            }

            // Pool stats
            switch (poolTypeI)
            {
                case 0:
                {
                    poolBlockCount = 1;
                    break; // At least 1 added for dedicated allocation.
                }

                case 1:
                {
                    poolBlockCount = 2;
                    break; // 1 for custom pool block and 1 for dedicated allocation.
                }

                case 2:
                {
                    poolBlockCount = 1;
                    break; // Only custom pool, no dedicated allocation.
                }
            }

            if (poolTypeI > 0)
            {
                D3D12MA_DetailedStatistics poolStats = new D3D12MA_DetailedStatistics();
                ((poolTypeI == 2) ? pool2 : pool1).Get()->CalculateStatistics(&poolStats);

                CHECK_BOOL(poolStats.Stats.AllocationCount == poolAllocCount);
                CHECK_BOOL(poolStats.Stats.AllocationBytes == poolAllocCount * MEGABYTE);
                CHECK_BOOL(poolStats.Stats.BlockCount == poolBlockCount);
            }

            totalNewAllocCount += poolAllocCount;
            totalNewBlockCount += poolBlockCount;
        }

        ctx.allocator->CalculateStatistics(&statsEnd);

        CHECK_BOOL(statsEnd.Total.Stats.AllocationCount == (statsBeg.Total.Stats.AllocationCount + totalNewAllocCount));
        CHECK_BOOL(statsEnd.Total.Stats.BlockCount >= (statsBeg.Total.Stats.BlockCount + totalNewBlockCount));
        CHECK_BOOL(statsEnd.Total.Stats.AllocationBytes == (statsBeg.Total.Stats.AllocationBytes + (totalNewAllocCount * MEGABYTE)));

        bufs.Dispose();
    }

    internal static void TestCustomPool_MinAllocationAlignment([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test custom pool MinAllocationAlignment\n");

        const ulong BUFFER_SIZE = 32;
        const nuint BUFFER_COUNT = 4;
        const ulong MIN_ALIGNMENT = 128 * 1024;

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;
        poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
        poolDesc.MinAllocationAlignment = MIN_ALIGNMENT;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, BUFFER_SIZE);

        ComPtr<D3D12MA_Allocation>* allocs = stackalloc ComPtr<D3D12MA_Allocation>[(int)(BUFFER_COUNT)];

        for (nuint i = 0; i < BUFFER_COUNT; ++i)
        {
            CHECK_HR(ctx.allocator->CreateResource(
                &allocDesc,
                &resDesc,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                null,                               // pOptimizedClearValue
                (D3D12MA_Allocation**)(&allocs[i]),
                IID_NULL,                           // riidResource
                null                                // ppvResource
            ));
            CHECK_BOOL(allocs[i].Get()->GetOffset() % MIN_ALIGNMENT == 0);
        }

        for (nuint i = 0; i < BUFFER_COUNT; i++)
        {
            allocs[i].Dispose();
        }
    }

    internal static void TestCustomPool_Committed([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test custom pool committed\n");

        const ulong BUFFER_SIZE = 32;

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
            Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, BUFFER_SIZE);

        using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
        CHECK_HR(ctx.allocator->CreateResource(
            &allocDesc,
            &resDesc,
            D3D12_RESOURCE_STATE_COMMON,
            null,                           // pOptimizedClearValue
            (D3D12MA_Allocation**)(&alloc),
            IID_NULL,                       // riidResource
            null                            // ppvResource
        ));
        CHECK_BOOL(alloc.Get()->GetHeap() == null);
        CHECK_BOOL(alloc.Get()->GetResource() != null);
        CHECK_BOOL(alloc.Get()->GetOffset() == 0);
    }

    internal static HRESULT TestCustomHeap([NativeTypeName("const TestContext &")] in TestContext ctx, [NativeTypeName("const D3D12_HEAP_PROPERTIES &")] in D3D12_HEAP_PROPERTIES heapProps)
    {
        D3D12MA_TotalStatistics globalStatsBeg = new D3D12MA_TotalStatistics();
        ctx.allocator->CalculateStatistics(&globalStatsBeg);

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            HeapProperties = heapProps,
            HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
            BlockSize = 10 * MEGABYTE,
            MinBlockCount = 1,
            MaxBlockCount = 1,
        };

        const ulong BUFFER_SIZE = 1 * MEGABYTE;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        HRESULT hr = ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool));

        if (SUCCEEDED(hr))
        {
            D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
                CustomPool = pool.Get(),
            };
            FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, BUFFER_SIZE);

            // Pool already allocated a block. We don't expect CreatePlacedResource to fail.
            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();

            CHECK_HR(ctx.allocator->CreateResource(
                &allocDesc,
                &resDesc,
                D3D12_RESOURCE_STATE_COPY_DEST,
                null,                               // pOptimizedClearValue
                (D3D12MA_Allocation**)(&alloc),
                __uuidof<ID3D12Resource>(),         // riidResource
                null                                // ppvResource
            ));

            D3D12MA_TotalStatistics globalStatsCurr = new D3D12MA_TotalStatistics();
            ctx.allocator->CalculateStatistics(&globalStatsCurr);

            // Make sure it is accounted only in CUSTOM heap not any of the standard heaps.

            CHECK_BOOL(memcmp(&globalStatsCurr.HeapType[0], &globalStatsBeg.HeapType[0], __sizeof<D3D12MA_DetailedStatistics>()) == 0);
            CHECK_BOOL(memcmp(&globalStatsCurr.HeapType[1], &globalStatsBeg.HeapType[1], __sizeof<D3D12MA_DetailedStatistics>()) == 0);
            CHECK_BOOL(memcmp(&globalStatsCurr.HeapType[2], &globalStatsBeg.HeapType[2], __sizeof<D3D12MA_DetailedStatistics>()) == 0);

            CHECK_BOOL(globalStatsCurr.HeapType[3].Stats.AllocationCount == globalStatsBeg.HeapType[3].Stats.AllocationCount + 1);
            CHECK_BOOL(globalStatsCurr.HeapType[3].Stats.BlockCount == globalStatsBeg.HeapType[3].Stats.BlockCount + 1);
            CHECK_BOOL(globalStatsCurr.HeapType[3].Stats.AllocationBytes == globalStatsBeg.HeapType[3].Stats.AllocationBytes + BUFFER_SIZE);
            CHECK_BOOL(globalStatsCurr.Total.Stats.AllocationCount == globalStatsBeg.Total.Stats.AllocationCount + 1);
            CHECK_BOOL(globalStatsCurr.Total.Stats.BlockCount == globalStatsBeg.Total.Stats.BlockCount + 1);
            CHECK_BOOL(globalStatsCurr.Total.Stats.AllocationBytes == globalStatsBeg.Total.Stats.AllocationBytes + BUFFER_SIZE);

            // Map and write some data.
            if ((heapProps.CPUPageProperty == D3D12_CPU_PAGE_PROPERTY_WRITE_COMBINE) || (heapProps.CPUPageProperty == D3D12_CPU_PAGE_PROPERTY_WRITE_BACK))
            {
                ID3D12Resource* res = alloc.Get()->GetResource();

                uint* mappedPtr = null;
                D3D12_RANGE readRange = new D3D12_RANGE();

                CHECK_HR(res->Map(0, &readRange, (void**)&mappedPtr));
                *mappedPtr = 0xDEADC0DE;

                res->Unmap(0, null);
            }
        }

        return hr;
    }

    internal static void TestCustomHeaps([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test custom heap\n");

        // Use custom pool but the same as READBACK, which should be always available.
        D3D12_HEAP_PROPERTIES heapProps = new D3D12_HEAP_PROPERTIES {
            Type = D3D12_HEAP_TYPE_CUSTOM,
            CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_WRITE_BACK,
            MemoryPoolPreference = D3D12_MEMORY_POOL_L0,            // System memory
        };

        HRESULT hr = TestCustomHeap(ctx, heapProps);
        CHECK_HR(hr);
    }

    internal static void TestStandardCustomCommittedPlaced([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test standard, custom, committed, placed\n");

        const D3D12_HEAP_TYPE heapType = D3D12_HEAP_TYPE_DEFAULT;
        const ulong bufferSize = 1024;

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();
        poolDesc.HeapProperties.Type = heapType;
        poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        List<ComPtr<D3D12MA_Allocation>> allocations = new List<ComPtr<D3D12MA_Allocation>>();

        D3D12MA_TotalStatistics statsBeg = new D3D12MA_TotalStatistics();
        D3D12MA_DetailedStatistics poolStatInfoBeg = new D3D12MA_DetailedStatistics();

        ctx.allocator->CalculateStatistics(&statsBeg);
        pool.Get()->CalculateStatistics(&poolStatInfoBeg);

        nuint poolAllocCount = 0;
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, bufferSize);

        for (uint standardCustomI = 0; standardCustomI < 2; ++standardCustomI)
        {
            bool useCustomPool = standardCustomI > 0;
            for (uint flagsI = 0; flagsI < 3; ++flagsI)
            {
                bool useCommitted = flagsI > 0;
                bool neverAllocate = flagsI > 1;

                D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC();
                if (useCustomPool)
                {
                    allocDesc.CustomPool = pool.Get();
                    allocDesc.HeapType = unchecked((D3D12_HEAP_TYPE)(0xCDCDCDCD)); // Should be ignored.
                    allocDesc.ExtraHeapFlags = unchecked((D3D12_HEAP_FLAGS)(0xCDCDCDCD)); // Should be ignored.
                }
                else
                {
                    allocDesc.HeapType = heapType;
                }

                if (useCommitted)
                {
                    allocDesc.Flags |= D3D12MA_ALLOCATION_FLAG_COMMITTED;
                }

                if (neverAllocate)
                {
                    allocDesc.Flags |= D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE;
                }

                using ComPtr<D3D12MA_Allocation> allocPtr = new ComPtr<D3D12MA_Allocation>();

                HRESULT hr = ctx.allocator->CreateResource(
                    &allocDesc,
                    &resDesc,
                    D3D12_RESOURCE_STATE_COMMON,
                    null,                       // pOptimizedClearValue
                    (D3D12MA_Allocation**)(&allocPtr),
                    IID_NULL,
                    null
                );
                CHECK_BOOL(SUCCEEDED(hr) == (allocPtr.Get() != null));

                if (allocPtr.Get() != null)
                {
                    allocations.Add(new ComPtr<D3D12MA_Allocation>(allocPtr));

                    if (useCustomPool)
                    {
                        ++poolAllocCount;
                    }
                }

                bool expectSuccess = !neverAllocate; // NEVER_ALLOCATE should always fail with COMMITTED.
                CHECK_BOOL(expectSuccess == SUCCEEDED(hr));

                if (SUCCEEDED(hr) && useCommitted)
                {
                    CHECK_BOOL(allocPtr.Get()->GetHeap() == null); // Committed allocation has implicit heap.
                }
            }
        }

        D3D12MA_TotalStatistics statsEnd = new D3D12MA_TotalStatistics();
        D3D12MA_DetailedStatistics poolStatInfoEnd = new D3D12MA_DetailedStatistics();
        ctx.allocator->CalculateStatistics(&statsEnd);
        pool.Get()->CalculateStatistics(&poolStatInfoEnd);

        CHECK_BOOL(statsEnd.Total.Stats.AllocationCount == (statsBeg.Total.Stats.AllocationCount + (uint)(allocations.Count)));
        CHECK_BOOL(statsEnd.Total.Stats.AllocationBytes >= (statsBeg.Total.Stats.AllocationBytes + ((uint)(allocations.Count) * bufferSize)));
        CHECK_BOOL(statsEnd.HeapType[0].Stats.AllocationCount == (statsBeg.HeapType[0].Stats.AllocationCount + (uint)(allocations.Count)));
        CHECK_BOOL(statsEnd.HeapType[0].Stats.AllocationBytes >= (statsBeg.HeapType[0].Stats.AllocationBytes + ((uint)(allocations.Count) * bufferSize)));
        CHECK_BOOL(poolStatInfoEnd.Stats.AllocationCount == (poolStatInfoBeg.Stats.AllocationCount + poolAllocCount));
        CHECK_BOOL(poolStatInfoEnd.Stats.AllocationBytes >= (poolStatInfoBeg.Stats.AllocationBytes + (poolAllocCount * bufferSize)));

        allocations.Dispose();
    }

    internal static void TestAliasingMemory([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test aliasing memory\n");

        D3D12_RESOURCE_DESC resDesc1 = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = 0,
            Width = 1920,
            Height = 1080,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
        };

        D3D12_RESOURCE_DESC resDesc2 = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = 0,
            Width = 1024,
            Height = 1024,
            DepthOrArraySize = 1,
            MipLevels = 0,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET,
        };

        D3D12_RESOURCE_ALLOCATION_INFO allocInfo1 =
            ctx.device->GetResourceAllocationInfo(0, 1, &resDesc1);
        D3D12_RESOURCE_ALLOCATION_INFO allocInfo2 =
            ctx.device->GetResourceAllocationInfo(0, 1, &resDesc2);

        D3D12_RESOURCE_ALLOCATION_INFO finalAllocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
            Alignment = Math.Max(allocInfo1.Alignment, allocInfo2.Alignment),
            SizeInBytes = Math.Max(allocInfo1.SizeInBytes, allocInfo2.SizeInBytes),
        };

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
            ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES,
        };

        using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();

        CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &finalAllocInfo, (D3D12MA_Allocation**)(&alloc)));
        CHECK_BOOL((alloc.Get() != null) && (alloc.Get()->GetHeap() != null));

        using ComPtr<ID3D12Resource> res1 = new ComPtr<ID3D12Resource>();

        CHECK_HR(ctx.allocator->CreateAliasingResource(
            alloc.Get(),
            0,                              // AllocationLocalOffset
            &resDesc1,
            D3D12_RESOURCE_STATE_COMMON,
            null,                           // pOptimizedClearValue
            __uuidof<ID3D12Resource>(),
            (void**)(&res1)
        ));
        CHECK_BOOL(res1.Get() != null);

        using ComPtr<ID3D12Resource> res2 = new ComPtr<ID3D12Resource>();

        CHECK_HR(ctx.allocator->CreateAliasingResource(
            alloc.Get(),
            0,                              // AllocationLocalOffset
            &resDesc2,
            D3D12_RESOURCE_STATE_COMMON,
            null,                           // pOptimizedClearValue
            __uuidof<ID3D12Resource>(),
            (void**)(&res2)
        ));
        CHECK_BOOL(res2.Get() != null);

        // You can use res1 and res2, but not at the same time!
    }

    internal static void TestAliasingImplicitCommitted([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test aliasing implicit dedicated\n");

        // The buffer will be large enough to be allocated as committed.
        // We still need it to have an explicit heap to be able to alias.

        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, 300 * MEGABYTE);

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
            Flags = D3D12MA_ALLOCATION_FLAG_CAN_ALIAS,
        };

        using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
        CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));
        CHECK_BOOL((alloc.Get() != null) && (alloc.Get()->GetHeap() != null));

        resDesc.Width = 200 * MEGABYTE;
        using ComPtr<ID3D12Resource> aliasingRes = new ComPtr<ID3D12Resource>();

        CHECK_HR(ctx.allocator->CreateAliasingResource(
            alloc.Get(),
            0,                                  // AllocationLocalOffset
            &resDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null,
            __uuidof<ID3D12Resource>(),
            (void**)(&aliasingRes)
        ));
        CHECK_BOOL(aliasingRes.Get() != null);
    }

    internal static void TestPoolMsaaTextureAsCommitted([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test MSAA texture always as committed in pool\n");

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES,
        };

        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.Flags = D3D12MA_POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };

        D3D12_RESOURCE_DESC resDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Width = 1024,
            Height = 512,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 2,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET,
        };

        using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
        CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_RENDER_TARGET, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

        // Committed allocation should not have explicit heap
        CHECK_BOOL(alloc.Get()->GetHeap() == null);
    }

    internal static void TestMapping([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test mapping\n");

        const uint count = 10;
        const ulong bufSize = 32ul * 1024;

        ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)(count)];

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, bufSize);

        for (uint i = 0; i < count; ++i)
        {
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&resources[i].allocation), __uuidof<ID3D12Resource>(), (void**)(&resources[i].resource)));

            void* mappedPtr = null;
            CHECK_HR(resources[i].resource.Get()->Map(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))), &mappedPtr));

            FillData(mappedPtr, bufSize, i);

            // Unmap every other buffer. Leave others mapped.
            if ((i % 2) != 0)
            {
                resources[i].resource.Get()->Unmap(0, null);
            }
        }

        for (uint i = 0; i < count; i++)
        {
            resources[i].Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool StatisticsEqual([NativeTypeName("const D3D12MA::DetailedStatistics &")] in D3D12MA_DetailedStatistics lhs, [NativeTypeName("const D3D12MA::DetailedStatistics &")] in D3D12MA_DetailedStatistics rhs)
    {
        return memcmp(Unsafe.AsPointer(ref Unsafe.AsRef(in lhs)), Unsafe.AsPointer(ref Unsafe.AsRef(in rhs)), __sizeof<D3D12MA_DetailedStatistics>()) == 0;
    }

    internal static void CheckStatistics([NativeTypeName("const D3D12MA::DetailedStatistics &")] in D3D12MA_DetailedStatistics stats)
    {
        CHECK_BOOL(stats.Stats.AllocationBytes <= stats.Stats.BlockBytes);

        if (stats.Stats.AllocationBytes > 0)
        {
            CHECK_BOOL(stats.Stats.AllocationCount > 0);
            CHECK_BOOL(stats.AllocationSizeMin <= stats.AllocationSizeMax);
        }

        if (stats.UnusedRangeCount > 0)
        {
            CHECK_BOOL(stats.UnusedRangeSizeMax > 0);
            CHECK_BOOL(stats.UnusedRangeSizeMin <= stats.UnusedRangeSizeMax);
        }
    }

    internal static void TestStats([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test stats\n");

        D3D12MA_TotalStatistics begStats = new D3D12MA_TotalStatistics();
        ctx.allocator->CalculateStatistics(&begStats);

        const uint count = 10;
        const ulong bufSize = 64ul * 1024;

        ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)(count)];

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, bufSize);

        for (uint i = 0; i < count; ++i)
        {
            if (i == count / 2)
            {
                allocDesc.Flags |= D3D12MA_ALLOCATION_FLAG_COMMITTED;
            }

            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&resources[i].allocation), __uuidof<ID3D12Resource>(), (void**)(&resources[i].resource)));
        }

        D3D12MA_TotalStatistics endStats = new D3D12MA_TotalStatistics();
        ctx.allocator->CalculateStatistics(&endStats);

        CHECK_BOOL(endStats.Total.Stats.BlockCount >= begStats.Total.Stats.BlockCount);
        CHECK_BOOL(endStats.Total.Stats.AllocationCount == begStats.Total.Stats.AllocationCount + count);
        CHECK_BOOL(endStats.Total.Stats.AllocationBytes == begStats.Total.Stats.AllocationBytes + (count * bufSize));
        CHECK_BOOL(endStats.Total.AllocationSizeMin <= bufSize);
        CHECK_BOOL(endStats.Total.AllocationSizeMax >= bufSize);

        CHECK_BOOL(endStats.HeapType[1].Stats.BlockCount >= begStats.HeapType[1].Stats.BlockCount);
        CHECK_BOOL(endStats.HeapType[1].Stats.AllocationCount >= begStats.HeapType[1].Stats.AllocationCount + count);
        CHECK_BOOL(endStats.HeapType[1].Stats.AllocationBytes >= begStats.HeapType[1].Stats.AllocationBytes + (count * bufSize));
        CHECK_BOOL(endStats.HeapType[1].AllocationSizeMin <= bufSize);
        CHECK_BOOL(endStats.HeapType[1].AllocationSizeMax >= bufSize);

        CHECK_BOOL(StatisticsEqual(begStats.HeapType[0], endStats.HeapType[0]));
        CHECK_BOOL(StatisticsEqual(begStats.HeapType[2], endStats.HeapType[2]));

        CheckStatistics(endStats.Total);
        CheckStatistics(endStats.HeapType[0]);
        CheckStatistics(endStats.HeapType[1]);
        CheckStatistics(endStats.HeapType[2]);

        D3D12MA_Budget localBudget = new D3D12MA_Budget();
        D3D12MA_Budget nonLocalBudget = new D3D12MA_Budget();

        ctx.allocator->GetBudget(&localBudget, &nonLocalBudget);

        CHECK_BOOL(localBudget.Stats.AllocationBytes <= localBudget.Stats.BlockBytes);
        CHECK_BOOL(endStats.HeapType[3].Stats.BlockCount == 0); // No allocation from D3D12_HEAP_TYPE_CUSTOM in this test.

        if (!ctx.allocator->IsUMA())
        {
            // Discrete GPU
            CHECK_BOOL(localBudget.Stats.AllocationBytes == endStats.HeapType[0].Stats.AllocationBytes);
            CHECK_BOOL(localBudget.Stats.BlockBytes == endStats.HeapType[0].Stats.BlockBytes);

            CHECK_BOOL(nonLocalBudget.Stats.AllocationBytes <= nonLocalBudget.Stats.BlockBytes);
            CHECK_BOOL(nonLocalBudget.Stats.AllocationBytes == (endStats.HeapType[1].Stats.AllocationBytes + endStats.HeapType[2].Stats.AllocationBytes));
            CHECK_BOOL(nonLocalBudget.Stats.BlockBytes == (endStats.HeapType[1].Stats.BlockBytes + endStats.HeapType[2].Stats.BlockBytes));
        }
        else
        {
            // Integrated GPU - all memory is local
            CHECK_BOOL(localBudget.Stats.AllocationBytes == (endStats.HeapType[0].Stats.AllocationBytes + endStats.HeapType[1].Stats.AllocationBytes + endStats.HeapType[2].Stats.AllocationBytes));
            CHECK_BOOL(localBudget.Stats.BlockBytes == (endStats.HeapType[0].Stats.BlockBytes + endStats.HeapType[1].Stats.BlockBytes + endStats.HeapType[2].Stats.BlockBytes));

            CHECK_BOOL(nonLocalBudget.Stats.AllocationBytes == 0);
            CHECK_BOOL(nonLocalBudget.Stats.BlockBytes == 0);
        }

        for (uint i = 0; i < count; i++)
        {
            resources[i].Dispose();
        }
    }

    internal static void TestTransfer([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test mapping\n");

        const uint count = 10;
        const ulong bufSize = 32ul * 1024;

        ResourceWithAllocation* resourcesUpload = stackalloc ResourceWithAllocation[(int)(count)];
        ResourceWithAllocation* resourcesDefault = stackalloc ResourceWithAllocation[(int)(count)];
        ResourceWithAllocation* resourcesReadback = stackalloc ResourceWithAllocation[(int)(count)];

        D3D12MA_ALLOCATION_DESC allocDescUpload = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
        };

        D3D12MA_ALLOCATION_DESC allocDescDefault = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
        };

        D3D12MA_ALLOCATION_DESC allocDescReadback = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_READBACK,
        };

        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, bufSize);

        // Create 3 sets of resources.
        for (uint i = 0; i < count; ++i)
        {
            CHECK_HR(ctx.allocator->CreateResource(&allocDescUpload, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&resourcesUpload[i].allocation), __uuidof<ID3D12Resource>(), (void**)(&resourcesUpload[i].resource)));
            CHECK_HR(ctx.allocator->CreateResource(&allocDescDefault, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&resourcesDefault[i].allocation), __uuidof<ID3D12Resource>(), (void**)(&resourcesDefault[i].resource)));
            CHECK_HR(ctx.allocator->CreateResource(&allocDescReadback, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&resourcesReadback[i].allocation), __uuidof<ID3D12Resource>(), (void**)(&resourcesReadback[i].resource)));
        }

        // Map and fill data in UPLOAD.
        for (uint i = 0; i < count; ++i)
        {
            void* mappedPtr = null;
            CHECK_HR(resourcesUpload[i].resource.Get()->Map(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))), &mappedPtr));

            FillData(mappedPtr, bufSize, i);

            // Unmap every other resource, leave others mapped.
            if ((i % 2) != 0)
            {
                resourcesUpload[i].resource.Get()->Unmap(0, null);
            }
        }

        // Transfer from UPLOAD to DEFAULT, from there to READBACK.
        ID3D12GraphicsCommandList* cmdList = BeginCommandList();

        for (uint i = 0; i < count; ++i)
        {
            cmdList->CopyBufferRegion(resourcesDefault[i].resource.Get(), 0, resourcesUpload[i].resource.Get(), 0, bufSize);
        }

        D3D12_RESOURCE_BARRIER* barriers = stackalloc D3D12_RESOURCE_BARRIER[(int)(count)];

        for (uint i = 0; i < count; i++)
        {
            barriers[i] = new D3D12_RESOURCE_BARRIER();
        }

        for (uint i = 0; i < count; ++i)
        {
            barriers[i].Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            barriers[i].Transition.pResource = resourcesDefault[i].resource.Get();
            barriers[i].Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
            barriers[i].Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_SOURCE;
            barriers[i].Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
        }

        cmdList->ResourceBarrier(count, barriers);

        for (uint i = 0; i < count; ++i)
        {
            cmdList->CopyBufferRegion(resourcesReadback[i].resource.Get(), 0, resourcesDefault[i].resource.Get(), 0, bufSize);
        }

        EndCommandList(cmdList);

        // Validate READBACK buffers.
        for (uint i = count; i-- != 0;)
        {
            D3D12_RANGE mapRange = new D3D12_RANGE {
                Begin = 0,
                End = (nuint)(bufSize),
            };

            void* mappedPtr = null;
            CHECK_HR(resourcesReadback[i].resource.Get()->Map(0, &mapRange, &mappedPtr));

            CHECK_BOOL(ValidateData(mappedPtr, bufSize, i));

            // Unmap every 3rd resource, leave others mapped.
            if ((i % 3) != 0)
            {
                resourcesReadback[i].resource.Get()->Unmap(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))));
            }
        }

        for (uint i = 0; i < count; i++)
        {
            resourcesReadback[i].Dispose();
        }

        for (uint i = 0; i < count; i++)
        {
            resourcesDefault[i].Dispose();
        }

        for (uint i = 0; i < count; i++)
        {
            resourcesUpload[i].Dispose();
        }
    }

    internal static void TestZeroInitialized([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test zero initialized\n");

        const ulong bufSize = 128ul * 1024;
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, bufSize);

        // # Create upload buffer and fill it with data.

        D3D12MA_ALLOCATION_DESC allocDescUpload = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
        };

        using ResourceWithAllocation bufUpload = new ResourceWithAllocation();
        CHECK_HR(ctx.allocator->CreateResource(&allocDescUpload, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&bufUpload.allocation), __uuidof<ID3D12Resource>(), (void**)(&bufUpload.resource)));

        {
            void* mappedPtr = null;
            CHECK_HR(bufUpload.resource.Get()->Map(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))), &mappedPtr));

            FillData(mappedPtr, bufSize, 5236245);
            bufUpload.resource.Get()->Unmap(0, null);
        }

        // # Create readback buffer

        D3D12MA_ALLOCATION_DESC allocDescReadback = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_READBACK,
        };

        using ResourceWithAllocation bufReadback = new ResourceWithAllocation();
        CHECK_HR(ctx.allocator->CreateResource(&allocDescReadback, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&bufReadback.allocation), __uuidof<ID3D12Resource>(), (void**)(&bufReadback.resource)));

        static void CheckBufferData([NativeTypeName("const ResourceWithAllocation &")] in ResourceWithAllocation buf, in ResourceWithAllocation bufReadback)
        {
            bool shouldBeZero = buf.allocation.Get()->WasZeroInitialized() != FALSE;

            {
                ID3D12GraphicsCommandList* cmdList = BeginCommandList();
                cmdList->CopyBufferRegion(bufReadback.resource.Get(), 0, buf.resource.Get(), 0, bufSize);
                EndCommandList(cmdList);
            }

            bool isZero = false;
            {
                // I could pass pReadRange = NULL but it generates D3D Debug layer warning: EXECUTION WARNING #930: MAP_INVALID_NULLRANGE

                D3D12_RANGE readRange = new D3D12_RANGE {
                    Begin = 0,
                    End = (nuint)(bufSize),
                };

                void* mappedPtr = null;
                CHECK_HR(bufReadback.resource.Get()->Map(0, &readRange, &mappedPtr));

                isZero = ValidateDataZero(mappedPtr, bufSize);
                bufReadback.resource.Get()->Unmap(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))));
            }

            _ = wprintf("Should be zero: {0}, is zero: {1}\n", shouldBeZero ? 1 : 0, isZero ? 1 : 0);

            if (shouldBeZero)
            {
                CHECK_BOOL(isZero);
            }
        }

        // # Test 1: Committed resource. Should always be zero initialized.
        using ResourceWithAllocation bufDefault = new ResourceWithAllocation();

        {
            D3D12MA_ALLOCATION_DESC allocDescDefault = new D3D12MA_ALLOCATION_DESC {
                HeapType = D3D12_HEAP_TYPE_DEFAULT,
                Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
            };
            CHECK_HR(ctx.allocator->CreateResource(&allocDescDefault, &resourceDesc, D3D12_RESOURCE_STATE_COPY_SOURCE, null, (D3D12MA_Allocation**)(&bufDefault.allocation), __uuidof<ID3D12Resource>(), (void**)(&bufDefault.resource)));

            _ = wprintf("  Committed: ");
            CheckBufferData(bufDefault, bufReadback);
            CHECK_BOOL(bufDefault.allocation.Get()->WasZeroInitialized());

            bufDefault.Dispose();
        }

        // # Test 2: (Probably) placed resource.

        for (uint i = 0; i < 2; ++i)
        {
            // 1. Create buffer

            D3D12MA_ALLOCATION_DESC allocDescDefault = new D3D12MA_ALLOCATION_DESC {
                HeapType = D3D12_HEAP_TYPE_DEFAULT,
            };
            CHECK_HR(ctx.allocator->CreateResource(&allocDescDefault, &resourceDesc, D3D12_RESOURCE_STATE_COPY_SOURCE, null, (D3D12MA_Allocation**)(&bufDefault.allocation), __uuidof<ID3D12Resource>(), (void**)(&bufDefault.resource)));

            // 2. Check it

            _ = wprintf("  Normal #{0}: ", i);
            CheckBufferData(bufDefault, bufReadback);

            // 3. Upload some data to it

            {
                ID3D12GraphicsCommandList* cmdList = BeginCommandList();

                D3D12_RESOURCE_BARRIER barrier = new D3D12_RESOURCE_BARRIER {
                    Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
                };
                barrier.Transition.pResource = bufDefault.resource.Get();
                barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_SOURCE;
                barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_DEST;
                barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
                cmdList->ResourceBarrier(1, &barrier);

                cmdList->CopyBufferRegion(bufDefault.resource.Get(), 0, bufUpload.resource.Get(), 0, bufSize);

                EndCommandList(cmdList);
            }

            // 4. Delete it

            bufDefault.Reset();
        }
    }

    internal static void TestMultithreading([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test multithreading\n");

        const uint threadCount = 32;
        const uint bufSizeMin = 1024u;
        const uint bufSizeMax = 1024u * 1024;

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
        };

        // Launch threads.
        Thread[] threads = new Thread[threadCount];

        for (uint threadIndex = 0; threadIndex < threadCount; ++threadIndex)
        {
            static void threadFunc(object? obj)
            {
                (nuint arg0, uint arg1, nuint arg2) = (ValueTuple<nuint, uint, nuint>)(obj!);

                ref readonly TestContext ctx = ref *(TestContext*)(arg0);
                uint threadIndex = arg1;
                D3D12MA_ALLOCATION_DESC* pAllocDesc = (D3D12MA_ALLOCATION_DESC*)(arg2);

                RandomNumberGenerator rand = new RandomNumberGenerator(threadIndex);
                List<ResourceWithAllocation> resources = new List<ResourceWithAllocation>(256);

                // Create starting number of buffers.
                const uint bufToCreateCount = 32;

                for (uint bufIndex = 0; bufIndex < bufToCreateCount; ++bufIndex)
                {
                    using ResourceWithAllocation res = new ResourceWithAllocation {
                        dataSeed = (threadIndex << 16) | bufIndex,
                        size = AlignUp((rand.Generate() % (bufSizeMax - bufSizeMin)) + bufSizeMin, 16),
                    };
                    FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, res.size);

                    CHECK_HR(ctx.allocator->CreateResource(pAllocDesc, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&res.allocation), __uuidof<ID3D12Resource>(), (void**)(&res.resource)));

                    void* mappedPtr = null;
                    CHECK_HR(res.resource.Get()->Map(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))), &mappedPtr));

                    FillData(mappedPtr, res.size, res.dataSeed);

                    // Unmap some of them, leave others mapped.
                    if (rand.GenerateBool())
                    {
                        res.resource.Get()->Unmap(0, null);
                    }

                    resources.Add(res);
                    Unsafe.AsRef(in res) = default;
                }

                Sleep(20);

                // Make a number of random allocate and free operations.
                const uint operationCount = 128;

                for (uint operationIndex = 0; operationIndex < operationCount; ++operationIndex)
                {
                    bool removePossible = resources.Count != 0;
                    bool remove = removePossible && rand.GenerateBool();

                    if (remove)
                    {
                        uint indexToRemove = rand.Generate() % (uint)(resources.Count);
                        resources[(int)(indexToRemove)].Dispose();
                        resources.RemoveAt((int)(indexToRemove));
                    }
                    else // Create new buffer.
                    {
                        using ResourceWithAllocation res = new ResourceWithAllocation {
                            dataSeed = (threadIndex << 16) | operationIndex,
                            size = AlignUp((rand.Generate() % (bufSizeMax - bufSizeMin)) + bufSizeMin, 16),
                        };
                        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, res.size);

                        CHECK_HR(ctx.allocator->CreateResource(pAllocDesc, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&res.allocation), __uuidof<ID3D12Resource>(), (void**)(&res.resource)));

                        void* mappedPtr = null;
                        CHECK_HR(res.resource.Get()->Map(0, null, &mappedPtr));

                        FillData(mappedPtr, res.size, res.dataSeed);

                        // Unmap some of them, leave others mapped.
                        if (rand.GenerateBool())
                        {
                            res.resource.Get()->Unmap(0, null);
                        }

                        resources.Add(res);
                        Unsafe.AsRef(in res) = default;
                    }
                }

                Sleep(20);

                // Validate data in all remaining buffers while deleting them.
                for (nuint resIndex = (uint)(resources.Count); resIndex-- != 0;)
                {
                    void* mappedPtr = null;
                    CHECK_HR(resources[(int)(resIndex)].resource.Get()->Map(0, null, &mappedPtr));

                    _ = ValidateData(mappedPtr, resources[(int)(resIndex)].size, resources[(int)(resIndex)].dataSeed);

                    // Unmap some of them, leave others mapped.
                    if ((resIndex % 3) == 1)
                    {
                        resources[(int)(resIndex)].resource.Get()->Unmap(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))));
                    }

                    resources[(int)(resIndex)].Dispose();
                    resources.RemoveAt((int)(resIndex));
                }

                resources.Dispose();
            }

            Thread thread = new Thread(threadFunc);
            threads[threadIndex] = thread;

            (nuint, uint, nuint) obj = ((nuint)(Unsafe.AsPointer(ref Unsafe.AsRef(in ctx))), threadIndex, (nuint)(&allocDesc));
            thread.Start(obj);
        }

        // Wait for threads to finish.
        for (uint threadIndex = threadCount; threadIndex-- != 0;)
        {
            threads[threadIndex].Join();
        }
    }

    internal static bool IsProtectedResourceSessionSupported([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        D3D12_FEATURE_DATA_PROTECTED_RESOURCE_SESSION_SUPPORT support = new D3D12_FEATURE_DATA_PROTECTED_RESOURCE_SESSION_SUPPORT();
        CHECK_HR(ctx.device->CheckFeatureSupport(D3D12_FEATURE_PROTECTED_RESOURCE_SESSION_SUPPORT, &support, __sizeof<D3D12_FEATURE_DATA_PROTECTED_RESOURCE_SESSION_SUPPORT>()));
        return support.Support > D3D12_PROTECTED_RESOURCE_SESSION_SUPPORT_FLAG_NONE;
    }

    internal static void TestLinearAllocator([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test linear allocator\n");
        RandomNumberGenerator rand = new RandomNumberGenerator(645332);

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
        };
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.Flags = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR;
        poolDesc.BlockSize = 64 * KILOBYTE * 300; // Alignment of buffers is always 64KB
        poolDesc.MinBlockCount = poolDesc.MaxBlockCount = 1;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();

        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC buffDesc, 0);

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };

        const nuint maxBufCount = 100;
        List<BufferInfo> buffInfo = new List<BufferInfo>();

        const ulong bufSizeMin = 16;
        const ulong bufSizeMax = 1024;
        ulong prevOffset = 0;

        // Test one-time free.
        for (nuint i = 0; i < 2; ++i)
        {
            // Allocate number of buffers of varying size that surely fit into this block.
            ulong bufSumSize = 0;
            ulong allocSumSize = 0;

            for (nuint j = 0; j < maxBufCount; ++j)
            {
                buffDesc.Width = AlignUp(bufSizeMin + (rand.Generate() % (bufSizeMax - bufSizeMin)), 16);

                using BufferInfo newBuffInfo = new BufferInfo();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                ulong offset = newBuffInfo.Allocation.Get()->GetOffset();
                CHECK_BOOL(j == 0 || offset > prevOffset);

                prevOffset = offset;
                bufSumSize += buffDesc.Width;
                allocSumSize += newBuffInfo.Allocation.Get()->GetSize();

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;
            }

            // Validate pool stats.
            D3D12MA_DetailedStatistics stats;
            pool.Get()->CalculateStatistics(&stats);

            CHECK_BOOL(stats.Stats.BlockBytes - stats.Stats.AllocationBytes == poolDesc.BlockSize - allocSumSize);
            CHECK_BOOL(allocSumSize >= bufSumSize);
            CHECK_BOOL(stats.Stats.AllocationCount == (uint)(buffInfo.Count));

            // Destroy the buffers in random order.
            while (buffInfo.Count != 0)
            {
                nuint indexToDestroy = rand.Generate() % (uint)(buffInfo.Count);
                buffInfo[(int)(indexToDestroy)].Dispose();
                buffInfo.RemoveAt((int)(indexToDestroy));
            }
        }

        // Test stack.
        {
            // Allocate number of buffers of varying size that surely fit into this block.
            for (nuint i = 0; i < maxBufCount; ++i)
            {
                buffDesc.Width = AlignUp(bufSizeMin + (rand.Generate() % (bufSizeMax - bufSizeMin)), 16);

                using BufferInfo newBuffInfo = new BufferInfo();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                ulong offset = newBuffInfo.Allocation.Get()->GetOffset();
                CHECK_BOOL(i == 0 || offset > prevOffset);

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;

                prevOffset = offset;
            }

            // Destroy few buffers from top of the stack.
            for (nuint i = 0; i < maxBufCount / 5; ++i)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);
            }

            // Create some more
            for (nuint i = 0; i < maxBufCount / 5; ++i)
            {
                buffDesc.Width = AlignUp(bufSizeMin + (rand.Generate() % (bufSizeMax - bufSizeMin)), 16);

                using BufferInfo newBuffInfo = new BufferInfo();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                ulong offset = newBuffInfo.Allocation.Get()->GetOffset();
                CHECK_BOOL(i == 0 || offset > prevOffset);

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;

                prevOffset = offset;
            }

            // Destroy the buffers in reverse order.
            while (buffInfo.Count != 0)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);
            }
        }

        // Test ring buffer.
        {
            // Allocate number of buffers that surely fit into this block.
            buffDesc.Width = bufSizeMax;
            for (nuint i = 0; i < maxBufCount; ++i)
            {
                using BufferInfo newBuffInfo = new BufferInfo();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                ulong offset = newBuffInfo.Allocation.Get()->GetOffset();
                CHECK_BOOL(i == 0 || offset > prevOffset);

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;

                prevOffset = offset;
            }

            // Free and allocate new buffers so many times that we make sure we wrap-around at least once.
            nuint buffersPerIter = (maxBufCount / 10) - 1;
            nuint iterCount = (nuint)(poolDesc.BlockSize / buffDesc.Width / buffersPerIter * 2);

            for (nuint iter = 0; iter < iterCount; ++iter)
            {
                for (nuint i = 0; i < buffersPerIter; i++)
                {
                    buffInfo[(int)(i)].Dispose();
                }
                buffInfo.RemoveRange(0, (int)(buffersPerIter));

                for (nuint bufPerIter = 0; bufPerIter < buffersPerIter; ++bufPerIter)
                {
                    using BufferInfo newBuffInfo = new BufferInfo();
                    CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                    buffInfo.Add(newBuffInfo);
                    Unsafe.AsRef(in newBuffInfo) = default;
                }
            }

            // Allocate buffers until we reach out-of-memory.
            uint debugIndex = 0;

            while (true)
            {
                using BufferInfo newBuffInfo = new BufferInfo();

                HRESULT hr = ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer));
                ++debugIndex;

                if (SUCCEEDED(hr))
                {
                    buffInfo.Add(newBuffInfo);
                    Unsafe.AsRef(in newBuffInfo) = default;
                }
                else
                {
                    CHECK_BOOL(hr == E_OUTOFMEMORY);
                    break;
                }
            }

            // Destroy the buffers in random order.
            while (buffInfo.Count != 0)
            {
                nuint indexToDestroy = rand.Generate() % (uint)(buffInfo.Count);
                buffInfo[(int)(indexToDestroy)].Dispose();
                buffInfo.RemoveAt((int)(indexToDestroy));
            }
        }

        // Test double stack.
        {
            // Allocate number of buffers of varying size that surely fit into this block, alternate from bottom/top.
            ulong prevOffsetLower = 0;
            ulong prevOffsetUpper = poolDesc.BlockSize;

            for (nuint i = 0; i < maxBufCount; ++i)
            {
                bool upperAddress = (i % 2) != 0;

                if (upperAddress)
                {
                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS;
                }
                else
                {
                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NONE;
                }

                buffDesc.Width = AlignUp(bufSizeMin + (rand.Generate() % (bufSizeMax - bufSizeMin)), 16);

                using BufferInfo newBuffInfo = new BufferInfo();

                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));
                ulong offset = newBuffInfo.Allocation.Get()->GetOffset();

                if (upperAddress)
                {
                    CHECK_BOOL(offset < prevOffsetUpper);
                    prevOffsetUpper = offset;
                }
                else
                {
                    CHECK_BOOL(offset >= prevOffsetLower);
                    prevOffsetLower = offset;
                }

                CHECK_BOOL(prevOffsetLower < prevOffsetUpper);

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;
            }

            // Destroy few buffers from top of the stack.
            for (nuint i = 0; i < maxBufCount / 5; ++i)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);
            }

            // Create some more
            for (nuint i = 0; i < maxBufCount / 5; ++i)
            {
                bool upperAddress = (i % 2) != 0;

                if (upperAddress)
                {
                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS;
                }
                else
                {
                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NONE;
                }

                buffDesc.Width = AlignUp(bufSizeMin + (rand.Generate() % (bufSizeMax - bufSizeMin)), 16);

                using BufferInfo newBuffInfo = new BufferInfo();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;
            }

            // Destroy the buffers in reverse order.
            while (buffInfo.Count != 0)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);
            }

            // Create buffers on both sides until we reach out of memory.

            prevOffsetLower = 0;
            prevOffsetUpper = poolDesc.BlockSize;

            for (nuint i = 0; true; ++i)
            {
                bool upperAddress = (i % 2) != 0;

                if (upperAddress)
                {
                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS;
                }
                else
                {
                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NONE;
                }

                buffDesc.Width = AlignUp(bufSizeMin + (rand.Generate() % (bufSizeMax - bufSizeMin)), 16);

                using BufferInfo newBuffInfo = new BufferInfo();
                HRESULT hr = ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer));

                if (SUCCEEDED(hr))
                {
                    ulong offset = newBuffInfo.Allocation.Get()->GetOffset();

                    if (upperAddress)
                    {
                        CHECK_BOOL(offset < prevOffsetUpper);
                        prevOffsetUpper = offset;
                    }
                    else
                    {
                        CHECK_BOOL(offset >= prevOffsetLower);
                        prevOffsetLower = offset;
                    }

                    CHECK_BOOL(prevOffsetLower < prevOffsetUpper);

                    buffInfo.Add(newBuffInfo);
                    Unsafe.AsRef(in newBuffInfo) = default;
                }
                else
                {
                    break;
                }
            }

            // Destroy the buffers in random order.
            while (buffInfo.Count != 0)
            {
                nuint indexToDestroy = rand.Generate() % (uint)(buffInfo.Count);
                buffInfo[(int)(indexToDestroy)].Dispose();
                buffInfo.RemoveAt((int)(indexToDestroy));
            }

            // Create buffers on upper side only, constant size, until we reach out of memory.

            prevOffsetUpper = poolDesc.BlockSize;
            allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS;
            buffDesc.Width = bufSizeMax;

            while (true)
            {
                using BufferInfo newBuffInfo = new BufferInfo();
                HRESULT hr = ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer));

                if (SUCCEEDED(hr))
                {
                    ulong offset = newBuffInfo.Allocation.Get()->GetOffset();
                    CHECK_BOOL(offset < prevOffsetUpper);

                    prevOffsetUpper = offset;

                    buffInfo.Add(newBuffInfo);
                    Unsafe.AsRef(in newBuffInfo) = default;
                }
                else
                {
                    break;
                }
            }

            // Destroy the buffers in reverse order.
            while (buffInfo.Count != 0)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);
            }
        }

        buffInfo.Dispose();
    }

    internal static void TestLinearAllocatorMultiBlock([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test linear allocator multi block\n");

        RandomNumberGenerator rand = new RandomNumberGenerator(345673);

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
        };
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.Flags = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();

        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC buffDesc, 1024 * 1024);

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };

        List<BufferInfo> buffInfo = new List<BufferInfo>();

        // Test one-time free.
        {
            // Allocate buffers until we move to a second block.
            ID3D12Heap* lastHeap = null;
            while (true)
            {
                using BufferInfo newBuffInfo = new BufferInfo();
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                ID3D12Heap* heap = newBuffInfo.Allocation.Get()->GetHeap();

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;

                if ((lastHeap != null) && (heap != lastHeap))
                {
                    break;
                }
                lastHeap = heap;
            }
            CHECK_BOOL(buffInfo.Count > 2);

            // Make sure that pool has now two blocks.

            D3D12MA_DetailedStatistics poolStats = new D3D12MA_DetailedStatistics();
            pool.Get()->CalculateStatistics(&poolStats);
            CHECK_BOOL(poolStats.Stats.BlockCount == 2);

            // Destroy all the buffers in random order.
            while (buffInfo.Count != 0)
            {
                nuint indexToDestroy = rand.Generate() % (uint)(buffInfo.Count);
                buffInfo[(int)(indexToDestroy)].Dispose();
                buffInfo.RemoveAt((int)(indexToDestroy));
            }

            // Make sure that pool has now at most one block.
            pool.Get()->CalculateStatistics(&poolStats);
            CHECK_BOOL(poolStats.Stats.BlockCount <= 1);
        }

        // Test stack.
        {
            using BufferInfo newBuffInfo = new BufferInfo();

            // Allocate buffers until we move to a second block.
            ID3D12Heap* lastHeap = null;

            while (true)
            {
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

                ID3D12Heap* heap = newBuffInfo.Allocation.Get()->GetHeap();

                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;

                if ((lastHeap != null) && (heap != lastHeap))
                {
                    break;
                }
                lastHeap = heap;
            }
            CHECK_BOOL(buffInfo.Count > 2);

            // Add few more buffers.
            for (uint i = 0; i < 5; ++i)
            {
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));
                buffInfo.Add(newBuffInfo);
                Unsafe.AsRef(in newBuffInfo) = default;
            }

            // Make sure that pool has now two blocks.
            D3D12MA_DetailedStatistics poolStats = new D3D12MA_DetailedStatistics();
            pool.Get()->CalculateStatistics(&poolStats);
            CHECK_BOOL(poolStats.Stats.BlockCount == 2);

            // Delete half of buffers, LIFO.
            for (nuint i = 0, countToDelete = (uint)(buffInfo.Count) / 2; i < countToDelete; ++i)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);;
            }

            // Add one more buffer.
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));
            buffInfo.Add(newBuffInfo);
            Unsafe.AsRef(in newBuffInfo) = default;

            // Make sure that pool has now one block.
            pool.Get()->CalculateStatistics(&poolStats);
            CHECK_BOOL(poolStats.Stats.BlockCount == 1);

            // Delete all the remaining buffers, LIFO.
            while (buffInfo.Count != 0)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);;
            }
        }

        buffInfo.Dispose();
    }

    internal static void ManuallyTestLinearAllocator([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Manually test linear allocator\n");

        RandomNumberGenerator rand = new RandomNumberGenerator(645332);

        D3D12MA_TotalStatistics origStats;
        ctx.allocator->CalculateStatistics(&origStats);

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
        };
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.Flags = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR;
        poolDesc.BlockSize = 6 * 64 * KILOBYTE;
        poolDesc.MinBlockCount = poolDesc.MaxBlockCount = 1;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();

        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC buffDesc, 0);

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };

        List<BufferInfo> buffInfo = new List<BufferInfo>();
        using BufferInfo newBuffInfo = new BufferInfo();

        // Test double stack.
        {
            /*
            Lower: Buffer 32 B, Buffer 1024 B, Buffer 32 B
            Upper: Buffer 16 B, Buffer 1024 B, Buffer 128 B
            Totally:
            1 block allocated
            393 216 DirectX 12 bytes
            6 new allocations
            2256 bytes in allocations (384 KB according to alignment)
            */

            buffDesc.Width = 32;
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

            buffInfo.Add(newBuffInfo);
            Unsafe.AsRef(in newBuffInfo) = default;

            buffDesc.Width = 1024;
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

            buffInfo.Add(newBuffInfo);
            Unsafe.AsRef(in newBuffInfo) = default;

            buffDesc.Width = 32;
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

            buffInfo.Add(newBuffInfo);
            Unsafe.AsRef(in newBuffInfo) = default;

            allocDesc.Flags |= D3D12MA_ALLOCATION_FLAG_UPPER_ADDRESS;

            buffDesc.Width = 128;
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

            buffInfo.Add(newBuffInfo);
            Unsafe.AsRef(in newBuffInfo) = default;

            buffDesc.Width = 1024;
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

            buffInfo.Add(newBuffInfo);
            Unsafe.AsRef(in newBuffInfo) = default;

            buffDesc.Width = 16;
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &buffDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&newBuffInfo.Allocation), __uuidof<ID3D12Resource>(), (void**)(&newBuffInfo.Buffer)));

            buffInfo.Add(newBuffInfo);
            Unsafe.AsRef(in newBuffInfo) = default;

            D3D12MA_TotalStatistics currStats;
            ctx.allocator->CalculateStatistics(&currStats);

            D3D12MA_DetailedStatistics poolStats;
            pool.Get()->CalculateStatistics(&poolStats);

            char* statsStr = null;
            ctx.allocator->BuildStatsString(&statsStr, FALSE);

            // PUT BREAKPOINT HERE TO CHECK.
            // Inspect: currStats versus origStats, poolStats, statsStr.

            ctx.allocator->FreeStatsString(statsStr);

            // Destroy the buffers in reverse order.
            while (buffInfo.Count != 0)
            {
                buffInfo[^1].Dispose();
                buffInfo.RemoveAt(buffInfo.Count - 1);;
            }
        }

        buffInfo.Dispose();
    }

    internal static void BenchmarkAlgorithmsCase([NativeTypeName("const TestContext &")] in TestContext ctx, TextWriter? file, D3D12MA_POOL_FLAGS algorithm, bool empty, FREE_ORDER freeOrder)
    {
        RandomNumberGenerator rand = new RandomNumberGenerator(16223);

        const ulong bufSize = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
        const nuint maxBufCapacity = 10000;
        const uint iterationCount = 10;

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
        };

        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.BlockSize = bufSize * maxBufCapacity;
        poolDesc.Flags |= algorithm;
        poolDesc.MinBlockCount = poolDesc.MaxBlockCount = 1;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        D3D12_RESOURCE_ALLOCATION_INFO allocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
            SizeInBytes = bufSize,
        };

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };

        List<ComPtr<D3D12MA_Allocation>> baseAllocations = new List<ComPtr<D3D12MA_Allocation>>();
        nuint allocCount = maxBufCapacity / 3;

        if (!empty)
        {
            // Make allocations up to 1/3 of pool size.
            for (ulong i = 0; i < allocCount; ++i)
            {
                using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
                CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &allocInfo, (D3D12MA_Allocation**)(&alloc)));

                baseAllocations.Add(alloc);
                _ = alloc.Detach();
            }

            // Delete half of them, choose randomly.
            nuint allocsToDelete = (nuint)(baseAllocations.Count) / 2;

            for (nuint i = 0; i < allocsToDelete; ++i)
            {
                nuint index = (nuint)(rand.Generate()) / (uint)(baseAllocations.Count);
                baseAllocations[(int)(index)].Dispose();
                baseAllocations.RemoveAt((int)(index));
            }
        }

        // BENCHMARK
        List<ComPtr<D3D12MA_Allocation>> testAllocations = new List<ComPtr<D3D12MA_Allocation>>();

        TimeSpan allocTotalDuration = TimeSpan.Zero;
        TimeSpan freeTotalDuration = TimeSpan.Zero;

        for (uint iterationIndex = 0; iterationIndex < iterationCount; ++iterationIndex)
        {
            testAllocations.Capacity = (int)(allocCount);

            // Allocations
            long allocTimeBeg = Stopwatch.GetTimestamp();

            for (nuint i = 0; i < allocCount; ++i)
            {
                using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
                CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &allocInfo, (D3D12MA_Allocation**)(&alloc)));

                testAllocations.Add(alloc);
                _ = alloc.Detach();
            }

            allocTotalDuration += new TimeSpan((long)((Stopwatch.GetTimestamp() - allocTimeBeg) * ((double)(TimeSpan.TicksPerSecond) / Stopwatch.Frequency)));

            // Deallocations
            switch (freeOrder)
            {
                case FREE_ORDER.FORWARD:
                {
                    // Leave testAllocations unchanged.
                    break;
                }

                case FREE_ORDER.BACKWARD:
                {
                    testAllocations.Reverse();
                    break;
                }

                case FREE_ORDER.RANDOM:
                {
                    Span<ComPtr<D3D12MA_Allocation>> tmp = CollectionsMarshal.AsSpan(testAllocations);
                    MyUniformRandomNumberGenerator rng = new MyUniformRandomNumberGenerator(ref rand);

                    for (uint i = (uint)(testAllocations.Count) - 1; i > 0; --i)
                    {
                        D3D12MA_SWAP(ref tmp[(int)(i)], ref tmp[(int)(rng.Invoke() % (i + 1))]);
                    }
                    break;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }
            }

            long freeTimeBeg = Stopwatch.GetTimestamp();

            testAllocations.Dispose();
            testAllocations.Clear();

            freeTotalDuration += new TimeSpan((long)((Stopwatch.GetTimestamp() - freeTimeBeg) * ((double)(TimeSpan.TicksPerSecond) / Stopwatch.Frequency)));
        }

        // Delete baseAllocations
        baseAllocations.Dispose();
        baseAllocations.Clear();

        float allocTotalSeconds = ToFloatSeconds(allocTotalDuration);
        float freeTotalSeconds = ToFloatSeconds(freeTotalDuration);

        _ = printf("    Algorithm={0} {1} FreeOrder={2}: allocations {3} s, free {4} s\n", AlgorithmToStr(algorithm), empty ? "Empty" : "Not empty", FREE_ORDER_NAMES[(nuint)(freeOrder)], allocTotalSeconds, freeTotalSeconds);

        if (file != null)
        {
            CurrentTimeToStr(out string currTime);
            _ = fprintf(file, "{0},{1},{2},{3},{4},{5},{6}\n", CODE_DESCRIPTION, currTime, AlgorithmToStr(algorithm), empty ? 1 : 0, FREE_ORDER_NAMES[(uint)(freeOrder)], allocTotalSeconds, freeTotalSeconds);
        }

        testAllocations.Dispose();
        baseAllocations.Dispose();
    }

    internal static void BenchmarkAlgorithms([NativeTypeName("const TestContext &")] in TestContext ctx, TextWriter? file)
    {
        _ = wprintf("Benchmark algorithms\n");

        if (file != null)
        {
            _ = fprintf(file, "Code,Time," + "Algorithm,Empty,Free order," + "Allocation time (s),Deallocation time (s)\n");
        }

        uint freeOrderCount = 1;

        if (ConfigType >= CONFIG_TYPE_LARGE)
        {
            freeOrderCount = 3;
        }
        else if (ConfigType >= CONFIG_TYPE_SMALL)
        {
            freeOrderCount = 2;
        }

        uint emptyCount = (ConfigType >= CONFIG_TYPE_SMALL) ? 2u : 1u;

        for (uint freeOrderIndex = 0; freeOrderIndex < freeOrderCount; ++freeOrderIndex)
        {
            FREE_ORDER freeOrder = FREE_ORDER.COUNT;

            switch (freeOrderIndex)
            {
                case 0:
                {
                    freeOrder = FREE_ORDER.BACKWARD;
                    break;
                }

                case 1:
                {
                    freeOrder = FREE_ORDER.FORWARD;
                    break;
                }

                case 2:
                {
                    freeOrder = FREE_ORDER.RANDOM;
                    break;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }
            }

            for (uint emptyIndex = 0; emptyIndex < emptyCount; ++emptyIndex)
            {
                for (uint algorithmIndex = 0; algorithmIndex < 2; ++algorithmIndex)
                {
                    Unsafe.SkipInit(out D3D12MA_POOL_FLAGS algorithm);

                    switch (algorithmIndex)
                    {
                        case 0:
                        {
                            algorithm = D3D12MA_POOL_FLAG_NONE;
                            break;
                        }
                        
                        case 1:
                        {
                            algorithm = D3D12MA_POOL_FLAG_ALGORITHM_LINEAR;
                            break;
                        }

                        default:
                        {
                            D3D12MA_FAIL();
                            break;
                        }
                    }

                    BenchmarkAlgorithmsCase(
                        ctx,
                        file,
                        algorithm,
                        (emptyIndex == 0), // empty
                        freeOrder
                    );
                }
            }
        }
    }

    [SupportedOSPlatform("windows10.0.19043.0")]
    internal static void TestDevice4([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test ID3D12Device4\n");

        if (!IsProtectedResourceSessionSupported(ctx))
        {
            _ = wprintf("D3D12_FEATURE_PROTECTED_RESOURCE_SESSION_SUPPORT returned no support for protected resource session.\n");
            return;
        }

        using ComPtr<ID3D12Device4> dev4 = new ComPtr<ID3D12Device4>();
        HRESULT hr = ctx.device->QueryInterface(__uuidof<ID3D12Device4>(), (void**)(&dev4));

        if (FAILED(hr))
        {
            _ = wprintf("QueryInterface for ID3D12Device4 FAILED.\n");
            return;
        }

        D3D12_PROTECTED_RESOURCE_SESSION_DESC sessionDesc = new D3D12_PROTECTED_RESOURCE_SESSION_DESC();
        using ComPtr<ID3D12ProtectedResourceSession> session = new ComPtr<ID3D12ProtectedResourceSession>();

        // This fails on the SOFTWARE adapter.
        hr = dev4.Get()->CreateProtectedResourceSession(&sessionDesc, __uuidof<ID3D12ProtectedResourceSession>(), (void**)(&session));

        if (FAILED(hr))
        {
            _ = wprintf("ID3D12Device4::CreateProtectedResourceSession FAILED.\n");
            return;
        }

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC();
        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
        poolDesc.pProtectedSession = session.Get();
        poolDesc.MinAllocationAlignment = 0;
        poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        hr = ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool));

        if (FAILED(hr))
        {
            _ = wprintf("Failed to create custom pool.\n");
            return;
        }

        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resourceDesc, 1024);

        for (uint testIndex = 0; testIndex < 2; ++testIndex)
        {
            // Create a buffer
            D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
                CustomPool = pool.Get(),
            };
            if (testIndex == 0)
            {
                allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;
            }

            using ComPtr<D3D12MA_Allocation> bufAlloc = new ComPtr<D3D12MA_Allocation>();
            using ComPtr<ID3D12Resource> bufRes = new ComPtr<ID3D12Resource>();

            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_COMMON, null, (D3D12MA_Allocation**)(&bufAlloc), __uuidof<ID3D12Resource>(), (void**)(&bufRes)));
            CHECK_BOOL((bufAlloc.Get() != null) && (bufAlloc.Get()->GetResource() == bufRes.Get()));

            // Make sure it's (not) committed.
            CHECK_BOOL(bufAlloc.Get()->GetHeap() == null == (testIndex == 0));

            // Allocate memory/heap
            // Temporarily disabled on NVIDIA as it causes BSOD on RTX2080Ti driver 461.40.
            if (g_AdapterDesc.VendorId != VENDOR_ID_NVIDIA)
            {
                D3D12_RESOURCE_ALLOCATION_INFO heapAllocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
                    SizeInBytes = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT * 2,
                    Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
                };

                using ComPtr<D3D12MA_Allocation> memAlloc = new ComPtr<D3D12MA_Allocation>();
                CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &heapAllocInfo, (D3D12MA_Allocation**)(&memAlloc)));

                CHECK_BOOL(memAlloc.Get()->GetHeap() != null);
            }
        }
    }

    [SupportedOSPlatform("windows10.0.19043.0")]
    internal static void TestDevice8([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test ID3D12Device8\n");

        using ComPtr<ID3D12Device8> dev8 = new ComPtr<ID3D12Device8>();

        CHECK_HR(ctx.device->QueryInterface(__uuidof<ID3D12Device8>(), (void**)(&dev8)));
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC1 resourceDesc, 1024 * 1024);

        // Create a committed buffer

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
            Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
        };

        using ComPtr<D3D12MA_Allocation> allocPtr0 = new ComPtr<D3D12MA_Allocation>();
        using ComPtr<ID3D12Resource> res0 = new ComPtr<ID3D12Resource>();

        CHECK_HR(ctx.allocator->CreateResource2(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_COMMON, null, (D3D12MA_Allocation**)(&allocPtr0), __uuidof<ID3D12Resource>(), (void**)(&res0)));
        CHECK_BOOL(allocPtr0.Get()->GetHeap() == null);

        // Create a placed buffer

        allocDesc.Flags &= ~D3D12MA_ALLOCATION_FLAG_COMMITTED;

        using ComPtr<D3D12MA_Allocation> allocPtr1 = new ComPtr<D3D12MA_Allocation>();
        using ComPtr<ID3D12Resource> res1 = new ComPtr<ID3D12Resource>();

        CHECK_HR(ctx.allocator->CreateResource2(&allocDesc, &resourceDesc, D3D12_RESOURCE_STATE_COMMON, null, (D3D12MA_Allocation**)(&allocPtr1), __uuidof<ID3D12Resource>(), (void**)(&res1)));
        CHECK_BOOL(allocPtr1.Get()->GetHeap() != null);
    }

    internal static void TestVirtualBlocks([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test virtual blocks\n");

        const ulong blockSize = 16 * MEGABYTE;
        const ulong alignment = 256;

        // # Create block 16 MB

        using ComPtr<D3D12MA_VirtualBlock> block = new ComPtr<D3D12MA_VirtualBlock>();

        D3D12MA_VIRTUAL_BLOCK_DESC blockDesc = new D3D12MA_VIRTUAL_BLOCK_DESC {
            pAllocationCallbacks = ctx.allocationCallbacks,
            Size = blockSize,
        };
        CHECK_HR(D3D12MA_CreateVirtualBlock(&blockDesc, (D3D12MA_VirtualBlock**)(&block)));

        CHECK_BOOL(block.Get() != null);

        // # Allocate 8 MB

        D3D12MA_VIRTUAL_ALLOCATION_DESC allocDesc = new D3D12MA_VIRTUAL_ALLOCATION_DESC {
            Alignment = alignment,
            pPrivateData = (void*)(nuint)(1),
            Size = 8 * MEGABYTE,
        };

        D3D12MA_VirtualAllocation alloc0;
        CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc0, null));

        // # Validate the allocation

        D3D12MA_VIRTUAL_ALLOCATION_INFO alloc0Info = new D3D12MA_VIRTUAL_ALLOCATION_INFO();
        block.Get()->GetAllocationInfo(alloc0, &alloc0Info);

        CHECK_BOOL(alloc0Info.Offset < blockSize);
        CHECK_BOOL(alloc0Info.Size == allocDesc.Size);
        CHECK_BOOL(alloc0Info.pPrivateData == allocDesc.pPrivateData);

        // # Check SetUserData

        block.Get()->SetAllocationPrivateData(alloc0, (void*)(nuint)(2));
        block.Get()->GetAllocationInfo(alloc0, &alloc0Info);
        CHECK_BOOL(alloc0Info.pPrivateData == (void*)(nuint)(2));

        // # Allocate 4 MB

        allocDesc.Size = 4 * MEGABYTE;
        allocDesc.Alignment = alignment;

        D3D12MA_VirtualAllocation alloc1;
        CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc1, null));

        D3D12MA_VIRTUAL_ALLOCATION_INFO alloc1Info = new D3D12MA_VIRTUAL_ALLOCATION_INFO();
        block.Get()->GetAllocationInfo(alloc1, &alloc1Info);

        CHECK_BOOL(alloc1Info.Offset < blockSize);
        CHECK_BOOL(alloc1Info.Offset + (4 * MEGABYTE) <= alloc0Info.Offset || alloc0Info.Offset + (8 * MEGABYTE) <= alloc1Info.Offset); // Check if they don't overlap.

        // # Allocate another 8 MB - it should fail

        allocDesc.Size = 8 * MEGABYTE;
        allocDesc.Alignment = alignment;

        D3D12MA_VirtualAllocation alloc2;
        CHECK_BOOL(FAILED(block.Get()->Allocate(&allocDesc, &alloc2, null)));

        CHECK_BOOL(alloc2.AllocHandle == 0);

        // # Free the 4 MB block. Now allocation of 8 MB should succeed.

        block.Get()->FreeAllocation(alloc1);

        ulong alloc2Offset;
        CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc2, &alloc2Offset));

        CHECK_BOOL(alloc2Offset < blockSize);
        CHECK_BOOL(alloc2Offset + (4 * MEGABYTE) <= alloc0Info.Offset || alloc0Info.Offset + (8 * MEGABYTE) <= alloc2Offset); // Check if they don't overlap.

        // # Calculate statistics

        D3D12MA_DetailedStatistics statInfo = new D3D12MA_DetailedStatistics();
        block.Get()->CalculateStatistics(&statInfo);

        CHECK_BOOL(statInfo.Stats.AllocationCount == 2);
        CHECK_BOOL(statInfo.Stats.BlockCount == 1);
        CHECK_BOOL(statInfo.Stats.AllocationBytes == blockSize);
        CHECK_BOOL(statInfo.Stats.BlockBytes == blockSize);

        // # Generate JSON dump

        char* json = null;
        block.Get()->BuildStatsString(&json);
        {
            ReadOnlySpan<char> str = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)(json));
            CHECK_BOOL(str.IndexOf("\"CustomData\": 1") != -1);
            CHECK_BOOL(str.IndexOf("\"CustomData\": 2") != -1);
        }
        block.Get()->FreeStatsString(json);

        // # Free alloc0, leave alloc2 unfreed.

        block.Get()->FreeAllocation(alloc0);

        // # Test alignment

        {
            const nuint allocCount = 10;
            D3D12MA_VirtualAllocation* allocs = stackalloc D3D12MA_VirtualAllocation[(int)(allocCount)];

            for (nuint i = 0; i < allocCount; i++)
            {
                allocs[i] = new D3D12MA_VirtualAllocation();
            }

            for (nuint i = 0; i < allocCount; ++i)
            {
                bool alignment0 = i == allocCount - 1;

                allocDesc.Size = (i * 3) + 15;
                allocDesc.Alignment = alignment0 ? 0ul : 8ul;

                ulong offset;
                CHECK_HR(block.Get()->Allocate(&allocDesc, &allocs[i], &offset));

                if (!alignment0)
                {
                    CHECK_BOOL(offset % allocDesc.Alignment == 0);
                }
            }

            for (nuint i = allocCount; i-- != 0;)
            {
                block.Get()->FreeAllocation(allocs[i]);
            }
        }

        // # Final cleanup

        block.Get()->FreeAllocation(alloc2);
    }

    internal static void TestVirtualBlocksAlgorithms([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test virtual blocks algorithms\n");

        RandomNumberGenerator rand = new RandomNumberGenerator(3454335);

        [return: NativeTypeName("UINT64")]
        ulong calcRandomAllocSize()
        {
            return (rand.Generate() % 20) + 5;
        }

        for (nuint algorithmIndex = 0; algorithmIndex < 2; ++algorithmIndex)
        {
            // Create the block
            D3D12MA_VIRTUAL_BLOCK_DESC blockDesc = new D3D12MA_VIRTUAL_BLOCK_DESC {
                pAllocationCallbacks = ctx.allocationCallbacks,
                Size = 10_000,
            };

            switch (algorithmIndex)
            {
                case 0:
                {
                    blockDesc.Flags = D3D12MA_VIRTUAL_BLOCK_FLAG_NONE;
                    break;
                }
                
                case 1:
                {
                    blockDesc.Flags = D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR;
                    break;
                }
            }

            using ComPtr<D3D12MA_VirtualBlock> block = new ComPtr<D3D12MA_VirtualBlock>();
            CHECK_HR(D3D12MA_CreateVirtualBlock(&blockDesc, (D3D12MA_VirtualBlock**)(&block)));

            List<AllocData> allocations = new List<AllocData>();

            // Make some allocations
            for (nuint i = 0; i < 20; ++i)
            {
                D3D12MA_VIRTUAL_ALLOCATION_DESC allocDesc = new D3D12MA_VIRTUAL_ALLOCATION_DESC {
                    Size = calcRandomAllocSize(),
                };
                allocDesc.pPrivateData = (void*)(nuint)(allocDesc.Size * 10);

                if (i < 10)
                {
                }
                else if (i < 20 && algorithmIndex == 1)
                {
                    allocDesc.Flags = D3D12MA_VIRTUAL_ALLOCATION_FLAG_UPPER_ADDRESS;
                }

                AllocData alloc = new AllocData {
                    requestedSize = allocDesc.Size,
                };
                CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc.allocation, null));

                D3D12MA_VIRTUAL_ALLOCATION_INFO allocInfo;
                block.Get()->GetAllocationInfo(alloc.allocation, &allocInfo);
                CHECK_BOOL(allocInfo.Size >= allocDesc.Size);
                alloc.allocOffset = allocInfo.Offset;
                alloc.allocationSize = allocInfo.Size;

                allocations.Add(alloc);
            }

            // Free some of the allocations
            for (nuint i = 0; i < 5; ++i)
            {
                nuint index = rand.Generate() % (uint)(allocations.Count);
                block.Get()->FreeAllocation(allocations[(int)(index)].allocation);
                allocations.RemoveAt((int)(index));
            }

            // Allocate some more
            for (nuint i = 0; i < 6; ++i)
            {
                D3D12MA_VIRTUAL_ALLOCATION_DESC allocDesc = new D3D12MA_VIRTUAL_ALLOCATION_DESC {
                    Size = calcRandomAllocSize(),
                };
                allocDesc.pPrivateData = (void*)(nuint)(allocDesc.Size * 10);

                AllocData alloc = new AllocData {
                    requestedSize = allocDesc.Size,
                };
                CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc.allocation, null));

                D3D12MA_VIRTUAL_ALLOCATION_INFO allocInfo;
                block.Get()->GetAllocationInfo(alloc.allocation, &allocInfo);
                CHECK_BOOL(allocInfo.Size >= allocDesc.Size);
                alloc.allocOffset = allocInfo.Offset;
                alloc.allocationSize = allocInfo.Size;

                allocations.Add(alloc);
            }

            // Allocate some with extra alignment
            for (nuint i = 0; i < 3; ++i)
            {
                D3D12MA_VIRTUAL_ALLOCATION_DESC allocDesc = new D3D12MA_VIRTUAL_ALLOCATION_DESC {
                    Size = calcRandomAllocSize(),
                    Alignment = 16,
                };
                allocDesc.pPrivateData = (void*)(nuint)(allocDesc.Size * 10);

                AllocData alloc = new AllocData {
                    requestedSize = allocDesc.Size,
                };
                CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc.allocation, null));

                D3D12MA_VIRTUAL_ALLOCATION_INFO allocInfo;
                block.Get()->GetAllocationInfo(alloc.allocation, &allocInfo);
                CHECK_BOOL(allocInfo.Offset % 16 == 0);
                CHECK_BOOL(allocInfo.Size >= allocDesc.Size);
                alloc.allocOffset = allocInfo.Offset;
                alloc.allocationSize = allocInfo.Size;

                allocations.Add(alloc);
            }

            // Check if the allocations don't overlap
            fixed (AllocData* pAllocations = CollectionsMarshal.AsSpan(allocations))
            {
                D3D12MA_SORT(pAllocations, pAllocations + allocations.Count, (lhs, rhs) => lhs.allocOffset.CompareTo(rhs.allocOffset));
            }

            for (nuint i = 0; i < ((uint)(allocations.Count) - 1); ++i)
            {
                CHECK_BOOL(allocations[(int)(i + 1)].allocOffset >= allocations[(int)(i)].allocOffset + allocations[(int)(i)].allocationSize);
            }

            // Check pPrivateData
            {
                AllocData alloc = allocations[^1];

                D3D12MA_VIRTUAL_ALLOCATION_INFO allocInfo;
                block.Get()->GetAllocationInfo(alloc.allocation, &allocInfo);
                CHECK_BOOL((nuint)allocInfo.pPrivateData == alloc.requestedSize * 10);

                block.Get()->SetAllocationPrivateData(alloc.allocation, (void*)(nuint)666);
                block.Get()->GetAllocationInfo(alloc.allocation, &allocInfo);
                CHECK_BOOL((nuint)allocInfo.pPrivateData == 666);
            }

            // Calculate statistics
            {
                ulong actualAllocSizeMin = ulong.MaxValue;
                ulong actualAllocSizeMax = 0;
                ulong actualAllocSizeSum = 0;

                foreach (AllocData a in allocations)
                {
                    actualAllocSizeMin = Math.Min(actualAllocSizeMin, a.allocationSize);
                    actualAllocSizeMax = Math.Max(actualAllocSizeMax, a.allocationSize);

                    actualAllocSizeSum += a.allocationSize;
                }

                D3D12MA_DetailedStatistics statInfo = new D3D12MA_DetailedStatistics();
                block.Get()->CalculateStatistics(&statInfo);

                CHECK_BOOL(statInfo.Stats.AllocationCount == allocations.Count);
                CHECK_BOOL(statInfo.Stats.BlockCount == 1);
                CHECK_BOOL(statInfo.Stats.BlockBytes == blockDesc.Size);
                CHECK_BOOL(statInfo.AllocationSizeMax == actualAllocSizeMax);
                CHECK_BOOL(statInfo.AllocationSizeMin == actualAllocSizeMin);
                CHECK_BOOL(statInfo.Stats.AllocationBytes >= actualAllocSizeSum);
            }

            // Build JSON dump string
            {
                char* json = null;
                block.Get()->BuildStatsString(&json);

                // put a breakpoint here to debug
                block.Get()->FreeStatsString(json);
            }

            // Final cleanup
            block.Get()->Clear();
        }
    }

    internal static void TestVirtualBlocksAlgorithmsBenchmark([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Benchmark virtual blocks algorithms\n");

        const nuint ALLOCATION_COUNT = 7200;
        const uint MAX_ALLOC_SIZE = 2056;

        D3D12MA_VIRTUAL_BLOCK_DESC blockDesc = new D3D12MA_VIRTUAL_BLOCK_DESC {
            pAllocationCallbacks = ctx.allocationCallbacks,
            Size = 0,
        };

        RandomNumberGenerator rand = new RandomNumberGenerator(20092010);
        uint* allocSizes = stackalloc uint[(int)(ALLOCATION_COUNT)];

        for (nuint i = 0; i < ALLOCATION_COUNT; ++i)
        {
            allocSizes[i] = (rand.Generate() % MAX_ALLOC_SIZE) + 1;
            blockDesc.Size += allocSizes[i];
        }

        blockDesc.Size = (ulong)(blockDesc.Size * 1.5); // 50% size margin in case of alignment
        D3D12MA_VirtualAllocation* allocs = stackalloc D3D12MA_VirtualAllocation[(int)(ALLOCATION_COUNT)];

        for (byte alignmentIndex = 0; alignmentIndex < 4; ++alignmentIndex)
        {
            Unsafe.SkipInit(out ulong alignment);

            switch (alignmentIndex)
            {
                case 0:
                {
                    alignment = 1;
                    break;
                }

                case 1:
                {
                    alignment = 16;
                    break;
                }

                case 2:
                {
                    alignment = 64;
                    break;
                }

                case 3:
                {
                    alignment = 256;
                    break;
                }

                default:
                {
                    D3D12MA_FAIL();
                    break;
                }
            }

            _ = printf("    Alignment={0}\n", alignment);

            for (byte algorithmIndex = 0; algorithmIndex < 2; ++algorithmIndex)
            {
                switch (algorithmIndex)
                {
                    case 0:
                    {
                        blockDesc.Flags = D3D12MA_VIRTUAL_BLOCK_FLAG_NONE;
                        break;
                    }

                    case 1:
                    {
                        blockDesc.Flags = D3D12MA_VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR;
                        break;
                    }

                    default:
                    {
                        D3D12MA_FAIL();
                        break;
                    }
                }

                using ComPtr<D3D12MA_VirtualBlock> block = new ComPtr<D3D12MA_VirtualBlock>();
                CHECK_HR(D3D12MA_CreateVirtualBlock(&blockDesc, (D3D12MA_VirtualBlock**)(&block)));

                TimeSpan allocDuration = TimeSpan.Zero;
                TimeSpan freeDuration = TimeSpan.Zero;

                // Alloc
                long timeBegin = Stopwatch.GetTimestamp();

                for (nuint i = 0; i < ALLOCATION_COUNT; ++i)
                {
                    D3D12MA_VIRTUAL_ALLOCATION_DESC allocCreateInfo = new D3D12MA_VIRTUAL_ALLOCATION_DESC {
                        Size = allocSizes[i],
                        Alignment = alignment,
                    };

                    CHECK_HR(block.Get()->Allocate(&allocCreateInfo, allocs + i, null));
                }
                allocDuration += new TimeSpan((long)((Stopwatch.GetTimestamp() - timeBegin) * ((double)(TimeSpan.TicksPerSecond) / Stopwatch.Frequency)));

                // Free
                timeBegin = Stopwatch.GetTimestamp();

                for (nuint i = ALLOCATION_COUNT; i != 0;)
                {
                    block.Get()->FreeAllocation(allocs[--i]);
                }

                freeDuration += new TimeSpan((long)((Stopwatch.GetTimestamp() - timeBegin) * ((double)(TimeSpan.TicksPerSecond) / Stopwatch.Frequency)));
                _ = printf("        Algorithm={0}  \tallocations {1} s,   \tfree {2} s\n", VirtualAlgorithmToStr(blockDesc.Flags), ToFloatSeconds(allocDuration), ToFloatSeconds(freeDuration));
            }

            _ = printf("\n");
        }
    }

    internal static void ProcessDefragmentationPass([NativeTypeName("const TestContext &")] in TestContext ctx, [NativeTypeName("D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO &")] ref D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO stepInfo)
    {
        List<D3D12_RESOURCE_BARRIER> startBarriers = new List<D3D12_RESOURCE_BARRIER>();
        List<D3D12_RESOURCE_BARRIER> finalBarriers = new List<D3D12_RESOURCE_BARRIER>();

        bool defaultHeap = false;
        for (uint i = 0; i < stepInfo.MoveCount; ++i)
        {
            if (stepInfo.pMoves[i].Operation == D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY)
            {
                bool isDefaultHeap = stepInfo.pMoves[i].pSrcAllocation->GetHeap()->GetDesc().Properties.Type == D3D12_HEAP_TYPE_DEFAULT;

                // Create new resource
                D3D12_RESOURCE_DESC desc = stepInfo.pMoves[i].pSrcAllocation->GetResource()->GetDesc();

                using ComPtr<ID3D12Resource> dstRes = new ComPtr<ID3D12Resource>();
                CHECK_HR(ctx.device->CreatePlacedResource(stepInfo.pMoves[i].pDstTmpAllocation->GetHeap(), stepInfo.pMoves[i].pDstTmpAllocation->GetOffset(), &desc, isDefaultHeap ? D3D12_RESOURCE_STATE_COPY_DEST : D3D12_RESOURCE_STATE_GENERIC_READ, null, __uuidof<ID3D12Resource>(), (void**)(&dstRes)));

                stepInfo.pMoves[i].pDstTmpAllocation->SetResource(dstRes.Get());

                // Perform barriers only if not in right state
                if (isDefaultHeap)
                {
                    defaultHeap = true;

                    // Move new resource into previous state
                    D3D12_RESOURCE_BARRIER barrier = new D3D12_RESOURCE_BARRIER {
                        Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
                        Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
                    };

                    barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
                    barrier.Transition.pResource = dstRes.Get();
                    barrier.Transition.StateAfter = (D3D12_RESOURCE_STATES)(nuint)stepInfo.pMoves[i].pSrcAllocation->GetPrivateData();
                    barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;

                    finalBarriers.Add(barrier);

                    // Move resource into right state

                    barrier.Transition.pResource = stepInfo.pMoves[i].pSrcAllocation->GetResource();
                    barrier.Transition.StateBefore = barrier.Transition.StateAfter;
                    barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_SOURCE;

                    startBarriers.Add(barrier);
                }
            }
        }

        if (defaultHeap)
        {
            ID3D12GraphicsCommandList* cl = BeginCommandList();

            fixed (D3D12_RESOURCE_BARRIER* pStartBarriers = CollectionsMarshal.AsSpan(startBarriers))
            {
                cl->ResourceBarrier((uint)(startBarriers.Count), pStartBarriers);
            }

            // Copy resources
            for (uint i = 0; i < stepInfo.MoveCount; ++i)
            {
                if (stepInfo.pMoves[i].Operation == D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY)
                {
                    ID3D12Resource* dstRes = stepInfo.pMoves[i].pDstTmpAllocation->GetResource();
                    ID3D12Resource* srcRes = stepInfo.pMoves[i].pSrcAllocation->GetResource();

                    if (stepInfo.pMoves[i].pDstTmpAllocation->GetHeap()->GetDesc().Properties.Type == D3D12_HEAP_TYPE_DEFAULT)
                    {
                        cl->CopyResource(dstRes, srcRes);
                    }
                    else
                    {
                        D3D12_RANGE range = new D3D12_RANGE();
                        void* dst;
                        CHECK_HR(dstRes->Map(0, &range, &dst));
                        void* src;
                        CHECK_HR(srcRes->Map(0, &range, &src));
                        _ = memcpy(dst, src, (nuint)(stepInfo.pMoves[i].pSrcAllocation->GetSize()));
                        dstRes->Unmap(0, null);
                        srcRes->Unmap(0, null);
                    }
                }
            }

            fixed (D3D12_RESOURCE_BARRIER* pFinalBarriers = CollectionsMarshal.AsSpan(finalBarriers))
            {
                cl->ResourceBarrier((uint)(finalBarriers.Count), pFinalBarriers);
            }
            EndCommandList(cl);
        }
        else
        {
            // Copy only CPU-side
            for (uint i = 0; i < stepInfo.MoveCount; ++i)
            {
                if (stepInfo.pMoves[i].Operation == D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_COPY)
                {
                    D3D12_RANGE range = new D3D12_RANGE();

                    void* dst;
                    ID3D12Resource* dstRes = stepInfo.pMoves[i].pDstTmpAllocation->GetResource();
                    CHECK_HR(dstRes->Map(0, &range, &dst));

                    void* src;
                    ID3D12Resource* srcRes = stepInfo.pMoves[i].pSrcAllocation->GetResource();
                    CHECK_HR(srcRes->Map(0, &range, &src));

                    _ = memcpy(dst, src, (nuint)(stepInfo.pMoves[i].pSrcAllocation->GetSize()));
                    dstRes->Unmap(0, null);
                    srcRes->Unmap(0, null);
                }
            }
        }
    }

    internal static void Defragment([NativeTypeName("const TestContext &")] in TestContext ctx, [NativeTypeName("D3D12MA::DEFRAGMENTATION_DESC &")] ref D3D12MA_DEFRAGMENTATION_DESC defragDesc, D3D12MA_Pool* pool, D3D12MA_DEFRAGMENTATION_STATS* defragStats = null)
    {
        using ComPtr<D3D12MA_DefragmentationContext> defragCtx = new ComPtr<D3D12MA_DefragmentationContext>();

        if (pool != null)
        {
            CHECK_HR(pool->BeginDefragmentation((D3D12MA_DEFRAGMENTATION_DESC*)(Unsafe.AsPointer(ref defragDesc)), (D3D12MA_DefragmentationContext**)(&defragCtx)));
        }
        else
        {
            ctx.allocator->BeginDefragmentation((D3D12MA_DEFRAGMENTATION_DESC*)(Unsafe.AsPointer(ref defragDesc)), (D3D12MA_DefragmentationContext**)(&defragCtx));
        }

        HRESULT hr = S_OK;
        D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO pass = new D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO();

        while ((hr = defragCtx.Get()->BeginPass(&pass)) == S_FALSE)
        {
            ProcessDefragmentationPass(ctx, ref pass);

            if ((hr = defragCtx.Get()->EndPass(&pass)) == S_OK)
            {
                break;
            }

            CHECK_BOOL(hr == S_FALSE);
        }

        CHECK_HR(hr);

        if (defragStats != null)
        {
            defragCtx.Get()->GetStats(defragStats);
        }
    }

    internal static void TestDefragmentationSimple([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test defragmentation simple\n");

        RandomNumberGenerator rand = new RandomNumberGenerator(667);

        const uint ALLOC_SEED = 20220310;
        const ulong BUF_SIZE = 0x10000;
        const ulong BLOCK_SIZE = BUF_SIZE * 8;

        const ulong MIN_BUF_SIZE = 32;
        const ulong MAX_BUF_SIZE = BUF_SIZE * 4;

        [return: NativeTypeName("UINT64")]
        ulong RandomBufSize()
        {
            return AlignUp((rand.Generate() % (MAX_BUF_SIZE - MIN_BUF_SIZE + 1)) + MIN_BUF_SIZE, 64);
        }

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            BlockSize = BLOCK_SIZE,
        };

        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;
        poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, BUF_SIZE);

        D3D12MA_DEFRAGMENTATION_DESC defragDesc = new D3D12MA_DEFRAGMENTATION_DESC {
            Flags = D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST,
        };

        // Defragmentation of empty pool.
        {
            using ComPtr<D3D12MA_DefragmentationContext> defragCtx = new ComPtr<D3D12MA_DefragmentationContext>();
            CHECK_HR(pool.Get()->BeginDefragmentation(&defragDesc, (D3D12MA_DefragmentationContext**)(&defragCtx)));

            D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO pass = new D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO();
            CHECK_BOOL(defragCtx.Get()->BeginPass(&pass) == S_OK);

            D3D12MA_DEFRAGMENTATION_STATS defragStats = new D3D12MA_DEFRAGMENTATION_STATS();
            defragCtx.Get()->GetStats(&defragStats);

            CHECK_BOOL((defragStats.AllocationsMoved == 0) && (defragStats.BytesFreed == 0) && (defragStats.BytesMoved == 0) && (defragStats.HeapsFreed == 0));
        }

        D3D12_RANGE mapRange = new D3D12_RANGE();
        void* mapPtr;
        List<ComPtr<D3D12MA_Allocation>> allocations = new List<ComPtr<D3D12MA_Allocation>>();

        // persistentlyMappedOption = 0 - not persistently mapped.
        // persistentlyMappedOption = 1 - persistently mapped.
        for (byte persistentlyMappedOption = 0; persistentlyMappedOption < 2; ++persistentlyMappedOption)
        {
            _ = wprintf("  Persistently mapped option = {0}\n", persistentlyMappedOption);
            bool persistentlyMapped = persistentlyMappedOption != 0;

            // # Test 1
            // Buffers of fixed size.
            // Fill 2 blocks. Remove odd buffers. Defragment everything.
            // Expected result: at least 1 block freed.
            {
                for (nuint i = 0; i < BLOCK_SIZE / BUF_SIZE * 2; ++i)
                {
                    using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
                    CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

                    if (persistentlyMapped)
                    {
                        CHECK_HR(alloc.Get()->GetResource()->Map(0, &mapRange, &mapPtr));
                    }

                    allocations.Add(alloc);
                    _ = alloc.Detach();
                }

                for (nuint i = 1; i < (uint)(allocations.Count); ++i)
                {
                    allocations[(int)(i)].Dispose();
                    allocations.RemoveAt((int)(i));
                }

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    FillAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                // Set data for defragmentation retrieval
                foreach (var alloc in allocations)
                {
                    alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_GENERIC_READ));
                }

                D3D12MA_DEFRAGMENTATION_STATS defragStats;
                Defragment(ctx, ref defragDesc, pool.Get(), &defragStats);
                CHECK_BOOL(defragStats.AllocationsMoved == 4 && defragStats.BytesMoved == 4 * BUF_SIZE);

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    ValidateAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                allocations.Dispose();
                allocations.Clear();
            }

            // # Test 2
            // Buffers of fixed size.
            // Fill 2 blocks. Remove odd buffers. Defragment one buffer at time.
            // Expected result: Each of 4 interations makes some progress.
            {
                for (nuint i = 0; i < BLOCK_SIZE / BUF_SIZE * 2; ++i)
                {
                    using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
                    CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

                    if (persistentlyMapped)
                    {
                        CHECK_HR(alloc.Get()->GetResource()->Map(0, &mapRange, &mapPtr));
                    }

                    allocations.Add(alloc);
                    _ = alloc.Detach();
                }

                for (nuint i = 1; i < (uint)(allocations.Count); ++i)
                {
                    allocations[(int)(i)].Dispose();
                    allocations.RemoveAt((int)(i));
                }

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    FillAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                // Set data for defragmentation retrieval
                foreach (var alloc in allocations)
                {
                    alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_GENERIC_READ));
                }

                defragDesc.MaxAllocationsPerPass = 1;
                defragDesc.MaxBytesPerPass = BUF_SIZE;

                using ComPtr<D3D12MA_DefragmentationContext> defragCtx = new ComPtr<D3D12MA_DefragmentationContext>();
                CHECK_HR(pool.Get()->BeginDefragmentation(&defragDesc, (D3D12MA_DefragmentationContext**)(&defragCtx)));

                for (nuint i = 0; i < BLOCK_SIZE / BUF_SIZE / 2; ++i)
                {
                    D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO pass = new D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO();
                    CHECK_BOOL(defragCtx.Get()->BeginPass(&pass) == S_FALSE);

                    ProcessDefragmentationPass(ctx, ref pass);

                    CHECK_BOOL(defragCtx.Get()->EndPass(&pass) == S_FALSE);
                }

                D3D12MA_DEFRAGMENTATION_STATS defragStats = new D3D12MA_DEFRAGMENTATION_STATS();
                defragCtx.Get()->GetStats(&defragStats);
                CHECK_BOOL(defragStats.AllocationsMoved == 4 && defragStats.BytesMoved == 4 * BUF_SIZE);

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    ValidateAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                allocations.Dispose();
                allocations.Clear();
            }

            // # Test 3
            // Buffers of variable size.
            // Create a number of buffers. Remove some percent of them.
            // Defragment while having some percent of them unmovable.
            // Expected result: Just simple validation.
            {
                for (nuint i = 0; i < 100; ++i)
                {
                    D3D12_RESOURCE_DESC localResDesc = resDesc;
                    localResDesc.Width = RandomBufSize();

                    using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
                    CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &localResDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

                    if (persistentlyMapped)
                    {
                        CHECK_HR(alloc.Get()->GetResource()->Map(0, &mapRange, &mapPtr));
                    }

                    allocations.Add(alloc);
                    _ = alloc.Detach();
                }

                const uint percentToDelete = 60;
                nuint numberToDelete = (uint)(allocations.Count) * percentToDelete / 100;

                for (nuint i = 0; i < numberToDelete; ++i)
                {
                    nuint indexToDelete = rand.Generate() % (uint)(allocations.Count);
                    allocations[(int)(indexToDelete)].Dispose();
                    allocations.RemoveAt((int)(indexToDelete));
                }

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    FillAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                // Non-movable allocations will be at the beginning of allocations array.

                const uint percentNonMovable = 20;
                nuint numberNonMovable = (uint)(allocations.Count) * percentNonMovable / 100;

                for (nuint i = 0; i < numberNonMovable; ++i)
                {
                    nuint indexNonMovable = i + (rand.Generate() % (uint)((uint)(allocations.Count) - i));

                    if (indexNonMovable != i)
                    {
                        Span<ComPtr<D3D12MA_Allocation>> tmp = CollectionsMarshal.AsSpan(allocations);
                        D3D12MA_SWAP(ref tmp[(int)(i)], ref tmp[(int)(indexNonMovable)]);
                    }
                }

                // Set data for defragmentation retrieval
                foreach (var alloc in allocations)
                {
                    alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_GENERIC_READ));
                }

                defragDesc.MaxAllocationsPerPass = 0;
                defragDesc.MaxBytesPerPass = 0;

                using ComPtr<D3D12MA_DefragmentationContext> defragCtx = new ComPtr<D3D12MA_DefragmentationContext>();
                CHECK_HR(pool.Get()->BeginDefragmentation(&defragDesc, (D3D12MA_DefragmentationContext**)(&defragCtx)));

                HRESULT hr = S_OK;
                D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO pass = new D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO();

                while ((hr = defragCtx.Get()->BeginPass(&pass)) == S_FALSE)
                {
                    D3D12MA_DEFRAGMENTATION_MOVE* end = pass.pMoves + pass.MoveCount;

                    for (uint i = 0; i < numberNonMovable; ++i)
                    {
                        D3D12MA_DEFRAGMENTATION_MOVE* move = pass.pMoves;

                        while (move != end)
                        {
                            if (move->pSrcAllocation == allocations[(int)(i)].Get())
                            {
                                break;
                            }
                            move++;
                        }

                        if (move != end)
                        {
                            move->Operation = D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE;
                        }
                    }

                    ProcessDefragmentationPass(ctx, ref pass);

                    if ((hr = defragCtx.Get()->EndPass(&pass)) == S_OK)
                    {
                        break;
                    }

                    CHECK_BOOL(hr == S_FALSE);
                }
                CHECK_BOOL(hr == S_OK);

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    ValidateAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                allocations.Dispose();
                allocations.Clear();
            }
        }

        allocations.Dispose();
    }

    internal static void TestDefragmentationAlgorithms([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test defragmentation algorithms\n");

        RandomNumberGenerator rand = new RandomNumberGenerator(669);

        const uint ALLOC_SEED = 20091225;
        const ulong BUF_SIZE = 0x10000;
        const ulong BLOCK_SIZE = BUF_SIZE * 400;

        const ulong MIN_BUF_SIZE = 32;
        const ulong MAX_BUF_SIZE = BUF_SIZE * 4;

        [return: NativeTypeName("UINT64")]
        ulong RandomBufSize()
        {
            return AlignUp((rand.Generate() % (MAX_BUF_SIZE - MIN_BUF_SIZE + 1)) + MIN_BUF_SIZE, 64);
        }

        D3D12MA_POOL_DESC poolDesc = new D3D12MA_POOL_DESC {
            BlockSize = BLOCK_SIZE,
        };

        poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;
        poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

        using ComPtr<D3D12MA_Pool> pool = new ComPtr<D3D12MA_Pool>();
        CHECK_HR(ctx.allocator->CreatePool(&poolDesc, (D3D12MA_Pool**)(&pool)));

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            CustomPool = pool.Get(),
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, BUF_SIZE);

        D3D12MA_DEFRAGMENTATION_DESC defragDesc = new D3D12MA_DEFRAGMENTATION_DESC();

        List<ComPtr<D3D12MA_Allocation>> allocations = new List<ComPtr<D3D12MA_Allocation>>();

        for (byte i = 0; i < 3; ++i)
        {
            switch (i)
            {
                case 0:
                {
                    defragDesc.Flags = D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FAST;
                    break;
                }

                case 1:
                {
                    defragDesc.Flags = D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED;
                    break;
                }

                case 2:
                {
                    defragDesc.Flags = D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL;
                    break;
                }
            }

            _ = wprintf("  Algorithm = {0}\n", DefragmentationAlgorithmToStr((uint)(defragDesc.Flags)));

            // 0 - Without immovable allocations
            // 1 - With immovable allocations
            for (byte j = 0; j < 2; ++j)
            {
                for (nuint k = 0; k < 800; ++k)
                {
                    resDesc.Width = RandomBufSize();

                    using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
                    CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

                    allocations.Add(alloc);
                    _ = alloc.Detach();
                }

                const uint percentToDelete = 55;
                nuint numberToDelete = (uint)(allocations.Count) * percentToDelete / 100;

                for (nuint k = 0; k < numberToDelete; ++k)
                {
                    nuint indexToDelete = rand.Generate() % (uint)(allocations.Count);
                    allocations[(int)(indexToDelete)].Dispose();
                    allocations.RemoveAt((int)(indexToDelete));
                }

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    FillAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                // Non-movable allocations will be at the beginning of allocations array.
                const uint percentNonMovable = 20;
                nuint numberNonMovable = j == 0 ? 0 : ((uint)(allocations.Count) * percentNonMovable / 100);

                for (nuint k = 0; k < numberNonMovable; ++k)
                {
                    nuint indexNonMovable = k + (rand.Generate() % (uint)((uint)(allocations.Count) - k));

                    if (indexNonMovable != k)
                    {
                        Span<ComPtr<D3D12MA_Allocation>> tmp = CollectionsMarshal.AsSpan(allocations);
                        D3D12MA_SWAP(ref tmp[(int)(k)], ref tmp[(int)(indexNonMovable)]);
                    }
                }

                // Set data for defragmentation retrieval
                foreach (var alloc in allocations)
                {
                    alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_GENERIC_READ));
                }

                string output = DefragmentationAlgorithmToStr((uint)(defragDesc.Flags));

                if (j == 0)
                {
                    output += "_NoMove";
                }
                else
                {
                    output += "_Move";
                }

                SaveStatsStringToFile(ctx, (output + "_Before.json"));

                using ComPtr<D3D12MA_DefragmentationContext> defragCtx = new ComPtr<D3D12MA_DefragmentationContext>();
                CHECK_HR(pool.Get()->BeginDefragmentation(&defragDesc, (D3D12MA_DefragmentationContext**)(&defragCtx)));

                HRESULT hr = S_OK;
                D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO pass = new D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO();

                while ((hr = defragCtx.Get()->BeginPass(&pass)) == S_FALSE)
                {
                    D3D12MA_DEFRAGMENTATION_MOVE* end = pass.pMoves + pass.MoveCount;

                    for (uint k = 0; k < numberNonMovable; ++k)
                    {
                        D3D12MA_DEFRAGMENTATION_MOVE* move = pass.pMoves;

                        while (move != end)
                        {
                            if (move->pSrcAllocation == allocations[(int)(k)].Get())
                            {
                                break;
                            }
                            move++;
                        }

                        if (move != end)
                        {
                            move->Operation = D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE;
                        }
                    }

                    for (uint k = 0; k < pass.MoveCount; ++k)
                    {
                        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                        {
                            var it = pAllocations;

                            while (it != (pAllocations + allocations.Count))
                            {
                                if (pass.pMoves[k].pSrcAllocation == it->Get())
                                {
                                    break;
                                }
                                it++;
                            }

                            D3D12MA_ASSERT(it != (pAllocations + allocations.Count));
                        }
                    }

                    ProcessDefragmentationPass(ctx, ref pass);

                    if ((hr = defragCtx.Get()->EndPass(&pass)) == S_OK)
                    {
                        break;
                    }

                    CHECK_BOOL(hr == S_FALSE);
                }
                CHECK_BOOL(hr == S_OK);

                SaveStatsStringToFile(ctx, (output + "_After.json"));

                fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                {
                    ValidateAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
                }

                allocations.Dispose();
                allocations.Clear();
            }
        }

        allocations.Dispose();
    }

    internal static void TestDefragmentationFull([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        const uint ALLOC_SEED = 20101220;
        List<ComPtr<D3D12MA_Allocation>> allocations = new List<ComPtr<D3D12MA_Allocation>>();

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
            ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
        };
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, 0x10000);

        // Create initial allocations.
        for (nuint i = 0; i < 400; ++i)
        {
            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

            allocations.Add(alloc);
            _ = alloc.Detach();
        }

        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            FillAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }

        // Delete random allocations

        const nuint allocationsToDeletePercent = 80;
        nuint allocationsToDelete = (uint)(allocations.Count) * allocationsToDeletePercent / 100;

        RandomNumberGenerator rand = new RandomNumberGenerator();

        for (nuint i = 0; i < allocationsToDelete; ++i)
        {
            nuint index = (nuint)(rand.Generate() % (uint)(allocations.Count));
            allocations[(int)(index)].Dispose();
            allocations.RemoveAt((int)(index));
        }
        SaveStatsStringToFile(ctx, "FullBefore.json");

        {
            // Set data for defragmentation retrieval
            foreach (var alloc in allocations)
            {
                alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_GENERIC_READ));
            }

            const uint defragCount = 1;

            for (uint defragIndex = 0; defragIndex < defragCount; ++defragIndex)
            {
                D3D12MA_DEFRAGMENTATION_DESC defragDesc = new D3D12MA_DEFRAGMENTATION_DESC {
                    Flags = D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL,
                };

                _ = wprintf("Test defragmentation full #{0}\n", defragIndex);

                long begTime = Stopwatch.GetTimestamp();

                D3D12MA_DEFRAGMENTATION_STATS stats;
                Defragment(ctx, ref defragDesc, null, &stats);

                float defragmentDuration = ToFloatSeconds(new TimeSpan((long)((Stopwatch.GetTimestamp() - begTime) * ((double)(TimeSpan.TicksPerSecond) / Stopwatch.Frequency))));

                _ = wprintf("Moved allocations {0}, bytes {1}\n", stats.AllocationsMoved, stats.BytesMoved);
                _ = wprintf("Freed blocks {0}, bytes {1}\n", stats.HeapsFreed, stats.BytesFreed);
                _ = wprintf("Time: {0:F2} s\n", defragmentDuration);

                SaveStatsStringToFile(ctx, ($"FullAfter_{defragIndex}.json"));
            }
        }

        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            ValidateAllocationsData(pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }
        allocations.Dispose();
    }

    internal static void TestDefragmentationGpu([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test defragmentation GPU\n");

        const uint ALLOC_SEED = 20180314;
        List<ComPtr<D3D12MA_Allocation>> allocations = new List<ComPtr<D3D12MA_Allocation>>();

        // Create that many allocations to surely fill 3 new blocks of 256 MB.
        const ulong bufSizeMin = 5ul * 1024 * 1024;
        const ulong bufSizeMax = 10ul * 1024 * 1024;
        const ulong totalSize = 3ul * 256 * 1024 * 1024;
        const nuint bufCount = (nuint)(totalSize / bufSizeMin);
        const nuint percentToLeave = 30;
        const nuint percentNonMovable = 3;

        RandomNumberGenerator rand = new RandomNumberGenerator(234522);
        FillResourceDescForBuffer(out D3D12_RESOURCE_DESC resDesc, 0x10000);

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
            ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
        };

        // Create all intended buffers.
        for (nuint i = 0; i < bufCount; ++i)
        {
            resDesc.Width = AlignUp((rand.Generate() % (bufSizeMax - bufSizeMin)) + bufSizeMin, 32ul);

            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

            allocations.Add(alloc);
            _ = alloc.Detach();
        }

        // Destroy some percentage of them.
        {
            nuint buffersToDestroy = RoundDiv(bufCount * (100 - percentToLeave), 100);

            for (nuint i = 0; i < buffersToDestroy; ++i)
            {
                nuint index = rand.Generate() % (uint)(allocations.Count);
                allocations[(int)(index)].Dispose();
                allocations.RemoveAt((int)(index));
            }
        }

        // Set data for defragmentation retrieval
        foreach (var alloc in allocations)
        {
            alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER));
        }

        // Fill them with meaningful data.

        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            FillAllocationsDataGPU(ctx, pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }

        SaveStatsStringToFile(ctx, "GPU_defragmentation_A_before.json");

        // Defragment using GPU only.
        {
            nuint numberNonMovable = (uint)(allocations.Count) * percentNonMovable / 100;

            for (nuint i = 0; i < numberNonMovable; ++i)
            {
                nuint indexNonMovable = i + (rand.Generate() % (uint)((uint)(allocations.Count) - i));

                if (indexNonMovable != i)
                {
                    Span<ComPtr<D3D12MA_Allocation>> tmp = CollectionsMarshal.AsSpan(allocations);
                    D3D12MA_SWAP(ref tmp[(int)(i)], ref tmp[(int)(indexNonMovable)]);
                }
            }

            D3D12MA_DEFRAGMENTATION_DESC defragDesc = new D3D12MA_DEFRAGMENTATION_DESC();
            D3D12MA_DEFRAGMENTATION_STATS stats;
            Defragment(ctx, ref defragDesc, null, &stats);

            CHECK_BOOL(stats.AllocationsMoved > 0 && stats.BytesMoved > 0);
            CHECK_BOOL(stats.HeapsFreed > 0 && stats.BytesFreed > 0);
        }

        SaveStatsStringToFile(ctx, "GPU_defragmentation_B_after.json");

        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            ValidateAllocationsDataGPU(ctx, pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }
        allocations.Dispose();
    }

    internal static void TestDefragmentationIncrementalBasic([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test defragmentation incremental basic\n");

        const uint ALLOC_SEED = 20210918;
        List<ComPtr<D3D12MA_Allocation>> allocations = new List<ComPtr<D3D12MA_Allocation>>();

        // Create that many allocations to surely fill 3 new blocks of 256 MB.

        Span<uint> imageSizes = stackalloc uint[3] {
            256,
            512,
            1024,
        };

        const ulong bufSizeMin = 5ul * 1024 * 1024;
        const ulong bufSizeMax = 10ul * 1024 * 1024;
        const ulong totalSize = 3ul * 256 * 1024 * 1024;

        nuint imageCount = (nuint)(totalSize / ((nuint)(imageSizes[0]) * imageSizes[0] * 4) / 2);

        const nuint bufCount = (nuint)(totalSize / bufSizeMin) / 2;
        const nuint percentToLeave = 30;

        RandomNumberGenerator rand = new RandomNumberGenerator(234522);

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
        };

        D3D12_RESOURCE_DESC resDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = 0,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_NONE,
        };

        // Create all intended images.
        for (nuint i = 0; i < imageCount; ++i)
        {
            uint size = imageSizes[(int)(rand.Generate() % 3)];
            resDesc.Width = size;
            resDesc.Height = size;

            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

            alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE));

            allocations.Add(alloc);
            _ = alloc.Detach();
        }

        // And all buffers
        FillResourceDescForBuffer(out resDesc, 0x10000);

        for (nuint i = 0; i < bufCount; ++i)
        {
            resDesc.Width = AlignUp((rand.Generate() % (bufSizeMax - bufSizeMin)) + bufSizeMin, 32ul);

            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

            alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER));

            allocations.Add(alloc);
            _ = alloc.Detach();
        }

        // Destroy some percentage of them.
        {
            nuint allocationsToDestroy = RoundDiv((imageCount + bufCount) * (100 - percentToLeave), 100);

            for (nuint i = 0; i < allocationsToDestroy; ++i)
            {
                nuint index = rand.Generate() % (uint)(allocations.Count);
                allocations[(int)(index)].Dispose();
                allocations.RemoveAt((int)(index));
            }
        }

        // Fill them with meaningful data.
        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            FillAllocationsDataGPU(ctx, pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }

        SaveStatsStringToFile(ctx, "GPU_defragmentation_incremental_basic_A_before.json");
        // Defragment using GPU only.
        {
            D3D12MA_DEFRAGMENTATION_DESC defragDesc = new D3D12MA_DEFRAGMENTATION_DESC();

            using ComPtr<D3D12MA_DefragmentationContext> defragCtx = new ComPtr<D3D12MA_DefragmentationContext>();
            ctx.allocator->BeginDefragmentation(&defragDesc, (D3D12MA_DefragmentationContext**)(&defragCtx));

            HRESULT hr = S_OK;
            D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO pass = new D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO();

            while ((hr = defragCtx.Get()->BeginPass(&pass)) == S_FALSE)
            {
                // Ignore data outside of test
                for (uint i = 0; i < pass.MoveCount; ++i)
                {
                    fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                    {
                        var it = pAllocations;

                        while (it != (pAllocations + allocations.Count))
                        {
                            if (pass.pMoves[i].pSrcAllocation == it->Get())
                            {
                                break;
                            }
                            it++;
                        }

                        if (it == (pAllocations + allocations.Count))
                        {
                            pass.pMoves[i].Operation = D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE;
                        }
                    }
                }

                ProcessDefragmentationPass(ctx, ref pass);

                if ((hr = defragCtx.Get()->EndPass(&pass)) == S_OK)
                {
                    break;
                }

                CHECK_BOOL(hr == S_FALSE);
            }

            CHECK_BOOL(hr == S_OK);

            D3D12MA_DEFRAGMENTATION_STATS stats = new D3D12MA_DEFRAGMENTATION_STATS();
            defragCtx.Get()->GetStats(&stats);

            CHECK_BOOL(stats.AllocationsMoved > 0 && stats.BytesMoved > 0);
            CHECK_BOOL(stats.HeapsFreed > 0 && stats.BytesFreed > 0);
        }

        SaveStatsStringToFile(ctx, "GPU_defragmentation_incremental_basic_B_after.json");

        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            ValidateAllocationsDataGPU(ctx, pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }
        allocations.Dispose();
    }

    internal static void TestDefragmentationIncrementalComplex([NativeTypeName("const TestContext &")] in TestContext ctx)
    {
        _ = wprintf("Test defragmentation incremental complex\n");

        const uint ALLOC_SEED = 20180112;
        List<ComPtr<D3D12MA_Allocation>> allocations = new List<ComPtr<D3D12MA_Allocation>>();

        // Create that many allocations to surely fill 3 new blocks of 256 MB.
        Span<uint> imageSizes = stackalloc uint[3] {
            256,
            512,
            1024,
        };

        const ulong bufSizeMin = 5ul * 1024 * 1024;
        const ulong bufSizeMax = 10ul * 1024 * 1024;
        const ulong totalSize = 3ul * 256 * 1024 * 1024;

        nuint imageCount = (nuint)(totalSize / (imageSizes[0] * imageSizes[0] * 4)) / 2;

        const nuint bufCount = (nuint)(totalSize / bufSizeMin) / 2;
        const nuint percentToLeave = 30;

        RandomNumberGenerator rand = new RandomNumberGenerator(234522);

        D3D12MA_ALLOCATION_DESC allocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
        };

        D3D12_RESOURCE_DESC resDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = 0,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_NONE,
        };

        // Create all intended images.
        for (nuint i = 0; i < imageCount; ++i)
        {
            uint size = imageSizes[(int)(rand.Generate() % 3)];

            resDesc.Width = size;
            resDesc.Height = size;

            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

            alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE));

            allocations.Add(alloc);
            _ = alloc.Detach();
        }

        // And all buffers
        FillResourceDescForBuffer(out resDesc, 0x10000);

        for (nuint i = 0; i < bufCount; ++i)
        {
            resDesc.Width = AlignUp((rand.Generate() % (bufSizeMax - bufSizeMin)) + bufSizeMin, 32ul);

            using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
            CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc, D3D12_RESOURCE_STATE_COPY_DEST, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

            alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER));

            allocations.Add(alloc);
            _ = alloc.Detach();
        }

        // Destroy some percentage of them.
        {
            nuint allocationsToDestroy = RoundDiv((imageCount + bufCount) * (100 - percentToLeave), 100);

            for (nuint i = 0; i < allocationsToDestroy; ++i)
            {
                nuint index = rand.Generate() % (uint)(allocations.Count);
                allocations[(int)(index)].Dispose();
                allocations.RemoveAt((int)(index));
            }
        }

        // Fill them with meaningful data.
        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            FillAllocationsDataGPU(ctx, pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }

        SaveStatsStringToFile(ctx, "GPU_defragmentation_incremental_complex_A_before.json");

        const nuint maxAdditionalAllocations = 100;
        List<ComPtr<D3D12MA_Allocation>> additionalAllocations = new List<ComPtr<D3D12MA_Allocation>>((int)(maxAdditionalAllocations));

        void makeAdditionalAllocation(in TestContext ctx, D3D12MA_ALLOCATION_DESC* pAllocDesc, D3D12_RESOURCE_DESC* pResDesc)
        {
            if ((uint)(additionalAllocations.Count) < maxAdditionalAllocations)
            {
                pResDesc->Width = AlignUp(bufSizeMin + (rand.Generate() % (bufSizeMax - bufSizeMin)), 16ul);

                using ComPtr<D3D12MA_Allocation> alloc = new ComPtr<D3D12MA_Allocation>();
                CHECK_HR(ctx.allocator->CreateResource(pAllocDesc, pResDesc, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, null, (D3D12MA_Allocation**)(&alloc), IID_NULL, null));

                alloc.Get()->SetPrivateData((void*)(uint)(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER));

                additionalAllocations.Add(alloc);
                _ = alloc.Detach();
            }
        }

        // Defragment using GPU only.
        {
            D3D12MA_DEFRAGMENTATION_DESC defragDesc = new D3D12MA_DEFRAGMENTATION_DESC {
                Flags = D3D12MA_DEFRAGMENTATION_FLAG_ALGORITHM_FULL,
            };

            using ComPtr<D3D12MA_DefragmentationContext> defragCtx = new ComPtr<D3D12MA_DefragmentationContext>();
            ctx.allocator->BeginDefragmentation(&defragDesc, (D3D12MA_DefragmentationContext**)(&defragCtx));

            makeAdditionalAllocation(ctx, &allocDesc, &resDesc);

            HRESULT hr = S_OK;
            D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO pass = new D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO();

            while ((hr = defragCtx.Get()->BeginPass(&pass)) == S_FALSE)
            {
                makeAdditionalAllocation(ctx, &allocDesc, &resDesc);

                // Ignore data outside of test
                for (uint i = 0; i < pass.MoveCount; ++i)
                {
                    fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
                    {
                        var it = pAllocations;

                        while (it != (pAllocations + allocations.Count))
                        {
                            if (pass.pMoves[i].pSrcAllocation == it->Get())
                            {
                                break;
                            }
                            it++;
                        }

                        if (it == (pAllocations + allocations.Count))
                        {
                            fixed (ComPtr<D3D12MA_Allocation>* pAdditionalAllocations = CollectionsMarshal.AsSpan(additionalAllocations))
                            {
                                var it2 = pAdditionalAllocations;

                                while (it2 != (pAdditionalAllocations + additionalAllocations.Count))
                                {
                                    if (pass.pMoves[i].pSrcAllocation == it2->Get())
                                    {
                                        break;
                                    }
                                    it2++;
                                }

                                if (it == (pAdditionalAllocations + additionalAllocations.Count))
                                {
                                    pass.pMoves[i].Operation = D3D12MA_DEFRAGMENTATION_MOVE_OPERATION_IGNORE;
                                }
                            }
                        }
                    }
                }

                ProcessDefragmentationPass(ctx, ref pass);

                makeAdditionalAllocation(ctx, &allocDesc, &resDesc);

                if ((hr = defragCtx.Get()->EndPass(&pass)) == S_OK)
                {
                    break;
                }

                CHECK_BOOL(hr == S_FALSE);
            }

            CHECK_BOOL(hr == S_OK);

            D3D12MA_DEFRAGMENTATION_STATS stats = new D3D12MA_DEFRAGMENTATION_STATS();
            defragCtx.Get()->GetStats(&stats);

            CHECK_BOOL(stats.AllocationsMoved > 0 && stats.BytesMoved > 0);
            CHECK_BOOL(stats.HeapsFreed > 0 && stats.BytesFreed > 0);
        }

        SaveStatsStringToFile(ctx, "GPU_defragmentation_incremental_complex_B_after.json");

        fixed (ComPtr<D3D12MA_Allocation>* pAllocations = CollectionsMarshal.AsSpan(allocations))
        {
            ValidateAllocationsDataGPU(ctx, pAllocations, (uint)(allocations.Count), ALLOC_SEED);
        }

        additionalAllocations.Dispose();
        allocations.Dispose();
    }

    internal static void TestGroupVirtual([NativeTypeName("const TestContext &")] in TestContext ctx, bool benchmark)
    {
        if (benchmark)
        {
            TestVirtualBlocksAlgorithmsBenchmark(ctx);
        }
        else
        {
            TestVirtualBlocks(ctx);
            TestVirtualBlocksAlgorithms(ctx);
        }
    }

    [SupportedOSPlatform("windows10.0")]
    internal static void TestGroupBasics([NativeTypeName("const TestContext &")] in TestContext ctx, bool benchmark)
    {
        if (benchmark)
        {
            TextWriter file = new StreamWriter("Results.csv");
            BenchmarkAlgorithms(ctx, file);
            file.Close();
        }
        else if (D3D12MA_DEBUG_MARGIN != 0)
        {
            TestDebugMargin(ctx);
            TestDebugMarginNotInVirtualAllocator(ctx);
        }
        else
        {
            TestJson(ctx);
            TestCommittedResourcesAndJson(ctx);
            TestCustomHeapFlags(ctx);
            TestPlacedResources(ctx);
            TestOtherComInterface(ctx);
            TestCustomPools(ctx);
            TestCustomPool_MinAllocationAlignment(ctx);
            TestCustomPool_Committed(ctx);
            TestPoolsAndAllocationParameters(ctx);
            TestCustomHeaps(ctx);
            TestStandardCustomCommittedPlaced(ctx);
            TestAliasingMemory(ctx);
            TestAliasingImplicitCommitted(ctx);
            TestPoolMsaaTextureAsCommitted(ctx);
            TestMapping(ctx);
            TestStats(ctx);
            TestTransfer(ctx);
            TestZeroInitialized(ctx);
            TestMultithreading(ctx);
            TestLinearAllocator(ctx);
            TestLinearAllocatorMultiBlock(ctx);
            ManuallyTestLinearAllocator(ctx);

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19043, 0))
            {
                TestDevice4(ctx);
                TestDevice8(ctx);
            }
        }
    }

    internal static void TestGroupDefragmentation([NativeTypeName("const TestContext &")] in TestContext ctx, bool benchmark)
    {
        TestDefragmentationSimple(ctx);
        TestDefragmentationAlgorithms(ctx);
        TestDefragmentationFull(ctx);
        TestDefragmentationGpu(ctx);
        TestDefragmentationIncrementalBasic(ctx);
        TestDefragmentationIncrementalComplex(ctx);
    }
}
