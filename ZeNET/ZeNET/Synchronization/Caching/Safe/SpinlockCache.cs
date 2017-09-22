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
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TObject">The type of the immutable object.</typeparam>    
    /// <threadsafety static="true" instance="true"/>
    public class SpinlockCache<TKey, TObject> : IImmutableObjectCache<TKey, TObject>
    {
        private Dictionary<TKey, ObjectContainer> cache;
        private LinkedList<ObjectContainer> cacheIndex;

        private SpinlockReaderWriter lockCache = new SpinlockReaderWriter();
        private SpinlockReaderWriter lockDeleter = new SpinlockReaderWriter();

        private volatile int concurrentAccessors = 0; // used for courtesy yielding by deleters to other accessors
        private Func<TKey, TObject> objectBuildFunction;
        private long minLifeInTicks;
        private long deletionThreshold = DateTime.MinValue.Ticks;
        private bool anticipateSlowKeyEqualityComparisons;

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
        /// Initializes the cache with the specified settings.
        /// </summary>
        /// <param name="objectBuildFunction">The function that computes the
        /// object to cache from the key.</param>
        /// <param name="minLifeSeconds">The time (in seconds) since the last
        /// request for the object for which the cached object is ineligible for
        /// deletion through calls to <see cref="DeleteOld"/>.</param>
        /// <param name="comparer">The equality comparer to use for determining
        /// whether two keys are equal.</param>
        /// <param name="anticipateSlowKeyEqualityComparisons">
        /// Instructs the cache that the equality comparer for keys may be slow and that the cache
        /// should attempt to pre-fetch a matching key from the cache using a shared lock.
        /// </param>
        /// <remarks>
        /// <para>
        /// There are cases where the type of the keys is a reference type but the cache needs to
        /// use some sort of value comparison for equality. In those cases, <paramref name="comparer"/>
        /// will implement the logic for value-based comparison. And the comparison could take a
        /// long time to complete (if, for example, it performs the equivalent of more than fifty
        /// elementary-type comparisons), especially if the compared objects are in fact equal.
        /// </para>
        /// 
        /// <para>
        /// In those cases, it is recommended that the <see cref="IEqualityComparer{T}.Equals(T, T)"/>
        /// implementation perform a check of reference equality via <see cref="Object.ReferenceEquals(object, object)"/>
        /// and return <b>true</b> immediately if there is indeed reference equality. And it will
        /// probably be beneficial to set <paramref name="anticipateSlowKeyEqualityComparisons"/> to
        /// <b>true</b> if there is a probability of high-concurrency scenarios. Doing so will
        /// improve the throughput of the cache in calls to <see cref="GetObject(TKey)"/>, because it
        /// attempts a pre-fetch of a matching key and if one is found, the matching key is used in
        /// the next steps instead of the supplied key. The shared lock allows several threads to
        /// perform the pre-fetch at the same time.
        /// </para>
        /// <para>
        /// In all other cases, <paramref name="anticipateSlowKeyEqualityComparisons"/> should be
        /// set to <b>false</b>.
        /// </para>
        /// </remarks>
        public SpinlockCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds, IEqualityComparer<TKey> comparer, bool anticipateSlowKeyEqualityComparisons)
        {
            if (minLifeSeconds < 0)
                throw new ArgumentException("Minimum life of the cached object cannot be negative.", "minLifeSeconds");

            this.cache = new Dictionary<TKey, ObjectContainer>(comparer);
            this.cacheIndex = new LinkedList<ObjectContainer>();
            this.objectBuildFunction = objectBuildFunction;
            if (minLifeSeconds * 1E7 >= DateTime.MaxValue.Ticks)
                this.minLifeInTicks = DateTime.MaxValue.Ticks;
            else
                this.minLifeInTicks = (long)(minLifeSeconds * 1E7);

            this.anticipateSlowKeyEqualityComparisons = anticipateSlowKeyEqualityComparisons;
        }

        /// <summary>
        /// Initializes the cache to use the default equality comparer for keys and to use the
        /// specified minimum life for objects in the cache.
        /// </summary>
        /// <param name="objectBuildFunction">The function that computes the
        /// object to cache from the key.</param>
        /// <param name="minLifeSeconds">The time (in seconds) since the last
        /// request for the object for which the cached object is ineligible for
        /// deletion through calls to <see cref="DeleteOld"/>.</param>
        public SpinlockCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds) :
            this(objectBuildFunction, minLifeSeconds, EqualityComparer<TKey>.Default, false)
        { }

        /// <summary>
        /// Initializes the cache using the default equality comparer for keys, the specified
        /// minimum life for objects in the cache, and the specified lock-taking behavior in calls
        /// to <see cref="GetObject(TKey)"/>.
        /// </summary>
        /// <param name="objectBuildFunction">The function that computes the
        /// object to cache from the key.</param>
        /// <param name="minLifeSeconds">The time (in seconds) since the last
        /// request for the object for which the cached object is ineligible for
        /// deletion through calls to <see cref="DeleteOld"/>.</param>
        /// <param name="anticipateSlowKeyEqualityComparisons">
        /// Instructs the cache that the equality comparer for keys may be slow and that the cache
        /// should attempt to pre-fetch a matching key from the cache using a shared lock.
        /// </param>
        /// <remarks>
        /// For more information on <paramref name="anticipateSlowKeyEqualityComparisons"/>, see the note
        /// under <see cref="SpinlockCache{TKey, TObject}.SpinlockCache(Func{TKey, TObject}, double, IEqualityComparer{TKey}, bool)"/>
        /// </remarks>
        public SpinlockCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds, bool anticipateSlowKeyEqualityComparisons) :
            this(objectBuildFunction, minLifeSeconds, EqualityComparer<TKey>.Default, anticipateSlowKeyEqualityComparisons)
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
        /// <remarks>
        /// <para>
        /// If the object-building function throws an exception when called with <paramref name="key"/>,
        /// then it may be appropriate to call <see cref="Remove(TKey)"/> with <paramref name="key"/>
        /// as argument. This is because the <see cref="Exception"/> thrown by the object-building
        /// function is cached just as the built object would have been cached if there had not been
        /// an exception thrown. The life of the cached exception is subject to the same conditions
        /// that govern the life of cached objects, and calls to fetch the object for the same
        /// <paramref name="key"/> (or a matching one) will trigger the throwing of the same cached
        /// exception without another attempt to build the object.
        /// </para>
        /// </remarks>
        public TObject GetObject(TKey key)
        {
            TObject ret = default(TObject);
            ObjectContainer container = default(ObjectContainer);            

            bool lockTaken;

            if (this.anticipateSlowKeyEqualityComparisons)
            {
                // We mitigate the problem posed by slow equality comparisons of
                // keys by trying to swap out the supplied key with the existing key
                // in the Dictionary, using only a read lock.       
                lockTaken = false;
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
            }

            lockTaken = false;
            int prevAccessors = -1;
            bool needsComputation;
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
                try
                {
                    container.CachedObject = ret = this.objectBuildFunction(key);                    
                } catch (Exception ex)
                {
                    container.computationException = ex;
                    throw;
                } finally
                {
                    container.ComputeFinished = true;
                }
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

                if (container.computationException != null)
                    throw container.computationException;
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
            long myDeletionThreshold;
            DateTime now = DateTime.UtcNow;
            if (now.Ticks >= this.minLifeInTicks)
                myDeletionThreshold = now.AddTicks(-this.minLifeInTicks).Ticks;
            else
                myDeletionThreshold = DateTime.MinValue.Ticks;

            long replacedDeletionThreshold = Interlocked.Exchange(ref this.deletionThreshold, myDeletionThreshold);
            // Undo the exchange in case we replaced a later threshold with an earlier threshold
            // (would imply there was a concurrent deleter):
            while (new DateTime(replacedDeletionThreshold) > new DateTime(myDeletionThreshold)) 
            {
                myDeletionThreshold = replacedDeletionThreshold;
                replacedDeletionThreshold = Interlocked.Exchange(ref this.deletionThreshold, replacedDeletionThreshold);
            }

            DateTime cacheClearedUpToDateTime = DateTime.MinValue;
            DateTime toClearUpToDateTime = new DateTime(myDeletionThreshold);

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
                            while (this.concurrentAccessors != 0)
                                Thread.Sleep(0);

                            bool cacheLockTaken = false;
                            try
                            {
                                this.lockCache.TryEnterWriteLock(ref cacheLockTaken);
                                if (cacheLockTaken)
                                {
                                    while (cacheClearedUpToDateTime <= toClearUpToDateTime && this.concurrentAccessors == 0)
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

            Contract.EndContractBlock();

            if (this.Count > maxCount)
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
        private class ObjectContainer
#endif
        {
            public DateTime LastAccessTimeUtc;
            public LinkedListNode<ObjectContainer> Node = default(LinkedListNode<ObjectContainer>);
            public TKey Key { private set; get; }
            public TObject CachedObject = default(TObject);
            public volatile bool ComputeFinished = false;
            public volatile Exception computationException = null;

            public ObjectContainer(TKey key)
            {
                this.Key = key;
                this.LastAccessTimeUtc = DateTime.UtcNow;
            }
            public void UpdateTimestamp() { this.LastAccessTimeUtc = DateTime.UtcNow; }
        }
    }
}
