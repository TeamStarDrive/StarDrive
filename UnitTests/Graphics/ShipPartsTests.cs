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
    }

    [TestMethod]
    public void AModelCutIntoPartsStartsDrawnWhole()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/Ships/Terran/HeavyGunboat/ship10b.fbx");
        Assert.IsNull(mesh.CreateSceneObject().PartShown);
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

        Assert.IsTrue(grid.GetParts(out int[] _));
        for (int i = 0; i < slots.Count; ++i)
            Assert.AreEqual(map[hull.HullSlots[i].Pos.X, hull.HullSlots[i].Pos.Y], grid.PartOf(slots[i].Pos), $"the module on hull slot {hull.HullSlots[i].Pos}");
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

        Assert.IsTrue(grid.GetParts(out int[] partArea));
        var area = new int[hull.NumParts];
        for (int i = 0; i < slots.Count; ++i)
        {
            int part = map[slots[i].Pos.X, slots[i].Pos.Y];
            Assert.AreEqual(part, grid.PartOf(slots[i].Pos), $"the module at {slots[i].Pos}");
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
            Assert.IsTrue(grid.GetParts(out int[] partArea));
            int expected = ShipHull.MostCovered(cover);
            Assert.AreEqual(expected, grid.PartOf(new(x, y)), $"the 2x2 module at {x},{y}");
            Assert.AreEqual(expected, grid.PartOf(new(x + 1, y + 1)), $"the 2x2 module at {x},{y}, from its far cell");
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
        Assert.IsFalse(grid.GetParts(out int[] partArea));
        Assert.IsNull(partArea);
        Assert.AreEqual(-1, grid.PartOf(new(0, 0)), "and its module no part");
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

    const string GunboatDesign = "TEST_Spearhead mk1-a";

    // a universe whose player can spawn ships on the Heavy Gunboat hull; with sure rolls every chance above none comes up for the player
    void SetupGunboats(bool sureRolls = true)
    {
        CreateUniverseAndPlayerEmpire();
        LoadStarterShips(GunboatDesign);
        if (sureRolls)
            SetDice(Player, SureDice());
    }

    static void SetDice(Empire empire, Ship_Game.Utils.RandomBase dice)
    {
        typeof(Empire).GetField(nameof(Empire.Random))!.SetValue(empire, dice);
    }

    // a roll of any chance above none succeeds, or none ever does
    static FixedDice SureDice() => new(0.0);
    static FixedDice NeverDice() => new(0.999999);

    // every draw the same, counted
    sealed class FixedDice : Ship_Game.Utils.RandomBase
    {
        readonly Draws Source;
        public FixedDice(double draw) : base(1) => Source = new(draw);
        public int Count => Source.Count;
        protected override System.Random Rand => Source;

        sealed class Draws(double draw) : System.Random
        {
            public int Count;
            public override double NextDouble() { ++Count; return draw; }
            protected override double Sample() => draw;
            public override int Next() => 0;
            public override int Next(int maxValue) => (int)(draw * maxValue);
            public override int Next(int minValue, int maxValue) => minValue + (int)(draw * (maxValue - minValue));
        }
    }

    [TestCleanup]
    public override void Cleanup()
    {
        if (Universe != null)
        {
            Universe.Particles?.Dispose();
            Universe.Particles = null;
        }
        base.Cleanup();
    }

    // away from the camera
    Ship SpawnGunboat(float x = 200_000f) => SpawnGunboatAt(new(x, 200_000f));

    // its hull's model loaded, so the hull knows its parts
    Ship SpawnGunboatAt(SDGraphics.Vector2 at)
    {
        Ship ship = SpawnShip(GunboatDesign, Player, at);
        Assert.IsTrue(ship.BaseHull.LoadModel(out SceneObject _, Content), "setup: its hull's model loads");
        Assert.IsTrue(ship.Grid.GetParts(out int[] _), "setup: its hull knows its parts");
        return ship;
    }

    // a part's modules, smallest first, and its slot area
    static ShipModule[] ModulesOf(Ship ship, int part, out int area)
    {
        ship.Grid.GetParts(out int[] partArea);
        area = partArea[part];
        var modules = new System.Collections.Generic.List<ShipModule>();
        int moduleArea = 0;
        foreach (ShipModule m in ship.Modules)
        {
            if (ship.Grid.PartOf(m.Pos) == part)
            {
                modules.Add(m);
                moduleArea += m.Area;
            }
        }
        modules.Sort((a, b) => a.Area.CompareTo(b.Area));
        Assert.IsTrue(modules.Count > 1, $"setup: modules under part {part}");
        Assert.AreEqual(area, moduleArea, $"setup: part {part}'s slot area is its modules'");
        return modules.ToArray();
    }

    static void Kill(ShipModule m) => m.SetHealth(0f, "Test");
    static void Repair(ShipModule m) => m.SetHealth(m.ActualMaxHealth, "Test");

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    public void APlateCanGoOnlyOnceMoreThanThreeQuartersOfItIsDead(int plate)
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        ShipModule[] modules = ModulesOf(ship, plate, out int area);
        Assert.IsNull(ship.PartShownNow(), "a ship with no damage has every part on");

        int dead = 0;
        foreach (ShipModule m in modules)
        {
            Kill(m);
            dead += m.Area;
            float[] shown = ship.PartShownNow();
            if (dead <= 0.75f * area)
            {
                Assert.IsNull(shown, $"{dead} of {area} dead, the plate stays on whatever the roll");
                continue;
            }
            Assert.IsNotNull(shown, $"{dead} of {area} dead, a sure roll loses the plate");
            for (int part = 0; part < shown.Length; ++part)
                Assert.AreEqual(part == plate ? 0f : 1f, shown[part], $"{dead} of {area} dead: part {part}");
        }
        Assert.IsTrue(ship.Active, "setup: the ship lives through it");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    public void ADeathPastThreeQuartersLosesThePlateByTheShareDeadPastIt(int plate)
    {
        SetupGunboats(sureRolls: false);
        Ship ship = SpawnGunboat();
        ShipModule[] modules = ModulesOf(ship, plate, out int area);
        const int tries = 4000;

        int before = 0, killed = 0;
        while (before + modules[killed].Area <= 0.75f * area)
            before += modules[killed++].Area;
        float past = (before + modules[killed].Area) / (float)area - 0.75f;
        AssertLossRate(past, LossRate(ship, System.MemoryExtensions.AsSpan(modules, 0, killed).ToArray(), modules[killed], tries), tries,
                       $"the death taking the plate to {before + modules[killed].Area} of {area} dead");

        AssertLossRate(0.25f, LossRate(ship, System.MemoryExtensions.AsSpan(modules, 1).ToArray(), modules[0], tries), tries,
                       "the death of its last module");
    }

    // how often the last module's death loses the plate, the others dead: real dice for that roll, none that succeed between them
    static float LossRate(Ship ship, ShipModule[] others, ShipModule last, int tries)
    {
        Empire owner = ship.Loyalty;
        Ship_Game.Utils.RandomBase dice = owner.Random;
        SetDice(owner, NeverDice());
        foreach (ShipModule m in others)
            Kill(m);
        Assert.IsNull(ship.PartShownNow(), "setup: the others dead, the plate is on");

        int lost = 0;
        for (int t = 0; t < tries; ++t)
        {
            SetDice(owner, dice);
            Kill(last);
            bool gone = ship.PartShownNow() != null;
            SetDice(owner, NeverDice());
            Repair(last);
            if (gone)
            {
                ++lost;
                foreach (ShipModule m in others)
                    Repair(m);
                Assert.IsNull(ship.PartShownNow(), "setup: repaired whole");
                foreach (ShipModule m in others)
                    Kill(m);
            }
            ship.PartShownNow();
        }

        foreach (ShipModule m in others)
            Repair(m);
        Assert.IsNull(ship.PartShownNow(), "setup: whole again after the tries");
        SetDice(owner, dice);
        return lost / (float)tries;
    }

    static void AssertLossRate(float expected, float rate, int tries, string what)
    {
        float tolerance = 5f * (float)System.Math.Sqrt(expected * (1f - expected) / tries) + 0.003f;
        Assert.IsTrue(expected > tolerance, $"setup: {what} has a chance told from none");
        AssertEqual(tolerance, expected, rate, $"{what}: lost on {expected:P1} of the deaths");
    }

    [TestMethod]
    public void DeathsUnderAPlateInOneUpdateRollOnce()
    {
        SetupGunboats(sureRolls: false);
        Ship ship = SpawnGunboat();
        ShipModule[] modules = ModulesOf(ship, 1, out int area);
        int together = modules.Length - 3; // its three biggest die in one update
        int dead = 0;
        for (int i = 0; i < together && dead + modules[i].Area <= 0.75f * area; ++i)
        {
            dead += modules[i].Area;
            Kill(modules[i]);
        }
        Assert.IsNull(ship.PartShownNow(), "setup: three quarters dead or less, the plate is on");

        FixedDice dice = NeverDice();
        SetDice(Player, dice);
        for (int i = together; i < modules.Length; ++i)
        {
            Kill(modules[i]);
            dead += modules[i].Area;
        }
        Assert.IsTrue(dead > 0.75f * area, "setup: they take it past three quarters");
        ship.PartShownNow();
        Assert.AreEqual(1, dice.Count, "three deaths under it in one update roll once");
    }

    [TestMethod]
    public void ALostPlateComesBackByItsRepairsSinceItsLowest()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        const int plate = 1;
        ShipModule[] modules = ModulesOf(ship, plate, out int area);
        int alive = area, living = modules.Length;
        while (ship.PartShownNow() == null) // the biggest first, leaving small ones
        {
            Kill(modules[--living]);
            alive -= modules[living].Area;
        }

        ShipModule last = modules[0];
        Kill(last);
        int lowest = alive - last.Area;
        Assert.IsTrue(lowest > 0, "setup: its lowest is above none, so a repair counts from there");
        Assert.AreEqual(0f, ship.PartShownNow()[plate], "dying on, the lost plate shows nothing");

        Repair(last);
        AssertEqual(0.0001f, (alive - lowest) / (float)(area - lowest), ship.PartShownNow()[plate],
                    "a repair brings it back by its share of what was dead at its lowest");
        for (int i = living; i < modules.Length; ++i)
            Repair(modules[i]);
        Assert.IsNull(ship.PartShownNow(), "repaired whole, the plate is on again");
    }

    [TestMethod]
    public void AShipsModelLeavesOutItsLostPlatesFromItsNextUpdate()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        SceneObject so = Content.LoadStaticMesh("Model/Ships/Terran/HeavyGunboat/ship10b.fbx").CreateSceneObject();
        typeof(Ship).GetField("ShipSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(ship, so);
        ShipModule[] modules = ModulesOf(ship, 5, out int _);
        foreach (ShipModule m in modules)
            Kill(m);

        UState.Objects.Update(TestSimStep);
        CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f, 1f, 0f, 1f }, so.PartShown, "the lost plate is left out");

        foreach (ShipModule m in modules)
            Repair(m);
        UState.Objects.Update(TestSimStep);
        Assert.IsNull(so.PartShown, "repaired whole, the model is drawn whole");
    }

    [TestMethod]
    public void AModuleUnderALostPlateShowsNoDamageTillThePlateIsWhole()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        ShipModule[] lost = ModulesOf(ship, 5, out int _);
        ShipModule[] on = ModulesOf(ship, 6, out int _);
        foreach (ShipModule m in lost)
            Kill(m);
        Kill(on[0]);
        ship.PartShownNow();
        Repair(lost[0]);
        ship.PartShownNow();
        Assert.IsTrue(ship.PartShown[5] is > 0f and < 1f, "setup: the lost plate is on its way back");

        Assert.IsTrue(ship.UnderLostPart(lost[1]), "a dead module under the lost plate went with it");
        Assert.IsTrue(ship.UnderLostPart(lost[0]), "and one repaired under it shows no damage either while the plate is not whole");
        Assert.IsFalse(ship.UnderLostPart(on[0]), "a dead one under a plate still on shows its damage");
        foreach (ShipModule m in lost)
            Repair(m);
        ship.PartShownNow();
        Assert.IsFalse(ship.UnderLostPart(lost[0]), "the plate whole again, its modules show their damage");
    }

    [TestMethod]
    public void ASaveKeepsTheLostPlatesAndTheirRepairsSince()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        ShipModule[] modules = ModulesOf(ship, 3, out int _);
        foreach (ShipModule m in modules)
            Kill(m);
        Assert.AreEqual(0f, ship.PartShownNow()[3], "setup: the plate is lost");
        Repair(modules[0]);
        float[] before = ship.PartShownNow();
        Assert.IsTrue(before[3] > 0f && before[3] < 1f, "setup: the lost plate is on its way back");

        UState.Objects.Update(TestSimStep); // in its empire's list, to be saved
        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.ShipParts", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Assert.IsNotNull(loaded, "setup: the save loads");
        Ship after = loaded.UState.Objects.FindShip(ship.Id);
        CollectionAssert.AreEqual(before, after.PartShownNow(), "the lost plate and its repairs since come back");
    }

    [TestMethod]
    public void ADerelictShowsOnlyTheShipsWholePlatesAndSmokesNotUnderTheLostOnes()
    {
        SetupGunboats();
        Ship whole = SpawnGunboat();
        ShipModule[] modules = ModulesOf(whole, 5, out int area);
        int dead = 0;
        foreach (ShipModule m in modules)
        {
            if (dead + m.Area > 0.75f * area)
                break;
            Kill(m);
            dead += m.Area;
        }
        var onWreck = new Derelict(whole, null, salvageable: false, seen: false);
        Assert.IsNull(onWreck.PartShown, "setup: the plate is still on");
        Assert.IsTrue(onWreck.NumSmokeSources > 0, "setup: the dead modules under a plate still on smoke");

        Ship ship = SpawnGunboat(210_000f);
        ShipModule[] lost = ModulesOf(ship, 5, out int _);
        foreach (ShipModule m in lost)
            Kill(m);
        ship.PartShownNow();
        Repair(lost[0]);
        Assert.IsTrue(ship.PartShownNow()[5] is > 0f and < 1f, "setup: the lost plate is on its way back");

        var model = new SceneObject("ship"); // the ship's model, seen at its death, which the derelict takes over
        var d = new Derelict(ship, model, salvageable: false, seen: true);
        CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f, 1f, 0f, 1f }, d.PartShown, "the derelict shows only the plates the ship had whole");
        Assert.AreSame(d.PartShown, model.PartShown, "on the model it takes over");
        Assert.AreEqual(0, d.NumSmokeSources, "and no smoke from under the lost plate, where the only damage is");

        var unseen = new Derelict(ship, null, salvageable: false, seen: false);
        var so = new SceneObject("derelict");
        Assert.IsTrue(unseen.AttachModel(so), "setup: a model is attached later");
        Assert.AreSame(unseen.PartShown, so.PartShown, "which leaves the plate out too");
    }

    [TestMethod]
    public void ARecoveredShipKeepsItsDerelictsLostPlateAndLosesNoneToItsRecovery()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        foreach (ShipModule m in ModulesOf(ship, 5, out int _))
            Kill(m);
        var d = new Derelict(ship, null, salvageable: false, seen: false);

        Ship recovered = Ship.CreateRecoveredShip(UState, d, Player, model: null);
        float[] shown = recovered.PartShownNow();
        Assert.IsNotNull(shown, "the recovered ship has a lost plate");
        Assert.AreEqual(0f, shown[5], "its derelict's, coming back with its repairs");
        bool deadOn = false;
        for (int part = 1; part < shown.Length; ++part)
        {
            if (part == 5)
                continue;
            int dead = 0;
            foreach (ShipModule m in ModulesOf(recovered, part, out int area))
            {
                if (!m.Active)
                    dead += m.Area;
                deadOn |= dead > 0.75f * area;
            }
            Assert.AreEqual(1f, shown[part], $"part {part} stays on through the recovery's damage");
        }
        Assert.IsTrue(deadOn, "setup: the recovery leaves another plate more than three quarters dead");
    }

    // a plate more than three quarters dead, 0 for none
    static int ThreeQuartersDeadPlate(Ship ship)
    {
        ship.Grid.GetParts(out int[] partArea);
        for (int part = 1; part < partArea.Length; ++part)
        {
            ShipModule[] modules = ModulesOf(ship, part, out int area);
            int dead = 0;
            foreach (ShipModule m in modules)
                if (!m.Active)
                    dead += m.Area;
            if (dead > 0.75f * area)
                return part;
        }
        return 0;
    }

    [TestMethod]
    public void ADeathBeforeTheDesignKnowsItsPartsHitsOnlyItsOwnPart()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        var d = new Derelict(ship, null, salvageable: false, seen: false);
        Ship recovered = Ship.CreateRecoveredShip(UState, d, Player, model: null);
        Assert.IsNull(recovered.PartShownNow(), "setup: the recovery's damage loses no plate");
        int deadOn = ThreeQuartersDeadPlate(recovered);
        Assert.IsTrue(deadOn > 0, "setup: the recovery leaves a plate more than three quarters dead");

        int other = deadOn == 1 ? 2 : 1;
        ShipModule alive = System.Array.Find(ModulesOf(recovered, other, out int _), m => m.Active);
        Assert.IsNotNull(alive, $"setup: a module alive under plate {other}");
        typeof(ModuleGridFlyweight).GetField("Parts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                                   .SetValue(recovered.Grid, null); // as before any ship of the design counted its parts
        Kill(alive);

        float[] shown = recovered.PartShownNow();
        Assert.IsTrue(shown == null || shown[deadOn] == 1f, $"plate {deadOn} stays on, as no module died under it");
    }

    static object DamageVisualOf(ShipModule m)
    {
        return typeof(ShipModule).GetField("DamageVisualizer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(m);
    }

    // seen by the player, in the camera's view
    void ShowOnScreen(Ship ship)
    {
        ship.InFrustum = true;
        ship.KnownByEmpires.SetSeen(Player);
    }

    static ShipModule[] Burning(ShipModule[] modules)
    {
        ShipModule[] burning = System.Array.FindAll(modules, m => m.CanVisualizeDamage);
        Assert.IsTrue(burning.Length > 0, "setup: modules that show damage");
        return burning;
    }

    [TestMethod]
    public void AShipShowsNoDamageUnderALostPlateAliveOrDying()
    {
        SetupGunboats();
        Universe.Particles = new Ship_Game.Graphics.Particles.ParticleManager(Content);
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        Ship ship = SpawnGunboat();
        ShipModule[] lost = Burning(ModulesOf(ship, 5, out int _));
        foreach (ShipModule m in ModulesOf(ship, 5, out int _))
            Kill(m);
        ShipModule onPlateOn = Burning(ModulesOf(ship, 6, out int _))[0];
        Kill(onPlateOn);
        ship.PartShownNow();

        ShowOnScreen(ship);
        ship.UpdateModulePositions(TestSimStep, forceUpdate: true);
        Assert.IsNotNull(DamageVisualOf(onPlateOn), "setup: a dead module under a plate still on burns");
        foreach (ShipModule m in lost)
            Assert.IsNull(DamageVisualOf(m), "one under the lost plate shows nothing");

        SceneObject so = Content.LoadStaticMesh("Model/Ships/Terran/HeavyGunboat/ship10b.fbx").CreateSceneObject();
        typeof(Ship).GetField("ShipSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(ship, so);
        ship.Dying = true;
        ship.SetDieTimer(10f);
        ShipModule[] lostDying = Burning(ModulesOf(ship, 3, out int _));
        foreach (ShipModule m in ModulesOf(ship, 3, out int _))
            Kill(m);
        ShowOnScreen(ship);
        ship.Update(TestSimStep);
        Assert.IsTrue(ship.Dying && ship.Active, "setup: the ship is tumbling");
        Assert.IsNotNull(so.PartShown, "a plate lost as it tumbles");
        Assert.AreEqual(0f, so.PartShown[3], "leaves its model");
        Assert.IsNotNull(DamageVisualOf(onPlateOn), "setup: the dead module under a plate still on burns as it dies");
        foreach (ShipModule m in lostDying)
            Assert.IsNull(DamageVisualOf(m), "one under the plate lost as it tumbles shows nothing");
    }

    [TestMethod]
    public void ASaveKeepsADerelictsLostPlates()
    {
        SetupGunboats();
        var site = new SDGraphics.Vector2(600_000f, 600_000f);
        AddDummyPlanet(site, 1f, 1f, 1f, site + new SDGraphics.Vector2(0, 60_000f), explored: true);
        Ship ship = SpawnGunboatAt(site + new SDGraphics.Vector2(30_000f, 0));
        foreach (ShipModule m in ModulesOf(ship, 5, out int _))
            Kill(m);
        Assert.IsTrue(UState.Derelicts.TryAdd(ship, salvageable: true, seen: false), "setup: a salvageable derelict");
        Universe.InvokePendingSimThreadActions();
        ship.QueueTotalRemoval();
        Derelict d = System.Linq.Enumerable.Last(UState.Derelicts.Current);
        CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f, 1f, 0f, 1f }, d.PartShown, "setup: the derelict has the lost plate");

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.ShipPartsDerelict", throwOnError: true);
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Assert.IsNotNull(loaded, "setup: the save loads");
        Derelict after = System.Array.Find(loaded.UState.Derelicts.ToArray(), x => x.Id == d.Id);
        Assert.IsNotNull(after, "setup: the derelict comes back");
        CollectionAssert.AreEqual(d.PartShown, after.PartShown, "with its lost plate");
    }

    static object ShipField(Ship ship, string name)
    {
        return typeof(Ship).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(ship);
    }

    [TestMethod]
    public void ASaveKeepsADeathNotCountedYet()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        UState.Objects.Update(TestSimStep); // in its empire's list, to be saved
        foreach (ShipModule m in ModulesOf(ship, 3, out int _))
            Kill(m); // the plate's deaths come just before the save, with no update to count them

        UState.StarDate = 1001f; // the loader needs a stardate past 1000
        SavedGame save = Universe.Save("UnitTest.ShipPartsHit", throwOnError: true);
        Assert.IsNull(ShipField(ship, "LostPartLow"), "setup: the save came before the count");
        UniverseScreen loaded = LoadSave(save.SaveFile);
        Assert.IsNotNull(loaded, "setup: the save loads");
        Ship after = loaded.UState.Objects.FindShip(ship.Id);
        SetDice(after.Loyalty, SureDice()); // the loaded empire rolls anew
        float[] shown = after.PartShownNow();
        Assert.IsNotNull(shown, "the deaths count after loading");
        Assert.AreEqual(0f, shown[3], "and the plate is lost");
    }

    [TestMethod]
    public void ADerelictMadeBeforeItsHullsModelLoadsTakesTheLostPlatesAsSaved()
    {
        SetupGunboats();
        Ship ship = SpawnGunboat();
        foreach (ShipModule m in ModulesOf(ship, 5, out int _))
            Kill(m);
        Assert.IsNotNull(ship.PartShownNow(), "setup: the plate is lost");

        // as loaded from a save in a game that has not drawn the hull yet
        var triangles = typeof(ShipHull).GetField("PartTriangles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        object loadedTriangles = triangles.GetValue(ship.BaseHull);
        try
        {
            triangles.SetValue(ship.BaseHull, null);
            typeof(Ship).GetProperty(nameof(Ship.PartShown))!.SetValue(ship, null);
            typeof(Ship).GetField("PartsOutdated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(ship, true);
            Assert.IsNull(ship.PartShownNow(), "setup: the parts cannot be counted");

            var d = new Derelict(ship, null, salvageable: false, seen: false);
            CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f, 1f, 0f, 1f }, d.PartShown, "the derelict takes the lost plate as saved");
        }
        finally
        {
            triangles.SetValue(ship.BaseHull, loadedTriangles);
        }
    }
}
