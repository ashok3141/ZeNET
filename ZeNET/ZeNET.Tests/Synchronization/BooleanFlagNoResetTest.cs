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
using ZeNET.Synchronization;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ZeNET.Tests.Synchronization
{
    [TestClass]
    public class BooleanFlagNoResetTest
    {
        const int completionWaitTimeMs = 5000;
        const int prescribedRepetitions = 100;

        [TestMethod]
        public void BooleanFlagNoReset_Basic()
        {
            BooleanFlagNoReset bfnr = new BooleanFlagNoReset();
            Assert.IsFalse(bfnr.IsSet, "Initial state should be not set.");
            bfnr.Set();
            Assert.IsTrue(bfnr.IsSet, "IsTrue property tests false after the flag was set.");

            int finished = 0;
            Thread t = new Thread(() => { bfnr.Wait(); Thread.VolatileWrite(ref finished, 1); });
            t.Start();

            Stopwatch sw = Stopwatch.StartNew();
            while (Thread.VolatileRead(ref finished) == 0 && sw.ElapsedMilliseconds <= completionWaitTimeMs)
                Thread.Sleep(10);

            Assert.AreEqual<int>(1, Thread.VolatileRead(ref finished), "The finished state was not set to 1.");
        }

        [TestMethod]
        public void BooleanFlagNoReset_SingleSet()
        {
            const int waiterCount = 4;
            Random r = new Random();
            Stopwatch sw = new Stopwatch();
            BooleanFlagNoReset bfnr;
            DateTime signalingTime;
            ConcurrentBag<DateTime> completionTimes = new ConcurrentBag<DateTime>();
            Thread[] waiters = new Thread[waiterCount];

            //DateTime[] completionTimes = new DateTime[waiterCount];
            for (int reps = 0; reps < prescribedRepetitions; reps++)
            {
                bfnr = new BooleanFlagNoReset();

                for (int i = 0; i < waiterCount; i++)
                {
                    waiters[i] = new Thread(() =>
                    {
                        bfnr.Wait();
                        completionTimes.Add(DateTime.UtcNow);
                    });
                }
                Array.ForEach(waiters, t => t.Start());

                Thread.Sleep(r.Next(0, 7));
                signalingTime = DateTime.UtcNow;
                bfnr.Set();
                sw.Reset(); sw.Start();
                while (completionTimes.Count < waiterCount && sw.ElapsedMilliseconds <= completionWaitTimeMs)
                    Thread.Sleep(10);
                sw.Stop();
                Assert.AreEqual<int>(waiterCount, completionTimes.Count, "One or more threads did not finish though they should have been signaled.");

                foreach (DateTime val in completionTimes)
                    Assert.IsTrue(val >= signalingTime, "Somehow a thread got signaled before it was really time.");
                while (completionTimes.TryTake(out signalingTime)) ;
                signalingTime = DateTime.MinValue;

            }
        }

        [TestMethod]
        public void BooleanFlagNoReset_MultiSet()
        {
            const int waiterCount = 0;
            const int setterCount = 3;
            const int jitWaiterCount = 3; // just-in-time waiters
            Random r = new Random();            
            BooleanFlagNoReset bfnr = new BooleanFlagNoReset();
            
            List<long> signaledTimes = new List<long>();
            List<long> settingTimes = new List<long>();

            Thread[] jitWaiters = new Thread[jitWaiterCount];
            Thread[] waiters = new Thread[waiterCount];
            Thread[] setters = new Thread[setterCount];

            
            for (int reps = 0; reps < prescribedRepetitions; reps++)
            {
                int settersMaySet = 0;
                Thread.VolatileWrite(ref settersMaySet, 0);
                
                bfnr = new BooleanFlagNoReset();
                for (int i = 0; i < setters.Length; i++)
                {
                    setters[i] = new Thread(() =>
                    {
                        while (Thread.VolatileRead(ref settersMaySet) == 0) ;
                        long ticks = Stopwatch.GetTimestamp();
                        bfnr.Set();                        
                        lock (settingTimes)
                            settingTimes.Add(ticks);
                    }
                    );
                }

                for (int i = 0; i < waiters.Length; i++)
                {
                    waiters[i] = new Thread(() =>
                    {
                        bfnr.Wait();
                        long ticks = Stopwatch.GetTimestamp();
                        lock (signaledTimes)
                            signaledTimes.Add(ticks);
                    }
                    );
                }

                for (int i = 0; i < jitWaiters.Length; i++)
                {
                    jitWaiters[i] = new Thread(() =>
                    {
                        while (Thread.VolatileRead(ref settersMaySet) == 0) ;
                        bfnr.Wait();
                        long ticks = Stopwatch.GetTimestamp();
                        lock (signaledTimes)
                            signaledTimes.Add(ticks);
                    }
                    );
                }
                Array.ForEach(setters, t => t.Start());
                Array.ForEach(waiters, t => t.Start());
                Array.ForEach(jitWaiters, t => t.Start());
                Thread.Sleep(r.Next(0, 7));
                Assert.IsFalse(bfnr.IsSet, "Somehow the flag is set initially.");
                Thread.VolatileWrite(ref settersMaySet, 1);

                Stopwatch sw = new Stopwatch();
                sw.Reset(); sw.Start();
                int completionTimesCount = 0, settingTimesCount = 0;
                while (completionTimesCount < jitWaiterCount + waiterCount && settingTimesCount < setterCount && sw.ElapsedMilliseconds < completionWaitTimeMs)
                {
                    Thread.Sleep(5);
                    lock (signaledTimes) completionTimesCount = signaledTimes.Count;
                    lock (settingTimes) settingTimesCount = settingTimes.Count;
                }
                sw.Stop();

                Assert.AreEqual<int>(jitWaiterCount + waiterCount, completionTimesCount,
                    String.Format(
                        "One or more waiters failed to finish on repetition {0}. The setter count is {1}. The thread states of waiters are {2}. JitWaiters: {3}",
                        reps,
                        settingTimes.Count,
                        String.Join(", ", waiters.Select(w => w.ThreadState.ToString())),
                        String.Join(", ", jitWaiters.Select(w => w.ThreadState.ToString()))
                        ));
                Assert.AreEqual<int>(setterCount, settingTimesCount, String.Format("One or more setters failed to finish on rep {0}. The waiter count was {1}", reps, signaledTimes.Count));
                Assert.IsTrue(bfnr.IsSet, "Somehow the flag is not set.");

                long settingTime = settingTimes.Min() ;
                settingTimes.Clear();
                long signaledTime = signaledTimes.Min();
                signaledTimes.Clear();

                if (signaledTime < settingTime)
                    Assert.Fail(String.Format("One or more waiters got signaled before they should have on rep {0}.", reps));                
            }
        }
    }
}
