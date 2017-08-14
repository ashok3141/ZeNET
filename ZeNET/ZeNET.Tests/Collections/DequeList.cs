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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ZeNET.Collections;
using static ZeNET.Tests.Collections.ListTestHelper;



namespace ZeNET.Tests.Collections
{
    [TestClass]
    public class DequeList
    {
        [TestMethod]
        public void DequeList_AllMethodsIsolated()
        {
            Random r = new Random();
            DequeList<int> list = new DequeList<int>();
            DequeListTester<int> tester = new DequeListTester<int>();

            int mismatchIndex;
            string errorMessage;

            int size = 1000;

            for (int i = 0; i < size; i++)
            {
                int item = r.Next(0, 2 * size);
                list.Add(item);
                tester.Add(item);
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }

            for (mismatchIndex = size - 1; mismatchIndex >= 0; mismatchIndex--)
            {
                int item1 = list.PopRight();
                int item2 = tester.PopRight();
                Assert.AreEqual<int>(item1, item2, String.Format("Mismatch during PopRight of popped items at index {0}", mismatchIndex));
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }

            for (int i = 0; i < size; i++)
            {
                int item = r.Next(0, 2 * size);
                list.PushRight(item);
                tester.PushRight(item);
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }

            for (mismatchIndex = size - 1; mismatchIndex >= 0; mismatchIndex--)
            {
                int item1 = list.PopRight();
                int item2 = tester.PopRight();
                Assert.AreEqual<int>(item1, item2, String.Format("Mismatch during PopRight of popped items at index {0}", mismatchIndex));
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }

            for (int i = 0; i < size; i++)
            {
                int item = r.Next(0, 2 * size);
                list.PushLeft(item);
                tester.PushLeft(item);
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }

            for (mismatchIndex = size - 1; mismatchIndex >= 0; mismatchIndex--)
            {
                int item1 = list.PopLeft();
                int item2 = tester.PopLeft();
                Assert.AreEqual<int>(item1, item2, String.Format("Mismatch during PopLeft of popped items at index {0}", mismatchIndex));
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }

            for (int i = 0; i < size; i++)
            {
                int item = r.Next(0, 2 * size);
                list.PushLeft(item);
                tester.PushLeft(item);
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }

            while (list.Count > 0)
            {
                int x = r.Next(0, list.Count);
                int item1 = list[x];
                int item2 = tester[x];
                list.RemoveAt(x);
                tester.RemoveAt(x);
                Assert.AreEqual<int>(item1, item2, String.Format("Mismatch during RemoveAt of popped items at index {0}", mismatchIndex));
                Assert.IsTrue(TestEquality(list, tester, Comparer<int>.Default, out mismatchIndex, out errorMessage), errorMessage);
            }
        }

        private struct ActionPair
        {
            public string Description { get; private set; }
            public Func<bool> TestAction { get; private set; }
            public ActionPair(string description, Func<bool> testAction) : this()
            {
                this.Description = description;
                this.TestAction = testAction;
            }
        }

        private ActionPair[] MakeActionList<T>(Func<T> generator, DequeList<T> list, DequeListTester<T> tester)
        {
            List<ActionPair> actionList = new List<ActionPair>();
            Random r = new Random();

            actionList.Add(new ActionPair("PushLeft", () =>
            {
                T x = generator();
                list.PushLeft(x);
                tester.PushLeft(x);
                return true;
            }));

            actionList.Add(new ActionPair("PushRight", () =>
            {
                T x = generator();
                list.PushRight(x);
                tester.PushRight(x);
                return true;
            }));

            actionList.Add(new ActionPair("PopLeft", () =>
            {
                T x1 = default(T), x2 = default(T);
                bool e1 = false, e2 = false;

                try { x1 = list.PopLeft(); }
                catch { e1 = true; }
                try { x2 = tester.PopLeft(); }
                catch { e2 = true; }

                if (!e1 && !e2)
                    return x1.Equals(x2);
                else if (e1 != e2)
                    return false;

                return true;
            }));

            actionList.Add(new ActionPair("PopRight", () =>
            {
                T x1 = default(T), x2 = default(T);
                bool e1 = false, e2 = false;

                try { x1 = list.PopRight(); }
                catch { e1 = true; }
                try { x2 = tester.PopRight(); }
                catch { e2 = true; }

                if (!e1 && !e2)
                    return x1.Equals(x2);
                else if (e1 != e2)
                    return false;

                return true;
            }));

            actionList.Add(new ActionPair("Contains", () =>
            {
                if (r.Next(0, 2) == 0) // check for an actually contained item
                {
                    if (list.Count > 0)
                    {
                        int index = r.Next(0, list.Count);
                        T elem = list[index];
                        return list.Contains(elem) && tester.Contains(elem);
                    }
                    else
                        return true;
                }
                else
                {
                    T elem = generator();
                    return list.Contains(elem) == tester.Contains(elem);
                }
            }));

            actionList.Add(new ActionPair("Remove", () =>
            {
                if (r.Next(0, 2) == 0) // check for an actually contained item
                {
                    if (list.Count > 0)
                    {
                        int index = r.Next(0, list.Count);
                        T elem = list[index];
                        return list.Remove(elem) == true && tester.Remove(elem);
                    }
                    else
                        return !list.Remove(generator());
                }
                else
                {
                    T elem = generator();
                    return list.Remove(elem) == tester.Remove(elem);
                }
            }));

            actionList.Add(new ActionPair("IndexOf", () =>
            {
                if (r.Next(0, 2) == 0) // check for an actually contained item
                {
                    if (list.Count > 0)
                    {
                        int index = r.Next(0, list.Count);
                        T elem = list[index];
                        return list.IndexOf(elem) == index && tester.IndexOf(elem) == index;
                    }
                    else
                        return true;
                }
                else
                {
                    T elem = generator();
                    return list.Contains(elem) == tester.Contains(elem);
                }
            }));

            actionList.Add(new ActionPair("RemoveAt", () =>
            {
                if (list.Count > 0)
                {
                    int index = r.Next(0, list.Count);
                    list.RemoveAt(index);
                    tester.RemoveAt(index);
                }

                return true;
            }));

            actionList.Add(new ActionPair("Insert", () =>
            {
                int index = r.Next(0, list.Count);
                T elem = generator();
                try { list.Insert(index, elem); }
                catch (Exception ex) { throw new Exception(String.Format("The index being inserted to was {0}. Element = {1}. Original exception message = {2}.", index, elem, ex.Message), ex); }

                tester.Insert(index, elem);
                return true;
            }));

            return actionList.ToArray();
        }

        [TestMethod]
        public void DequeList_AllMethodsRandomized()
        {
            const int reps = 1000;
            Random r = new Random();
            DequeList<int> list = new DequeList<int>();
            DequeListTester<int> tester = new DequeListTester<int>();
            Func<int> generator = () => { return r.Next(); };

            ActionPair[] testActions = MakeActionList<int>(generator, list, tester);

            int noOfActions = 10;

            for (int repNo = 0; repNo < reps; repNo++)
            {
                ActionPair[] trialRun = Enumerable.Range(0, noOfActions).SelectMany(i => testActions).ToArray();
                ShuffleArray(trialRun);

                for (int i = 0; i < noOfActions; i++)
                {
                    bool res = false;
                    try
                    {
                        res = trialRun[i].TestAction();
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail(String.Format("Exception caught while running test {0}. Count was {1}. Exception thrown was {2}", trialRun[i].Description, list.Count, ex.Message));
                    }

                    if (!res)
                        Assert.Fail(String.Format("The result of test {0} was failure. Count was {1}.", trialRun[i].Description, list.Count));

                    int mismatchIndex;
                    string message;
                    if (!TestEquality<int>(list, tester, Comparer<int>.Default, out mismatchIndex, out message))
                    {
                        Assert.Fail(String.Format("Equality was violated right after running the test {0}. Mismatch occurred at {1}. Message returned was {2}. Count is {3}.", trialRun[i].Description, mismatchIndex, message, list.Count));
                    }
                }

                list.Clear();
                tester.Clear();

                noOfActions += 2;
            }
        }
    }
}
