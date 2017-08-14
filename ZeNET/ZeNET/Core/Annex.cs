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



// Start: standard inclusion list
using System;
using ZeNET.Core.Extensions;
#if Framework_4
using System.Diagnostics.Contracts;
using System.Linq;
#else
using ZeNET.Core.Compatibility;
using ZeNET.Core.Compatibility.ProLinq;
using ZeNET.Core.Compatibility.ProSystem;
#endif
// End: standard inclusion list

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZeNET.Core
{
    /// <summary>
    /// Provides an assortment of methods, aimed at improving the programmability in .NET.
    /// </summary>
    public static class Annex
    {
        /// <summary>
        /// Swaps <paramref name="x"/> and <paramref name="y"/>.
        /// </summary>
        /// <typeparam name="T">The type of the two arguments.</typeparam>
        /// <param name="x">The first variable.</param>
        /// <param name="y">The second variable.</param>
#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Swap<T>(ref T x, ref T y)
        {
            T temp = x;
            x = y;
            y = temp;
        }


        /// <summary>
        /// A list of numbers, suitable for constructing a loop with extension methods.
        /// </summary>
        /// <param name="length">The length of the list (or the number of iterations in the loop).</param>
        /// <returns>The numbers from 0 to <i>length</i> - 1.</returns>
        /// <remarks>
        /// This method returns the same list as <code>Annex.Range(0, length - 1)</code>.
        /// </remarks>
        public static IEnumerable<int> Loop(int length)
        {
            if (!(length >= 0))
                throw new ArgumentException("length must be nonnegative.", "length");
            Contract.EndContractBlock();

            for (int i = 0; i < length; i++)
                yield return i;
        }

        /// <summary>
        /// A list of numbers between two integers (bounds included), with a increment of 1.
        /// </summary>
        /// <param name="from">The first number in the range.</param>
        /// <param name="to">The last number in the range.</param>
        /// <returns>The list <paramref name="from"/>, <paramref name="from"/> + 1, ...,
        /// <paramref name="to"/>.</returns>
        public static IEnumerable<int> Range(int from, int to)
        {
            if (to < from)
                throw new ArgumentException("Necessary condition: to >= from");
            Contract.EndContractBlock();

            for (int i = from; i <= to; i++)
                yield return i;
        }

        /// <summary>
        /// A list of numbers between two integers (bounds included), with the specified increment from one number to the next
        /// </summary>
        /// <param name="from">The first number in the range.</param>
        /// <param name="to">The last number in the range.</param>
        /// <param name="increment">The increment from one number to the next.</param>
        /// <returns>
        /// The list <paramref name="from"/>, <paramref name="from"/> + <paramref name="increment"/>,
        /// <paramref name="from"/> + 2 * <paramref name="increment"/>, ..., <paramref name="to"/>.
        /// </returns>
        /// <remarks>
        /// The last number in the range need not be <paramref name="to"/>, because <paramref name="increment"/>
        /// may not divide (<paramref name="to"/> - <paramref name="from"/>). Negative values are
        /// permitted (indeed, necessary) for <paramref name="increment"/> when <paramref name="to"/>
        /// is less than <paramref name="from"/>.
        /// </remarks>
        public static IEnumerable<int> Range(int from, int to, int increment)
        {
            if (!(from == to || (from > to && increment < 0) || (from < to && increment > 0)))
                throw new ArgumentException("Arguments must define a finite range.");
            // Contract.Requires<ArgumentException>(from == to || (from > to && increment < 0) || (from < to && increment > 0), "Arguments must define a finite range.");
            Contract.EndContractBlock();

            if (to > from)
                for (int i = from; i <= to; i += increment)
                    yield return i;
            else if (to < from)
                for (int i = from; i >= to; i += increment)
                    yield return i;
            else
                yield return from;
        }
    }
}
