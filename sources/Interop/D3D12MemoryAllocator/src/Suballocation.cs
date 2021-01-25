// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using UINT64 = System.UInt64;

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents a region of NormalBlock that is either assigned and returned as
    /// allocated memory block or free.
    /// </summary>
    internal unsafe struct Suballocation
    {
        public UINT64 offset;
        public UINT64 size;
        public void* userData;
        public SuballocationType type;
    };
}
