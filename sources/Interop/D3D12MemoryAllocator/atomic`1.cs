// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    // Minimal port of std::atomic<T> for the purposes of this project
    internal struct atomic<T>
        where T : unmanaged
    {
        internal T value;
    }
}
