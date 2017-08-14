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
using System.Diagnostics;
using System.Reflection;

namespace ZeNET.Core.Compatibility
{
    /// <inheritdoc cref="System.Diagnostics.Contracts.ContractInvariantMethodAttribute"/>
    [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
    [AttributeUsageAttribute(AttributeTargets.Method, AllowMultiple = false,
        Inherited = false)]
    public sealed class ContractInvariantMethodAttribute : Attribute { }

    /// <inheritdoc cref="System.Diagnostics.Contracts.PureAttribute"/>
    [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
    [AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.Delegate,
        AllowMultiple = false, Inherited = true)]
    public sealed class PureAttribute : Attribute { }

    /// <inheritdoc cref="System.Diagnostics.Contracts.Contract"/>
    public static class Contract
    {
        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Assert(bool)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Assert(bool condition) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Assert(bool, string)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Assert(bool condition, string userMessage) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Assume(bool)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Assume(bool condition) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Assume(bool, string)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Assume(bool condition, string userMessage) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.EndContractBlock"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void EndContractBlock() { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Ensures(bool)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Ensures(bool condition) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Ensures(bool, string)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Ensures(bool condition, string userMessage) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.EnsuresOnThrow{TException}(bool)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void EnsuresOnThrow<TException>(bool condition) where TException : Exception { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.EnsuresOnThrow{TException}(bool, string)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void EnsuresOnThrow<TException>(bool condition, string userMessage) where TException : Exception { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Requires(bool)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Requires(bool condition) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Requires{TException}(bool)"/>
        public static void Requires<TException>(bool condition) where TException : Exception
        {
            if (!condition)
            {
                ConstructorInfo ctor = typeof(TException).GetConstructor(new Type[] { });
                if (ctor != default(ConstructorInfo))
                    throw (TException)ctor.Invoke(new object[] { });
                else
                    throw new Exception("Exception of type " + typeof(TException).ToString() + " thrown.");
            }
        }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Requires{TException}(bool, string)"/>
        public static void Requires<TException>(bool condition, string userMessage) where TException : Exception
        {
            if (!condition)
            {
                ConstructorInfo ctor = typeof(TException).GetConstructor(new Type[] { typeof(string) });
                if (ctor != default(ConstructorInfo))
                    throw (TException)ctor.Invoke(new object[] { userMessage });
                else
                    throw new Exception("Exception of type " + typeof(TException).ToString() + " thrown.");
            }
        }


        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Invariant(bool)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Invariant(bool condition) { }


        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Invariant(bool, string)"/>
        [ConditionalAttribute("SHOULD_NEVER_BE_SET")]
        public static void Invariant(bool condition, string userMessage) { }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.Result{T}"/>
        public static T Result<T>() { return default(T); }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.ValueAtReturn{T}(out T)"/>
        public static T ValueAtReturn<T>(out T value) { value = default(T); return default(T); }

        /// <inheritdoc cref="System.Diagnostics.Contracts.Contract.OldValue{T}(T)"/>
        public static T OldValue<T>(T value) { return default(T); }
    }
}