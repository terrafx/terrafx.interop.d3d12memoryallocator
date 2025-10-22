// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Common.h and Common.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;

namespace TerraFX.Interop.DirectX.UnitTests;

public readonly unsafe partial struct MyUniformRandomNumberGenerator
{
    [NativeTypeName("RandomNumberGenerator &")]
    private readonly RandomNumberGenerator* m_Gen;
    
    public MyUniformRandomNumberGenerator([NativeTypeName("RandomNumberGenerator &")] ref RandomNumberGenerator gen)
    {
        m_Gen = (RandomNumberGenerator*)(Unsafe.AsPointer(ref gen));
    }

    [return: NativeTypeName("uint32_t")]
    public static uint min()
    {
        return 0;
    }

    [return: NativeTypeName("uint32_t")]
    public static uint max()
    {
        return uint.MaxValue;
    }

    [return: NativeTypeName("uint32_t")]
    public readonly uint Invoke()
    {
        return m_Gen->Generate();
    }
}
