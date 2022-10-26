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
        
    protected void DebugVisualize(ISpatial tree, SpatialObjectBase[] objects)
    {
        var vis = new SpatialVisualization(objects, tree, moveShips:false);
        EnableMockInput(false); // switch from mocked input to real input
        Game.ShowAndRun(screen: vis); // run the sim
        EnableMockInput(true); // restore the mock input
    }

    SpatialObjectBase[] GetSystems() => UState.Systems.Select(s => (SpatialObjectBase)s);
    SpatialObjectBase[] GetPlanets() => UState.Planets.Select(p => (SpatialObjectBase)p);
    SpatialObjectBase[] GetSystemsAndPlanets() => GetSystems().Concat(GetPlanets());

    [TestMethod]
    public void SearchForSolarSystems()
    {
        CreateCustomUniverseSandbox(numOpponents:5, GalSize.Epic);
        SpatialObjectBase[] systems = GetSystems();

        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:8, smallestCell:64_000);
        System.Array.ForEach(systems, tree.Insert);

        //var searchArea = new AABoundingBox2D(Vector2.Zero, UState.Size * 0.2f);
        //SearchOptions opt = new(searchArea);
        //for (int i = 0; i < 1_000_000; ++i)
        //    tree.Find(opt);
        //return;

        if (EnableVisualization)
            DebugVisualize(tree, systems);
    }

    [TestMethod]
    public void GenerateUniverseAndFindAllSystems()
    {
        CreateCustomUniverseSandbox(numOpponents:5, GalSize.Epic);
        SpatialObjectBase[] solarBodies = GetSystemsAndPlanets();

        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:16, smallestCell:16_000);
        System.Array.ForEach(solarBodies, tree.Insert);

        if (EnableVisualization)
            DebugVisualize(tree, solarBodies);
    }

    [TestMethod]
    public void GenerateUniverseAndFindAllSystems2()
    {
        CreateCustomUniverseSandbox(numOpponents:5, GalSize.Epic);
        SpatialObjectBase[] solarBodies = GetSystemsAndPlanets();

        var tree = new Qtree(UState.UniverseWidth, 16_000);
        tree.UpdateAll(solarBodies);
        tree.UpdateAll(solarBodies);
        tree.UpdateAll(solarBodies);

        if (EnableVisualization)
            DebugVisualize(tree, solarBodies);
    }
}
