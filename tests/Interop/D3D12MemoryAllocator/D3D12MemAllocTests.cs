// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Threading;
using NUnit.Framework;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.D3D12_HEAP_TYPE;
using static TerraFX.Interop.ALLOCATION_FLAGS;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.ALLOCATOR_FLAGS;
using static TerraFX.Interop.D3D12_RESOURCE_BARRIER_TYPE;

using AllocationUniquePtr = TerraFX.Interop.UnitTests.unique_ptr<TerraFX.Interop.Allocation>;
using PoolUniquePtr = TerraFX.Interop.UnitTests.unique_ptr<TerraFX.Interop.Pool>;
using VirtualBlockUniquePtr = TerraFX.Interop.UnitTests.unique_ptr<TerraFX.Interop.VirtualBlock>;

namespace TerraFX.Interop.UnitTests
{
    internal unsafe static partial class D3D12MemAllocTests
    {
        [Test]
        public static void TestVirtualBlocks()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            const ulong blockSize = 16 * MEGABYTE;
            const ulong alignment = 256;

            // # Create block 16 MB

            using VirtualBlockUniquePtr block = default;
            VirtualBlock* blockPtr = null;
            VIRTUAL_BLOCK_DESC blockDesc = default;
            blockDesc.pAllocationCallbacks = ctx.allocationCallbacks;
            blockDesc.Size = blockSize;
            CHECK_HR(CreateVirtualBlock(&blockDesc, &blockPtr));
            CHECK_BOOL(blockPtr != null);
            block.reset(blockPtr);

            // # Allocate 8 MB

            VIRTUAL_ALLOCATION_DESC allocDesc = default;
            allocDesc.Alignment = alignment;
            allocDesc.pUserData = (void*)(uint)1;
            allocDesc.Size = 8 * MEGABYTE;
            ulong alloc0Offset;
            CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc0Offset));
            CHECK_BOOL(alloc0Offset < blockSize);

            // # Validate the allocation

            VIRTUAL_ALLOCATION_INFO allocInfo = default;
            block.Get()->GetAllocationInfo(alloc0Offset, &allocInfo);
            CHECK_BOOL(allocInfo.size == allocDesc.Size);
            CHECK_BOOL(allocInfo.pUserData == allocDesc.pUserData);

            // # Check SetUserData

            block.Get()->SetAllocationUserData(alloc0Offset, (void*)(uint)2);
            block.Get()->GetAllocationInfo(alloc0Offset, &allocInfo);
            CHECK_BOOL(allocInfo.pUserData == (void*)(uint)2);

            // # Allocate 4 MB

            allocDesc.Size = 4 * MEGABYTE;
            allocDesc.Alignment = alignment;
            ulong alloc1Offset;
            CHECK_HR(block.Get()->Allocate(&allocDesc, &alloc1Offset));
            CHECK_BOOL(alloc1Offset < blockSize);
            CHECK_BOOL(alloc1Offset + 4 * MEGABYTE <= alloc0Offset || alloc0Offset + 8 * MEGABYTE <= alloc1Offset); // Check if they don't overlap.

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
            CHECK_BOOL(alloc2Offset + 4 * MEGABYTE <= alloc0Offset || alloc0Offset + 8 * MEGABYTE <= alloc2Offset); // Check if they don't overlap.

            // # Calculate statistics

            StatInfo statInfo = default;
            block.Get()->CalculateStats(&statInfo);
            CHECK_BOOL(statInfo.AllocationCount == 2);
            CHECK_BOOL(statInfo.BlockCount == 1);
            CHECK_BOOL(statInfo.UsedBytes == blockSize);
            CHECK_BOOL(statInfo.UnusedBytes + statInfo.UsedBytes == blockSize);

            // # Generate JSON dump

            ushort* json = null;
            block.Get()->BuildStatsString(&json);
            {
                string str = new((char*)json);
                CHECK_BOOL(str.Contains("\"UserData\": 1"));
                CHECK_BOOL(str.Contains("\"UserData\": 2"));
            }
            block.Get()->FreeStatsString(json);

            // # Free alloc0, leave alloc2 unfreed.

            block.Get()->FreeAllocation(alloc0Offset);

            // # Test alignment

            {
                nuint allocCount = 10;
                ulong* allocOffset = stackalloc ulong[(int)allocCount];
                for (nuint i = 0; i < allocCount; ++i)
                {
                    bool alignment0 = i == allocCount - 1;
                    allocDesc.Size = i * 3 + 15;
                    allocDesc.Alignment = alignment0 ? 0 : 8;
                    CHECK_HR(block.Get()->Allocate(&allocDesc, &allocOffset[i]));
                    if (!alignment0)
                    {
                        CHECK_BOOL(allocOffset[i] % allocDesc.Alignment == 0);
                    }
                }

                for (nuint i = allocCount; unchecked(i-- > 0);)
                {
                    block.Get()->FreeAllocation(allocOffset[i]);
                }
            }

            // # Final cleanup

            block.Get()->FreeAllocation(alloc2Offset);

            //block->Clear();
        }

        [Test]
        public static void TestFrameIndexAndJson()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            const ulong bufSize = 32UL * 1024;

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;
            allocDesc.Flags = ALLOCATION_FLAG_COMMITTED;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(&resourceDesc, bufSize);

            const uint BEGIN_INDEX = 10;
            const uint END_INDEX = 20;
            for (uint frameIndex = BEGIN_INDEX; frameIndex < END_INDEX; ++frameIndex)
            {
                ctx.allocator->SetCurrentFrameIndex(frameIndex);
                Allocation* alloc = null;
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    null));

                ushort* statsString;
                ctx.allocator->BuildStatsString(&statsString, TRUE);
                string statsStr = new((char*)statsString);
                for (uint testIndex = BEGIN_INDEX; testIndex < END_INDEX; ++testIndex)
                {
                    string buffer = $"\"CreationFrameIndex\": {testIndex}";
                    if (testIndex == frameIndex)
                    {
                        CHECK_BOOL(statsStr.Contains(buffer));
                    }
                    else
                    {
                        CHECK_BOOL(!statsStr.Contains(buffer));
                    }
                }
                ctx.allocator->FreeStatsString(statsString);
                alloc->Release();
            }
        }

        [Test]
        public static void TestCommittedResourcesAndJson()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            const uint count = 4;
            const ulong bufSize = 32 * 1024;
            string?[] names = new[]{
                "Resource\nFoo\r\nBar",
                "Resource \"'&<>?#@!&-=_+[]{};:,./\\",
                null,
                "",
            };

            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
            allocDesc.Flags = ALLOCATION_FLAG_COMMITTED;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(&resourceDesc, bufSize);

            for (uint i = 0; i < count; ++i)
            {
                bool receiveExplicitResource = i < 2;

                Allocation* alloc = null;
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_COPY_DEST,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    receiveExplicitResource ? (void**)&resources[i].resource : null));
                resources[i].allocation.reset(alloc);

                if (receiveExplicitResource)
                {
                    ID3D12Resource* res = resources[i].resource.Get();
                    CHECK_BOOL(res != null && res == resources[i].allocation.Get()->GetResource());
                    uint refCountAfterAdd = res->AddRef();
                    CHECK_BOOL(refCountAfterAdd == 3);
                    res->Release();
                }

                // Make sure it has implicit heap.
                CHECK_BOOL(resources[i].allocation.Get()->GetHeap() == null && resources[i].allocation.Get()->GetOffset() == 0);

                fixed (char* p = names[i])
                    resources[i].allocation.Get()->SetName((ushort*)p);
            }

            // Check names.
            for (uint i = 0; i < count; ++i)
            {
                ushort* allocName = resources[i].allocation.Get()->GetName();
                if (allocName != null)
                {
                    CHECK_BOOL(new string((char*)allocName) == names[i]);
                }
                else
                {
                    CHECK_BOOL(names[i] == null);
                }
            }

            ushort* jsonString;
            ctx.allocator->BuildStatsString(&jsonString, TRUE);
            string jsonStr = new((char*)jsonString);
            CHECK_BOOL(jsonStr.Contains("\"Resource\\nFoo\\r\\nBar\""));
            CHECK_BOOL(jsonStr.Contains("\"Resource \\\"'&<>?#@!&-=_+[]{};:,.\\/\\\\\""));
            CHECK_BOOL(jsonStr.Contains("\"\""));
            ctx.allocator->FreeStatsString(jsonString);
        }

        [Test]
        public static void TestCustomHeapFlags()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            // 1. Just memory heap with custom flags
            {
                ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES |
                    D3D12_HEAP_FLAG_SHARED; // Extra flag.

                D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = default;
                resAllocInfo.SizeInBytes = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
                resAllocInfo.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

                Allocation* alloc = null;
                CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &resAllocInfo, &alloc));
                ResourceWithAllocation res = default;
                res.allocation.reset(alloc);

                // Must be created as separate allocation.
                CHECK_BOOL(res.allocation.Get()->GetOffset() == 0);
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

                ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_SHARED | D3D12_HEAP_FLAG_SHARED_CROSS_ADAPTER; // Extra flags.

                ResourceWithAllocation res = default;
                Allocation* alloc = null;
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_COMMON,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&res.resource));
                res.allocation.reset(alloc);

                // Must be created as committed.
                CHECK_BOOL(res.allocation.Get()->GetHeap() == null);
            }
        }

        [Test]
        public static void TestPlacedResources()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            bool alwaysCommitted = (ctx.allocatorFlags & ALLOCATOR_FLAG_ALWAYS_COMMITTED) != 0;

            const uint count = 4;
            const ulong bufSize = 32ul * 1024;
            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(&resourceDesc, bufSize);

            Allocation* alloc = null;
            for (uint i = 0; i < count; ++i)
            {
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&resources[i].resource));
                resources[i].allocation.reset(alloc);

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
                    ResourceWithAllocation* resI = &resources[i];
                    ResourceWithAllocation* resJ = &resources[j];
                    if (resI->allocation.Get()->GetHeap() != null &&
                        resI->allocation.Get()->GetHeap() == resJ->allocation.Get()->GetHeap())
                    {
                        sameHeapFound = true;
                        CHECK_BOOL(resI->allocation.Get()->GetOffset() + resI->allocation.Get()->GetSize() <= resJ->allocation.Get()->GetOffset() ||
                            resJ->allocation.Get()->GetOffset() + resJ->allocation.Get()->GetSize() <= resI->allocation.Get()->GetOffset());
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
            ResourceWithAllocation textureRes = default;
            CHECK_HR(ctx.allocator->CreateResource(
                &allocDesc,
                &resourceDesc,
                D3D12_RESOURCE_STATE_COPY_DEST,
                null,
                &alloc,
                __uuidof<ID3D12Resource>(),
                (void**)&textureRes.resource));
            textureRes.allocation.reset(alloc);

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
            ResourceWithAllocation renderTargetRes = default;
            CHECK_HR(ctx.allocator->CreateResource(
                &allocDesc,
                &resourceDesc,
                D3D12_RESOURCE_STATE_RENDER_TARGET,
                null,
                &alloc,
                __uuidof<ID3D12Resource>(),
                (void**)&renderTargetRes.resource));
            renderTargetRes.allocation.reset(alloc);
        }

        [Test]
        public static void TestOtherComInterface()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            D3D12_RESOURCE_DESC resDesc;
            FillResourceDescForBuffer(&resDesc, 0x10000);

            for (uint i = 0; i < 2; ++i)
            {
                ALLOCATION_DESC allocDesc = default;
                allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                if (i == 1)
                {
                    allocDesc.Flags = ALLOCATION_FLAG_COMMITTED;
                }

                Allocation* alloc = null;
                using ComPtr<ID3D12Pageable> pageable = default;
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resDesc,
                    D3D12_RESOURCE_STATE_COMMON,
                    null, // pOptimizedClearValue
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)pageable.GetAddressOf()));

                // Do something with the interface to make sure it's valid.
                using ComPtr<ID3D12Device> device = default;
                CHECK_HR(pageable.Get()->GetDevice(__uuidof<ID3D12Device>(), (void**)&device));
                CHECK_BOOL(device == ctx.device);

                alloc->Release();
            }
        }

        [Test]
        public static void TestCustomPools()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            // # Fetch global stats 1

            Stats globalStatsBeg = default;
            ctx.allocator->CalculateStats(&globalStatsBeg);

            // # Create pool, 1..2 blocks of 11 MB

            POOL_DESC poolDesc = default;
            poolDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
            poolDesc.HeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
            poolDesc.BlockSize = 11 * MEGABYTE;
            poolDesc.MinBlockCount = 1;
            poolDesc.MaxBlockCount = 2;

            Pool* poolPtr;
            CHECK_HR(ctx.allocator->CreatePool(&poolDesc, &poolPtr));
            using PoolUniquePtr pool = poolPtr;

            Allocation* allocPtr;

            // # Validate stats for empty pool

            StatInfo poolStats = default;
            pool.Get()->CalculateStats(&poolStats);
            CHECK_BOOL(poolStats.BlockCount == 1);
            CHECK_BOOL(poolStats.AllocationCount == 0);
            CHECK_BOOL(poolStats.UsedBytes == 0);
            CHECK_BOOL(poolStats.UnusedBytes == poolStats.BlockCount * poolDesc.BlockSize);

            // # SetName and GetName
            const string NAME = "Custom pool name 1";
            fixed(char* p = NAME) pool.Get()->SetName((ushort*)p);
            CHECK_BOOL(new string((char*)pool.Get()->GetName()) == NAME);

            // # SetMinBytes

            CHECK_HR(pool.Get()->SetMinBytes(15 * MEGABYTE));
            pool.Get()->CalculateStats(&poolStats);
            CHECK_BOOL(poolStats.BlockCount == 2);
            CHECK_BOOL(poolStats.AllocationCount == 0);
            CHECK_BOOL(poolStats.UsedBytes == 0);
            CHECK_BOOL(poolStats.UnusedBytes == poolStats.BlockCount * poolDesc.BlockSize);

            CHECK_HR(pool.Get()->SetMinBytes(0));
            pool.Get()->CalculateStats(&poolStats);
            CHECK_BOOL(poolStats.BlockCount == 1);
            CHECK_BOOL(poolStats.AllocationCount == 0);
            CHECK_BOOL(poolStats.UsedBytes == 0);
            CHECK_BOOL(poolStats.UnusedBytes == poolStats.BlockCount * poolDesc.BlockSize);

            // # Create buffers 2x 5 MB

            ALLOCATION_DESC allocDesc = default;
            allocDesc.CustomPool = pool.Get();
            allocDesc.ExtraHeapFlags = unchecked((D3D12_HEAP_FLAGS)0xCDCDCDCDu); // Should be ignored.
            allocDesc.HeapType = unchecked((D3D12_HEAP_TYPE)0xCDCDCDCDu); // Should be ignored.

            const ulong BUFFER_SIZE = 5 * MEGABYTE;
            D3D12_RESOURCE_DESC resDesc;
            FillResourceDescForBuffer(&resDesc, BUFFER_SIZE);

            AllocationUniquePtr* allocs = stackalloc AllocationUniquePtr[4];
            for (uint i = 0; i < 2; ++i)
            {
                CHECK_HR(ctx.allocator->CreateResource(&allocDesc, &resDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null, // pOptimizedClearValue
                    &allocPtr,
                    __uuidof<ID3D12Resource>(), null)); // riidResource, ppvResource
                allocs[i].reset(allocPtr);
            }

            // # Validate pool stats now

            pool.Get()->CalculateStats(&poolStats);
            CHECK_BOOL(poolStats.BlockCount == 1);
            CHECK_BOOL(poolStats.AllocationCount == 2);
            CHECK_BOOL(poolStats.UsedBytes == 2 * BUFFER_SIZE);
            CHECK_BOOL(poolStats.UnusedBytes == poolDesc.BlockSize - poolStats.UsedBytes);

            // # Check that global stats are updated as well

            Stats globalStatsCurr = default;
            ctx.allocator->CalculateStats(&globalStatsCurr);

            CHECK_BOOL(globalStatsCurr.Total.AllocationCount == globalStatsBeg.Total.AllocationCount + poolStats.AllocationCount);
            CHECK_BOOL(globalStatsCurr.Total.BlockCount == globalStatsBeg.Total.BlockCount + poolStats.BlockCount);
            CHECK_BOOL(globalStatsCurr.Total.UsedBytes == globalStatsBeg.Total.UsedBytes + poolStats.UsedBytes);

            // # NEVER_ALLOCATE and COMMITTED should fail

            for (uint i = 0; i < 2; ++i)
            {
                allocDesc.Flags = i == 0 ?
                    ALLOCATION_FLAG_NEVER_ALLOCATE :
                    ALLOCATION_FLAG_COMMITTED;
                HRESULT hr = ctx.allocator->CreateResource(&allocDesc, &resDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null, // pOptimizedClearValue
                    &allocPtr,
                    __uuidof<ID3D12Resource>(), null); // riidResource, ppvResource
                CHECK_BOOL(FAILED(hr));
            }

            // # 3 more buffers. 3rd should fail.

            allocDesc.Flags = ALLOCATION_FLAG_NONE;
            for (uint i = 2; i < 5; ++i)
            {
                HRESULT hr = ctx.allocator->CreateResource(&allocDesc, &resDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null, // pOptimizedClearValue
                    &allocPtr,
                    __uuidof<ID3D12Resource>(), null); // riidResource, ppvResource
                if (i < 4)
                {
                    CHECK_HR(hr);
                    allocs[i].reset(allocPtr);
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
            CHECK_BOOL(poolStats.UnusedBytes == poolStats.BlockCount * poolDesc.BlockSize - poolStats.UsedBytes);

            // # Make room, AllocateMemory, CreateAliasingResource

            allocs[3].reset();
            allocs[0].reset();

            D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo = default;
            resAllocInfo.SizeInBytes = 5 * MEGABYTE;
            resAllocInfo.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

            CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &resAllocInfo, &allocPtr));
            allocs[0].reset(allocPtr);

            resDesc.Width = 1 * MEGABYTE;
            using ComPtr<ID3D12Resource> res = default;
            CHECK_HR(ctx.allocator->CreateAliasingResource(allocs[0].Get(),
                0, // AllocationLocalOffset
                &resDesc,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                null, // pOptimizedClearValue
                __uuidof<ID3D12Resource>(),
                (void**)&res));
        }

        [Test]
        public static void TestDefaultPoolMinBytes()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            Stats stats;
            ctx.allocator->CalculateStats(&stats);
            ulong gpuAllocatedBefore = stats.HeapType[0].UsedBytes + stats.HeapType[0].UnusedBytes;

            ulong gpuAllocatedMin = gpuAllocatedBefore * 105 / 100;
            CHECK_HR(ctx.allocator->SetDefaultHeapMinBytes(D3D12_HEAP_TYPE_DEFAULT, D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS, CeilDiv(gpuAllocatedMin, 3ul)));
            CHECK_HR(ctx.allocator->SetDefaultHeapMinBytes(D3D12_HEAP_TYPE_DEFAULT, D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES, CeilDiv(gpuAllocatedMin, 3ul)));
            CHECK_HR(ctx.allocator->SetDefaultHeapMinBytes(D3D12_HEAP_TYPE_DEFAULT, D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES, CeilDiv(gpuAllocatedMin, 3ul)));

            ctx.allocator->CalculateStats(&stats);
            ulong gpuAllocatedAfter = stats.HeapType[0].UsedBytes + stats.HeapType[0].UnusedBytes;
            CHECK_BOOL(gpuAllocatedAfter >= gpuAllocatedMin);

            CHECK_HR(ctx.allocator->SetDefaultHeapMinBytes(D3D12_HEAP_TYPE_DEFAULT, D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS, 0));
            CHECK_HR(ctx.allocator->SetDefaultHeapMinBytes(D3D12_HEAP_TYPE_DEFAULT, D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES, 0));
            CHECK_HR(ctx.allocator->SetDefaultHeapMinBytes(D3D12_HEAP_TYPE_DEFAULT, D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES, 0));
        }

        [Test]
        public static void TestAliasingMemory()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

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

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
            allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES;

            Allocation* alloc = null;
            CHECK_HR(ctx.allocator->AllocateMemory(&allocDesc, &finalAllocInfo, &alloc));
            CHECK_BOOL(alloc != null && alloc->GetHeap() != null);

            ID3D12Resource* res1 = null;
            CHECK_HR(ctx.allocator->CreateAliasingResource(
                alloc,
                0, // AllocationLocalOffset
                &resDesc1,
                D3D12_RESOURCE_STATE_COMMON,
                null, // pOptimizedClearValue
                __uuidof<ID3D12Resource>(),
                (void**)&res1));
            CHECK_BOOL(res1 != null);

            ID3D12Resource* res2 = null;
            CHECK_HR(ctx.allocator->CreateAliasingResource(
                alloc,
                0, // AllocationLocalOffset
                &resDesc2,
                D3D12_RESOURCE_STATE_COMMON,
                null, // pOptimizedClearValue
                __uuidof<ID3D12Resource>(),
                (void**)&res2));
            CHECK_BOOL(res2 != null);

            // You can use res1 and res2, but not at the same time!

            res2->Release();
            res1->Release();
            alloc->Release();
        }

        [Test]
        public static void TestMapping()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            const uint count = 10;
            const ulong bufSize = 32ul * 1024;
            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(&resourceDesc, bufSize);

            for (uint i = 0; i < count; ++i)
            {
                Allocation* alloc = null;
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&resources[i].resource));
                resources[i].allocation.reset(alloc);

                void* mappedPtr = null;
                D3D12_RANGE EMPTY_RANGE = default;
                CHECK_HR(resources[i].resource.Get()->Map(0, &EMPTY_RANGE, &mappedPtr));

                FillData(mappedPtr, bufSize, i);

                // Unmap every other buffer. Leave others mapped.
                if ((i % 2) != 0)
                {
                    resources[i].resource.Get()->Unmap(0, null);
                }
            }
        }

        static bool StatInfoEqual(ref StatInfo lhs, ref StatInfo rhs)
        {
            return lhs.BlockCount == rhs.BlockCount &&
                lhs.AllocationCount == rhs.AllocationCount &&
                lhs.UnusedRangeCount == rhs.UnusedRangeCount &&
                lhs.UsedBytes == rhs.UsedBytes &&
                lhs.UnusedBytes == rhs.UnusedBytes &&
                lhs.AllocationSizeMin == rhs.AllocationSizeMin &&
                lhs.AllocationSizeMax == rhs.AllocationSizeMax &&
                lhs.AllocationSizeAvg == rhs.AllocationSizeAvg &&
                lhs.UnusedRangeSizeMin == rhs.UnusedRangeSizeMin &&
                lhs.UnusedRangeSizeMax == rhs.UnusedRangeSizeMax &&
                lhs.UnusedRangeSizeAvg == rhs.UnusedRangeSizeAvg;
        }

        static void CheckStatInfo(ref StatInfo statInfo)
        {
            if (statInfo.AllocationCount > 0)
            {
                CHECK_BOOL(statInfo.AllocationSizeAvg >= statInfo.AllocationSizeMin &&
                    statInfo.AllocationSizeAvg <= statInfo.AllocationSizeMax);
            }
            if (statInfo.UsedBytes > 0)
            {
                CHECK_BOOL(statInfo.AllocationCount > 0);
            }
            if (statInfo.UnusedRangeCount > 0)
            {
                CHECK_BOOL(statInfo.UnusedRangeSizeAvg >= statInfo.UnusedRangeSizeMin &&
                    statInfo.UnusedRangeSizeAvg <= statInfo.UnusedRangeSizeMax);
                CHECK_BOOL(statInfo.UnusedRangeSizeMin > 0);
                CHECK_BOOL(statInfo.UnusedRangeSizeMax > 0);
            }
        }

        [Test]
        public static void TestStats()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            Stats begStats = default;
            ctx.allocator->CalculateStats(&begStats);

            const uint count = 10;
            const ulong bufSize = 64ul * 1024;
            ResourceWithAllocation* resources = stackalloc ResourceWithAllocation[(int)count];

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(&resourceDesc, bufSize);

            for (uint i = 0; i < count; ++i)
            {
                if (i == count / 2)
                    allocDesc.Flags |= ALLOCATION_FLAG_COMMITTED;
                Allocation* alloc = null;
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDesc,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&resources[i].resource));
                resources[i].allocation.reset(alloc);
            }

            Stats endStats = default;
            ctx.allocator->CalculateStats(&endStats);

            CHECK_BOOL(endStats.Total.BlockCount >= begStats.Total.BlockCount);
            CHECK_BOOL(endStats.Total.AllocationCount == begStats.Total.AllocationCount + count);
            CHECK_BOOL(endStats.Total.UsedBytes == begStats.Total.UsedBytes + count * bufSize);
            CHECK_BOOL(endStats.Total.AllocationSizeMin <= bufSize);
            CHECK_BOOL(endStats.Total.AllocationSizeMax >= bufSize);

            CHECK_BOOL(endStats.HeapType[1].BlockCount >= begStats.HeapType[1].BlockCount);
            CHECK_BOOL(endStats.HeapType[1].AllocationCount >= begStats.HeapType[1].AllocationCount + count);
            CHECK_BOOL(endStats.HeapType[1].UsedBytes >= begStats.HeapType[1].UsedBytes + count * bufSize);
            CHECK_BOOL(endStats.HeapType[1].AllocationSizeMin <= bufSize);
            CHECK_BOOL(endStats.HeapType[1].AllocationSizeMax >= bufSize);

            CHECK_BOOL(StatInfoEqual(ref begStats.HeapType[0], ref endStats.HeapType[0]));
            CHECK_BOOL(StatInfoEqual(ref begStats.HeapType[2], ref endStats.HeapType[2]));

            CheckStatInfo(ref endStats.Total);
            CheckStatInfo(ref endStats.HeapType[0]);
            CheckStatInfo(ref endStats.HeapType[1]);
            CheckStatInfo(ref endStats.HeapType[2]);

            Budget gpuBudget = default, cpuBudget = default;
            ctx.allocator->GetBudget(&gpuBudget, &cpuBudget);

            CHECK_BOOL(gpuBudget.AllocationBytes <= gpuBudget.BlockBytes);
            CHECK_BOOL(gpuBudget.AllocationBytes == endStats.HeapType[0].UsedBytes);
            CHECK_BOOL(gpuBudget.BlockBytes == endStats.HeapType[0].UsedBytes + endStats.HeapType[0].UnusedBytes);

            CHECK_BOOL(cpuBudget.AllocationBytes <= cpuBudget.BlockBytes);
            CHECK_BOOL(cpuBudget.AllocationBytes == endStats.HeapType[1].UsedBytes + endStats.HeapType[2].UsedBytes);
            CHECK_BOOL(cpuBudget.BlockBytes == endStats.HeapType[1].UsedBytes + endStats.HeapType[1].UnusedBytes +
                endStats.HeapType[2].UsedBytes + endStats.HeapType[2].UnusedBytes);
        }

        [Test]
        public static void TestTransfer()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            const uint count = 10;
            const ulong bufSize = 32ul * 1024;

            ResourceWithAllocation* resourcesUpload = stackalloc ResourceWithAllocation[(int)count];
            ResourceWithAllocation* resourcesDefault = stackalloc ResourceWithAllocation[(int)count];
            ResourceWithAllocation* resourcesReadback = stackalloc ResourceWithAllocation[(int)count];

            ALLOCATION_DESC allocDescUpload = default;
            allocDescUpload.HeapType = D3D12_HEAP_TYPE_UPLOAD;
            ALLOCATION_DESC allocDescDefault = default;
            allocDescDefault.HeapType = D3D12_HEAP_TYPE_DEFAULT;
            ALLOCATION_DESC allocDescReadback = default;
            allocDescReadback.HeapType = D3D12_HEAP_TYPE_READBACK;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(&resourceDesc, bufSize);

            // Create 3 sets of resources.
            for (uint i = 0; i < count; ++i)
            {
                Allocation* alloc = null;
                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDescUpload,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&resourcesUpload[i].resource));
                resourcesUpload[i].allocation.reset(alloc);

                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDescDefault,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_COPY_DEST,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&resourcesDefault[i].resource));
                resourcesDefault[i].allocation.reset(alloc);

                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDescReadback,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_COPY_DEST,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&resourcesReadback[i].resource));
                resourcesReadback[i].allocation.reset(alloc);
            }

            // Map and fill data in UPLOAD.
            for (uint i = 0; i < count; ++i)
            {
                void* mappedPtr = null;
                D3D12_RANGE EMPTY_RANGE = default;
                CHECK_HR(resourcesUpload[i].resource.Get()->Map(0, &EMPTY_RANGE, &mappedPtr));

                FillData(mappedPtr, bufSize, i);

                // Unmap every other resource, leave others mapped.
                if ((i % 2) != 0)
                {
                    resourcesUpload[i].resource.Get()->Unmap(0, null);
                }
            }

            // Transfer from UPLOAD to DEFAULT, from there to READBACK.
            ID3D12GraphicsCommandList* cmdList = runner.BeginCommandList();
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
            runner.EndCommandList(&cmdList);

            // Validate READBACK buffers.
            for (uint i = count; unchecked(i-- > 0);)
            {
                D3D12_RANGE mapRange = new(0, (nuint)bufSize);
                void* mappedPtr = null;
                CHECK_HR(resourcesReadback[i].resource.Get()->Map(0, &mapRange, &mappedPtr));

                CHECK_BOOL(ValidateData(mappedPtr, bufSize, i));

                // Unmap every 3rd resource, leave others mapped.
                if ((i % 3) != 0)
                {
                    D3D12_RANGE EMPTY_RANGE = default;
                    resourcesReadback[i].resource.Get()->Unmap(0, &EMPTY_RANGE);
                }
            }
        }

        [Test]
        public static void TestZeroInitialized()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            const ulong bufSize = 128ul * 1024;
            Allocation* alloc = null;

            D3D12_RESOURCE_DESC resourceDesc;
            FillResourceDescForBuffer(&resourceDesc, bufSize);

            // # Create upload buffer and fill it with data.

            ALLOCATION_DESC allocDescUpload = default;
            allocDescUpload.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            ResourceWithAllocation bufUpload = default;
            CHECK_HR(ctx.allocator->CreateResource(
                &allocDescUpload,
                &resourceDesc,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                null,
                &alloc,
                __uuidof<ID3D12Resource>(),
                (void**)&bufUpload.resource));
            bufUpload.allocation.reset(alloc);

            {
                void* mappedPtr = null;
                D3D12_RANGE EMPTY_RANGE = default;
                CHECK_HR(bufUpload.resource.Get()->Map(0, &EMPTY_RANGE, &mappedPtr));
                FillData(mappedPtr, bufSize, 5236245);
                bufUpload.resource.Get()->Unmap(0, null);
            }

            // # Create readback buffer

            ALLOCATION_DESC allocDescReadback = default;
            allocDescReadback.HeapType = D3D12_HEAP_TYPE_READBACK;

            ResourceWithAllocation bufReadback = default;
            CHECK_HR(ctx.allocator->CreateResource(
                &allocDescReadback,
                &resourceDesc,
                D3D12_RESOURCE_STATE_COPY_DEST,
                null,
                &alloc,
                __uuidof<ID3D12Resource>(),
                (void**)&bufReadback.resource));
            bufReadback.allocation.reset(alloc);

            static void CheckBufferData(TestRunner runner, ResourceWithAllocation* bufReadback, ResourceWithAllocation* buf)
            {
                bool shouldBeZero = buf->allocation.Get()->WasZeroInitialized() != FALSE;

                {
                    ID3D12GraphicsCommandList* cmdList = runner.BeginCommandList();
                    cmdList->CopyBufferRegion(bufReadback->resource, 0, buf->resource, 0, bufSize);
                    runner.EndCommandList(&cmdList);
                }

                bool isZero = false;
                {
                    D3D12_RANGE readRange = new(0, (nuint)bufSize); // I could pass pReadRange = NULL but it generates D3D Debug layer warning: EXECUTION WARNING #930: MAP_INVALID_NULLRANGE
                    void* mappedPtr = null;
                    CHECK_HR(bufReadback->resource.Get()->Map(0, &readRange, &mappedPtr));
                    isZero = ValidateDataZero(mappedPtr, bufSize);
                    D3D12_RANGE EMPTY_RANGE = default;
                    bufReadback->resource.Get()->Unmap(0, &EMPTY_RANGE);
                }

                Console.WriteLine($"Should be zero: {shouldBeZero}, is zero: {isZero}");

                if (shouldBeZero)
                {
                    CHECK_BOOL(isZero);
                }
            };

            // # Test 1: Committed resource. Should always be zero initialized.

            ResourceWithAllocation bufDefault = default;
            {
                ALLOCATION_DESC allocDescDefault = default;
                allocDescDefault.HeapType = D3D12_HEAP_TYPE_DEFAULT;
                allocDescDefault.Flags = ALLOCATION_FLAG_COMMITTED;

                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDescDefault,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_COPY_SOURCE,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&bufDefault.resource));
                bufDefault.allocation.reset(alloc);

                Console.Write("  Committed: ");
                CheckBufferData(runner, &bufReadback, &bufDefault);
                CHECK_BOOL(bufDefault.allocation.Get()->WasZeroInitialized() > 0);
            }

            // # Test 2: (Probably) placed resource.

            bufDefault = default;
            for (uint i = 0; i < 2; ++i)
            {
                // 1. Create buffer

                ALLOCATION_DESC allocDescDefault = default;
                allocDescDefault.HeapType = D3D12_HEAP_TYPE_DEFAULT;

                CHECK_HR(ctx.allocator->CreateResource(
                    &allocDescDefault,
                    &resourceDesc,
                    D3D12_RESOURCE_STATE_COPY_SOURCE,
                    null,
                    &alloc,
                    __uuidof<ID3D12Resource>(),
                    (void**)&bufDefault.resource));
                bufDefault.allocation.reset(alloc);

                // 2. Check it

                Console.Write($"  Normal #{i}: ");
                CheckBufferData(runner, &bufReadback, &bufDefault);

                // 3. Upload some data to it

                {
                    ID3D12GraphicsCommandList* cmdList = runner.BeginCommandList();

                    D3D12_RESOURCE_BARRIER barrier = default;
                    barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
                    barrier.Transition.pResource = bufDefault.resource;
                    barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_SOURCE;
                    barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_COPY_DEST;
                    barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
                    cmdList->ResourceBarrier(1, &barrier);

                    cmdList->CopyBufferRegion(bufDefault.resource, 0, bufUpload.resource, 0, bufSize);

                    runner.EndCommandList(&cmdList);
                }

                // 4. Delete it

                bufDefault.Reset();
            }
        }

        [Test]
        public static void TestMultithreading()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            const uint threadCount = 32;
            const uint bufSizeMin = 1024u;
            const uint bufSizeMax = 1024u * 1024;

            // Launch threads.
            //std::thread threads[threadCount];
            Thread[] threads = new Thread[threadCount];
            for (uint threadIndex = 0; threadIndex < threadCount; ++threadIndex)
            {
                void threadFunc()
                {
                    Random rand = new((int)threadIndex);

                    ALLOCATION_DESC allocDesc = default;
                    allocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

                    System.Collections.Generic.List<ResourceWithAllocation> resources = new(256);

                    // Create starting number of buffers.
                    const uint bufToCreateCount = 32;
                    for (uint bufIndex = 0; bufIndex < bufToCreateCount; ++bufIndex)
                    {
                        ResourceWithAllocation res = default;
                        res.dataSeed = (threadIndex << 16) | bufIndex;
                        res.size = AlignUp((nuint)(rand.Next() % (bufSizeMax - bufSizeMin) + bufSizeMin), 16);

                        D3D12_RESOURCE_DESC resourceDesc;
                        FillResourceDescForBuffer(&resourceDesc, res.size);

                        Allocation* alloc = null;
                        CHECK_HR(ctx.allocator->CreateResource(
                            &allocDesc,
                            &resourceDesc,
                            D3D12_RESOURCE_STATE_GENERIC_READ,
                            null,
                            &alloc,
                            __uuidof<ID3D12Resource>(),
                            (void**)&res.resource));
                        res.allocation.reset(alloc);

                        void* mappedPtr = null;
                        D3D12_RANGE EMPTY_RANGE = default;
                        CHECK_HR(res.resource.Get()->Map(0, &EMPTY_RANGE, &mappedPtr));

                        FillData(mappedPtr, res.size, res.dataSeed);

                        // Unmap some of them, leave others mapped.
                        if (rand.Next() % 2 == 1)
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
                        bool removePossible = resources.Count > 0;
                        bool remove = removePossible && (rand.Next() % 2 == 1);
                        if (remove)
                        {
                            uint indexToRemove = (uint)(rand.Next() % resources.Count);
                            resources.RemoveAt((int)indexToRemove);
                        }
                        else // Create new buffer.
                        {
                            ResourceWithAllocation res = default;
                            res.dataSeed = (threadIndex << 16) | operationIndex;
                            res.size = AlignUp((nuint)(rand.Next() % (bufSizeMax - bufSizeMin) + bufSizeMin), 16);
                            D3D12_RESOURCE_DESC resourceDesc;
                            FillResourceDescForBuffer(&resourceDesc, res.size);

                            Allocation* alloc = null;
                            CHECK_HR(ctx.allocator->CreateResource(
                                &allocDesc,
                                &resourceDesc,
                                D3D12_RESOURCE_STATE_GENERIC_READ,
                                null,
                                &alloc,
                                __uuidof<ID3D12Resource>(),
                                (void**)&res.resource));
                            res.allocation.reset(alloc);

                            void* mappedPtr = null;
                            CHECK_HR(res.resource.Get()->Map(0, null, &mappedPtr));

                            FillData(mappedPtr, res.size, res.dataSeed);

                            // Unmap some of them, leave others mapped.
                            if (rand.Next() % 2 == 1)
                            {
                                res.resource.Get()->Unmap(0, null);
                            }

                            resources.Add(res);
                        }
                    }

                    Sleep(20);

                    // Validate data in all remaining buffers while deleting them.
                    for (nuint resIndex = (nuint)resources.Count; unchecked(resIndex-- > 0);)
                    {
                        void* mappedPtr = null;
                        CHECK_HR(resources[(int)resIndex].resource.Get()->Map(0, null, &mappedPtr));

                        ValidateData(mappedPtr, resources[(int)resIndex].size, resources[(int)resIndex].dataSeed);

                        // Unmap some of them, leave others mapped.
                        if ((resIndex % 3) == 1)
                        {
                            D3D12_RANGE EMPTY_RANGE = default;
                            resources[(int)resIndex].resource.Get()->Unmap(0, &EMPTY_RANGE);
                        }

                        resources.RemoveAt((int)resIndex);
                    }
                };

                threads[threadIndex] = new(threadFunc);
                threads[threadIndex].Start();
            }

            // Wait for threads to finish.
            for (uint threadIndex = threadCount; unchecked(threadIndex-- > 0);)
            {
                threads[threadIndex].Join();
            }
        }

        [Test]
        public static void TestDevice4()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            using ComPtr<ID3D12Device4> dev4 = default;
            CHECK_HR(ctx.device->QueryInterface(__uuidof<ID3D12Device4>(), (void**)&dev4));

            D3D12_PROTECTED_RESOURCE_SESSION_DESC sessionDesc = default;
            using ComPtr<ID3D12ProtectedResourceSession> session = default;
            CHECK_HR(dev4.Get()->CreateProtectedResourceSession(
                &sessionDesc,
                __uuidof<ID3D12ProtectedResourceSession>(),
                (void**)&session));

            // Create a buffer

            D3D12_RESOURCE_DESC resourceDesc = default;
            FillResourceDescForBuffer(&resourceDesc, 1024);

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

            Allocation* alloc = null;
            using ComPtr<ID3D12Resource> bufRes = default;
            CHECK_HR(ctx.allocator->CreateResource1(&allocDesc, &resourceDesc,
                D3D12_RESOURCE_STATE_COMMON, null,
                session, &alloc, __uuidof<ID3D12Resource>(), (void**)&bufRes));
            using AllocationUniquePtr bufAllocPtr = alloc;

            // Create a heap
            // Temporarily commented out as it caues BSOD on RTX2080Ti driver 461.40.
            //D3D12_RESOURCE_ALLOCATION_INFO heapAllocInfo = new(
            //    D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT * 100, // SizeInBytes
            //    D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT // Alignment
            //);

            //CHECK_HR(ctx.allocator->AllocateMemory1(&allocDesc, &heapAllocInfo, session, &alloc));
            //using AllocationUniquePtr heapAllocPtr = alloc;
        }

        [Test]
        public static void TestDevice8()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            using TestRunner runner = new();
            runner.CreateContext(out TestContext ctx);

            using ComPtr<ID3D12Device8> dev8 = default;
            CHECK_HR(ctx.device->QueryInterface(__uuidof<ID3D12Device8>(), (void**)&dev8));

            D3D12_RESOURCE_DESC1 resourceDesc = default;
            FillResourceDescForBuffer(&resourceDesc, 1024 * 1024);

            // Create a committed buffer

            ALLOCATION_DESC allocDesc = default;
            allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
            allocDesc.Flags = ALLOCATION_FLAG_COMMITTED;

            Allocation* alloc0 = null;
            using ComPtr<ID3D12Resource> res0 = default;
            CHECK_HR(ctx.allocator->CreateResource2(&allocDesc, &resourceDesc,
                D3D12_RESOURCE_STATE_COMMON, null, null,
                &alloc0, __uuidof<ID3D12Resource>(), (void**)&res0));
            using AllocationUniquePtr allocPtr0 = alloc0;
            CHECK_BOOL(alloc0->GetHeap() == null);

            // Create a placed buffer

            allocDesc.Flags &= ~ALLOCATION_FLAG_COMMITTED;

            Allocation* alloc1 = null;
            using ComPtr<ID3D12Resource> res1 = null;
            CHECK_HR(ctx.allocator->CreateResource2(&allocDesc, &resourceDesc,
                D3D12_RESOURCE_STATE_COMMON, null, null,
                &alloc1, __uuidof<ID3D12Resource>(), (void**)&res1));
            using AllocationUniquePtr allocPtr1 = alloc1;
            CHECK_BOOL(alloc1->GetHeap() != null);
        }

        public static void CHECK_HR(int hResult) => Assert.IsTrue(SUCCEEDED(hResult));

        public static void CHECK_BOOL(bool cond) => Assert.IsTrue(cond);

        [NativeTypeName("UINT64")] private const ulong MEGABYTE = 1024 * 1024;

        internal struct D3d12maObjDeleter<T>
            where T : unmanaged
        {
            public void Invoke(T* obj)
            {
                if (obj != null)
                {
                    if (typeof(T) == typeof(Allocation))
                        ((Allocation*)obj)->Release();
                    else if (typeof(T) == typeof(Pool))
                        ((Pool*)obj)->Release();
                    else if (typeof(T) == typeof(VirtualBlock))
                        ((VirtualBlock*)obj)->Release();
                    else
                        throw new NotSupportedException("Invalid type argument");
                }
            }
        }

        private struct ResourceWithAllocation
        {
            public ComPtr<ID3D12Resource> resource;
            public AllocationUniquePtr allocation;
            [NativeTypeName("UINT64")] public ulong size;
            [NativeTypeName("UINT")] public uint dataSeed;

            public static void Init(out ResourceWithAllocation value)
            {
                value = default;
                value.size = UINT64_MAX;
                value.dataSeed = 0;
            }

            public void Reset()
            {
                resource.Get()->Release();
                allocation.reset();
                size = UINT64_MAX;
                dataSeed = 0;
            }
        }

        private static void FillResourceDescForBuffer<TD3D12_RESOURCE_DESC>(TD3D12_RESOURCE_DESC* outResourceDesc, [NativeTypeName("UINT64")] ulong size)
            where TD3D12_RESOURCE_DESC : unmanaged
        {
            *outResourceDesc = default;
            D3D12_RESOURCE_DESC* pOutResourceDesc = (D3D12_RESOURCE_DESC*)outResourceDesc;
            pOutResourceDesc->Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            pOutResourceDesc->Alignment = 0;
            pOutResourceDesc->Width = size;
            pOutResourceDesc->Height = 1;
            pOutResourceDesc->DepthOrArraySize = 1;
            pOutResourceDesc->MipLevels = 1;
            pOutResourceDesc->Format = DXGI_FORMAT_UNKNOWN;
            pOutResourceDesc->SampleDesc.Count = 1;
            pOutResourceDesc->SampleDesc.Quality = 0;
            pOutResourceDesc->Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            pOutResourceDesc->Flags = D3D12_RESOURCE_FLAG_NONE;
        }

        private static void FillData(void* outPtr, [NativeTypeName("UINT64")] ulong sizeInBytes, [NativeTypeName("UINT")] uint seed)
        {
            uint* outValues = (uint*)outPtr;
            ulong sizeInValues = sizeInBytes / sizeof(uint);
            uint value = seed;
            for (uint i = 0; i < sizeInValues; ++i)
            {
                outValues[i] = value++;
            }
        }

        private static bool ValidateData(void* ptr, [NativeTypeName("UINT64")] ulong sizeInBytes, [NativeTypeName("UINT")] uint seed)
        {
            uint* values = (uint*)ptr;
            ulong sizeInValues = sizeInBytes / sizeof(uint);
            uint value = seed;
            for (uint i = 0; i < sizeInValues; ++i)
            {
                if (values[i] != value++)
                {
                    //FAIL("ValidateData failed.");
                    return false;
                }
            }
            return true;
        }

        private static bool ValidateDataZero(void* ptr, [NativeTypeName("UINT64")] ulong sizeInBytes)
        {
            uint* values = (uint*)ptr;
            ulong sizeInValues = sizeInBytes / sizeof(uint);
            for (uint i = 0; i < sizeInValues; ++i)
            {
                if (values[i] != 0)
                {
                    //FAIL("ValidateData failed.");
                    return false;
                }
            }
            return true;
        }

        static ulong CeilDiv(ulong x, ulong y)
        {
            return (x + y - 1) / y;
        }
    }
}
