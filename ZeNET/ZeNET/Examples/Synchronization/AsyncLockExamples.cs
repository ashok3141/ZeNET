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

using ZeNET.Synchronization;
#if Framework_4
using System.Threading.Tasks;
#endif

namespace ZeNET.Examples.Synchronization
{
    internal static class AsyncLockExamples
    {
#if Framework_4
        public async static void DocumentationExamples1()
        {
#region EnterAsyncExample1
            // WARNING: This snippet contains bad code!
            AsyncLock lck = new AsyncLock(true); // receipts may be reused
            Task<bool> receipt = null, cpy = null;
            try
            {
                await lck.EnterAsync(ref receipt);
                cpy = receipt; // This is dangerous and requires caution in the handling of cpy
                // Do the work of the critical section here
             } finally
             {
                 lck.Exit(ref receipt);
             }
             
             // ... other code
             
             if (lck.IsHeldBy(cpy))
                 Console.WriteLine("IsHeldBy returned true even though the lock is not held.");
             lck.Exit(ref cpy); // WARNING: This might release another requestor's lock
#endregion
        }

        public async static void DocumentationExamples2()
        {
#region EnterAsyncExample2
            AsyncLock lck = new AsyncLock(true); // may substitute false to disallow receipt re-use

            Task<bool> receipt = null;
            try
            {
                await lck.EnterAsync(ref receipt);
                // Critical section -- concurrency limited to one thread here
            }
            finally
            {
                lck.Exit(ref receipt);
            }
#endregion
        }

        public async static void DocumentationExamples3()
        {
#region EnterAsyncExample3
            AsyncLock lck = new AsyncLock(true); // may substitute false
            
            Task<bool> receipt = null;
            try
            {
            	if (await lck.TryEnterAsync(ref receipt))
            	{
            	    // Critical section
            	}
            } finally
            {
            	lck.Exit(ref receipt);
            }
#endregion
        }
#endif
    }
}
