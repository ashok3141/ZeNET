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

namespace ZeNET.Tests.Collections
{
    public class DequeListTester<T> : IList<T>
    {
        private List<T> deque = new List<T>();

        #region IList<T>
        public int Count { get { return this.deque.Count; } }
        public bool IsReadOnly { get { return false; } }
        public T this[int index]
        {
            get { return this.deque[index]; }
            set { this.deque[index] = value; }
        }

        public void Add(T item) { this.deque.Add(item); }
        public void Clear() { this.deque.Clear(); }
        public bool Contains(T item) { return this.deque.Contains(item); }
        public void CopyTo(T[] array, int arrayIndex) { this.deque.CopyTo(array, arrayIndex); }
        public IEnumerator<T> GetEnumerator() { return this.deque.GetEnumerator(); }
        public int IndexOf(T item) { return this.deque.IndexOf(item); }
        public void Insert(int index, T item) { this.deque.Insert(index, item); }
        public bool Remove(T item) { return this.deque.Remove(item); }
        public void RemoveAt(int index) { this.deque.RemoveAt(index); }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.deque.GetEnumerator();
        }
        #endregion

        public void PushRight(T item) { this.deque.Add(item); }
        public void PushLeft(T item) { this.deque.Insert(0, item); }
        public T PopRight()
        {
            T ret = this.deque[this.deque.Count - 1];
            this.deque.RemoveAt(this.deque.Count - 1);
            return ret;
        }
        public T PopLeft()
        {
            T ret = this.deque[0];
            this.deque.RemoveAt(0);
            return ret;
        }
        public bool TryPopRight(out T item)
        {
            if (this.deque.Count == 0)
            {
                item = default(T);
                return false;
            }
            else
            {
                item = this.deque[this.deque.Count - 1];
                this.deque.RemoveAt(this.deque.Count - 1);
                return true;
            }
        }
        public bool TryPopLeft(out T item)
        {
            if (this.deque.Count == 0)
            {
                item = default(T);
                return false;
            }
            else
            {
                item = this.deque[0];
                this.deque.RemoveAt(0);
                return true;
            }
        }
    }
}
