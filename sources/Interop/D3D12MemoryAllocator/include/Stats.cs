// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TerraFX.Interop
{
    /// <summary>General statistics from the current state of the allocator.</summary>
    public struct Stats
    {
        /// <summary>Total statistics from all heap types.</summary>
        public StatInfo Total;

        /// <summary>
        /// One StatInfo for each type of heap located at the following indices:
        /// 0 - DEFAULT, 1 - UPLOAD, 2 - READBACK.
        /// </summary>
        public __Stats_Values HeapType;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct __Stats_Values
        {
            private StatInfo _HeapType0;
            private StatInfo _HeapType1;
            private StatInfo _HeapType2;

            public unsafe ref StatInfo this[int index]
            {
                get
                {
                    fixed (__Stats_Values* p = &this)
                    {
                        switch (index)
                        {
                            case 0: return ref p->_HeapType0;
                            case 1: return ref p->_HeapType1;
                            case 2: return ref p->_HeapType2;
                            default: return ref Unsafe.NullRef<StatInfo>();
                        }
                    }
                }
            }
        }
    }
}
