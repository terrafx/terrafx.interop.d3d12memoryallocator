// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;

namespace TerraFX.Interop
{
    /// <summary>Bit flags to be used with <see cref="ALLOCATOR_DESC.Flags"/>.</summary>
    [Flags]
    public enum ALLOCATOR_FLAGS
    {
        /// <summary>Zero</summary>
        ALLOCATOR_FLAG_NONE = 0,

        /// <summary>
        /// Allocator and all objects created from it will not be synchronized internally, so you
        /// must guarantee they are used from only one thread at a time or synchronized by you.
        /// <para>Using this flag may increase performance because internal mutexes are not used.</para>
        /// </summary>
        ALLOCATOR_FLAG_SINGLETHREADED = 0x1,

        /// <summary>Every allocation will have its own memory block. To be used for debugging purposes.</summary>
        ALLOCATOR_FLAG_ALWAYS_COMMITTED = 0x2,
    }
}
