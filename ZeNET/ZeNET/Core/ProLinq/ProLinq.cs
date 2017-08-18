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
#if Framework_4
using System.Diagnostics.Contracts;
using System.Linq;
#else
using ZeNET.Core.Compatibility;
using ZeNET.Core.Compatibility.ProSystem;
#endif
// End: standard inclusion list

using System.Collections;
using System.Collections.Generic;

namespace ZeNET.Core.Compatibility.ProLinq
{
    /// <inheritdoc cref="System.Linq.Enumerable"/>
    /// <remarks>
    /// This compatibility implementation includes only a subset of the methods available in .NET
    /// Framework versions later than 2.0.
    /// </remarks>
    public static class Enumerable
    {
        /// <inheritdoc cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource})"/>
        public static bool Any<TSource>(this IEnumerable<TSource> source)
        {
            foreach (TSource item in source)
                return true;
            return false;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, System.Func{TSource, bool})"/>
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource item in source)
                if (predicate(item))
                    return true;

            return false;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.All{TSource}(IEnumerable{TSource}, System.Func{TSource, bool})"/>
        public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource item in source)
                if (!predicate(item))
                    return false;
            return true;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Cast{TResult}(IEnumerable)"/>
        public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source)
        {
            foreach (object obj in source)
            {
                yield return (TResult)source;
            }
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Concat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            foreach (TSource item in first)
                yield return item;

            foreach (TSource item in second)
                yield return item;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Range(int, int)"/>
        public static IEnumerable<int> Range(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return start + i;
        }

        /// <inheritdoc cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            foreach (TSource item in source)
                yield return selector(item);
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, System.Func{TSource, TResult})"/>
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            int index = 0;
            foreach (TSource item in source)
                yield return selector(item, index++);
        }

        /// <inheritdoc cref="System.Linq.Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, System.Func{TSource, IEnumerable{TResult}})"/>
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            foreach (TSource item in source)
                foreach (TResult result in selector(item))
                    yield return result;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.SelectMany{TSource, TCollection, TResult}(IEnumerable{TSource}, System.Func{TSource, int, IEnumerable{TCollection}}, System.Func{TSource, TCollection, TResult})"/>
        public static IEnumerable<TResult> SelectMany<TSource, TResult>
            (this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            int index = 0;
            foreach (TSource item in source)
                foreach (TResult result in selector(item, index++))
                    yield return result;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            IEqualityComparer<TSource> comparer = EqualityComparer<TSource>.Default;

            if (Object.ReferenceEquals(first, second))
                return true;
            else if (first is ICollection<TSource> && second is ICollection<TSource> && ((ICollection<TSource>)first).Count != ((ICollection<TSource>)second).Count)
                return false;
            else
            {
                using (IEnumerator<TSource> enumerator1 = first.GetEnumerator())
                using (IEnumerator<TSource> enumerator2 = second.GetEnumerator())
                {
                    while (enumerator1.MoveNext())
                        if (!enumerator2.MoveNext() || !comparer.Equals(enumerator1.Current, enumerator2.Current))
                            return false;

                    if (enumerator2.MoveNext())
                        return false;
                    else
                        return true;
                }
            }
        }

        /// <inheritdoc cref="System.Linq.Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource})"/>
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (Object.ReferenceEquals(first, second))
                return true;
            else if (first is ICollection<TSource> && second is ICollection<TSource> && ((ICollection<TSource>)first).Count != ((ICollection<TSource>)second).Count)
                return false;
            else
            {
                using (IEnumerator<TSource> enumerator1 = first.GetEnumerator())
                using (IEnumerator<TSource> enumerator2 = second.GetEnumerator())
                {
                    while (enumerator1.MoveNext())
                        if (!enumerator2.MoveNext() || !comparer.Equals(enumerator1.Current, enumerator2.Current))
                            return false;

                    if (enumerator2.MoveNext())
                        return false;
                    else
                        return true;
                }
            }
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/>
        public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source is IList<TSource>)
            {
                IList<TSource> lst = (IList<TSource>)source;
                int size = lst.Count;
                for (int ind = count; ind < size; ind++)
                    yield return lst[ind];
            }
            else
            {
                int index = 0;
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    while (index < count && enumerator.MoveNext())
                        index++;

                    if (index == count)
                        while (enumerator.MoveNext())
                            yield return enumerator.Current;
                }
            }
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)"/>
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            int index = 0;
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                while (index < count && enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                    index++;
                }
            }
        }

        /// <inheritdoc cref="System.Linq.Enumerable.ToArray{TSource}(IEnumerable{TSource})"/>
        public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
        {
            if (source is ICollection<TSource>)
            {
                ICollection<TSource> coll = (ICollection<TSource>)source;
                TSource[] ret = new TSource[coll.Count];
                coll.CopyTo(ret, 0);
                return ret;
            }
            else
            {
                List<TSource> lst = new List<TSource>();
                foreach (TSource item in source)
                    lst.Add(item);

                return lst.ToArray();
            }
        }

        /// <inheritdoc cref="System.Linq.Enumerable.ToList{TSource}(IEnumerable{TSource})"/>
        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> lst = new List<TSource>();
            foreach (TSource item in source)
                lst.Add(item);

            return lst;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, System.Func{TSource, bool})"/>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource item in source)
                if (predicate(item))
                    yield return item;
        }

        /// <inheritdoc cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, System.Func{TSource, int, bool})"/>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = 0;
            foreach (TSource item in source)
            {
                if (predicate(item, index))
                    yield return item;
                index++;
            }
        }
    }
}
