using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;

namespace UnitTests
{
    public static class AssertExtensions
    {
        public static void Equal(this Assert assert, float tolerance, in Vector2 expected, in Vector2 actual)
        {
            if (expected.X.AlmostEqual(actual.X, tolerance) &&
                expected.Y.AlmostEqual(actual.Y, tolerance))
            {
                return; // OK
            }
            throw new AssertFailedException($"Expected {expected} does not match Actual {actual}");
        }

        public static void Equal(this Assert assert, object expected, object actual)
        {
            if (expected == null)
            {
                if (actual == null) return;
                throw new AssertFailedException($"Expected null does not match Actual {actual}");
            }

            if (expected is ICollection expectedCollection)
            {
                if (actual is ICollection actualCollection)
                {
                    Equal(assert, expectedCollection, actualCollection);
                }
                else
                {
                    throw new AssertFailedException($"Expected {expected} - collection but got Actual {actual}");
                }
            }
            else
            {
                Assert.AreEqual(expected, actual);
            }
        }

        static object[] ToArray(ICollection c)
        {
            var items = new object[c.Count];
            c.CopyTo(items, 0);
            return items;
        }

        public static void Equal(this Assert assert, ICollection expected, ICollection actual)
        {
            if (expected == null)
            {
                if (actual == null) return;
                throw new AssertFailedException($"Expected null does not match Actual {actual}");
            }

            if (expected.Count != actual.Count)
                throw new AssertFailedException(
                    $"Expected.Length {expected.Count} does not match Actual.Length {actual.Count}");

            object[] e = ToArray(expected);
            object[] a = ToArray(actual);

            for (int i = 0; i < e.Length; ++i)
            {
                assert.Equal(e[i], a[i]);
            }
        }
    }
}
