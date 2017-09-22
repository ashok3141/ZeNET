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

#if Framework_4
using MRE = System.Threading.ManualResetEventSlim;
#else
using MRE = System.Threading.ManualResetEvent;
#endif

namespace ZeNET.Synchronization
{
    /// <summary>
    /// Represents a boolean flag that is initially false, becomes true when <see cref="Set"/> is
    /// called, and remains true thereafter, all the while allowing threads to wait for the flag to
    /// be set by calling <see cref="Wait"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct works adaptively to try to deliver the best possible performance and to use the
    /// least amount of memory in all cases. If there are no calls to <see cref="Wait"/> before the
    /// flag is set, then it never instantiates a wait handle, instead using a
    /// <see cref="System.Boolean"/> value to store its state.
    /// </para>
    /// <para>
    /// When there is a call to <see cref="Wait"/> before the flag has been set, a
    /// <see cref="System.Threading.ManualResetEventSlim"/> or
    /// <see cref="System.Threading.ManualResetEvent"/> is instantiated (depending on the target
    /// .NET Framework version). The call then uses that wait handle to become blocked, as do any
    /// subsequent calls to <see cref="Wait"/>. The first call to <see cref="Set"/> sets the wait
    /// handle and clears the reference to it, so that it may be picked up by the garbage collector.
    /// </para>
    /// </remarks>
    /// <threadsafety static="true" instance="true"/>
    /// <example>
    /// <code language="C#" title="Basic example" source="..\ZeNET\Examples\Synchronization\BooleanFlagNoResetExamples.cs" region="BooleanFlagNoResetExample1" />
    /// </example>
    public struct BooleanFlagNoReset
    {
        /// <summary>
        /// Indicates whether the flag has been set.
        /// </summary>
        /// <value><em>True</em> if it has been set, <em>false</em> otherwise.</value>
        public bool IsSet { get { return this.isSet; } }
        private volatile bool isSet;
        private volatile MRE waitHandle;

        /// <summary>
        /// Blocks the current thread until the flag has been set.
        /// </summary>
        public void Wait()
        {
            if (!this.isSet)
            {
                try
                {
                    if (this.waitHandle == default(MRE))
                        Interlocked.CompareExchange<MRE>(ref this.waitHandle, new MRE(false), default(MRE));

                    if (!this.isSet)
                    {
#if Framework_4
                        this.waitHandle?.Wait();
#else
                        this.waitHandle?.WaitOne();
#endif
                    }
                }
                finally
                {
                    if (this.isSet)
                        Interlocked.Exchange(ref this.waitHandle, default(MRE))?.Set();
                }
            }
        }

        /// <summary>
        /// Sets the boolean flag.
        /// </summary>
        /// <remarks>This method may be called multiple times, and concurrently.</remarks>
        public void Set()
        {
            if (!this.isSet)
            {
                try { }
                finally
                {
                    this.isSet = true;
                    Interlocked.Exchange(ref this.waitHandle, default(MRE))?.Set();
                }
            }
        }
    }
}