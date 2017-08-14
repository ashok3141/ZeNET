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

namespace ZeNET.Core
{
    /// <summary>
    /// A generic class enabling the capture of a value type or another reference type.
    /// </summary>
    /// <typeparam name="T">The type of the captured object or struct.</typeparam>
    public class Ref<T>
    {
        /// <summary>
        /// The captured variable, exposed as a public field.
        /// </summary>
        public T Value;

        /// <summary>
        /// Initializes the captured variable with the specified value.
        /// </summary>
        /// <param name="value">The initial value for the captured variable.</param>
        public Ref(T value)
        {
            this.Value = value;
        }


    }
}
