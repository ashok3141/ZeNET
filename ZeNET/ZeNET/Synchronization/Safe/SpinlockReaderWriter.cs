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

using System.Threading;

namespace ZeNET.Synchronization.Safe
{
    /// <summary>
    /// A reader-writer lock that can be used in "safe" code, i.e., on certain
    /// hosts that do not permit classes or methods that set the
    /// HostProtectionAttribute.ExternalThreading attribute (e.g., SQL Server
    /// running assemblies with SAFE permission only). The functionality of this
    /// struct is broadly similar to that of
    /// <see cref="System.Threading.ReaderWriterLockSlim"/>.
    /// </summary>
    ///
    /// <example>
    /// Here's a simple example of instantiating a reader-writer lock, and
    /// obtaining and releasing a read lock. See individual methods for other
    /// examples.
    /// <code>
    /// SpinlockReaderWriter srw = new SpinlockReaderWriter();
    ///
    /// bool lockTaken = false; // The initial value is required to be false
    /// try
    /// {
    ///     // Reliably record whether the lock was granted, notwithstanding
    ///     // exceptions thrown (even a thread abort), in lockTaken:
    ///     srw.EnterReadLock(ref lockTaken);
    ///     /* This section runs under a shared lock */
    /// }
    /// finally
    /// {
    ///     if (lockTaken)
    ///         srw.ExitReadLock()
    /// }
    /// </code>
    /// </example>
    ///
    /// <remarks>This struct uses only spinlocking to achieve synchronization,
    /// thus being suitable only for cases where the lock will be typically held
    /// for a short duration.
    ///
    /// <para>If a request for a writer lock cannot be granted immediately
    /// because a reader lock is being held, then the request continues to be
    /// refused until all existing and new reader lock requests release their
    /// locks. Thus, reader locks are implicitly given priority.</para>
    ///
    /// <para>There is no guarantee that waiting threads are granted locks in
    /// the order in which they request them.</para>
    /// 
    /// <para>The lock does not record the identity of the thread acquiring the
    /// lock, and does not enforce any affinity of the thread to the lock. One
    /// thread may release a lock acquired by another thread. Also, write locks
    /// may not be entered multiple times by a thread (because thread identity
    /// is not tracked).</para>
    /// </remarks>
    public struct SpinlockReaderWriter
    {
        // The higher 32 bits store the ThreadId fleetingly during a state
        // transition, and the lower 32 bits store the number of concurrent
        // readers at the moment. The lower 32 bits have the special value of
        // 0xffffffff when an exclusive lock is held.        
        private long lockState;
        private const ulong clearHigh = 0x00000000ffffffff;
        private const long xLockLowBits = (long)clearHigh; // alias added for clarity
        private const ulong clearLow = 0xffffffff00000000;

        #region Properties
        /// <summary>
        /// Indicates whether a call to <see cref="EnterReadLock(ref bool)"/>
        /// would succeed without a wait or to
        /// <see cref="TryEnterReadLock(ref bool)"/> on the first attempt.
        /// </summary>
        public bool IsReadable
        {
            get
            {
                return ((ulong)Interlocked.Read(ref this.lockState) & clearHigh) != xLockLowBits;
            }
        }

        /// <summary>
        /// Indicates whether a call to <see cref="EnterReadLock(ref bool)"/>
        /// would succeed without a wait or to
        /// <see cref="TryEnterWriteLock(ref bool)"/> the first attempt.
        /// </summary>
        public bool IsWritable
        {
            get
            {
                return ((ulong)Interlocked.Read(ref this.lockState) & clearHigh) == 0;
            }
        }
        #endregion Properties

        /// <summary>
        /// Try once to acquire a writer lock, which is guaranteed to be
        /// granted if no lock was held at the time of the request.
        /// </summary>
        /// <param name="lockTaken">A flag that is reliably set to indicate
        /// whether the lock was granted.</param>
        /// <example>
        /// The recommended pattern:
        /// <code>
        /// SpinlockReaderWriter srw = new SpinlockReaderWriter();
        /// bool lockTaken = false;
        /// try
        /// {
        ///     // Reliably record whether the lock was granted, notwithstanding
        ///     // exceptions thrown (even a thread abort), in lockTaken:
        ///     srw.TryEnterWriteLock(ref bool lockTaken);
        ///     if (lockTaken)
        ///     {
        ///         // Runs only if the lock was granted on the one attempt.
        ///     }
        /// }
        /// finally
        /// {
        ///     if (lockTaken)
        ///         srw.ExitWriteLock();
        /// }
        /// </code>
        /// </example>
        public void TryEnterWriteLock(ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException("The initial value of lockTaken should be false.", "lockTaken");

            if (((ulong)Interlocked.Read(ref this.lockState) & 0x00000000ffffffff) != 0)
                return;

            ulong myHighBits = (ulong)Thread.CurrentThread.ManagedThreadId << 32;

            ulong existing = UInt64.MaxValue;
            try
            {
                do
                {
                    existing = (ulong)Interlocked.CompareExchange(ref this.lockState, (long)myHighBits, 0L);
                    if (existing == 0)
                    {
                        Interlocked.Exchange(ref this.lockState, (long)xLockLowBits);
                        lockTaken = true;
                        return;
                    }
                } while ((existing & clearHigh) == 0);
            }
            finally
            {
                if (!lockTaken)
                {
                    if (existing == 0 || ((ulong)Interlocked.Read(ref this.lockState) & clearLow) == myHighBits)
                        Interlocked.Exchange(ref this.lockState, 0);
                }
            }
        }

        /// <summary>
        /// Keep spinning until a writer lock is acquired.
        /// </summary>
        /// <param name="lockTaken">A flag that is reliably set to indicate
        /// whether the lock was granted.</param>
        /// <example>
        /// The recommended pattern:
        /// <code>
        /// SpinlockReaderWriter srw = new SpinlockReaderWriter();
        /// bool lockTaken = false; // The initial value is required to be false
        /// try
        /// {
        ///     // Reliably record whether the lock was granted, notwithstanding
        ///     // exceptions thrown (even a thread abort), in lockTaken:
        ///     srw.EnterWriteLock(ref bool lockTaken);
        ///     /*    
        ///         This block runs under a exclusive lock.
        ///     */
        /// }
        /// finally
        /// {
        ///     if (lockTaken)
        ///         srw.ExitWriteLock();
        /// }
        /// </code>
        /// </example>
        public void EnterWriteLock(ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException("The initial value of lockTaken should be false.", "lockTaken");

            this.TryEnterWriteLock(ref lockTaken);
            while (!lockTaken)
            {
                Thread.Sleep(0);
                this.TryEnterWriteLock(ref lockTaken);
            }
        }

        /// <summary>
        /// Exit a write lock.
        /// </summary>
        public void ExitWriteLock()
        {
            ulong existingValue = (ulong)Interlocked.CompareExchange(ref this.lockState, 0, xLockLowBits);
            if (existingValue != clearHigh)
                throw new InvalidOperationException("Cannot ExitWrite when no write lock is held.");
        }

        /// <summary>
        /// Try once to acquire a reader lock, which is guaranteed to be
        /// granted if no writer lock was held at the time of the request.
        /// </summary>
        /// <param name="lockTaken">A flag that is reliably set to indicate
        /// whether the lock was granted.</param>
        public void TryEnterReadLock(ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException("The initial value of lockTaken should be false.", "lockTaken");
            ulong myHighBits = (ulong)Thread.CurrentThread.ManagedThreadId << 32;

            ulong myLockState = 0, myAttempt = UInt64.MaxValue;

            while (true)
            {
                myLockState = (ulong)Interlocked.Read(ref this.lockState);
                if ((myLockState & clearHigh) == xLockLowBits) // an exclusive lock is already held or is about to be held                    
                    return;

                // The following three lines ensure that at no point is myAttempt == myLockState
                ulong temp = myLockState;
                myLockState = UInt64.MaxValue;
                myAttempt = (temp & clearHigh) + (((temp & clearLow) != 0) ? 1UL : 0UL);

                bool skipFinally = false;
                try
                {
                    myLockState = (ulong)Interlocked.CompareExchange(ref this.lockState, (long)(myAttempt | myHighBits), (long)myAttempt); // Line (1)
                    if (myAttempt == myLockState)
                    {
                        Interlocked.Add(ref this.lockState, -((long)(myHighBits - 1))); // Line (2): simultaneously remove the high bits and add 1
                        lockTaken = true;
                        return;
                    }
                    else
                    {
                        skipFinally = true;
                        Thread.Sleep(0);
                    }
                }
                finally
                {
                    if (!lockTaken && !skipFinally) // we try to revert a partial or completed taking of a reader lock
                    {
                        ulong currState = (ulong)Interlocked.Read(ref this.lockState);
                        if ((currState & clearLow) > 0) // we may have run Line (1) and succeeded in the exchange, but quit before Line (2)
                        {
                            if ((currState & clearLow) == myHighBits) // confirm that we successfully compare-exchanged in Line (1), but didn't run Line (2)
                                Interlocked.Add(ref this.lockState, -((long)myHighBits)); // clear our high bits
                        }
                        else if (myAttempt == myLockState) // confirm that we ran Line (2)
                        {
                            Interlocked.Decrement(ref this.lockState); // revert our reader lock
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Keep spinning until a reader lock is acquired.
        /// </summary>
        /// <param name="lockTaken">A flag that is reliably set to indicate
        /// whether the lock was granted.</param>
        public void EnterReadLock(ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException("The initial value of lockTaken should be false.", "lockTaken");

            this.TryEnterReadLock(ref lockTaken);
            while (!lockTaken)
            {
                Thread.Sleep(0);
                this.TryEnterReadLock(ref lockTaken);
            }
        }

        /// <summary>
        /// Exit a reader lock.
        /// </summary>
        public void ExitReadLock()
        {
            ulong newValue = (ulong)Interlocked.Decrement(ref this.lockState);

            if ((newValue & clearHigh) >= clearHigh - 1)
            {
                Interlocked.Increment(ref this.lockState);
                if ((newValue & clearHigh) == clearHigh - 1)
                    throw new InvalidOperationException("Cannot ExitRead when a write lock is held. This ExitRead attempt may have corrupted the lock state.");
                else
                    throw new Exception("Cannot ExitRead when no read lock is held. This ExitRead attempt may have corrupted the lock state.");
            }
        }
    }
}
