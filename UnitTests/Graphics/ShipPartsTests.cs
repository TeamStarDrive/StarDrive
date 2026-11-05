using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data.Mesh;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;

namespace UnitTests.Graphics;

/// <summary>
/// A model cut into parts names each object with its part number first; it loads as one mesh per material with its
/// parts in part order, and a ship's model draws only the parts it still shows.
/// </summary>
[TestClass]
public class ShipPartsTests : StarDriveTest
{
    [TestMethod]
    [DataRow("00_Core", 0)]
    [DataRow("03_MidLeft", 3)]
    [DataRow("7", 7)]
    [DataRow("31_Fin", 31)]
    [DataRow("32_Fin", -1)]
    [DataRow("Core", -1)]
    [DataRow("Mesh.021", -1)]
    [DataRow("", -1)]
    public void APartNumberStartsTheObjectName(string objectName, int part)
    {
        Assert.AreEqual(part, StaticMesh.PartIdOf(objectName));
    }

    [TestMethod]
    public void AModelCutIntoPartsLoadsAsOneMeshWithItsPartsInOrder()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/Ships/Terran/HeavyGunboat/ship10b.fbx");
        Assert.AreEqual(7, mesh.NumParts, "the core and six parts");
        Assert.AreEqual(1, mesh.RawMeshes.Count, "its one material draws at once");

        MeshData md = mesh.RawMeshes[0];
        CollectionAssert.AreEqual(new[] { 631, 68, 68, 61, 61, 128, 128 }, md.PartPrimitives, "each part's triangles");
        int start = 0;
        for (int part = 0; part < mesh.NumParts; ++part)
        {
            Assert.AreEqual(start, md.PartStart[part], $"part {part} follows the one before it");
            start += md.PartPrimitives[part] * 3;
        }
        Assert.AreEqual(md.PrimitiveCount * 3, start, "and the parts hold every triangle");
        for (int part = 0; part < mesh.NumParts; ++part)
            Assert.AreEqual(md.PartPrimitives[part] * 3, mesh.PartTriangles[part].Length, $"part {part}'s corners for its hull's part map");
    }

    [TestMethod]
    public void AModelInOnePieceHasNoParts()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/Ships/Terran/Battleship/Battleship.fbx");
        Assert.AreEqual(0, mesh.NumParts);
        Assert.IsNull(mesh.RawMeshes[0].PartStart);
        Assert.IsNull(mesh.CreateSceneObject().PartShown, "its model shows no parts");
    }

    [TestMethod]
    public void AModelCutIntoPartsStartsWithEveryPartShown()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/Ships/Terran/HeavyGunboat/ship10b.fbx");
        SceneObject so = mesh.CreateSceneObject();
        CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f }, so.PartShown);
    }

    // the model seen from above, as the game shows it: the number of pixels it covers
    int DrawnPixels(StaticMesh mesh, float[] shown)
    {
        var device = Game.GraphicsDevice;
        SceneObject so = mesh.CreateSceneObject();
        so.Visibility = SynapseGaming.LightingSystem.Core.ObjectVisibility.Rendered;
        so.PartShown = shown;
        using var scene = new SynapseGaming.LightingSystem.Core.SceneInterface(Game.Graphics);
        scene.LightManager.Submit(new SynapseGaming.LightingSystem.Lights.DirectionalLight
        {
            Direction = new(0.3f, -0.4f, -0.85f), DiffuseColor = Microsoft.Xna.Framework.Vector3.One, Intensity = 1f, Enabled = true,
        });
        scene.ObjectManager.Submit(so);
        var state = new SynapseGaming.LightingSystem.Core.SceneState();
        so.World = Microsoft.Xna.Framework.Matrix.CreateRotationX(Microsoft.Xna.Framework.MathHelper.Pi);
        var view = Microsoft.Xna.Framework.Matrix.CreateLookAt(new(0, 0, 500), Microsoft.Xna.Framework.Vector3.Zero, Microsoft.Xna.Framework.Vector3.Up);
        var projection = Microsoft.Xna.Framework.Matrix.CreateOrthographic(400, 400, 1, 1000);
        state.BeginFrameRendering(ref view, ref projection, 0f, null, false);
        scene.BeginFrameRendering(state);

        using var rt = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(device, 128, 128, false,
            Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color, Microsoft.Xna.Framework.Graphics.DepthFormat.Depth24);
        var previous = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(rt);
            device.Clear(Microsoft.Xna.Framework.Color.Transparent);
            scene.RenderScene();
        }
        finally
        {
            device.SetRenderTargets(previous);
        }
        var pixels = new Microsoft.Xna.Framework.Color[128 * 128];
        rt.GetData(pixels);
        int drawn = 0;
        foreach (var px in pixels)
            if (px.A > 0)
                ++drawn;
        return drawn;
    }

    [TestMethod]
    public void AShipModelLeavesOutThePartsItNoLongerShows()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/Ships/Terran/HeavyGunboat/ship10b.fbx");
        int whole = DrawnPixels(mesh, new[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f });
        int noBackLeft = DrawnPixels(mesh, new[] { 1f, 1f, 1f, 1f, 1f, 0f, 1f });
        int coreOnly = DrawnPixels(mesh, new[] { 1f, 0f, 0f, 0f, 0f, 0f, 0f });
        Assert.IsTrue(coreOnly > 0, "setup: the core draws");
        Assert.IsTrue(noBackLeft < whole, $"the lost back-left plate leaves its pixels: {noBackLeft} of {whole}");
        Assert.IsTrue(noBackLeft > coreOnly, $"and only those: {noBackLeft} against the core's {coreOnly}");
    }

    ShipHull LoadedGunboatHull()
    {
        Assert.IsTrue(Ship_Game.ResourceManager.Hull("Terran/HeavyGunboat", out ShipHull hull), "setup: the Heavy Gunboat hull");
        Assert.IsTrue(hull.LoadModel(out SceneObject _, Content), "setup: its model loads");
        return hull;
    }

    static string MapText(byte[,] map)
    {
        var text = new System.Text.StringBuilder();
        for (int y = 0; y < map.GetLength(1); ++y)
        {
            for (int x = 0; x < map.GetLength(0); ++x)
                text.Append(map[x, y]);
            text.AppendLine();
        }
        return text.ToString();
    }

    [TestMethod]
    public void EachPlateOfTheGunboatCoversTheCellsOnItsSideAndRow()
    {
        ShipHull hull = LoadedGunboatHull();
        byte[,] map = hull.PartMap;
        Assert.IsNotNull(map, "the model's parts make the hull's part map");
        Assert.AreEqual(7, hull.NumParts);
        System.Console.WriteLine(MapText(map));

        var rows = new System.Collections.Generic.Dictionary<int, (int Min, int Max)>();
        for (int y = 0; y < hull.Size.Y; ++y)
        for (int x = 0; x < hull.Size.X; ++x)
        {
            int part = map[x, y];
            if (part == 0)
                continue;
            bool left = part % 2 == 1; // 1 FrontLeft, 3 MidLeft, 5 BackLeft
            Assert.AreEqual(left, x < hull.GridCenter.X, $"part {part} at {x},{y} lies on its own side of the center line");
            rows[part] = rows.TryGetValue(part, out var r) ? (System.Math.Min(r.Min, y), System.Math.Max(r.Max, y)) : (y, y);
        }
        for (int part = 1; part < 7; ++part)
            Assert.IsTrue(rows.ContainsKey(part), $"part {part} covers some cell");
        Assert.IsTrue(rows[1].Max < rows[5].Min && rows[2].Max < rows[6].Min, "the front plates lie ahead of the back plates");
        Assert.IsTrue(rows[3].Min <= rows[5].Min && rows[3].Max >= rows[1].Min, "the middle plates lie between them");
    }

    [TestMethod]
    public void MovingTheModelOnTheGridMovesItsPartMap()
    {
        ShipHull hull = LoadedGunboatHull();
        byte[,] map = hull.PartMap;
        ShipHull moved = hull.GetClone(); // as the hull editor's mesh offset slider does
        moved.MeshOffset = new(hull.MeshOffset.X, hull.MeshOffset.Y + 32f);
        byte[,] movedMap = moved.PartMap;
        for (int y = 0; y + 2 < hull.Size.Y; ++y)
        for (int x = 0; x < hull.Size.X; ++x)
            Assert.AreEqual(map[x, y], ShipHull.PartAt(movedMap, new(x, y + 2)), $"the cell at {x},{y} two rows down");
        Assert.AreSame(map, hull.PartMap, "the original keeps its map");
    }

    [TestMethod]
    public void MovingTheGridCenterMovesThePartMap()
    {
        ShipHull hull = LoadedGunboatHull();
        byte[,] map = hull.PartMap;
        ShipHull moved = hull.GetClone(); // as the hull editor's slot painting does when the grid grows to the left
        moved.GridCenter = new(hull.GridCenter.X + 1, hull.GridCenter.Y);
        moved.Size = new(hull.Size.X + 1, hull.Size.Y);
        byte[,] movedMap = moved.PartMap;
        for (int y = 0; y < hull.Size.Y; ++y)
        for (int x = 0; x < hull.Size.X; ++x)
            Assert.AreEqual(map[x, y], ShipHull.PartAt(movedMap, new(x + 1, y)), $"the cell at {x},{y} one column right");
    }

    [TestMethod]
    public void ADesignGridOffsetFromItsHullFindsThePartsThroughBothCenters()
    {
        ShipHull hull = LoadedGunboatHull();
        byte[,] map = hull.PartMap;
        var shift = new SDGraphics.Point(1, 2); // the design's grid a column wider on the left and two rows taller on top
        var slots = new System.Collections.Generic.List<DesignSlot>();
        foreach (HullSlot slot in hull.HullSlots)
            slots.Add(new(new(slot.Pos.X + shift.X, slot.Pos.Y + shift.Y), "SteelArmorSmall", new(1, 1), 0, ModuleOrientation.Normal, null));
        var info = new ShipGridInfo
        {
            Size = new(hull.Size.X + shift.X, hull.Size.Y + shift.Y),
            Center = new(hull.GridCenter.X + shift.X, hull.GridCenter.Y + shift.Y),
            SurfaceArea = slots.Count,
        };
        var grid = new ModuleGridFlyweight("test", info, slots.ToArray(), hull);

        Assert.IsTrue(grid.GetParts(out byte[] partOfModule, out int[] _));
        for (int i = 0; i < slots.Count; ++i)
            Assert.AreEqual(map[hull.HullSlots[i].Pos.X, hull.HullSlots[i].Pos.Y], partOfModule[i], $"the module on hull slot {hull.HullSlots[i].Pos}");
    }

    [TestMethod]
    public void EachModuleTakesThePartItSitsUnder()
    {
        ShipHull hull = LoadedGunboatHull();
        byte[,] map = hull.PartMap;
        var slots = new System.Collections.Generic.List<DesignSlot>();
        foreach (HullSlot slot in hull.HullSlots)
            slots.Add(new(slot.Pos, "SteelArmorSmall", new(1, 1), 0, ModuleOrientation.Normal, null));
        var info = new ShipGridInfo { Size = hull.Size, Center = hull.GridCenter, SurfaceArea = slots.Count };
        var grid = new ModuleGridFlyweight("test", info, slots.ToArray(), hull);

        Assert.IsTrue(grid.GetParts(out byte[] partOfModule, out int[] partArea));
        var area = new int[hull.NumParts];
        for (int i = 0; i < slots.Count; ++i)
        {
            int part = map[slots[i].Pos.X, slots[i].Pos.Y];
            Assert.AreEqual(part, partOfModule[i], $"the module at {slots[i].Pos}");
            ++area[part];
        }
        CollectionAssert.AreEqual(area, partArea, "each part's slot area");
    }

    [TestMethod]
    public void ABiggerModuleTakesThePartUnderMostOfItsCells()
    {
        ShipHull hull = LoadedGunboatHull();
        byte[,] map = hull.PartMap;
        // a 2x2 module wherever its four cells hold a plate and something else
        for (int y = 0; y + 1 < hull.Size.Y; ++y)
        for (int x = 0; x + 1 < hull.Size.X; ++x)
        {
            var cover = new int[hull.NumParts];
            ++cover[map[x, y]]; ++cover[map[x + 1, y]]; ++cover[map[x, y + 1]]; ++cover[map[x + 1, y + 1]];
            var slot = new DesignSlot(new(x, y), "SteelArmorMed", new(2, 2), 0, ModuleOrientation.Normal, null);
            var info = new ShipGridInfo { Size = hull.Size, Center = hull.GridCenter, SurfaceArea = 4 };
            var grid = new ModuleGridFlyweight("test", info, new[] { slot }, hull);
            Assert.IsTrue(grid.GetParts(out byte[] partOfModule, out int[] partArea));
            int expected = ShipHull.MostCovered(cover);
            Assert.AreEqual(expected, partOfModule[0], $"the 2x2 module at {x},{y}");
            Assert.AreEqual(4, partArea[expected], "its whole area counts for that part");
        }
    }

    [TestMethod]
    [DataRow(new[] { 3, 1 }, 0, DisplayName = "the core under most of it")]
    [DataRow(new[] { 2, 2 }, 1, DisplayName = "a tie goes to the part over the core")]
    [DataRow(new[] { 0, 1, 1 }, 1, DisplayName = "a tie between parts goes to the lower number")]
    [DataRow(new[] { 1, 0, 2 }, 2)]
    [DataRow(new[] { 0, 0, 0 }, 0, DisplayName = "nothing under it is the core")]
    public void ThePartCoveringMostWins(int[] cover, int part)
    {
        Assert.AreEqual(part, ShipHull.MostCovered(cover));
    }

    [TestMethod]
    public void NoModuleHasAPartBeforeTheModelLoads()
    {
        var hull = new ShipHull { HullSlots = new[] { new HullSlot(0, 0, Restrictions.IO) } };
        var slots = new[] { new DesignSlot(new(0, 0), "SteelArmorSmall", new(1, 1), 0, ModuleOrientation.Normal, null) };
        var grid = new ModuleGridFlyweight("test", new ShipGridInfo { Size = new(1, 1), SurfaceArea = 1 }, slots, hull);
        Assert.IsFalse(grid.GetParts(out byte[] partOfModule, out int[] _));
        Assert.IsNull(partOfModule);
    }

    static RenderableMesh PartsMesh(int[] primitives)
    {
        var start = new int[primitives.Length];
        int total = 0;
        for (int part = 0; part < primitives.Length; ++part)
        {
            start[part] = total * 3;
            total += primitives[part];
        }
        return new(null, null, default, default, null, null, null, 0, Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList,
                   total, 0, 0, 0, 0) { PartStart = start, PartPrimitives = primitives };
    }

    static string Runs(RenderableMesh rm, float[] shown)
    {
        var runs = new System.Collections.Generic.List<string>();
        for (int part = 0; rm.NextShownRun(shown, ref part, out int start, out int primitives);)
            runs.Add($"{start}+{primitives}");
        return string.Join(" ", runs);
    }

    [TestMethod]
    [DataRow(new[] { 1f, 1f, 1f, 1f }, "0+10")]
    [DataRow(new[] { 1f, 1f, 0f, 1f }, "0+5 21+3")]
    [DataRow(new[] { 0f, 1f, 1f, 1f }, "12+6")]
    [DataRow(new[] { 1f, 1f, 1f, 0f }, "0+7")]
    [DataRow(new[] { 1f, 0f, 1f, 0f }, "0+4 15+2")]
    [DataRow(new[] { 0f, 0f, 0f, 0f }, "")]
    [DataRow(new[] { 1f, 0.5f, 1f, 1f }, "0+4 15+5", DisplayName = "a part on its way back is not drawn whole")]
    public void AModelDrawsTheRunsOfItsShownParts(float[] shown, string runs)
    {
        Assert.AreEqual(runs, Runs(PartsMesh(new[] { 4, 1, 2, 3 }), shown));
    }

    [TestMethod]
    public void APartWithNoTrianglesDoesNotSplitARun()
    {
        Assert.AreEqual("0+7", Runs(PartsMesh(new[] { 4, 0, 3 }), new[] { 1f, 0f, 1f }));
    }

    [TestMethod]
    public void AModelInOnePieceIsOneRun()
    {
        var rm = new RenderableMesh(null, null, default, default, null, null, null, 0,
                                    Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, 12, 0, 0, 30, 0);
        Assert.AreEqual("30+12", Runs(rm, null));
        Assert.AreEqual("30+12", Runs(rm, new[] { 0f }), "shown levels mean nothing to a model with no parts");
    }
}
