// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from UnscopedRefAttribute.cs in the dotnet/runtime repo 
// Original source is Copyright © .NET Foundation. Licensed under the MIT License (MIT)

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class UnscopedRefAttribute : Attribute
{
    public UnscopedRefAttribute() { }
}
