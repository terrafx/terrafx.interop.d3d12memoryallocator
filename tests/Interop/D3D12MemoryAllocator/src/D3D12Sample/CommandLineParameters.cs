// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from D3D12Sample.cpp in D3D12MemoryAllocator tag v2.0.1
// Original source is Copyright © Advanced Micro Devices, Inc. All rights reserved. Licensed under the MIT License (MIT).

using System;

namespace TerraFX.Interop.DirectX.UnitTests;

internal partial struct CommandLineParameters
{
    public bool m_Help;

    public bool m_List;

    public bool m_Test;

    public bool m_Benchmark;

    public GPUSelection m_GPUSelection;

    public bool Parse(string[] args)
    {
        for (int i = 0; i < args.Length; ++i)
        {
            if (args[i].Equals("-h", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--Help", StringComparison.OrdinalIgnoreCase))
            {
                m_Help = true;
            }
            else if (args[i].Equals("-l", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--List", StringComparison.OrdinalIgnoreCase))
            {
                m_List = true;
            }
            else if ((args[i].Equals("-g", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--GPU", StringComparison.OrdinalIgnoreCase)) && ((i + 1) < args.Length))
            {
                m_GPUSelection.Substring = args[i + 1];
                ++i;
            }
            else if ((args[i].Equals("-i", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--GPUIndex", StringComparison.OrdinalIgnoreCase)) && ((i + 1) < args.Length))
            {
                m_GPUSelection.Index = uint.Parse(args[i + 1]);
                ++i;
            }
            else if (args[i].Equals("-t", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--Test", StringComparison.OrdinalIgnoreCase))
            {
                m_Test = true;
            }
            else if (args[i].Equals("-b", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--Benchmark", StringComparison.OrdinalIgnoreCase))
            {
                m_Benchmark = true;
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}
