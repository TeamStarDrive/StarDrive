using System;
using System.Collections;
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
            {
                items[i++] = o;
            }
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

        public static void Equal(this Assert assert, ICollection expected, ICollection actual, string message = "")
        {
            if (expected == null)
            {
                if (actual == null) return;
                throw new AssertFailedException($"Expected null does not match Actual {actual}. {message}");
            }

            if (expected.Count != actual.Count)
                throw new AssertFailedException(
                    $"Expected.Length {expected.Count} does not match Actual.Length {actual.Count}. {message}");

            object[] e = ToArray(expected);
            object[] a = ToArray(actual);

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
    }
}
