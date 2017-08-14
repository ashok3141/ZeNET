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
using ZeNET.Synchronization.Safe;
using ZeNET.Core;

namespace ZeNET.Tests.Synchronization.Safe
{
    [TestClass]
    public class SpinlockReaderWriterTest
    {
        [TestMethod]
        public void SpinlockReaderWriter_BasicTest()
        {
            SpinlockReaderWriter srw = new SpinlockReaderWriter();

            Assert.AreEqual(true, srw.IsReadable, "SpinlockReaderWriter not initially readable.");
            Assert.AreEqual(true, srw.IsWritable, "SpinlockReaderWriter not initially writable.");

            bool lockTaken = false;
            srw.TryEnterReadLock(ref lockTaken);
            Assert.AreEqual(true, lockTaken, "Read lock acquisition failed in the initial state.");
            Assert.AreEqual(true, srw.IsReadable, "SpinlockReaderWriter not readable after a reader lock was acquired.");
            Assert.AreEqual(false, srw.IsWritable, "SpinlockReaderWriter writable after a reader lock was acquired.");
            srw.ExitReadLock();

            Assert.AreEqual(true, srw.IsReadable, "SpinlockReaderWriter not readable after reader lock was released.");
            Assert.AreEqual(true, srw.IsReadable, "SpinlockReaderWriter not writable after reader lock was released.");


            lockTaken = false;
            srw.TryEnterWriteLock(ref lockTaken);
            Assert.AreEqual(true, lockTaken, "Write lock acquisition failed when no other lock was held.");
            Assert.AreEqual(false, srw.IsReadable, "SpinlockReaderWriter readable after a writer lock was acquired.");
            Assert.AreEqual(false, srw.IsWritable, "SpinlockReaderWriter writable after a writer lock was acquired.");
            srw.ExitWriteLock();

            Assert.AreEqual(true, srw.IsReadable, "SpinlockReaderWriter not readable after writer lock was released.");
            Assert.AreEqual(true, srw.IsReadable, "SpinlockReaderWriter not writable after writer lock was released.");
        }

        [TestMethod]
        public void SpinlockReaderWriter_TestIncompatibleGrants()
        {
            Action<string> err = s =>
            {
                Assert.Fail(s);
            };
            Ref<SpinlockReaderWriter> srw = new Ref<SpinlockReaderWriter>(new SpinlockReaderWriter());

            LockAnalysis.IncompatibleGrantTest(srw, err, 2000, false);
            LockAnalysis.IncompatibleGrantTest(srw, err, 7000, true);

            Assert.AreEqual(true, srw.Value.IsReadable, "SpinlockReaderWriter not readable at the end of the test.");
            Assert.AreEqual(true, srw.Value.IsWritable, "SpinlockReaderWriter not initially at the end of the test.");
        }
    }
}
