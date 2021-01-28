// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TerraFX.Interop
{
    /// <summary>General statistics from the current state of the allocator.</summary>
    public struct Stats
    {
        /// <summary>Total statistics from all heap types.</summary>
        public StatInfo Total;

        /// <summary>One StatInfo for each type of heap located at the following indices: 0 - DEFAULT, 1 - UPLOAD, 2 - READBACK.</summary>
        [NativeTypeName("StatInfo HeapType[HEAP_TYPE_COUNT]")]
        public __Stats_e__FixedBuffer HeapType;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __Stats_e__FixedBuffer
        {
            public StatInfo _HeapType0;
            public StatInfo _HeapType1;
            public StatInfo _HeapType2;

            public ref StatInfo this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref AsSpan()[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<StatInfo> AsSpan() => MemoryMarshal.CreateSpan(ref _HeapType0, 3);
        }
    }
}
