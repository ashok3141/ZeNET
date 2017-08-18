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
using System.Text;

namespace ZeNET.Core.Extensions
{
    /// <summary>
    /// Provides extension methods that add to the string-handling capabilities of the .NET library.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Extracts a substring without imposing constraints on <paramref name="startIndex"/> or
        /// <paramref name="length"/>, not even on their signs or their relative magnitudes.
        /// </summary>
        /// <param name="src">The source string from which to extract a substring.</param>
        /// <param name="startIndex">The offset of the start position of the substring.</param>
        /// <param name="length">The increment to <paramref name="startIndex"/> that would identify
        /// the offset of the other extreme of the substring.</param>
        /// <returns>The substring.</returns>
        /// <remarks>
        /// <para>
        /// The returned value is identical to that of <see cref="System.String.Substring(int, int)"/>
        /// for the same arguments whenever it returns a substring without throwing exceptions.
        /// Character offsets outside the bounds of the string are treated as pointing to
        /// zero-length characters.
        /// </para>
        /// <para>
        /// The returned string always retains the order of the characters in <paramref name="src"/>,
        /// even when <paramref name="length"/> is negative.
        /// </para>
        /// </remarks>
        public static string FreeSubstring(this string src, int startIndex, int length)
        {
            // Works the same as String.Substring whenever startIndex and length are allowed by String.Substring:
            Contract.Ensures(startIndex < 0 ||
                startIndex > src.Length ||
                length < 0 ||
                length > src.Length - startIndex ||
                Contract.Result<string>() == src.Substring(startIndex, length)
            );
            Contract.Ensures(src.Contains(Contract.Result<string>()));
            Contract.Ensures(System.Math.Abs(length) >= Contract.Result<string>().Length);

            int requestedEnd = startIndex + length;

            if (startIndex < 0)
                startIndex = 0;
            else if (startIndex > src.Length)
                startIndex = src.Length;

            if (requestedEnd < 0)
                requestedEnd = 0;
            else if (requestedEnd > src.Length)
                requestedEnd = src.Length;

            if (requestedEnd < startIndex)
                return src.Substring(requestedEnd, startIndex - requestedEnd);
            else
                return src.Substring(startIndex, requestedEnd - startIndex);
        }


        /// <summary>
        /// Extracts a substring without constraints on (even the sign of) <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="src">The source string from which to extract a substring.</param>
        /// <param name="startIndex"></param>
        /// <returns>The substring starting at the position with offset <paramref name="startIndex"/>
        /// and going to the end of the string.</returns>
        public static string FreeSubstring(this string src, int startIndex)
        {
            return src.FreeSubstring(startIndex, src.Length - startIndex);
        }

        /// <summary>
        /// Converts an array of values to a CSV string, with fields escaped if necessary.
        /// </summary>
        /// <typeparam name="T">The type of the values (may be <see cref="object"/>).</typeparam>
        /// <param name="array">The array of values.</param>
        /// <returns>The CSV string.</returns>
        public static string ToCSVRow<T>(this T[] array)
        {
            using (IEnumerator<T> enumerator = ((IEnumerable<T>)array).GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    StringBuilder sb = new StringBuilder();
                    string str = enumerator.Current.ToString();
                    if (str.Contains("\""))
                        str = "\"" + str.Replace("\"", "\"\"") + "\"";
                    else if (str.Contains(",") || str.Contains("\n") || str.Contains("\r"))
                        str = "\"" + str + "\"";

                    sb.Append(str);

                    while (enumerator.MoveNext())
                    {
                        sb.Append(",");
                        str = enumerator.Current.ToString();
                        if (str.Contains("\""))
                            str = "\"" + str.Replace("\"", "\"\"") + "\"";
                        else if (str.Contains(",") || str.Contains("\n") || str.Contains("\r"))
                            str = "\"" + str + "\"";
                        sb.Append(str);
                    }

                    return sb.ToString();
                }
                else
                    return String.Empty;
            }
        }
    }
}
