// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.DirectX;
using static TerraFX.Interop.DirectX.DXGI;
using static TerraFX.Interop.DirectX.DXGI_ADAPTER_FLAG;
using static TerraFX.Interop.DirectX.UnitTests.D3D12MemAllocTests;
using static TerraFX.Interop.Windows.Windows;

namespace TerraFX.Interop.DirectX.UnitTests;

internal unsafe partial struct DXGIUsage : IDisposable
{
    private ComPtr<IDXGIFactory4> m_DXGIFactory;

    public void Dispose()
    {
        m_DXGIFactory.Dispose();
    }

    public void Init()
    {
        g_Instance = (HINSTANCE)(GetModuleHandleW(null));
        _ = CoInitialize(null);

        fixed (ComPtr<IDXGIFactory4>* ppDXGIFactory = &m_DXGIFactory)
        {
            CHECK_HR(CreateDXGIFactory1(__uuidof<IDXGIFactory4>(), (void**)(ppDXGIFactory)));
        }
    }

    public readonly IDXGIFactory4* GetDXGIFactory()
    {
        return m_DXGIFactory.Get();
    }

    public readonly void PrintAdapterList()
    {
        uint index = 0;
        using ComPtr<IDXGIAdapter1> adapter = new ComPtr<IDXGIAdapter1>();

        while (m_DXGIFactory.Get()->EnumAdapters1(index, (IDXGIAdapter1**)(&adapter)) != DXGI_ERROR_NOT_FOUND)
        {
            DXGI_ADAPTER_DESC1 desc;
            _ = adapter.Get()->GetDesc1(&desc);

            bool isSoftware = (desc.Flags & (uint)(DXGI_ADAPTER_FLAG_SOFTWARE)) != 0;
            string suffix = isSoftware ? " (SOFTWARE)" : "";
            _ = wprintf("Adapter {0}: {1}{2}\n", index, ((ReadOnlySpan<char>)(g_AdapterDesc.Description)).ToString(), suffix);

            _ = adapter.Reset();
            ++index;
        }
    }

    // If failed, returns null pointer.
    public readonly ComPtr<IDXGIAdapter1> CreateAdapter([NativeTypeName("const GPUSelection &")] in GPUSelection GPUSelection)
    {
        using ComPtr<IDXGIAdapter1> adapter = new ComPtr<IDXGIAdapter1>();

        if (GPUSelection.Index != uint.MaxValue)
        {
            // Cannot specify both index and name.
            if (!string.IsNullOrEmpty(GPUSelection.Substring))
            {
                return adapter;
            }

            CHECK_HR(m_DXGIFactory.Get()->EnumAdapters1(GPUSelection.Index, (IDXGIAdapter1**)(&adapter)));
            return new ComPtr<IDXGIAdapter1>(adapter);
        }

        if (!string.IsNullOrEmpty(GPUSelection.Substring))
        {
            using ComPtr<IDXGIAdapter1> tmpAdapter = new ComPtr<IDXGIAdapter1>();

            for (uint i = 0; m_DXGIFactory.Get()->EnumAdapters1(i, (IDXGIAdapter1**)(&tmpAdapter)) != DXGI_ERROR_NOT_FOUND; ++i)
            {
                DXGI_ADAPTER_DESC1 desc;
                _ = tmpAdapter.Get()->GetDesc1(&desc);

                fixed (char* pszSrch = GPUSelection.Substring)
                {
                    if (StrStrIW(&desc.Description[0], pszSrch) != null)
                    {
                        // Second matching adapter found - error.
                        if (adapter.Get() != null)
                        {
                            _ = adapter.Reset();
                            return adapter;
                        }
                        // First matching adapter found.
                        *(&adapter) = tmpAdapter;
                    }
                    else
                    {
                        _ = tmpAdapter.Reset();
                    }
                }
            }

            // Found or not, return it.
            return new ComPtr<IDXGIAdapter1>(adapter);
        }

        // Select first one.
        _ = m_DXGIFactory.Get()->EnumAdapters1(0, (IDXGIAdapter1**)(&adapter));
        return new ComPtr<IDXGIAdapter1>(adapter);
    }
}
