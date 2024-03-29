// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12MemAlloc.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace TerraFX.Interop.DirectX;

/// <summary>Keeps track of the range of bytes that are surely initialized with zeros. Everything outside of it is considered uninitialized memory that may contain garbage data.</summary>
/// <remarks>The range is left-inclusive.</remarks>
internal partial struct D3D12MA_ZeroInitializedRange
{
    [NativeTypeName("UINT64")]
    public ulong m_ZeroBeg;

    [NativeTypeName("UINT64")]
    public ulong m_ZeroEnd;

    public void Reset([NativeTypeName("UINT64")] ulong size)
    {
        D3D12MA_ASSERT(size > 0);
        m_ZeroBeg = 0;
        m_ZeroEnd = size;
    }

    public readonly BOOL IsRangeZeroInitialized([NativeTypeName("UINT64")] ulong beg, [NativeTypeName("UINT64")] ulong end)
    {
        D3D12MA_ASSERT(beg < end);
        return (m_ZeroBeg <= beg) && (end <= m_ZeroEnd);
    }

    public void MarkRangeAsUsed([NativeTypeName("UINT64")] ulong usedBeg, [NativeTypeName("UINT64")] ulong usedEnd)
    {
        D3D12MA_ASSERT(usedBeg < usedEnd);

        if ((usedEnd <= m_ZeroBeg) || (m_ZeroEnd <= usedBeg))
        {
            // No new bytes marked.
            return;
        }

        if ((usedBeg <= m_ZeroBeg) && (m_ZeroEnd <= usedEnd))
        {
            // All bytes marked.
            m_ZeroBeg = m_ZeroEnd = 0;
        }
        else
        {
            // Some bytes marked.

            ulong remainingZeroBefore = (usedBeg > m_ZeroBeg) ? (usedBeg - m_ZeroBeg) : 0;
            ulong remainingZeroAfter = (usedEnd < m_ZeroEnd) ? (m_ZeroEnd - usedEnd) : 0;

            D3D12MA_ASSERT((remainingZeroBefore > 0) || (remainingZeroAfter > 0));

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
