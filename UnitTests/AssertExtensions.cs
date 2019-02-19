using System;
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
    }
}
