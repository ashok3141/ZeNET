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
using ZeNET.Synchronization;

namespace ZeNET.Examples.Synchronization
{
    internal static class BooleanFlagNoResetExamples
    {
        public static void DocumentationExamples1()
        {
            #region BooleanFlagNoResetExample1
            BooleanFlagNoReset flag = new BooleanFlagNoReset();
            Thread t1, t2;
            (t1 = new Thread(() =>
            {
                flag.Wait();
                Console.WriteLine("I am thread {0}.", Thread.CurrentThread.ManagedThreadId);
            }
            )).Start();

            (t2 = new Thread(() =>
            {
                flag.Wait();
                Console.WriteLine("I am thread {0}.", Thread.CurrentThread.ManagedThreadId);
            }
            )).Start();

            Console.WriteLine("The two threads will wait three seconds.");
            Thread.Sleep(3000);
            flag.Set();
            t1.Join();
            t2.Join();
            Console.WriteLine("Threads ended.");
            #endregion
        }
    }
}
