// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>Calculated statistics of memory usage in entire allocator.</summary>
    public struct D3D12MA_StatInfo
    {
        /// <summary>Number of memory blocks (heaps) allocated.</summary>
        [NativeTypeName("UINT")]
        public uint BlockCount;

        /// <summary>Number of D3D12MA::Allocation objects allocated.</summary>
        [NativeTypeName("UINT")]
        public uint AllocationCount;

        /// <summary>Number of free ranges of memory between allocations.</summary>
        [NativeTypeName("UINT")]
        public uint UnusedRangeCount;

        /// <summary>Total number of bytes occupied by all allocations.</summary>
        [NativeTypeName("UINT64")]
        public ulong UsedBytes;

        /// <summary>Total number of bytes occupied by unused ranges.</summary>
        [NativeTypeName("UINT64")]
        public ulong UnusedBytes;

        [NativeTypeName("UINT64")]
        public ulong AllocationSizeMin;

        [NativeTypeName("UINT64")]
        public ulong AllocationSizeAvg;

        [NativeTypeName("UINT64")]
        public ulong AllocationSizeMax;

        [NativeTypeName("UINT64")]
        public ulong UnusedRangeSizeMin;

        [NativeTypeName("UINT64")]
        public ulong UnusedRangeSizeAvg;

        [NativeTypeName("UINT64")]
        public ulong UnusedRangeSizeMax;
    }
}
