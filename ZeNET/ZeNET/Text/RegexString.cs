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
    /// Represents a regular expression pattern (with encapsulated options) that can integrate and
    /// be integrated into other such patterns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of this class, along with its methods, are useful in building various kinds of
    /// parsers, including those that can parse executable scripts.
    /// </para>
    /// <para>
    /// The variable names must consist of only <a href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions#WordCharacter">word characters</a>.
    /// On a standard US keyboard, this consists of all the alphanumeric characters and the
    /// underscore (_).
    /// </para>
    /// </remarks>
    public class RegexString
    {
        private static readonly Regex validOptions = new Regex(@"^  (?<def>[imsnx]*) (?> - (?<undef>[imsnx]+) )? $", RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex splittingRegex = new Regex(@" (?<var>%%) | % (?<B>\{)? (?<var>\w+) (?<-B>\})? ", RegexOptions.IgnorePatternWhitespace);

        private string pattern;
        private string options;

        private static string EliminateDuplicates(string inp)
        {
            Dictionary<char, byte> charSet = new Dictionary<char, byte>();
            foreach (char c in inp) charSet[c] = 0;

            char[] arr = new char[charSet.Count];
            charSet.Keys.CopyTo(arr, 0);

            return new string(arr);
        }

        /// <summary>
        /// Casts the string into a <see cref="RegexString"/> with no encapsulated options.
        /// </summary>
        /// <param name="value">The string to cast.</param>
        /// <remarks>
        /// The cast produces the same result as <see cref="RegexString.RegexString(string)"/>.
        /// </remarks>
        public static implicit operator RegexString(string value)
        {
            return new RegexString(value);
        }

        /// <summary>
        /// Creates a pure string, with encapsulated options, from a <see cref="RegexString"/> that
        /// can be used as a pattern to instantiate a <see cref="Regex"/> object.
        /// </summary>
        /// <param name="rvalue">The source of the pattern and options.</param>
        /// <remarks>
        /// The cast produces the same result as <see cref="RegexString.GetWrappedValue"/>.
        /// </remarks>
        public static explicit operator string(RegexString rvalue)
        {
            return rvalue.GetWrappedValue();
        }

        /// <summary>
        /// Creates a <see cref="RegexString"/> with no encapsulated options.
        /// </summary>
        /// <param name="inp">The source pattern.</param>
        public RegexString(string inp) : this(inp, String.Empty) { }

        /// <summary>
        /// Creates a <see cref="RegexString"/> with the specified encapsulated options.
        /// </summary>
        /// <param name="inp">The source pattern, without the options.</param>
        /// <param name="options">The set of letters specifying the options acceptable in a <see cref="Regex"/> object.</param>
        /// <remarks>
        /// <para>
        /// This class allows one to create re-usable regular expression objects, much like Perl
        /// does. Building a large regular expression pattern from smaller pieces also improves
        /// readability.
        /// </para>
        /// <para>
        /// The only acceptable letters for options are: i, m, s, n, and x. The string <paramref name="options"/>
        /// should be formatted as [<i>options to set</i>]-[<i>options to unset</i>], where either
        /// group may be omitted.
        /// </para>
        /// <para>
        /// Some strings valid as <see cref="options"/> are: <code>i</code> (set "ignore case"),
        /// <code>x-i</code> (set "ignore pattern whitespace", and unset "ignore case"), and
        /// <code>-ix</code> (unset both "ignore case" and "ignore pattern whitespace").
        /// </para>
        /// <para>
        /// Note that the options have effect only within the pattern (therefore, within any pattern
        /// integrated into the pattern).
        /// </para>
        /// </remarks>
        public RegexString(string inp, string options)
        {
            Match m = validOptions.Match(options);
            if (!m.Success)
                throw new Exception(@"Invalid options specified (must contain only letters from 'imsnx'): " + options);

            this.pattern = inp;

            this.options = EliminateDuplicates(m.Groups["def"].Value) +
                (
                    m.Groups["undef"].Success ? "-" + EliminateDuplicates(m.Groups["undef"].Value) : String.Empty
                );
        }

        /// <summary>
        /// Creates a pure string, with encapsulated options, from a <see cref="RegexString"/> that
        /// can be used as a pattern to instantiate a <see cref="Regex"/> object.
        /// </summary>
        /// <returns>The string that can be used a <see cref="Regex"/> pattern.</returns>
        public string GetWrappedValue()
        {
            if (this.options == String.Empty)
                return String.Format(@"(?>{0})", this.pattern);
            else
                return String.Format(@"(?{0}:{1})", this.options, this.pattern);
        }

        /// <summary>
        /// Creates a pure string, with encapsulated options, from a <see cref="RegexString"/> that
        /// can be used as a pattern to instantiate a <see cref="Regex"/> object.
        /// </summary>
        /// <remarks>
        /// The cast produces the same result as <see cref="RegexString.GetWrappedValue"/>.
        /// </remarks>
        public override string ToString()
        {
            return this.GetWrappedValue();
        }

        private delegate bool ValueFetchingDelegate(string key, out string value);

        /// <summary>
        /// Builds up a string regular expression pattern, substituting in the string patterns
        /// wherever <see cref="RegexString"/> objects are referenced.
        /// </summary>
        /// <param name="pattern">The string pattern into which to interpolate.</param>
        /// <param name="regexVariables">The set of other patterns to use to interpolate, represented
        /// as a dictionary where the names of the string patterns are the keys and the string
        /// patterns are the values.</param>
        /// <param name="comparer">The comparer to use for the keys in <paramref name="regexVariables"/>.</param>
        /// <returns>The built-up final string.</returns>
        public static string Interpolate(string pattern, IDictionary<string, string> regexVariables, IEqualityComparer<string> comparer)
        {
            ValueFetchingDelegate fetcher = regexVariables.TryGetValue;
            if (comparer == default(IEqualityComparer<string>))
                comparer = EqualityComparer<string>.Default;
            return Interpolate(pattern, fetcher, comparer);
        }

        /// <inheritdoc cref="RegexString.Interpolate(string, IDictionary{string, string}, IEqualityComparer{string})"/>
        /// <summary>
        /// Builds up a string regular expression pattern, substituting in the string patterns
        /// wherever <see cref="RegexString"/> objects are referenced.
        /// </summary>
        /// <remarks>The default comparer for strings is used for the keys of the dictionary.</remarks>
        /// <returns>The built-up final string.</returns>
        public static string Interpolate(string pattern, IDictionary<string, string> regexVariables)
        {
            return Interpolate(pattern, regexVariables, EqualityComparer<string>.Default);
        }

        /// <summary>
        /// Builds up a string regular expression pattern, substituting in the string patterns
        /// wherever <see cref="RegexString"/> objects are referenced.
        /// </summary>
        /// <param name="pattern">The string pattern into which to interpolate.</param>
        /// <param name="regexVariables">The set of other patterns to use to interpolate, represented
        /// as a dictionary where the names of the string patterns are the keys and the
        /// <see cref="RegexString"/> patterns are the values.</param>
        /// <param name="comparer">The comparer to use for the keys of dictionary.</param>
        /// <returns>The built-up final string.</returns>
        public static string Interpolate(string pattern, IDictionary<string, RegexString> regexVariables, IEqualityComparer<string> comparer)
        {
            if (comparer == default(IEqualityComparer<string>))
                throw new ArgumentNullException("comparer");

            ValueFetchingDelegate fetcher = delegate (string key, out string value)
            {
                RegexString rexVar;
                bool ret = regexVariables.TryGetValue(key, out rexVar);
                if (ret)
                    value = rexVar.GetWrappedValue();
                else
                    value = default(string);
                return ret;
            };

            if (comparer == default(IEqualityComparer<string>))
                return Interpolate(pattern, fetcher, EqualityComparer<string>.Default);
            else
                return Interpolate(pattern, fetcher, comparer);
        }

        /// <inheritdoc cref="Interpolate(string, IDictionary{string, RegexString}, IEqualityComparer{string})"/>
        /// <remarks>The default comparer for strings is used for the keys of the dictionary.</remarks>
#if Framework_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static string Interpolate(string pattern, IDictionary<string, RegexString> regexVariables)
        {
            return Interpolate(pattern, regexVariables, EqualityComparer<string>.Default);
        }

        private static string[] splitIntoPieces(string expr, out int[] charPositions)
        {
            List<string> pieces = new List<string>();
            List<int> positions = new List<int>();

            Match m;
            int startPos = 0;
            while ((m = splittingRegex.Match(expr, startPos)).Success)
            {
                if (m.Groups["B"].Success)
                {
                    throw new ParseException(expr, m.Index + m.Length, @"Did not find the expected ""}"".");
                    // throw new Exception (String.Format(@"Did not find the expected ""}}"" after position {0}", m.Index + m.Length));
                }

                pieces.Add(expr.Substring(startPos, m.Index - startPos));
                positions.Add(startPos);
                pieces.Add(m.Groups["var"].Value);
                positions.Add(m.Index);

                startPos = m.Index + m.Length;
            }
            pieces.Add(expr.Substring(startPos));
            positions.Add(startPos);

            charPositions = positions.ToArray();
            return pieces.ToArray();
        }

        private static string Interpolate(string pattern, ValueFetchingDelegate valDelegate, IEqualityComparer<string> comparer)
        {
            var interpolated = new Dictionary<string, string>(comparer);

            Func<string, string> getInterpolatedResult = default(Func<string, string>);
            getInterpolatedResult = delegate (string patt)
            {
                int[] charPositions;
                string[] pattPieces = splitIntoPieces(patt, out charPositions);

                for (int i = 1; i < pattPieces.Length; i += 2)
                {
                    string outString;
                    string key = pattPieces[i];
                    if (key == "%%")
                        pattPieces[i] = "%";
                    else if (interpolated.TryGetValue(key, out outString))
                    {
                        if (outString != default(string))
                            pattPieces[i] = outString;
                        else
                            throw new ParseException(patt, charPositions[i], String.Format("Invalid self-reference detected in evaluating \"{0}\".", pattPieces[i]));
                    }
                    else if (valDelegate(key, out outString))
                    {
                        interpolated.Add(key, default(string));
                        try
                        {
                            interpolated[key] = pattPieces[i] = getInterpolatedResult(outString);
                        }
                        catch (Exception e)
                        {
                            throw new VariableNameNotFound(patt, charPositions[i], key, e);
                        }
                    }
                    else
                        throw new VariableNameNotFound(patt, charPositions[i], key);
                }

                return String.Concat(pattPieces);
            };

            return getInterpolatedResult(pattern);
        }

        /// <summary>
        /// Represents the error that a regex variable referenced in a <see cref="RegexString"/> was not defined.
        /// </summary>
        [Serializable]
        public class VariableNameNotFound : ParseException
        {
            /// <summary>
            /// The name of the variable that was not defined.
            /// </summary>
            public string VariableName { get; private set; }

#if Framework_4_5
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            private static string ComposeMessage(string varName)
            {
                return String.Format("Variable not defined: {0}.", varName);
            }

            /// <inheritdoc/>
            public override string Message
            {
                get
                {
                    if (this.InnerException != default(Exception))
                        return String.Format("{0}{1}The above error was caused by:{1}\t{2}.", base.Message, Environment.NewLine, this.InnerException.Message.Replace("\n", "\n\t"));
                    else
                        return base.Message;
                }
            }

            internal VariableNameNotFound(string src, int location, string varName) : base(src, location, ComposeMessage(varName)) { }

            internal VariableNameNotFound(string src, int location, string varName, Exception innerException) : base(src, location, ComposeMessage(varName), innerException) { }
        }
    }
}
