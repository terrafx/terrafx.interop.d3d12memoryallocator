// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    /// <summary>General statistics from the current state of the allocator.</summary>
    public struct D3D12MA_Stats
    {
        /// <summary>Total statistics from all heap types.</summary>
        public D3D12MA_StatInfo Total;

        /// <summary>One StatInfo for each type of heap located at the following indices: 0 - DEFAULT, 1 - UPLOAD, 2 - READBACK, 3 - CUSTOM.</summary>
        [NativeTypeName("StatInfo HeapType[HEAP_TYPE_COUNT]")]
        public __Stats_e__FixedBuffer HeapType;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __Stats_e__FixedBuffer
        {
            public D3D12MA_StatInfo e0;
            public D3D12MA_StatInfo e1;
            public D3D12MA_StatInfo e2;
            public D3D12MA_StatInfo e3;

            public ref D3D12MA_StatInfo this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref AsSpan()[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe Span<D3D12MA_StatInfo> AsSpan()
            {
                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((sizeof(__Stats_e__FixedBuffer) / sizeof(D3D12MA_StatInfo)) == D3D12MA_HEAP_TYPE_COUNT) && ((sizeof(__Stats_e__FixedBuffer) % sizeof(D3D12MA_StatInfo)) == 0));

                return MemoryMarshal.CreateSpan(ref e0, (int)D3D12MA_HEAP_TYPE_COUNT);
            }
        }
    }
}
