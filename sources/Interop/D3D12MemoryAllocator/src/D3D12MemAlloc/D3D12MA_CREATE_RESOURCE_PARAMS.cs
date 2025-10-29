// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v3.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Simple variant data structure to hold all possible variations of <see cref="ID3D12Device.CreateCommittedResource" /> and <see cref="ID3D12Device.CreatePlacedResource" /> arguments.</summary>
internal unsafe partial struct D3D12MA_CREATE_RESOURCE_PARAMS
{
    public VARIANT Variant;

    private _Anonymous1_e__Union Anonymous1;

    private _Anonymous2_e__Union Anonymous2;

    private readonly D3D12_CLEAR_VALUE* pOptimizedClearValue;

    private readonly uint NumCastableFormats;

    private readonly DXGI_FORMAT* pCastableFormats;

    public D3D12MA_CREATE_RESOURCE_PARAMS([NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue)
    {
        Variant = VARIANT.WITH_STATE;
        Anonymous1.pResourceDesc = pResourceDesc;
        Anonymous2.InitialResourceState = InitialResourceState;
        this.pOptimizedClearValue = pOptimizedClearValue;
    }

    public D3D12MA_CREATE_RESOURCE_PARAMS([NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue)
    {
        Variant = VARIANT.WITH_STATE_AND_DESC1;
        Anonymous1.pResourceDesc1 = pResourceDesc;
        Anonymous2.InitialResourceState = InitialResourceState;
        this.pOptimizedClearValue = pOptimizedClearValue;
    }

    public D3D12MA_CREATE_RESOURCE_PARAMS([NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats)
    {
        Variant = VARIANT.WITH_LAYOUT;
        Anonymous1.pResourceDesc1 = pResourceDesc;
        Anonymous2.InitialLayout = InitialLayout;
        this.pOptimizedClearValue = pOptimizedClearValue;
        this.NumCastableFormats = NumCastableFormats;
        this.pCastableFormats = pCastableFormats;
    }

    [return: NativeTypeName("const D3D12_RESOURCE_DESC *")]
    public readonly D3D12_RESOURCE_DESC* GetResourceDesc()
    {
        D3D12MA_ASSERT(Variant == VARIANT.WITH_STATE);
        return Anonymous1.pResourceDesc;
    }

    [UnscopedRef]
    [return: NativeTypeName("const D3D12_RESOURCE_DESC *&")]
    public ref D3D12_RESOURCE_DESC* AccessResourceDesc()
    {
        D3D12MA_ASSERT(Variant == VARIANT.WITH_STATE);
        return ref Anonymous1.pResourceDesc;
    }

    [return: NativeTypeName("const D3D12_RESOURCE_DESC *")]
    public readonly D3D12_RESOURCE_DESC* GetBaseResourceDesc()
    {
        // D3D12_RESOURCE_DESC1 can be cast to D3D12_RESOURCE_DESC by discarding the new members at the end.
        return Anonymous1.pResourceDesc;
    }

    public readonly D3D12_RESOURCE_STATES GetInitialResourceState()
    {
        D3D12MA_ASSERT(Variant < VARIANT.WITH_LAYOUT);
        return Anonymous2.InitialResourceState;
    }

    [return: NativeTypeName("const D3D12_CLEAR_VALUE *")]
    public readonly D3D12_CLEAR_VALUE* GetOptimizedClearValue()
    {
        return pOptimizedClearValue;
    }

    [return: NativeTypeName("const D3D12_RESOURCE_DESC1 *")]
    public readonly D3D12_RESOURCE_DESC1* GetResourceDesc1()
    {
        D3D12MA_ASSERT(Variant >= VARIANT.WITH_STATE_AND_DESC1);
        return Anonymous1.pResourceDesc1;
    }

    [UnscopedRef]
    [return: NativeTypeName("const D3D12_RESOURCE_DESC1 *&")]
    public ref D3D12_RESOURCE_DESC1* AccessResourceDesc1()
    {
        D3D12MA_ASSERT(Variant >= VARIANT.WITH_STATE_AND_DESC1);
        return ref Anonymous1.pResourceDesc1;
    }

    public readonly D3D12_BARRIER_LAYOUT GetInitialLayout()
    {
        D3D12MA_ASSERT(Variant >= VARIANT.WITH_LAYOUT);
        return Anonymous2.InitialLayout;
    }

    [return: NativeTypeName("UINT32")]
    public readonly uint GetNumCastableFormats()
    {
        D3D12MA_ASSERT(Variant >= VARIANT.WITH_LAYOUT);
        return NumCastableFormats;
    }

    [return: NativeTypeName("const DXGI_FORMAT *")]
    public readonly DXGI_FORMAT* GetCastableFormats()
    {
        D3D12MA_ASSERT(Variant >= VARIANT.WITH_LAYOUT);
        return pCastableFormats;
    }


    public enum VARIANT
    {
        INVALID = 0,
        WITH_STATE,
        WITH_STATE_AND_DESC1,
        WITH_LAYOUT
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct _Anonymous1_e__Union
    {
        [FieldOffset(0)]
        public D3D12_RESOURCE_DESC* pResourceDesc;

        [FieldOffset(0)]
        public D3D12_RESOURCE_DESC1* pResourceDesc1;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct _Anonymous2_e__Union
    {
        [FieldOffset(0)]
        public D3D12_RESOURCE_STATES InitialResourceState;

        [FieldOffset(0)]
        public D3D12_BARRIER_LAYOUT InitialLayout;
    }
}
