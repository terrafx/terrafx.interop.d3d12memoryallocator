# D3D12 Memory Allocator

**Version 2.0.0-development** (2020-11-03)

Copyright (c) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.

License: MIT

Documentation of all members: D3D12MemAlloc.h

# Table of contents

- [Quick start](#quick-start)
  - [Project setup](#project-setup)
  - [Creating resources](#creating-resources)
  - [Mapping memory](#mapping-memory)
  - [Custom pools](#custom-pools)
  - [Resource aliasing](#resource-aliasing)
  - [Virtual allocator](#virtual-allocator)
- [Configuration](#configuration)
  - [Custom CPU memory allocator](#custom-cpu-memory-allocator)
- [General considerations](#general-considerations)
  - [Thread safety](#thread-safety)
  - [Future plans](#future-plans)
  - [Features not supported](#features-not-supported)

### See also

- [Product page on GPUOpen](https://gpuopen.com/gaming-product/d3d12-memory-allocator/)
- [Source repository on GitHub](https://github.com/GPUOpen-LibrariesAndSDKs/D3D12MemoryAllocator)

# Quick start

## Project setup

This is a small, standalone C++ library. It consists of a pair of 2 files:
"%D3D12MemAlloc.h" header file with public interface and "D3D12MemAlloc.cpp" with
internal implementation. The only external dependencies are WinAPI, Direct3D 12,
and parts of C/C++ standard library (but STL containers, exceptions, or RTTI are
not used).

The library is developed and tested using Microsoft Visual Studio 2019, but it
should work with other compilers as well. It is designed for 64-bit code.

To use the library in your project:

1. Copy files `D3D12MemAlloc.cpp`, `%D3D12MemAlloc.h` to your project.

2. Make `D3D12MemAlloc.cpp` compiling as part of the project, as C++ code.

3. Include library header in each CPP file that needs to use the library.

```cpp
#include "D3D12MemAlloc.h"
```

4. Right after you created `ID3D12Device`, fill D3D12MA::ALLOCATOR_DESC
structure and call function D3D12MA::CreateAllocator to create the main
D3D12MA::Allocator object.

Please note that all symbols of the library are declared inside #D3D12MA namespace.

```cpp
IDXGIAdapter* adapter = (...)
ID3D12Device* device = (...)

D3D12MA::ALLOCATOR_DESC allocatorDesc = {};
allocatorDesc.pDevice = device;
allocatorDesc.pAdapter = adapter;

D3D12MA::Allocator* allocator;
HRESULT hr = D3D12MA::CreateAllocator(&allocatorDesc, &allocator);
```

5. Right before destroying the D3D12 device, destroy the allocator object.

Please note that objects of this library must be destroyed by calling `Release`
method (despite they are not COM interfaces and no reference counting is involved).

```cpp
allocator->Release();
```

## Creating resources

To use the library for creating resources (textures and buffers), call method
D3D12MA::Allocator::CreateResource in the place where you would previously call
`ID3D12Device::CreateCommittedResource`.

The function has similar syntax, but it expects structure D3D12MA::ALLOCATION_DESC
to be passed along with `D3D12_RESOURCE_DESC` and other parameters for created
resource. This structure describes parameters of the desired memory allocation,
including choice of `D3D12_HEAP_TYPE`.

The function also returns a new object of type D3D12MA::Allocation, created along
with usual `ID3D12Resource`. It represents allocated memory and can be queried
for size, offset, `ID3D12Resource`, and `ID3D12Heap` if needed.

```cpp
D3D12_RESOURCE_DESC resourceDesc = {};
resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
resourceDesc.Alignment = 0;
resourceDesc.Width = 1024;
resourceDesc.Height = 1024;
resourceDesc.DepthOrArraySize = 1;
resourceDesc.MipLevels = 1;
resourceDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
resourceDesc.SampleDesc.Count = 1;
resourceDesc.SampleDesc.Quality = 0;
resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
resourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

D3D12MA::ALLOCATION_DESC allocationDesc = {};
allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;

D3D12Resource* resource;
D3D12MA::Allocation* allocation;
HRESULT hr = allocator->CreateResource(
    &allocationDesc,
    &resourceDesc,
    D3D12_RESOURCE_STATE_COPY_DEST,
    NULL,
    &allocation,
    IID_PPV_ARGS(&resource));
```

You need to remember both resource and allocation objects and destroy them
separately when no longer needed.

```cpp
allocation->Release();
resource->Release();
```

The advantage of using the allocator instead of creating committed resource, and
the main purpose of this library, is that it can decide to allocate bigger memory
heap internally using `ID3D12Device::CreateHeap` and place multiple resources in
it, at different offsets, using `ID3D12Device::CreatePlacedResource`. The library
manages its own collection of allocated memory blocks (heaps) and remembers which
parts of them are occupied and which parts are free to be used for new resources.

It is important to remember that resources created as placed don't have their memory
initialized to zeros, but may contain garbage data, so they need to be fully initialized
before usage, e.g. using Clear (`ClearRenderTargetView`), Discard (`DiscardResource`),
or copy (`CopyResource`).

The library also automatically handles resource heap tier.
When `D3D12_FEATURE_DATA_D3D12_OPTIONS::ResourceHeapTier` equals `D3D12_RESOURCE_HEAP_TIER_1`,
resources of 3 types: buffers, textures that are render targets or depth-stencil,
and other textures must be kept in separate heaps. When `D3D12_RESOURCE_HEAP_TIER_2`,
they can be kept together. By using this library, you don't need to handle this
manually.

## Mapping memory

The process of getting regular CPU-side pointer to the memory of a resource in
Direct3D is called "mapping". There are rules and restrictions to this process,
as described in D3D12 documentation of [ID3D12Resource::Map method](https://docs.microsoft.com/en-us/windows/desktop/api/d3d12/nf-d3d12-id3d12resource-map).

Mapping happens on the level of particular resources, not entire memory heaps,
and so it is out of scope of this library. Just as the linked documentation says:

- Returned pointer refers to data of particular subresource, not entire memory heap.
- You can map same resource multiple times. It is ref-counted internally.
- Mapping is thread-safe.
- Unmapping is not required before resource destruction.
- Unmapping may not be required before using written data - some heap types on
  some platforms support resources persistently mapped.

When using this library, you can map and use your resources normally without
considering whether they are created as committed resources or placed resources in one large heap.

Example for buffer created and filled in `UPLOAD` heap type:

```cpp
const UINT64 bufSize = 65536;
const float* bufData = (...);

D3D12_RESOURCE_DESC resourceDesc = {};
resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
resourceDesc.Alignment = 0;
resourceDesc.Width = bufSize;
resourceDesc.Height = 1;
resourceDesc.DepthOrArraySize = 1;
resourceDesc.MipLevels = 1;
resourceDesc.Format = DXGI_FORMAT_UNKNOWN;
resourceDesc.SampleDesc.Count = 1;
resourceDesc.SampleDesc.Quality = 0;
resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
resourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

D3D12MA::ALLOCATION_DESC allocationDesc = {};
allocationDesc.HeapType = D3D12_HEAP_TYPE_UPLOAD;

D3D12Resource* resource;
D3D12MA::Allocation* allocation;
HRESULT hr = allocator->CreateResource(
    &allocationDesc,
    &resourceDesc,
    D3D12_RESOURCE_STATE_GENERIC_READ,
    NULL,
    &allocation,
    IID_PPV_ARGS(&resource));

void* mappedPtr;
hr = resource->Map(0, NULL, &mappedPtr);

memcpy(mappedPtr, bufData, bufSize);

resource->Unmap(0, NULL);
```

## Custom pools

A "pool" is a collection of memory blocks that share certain properties.

Allocator creates 3 default pools: for `D3D12_HEAP_TYPE_DEFAULT`, `UPLOAD`, `READBACK`. A default pool automatically grows in size. Size of allocated blocks is also variable and managed automatically. Typical allocations are created in these pools. You can also create custom pools.

### Usage

To create a custom pool, fill in structure `D3D12MA::POOL_DESC` and call function `D3D12MA::Allocator::CreatePool` to obtain object `D3D12MA::Pool`. Example:

```cpp
POOL_DESC poolDesc = {};
poolDesc.HeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
Pool* pool;
HRESULT hr = allocator->CreatePool(&poolDesc, &pool);
```

To allocate resources out of a custom pool, only set member `D3D12MA::ALLOCATION_DESC::CustomPool`. Example:

```cpp
ALLOCATION_DESC allocDesc = {};
allocDesc.CustomPool = pool;
D3D12_RESOURCE_DESC resDesc = ...
Allocation* alloc;
hr = allocator->CreateResource(&allocDesc, &resDesc,
    D3D12_RESOURCE_STATE_GENERIC_READ, NULL, &alloc, IID_NULL, NULL);
```

All allocations must be released before releasing the pool.
The pool must be released before releasing the allocator.

```cpp
alloc->Release();
pool->Release();
```

### Features and benefits

While it is recommended to use default pools whenever possible for simplicity and to give the allocator more opportunities for internal optimizations, custom pools may be useful in following cases:
- To keep some resources separate from others in memory.
' To keep track of memory usage of just a specific group of resources. Statistics can be queried using `D3D12MA::Pool::CalculateStats`.
- To use specific size of a memory block (`ID3D12Heap`). To set it, use member `D3D12MA::POOL_DESC::BlockSize`.
  When set to 0, the library uses automatically determined, variable block sizes.
- To reserve some minimum amount of memory allocated. To use it, set member `D3D12MA::POOL_DESC::MinBlockCount`.
- To limit maximum amount of memory allocated. To use it, set member `D3D12MA::POOL_DESC::MaxBlockCount`.
- To use extended parameters of the D3D12 memory allocation. While resources created from default pools can only specify `D3D12_HEAP_TYPE_DEFAULT`, `UPLOAD`, `READBACK`, a custom pool may use non-standard `D3D12_HEAP_PROPERTIES` (member D3D12MA::POOL_DESC::HeapProperties) and `D3D12_HEAP_FLAGS` (`D3D12MA::POOL_DESC::HeapFlags`), which is useful e.g. for cross-adapter sharing or UMA (see also `D3D12MA::Allocator::IsUMA`).

New versions of this library support creating **committed allocations in custom pools**.

It is supported only when `D3D12MA::POOL_DESC::BlockSize = 0`.
To use this feature, set `D3D12MA::ALLOCATION_DESC::CustomPool` to the pointer to your custom pool and
`D3D12MA::ALLOCATION_DESC::Flags` to `D3D12MA::ALLOCATION_FLAG_COMMITTED`. Example:

```cpp
ALLOCATION_DESC allocDesc = {};
allocDesc.CustomPool = pool;
allocDesc.Flags = ALLOCATION_FLAG_COMMITTED;

D3D12_RESOURCE_DESC resDesc = ...
Allocation* alloc;
ID3D12Resource* res;
hr = allocator->CreateResource(&allocDesc, &resDesc,
    D3D12_RESOURCE_STATE_GENERIC_READ, NULL, &alloc, IID_PPV_ARGS(&res));
```

This feature may seem unnecessary, but creating committed allocations from custom pools may be useful
in some cases, e.g. to have separate memory usage statistics for some group of resources or to use
extended allocation parameters, like custom `D3D12_HEAP_PROPERTIES`, which are available only in custom pools.

## Resource aliasing

New explicit graphics APIs (Vulkan and Direct3D 12), thanks to manual memory
management, give an opportunity to alias (overlap) multiple resources in the
same region of memory - a feature not available in the old APIs (Direct3D 11, OpenGL).
It can be useful to save video memory, but it must be used with caution.

For example, if you know the flow of your whole render frame in advance, you
are going to use some intermediate textures or buffers only during a small range of render passes,
and you know these ranges don't overlap in time, you can create these resources in
the same place in memory, even if they have completely different parameters (width, height, format etc.).

![Resource aliasing (overlap)](https://github.com/GPUOpen-LibrariesAndSDKs/D3D12MemoryAllocator/blob/master/docs/gfx/Aliasing.png?raw=true)

Such scenario is possible using D3D12MA, but you need to create your resources
using special function D3D12MA::Allocator::CreateAliasingResource.
Before that, you need to allocate memory with parameters calculated using formula:

- allocation size = max(size of each resource)
- allocation alignment = max(alignment of each resource)

Following example shows two different textures created in the same place in memory,
allocated to fit largest of them.

```cpp
D3D12_RESOURCE_DESC resDesc1 = {};
resDesc1.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
resDesc1.Alignment = 0;
resDesc1.Width = 1920;
resDesc1.Height = 1080;
resDesc1.DepthOrArraySize = 1;
resDesc1.MipLevels = 1;
resDesc1.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
resDesc1.SampleDesc.Count = 1;
resDesc1.SampleDesc.Quality = 0;
resDesc1.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
resDesc1.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;

D3D12_RESOURCE_DESC resDesc2 = {};
resDesc2.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
resDesc2.Alignment = 0;
resDesc2.Width = 1024;
resDesc2.Height = 1024;
resDesc2.DepthOrArraySize = 1;
resDesc2.MipLevels = 0;
resDesc2.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
resDesc2.SampleDesc.Count = 1;
resDesc2.SampleDesc.Quality = 0;
resDesc2.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
resDesc2.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

const D3D12_RESOURCE_ALLOCATION_INFO allocInfo1 =
    device->GetResourceAllocationInfo(0, 1, &resDesc1);
const D3D12_RESOURCE_ALLOCATION_INFO allocInfo2 =
    device->GetResourceAllocationInfo(0, 1, &resDesc2);

D3D12_RESOURCE_ALLOCATION_INFO finalAllocInfo = {};
finalAllocInfo.Alignment = std::max(allocInfo1.Alignment, allocInfo2.Alignment);
finalAllocInfo.SizeInBytes = std::max(allocInfo1.SizeInBytes, allocInfo2.SizeInBytes);

D3D12MA::ALLOCATION_DESC allocDesc = {};
allocDesc.HeapType = D3D12_HEAP_TYPE_DEFAULT;
allocDesc.ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES;

D3D12MA::Allocation* alloc;
hr = allocator->AllocateMemory(&allocDesc, &finalAllocInfo, &alloc);
assert(alloc != NULL && alloc->GetHeap() != NULL);

ID3D12Resource* res1;
hr = allocator->CreateAliasingResource(
    alloc,
    0, // AllocationLocalOffset
    &resDesc1,
    D3D12_RESOURCE_STATE_COMMON,
    NULL, // pOptimizedClearValue
    IID_PPV_ARGS(&res1));

ID3D12Resource* res2;
hr = allocator->CreateAliasingResource(
    alloc,
    0, // AllocationLocalOffset
    &resDesc2,
    D3D12_RESOURCE_STATE_COMMON,
    NULL, // pOptimizedClearValue
    IID_PPV_ARGS(&res2));

// You can use res1 and res2, but not at the same time!

res2->Release();
res1->Release();
alloc->Release();
```

Remember that using resouces that alias in memory requires proper synchronization.
You need to issue a special barrier of type `D3D12_RESOURCE_BARRIER_TYPE_ALIASING`.
You also need to treat a resource after aliasing as uninitialized - containing garbage data.
For example, if you use `res1` and then want to use `res2`, you need to first initialize `res2`
using either Clear, Discard, or Copy to the entire resource.

Additional considerations:

- D3D12 also allows to interpret contents of memory between aliasing resources consistently in some cases,
  which is called "data inheritance". For details, see
  Microsoft documentation, chapter [Memory Aliasing and Data Inheritance](https://docs.microsoft.com/en-us/windows/win32/direct3d12/memory-aliasing-and-data-inheritance).
- You can create more complex layout where different textures and buffers are bound
  at different offsets inside one large allocation. For example, one can imagine
  a big texture used in some render passes, aliasing with a set of many small buffers
  used in some further passes. To bind a resource at non-zero offset of an allocation,
  call D3D12MA::Allocator::CreateAliasingResource with appropriate value of `AllocationLocalOffset` parameter.
- Resources of the three categories: buffers, textures with `RENDER_TARGET` or `DEPTH_STENCIL` flags, and all other textures,
  can be placed in the same memory only when `allocator->GetD3D12Options().ResourceHeapTier >= D3D12_RESOURCE_HEAP_TIER_2`.
  Otherwise they must be placed in different memory heap types, and thus aliasing them is not possible.

## Virtual allocator

As an extra feature, the core allocation algorithm of the library is exposed through a simple and convenient API of "virtual allocator".
It doesn't allocate any real GPU memory. It just keeps track of used and free regions of a "virtual block".
You can use it to allocate your own memory or other objects, even completely unrelated to D3D12.
A common use case is sub-allocation of pieces of one large GPU buffer.

### Creating virtual block

To use this functionality, there is no main "allocator" object.
You don't need to have D3D12MA::Allocator object created.
All you need to do is to create a separate D3D12MA::VirtualBlock object for each block of memory you want to be managed by the allocator:

- Fill in D3D12MA::ALLOCATOR_DESC structure.

- Call D3D12MA::CreateVirtualBlock. Get new D3D12MA::VirtualBlock object.

Example:

```cpp
D3D12MA::VIRTUAL_BLOCK_DESC blockDesc = {};
blockDesc.Size = 1048576; // 1 MB

D3D12MA::VirtualBlock *block;
HRESULT hr = CreateVirtualBlock(&blockDesc, &block);
```

### Making virtual allocations

D3D12MA::VirtualBlock object contains internal data structure that keeps track of free and occupied regions
using the same code as the main D3D12 memory allocator.
However, there is no "virtual allocation" object.
When you request a new allocation, a `UINT64` number is returned.
It is an offset inside the block where the allocation has been placed, but it also uniquely identifies the allocation within this block.

In order to make an allocation:

- Fill in D3D12MA::VIRTUAL_ALLOCATION_DESC structure.

- Call D3D12MA::VirtualBlock::Allocate. Get new `UINT64 offset` that identifies the allocation.

Example:

```cpp
D3D12MA::VIRTUAL_ALLOCATION_DESC allocDesc = {};
allocDesc.Size = 4096; // 4 KB

UINT64 allocOffset;
hr = block->Allocate(&allocDesc, &allocOffset);
if(SUCCEEDED(hr))
{
    // Use the 4 KB of your memory starting at allocOffset.
}
else
{
    // Allocation failed - no space for it could be found. Handle this error!
}
```

### Deallocation

When no longer needed, an allocation can be freed by calling D3D12MA::VirtualBlock::FreeAllocation.
You can only pass to this function the exact offset that was previously returned by D3D12MA::VirtualBlock::Allocate
and not any other location within the memory.

When whole block is no longer needed, the block object can be released by calling D3D12MA::VirtualBlock::Release.
All allocations must be freed before the block is destroyed, which is checked internally by an assert.
However, if you don't want to call `block->FreeAllocation` for each allocation, you can use D3D12MA::VirtualBlock::Clear to free them all at once -
a feature not available in normal D3D12 memory allocator. Example:

```cpp
block->FreeAllocation(allocOffset);
block->Release();
```

### Allocation parameters

You can attach a custom pointer to each allocation by using D3D12MA::VirtualBlock::SetAllocationUserData.
Its default value is `NULL`.
It can be used to store any data that needs to be associated with that allocation - e.g. an index, a handle, or a pointer to some
larger data structure containing more information. Example:

```cpp
struct CustomAllocData
{
    std::string m_AllocName;
};
CustomAllocData* allocData = new CustomAllocData();
allocData->m_AllocName = "My allocation 1";
block->SetAllocationUserData(allocOffset, allocData);
```

The pointer can later be fetched, along with allocation size, by passing the allocation offset to function
D3D12MA::VirtualBlock::GetAllocationInfo and inspecting returned structure D3D12MA::VIRTUAL_ALLOCATION_INFO.
If you allocated a new object to be used as the custom pointer, don't forget to delete that object before freeing the allocation!
Example:

```cpp
VIRTUAL_ALLOCATION_INFO allocInfo;
block->GetAllocationInfo(allocOffset, &allocInfo);
delete (CustomAllocData*)allocInfo.pUserData;

block->FreeAllocation(allocOffset);
```

### Alignment and units

It feels natural to express sizes and offsets in bytes.
If an offset of an allocation needs to be aligned to a multiply of some number (e.g. 4 bytes), you can fill optional member
D3D12MA::VIRTUAL_ALLOCATION_DESC::Alignment to request it. Example:

```cpp
D3D12MA::VIRTUAL_ALLOCATION_DESC allocDesc = {};
allocDesc.Size = 4096; // 4 KB
allocDesc.Alignment = 4; // Returned offset must be a multiply of 4 B

UINT64 allocOffset;
hr = block->Allocate(&allocDesc, &allocOffset);
```

Alignments of different allocations made from one block may vary.
However, if all alignments and sizes are always multiply of some size e.g. 4 B or `sizeof(MyDataStruct)`,
you can express all sizes, alignments, and offsets in multiples of that size instead of individual bytes.
It might be more convenient, but you need to make sure to use this new unit consistently in all the places:

- D3D12MA::VIRTUAL_BLOCK_DESC::Size
- D3D12MA::VIRTUAL_ALLOCATION_DESC::Size and D3D12MA::VIRTUAL_ALLOCATION_DESC::Alignment
- Using offset returned by D3D12MA::VirtualBlock::Allocate

### Statistics

You can obtain statistics of a virtual block using D3D12MA::VirtualBlock::CalculateStats.
The function fills structure D3D12MA::StatInfo - same as used by the normal D3D12 memory allocator.
Example:

```cpp
D3D12MA::StatInfo statInfo;
block->CalculateStats(&statInfo);
printf("My virtual block has %llu bytes used by %u virtual allocations\n",
    statInfo.UsedBytes, statInfo.AllocationCount);
```

You can also request a full list of allocations and free regions as a string in JSON format by calling
D3D12MA::VirtualBlock::BuildStatsString.
Returned string must be later freed using D3D12MA::VirtualBlock::FreeStatsString.
The format of this string may differ from the one returned by the main D3D12 allocator, but it is similar.

### Additional considerations

Note that the "virtual allocator" functionality is implemented on a level of individual memory blocks.
Keeping track of a whole collection of blocks, allocating new ones when out of free space,
deleting empty ones, and deciding which one to try first for a new allocation must be implemented by the user.


# Configuration

Please check file `D3D12MemAlloc.cpp` lines between "Configuration Begin" and
"Configuration End" to find macros that you can define to change the behavior of
the library, primarily for debugging purposes.

## Custom CPU memory allocator

If you use custom allocator for CPU memory rather than default C++ operator `new`
and `delete` or `malloc` and `free` functions, you can make this library using
your allocator as well by filling structure D3D12MA::ALLOCATION_CALLBACKS and
passing it as optional member D3D12MA::ALLOCATOR_DESC::pAllocationCallbacks.
Functions pointed there will be used by the library to make any CPU-side
allocations. Example:

```cpp
#include <malloc.h>

void* CustomAllocate(size_t Size, size_t Alignment, void* pUserData)
{
    void* memory = _aligned_malloc(Size, Alignment);
    // Your extra bookkeeping here...
    return memory;
}

void CustomFree(void* pMemory, void* pUserData)
{
    // Your extra bookkeeping here...
    _aligned_free(pMemory);
}

(...)

D3D12MA::ALLOCATION_CALLBACKS allocationCallbacks = {};
allocationCallbacks.pAllocate = &CustomAllocate;
allocationCallbacks.pFree = &CustomFree;

D3D12MA::ALLOCATOR_DESC allocatorDesc = {};
allocatorDesc.pDevice = device;
allocatorDesc.pAdapter = adapter;
allocatorDesc.pAllocationCallbacks = &allocationCallbacks;

D3D12MA::Allocator* allocator;
HRESULT hr = D3D12MA::CreateAllocator(&allocatorDesc, &allocator);
```

# General considerations

## Thread safety

- The library has no global state, so separate D3D12MA::Allocator objects can be used independently.
  In typical applications there should be no need to create multiple such objects though - one per `ID3D12Device` is enough.
- All calls to methods of D3D12MA::Allocator class are safe to be made from multiple
  threads simultaneously because they are synchronized internally when needed.
- When the allocator is created with D3D12MA::ALLOCATOR_FLAG_SINGLETHREADED,
  calls to methods of D3D12MA::Allocator class must be made from a single thread or synchronized by the user.
  Using this flag may improve performance.

## Future plans

Features planned for future releases:

Near future: feature parity with [Vulkan Memory Allocator](https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator/), including:

- Alternative allocation algorithms: linear allocator, buddy allocator
- Support for priorities using `ID3D12Device1::SetResidencyPriority`

Later:

- Memory defragmentation
- Support for multi-GPU (multi-adapter)

## Features not supported

Features deliberately excluded from the scope of this library:

- Descriptor allocation. Although also called "heaps", objects that represent
  descriptors are separate part of the D3D12 API from buffers and textures.
- Support for reserved (tiled) resources. We don't recommend using them.
- Support for `ID3D12Device::Evict` and `MakeResident`. We don't recommend using them.
- Handling CPU memory allocation failures. When dynamically creating small C++
  objects in CPU memory (not the GPU memory), allocation failures are not
  handled gracefully, because that would complicate code significantly and
  is usually not needed in desktop PC applications anyway.
  Success of an allocation is just checked with an assert.
- Code free of any compiler warnings - especially those that would require complicating the code
  just to please the compiler complaining about unused parameters, variables, or expressions being
  constant in Relese configuration, e.g. because they are only used inside an assert.
- This is a C++ library.
  Bindings or ports to any other programming languages are welcomed as external projects and
  are not going to be included into this repository.