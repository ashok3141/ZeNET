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
using System.Threading;
using static ZeNET.Core.BitMath;

namespace ZeNET.Collections
{
    /// <summary>
    /// A standard max-heap implementation with additional "linkage" capability to another
    /// collection of the same objects, probably organized by a different key.
    /// </summary>
    /// <typeparam name="T">The type of items in the heap.</typeparam>
    /// 
    /// <remarks>
    /// <para>
    /// The linked heap needs the <see cref="ILinkedHeapNode.HeapIndex"/> property of the objects
    /// stored in it and uses it internally. The value stored in it is not useful outside the
    /// context of a heap and do not ever have to be read by other classes. Certainly, it should not
    /// be modified by other classes. If it is modified, strange unexpected errors will likely occur
    /// later, and exceptions may be thrown by calls to the methods of the heap.
    /// </para>
    /// <para>
    /// The heap by default uses <see cref="Comparer{T}.Default"/> to determine the ordering of the
    /// items within it, though this can be overridden at the time of construction. "Duplicates" are
    /// permitted (that is, a comparison between two objects may return 0), though a single object
    /// can be stored only once. The sameness of two objects is always determined via
    /// <see cref="object.ReferenceEquals(object, object)"/>.
    /// </para>
    /// </remarks>
    /// <threadsafety static="true" instance="false"/>
    public class LinkedHeap<T> : IPriorityQueue<T>
        where T : class, ILinkedHeapNode
    {
        private const int MinCapacity = 4;
        private T[] heap;
        private int heapLength;
        private Comparison<T> doCompare;

        private Action notifyOfModifications = default(Action);

        /// <summary>
        /// Initializes the linked heap to use a custom comparison function for elements to maintain
        /// the heap property.
        /// </summary>
        /// <param name="comparison">The comparison function to use.</param>
        public LinkedHeap(Comparison<T> comparison)
        {
            this.doCompare = comparison;
            this.heapLength = 0;
        }

        /// <summary>
        /// Initializes the linked heap to use a custom comparer for comparing elements to determine
        /// the ordering among them. The check for whether two elements are the same is always done
        /// using <see cref="object.ReferenceEquals(object, object)"/>.
        /// </summary>
        /// <param name="comparer">The comparer whose <see cref="IComparer{T}.Compare(T, T)"/> is
        /// used as the comparison function.</param>
        public LinkedHeap(IComparer<T> comparer)
            : this(comparer.Compare) { }

        /// <summary>
        /// Initializes the linked heap, using the default comparer <see cref="Comparer{T}.Default"/>
        /// for comparison of elements.
        /// </summary>
        public LinkedHeap() : this(Comparer<T>.Default.Compare) { }

        /// <inheritdoc cref="ICollection{T}.Count"/>
        public int Count { get { return this.heapLength; } }
        bool ICollection<T>.IsReadOnly { get { return false; } }

        /// <summary>
        /// Indicates whether the heap is empty.
        /// </summary>
        public bool IsEmpty { get { return this.heapLength == 0; } }

        /// <summary>
        /// Fetches the maximum value stored in the heap, without deleting it.
        /// </summary>
        /// <remarks>
        /// This operation has a time complexity of <i>O</i>(1).
        /// </remarks>
        public T Max
        {
            get
            {
                if (this.heapLength == 0)
                    throw new InvalidOperationException("Heap is empty. Max does not exist.");
                return this.heap[0];
            }
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void noteModification()
        {
            if (this.notifyOfModifications != null)
            {
                this.notifyOfModifications();
                this.notifyOfModifications = null;
            }
        }

        /// <inheritdoc/>
        /// <remarks>This operation has a time complexity <i>O</i>(lg <i>n</i>), where <i>n</i> is
        /// the number of items in the heap.</remarks>
        public void Add(T item)
        {
            this.noteModification();

            if (this.heap == default(T[]) || this.heapLength == this.heap.Length)
            {
                int newLength = PowerOfTwoCeiling(this.heapLength) << 1;
                if (newLength < MinCapacity)
                    newLength = MinCapacity;
                T[] replacement = new T[newLength];
                this.heap?.CopyTo(replacement, 0);
                this.heap = replacement;
            }

            this.heap[this.heapLength] = item;
            this.heap[this.heapLength].HeapIndex = this.heapLength;
            this.swimUp(this.heapLength++);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.noteModification();
            this.heap = default(T[]);
            this.heapLength = 0;
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(this.heap, 0, array, arrayIndex, this.heapLength);
        }

        private IEnumerator<T> getEnumerator()
        {
            bool changed = false;
            Action acceptNotification = () => { changed = true; };
            try
            {                
                this.notifyOfModifications += acceptNotification;

                for (int i = 0; i < this.heapLength; i++)
                {
                    yield return this.heap[i];
                    if (changed)
                        throw new InvalidOperationException("The heap has been modified. Cannot continue enumeration.");
                }
            } finally
            {
                if (!changed)
                    this.notifyOfModifications -= acceptNotification;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return this.getEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.getEnumerator();
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            int ind = item.HeapIndex;
            return ind >= 0 && ind < this.heapLength && Object.ReferenceEquals(this.heap[ind], item);
        }

        /// <summary>
        /// Deletes the maximum element in the heap and returns it.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This operation has a time complexity <i>O</i>(lg <i>n</i>), where <i>n</i> is
        /// the number of elements in the heap.</remarks>
        public T DeleteMax()
        {
            if (this.Count == 0)
                throw new InvalidOperationException("Heap is empty. Max does not exist.");
            Contract.EndContractBlock();

            this.noteModification();

            T ret = this.heap[0];
            ret.HeapIndex = -1;
            if (this.heapLength == 1)
                this.Clear();
            else
            {
                int end = --this.heapLength;
                if ((this.heapLength << 2) <= this.heap.Length && this.heap.Length > MinCapacity)
                {
                    T[] replacement = new T[PowerOfTwoCeiling(this.heapLength) << 1];
                    Array.Copy(this.heap, 1, replacement, 1, end - 1);
                    replacement[0] = this.heap[end];
                    this.heap = replacement;                    
                }
                else
                {
                    this.heap[0] = this.heap[end];
                    this.heap[end] = default(T);
                }

                this.heap[0].HeapIndex = 0;
                this.sink(0);
            }

            return ret;
        }

        /// <summary>
        /// Causes an element to swim up through its ancestry to its right location in the heap.
        /// </summary>
        /// <param name="ind">The index of the element to cause to swim up.</param>
        /// <returns>True if there was any movement necessary (and hence occurred), false if not.</returns>
        private bool swimUp(int ind)
        {
            int parent = (ind - 1) >> 1; // N.B.: (-1 >> 1) is -1.
            if (parent >= 0 && this.doCompare(this.heap[parent], this.heap[ind]) < 0)
            {
                T swimmingElement = this.heap[ind];
                this.heap[ind] = this.heap[parent];
                this.heap[ind].HeapIndex = ind;

                ind = parent;
                parent = (ind - 1) >> 1;
                while (parent >= 0 && this.doCompare(this.heap[parent], swimmingElement) < 0)
                {
                    this.heap[ind] = this.heap[parent];
                    this.heap[ind].HeapIndex = ind;
                    ind = parent;
                    parent = (ind - 1) >> 1;
                }

                this.heap[ind] = swimmingElement;
                this.heap[ind].HeapIndex = ind;
                return true;
            }
            else
                return false;
        }

        private bool sink(int ind)
        {
            int uBound = this.heapLength - 1;
            int greaterChild = (ind << 1) | 1;
            if (greaterChild < uBound)
            {
                if (this.doCompare(this.heap[greaterChild + 1], this.heap[greaterChild]) >= 0)
                    greaterChild++;

                if (this.doCompare(this.heap[ind], this.heap[greaterChild]) < 0)
                {
                    T sinkingElement = this.heap[ind];
                    this.heap[ind] = this.heap[greaterChild];
                    this.heap[ind].HeapIndex = ind;
                    ind = greaterChild;
                    greaterChild = (ind << 1) | 1;

                    while (greaterChild <= uBound)
                    {
                        if (greaterChild < uBound && this.doCompare(this.heap[greaterChild + 1], this.heap[greaterChild]) >= 0)
                            greaterChild++;

                        if (this.doCompare(sinkingElement, this.heap[greaterChild]) < 0)
                        {
                            this.heap[ind] = this.heap[greaterChild];
                            this.heap[ind].HeapIndex = ind;
                        }
                        else
                            break;

                        ind = greaterChild;
                        greaterChild = (ind << 1) | 1;
                    }

                    this.heap[ind] = sinkingElement;
                    this.heap[ind].HeapIndex = ind;
                    return true;
                }
            } else if (greaterChild == uBound && this.doCompare(this.heap[ind], this.heap[greaterChild]) < 0)
            {
                T temp = this.heap[ind];
                this.heap[ind] = this.heap[greaterChild];
                this.heap[greaterChild] = temp;
                this.heap[greaterChild].HeapIndex = greaterChild;
                this.heap[ind].HeapIndex = ind;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Registers the possibly changed ordering of <paramref name="item"/> relative to the other
        /// objects.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <remarks>
        /// <para>
        /// This method should be called whenever a change is made to <paramref name="item"/> that
        /// might change the result of comparing it (using the comparer specified for the heap) to
        /// other objects stored in the heap.
        /// </para>
        /// <para>
        /// If changes to multiple items in the heap are to be made that require calls to
        /// <see cref="Refresh(T)"/>, then the call should be made after changes made to every item.
        /// It is not enough to make calls for all items together at the end.
        /// </para>
        /// <para>
        /// In the absence of this method, one would have to call <see cref="Remove(T)"/>, followed
        /// by <see cref="Add(T)"/>, for the item. Calling only <see cref="Refresh(T)"/> is a much
        /// faster alternative.
        /// </para>
        /// </remarks>
        public void Refresh(T item)
        {
            this.noteModification();

            int ind = item.HeapIndex;
            if (ind < 0 || ind >= this.heapLength || !object.ReferenceEquals(this.heap[ind], item))
                throw new ArgumentException("Either item does not belong to the heap or was not properly updated.", "item");

            if (!this.swimUp(ind))
                this.sink(ind);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            int ind = item.HeapIndex;
            int end = this.heapLength - 1;
            if (ind < 0 || ind > end || !object.ReferenceEquals(this.heap[ind], item))
                return false;

            this.noteModification();

            item.HeapIndex = -1;
            if (this.heap.Length == 1)
                this.Clear();
            else
            {
                if (ind < end)
                {
                    this.heap[ind] = this.heap[end];
                    this.heap[ind].HeapIndex = ind;
                    this.heap[end] = default(T);
                    this.heapLength--;

                    if (!this.swimUp(ind))
                        this.sink(ind);
                }
                else
                {
                    this.heap[end] = default(T);
                    this.heapLength--;
                }

                if ((this.heapLength << 2) <= this.heap.Length && this.heap.Length > MinCapacity)
                {
                    T[] replacement = new T[PowerOfTwoCeiling(this.heapLength) << 1];
                    Array.Copy(this.heap, 0, replacement, 0, this.heapLength);
                    this.heap = replacement;
                }
            }
            return true;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(this.hasLinkageIntegrity(), "The heap does not have linkage integrity.");
            Contract.Invariant(this.maintainsHeapProperty(), "The heap is violating the required heap property.");
        }

        #region Debugging
        private bool hasLinkageIntegrity()
        {
            for (int i = 0; i < this.heapLength; i++)
                if (this.heap[i].HeapIndex != i)
                    return false;
            return true;
        }

        private bool maintainsHeapProperty()
        {
            for (int i = 0; i < this.heapLength; i++)
            {
                int child = (i << 1) | 1;

                if (child < this.heapLength && this.doCompare(this.heap[i], this.heap[child]) < 0)
                    return false;
                if (++child < this.heapLength && this.doCompare(this.heap[i], this.heap[child]) < 0)
                    return false;
            }
            return true;
        }
        #endregion
    }
}
