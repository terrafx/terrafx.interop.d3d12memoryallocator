// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from Tests.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;

namespace TerraFX.Interop.UnitTests
{
    internal unsafe struct D3d12maObjDeleter<T>
        where T : unmanaged
    {
        public static void Invoke(T* obj)
        {
            if (obj != null)
            {
                if (typeof(T) == typeof(D3D12MA_Pool))
                {
                    ((D3D12MA_Pool*)obj)->Release();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
