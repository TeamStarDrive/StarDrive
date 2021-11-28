using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests
{
    public static class AssertExtensions
    {
        public static void Equal(this Assert assert, float expected, float actual)
        {
            if (expected.AlmostEqual(actual))
            {
                return; // OK
            }
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}");
        }

        public static void Equal(this Assert assert, float tolerance, float expected, float actual)
        {
            if (expected.AlmostEqual(actual, tolerance))
            {
                return; // OK
            }
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}");
        }
        
        public static void Equal(this Assert assert, float tolerance, float expected, float actual, string message)
        {
            if (expected.AlmostEqual(actual, tolerance))
            {
                return; // OK
            }
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}. {message}");
        }

        public static void Equal(this Assert assert, in Point expected, in Point actual)
        {
            if (expected.X == actual.X && expected.Y == actual.Y)
                return; // OK
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}");
        }

        public static void Equal(this Assert assert, float tolerance, in Vector2 expected, in Vector2 actual)
        {
            if (expected.X.AlmostEqual(actual.X, tolerance) &&
                expected.Y.AlmostEqual(actual.Y, tolerance))
            {
                return; // OK
            }
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}");
        }

        public static void Equal(this Assert assert, float tolerance, in Vector3 expected, in Vector3 actual)
        {
            if (expected.X.AlmostEqual(actual.X, tolerance) &&
                expected.Y.AlmostEqual(actual.Y, tolerance) &&
                expected.Z.AlmostEqual(actual.Z, tolerance))
            {
                return; // OK
            }
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}");
        }

        public static void Equal(this Assert assert, float tolerance, in Vector4 expected, in Vector4 actual)
        {
            if (expected.X.AlmostEqual(actual.X, tolerance) &&
                expected.Y.AlmostEqual(actual.Y, tolerance) &&
                expected.Z.AlmostEqual(actual.Z, tolerance) &&
                expected.W.AlmostEqual(actual.W, tolerance))
            {
                return; // OK
            }
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}");
        }

        public static void Equal(this Assert assert, object expected, object actual, string message = "")
        {
            if (expected == null)
            {
                if (actual == null) return;
                throw new AssertFailedException($"Expected null does not match Actual {actual}. {message}");
            }

            if (expected is ICollection expectedCollection)
            {
                if (actual is ICollection actualCollection)
                {
                    Equal(assert, expectedCollection, actualCollection);
                }
                else
                {
                    throw new AssertFailedException($"Expected {expected} - collection but got Actual {actual}. {message}");
                }
            }
            else
            {
                Assert.AreEqual(expected, actual, message);
            }
        }

        static object[] ToArray(ICollection c)
        {
            var items = new object[c.Count];
            int i = 0;
            foreach (object o in c)
                items[i++] = o;
            return items;
        }

        static T[] ToArray<T>(ICollection<T> c)
        {
            var items = new T[c.Count];
            c.CopyTo(items, 0);
            return items;
        }

        static string ToString(ICollection c)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            int i = 0, count = c.Count;
            foreach (object o in c)
            {
                sb.Append(o);
                if (++i != count) sb.Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }

        static string ToString<T>(ICollection<T> c)
        {
            return ToString((ICollection)ToArray(c));
        }

        public static void Equal(this Assert assert, ICollection expected, ICollection actual, string message = "")
        {
            if (expected == null)
            {
                if (actual == null) return;
                throw new AssertFailedException($"Expected null does not match Actual {actual}. {message}");
            }

            try
            {
                if (expected.Count != actual.Count)
                    throw new AssertFailedException(
                        $"Expected.Length {expected.Count} does not match Actual.Length {actual.Count}. {message}");

                object[] e = ToArray(expected);
                object[] a = ToArray(actual);

                for (int i = 0; i < e.Length; ++i)
                {
                    assert.Equal(e[i], a[i], message);
                }
            }
            catch (AssertFailedException ex)
            {
                throw new AssertFailedException($"{ex.Message}\nExpected: {ToString(expected)}\nActual: {ToString(actual)}");
            }
        }

        public static void EqualCollections<T>(this Assert assert, ICollection<T> expected, ICollection<T> actual, string message = "")
        {
            if (expected == null)
            {
                if (actual == null) return;
                throw new AssertFailedException($"Expected null does not match Actual {actual}. {message}");
            }

            if (expected.Count != actual.Count)
                throw new AssertFailedException(
                    $"Expected.Length {expected.Count} does not match Actual.Length {actual.Count}. {message}");

            T[] e = ToArray(expected);
            T[] a = ToArray(actual);

            try
            {
                for (int i = 0; i < e.Length; ++i)
                {
                    assert.Equal(e[i], a[i], message);
                }
            }
            catch (AssertFailedException ex)
            {
                throw new AssertFailedException($"{ex.Message}\nExpected: {ToString(expected)}\nActual: {ToString(actual)}");
            }
        }

        public static void MemberwiseEqual<T>(this Assert assert, T expected, T actual, string message = "")
        {
            Array<string> mismatches = expected.MemberwiseCompare(actual);
            if (mismatches.Count > 0)
            {
                string mismatchText = string.Join("\n", mismatches);
                throw new AssertFailedException($"MemberwiseEqual found {mismatches.Count} mismatches: {message}\n{mismatchText}");
            }
        }
        
        static string SafeSubstring(string s, int startIndex, int length, out int inserted)
        {
            inserted = 0;
            string result = "";
            for (int i = startIndex, n = 0; i < s.Length && n < length; ++i, ++n)
            {
                char ch = s[i];
                if      (ch == '\n') { result += "\\n"; ++inserted; }
                else if (ch == '\t') { result += "\\t"; ++inserted; }
                else result += ch;
            }
            return result;
        }

        /// <summary>
        /// Asserts two strings are equal. On failure, generates a comparison
        /// </summary>
        public static void Equal(this Assert assert, string expected, string actual)
        {
            if (string.Equals(expected, actual))
                return;

            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
                throw new AssertFailedException($"Expected:<{expected}>. Actual:<{actual}>.");

            int mismatch = 0;
            for (int i = 0; i < expected.Length && i < actual.Length; ++i, ++mismatch)
                if (expected[i] != actual[i])
                    break;

            const int offset = 5;
            int from = mismatch - offset;
            if (from < 0) from = 0;

            string s1 = SafeSubstring(expected, from, offset*3, out _);
            string s2 = SafeSubstring(actual, from, offset*3, out int inserted);
            int actualOffset = (mismatch - from) + inserted;

            throw new AssertFailedException(
                $"Expected: \"{s1}\"\n"+
                $"Actual:   \"{s2}\"\n"+
                $"           {new string('.', actualOffset)}^\n"+
                $"Strings mismatch at index {mismatch}\n"+
                $"Expected:<{expected}>. Actual:<{actual}>."
            );
        }

        /// <summary>
        /// Asserts that `actual` value is less than provided value
        /// e.g. Assert.That.LessThan(healthPercent, 0.5f);
        /// </summary>
        public static void LessThan<T>(this Assert assert, T actual, T lessThan, string message = "")
            where T : IComparable<T>
        {
            int difference = actual.CompareTo(lessThan);
            if (difference >= 0)
            {
                throw new AssertFailedException($"LessThan failed: {actual} < {lessThan} {message}");
            }
        }

        /// <summary>
        /// Asserts that `actual` value is greater than provided value
        /// e.g. Assert.That.GreaterThan(healthPercent, 0.5f);
        /// </summary>
        public static void GreaterThan<T>(this Assert assert, T actual, T greaterThan, string message = "")
            where T : IComparable<T>
        {
            int difference = actual.CompareTo(greaterThan);
            if (difference <= 0)
            {
                throw new AssertFailedException($"Greater Than failed: {actual} > {greaterThan} {message}");
            }
        }
    }
}
