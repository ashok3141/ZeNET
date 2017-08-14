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



using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeNET.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ZeNET.Tests.Collections
{
    [TestClass]
    public class LinkedHeap
    {
        [TestMethod]
        public void LinkedHeap_BasicTest()
        {
            LinkedHeap<TestClass> heap =
                new LinkedHeap<TestClass>(
                    delegate (TestClass tc1, TestClass tc2)
                    {
                        return tc1.Ord.CompareTo(tc2.Ord);
                    }
            );
            SortedDictionary<int, TestClass> fromInt = new SortedDictionary<int, TestClass>();
            Random r = new Random();

            for (int reps = 0; reps < 20; reps++)
            {
                fromInt.Clear();
                while (fromInt.Count < 1000)
                {
                    string key = new String(
                        Enumerable.Range(0, 20)
                        .Select(i => (char)r.Next(32, (int)'z'))
                        .ToArray()
                    );
                    int randomInt = r.Next();
                    TestClass tc = new TestClass(key, r.Next());

                    if (!fromInt.ContainsKey(randomInt))
                    {
                        fromInt[randomInt] = tc;
                        try
                        {
                            heap.Add(tc);
                        }
                        catch (Exception ex)
                        {
                            Assert.Fail("Failure while adding a new item when the existing count is " + fromInt.Count.ToString() + ": " + ex.Message);
                        }
                    }
                }


                if (heap.Count > 0)
                {
                    int prevKey = heap.DeleteMax().Ord;
                    while (heap.Count > 0)
                    {
                        int ord = heap.DeleteMax().Ord;
                        if (ord > prevKey)
                            Assert.Fail("Ordering reported by the heap is incorrect.");
                        prevKey = ord;
                    }
                }
            }

            // TODO: Add code to use the SortedDictionary properly. Alternatively, use SortedSet.

            //while (heap.Count > 0)
            //    lst.Add(heap.DeleteMax());

            //Assert.IsTrue(lst.SequenceEqual(fromInt.Select(val => val.Value)), "The order in the heap was not correct: " +
            //    String.Join(",", fromInt.Select(v => v.Value.Ord.ToString())) + " != " + String.Join(",", lst.Select(tc => tc.Ord.ToString())));

        }

        public class TestClass : ILinkedHeapNode
        {
            public int HeapIndex { get; set; }
            public string Label { get; private set; }
            public int Ord { get; private set; }

            public TestClass(string label, int ord)
            {
                this.Label = label;
                this.Ord = ord;
            }
        }
    }
}
