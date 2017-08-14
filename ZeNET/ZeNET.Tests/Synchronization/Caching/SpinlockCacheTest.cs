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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeNET.Synchronization.Caching.Safe;
using ZeNET.Core;
using System.Threading;

namespace ZeNET.Tests.Synchronization.Caching
{
    [TestClass]
    public class SpinlockCacheTest
    {
        [TestMethod]
        public void SpinlockCache_BasicSingleThreaded()
        {
            // SpinlockCache cache;
            const int minLifeMilliseconds = 300;

            int timesCalled = 0;

            SpinlockCache<string, string> cache = new SpinlockCache<string, string>(
                delegate (string s)
                {
                    timesCalled++;
                    return s + "Computed";
                }, minLifeMilliseconds / 1000.0);

            Assert.AreEqual<int>(0, cache.Count, "Initial Count wrong.");

            string[] inputs = new string[100];
            int[] order = new int[inputs.Length];
            Random r = new Random();


            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
                inputs[i] = new String(Enumerable.Range(0, 15).Select(n => (char)r.Next(32, (int)'z' + 1)).ToArray()); // random string
            }

            for (int i = 0; i < order.Length; i++)
            {
                int j = r.Next(i, order.Length);
                int temp = order[i];
                order[i] = order[j];
                order[j] = temp;
            }

            for (int i = 0; i < order.Length; i++)
            {
                string inp = inputs[order[i]];
                string res = cache.GetObject(inp);
                Assert.AreEqual<int>(i + 1, cache.Count, String.Format("Cache count incorrect after {0} additions", i + 1));
                Assert.AreEqual<string>(inp + "Computed", res, "Incorrect computed value for " + inp);
                Assert.AreEqual<int>(i + 1, timesCalled, String.Format("Count of number of invocations wrong after {0} attempts.", timesCalled));
            }

            for (int reps = 0; reps < 5; reps++)
            {
                for (int i = 0; i < order.Length; i++)
                {
                    int j = r.Next(i, order.Length);
                    int temp = order[i];
                    order[i] = order[j];
                    order[j] = temp;
                }

                for (int i = 0; i < order.Length; i++)
                {
                    string res = cache.GetObject(inputs[i]);
                    Assert.AreEqual<int>(order.Length, cache.Count);
                    Assert.AreEqual<string>(inputs[i] + "Computed", res, "Incorrect computed value for " + inputs[i]);
                    Assert.AreEqual<int>(order.Length, timesCalled);
                }
            }

            Thread.Sleep(minLifeMilliseconds + 5);
            DateTime deletionRuntime = DateTime.UtcNow;
            cache.DeleteOld();

#if DEBUG
            Assert.AreEqual<int>(0, cache.Count,
                String.Format("Cache not cleared despite all objects having reached the end of their life: {0} remain. Deletion runtime = {1}, First LastAccessTimeUtc = {2}, DeletionThreshold = {3};" + 
                    @"DeleteLockWritable = {4}, CacheLockWritable = {5}",
                    cache.Count,
                    deletionRuntime.ToString("HH:mm:ss.ffff"),
                    cache.private_cacheIndex.Count == 0 ? "" : cache.private_cacheIndex.First.Value.LastAccessTimeUtc.ToString("HH:mm:ss.ffff"),
                    new DateTime(cache.private_deletionThreshold).ToString("HH:mm:ss.ffff"),
                    cache.private_IsLockDeleteWritable,
                    cache.private_IsLockCacheWritable
                )
            );
#else
            Assert.AreEqual<int>(0, cache.Count,
                String.Format("Cache not cleared despite all objects having reached the end of their life: {0} remain.", cache.Count)
            );
#endif
        }

        [TestMethod]
        public void SpinlockCache_MultiThreaded()
        {
            const int maxLifeMilliseconds = 0;

            SpinlockCache<string, string> cache = new SpinlockCache<string, string>(
                delegate (string s)
                {
                    return s + "Computed";
                }, maxLifeMilliseconds / 1000.0);

            Assert.AreEqual<int>(0, cache.Count, "Initial Count wrong.");


            string[] inputs = new string[100];
            Random rStrings = new Random();

            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = new String(Enumerable.Range(0, 15).Select(n => (char)rStrings.Next(32, (int)'z' + 1)).ToArray()); // random string



            Thread[] accessorThreads = new Thread[5];
            Thread[] deleterThreads = new Thread[3];
            Ref<int> stopRunning = new Ref<int>(0);

            string failureMessage = null;
            ManualResetEventSlim mainThreadSignal = new ManualResetEventSlim(false);

            for (int threadNo = 0; threadNo < accessorThreads.Length; threadNo++)
                accessorThreads[threadNo] = new Thread(() =>
                {
                    int[] order = new int[inputs.Length];
                    Random rOrder = new Random();
                    int iterationNo = 0;

                    for (int i = 0; i < order.Length; i++)
                        order[i] = i;

                    while (Thread.VolatileRead(ref stopRunning.Value) == 0)
                    {
                        iterationNo++;
                        for (int i = 0; i < order.Length - 1; i++)
                        {
                            int j = rStrings.Next(i, order.Length);
                            int temp = order[i];
                            order[i] = order[j];
                            order[j] = temp;
                        }

                        for (int i = 0; i < order.Length; i++)
                        {
                            string inp = inputs[order[i]];
                            string res = "";
                            try
                            {
                                res = cache.GetObject(inp);
                            }
                            catch (Exception e)
                            {
                                if (Interlocked.Increment(ref stopRunning.Value) == 1)
                                    failureMessage = String.Format("Exception caught during access on one of the threads on iteration number {0}: {1}", iterationNo, e.Message);
                                mainThreadSignal.Set();
                            }

                            Assert.AreEqual<string>(inp + "Computed", res, "Incorrect computed value for " + inp);
                        }
                    }
                });

            for (int threadNo = 0; threadNo < deleterThreads.Length; threadNo++)
            {
                deleterThreads[threadNo] = new Thread(() =>
                {
                    int iterationNo = 0;
                    while (Thread.VolatileRead(ref stopRunning.Value) == 0)
                    {
                        iterationNo++;
                        try
                        {
                            cache.DeleteOld();
                        }
                        catch (Exception e)
                        {
                            if (Interlocked.Increment(ref stopRunning.Value) == 1)
                                failureMessage = String.Format("Exception caught during deletion on one of the threads on iteration number {0}: {1}", iterationNo, e.Message);
                            mainThreadSignal.Set();
                        }
                    }
                });
            }

            Array.ForEach(accessorThreads, t => t.Start());
            Array.ForEach(deleterThreads, t => t.Start());

            new Thread(() => { Thread.Sleep(300); mainThreadSignal.Set(); }).Start();
            mainThreadSignal.Wait();

            Thread.VolatileWrite(ref stopRunning.Value, 1);

            if (failureMessage != null)
                Assert.Fail(failureMessage);

            Array.ForEach(accessorThreads, t => t.Join());
            Array.ForEach(deleterThreads, t => t.Join());

            int cacheCount = cache.Count;
            try
            {
                cache.DeleteOld();
            }
            catch (Exception e)
            {
                Assert.Fail(String.Format("Exception caught during the final delete. Count was {0} before the attempt. Exception message is {1}.", cacheCount, e.Message));
            }
            int finalCacheSize = cache.Count;
            Assert.AreEqual<int>(0, finalCacheSize, String.Format("At the end, the cache count is {0} rather than 0.", finalCacheSize));

#if DEBUG
            Assert.AreEqual<bool>(true, cache.private_IsLockCacheWritable, "At the end, the cache lock is not writable.");
            Assert.AreEqual<bool>(true, cache.private_IsLockDeleteWritable, "At the end, the deleter lock is not writable.");
#endif
        }


        [TestMethod]
        public void SpinlockCache_MultiThreadedSpecificRemovals()
        {
            const int maxLifeMilliseconds = 0;

            SpinlockCache<string, string> cache = new SpinlockCache<string, string>(
                delegate (string s)
                {
                    return s + "Computed";
                }, maxLifeMilliseconds / 1000.0);

            Assert.AreEqual<int>(0, cache.Count, "Initial Count wrong.");


            string[] inputs = new string[10000];
            Random rStrings = new Random();

            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = new String(Enumerable.Range(0, 15).Select(n => (char)rStrings.Next(32, (int)'z' + 1)).ToArray()); // random string


            Thread[] accessorThreads = new Thread[5];
            Thread[] deleterThreads = new Thread[2];
            Thread[] removerThreads = new Thread[3];
            Thread[] trimmerThreads = new Thread[4];

            Ref<int> stopRunning = new Ref<int>(0);

            string failureMessage = null;
            ManualResetEventSlim mainThreadSignal = new ManualResetEventSlim(false);

            for (int threadNo = 0; threadNo < accessorThreads.Length; threadNo++)
                accessorThreads[threadNo] = new Thread(() =>
                {
                    int[] order = new int[inputs.Length];
                    Random rOrder = new Random();
                    int iterationNo = 0;

                    for (int i = 0; i < order.Length; i++)
                        order[i] = i;

                    while (Thread.VolatileRead(ref stopRunning.Value) == 0)
                    {
                        iterationNo++;
                        for (int i = 0; i < order.Length - 1; i++)
                        {
                            int j = rStrings.Next(i, order.Length);
                            int temp = order[i];
                            order[i] = order[j];
                            order[j] = temp;
                        }

                        for (int i = 0; i < order.Length; i++)
                        {
                            string inp = inputs[order[i]];
                            string res = "";
                            try
                            {
                                res = cache.GetObject(inp);
                            }
                            catch (Exception e)
                            {
                                if (Interlocked.Increment(ref stopRunning.Value) == 1)
                                    failureMessage = String.Format("Exception caught during access on one of the threads on iteration number {0}: {1}", iterationNo, e.Message);
                                mainThreadSignal.Set();
                            }

                            Assert.AreEqual<string>(inp + "Computed", res, "Incorrect computed value for " + inp);
                        }
                    }
                });

            for (int threadNo = 0; threadNo < removerThreads.Length; threadNo++)
                removerThreads[threadNo] = new Thread(() =>
                {
                    int[] order = new int[inputs.Length];
                    Random rOrder = new Random();
                    int iterationNo = 0;

                    for (int i = 0; i < order.Length; i++)
                        order[i] = i;

                    while (Thread.VolatileRead(ref stopRunning.Value) == 0)
                    {
                        iterationNo++;
                        for (int i = 0; i < order.Length - 1; i++)
                        {
                            int j = rStrings.Next(i, order.Length);
                            int temp = order[i];
                            order[i] = order[j];
                            order[j] = temp;
                        }

                        for (int i = 0; i < order.Length; i++)
                        {
                            string inp = inputs[order[i]];
                            try
                            {
                                cache.Remove(inp);
                            }
                            catch (Exception e)
                            {
                                if (Interlocked.Increment(ref stopRunning.Value) == 1)
                                    failureMessage = String.Format("Exception caught during removal on one of the threads on iteration number {0}: {1}", iterationNo, e.Message);
                                mainThreadSignal.Set();
                            }
                        }
                    }
                });

            for (int threadNo = 0; threadNo < deleterThreads.Length; threadNo++)
            {
                deleterThreads[threadNo] = new Thread(() =>
                {
                    int iterationNo = 0;
                    while (Thread.VolatileRead(ref stopRunning.Value) == 0)
                    {
                        iterationNo++;
                        try
                        {
                            cache.DeleteOld();
                        }
                        catch (Exception e)
                        {
                            if (Interlocked.Increment(ref stopRunning.Value) == 1)
                                failureMessage = String.Format("Exception caught during deletion on one of the threads on iteration number {0}: {1}", iterationNo, e.Message);
                            mainThreadSignal.Set();
                        }
                    }
                });
            }

            for (int threadNo = 0; threadNo < trimmerThreads.Length; threadNo++)
            {
                trimmerThreads[threadNo] = new Thread(() =>
                {
                    int iterationNo = 0;
                    while (Thread.VolatileRead(ref stopRunning.Value) == 0)
                    {
                        iterationNo++;
                        try
                        {
                            cache.TrimTo(inputs.Length * 2 / 3);
                        }
                        catch (Exception e)
                        {
                            if (Interlocked.Increment(ref stopRunning.Value) == 1)
                                failureMessage = String.Format("Exception caught during trimming on one of the threads on iteration number {0}: {1}", iterationNo, e.Message);
                            mainThreadSignal.Set();
                        }
                    }
                });
            }

            Array.ForEach(accessorThreads, t => t.Start());
            Array.ForEach(deleterThreads, t => t.Start());
            Array.ForEach(removerThreads, t => t.Start());
            Array.ForEach(trimmerThreads, t => t.Start());

            new Thread(() => { Thread.Sleep(300); mainThreadSignal.Set(); }).Start();
            mainThreadSignal.Wait();

            Thread.VolatileWrite(ref stopRunning.Value, 1);

            if (failureMessage != null)
                Assert.Fail(failureMessage);

            Array.ForEach(accessorThreads, t => t.Join());
            Array.ForEach(deleterThreads, t => t.Join());
            Array.ForEach(trimmerThreads, t => t.Join());
            Array.ForEach(removerThreads, t => t.Join());

            int cacheCount = cache.Count;
            try
            {
                cache.DeleteOld();
            }
            catch (Exception e)
            {
                Assert.Fail(String.Format("Exception caught during the final delete. Count was {0} before the attempt. Exception message is {1}.", cacheCount, e.Message));
            }
            int finalCacheSize = cache.Count;
            Assert.AreEqual<int>(0, finalCacheSize, String.Format("At the end, the cache count is {0} rather than 0.", finalCacheSize));

#if DEBUG
            Assert.AreEqual<bool>(true, cache.private_IsLockCacheWritable, "At the end, the cache lock is not writable.");
            Assert.AreEqual<bool>(true, cache.private_IsLockDeleteWritable, "At the end, the deleter lock is not writable.");
#endif
        }
    }
}
