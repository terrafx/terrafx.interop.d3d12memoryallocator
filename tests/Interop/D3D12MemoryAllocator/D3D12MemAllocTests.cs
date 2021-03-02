// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics;
using NUnit.Framework;
using static TerraFX.Interop.D3D12MemoryAllocator;
using static TerraFX.Interop.Windows;

namespace TerraFX.Interop.UnitTests
{
    [TestFixture]
    [Platform("win")]
    internal unsafe static partial class D3D12MemAllocTests
    {
        // POSITION
        private static ReadOnlySpan<sbyte> POSITION_SEMANTIC_NAME => new sbyte[] { 0x50, 0x4F, 0x53, 0x49, 0x54, 0x49, 0x4F, 0x4E, 0x00 };

        // TEXCOORD
        private static ReadOnlySpan<sbyte> TEXCOORD_SEMANTIC_NAME => new sbyte[] { 0x54, 0x45, 0x58, 0x43, 0x4F, 0x4F, 0x52, 0x44, 0x00 };

        private static TestContext testCtx;

        [Test]
        [Explicit]
        public static void RenderTest()
        {
            ShowWindow(g_Wnd, SW_SHOWNORMAL);

            MSG msg;

            for (;;)
            {
                if (PeekMessage(&msg, IntPtr.Zero, 0, 0, PM_REMOVE) != 0)
                {
                    if (msg.message == WM_QUIT)
                    {
                        break;
                    }

                    TranslateMessage(&msg);
                    DispatchMessage(&msg);
                }
                else
                {
                    ulong newTimeValue = GetTickCount64() - g_TimeOffset;
                    g_TimeDelta = (float)(newTimeValue - g_TimeValue) * 0.001f;
                    g_TimeValue = newTimeValue;
                    g_Time = (float)newTimeValue * 0.001f;

                    Update();
                    Render();
                }
            }
        }

        [OneTimeSetUp]
        public static void Setup()
        {
            g_Instance = GetModuleHandle(null);
            CoInitializeEx(null, 0);

            ushort classR;

            fixed (char* className = CLASS_NAME)
            fixed (char* windowTitle = WINDOW_TITLE)
            {
                WNDCLASSEXW wndClass;
                ZeroMemory(&wndClass, (nuint)sizeof(WNDCLASSEXW));
                wndClass.cbSize = (uint)sizeof(WNDCLASSEXW);
                wndClass.style = CS_VREDRAW | CS_HREDRAW | CS_DBLCLKS;
                wndClass.hbrBackground = IntPtr.Zero;
                wndClass.hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW);
                wndClass.hIcon = LoadIcon(IntPtr.Zero, IDI_APPLICATION);
                wndClass.hInstance = g_Instance;
                wndClass.lpfnWndProc = &WndProc;
                wndClass.lpszClassName = (ushort*)className;

                classR = RegisterClassEx(&wndClass);
                Debug.Assert(classR != 0);

                uint style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX;
                uint exStyle = 0;

                RECT rect = new RECT {
                    left = 0,
                    top = 0,
                    right = SIZE_X,
                    bottom = SIZE_Y
                };

                AdjustWindowRectEx(&rect, style, FALSE, exStyle);

                g_Wnd = CreateWindowEx(
                    exStyle,
                    (ushort*)className,
                    (ushort*)windowTitle,
                    style,
                    CW_USEDEFAULT, CW_USEDEFAULT,
                    rect.right - rect.left, rect.bottom - rect.top,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    g_Instance,
                    null
                );
            }

            Debug.Assert(g_Wnd != IntPtr.Zero);

            testCtx = new TestContext {
                allocationCallbacks = g_AllocationCallbacks,
                device = g_Device,
                allocator = g_Allocator,
                allocatorFlags = g_AllocatorFlags,
            };
        }

        [OneTimeTearDown]
        public static void Teardown()
        {
            if (g_Wnd != IntPtr.Zero)
            {
                PostMessage(g_Wnd, WM_CLOSE, 0, 0);
            }

            g_Device.Dispose();
            g_SwapChain.Dispose();
            g_CommandQueue.Dispose();
            g_RtvDescriptorHeap.Dispose();

            for (uint i = 0; i < FRAME_BUFFER_COUNT; i++)
            {
                g_RenderTargets[(int)i].Dispose();
            }

            for (uint i = 0; i < FRAME_BUFFER_COUNT; i++)
            {
                g_CommandAllocators[(int)i].Dispose();
            }

            g_CommandList.Dispose();

            for (uint i = 0; i < FRAME_BUFFER_COUNT; i++)
            {
                g_Fences[(int)i].Dispose();
            }

            g_PipelineStateObject.Dispose();
            g_RootSignature.Dispose();
            g_VertexBuffer.Dispose();
            g_IndexBuffer.Dispose();
            g_DepthStencilBuffer.Dispose();
            g_DepthStencilDescriptorHeap.Dispose();

            for (uint i = 0; i < FRAME_BUFFER_COUNT; i++)
            {
                g_CbPerObjectUploadHeaps[(int)i].Dispose();
            }

            for (uint i = 0; i < FRAME_BUFFER_COUNT; i++)
            {
                g_MainDescriptorHeap[(int)i].Dispose();
            }

            for (uint i = 0; i < FRAME_BUFFER_COUNT; i++)
            {
                g_ConstantBufferUploadHeap[(int)i].Dispose();
            }

            g_Texture.Dispose();
        }

        [Test]
        public static void TestVirtualBlocks() => TestVirtualBlocks(in testCtx);

        [Test]
        public static void TestFrameIndexAndJson() => TestFrameIndexAndJson(in testCtx);

        [Test]
        public static void TestCommittedResourcesAndJson() => TestCommittedResourcesAndJson(in testCtx);

        [Test]
        public static void TestCustomHeapFlags() => TestCustomHeapFlags(in testCtx);

        [Test]
        public static void TestPlacedResources() => TestPlacedResources(in testCtx);

        [Test]
        public static void TestOtherComInterface() => TestOtherComInterface(in testCtx);

        [Test]
        public static void TestCustomPools() => TestCustomPools(in testCtx);

        [Test]
        public static void TestDefaultPoolMinBytes() => TestDefaultPoolMinBytes(in testCtx);

        [Test]
        public static void TestAliasingMemory() => TestAliasingMemory(in testCtx);

        [Test]
        public static void TestMapping() => TestMapping(in testCtx);

        [Test]
        public static void TestStats() => TestStats(in testCtx);

        [Test]
        public static void TestTransfer() => TestTransfer(in testCtx);

        [Test]
        public static void TestZeroInitialized() => TestZeroInitialized(in testCtx);

        [Test]
        public static void TestMultithreading() => TestMultithreading(in testCtx);

        [Test]
        public static void TestDevice4() => TestDevice4(in testCtx);

        [Test]
        public static void TestDevice8() => TestDevice8(in testCtx);
    }
}
