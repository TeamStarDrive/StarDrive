using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Spatial;
using SDUtils;
using SDGraphics;
using Ship_Game.Utils;

namespace UnitTests.Universe;

// NOTE: This tests GenericQtree only. For collision quadtree check TestNativeSpatial
[TestClass]
public class GenericQtreeTests : StarDriveTest
{
    protected static bool EnableVisualization = false;

    public GenericQtreeTests()
    {
    }

    protected void DebugVisualize(GenericQtree tree, SpatialObjectBase[] objects)
    {
        var vis = new GenericQtreeVisualization(objects, tree);
        EnableMockInput(false); // switch from mocked input to real input
        Game.ShowAndRun(screen: vis); // run the sim
        EnableMockInput(true); // restore the mock input
    }

    SolarSystem[] GetSystems() => UState.Systems.ToArr();
    Planet[] GetPlanets() => UState.Planets.ToArr();
    SpatialObjectBase[] GetSystemsAndPlanets() => GetSystems().Concat<SpatialObjectBase>(GetPlanets());

    [TestMethod]
    public void Insert()
    {
        CreateUniverseAndPlayerEmpire();
        var tree = new GenericQtree(UState.UniverseWidth);

        float r = UState.UniverseRadius;
        Planet NW = AddDummyPlanet(new(-r,-r));
        Planet NE = AddDummyPlanet(new(+r,-r));
        Planet SE = AddDummyPlanet(new(+r,+r));
        Planet SW = AddDummyPlanet(new(-r,+r));

        tree.Insert(NW);
        Assert.AreEqual(1, tree.Count);
        Assert.IsTrue(tree.Contains(NW));
        Assert.IsFalse(tree.Contains(NE));
        Assert.AreEqual(NW, tree.FindOne(NW.Position, 1000));

        tree.Insert(NW); // double-insert
        Assert.AreEqual(1, tree.Count); // nothing should happen

        tree.Insert(NE);
        Assert.AreEqual(2, tree.Count);
        Assert.IsTrue(tree.Contains(NE));
        Assert.AreEqual(NE, tree.FindOne(NE.Position, 1000));
        
        tree.Insert(SE);
        Assert.AreEqual(3, tree.Count);
        Assert.IsTrue(tree.Contains(SE));
        Assert.AreEqual(SE, tree.FindOne(SE.Position, 1000));

        tree.Insert(SW);
        Assert.AreEqual(4, tree.Count);
        Assert.IsTrue(tree.Contains(SW));
        Assert.AreEqual(SW, tree.FindOne(SW.Position, 1000));
    }

    [TestMethod]
    public void Remove()
    {
        CreateUniverseAndPlayerEmpire();
        var tree = new GenericQtree(UState.UniverseWidth);

        float r = UState.UniverseRadius;
        Planet NW = AddDummyPlanet(new(-r,-r));
        Planet NE = AddDummyPlanet(new(+r,-r));
        Planet SE = AddDummyPlanet(new(+r,+r));
        Planet SW = AddDummyPlanet(new(-r,+r));
        tree.Insert(NW); tree.Insert(NE); tree.Insert(SE); tree.Insert(SW);
        Assert.AreEqual(4, tree.Count);

        tree.Remove(NW);
        Assert.AreEqual(3, tree.Count);
        Assert.IsFalse(tree.Contains(NW));
        Assert.IsTrue(tree.Contains(NE));
        Assert.AreEqual(null, tree.FindOne(NW.Position, 1000));

        tree.Remove(NE);
        Assert.AreEqual(2, tree.Count);
        Assert.IsFalse(tree.Contains(NE));
        Assert.AreEqual(null, tree.FindOne(NE.Position, 1000));

        tree.Remove(SE);
        Assert.AreEqual(1, tree.Count);
        Assert.IsFalse(tree.Contains(SE));
        Assert.AreEqual(null, tree.FindOne(SE.Position, 1000));

        tree.Remove(SW);
        Assert.AreEqual(0, tree.Count);
        Assert.IsFalse(tree.Contains(SW));
        Assert.AreEqual(null, tree.FindOne(SW.Position, 1000));

        // make sure it's completely empty
        Assert.AreEqual(null, tree.FindOne(Vector2.Zero, UState.UniverseRadius));
    }
    
    [TestMethod]
    public void Update()
    {
        CreateUniverseAndPlayerEmpire();
        var tree = new GenericQtree(UState.UniverseWidth);

        float r = UState.UniverseRadius;
        Vector2 NW = new(-r,-r);
        Vector2 NE = new(+r,-r);
        Vector2 SE = new(+r,+r);
        Vector2 SW = new(-r,+r);
        Planet p = AddDummyPlanet(NW);

        p.Position = NW;
        tree.Insert(p);
        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual(p, tree.FindOne(NW, 1000));

        p.Position = NE;
        tree.Update(p);
        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual(p, tree.FindOne(NE, 1000));

        p.Position = SE;
        tree.Update(p);
        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual(p, tree.FindOne(SE, 1000));
        
        p.Position = SW;
        tree.Update(p);
        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual(p, tree.FindOne(SW, 1000));
    }

    [TestMethod]
    public void InsertOrUpdateCrowdedTree()
    {
        CreateUniverseAndPlayerEmpire();
        var tree = new GenericQtree(UState.UniverseWidth);

        var rand = new SeededRandom(1337);
        float crowdRadius = 100_000;
        for (int i = 0; i < 100; ++i)
        {
            Planet crowd = AddDummyPlanet(rand.Vector2D(crowdRadius));
            tree.Insert(crowd);
        }

        Vector2[] path =
        {
            new(-50_000, -50_000),
            new(+50_000, -50_000),
            new(0, 0),
            new(+50_000, +50_000),
            new(-50_000, +50_000),
        };

        Planet p = AddDummyPlanet(new(1,1));

        foreach (Vector2 pos in path)
        {
            p.Position = pos;
            tree.InsertOrUpdate(p);
            var results = tree.Find(pos, 1000);
            Assert.IsTrue(results.Contains(p));
        }
    }

    void DoPerfTest(GenericQtree tree)
    {
        var t1 = new PerfTimer();
        var searchArea = new AABoundingBox2D(Vector2.Zero, UState.Size * 0.2f);
        SearchOptions opt = new(searchArea);
        for (int i = 0; i < 1_000_000; ++i)
            tree.Find(opt);
        float elapsed = t1.ElapsedMillis;
        Console.WriteLine($"Elapsed: {elapsed:0.00}ms");
    }

    [TestMethod]
    public void SearchForSolarSystems()
    {
        CreateCustomUniverseSandbox(numOpponents:5, GalSize.Epic);
        SolarSystem[] systems = GetSystems();

        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:8, smallestCell:32_000);
        System.Array.ForEach(systems, tree.Insert);

        foreach (SolarSystem s in systems)
        {
            SpatialObjectBase found = tree.FindOne(s.Position, 1_000);
            Assert.AreEqual(s, found, "Must find the same system");
        }

        //DoPerfTest(tree);

        if (EnableVisualization)
            DebugVisualize(tree, systems);
    }

    [TestMethod]
    public void SearchForPlanets()
    {
        CreateCustomUniverseSandbox(numOpponents:5, GalSize.Epic);
        Planet[] planets = GetPlanets();

        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:8, smallestCell:32_000);
        System.Array.ForEach(planets, tree.Insert);

        foreach (Planet p in planets)
        {
            SpatialObjectBase found = tree.FindOne(p.Position, 1_000);
            Assert.AreEqual(p, found, "Must find the same planet");
        }

        if (EnableVisualization)
            DebugVisualize(tree, planets);
    }

    [TestMethod]
    public void SearchForEverythingInsideASystem()
    {
        CreateCustomUniverseSandbox(numOpponents:5, GalSize.Epic);
        SpatialObjectBase[] solarBodies = GetSystemsAndPlanets();
        SolarSystem[] systems = GetSystems();

        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:16, smallestCell:16_000);
        System.Array.ForEach(solarBodies, tree.Insert);
        
        foreach (SolarSystem s in systems)
        {
            SpatialObjectBase[] found = tree.Find(s.Position, s.Radius);

            Assert.IsTrue(found.Contains(s));
            foreach (Planet p in s.PlanetList)
                Assert.IsTrue(found.Contains(p));
            Assert.AreEqual(s.PlanetList.Count + 1, found.Length);
        }

        if (EnableVisualization)
            DebugVisualize(tree, solarBodies);
    }
}
