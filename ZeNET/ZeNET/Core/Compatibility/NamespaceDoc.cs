/******************************************************************************/
// Copyright (c) 2017 Ashok Gurumurthy

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
/******************************************************************************/



namespace ZeNET.Core.Compatibility
{
    /// <summary>
    /// Contains members that allow code to be written that can be compiled targeting any of a
    /// supported set of .NET Framework versions (specifically, both v2.0 and v4.0 or compatible).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This namespace should be included whenever the project will be compiled targeting .NET Framework
    /// v2.0. It will make some of the commonly used features of later versions of C# available in v2.0.
    /// The file should not be included when targeting a later C# version, because doing so will create
    /// collisions with, e.g., <see cref="System"/> or <see cref="System.Linq"/>.
    /// </para>
    /// <para>
    /// Here is a suggested set of <code>using</code>  directives:
    /// <code language="C#" title="using directives">
    /// using System;
    /// #if Framework_4
    /// using System.Diagnostics.Contracts;
    /// using System.Linq;
    /// #else
    /// using ZeNET.Core.Compatibility;
    /// #endif
    /// </code>
    /// This should be accompanied by the following lines added to the .csproj file:
    /// <code language="xml" title=".csproj">
    /// <DefineConstants Condition=" '$(TargetFrameworkVersion)' == 'v4.0' ">Framework_4</DefineConstants>
    /// <DefineConstants Condition=" '$(TargetFrameworkVersion)' == 'v4.5' ">Framework_4;Framework_4_5</DefineConstants>
    /// </code>
    /// </para>
    /// </remarks>
    [System.Runtime.CompilerServices.CompilerGenerated()]
    class NamespaceGroupDoc
    {
    }

    /// <summary>
    /// Defines assorted addenda to .NET Framework 2.0, so that projects targeting .NET Framework
    /// 2.0 can use some of the common useful features in later Framework versions.
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated()]
    class NamespaceDoc
    {
    }
}
