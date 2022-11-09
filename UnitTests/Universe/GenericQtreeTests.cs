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
        var vis = new GenericQtreeVisualization(this, objects, tree);
        EnableMockInput(false); // switch from mocked input to real input
        Game.ShowAndRun(screen: vis); // run the sim
        EnableMockInput(true); // restore the mock input
    }

    SolarSystem[] GetSystems() => UState.Systems.ToArr();
    Planet[] GetPlanets() => UState.Planets.ToArr();
    SpatialObjectBase[] GetSystemsAndPlanets() => GetSystems().Concat<SpatialObjectBase>(GetPlanets());

    Planet SpawnPlanetAt(Vector2 pos)
    {
        Planet p = AddDummyPlanet(pos);
        p.Position = pos;
        p.Radius = 50000;
        return p;
    }

    [TestMethod]
    public void Insert()
    {
        CreateUniverseAndPlayerEmpire();
        // set node threshold to 2 so we can actually test
        // the subdivision logic somewhat
        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:2);
        AssertGreaterThan(tree.FullSize, tree.WorldSize);

        float r4 = UState.UniverseRadius / 2;
        Planet NW = SpawnPlanetAt(new(-r4, -r4));
        Planet NE = SpawnPlanetAt(new(+r4, -r4));
        Planet SE = SpawnPlanetAt(new(+r4, +r4));
        Planet SW = SpawnPlanetAt(new(-r4, +r4));

        tree.Insert(NW);
        AssertEqual(1, tree.Count);
        Assert.IsTrue(tree.Contains(NW));
        Assert.IsFalse(tree.Contains(NE));
        AssertEqual(NW, tree.FindOne(NW.Position, 1000));
        AssertEqual(1, tree.CountNumberOfNodes());

        tree.Insert(NW); // double-insert
        AssertEqual(1, tree.Count); // nothing should happen

        tree.Insert(NE);
        AssertEqual(2, tree.Count);
        Assert.IsTrue(tree.Contains(NE));
        AssertEqual(NE, tree.FindOne(NE.Position, 1000));
        AssertEqual(1, tree.CountNumberOfNodes());

        tree.Insert(SE);
        AssertEqual(3, tree.Count);
        Assert.IsTrue(tree.Contains(SE));
        AssertEqual(SE, tree.FindOne(SE.Position, 1000));
        // now qtree should subdivide, because cellThreshold:2
        // subdivision always adds +4 nodes
        AssertEqual(1+4, tree.CountNumberOfNodes());

        tree.Insert(SW);
        AssertEqual(4, tree.Count);
        Assert.IsTrue(tree.Contains(SW));
        AssertEqual(SW, tree.FindOne(SW.Position, 1000));
        AssertEqual(1+4, tree.CountNumberOfNodes());
    }

    // remove will also test the # of nodes after removal
    [TestMethod]
    public void Remove()
    {
        CreateUniverseAndPlayerEmpire();
        // set cell threshold to 1, so we can actually test cell node behavior
        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:1);

        float r2 = UState.UniverseRadius / 2;
        Vector2 NW = new(-r2, -r2);
        Vector2 NE = new(+r2, -r2);
        Vector2 SE = new(+r2, +r2);
        Vector2 SW = new(-r2, +r2);
        float r4 = r2 / 2;

        // add 4 planets at each cardinal quadrant
        Array<Planet> InsertPlanets(Vector2 center)
        {
            Array<Planet> planets = new();
            planets.Add(SpawnPlanetAt(center + new Vector2(-r4, -r4)));
            planets.Add(SpawnPlanetAt(center + new Vector2(+r4, -r4)));
            planets.Add(SpawnPlanetAt(center + new Vector2(+r4, +r4)));
            planets.Add(SpawnPlanetAt(center + new Vector2(-r4, +r4)));
            foreach (Planet p in planets) tree.Insert(p);
            return planets;
        }
        void CanFind(Planet p) => Assert.IsTrue(tree.Find(p.Position, 1).Contains(p));
        void CannotFind(Planet p) => Assert.IsFalse(tree.Find(p.Position, 1).Contains(p));

        Array<Planet> NWPlanets = InsertPlanets(NW);
        Array<Planet> NEPlanets = InsertPlanets(NE);
        Array<Planet> SEPlanets = InsertPlanets(SE);
        Array<Planet> SWPlanets = InsertPlanets(SW);

        // now we should have 4 cardinal directions filled
        // with each cardinal quadrant holding 4 nodes:
        AssertEqual(16, tree.Count);
        AssertEqual(1 + 1*4 + 4*4, tree.CountNumberOfNodes());

        // Remove all NW nodes
        NWPlanets.ForEach(CanFind);
        NWPlanets.ForEach(p => Assert.IsTrue(tree.Contains(p)));
        NWPlanets.ForEach(p => tree.Remove(p));
        AssertEqual(12, tree.Count);
        AssertEqual(1 + 1*4 + 3*4, tree.CountNumberOfNodes());
        NWPlanets.ForEach(p => Assert.IsFalse(tree.Contains(p)));
        NWPlanets.ForEach(CannotFind);

        // Remove all NE nodes
        NEPlanets.ForEach(CanFind);
        NEPlanets.ForEach(p => Assert.IsTrue(tree.Contains(p)));
        NEPlanets.ForEach(p => tree.Remove(p));
        AssertEqual(8, tree.Count);
        AssertEqual(1 + 1*4 + 2*4, tree.CountNumberOfNodes());
        NEPlanets.ForEach(p => Assert.IsFalse(tree.Contains(p)));
        NEPlanets.ForEach(CannotFind);

        // Remove all SE nodes
        SEPlanets.ForEach(CanFind);
        SEPlanets.ForEach(p => Assert.IsTrue(tree.Contains(p)));
        SEPlanets.ForEach(p => tree.Remove(p));
        AssertEqual(4, tree.Count);
        AssertEqual(1 + 1*4 + 1*4, tree.CountNumberOfNodes());
        SEPlanets.ForEach(p => Assert.IsFalse(tree.Contains(p)));
        SEPlanets.ForEach(CannotFind);

        // Remove all SW nodes
        SWPlanets.ForEach(CanFind);
        SWPlanets.ForEach(p => Assert.IsTrue(tree.Contains(p)));
        SWPlanets.ForEach(p => tree.Remove(p));
        AssertEqual(0, tree.Count);
        AssertEqual(1 + 0*4 + 0*4, tree.CountNumberOfNodes());
        SWPlanets.ForEach(p => Assert.IsFalse(tree.Contains(p)));
        SWPlanets.ForEach(CannotFind);

        // make sure it's completely empty and no ghosts are returned
        AssertEqual(null, tree.FindOne(Vector2.Zero, UState.UniverseRadius));
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
        AssertEqual(1, tree.Count);
        AssertEqual(p, tree.FindOne(NW, 1000));

        p.Position = NE;
        tree.Update(p);
        AssertEqual(1, tree.Count);
        AssertEqual(p, tree.FindOne(NE, 1000));

        p.Position = SE;
        tree.Update(p);
        AssertEqual(1, tree.Count);
        AssertEqual(p, tree.FindOne(SE, 1000));
        
        p.Position = SW;
        tree.Update(p);
        AssertEqual(1, tree.Count);
        AssertEqual(p, tree.FindOne(SW, 1000));
    }

    [TestMethod]
    public void InsertOrUpdateCrowdedTree()
    {
        CreateUniverseAndPlayerEmpire();
        // set a low cell threshold to ensure lots of subdivisions
        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:4);

        var rand = new SeededRandom(1337);
        float crowdRadius = 100_000;
        for (int i = 0; i < 100; ++i)
        {
            Planet crowd = SpawnPlanetAt(rand.Vector2D(crowdRadius));
            crowd.Radius = 1000;
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

        Planet p = SpawnPlanetAt(new(1,1));
        p.Radius = 1000;

        foreach (Vector2 pos in path)
        {
            p.Position = pos;
            tree.InsertOrUpdate(p);
            var results = tree.Find(pos, 1);
            Assert.IsTrue(results.Contains(p),
                "Finding inside of updated bounds should match");

            var outsideResults = tree.Find(pos+new Vector2(1001), 1);
            Assert.IsFalse(outsideResults.Contains(p),
                "Finding outside of updated bounds should not match");
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
        Array.ForEach(systems, tree.Insert);

        foreach (SolarSystem s in systems)
        {
            SpatialObjectBase found = tree.FindOne(s.Position, 1_000);
            AssertEqual(s, found, "Must find the same system");
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
        Array.ForEach(planets, tree.Insert);

        foreach (Planet p in planets)
        {
            SpatialObjectBase found = tree.FindOne(p.Position, 1_000);
            AssertEqual(p, found, "Must find the same planet");
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
        Array.ForEach(solarBodies, tree.Insert);
        
        foreach (SolarSystem s in systems)
        {
            SpatialObjectBase[] found = tree.Find(s.Position, s.Radius);

            Assert.IsTrue(found.Contains(s));
            foreach (Planet p in s.PlanetList)
                Assert.IsTrue(found.Contains(p));
            AssertEqual(s.PlanetList.Count + 1, found.Length);
        }

        if (EnableVisualization)
            DebugVisualize(tree, solarBodies);
    }
}
