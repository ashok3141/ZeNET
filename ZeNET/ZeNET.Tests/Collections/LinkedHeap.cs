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
        }

        [TestMethod]
        public void LinkedHeap_Comprehensive()
        {
            LinkedHeap<HeapTestClass> heap = new LinkedHeap<HeapTestClass>();
            SortedSet<HeapTestClass> sortedSet = new SortedSet<HeapTestClass>(new StrictComparerHeapTest());
            HashSet<HeapTestClass> hashSet = new HashSet<HeapTestClass>();
            Random r = new Random();
            Func<int> ordGenerator = () => { return r.Next(0, 50); };
            const int testMaxLen = 200;

            for (int maxSize = 1; maxSize < testMaxLen; maxSize++)
            {
                // Serial addition
                try
                {
                    for (int i = 0; i < maxSize; i++)
                    {
                        int ord = ordGenerator();
                        string label = new String(Enumerable.Range(0, 10).Select(randChar => (char)r.Next(33, 96 + 26)).ToArray());
                        HeapTestClass obj = new HeapTestClass(label, ord);
                        heap.Add(obj);
                        hashSet.Add(obj);
                        sortedSet.Add(obj);
                        Assert.IsTrue(heap.Max.CompareTo(sortedSet.Max) == 0, String.Format("Max elements do not match during Serial Addition. Index is {0} of {1}", i, maxSize));
                        Assert.AreEqual<int>(sortedSet.Count, heap.Count, String.Format("Count of the heap is wrong Serial Addition for maxSize {0}", maxSize));
                        Assert.IsTrue(hashSet.SetEquals(heap), String.Format("Enumerable does not have the right set of elements during Serial Addition. i = {0}, maxSize = {1}. {2} vs {3}",
                            i,
                            maxSize,
                            String.Join(", ", hashSet.Select(item => item.Ord.ToString())),
                            String.Join(", ", heap.Select(item => item.Ord.ToString()))
                        ));
                    }
                } catch (Exception ex)
                {
                    throw new Exception("Failure during Serial Addition: " + ex.Message, ex);
                }

                // Refreshes
                HeapTestClass[] objArray = sortedSet.ToArray();
                for (int i = 0; i < maxSize; i++)
                {
                    try
                    {
                        HeapTestClass obj = objArray[i];
                        sortedSet.Remove(obj);
                        obj.Ord = ordGenerator();
                        sortedSet.Add(obj);
                        heap.Refresh(obj);
                        Assert.IsTrue(heap.Max.CompareTo(sortedSet.Max) == 0, String.Format("Max elements do not match during Refresh test. Index is {0} of {1}", i, maxSize));
                        Assert.IsTrue(hashSet.SetEquals(heap), "Enumerable does not have the right set of elements during Refresh.");
                    } catch (Exception ex)
                    {
                        throw new Exception(String.Format("Failure during Refreshes at i = {0}, maxSize = {1}: {2}", i, maxSize, ex.Message), ex);
                    }
                }

                // Random removals
                ListTestHelper.ShuffleArray(objArray);
                try
                {
                    for (int i = 0; i < maxSize; i++)
                    {
                        HeapTestClass obj = objArray[i];
                        Assert.IsTrue(heap.Max.CompareTo(sortedSet.Max) == 0, String.Format("Max elements do not match during Random Removal test. Index is {0} of {1}", i, maxSize));
                        sortedSet.Remove(obj);
                        heap.Remove(obj);
                        hashSet.Remove(obj);
                        Assert.IsTrue(hashSet.SetEquals(heap), "Enumerable does not have the right set of elements during Random Removal.");
                    }
                } catch (Exception ex)
                {
                    throw new Exception("Failure during Random Removals: " + ex.Message, ex);
                }

                // Repeat Addition
                try
                {
                    for (int i = 0; i < objArray.Length; i++)
                    {
                        HeapTestClass obj = objArray[i];
                        heap.Add(obj);
                        sortedSet.Add(obj);
                        hashSet.Add(obj);
                        Assert.IsTrue(heap.Max.CompareTo(sortedSet.Max) == 0, String.Format("Max elements do not match during Repeat Addition. Index is {0} of {1}", i, maxSize));
                        Assert.IsTrue(hashSet.SetEquals(heap), "Enumerable does not have the right set of elements during Repeat Addition.");
                        Assert.AreEqual<int>(sortedSet.Count, heap.Count, String.Format("Count of the heap is wrong Repeat Addition for maxSize {0}", maxSize));
                    }
                } catch (Exception ex)
                {
                    throw new Exception("Failure during Repeat Addition: " + ex.Message, ex);
                }

                // Serial Deletion
                try
                {
                    for (int i = 0; i < maxSize; i++)
                    {
                        HeapTestClass obj = heap.DeleteMax();
                        Assert.IsTrue(obj.CompareTo(sortedSet.Max) == 0, String.Format("Max elements do not match during serial-deletion test when maxSize = {0}, deletions = {1}", maxSize, i + 1));
                        sortedSet.Remove(obj);
                        hashSet.Remove(obj);
                        Assert.AreEqual<int>(hashSet.Count, heap.Count, String.Format("Count of the heap is wrong during Serial Deletion for maxSize {0}", maxSize));
                        Assert.IsTrue(hashSet.SetEquals(heap), String.Format("Enumerable does not have the right set of elements during Serial Deletion. i = {0}, maxSize = {1}. {2} vs {3}",
                            i,
                            maxSize,
                            String.Join(", ", hashSet.Select(item => item.Ord.ToString())),
                            String.Join(", ", heap.Select(item => item.Ord.ToString()))
                        ));
                    }
                } catch (Exception ex)
                {
                    throw new Exception("Failure during Serial Deletion: " + ex.Message, ex);
                }
            }
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

        private class HeapTestClass : ILinkedHeapNode, IComparable<HeapTestClass>
        {
            private static long Id = 0;

            public int HeapIndex { get; set; }
            public string Label { get; private set; }
            public int Ord { get; set; }
            public long MyId { get; private set; }


            public HeapTestClass(string label, int ord)
            {
                this.Label = label;
                this.Ord = ord;
                this.MyId = ++Id;
            }

            public int CompareTo(HeapTestClass other)
            {
                return this.Ord.CompareTo(other.Ord);
            }
        }

        private class StrictComparerHeapTest : Comparer<HeapTestClass>
        {
            public override int Compare(HeapTestClass item1, HeapTestClass item2)
            {
                int ret = item1.Ord.CompareTo(item2.Ord);
                if (ret == 0)
                    return item1.MyId.CompareTo(item2.MyId);
                else
                    return ret;
            }
        }
    }
}
