// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using SuballocationList = TerraFX.Interop.List<TerraFX.Interop.Suballocation>;

namespace TerraFX.Interop
{
    /// <summary>Parameters of planned allocation inside a NormalBlock.</summary>
    internal struct AllocationRequest
    {
        [NativeTypeName("UINT64")] public ulong offset;
        [NativeTypeName("UINT64")] public ulong sumFreeSize; // Sum size of free items that overlap with proposed allocation.
        [NativeTypeName("UINT64")] public ulong sumItemSize; // Sum size of items to make lost that overlap with proposed allocation.
        public SuballocationList.iterator item;
        [NativeTypeName("BOOL")] public int zeroInitialized;
    };
}
