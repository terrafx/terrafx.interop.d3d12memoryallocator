// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Runtime.InteropServices;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.DXGI_ADAPTER_FLAG;
using static TerraFX.Interop.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.D3D12_FENCE_FLAGS;
using static TerraFX.Interop.ALLOCATOR_FLAGS;

namespace TerraFX.Interop.UnitTests
{
    internal sealed unsafe class TestRunner : IDisposable
    {
        private readonly IDXGIFactory4* _dxgiFactory;
        private readonly IDXGIAdapter* _dxgiAdapter;
        private readonly ID3D12Device* _d3dDevice;
        private readonly ID3D12CommandQueue* _commandQueue;
        private readonly ID3D12CommandAllocator* _commandAllocator;
        private readonly ID3D12Fence* _fence;
        private ulong _fenceValue;
        private readonly IntPtr _fenceEvent;
        private readonly ID3D12GraphicsCommandList* _graphicsCommandList;
        private readonly Allocator* _allocator;
        private readonly ALLOCATION_CALLBACKS* _allocs;

        public TestRunner()
        {
            _dxgiFactory = CreateDxgiFactory();
            _dxgiAdapter = GetAdapter(_dxgiFactory);
            _d3dDevice = CreateD3DDevice(_dxgiAdapter);
            _commandQueue = CreateCommandQueue(_d3dDevice);
            _commandAllocator = CreateCommandAllocator(_d3dDevice);
            _fence = CreateFence(_d3dDevice);
            _fenceValue = 0;
            _fenceEvent = CreateFenceEvent();
            _graphicsCommandList = CreateGraphicsCommandLists(_d3dDevice, _commandAllocator);
            _allocator = CreateAllocator(_d3dDevice, _dxgiAdapter);
            _allocs = CreateAllocs();

            static IDXGIFactory4* CreateDxgiFactory()
            {
                uint dxgiFactoryFlags = TryEnableDebugLayer() ? DXGI_CREATE_FACTORY_DEBUG : 0u;

                IDXGIFactory4* dxgiFactory;

                ThrowIfFailed(CreateDXGIFactory2(dxgiFactoryFlags, __uuidof<IDXGIFactory4>(), (void**)&dxgiFactory));

                return dxgiFactory;
            }

            static bool TryEnableDebugLayer()
            {
#if DEBUG
                using ComPtr<ID3D12Debug> debugController = null;

                if (SUCCEEDED(D3D12GetDebugInterface(__uuidof<ID3D12Debug>(), (void**)&debugController)))
                {
                    debugController.Get()->EnableDebugLayer();
                    return true;
                }
#endif

                return false;
            }

            static IDXGIAdapter* GetAdapter(IDXGIFactory4* pFactory)
            {
                IDXGIAdapter1* adapter;

                for (var adapterIndex = 0u; DXGI_ERROR_NOT_FOUND != pFactory->EnumAdapters1(adapterIndex, &adapter); ++adapterIndex)
                {
                    DXGI_ADAPTER_DESC1 desc;
                    _ = adapter->GetDesc1(&desc);

                    if ((desc.Flags & (uint)DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
                    {
                        continue;
                    }

                    if (SUCCEEDED(D3D12CreateDevice((IUnknown*)adapter, D3D_FEATURE_LEVEL_11_0, __uuidof<ID3D12Device>(), null)))
                    {
                        break;
                    }
                }

                return (IDXGIAdapter*)adapter;
            }

            static ID3D12Device* CreateD3DDevice(IDXGIAdapter* pAdapter)
            {
                ID3D12Device* d3dDevice;

                ThrowIfFailed(D3D12CreateDevice((IUnknown*)pAdapter, D3D_FEATURE_LEVEL_11_0, __uuidof<ID3D12Device>(), (void**)&d3dDevice));

                return d3dDevice;
            }

            static ID3D12CommandQueue* CreateCommandQueue(ID3D12Device* pDevice)
            {
                D3D12_COMMAND_QUEUE_DESC queueDesc = default;
                ID3D12CommandQueue* commandQueue;

                ThrowIfFailed(pDevice->CreateCommandQueue(&queueDesc, __uuidof<ID3D12CommandQueue>(), (void**)&commandQueue));

                return commandQueue;
            }

            static ID3D12CommandAllocator* CreateCommandAllocator(ID3D12Device* pDevice)
            {
                ID3D12CommandAllocator* commandAllocator;

                ThrowIfFailed(pDevice->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, __uuidof<ID3D12CommandAllocator>(), (void**)&commandAllocator));

                return commandAllocator;
            }

            static ID3D12Fence* CreateFence(ID3D12Device* pDevice)
            {
                ID3D12Fence* fence;

                ThrowIfFailed(pDevice->CreateFence(InitialValue: 0, D3D12_FENCE_FLAG_NONE, __uuidof<ID3D12Fence>(), (void**)&fence));

                return fence;
            }

            static IntPtr CreateFenceEvent()
            {
                var fenceEvent = CreateEventW(lpEventAttributes: null, bManualReset: FALSE, bInitialState: FALSE, lpName: null);

                if (fenceEvent == IntPtr.Zero)
                {
                    var hr = Marshal.GetHRForLastWin32Error();
                    Marshal.ThrowExceptionForHR(hr);
                }

                return fenceEvent;
            }

            static ID3D12GraphicsCommandList* CreateGraphicsCommandLists(ID3D12Device* pDevice, ID3D12CommandAllocator* pCommandAllocator)
            {
                ID3D12GraphicsCommandList* graphicsCommandList;

                ThrowIfFailed(pDevice->CreateCommandList(nodeMask: 0, D3D12_COMMAND_LIST_TYPE_DIRECT, pCommandAllocator, null, __uuidof<ID3D12GraphicsCommandList>(), (void**)&graphicsCommandList));

                ThrowIfFailed(graphicsCommandList->Close());

                return graphicsCommandList;
            }

            static Allocator* CreateAllocator(ID3D12Device* pDevice, IDXGIAdapter* pAdapter)
            {
                Allocator* allocator;

                ALLOCATOR_DESC allocatorDesc = default;
                allocatorDesc.Flags = ALLOCATOR_FLAG_NONE;
                allocatorDesc.pDevice = pDevice;
                allocatorDesc.pAdapter = pAdapter;

                ThrowIfFailed(D3D12MemoryAllocator.CreateAllocator(&allocatorDesc, &allocator));

                return allocator;
            }

            static ALLOCATION_CALLBACKS* CreateAllocs()
            {
                [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
                static extern void* _aligned_malloc([NativeTypeName("size_t")] nuint _Size, [NativeTypeName("size_t")] nuint _Alignment);

                [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
                static extern void _aligned_free(void* _Block);

                static void* Allocate(nuint Size, nuint Alignment, void* _  /*pUserData*/)
                {
                    return _aligned_malloc(Size, Alignment);
                }

                static void Free(void* pMemory, void* _ /*pUserData*/)
                {
                    _aligned_free(pMemory);
                }

                ALLOCATION_CALLBACKS* allocs = (ALLOCATION_CALLBACKS*)Marshal.AllocHGlobal(sizeof(ALLOCATION_CALLBACKS));

                allocs->pAllocate = &Allocate;
                allocs->pFree = &Free;
                allocs->pUserData = null;

                return allocs;
            }
        }

        public void CreateContext(out TestContext ctx)
        {
            ctx = default;
            ctx.allocationCallbacks = _allocs;
            ctx.device = _d3dDevice;
            ctx.allocator = _allocator;
            ctx.allocatorFlags = ALLOCATOR_FLAG_NONE;
        }

        public ID3D12GraphicsCommandList* BeginCommandList()
        {
            ThrowIfFailed(_graphicsCommandList->Reset(_commandAllocator, null));

            return _graphicsCommandList;
        }

        public void EndCommandList(ID3D12GraphicsCommandList** cmdList)
        {
            (*cmdList)->Close();

            _commandQueue->ExecuteCommandLists(1, (ID3D12CommandList**)cmdList);

            _fenceValue++;
            ThrowIfFailed(_commandQueue->Signal(_fence, _fenceValue));

            if (_fence->GetCompletedValue() < _fenceValue)
            {
                ThrowIfFailed(_fence->SetEventOnCompletion(_fenceValue, _fenceEvent));

                WaitForSingleObject(_fenceEvent, INFINITE);
            }
        }

        public void Dispose()
        {
            _dxgiFactory->Release();
            _dxgiAdapter->Release();
            _d3dDevice->Release();
            _commandQueue->Release();
            _commandAllocator->Release();
            _fence->Release();
            CloseHandle(_fenceEvent);
            _graphicsCommandList->Release();
            Marshal.FreeHGlobal((IntPtr)_allocs);
        }

        private static void ThrowIfFailed(HRESULT hr)
        {
            if (FAILED(hr))
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
