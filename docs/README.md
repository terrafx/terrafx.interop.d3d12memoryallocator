# TerraFX.Interop.D3D12MemoryAllocator

Managed port of D3D12MemoryAllocator.

![ci](https://github.com/terrafx/terrafx.interop.d3d12memoryallocator/actions/workflows/ci.yml/badge.svg?branch=main&event=push)
[![Discord](https://img.shields.io/discord/593547387457372212.svg?label=Discord&style=plastic)](https://discord.terrafx.dev/)

Packages are available at: https://github.com/orgs/terrafx/packages or via the NuGet Feed URL: https://pkgs.terrafx.dev/index.json

## Table of Contents

* [Code of Conduct](#code-of-conduct)
* [License](#license)
* [Contributing](#contributing)
* [Goals](#goals)
* [Languages and Frameworks](#languages-and-frameworks)

### Code of Conduct

TerraFX and everyone contributing (this includes issues, pull requests, the
wiki, etc) must abide by the .NET Foundation Code of Conduct:
https://dotnetfoundation.org/about/code-of-conduct.

Instances of abusive, harassing, or otherwise unacceptable behavior may be
reported by contacting the project team at conduct@dotnetfoundation.org.

### License

Copyright © Tanner Gooding and Contributors. Licensed under the MIT License
(MIT). See [LICENSE](https://github.com/terrafx/terrafx.interop.d3d12memoryallocator/blob/main/LICENSE.md) in the repository root for more information.

### Contributing

If you are looking to contribute you should read our
[Contributing](https://github.com/terrafx/terrafx.interop.d3d12memoryallocator/blob/main/docs/CONTRIBUTING.md) documentation.

### Goals

Provide a managed port of GPUOpen-LibrariesAndSDKs/D3D12MemoryAllocator

The library is  blittable, trim safe, AOT compatible, and as close to 1-to-1 with the underlying C API definitions as feasible. The general setup is fully compatible with the native definitions and could even be used against native exports that produce the relevant allocation objects if that were desired; however, since all the code is present this is primarily a port of the logic and doesn't rely on the original native library existing.

### .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
