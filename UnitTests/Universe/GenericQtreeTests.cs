using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Spatial;
using SDUtils;

namespace UnitTests.Universe;

// NOTE: This tests GenericQtree only. For collision quadtree check TestNativeSpatial
[TestClass]
public class GenericQtreeTests : StarDriveTest
{
    protected static bool EnableVisualization = true;

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

    SpatialObjectBase[] GetSystemsAndPlanets()
    {
        var solarBodies = new Array<SpatialObjectBase>();
        foreach (SolarSystem s in UState.Systems)
        {
            solarBodies.Add(s);
            foreach (Planet p in s.PlanetList)
                solarBodies.Add(p);
        }
        return solarBodies.ToArr();
    }

    [TestMethod]
    public void SearchForSolarSystems()
    {
        CreateUniverseAndPlayerEmpire(universeRadius:5_000_000f);
        Planet playerHome = AddHomeWorldToEmpire(new(500_000, 750_000f), Player);
        Planet enemyHome = AddHomeWorldToEmpire(new(-500_000, -750_000f), Enemy);
        SpatialObjectBase[] solarBodies = GetSystemsAndPlanets();

        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:16, smallestCell:16_000);
        foreach (SpatialObjectBase so in solarBodies)
            tree.Insert(so);

        if (EnableVisualization)
            DebugVisualize(tree, solarBodies);
    }

    [TestMethod]
    public void GenerateUniverseAndFindAllSystems()
    {
        CreateCustomUniverseSandbox(numOpponents:5, GalSize.Epic);
        SpatialObjectBase[] solarBodies = GetSystemsAndPlanets();

        var tree = new GenericQtree(UState.UniverseWidth, cellThreshold:16, smallestCell:16_000);
        foreach (SpatialObjectBase so in solarBodies)
            tree.Insert(so);

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
