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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ZeNET.Synchronization.Safe;
using ZeNET.Core;

namespace ZeNET.Tests.Synchronization.Safe
{
    using LED = ZeNET.Tests.Synchronization.Safe.LockAnalysis.LockEventGrantTest;
    using Cons = ZeNET.Tests.Synchronization.Safe.LockAnalysis.LockEventGrantTest.Constants;
    using System.Collections.Generic;

    public static class LockAnalysis
    {
        public static void IncompatibleGrantTest(Ref<SpinlockReaderWriter> rwLock, Action<string> recordErrorMessage, int testDurationMilliseconds, bool tryAborting = false)
        {
            // ReaderWriterLockSlim lck = new ReaderWriterLockSlim();
            // Ref<SpinlockReaderWriter> rwLock = new Ref<SpinlockReaderWriter>(new SpinlockReaderWriter());

            Stopwatch sw = Stopwatch.StartNew();
            bool stopRunning = false;

            ConcurrentBag<LED> bag = new ConcurrentBag<LED>();

            ThreadStart readerCode = () =>
            {
                long t1, t2;
                Random r = new Random();
                while (!stopRunning)
                {

                    bool lockTaken = false;
                    try
                    {
                        t1 = 0;
                        try
                        {
                            rwLock.Value.EnterReadLock(ref lockTaken);
                            Interlocked.Exchange(ref t1, sw.ElapsedTicks);
                        }
                        finally
                        {
                            if (lockTaken)
                            {
                                if (t1 != 0)
                                    bag.Add(new LED(t1, Cons.Entered, Cons.ReadLock));
                                else
                                {
                                    rwLock.Value.ExitReadLock();
                                    lockTaken = false;
                                }
                            }
                        }

                        if (lockTaken)
                            Thread.Sleep(r.Next(0, 3));
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            t2 = sw.ElapsedTicks;
                            rwLock.Value.ExitReadLock();
                            bag.Add(new LED(t2, Cons.Exited, Cons.ReadLock));
                        }
                    }
                }
            };

            ThreadStart writerCode = () =>
            {
                long t1, t2;
                Random r = new Random();
                while (!stopRunning)
                {

                    bool lockTaken = false;
                    try
                    {
                        t1 = 0;
                        try
                        {
                            rwLock.Value.EnterWriteLock(ref lockTaken);
                            Interlocked.Exchange(ref t1, sw.ElapsedTicks);
                        }
                        finally
                        {
                            if (lockTaken)
                            {
                                if (t1 != 0)
                                    bag.Add(new LED(t1, Cons.Entered, Cons.WriteLock));
                                else
                                {
                                    rwLock.Value.ExitWriteLock();
                                    lockTaken = false;
                                }
                            }
                        }

                        if (lockTaken)
                            Thread.Sleep(r.Next(0, 3));
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            t2 = sw.ElapsedTicks;
                            rwLock.Value.ExitWriteLock();
                            bag.Add(new LED(t2, Cons.Exited, Cons.WriteLock));
                        }
                    }
                }
            };

            Thread[] readThreads = new Thread[2];
            Thread[] writeThreads = new Thread[5];

            for (int i = 0; i < readThreads.Length; i++)
                readThreads[i] = new Thread(readerCode);

            for (int i = 0; i < writeThreads.Length; i++)
                writeThreads[i] = new Thread(writerCode);

            Array.ForEach(readThreads, th => { th.Start(); });
            Array.ForEach(writeThreads, th => { th.Start(); });

            int abortedReaderCount = 0, abortedWriterCount = 0;
            ThreadStart bullInAChinaShop = () =>
            {
                int totalThreads = readThreads.Length + writeThreads.Length;
                Random r = new Random();

                for (int reps = 0; reps < 5 || !stopRunning; reps++)
                {
                    Thread.Sleep(10);
                    int threadNo = r.Next(0, totalThreads);

                    if (threadNo < readThreads.Length)
                    {
                        try
                        {
                            readThreads[threadNo].Abort();
                            abortedReaderCount++;
                        }
                        catch { }
                        readThreads[threadNo] = new Thread(readerCode);
                        readThreads[threadNo].Start();
                    }
                    else
                    {
                        threadNo -= readThreads.Length;
                        try
                        {
                            writeThreads[threadNo].Abort();
                            abortedWriterCount++;
                        }
                        catch { }
                        writeThreads[threadNo] = new Thread(writerCode);
                        writeThreads[threadNo].Start();
                    }
                }

                // Console.WriteLine("I aborted {0} readers and {1} writers", abortedReaderCount, abortedWriterCount);
            };

            Thread destroyer = new Thread(bullInAChinaShop);
            if (tryAborting)
                destroyer.Start();

            Thread.Sleep(testDurationMilliseconds);
            stopRunning = true;
            Array.ForEach(readThreads, th => { th.Join(); });
            Array.ForEach(writeThreads, th => { th.Join(); });
            if (tryAborting)
                destroyer.Join();

            AnalyzeLockEventsForIllegalGrants(bag, recordErrorMessage);
        }

        public static void AnalyzeLockEventsForIllegalGrants(IEnumerable<LED> events, Action<string> recordErrorMessage)
        {
            LED[] arr = events.OrderBy(e => e.Ticks).ThenBy(e => e.IsEnter ? 1 : 0).ToArray();

            bool writeLockHeld = false;
            int numberOfReaders = 0;

            int readEvents = 0, writeEvents = 0;

            Array.ForEach(arr, led =>
            {
                if (led.IsShared)
                    readEvents++;
                else
                    writeEvents++;

                if (led.IsEnter)
                {
                    if (writeLockHeld || (numberOfReaders > 0 && !led.IsShared))
                        recordErrorMessage(String.Format("Incompatible lock obtained. writeLockHeld = {0}, numberOfReaders = {1}, led = ({2})", writeLockHeld, numberOfReaders, led));
                    if (led.IsShared)
                        numberOfReaders++;
                    else
                        writeLockHeld = true;
                }
                else
                {
                    if (led.IsShared)
                    {
                        if (numberOfReaders == 0)
                            recordErrorMessage(String.Format("Illegal lock release. writeLockHeld = {0}, numberOfReaders = {1}, led = ({2})", writeLockHeld, numberOfReaders, led));
                        numberOfReaders--;
                    }
                    else
                    {
                        if (!writeLockHeld)
                            recordErrorMessage(String.Format("Illegal lock release. writeLockHeld = {0}, numberOfReaders = {1}, led = ({2})", writeLockHeld, numberOfReaders, led));
                        writeLockHeld = false;
                    }
                }
            }
            );

            // Console.WriteLine("Analyzed {0} events, with {1} read events and {2} write events", arr.Length, readEvents, writeEvents);
            // Console.WriteLine("All errors printed. Now printing final lock state. writeLockHeld = {0}, numberOfReaders = {1}", writeLockHeld, numberOfReaders);
        }

        public struct LockEventGrantTest
        {
            public long Ticks { get; private set; }
            public bool IsEnter { get; private set; }
            public bool IsShared { get; private set; }

            public LockEventGrantTest(long ticks, bool isEnter, bool isShared)
                : this()
            {
                this.Ticks = ticks;
                this.IsEnter = isEnter;
                this.IsShared = isShared;
            }

            public LockEventGrantTest(long ticks, Constants isEnter, Constants isShared)
                : this()
            {
                this.Ticks = ticks;
                if (isEnter == Constants.Entered)
                    this.IsEnter = true;
                else if (isEnter == Constants.Exited)
                    this.IsEnter = false;
                else
                    throw new Exception(String.Format("Invalid value of isEnter passed: {0}", isEnter));

                if (isShared == Constants.ReadLock)
                    this.IsShared = true;
                else if (isShared == Constants.WriteLock)
                    this.IsShared = false;
                else
                    throw new Exception(String.Format("Invalid value of isShared passed: {0}", isShared));
            }

            public override string ToString()
            {
                return String.Format("At {0}, {1} {2}", this.Ticks, this.IsEnter ? "entered" : "exited", this.IsShared ? "read lock" : "write lock");
            }

            public enum Constants
            {
                Entered = 0, Exited = 1, ReadLock = 2, WriteLock = 3
            }
        }
    }
}
