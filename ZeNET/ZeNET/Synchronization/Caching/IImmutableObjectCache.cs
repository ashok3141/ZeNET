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



namespace ZeNET.Synchronization.Caching
{
    /// <summary>
    /// Defines an API for a cache of immutable objects that are retained in the cache for at least
    /// a set amount of time after the last access.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys for the objects in the cache.</typeparam>
    /// <typeparam name="TObject">The type of the objects stored in the cache.</typeparam>
    /// <remarks>
    /// The cache implementation will typically be thread safe.
    /// </remarks>
    public interface IImmutableObjectCache<TKey, TObject>
    {
        /// <summary>
        /// Retrieves a cached object or builds a new one, adds it to the cache, and returns it.
        /// </summary>
        /// <param name="key">The key from which the immutable object can be built.</param>
        /// <returns>The built object.</returns>
        TObject GetObject(TKey key);
        
        /// <summary>
        /// Deletes objects from the cache that have not been accessed for at least the set amount
        /// of time.
        /// </summary>
        void DeleteOld();
        
        /// <summary>
        /// Ensures that the cache contains no more than <paramref name="maxCount"/> objects by
        /// starting deleting the objects with the earliest last-access times.
        /// </summary>
        /// <param name="maxCount">The maximum number of objects to retain in the cache.</param>
        void TrimTo(int maxCount);

        /// <summary>
        /// Removes the object corresponding to <paramref name="key"/>, if it is present in the
        /// cache.
        /// </summary>
        /// <param name="key">The key identifying the object to remove.</param>
        /// <returns><b>True</b> if the object was found and removed, <b>false</b> otherwise.</returns>
        bool Remove(TKey key);

        /// <summary>
        /// The number of objects present in the cache currently.
        /// </summary>
        int Count { get; }
    }
}
