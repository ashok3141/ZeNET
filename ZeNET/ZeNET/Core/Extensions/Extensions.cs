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
#if Framework_4
using System.Diagnostics.Contracts;
using System.Linq;
#else
using ZeNET.Core.Compatibility;
using ZeNET.Core.Compatibility.ProLinq;
using ZeNET.Core.Compatibility.ProSystem;
#endif
// End: standard inclusion list

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZeNET.Core.Extensions
{
    /// <summary>
    /// Provides an assortment of general-purpose extension methods.
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// A generalized integer modulo that that allows an offset.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <param name="lBound">The offset.</param>
        /// <returns>
        /// The integer in the range from <paramref name="lBound"/> (bound included) to
        /// <paramref name="lBound"/> + <paramref name="divisor"/> (bound excluded) that
        /// can be obtained by adding an integer multiple of <paramref name="divisor"/> to
        /// <paramref name="dividend"/>.
        /// </returns>
        /// <remarks>
        /// Negative, as well as positive, integers are permitted for all arguments, and except
        /// <paramref name="divisor"/> the arguments may be 0.
        /// </remarks>
#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int Mod(this int dividend, int divisor, int lBound)
        {
            Contract.Requires(divisor != 0);
            Contract.Ensures(((Func<int, bool>)(ret =>
                    (divisor > 0 && ret >= lBound && ret < lBound + divisor) ||
                    (divisor < 0 && ret <= lBound && ret > lBound + divisor)
                ))(Contract.Result<int>())
            );

            Contract.Ensures(Contract.Result<int>() == modAltCalculation(dividend, divisor, lBound)); // an alternative way to calculate it, surely slower

            int res = (dividend - lBound) % divisor;
            if (res != 0 && ((res ^ divisor) & Int32.MinValue) == Int32.MinValue) // res and divisor have opposite signs
                return lBound + res + divisor;
            else
                return lBound + res;
        }

        /// <summary>
        /// A generalized modulo that allows an offset.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <param name="lBound">The offset.</param>
        /// <returns>The unique number that both lies in the range from <paramref name="lBound"/> to
        /// <paramref name="lBound"/> + <paramref name="divisor"/> and can be obtained by subtracting
        /// an integer multiple of <paramref name="divisor"/> from <paramref name="dividend"/>.
        /// </returns>
        /// <remarks>
        /// Negative, as well as positive, integers are permitted for all arguments, and except
        /// <paramref name="divisor"/> the arguments may be 0.
        /// </remarks>
        public static double Mod(this double dividend, double divisor, double lBound)
        {
            if (divisor == 0)
                throw new ArgumentException("divisor should be nonzero", "divisor");
            Contract.Ensures(((Func<double, bool>)(delegate (double x) { return System.Math.Abs(x - System.Math.Round(x, 0)) < 1E-13; }))
                ((dividend - Contract.Result<double>()) / divisor)
            );
            Contract.Ensures(Contract.Result<double>().IsBetween(lBound, lBound + divisor));
            Contract.EndContractBlock();

            return dividend - System.Math.Floor((dividend - lBound) / divisor) * divisor;
        }

        /// <summary>
        /// A generalized integer modulo that returns a value from 0 to <paramref name="divisor"/>.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <returns>
        /// The integer in the range from 0 (bound included) to <paramref name="divisor"/> (bound
        /// excluded) that can be obtained by adding an integer multiple of <paramref name="divisor"/>
        /// to <paramref name="dividend"/>.
        /// </returns>
        /// <remarks>
        /// Negative, as well as positive, integers are permitted for all arguments, and except
        /// <paramref name="divisor"/> the arguments may be 0.
        /// </remarks>
#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int Mod(this int dividend, int divisor)
        {
            return dividend.Mod(divisor, 0);
        }

        private static int modAltCalculation(int dividend, int divisor, int lBound)
        {
            int diff = dividend - lBound;
            int factor = diff / divisor;

            for (int i = -1; i <= 0; i++)
            {
                int candidate = dividend - (factor + i) * divisor;
                if (divisor > 0)
                {
                    if (candidate >= lBound && candidate < lBound + divisor)
                        return candidate;
                }
                else
                {
                    if (candidate <= lBound && candidate > lBound + divisor)
                        return candidate;
                }
            }

            throw new Exception("Something went wrong. Quitting.");
        }

        /// <summary>
        /// Indicates whether a value lies between two values (bounds included), with the bounds
        /// specified in any order.
        /// </summary>
        /// <typeparam name="T">The type of all three values.</typeparam>
        /// <param name="value">The value tested for lying in the range.</param>
        /// <param name="a">One inclusive bound of the range.</param>
        /// <param name="b">The other inclusive bound of the range.</param>
        /// <returns><b>True</b> if <paramref name="value"/> is between <paramref name="a"/> and
        /// <paramref name="b"/> according to <see cref="IComparable{T}.CompareTo(T)"/>.</returns>
#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool IsBetween<T>(this T value, T a, T b)
            where T : IComparable<T>
        {
            Contract.Ensures(Contract.Result<bool>() == (value.CompareTo(a) >= 0 && value.CompareTo(b) <= 0) ||
                (value.CompareTo(a) <= 0 && value.CompareTo(b) >= 0));

            int cmpA = value.CompareTo(a);
            if (cmpA == 0)
                return true;

            int cmpB = value.CompareTo(b);
            if (cmpB == 0)
                return true;

            return (((cmpA ^ cmpB) & Int32.MinValue) == Int32.MinValue);
        }
    }
}
