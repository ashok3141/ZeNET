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



using SC = System.Collections;
using SCG = System.Collections.Generic;


/// <summary>
/// Contains members that allow code to be written that can be compiled targeting any of a supported
/// set of .NET Framework versions (specifically, both v2.0 and v4.0 or compatible).
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
namespace ZeNET.Core.Compatibility.ProSystem
{
    /// <inheritdoc cref="System.Func{TResult}"/>
    public delegate TResult Func<TResult>();
    /// <inheritdoc cref="System.Func{T, TResult}"/>
    public delegate TResult Func<T, TResult>(T arg);
    /// <inheritdoc cref="System.Func{T1, T2, TResult}"/>
    public delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
    /// <inheritdoc cref="System.Func{T1, T2, T3, TResult}"/>
    public delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    /// <inheritdoc cref="System.Func{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult}"/>
    public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);

    /// <inheritdoc cref="System.Action"/>
    public delegate void Action();
    /// <inheritdoc cref="System.Action{T1, T2}"/>
    public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
    /// <inheritdoc cref="System.Action{T1, T2, T3}"/>
    public delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4}"/>
    public delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5}"/>
    public delegate void Action<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
    /// <inheritdoc cref="System.Action{T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16}"/>
    public delegate void Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);

    /// <inheritdoc cref="System.Collections.Generic.ISet{T}"/>
    /// <remarks>
    /// The interface definition is repeated here so that it can be used when compiling targeting
    /// .NET Framework 2.0.
    /// </remarks>
    public interface ISet<T> : SCG.ICollection<T>, SCG.IEnumerable<T>, SC.IEnumerable
    {
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.Add(T)"/>
        new bool Add(T item);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.ExceptWith(SCG.IEnumerable{T})"/>
        void ExceptWith(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.IntersectWith(SCG.IEnumerable{T})"/>
        void IntersectWith(SCG.IEnumerable<T> other);

        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.IsProperSubsetOf(SCG.IEnumerable{T})"/>
        bool IsProperSubsetOf(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.IsProperSupersetOf(SCG.IEnumerable{T})"/>
        bool IsProperSupersetOf(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.IsSubsetOf(SCG.IEnumerable{T})"/>
        bool IsSubsetOf(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.IsSupersetOf(SCG.IEnumerable{T})"/>
        bool IsSupersetOf(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.Overlaps(SCG.IEnumerable{T})"/>
        bool Overlaps(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.SetEquals(SCG.IEnumerable{T})"/>
        bool SetEquals(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.SymmetricExceptWith(SCG.IEnumerable{T})"/>
        void SymmetricExceptWith(SCG.IEnumerable<T> other);
        /// <inheritdoc cref="System.Collections.Generic.ISet{T}.UnionWith(SCG.IEnumerable{T})"/>
        void UnionWith(SCG.IEnumerable<T> other);
    }
}

#if !Framework_4
namespace System.Runtime.CompilerServices
{
    /// <inheritdoc cref="System.Runtime.CompilerServices.ExtensionAttribute"/>
    /// <remarks>The definition is included for the sole purpose of allowing compiling targeting
    /// .NET Framework 2.0.</remarks>
    public class ExtensionAttribute : Attribute { }
}
#endif