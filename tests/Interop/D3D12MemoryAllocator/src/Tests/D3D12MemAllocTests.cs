// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using TerraFX.Interop.Windows;
using TerraFX.Interop.Windows.D3D12;
using static TerraFX.Interop.Windows.D3D12.D3D12_CPU_PAGE_PROPERTY;
using static TerraFX.Interop.Windows.D3D12.D3D12_FEATURE;
using static TerraFX.Interop.Windows.D3D12.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_HEAP_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_MEMORY_POOL;
using static TerraFX.Interop.Windows.D3D12.D3D12_PROTECTED_RESOURCE_SESSION_SUPPORT_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_BARRIER_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_STATES;
using static TerraFX.Interop.Windows.D3D12.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.Windows.DXGI.DXGI_FORMAT;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.D3D12MA.D3D12MA_ALLOCATION_FLAGS;
using static TerraFX.Interop.D3D12MA.D3D12MA_ALLOCATOR_FLAGS;
using static TerraFX.Interop.D3D12MA.D3D12MemAlloc;

namespace TerraFX.Interop.D3D12MA.UnitTests
{
    internal unsafe static partial class D3D12MemAllocTests
    {
        [NativeTypeName("ulong")]
        private const ulong MEGABYTE = 1024 * 1024;

        private static void FillResourceDescForBuffer<TD3D12_RESOURCE_DESC>([NativeTypeName("TD3D12_RESOURCE_DESC&")] out TD3D12_RESOURCE_DESC outResourceDesc, [NativeTypeName("ulong")] ulong size)
            where TD3D12_RESOURCE_DESC : unmanaged
        {
            Unsafe.SkipInit(out outResourceDesc);
            ref D3D12_RESOURCE_DESC resourceDesc = ref Unsafe.As<TD3D12_RESOURCE_DESC, D3D12_RESOURCE_DESC>(ref outResourceDesc);

            resourceDesc = default;
            resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            resourceDesc.Alignment = 0;
            resourceDesc.Width = size;
            resourceDesc.Height = 1;
            resourceDesc.DepthOrArraySize = 1;
            resourceDesc.MipLevels = 1;
            resourceDesc.Format = DXGI_FORMAT_UNKNOWN;
            resourceDesc.SampleDesc.Count = 1;
            resourceDesc.SampleDesc.Quality = 0;
            resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            resourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
        }

        private static void FillData(void* outPtr, [NativeTypeName("const ulong")] ulong sizeInBytes, [NativeTypeName("UINT")] uint seed)
        {
            uint* outValues = (uint*)outPtr;
            ulong sizeInValues = sizeInBytes / sizeof(uint);
            uint value = seed;

            for (uint i = 0; i < sizeInValues; ++i)
            {
                outValues[i] = value++;
            }
        }

        private static bool ValidateData([NativeTypeName("const void*")] void* ptr, [NativeTypeName("const ulong")] ulong sizeInBytes, [NativeTypeName("UINT")] uint seed)
        {
            uint* values = (uint*)ptr;
            ulong sizeInValues = sizeInBytes / sizeof(uint);
            uint value = seed;

            for (uint i = 0; i < sizeInValues; ++i)
            {
                if (values[i] != value++)
                {
                    // FAIL("ValidateData failed.");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateDataZero([NativeTypeName("const void*")] void* ptr, [NativeTypeName("const ulong")] ulong sizeInBytes)
        {
            uint* values = (uint*)ptr;
            ulong sizeInValues = sizeInBytes / sizeof(uint);

            for (uint i = 0; i < sizeInValues; ++i)
            {
                if (values[i] != 0)
                {
                    // FAIL("ValidateData failed.");
                    return false;
                }
            }

            return true;
        }

        public static void TestVirtualBlocks([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test virtual blocks");

            const ulong blockSize = 16 * MEGABYTE;
            const ulong alignment = 256;

            // # Create block 16 MB

            ComPtr<D3D12MA_VirtualBlock> block = default;

            try
            {
                D3D12MA_VIRTUAL_BLOCK_DESC blockDesc = default;
                blockDesc.pAllocationCallbacks = ctx.allocationCallbacks;
                blockDesc.Size = blockSize;

                CHECK_HR(D3D12MA_CreateVirtualBlock(&blockDesc, block.GetAddressOf()));
                CHECK_BOOL(block.Get() != null);

                // # Allocate 8 MB

                D3D12MA_VIRTUAL_ALLOCATION_DESC allocDesc = default;
                allocDesc.Alignment = alignment;
                allocDesc.pUserData = (void*)(nuint)1;
                allocDesc.Size = 8 * MEGABYTE;

                ulong alloc0Offset;
                CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc0Offset));
                CHECK_BOOL(alloc0Offset < blockSize);

                // # Validate the allocation

                D3D12MA_VIRTUAL_ALLOCATION_INFO allocInfo = default;
                block.Get()->GetAllocationInfo(alloc0Offset, &allocInfo);
                CHECK_BOOL(allocInfo.size == allocDesc.Size);
                CHECK_BOOL(allocInfo.pUserData == allocDesc.pUserData);

                // # Check SetUserData

                block.Get()->SetAllocationUserData(alloc0Offset, (void*)(nuint)2);
                block.Get()->GetAllocationInfo(alloc0Offset, &allocInfo);

                CHECK_BOOL(allocInfo.pUserData == (void*)(nuint)2);

                // # Allocate 4 MB

                allocDesc.Size = 4 * MEGABYTE;
                allocDesc.Alignment = alignment;

                ulong alloc1Offset;
                CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc1Offset));
                CHECK_BOOL(alloc1Offset < blockSize);
                CHECK_BOOL(alloc1Offset + (4 * MEGABYTE) <= alloc0Offset || alloc0Offset + (8 * MEGABYTE) <= alloc1Offset); // Check if they don't overlap.

                // # Allocate another 8 MB - it should fail

                allocDesc.Size = 8 * MEGABYTE;
                allocDesc.Alignment = alignment;

                ulong alloc2Offset;
                CHECK_BOOL(FAILED(block.Get()->Allocate(&allocDesc, &alloc2Offset)));
                CHECK_BOOL(alloc2Offset == UINT64_MAX);

                // # Free the 4 MB block. Now allocation of 8 MB should succeed.

                block.Get()->FreeAllocation(alloc1Offset);

                CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc2Offset));
                CHECK_BOOL(alloc2Offset < blockSize);
                CHECK_BOOL(alloc2Offset + (4 * MEGABYTE) <= alloc0Offset || alloc0Offset + (8 * MEGABYTE) <= alloc2Offset); // Check if they don't overlap.

                // # Calculate statistics

                D3D12MA_StatInfo statInfo = default;
                block.Get()->CalculateStats(&statInfo);

                CHECK_BOOL(statInfo.AllocationCount == 2);
                CHECK_BOOL(statInfo.BlockCount == 1);
                CHECK_BOOL(statInfo.UsedBytes == blockSize);
                CHECK_BOOL(statInfo.UnusedBytes + statInfo.UsedBytes == blockSize);

                // # Generate JSON dump

                ushort* json = null;
                block.Get()->BuildStatsString(&json);
                {
                    string str = Marshal.PtrToStringUni((IntPtr)json)!;
                    CHECK_BOOL(str.IndexOf("\"UserData\": 1") != -1);
                    CHECK_BOOL(str.IndexOf("\"UserData\": 2") != -1);
                }
                block.Get()->FreeStatsString(json);

                // # Free alloc0, leave alloc2 unfreed.

                block.Get()->FreeAllocation(alloc0Offset);

                // # Test alignment

                {
                    const nuint allocCount = 10;
                    ulong* allocOffset = stackalloc ulong[(int)allocCount];

                    for (nuint i = 0; i < allocCount; ++i)
                    {
                        bool alignment0 = i == allocCount - 1;

                        allocDesc.Size = (i * 3) + 15;
                        allocDesc.Alignment = alignment0 ? 0ul : 8ul;

                        CHECK_HR(block.Get()->Allocate(&allocDesc, &allocOffset[i]));

                        if (!alignment0)
                        {
                            CHECK_BOOL(allocOffset[i] % allocDesc.Alignment == 0);
                        }
                    }

                    for (nuint i = allocCount; unchecked(i-- != 0);)
                    {
                        block.Get()->FreeAllocation(allocOffset[i]);
                    }
                }

                // # Final cleanup

                block.Get()->FreeAllocation(alloc2Offset);

                //block->Clear();
            }
            finally
            {
                block.Dispose();
            }
        }

        private static void TestFrameIndexAndJson([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            const ulong bufSize = 32ul * 1024;

            D3D12MA_ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;
            allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(out resourceDesc, bufSize);

            const uint BEGIN_INDEX = 10;
            const uint END_INDEX = 20;

            for (uint frameIndex = BEGIN_INDEX; frameIndex < END_INDEX; ++frameIndex)
            {
                ctx.allocator->SetCurrentFrameIndex(frameIndex);
                D3D12MA_Allocation* alloc = null;

                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    &alloc,
                    null,
                    null));

                ushort* pStatsString;
                ctx.allocator->BuildStatsString(&pStatsString, TRUE);
                string statsString = Marshal.PtrToStringUni((IntPtr)pStatsString)!;

                for (uint testIndex = BEGIN_INDEX; testIndex < END_INDEX; ++testIndex)
                {
                    string buffer = $"\"CreationFrameIndex\": {testIndex}";
                    if (testIndex == frameIndex)
                    {
                        CHECK_BOOL(statsString.IndexOf(buffer) != -1);
                    }
                    else
                    {
                        CHECK_BOOL(statsString.IndexOf(buffer) == -1);
                    }
                }

                ctx.allocator->FreeStatsString(pStatsString);
                _ = alloc->Release();
            }
        }

        private static void TestCommittedResourcesAndJson([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test committed resources and JSON");

            const uint count = 4;
            const ulong bufSize = 32ul * 1024;

            string?[] names = new string?[(int)count] {
                "Resource\nFoo\r\nBar",
                "Resource \"'&<>?#@!&-=_+[]{};:,./\\",
                null,
                ""
            };

            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            for (uint i = 0; i < count; i++)
            {
                ResourceWithAllocation._ctor(ref resources[i]);
            }

            try
            {
                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;

                D3D12_RESOURCE_DESC resourceDesc;
                FillResourceDescForBuffer(out resourceDesc, bufSize);

                for (uint i = 0; i < count; ++i)
                {
                    bool receiveExplicitResource = i < 2;

                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDesc,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_COPY_DEST,
                        null,
                        resources[i].allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), receiveExplicitResource ? (void**)&resources[i].resource : null
                    ));

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

                    fixed (char* name = names[i])
                    {
                        resources[i].allocation.Get()->SetName((ushort*)name);
                    }
                }

                // Check names.
                for (uint i = 0; i < count; ++i)
                {
                    ushort* pAllocName = resources[i].allocation.Get()->GetName();
                    string? allocName = Marshal.PtrToStringUni((IntPtr)pAllocName);

                    if (allocName != null)
                    {
                        CHECK_BOOL(string.Compare(allocName, names[i]) == 0);
                    }
                    else
                    {
                        CHECK_BOOL(names[i] == null);
                    }
                }

                ushort* pJsonString;
                ctx.allocator->BuildStatsString(&pJsonString, TRUE);
                string jsonString = Marshal.PtrToStringUni((IntPtr)pJsonString)!;

                CHECK_BOOL(jsonString.IndexOf("\"Resource\\nFoo\\r\\nBar\"") != -1);
                CHECK_BOOL(jsonString.IndexOf("\"Resource \\\"'&<>?#@!&-=_+[]{};:,.\\/\\\\\"") != -1);
                CHECK_BOOL(jsonString.IndexOf("\"\"") != -1);

                ctx.allocator->FreeStatsString(pJsonString);
            }
            finally
            {
                for (uint i = 0; i < count; i++)
                {
                    resources[i].Dispose();
                }
            }
        }

        private static void TestCustomHeapFlags([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test custom heap flags");

            // 1. Just memory heap with custom flags
            {
                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAG_SHARED; // Extra flag.

                D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = default;
                resAllocInfo.SizeInBytes = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
                resAllocInfo.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;                

                ResourceWithAllocation res;
                Unsafe.SkipInit(out res);
                ResourceWithAllocation._ctor(ref res);

                try
                {
                    CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &resAllocInfo, res.allocation.GetAddressOf()));

                    // Must be created as separate allocation.
                    CHECK_BOOL(res.allocation.Get()->GetOffset() == 0);
                }
                finally
                {
                    res.Dispose();
                }
            }

            // 2. Committed resource with custom flags
            {
                D3D12_RESOURCE_DESC resourceDesc = default;
                resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                resourceDesc.Alignment = 0;
                resourceDesc.Width = 1920;
                resourceDesc.Height = 1080;
                resourceDesc.DepthOrArraySize = 1;
                resourceDesc.MipLevels = 1;
                resourceDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
                resourceDesc.SampleDesc.Count = 1;
                resourceDesc.SampleDesc.Quality = 0;
                resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
                resourceDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_CROSS_ADAPTER;

                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_SHARED | D3D12_HEAP_FLAG_SHARED_CROSS_ADAPTER; // Extra flags.

                ResourceWithAllocation res;
                Unsafe.SkipInit(out res);
                ResourceWithAllocation._ctor(ref res);

                try
                {
                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDesc,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_COMMON,
                        null,
                        res.allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&res.resource
                    ));

                    // Must be created as committed.
                    CHECK_BOOL(res.allocation.Get()->GetHeap() == null);
                }
                finally
                {
                    res.Dispose();
                }
            }
        }

        private static void TestPlacedResources([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test placed resources");

            bool alwaysCommitted = (ctx.allocatorFlags & D3D12MA_ALLOCATOR_FLAG_ALWAYS_COMMITTED) != 0;

            const uint count = 4;
            const ulong bufSize = 32ul * 1024;

            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            for (uint i = 0; i < count; i++)
            {
                ResourceWithAllocation._ctor(ref resources[i]);
            }

            try
            {
                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

                D3D12_RESOURCE_DESC resourceDesc;
                FillResourceDescForBuffer(out resourceDesc, bufSize);

                for (uint i = 0; i < count; ++i)
                {
                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDesc,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_GENERIC_READ,
                        null,
                        resources[i].allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&resources[i].resource
                    ));

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

                            CHECK_BOOL(
                                ((resI.allocation.Get()->GetOffset() + resI.allocation.Get()->GetSize()) <= resJ.allocation.Get()->GetOffset()) ||
                                ((resJ.allocation.Get()->GetOffset() + resJ.allocation.Get()->GetSize()) <= resI.allocation.Get()->GetOffset())
                            );
                        }
                    }
                }

                if (!alwaysCommitted)
                {
                    CHECK_BOOL(sameHeapFound);
                }

                // Additionally create a texture to see if no error occurs due to bad handling of Resource Tier.
                resourceDesc = default;
                resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                resourceDesc.Alignment = 0;
                resourceDesc.Width = 1024;
                resourceDesc.Height = 1024;
                resourceDesc.DepthOrArraySize = 1;
                resourceDesc.MipLevels = 1;
                resourceDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
                resourceDesc.SampleDesc.Count = 1;
                resourceDesc.SampleDesc.Quality = 0;
                resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
                resourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

                ResourceWithAllocation textureRes;
                Unsafe.SkipInit(out textureRes);
                ResourceWithAllocation._ctor(ref textureRes);

                try
                {
                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDesc,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_COPY_DEST,
                        null,
                        textureRes.allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&textureRes.resource
                    ));
                }
                finally
                {
                    textureRes.Dispose();
                }

                // Additionally create an MSAA render target to see if no error occurs due to bad handling of Resource Tier.
                resourceDesc = default;
                resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                resourceDesc.Alignment = 0;
                resourceDesc.Width = 1920;
                resourceDesc.Height = 1080;
                resourceDesc.DepthOrArraySize = 1;
                resourceDesc.MipLevels = 1;
                resourceDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
                resourceDesc.SampleDesc.Count = 2;
                resourceDesc.SampleDesc.Quality = 0;
                resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
                resourceDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

                ResourceWithAllocation renderTargetRes;
                Unsafe.SkipInit(out renderTargetRes);
                ResourceWithAllocation._ctor(ref renderTargetRes);

                try
                {
                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDesc,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_RENDER_TARGET,
                        null,
                        renderTargetRes.allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&renderTargetRes.resource
                    ));
                }
                finally
                {
                    renderTargetRes.Dispose();
                }
            }
            finally
            {
                for (uint i = 0; i < count; i++)
                {
                    resources[i].Dispose();
                }
            }
        }

        private static void TestOtherComInterface([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test other COM interface");

            D3D12_RESOURCE_DESC resDesc;
            FillResourceDescForBuffer(out resDesc, 0x10000);

            for (uint i = 0; i < 2; ++i)
            {
                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

                if (i == 1)
                {
                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;
                }

                using ComPtr< D3D12MA_Allocation> alloc = default;
                using ComPtr<ID3D12Pageable> pageable = default;

                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resDesc,
                    D3D12_RESOURCE_STATE_COMMON,
                    null, // pOptimizedClearValue
                    alloc.GetAddressOf(),
                    __uuidof<ID3D12Pageable>(), (void**)&pageable
                ));

                // Do something with the interface to make sure it's valid.
                using ComPtr<ID3D12Device> device = default;
                CHECK_HR(pageable.Get()->GetDevice(__uuidof<ID3D12Device>(), (void**)&device));
                CHECK_BOOL(device == ctx.device);
            }
        }

        private static void TestCustomPools([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test custom pools");

            // # Fetch global stats 1

            D3D12MA_Stats globalStatsBeg = default;
            ctx.allocator->CalculateStats(&globalStatsBeg);

            // # Create pool, 1..2 blocks of 11 MB

            D3D12MA_POOL_DESC poolDesc = default;
            poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
            poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
            poolDesc.BlockSize = 11 * MEGABYTE;
            poolDesc.MinBlockCount = 1;
            poolDesc.MaxBlockCount = 2;

            using ComPtr<D3D12MA_Pool> pool = default;

            CHECK_HR(ctx.allocator->CreatePool(&poolDesc, pool.GetAddressOf()));

            try
            {
                // # Validate stats for empty pool

                D3D12MA_StatInfo poolStats = default;
                pool.Get()->CalculateStats(&poolStats);

                CHECK_BOOL(poolStats.BlockCount == 1);
                CHECK_BOOL(poolStats.AllocationCount == 0);
                CHECK_BOOL(poolStats.UsedBytes == 0);
                CHECK_BOOL(poolStats.UnusedBytes == poolStats.BlockCount * poolDesc.BlockSize);

                // # SetName and GetName

                const string NAME = "Custom pool name 1";

                fixed (char* name = NAME)
                {
                    pool.Get()->SetName((ushort*)name);
                }

                var poolName = Marshal.PtrToStringUni((IntPtr)pool.Get()->GetName());

                CHECK_BOOL(string.Compare(poolName, NAME) == 0);

                // # Create buffers 2x 5 MB

                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.CustomPool = pool.Get();
                allocDesc.ExtraHeapFlags = unchecked((D3D12_HEAP_FLAGS)0xCDCDCDCD); // Should be ignored.
                allocDesc.HeapType = unchecked((D3D12_HEAP_TYPE)0xCDCDCDCD); // Should be ignored.

                const ulong BUFFER_SIZE = 5 * MEGABYTE;

                D3D12_RESOURCE_DESC resDesc;
                FillResourceDescForBuffer(out resDesc, BUFFER_SIZE);

                ComPtr<D3D12MA_Allocation>* allocs = stackalloc ComPtr<D3D12MA_Allocation>[4];

                try
                {
                    for (uint i = 0; i < 2; ++i)
                    {
                        CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc,
                            D3D12_RESOURCE_STATE_GENERIC_READ,
                            null, // pOptimizedClearValue
                            allocs[i].GetAddressOf(),
                            __uuidof<ID3D12Resource>(), null // riidResource, ppvResource
                        ));
                    }

                    // # Validate pool stats now

                    pool.Get()->CalculateStats(&poolStats);

                    CHECK_BOOL(poolStats.BlockCount == 1);
                    CHECK_BOOL(poolStats.AllocationCount == 2);
                    CHECK_BOOL(poolStats.UsedBytes == 2 * BUFFER_SIZE);
                    CHECK_BOOL(poolStats.UnusedBytes == poolDesc.BlockSize - poolStats.UsedBytes);

                    // # Check that global stats are updated as well

                    D3D12MA_Stats globalStatsCurr = default;
                    ctx.allocator->CalculateStats(&globalStatsCurr);

                    CHECK_BOOL(globalStatsCurr.Total.AllocationCount == globalStatsBeg.Total.AllocationCount + poolStats.AllocationCount);
                    CHECK_BOOL(globalStatsCurr.Total.BlockCount == globalStatsBeg.Total.BlockCount + poolStats.BlockCount);
                    CHECK_BOOL(globalStatsCurr.Total.UsedBytes == globalStatsBeg.Total.UsedBytes + poolStats.UsedBytes);

                    // # NEVER_ALLOCATE and COMMITTED should fail
                    // (Committed allocations not allowed in this pool because BlockSize != 0.)

                    for (uint i = 0; i < 2; ++i)
                    {
                        allocDesc.Flags = i == 0 ? D3D12MA_ALLOCATION_FLAG_NEVER_ALLOCATE : D3D12MA_ALLOCATION_FLAG_COMMITTED;

                        using ComPtr<D3D12MA_Allocation> alloc = default;

                        int hr = ctx.allocator->CreateResource(&allocDesc, &resDesc,
                            D3D12_RESOURCE_STATE_GENERIC_READ,
                            null, // pOptimizedClearValue
                            alloc.GetAddressOf(),
                            __uuidof<ID3D12Resource>(), null // riidResource, ppvResource
                        );

                        CHECK_BOOL(FAILED(hr));
                    }

                    // # 3 more buffers. 3rd should fail.

                    allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_NONE;
                    for (uint i = 2; i < 5; ++i)
                    {
                        using ComPtr<D3D12MA_Allocation> alloc = default;

                        HRESULT hr = ctx.allocator->CreateResource(&allocDesc, &resDesc,
                            D3D12_RESOURCE_STATE_GENERIC_READ,
                            null, // pOptimizedClearValue
                            alloc.GetAddressOf(),
                            __uuidof<ID3D12Resource>(), null // riidResource, ppvResource
                        );

                        if (i < 4)
                        {
                            CHECK_HR(hr);
                            allocs[i].Attach(alloc.Detach());
                        }
                        else
                        {
                            CHECK_BOOL(FAILED(hr));
                        }
                    }

                    pool.Get()->CalculateStats(&poolStats);

                    CHECK_BOOL(poolStats.BlockCount == 2);
                    CHECK_BOOL(poolStats.AllocationCount == 4);
                    CHECK_BOOL(poolStats.UsedBytes == 4 * BUFFER_SIZE);
                    CHECK_BOOL(poolStats.UnusedBytes == (poolStats.BlockCount * poolDesc.BlockSize) - poolStats.UsedBytes);

                    // # Make room, AllocateMemory, CreateAliasingResource

                    _ = allocs[3].Reset();
                    _ = allocs[0].Reset();

                    D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = default;
                    resAllocInfo.SizeInBytes = 5 * MEGABYTE;
                    resAllocInfo.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

                    CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &resAllocInfo, allocs[0].GetAddressOf()));

                    resDesc.Width = 1 * MEGABYTE;
                    using ComPtr<ID3D12Resource> res = default;
                    CHECK_HR(ctx.allocator->CreateAliasingResource(
                        allocs[0].Get(),
                        0, // AllocationLocalOffset
                        &resDesc,
                        D3D12_RESOURCE_STATE_GENERIC_READ,
                        null, // pOptimizedClearValue
                        __uuidof<ID3D12Resource>(), (void**)&res
                    ));
                }
                finally
                {
                    allocs[0].Dispose();
                    allocs[1].Dispose();
                    allocs[2].Dispose();
                    allocs[3].Dispose();
                }
            }
            finally
            {
                pool.Dispose();
            }
        }

        private static void TestCustomPool_MinAllocationAlignment([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test custom pool MinAllocationAlignment");

            const ulong BUFFER_SIZE = 32;
            const nuint BUFFER_COUNT = 4;
            const ulong MIN_ALIGNMENT = 128 * 1024;

            D3D12MA_POOL_DESC poolDesc = default;
            poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;
            poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
            poolDesc.MinAllocationAlignment = MIN_ALIGNMENT;

            ComPtr<D3D12MA_Pool> pool = default;

            CHECK_HR(ctx.allocator->CreatePool(&poolDesc, pool.GetAddressOf()));

            try
            {
                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.CustomPool = pool.Get();

                D3D12_RESOURCE_DESC resDesc;
                FillResourceDescForBuffer(out resDesc, BUFFER_SIZE);

                ComPtr<D3D12MA_Allocation>* allocs = stackalloc ComPtr<D3D12MA_Allocation>[(int)BUFFER_COUNT];

                try
                {
                    for (nuint i = 0; i < BUFFER_COUNT; ++i)
                    {
                        CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc,
                            D3D12_RESOURCE_STATE_GENERIC_READ,
                            null, // pOptimizedClearValue
                            allocs[i].GetAddressOf(),
                            null,
                            null)); // riidResource, ppvResource
                        CHECK_BOOL(allocs[i].Get()->GetOffset() % MIN_ALIGNMENT == 0);
                    }
                }
                finally
                {
                    for (nuint i = 0; i < BUFFER_COUNT; i++)
                    {
                        allocs[(int)i].Dispose();
                    }
                }
            }
            finally
            {
                pool.Dispose();
            }
        }

        private static HRESULT TestCustomHeap([NativeTypeName("const TestContext&")] in TestContext ctx, [NativeTypeName("const D3D12_HEAP_PROPERTIES&")] in D3D12_HEAP_PROPERTIES heapProps)
        {
            D3D12MA_Stats globalStatsBeg = default;
            ctx.allocator->CalculateStats(&globalStatsBeg);

            D3D12MA_POOL_DESC poolDesc = default;
            poolDesc.HeapProperties = heapProps;
            poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
            poolDesc.BlockSize = 10 * MEGABYTE;
            poolDesc.MinBlockCount = 1;
            poolDesc.MaxBlockCount = 1;

            const ulong BUFFER_SIZE = 1 * MEGABYTE;

            ComPtr<D3D12MA_Pool> pool = default;

            HRESULT hr = ctx.allocator->CreatePool(&poolDesc, pool.GetAddressOf());

            try
            {
                if (SUCCEEDED(hr))
                {
                    D3D12MA_ALLOCATION_DESC allocDesc = default;
                    allocDesc.CustomPool = pool.Get();

                    D3D12_RESOURCE_DESC resDesc;
                    FillResourceDescForBuffer(out resDesc, BUFFER_SIZE);

                    // Pool already allocated a block. We don't expect CreatePlacedResource to fail.
                    using ComPtr<D3D12MA_Allocation> alloc = default;

                    CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc,
                        D3D12_RESOURCE_STATE_COPY_DEST,
                        null, // pOptimizedClearValue
                        alloc.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), null)); // riidResource, ppvResource

                    D3D12MA_Stats globalStatsCurr = default;
                    ctx.allocator->CalculateStats(&globalStatsCurr);

                    // Make sure it is accounted only in CUSTOM heap not any of the standard heaps.
                    CHECK_BOOL(memcmp(Unsafe.AsPointer(ref globalStatsCurr.HeapType[0]), Unsafe.AsPointer(ref globalStatsBeg.HeapType[0]), (nuint)sizeof(D3D12MA_StatInfo)) == 0);
                    CHECK_BOOL(memcmp(Unsafe.AsPointer(ref globalStatsCurr.HeapType[1]), Unsafe.AsPointer(ref globalStatsBeg.HeapType[1]), (nuint)sizeof(D3D12MA_StatInfo)) == 0);
                    CHECK_BOOL(memcmp(Unsafe.AsPointer(ref globalStatsCurr.HeapType[2]), Unsafe.AsPointer(ref globalStatsBeg.HeapType[2]), (nuint)sizeof(D3D12MA_StatInfo)) == 0);
                    CHECK_BOOL(globalStatsCurr.HeapType[3].AllocationCount == globalStatsBeg.HeapType[3].AllocationCount + 1);
                    CHECK_BOOL(globalStatsCurr.HeapType[3].BlockCount == globalStatsBeg.HeapType[3].BlockCount + 1);
                    CHECK_BOOL(globalStatsCurr.HeapType[3].UsedBytes == globalStatsBeg.HeapType[3].UsedBytes + BUFFER_SIZE);
                    CHECK_BOOL(globalStatsCurr.Total.AllocationCount == globalStatsBeg.Total.AllocationCount + 1);
                    CHECK_BOOL(globalStatsCurr.Total.BlockCount == globalStatsBeg.Total.BlockCount + 1);
                    CHECK_BOOL(globalStatsCurr.Total.UsedBytes == globalStatsBeg.Total.UsedBytes + BUFFER_SIZE);

                    // Map and write some data.
                    if (heapProps.CPUPageProperty == D3D12_CPU_PAGE_PROPERTY_WRITE_COMBINE ||
                        heapProps.CPUPageProperty == D3D12_CPU_PAGE_PROPERTY_WRITE_BACK)
                    {
                        ID3D12Resource* res = alloc.Get()->GetResource();

                        uint* mappedPtr = null;
                        D3D12_RANGE readRange = default;
                        CHECK_HR(res->Map(0, &readRange, (void**)&mappedPtr));

                        *mappedPtr = 0xDEADC0DE;

                        res->Unmap(0, null);
                    }
                }
            }
            finally
            {
                pool.Dispose();
            }

            return hr;
        }

        private static void TestCustomHeaps([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test custom heap");

            D3D12_HEAP_PROPERTIES heapProps = default;

            // Use custom pool but the same as READBACK, which should be always available.
            heapProps.Type = D3D12_HEAP_TYPE_CUSTOM;
            heapProps.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_WRITE_BACK;
            heapProps.MemoryPoolPreference = D3D12_MEMORY_POOL_L0; // System memory
            HRESULT hr = TestCustomHeap(ctx, heapProps);
            CHECK_HR(hr);
        }

        private static void TestStandardCustomCommittedPlaced([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test standard, custom, committed, placed\n");

            const D3D12_HEAP_TYPE heapType = D3D12_HEAP_TYPE_DEFAULT;
            const ulong bufferSize = 1024;

            D3D12MA_POOL_DESC poolDesc = default;
            poolDesc.HeapProperties.Type = heapType;
            poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

            ComPtr<D3D12MA_Pool> pool = default;

            CHECK_HR(ctx.allocator->CreatePool(&poolDesc, pool.GetAddressOf()));

            try
            {
                var allocations = new List<ComPtr<D3D12MA_Allocation>>();

                try
                {
                    D3D12_RESOURCE_DESC resDesc;
                    FillResourceDescForBuffer(out resDesc, bufferSize);

                    for (uint standardCustomI = 0; standardCustomI < 2; ++standardCustomI)
                    {
                        bool useCustomPool = standardCustomI > 0;
                        for (uint flagsI = 0; flagsI < 3; ++flagsI)
                        {
                            bool useCommitted = flagsI > 0;
                            bool neverAllocate = flagsI > 1;

                            D3D12MA_ALLOCATION_DESC allocDesc = default;
                            if (useCustomPool)
                            {
                                allocDesc.CustomPool = pool.Get();
                                allocDesc.HeapType = unchecked((D3D12_HEAP_TYPE)0xCDCDCDCD); // Should be ignored.
                                allocDesc.ExtraHeapFlags = unchecked((D3D12_HEAP_FLAGS)0xCDCDCDCD); // Should be ignored.
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

                            ComPtr<D3D12MA_Allocation> allocPtr = default;

                            int hr = ctx.allocator->CreateResource(
                                &allocDesc,
                                &resDesc,
                                D3D12_RESOURCE_STATE_COMMON,
                                null, // pOptimizedClearValue
                                allocPtr.GetAddressOf(),
                                null,
                                null);

                            if (allocPtr.Get() != null)
                            {
                                allocations.Add(allocPtr);
                            }

                            bool expectSuccess = !neverAllocate; // NEVER_ALLOCATE should always fail with COMMITTED.
                            CHECK_BOOL(expectSuccess == SUCCEEDED(hr));
                            if (SUCCEEDED(hr) && useCommitted)
                            {
                                CHECK_BOOL(allocPtr.Get()->GetHeap() == null); // Committed allocation has implicit heap.
                            }
                        }
                    }
                }
                finally
                {
                    foreach (ref ComPtr<D3D12MA_Allocation> allocation in CollectionsMarshal.AsSpan(allocations))
                    {
                        allocation.Dispose();
                    }
                }
            }
            finally
            {
                pool.Dispose();
            }
        }

        private static void TestAliasingMemory([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test aliasing memory");

            D3D12_RESOURCE_DESC resDesc1 = default;
            resDesc1.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
            resDesc1.Alignment = 0;
            resDesc1.Width = 1920;
            resDesc1.Height = 1080;
            resDesc1.DepthOrArraySize = 1;
            resDesc1.MipLevels = 1;
            resDesc1.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
            resDesc1.SampleDesc.Count = 1;
            resDesc1.SampleDesc.Quality = 0;
            resDesc1.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
            resDesc1.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;

            D3D12_RESOURCE_DESC resDesc2 = default;
            resDesc2.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
            resDesc2.Alignment = 0;
            resDesc2.Width = 1024;
            resDesc2.Height = 1024;
            resDesc2.DepthOrArraySize = 1;
            resDesc2.MipLevels = 0;
            resDesc2.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
            resDesc2.SampleDesc.Count = 1;
            resDesc2.SampleDesc.Quality = 0;
            resDesc2.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
            resDesc2.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

            D3D12_RESOURCE_ALLOCATION_INFO allocInfo1 = ctx.device->GetResourceAllocationInfo(0, 1, &resDesc1);
            D3D12_RESOURCE_ALLOCATION_INFO allocInfo2 = ctx.device->GetResourceAllocationInfo(0, 1, &resDesc2);

            D3D12_RESOURCE_ALLOCATION_INFO finalAllocInfo = default;
            finalAllocInfo.Alignment = Math.Max(allocInfo1.Alignment, allocInfo2.Alignment);
            finalAllocInfo.SizeInBytes = Math.Max(allocInfo1.SizeInBytes, allocInfo2.SizeInBytes);

            D3D12MA_ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
            allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES;

            using ComPtr<D3D12MA_Allocation> alloc = default;

            CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &finalAllocInfo, alloc.GetAddressOf()));
            CHECK_BOOL((alloc.Get() != null) && (alloc.Get()->GetHeap() != null));

            ID3D12Resource* res1 = null;
            CHECK_HR(ctx.allocator->CreateAliasingResource(
                alloc,
                0, // AllocationLocalOffset
                &resDesc1,
                D3D12_RESOURCE_STATE_COMMON,
                null, // pOptimizedClearValue
                __uuidof<ID3D12Resource>(), (void**)&res1
            ));
            CHECK_BOOL(res1 != null);

            ID3D12Resource* res2 = null;
            CHECK_HR(ctx.allocator->CreateAliasingResource(
                alloc,
                0, // AllocationLocalOffset
                &resDesc2,
                D3D12_RESOURCE_STATE_COMMON,
                null, // pOptimizedClearValue
                __uuidof<ID3D12Resource>(), (void**)&res2
            ));
            CHECK_BOOL(res2 != null);

            // You can use res1 and res2, but not at the same time!

            _ = res2->Release();
            _ = res1->Release();
        }

        private static void TestMapping([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test mapping");

            const uint count = 10;
            const ulong bufSize = 32ul * 1024;

            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            for (uint i = 0; i < count; i++)
            {
                ResourceWithAllocation._ctor(ref resources[i]);
            }

            try
            {
                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

                D3D12_RESOURCE_DESC resourceDesc;
                FillResourceDescForBuffer(out resourceDesc, bufSize);

                for (uint i = 0; i < count; ++i)
                {
                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDesc,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_GENERIC_READ,
                        null,
                        resources[i].allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&resources[i].resource
                    ));

                    void* mappedPtr = null;
                    CHECK_HR(resources[i].resource.Get()->Map(0, EMPTY_RANGE, &mappedPtr));

                    FillData(mappedPtr, bufSize, i);

                    // Unmap every other buffer. Leave others mapped.
                    if ((i % 2) != 0)
                    {
                        resources[i].resource.Get()->Unmap(0, null);
                    }
                }
            }
            finally
            {
                for (uint i = 0; i < count; i++)
                {
                    resources[i].Dispose();
                }
            }
        }

        private static bool StatInfoEqual([NativeTypeName("const D3D12MA_StatInfo&")] in D3D12MA_StatInfo lhs, [NativeTypeName("const D3D12MA_StatInfo&")] in D3D12MA_StatInfo rhs)
        {
            return (lhs.BlockCount == rhs.BlockCount) &&
                   (lhs.AllocationCount == rhs.AllocationCount) &&
                   (lhs.UnusedRangeCount == rhs.UnusedRangeCount) &&
                   (lhs.UsedBytes == rhs.UsedBytes) &&
                   (lhs.UnusedBytes == rhs.UnusedBytes) &&
                   (lhs.AllocationSizeMin == rhs.AllocationSizeMin) &&
                   (lhs.AllocationSizeMax == rhs.AllocationSizeMax) &&
                   (lhs.AllocationSizeAvg == rhs.AllocationSizeAvg) &&
                   (lhs.UnusedRangeSizeMin == rhs.UnusedRangeSizeMin) &&
                   (lhs.UnusedRangeSizeMax == rhs.UnusedRangeSizeMax) &&
                   (lhs.UnusedRangeSizeAvg == rhs.UnusedRangeSizeAvg);
        }

        private static void CheckStatInfo([NativeTypeName("const D3D12MA_StatInfo&")] in D3D12MA_StatInfo statInfo)
        {
            if (statInfo.AllocationCount > 0)
            {
                CHECK_BOOL(
                    (statInfo.AllocationSizeAvg >= statInfo.AllocationSizeMin) &&
                    (statInfo.AllocationSizeAvg <= statInfo.AllocationSizeMax)
                );
            }

            if (statInfo.UsedBytes > 0)
            {
                CHECK_BOOL(statInfo.AllocationCount > 0);
            }

            if (statInfo.UnusedRangeCount > 0)
            {
                CHECK_BOOL(
                    (statInfo.UnusedRangeSizeAvg >= statInfo.UnusedRangeSizeMin) &&
                    (statInfo.UnusedRangeSizeAvg <= statInfo.UnusedRangeSizeMax)
                );
                CHECK_BOOL(statInfo.UnusedRangeSizeMin > 0);
                CHECK_BOOL(statInfo.UnusedRangeSizeMax > 0);
            }
        }

        private static void TestStats([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test stats");

            D3D12MA_Stats begStats = default;
            ctx.allocator->CalculateStats(&begStats);

            const uint count = 10;
            const ulong bufSize = 64ul * 1024;

            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            for (uint i = 0; i < count; i++)
            {
                ResourceWithAllocation._ctor(ref resources[i]);
            }

            try
            {
                D3D12MA_ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

                D3D12_RESOURCE_DESC resourceDesc;
                FillResourceDescForBuffer(out resourceDesc, bufSize);

                for (uint i = 0; i < count; ++i)
                {
                    if (i == count / 2)
                    {
                        allocDesc.Flags |= D3D12MA_ALLOCATION_FLAG_COMMITTED;
                    }

                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDesc,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_GENERIC_READ,
                        null,
                        resources[i].allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&resources[i].resource
                    ));
                }

                D3D12MA_Stats endStats = default;
                ctx.allocator->CalculateStats(&endStats);

                CHECK_BOOL(endStats.Total.BlockCount >= begStats.Total.BlockCount);
                CHECK_BOOL(endStats.Total.AllocationCount == begStats.Total.AllocationCount + count);
                CHECK_BOOL(endStats.Total.UsedBytes == begStats.Total.UsedBytes + (count * bufSize));
                CHECK_BOOL(endStats.Total.AllocationSizeMin <= bufSize);
                CHECK_BOOL(endStats.Total.AllocationSizeMax >= bufSize);

                CHECK_BOOL(endStats.HeapType[1].BlockCount >= begStats.HeapType[1].BlockCount);
                CHECK_BOOL(endStats.HeapType[1].AllocationCount >= begStats.HeapType[1].AllocationCount + count);
                CHECK_BOOL(endStats.HeapType[1].UsedBytes >= begStats.HeapType[1].UsedBytes + (count * bufSize));
                CHECK_BOOL(endStats.HeapType[1].AllocationSizeMin <= bufSize);
                CHECK_BOOL(endStats.HeapType[1].AllocationSizeMax >= bufSize);

                CHECK_BOOL(StatInfoEqual(begStats.HeapType[0], endStats.HeapType[0]));
                CHECK_BOOL(StatInfoEqual(begStats.HeapType[2], endStats.HeapType[2]));

                CheckStatInfo(endStats.Total);
                CheckStatInfo(endStats.HeapType[0]);
                CheckStatInfo(endStats.HeapType[1]);
                CheckStatInfo(endStats.HeapType[2]);

                D3D12MA_Budget gpuBudget = default, cpuBudget = default;
                ctx.allocator->GetBudget(&gpuBudget, &cpuBudget);

                CHECK_BOOL(gpuBudget.AllocationBytes <= gpuBudget.BlockBytes);
                CHECK_BOOL(gpuBudget.AllocationBytes == endStats.HeapType[0].UsedBytes);
                CHECK_BOOL(gpuBudget.BlockBytes == endStats.HeapType[0].UsedBytes + endStats.HeapType[0].UnusedBytes);

                CHECK_BOOL(cpuBudget.AllocationBytes <= cpuBudget.BlockBytes);
                CHECK_BOOL(cpuBudget.AllocationBytes == endStats.HeapType[1].UsedBytes + endStats.HeapType[2].UsedBytes);
                CHECK_BOOL(cpuBudget.BlockBytes == (endStats.HeapType[1].UsedBytes + endStats.HeapType[1].UnusedBytes + endStats.HeapType[2].UsedBytes + endStats.HeapType[2].UnusedBytes));
            }
            finally
            {
                for (uint i = 0; i < count; i++)
                {
                    resources[i].Dispose();
                }
            }
        }

        private static void TestTransfer([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test mapping");

            const uint count = 10;
            const ulong bufSize = 32ul * 1024;

            ResourceWithAllocation* resourcesUpload = stackalloc ResourceWithAllocation[(int)count];

            for (uint i = 0; i < count; i++)
            {
                ResourceWithAllocation._ctor(ref resourcesUpload[i]);
            }

            ResourceWithAllocation* resourcesDefault = stackalloc ResourceWithAllocation[(int)count];

            for (uint i = 0; i < count; i++)
            {
                ResourceWithAllocation._ctor(ref resourcesDefault[i]);
            }

            ResourceWithAllocation* resourcesReadback = stackalloc ResourceWithAllocation[(int)count];

            for (uint i = 0; i < count; i++)
            {
                ResourceWithAllocation._ctor(ref resourcesReadback[i]);
            }

            try
            {
                D3D12MA_ALLOCATION_DESC allocDescUpload = default;
                allocDescUpload.HeapType = D3D12_HEAP_TYPE_UPLOAD;

                D3D12MA_ALLOCATION_DESC allocDescDefault = default;
                allocDescDefault.HeapType = D3D12_HEAP_TYPE_DEFAULT;

                D3D12MA_ALLOCATION_DESC allocDescReadback = default;
                allocDescReadback.HeapType = D3D12_HEAP_TYPE_READBACK;

                D3D12_RESOURCE_DESC resourceDesc;
                FillResourceDescForBuffer(out resourceDesc, bufSize);

                // Create 3 sets of resources.
                for (uint i = 0; i < count; ++i)
                {
                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDescUpload,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_GENERIC_READ,
                        null,
                        resourcesUpload[i].allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&resourcesUpload[i].resource
                    ));

                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDescDefault,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_COPY_DEST,
                        null,
                        resourcesDefault[i].allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&resourcesDefault[i].resource
                    ));

                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDescReadback,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_COPY_DEST,
                        null,
                        resourcesReadback[i].allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&resourcesReadback[i].resource
                    ));
                }

                // Map and fill data in UPLOAD.
                for (uint i = 0; i < count; ++i)
                {
                    void* mappedPtr = null;
                    CHECK_HR(resourcesUpload[i].resource.Get()->Map(0, EMPTY_RANGE, &mappedPtr));

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
                    cmdList->CopyBufferRegion(resourcesDefault[i].resource, 0, resourcesUpload[i].resource, 0, bufSize);
                }

                D3D12_RESOURCE_BARRIER* barriers = stackalloc D3D12_RESOURCE_BARRIER[(int)count];

                for (uint i = 0; i < count; ++i)
                {
                    barriers[i].Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
                    barriers[i].Transition.pResource = resourcesDefault[i].resource;
                    barriers[i].Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
                    barriers[i].Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_SOURCE;
                    barriers[i].Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
                }

                cmdList->ResourceBarrier(count, barriers);

                for (uint i = 0; i < count; ++i)
                {
                    cmdList->CopyBufferRegion(resourcesReadback[i].resource, 0, resourcesDefault[i].resource, 0, bufSize);
                }

                EndCommandList(cmdList);

                // Validate READBACK buffers.
                for (uint i = count; unchecked(i-- != 0);)
                {
                    D3D12_RANGE mapRange = new D3D12_RANGE { Begin = 0, End = (nuint)bufSize };
                    void* mappedPtr = null;
                    CHECK_HR(resourcesReadback[i].resource.Get()->Map(0, &mapRange, &mappedPtr));

                    CHECK_BOOL(ValidateData(mappedPtr, bufSize, i));

                    // Unmap every 3rd resource, leave others mapped.
                    if ((i % 3) != 0)
                    {
                        resourcesReadback[i].resource.Get()->Unmap(0, EMPTY_RANGE);
                    }
                }
            }
            finally
            {
                for (uint i = 0; i < count; i++)
                {
                    resourcesUpload[i].Dispose();
                }

                for (uint i = 0; i < count; i++)
                {
                    resourcesDefault[i].Dispose();
                }

                for (uint i = 0; i < count; i++)
                {
                    resourcesReadback[i].Dispose();
                }
            }
        }

        private static void TestZeroInitialized([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test zero initialized");

            const ulong bufSize = 128ul * 1024;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(out resourceDesc, bufSize);

            // # Create upload buffer and fill it with data.

            D3D12MA_ALLOCATION_DESC allocDescUpload = default;
            allocDescUpload.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            ResourceWithAllocation bufUpload;
            Unsafe.SkipInit(out bufUpload);
            ResourceWithAllocation._ctor(ref bufUpload);

            try
            {
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDescUpload,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    bufUpload.allocation.GetAddressOf(),
                    __uuidof<ID3D12Resource>(), (void**)&bufUpload.resource));

                {
                    void* mappedPtr = null;
                    CHECK_HR(bufUpload.resource.Get()->Map(0, EMPTY_RANGE, &mappedPtr));
                    FillData(mappedPtr, bufSize, 5236245);
                    bufUpload.resource.Get()->Unmap(0, null);
                }

                // # Create readback buffer

                D3D12MA_ALLOCATION_DESC allocDescReadback = default;
                allocDescReadback.HeapType = D3D12_HEAP_TYPE_READBACK;

                ResourceWithAllocation bufReadback;
                Unsafe.SkipInit(out bufReadback);
                ResourceWithAllocation._ctor(ref bufReadback);

                try
                {
                    CHECK_HR(ctx.allocator->CreateResource(
                        &allocDescReadback,
                        &resourceDesc,
                        D3D12_RESOURCE_STATE_COPY_DEST,
                        null,
                        bufReadback.allocation.GetAddressOf(),
                        __uuidof<ID3D12Resource>(), (void**)&bufReadback.resource));

                    Action<IntPtr, IntPtr> CheckBufferData = (IntPtr pBuf, IntPtr pBufReadback) => {
                        ref readonly ResourceWithAllocation buf = ref Unsafe.AsRef<ResourceWithAllocation>((void*)pBuf);
                        ref readonly ResourceWithAllocation bufReadback = ref Unsafe.AsRef<ResourceWithAllocation>((void*)pBufReadback);

                        bool shouldBeZero = buf.allocation.Get()->WasZeroInitialized() != FALSE;

                        {
                            ID3D12GraphicsCommandList* cmdList = BeginCommandList();
                            cmdList->CopyBufferRegion(bufReadback.resource, 0, buf.resource, 0, bufSize);
                            EndCommandList(cmdList);
                        }

                        bool isZero = false;
                        {
                            D3D12_RANGE readRange = new D3D12_RANGE { Begin = 0, End = (nuint)bufSize }; // I could pass pReadRange = NULL but it generates D3D Debug layer warning: EXECUTION WARNING #930: MAP_INVALID_NULLRANGE
                            void* mappedPtr = null;

                            CHECK_HR(bufReadback.resource.Get()->Map(0, &readRange, &mappedPtr));

                            isZero = ValidateDataZero(mappedPtr, bufSize);
                            bufReadback.resource.Get()->Unmap(0, EMPTY_RANGE);
                        }

                        Console.WriteLine($"Should be zero: {(shouldBeZero ? 1 : 0)}, is zero: {(isZero ? 1 : 0)}");

                        if (shouldBeZero)
                        {
                            CHECK_BOOL(isZero);
                        }
                    };

                    // # Test 1: Committed resource. Should always be zero initialized.

                    {
                        D3D12MA_ALLOCATION_DESC allocDescDefault = default;
                        allocDescDefault.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                        allocDescDefault.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;

                        ResourceWithAllocation bufDefault;
                        Unsafe.SkipInit(out bufDefault);
                        ResourceWithAllocation._ctor(ref bufDefault);

                        try
                        {
                            CHECK_HR(ctx.allocator->CreateResource(
                                &allocDescDefault,
                                &resourceDesc,
                                D3D12_RESOURCE_STATE_COPY_SOURCE,
                                null,
                                bufDefault.allocation.GetAddressOf(),
                                __uuidof<ID3D12Resource>(), (void**)&bufDefault.resource
                            ));

                            Console.WriteLine("  Committed: ");
                            CheckBufferData((IntPtr)(&bufDefault), (IntPtr)(&bufReadback));
                            CHECK_BOOL(bufDefault.allocation.Get()->WasZeroInitialized() != 0);
                        }
                        finally
                        {
                            bufDefault.Dispose();
                        }
                    }

                    // # Test 2: (Probably) placed resource.

                    {
                        ResourceWithAllocation bufDefault = default;

                        try

                        {
                            for (uint i = 0; i < 2; ++i)
                            {
                                // 1. Create buffer

                                D3D12MA_ALLOCATION_DESC allocDescDefault = default;
                                allocDescDefault.HeapType = D3D12_HEAP_TYPE_DEFAULT;

                                CHECK_HR(ctx.allocator->CreateResource(
                                    &allocDescDefault,
                                    &resourceDesc,
                                    D3D12_RESOURCE_STATE_COPY_SOURCE,
                                    null,
                                    bufDefault.allocation.GetAddressOf(),
                                    __uuidof<ID3D12Resource>(), (void**)&bufDefault.resource
                                ));

                                // 2. Check it

                                Console.WriteLine($"  Normal #{i}: ");
                                CheckBufferData((IntPtr)(&bufDefault), (IntPtr)(&bufReadback));

                                // 3. Upload some data to it

                                {
                                    ID3D12GraphicsCommandList* cmdList = BeginCommandList();

                                    D3D12_RESOURCE_BARRIER barrier = default;
                                    barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
                                    barrier.Transition.pResource = bufDefault.resource;
                                    barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_SOURCE;
                                    barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_DEST;
                                    barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
                                    cmdList->ResourceBarrier(1, &barrier);

                                    cmdList->CopyBufferRegion(bufDefault.resource, 0, bufUpload.resource, 0, bufSize);

                                    EndCommandList(cmdList);
                                }

                                // 4. Delete it

                                bufDefault.Reset();
                            }
                        }
                        finally
                        {
                            bufDefault.Dispose();
                        }
                    }
                }
                finally
                {
                    bufReadback.Dispose();
                }
            }
            finally
            {
                bufUpload.Dispose();
            }
        }

        private static void TestMultithreading([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test multithreading");

            const uint threadCount = 1;
            const uint bufSizeMin = 1024u;
            const uint bufSizeMax = 1024u * 1024;

            D3D12MA_ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            // Launch threads.
            Thread[] threads = new Thread[threadCount];

            for (uint threadIndex = 0; threadIndex < threadCount; ++threadIndex)
            {
                ParameterizedThreadStart threadFunc = (object? obj) => {
                    (TestContext ctx, D3D12MA_ALLOCATION_DESC allocDesc, uint threadIndex) = (ValueTuple<TestContext, D3D12MA_ALLOCATION_DESC, uint>)(obj!);
                    RandomNumberGenerator rand = new RandomNumberGenerator(threadIndex);

                    List<ResourceWithAllocation> resources = new List<ResourceWithAllocation>(256);

                    try
                    {
                        // Create starting number of buffers.
                        const uint bufToCreateCount = 32;

                        for (uint bufIndex = 0; bufIndex < bufToCreateCount; ++bufIndex)
                        {
                            ResourceWithAllocation res;
                            Unsafe.SkipInit(out res);
                            ResourceWithAllocation._ctor(ref res);

                            res.dataSeed = (threadIndex << 16) | bufIndex;
                            res.size = AlignUp((rand.Generate() % (bufSizeMax - bufSizeMin)) + bufSizeMin, 16);

                            D3D12_RESOURCE_DESC resourceDesc;
                            FillResourceDescForBuffer(out resourceDesc, res.size);

                            CHECK_HR(ctx.allocator->CreateResource(
                                &allocDesc,
                                &resourceDesc,
                                D3D12_RESOURCE_STATE_GENERIC_READ,
                                null,
                                res.allocation.GetAddressOf(),
                                __uuidof<ID3D12Resource>(), (void**)&res.resource
                            ));

                            void* mappedPtr = null;
                            CHECK_HR(res.resource.Get()->Map(0, EMPTY_RANGE, &mappedPtr));

                            FillData(mappedPtr, res.size, res.dataSeed);

                            // Unmap some of them, leave others mapped.
                            if (rand.GenerateBool())
                            {
                                res.resource.Get()->Unmap(0, null);
                            }

                            resources.Add(res);
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
                                uint indexToRemove = rand.Generate() % (uint)resources.Count;
                                resources[(int)indexToRemove].Dispose();
                                resources.RemoveAt((int)indexToRemove);
                            }
                            else // Create new buffer.
                            {
                                ResourceWithAllocation res;
                                Unsafe.SkipInit(out res);
                                ResourceWithAllocation._ctor(ref res);

                                res.dataSeed = (threadIndex << 16) | operationIndex;
                                res.size = AlignUp((rand.Generate() % (bufSizeMax - bufSizeMin)) + bufSizeMin, 16);

                                D3D12_RESOURCE_DESC resourceDesc;
                                FillResourceDescForBuffer(out resourceDesc, res.size);

                                CHECK_HR(ctx.allocator->CreateResource(
                                    &allocDesc,
                                    &resourceDesc,
                                    D3D12_RESOURCE_STATE_GENERIC_READ,
                                    null,
                                    res.allocation.GetAddressOf(),
                                    __uuidof<ID3D12Resource>(), (void**)&res.resource
                                ));

                                void* mappedPtr = null;
                                CHECK_HR(res.resource.Get()->Map(0, null, &mappedPtr));

                                FillData(mappedPtr, res.size, res.dataSeed);

                                // Unmap some of them, leave others mapped.
                                if (rand.GenerateBool())
                                {
                                    res.resource.Get()->Unmap(0, null);
                                }

                                resources.Add(res);
                            }
                        }

                        Sleep(20);

                        // Validate data in all remaining buffers while deleting them.
                        for (nuint resIndex = (nuint)resources.Count; unchecked(resIndex-- != 0);)
                        {
                            void* mappedPtr = null;
                            CHECK_HR(resources[(int)resIndex].resource.Get()->Map(0, null, &mappedPtr));

                            _ = ValidateData(mappedPtr, resources[(int)resIndex].size, resources[(int)resIndex].dataSeed);

                            // Unmap some of them, leave others mapped.
                            if ((resIndex % 3) == 1)
                            {
                                resources[(int)resIndex].resource.Get()->Unmap(0, EMPTY_RANGE);
                            }

                            resources[(int)resIndex].Dispose();
                            resources.RemoveAt((int)resIndex);
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < resources.Count; i++)
                        {
                            resources[i].Dispose();
                        }
                    }
                };

                threads[threadIndex] = new Thread(threadFunc);
                threads[threadIndex].Start((ctx, allocDesc, threadIndex));
            }

            // Wait for threads to finish.
            for (uint threadIndex = threadCount; unchecked(threadIndex-- != 0);)
            {
                threads[threadIndex].Join();
            }
        }

        private static bool IsProtectedResourceSessionSupported([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            D3D12_FEATURE_DATA_PROTECTED_RESOURCE_SESSION_SUPPORT support = default;
            CHECK_HR(ctx.device->CheckFeatureSupport(D3D12_FEATURE_PROTECTED_RESOURCE_SESSION_SUPPORT, &support, (uint)sizeof(D3D12_FEATURE_DATA_PROTECTED_RESOURCE_SESSION_SUPPORT)));
            return support.Support > D3D12_PROTECTED_RESOURCE_SESSION_SUPPORT_FLAG_NONE;
        }

        private static void TestDevice4([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test ID3D12Device4");

            if (!IsProtectedResourceSessionSupported(in ctx))
            {
                Assert.Inconclusive("D3D12_FEATURE_PROTECTED_RESOURCE_SESSION_SUPPORT returned no support for protected resource session.");
            }

            using ComPtr<ID3D12Device4> dev4 = default;

            HRESULT hr = ctx.device->QueryInterface(__uuidof<ID3D12Device>(), (void**)&dev4);
            if (FAILED(hr))
            {
                Assert.Inconclusive("QueryInterface for ID3D12Device4 FAILED.");
            }

            D3D12_PROTECTED_RESOURCE_SESSION_DESC sessionDesc = default;
            using ComPtr<ID3D12ProtectedResourceSession> session = default;

            // This fails on the SOFTWARE adapter.
            hr = dev4.Get()->CreateProtectedResourceSession(&sessionDesc, __uuidof<ID3D12ProtectedResourceSession>(), (void**)&session);
            if (FAILED(hr))
            {
                Assert.Inconclusive("ID3D12Device4::CreateProtectedResourceSession FAILED.");
            }

            // Create a buffer

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(out resourceDesc, 1024);

            D3D12MA_ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

            using ComPtr<D3D12MA_Allocation> bufAlloc = default;
            using ComPtr<ID3D12Resource> bufRes = default;

            CHECK_HR(ctx.allocator->CreateResource1(
                &allocDesc,
                &resourceDesc,
                D3D12_RESOURCE_STATE_COMMON,
                null,
                session,
                bufAlloc.GetAddressOf(),
                __uuidof<ID3D12Resource>(), (void**)&bufRes
            ));

            // Create a heap
            // Temporarily commented out as it caues BSOD on RTX2080Ti driver 461.40.
#if false
            D3D12_RESOURCE_ALLOCATION_INFO heapAllocInfo = new D3D12_RESOURCE_ALLOCATION_INFO {
                SizeInBytes = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT * 100,
                Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
            };

            using ComPtr<D3D12MA_Allocation> heapAlloc = default;

            CHECK_HR(ctx.allocator->AllocateMemory1(&allocDesc, &heapAllocInfo, session, heapAlloc.GetAddressOf()));
#endif
        }

        private static void TestDevice8([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("Test ID3D12Device8");

            using ComPtr<ID3D12Device8> dev8 = default;

            if (FAILED(ctx.device->QueryInterface(__uuidof<ID3D12Device8>(), (void**)&dev8)))
            {
                Assert.Inconclusive();
            }

            D3D12_RESOURCE_DESC1 resourceDesc;
            FillResourceDescForBuffer(out resourceDesc, 1024 * 1024);

            // Create a committed buffer

            D3D12MA_ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
            allocDesc.Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED;

            using ComPtr<D3D12MA_Allocation> alloc0 = default;
            using ComPtr<ID3D12Resource> res0 = default;

            CHECK_HR(ctx.allocator->CreateResource2(
                &allocDesc,
                &resourceDesc,
                D3D12_RESOURCE_STATE_COMMON,
                null,
                null,
                alloc0.GetAddressOf(),
                __uuidof<ID3D12Resource>(), (void**)&res0
            ));

            CHECK_BOOL(alloc0.Get()->GetHeap() == null);

            // Create a placed buffer

            allocDesc.Flags &= ~D3D12MA_ALLOCATION_FLAG_COMMITTED;

            using ComPtr<D3D12MA_Allocation> alloc1 = default;
            using ComPtr<ID3D12Resource> res1 = default;

            CHECK_HR(ctx.allocator->CreateResource2(
                &allocDesc,
                &resourceDesc,
                D3D12_RESOURCE_STATE_COMMON,
                null,
                null,
                alloc1.GetAddressOf(),
                __uuidof<ID3D12Resource>(), (void**)&res1
            ));

            CHECK_BOOL(alloc1.Get()->GetHeap() != null);
        }

        public static void TestGroupVirtual([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            TestVirtualBlocks(in ctx);
        }

        public static void TestGroupBasics([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            TestFrameIndexAndJson(in ctx);
            TestCommittedResourcesAndJson(in ctx);
            TestCustomHeapFlags(in ctx);
            TestPlacedResources(in ctx);
            TestOtherComInterface(in ctx);
            TestCustomPools(in ctx);
            TestCustomPool_MinAllocationAlignment(in ctx);
            TestCustomHeaps(in ctx);
            TestStandardCustomCommittedPlaced(in ctx);
            TestAliasingMemory(in ctx);
            TestMapping(in ctx);
            TestStats(in ctx);
            TestTransfer(in ctx);
            TestZeroInitialized(in ctx);
            TestMultithreading(in ctx);
            TestDevice4(in ctx);
            TestDevice8(in ctx);
        }

        public static void Test([NativeTypeName("const TestContext&")] in TestContext ctx)
        {
            Console.WriteLine("TESTS BEGIN");

            TestGroupVirtual(ctx);
            TestGroupBasics(ctx);

            Console.WriteLine("TESTS END");
        }
    }
}
