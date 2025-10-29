// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.DirectX.D3D_PRIMITIVE_TOPOLOGY;
using static TerraFX.Interop.DirectX.D3D_ROOT_SIGNATURE_VERSION;
using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.D3D12_BLEND;
using static TerraFX.Interop.DirectX.D3D12_BLEND_OP;
using static TerraFX.Interop.DirectX.D3D12_CLEAR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_COLOR_WRITE_ENABLE;
using static TerraFX.Interop.DirectX.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.DirectX.D3D12_COMPARISON_FUNC;
using static TerraFX.Interop.DirectX.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
using static TerraFX.Interop.DirectX.D3D12_CULL_MODE;
using static TerraFX.Interop.DirectX.D3D12_DEPTH_WRITE_MASK;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_RANGE_TYPE;
using static TerraFX.Interop.DirectX.D3D12_DSV_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_DSV_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_FEATURE;
using static TerraFX.Interop.DirectX.D3D12_FENCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_FILL_MODE;
using static TerraFX.Interop.DirectX.D3D12_FILTER;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_INPUT_CLASSIFICATION;
using static TerraFX.Interop.DirectX.D3D12_LOGIC_OP;
using static TerraFX.Interop.DirectX.D3D12_PRIMITIVE_TOPOLOGY_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_BARRIER_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_HEAP_TIER;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_STATES;
using static TerraFX.Interop.DirectX.D3D12_ROOT_PARAMETER_TYPE;
using static TerraFX.Interop.DirectX.D3D12_ROOT_SIGNATURE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_SHADER_VISIBILITY;
using static TerraFX.Interop.DirectX.D3D12_SRV_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_STATIC_BORDER_COLOR;
using static TerraFX.Interop.DirectX.D3D12_STENCIL_OP;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_ADDRESS_MODE;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12MA_ALLOCATOR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;
using static TerraFX.Interop.DirectX.DirectX;
using static TerraFX.Interop.DirectX.DXGI;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;
using static TerraFX.Interop.DirectX.DXGI_SWAP_EFFECT;
using static TerraFX.Interop.Windows.CS;
using static TerraFX.Interop.Windows.IDC;
using static TerraFX.Interop.Windows.IDI;
using static TerraFX.Interop.Windows.PM;
using static TerraFX.Interop.Windows.VK;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WM;
using static TerraFX.Interop.Windows.WS;

namespace TerraFX.Interop.DirectX.UnitTests;

public static unsafe partial class D3D12MemAllocTests
{
    [NativeTypeName("const wchar_t *")]
    internal const string CLASS_NAME = "D3D12MemAllocSample";

    [NativeTypeName("const wchar_t *")]
    internal const string WINDOW_TITLE = "D3D12 Memory Allocator Sample";

    internal const int SIZE_X = 1024;

    internal const int SIZE_Y = 576;

    internal const bool FULLSCREEN = false;

    [NativeTypeName("UINT")]
    internal const uint PRESENT_SYNC_INTERVAL = 1;

    internal const DXGI_FORMAT RENDER_TARGET_FORMAT = DXGI_FORMAT_R8G8B8A8_UNORM;

    internal const DXGI_FORMAT DEPTH_STENCIL_FORMAT = DXGI_FORMAT_D32_FLOAT;

    // number of buffers we want, 2 for double buffering, 3 for triple buffering
    [NativeTypeName("size_t")]
    internal const nuint FRAME_BUFFER_COUNT = 3;

    internal const D3D_FEATURE_LEVEL MY_D3D_FEATURE_LEVEL = D3D_FEATURE_LEVEL_12_0;

#pragma warning disable CA1802,CA1805
    internal static readonly bool ENABLE_DEBUG_LAYER = true;

    internal static readonly bool ENABLE_CPU_ALLOCATION_CALLBACKS = true;

    internal static readonly bool ENABLE_CPU_ALLOCATION_CALLBACKS_PRINT = false;
#pragma warning restore CA1802,CA1805

    internal const D3D12MA_ALLOCATOR_FLAGS g_AllocatorFlags = D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED;

    // Used only when ENABLE_CPU_ALLOCATION_CALLBACKS
    internal static D3D12MA_ALLOCATION_CALLBACKS* g_AllocationCallbacks = (D3D12MA_ALLOCATION_CALLBACKS*)(RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(D3D12MemAllocTests), sizeof(D3D12MA_ALLOCATION_CALLBACKS)));

    internal static HINSTANCE g_Instance;

    internal static HWND g_Wnd;

    // In ms.
    [NativeTypeName("UINT64")]
    internal static ulong g_TimeOffset;

    // Time since g_TimeOffset, in ms.
    [NativeTypeName("UINT64")]
    internal static ulong g_TimeValue;

    // g_TimeValue converted to float, in seconds.
    internal static float g_Time;

    internal static float g_TimeDelta;

    internal static DXGIUsage* g_DXGIUsage;

    internal static ComPtr<ID3D12Device> g_Device;

    internal static DXGI_ADAPTER_DESC1 g_AdapterDesc;

    internal static ComPtr<D3D12MA_Allocator> g_Allocator;

    // swapchain used to switch between render targets
    internal static ComPtr<IDXGISwapChain3> g_SwapChain;

    // container for command lists
    internal static ComPtr<ID3D12CommandQueue> g_CommandQueue;

    // a descriptor heap to hold resources like the render targets
    internal static ComPtr<ID3D12DescriptorHeap> g_RtvDescriptorHeap;

    // number of render targets equal to buffer count
    [NativeTypeName("ComPtr<ID3D12Resource>[FRAME_BUFFER_COUNT]")]
    internal static _g_RenderTargets_e__FixedBuffer g_RenderTargets;

    // we want enough allocators for each buffer * number of threads (we only have one thread)
    [NativeTypeName("ComPtr<ID3D12CommandAllocator>[FRAME_BUFFER_COUNT]")]
    internal static _g_CommandAllocators_e__FixedBuffer g_CommandAllocators;

    // a command list we can record commands into, then execute them to render the frame
    internal static ComPtr<ID3D12GraphicsCommandList> g_CommandList;

    // an object that is locked while our command list is being executed by the gpu. We need as many
    [NativeTypeName("ComPtr<ID3D12Fence>[FRAME_BUFFER_COUNT]")]
    internal static _g_Fences_e__FixedBuffer g_Fences;

    // a handle to an event when our g_Fences is unlocked by the gpu
    internal static HANDLE g_FenceEvent;

    // this value is incremented each frame. each g_Fences will have its own value
    [NativeTypeName("UINT64[FRAME_BUFFER_COUNT]")]
    internal static _g_FenceValues_e__FixedBuffer g_FenceValues;

    // current rtv we are on
    [NativeTypeName("UINT")]
    internal static uint g_FrameIndex;

    // size of the rtv descriptor on the g_Device (all front and back buffers will be the same size)
    [NativeTypeName("UINT")]
    internal static uint g_RtvDescriptorSize;

    internal static ComPtr<ID3D12PipelineState> g_PipelineStateObject;

    internal static ComPtr<ID3D12RootSignature> g_RootSignature;

    internal static ComPtr<ID3D12Resource> g_VertexBuffer;

    internal static D3D12MA_Allocation* g_VertexBufferAllocation;

    internal static ComPtr<ID3D12Resource> g_IndexBuffer;

    internal static D3D12MA_Allocation* g_IndexBufferAllocation;

    internal static D3D12_VERTEX_BUFFER_VIEW g_VertexBufferView;

    internal static D3D12_INDEX_BUFFER_VIEW g_IndexBufferView;

    internal static ComPtr<ID3D12Resource> g_DepthStencilBuffer;

    internal static D3D12MA_Allocation* g_DepthStencilAllocation;

    internal static ComPtr<ID3D12DescriptorHeap> g_DepthStencilDescriptorHeap;

    [NativeTypeName("size_t")]
    internal static readonly nuint ConstantBufferPerObjectAlignedSize = AlignUp(__sizeof<ConstantBuffer1_VS>(), 256);

    [NativeTypeName("D3D12MA::Allocation *[FRAME_BUFFER_COUNT]")]
    internal static _g_CbPerObjectUploadHeapAllocations_e__FixedBuffer g_CbPerObjectUploadHeapAllocations;

    [NativeTypeName("ComPtr<ID3D12Resource>[FRAME_BUFFER_COUNT]")]
    internal static _g_CbPerObjectUploadHeaps_e__FixedBuffer g_CbPerObjectUploadHeaps;

    [NativeTypeName("void *[FRAME_BUFFER_COUNT]")]
    internal static _g_CbPerObjectAddress_e__FixedBuffer g_CbPerObjectAddress;

    [NativeTypeName("uint32_t")]
    internal static uint g_CubeIndexCount;

    [NativeTypeName("ComPtr<ID3D12DescriptorHeap>[FRAME_BUFFER_COUNT]")]
    internal static _g_MainDescriptorHeap_e__FixedBuffer g_MainDescriptorHeap;

    [NativeTypeName("ComPtr<ID3D12Resource>[FRAME_BUFFER_COUNT]")]
    internal static _g_ConstantBufferUploadHeap_e__FixedBuffer g_ConstantBufferUploadHeap;

    [NativeTypeName("D3D12MA_Allocation *[FRAME_BUFFER_COUNT]")]
    internal static _g_ConstantBufferUploadAllocation_e__FixedBuffer g_ConstantBufferUploadAllocation;

    [NativeTypeName("void *[FRAME_BUFFER_COUNT]")]
    internal static _g_ConstantBufferAddress_e__FixedBuffer g_ConstantBufferAddress;

    internal static ComPtr<ID3D12Resource> g_Texture;

    internal static D3D12MA_Allocation* g_TextureAllocation;

    internal static readonly void* CUSTOM_ALLOCATION_PRIVATE_DATA = (void*)(nuint)(0xDEADC0DE);

    [NativeTypeName("std::atomic<size_t>")]
    internal static nuint g_CpuAllocationCount;

    internal static CommandLineParameters g_CommandLineParameters;

    [UnmanagedCallersOnly]
    internal static void* CustomAllocate([NativeTypeName("size_t")] nuint Size, [NativeTypeName("size_t")] nuint Alignment, void* pPrivateData)
    {
        Debug.Assert(pPrivateData == CUSTOM_ALLOCATION_PRIVATE_DATA);
        void* memory = NativeMemory.AlignedAlloc(Size, Alignment);

        if (ENABLE_CPU_ALLOCATION_CALLBACKS_PRINT)
        {
            _ = wprintf("Allocate Size={0} Alignment={1} -> {2}\n", Size, Alignment, (nuint)(memory));
        }

        if (Environment.Is64BitProcess)
        {
            _ = Interlocked.Increment(ref Unsafe.As<nuint, ulong>(ref g_CpuAllocationCount));
        }
        else
        {
            _ = Interlocked.Increment(ref Unsafe.As<nuint, uint>(ref g_CpuAllocationCount));
        }
        return memory;
    }

    [UnmanagedCallersOnly]
    internal static void CustomFree(void* pMemory, void* pPrivateData)
    {
        Debug.Assert(pPrivateData == CUSTOM_ALLOCATION_PRIVATE_DATA);

        if (pMemory != null)
        {
            if (Environment.Is64BitProcess)
            {
                _ = Interlocked.Decrement(ref Unsafe.As<nuint, ulong>(ref g_CpuAllocationCount));
            }
            else
            {
                _ = Interlocked.Decrement(ref Unsafe.As<nuint, uint>(ref g_CpuAllocationCount));
            }

            if (ENABLE_CPU_ALLOCATION_CALLBACKS_PRINT)
            {
                _ = wprintf("Free {0}\n", (nuint)(pMemory));
            }

            NativeMemory.AlignedFree(pMemory);
        }
    }

    internal static void SetDefaultRasterizerDesc([NativeTypeName("D3D12_RASTERIZER_DESC &")] out D3D12_RASTERIZER_DESC outDesc)
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

    internal static void SetDefaultBlendDesc([NativeTypeName("D3D12_BLEND_DESC &")] out D3D12_BLEND_DESC outDesc)
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
            RenderTargetWriteMask = (byte)(D3D12_COLOR_WRITE_ENABLE_ALL),
        };
        Unsafe.SkipInit(out outDesc.RenderTarget);

        for (uint i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; ++i)
        {
            outDesc.RenderTarget[(int)(i)] = defaultRenderTargetBlendDesc;
        }
    }

    internal static void SetDefaultDepthStencilDesc([NativeTypeName("D3D12_DEPTH_STENCIL_DESC &")] out D3D12_DEPTH_STENCIL_DESC outDesc)
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
            StencilFunc = D3D12_COMPARISON_FUNC_ALWAYS,
        };

        outDesc.FrontFace = defaultStencilOp;
        outDesc.BackFace = defaultStencilOp;
    }

    internal static void WaitForFrame([NativeTypeName("size_t")] nuint frameIndex) // wait until gpu is finished with command list
    {
        // if the current g_Fences value is still less than "g_FenceValues", then we know the GPU has not finished executing
        // the command queue since it has not reached the "g_CommandQueue->Signal(g_Fences, g_FenceValues)" command

        if (g_Fences[(int)(frameIndex)].Get()->GetCompletedValue() < g_FenceValues[(int)(frameIndex)])
        {
            // we have the g_Fences create an event which is signaled once the g_Fences's current value is "g_FenceValues"
            CHECK_HR(g_Fences[(int)(frameIndex)].Get()->SetEventOnCompletion(g_FenceValues[(int)(frameIndex)], g_FenceEvent));

            // We will wait until the g_Fences has triggered the event that it's current value has reached "g_FenceValues". once it's value
            // has reached "g_FenceValues", we know the command queue has finished executing
            _ = WaitForSingleObject(g_FenceEvent, INFINITE);
        }
    }

    internal static void WaitGPUIdle([NativeTypeName("size_t")] nuint frameIndex)
    {
        g_FenceValues[(int)(frameIndex)]++;
        CHECK_HR(g_CommandQueue.Get()->Signal(g_Fences[(int)(frameIndex)].Get(), g_FenceValues[(int)(frameIndex)]));
        WaitForFrame(frameIndex);
    }

    [return: NativeTypeName("const wchar_t *")]
    internal static string VendorIDToStr([NativeTypeName("uint32_t")] uint vendorID)
    {
        return vendorID switch {
            0x10001 => "VIV",
            0x10002 => "VSI",
            0x10003 => "KAZAN",
            0x10004 => "CODEPLAY",
            0x10005 => "MESA",
            0x10006 => "POCL",
            VENDOR_ID_AMD => "AMD",
            VENDOR_ID_NVIDIA => "NVIDIA",
            VENDOR_ID_INTEL => "Intel",
            0x1010 => "ImgTec",
            0x13B5 => "ARM",
            0x5143 => "Qualcomm",
            _ => "",
        };
    }

    [return: NativeTypeName("std::wstring")]
    internal static string SizeToStr([NativeTypeName("size_t")] nuint size)
    {
        if (size == 0)
        {
            return "0";
        }

        string result;
        double size2 = (double)(size);

        if (size2 >= 1024.0 * 1024.0 * 1024.0 * 1024.0)
        {
            _ = swprintf_s(out result, "{0:F2} TB", size2 / (1024.0 * 1024.0 * 1024.0 * 1024.0));
        }
        else if (size2 >= 1024.0 * 1024.0 * 1024.0)
        {
            _ = swprintf_s(out result, "{0:F2} GB", size2 / (1024.0 * 1024.0 * 1024.0));
        }
        else if (size2 >= 1024.0 * 1024.0)
        {
            _ = swprintf_s(out result, "{0:F2} MB", size2 / (1024.0 * 1024.0));
        }
        else if (size2 >= 1024.0)
        {
            _ = swprintf_s(out result, "{0:F2} KB", size2 / 1024.0);
        }
        else
        {
            _ = swprintf_s(out result, "{0} B", size);
        }

        return result;
    }

    internal static void PrintAdapterInformation(IDXGIAdapter1* adapter)
    {
        _ = wprintf("DXGI_ADAPTER_DESC1:\n");
        _ = wprintf("    Description = {0}\n", ((ReadOnlySpan<char>)(g_AdapterDesc.Description)).ToString());
        _ = wprintf("    VendorId = 0x{0:X} ({1})\n", g_AdapterDesc.VendorId, VendorIDToStr(g_AdapterDesc.VendorId));
        _ = wprintf("    DeviceId = 0x{0:X}\n", g_AdapterDesc.DeviceId);
        _ = wprintf("    SubSysId = 0x{0:X}\n", g_AdapterDesc.SubSysId);
        _ = wprintf("    Revision = 0x{0:X}\n", g_AdapterDesc.Revision);
        _ = wprintf("    DedicatedVideoMemory = {0} B ({1})\n", g_AdapterDesc.DedicatedVideoMemory, SizeToStr(g_AdapterDesc.DedicatedVideoMemory));
        _ = wprintf("    DedicatedSystemMemory = {0} B ({1})\n", g_AdapterDesc.DedicatedSystemMemory, SizeToStr(g_AdapterDesc.DedicatedSystemMemory));
        _ = wprintf("    SharedSystemMemory = {0} B ({1})\n", g_AdapterDesc.SharedSystemMemory, SizeToStr(g_AdapterDesc.SharedSystemMemory));

        ref readonly D3D12_FEATURE_DATA_D3D12_OPTIONS options = ref *g_Allocator.Get()->GetD3D12Options();

        _ = wprintf("D3D12_FEATURE_DATA_D3D12_OPTIONS:\n");
        _ = wprintf("    StandardSwizzle64KBSupported = {0}\n", options.StandardSwizzle64KBSupported ? 1 : 0);
        _ = wprintf("    CrossAdapterRowMajorTextureSupported = {0}\n", options.CrossAdapterRowMajorTextureSupported ? 1 : 0);

        switch (options.ResourceHeapTier)
        {
            case D3D12_RESOURCE_HEAP_TIER_1:
            {
                _ = wprintf("    ResourceHeapTier = D3D12_RESOURCE_HEAP_TIER_1\n");
                break;
            }

            case D3D12_RESOURCE_HEAP_TIER_2:
            {
                _ = wprintf("    ResourceHeapTier = D3D12_RESOURCE_HEAP_TIER_2\n");
                break;
            }

            default:
            {
                Debug.Fail("");
                break;
            }
        }

        using ComPtr<IDXGIAdapter3> adapter3 = new ComPtr<IDXGIAdapter3>();

        if (SUCCEEDED(adapter->QueryInterface(__uuidof<IDXGIAdapter3>(), (void**)(&adapter3))))
        {
            _ = wprintf("DXGI_QUERY_VIDEO_MEMORY_INFO:\n");

            for (uint groupIndex = 0; groupIndex < 2; ++groupIndex)
            {
                DXGI_MEMORY_SEGMENT_GROUP group = (groupIndex == 0) ? DXGI_MEMORY_SEGMENT_GROUP_LOCAL : DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL;
                string groupName = (groupIndex == 0) ? "DXGI_MEMORY_SEGMENT_GROUP_LOCAL" : "DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL";

                DXGI_QUERY_VIDEO_MEMORY_INFO info = new DXGI_QUERY_VIDEO_MEMORY_INFO();
                CHECK_HR(adapter3.Get()->QueryVideoMemoryInfo(0, group, &info));

                _ = wprintf("    {0}:\n", groupName);
                _ = wprintf("        Budget = {0} B ({1})\n", info.Budget, SizeToStr((nuint)(info.Budget)));
                _ = wprintf("        CurrentUsage = {0} B ({1})\n", info.CurrentUsage, SizeToStr((nuint)(info.CurrentUsage)));
                _ = wprintf("        AvailableForReservation = {0} B ({1})\n", info.AvailableForReservation, SizeToStr((nuint)(info.AvailableForReservation)));
                _ = wprintf("        CurrentReservation = {0} B ({1})\n", info.CurrentReservation, SizeToStr((nuint)(info.CurrentReservation)));
            }
        }

        Debug.Assert(g_Device.Get() != null);
        D3D12_FEATURE_DATA_ARCHITECTURE1 architecture1 = new D3D12_FEATURE_DATA_ARCHITECTURE1();

        if (SUCCEEDED(g_Device.Get()->CheckFeatureSupport(D3D12_FEATURE_ARCHITECTURE1, &architecture1, __sizeof<D3D12_FEATURE_DATA_ARCHITECTURE1>())))
        {
            _ = wprintf("D3D12_FEATURE_DATA_ARCHITECTURE1:\n");
            _ = wprintf("    UMA: {0}\n", architecture1.UMA ? 1 : 0);
            _ = wprintf("    CacheCoherentUMA: {0}\n", architecture1.CacheCoherentUMA ? 1 : 0);
            _ = wprintf("    IsolatedMMU: {0}\n", architecture1.IsolatedMMU ? 1 : 0);
        }
    }

    // initializes direct3d 12
    [SupportedOSPlatform("windows10.0")]
    internal static void InitD3D()
    {
        Debug.Assert(g_DXGIUsage != null);

        using ComPtr<IDXGIAdapter1> adapter = g_DXGIUsage->CreateAdapter(g_CommandLineParameters.m_GPUSelection);
        CHECK_BOOL(adapter.Get() != null);

        fixed (DXGI_ADAPTER_DESC1* pAdapterDesc = &g_AdapterDesc)
        {
            CHECK_HR(adapter.Get()->GetDesc1(pAdapterDesc));
        }

        // Must be done before D3D12 device is created.
        if (ENABLE_DEBUG_LAYER)
        {
            using ComPtr<ID3D12Debug> debug = new ComPtr<ID3D12Debug>();

            if (SUCCEEDED(D3D12GetDebugInterface(__uuidof<ID3D12Debug>(), (void**)(&debug))))
            {
                debug.Get()->EnableDebugLayer();
            }
        }

        // Create the g_Device
        ID3D12Device* device = null;

        CHECK_HR(D3D12CreateDevice((IUnknown*)(adapter.Get()), MY_D3D_FEATURE_LEVEL, __uuidof<ID3D12Device>(), (void**)(&device)));
        g_Device.Attach(device);

        // Create allocator
        {
            D3D12MA_ALLOCATOR_DESC desc = new D3D12MA_ALLOCATOR_DESC {
                Flags = g_AllocatorFlags,
                pDevice = device,
                pAdapter = (IDXGIAdapter*)(adapter.Get()),
            };

            if (ENABLE_CPU_ALLOCATION_CALLBACKS)
            {
                delegate* unmanaged<nuint, nuint, void*, void*> pAllocate = &CustomAllocate;
                g_AllocationCallbacks->pAllocate = pAllocate;

                delegate* unmanaged<void*, void*, void> pFree = &CustomFree;
                g_AllocationCallbacks->pFree = pFree;

                g_AllocationCallbacks->pPrivateData = CUSTOM_ALLOCATION_PRIVATE_DATA;

                desc.pAllocationCallbacks = g_AllocationCallbacks;
            }

            fixed (ComPtr<D3D12MA_Allocator>* ppAllocator = &g_Allocator)
            {
                CHECK_HR(D3D12MA_CreateAllocator(&desc, (D3D12MA_Allocator**)(ppAllocator)));
            }
        }

        PrintAdapterInformation(adapter.Get());
        _ = wprintf("\n");

        // -- Create the Command Queue -- //

        // we will be using all the default values
        D3D12_COMMAND_QUEUE_DESC cqDesc = new D3D12_COMMAND_QUEUE_DESC();

        // create the command queue
        ID3D12CommandQueue* commandQueue = null;
        CHECK_HR(g_Device.Get()->CreateCommandQueue(&cqDesc, __uuidof<ID3D12CommandQueue>(), (void**)(&commandQueue)));

        g_CommandQueue.Attach(commandQueue);

        // -- Create the Swap Chain (double/tripple buffering) -- //

        // this is to describe our display mode
        DXGI_MODE_DESC backBufferDesc = new DXGI_MODE_DESC {
            Width = SIZE_X,                 // buffer width
            Height = SIZE_Y,                // buffer height
            Format = RENDER_TARGET_FORMAT,  // format of the buffer (rgba 32 bits, 8 bits for each chanel)
        };

        // describe our multi-sampling. We are not multi-sampling, so we set the count to 1 (we need at least one sample of course)
        DXGI_SAMPLE_DESC sampleDesc = new DXGI_SAMPLE_DESC {
            Count = 1, // multisample count (no multisampling, so we just put 1, since we still need 1 sample)
        };

        // Describe and create the swap chain.
        DXGI_SWAP_CHAIN_DESC swapChainDesc = new DXGI_SWAP_CHAIN_DESC {
            BufferCount = (uint)(FRAME_BUFFER_COUNT),       // number of buffers we have
            BufferDesc = backBufferDesc,                    // our back buffer description
            BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,  // this says the pipeline will render to this swap chain
            SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,     // dxgi will discard the buffer (data) after we call present
            OutputWindow = g_Wnd,                           // handle to our window
            SampleDesc = sampleDesc,                        // our multi-sampling description
            Windowed = !FULLSCREEN,                         // set to true, then if in fullscreen must call SetFullScreenState with true for full screen to get uncapped fps
        };

        IDXGISwapChain* tempSwapChain;

        CHECK_HR(g_DXGIUsage->GetDXGIFactory()->CreateSwapChain(
            (IUnknown*)(g_CommandQueue.Get()), // the queue will be flushed once the swap chain is created
            &swapChainDesc,                    // give it the swap chain description we created above
            &tempSwapChain                     // store the created swap chain in a temp IDXGISwapChain interface
        ));

        g_SwapChain.Attach((IDXGISwapChain3*)(tempSwapChain));

        g_FrameIndex = g_SwapChain.Get()->GetCurrentBackBufferIndex();

        // -- Create the Back Buffers (render target views) Descriptor Heap -- //

        // describe an rtv descriptor heap and create
        D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC {
            NumDescriptors = (uint)(FRAME_BUFFER_COUNT),    // number of descriptors for this heap.
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV,          // this heap is a render target view heap
            Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,        // This heap will not be directly referenced by the shaders (not shader visible), as this will store the output from the pipeline
                                                            // otherwise we would set the heap's flag to D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
        };

        ID3D12DescriptorHeap* rtvDescriptorHeap = null;
        CHECK_HR(g_Device.Get()->CreateDescriptorHeap(&rtvHeapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)(&rtvDescriptorHeap)));
        g_RtvDescriptorHeap.Attach(rtvDescriptorHeap);

        // get the size of a descriptor in this heap (this is a rtv heap, so only rtv descriptors should be stored in it.
        // descriptor sizes may vary from g_Device to g_Device, which is why there is no set size and we must ask the 
        // g_Device to give us the size. we will use this size to increment a descriptor handle offset
        g_RtvDescriptorSize = g_Device.Get()->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

        // get a handle to the first descriptor in the descriptor heap. a handle is basically a pointer,
        // but we cannot literally use it like a c++ pointer.
        D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = g_RtvDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart();

        // Create a RTV for each buffer (double buffering is two buffers, tripple buffering is 3).
        for (int i = 0; i < (int)(FRAME_BUFFER_COUNT); i++)
        {
            // first we get the n'th buffer in the swap chain and store it in the n'th
            // position of our ID3D12Resource array
            ID3D12Resource* res = null;
            CHECK_HR(g_SwapChain.Get()->GetBuffer((uint)(i), __uuidof<ID3D12Resource>(), (void**)(&res)));
            g_RenderTargets[i].Attach(res);

            // the we "create" a render target view which binds the swap chain buffer (ID3D12Resource[n]) to the rtv handle
            g_Device.Get()->CreateRenderTargetView(g_RenderTargets[i].Get(), null, rtvHandle);

            // we increment the rtv handle by the rtv descriptor size we got above
            rtvHandle.ptr += g_RtvDescriptorSize;
        }

        // -- Create the Command Allocators -- //

        for (int i = 0; i < (int)(FRAME_BUFFER_COUNT); i++)
        {
            ID3D12CommandAllocator* commandAllocator = null;
            CHECK_HR(g_Device.Get()->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, __uuidof<ID3D12CommandAllocator>(), (void**)(&commandAllocator)));
            g_CommandAllocators[i].Attach(commandAllocator);
        }

        // create the command list with the first allocator
        fixed (ComPtr<ID3D12GraphicsCommandList>* ppCommandList = &g_CommandList)
        {
            CHECK_HR(g_Device.Get()->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, g_CommandAllocators[0].Get(), null, __uuidof<ID3D12GraphicsCommandList>(), (void**)(ppCommandList)));
        }

        // command lists are created in the recording state. our main loop will set it up for recording again so close it now
        _ = g_CommandList.Get()->Close();

        // create a depth stencil descriptor heap so we can get a pointer to the depth stencil buffer
        D3D12_DESCRIPTOR_HEAP_DESC dsvHeapDesc = new D3D12_DESCRIPTOR_HEAP_DESC {
            NumDescriptors = 1,
            Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
            Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
        };

        fixed (ComPtr<ID3D12DescriptorHeap>* ppDepthStencilDescriptorHeap = &g_DepthStencilDescriptorHeap)
        {
            CHECK_HR(g_Device.Get()->CreateDescriptorHeap(&dsvHeapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)(ppDepthStencilDescriptorHeap)));
        }

        D3D12_CLEAR_VALUE depthOptimizedClearValue = new D3D12_CLEAR_VALUE {
            Format = DEPTH_STENCIL_FORMAT,
            DepthStencil = new D3D12_DEPTH_STENCIL_VALUE {
                Depth = 1.0f,
                Stencil = 0,
            },
        };

        D3D12MA_ALLOCATION_DESC depthStencilAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
        };

        D3D12_RESOURCE_DESC depthStencilResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
            Alignment = 0,
            Width = SIZE_X,
            Height = SIZE_Y,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DEPTH_STENCIL_FORMAT,
            SampleDesc = new DXGI_SAMPLE_DESC {
                Count = 1,
                Quality = 0,
            },
            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
            Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL,
        };

        fixed (D3D12MA_Allocation** ppDepthStencilAllocation = &g_DepthStencilAllocation)
        fixed (ComPtr<ID3D12Resource>* ppDepthStencilBuffer = &g_DepthStencilBuffer)
        {
            CHECK_HR(g_Allocator.Get()->CreateResource(&depthStencilAllocDesc, &depthStencilResourceDesc, D3D12_RESOURCE_STATE_DEPTH_WRITE, &depthOptimizedClearValue, ppDepthStencilAllocation, __uuidof<ID3D12Resource>(), (void**)(ppDepthStencilBuffer)));
        }

        fixed (char* pName = "Depth/Stencil Resource Heap")
        {
            CHECK_HR(g_DepthStencilBuffer.Get()->SetName(pName));
        }

        D3D12_DEPTH_STENCIL_VIEW_DESC depthStencilDesc = new D3D12_DEPTH_STENCIL_VIEW_DESC {
            Format = DEPTH_STENCIL_FORMAT,
            ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D,
            Flags = D3D12_DSV_FLAG_NONE,
        };
        g_Device.Get()->CreateDepthStencilView(g_DepthStencilBuffer.Get(), &depthStencilDesc, g_DepthStencilDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart());

        // -- Create a Fence & Fence Event -- //

        // create the fences
        for (int i = 0; i < (int)(FRAME_BUFFER_COUNT); i++)
        {
            ID3D12Fence* fence = null;
            CHECK_HR(g_Device.Get()->CreateFence(0, D3D12_FENCE_FLAG_NONE, __uuidof<ID3D12Fence>(), (void**)(&fence)));
            g_Fences[i].Attach(fence);

            // set the initial g_Fences value to 0
            g_FenceValues[i] = 0;
        }

        // create a handle to a g_Fences event
        g_FenceEvent = CreateEvent(null, FALSE, FALSE, null);
        Debug.Assert(g_FenceEvent != HANDLE.NULL);

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

        const int _countof_rootParameters = 3;
        D3D12_ROOT_PARAMETER* rootParameters = stackalloc D3D12_ROOT_PARAMETER[_countof_rootParameters];

        rootParameters[0].ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
        rootParameters[0].DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE {
            NumDescriptorRanges = 1,
            pDescriptorRanges = &cbDescriptorRange,
        };
        rootParameters[0].ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;

        rootParameters[1].ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV;
        rootParameters[1].Descriptor = new D3D12_ROOT_DESCRIPTOR {
            ShaderRegister = 1,
            RegisterSpace = 0,
        };
        rootParameters[1].ShaderVisibility = D3D12_SHADER_VISIBILITY_VERTEX;

        rootParameters[2].ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
        rootParameters[2].DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE {
            NumDescriptorRanges = 1,
            pDescriptorRanges = &textureDescRange,
        };
        rootParameters[2].ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;

        // create root signature

        // create a static sampler
        D3D12_STATIC_SAMPLER_DESC sampler = new D3D12_STATIC_SAMPLER_DESC {
            Filter = D3D12_FILTER_MIN_MAG_MIP_POINT,
            AddressU = D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            AddressV = D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            AddressW = D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            MipLODBias = 0,
            MaxAnisotropy = 0,
            ComparisonFunc = D3D12_COMPARISON_FUNC_NEVER,
            BorderColor = D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK,
            MinLOD = 0.0f,
            MaxLOD = D3D12_FLOAT32_MAX,
            ShaderRegister = 0,
            RegisterSpace = 0,
            ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
        };

        D3D12_ROOT_SIGNATURE_DESC rootSignatureDesc = new D3D12_ROOT_SIGNATURE_DESC {
            NumParameters = _countof_rootParameters,
            pParameters = rootParameters,
            NumStaticSamplers = 1,
            pStaticSamplers = &sampler,
            Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT | D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS | D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS | D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS,
        };

        using ComPtr<ID3DBlob> signatureBlob = new ComPtr<ID3DBlob>();
        ID3DBlob* signatureBlobPtr;

        CHECK_HR(D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signatureBlobPtr, null));
        signatureBlob.Attach(signatureBlobPtr);

        ID3D12RootSignature* rootSignature = null;
        CHECK_HR(device->CreateRootSignature(0, signatureBlob.Get()->GetBufferPointer(), signatureBlob.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), (void**)(&rootSignature)));
        g_RootSignature.Attach(rootSignature);

        for (int i = 0; i < (int)(FRAME_BUFFER_COUNT); ++i)
        {
            D3D12_DESCRIPTOR_HEAP_DESC heapDesc = new D3D12_DESCRIPTOR_HEAP_DESC {
                NumDescriptors = 2,
                Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE,
                Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
            };

            fixed (ComPtr<ID3D12DescriptorHeap>* ppMainDescriptorHeap = &g_MainDescriptorHeap[i])
            {
                CHECK_HR(g_Device.Get()->CreateDescriptorHeap(&heapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)(ppMainDescriptorHeap)));
            }
        }

        // # CONSTANT BUFFER

        for (int i = 0; i < (int)(FRAME_BUFFER_COUNT); ++i)
        {
            D3D12MA_ALLOCATION_DESC constantBufferUploadAllocDesc = new D3D12MA_ALLOCATION_DESC {
                HeapType = D3D12_HEAP_TYPE_UPLOAD,
            };

            D3D12_RESOURCE_DESC constantBufferResourceDesc = new D3D12_RESOURCE_DESC {
                Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
                Alignment = 0,
                Width = 1024 * 64,
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

            fixed (Pointer<D3D12MA_Allocation>* ppConstantBufferUploadAllocation = &g_ConstantBufferUploadAllocation[i])
            fixed (ComPtr<ID3D12Resource>* ppConstantBufferUploadHeap = &g_ConstantBufferUploadHeap[i])
            {
                CHECK_HR(g_Allocator.Get()->CreateResource(&constantBufferUploadAllocDesc, &constantBufferResourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, null, (D3D12MA_Allocation**)(ppConstantBufferUploadAllocation), __uuidof<ID3D12Resource>(), (void**)(ppConstantBufferUploadHeap)));
            }

            fixed (char* pName = "Constant Buffer Upload Resource Heap")
            {
                _ = g_ConstantBufferUploadHeap[i].Get()->SetName(pName);
            }

            D3D12_CONSTANT_BUFFER_VIEW_DESC cbvDesc = new D3D12_CONSTANT_BUFFER_VIEW_DESC {
                BufferLocation = g_ConstantBufferUploadHeap[i].Get()->GetGPUVirtualAddress(),
                SizeInBytes = AlignUp(__sizeof<ConstantBuffer0_PS>(), 256)
            };
            g_Device.Get()->CreateConstantBufferView(&cbvDesc, g_MainDescriptorHeap[i].Get()->GetCPUDescriptorHandleForHeapStart());

            fixed (Pointer* ppConstantBufferAddress = &g_ConstantBufferAddress[i])
            {
                CHECK_HR(g_ConstantBufferUploadHeap[i].Get()->Map(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))), (void**)(ppConstantBufferAddress)));
            }
        }

        // create input layout

        // The input layout is used by the Input Assembler so that it knows
        // how to read the vertex data bound to it.

        const int _countof_inputLayout = 2;
        D3D12_INPUT_ELEMENT_DESC* inputLayout = stackalloc D3D12_INPUT_ELEMENT_DESC[_countof_inputLayout] {
            new D3D12_INPUT_ELEMENT_DESC {
                SemanticName = (sbyte*)(Unsafe.AsPointer(ref MemoryMarshal.GetReference("POSITION"u8))),
                SemanticIndex = 0,
                Format = DXGI_FORMAT_R32G32B32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = D3D12_APPEND_ALIGNED_ELEMENT,
                InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                InstanceDataStepRate = 0,
            },
            new D3D12_INPUT_ELEMENT_DESC {
                SemanticName = (sbyte*)(Unsafe.AsPointer(ref MemoryMarshal.GetReference("TEXCOORD"u8))),
                SemanticIndex = 0,
                Format = DXGI_FORMAT_R32G32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = D3D12_APPEND_ALIGNED_ELEMENT,
                InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                InstanceDataStepRate = 0
            },
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

        // a structure to define a pso
        D3D12_GRAPHICS_PIPELINE_STATE_DESC psoDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC {
            InputLayout = new D3D12_INPUT_LAYOUT_DESC {
                NumElements = _countof_inputLayout,
                pInputElementDescs = inputLayout,
            },
            pRootSignature = g_RootSignature.Get(),                         // the root signature that describes the input data this pso needs
            VS = new D3D12_SHADER_BYTECODE {
                BytecodeLength = (uint)(VS_g_main.Length),
                pShaderBytecode = Unsafe.AsPointer(ref MemoryMarshal.GetReference(VS_g_main)),
            },
            PS = new D3D12_SHADER_BYTECODE {
                BytecodeLength = (uint)(PS_g_main.Length),
                pShaderBytecode = Unsafe.AsPointer(ref MemoryMarshal.GetReference(PS_g_main)),
            },
            PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE, // type of topology we are drawing
            DSVFormat = DEPTH_STENCIL_FORMAT,
            SampleDesc = sampleDesc,                                        // must be the same sample description as the swapchain and depth/stencil buffer
            SampleMask = 0xffffffff,                                        // sample mask has to do with multi-sampling. 0xffffffff means point sampling is done
            NumRenderTargets = 1,                                           // we are only binding one render target
        };

        // format of the render target
        psoDesc.RTVFormats[0] = RENDER_TARGET_FORMAT;

        SetDefaultRasterizerDesc(out psoDesc.RasterizerState);
        SetDefaultBlendDesc(out psoDesc.BlendState);
        SetDefaultDepthStencilDesc(out psoDesc.DepthStencilState);

        // create the pso
        ID3D12PipelineState* pipelineStateObject;
        CHECK_HR(device->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), (void**)(&pipelineStateObject)));
        g_PipelineStateObject.Attach(pipelineStateObject);

        // Create vertex buffer

        // a triangle
        const int _countof_vList = 24;
        Vertex* vList = stackalloc Vertex[_countof_vList] {
            // front face
            new Vertex(-0.5f, 0.5f, -0.5f, 0.0f, 0.0f),
            new Vertex(0.5f, -0.5f, -0.5f, 1.0f, 1.0f),
            new Vertex(-0.5f, -0.5f, -0.5f, 0.0f, 1.0f),
            new Vertex(0.5f, 0.5f, -0.5f, 1.0f, 0.0f),

            // right side face
            new Vertex(0.5f, -0.5f, -0.5f, 0.0f, 1.0f),
            new Vertex(0.5f, 0.5f, 0.5f, 1.0f, 0.0f),
            new Vertex(0.5f, -0.5f, 0.5f, 1.0f, 1.0f),
            new Vertex(0.5f, 0.5f, -0.5f, 0.0f, 0.0f),

            // left side face
            new Vertex(-0.5f, 0.5f, 0.5f, 0.0f, 0.0f),
            new Vertex(-0.5f, -0.5f, -0.5f, 1.0f, 1.0f),
            new Vertex(-0.5f, -0.5f, 0.5f, 0.0f, 1.0f),
            new Vertex(-0.5f, 0.5f, -0.5f, 1.0f, 0.0f),

            // back face
            new Vertex(0.5f, 0.5f, 0.5f, 0.0f, 0.0f),
            new Vertex(-0.5f, -0.5f, 0.5f, 1.0f, 1.0f),
            new Vertex(0.5f, -0.5f, 0.5f, 0.0f, 1.0f),
            new Vertex(-0.5f, 0.5f, 0.5f, 1.0f, 0.0f),

            // top face
            new Vertex(-0.5f, 0.5f, -0.5f, 0.0f, 0.0f),
            new Vertex(0.5f, 0.5f, 0.5f, 1.0f, 1.0f),
            new Vertex(0.5f, 0.5f, -0.5f, 0.0f, 1.0f),
            new Vertex(-0.5f, 0.5f, 0.5f, 1.0f, 0.0f),

            // bottom face
            new Vertex(0.5f, -0.5f, 0.5f, 0.0f, 0.0f),
            new Vertex(-0.5f, -0.5f, -0.5f, 1.0f, 1.0f),
            new Vertex(0.5f, -0.5f, -0.5f, 0.0f, 1.0f),
            new Vertex(-0.5f, -0.5f, 0.5f, 1.0f, 0.0f),
        };
        uint vBufferSize = __sizeof<Vertex>() * _countof_vList;

        // create default heap
        // default heap is memory on the GPU. Only the GPU has access to this memory
        // To get data into this heap, we will have to upload the data using
        // an upload heap
        D3D12MA_ALLOCATION_DESC vertexBufferAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
        };

        D3D12_RESOURCE_DESC vertexBufferResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = vBufferSize,
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

        ID3D12Resource* vertexBufferPtr;

        fixed (D3D12MA_Allocation** pVertexBufferAllocation = &g_VertexBufferAllocation)
        {
            CHECK_HR(g_Allocator.Get()->CreateResource(
                &vertexBufferAllocDesc,
                &vertexBufferResourceDesc,      // resource description for a buffer
                D3D12_RESOURCE_STATE_COPY_DEST, // we will start this heap in the copy destination state since we will copy data
                                                // from the upload heap to this heap
                null,                           // optimized clear value must be null for this type of resource. used for render targets and depth/stencil buffers
                pVertexBufferAllocation,
                __uuidof<ID3D12Resource>(),
                (void**)(&vertexBufferPtr)
            ));
        }
        g_VertexBuffer.Attach(vertexBufferPtr);

        // we can give resource heaps a name so when we debug with the graphics debugger we know what resource we are looking at
        fixed (char* pName = "Vertex Buffer Resource Heap")
        {
            _ = g_VertexBuffer.Get()->SetName(pName);
        }

        // create upload heap
        // upload heaps are used to upload data to the GPU. CPU can write to it, GPU can read from it
        // We will upload the vertex buffer using this heap to the default heap
        D3D12MA_ALLOCATION_DESC vBufferUploadAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD
        };

        D3D12_RESOURCE_DESC vertexBufferUploadResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = vBufferSize,
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

        using ComPtr<ID3D12Resource> vBufferUploadHeap = new ComPtr<ID3D12Resource>();
        D3D12MA_Allocation* vBufferUploadHeapAllocation = null;

        CHECK_HR(g_Allocator.Get()->CreateResource(
            &vBufferUploadAllocDesc,
            &vertexBufferUploadResourceDesc,    // resource description for a buffer
            D3D12_RESOURCE_STATE_GENERIC_READ,  // GPU will read from this buffer and copy its contents to the default heap
            null,
            &vBufferUploadHeapAllocation,
            __uuidof<ID3D12Resource>(),
            (void**)(&vBufferUploadHeap)
        ));

        fixed (char* pName = "Vertex Buffer Upload Resource Heap")
        {
            _ = vBufferUploadHeap.Get()->SetName(pName);
        }

        // store vertex buffer in upload heap
        D3D12_SUBRESOURCE_DATA vertexData = new D3D12_SUBRESOURCE_DATA {
            pData = (byte*)(vList),             // pointer to our vertex array
            RowPitch = (nint)(vBufferSize),     // size of all our triangle vertex data
            SlicePitch = (nint)(vBufferSize),   // also the size of our triangle vertex data
        };

        CHECK_HR(g_CommandList.Get()->Reset(g_CommandAllocators[(int)(g_FrameIndex)].Get(), null));

        // we are now creating a command with the command list to copy the data from the upload heap to the default heap
        ulong r = UpdateSubresources(g_CommandList.Get(), g_VertexBuffer.Get(), vBufferUploadHeap.Get(), 0, 0, 1, &vertexData);
        Debug.Assert(r != 0);

        // transition the vertex buffer data from copy destination state to vertex buffer state
        D3D12_RESOURCE_BARRIER vbBarrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
        };

        vbBarrier.Transition.pResource = g_VertexBuffer.Get();
        vbBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
        vbBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
        vbBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

        g_CommandList.Get()->ResourceBarrier(1, &vbBarrier);

        // Create index buffer

        // a quad (2 triangles)
        const int _countof_iList = 36;
        ushort* iList = stackalloc ushort[_countof_iList] {
            // ffront face
            0, 1, 2,    // first triangle
            0, 3, 1,    // second triangle

            // left face
            4, 5, 6,    // first triangle
            4, 7, 5,    // second triangle

            // right face
            8, 9, 10,   // first triangle
            8, 11, 9,   // second triangle

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

        g_CubeIndexCount = (uint)(_countof_iList);
        nuint iBufferSize = sizeof(ushort) * _countof_iList; ;

        // create default heap to hold index buffer
        D3D12MA_ALLOCATION_DESC indexBufferAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT
        };

        D3D12_RESOURCE_DESC indexBufferResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = iBufferSize,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_UNKNOWN
        };

        indexBufferResourceDesc.SampleDesc.Count = 1;
        indexBufferResourceDesc.SampleDesc.Quality = 0;
        indexBufferResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
        indexBufferResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

        fixed (D3D12MA_Allocation** ppIndexBufferAllocation = &g_IndexBufferAllocation)
        fixed (ComPtr<ID3D12Resource>* ppIndexBuffer = &g_IndexBuffer)
        {
            CHECK_HR(g_Allocator.Get()->CreateResource(
                &indexBufferAllocDesc,
                &indexBufferResourceDesc,       // resource description for a buffer
                D3D12_RESOURCE_STATE_COPY_DEST, // start in the copy destination state
                null,                           // optimized clear value must be null for this type of resource
                ppIndexBufferAllocation,
                __uuidof<ID3D12Resource>(),
                (void**)(ppIndexBuffer)
            ));
        }

        // we can give resource heaps a name so when we debug with the graphics debugger we know what resource we are looking at
        fixed (char* pName = "Index Buffer Resource Heap")
        {
            _ = g_IndexBuffer.Get()->SetName(pName);
        }

        // create upload heap to upload index buffer
        D3D12MA_ALLOCATION_DESC iBufferUploadAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD
        };

        D3D12_RESOURCE_DESC indexBufferUploadResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = iBufferSize,
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

        using ComPtr<ID3D12Resource> iBufferUploadHeap = new ComPtr<ID3D12Resource>();
        D3D12MA_Allocation* iBufferUploadHeapAllocation = null;

        CHECK_HR(g_Allocator.Get()->CreateResource(
            &iBufferUploadAllocDesc,
            &indexBufferUploadResourceDesc,     // resource description for a buffer
            D3D12_RESOURCE_STATE_GENERIC_READ,  // GPU will read from this buffer and copy its contents to the default heap
            null,
            &iBufferUploadHeapAllocation,
            __uuidof<ID3D12Resource>(),
            (void**)(&iBufferUploadHeap)
        ));

        fixed (char* pName = "Index Buffer Upload Resource Heap")
        {
            CHECK_HR(iBufferUploadHeap.Get()->SetName(pName));
        }

        // store vertex buffer in upload heap
        D3D12_SUBRESOURCE_DATA indexData = new D3D12_SUBRESOURCE_DATA {
            pData = iList,                      // pointer to our index array
            RowPitch = (nint)(iBufferSize),     // size of all our index buffer
            SlicePitch = (nint)(iBufferSize),   // also the size of our index buffer
        };

        // we are now creating a command with the command list to copy the data from
        // the upload heap to the default heap
        r = UpdateSubresources(g_CommandList.Get(), g_IndexBuffer.Get(), iBufferUploadHeap.Get(), 0, 0, 1, &indexData);
        Debug.Assert(r != 0);

        // transition the index buffer data from copy destination state to vertex buffer state
        D3D12_RESOURCE_BARRIER ibBarrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
        };

        ibBarrier.Transition.pResource = g_IndexBuffer.Get();
        ibBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
        ibBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_INDEX_BUFFER;
        ibBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

        g_CommandList.Get()->ResourceBarrier(1, &ibBarrier);

        // create a vertex buffer view for the triangle. We get the GPU memory address to the vertex pointer using the GetGPUVirtualAddress() method
        g_VertexBufferView.BufferLocation = g_VertexBuffer.Get()->GetGPUVirtualAddress();
        g_VertexBufferView.StrideInBytes = __sizeof<Vertex>();
        g_VertexBufferView.SizeInBytes = vBufferSize;

        // create a index buffer view for the triangle. We get the GPU memory address to the vertex pointer using the GetGPUVirtualAddress() method
        g_IndexBufferView.BufferLocation = g_IndexBuffer.Get()->GetGPUVirtualAddress();
        g_IndexBufferView.Format = DXGI_FORMAT_R16_UINT;
        g_IndexBufferView.SizeInBytes = (uint)(iBufferSize);

        D3D12MA_ALLOCATION_DESC cbPerObjectUploadAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
        };

        D3D12_RESOURCE_DESC cbPerObjectUploadResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = 1024 * 64,
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

        for (nuint i = 0; i < FRAME_BUFFER_COUNT; ++i)
        {
            // create resource for cube 1

            fixed (Pointer<D3D12MA_Allocation>* ppCbPerObjectUploadHeapAllocation = &g_CbPerObjectUploadHeapAllocations[(int)(i)])
            fixed (ComPtr<ID3D12Resource>* ppCbPerObjectUploadHeap = &g_CbPerObjectUploadHeaps[(int)(i)])
            {
                CHECK_HR(g_Allocator.Get()->CreateResource(
                    &cbPerObjectUploadAllocDesc,
                    &cbPerObjectUploadResourceDesc, // size of the resource heap. Must be a multiple of 64KB for single-textures and constant buffers
                    D3D12_RESOURCE_STATE_GENERIC_READ, // will be data that is read from so we keep it in the generic read state
                    null, // we do not have use an optimized clear value for constant buffers
                    (D3D12MA_Allocation**)(ppCbPerObjectUploadHeapAllocation),
                    __uuidof<ID3D12Resource>(),
                    (void**)(ppCbPerObjectUploadHeap)
                ));
            }

            fixed (char* pName = "Constant Buffer Upload Resource Heap")
            {
                _ = g_CbPerObjectUploadHeaps[(int)(i)].Get()->SetName(pName);
            }

            fixed (Pointer* ppCbPerObjectAddress = &g_CbPerObjectAddress[(int)(i)])
            {
                CHECK_HR(g_CbPerObjectUploadHeaps[(int)(i)].Get()->Map(0, (D3D12_RANGE*)(Unsafe.AsPointer(ref Unsafe.AsRef(in EMPTY_RANGE))), (void**)(ppCbPerObjectAddress)));
            }
        }

        // # TEXTURE

        D3D12_RESOURCE_DESC textureDesc;
        nuint imageBytesPerRow;
        nuint imageSize = nuint.MaxValue;

        sbyte[] imageData;
        {
            uint sizeX = 256;
            uint sizeY = 256;
            DXGI_FORMAT format = DXGI_FORMAT_R8G8B8A8_UNORM;
            uint bytesPerPixel = 4;

            imageBytesPerRow = sizeX * bytesPerPixel;
            imageSize = sizeY * imageBytesPerRow;

            imageData = new sbyte[imageSize];

            fixed (sbyte* pImageData = imageData)
            {
                sbyte* rowPtr = (sbyte*)(pImageData);

                for (uint y = 0; y < sizeY; ++y)
                {
                    sbyte* pixelPtr = rowPtr;

                    for (uint x = 0; x < sizeX; ++x)
                    {
                        *(byte*)(pixelPtr) = (byte)(x);     // R
                        *(byte*)(pixelPtr + 1) = (byte)(y); // G
                        *(byte*)(pixelPtr + 2) = 0x00;      // B
                        *(byte*)(pixelPtr + 3) = 0xFF;      // A

                        *(byte*)(pixelPtr) = (byte)((x > 128) ? 0xFF : 0x00);
                        *(byte*)(pixelPtr + 1) = (byte)((y > 128) ? 0xFF : 0x00);

                        pixelPtr += bytesPerPixel;
                    }

                    rowPtr += imageBytesPerRow;
                }
            }

            textureDesc = new D3D12_RESOURCE_DESC {
                Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                Alignment = 0,
                Width = sizeX,
                Height = sizeY,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = format,
                SampleDesc = new DXGI_SAMPLE_DESC {
                    Count = 1,
                    Quality = 0,
                },
                Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
                Flags = D3D12_RESOURCE_FLAG_NONE,
            };
        }

        D3D12MA_ALLOCATION_DESC textureAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
        };

        fixed (D3D12MA_Allocation** ppTextureAllocation = &g_TextureAllocation)
        fixed (ComPtr<ID3D12Resource>* ppTexture = &g_Texture)
        {
            CHECK_HR(g_Allocator.Get()->CreateResource(
                &textureAllocDesc,
                &textureDesc,
                D3D12_RESOURCE_STATE_COPY_DEST,
                null, // pOptimizedClearValue
                ppTextureAllocation,
                __uuidof<ID3D12Resource>(),
                (void**)(ppTexture)
            ));
        }

        fixed (char* pName = nameof(g_Texture))
        {
            _ = g_Texture.Get()->SetName(pName);
        }

        ulong textureUploadBufferSize;

        device->GetCopyableFootprints(
            &textureDesc,
            0,                          // FirstSubresource
            1,                          // NumSubresources
            0,                          // BaseOffset
            null,                       // pLayouts
            null,                       // pNumRows
            null,                       // pRowSizeInBytes
            &textureUploadBufferSize    // pTotalBytes
        );

        D3D12MA_ALLOCATION_DESC textureUploadAllocDesc = new D3D12MA_ALLOCATION_DESC {
            HeapType = D3D12_HEAP_TYPE_UPLOAD,
        };

        D3D12_RESOURCE_DESC textureUploadResourceDesc = new D3D12_RESOURCE_DESC {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = textureUploadBufferSize,
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

        using ComPtr<ID3D12Resource> textureUpload = new ComPtr<ID3D12Resource>();
        D3D12MA_Allocation* textureUploadAllocation;

        CHECK_HR(g_Allocator.Get()->CreateResource(
            &textureUploadAllocDesc,
            &textureUploadResourceDesc,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            null, // pOptimizedClearValue
            &textureUploadAllocation,
            __uuidof<ID3D12Resource>(),
            (void**)(&textureUpload)
        ));

        fixed (char* pName = nameof(textureUpload))
        {
            _= textureUpload.Get()->SetName(pName);
        }

        fixed (sbyte* pImageData = imageData)
        {
            D3D12_SUBRESOURCE_DATA textureSubresourceData = new D3D12_SUBRESOURCE_DATA {
                pData = pImageData,
                RowPitch = (nint)(imageBytesPerRow),
                SlicePitch = (nint)(imageBytesPerRow * textureDesc.Height),
            };

            _ = UpdateSubresources(g_CommandList.Get(), g_Texture.Get(), textureUpload.Get(), 0, 0, 1, &textureSubresourceData);
        }

        D3D12_RESOURCE_BARRIER textureBarrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION
        };

        textureBarrier.Transition.pResource = g_Texture.Get();
        textureBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_COPY_DEST;
        textureBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
        textureBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

        g_CommandList.Get()->ResourceBarrier(1, &textureBarrier);

        D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = textureDesc.Format,
            ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D
        };
        srvDesc.Texture2D.MipLevels = 1;

        for (nuint i = 0; i < FRAME_BUFFER_COUNT; ++i)
        {
            D3D12_CPU_DESCRIPTOR_HANDLE descHandle = new D3D12_CPU_DESCRIPTOR_HANDLE {
                ptr = g_MainDescriptorHeap[(int)(i)].Get()->GetCPUDescriptorHandleForHeapStart().ptr + g_Device.Get()->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV)
            };
            g_Device.Get()->CreateShaderResourceView(g_Texture.Get(), &srvDesc, descHandle);
        }

        // # END OF INITIAL COMMAND LIST

        // Now we execute the command list to upload the initial assets (triangle data)
        _ = g_CommandList.Get()->Close();

        const int _countof_ppCommandLists = 1;

        ID3D12CommandList** ppCommandLists = stackalloc ID3D12CommandList*[_countof_ppCommandLists] {
            (ID3D12CommandList*)(g_CommandList.Get()),
        };

        commandQueue->ExecuteCommandLists(_countof_ppCommandLists, ppCommandLists);

        // increment the fence value now, otherwise the buffer might not be uploaded by the time we start drawing
        WaitGPUIdle(g_FrameIndex);

        _ = textureUploadAllocation->Release();
        _ = iBufferUploadHeapAllocation->Release();
        _ = vBufferUploadHeapAllocation->Release();
    }

    internal static void Update()
    {
        {
            float f = (MathF.Sin(g_Time * (PI * 2.0f)) * 0.5f) + 0.5f;

            ConstantBuffer0_PS cb;
            cb.Color = new vec4(f, f, f, 1.0f);

            _ = memcpy(g_ConstantBufferAddress[(int)(g_FrameIndex)].Value, &cb, __sizeof<ConstantBuffer0_PS>());
        }

        {
            mat4 projection = mat4.Perspective(
                45.0f * (PI / 180.0f),        // fovY
                (float)(SIZE_X) / (float)(SIZE_Y),  // aspectRatio
                0.1f,                               // zNear
                1000.0f                             // zFar
            );

            mat4 view = mat4.LookAt(
                new vec3(0.0f, 0.0f, 0.0f),     // at
                new vec3(-0.4f, 1.7f, -3.5f),   // eye
                new vec3(0.0f, 1.0f, 0.0f)      // up
            );

            mat4 viewProjection = view * projection;

            mat4 cube1World = mat4.RotationZ(g_Time);

            ConstantBuffer1_VS cb;
            mat4 worldViewProjection = cube1World * viewProjection;

            cb.WorldViewProj = worldViewProjection.Transposed();
            _= memcpy(g_CbPerObjectAddress[(int)(g_FrameIndex)].Value, &cb, __sizeof<ConstantBuffer1_VS>());

            mat4 cube2World = mat4.Scaling(0.5f) * mat4.RotationX(g_Time * 2.0f) * mat4.Translation(new vec3(-1.2f, 0.0f, 0.0f)) * cube1World;
            worldViewProjection = cube2World * viewProjection;

            cb.WorldViewProj = worldViewProjection.Transposed();
            _ = memcpy((byte*)(g_CbPerObjectAddress[(int)(g_FrameIndex)].Value) + ConstantBufferPerObjectAlignedSize, &cb, __sizeof<ConstantBuffer1_VS>());
        }
    }

    [SupportedOSPlatform("windows10.0")]
    internal static void Render() // execute the command list
    {
        // # Here was UpdatePipeline function.

        // swap the current rtv buffer index so we draw on the correct buffer
        g_FrameIndex = g_SwapChain.Get()->GetCurrentBackBufferIndex();

        // We have to wait for the gpu to finish with the command allocator before we reset it
        WaitForFrame(g_FrameIndex);

        // increment g_FenceValues for next frame
        g_FenceValues[(int)(g_FrameIndex)]++;

        // we can only reset an allocator once the gpu is done with it
        // resetting an allocator frees the memory that the command list was stored in
        CHECK_HR(g_CommandAllocators[(int)(g_FrameIndex)].Get()->Reset());

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
        CHECK_HR(g_CommandList.Get()->Reset(g_CommandAllocators[(int)(g_FrameIndex)].Get(), null));

        // here we start recording commands into the g_CommandList (which all the commands will be stored in the g_CommandAllocators)

        // transition the "g_FrameIndex" render target from the present state to the render target state so the command list draws to it starting from here
        D3D12_RESOURCE_BARRIER presentToRenderTargetBarrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION
        };

        presentToRenderTargetBarrier.Transition.pResource = g_RenderTargets[(int)(g_FrameIndex)].Get();
        presentToRenderTargetBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_PRESENT;
        presentToRenderTargetBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_RENDER_TARGET;
        presentToRenderTargetBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

        g_CommandList.Get()->ResourceBarrier(1, &presentToRenderTargetBarrier);

        // here we again get the handle to our current render target view so we can set it as the render target in the output merger stage of the pipeline
        D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = new D3D12_CPU_DESCRIPTOR_HANDLE {
            ptr = g_RtvDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart().ptr + (g_FrameIndex * g_RtvDescriptorSize)
        };

        D3D12_CPU_DESCRIPTOR_HANDLE dsvHandle = g_DepthStencilDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart();

        // set the render target for the output merger stage (the output of the pipeline)
        g_CommandList.Get()->OMSetRenderTargets(1, &rtvHandle, FALSE, &dsvHandle);

        g_CommandList.Get()->ClearDepthStencilView(g_DepthStencilDescriptorHeap.Get()->GetCPUDescriptorHandleForHeapStart(), D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0, 0, null);

        // Clear the render target by using the ClearRenderTargetView command
        float* clearColor = stackalloc float[4] { 0.0f, 0.2f, 0.4f, 1.0f };
        g_CommandList.Get()->ClearRenderTargetView(rtvHandle, clearColor, 0, null);

        g_CommandList.Get()->SetPipelineState(g_PipelineStateObject.Get());

        g_CommandList.Get()->SetGraphicsRootSignature(g_RootSignature.Get());

        const int _countof_descriptorHeaps = 1;
        ID3D12DescriptorHeap** descriptorHeaps = stackalloc ID3D12DescriptorHeap*[_countof_descriptorHeaps] {
            g_MainDescriptorHeap[(int)(g_FrameIndex)].Get()
        };
        g_CommandList.Get()->SetDescriptorHeaps(_countof_descriptorHeaps, descriptorHeaps);

        g_CommandList.Get()->SetGraphicsRootDescriptorTable(0, g_MainDescriptorHeap[(int)(g_FrameIndex)].Get()->GetGPUDescriptorHandleForHeapStart());
        g_CommandList.Get()->SetGraphicsRootDescriptorTable(2, g_MainDescriptorHeap[(int)(g_FrameIndex)].Get()->GetGPUDescriptorHandleForHeapStart());

        D3D12_VIEWPORT viewport = new D3D12_VIEWPORT {
            TopLeftX = 0.0f,
            TopLeftY = 0.0f,
            Width = (float)(SIZE_X),
            Height = (float)(SIZE_Y),
            MinDepth = 0.0f,
            MaxDepth = 1.0f,
        };

        // set the viewports
        g_CommandList.Get()->RSSetViewports(1, &viewport);

        RECT scissorRect = new RECT {
            left = 0,
            top = 0,
            right = SIZE_X,
            bottom = SIZE_Y
        };

        // set the scissor rects
        g_CommandList.Get()->RSSetScissorRects(1, &scissorRect);

        // set the primitive topology
        g_CommandList.Get()->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        fixed (D3D12_VERTEX_BUFFER_VIEW* pVertexBufferView = &g_VertexBufferView)
        {
            // set the vertex buffer (using the vertex buffer view)
            g_CommandList.Get()->IASetVertexBuffers(0, 1, pVertexBufferView);
        }

        fixed (D3D12_INDEX_BUFFER_VIEW* pIndexBufferView = &g_IndexBufferView)
        {
            g_CommandList.Get()->IASetIndexBuffer(pIndexBufferView);
        }

        g_CommandList.Get()->SetGraphicsRootConstantBufferView(1, g_CbPerObjectUploadHeaps[(int)(g_FrameIndex)].Get()->GetGPUVirtualAddress());
        g_CommandList.Get()->DrawIndexedInstanced(g_CubeIndexCount, 1, 0, 0, 0);

        g_CommandList.Get()->SetGraphicsRootConstantBufferView(1, g_CbPerObjectUploadHeaps[(int)(g_FrameIndex)].Get()->GetGPUVirtualAddress() + ConstantBufferPerObjectAlignedSize);
        g_CommandList.Get()->DrawIndexedInstanced(g_CubeIndexCount, 1, 0, 0, 0);

        // transition the "g_FrameIndex" render target from the render target state to the present state. If the debug layer is enabled, you will receive a
        // warning if present is called on the render target when it's not in the present state
        D3D12_RESOURCE_BARRIER renderTargetToPresentBarrier = new D3D12_RESOURCE_BARRIER {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
        };

        renderTargetToPresentBarrier.Transition.pResource = g_RenderTargets[(int)(g_FrameIndex)].Get();
        renderTargetToPresentBarrier.Transition.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET;
        renderTargetToPresentBarrier.Transition.StateAfter = D3D12_RESOURCE_STATE_PRESENT;
        renderTargetToPresentBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

        g_CommandList.Get()->ResourceBarrier(1, &renderTargetToPresentBarrier);

        CHECK_HR(g_CommandList.Get()->Close());

        // ================

        // create an array of command lists (only one command list here)
        const int _countof_ppCommandLists = 1;
        ID3D12CommandList** ppCommandLists = stackalloc ID3D12CommandList*[_countof_ppCommandLists] {
            (ID3D12CommandList*)(g_CommandList.Get())
        };

        // execute the array of command lists
        g_CommandQueue.Get()->ExecuteCommandLists(_countof_ppCommandLists, ppCommandLists);

        // this command goes in at the end of our command queue. we will know when our command queue 
        // has finished because the g_Fences value will be set to "g_FenceValues" from the GPU since the command
        // queue is being executed on the GPU
        CHECK_HR(g_CommandQueue.Get()->Signal(g_Fences[(int)(g_FrameIndex)].Get(), g_FenceValues[(int)(g_FrameIndex)]));

        // present the current backbuffer
        CHECK_HR(g_SwapChain.Get()->Present(PRESENT_SYNC_INTERVAL, 0));
    }

    [SupportedOSPlatform("windows10.0")]
    internal static void Cleanup() // release com ojects and clean up memory
    {
        // wait for the gpu to finish all frames
        for (nuint i = 0; i < FRAME_BUFFER_COUNT; ++i)
        {
            WaitForFrame(i);
            CHECK_HR(g_CommandQueue.Get()->Wait(g_Fences[(int)(i)].Get(), g_FenceValues[(int)(i)]));
        }

        // get swapchain out of full screen before exiting
        BOOL fs = false;
        CHECK_HR(g_SwapChain.Get()->GetFullscreenState(&fs, null));

        if (fs)
        {
            _ = g_SwapChain.Get()->SetFullscreenState(false, null);
        }

        WaitGPUIdle(0);

        _ = g_Texture.Reset();

        _ = g_TextureAllocation->Release();
        g_TextureAllocation = null;

        _ = g_IndexBuffer.Reset();

        _ = g_IndexBufferAllocation->Release();
        g_IndexBufferAllocation = null;

        _ = g_VertexBuffer.Reset();

        _ = g_VertexBufferAllocation->Release();
        g_VertexBufferAllocation = null;

        _ = g_PipelineStateObject.Reset();
        _ = g_RootSignature.Reset();

        _ = CloseHandle(g_FenceEvent);

        _ = g_CommandList.Reset();
        _ = g_CommandQueue.Reset();

        for (nuint i = FRAME_BUFFER_COUNT; i-- != 0;)
        {
            _ = g_CbPerObjectUploadHeaps[(int)(i)].Reset();

            _ = g_CbPerObjectUploadHeapAllocations[(int)(i)].Value->Release();
            g_CbPerObjectUploadHeapAllocations[(int)(i)].Value = null;

            _ = g_MainDescriptorHeap[(int)(i)].Reset();
            _ = g_ConstantBufferUploadHeap[(int)(i)].Reset();

            _ = g_ConstantBufferUploadAllocation[(int)(i)].Value->Release();
            g_ConstantBufferUploadAllocation[(int)(i)].Value = null;
        }

        _ = g_DepthStencilDescriptorHeap.Reset();
        _ = g_DepthStencilBuffer.Reset();

        _ = g_DepthStencilAllocation->Release();
        g_DepthStencilAllocation = null;

        _ = g_RtvDescriptorHeap.Reset();

        for (nuint i = FRAME_BUFFER_COUNT; i-- != 0;)
        {
            _ = g_RenderTargets[(int)(i)].Reset();
            _ = g_CommandAllocators[(int)(i)].Reset();
            _ = g_Fences[(int)(i)].Reset();
        }

        _ = g_Allocator.Reset();

        if (ENABLE_CPU_ALLOCATION_CALLBACKS)
        {
            Debug.Assert(Volatile.Read(ref g_CpuAllocationCount) == (nuint)(0u));
        }

        _ = g_Device.Reset();
        _ = g_SwapChain.Reset();
    }

    [SupportedOSPlatform("windows10.0")]
    internal static void ExecuteTests(bool benchmark)
    {
        try
        {
            TestContext ctx = new TestContext {
                allocationCallbacks = g_AllocationCallbacks,
                device = g_Device.Get(),
                allocator = g_Allocator.Get(),
                allocatorFlags = g_AllocatorFlags,
            };
            Test(ctx, benchmark);
        }
        catch (Exception ex)
        {
            _ = wprintf("ERROR: {0}\n", ex.Message);
        }
    }

    [SupportedOSPlatform("windows10.0")]
    internal static void OnKeyDown(WPARAM key)
    {
        switch (key)
        {
            case 'T':
            {
                ExecuteTests(benchmark: false);
                break;
            }

            case 'B':
            {
                ExecuteTests(benchmark: true);
                break;
            }

            case 'J':
            {
                char* statsString = null;
                g_Allocator.Get()->BuildStatsString(&statsString, TRUE);

                _ = wprintf("{0}\n", new string((char*)(statsString)));
                g_Allocator.Get()->FreeStatsString(statsString);
            }
            break;

            case VK_ESCAPE:
            {
                _ = PostMessageW(g_Wnd, WM_CLOSE, 0, 0);
                break;
            }
        }
    }

    [UnmanagedCallersOnly]
    [SupportedOSPlatform("windows10.0")]
    internal static LRESULT WndProc(HWND wnd, [NativeTypeName("UINT")] uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_DESTROY:
            {
                try
                {
                    Cleanup();
                }
                catch (Exception ex)
                {
                    _ = fwprintf(Console.Error, "ERROR: {0}\n", ex.Message);
                }

                PostQuitMessage(0);
                return 0;
            }

            case WM_KEYDOWN:
            {
                try
                {
                    OnKeyDown(wParam);
                }
                catch (Exception ex)
                {
                    _ = fwprintf(Console.Error, "ERROR: {0}\n", ex.Message);
                    _ = DestroyWindow(wnd);
                }

                return 0;
            }
        }

        return DefWindowProc(wnd, msg, wParam, lParam);
    }

    internal static ID3D12GraphicsCommandList* BeginCommandList()
    {
        CHECK_HR(g_CommandList.Get()->Reset(g_CommandAllocators[(int)(g_FrameIndex)].Get(), null));
        return g_CommandList.Get();
    }

    internal static void EndCommandList(ID3D12GraphicsCommandList* cmdList)
    {
        _ = cmdList->Close();

        ID3D12CommandList* genericCmdList = (ID3D12CommandList*)(cmdList);
        g_CommandQueue.Get()->ExecuteCommandLists(1, &genericCmdList);

        WaitGPUIdle(g_FrameIndex);
    }

    internal static void PrintLogo()
    {
        _ = wprintf("{0}\n", WINDOW_TITLE);
    }

    internal static void PrintHelp()
    {
        _ = wprintf(
            "Command line syntax:\n" +
            "-h, --Help   Print this information\n" +
            "-l, --List   Print list of GPUs\n" +
            "-g S, --GPU S   Select GPU with name containing S\n" +
            "-i N, --GPUIndex N   Select GPU index N\n" +
            "-t, --Test   Run tests and exit\n"
        );
    }

    [SupportedOSPlatform("windows10.0")]
    internal static int MainWindow()
    {
        WNDCLASSEXW wndClass;

        fixed (char* pClassName = CLASS_NAME)
        {
            wndClass = new WNDCLASSEXW {
                cbSize = __sizeof<WNDCLASSEXW>(),
                style = CS_VREDRAW | CS_HREDRAW | CS_DBLCLKS,
                hbrBackground = HBRUSH.NULL,
                hCursor = LoadCursor(HINSTANCE.NULL, IDC_ARROW),
                hIcon = LoadIcon(HINSTANCE.NULL, IDI_APPLICATION),
                hInstance = g_Instance,
                lpfnWndProc = &WndProc,
                lpszClassName = pClassName,
            };
        }

        ushort classR = 0;
        MSG msg = new MSG();

        try
        {
            classR = RegisterClassEx(&wndClass);
            Debug.Assert(classR != 0);

            uint style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX | WS_VISIBLE;
            uint exStyle = 0;

            RECT rect = new RECT {
                left = 0,
                top = 0,
                right = SIZE_X,
                bottom = SIZE_Y,
            };
            _ = AdjustWindowRectEx(&rect, style, FALSE, exStyle);

            fixed (char* pClassName = CLASS_NAME)
            fixed (char* pWindowTitle = WINDOW_TITLE)
            {
                g_Wnd = CreateWindowEx(
                    exStyle,
                    pClassName,
                    pWindowTitle,
                    style,
                    CW_USEDEFAULT, CW_USEDEFAULT,
                    (rect.right - rect.left),
                    (rect.bottom - rect.top),
                    HWND.NULL,
                    HMENU.NULL,
                    g_Instance,
                    null
                );
                Debug.Assert(g_Wnd != HWND.NULL);
            }

            InitD3D();
            g_TimeOffset = GetTickCount64();

            // Execute tests and close program

            if (g_CommandLineParameters.m_Test)
            {
                ExecuteTests(benchmark: false);
                _ = PostMessageW(g_Wnd, WM_CLOSE, 0, 0);
            }

            if (g_CommandLineParameters.m_Benchmark)
            {
                ExecuteTests(benchmark: true);
                _ = PostMessageW(g_Wnd, WM_CLOSE, 0, 0);
            }

            for (; ; )
            {
                if (PeekMessage(&msg, HWND.NULL, 0, 0, PM_REMOVE))
                {
                    if (msg.message == WM_QUIT)
                    {
                        break;
                    }

                    _ = TranslateMessage(&msg);
                    _ = DispatchMessageW(&msg);
                }
                else
                {
                    ulong newTimeValue = GetTickCount64() - g_TimeOffset;
                    g_TimeDelta = (float)(newTimeValue - g_TimeValue) * 0.001f;

                    g_TimeValue = newTimeValue;
                    g_Time = (float)(newTimeValue) * 0.001f;

                    Update();
                    Render();
                }
            }
        }
        finally
        {
            if (classR != 0)
            {
                fixed (char* pClassName = CLASS_NAME)
                {
                    _ = UnregisterClassW(pClassName, g_Instance);
                }
            }
        }

        return (int)msg.wParam;
    }

    [SupportedOSPlatform("windows10.0")]
    internal static int Main2(string[] args)
    {
        PrintLogo();

        if (!g_CommandLineParameters.Parse(args))
        {
            _ = wprintf("ERROR: Invalid command line syntax.\n");
            PrintHelp();
            return (int)(ExitCode.CommandLineError);
        }

        if (g_CommandLineParameters.m_Help)
        {
            PrintHelp();
            return (int)(ExitCode.Help);
        }

        DXGIUsage* DXGIUsage = cxx_new<DXGIUsage>();
        DXGIUsage->Init();
        g_DXGIUsage = DXGIUsage;

        if (g_CommandLineParameters.m_List)
        {
            DXGIUsage->PrintAdapterList();
            return (int)(ExitCode.GPUList);
        }

        return MainWindow();
    }

    [SupportedOSPlatform("windows10.0")]
    internal static int wmain(string[] args)
    {
        try
        {
            return Main2(args);
        }
        catch (Exception ex)
        {
            _ = fwprintf(Console.Error, "ERROR: {0}\n", ex.Message);
            return (int)(ExitCode.RuntimeError);
        }
    }

#pragma warning disable CS0649
    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_RenderTargets_e__FixedBuffer
    {
        public ComPtr<ID3D12Resource> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_CommandAllocators_e__FixedBuffer
    {
        public ComPtr<ID3D12CommandAllocator> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_Fences_e__FixedBuffer
    {
        public ComPtr<ID3D12Fence> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_FenceValues_e__FixedBuffer
    {
        public ulong e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_CbPerObjectUploadHeapAllocations_e__FixedBuffer
    {
        public Pointer<D3D12MA_Allocation> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_CbPerObjectUploadHeaps_e__FixedBuffer
    {
        public ComPtr<ID3D12Resource> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_CbPerObjectAddress_e__FixedBuffer
    {
        public Pointer e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_MainDescriptorHeap_e__FixedBuffer
    {
        public ComPtr<ID3D12DescriptorHeap> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_ConstantBufferUploadHeap_e__FixedBuffer
    {
        public ComPtr<ID3D12Resource> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_ConstantBufferUploadAllocation_e__FixedBuffer
    {
        public Pointer<D3D12MA_Allocation> e0;
    }

    [InlineArray((int)(FRAME_BUFFER_COUNT))]
    internal partial struct _g_ConstantBufferAddress_e__FixedBuffer
    {
        public Pointer e0;
    }
#pragma warning restore CS0649
}
