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

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static ZeNET.Core.BitMath;

namespace ZeNET.Collections
{
    /// <summary>
    /// An all-purpose collection that is simultaneously a deque (double-ended queue) and a list
    /// that allows random access.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the collection</typeparam>
    /// <remarks>
    /// <para>
    /// The double-ended queue (deque) allows adding or removing items from either end (the left end
    /// or the right end). The collection can also be viewed as a list (indeed, it implements
    /// <see cref="IList{T}"/>), and random access is possible through <see cref="this[int]"/>.
    /// </para>
    /// <para>
    /// When the collection is viewed as a list, the left end becomes the beginning of the list, and
    /// the right end becomes the end of the list. Enumerations of the collection report the
    /// elements from the left end to the right end.
    /// </para>
    /// <para>
    /// To use this collection as a regular queue, use <see cref="PushRight(T)"/> as the enqueuing
    /// method and <see cref="PopLeft"/> or <see cref="TryPopLeft(out T)"/> as the dequeuing method.
    /// To use it as a regular stack, use <see cref="PushLeft(T)"/> as the pushing method and
    /// <see cref="PopLeft"/> or <see cref="TryPopLeft(out T)"/> as the popping method. These
    /// choices ensure that the enumerations report the elements in the expected order (which is
    /// the order produced by <see cref="Queue{T}.GetEnumerator"/> and <see cref="Stack{T}.GetEnumerator"/>.
    /// </para>
    /// <para>
    /// All single-element accesses at either end are <i>O</i>(1) operations (adding or removing or
    /// peeking). 
    /// </para>
    /// <para>
    /// <em>NOTE: Unit testing for this class is pending.</em>
    /// </para>
    /// </remarks>
    /// <threadsafety static="true" instance="false"/>
    public class DequeList<T> : IList<T>
    {
        private static readonly IComparer<T> comparer = Comparer<T>.Default;

        private const int MinCapacity = 4;
        private T[] arr = null;
        private int arrBound = -1; //  always arr.Length - 1 if arr != null; -1 if arr == null
        private int lBound = 0;
        private int count = 0;

        private int enumerationCount = 0, modificationBarrier = 0;

        /// <inheritdoc/>
        public int Count { get { return this.count; } }

        /// <inheritdoc/>
        public bool IsReadOnly { get { return false; } }

        /// <inheritdoc/>        
        public T this[int index]
        {
#if Framework_4_5
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get
            {
                if (index >= this.count)
                    throw new ArgumentOutOfRangeException("index");
                Contract.EndContractBlock();

                return this.arr[(this.lBound + index) & this.arrBound];
            }
#if Framework_4_5
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            set
            {
                if (index >= this.count)
                    throw new ArgumentOutOfRangeException("index");
                Contract.EndContractBlock();

                this.modificationBarrier = this.enumerationCount;
                this.arr[(this.lBound + index) & this.arrBound] = value;
            }
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(this.arrBound == (this.arr?.Length ?? 0) - 1, "arrBound value wrong/invalid");
            Contract.Invariant(this.lBound >= 0 && (this.lBound <= this.arrBound || this.arrBound == -1), "lBound value wrong/invalid");
            Contract.Invariant(this.count >= 0 && this.count <= this.arrBound + 1, "count value wrong/invalid");
            Contract.Invariant(this.arrBound < 0 || this.arrBound >= MinCapacity - 1, "length of array below MinCapacity");
            Contract.Invariant((this.arrBound & (this.arrBound + 1)) == 0, "array length is not a power of two");
        }

        /// <summary>
        /// Ensures there will be room for at least one more item by expanding the underlying array
        /// if necessary.
        /// </summary>
        private void MakeRoomForOne()
        {
            Contract.Ensures(this.arr != default(T[]) && this.count <= this.arrBound);

            if (this.count > this.arrBound)
            {
                int newLength = System.Math.Max(MinCapacity, (this.arrBound + 1) << 1);
                T[] replacement = new T[newLength];
                if (this.arrBound >= 0)
                {
                    int diff = this.arrBound + 1 - this.lBound;
                    Array.Copy(this.arr, this.lBound, replacement, 0, diff);
                    Array.Copy(this.arr, 0, replacement, diff, this.lBound);
                }

                this.arr = replacement;
                this.lBound = 0;
                this.arrBound = newLength - 1;
            }
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void pushRight(T item)
        {
            // See PushRight() for Contract
            this.modificationBarrier = this.enumerationCount;
            if (this.count > this.arrBound)
                this.MakeRoomForOne();
            this.arr[(this.lBound + this.count++) & this.arrBound] = item;
        }

        /// <summary>
        /// Adds an item to the right end of the deque (or, if it is viewed as a list, at the
        /// end of the list).
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void PushRight(T item)
        {
            Contract.Ensures(this.Count == Contract.OldValue<int>(this.Count) + 1);
            Contract.Ensures(this[this.Count - 1].Equals(item));
            Contract.Ensures(this.Take(this.Count - 1).SequenceEqual(Contract.OldValue(this.ToArray())));

            pushRight(item);
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private T popRight()
        {
            Contract.Requires(this.count > 0);
            Contract.Ensures(Contract.OldValue(this[this.Count - 1]).Equals(Contract.Result<T>()));
            Contract.Ensures(this.Count == Contract.OldValue<int>(this.Count) - 1);

            this.modificationBarrier = this.enumerationCount;
            int ind = (this.lBound + --this.count) & this.arrBound;
            T ret = this.arr[ind];
            this.arr[ind] = default(T);
            if (this.count <= (this.arrBound >> 2))
                this.downsize();
            return ret;
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private T popLeft()
        {
            Contract.Requires(this.count > 0);
            Contract.Ensures(Contract.OldValue(this[0]).Equals(Contract.Result<T>()));
            Contract.Ensures(this.Count == Contract.OldValue<int>(this.Count) - 1);

            this.modificationBarrier = this.enumerationCount;
            T ret = this.arr[this.lBound];
            this.arr[this.lBound] = default(T);
            this.lBound = (this.lBound + 1) & this.arrBound;
            this.count--;

            if (this.count <= (this.arrBound >> 2))
                this.downsize();
            return ret;
        }

        /// <summary>
        /// Fetches and removes the item at the right end of the deque (or, if it is viewed as a
        /// list, from the end of the list).
        /// </summary>
        /// <returns>The item removed.</returns>
        public T PopRight()
        {
            if (this.Count == 0)
                throw new InvalidOperationException("Cannot remove an item when the collection is empty.");
            Contract.Ensures(this.Count == Contract.OldValue<int>(this.Count) - 1);
            Contract.EndContractBlock();

            return this.popRight();
        }

        /// <summary>
        /// Tries to fetch and remove the item at the right end of the deque (or, if it is
        /// viewed as a list, from the end of the list).
        /// </summary>
        /// <param name="item">The item removed (or default(T) if the collection was empty).</param>
        /// <returns>
        /// <b>True</b> if the fetch was successful, <b>false</b> if it was unsuccessful (because the
        /// collection was empty).
        /// </returns>
        public bool TryPopRight(out T item)
        {
            Contract.Ensures(Contract.OldValue(this.Count) != 0 || (Contract.Result<bool>() == false && this.Count == 0));
            Contract.Ensures(Contract.OldValue(this.Count) != 0 || Contract.ValueAtReturn<T>(out item).Equals(default(T)));

            Contract.Ensures(Contract.OldValue(this.Count) == 0 || (Contract.Result<bool>() == true && this.Count == Contract.OldValue(this.Count) - 1));
            Contract.Ensures(Contract.OldValue(this.Count) == 0 || Contract.ValueAtReturn<T>(out item).Equals(Contract.OldValue(this[this.Count - 1])));

            if (this.count == 0)
            {
                item = default(T);
                return false;
            }
            else
            {
                item = this.popRight();
                return true;
            }
        }

        /// <summary>
        /// Adds an item to the left end of the deque (or, if it is viewed as a list, at the
        /// start of the list).
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void PushLeft(T item)
        {
            Contract.Ensures(this.Count == Contract.OldValue<int>(this.Count) + 1);
            Contract.Ensures(this[0].Equals(item));

            if (this.count > this.arrBound)
                this.MakeRoomForOne();
            this.arr[this.lBound = (this.lBound + this.arrBound) & this.arrBound] = item;
            this.count++;
        }

        /// <summary>
        /// Fetches and removes the leftmost item in the collection (or, if viewed as a list,
        /// the first element).
        /// </summary>
        /// <returns>The removed item.</returns>
        public T PopLeft()
        {
            if (this.Count == 0)
                throw new InvalidOperationException("Cannot remove an item when the collection is empty.");            
            Contract.Ensures(this.Count == Contract.OldValue<int>(this.Count) - 1);
            Contract.EndContractBlock();

            return this.popLeft();
        }

        /// <summary>
        /// Tries to fetch and remove the item at the left end of the deque (or, if it is viewed as
        /// a list, from the start of the list).
        /// </summary>
        /// <param name="item">The item removed (or default(T) if the collection was empty).</param>
        /// <returns>
        /// True if the fetch was successful, false if it was unsuccessful (because the
        /// collection was empty).
        /// </returns>
        public bool TryPopLeft(out T item)
        {
            Contract.Ensures(Contract.OldValue(this.Count) != 0 || (Contract.Result<bool>() == false && this.Count == 0));
            Contract.Ensures(Contract.OldValue(this.Count) != 0 || Contract.ValueAtReturn<T>(out item).Equals(default(T)));

            Contract.Ensures(Contract.OldValue(this.Count) == 0 || (Contract.Result<bool>() == true && this.Count == Contract.OldValue(this.Count) - 1));
            Contract.Ensures(Contract.OldValue(this.Count) == 0 || Contract.ValueAtReturn<T>(out item).Equals(Contract.OldValue(this[0])));

            if (this.count == 0)
            {
                item = default(T);
                return false;
            }
            else
            {
                item = this.popLeft();
                return true;
            }
        }

        /// <inheritdoc/>
        /// <remarks>The order of the elements is from left to right in the collection (or, if
        /// it is viewed as a list, from beginning to end).
        /// </remarks>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
                throw new ArgumentException("arrayIndex");
            Contract.Ensures(this.SequenceEqual(array.Skip(arrayIndex).Take(this.count)));
            Contract.EndContractBlock();

            if (this.count > 0)
            {
                int firstStepCount = this.arrBound - this.lBound + 1;

                if (firstStepCount >= this.count)
                {
                    Contract.Assert(this.lBound + this.count <= this.arrBound + 1, "Branching logic wrong in CopyTo (1).");
                    Array.Copy(this.arr, this.lBound, array, arrayIndex, this.count);
                }
                else
                {
                    Contract.Assert(this.lBound + firstStepCount <= this.arrBound + 1, "Branching logic wrong in CopyTo (2).");
                    Array.Copy(this.arr, this.lBound, array, arrayIndex, firstStepCount);
                    Array.Copy(this.arr, 0, array, arrayIndex + firstStepCount, this.count - firstStepCount);
                }
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.modificationBarrier = this.enumerationCount;
            this.lBound = 0;
            this.count = 0;
            this.arrBound = -1;
            this.arr = default(T[]);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return this.enumerate().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.enumerate().GetEnumerator();
        }

        private IEnumerable<T> enumerate()
        {
            int myBarrier = ++this.enumerationCount;
            int count = this.Count;
            for (int i = 0; i < count; i++)
            {
                yield return this.arr[(this.lBound + i) & this.arrBound];
                if (this.modificationBarrier >= myBarrier)
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            int index = this.indexOf(item);
            if (index == -1)
                return false;
            else
            {
                this.removeAt(index);
                return true;
            }
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return this.indexOf(item) != -1;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Calling this method is the same as calling <see cref="PushRight(T)"/>.
        /// </remarks>
        public void Add(T item)
        {
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);
            Contract.Ensures(this[this.Count - 1].Equals(item));
            Contract.Ensures(this.Take(this.Count - 1).SequenceEqual(Contract.OldValue(this.ToArray())));

            this.pushRight(item);
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void removeAt(int index)
        {
            if (index >= this.Count / 2)
            {
                for (int i = index + 1; i < this.Count; i++)
                    this[i - 1] = this[i];
                this.popRight();
            }
            else
            {
                for (int i = index - 1; i >= 0; i--)
                    this[i + 1] = this[i];
                this.popLeft();
            }
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            if (!(index >= 0 && index < this.Count))
                throw new ArgumentOutOfRangeException("index");
            // Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index < this.Count, "index");
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) - 1);
            Contract.Ensures(this.Take(index).SequenceEqual(Contract.OldValue(this.Take(index).ToArray())));
            Contract.Ensures(this.Skip(index).SequenceEqual(Contract.OldValue(this.Skip(index + 1).ToArray())));
            Contract.EndContractBlock();

            //if (!(index >= 0 && index < this.Count))
            //    throw new ArgumentOutOfRangeException("index was not within the necessary bounds.", "index");

            this.removeAt(index);
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > this.Count)
                throw new ArgumentOutOfRangeException("index", index, "Index is out of bounds.");
            Contract.Ensures(this[index].Equals(item));
            Contract.Ensures(this.Count == Contract.OldValue(this.Count) + 1);
            Contract.EndContractBlock();

            // TODO: The following code should be made more efficient using Array.Copy
            if (this.count == 0)
                this.pushRight(item);
            else if (index >= this.count - index)
            {
                this.pushRight(this[this.count - 1]);
                for (int i = this.count - 2; i >= index + 1; i--)
                    this[i] = this[i - 1];

                this[index] = item;
            }
            else
            {
                T firstElem = this[0];
                for (int i = 0; i < index - 1; i++)
                    this[i] = this[i + 1];
                this.PushLeft(firstElem);
                this[index] = item;
            }
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            return this.indexOf(item);
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int indexOf(T item)
        {
            Contract.Ensures(
                ((Func<int, bool>)
                    (ret =>
                        (ret >= 0 && ret < this.Count && comparer.Compare(this[ret], item) == 0 && this.Take(ret).All(it => comparer.Compare(it, item) != 0)) ||
                        (ret < 0 && this.All(it => comparer.Compare(it, item) != 0))
                    )
                )(Contract.Result<int>())
            );

            Predicate<T> pred = elem => comparer.Compare(elem, item) == 0;
            if (this.count == 0)
                return -1;

            int remArraySize = this.arrBound + 1 - this.lBound;
            if (this.count <= remArraySize)
            {
                int res = Array.FindIndex(this.arr, this.lBound, this.count, pred);
                if (res >= 0)
                    return res - this.lBound;
                else
                    return res;
            }
            else
            {
                int res = Array.FindIndex(this.arr, this.lBound, remArraySize, pred);
                if (res >= 0)
                    return res - this.lBound;
                else
                {
                    res = Array.FindIndex(this.arr, 0, this.count - remArraySize, pred);
                    if (res >= 0)
                        return res + remArraySize;
                    else
                        return res;
                }
            }
        }

        private void downsize()
        {
            Contract.Ensures(this.arrBound + 1 >= 2 * this.count);

            if (this.count <= ((this.arrBound + 1) >> 2) && this.arrBound >= MinCapacity)
            {
                int newLength = System.Math.Max(MinCapacity, PowerOfTwoCeiling(this.count) << 1);

                if (newLength <= this.arrBound)
                {
                    T[] replacement = new T[newLength];
                    int maxAllowedCount = this.arrBound + 1 - this.lBound;

                    if (maxAllowedCount >= this.count)
                    {
                        Contract.Assert(this.lBound + this.count <= this.arr.Length, "Branching logic wrong in Downsize.");
                        Array.Copy(this.arr, this.lBound, replacement, 0, this.count);
                    }
                    else
                    {
                        Array.Copy(this.arr, this.lBound, replacement, 0, maxAllowedCount);
                        Array.Copy(this.arr, 0, replacement, maxAllowedCount, this.count - maxAllowedCount);
                    }

                    this.lBound = 0;
                    this.arr = replacement;
                    this.arrBound = newLength - 1;
                }
            }
        }
    }
}