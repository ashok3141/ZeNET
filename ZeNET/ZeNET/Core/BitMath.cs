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

using System.Runtime.CompilerServices;

namespace ZeNET.Core
{
    /// <summary>
    /// Contains miscellaneous methods of a mathematical nature.
    /// </summary>
    public static class BitMath
    {
        /// <summary>
        /// Computes very fast the smallest nonnegative integer not smaller than  <paramref name="x"/>
        /// that is also an integral power of two.
        /// </summary>
        /// <param name="x">The value whose power-of-two-ceiling is computed.</param>
        /// <returns>The integer power of two.</returns>
        public static int PowerOfTwoCeiling(int x)
        {
            Contract.Ensures(Contract.Result<int>() == altPowerOfTwoCeiling(x));

            if (x <= 256)
            {
                if (x > 16)
                {
                    if (x > 64)
                    {
                        if (x > 128)
                            return 256;
                        else
                            return 128;
                    }
                    else
                    {
                        if (x > 32)
                            return 64;
                        else
                            return 32;
                    }
                }
                else
                {
                    if (x > 4)
                    {
                        if (x > 8)
                            return 16;
                        else
                            return 8;
                    }
                    else
                    {
                        if (x > 2)
                            return 4;
                        else if (x == 2)
                            return 2;
                        else
                            return 1;
                    }
                }
            }
            else
            {
                if (x > 524288)
                {
                    if (x > 16777216)
                    {
                        if (x > 134217728)
                        {
                            if (x > 268435456)
                            {
                                if (x > 536870912)
                                    return 1073741824;
                                else
                                    return 536870912;
                            }
                            else
                                return 268435456;
                        }
                        else
                        {
                            if (x > 33554432)
                            {
                                if (x > 67108864)
                                    return 134217728;
                                else
                                    return 67108864;
                            }
                            else
                                return 33554432;
                        }
                    }
                    else
                    {
                        if (x > 2097152)
                        {
                            if (x > 4194304)
                            {
                                if (x > 8388608)
                                    return 16777216;
                                else
                                    return 8388608;
                            }
                            else
                                return 4194304;
                        }
                        else
                        {
                            if (x > 1048576)
                                return 2097152;
                            else
                                return 1048576;
                        }
                    }
                }
                else
                {
                    if (x > 8192)
                    {
                        if (x > 65536)
                        {
                            if (x > 131072)
                            {
                                if (x > 262144)
                                    return 524288;
                                else
                                    return 262144;
                            }
                            else
                                return 131072;
                        }
                        else
                        {
                            if (x > 16384)
                            {
                                if (x > 32768)
                                    return 65536;
                                else
                                    return 32768;
                            }
                            else
                                return 16384;
                        }
                    }
                    else
                    {
                        if (x > 1024)
                        {
                            if (x > 2048)
                            {
                                if (x > 4096)
                                    return 8192;
                                else
                                    return 4096;
                            }
                            else
                                return 2048;
                        }
                        else
                        {
                            if (x > 512)
                                return 1024;
                            else
                                return 512;
                        }
                    }
                }
            }
        }

        private static int altPowerOfTwoCeiling(int x)
        {
            int ret = 1;
            while (ret < x && ret > 0)
                ret <<= 1;

            return ret > 0 ? ret : 0x40000000;
        }

        /// <summary>
        /// Calculates the (integer) exponent of two corresponding to the given integer power of two.
        /// </summary>
        /// <param name="inp">The power of two.</param>
        /// <returns>The exponent of two.</returns>
#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ExponentOfTwo(int inp)
        {
            Contract.Requires((inp & (inp + 1)) == 0); // inp should be a power of two
            Contract.Ensures((1 << Contract.Result<int>()) == inp);

            uint pow = (uint)inp;
            return ((pow & 0xaaaaaaaa) == 0 ? 0 : 1) |
                ((pow & 0xcccccccc) == 0 ? 0 : 2) |
                ((pow & 0xf0f0f0f0) == 0 ? 0 : 4) |
                ((pow & 0xff00ff00) == 0 ? 0 : 8) |
                ((pow & 0xffff0000) == 0 ? 0 : 16);
        }
    }
}
