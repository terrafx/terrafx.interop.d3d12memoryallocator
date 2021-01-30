// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace TerraFX.Interop.UnitTests
{
    internal unsafe struct TestContext
    {
        public ALLOCATION_CALLBACKS* allocationCallbacks;
        public ID3D12Device* device;
        public Allocator* allocator;
        public ALLOCATOR_FLAGS allocatorFlags;
    }
}
