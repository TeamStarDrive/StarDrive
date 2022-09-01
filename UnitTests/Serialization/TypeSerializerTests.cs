using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Serialization;

namespace UnitTests.Serialization;

[TestClass]
public class TypeSerializerTests : StarDriveTest
{
    [StarDataType]
    class UserClass
    {
        [StarData] public string Name;
        [StarData] public int Id;
        [StarDataConstructor] public UserClass(string name, int id)
        {
            Name = name;
            Id = id;
        }
    }

    [TestMethod]
    public void CreateInstancePerf()
    {
        const int iterations = 100_000;
        var type = typeof(UserClass);
        var ser = new BinarySerializer(type);

        var t1 = new PerfTimer();
        object[] defaultParams = { null, null };
        for (int i = 0; i < iterations; i++)
            Activator.CreateInstance(type, defaultParams);
        var e1 = t1.ElapsedMillis;
        Log.Info($"Activator.CreateInstance elapsed: {e1:0}ms");

        var t2 = new PerfTimer();
        for (int i = 0; i < iterations; i++)
            ser.CreateInstance();
        var e2 = t2.ElapsedMillis;
        Log.Info($"CreateInstance elapsed: {e2:0}ms");
    }

    [StarDataType]
    struct UserStruct
    {
        [StarData] public string Name;
        [StarData] public int Id;
    }
    
    [TestMethod]
    public void CreateStructPerf()
    {
        const int iterations = 1_000_000;
        var type = typeof(UserStruct);
        var ser = new BinarySerializer(type);

        var t1 = new PerfTimer();
        for (int i = 0; i < iterations; i++)
            Activator.CreateInstance(type);
        var e1 = t1.ElapsedMillis;
        Log.Info($"Activator.CreateInstance elapsed: {e1:0}ms");

        var t2 = new PerfTimer();
        for (int i = 0; i < iterations; i++)
            ser.CreateInstance();
        var e2 = t2.ElapsedMillis;
        Log.Info($"CreateInstance elapsed: {e2:0}ms");
    }
}
