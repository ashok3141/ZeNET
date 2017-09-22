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



using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeNET.Core;
using ZeNET.Synchronization;
using ZeNET.Synchronization.Safe;
using ZeNET.Tests.Synchronization.Safe;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace ZeNET.Tests.Synchronization
{
    [TestClass]
    public class AsyncLockTest
    {
        [TestMethod]
        public void AsyncLock_BasicTest()
        {
            AsyncLock asyLock = new AsyncLock(true);

            Assert.AreEqual(false, asyLock.IsHeld, "AsyncLock initially held.");

            Task<bool> grantReceipt = null;

            for (int i = 0; i < 10; i++)
            {
                asyLock = new AsyncLock(i % 2 == 0);
                string stdSuffix = String.Format(" The iteration was {0}, reusability state was {1}", i, i % 2 == 0);

                asyLock.TryEnterAsync(ref grantReceipt);
                try
                {
                    Assert.AreEqual(grantReceipt.Result, true, "Lock acquisition failed in the initial state." + stdSuffix);
                    Assert.AreEqual(asyLock.IsHeld, true, "AsyncLock IsHeld testing false even after it was acquired." + stdSuffix);
                    Assert.AreEqual(asyLock.IsHeldBy(grantReceipt), true, "AsyncLock IsHeldBy testing false." + stdSuffix);                    
                }
                catch (Exception e)
                {
                    Assert.Fail(String.Format("Failure during basic tests (1), with message: {0}." + stdSuffix, e.Message));
                }

                Task<bool> duplicateReceipt = grantReceipt;
                try
                {
                    Assert.AreEqual<bool>(asyLock.Exit(ref grantReceipt), true, "Lock was not successfully released." + stdSuffix);
                    Assert.AreEqual(asyLock.IsHeld, false, "AsyncLock IsHeld testing true even after it was released." + stdSuffix);
                    Assert.AreEqual(asyLock.IsHeldBy(duplicateReceipt), false, "AsyncLock IsHeldBy testing true even after it was released." + stdSuffix);
                }
                catch (Exception e)
                {
                    Assert.Fail(String.Format("Failure during basic tests (2), with message: {0}. " + stdSuffix, e.Message));
                }
            }
        }

        // [TestMethod]
        public void AsyncLock_IncompatibleGrants_Reuse_Abort()
        {
            AsyncLock tskLock = new AsyncLock(true);
            LockAnalysis.IncompatibleGrantTestAsyncLock(tskLock, Assert.Fail, 1000, true);
            Assert.AreEqual(false, tskLock.IsHeld, "AsyncLock held even at the end of the test (reusable configuration).");
        }

        [TestMethod]
        public void AsyncLock_IncompatibleGrants_Reuse_NoAbort()
        {
            AsyncLock tskLock = new AsyncLock(true);
            LockAnalysis.IncompatibleGrantTestAsyncLock(tskLock, Assert.Fail, 1000, false);
            Assert.AreEqual(false, tskLock.IsHeld, "AsyncLock held even at the end of the test (reusable configuration).");
        }

        // [TestMethod]
        public void AsyncLock_IncompatibleGrants_NoReuse_Abort()
        {
            AsyncLock tskLock = new AsyncLock(true);
            LockAnalysis.IncompatibleGrantTestAsyncLock(tskLock, Assert.Fail, 1000, true);
            Assert.AreEqual(false, tskLock.IsHeld, "AsyncLock held even at the end of the test (reusable configuration).");
        }

        [TestMethod]
        public void AsyncLock_IncompatibleGrants_NoReuse_NoAbort()
        {
            AsyncLock tskLock = new AsyncLock(true);
            LockAnalysis.IncompatibleGrantTestAsyncLock(tskLock, Assert.Fail, 1000, false);
            Assert.AreEqual(false, tskLock.IsHeld, "AsyncLock held even at the end of the test (reusable configuration).");
        }

        [TestMethod]
        public void AsyncLock_IndirectTests()
        {
            AsyncLock tskLock = new AsyncLock(false);
            Ref<SpinlockReaderWriter> srw = new Ref<SpinlockReaderWriter>(new SpinlockReaderWriter());
            Action<string> reportError = s => { Assert.Fail(s); throw new Exception(s); };

            const int reps = 10000;
            const int threadCount = 1;
            SemaphoreSlim sem = new SemaphoreSlim(threadCount * 100, threadCount * 100);
            Queue<int> qu = new Queue<int>();

            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(obj =>
                {
                    List<Task> list = new List<Task>();
                    // Task latestTask = null;
                    int threadIdentity = (int)obj;

                    for (int r = 0; r < reps; r++)
                    {
                        //if (sem.CurrentCount == 0)
                        //{
                        //    Task.WaitAll(list.ToArray());
                        //    list.Clear();
                        //}
                        //sem.Wait();
                        Task temp = AddToQueue(tskLock, r * threadCount + threadIdentity, qu, reportError);
                        temp.Wait();
                        //list.Add(temp);
                        //sem.Release();
                    }

                    //Task.WaitAll(list.ToArray());
                    //list.Clear();
                });                
            }

            for (int i = 0; i < threadCount; i++)
                threads[i].Start(i);

            Array.ForEach(threads, t => t.Join());
            // Thread.Sleep(1000);

            Assert.AreEqual<int>(reps * threadCount, qu.Count, "Final queue count is wrong.");
        }

        private async Task AddToQueue(AsyncLock lck, int i, Queue<int> queue, Action<string> reportError)
        {
            Task<bool> grantReceipt = null;
            //bool lockTaken = false;

            //lock (queue)
            //    queue.Enqueue(i);

            try
            {
                await lck.EnterAsync(ref grantReceipt);
                // srw.Value.EnterWriteLock(ref lockTaken);
                // lock (queue)
                queue.Enqueue(i);
            } catch (Exception ex)
            {
                reportError(ex.Message);
            }
            finally
            {
                lck.Exit(ref grantReceipt);
                //if (lockTaken)
                //    srw.Value.ExitWriteLock();
            }
        }

        public void AsyncLock_DummyTest()
        {
            return;
            innerFailure(Assert.Fail);
        }

        private int innerFailure(Action<string> errorOut)
        {
            ExecutionContext ec = ExecutionContext.Capture(); // .CreateCopy();
            ExecutionContext.Run(ec, (state) => errorOut("I am failing from before the thread."), null);

            Thread t = new Thread((object execContext) =>
                {
                    // ExecutionContext ecInner = (ExecutionContext)execContext;
                    Thread.Sleep(10);
                    ExecutionContext.Run(ec, (state) => errorOut("I am failing from within the thread"), null);
                    errorOut("I failed from a different thread.");
                }
            );
            t.Start();
            t.Join();
            //errorOut("I failed from the inner method.");
            return 10;
        }
    }
}
