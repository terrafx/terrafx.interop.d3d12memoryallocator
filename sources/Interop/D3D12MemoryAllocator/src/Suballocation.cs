// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop
{
    /// <summary>
    /// Represents a region of NormalBlock that is either assigned and returned as
    /// allocated memory block or free.
    /// </summary>
    internal unsafe struct Suballocation
    {
        [NativeTypeName("UINT64")] public ulong offset;
        [NativeTypeName("UINT64")] public ulong size;
        public void* userData;
        public SuballocationType type;
    };
}
