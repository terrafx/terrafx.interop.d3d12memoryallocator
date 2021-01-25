// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using UINT64 = System.UInt64;
using BOOL = System.Int32;

using SuballocationList = TerraFX.Interop.List<TerraFX.Interop.Suballocation>;

namespace TerraFX.Interop
{
    /// <summary>Parameters of planned allocation inside a NormalBlock.</summary>
    internal struct AllocationRequest
    {
        public UINT64 offset;
        public UINT64 sumFreeSize; // Sum size of free items that overlap with proposed allocation.
        public UINT64 sumItemSize; // Sum size of items to make lost that overlap with proposed allocation.
        public SuballocationList.iterator item;
        public BOOL zeroInitialized;
    };
}
