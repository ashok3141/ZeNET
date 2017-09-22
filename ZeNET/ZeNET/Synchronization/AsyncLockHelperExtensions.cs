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

namespace ZeNET.Synchronization
{
#if Framework_4
    internal static class AsyncLockHelperExtensions
    {
#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Enqueue(this LinkedList<AsyncLock.SyncTask> queue, AsyncLock.SyncTask item)
        {
            if (item.Node != null)
                throw new ArgumentException("Already assigned to some queue.", "item");
            Contract.EndContractBlock();

            item.Node = queue.AddLast(item);
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool RemoveO1(this LinkedList<AsyncLock.SyncTask> queue, AsyncLock.SyncTask item)
        {
            if (item.Node == null || !Object.ReferenceEquals(item.Node.List, queue) || item.Node.Value != item)
                return false;

            queue.Remove(item.Node);
            item.Node = null;
            return true;
        }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static AsyncLock.SyncTask Dequeue(this LinkedList<AsyncLock.SyncTask> queue)
        {
            if (queue.Count == 0)
                throw new ArgumentException("Queue is empty. Cannot dequeue.", "queue");
            Contract.EndContractBlock();

            LinkedListNode<AsyncLock.SyncTask> node = queue.First;
            queue.RemoveFirst();
            node.Value.Node = null;
            return node.Value;
        }
    }
#endif
}
