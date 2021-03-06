// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator commit 5457bcdaee73ee1f3fe6027bbabf959119f88b3d
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using static TerraFX.Interop.D3D12MemAlloc;

namespace TerraFX.Interop
{
    /// <summary>
    /// Keeps track of the range of bytes that are surely initialized with zeros. Everything outside of it is considered uninitialized memory that may contain garbage data.
    /// <para>The range is left-inclusive.</para>
    /// </summary>
    internal struct D3D12MA_ZeroInitializedRange
    {
        [NativeTypeName("UINT64")]
        private ulong m_ZeroBeg, m_ZeroEnd;

        public void Reset([NativeTypeName("UINT64")] ulong size)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (size > 0));

            m_ZeroBeg = 0;
            m_ZeroEnd = size;
        }

        [return: NativeTypeName("BOOL")]
        public readonly int IsRangeZeroInitialized([NativeTypeName("UINT64")] ulong beg, [NativeTypeName("UINT64")] ulong end)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (beg < end));
            return ((m_ZeroBeg <= beg) && (end <= m_ZeroEnd)) ? 1 : 0;
        }

        public void MarkRangeAsUsed([NativeTypeName("UINT64")] ulong usedBeg, [NativeTypeName("UINT64")] ulong usedEnd)
        {
            D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && (usedBeg < usedEnd));

            // No new bytes marked.
            if ((usedEnd <= m_ZeroBeg) || (m_ZeroEnd <= usedBeg))
            {
                return;
            }

            // All bytes marked.
            if ((usedBeg <= m_ZeroBeg) && (m_ZeroEnd <= usedEnd))
            {
                m_ZeroBeg = m_ZeroEnd = 0;
            }
            else
            {
                // Some bytes marked.

                ulong remainingZeroBefore = (usedBeg > m_ZeroBeg) ? (usedBeg - m_ZeroBeg) : 0;
                ulong remainingZeroAfter = (usedEnd < m_ZeroEnd) ? (m_ZeroEnd - usedEnd) : 0;

                D3D12MA_ASSERT((D3D12MA_DEBUG_LEVEL > 0) && ((remainingZeroBefore > 0) || (remainingZeroAfter > 0)));

                if (remainingZeroBefore > remainingZeroAfter)
                {
                    m_ZeroEnd = usedBeg;
                }
                else
                {
                    m_ZeroBeg = usedEnd;
                }
            }
        }
    }
}
