using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Utils;

namespace UnitTests.Collections
{
    [TestClass]
    public class SafeArrayTests : SafeArrayTypesTestBase
    {
        public override IArray<T> New<T>() => new SafeArray<T>();
        public override IArray<T> New<T>(params T[] args) => new SafeArray<T>(args);
    }
}
