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
using System.Collections.Generic;
using System.Linq;

namespace ZeNET.Tests.Collections
{
    public static class ListTestHelper
    {
        public static bool TestEquality<T>(IList<T> lst1, IList<T> lst2, IComparer<T> comparer, out int mismatchIndex, out string message)
        {

            if (lst1.Count != lst2.Count)
            {
                mismatchIndex = 0;
                message = String.Format("Counts unequal", lst1.Count, lst2.Count);
                return false;
            }

            int lstCount = lst1.Count;
            for (mismatchIndex = 0; mismatchIndex < lstCount; mismatchIndex++)
            {
                if (comparer.Compare(lst1[mismatchIndex], lst2[mismatchIndex]) != 0)
                {
                    message = "Items do not match.";
                    return false;
                }
            }

            if (!lst1.SequenceEqual(lst2))
            {
                mismatchIndex = -1;
                message = "Enumerators don't match.";
                return false;
            }

            mismatchIndex = default(int);
            message = default(string);
            return true;
        }

        public static void ShuffleArray<T>(T[] array)
        {
            int arrLength = array.Length;
            Random r = new Random();
            for (int i = 0; i < arrLength - 1; i++)
            {
                int swapWith = r.Next(i, arrLength);
                T temp = array[i];
                array[i] = array[swapWith];
                array[swapWith] = temp;
            }
        }
    }
}
