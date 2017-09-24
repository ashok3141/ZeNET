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
using System.Text;
using System.Threading;

namespace ZeNET.Synchronization.Caching
{
    /// <summary>
    /// Creates a wrapper around an <see cref="IImmutableObjectCache{TKey, TObject}"/> object and
    /// manages periodic calling of its <see cref="IImmutableObjectCache{TKey, TObject}.DeleteOld"/>
    /// method.
    /// </summary>
    /// <inheritdoc cref="IImmutableObjectCache{TKey, TObject}"/>
    public class AutoDeleteImmutableCache<TKey,TObject> : IImmutableObjectCache<TKey, TObject>
    {
        /// <summary>
        /// The original cache for which periodic deletion is managed by the wrapper.
        /// </summary>
        public IImmutableObjectCache<TKey,TObject> Cache { get; private set; }

        /// <summary>
        /// The interval (in seconds) at which periodic calls are made to
        /// <see cref="IImmutableObjectCache{TKey, TObject}.DeleteOld"/>.
        /// </summary>
        /// <threadsafety static="true" instance="true"/>
        public double DeleteIntervalSeconds { get { return this.deleteIntervalMilliseconds / 1000.0; } }
        private int deleteIntervalMilliseconds;

        // Both this.timer and this.periodicDeletionScheduled below shall only be modified inside a
        // lock (this.timer) block:
        private Timer timer;
        private volatile bool periodicDeletionScheduled = false;

        /// <summary>
        /// Initializes the wrapper to use the specified cache and to call its
        /// <see cref="IImmutableObjectCache{TKey, TObject}.DeleteOld"/> method every
        /// <paramref name="deleteIntervalSeconds"/> seconds.
        /// </summary>
        /// <param name="cache">The cache around which to create the wrapper.</param>
        /// <param name="deleteIntervalSeconds">The interval between deletion calls.</param>
        /// <remarks>
        /// <para>
        /// The periodic calls to <see cref="IImmutableObjectCache{TKey, TObject}.DeleteOld"/> are
        /// suspended when the count of objects in the cache (<see cref="IImmutableObjectCache{TKey, TObject}.Count"/>)
        /// drops to zero and resume when one or more objects are added.
        /// </para>
        /// </remarks>
        public AutoDeleteImmutableCache(IImmutableObjectCache<TKey,TObject> cache, double deleteIntervalSeconds)
        {
            if (deleteIntervalSeconds <= 0)
                throw new ArgumentException("The deletion interval should be a positive number.", "deleteIntervalSeconds");

            this.Cache = cache;
            if (deleteIntervalSeconds >= Int32.MaxValue / 1000.0)
                this.deleteIntervalMilliseconds = Int32.MaxValue;
            else
                this.deleteIntervalMilliseconds = (int)(deleteIntervalSeconds * 1000.0);

            if (this.deleteIntervalMilliseconds == 0)
                this.deleteIntervalMilliseconds = 1;
            
            this.timer = new Timer(this.DoDeletion);
        }

        private void DoDeletion (object state)
        {
            this.Cache.DeleteOld();
            if (this.Cache.Count == 0)
            {
                lock (this.timer)
                {
                    if (this.Cache.Count == 0)
                        this.timer.Change(Timeout.Infinite, Timeout.Infinite);
                    this.periodicDeletionScheduled = false;
                }
            }
        }

        /// <inheritdoc/>
        public TObject GetObject(TKey key)
        {
            if (!this.periodicDeletionScheduled)
            {
                TObject ret = this.Cache.GetObject(key);
                
                lock (this.timer)
                {
                    if (!this.periodicDeletionScheduled)
                    {
                        this.timer.Change(this.deleteIntervalMilliseconds, this.deleteIntervalMilliseconds);
                        this.periodicDeletionScheduled = true;
                    }                        
                }                

                return ret;
            } else
                return this.Cache.GetObject(key);
        }

        /// <inheritdoc/>
        public void DeleteOld()
        {
            this.Cache.DeleteOld();
        }

        /// <inheritdoc/>
        public void TrimTo(int maxCount)
        {
            this.Cache.TrimTo(maxCount);
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            return this.Cache.Remove(key);
        }

        /// <inheritdoc/>
        public int Count { get { return this.Cache.Count; } }
    }
}
