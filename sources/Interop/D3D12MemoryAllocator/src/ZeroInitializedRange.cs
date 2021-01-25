// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using static TerraFX.Interop.D3D12MemoryAllocator;

using UINT64 = System.UInt64;
using BOOL = System.Int32;

namespace TerraFX.Interop
{
    /// <summary>
    /// Keeps track of the range of bytes that are surely initialized with zeros.
    /// Everything outside of it is considered uninitialized memory that may contain
    /// garbage data.
    /// <para>The range is left-inclusive.</para>
    /// </summary>
    internal struct ZeroInitializedRange
    {
        public void Reset(UINT64 size)
        {
            D3D12MA_ASSERT(size > 0);
            m_ZeroBeg = 0;
            m_ZeroEnd = size;
        }

        public BOOL IsRangeZeroInitialized(UINT64 beg, UINT64 end)
        {
            D3D12MA_ASSERT(beg < end);
            return (m_ZeroBeg <= beg && end <= m_ZeroEnd) ? 1 : 0;
        }

        public void MarkRangeAsUsed(UINT64 usedBeg, UINT64 usedEnd)
        {
            D3D12MA_ASSERT(usedBeg < usedEnd);
            // No new bytes marked.
            if (usedEnd <= m_ZeroBeg || m_ZeroEnd <= usedBeg)
            {
                return;
            }
            // All bytes marked.
            if (usedBeg <= m_ZeroBeg && m_ZeroEnd <= usedEnd)
            {
                m_ZeroBeg = m_ZeroEnd = 0;
            }
            // Some bytes marked.
            else
            {
                UINT64 remainingZeroBefore = usedBeg > m_ZeroBeg ? usedBeg - m_ZeroBeg : 0;
                UINT64 remainingZeroAfter = usedEnd < m_ZeroEnd ? m_ZeroEnd - usedEnd : 0;
                D3D12MA_ASSERT(remainingZeroBefore > 0 || remainingZeroAfter > 0);
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

        private UINT64 m_ZeroBeg, m_ZeroEnd;
    }
}
