// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Interop.UnitTests.D3D12MemAllocTests;

namespace TerraFX.Interop.UnitTests
{
    // A minimal port of std::unique_ptr for the purposes of this project
    internal unsafe struct unique_ptr<T> : IDisposable
        where T : unmanaged
    {
        private T* ptr_;

        public unique_ptr(T* other)
        {
            ptr_ = other;
        }

        public static implicit operator unique_ptr<T>(T* other)
        {
            return new unique_ptr<T>(other);
        }

        public static implicit operator T*(unique_ptr<T> other)
        {
            return other.Get();
        }

        public void Dispose()
        {
            T* pointer = ptr_;
            if (pointer != null)
            {
                ptr_ = null;

                default(D3d12maObjDeleter<T>).Invoke(pointer);
            }
        }

        public readonly T* Get()
        {
            return ptr_;
        }

        public void reset(T* _Ptr = null)
        {
            T* _Old = ptr_;
            ptr_ = _Ptr;
            if (_Old != null)
            {
                default(D3d12maObjDeleter<T>).Invoke(_Old);
            }
        }
    }
}
