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
using System.Threading;

namespace ZeNET.Synchronization.Caching
{
#if Framework_4
    /// <summary>
    /// A cache of immutable objects of type <typeparamref name="TObject"/> that
    /// can be built from just a key of type <typeparamref name="TKey"/> using
    /// the supplied object-building function. The objects are retained in
    /// the cache for a minimum time that is specified at the time of construction.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TObject">The type of the immutable object.</typeparam>    
    /// <remarks>
    /// <para>
    /// The removal of objects that are due to be removed (based on the last time they were accessed)
    /// is not automatic and can be triggered by calling <see cref="DeleteOld"/>. To have the method
    /// called periodically, one could use the wrapper provided by <see cref="AutoDeleteImmutableCache{TKey, TObject}"/>.
    /// </para>
    /// <para>
    /// At present, this class is not available when targeting .NET Framework v2.0.
    /// </para>
    /// </remarks>
    /// <threadsafety static="true" instance="true"/>
    public class ImmutableObjectCache<TKey, TObject> : IImmutableObjectCache<TKey, TObject>
    {
        private Dictionary<TKey, ObjectContainer> cache;
        private LinkedList<ObjectContainer> cacheIndex;

        private ReaderWriterLockSlim lockCache = new ReaderWriterLockSlim();
        private object lockDeleter = new object();

        private volatile int concurrentAccessors = 0; // used for courtesy yielding by deleters to other accessors
        private volatile bool deleterNeedsSignaling = false;
        private ManualResetEventSlim deleterSignaler = new ManualResetEventSlim(false);

        private Func<TKey, TObject> objectBuildFunction;
        private long minLifeInTicks;
        private long deletionThreshold = DateTime.MinValue.Ticks;
        private bool anticipateSlowKeyEqualityComparisons;

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
        public ImmutableObjectCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds, IEqualityComparer<TKey> comparer, bool anticipateSlowKeyEqualityComparisons)
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
        public ImmutableObjectCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds) :
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
        /// <seealso cref="GetObject(TKey)"/>
        public ImmutableObjectCache(Func<TKey, TObject> objectBuildFunction, double minLifeSeconds, bool anticipateSlowKeyEqualityComparisons) :
            this(objectBuildFunction, minLifeSeconds, EqualityComparer<TKey>.Default, anticipateSlowKeyEqualityComparisons)
        { }

        /// <summary>
        /// Gets the number of objects currently in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                try
                {
                    this.lockCache.EnterReadLock();
                    return this.cache.Count;
                }
                finally
                {
                    if (this.lockCache.IsReadLockHeld)
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

            if (this.anticipateSlowKeyEqualityComparisons)
            {
                // We mitigate the problem posed by slow equality comparisons of
                // keys by trying to swap out the supplied key with the existing key
                // in the Dictionary, using only a read lock.       
                try
                {
                    this.lockCache.EnterReadLock();
                    if (this.cache.TryGetValue(key, out container))
                        key = container.Key;
                }
                finally
                {
                    if (this.lockCache.IsReadLockHeld)
                        this.lockCache.ExitReadLock();
                }
            }

            int prevAccessors = -1;
            bool needsComputation;
            try
            {
                try { } finally { prevAccessors = Interlocked.Increment(ref this.concurrentAccessors); }
                this.lockCache.EnterWriteLock();                

                if (this.cache.TryGetValue(key, out container))
                {
                    needsComputation = false;
                    Thread.BeginCriticalRegion();
                    container.UpdateTimestamp();
                    ret = container.CachedObject;
                    this.cacheIndex.Remove(container.Node);
                    this.cacheIndex.AddLast(container.Node);
                    Thread.EndCriticalRegion();
                }
                else
                {
                    needsComputation = true;
                    container = new ObjectContainer(key);
                    ret = default(TObject);
                    container.CachedObject = default(TObject);
                    Thread.BeginCriticalRegion();
                    this.cache[key] = container;
                    this.cacheIndex.AddLast(container);
                    container.Node = this.cacheIndex.Last;
                    Thread.EndCriticalRegion();
                }
            }
            finally
            {
                if (this.lockCache.IsWriteLockHeld)
                {
                    this.lockCache.ExitWriteLock();
                    if (Interlocked.Decrement(ref this.concurrentAccessors) == 0 && this.deleterNeedsSignaling)
                    {
                        this.deleterNeedsSignaling = false;
                        this.deleterSignaler.Set();
                    }
                }
                else if (prevAccessors != -1)
                    Interlocked.Decrement(ref this.concurrentAccessors);
            }

            if (needsComputation)
            {
                try
                {
                    container.CachedObject = ret = this.objectBuildFunction(key);
                }
                catch (Exception ex)
                {
                    container.computationException = ex;
                    throw;
                }
                finally
                {
                    container.ComputationFlag.Set();
                }
            }
            else
            {
                container.ComputationFlag.Wait();

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

            int prevAccessorCount = -1;
            try
            {
                try { } finally { prevAccessorCount = Interlocked.Increment(ref this.concurrentAccessors); }
                if (this.lockCache.TryEnterWriteLock(0))
                {
                    if (ret = this.cache.TryGetValue(key, out container))
                    {
                        Thread.BeginCriticalRegion();
                        this.cacheIndex.Remove(container.Node);
                        this.cache.Remove(key);
                        Thread.EndCriticalRegion();
                    }
                }
                else
                {
                    try
                    {
                        this.lockCache.EnterReadLock();
                        if (!this.cache.ContainsKey(key))
                            return false;
                    }
                    finally
                    {
                        if (this.lockCache.IsReadLockHeld)
                            this.lockCache.ExitReadLock();
                    }

                    // The key is definitely present in the cache, so ...
                    this.lockCache.EnterWriteLock();
                    if (ret = this.cache.TryGetValue(key, out container))
                    {
                        this.cacheIndex.Remove(container.Node);
                        this.cache.Remove(key);
                    }
                }
            }
            finally
            {
                if (this.lockCache.IsWriteLockHeld)
                    this.lockCache.ExitWriteLock();

                if (prevAccessorCount != -1 && Interlocked.Decrement(ref this.concurrentAccessors) == 0 && this.deleterNeedsSignaling)
                {
                    this.deleterNeedsSignaling = true;
                    this.deleterSignaler.Set();
                }
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
            while (replacedDeletionThreshold > myDeletionThreshold)
            {
                myDeletionThreshold = replacedDeletionThreshold;
                replacedDeletionThreshold = Interlocked.Exchange(ref this.deletionThreshold, replacedDeletionThreshold);
            }

            DateTime cacheClearedUpToDateTime = DateTime.MinValue;
            DateTime toClearUpToDateTime = new DateTime(myDeletionThreshold);

            bool deleteLockTaken;
            do
            {
                deleteLockTaken = false;
                try
                {                    
                    Monitor.TryEnter(this.lockDeleter, ref deleteLockTaken);
                    if (deleteLockTaken)
                    {
                        do
                        {
                            while (this.concurrentAccessors != 0)
                            {
                                // Only a thread holding this.lockDeleter calls Reset():
                                this.deleterSignaler.Reset();
                                
                                this.deleterNeedsSignaling = true;

                                if (this.concurrentAccessors != 0)
                                    this.deleterSignaler.Wait();
                            }

                            try
                            {
                                if (this.lockCache.TryEnterWriteLock(0))
                                {
                                    while (cacheClearedUpToDateTime <= toClearUpToDateTime && this.concurrentAccessors == 0)
                                    {
                                        if (this.cacheIndex.Count > 0)
                                        {
                                            ObjectContainer earliestEntry = this.cacheIndex.First.Value;
                                            cacheClearedUpToDateTime = earliestEntry.LastAccessTimeUtc;
                                            if (cacheClearedUpToDateTime <= toClearUpToDateTime)
                                            {
                                                Thread.BeginCriticalRegion();
                                                this.cacheIndex.RemoveFirst();
                                                this.cache.Remove(earliestEntry.Key);
                                                Thread.EndCriticalRegion();
                                            }
                                        }
                                        else
                                            cacheClearedUpToDateTime = DateTime.UtcNow;
                                    }
                                }
                            }
                            finally
                            {
                                if (this.lockCache.IsWriteLockHeld)
                                    this.lockCache.ExitWriteLock();
                            }
                        } while (cacheClearedUpToDateTime <= toClearUpToDateTime);
                    }
                    toClearUpToDateTime = new DateTime(Interlocked.Read(ref this.deletionThreshold));
                }
                finally
                {
                    if (deleteLockTaken)
                        Monitor.Exit(this.lockDeleter);                        
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
                lock (this.lockDeleter)
                {
                    try
                    {
                        this.lockCache.EnterWriteLock();
                        while (this.cache.Count > maxCount)
                        {
                            ObjectContainer itemToRemove = this.cacheIndex.First.Value;
                            Thread.BeginCriticalRegion();
                            this.cacheIndex.RemoveFirst();
                            this.cache.Remove(itemToRemove.Key);
                            Thread.EndCriticalRegion();
                        }
                    }
                    finally
                    {
                        if (this.lockCache.IsWriteLockHeld)
                            this.lockCache.ExitWriteLock();
                    }
                }

                this.DeleteOld();
            }
        }

        private class ObjectContainer
        {
            public DateTime LastAccessTimeUtc;
            public LinkedListNode<ObjectContainer> Node = default(LinkedListNode<ObjectContainer>);
            public TKey Key { private set; get; }
            public TObject CachedObject = default(TObject);
            public BooleanFlagNoReset ComputationFlag = new BooleanFlagNoReset();
            public volatile Exception computationException = null;

            public ObjectContainer(TKey key)
            {
                this.Key = key;
                this.LastAccessTimeUtc = DateTime.UtcNow;
            }
            public void UpdateTimestamp() { this.LastAccessTimeUtc = DateTime.UtcNow; }
        }
    }
#endif
}
