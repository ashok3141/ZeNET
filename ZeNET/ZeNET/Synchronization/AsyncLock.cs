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
using System.Threading;

#if Framework_4
using System.Threading.Tasks;
#endif

namespace ZeNET.Synchronization
{
#if Framework_4
    /// <summary>
    /// Represents an exclusive lock that is the asynchronous analog of locks obtained using
    /// <see cref="Monitor"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the present time, the .NET Framework lacks a high-performance asynchronous (non-blocking)
    /// lock, instead providing slow alternatives such as <see cref="SemaphoreSlim"/> (to be used
    /// via <see cref="SemaphoreSlim.WaitAsync()"/>. In an application that needs to share I/O handles,
    /// such as <see cref="System.IO.Stream"/> objects or database connection objects, that support
    /// asynchronous operations, there is a need to obtain exclusive locks to perform them. Using
    /// the <see cref="Monitor"/> class may be impossible, because the locks so obtained have thread
    /// affinity, so that it cannot tolerate an asynchronous continuation inside the critical
    /// section. Or doing so will give up some of the gains of asynchronous programming, because
    /// <see cref="Monitor"/> locks block the current thread. (The <b>lock</b> keyword in C#
    /// implicitly uses the <see cref="Monitor"/> class.)
    /// </para>
    /// 
    /// <para>
    /// <see cref="AsyncLock"/> solves these problems, by providing an asynchronous lock that
    /// returns an awaitable <see cref="Task{Boolean}"/> "receipt" to the requestor of an exclusive
    /// lock. The receipt plays the part of the thread ID in synchronous locks. Its value (<see cref="Task{Boolean}.Result"/>
    /// indicates whether the lock was granted and may also be used to query whether the receipt
    /// represents a current lock. The lock is released using the same receipt.
    /// </para>
    /// <para>
    /// In the cases where there is no contention for the lock, <see cref="AsyncLock"/> performs
    /// quite well. If <see cref="Monitor"/> locks take about 20 ns per <see cref="Monitor.Enter(object)"/>-and-<see cref="Monitor.Exit(object)"/>,
    /// <see cref="AsyncLock"/> takes about 45 ns (when using reusable receipts, that is, <see cref="AsyncLock.ReusesReceipts"/>
    /// is true).
    /// </para>
    /// <para>
    /// Locks are granted in a strict FIFO (first-in-first-out) order. An apparent violation occurs
    /// when locks are being granted synchronously, wherein a requestor (say <i>A</i>) that began an
    /// entering request later than another (say <i>B</i>) may be granted the lock before <i>B</i>. This is not a
    /// real violation, because in such cases the call to the entering method by <i>B</i> will not return a
    /// value before the call by <i>A</i> begins.
    /// </para>
    /// <para>
    /// Whenever the receipt to a requestor (say <i>A</i>) is an incomplete task (meaning that the
    /// lock has not yet been granted), it will always be true that any enter request (by, say,
    /// <i>B</i>) initiated after <i>A</i> received its receipt will not be granted the lock before
    /// <i>A</i>'s receipt <see cref="Task{Boolean}"/> is completed (by virtue of the lock being
    /// granted or timed out or canceled).
    /// </para>
    /// </remarks>    
    public class AsyncLock
    {
        /// <summary>
        /// Indicates whether this lock reuses receipts when the lock can be granted synchronously
        /// (see <see cref="AsyncLock.AsyncLock(bool)"/> for more information on receipt reuse).
        /// </summary>
        public bool ReusesReceipts { get { return this.reusesReceipts; } }
        private readonly bool reusesReceipts;
        private readonly LinkedList<SyncTask> waitingTasks = new LinkedList<SyncTask>(); // lock(waitingTasks) is taken to be locking timeoutTasks and syncTaskLookup as well
        private readonly Dictionary<Task<bool>, SyncTask> syncTaskLookup = new Dictionary<Task<bool>, SyncTask>();

        private volatile int queueCount = 0;
        private const int maxSpins = 200;
        private const int maxSpinWaitLimit = 4;
        private int spinwaitingThreadCount = 0;

        private readonly Task<bool> stockTrueResult;
        private readonly Task<bool> stockFalseResult;
        private volatile Task<bool> lockHeldBy = null;

        private SortedSet<SyncTask> timeoutTasks = null;
        private Timer timer = null;

        /// <summary>
        /// Initializes the lock using the requested setting for reusing request receipts.
        /// </summary>
        /// <param name="reuseReceipts">Controls whether the lock will return the same (true or
        /// false) receipt more than once in case the result of the lock attempt is synchronously
        /// known. Specify <b>true</b> to allow reuse and false to disallow reuse.</param>
        /// <remarks>
        /// <para>
        /// Allowing receipt re-use avoids the (very slight) overhead of allocating a new <see cref="Task{Boolean}"/>
        /// object in the most common case: when the lock can be granted synchronously or almost
        /// synchronously (including brief spinning). When an incomplete <see cref="Task{Boolean}"/>
        /// is returned, it is always a new object that has never been used before as a receipt and
        /// will not be in the future, regardless of <see cref="AsyncLock.ReusesReceipts"/>.
        /// </para>
        /// <para>
        /// Disallowing re-use has no utility other than as insurance against extremely careless
        /// handling of the receipts: when a requestor receives a stock true-resulting receipt, it
        /// is possible that it will try to release the lock more than once, through careless
        /// programming. Though the <see cref="AsyncLock.Exit(ref Task{bool})"/> method resets the
        /// receipt to <b>null</b> when the lock release is successful, the requestor may be
        /// retaining a copy of it. And if it tries to release the lock again using the same receipt,
        /// it may inadvertently release the lock granted to another requestor, because the other
        /// requestor may have received the same lock receipt.
        /// </para>
        /// <para>
        /// Besides the above scenario, the only disadvantage of re-using receipts is that if a copy
        /// is made of the receipt and the copy is used to check whether the lock is held (via <see cref="AsyncLock.IsHeldBy(Task{bool})"/>),
        /// the result may be true, even if the lock was released using the original receipt. The
        /// example below illustrates this:
        /// </para>
        /// <code language="C#" title="Dangerous use of AsyncLock receipt" source="..\ZeNET\Examples\Synchronization\AsyncLockExamples.cs" region="EnterAsyncExample1"/>
        /// </remarks>
        public AsyncLock(bool reuseReceipts)
        {

            if (reuseReceipts)
            {
                this.stockTrueResult = Task<bool>.FromResult(true);
                this.stockFalseResult = Task<bool>.FromResult(false);
                this.reusesReceipts = true;
            }
            else
            {
                this.stockTrueResult = null;
                this.stockFalseResult = null;
                this.reusesReceipts = false;
            }

            this.initializeTimeoutManager();
        }

        /// <summary>
        /// Indicates whether the lock is granted to any requestor.
        /// </summary>
        public bool IsHeld { get { return this.lockHeldBy != null; } }

        /// <summary>
        /// Indicates whether the lock is held against the receipt <paramref name="receipt"/>.
        /// </summary>
        /// <param name="receipt">The receipt that is tested for ownership of the lock.</param>
        /// <returns><b>True</b> if <paramref name="receipt"/> holds the lock, <b>false</b> otherwise.</returns>
        public bool IsHeldBy(Task<bool> receipt)
        {
            if (receipt == null || this.lockHeldBy == null)
                return false;

            if (Object.ReferenceEquals(receipt, this.lockHeldBy))
                return true;

            lock (this.waitingTasks)
                return Object.ReferenceEquals(receipt, this.lockHeldBy);
        }

        /// <summary>
        /// Enters the lock and reliably sets the receipt for the action.
        /// </summary>
        /// <param name="receipt">The variable that will contain the receipt for the grant request.
        /// If it is set at all by the method, its value will be <b>true</b>. When the lock is
        /// granted synchronously (or almost synchronously, after brief spinning), <paramref name="receipt"/>
        /// will be a completed task, meaning that any attached continuations will proceed
        /// synchronously. But if it cannot be granted synchronously, <paramref name="receipt"/> will
        /// be an incomplete task.
        /// </param>
        /// <returns>The same receipt that was assigned to <paramref name="receipt"/>, as a convenience.</returns>
        /// <remarks>
        /// <para>
        /// When <paramref name="receipt"/> is set by the method, its <see cref="Task{Boolean}.Result"/>
        /// will be <b>true</b>. When the lock is granted synchronously (or almost synchronously, after brief spinning),
        /// <paramref name="receipt"/> will be a completed task, meaning that any attached continuations
        /// will proceed synchronously. But if it cannot be granted synchronously, <paramref name="receipt"/> will
        /// be initially an incomplete task. It will be completed when the lock can finally be granted.
        /// </para>
        /// <para>
        /// The recommended pattern for obtaining and releasing a lock using this method is:
        /// <code language="C#" title="Recommended pattern" source="..\ZeNET\Examples\Synchronization\AsyncLockExamples.cs" region="EnterAsyncExample2"/>
        /// </para>
        /// </remarks>
        public Task<bool> EnterAsync(ref Task<bool> receipt)
        {
            // 1. Try the fastest method first.
            bool fastEnterSucceeded = false;
            Task<bool> ret = this.reusesReceipts ? stockTrueResult : Task<bool>.FromResult(true);
            try { }
            finally
            {
                if (Interlocked.CompareExchange(ref this.lockHeldBy, ret, null) == null)
                {
                    receipt = ret;
                    fastEnterSucceeded = true;
                }
            }

            if (fastEnterSucceeded)
                return receipt;

            // 2. Since the fast method failed, try spinwaiting and retrying up to maxSpins times
            if (this.queueCount <= 0)
            {
                int myIncrValue = 0;
                try
                {
                    try { } finally { myIncrValue = Interlocked.Increment(ref this.spinwaitingThreadCount); }

                    SpinWait sWait = new SpinWait();
                    while (sWait.Count < maxSpins && this.queueCount <= 0 && this.spinwaitingThreadCount <= maxSpinWaitLimit)
                    {
                        sWait.SpinOnce();

                        try { } finally
                        {
                            if (Interlocked.CompareExchange(ref this.lockHeldBy, ret, null) == null)
                            {
                                receipt = ret;
                                fastEnterSucceeded = true;
                            }
                        }

                        if (fastEnterSucceeded)
                            return receipt;
                    }
                } finally
                {
                    if (myIncrValue > 0)
                        Interlocked.Decrement(ref this.spinwaitingThreadCount);
                }
            }

            // 3. Since spinwaiting failed, queue up            
            return this.enterGeneralized(ref receipt, Timeout.Infinite, CancellationToken.None);            
        }

        /// <summary>
        /// Tries to obtain a lock synchronously.
        /// </summary>
        /// <param name="receipt">The receipt for the operation.</param>
        /// <returns>The same receipt object that was assigned to <paramref name="receipt"/>.</returns>
        /// <remarks>
        /// <para>
        /// The returned receipt will always be a completed task. Its <see cref="Task{Boolean}.Result"/>
        /// will be <b>true</b> if the lock was granted and <b>false</b> if not.
        /// </para>
        /// <para>
        /// The recommended pattern for this is:
        /// <code language="C#" title="Recommended pattern" source="..\ZeNET\Examples\Synchronization\AsyncLockExamples.cs" region="EnterAsyncExample3"/>
        /// </para>
        /// <seealso cref="EnterAsync(ref Task{bool})"/>.
        /// </remarks>
        public Task<bool> TryEnterAsync(ref Task<bool> receipt)
        {
            Task<bool> ret = this.reusesReceipts ? stockTrueResult : Task<bool>.FromResult(true);
            try { }
            finally
            {
                if (Interlocked.CompareExchange(ref this.lockHeldBy, ret, null) == null)
                    receipt = ret;
                else
                    receipt = this.reusesReceipts ? this.stockFalseResult : Task<bool>.FromResult(false);
            }

            return receipt;
        }

        // TODO: Make this public after more rigorous unit testing
        /// <summary>
        /// Tries to obtain a lock subject to a timeout and/or a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="receipt">The receipt for the operation.</param>
        /// <param name="timeout">The time in milliseconds to wait to obtain the lock, or <see cref="Timeout.Infinite"/>
        /// to ask for indefinite waiting.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>The same receipt object that was assigned to <paramref name="receipt"/>.</returns>
        /// <remarks>
        /// <para>
        /// NOTE that this method is marked protected because it requires further unit testing,
        /// though it apparently works as intended. When such testing is complete, it will be made
        /// public.
        /// </para>
        /// <para>
        /// The receipt gets a <see cref="Task{Boolean}.Result"/> value of <b>false</b>, if the
        /// request times out, and <b>true</b> if the lock is obtained before that. If a
        /// cancellation is requested before <see cref="Task{Boolean}.Result"/> is assigned a value,
        /// then the receipt goes into a canceled state.
        /// </para>
        /// </remarks>
        protected Task<bool> TryEnterAsync(ref Task<bool> receipt, int timeout, CancellationToken cancellationToken)
        {
            if (timeout < 0 && timeout != Timeout.Infinite)
                throw new ArgumentException("The timeout must be nonnegative or Timeout.Infinite.", "timeout");
            Contract.EndContractBlock();

            if (cancellationToken.IsCancellationRequested)
                return new Task<bool>(returnFalse, cancellationToken);
            else if (timeout == 0)
                return this.TryEnterAsync(ref receipt);

            bool fastEnterSucceeded = false;
            Task<bool> ret = this.reusesReceipts ? stockTrueResult : Task<bool>.FromResult(true);
            try { }
            finally
            {
                if (Interlocked.CompareExchange(ref this.lockHeldBy, ret, null) == null)
                {
                    receipt = ret;
                    fastEnterSucceeded = true;
                }
            }

            if (fastEnterSucceeded)
                return receipt;
            else
                return this.enterGeneralized(ref receipt, timeout, cancellationToken);
        }

        private static bool returnFalse() { return false; }

        private void initializeTimeoutManager()
        {
            lock (this.waitingTasks)
            {
                if (this.timeoutTasks == null)
                {
                    this.timeoutTasks = new SortedSet<SyncTask>();
                    this.timer = new Timer(this.clearTimedOutRequests, this, Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private Task<bool> enterGeneralized(ref Task<bool> receipt, int timeout, CancellationToken cancellationToken)
        {
            if (timeout != Timeout.Infinite && timeout < 0)
                throw new ArgumentException("Timeout should be either nonnegative or Timeout.Infinite.", "timeout");
            Contract.EndContractBlock();

            if (cancellationToken.IsCancellationRequested)
                return new Task<bool>(returnFalse, cancellationToken);

            DateTime timeoutMomentUtc = timeout > 0 ? DateTime.UtcNow.AddMilliseconds(timeout) : default(DateTime);

            lock (this.waitingTasks)
            {
                try
                {
                    SpinWait spinWait = new SpinWait();
                    while (Interlocked.CompareExchange(ref this.queueCount, 1, 0) < 0)
                        spinWait.SpinOnce();
                    
                    if (this.waitingTasks.Count == 0)
                    {
                        Task<bool> ret = this.reusesReceipts ? stockTrueResult : Task<bool>.FromResult(true);
                        bool returnNow = false;
                        try { } finally
                        {
                            if (Interlocked.CompareExchange(ref this.lockHeldBy, ret, null) == null)
                            {                                
                                receipt = ret;
                                returnNow = true;
                            }
                        }
                        if (returnNow)
                            return receipt;
                    }

                    TaskCompletionSource<bool> src = new TaskCompletionSource<bool>();
                    SyncTask sTask = timeout > 0 ? new SyncTask(src, cancellationToken, timeoutMomentUtc, this)
                        : new SyncTask(src, cancellationToken, this);

                    try { } finally
                    {
                        if (timeout > 0)
                        {
                            if (timeoutMomentUtc < DateTime.UtcNow)
                                sTask.TrySignalSource(SyncTask.SourceStates.False);
                            else
                            {
                                this.waitingTasks.Enqueue(sTask);
                                this.syncTaskLookup[sTask.ReceiptTask] = sTask;

                                if (this.timeoutTasks == null) this.initializeTimeoutManager();

                                if (this.timeoutTasks.Count == 0 || sTask.CompareTo(this.timeoutTasks.Max) > 0)
                                {
                                    this.timeoutTasks.Add(sTask);
                                    int delay = Math.Max(0, (int)Math.Ceiling((timeoutMomentUtc - DateTime.UtcNow).TotalMilliseconds));
                                    this.timer.Change(delay, Timeout.Infinite);
                                }
                                else
                                    this.timeoutTasks.Add(sTask);
                            }
                        }
                        else if (timeout == Timeout.Infinite)
                        {
                            this.waitingTasks.Enqueue(sTask);
                            this.syncTaskLookup[sTask.ReceiptTask] = sTask;
                        }
                        receipt = src.Task;
                    }

                    return receipt;
                }
                finally
                {
                    this.queueCount = this.waitingTasks.Count;
                }
            }

        }

        private void clearTimedOutRequests(object state)
        {
            lock (this.waitingTasks)
            {
                while (this.timeoutTasks.Count > 0)
                {
                    SyncTask sTask = this.timeoutTasks.Max;
                    Thread.BeginCriticalRegion();
                    if (sTask.TimeoutMomentUtc <= DateTime.UtcNow)
                    {
                        this.timeoutTasks.Remove(sTask);
                        this.waitingTasks.RemoveO1(sTask);
                        this.syncTaskLookup.Remove(sTask.ReceiptTask);

                        sTask.TrySignalSource(SyncTask.SourceStates.False);
                        sTask.Dispose();
                    }
                    else
                    {
                        int delay = Math.Max(0, (int)Math.Ceiling((sTask.TimeoutMomentUtc - DateTime.UtcNow).TotalMilliseconds));
                        this.timer.Change(delay, Timeout.Infinite);
                        break;
                    }
                    Thread.EndCriticalRegion();
                }
            }
        }

        /// <summary>
        /// Releases the lock, if it was held against the specified receipt, or cancels the lock
        /// request represented by the receipt so that it will not be granted in the future.
        /// </summary>
        /// <param name="receipt">The receipt object against which the lock is held.</param>
        /// <returns><b>True</b> if the lock was released (implying that the receipt was current),
        /// <b>false</b> otherwise (implying that the lock was not held against that receipt).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Whenever <paramref name="receipt"/> is current (meaning that the lock is held against it)
        /// or valid (meaning that the lock is slated to be granted in the future against it),
        /// <see cref="Exit(ref Task{bool})"/> will reliably set it to null. However, the return
        /// value will be <b>true</b> only if the receipt was current.
        /// </para>
        /// <para>
        /// The caution noted in <see cref="AsyncLock"/> for the case where re-use of receipts is
        /// allowed applies particularly to this method.
        /// </para>
        /// </remarks>
        public bool Exit(ref Task<bool> receipt)
        {
            if (receipt == null)
                return false;

            // Try to exit quickly, if nothing is queued up or being queued up
            int oldQueueCount = -1;
            try
            {
                try { } finally { oldQueueCount = Interlocked.CompareExchange(ref this.queueCount, -1, 0); }
                if (oldQueueCount == 0 && this.lockHeldBy == receipt)
                {
                    try { } finally
                    {
                        this.lockHeldBy = null;
                        receipt = null;
                    }
                    return true;
                }
            }
            finally
            {
                if (oldQueueCount == 0)
                    this.queueCount = 0;
            }

            return this.exitSlowly(ref receipt);
        }

        private bool exitSlowly(ref Task<bool> receipt)
        {
            lock (this.waitingTasks)
            {
                if (!Object.ReferenceEquals(this.lockHeldBy, receipt))
                {
                    SyncTask sTask;
                    try { } finally
                    {
                        if (this.syncTaskLookup.TryGetValue(receipt, out sTask))
                        {
                            this.waitingTasks.RemoveO1(sTask);
                            this.syncTaskLookup.Remove(sTask.ReceiptTask);
                            this.timeoutTasks?.Remove(sTask);
                            sTask.Dispose();
                            receipt = null;
                        }
                    }
                    return false;
                }

                try
                {
                    SyncTask sTask = null;
                    while (this.waitingTasks.Count > 0)
                    {
                        using (sTask = this.waitingTasks.Dequeue())
                        {
                            this.syncTaskLookup.Remove(sTask.ReceiptTask);
                            this.timeoutTasks?.Remove(sTask);

                            if (sTask.PrepareDequeue())
                            {
                                this.lockHeldBy = sTask.ReceiptTask;
                                receipt = null;
                                return true;
                            }
                        }
                    }

                    try { } finally
                    {
                        this.lockHeldBy = null;
                        receipt = null;
                    }
                    return true;
                }
                finally
                {
                    this.queueCount = this.waitingTasks.Count;
                }
            }
        }

        internal class SyncTask : IDisposable, IComparable<SyncTask>
        {
            private TaskCompletionSource<bool> Source { get; set; }
            private volatile int sourceState = 0; // 0=not completed, 1=true, 2=false, 3=canceled, 4=disposed
            public Task<bool> ReceiptTask { get { return this.Source.Task; } }
            public CancellationToken Token { get; private set; }
            public bool Unregistered { get { return this.isUnregistered; } }
            public DateTime TimeoutMomentUtc { get { return this.timeoutMomentUtc; } }

            internal LinkedListNode<SyncTask> Node { get; set; }

            private volatile bool isUnregistered = false;
            // private CancellationTokenSource timeoutCancelingSource;
            private CancellationTokenRegistration ctRegistration;

            private DateTime timeoutMomentUtc;

            public int CompareTo(SyncTask other)
            {
                // Order first by
                int ret = other.timeoutMomentUtc.CompareTo(this.timeoutMomentUtc);
                if (ret != 0)
                    return ret;

                // Are they the same ?
                if (Object.ReferenceEquals(this, other))
                    return 0;

                // Then order by
                ret = this.GetHashCode().CompareTo(other.GetHashCode());
                if (ret != 0)
                    return ret;

                // Then order by
                return (this.Source.GetHashCode() ^ this.ReceiptTask.GetHashCode()).CompareTo(other.Source.GetHashCode() ^ other.ReceiptTask.GetHashCode());
            }

            public enum SourceStates : int
            {
                NotCompleted = 0, True = 1, False = 2, Canceled = 3, Disposed = 4
            }

            public bool TrySignalSource(SourceStates state)
            {
                if ((SourceStates)this.sourceState == SourceStates.Disposed)
                    throw new ObjectDisposedException("SyncTask");

                int oldValue = -1;
                try { } finally
                {
                    if ((oldValue = Interlocked.CompareExchange(ref this.sourceState, (int)state, 0)) == 0)
                    {
                        switch (state)
                        {
                            case SourceStates.True:
                                Task.Factory.StartNew(() => { this.Source.TrySetResult(true); });
                                break;
                            case SourceStates.False:
                                Task.Factory.StartNew(() => { this.Source.TrySetResult(false); });
                                break;
                            case SourceStates.Canceled:
                                Task.Factory.StartNew(() => { this.Source.TrySetCanceled(); });
                                if (this.ctRegistration != default(CancellationTokenRegistration))
                                {
                                    this.ctRegistration.Dispose();
                                    this.ctRegistration = default(CancellationTokenRegistration);
                                }
                                break;
                            case SourceStates.Disposed:
                                throw new ArgumentException("SyncTask cannot be disposed this way.");
                            default:
                                break;
                        }
                    }
                }

                return oldValue == 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool PrepareDequeue()
            {
                if ((SourceStates)this.sourceState == SourceStates.Disposed)
                    throw new ObjectDisposedException("SyncTask");
                bool ret = false;
                if (!this.isUnregistered)
                {
                    this.isUnregistered = true;
                    ret = this.TrySignalSource(SourceStates.True);

                    if (this.ctRegistration != default(CancellationTokenRegistration))
                    {
                        this.ctRegistration.Dispose();
                        this.ctRegistration = default(CancellationTokenRegistration);
                    }
                }

                return ret;
            }

            public SyncTask(TaskCompletionSource<bool> source, CancellationToken token, AsyncLock parentLock) :
                this(source, token, DateTime.MaxValue, parentLock)
            { }

            public SyncTask(TaskCompletionSource<bool> source, CancellationToken token, DateTime timeoutMomentUtc, AsyncLock parentLock)
            {
                this.Source = source;
                this.Token = token;
                // this.timeoutCancelingSource = timeoutCancelingSource;
                if (token.CanBeCanceled)
                    this.ctRegistration = token.Register(() =>
                        {
                            if (this.TrySignalSource(SourceStates.Canceled))
                            {
                                lock (parentLock.waitingTasks)
                                {
                                    using (SyncTask sTask = this)
                                    {
                                        parentLock.waitingTasks.RemoveO1(this);
                                        parentLock.syncTaskLookup.Remove(this.ReceiptTask);
                                        parentLock.timeoutTasks.Remove(this);
                                        this.Dispose();
                                    }
                                }
                            }
                        }
                    );
                else
                    this.ctRegistration = default(CancellationTokenRegistration);
                this.timeoutMomentUtc = timeoutMomentUtc;
            }

            public void Dispose()
            {
                SourceStates oldValue = (SourceStates)Interlocked.Exchange(ref this.sourceState, (int)SourceStates.Disposed);
                if (oldValue != SourceStates.Disposed)
                {
                    if (this.ctRegistration != default(CancellationTokenRegistration))
                    {
                        this.ctRegistration.Dispose();
                        this.ctRegistration = default(CancellationTokenRegistration);
                    }
                    
                    if (oldValue == SourceStates.Disposed) // TODO: Check if this is necessary!
                        Task.Run(() => this.Source.TrySetException(new ObjectDisposedException("Lock awaiter was unexpectedly disposed.")));
                }
            }
        }
    }
#endif
}
