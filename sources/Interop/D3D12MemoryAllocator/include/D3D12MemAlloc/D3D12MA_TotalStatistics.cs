// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.h in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_MEMORY_POOL;
using static TerraFX.Interop.DirectX.DXGI_MEMORY_SEGMENT_GROUP;

namespace TerraFX.Interop.DirectX;

/// <summary>General statistics from current state of the allocator - total memory usage across all memory heaps and segments.</summary>
/// <remarks>These are slower to calculate. Use for debugging purposes. See function <see cref="D3D12MA_Allocator.CalculateStatistics" />.</remarks>
public partial struct D3D12MA_TotalStatistics
{
    /// <summary>
    ///   <para>One element for each type of heap located at the following indices:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>0 = <see cref="D3D12_HEAP_TYPE_DEFAULT" /></description>
    ///     </item>
    ///     <item>
    ///       <description>1 = <see cref="D3D12_HEAP_TYPE_UPLOAD" /></description>
    ///     </item>
    ///     <item>
    ///       <description>2 = <see cref="D3D12_HEAP_TYPE_READBACK" /></description>
    ///     </item>
    ///     <item>
    ///       <description>3 = <see cref="D3D12_HEAP_TYPE_CUSTOM" /></description>
    ///     </item>
    ///   </list>
    /// </summary>
    [NativeTypeName("D3D12MA::DetailedStatistics[4]")]
    public _HeapType_e__FixedBuffer HeapType;

    /// <summary>
    ///   <para>One element for each memory segment group located at the following indices:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>0 = <see cref="DXGI_MEMORY_SEGMENT_GROUP_LOCAL" /></description>
    ///     </item>
    ///     <item>
    ///       <description>1 = <see cref="DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL" /></description>
    ///     </item>
    ///   </list>
    /// </summary>
    /// <remarks>
    ///   <para>Meaning of these segment groups is:</para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>
    ///         <para>When <c>IsUMA() == FALSE</c> (discrete graphics card):</para>
    ///         <list type="bullet">
    ///           <item>
    ///             <description><see cref="DXGI_MEMORY_SEGMENT_GROUP_LOCAL" /> (index 0) represents GPU memory (resources allocated in <see cref="D3D12_HEAP_TYPE_DEFAULT" /> or <see cref="D3D12_MEMORY_POOL_L1" />).</description>
    ///           </item>
    ///           <item>
    ///             <description><see cref="DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL" /> (index 1) represents system memory (resources allocated in <see cref="D3D12_HEAP_TYPE_UPLOAD" />, <see cref="D3D12_HEAP_TYPE_READBACK" />, or <see cref="D3D12_MEMORY_POOL_L0" />).</description>
    ///           </item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         <para>When <c>IsUMA() == TRUE</c> (integrated graphics chip):</para>
    ///         <list type="bullet">
    ///           <item>
    ///             <description><see cref="DXGI_MEMORY_SEGMENT_GROUP_LOCAL" /> (index 0) represents memory shared for all the resources.</description>
    ///           </item>
    ///           <item>
    ///             <description><see cref="DXGI_MEMORY_SEGMENT_GROUP_NON_LOCAL" /> (index 1) is unused and always 0.</description>
    ///           </item>
    ///         </list>
    ///       </description>
    ///     </item>
    ///   </list>
    /// </remarks>
    [NativeTypeName("D3D12MA::DetailedStatistics[2]")]
    public _MemorySegmentGroup_e__FixedBuffer MemorySegmentGroup;

    /// <summary>Total statistics from all memory allocated from D3D12.</summary>
    public D3D12MA_DetailedStatistics Total;

    public partial struct _HeapType_e__FixedBuffer
    {
        public D3D12MA_DetailedStatistics e0;
        public D3D12MA_DetailedStatistics e1;
        public D3D12MA_DetailedStatistics e2;
        public D3D12MA_DetailedStatistics e3;

        [UnscopedRef]
        public ref D3D12MA_DetailedStatistics this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref AsSpan()[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public Span<D3D12MA_DetailedStatistics> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 4);
    }

    public partial struct _MemorySegmentGroup_e__FixedBuffer
    {
        public D3D12MA_DetailedStatistics e0;
        public D3D12MA_DetailedStatistics e1;

        [UnscopedRef]
        public ref D3D12MA_DetailedStatistics this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref AsSpan()[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public Span<D3D12MA_DetailedStatistics> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 2);
    }
}
