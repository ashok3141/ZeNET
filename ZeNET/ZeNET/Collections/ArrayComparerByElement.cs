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

using ZeNET.Core;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZeNET.Collections
{
    /// <summary>
    /// Implements the comparison interfaces <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/>
    /// (along with the nongeneric counterparts <see cref="IEqualityComparer"/> and <see cref="IComparer"/>)
    /// for an array that is based on the elements of the array rather than the array object.
    /// </summary>
    /// <typeparam name="T">The type of the key values (will often be <see cref="Object"/>).
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// The class treats the elements of the array as the members of a tuple (perhaps representing a
    /// unique identifier of some class of object). The ordering produced by using this comparer is
    /// what would be produced by ordering by the first element of the array first, then by the
    /// second element, then by the third, and so on.
    /// </para>
    /// <para>
    /// <em>NOTE: Unit testing for this class is pending.</em>
    /// </para>
    /// </remarks>
    public sealed class ArrayComparerByElement<T> : IEqualityComparer<T[]>, IEqualityComparer, IComparer<T[]>, IComparer
    {
        private static readonly IComparer<T> defaultComparer = Comparer<T>.Default;
        private static readonly IEqualityComparer<T> defaultEqComparer = EqualityComparer<T>.Default;

        private static readonly ArrayComparerByElement<T> _Default = new ArrayComparerByElement<T>();

        private ArrayComparerByElement() { }

        /// <summary>
        /// Fetches the only instance of this class.
        /// </summary>
        public static ArrayComparerByElement<T> Default { get { return _Default; } }

        /// <inheritdoc/>
        public bool Equals(T[] a, T[] b)
        {
            if (object.ReferenceEquals(a, b))
                return true;

            if (a?.Length != b?.Length)
                return false;

            int size = a.Length;
            for (int i = 0; i < size; i++)
                if (!defaultEqComparer.Equals(a[i], b[i]))
                    return false;

            return true;
        }

        /// <inheritdoc/>
        bool IEqualityComparer.Equals(object a, object b)
        {
            if (object.ReferenceEquals(a, b))
                return true;

            T[] a_arr;
            T[] b_arr;

            try
            {
                a_arr = (T[])a;
                b_arr = (T[])b;
            }
            catch
            {
                return false;
            }

            return ((IEqualityComparer<T[]>)this).Equals(a_arr, b_arr);
        }

        /// <inheritdoc/>
        public int GetHashCode(T[] obj)
        {
            return Hashing.Murmur3_32bit(obj);
        }

        /// <inheritdoc/>
        int IEqualityComparer.GetHashCode(object x)
        {
            return Hashing.Murmur3_32bit((T[])x);
        }

        /// <inheritdoc cref="IComparer{T}.Compare(T, T)"/>
        /// <remarks>
        /// When <paramref name="x"/> and <paramref name="y"/> have different numbers of elements in
        /// them, the comparison is done element by element up to the last element of the shorter
        /// array. If all elements up to that point are equal, then the shorter array is deemed to
        /// be less than the longer array. This is similar to how strings are compared.
        /// </remarks>
        public int Compare(T[] x, T[] y)
        {
            return this.compare(x, y);
        }

        /// <inheritdoc/>
        int IComparer.Compare(object x, object y)
        {
            if (!(x is T[]))
                throw new ArgumentException("x is not of the right type for this method.", "x");
            if (!(y is T[]))
                throw new ArgumentException("y is not of the right type for this method.", "y");
            Contract.EndContractBlock();

            //Contract.Requires<ArgumentException>(x is T[], "x is not of the right type for this method.");
            //Contract.Requires<ArgumentException>(y is T[], "y is not of the right type for this method.");

            return this.compare((T[])x, (T[])y);
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int compare(T[] x, T[] y)
        {
            int xLength = x.Length;
            int yLength = y.Length;

            int size = (xLength < yLength) ? xLength : yLength;

            for (int i = 0; i < size; i++)
            {
                int comparison = defaultComparer.Compare(x[i], y[i]);

                if (comparison != 0)
                    return comparison;
            }

            return xLength.CompareTo(yLength);
        }
    }
}
