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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ZeNET.Text
{
    /// <summary>
    /// Represents an error encountered while trying to parse a string (such as an executable script).
    /// </summary>
    [Serializable]
    public class ParseException : Exception
    {
        /// <summary>
        /// Indicates the line number in the source string (<see cref="SourceString"/>) where the
        /// error occurred.
        /// </summary>
        public int ErrorLine { get; private set; }
        /// <summary>
        /// Indicates the column number on the line (in the source string <see cref="SourceString"/>)
        /// where the error occurred.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// The source string in which the error occurred.
        /// </summary>
        protected string SourceString { get; private set; }

        /// <inheritdoc/>
        public override string Message { get { return this.message; } }

        private string message;

        /// <summary>
        /// Initializes the parse exception based on a source string, the location in the source
        /// string where the error occurred, and a headline message.
        /// </summary>
        /// <param name="src">The source string where the parse exception occurred.</param>
        /// <param name="location">The location of the error specified as an character offset from
        /// the start of the string.</param>
        /// <param name="mainMsg">The description of the error, which will be included as the first
        /// line of the <see cref="Message"/>.</param>
        public ParseException(string src, int location, string mainMsg) { this.initialize(src, location, mainMsg); }

        /// <inheritdoc cref="ParseException.ParseException(string, int, string)" select="summary|remarks|param"/>
        /// <summary>
        /// Initializes the parse exception based on a source string, the location in the source
        /// string where the error occurred, a headline message, and an inner exception.
        /// </summary>
        /// <param name="src">The source string where the parse exception occurred.</param>
        /// <param name="location">The location of the error specified as an character offset from
        /// the start of the string.</param>
        /// <param name="mainMsg">The description of the error, which will be included as the first
        /// line of the <see cref="Message"/>.</param>
        /// <param name="innerException">The inner exception that caused the parse exception.</param>
        public ParseException(string src, int location, string mainMsg, Exception innerException) : base(mainMsg, innerException) { this.initialize(src, location, mainMsg); }

#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void initialize(string src, int location, string mainMsg)
        {
            if (!location.IsBetween(0, src.Length))
                throw new ArgumentOutOfRangeException("location");
            Contract.EndContractBlock();

            this.SetState(src, location, mainMsg);
        }

        private void SetState(string srcString, int locationInString, string msgStem)
        {
            const int maxLines = 3; // the number of lines that may be included in the message up to and including the error location

            this.SourceString = srcString;

            int lineCount = 0;
            int pos = srcString.IndexOf('\n') + 1, prevPos = 0;
            Queue<int> lineBeginnings = new Queue<int>();
            lineBeginnings.Enqueue(0);

            while (pos <= locationInString && pos > 0 && pos <= srcString.Length)
            {
                lineCount++;
                lineBeginnings.Enqueue(pos);
                if (lineBeginnings.Count > maxLines)
                    lineBeginnings.Dequeue();
                prevPos = pos;
                pos = srcString.IndexOf('\n', pos) + 1;
            }
            this.ErrorLine = lineCount + 1;
            this.Column = locationInString - prevPos + 1;

            int startOffset = 0;
            if (lineBeginnings.Count > 0)
                startOffset = lineBeginnings.Dequeue() - locationInString;

            startOffset = System.Math.Max(-100, startOffset);

            string messageSeparator = Regex.IsMatch(msgStem, @"\s$") ? "" : " ";

            this.message = String.Format("{0}Parse error at \u21E8({1},{2}): {3}{4}\u21E8{5}", msgStem + messageSeparator, this.ErrorLine, this.Column,
                locationInString + startOffset > 0 ? "\u2026" : "",
                srcString.FreeSubstring(locationInString, startOffset),
                srcString.FreeSubstring(locationInString, 20));
        }
    }
}
