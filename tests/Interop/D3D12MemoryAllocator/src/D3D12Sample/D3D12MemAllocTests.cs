// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using TerraFX.Interop.Windows.D3D12;
using TerraFX.Interop.Windows.D3DCommon;
using TerraFX.Interop.Windows.DXGI;
using static TerraFX.Interop.Windows.D3DCommon.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.Windows.D3DCommon.D3D_PRIMITIVE_TOPOLOGY;
using static TerraFX.Interop.Windows.D3D12.D3D_ROOT_SIGNATURE_VERSION;
using static TerraFX.Interop.Windows.D3D12.D3D12_BLEND;
using static TerraFX.Interop.Windows.D3D12.D3D12_BLEND_OP;
using static TerraFX.Interop.Windows.D3D12.D3D12_CLEAR_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_COLOR_WRITE_ENABLE;
using static TerraFX.Interop.Windows.D3D12.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_COMPARISON_FUNC;
using static TerraFX.Interop.Windows.D3D12.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
using static TerraFX.Interop.Windows.D3D12.D3D12_CULL_MODE;
using static TerraFX.Interop.Windows.D3D12.D3D12_DEPTH_WRITE_MASK;
using static TerraFX.Interop.Windows.D3D12.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_DESCRIPTOR_HEAP_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_DESCRIPTOR_RANGE_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_DSV_DIMENSION;
using static TerraFX.Interop.Windows.D3D12.D3D12_DSV_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_FENCE_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_FILL_MODE;
using static TerraFX.Interop.Windows.D3D12.D3D12_FILTER;
using static TerraFX.Interop.Windows.D3D12.D3D12_HEAP_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_INPUT_CLASSIFICATION;
using static TerraFX.Interop.Windows.D3D12.D3D12_LOGIC_OP;
using static TerraFX.Interop.Windows.D3D12.D3D12_PRIMITIVE_TOPOLOGY_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_BARRIER_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_HEAP_TIER;
using static TerraFX.Interop.Windows.D3D12.D3D12_RESOURCE_STATES;
using static TerraFX.Interop.Windows.D3D12.D3D12_ROOT_PARAMETER_TYPE;
using static TerraFX.Interop.Windows.D3D12.D3D12_ROOT_SIGNATURE_FLAGS;
using static TerraFX.Interop.Windows.D3D12.D3D12_SHADER_VISIBILITY;
using static TerraFX.Interop.Windows.D3D12.D3D12_SRV_DIMENSION;
using static TerraFX.Interop.Windows.D3D12.D3D12_STATIC_BORDER_COLOR;
using static TerraFX.Interop.Windows.D3D12.D3D12_STENCIL_OP;
using static TerraFX.Interop.Windows.D3D12.D3D12_TEXTURE_ADDRESS_MODE;
using static TerraFX.Interop.Windows.D3D12.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.Windows.DXGI.DXGI_ADAPTER_FLAG;
using static TerraFX.Interop.Windows.DXGI.DXGI_FORMAT;
using static TerraFX.Interop.Windows.DXGI.DXGI_SWAP_EFFECT;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.D3D12.D3D12;
using static TerraFX.Interop.Windows.DXGI.DXGI;
using static TerraFX.Interop.Windows.D3D12MA.D3D12MA_ALLOCATOR_FLAGS;
using static TerraFX.Interop.Windows.D3D12MA.D3D12MemAlloc;

namespace TerraFX.Interop.Windows.D3D12MA.UnitTests
{
    internal unsafe static partial class D3D12MemAllocTests
    {
        [NativeTypeName("const wchar_t* const")]
        private const string CLASS_NAME = "D3D12MemAllocSample";

        [NativeTypeName("const wchar_t* const")]
        private const string WINDOW_TITLE = "D3D12 Memory Allocator Sample";

        private const int SIZE_X = 1024;

        private const int SIZE_Y = 576;

        private const bool FULLSCREEN = false;

        private const uint PRESENT_SYNC_INTERVAL = 1;

        private const DXGI_FORMAT RENDER_TARGET_FORMAT = DXGI_FORMAT_R8G8B8A8_UNORM;

        private const DXGI_FORMAT DEPTH_STENCIL_FORMAT = DXGI_FORMAT_D32_FLOAT;

        [NativeTypeName("size_t")]
        private const nuint FRAME_BUFFER_COUNT = 3; // number of buffers we want, 2 for double buffering, 3 for tripple buffering

        private const D3D_FEATURE_LEVEL MY_D3D_FEATURE_LEVEL = D3D_FEATURE_LEVEL_12_0;

        private const bool ENABLE_DEBUG_LAYER = true;

        private const bool ENABLE_CPU_ALLOCATION_CALLBACKS = true;

        private static readonly bool ENABLE_CPU_ALLOCATION_CALLBACKS_PRINT = false;

        private const D3D12MA_ALLOCATOR_FLAGS g_AllocatorFlags = D3D12MA_ALLOCATOR_FLAG_NONE;

        private static D3D12MA_ALLOCATION_CALLBACKS* g_AllocationCallbacks = (D3D12MA_ALLOCATION_CALLBACKS*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAllocTests), sizeof(D3D12MA_ALLOCATION_CALLBACKS)); // Used only when ENABLE_CPU_ALLOCATION_CALLBACKS

        private static HINSTANCE g_Instance;

        private static HWND g_Wnd;

        [NativeTypeName("UINT64")]
        private static ulong g_TimeOffset; // In ms.

        [NativeTypeName("UINT64")]
        private static ulong g_TimeValue; // Time since g_TimeOffset, in ms.

        private static float g_Time; // g_TimeValue converted to float, in seconds.

        private static float g_TimeDelta;

        private static ComPtr<ID3D12Device> g_Device;

        private static D3D12MA_Allocator* g_Allocator;

        private static ComPtr<IDXGISwapChain3> g_SwapChain; // swapchain used to switch between render targets

        private static ComPtr<ID3D12CommandQueue> g_CommandQueue; // container for command lists

        private static ComPtr<ID3D12DescriptorHeap> g_RtvDescriptorHeap; // a descriptor heap to hold resources like the render targets

        private static _e__FixedBuffer<ComPtr<ID3D12Resource>> g_RenderTargets; // number of render targets equal to buffer count

        private static _e__FixedBuffer<ComPtr<ID3D12CommandAllocator>> g_CommandAllocators; // we want enough allocators for each buffer * number of threads (we only have one thread)

        private static ComPtr<ID3D12GraphicsCommandList> g_CommandList; // a command list we can record commands into, then execute them to render the frame

        private static _e__FixedBuffer<ComPtr<ID3D12Fence>> g_Fences;    // an object that is locked while our command list is being executed by the gpu. We need as many 
                                                                         // as we have allocators (more if we want to know when the gpu is finished with an asset)

        private static HANDLE g_FenceEvent; // a handle to an event when our g_Fences is unlocked by the gpu

        [NativeTypeName("UINT64")]
        private static _e__FixedBuffer<ulong> g_FenceValues; // this value is incremented each frame. each g_Fences will have its own value

        [NativeTypeName("UINT")]
        private static uint g_FrameIndex; // current rtv we are on

        [NativeTypeName("UINT")]
        private static uint g_RtvDescriptorSize; // size of the rtv descriptor on the g_Device (all front and back buffers will be the same size)

        private static ComPtr<ID3D12PipelineState> g_PipelineStateObject;

        private static ComPtr<ID3D12RootSignature> g_RootSignature;

        private static ComPtr<ID3D12Resource> g_VertexBuffer;

        private static D3D12MA_Allocation* g_VertexBufferAllocation;

        private static ComPtr<ID3D12Resource> g_IndexBuffer;

        private static D3D12MA_Allocation* g_IndexBufferAllocation;

        private static D3D12_VERTEX_BUFFER_VIEW* g_VertexBufferView = (D3D12_VERTEX_BUFFER_VIEW*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAllocTests), sizeof(D3D12_VERTEX_BUFFER_VIEW));

        private static D3D12_INDEX_BUFFER_VIEW* g_IndexBufferView = (D3D12_INDEX_BUFFER_VIEW*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAllocTests), sizeof(D3D12_INDEX_BUFFER_VIEW));

        private static ComPtr<ID3D12Resource> g_DepthStencilBuffer;

        private static D3D12MA_Allocation* g_DepthStencilAllocation;

        private static ComPtr<ID3D12DescriptorHeap> g_DepthStencilDescriptorHeap;

        [NativeTypeName("size_t")]
        private static readonly nuint ConstantBufferPerObjectAlignedSize = AlignUp((nuint)sizeof(ConstantBuffer1_VS), 256);

        private static _e__FixedBuffer<Pointer<D3D12MA_Allocation>> g_CbPerObjectUploadHeapAllocations;

        private static _e__FixedBuffer<ComPtr<ID3D12Resource>> g_CbPerObjectUploadHeaps;

        private static _e__FixedBuffer<Pointer> g_CbPerObjectAddress;

        [NativeTypeName("uint32_t")]
        private static uint g_CubeIndexCount;

        private static _e__FixedBuffer<ComPtr<ID3D12DescriptorHeap>> g_MainDescriptorHeap;

        private static _e__FixedBuffer<ComPtr<ID3D12Resource>> g_ConstantBufferUploadHeap;

        private static _e__FixedBuffer<Pointer<D3D12MA_Allocation>> g_ConstantBufferUploadAllocation;

        private static _e__FixedBuffer<Pointer> g_ConstantBufferAddress;

        private static ComPtr<ID3D12Resource> g_Texture;

        private static D3D12MA_Allocation* g_TextureAllocation;

        [NativeTypeName("void* const")]
        private static void* CUSTOM_ALLOCATION_USER_DATA = (void*)(nuint)0xDEADC0DE;

        [NativeTypeName("std::atomic<size_t>")]
        private static volatile nuint g_CpuAllocationCount = 0;

        private static void* CustomAllocate([NativeTypeName("size_t")] nuint Size, [NativeTypeName("size_t")] nuint Alignment, void* pUserData)
        {
            Debug.Assert(pUserData == CUSTOM_ALLOCATION_USER_DATA);

            void* memory = _aligned_malloc(Size, Alignment);

            if (ENABLE_CPU_ALLOCATION_CALLBACKS_PRINT)
            {
                Console.WriteLine("Allocate Size={0} Alignment={1} -> {2:X}", Size, Alignment, (nuint)memory);
            }

            ++g_CpuAllocationCount;
            return memory;
        }

        private static void CustomFree(void* pMemory, void* pUserData)
        {
            Debug.Assert(pUserData == CUSTOM_ALLOCATION_USER_DATA);

            if (pMemory != null)
            {
                --g_CpuAllocationCount;

                if (ENABLE_CPU_ALLOCATION_CALLBACKS_PRINT)
                {
                    Console.WriteLine("Free {0:X}", (nuint)pMemory);
                }

                _aligned_free(pMemory);
            }
        }

        private static void SetDefaultRasterizerDesc([NativeTypeName("D3D12_RASTERIZER_DESC&")] ref D3D12_RASTERIZER_DESC outDesc)
        {
            outDesc.FillMode = D3D12_FILL_MODE_SOLID;
            outDesc.CullMode = D3D12_CULL_MODE_BACK;
            outDesc.FrontCounterClockwise = FALSE;
            outDesc.DepthBias = D3D12_DEFAULT_DEPTH_BIAS;
            outDesc.DepthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP;
            outDesc.SlopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
            outDesc.DepthClipEnable = TRUE;
            outDesc.MultisampleEnable = FALSE;
            outDesc.AntialiasedLineEnable = FALSE;
            outDesc.ForcedSampleCount = 0;
            outDesc.ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;
        }

        private static void SetDefaultBlendDesc([NativeTypeName("D3D12_BLEND_DESC&")] ref D3D12_BLEND_DESC outDesc)
        {
            outDesc.AlphaToCoverageEnable = FALSE;
            outDesc.IndependentBlendEnable = FALSE;

            D3D12_RENDER_TARGET_BLEND_DESC defaultRenderTargetBlendDesc = new D3D12_RENDER_TARGET_BLEND_DESC {
                BlendEnable = FALSE,
                LogicOpEnable = FALSE,
                SrcBlend = D3D12_BLEND_ONE,
                DestBlend = D3D12_BLEND_ZERO,
                BlendOp = D3D12_BLEND_OP_ADD,
                SrcBlendAlpha = D3D12_BLEND_ONE,
                DestBlendAlpha = D3D12_BLEND_ZERO,
                BlendOpAlpha = D3D12_BLEND_OP_ADD,
                LogicOp = D3D12_LOGIC_OP_NOOP,
                RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE_ALL
            };

            for (uint i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; ++i)
            {
                outDesc.RenderTarget[(int)i] = defaultRenderTargetBlendDesc;
            }
        }

        private static void SetDefaultDepthStencilDesc([NativeTypeName("D3D12_DEPTH_STENCIL_DESC&")] ref D3D12_DEPTH_STENCIL_DESC outDesc)
        {
            outDesc.DepthEnable = TRUE;
            outDesc.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;
            outDesc.DepthFunc = D3D12_COMPARISON_FUNC_LESS;
            outDesc.StencilEnable = FALSE;
            outDesc.StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK;
            outDesc.StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK;

            D3D12_DEPTH_STENCILOP_DESC defaultStencilOp = new D3D12_DEPTH_STENCILOP_DESC {
                StencilFailOp = D3D12_STENCIL_OP_KEEP,
                StencilDepthFailOp = D3D12_STENCIL_OP_KEEP,
                StencilPassOp = D3D12_STENCIL_OP_KEEP,
                StencilFunc = D3D12_COMPARISON_FUNC_ALWAYS
            };

            outDesc.FrontFace = defaultStencilOp;
            outDesc.BackFace = defaultStencilOp;
        }

        private static void WaitForFrame([NativeTypeName("size_t")] nuint frameIndex) // wait until gpu is finished with command list
        {
            // if the current g_Fences value is still less than "g_FenceValues", then we know the GPU has not finished executing
            // the command queue since it has not reached the "g_CommandQueue->Signal(g_Fences, g_FenceValues)" command
            if (g_Fences[(int)frameIndex].Get()->GetCompletedValue() < g_FenceValues[(int)frameIndex])
            {
                // we have the g_Fences create an event which is signaled once the g_Fences's current value is "g_FenceValues"
                CHECK_HR(g_Fences[(int)frameIndex].Get()->SetEventOnCompletion(g_FenceValues[(int)frameIndex], g_FenceEvent));

                // We will wait until the g_Fences has triggered the event that it's current value has reached "g_FenceValues". once it's value
                // has reached "g_FenceValues", we know the command queue has finished executing
                _ = WaitForSingleObject(g_FenceEvent, INFINITE);
            }
        }

        private static void WaitGPUIdle([NativeTypeName("size_t")] nuint frameIndex)
        {
            g_FenceValues[(int)frameIndex]++;
            CHECK_HR(g_CommandQueue.Get()->Signal(g_Fences[(int)frameIndex], g_FenceValues[(int)frameIndex]));
            WaitForFrame(frameIndex);
        }

        private static void InitD3D() // initializes direct3d 12
        {
            IDXGIFactory4* dxgiFactory;
            CHECK_HR(CreateDXGIFactory1(__uuidof<IDXGIFactory4>(), (void**)&dxgiFactory));

            IDXGIAdapter1* adapter = null; // adapters are the graphics card (this includes the embedded graphics on the motherboard)
            int adapterIndex = 0; // we'll start looking for directx 12  compatible graphics devices starting at index 0
            bool adapterFound = false; // set this to true when a good one was found

            // find first hardware gpu that supports d3d 12
            while (dxgiFactory->EnumAdapters1((uint)adapterIndex, &adapter) != DXGI_ERROR_NOT_FOUND)
            {
                DXGI_ADAPTER_DESC1 desc;
                _ = adapter->GetDesc1(&desc);

                if ((desc.Flags & (uint)DXGI_ADAPTER_FLAG_SOFTWARE) == 0)
                {
                    HRESULT hr = D3D12CreateDevice((IUnknown*)adapter, MY_D3D_FEATURE_LEVEL, __uuidof<ID3D12Device>(), null);

                    if (SUCCEEDED(hr))
                    {
                        adapterFound = true;
                        break;
                    }
                }

                _ = adapter->Release();
                adapterIndex++;
            }

            Debug.Assert(adapterFound);

            // Must be done before D3D12 device is created.
            if (ENABLE_DEBUG_LAYER)
            {
                using ComPtr<ID3D12Debug> debug = default;
                if (SUCCEEDED(D3D12GetDebugInterface(__uuidof<ID3D12Debug>(), (void**)&debug)))
                {
                    debug.Get()->EnableDebugLayer();
                }
            }

            // Create the g_Device
            ID3D12Device* device = null;

            CHECK_HR(D3D12CreateDevice(
                (IUnknown*)adapter,
                MY_D3D_FEATURE_LEVEL,
                __uuidof<ID3D12Device>(),
                (void**)&device
            ));

            g_Device.Attach(device);

            // Create allocator
            D3D12MA_Allocator* allocator = null;

            {
                D3D12MA_ALLOCATOR_DESC desc = default;
                desc.Flags = g_AllocatorFlags;
                desc.pDevice = device;
                desc.pAdapter = (IDXGIAdapter*)adapter;

                if (ENABLE_CPU_ALLOCATION_CALLBACKS)
                {
                    g_AllocationCallbacks->pAllocate = &CustomAllocate;
                    g_AllocationCallbacks->pFree = &CustomFree;
                    g_AllocationCallbacks->pUserData = CUSTOM_ALLOCATION_USER_DATA;
                    desc.pAllocationCallbacks = g_AllocationCallbacks;
                }

                CHECK_HR(D3D12MA_CreateAllocator(&desc, &allocator));
                g_Allocator = allocator;

                switch (g_Allocator->GetD3D12Options()->ResourceHeapTier)
                {
                    case D3D12_RESOURCE_HEAP_TIER_1:
                    {
                        Console.WriteLine("ResourceHeapTier = D3D12_RESOURCE_HEAP_TIER_1");
                        break;
                    }

                    case D3D12_RESOURCE_HEAP_TIER_2:
                    {
                        Console.WriteLine("ResourceHeapTier = D3D12_RESOURCE_HEAP_TIER_2");
                        break;
                    }

                    default:
                    {
                        Debug.Assert(false, "unreached");
                        break;
                    }
                }
            }

            // -- Create the Command Queue -- //

            D3D12_COMMAND_QUEUE_DESC cqDesc = default; // we will be using all the default values

            ID3D12CommandQueue* commandQueue = null;
            CHECK_HR(g_Device.Get()->CreateCommandQueue(&cqDesc, __uuidof<ID3D12CommandQueue>(), (void**)&commandQueue)); // create the command queue
            g_CommandQueue.Attach(commandQueue);

            // -- Create the Swap Chain (double/tripple buffering) -- //

            DXGI_MODE_DESC backBufferDesc = default; // this is to describe our display mode
            backBufferDesc.Width = SIZE_X; // buffer width
            backBufferDesc.Height = SIZE_Y; // buffer height
            backBufferDesc.Format = RENDER_TARGET_FORMAT; // format of the buffer (rgba 32 bits, 8 bits for each chanel)

            // describe our multi-sampling. We are not multi-sampling, so we set the count to 1 (we need at least one sample of course)
            DXGI_SAMPLE_DESC sampleDesc = default;
            sampleDesc.Count = 1; // multisample count (no multisampling, so we just put 1, since we still need 1 sample)

            // Describe and create the swap chain.
            DXGI_SWAP_CHAIN_DESC swapChainDesc = default;
            swapChainDesc.BufferCount = (uint)FRAME_BUFFER_COUNT; // number of buffers we have
            swapChainDesc.BufferDesc = backBufferDesc; // our back buffer description
            swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT; // this says the pipeline will render to this swap chain
            swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD; // dxgi will discard the buffer (data) after we call present
            swapChainDesc.OutputWindow = g_Wnd; // handle to our window
            swapChainDesc.SampleDesc = sampleDesc; // our multi-sampling description
            swapChainDesc.Windowed = !FULLSCREEN ? TRUE : FALSE; // set to true, then if in fullscreen must call SetFullScreenState with true for full screen to get uncapped fps

            IDXGISwapChain* tempSwapChain;

            CHECK_HR(dxgiFactory->CreateSwapChain(
                (IUnknown*)g_CommandQueue.Get(), // the queue will be flushed once the swap chain is created
                &swapChainDesc, // give it the swap chain description we created above
                &tempSwapChain // store the created swap chain in a temp IDXGISwapChain interface
            ));

            g_SwapChain.Attach((IDXGISwapChain3*)tempSwapChain);

            g_FrameIndex = g_SwapChain.Get()->GetCurrentBackBufferIndex();

            // -- Create the Back Buffers (render target views) Descriptor Heap -- //

            // describe an rtv descriptor heap and create
            D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = default;
            rtvHeapDesc.NumDescriptors = (uint)FRAME_BUFFER_COUNT; // number of descriptors for this heap.
            rtvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV; // this heap is a render target view heap

            // This heap will not be directly referenced by the shaders (not shader visible), as this will store the output from the pipeline
            // otherwise we would set the heap's flag to D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
            rtvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
            ID3D12DescriptorHeap* rtvDescriptorHeap = null;
            CHECK_HR(g_Device.Get()->CreateDescriptorHeap(&rtvHeapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)&rtvDescriptorHeap));
            g_RtvDescriptorHeap.Attach(rtvDescriptorHeap);

            // get the size of a descriptor in this heap (this is a rtv heap, so only rtv descriptors should be stored in it.
            // descriptor sizes may vary from g_Device to g_Device, which is why there is no set size and we must ask the 
            // g_Device to give us the size. we will use this size to increment a descriptor handle offset
            g_RtvDescriptorSize = g_Device.Get()->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

            // get a handle to the first descriptor in the descriptor heap. a handle is basically a pointer,
            // but we cannot literally use it like a c++ pointer.
            D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = g_RtvDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart();

            // Create a RTV for each buffer (double buffering is two buffers, tripple buffering is 3).
            for (int i = 0; i < (int)FRAME_BUFFER_COUNT; i++)
            {
                // first we get the n'th buffer in the swap chain and store it in the n'th
                // position of our ID3D12Resource array
                ID3D12Resource* res = null;
                CHECK_HR(g_SwapChain.Get()->GetBuffer((uint)i, __uuidof<ID3D12Resource>(), (void**)&res));
                g_RenderTargets[i].Attach(res);

                // the we "create" a render target view which binds the swap chain buffer (ID3D12Resource[n]) to the rtv handle
                g_Device.Get()->CreateRenderTargetView(g_RenderTargets[i], null, rtvHandle);

                // we increment the rtv handle by the rtv descriptor size we got above
                rtvHandle.ptr += g_RtvDescriptorSize;
            }

            // -- Create the Command Allocators -- //

            for (int i = 0; i < (int)FRAME_BUFFER_COUNT; i++)
            {
                ID3D12CommandAllocator* commandAllocator = null;
                CHECK_HR(g_Device.Get()->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, __uuidof<ID3D12CommandAllocator>(), (void**)&commandAllocator));
                g_CommandAllocators[i].Attach(commandAllocator);
            }

            // create the command list with the first allocator
            ID3D12GraphicsCommandList* commandList = null;
            CHECK_HR(g_Device.Get()->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, g_CommandAllocators[0], null, __uuidof<ID3D12GraphicsCommandList>(), (void**)&commandList));
            g_CommandList = commandList;

            // command lists are created in the recording state. our main loop will set it up for recording again so close it now
            _ = g_CommandList.Get()->Close();

            // create a depth stencil descriptor heap so we can get a pointer to the depth stencil buffer
            D3D12_DESCRIPTOR_HEAP_DESC dsvHeapDesc = default;
            dsvHeapDesc.NumDescriptors = 1;
            dsvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV;
            dsvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;

            ID3D12DescriptorHeap* depthStencilDescriptorHeap = null;
            CHECK_HR(g_Device.Get()->CreateDescriptorHeap(&dsvHeapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)&depthStencilDescriptorHeap));
            g_DepthStencilDescriptorHeap = depthStencilDescriptorHeap;

            D3D12_CLEAR_VALUE depthOptimizedClearValue = default;
            depthOptimizedClearValue.Format = DEPTH_STENCIL_FORMAT;
            depthOptimizedClearValue.DepthStencil.Depth = 1.0f;
            depthOptimizedClearValue.DepthStencil.Stencil = 0;

            D3D12MA_ALLOCATION_DESC depthStencilAllocDesc = default;
            depthStencilAllocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

            D3D12_RESOURCE_DESC depthStencilResourceDesc = default;
            depthStencilResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
            depthStencilResourceDesc.Alignment = 0;
            depthStencilResourceDesc.Width = SIZE_X;
            depthStencilResourceDesc.Height = SIZE_Y;
            depthStencilResourceDesc.DepthOrArraySize = 1;
            depthStencilResourceDesc.MipLevels = 1;
            depthStencilResourceDesc.Format = DEPTH_STENCIL_FORMAT;
            depthStencilResourceDesc.SampleDesc.Count = 1;
            depthStencilResourceDesc.SampleDesc.Quality = 0;
            depthStencilResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
            depthStencilResourceDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;

            D3D12MA_Allocation* depthStencilAllocation = null;
            ID3D12Resource* depthStencilBuffer = null;
            CHECK_HR(g_Allocator->CreateResource(
                &depthStencilAllocDesc,
                &depthStencilResourceDesc,
                D3D12_RESOURCE_STATE_DEPTH_WRITE,
                &depthOptimizedClearValue,
                &depthStencilAllocation,
                __uuidof<ID3D12Resource>(),
                (void**)&depthStencilBuffer
            ));
            g_DepthStencilAllocation = depthStencilAllocation;
            g_DepthStencilBuffer = depthStencilBuffer;

            fixed (char* name = "Depth/Stencil Resource Heap")
            {
                CHECK_HR(g_DepthStencilBuffer.Get()->SetName((ushort*)name));
            }

            D3D12_DEPTH_STENCIL_VIEW_DESC depthStencilDesc = default;
            depthStencilDesc.Format = DEPTH_STENCIL_FORMAT;
            depthStencilDesc.ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D;
            depthStencilDesc.Flags = D3D12_DSV_FLAG_NONE;

            g_Device.Get()->CreateDepthStencilView(g_DepthStencilBuffer, &depthStencilDesc, g_DepthStencilDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart());

            // -- Create a Fence & Fence Event -- //

            // create the fences
            for (int i = 0; i < (int)FRAME_BUFFER_COUNT; i++)
            {
                ID3D12Fence* fence = null;
                CHECK_HR(g_Device.Get()->CreateFence(0, D3D12_FENCE_FLAG_NONE, __uuidof<ID3D12Fence>(), (void**)&fence));
                g_Fences[i].Attach(fence);

                g_FenceValues[i] = 0; // set the initial g_Fences value to 0
            }

            // create a handle to a g_Fences event
            g_FenceEvent = CreateEvent(null, FALSE, FALSE, null);
            Debug.Assert(g_FenceEvent != IntPtr.Zero);

            D3D12_DESCRIPTOR_RANGE cbDescriptorRange;
            cbDescriptorRange.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_CBV;
            cbDescriptorRange.NumDescriptors = 1;
            cbDescriptorRange.BaseShaderRegister = 0;
            cbDescriptorRange.RegisterSpace = 0;
            cbDescriptorRange.OffsetInDescriptorsFromTableStart = 0;

            D3D12_DESCRIPTOR_RANGE textureDescRange;
            textureDescRange.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV;
            textureDescRange.NumDescriptors = 1;
            textureDescRange.BaseShaderRegister = 0;
            textureDescRange.RegisterSpace = 0;
            textureDescRange.OffsetInDescriptorsFromTableStart = 1;

            const uint _countof_rootParameters = 3;

            D3D12_ROOT_PARAMETER* rootParameters = stackalloc D3D12_ROOT_PARAMETER[(int)_countof_rootParameters];

            rootParameters[0].ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
            rootParameters[0].DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE { NumDescriptorRanges = 1, pDescriptorRanges = &cbDescriptorRange };
            rootParameters[0].ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;

            rootParameters[1].ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV;
            rootParameters[1].Descriptor = new D3D12_ROOT_DESCRIPTOR { ShaderRegister = 1, RegisterSpace = 0 };
            rootParameters[1].ShaderVisibility = D3D12_SHADER_VISIBILITY_VERTEX;

            rootParameters[2].ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
            rootParameters[2].DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE { NumDescriptorRanges = 1, pDescriptorRanges = &textureDescRange };
            rootParameters[2].ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;

            // create root signature

            // create a static sampler
            D3D12_STATIC_SAMPLER_DESC sampler = default;
            sampler.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
            sampler.AddressU = D3D12_TEXTURE_ADDRESS_MODE_BORDER;
            sampler.AddressV = D3D12_TEXTURE_ADDRESS_MODE_BORDER;
            sampler.AddressW = D3D12_TEXTURE_ADDRESS_MODE_BORDER;
            sampler.MipLODBias = 0;
            sampler.MaxAnisotropy = 0;
            sampler.ComparisonFunc = D3D12_COMPARISON_FUNC_NEVER;
            sampler.BorderColor = D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK;
            sampler.MinLOD = 0.0f;
            sampler.MaxLOD = D3D12_FLOAT32_MAX;
            sampler.ShaderRegister = 0;
            sampler.RegisterSpace = 0;
            sampler.ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;

            D3D12_ROOT_SIGNATURE_DESC rootSignatureDesc = default;
            rootSignatureDesc.NumParameters = _countof_rootParameters;
            rootSignatureDesc.pParameters = rootParameters;
            rootSignatureDesc.NumStaticSamplers = 1;
            rootSignatureDesc.pStaticSamplers = &sampler;
            rootSignatureDesc.Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT | D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS | D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS | D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS;

            using ComPtr<ID3DBlob> signatureBlob = default;
            ID3DBlob* signatureBlobPtr;
            CHECK_HR(D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signatureBlobPtr, null));
            signatureBlob.Attach(signatureBlobPtr);

            ID3D12RootSignature* rootSignature = null;
            CHECK_HR(device->CreateRootSignature(0, signatureBlob.Get()->GetBufferPointer(), signatureBlob.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), (void**)&rootSignature));
            g_RootSignature.Attach(rootSignature);

            for (int i = 0; i < (int)FRAME_BUFFER_COUNT; ++i)
            {
                D3D12_DESCRIPTOR_HEAP_DESC heapDesc = default;
                heapDesc.NumDescriptors = 2;
                heapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
                heapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;

                ID3D12DescriptorHeap* mainDescriptorHeap = null;
                CHECK_HR(g_Device.Get()->CreateDescriptorHeap(&heapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)&mainDescriptorHeap));
                g_MainDescriptorHeap[i] = mainDescriptorHeap;
            }

            // # CONSTANT BUFFER
            *EMPTY_RANGE = default;

            for (int i = 0; i < (int)FRAME_BUFFER_COUNT; ++i)
            {
                D3D12MA_ALLOCATION_DESC constantBufferUploadAllocDesc = default;
                constantBufferUploadAllocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

                D3D12_RESOURCE_DESC constantBufferResourceDesc = default;
                constantBufferResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
                constantBufferResourceDesc.Alignment = 0;
                constantBufferResourceDesc.Width = 1024 * 64;
                constantBufferResourceDesc.Height = 1;
                constantBufferResourceDesc.DepthOrArraySize = 1;
                constantBufferResourceDesc.MipLevels = 1;
                constantBufferResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
                constantBufferResourceDesc.SampleDesc.Count = 1;
                constantBufferResourceDesc.SampleDesc.Quality = 0;
                constantBufferResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
                constantBufferResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

                D3D12MA_Allocation* constantBufferUploadAllocation = null;
                ID3D12Resource* constantBufferUploadHeap = null;
                CHECK_HR(g_Allocator->CreateResource(
                    &constantBufferUploadAllocDesc,
                    &constantBufferResourceDesc,
                    D3D12_RESOURCE_STATE_GENERIC_READ,
                    null,
                    &constantBufferUploadAllocation,
                    __uuidof<ID3D12Resource>(), (void**)&constantBufferUploadHeap
                ));
                g_ConstantBufferUploadAllocation[i] = constantBufferUploadAllocation;
                g_ConstantBufferUploadHeap[i] = constantBufferUploadHeap;

                fixed (char* name = "Constant Buffer Upload Resource Heap")
                {
                    _ = g_ConstantBufferUploadHeap[i].Get()->SetName((ushort*)name);
                }

                D3D12_CONSTANT_BUFFER_VIEW_DESC cbvDesc = default;
                cbvDesc.BufferLocation = g_ConstantBufferUploadHeap[i].Get()->GetGPUVirtualAddress();
                cbvDesc.SizeInBytes = AlignUp((uint)sizeof(ConstantBuffer0_PS), 256u);
                g_Device.Get()->CreateConstantBufferView(&cbvDesc, g_MainDescriptorHeap[i].Get()->GetCPUDescriptorHandleForHeapStart());

                void* constantBufferAddress = null;
                CHECK_HR(g_ConstantBufferUploadHeap[i].Get()->Map(0, EMPTY_RANGE, &constantBufferAddress));
                g_ConstantBufferAddress[i] = constantBufferAddress;
            }

            // create input layout

            // The input layout is used by the Input Assembler so that it knows
            // how to read the vertex data bound to it.

            const uint _countof_inputLayout = 2;

            D3D12_INPUT_ELEMENT_DESC* inputLayout = stackalloc D3D12_INPUT_ELEMENT_DESC[(int)_countof_inputLayout];

            inputLayout[0] = new D3D12_INPUT_ELEMENT_DESC {
                SemanticName = (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(POSITION_SEMANTIC_NAME)),
                SemanticIndex = 0,
                Format = DXGI_FORMAT_R32G32B32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = D3D12_APPEND_ALIGNED_ELEMENT,
                InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                InstanceDataStepRate = 0
            };

            inputLayout[1] = new D3D12_INPUT_ELEMENT_DESC {
                SemanticName = (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(TEXCOORD_SEMANTIC_NAME)),
                SemanticIndex = 0,
                Format = DXGI_FORMAT_R32G32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = D3D12_APPEND_ALIGNED_ELEMENT,
                InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                InstanceDataStepRate = 0
            };

            // create a pipeline state object (PSO)

            // In a real application, you will have many pso's. for each different shader
            // or different combinations of shaders, different blend states or different rasterizer states,
            // different topology types (point, line, triangle, patch), or a different number
            // of render targets you will need a pso

            // VS is the only required shader for a pso. You might be wondering when a case would be where
            // you only set the VS. It's possible that you have a pso that only outputs data with the stream
            // output, and not on a render target, which means you would not need anything after the stream
            // output.

            D3D12_GRAPHICS_PIPELINE_STATE_DESC psoDesc = default; // a structure to define a pso
            psoDesc.InputLayout.NumElements = _countof_inputLayout;
            psoDesc.InputLayout.pInputElementDescs = inputLayout;
            psoDesc.pRootSignature = g_RootSignature; // the root signature that describes the input data this pso needs
            psoDesc.VS.BytecodeLength = (nuint)VS.g_main.Length;
            psoDesc.VS.pShaderBytecode = Unsafe.AsPointer(ref MemoryMarshal.GetReference(VS.g_main));
            psoDesc.PS.BytecodeLength = (nuint)PS.g_main.Length;
            psoDesc.PS.pShaderBytecode = Unsafe.AsPointer(ref MemoryMarshal.GetReference(PS.g_main));
            psoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE; // type of topology we are drawing
            psoDesc.RTVFormats[0] = RENDER_TARGET_FORMAT; // format of the render target
            psoDesc.DSVFormat = DEPTH_STENCIL_FORMAT;
            psoDesc.SampleDesc = sampleDesc; // must be the same sample description as the swapchain and depth/stencil buffer
            psoDesc.SampleMask = 0xffffffff; // sample mask has to do with multi-sampling. 0xffffffff means point sampling is done

            SetDefaultRasterizerDesc(ref psoDesc.RasterizerState);
            SetDefaultBlendDesc(ref psoDesc.BlendState);

            psoDesc.NumRenderTargets = 1; // we are only binding one render target

            SetDefaultDepthStencilDesc(ref psoDesc.DepthStencilState);

            // create the pso
            ID3D12PipelineState* pipelineStateObject;
            CHECK_HR(device->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), (void**)&pipelineStateObject));
            g_PipelineStateObject.Attach(pipelineStateObject);

            // Create vertex buffer

            // a triangle

            const uint _countof_vList = 24;

            Vertex* vList = stackalloc Vertex[(int)_countof_vList];

            // front face
            vList[0]  = new Vertex { pos = new vec3(-0.5f,  0.5f, -0.5f), texCoord = new vec2(0.0f, 0.0f) };
            vList[1]  = new Vertex { pos = new vec3( 0.5f, -0.5f, -0.5f), texCoord = new vec2(1.0f, 1.0f) };
            vList[2]  = new Vertex { pos = new vec3(-0.5f, -0.5f, -0.5f), texCoord = new vec2(0.0f, 1.0f) };
            vList[3]  = new Vertex { pos = new vec3( 0.5f,  0.5f, -0.5f), texCoord = new vec2(1.0f, 0.0f) };

            // right side face
            vList[4]  = new Vertex { pos = new vec3( 0.5f, -0.5f, -0.5f), texCoord = new vec2(0.0f, 1.0f) };
            vList[5]  = new Vertex { pos = new vec3( 0.5f,  0.5f,  0.5f), texCoord = new vec2(1.0f, 0.0f) };
            vList[6]  = new Vertex { pos = new vec3( 0.5f, -0.5f,  0.5f), texCoord = new vec2(1.0f, 1.0f) };
            vList[7]  = new Vertex { pos = new vec3( 0.5f,  0.5f, -0.5f), texCoord = new vec2(0.0f, 0.0f) };

            // left side face
            vList[8]  = new Vertex { pos = new vec3(-0.5f,  0.5f,  0.5f), texCoord = new vec2(0.0f, 0.0f) };
            vList[9]  = new Vertex { pos = new vec3(-0.5f, -0.5f, -0.5f), texCoord = new vec2(1.0f, 1.0f) };
            vList[10] = new Vertex { pos = new vec3(-0.5f, -0.5f,  0.5f), texCoord = new vec2(0.0f, 1.0f) };
            vList[11] = new Vertex { pos = new vec3(-0.5f,  0.5f, -0.5f), texCoord = new vec2(1.0f, 0.0f) };

            // back face
            vList[12] = new Vertex { pos = new vec3( 0.5f,  0.5f,  0.5f), texCoord = new vec2(0.0f, 0.0f) };
            vList[13] = new Vertex { pos = new vec3(-0.5f, -0.5f,  0.5f), texCoord = new vec2(1.0f, 1.0f) };
            vList[14] = new Vertex { pos = new vec3( 0.5f, -0.5f,  0.5f), texCoord = new vec2(0.0f, 1.0f) };
            vList[15] = new Vertex { pos = new vec3(-0.5f,  0.5f,  0.5f), texCoord = new vec2(1.0f, 0.0f) };

            // top face
            vList[16] = new Vertex { pos = new vec3(-0.5f,  0.5f, -0.5f), texCoord = new vec2(0.0f, 0.0f) };
            vList[17] = new Vertex { pos = new vec3( 0.5f,  0.5f,  0.5f), texCoord = new vec2(1.0f, 1.0f) };
            vList[18] = new Vertex { pos = new vec3( 0.5f,  0.5f, -0.5f), texCoord = new vec2(0.0f, 1.0f) };
            vList[19] = new Vertex { pos = new vec3(-0.5f,  0.5f,  0.5f), texCoord = new vec2(1.0f, 0.0f) };

            // bottom face
            vList[20] = new Vertex { pos = new vec3( 0.5f, -0.5f,  0.5f), texCoord = new vec2(0.0f, 0.0f) };
            vList[21] = new Vertex { pos = new vec3(-0.5f, -0.5f, -0.5f), texCoord = new vec2(1.0f, 1.0f) };
            vList[22] = new Vertex { pos = new vec3( 0.5f, -0.5f, -0.5f), texCoord = new vec2(0.0f, 1.0f) };
            vList[23] = new Vertex { pos = new vec3(-0.5f, -0.5f,  0.5f), texCoord = new vec2(1.0f, 0.0f) };

            uint vBufferSize = _countof_vList * (uint)sizeof(Vertex);

            // create default heap
            // default heap is memory on the GPU. Only the GPU has access to this memory
            // To get data into this heap, we will have to upload the data using
            // an upload heap
            D3D12MA_ALLOCATION_DESC vertexBufferAllocDesc = default;
            vertexBufferAllocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

            D3D12_RESOURCE_DESC vertexBufferResourceDesc = default;
            vertexBufferResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            vertexBufferResourceDesc.Alignment = 0;
            vertexBufferResourceDesc.Width = vBufferSize;
            vertexBufferResourceDesc.Height = 1;
            vertexBufferResourceDesc.DepthOrArraySize = 1;
            vertexBufferResourceDesc.MipLevels = 1;
            vertexBufferResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
            vertexBufferResourceDesc.SampleDesc.Count = 1;
            vertexBufferResourceDesc.SampleDesc.Quality = 0;
            vertexBufferResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            vertexBufferResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

            D3D12MA_Allocation* vertexBufferAllocation;
            ID3D12Resource* vertexBufferPtr;
            CHECK_HR(g_Allocator->CreateResource(
                &vertexBufferAllocDesc,
                &vertexBufferResourceDesc, // resource description for a buffer
                D3D12_RESOURCE_STATE_COPY_DEST, // we will start this heap in the copy destination state since we will copy data
                                                // from the upload heap to this heap
                null, // optimized clear value must be null for this type of resource. used for render targets and depth/stencil buffers
                &vertexBufferAllocation,
                __uuidof<ID3D12Resource>(), (void**)&vertexBufferPtr
            ));
            g_VertexBufferAllocation = vertexBufferAllocation;
            g_VertexBuffer.Attach(vertexBufferPtr);

            // we can give resource heaps a name so when we debug with the graphics debugger we know what resource we are looking at
            fixed (char* name = "Vertex Buffer Resource Heap")
            {
                _ = g_VertexBuffer.Get()->SetName((ushort*)name);
            }

            // create upload heap
            // upload heaps are used to upload data to the GPU. CPU can write to it, GPU can read from it
            // We will upload the vertex buffer using this heap to the default heap
            D3D12MA_ALLOCATION_DESC vBufferUploadAllocDesc = default;
            vBufferUploadAllocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            D3D12_RESOURCE_DESC vertexBufferUploadResourceDesc = default;
            vertexBufferUploadResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            vertexBufferUploadResourceDesc.Alignment = 0;
            vertexBufferUploadResourceDesc.Width = vBufferSize;
            vertexBufferUploadResourceDesc.Height = 1;
            vertexBufferUploadResourceDesc.DepthOrArraySize = 1;
            vertexBufferUploadResourceDesc.MipLevels = 1;
            vertexBufferUploadResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
            vertexBufferUploadResourceDesc.SampleDesc.Count = 1;
            vertexBufferUploadResourceDesc.SampleDesc.Quality = 0;
            vertexBufferUploadResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            vertexBufferUploadResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

            using ComPtr<ID3D12Resource> vBufferUploadHeap = default;
            D3D12MA_Allocation* vBufferUploadHeapAllocation = null;
            CHECK_HR(g_Allocator->CreateResource(
                &vBufferUploadAllocDesc,
                &vertexBufferUploadResourceDesc, // resource description for a buffer
                D3D12_RESOURCE_STATE_GENERIC_READ, // GPU will read from this buffer and copy its contents to the default heap
                null,
                &vBufferUploadHeapAllocation,
                __uuidof<ID3D12Resource>(), (void**)&vBufferUploadHeap
            ));

            fixed (char* name = "Vertex Buffer Upload Resource Heap")
            {
                _ = vBufferUploadHeap.Get()->SetName((ushort*)name);
            }

            // store vertex buffer in upload heap
            D3D12_SUBRESOURCE_DATA vertexData = default;
            vertexData.pData = (byte*)vList; // pointer to our vertex array
            vertexData.RowPitch = (nint)vBufferSize; // size of all our triangle vertex data
            vertexData.SlicePitch = (nint)vBufferSize; // also the size of our triangle vertex data

            CHECK_HR(g_CommandList.Get()->Reset(g_CommandAllocators[(int)g_FrameIndex], null));

            // we are now creating a command with the command list to copy the data from
            // the upload heap to the default heap
            ulong r = UpdateSubresources(g_CommandList, g_VertexBuffer, vBufferUploadHeap, 0, 0, 1, &vertexData);
            Debug.Assert(r != 0);

            // transition the vertex buffer data from copy destination state to vertex buffer state
            D3D12_RESOURCE_BARRIER vbBarrier = default;
            vbBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            vbBarrier.Transition.pResource = g_VertexBuffer;
            vbBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
            vbBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
            vbBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
            g_CommandList.Get()->ResourceBarrier(1, &vbBarrier);

            // Create index buffer

            // a quad (2 triangles)
            const uint _countof_iList = 36;

            ushort* iList = stackalloc ushort[(int)_countof_iList] {
                // ffront face
                0, 1, 2, // first triangle
                0, 3, 1, // second triangle

                // left face
                4, 5, 6, // first triangle
                4, 7, 5, // second triangle

                // right face
                8, 9, 10, // first triangle
                8, 11, 9, // second triangle

                // back face
                12, 13, 14, // first triangle
                12, 15, 13, // second triangle

                // top face
                16, 17, 18, // first triangle
                16, 19, 17, // second triangle

                // bottom face
                20, 21, 22, // first triangle
                20, 23, 21, // second triangle
            };

            g_CubeIndexCount = _countof_iList;

            nuint iBufferSize = _countof_iList * sizeof(ushort);

            // create default heap to hold index buffer
            D3D12MA_ALLOCATION_DESC indexBufferAllocDesc = default;
            indexBufferAllocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

            D3D12_RESOURCE_DESC indexBufferResourceDesc = default;
            indexBufferResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            indexBufferResourceDesc.Alignment = 0;
            indexBufferResourceDesc.Width = iBufferSize;
            indexBufferResourceDesc.Height = 1;
            indexBufferResourceDesc.DepthOrArraySize = 1;
            indexBufferResourceDesc.MipLevels = 1;
            indexBufferResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
            indexBufferResourceDesc.SampleDesc.Count = 1;
            indexBufferResourceDesc.SampleDesc.Quality = 0;
            indexBufferResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            indexBufferResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

            D3D12MA_Allocation* indexBufferAllocation;
            ID3D12Resource* indexBuffer;
            CHECK_HR(g_Allocator->CreateResource(
                &indexBufferAllocDesc,
                &indexBufferResourceDesc, // resource description for a buffer
                D3D12_RESOURCE_STATE_COPY_DEST, // start in the copy destination state
                null, // optimized clear value must be null for this type of resource
                &indexBufferAllocation,
                __uuidof<ID3D12Resource>(), (void**)&indexBuffer
            ));
            g_IndexBufferAllocation = indexBufferAllocation;
            g_IndexBuffer = indexBuffer;

            // we can give resource heaps a name so when we debug with the graphics debugger we know what resource we are looking at
            fixed (char* name = "Index Buffer Resource Heap")
            {
                _ = g_IndexBuffer.Get()->SetName((ushort*)name);
            }

            // create upload heap to upload index buffer
            D3D12MA_ALLOCATION_DESC iBufferUploadAllocDesc = default;
            iBufferUploadAllocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            D3D12_RESOURCE_DESC indexBufferUploadResourceDesc = default;
            indexBufferUploadResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            indexBufferUploadResourceDesc.Alignment = 0;
            indexBufferUploadResourceDesc.Width = iBufferSize;
            indexBufferUploadResourceDesc.Height = 1;
            indexBufferUploadResourceDesc.DepthOrArraySize = 1;
            indexBufferUploadResourceDesc.MipLevels = 1;
            indexBufferUploadResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
            indexBufferUploadResourceDesc.SampleDesc.Count = 1;
            indexBufferUploadResourceDesc.SampleDesc.Quality = 0;
            indexBufferUploadResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            indexBufferUploadResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

            using ComPtr<ID3D12Resource> iBufferUploadHeap = default;
            D3D12MA_Allocation* iBufferUploadHeapAllocation = null;
            CHECK_HR(g_Allocator->CreateResource(
                &iBufferUploadAllocDesc,
                &indexBufferUploadResourceDesc, // resource description for a buffer
                D3D12_RESOURCE_STATE_GENERIC_READ, // GPU will read from this buffer and copy its contents to the default heap
                null,
                &iBufferUploadHeapAllocation,
                __uuidof<ID3D12Resource>(), (void**)&iBufferUploadHeap
            ));

            fixed (char* name = "Index Buffer Upload Resource Heap")
            {
                CHECK_HR(iBufferUploadHeap.Get()->SetName((ushort*)name));
            }

            // store vertex buffer in upload heap
            D3D12_SUBRESOURCE_DATA indexData = default;
            indexData.pData = iList; // pointer to our index array
            indexData.RowPitch = (nint)iBufferSize; // size of all our index buffer
            indexData.SlicePitch = (nint)iBufferSize; // also the size of our index buffer

            // we are now creating a command with the command list to copy the data from
            // the upload heap to the default heap
            r = UpdateSubresources(g_CommandList, g_IndexBuffer, iBufferUploadHeap, 0, 0, 1, &indexData);
            Debug.Assert(r != 0);

            // transition the index buffer data from copy destination state to vertex buffer state
            D3D12_RESOURCE_BARRIER ibBarrier = default;
            ibBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            ibBarrier.Transition.pResource = g_IndexBuffer;
            ibBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
            ibBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_INDEX_BUFFER;
            ibBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

            g_CommandList.Get()->ResourceBarrier(1, &ibBarrier);

            // create a vertex buffer view for the triangle. We get the GPU memory address to the vertex pointer using the GetGPUVirtualAddress() method
            g_VertexBufferView->BufferLocation = g_VertexBuffer.Get()->GetGPUVirtualAddress();
            g_VertexBufferView->StrideInBytes = (uint)sizeof(Vertex);
            g_VertexBufferView->SizeInBytes = vBufferSize;

            // create a index buffer view for the triangle. We get the GPU memory address to the vertex pointer using the GetGPUVirtualAddress() method
            g_IndexBufferView->BufferLocation = g_IndexBuffer.Get()->GetGPUVirtualAddress();
            g_IndexBufferView->Format = DXGI_FORMAT_R16_UINT;
            g_IndexBufferView->SizeInBytes = (uint)iBufferSize;

            D3D12MA_ALLOCATION_DESC cbPerObjectUploadAllocDesc = default;
            cbPerObjectUploadAllocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            D3D12_RESOURCE_DESC cbPerObjectUploadResourceDesc = default;
            cbPerObjectUploadResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            cbPerObjectUploadResourceDesc.Alignment = 0;
            cbPerObjectUploadResourceDesc.Width = 1024 * 64;
            cbPerObjectUploadResourceDesc.Height = 1;
            cbPerObjectUploadResourceDesc.DepthOrArraySize = 1;
            cbPerObjectUploadResourceDesc.MipLevels = 1;
            cbPerObjectUploadResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
            cbPerObjectUploadResourceDesc.SampleDesc.Count = 1;
            cbPerObjectUploadResourceDesc.SampleDesc.Quality = 0;
            cbPerObjectUploadResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            cbPerObjectUploadResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

            for (nuint i = 0; i < FRAME_BUFFER_COUNT; ++i)
            {
                // create resource for cube 1
                D3D12MA_Allocation* cbPerObjectUploadHeapAllocations;
                ID3D12Resource* cbPerObjectUploadHeaps;
                CHECK_HR(g_Allocator->CreateResource(
                    &cbPerObjectUploadAllocDesc,
                    &cbPerObjectUploadResourceDesc, // size of the resource heap. Must be a multiple of 64KB for single-textures and constant buffers
                    D3D12_RESOURCE_STATE_GENERIC_READ, // will be data that is read from so we keep it in the generic read state
                    null, // we do not have use an optimized clear value for constant buffers
                    &cbPerObjectUploadHeapAllocations,
                    __uuidof<ID3D12Resource>(), (void**)&cbPerObjectUploadHeaps
                ));
                g_CbPerObjectUploadHeapAllocations[(int)i] = cbPerObjectUploadHeapAllocations;
                g_CbPerObjectUploadHeaps[(int)i].Attach(cbPerObjectUploadHeaps);

                fixed (char* name = "Constant Buffer Upload Resource Heap")
                {
                    _ = g_CbPerObjectUploadHeaps[(int)i].Get()->SetName((ushort*)name);
                }

                void* cbPerObjectAddress;
                CHECK_HR(g_CbPerObjectUploadHeaps[(int)i].Get()->Map(0, EMPTY_RANGE, &cbPerObjectAddress));
                g_CbPerObjectAddress[(int)i] = cbPerObjectAddress;
            }

            // # TEXTURE

            D3D12_RESOURCE_DESC textureDesc;
            nuint imageBytesPerRow;
            nuint imageSize = nuint.MaxValue;
            byte* imageData;
            {
                const uint sizeX = 256;
                const uint sizeY = 256;
                const DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM;
                const uint bytesPerPixel = 4;

                imageBytesPerRow = sizeX * bytesPerPixel;
                imageSize = sizeY * imageBytesPerRow;
                imageData = (byte*)_aligned_malloc(imageSize, 16);

                byte* rowPtr = imageData;

                for (uint y = 0; y < sizeY; ++y)
                {
                    byte* pixelPtr = rowPtr;

                    for (uint x = 0; x < sizeX; ++x)
                    {
                        *(byte*)pixelPtr = (byte)x; // R
                        *(byte*)(pixelPtr + 1) = (byte)y; // G
                        *(byte*)(pixelPtr + 2) = 0x00; // B
                        *(byte*)(pixelPtr + 3) = 0xFF; // A

                        *(byte*)pixelPtr = (byte)(x > 128 ? 0xFF : 00);
                        *(byte*)(pixelPtr + 1) = (byte)(y > 128 ? 0xFF : 00);

                        pixelPtr += bytesPerPixel;
                    }

                    rowPtr += imageBytesPerRow;
                }

                textureDesc = default;
                textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                textureDesc.Alignment = 0;
                textureDesc.Width = sizeX;
                textureDesc.Height = sizeY;
                textureDesc.DepthOrArraySize = 1;
                textureDesc.MipLevels = 1;
                textureDesc.Format = format;
                textureDesc.SampleDesc.Count = 1;
                textureDesc.SampleDesc.Quality = 0;
                textureDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
                textureDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
            }

            D3D12MA_ALLOCATION_DESC textureAllocDesc = default;
            textureAllocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

            D3D12MA_Allocation* textureAllocation;
            ID3D12Resource* texture;
            CHECK_HR(g_Allocator->CreateResource(
                &textureAllocDesc,
                &textureDesc,
                D3D12_RESOURCE_STATE_COPY_DEST,
                null, // pOptimizedClearValue
                &textureAllocation,
                __uuidof<ID3D12Resource>(), (void**)&texture
            ));
            g_TextureAllocation = textureAllocation;
            g_Texture = texture;

            fixed (char* name = "g_Texture")
            {
                _ = g_Texture.Get()->SetName((ushort*)name);
            }

            ulong textureUploadBufferSize;
            device->GetCopyableFootprints(
                &textureDesc,
                0, // FirstSubresource
                1, // NumSubresources
                0, // BaseOffset
                null, // pLayouts
                null, // pNumRows
                null, // pRowSizeInBytes
                &textureUploadBufferSize); // pTotalBytes

            D3D12MA_ALLOCATION_DESC textureUploadAllocDesc = default;
            textureUploadAllocDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

            D3D12_RESOURCE_DESC textureUploadResourceDesc = default;
            textureUploadResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
            textureUploadResourceDesc.Alignment = 0;
            textureUploadResourceDesc.Width = textureUploadBufferSize;
            textureUploadResourceDesc.Height = 1;
            textureUploadResourceDesc.DepthOrArraySize = 1;
            textureUploadResourceDesc.MipLevels = 1;
            textureUploadResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
            textureUploadResourceDesc.SampleDesc.Count = 1;
            textureUploadResourceDesc.SampleDesc.Quality = 0;
            textureUploadResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
            textureUploadResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

            using ComPtr<ID3D12Resource> textureUpload = default;
            D3D12MA_Allocation* textureUploadAllocation;
            CHECK_HR(g_Allocator->CreateResource(
                &textureUploadAllocDesc,
                &textureUploadResourceDesc,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                null, // pOptimizedClearValue
                &textureUploadAllocation,
                __uuidof<ID3D12Resource>(), (void**)&textureUpload
            ));

            fixed (char* name = "textureUpload")
            {
                _ = textureUpload.Get()->SetName((ushort*)name);
            }

            D3D12_SUBRESOURCE_DATA textureSubresourceData = default;
            textureSubresourceData.pData = imageData;
            textureSubresourceData.RowPitch = (nint)imageBytesPerRow;
            textureSubresourceData.SlicePitch = (nint)(imageBytesPerRow * textureDesc.Height);

            _ = UpdateSubresources(g_CommandList, g_Texture, textureUpload, 0, 0, 1, &textureSubresourceData);

            D3D12_RESOURCE_BARRIER textureBarrier = default;
            textureBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            textureBarrier.Transition.pResource = g_Texture;
            textureBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
            textureBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
            textureBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

            g_CommandList.Get()->ResourceBarrier(1, &textureBarrier);

            D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = default;
            srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
            srvDesc.Format = textureDesc.Format;
            srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
            srvDesc.Texture2D.MipLevels = 1;

            for (nuint i = 0; i < FRAME_BUFFER_COUNT; ++i)
            {
                D3D12_CPU_DESCRIPTOR_HANDLE descHandle = new D3D12_CPU_DESCRIPTOR_HANDLE {
                    ptr = g_MainDescriptorHeap[(int)i].Get()->GetCPUDescriptorHandleForHeapStart().ptr + g_Device.Get()->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV)
                };

                g_Device.Get()->CreateShaderResourceView(g_Texture, &srvDesc, descHandle);
            }

            // # END OF INITIAL COMMAND LIST

            // Now we execute the command list to upload the initial assets (triangle data)
            _ = g_CommandList.Get()->Close();

            const uint _countof_ppCommandLists = 1;

            ID3D12CommandList** ppCommandLists = stackalloc ID3D12CommandList*[(int)_countof_ppCommandLists] { (ID3D12CommandList*)g_CommandList.Get() };
            commandQueue->ExecuteCommandLists(_countof_ppCommandLists, ppCommandLists);

            // increment the fence value now, otherwise the buffer might not be uploaded by the time we start drawing
            WaitGPUIdle(g_FrameIndex);

            _ = textureUploadAllocation->Release();
            _ = iBufferUploadHeapAllocation->Release();
            _ = vBufferUploadHeapAllocation->Release();
        }

        private static void Update()
        {
            {
                float f = MathF.Sin(g_Time * (PI * 2.0f)) * 0.5f + 0.5f;
                ConstantBuffer0_PS cb;
                cb.Color = new vec4(f, f, f, 1.0f);
                _ = memcpy(g_ConstantBufferAddress[(int)g_FrameIndex], &cb, (nuint)sizeof(ConstantBuffer0_PS));
            }

            {
                mat4 projection = mat4.Perspective(
                    45.0f * (PI / 180.0f), // fovY
                    (float)SIZE_X / (float)SIZE_Y, // aspectRatio
                    0.1f, // zNear
                    1000.0f
                ); // zFar

                mat4 view = mat4.LookAt(
                    new vec3(0.0f, 0.0f, 0.0f), // at
                    new vec3(-.4f, 1.7f, -3.5f), // eye
                    new vec3(0.0f, 1.0f, 0.0f)
                ); // up

                mat4 viewProjection = view * projection;

                mat4 cube1World = mat4.RotationZ(g_Time);

                ConstantBuffer1_VS cb;
                mat4 worldViewProjection = cube1World * viewProjection;
                cb.WorldViewProj = worldViewProjection.Transposed();
                _ = memcpy(g_CbPerObjectAddress[(int)g_FrameIndex], &cb, (nuint)sizeof(ConstantBuffer1_VS));

                mat4 cube2World = mat4.Scaling(0.5f) * mat4.RotationX(g_Time * 2.0f) * mat4.Translation(new vec3(-1.2f, 0.0f, 0.0f)) * cube1World;

                worldViewProjection = cube2World * viewProjection;
                cb.WorldViewProj = worldViewProjection.Transposed();
                _ = memcpy((byte*)g_CbPerObjectAddress[(int)g_FrameIndex] + ConstantBufferPerObjectAlignedSize, &cb, (nuint)sizeof(ConstantBuffer1_VS));
            }
        }

        private static void Render() // execute the command list
        {
            // # Here was UpdatePipeline function.

            // swap the current rtv buffer index so we draw on the correct buffer
            g_FrameIndex = g_SwapChain.Get()->GetCurrentBackBufferIndex();

            // We have to wait for the gpu to finish with the command allocator before we reset it
            WaitForFrame(g_FrameIndex);

            // increment g_FenceValues for next frame
            g_FenceValues[(int)g_FrameIndex]++;

            // we can only reset an allocator once the gpu is done with it
            // resetting an allocator frees the memory that the command list was stored in
            CHECK_HR(g_CommandAllocators[(int)g_FrameIndex].Get()->Reset());

            // reset the command list. by resetting the command list we are putting it into
            // a recording state so we can start recording commands into the command allocator.
            // the command allocator that we reference here may have multiple command lists
            // associated with it, but only one can be recording at any time. Make sure
            // that any other command lists associated to this command allocator are in
            // the closed state (not recording).
            // Here you will pass an initial pipeline state object as the second parameter,
            // but in this tutorial we are only clearing the rtv, and do not actually need
            // anything but an initial default pipeline, which is what we get by setting
            // the second parameter to NULL
            CHECK_HR(g_CommandList.Get()->Reset(g_CommandAllocators[(int)g_FrameIndex], null));

            // here we start recording commands into the g_CommandList (which all the commands will be stored in the g_CommandAllocators)

            // transition the "g_FrameIndex" render target from the present state to the render target state so the command list draws to it starting from here
            D3D12_RESOURCE_BARRIER presentToRenderTargetBarrier = default;
            presentToRenderTargetBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            presentToRenderTargetBarrier.Transition.pResource = g_RenderTargets[(int)g_FrameIndex];
            presentToRenderTargetBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_PRESENT;
            presentToRenderTargetBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_RENDER_TARGET;
            presentToRenderTargetBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

            g_CommandList.Get()->ResourceBarrier(1, &presentToRenderTargetBarrier);

            // here we again get the handle to our current render target view so we can set it as the render target in the output merger stage of the pipeline
            D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = new D3D12_CPU_DESCRIPTOR_HANDLE {
                ptr = g_RtvDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart().ptr + g_FrameIndex * g_RtvDescriptorSize
            };

            D3D12_CPU_DESCRIPTOR_HANDLE dsvHandle = g_DepthStencilDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart();

            // set the render target for the output merger stage (the output of the pipeline)
            g_CommandList.Get()->OMSetRenderTargets(1, &rtvHandle, FALSE, &dsvHandle);

            g_CommandList.Get()->ClearDepthStencilView(g_DepthStencilDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart(), D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0, 0, null);

            // Clear the render target by using the ClearRenderTargetView command
            float* clearColor = stackalloc float[4] { 0.0f, 0.2f, 0.4f, 1.0f };
            g_CommandList.Get()->ClearRenderTargetView(rtvHandle, clearColor, 0, null);

            g_CommandList.Get()->SetPipelineState(g_PipelineStateObject);

            g_CommandList.Get()->SetGraphicsRootSignature(g_RootSignature);

            const uint _countof_descriptorHeaps = 1;

            ID3D12DescriptorHeap** descriptorHeaps = stackalloc ID3D12DescriptorHeap*[(int)_countof_descriptorHeaps] { g_MainDescriptorHeap[(int)g_FrameIndex] };
            g_CommandList.Get()->SetDescriptorHeaps(_countof_descriptorHeaps, descriptorHeaps);

            g_CommandList.Get()->SetGraphicsRootDescriptorTable(0, g_MainDescriptorHeap[(int)g_FrameIndex].Get()->GetGPUDescriptorHandleForHeapStart());
            g_CommandList.Get()->SetGraphicsRootDescriptorTable(2, g_MainDescriptorHeap[(int)g_FrameIndex].Get()->GetGPUDescriptorHandleForHeapStart());

            D3D12_VIEWPORT viewport = new D3D12_VIEWPORT {
                TopLeftX = 0.0f,
                TopLeftY = 0.0f,
                Width = (float)SIZE_X,
                Height = (float)SIZE_Y,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            };

            g_CommandList.Get()->RSSetViewports(1, &viewport); // set the viewports

            RECT scissorRect = new RECT {
                left = 0,
                top = 0,
                right = SIZE_X,
                bottom = SIZE_Y
            };

            g_CommandList.Get()->RSSetScissorRects(1, &scissorRect); // set the scissor rects

            g_CommandList.Get()->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST); // set the primitive topology
            g_CommandList.Get()->IASetVertexBuffers(0, 1, g_VertexBufferView); // set the vertex buffer (using the vertex buffer view)
            g_CommandList.Get()->IASetIndexBuffer(g_IndexBufferView);

            g_CommandList.Get()->SetGraphicsRootConstantBufferView(1, g_CbPerObjectUploadHeaps[(int)g_FrameIndex].Get()->GetGPUVirtualAddress());
            g_CommandList.Get()->DrawIndexedInstanced(g_CubeIndexCount, 1, 0, 0, 0);

            g_CommandList.Get()->SetGraphicsRootConstantBufferView(1, g_CbPerObjectUploadHeaps[(int)g_FrameIndex].Get()->GetGPUVirtualAddress() + ConstantBufferPerObjectAlignedSize);
            g_CommandList.Get()->DrawIndexedInstanced(g_CubeIndexCount, 1, 0, 0, 0);

            // transition the "g_FrameIndex" render target from the render target state to the present state. If the debug layer is enabled, you will receive a
            // warning if present is called on the render target when it's not in the present state
            D3D12_RESOURCE_BARRIER renderTargetToPresentBarrier = default;
            renderTargetToPresentBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            renderTargetToPresentBarrier.Transition.pResource = g_RenderTargets[(int)g_FrameIndex];
            renderTargetToPresentBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET;
            renderTargetToPresentBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_PRESENT;
            renderTargetToPresentBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

            g_CommandList.Get()->ResourceBarrier(1, &renderTargetToPresentBarrier);

            CHECK_HR(g_CommandList.Get()->Close());

            // ================

            // create an array of command lists (only one command list here)
            const uint _countof_ppCommandLists = 1;

            ID3D12CommandList** ppCommandLists = stackalloc ID3D12CommandList*[(int)_countof_ppCommandLists] { (ID3D12CommandList*)g_CommandList.Get() };

            // execute the array of command lists
            g_CommandQueue.Get()->ExecuteCommandLists(_countof_ppCommandLists, ppCommandLists);

            // this command goes in at the end of our command queue. we will know when our command queue 
            // has finished because the g_Fences value will be set to "g_FenceValues" from the GPU since the command
            // queue is being executed on the GPU
            CHECK_HR(g_CommandQueue.Get()->Signal(g_Fences[(int)g_FrameIndex], g_FenceValues[(int)g_FrameIndex]));

            // present the current backbuffer
            CHECK_HR(g_SwapChain.Get()->Present(PRESENT_SYNC_INTERVAL, 0));
        }

        private static void Cleanup() // release com ojects and clean up memory
        {
            // wait for the gpu to finish all frames
            for (nuint i = 0; i < FRAME_BUFFER_COUNT; ++i)
            {
                WaitForFrame(i);
                CHECK_HR(g_CommandQueue.Get()->Wait(g_Fences[(int)i], g_FenceValues[(int)i]));
            }

            // get swapchain out of full screen before exiting
            BOOL fs = FALSE;
            CHECK_HR(g_SwapChain.Get()->GetFullscreenState(&fs, null));

            if (fs != 0)
            {
                _ = g_SwapChain.Get()->SetFullscreenState(FALSE, null);
            }

            WaitGPUIdle(0);

            _ = g_Texture.Get()->Release();
            _ = g_TextureAllocation->Release();
            g_TextureAllocation = null;
            _ = g_IndexBuffer.Get()->Release();
            _ = g_IndexBufferAllocation->Release();
            g_IndexBufferAllocation = null;
            _ = g_VertexBuffer.Get()->Release();
            _ = g_VertexBufferAllocation->Release();
            g_VertexBufferAllocation = null;
            _ = g_PipelineStateObject.Get()->Release();
            _ = g_RootSignature.Get()->Release();

            _ = CloseHandle(g_FenceEvent);
            _ = g_CommandList.Get()->Release();
            _ = g_CommandQueue.Get()->Release();

            for (nuint i = FRAME_BUFFER_COUNT; unchecked(i-- != 0);)
            {
                _ = g_CbPerObjectUploadHeaps[(int)i].Get()->Release();
                _ = g_CbPerObjectUploadHeapAllocations[(int)i].Value->Release();
                g_CbPerObjectUploadHeapAllocations[(int)i] = null;
                _ = g_MainDescriptorHeap[(int)i].Get()->Release();
                _ = g_ConstantBufferUploadHeap[(int)i].Get()->Release();
                _ = g_ConstantBufferUploadAllocation[(int)i].Value->Release();
                g_ConstantBufferUploadAllocation[(int)i] = null;
            }

            _ = g_DepthStencilDescriptorHeap.Get()->Release();
            _ = g_DepthStencilBuffer.Get()->Release();
            _ = g_DepthStencilAllocation->Release();
            g_DepthStencilAllocation = null;
            _ = g_RtvDescriptorHeap.Get()->Release();

            for (nuint i = FRAME_BUFFER_COUNT; unchecked(i-- != 0);)
            {
                _ = g_RenderTargets[(int)i].Get()->Release();
                _ = g_CommandAllocators[(int)i].Get()->Release();
                _ = g_Fences[(int)i].Get()->Release();
            }

            _ = g_Allocator->Release();
            g_Allocator = null;

            if (ENABLE_CPU_ALLOCATION_CALLBACKS)
            {
                Debug.Assert(g_CpuAllocationCount == 0);
            }

            _ = g_Device.Get()->Release();
            _ = g_SwapChain.Get()->Release();
        }

        private static void ExecuteTests()
        {
            try
            {
                TestContext ctx = default;
                ctx.allocationCallbacks = g_AllocationCallbacks;
                ctx.device = g_Device;
                ctx.allocator = g_Allocator;
                ctx.allocatorFlags = g_AllocatorFlags;

                Test(in ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}s");
            }
        }

        private static void OnKeyDown([NativeTypeName("WPARAM")] nuint key)
        {
            switch (key)
            {
                case 'T':
                {
                    ExecuteTests();
                    break;
                }

                case 'J':
                {
                    ushort* statsString = null;
                    g_Allocator->BuildStatsString(&statsString, TRUE);

                    Console.WriteLine(Marshal.PtrToStringUni((IntPtr)statsString));
                    g_Allocator->FreeStatsString(statsString);
                }
                break;

                case VK_ESCAPE:
                {
                    _ = PostMessage(g_Wnd, WM_CLOSE, (WPARAM)0, 0);
                    break;
                }
            }
        }

        [UnmanagedCallersOnly]
        [return: NativeTypeName("LRESULT")]
        private static nint WndProc([NativeTypeName("HWND")] IntPtr wnd, [NativeTypeName("UINT")] uint msg, [NativeTypeName("WPARAM")] nuint wParam, [NativeTypeName("LPARAM")] nint lParam)
        {
            switch (msg)
            {
                case WM_CREATE:
                {
                    g_Wnd = (HWND)wnd;
                    InitD3D();
                    g_TimeOffset = GetTickCount64();
                    return 0;
                }

                case WM_DESTROY:
                {
                    Cleanup();
                    PostQuitMessage(0);
                    return 0;
                }

                case WM_KEYDOWN:
                {
                    OnKeyDown(wParam);
                    return 0;
                }
            }

            return DefWindowProc((HWND)wnd, msg, wParam, lParam);
        }

        private static ID3D12GraphicsCommandList* BeginCommandList()
        {
            CHECK_HR(g_CommandList.Get()->Reset(g_CommandAllocators[(int)g_FrameIndex], null));

            return g_CommandList;
        }

        private static void EndCommandList(ID3D12GraphicsCommandList* cmdList)
        {
            _ = cmdList->Close();

            ID3D12CommandList* genericCmdList = (ID3D12CommandList*)cmdList;
            g_CommandQueue.Get()->ExecuteCommandLists(1, &genericCmdList);

            WaitGPUIdle(g_FrameIndex);
        }

        private struct _e__FixedBuffer<T>
            where T : unmanaged
        {
#pragma warning disable CS0649
            public T e0;
            public T e1;
            public T e2;
#pragma warning restore CS0649

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return ref AsSpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan()
            {
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((sizeof(_e__FixedBuffer<T>) / sizeof(T)) == (int)FRAME_BUFFER_COUNT) && ((sizeof(_e__FixedBuffer<T>) % sizeof(T)) == 0));

                return MemoryMarshal.CreateSpan(ref e0, (int)FRAME_BUFFER_COUNT);
            }
        }
    }
}
