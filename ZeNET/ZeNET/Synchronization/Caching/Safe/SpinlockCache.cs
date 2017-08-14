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

using ZeNET.Synchronization.Safe;
using System.Collections.Generic;
using System.Threading;

namespace ZeNET.Synchronization.Caching.Safe
{
    /// <summary>
    /// A cache of immutable objects of type <typeparamref name="TObject"/> that
    /// can be computed from just a key of type <typeparamref name="TKey"/> using
    /// the supplied object-building function. The objects are retained in
    /// the cache for a minimum time that is specified at the time of construction.
    /// 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TObject">The type of the immutable object.</typeparam>
    public class SpinlockCache<TKey, TObject> : IImmutableObjectCache<TKey, TObject>
    {
        protected Dictionary<TKey, ObjectContainer> cache;
        protected LinkedList<ObjectContainer> cacheIndex;

        protected SpinlockReaderWriter lockCache = new SpinlockReaderWriter();
        protected SpinlockReaderWriter lockDeleter = new SpinlockReaderWriter();

        protected int concurrentAccessors = 0; // used for courtesy yielding by deleters to other accessors
        protected Func<TKey, TObject> objectBuildFunction;
        protected long minLifeInTicks;
        protected long deletionThreshold = DateTime.MinValue.Ticks;

#if DEBUG
        public Dictionary<TKey, ObjectContainer> private_cache { get { return this.cache;  } }
        public LinkedList<ObjectContainer> private_cacheIndex { get { return this.cacheIndex; } }

        public int private_concurrentAccessors { get { return this.concurrentAccessors; } } // used for courtesy yielding by deleters to other accessors
        public Func<TKey, TObject> private_objectBuildFunction { get { return this.objectBuildFunction; } }
        public double private_minLifeInTicks { get { return this.minLifeInTicks; } }
        public long private_deletionThreshold { get { return this.deletionThreshold; } }

        public bool private_IsLockDeleteWritable { get { return this.lockDeleter.IsWritable; } }
        public bool private_IsLockDeleteReadable { get { return this.lockDeleter.IsReadable; } }
        public bool private_IsLockCacheWritable { get { return this.lockCache.IsWritable; } }
        public bool private_IsLockCacheReadable { get { return this.lockCache.IsReadable; } }
        
        public readonly bool private_instanceDisposalNecessary;
#endif

        /// <summary>
        /// Initializes the cache to use a custom equality comparer for keys.
        /// </summary>
        /// <param name="objectBuildFunction">The function that computes the
        /// object to cache from the key.</param>
        /// <param name="minLifeSeconds">The time (in seconds) since the last
        /// request for the object for which the cached object is ineligible for
        /// deletion through calls to <see cref="DeleteOld"/>.</param>
        /// <param name="comparer">The equality comparer to use for determining
        /// whether two keys are equal.</param>
        public SpinlockCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds, IEqualityComparer<TKey> comparer)
        {
            if (minLifeSeconds < 0)
                throw new ArgumentException("Minimum life of the cached object cannot be negative.", "minLifeSeconds");

            this.cache = new Dictionary<TKey, ObjectContainer>(comparer);
            this.cacheIndex = new LinkedList<ObjectContainer>();
            this.objectBuildFunction = objectBuildFunction;
            this.minLifeInTicks = (int)(minLifeSeconds * 1E7);
        }

        /// <summary>
        /// Initializes the cache to use the default equality comparer for keys.
        /// </summary>
        /// <param name="objectBuildFunction">The function that computes the
        /// object to cache from the key.</param>
        /// <param name="minLifeSeconds">The time (in seconds) since the last
        /// request for the object for which the cached object is ineligible for
        /// deletion through calls to <see cref="DeleteOld"/>.</param>
        public SpinlockCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds) :
            this(objectBuildFunction, minLifeSeconds, EqualityComparer<TKey>.Default)
        { }

        /// <summary>
        /// Gets the number of objects currently in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                bool lockTaken = false;

                try
                {
                    this.lockCache.EnterReadLock(ref lockTaken);
                    return this.cache.Count;
                }
                finally
                {
                    if (lockTaken)
                        this.lockCache.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the computed object that would be returned by the object build
        /// function supplied at cache construction.
        /// </summary>
        /// <param name="key">The key to look up in the cache or, if no cached
        /// object is found, to send to the object build function to compute the
        /// object.</param>
        /// <returns>The computed object corresponding to <paramref name="key"/>.
        /// </returns>
        public TObject GetObject(TKey key)
        {
            TObject ret = default(TObject);
            ObjectContainer container = default(ObjectContainer);
            bool needsComputation = default(bool);

            // First, we mitigate the problem posed by a custom comparer
            // function for the Dictionary that has a slow equality checking
            // method, by swapping out the supplied key with the existing key
            // in the Dictionary, using only a read lock. TKey will usually be
            // a reference type in these cases, so in the next step, comparison
            // by reference should prove faster.
            bool lockTaken = false;
            try
            {
                this.lockCache.EnterReadLock(ref lockTaken);
                if (this.cache.TryGetValue(key, out container))
                    key = container.Key;
            }
            finally
            {
                if (lockTaken)
                    this.lockCache.ExitReadLock();
            }

            lockTaken = false;
            int prevAccessors = -1;
            try
            {
                try { } finally { prevAccessors = Interlocked.Increment(ref this.concurrentAccessors); }
                this.lockCache.EnterWriteLock(ref lockTaken);

                if (this.cache.TryGetValue(key, out container))
                {
                    needsComputation = false;
                    try { }
                    finally
                    {
                        container.UpdateTimestamp();
                        ret = container.CachedObject;
                        this.cacheIndex.Remove(container.Node);
                        this.cacheIndex.AddLast(container.Node);
                    }
                }
                else
                {
                    needsComputation = true;
                    container = new ObjectContainer(key);
                    ret = default(TObject);
                    container.CachedObject = default(TObject);
                    try { }
                    finally
                    {
                        this.cache[key] = container;
                        this.cacheIndex.AddLast(container);
                        container.Node = this.cacheIndex.Last;
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    this.lockCache.ExitWriteLock();
                    Interlocked.Decrement(ref this.concurrentAccessors);
                }
                else if (prevAccessors != -1)
                    Interlocked.Decrement(ref this.concurrentAccessors);
            }

            if (needsComputation)
            {
                container.CachedObject = ret = this.objectBuildFunction(key);
                container.ComputeFinished = true;
            }
            else
            {
                if (!container.ComputeFinished)
                {
                    do
                    {
                        Thread.Sleep(0);
                    } while (!container.ComputeFinished);
                }

                ret = container.CachedObject;
            }
            return ret;
        }

        /// <summary>
        /// Removes the indicated object, if it is present in the cache.
        /// </summary>
        /// <param name="key">The key to use to look up the object.</param>
        /// <returns>True if the object was present and deleted, false if it
        /// wasn't found.</returns>
        public bool Remove(TKey key)
        {
            bool ret = false;
            ObjectContainer container = default(ObjectContainer);

            bool lockTaken = false;
            int prevAccessorCount = -1;
            try
            {
                try { } finally { prevAccessorCount = Interlocked.Increment(ref this.concurrentAccessors); }
                this.lockCache.TryEnterWriteLock(ref lockTaken);
                if (lockTaken)
                {
                    if (ret = this.cache.TryGetValue(key, out container))
                    {
                        this.cacheIndex.Remove(container.Node);
                        this.cache.Remove(key);
                    }
                }
                else
                {
                    bool readLockTaken = false;
                    try
                    {
                        this.lockCache.EnterReadLock(ref readLockTaken);
                        if (!this.cache.ContainsKey(key))
                            return false;
                    }
                    finally
                    {
                        if (readLockTaken)
                            this.lockCache.ExitReadLock();
                    }

                    // The key is definitely present in the cache, so ...
                    this.lockCache.EnterWriteLock(ref lockTaken);
                    if (ret = this.cache.TryGetValue(key, out container))
                    {
                        this.cacheIndex.Remove(container.Node);
                        this.cache.Remove(key);
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    this.lockCache.ExitWriteLock();
                    Interlocked.Decrement(ref this.concurrentAccessors);
                }
                else if (prevAccessorCount != -1)
                    Interlocked.Decrement(ref this.concurrentAccessors);
            }

            return ret;
        }

        /// <summary>
        /// Removes all objects that are deemed too old, according to the
        /// supplied minLifeSeconds at the time of construction.
        /// </summary>
        /// <remarks>
        /// <para>When concurrent requests are made to this method, the
        /// deletion may finish asynchronously with respect to one or more
        /// requesting threads.</para>
        /// 
        /// <para>All calls to DeleteOld run effectively at a lower priority to
        /// calls to other methods. Specifically, threads running DeleteOld will
        /// only seek to proceed with deleting when no other access is going on.
        /// And after any item is deleted, the deleting thread will yield to
        /// other threads if it is detected that they are attempting to
        /// access the cache.</para>
        /// </remarks>
        public void DeleteOld()
        {
            long myDeletionThreshold = DateTime.UtcNow.AddTicks(-this.minLifeInTicks).Ticks;

            long replacedDeletionThreshold = Interlocked.Exchange(ref this.deletionThreshold, myDeletionThreshold);
            while (new DateTime(replacedDeletionThreshold) > new DateTime(myDeletionThreshold))
            {
                myDeletionThreshold = replacedDeletionThreshold;
                replacedDeletionThreshold = Interlocked.Exchange(ref this.deletionThreshold, replacedDeletionThreshold);
            }

            DateTime cacheClearedUpToDateTime = DateTime.MinValue;
            DateTime toClearUpToDateTime = new DateTime(replacedDeletionThreshold);

            bool deleteLockTaken = false;
            do
            {
                deleteLockTaken = false;
                try
                {
                    this.lockDeleter.TryEnterWriteLock(ref deleteLockTaken);
                    if (deleteLockTaken)
                    {
                        do
                        {
                            while (Thread.VolatileRead(ref this.concurrentAccessors) != 0)
                                Thread.Sleep(0);

                            bool cacheLockTaken = false;
                            try
                            {
                                this.lockCache.TryEnterWriteLock(ref cacheLockTaken);
                                if (cacheLockTaken)
                                {
                                    while (cacheClearedUpToDateTime <= toClearUpToDateTime && Thread.VolatileRead(ref this.concurrentAccessors) == 0)
                                    {
                                        if (this.cacheIndex.Count > 0)
                                        {
                                            ObjectContainer earliestEntry = this.cacheIndex.First.Value;
                                            cacheClearedUpToDateTime = earliestEntry.LastAccessTimeUtc;
                                            if (cacheClearedUpToDateTime <= toClearUpToDateTime)
                                            {
                                                try { }
                                                finally
                                                {
                                                    this.cacheIndex.RemoveFirst();
                                                    this.cache.Remove(earliestEntry.Key);
                                                }
                                            }
                                        }
                                        else
                                            cacheClearedUpToDateTime = DateTime.UtcNow;
                                    }
                                }
                            }
                            finally
                            {
                                if (cacheLockTaken)
                                    this.lockCache.ExitWriteLock();
                            }
                        } while (cacheClearedUpToDateTime <= toClearUpToDateTime);
                    }
                    toClearUpToDateTime = new DateTime(Interlocked.Read(ref this.deletionThreshold));
                }
                finally
                {
                    if (deleteLockTaken)
                        this.lockDeleter.ExitWriteLock();
                }
            } while (deleteLockTaken && cacheClearedUpToDateTime <= toClearUpToDateTime);
        }

        /// <summary>
        /// Deletes computed objects from the cache starting with the oldest
        /// (i.e., accessed last before the last access time of other objects)
        /// until the cache contains no more than <paramref name="maxCount"/>
        /// objects.
        /// </summary>
        /// <param name="maxCount">The maximum number of objects to allow in
        /// the cache after trimming.</param>
        /// <remarks>No object is deleted if the existing count is less than or
        /// equal to <paramref name="maxCount"/>.</remarks>
        public void TrimTo(int maxCount)
        {
            if (maxCount < 0)
                throw new ArgumentException("maxCount must be nonnegative.", "maxCount");

#if Framework_4
            Contract.EndContractBlock();
#endif

            if (this.cache.Count > maxCount)
            {
                bool deleteLockTaken = false;
                try
                {
                    this.lockDeleter.EnterWriteLock(ref deleteLockTaken);
                    bool cacheLockTaken = false;
                    try
                    {
                        this.lockCache.EnterWriteLock(ref cacheLockTaken);
                        while (this.cache.Count > maxCount)
                        {
                            ObjectContainer itemToRemove = this.cacheIndex.First.Value;
                            try { }
                            finally
                            {
                                this.cacheIndex.RemoveFirst();
                                this.cache.Remove(itemToRemove.Key);
                            }
                        }
                    }
                    finally
                    {
                        if (cacheLockTaken)
                            this.lockCache.ExitWriteLock();
                    }
                }
                finally
                {
                    if (deleteLockTaken)
                        this.lockDeleter.ExitWriteLock();
                }

                this.DeleteOld();
            }
        }

#if DEBUG
        public class ObjectContainer
#else
        protected class ObjectContainer
#endif
        {
            public DateTime LastAccessTimeUtc;
            public LinkedListNode<ObjectContainer> Node = default(LinkedListNode<ObjectContainer>);
            public TKey Key { private set; get; }
            public TObject CachedObject = default(TObject);
            public volatile bool ComputeFinished = false;

            public ObjectContainer(TKey key)
            {
                this.Key = key;
                this.LastAccessTimeUtc = DateTime.UtcNow;
            }
            public void UpdateTimestamp() { this.LastAccessTimeUtc = DateTime.UtcNow; }
        }
    }
}
