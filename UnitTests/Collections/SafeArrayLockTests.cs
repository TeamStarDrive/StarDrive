using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Utils;

namespace UnitTests.Collections
{
    [TestClass]
    public class SafeArrayLockTests : SafeArrayTypesTestBase
    {
        public override IArray<T> New<T>() => new SafeArrayLock<T>();
        public override IArray<T> New<T>(params T[] args) => new SafeArrayLock<T>(args);
    }
}
