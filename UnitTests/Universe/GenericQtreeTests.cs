using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Spatial;
using SDUtils;
using SDGraphics;

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

    SpatialObjectBase[] GetSystems() => UState.Systems.Select(s => (SpatialObjectBase)s);
    SpatialObjectBase[] GetPlanets() => UState.Planets.Select(p => (SpatialObjectBase)p);
    SpatialObjectBase[] GetSystemsAndPlanets() => GetSystems().Concat(GetPlanets());

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
        SpatialObjectBase[] systems = GetSystems();

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
        SpatialObjectBase[] planets = GetPlanets();

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
        SpatialObjectBase[] systems = GetSystems();

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
