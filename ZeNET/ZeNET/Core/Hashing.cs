/******************************************************************************/
// The Murmur3 hashing algorithm used in this class was adapted from

// https://github.com/PeterScott/murmur3/blob/master/murmur3.c

// The original author (Austin Appleby) and the author of the above (Peter
// Scott) have placed the algorithm and the source in the public domain. The
// copyright notice below applies only to this adaptation, not to the original
// sources.

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
    /// Provides methods useful in special-purpose hashing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <em>NOTE: Unit testing for this class is pending.</em>
    /// </para>
    /// </remarks>
    public static class Hashing
    {
        /// <summary>
        /// Calculates an <see cref="int"/> hash code for an array from the hash codes of its
        /// elements, adapted from the 32-bit Murmur3 hash algorithm.
        /// </summary>
        /// <param name="array">The source array whose elements contribute to the hash.</param>
        /// <returns>The hash based on the elements of the array.</returns>
        /// <remarks>
        /// <para>The method uses a fixed seed that was randomly generated (0xc8d28b43).</para>
        /// <para>
        /// <em>NOTE: Unit testing for this class is pending.</em>
        /// </para>
        /// </remarks>
        public static int Murmur3_32bit<T>(T[] array)
        {
            return Murmur3_32bit(array, 0xc8d28b43);
        }

        /// <summary>
        /// Calculates an <see cref="int"/> hash code for an array from the hash codes of its
        /// elements, adapted from the 32-bit Murmur3 hash algorithm.
        /// </summary>
        /// <param name="array">The source array whose elements contribute to the hash.</param>
        /// <param name="seed">The seed to use to generate the hash.</param>
        /// <returns>The hash based on the elements of the array.</returns>
        public static int Murmur3_32bit<T>(T[] array, uint seed)
        {
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            int arrayLength = array.Length;
            uint h1;

            if (arrayLength > 0)
            {
                uint k1 = (uint)array[0].GetHashCode() * c1;
                h1 = seed ^ (((k1 << 15) | (k1 >> 17)) * c2);
                h1 = ((h1 << 13) | (h1 >> 19)) * 5 + 0xe6546b64;

                for (int i = 1; i < arrayLength; i++)
                {
                    k1 = (uint)array[i].GetHashCode() * c1;
                    h1 ^= ((k1 << 15) | (k1 >> 17)) * c2;
                    h1 = ((h1 << 13) | (h1 >> 19)) * 5 + 0xe6546b64;
                }
            }
            else
                h1 = seed;

            h1 ^= (uint)(arrayLength << 2);

            // fmix32
            h1 = (h1 ^ (h1 >> 16)) * 0x85ebca6b;
            h1 = (h1 ^ (h1 >> 13)) * 0xc2b2ae35;

            return (int)(h1 ^ (h1 >> 16));
        }
    }
}
