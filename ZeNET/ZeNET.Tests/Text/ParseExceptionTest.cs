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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using ZeNET.Text;

namespace ZeNET.Tests.Text
{
    [TestClass]
    public class ParseExceptionTest
    {
        [TestMethod]
        public void ParseException_Basic()
        {
            string[] charSet = Enumerable.Range(32, 10).Select(i => new String(new char[] { (char)i })).Concat(new string[] { "\r", "\r\n" }).ToArray();
            Random r = new Random();

            for (int reps = 0; reps < 50; reps++)
            {
                string inp = String.Concat(Enumerable.Range(0, 100).Select(i => charSet[r.Next(0, charSet.Length)]));

                for (int i = 0; i <= inp.Length; i++)
                {
                    ParseException pe = default(ParseException);
                    try
                    {
                        pe = new ParseException(inp, i, "Error.");
                    }
                    catch (Exception e)
                    {
                        Assert.Fail(String.Format("Exception caught in forming ParseException with src = {0}, location = {1}. Text of exception = {2}", inp, i, e.Message));
                    }

                    int errorPos = i;

                    Regex rexp = new Regex(@"(?> [^\n] | (?<NewLine>\n)){" + errorPos.ToString() + @"}", RegexOptions.IgnorePatternWhitespace);
                    Match m = rexp.Match(inp);
                    if (!m.Success)
                        Assert.Fail("Unexpected error. Suspect a bug in the unit testing code, but not necessarily in the code being tested.");
                    else
                    {
                        int errorLine = m.Groups["NewLine"].Captures.Count + 1;
                        int columnNumber = -1;
                        if (m.Groups["NewLine"].Success)
                            columnNumber = m.Groups["NewLine"].Captures.Cast<Capture>().Last().Index;

                        columnNumber = errorPos - columnNumber;

                        if (errorLine != pe.ErrorLine || columnNumber != pe.Column)
                            Assert.Fail("Incorrect calculation detected. ({0},{1}) is correct, ({2},{3}) is not. Input string was {4}, error location was {5} in repetition {6}. String length was {7}. FInal character had ASCII code {8}.", errorLine, columnNumber, pe.ErrorLine, pe.Column, inp, errorPos, reps, inp.Length, (int)inp[inp.Length - 1]);
                    }
                }
            }
        }
    }
}
