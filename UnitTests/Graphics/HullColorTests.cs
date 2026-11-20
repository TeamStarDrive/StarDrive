using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game;
using Ship_Game.Data.Mesh;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using Ship_Game.Universe;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;
using UnitTests.Ships;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA2213

namespace UnitTests.Graphics;

/// <summary>
/// The player's ships take the empire color while the player's hull color box is on, as it is once the player takes a
/// color other than the race's own, and every opponent's and the pirates' while the opponents' box is, as it is once the
/// player picks an opponent a color: on the hull paint their model's tint map marks when the race paints its ships.
/// </summary>
[TestClass]
public class HullColorTests : StarDriveTest
{
    [TestMethod]
    [DataRow("Model/Ships/Vulfen/ship14_d.dds", "Model/Ships/Vulfen/ship14_t.dds")]
    [DataRow("Model/Ships/my_dir/hull_d2.dds", "Model/Ships/my_dir/hull_t2.dds")]
    [DataRow("mod models/Vulfar/ship13_d_0.dds", "mod models/Vulfar/ship13_t_0.dds")]
    [DataRow("Model/Ships/Terran/ship07_altglow_d.dds", "Model/Ships/Terran/ship07_altglow_t.dds")]
    [DataRow("Model/Ships/Kulrathi/SciFi_Ship_Escort-Diffuse.dds", "Model/Ships/Kulrathi/SciFi_Ship_Escort-Tint.dds")]
    [DataRow("Model/Ships/my_dir/hull.dds", null)]
    [DataRow("", null)]
    public void ATintMapSitsNextToItsDiffuseMapWithTForD(string diffuseMap, string tintMap)
    {
        Assert.AreEqual(tintMap, MeshInterface.TintMapFileOf(diffuseMap));
    }

    // the shipped tint maps load with their models, marking the share of the texture an owner paints; a model with none has none
    [TestMethod]
    [DataRow("Model/Ships/Vulfen/TypeXIII.fbx", 0.069f)]
    [DataRow("Model/Ships/Vulfen/TypeXIX.fbx", 0.069f)]
    [DataRow("Model/Ships/Draylok/ship18c.fbx", 0.762f)]
    [DataRow("Model/Ships/Ralyeh/ship17c.fbx", 0.300f)]
    [DataRow("Model/Ships/Pollops/Fern.fbx", 0.750f)]
    [DataRow("Model/Ships/Opteris/ship19d.fbx", 0.676f)]
    [DataRow("Model/Ships/Kulrathi/ship11f.fbx", 0.689f)]
    [DataRow("Model/Ships/Kulrathi/ship12d.fbx", 0.865f)]
    [DataRow("Model/Ships/Kulrathi/Yamamoto.fbx", 0.297f)]
    [DataRow("Model/Ships/Cordrazine/ship16e.fbx", 0.931f)]
    [DataRow("Model/Ships/Terran/Battleship/Battleship.fbx", 0.042f)]
    [DataRow("Model/Ships/Terran/SharedTextures/LightCruiser.fbx", 0.042f)]
    [DataRow("Model/Ships/Terran/Hunter/Fighter1.fbx", 0.110f)]
    [DataRow("Model/Ships/Terran/Caravan/Caravan.fbx", 0.058f)]
    [DataRow("Model/Ships/Terran/Shuttle/ship08.fbx", 0.191f)]
    [DataRow("Model/Stations/Station_Small.fbx", 0.013f)]
    [DataRow("Model/Stations/spacestation01_outer.fbx", 0.013f)]
    [DataRow("Model/Ships/CargoShips/CargoHaulerShort.fbx", 0.263f)]
    [DataRow("Model/Ships/Platforms/platform_base.fbx", 0.112f)]
    [DataRow("Model/Ships/Remnant/ship09e.fbx", 0.344f)]
    [DataRow("Model/Ships/Remnant/XenoFighterGreen/ship09a.fbx", 0.344f)]
    [DataRow("Model/Ships/Speeder/ship07.fbx", 0f)]
    public void AShipModelTakesTheTintMapNextToItsDiffuseMap(string model, float markedShare)
    {
        StaticMesh mesh = Content.LoadStaticMesh(model);
        LightingEffect fx = mesh.GetFirstEffect<LightingEffect>();
        Assert.IsNotNull(fx?.DiffuseMapTexture, $"setup: {model} loads with its diffuse map");
        Assert.AreEqual(markedShare > 0f, fx.TintMapTexture != null, $"{model} tint map {fx.TintMapFile}");
        if (fx.TintMapTexture != null)
            AssertEqual(0.002f, markedShare, MarkedShare(fx.TintMapTexture), $"{fx.TintMapFile} marks its whole region");
    }

    // a vanilla model's relative texture paths start at Content/, and its tint map is found with a mod running too
    [TestMethod]
    public void AVanillaModelFindsItsTintMapWithAModRunning()
    {
        try
        {
            GlobalStats.SetActiveModNoSave(new ModEntry(GamePlayGlobals.Deserialize(new System.IO.FileInfo("Mods/ExampleMod/Globals.yaml"))));
            Assert.IsTrue(GlobalStats.HasMod, "setup: a mod is running");
            using var content = new Ship_Game.Data.GameContentManager(Content.ServiceProvider, "ModTintMapTest");
            StaticMesh mesh = content.LoadStaticMesh("Model/Ships/Terran/Gunboat/Gunboat.fbx");
            LightingEffect fx = mesh.GetFirstEffect<LightingEffect>();
            Assert.IsNotNull(fx?.DiffuseMapTexture, "setup: the Gunboat loads with its diffuse map");
            StringAssert.StartsWith(fx.DiffuseMapFile, "Content/", "setup: its diffuse map path starts at Content/");
            Assert.IsNotNull(fx.TintMapTexture, $"the Gunboat's tint map {fx.TintMapFile} loads under the mod");
        }
        finally
        {
            GlobalStats.SetActiveModNoSave(null);
        }
    }

    // a model measures the OKLab lightness of the paint its tint map marks as it loads, black unused texture space left out; a model with no tint map measures none
    [TestMethod]
    [DataRow("Model/Ships/Cordrazine/ship16e.fbx", 0.239f)]
    [DataRow("Model/Ships/Pollops/Fern.fbx", 0.414f)]
    [DataRow("Model/Ships/Vulfen/TypeXIII.fbx", 0.359f)]
    [DataRow("Model/Ships/Draylok/ship18c.fbx", 0.288f)]
    [DataRow("Model/Ships/Opteris/ship19d.fbx", 0.590f)]
    [DataRow("Model/Ships/Speeder/ship07.fbx", 0f)]
    public void AShipModelMeasuresTheLightnessOfItsPaint(string model, float paintLightness)
    {
        StaticMesh mesh = Content.LoadStaticMesh(model);
        LightingEffect fx = mesh.GetFirstEffect<LightingEffect>();
        Assert.IsNotNull(fx?.DiffuseMapTexture, $"setup: {model} loads with its diffuse map");
        AssertEqual(0.003f, paintLightness, fx.PaintLightness, $"{model} paint under {fx.TintMapFile}");
    }

    // the paint is measured from the texture files the model loaded, not read back from the GPU
    [TestMethod]
    public void APaintIsMeasuredFromItsTextureFiles()
    {
        LightingEffect fx = Content.LoadStaticMesh("Model/Ships/Cordrazine/ship16e.fbx").GetFirstEffect<LightingEffect>();
        using var diffuse = new Microsoft.Xna.Framework.Graphics.Texture2D(Game.GraphicsDevice, 4, 4) { Name = fx.DiffuseMapTexture.Name };
        using var mask = new Microsoft.Xna.Framework.Graphics.Texture2D(Game.GraphicsDevice, 4, 4) { Name = fx.TintMapTexture.Name };
        AssertEqual(0.003f, 0.239f, MeshInterface.PaintLightnessOf(diffuse, mask), "blank textures named for the Cordrazine's files measure its paint");
    }

    // a paint whose files cannot be read is not measured, so it keeps its own lightness
    [TestMethod]
    public void APaintWhoseFilesCannotBeReadIsNotMeasured()
    {
        using var diffuse = new Microsoft.Xna.Framework.Graphics.Texture2D(Game.GraphicsDevice, 4, 4) { Name = "Content/Model/Ships/NoSuchShip/hull_d.dds" };
        using var mask = new Microsoft.Xna.Framework.Graphics.Texture2D(Game.GraphicsDevice, 4, 4) { Name = "Content/Model/Ships/NoSuchShip/hull_t.dds" };
        Assert.AreEqual(0f, MeshInterface.PaintLightnessOf(diffuse, mask));
    }

    // OKLab lightness runs from black to white as the eye sees it, so pure blue is lighter than its share of light suggests
    [TestMethod]
    [DataRow(0f, 0f, 0f, 0f)]
    [DataRow(1f, 1f, 1f, 1f)]
    [DataRow(0.502f, 0.502f, 0.502f, 0.600f)]
    [DataRow(0f, 0f, 1f, 0.452f)]
    [DataRow(1f, 1f, 0f, 0.968f)]
    [DataRow(1f, 0f, 0f, 0.628f)]
    public void AColorsLightnessIsHowLightItLooks(float red, float green, float blue, float lightness)
    {
        AssertEqual(0.001f, lightness, LightingEffect.LightnessOf(red, green, blue));
    }

    // a DXT1 texel read alone decodes as the whole image's decoding gives it, a texel its block marks transparent as black
    [TestMethod]
    public void ADxt1TexelReadAloneDecodesAsTheWholeImageDoes()
    {
        LightingEffect fx = Content.LoadStaticMesh("Model/Ships/Cordrazine/ship16e.fbx").GetFirstEffect<LightingEffect>();
        foreach (Microsoft.Xna.Framework.Graphics.Texture2D tex in new[] { fx.DiffuseMapTexture, fx.TintMapTexture })
        {
            Assert.AreEqual(Microsoft.Xna.Framework.Graphics.SurfaceFormat.Dxt1, tex.Format, $"setup: {tex.Name} is DXT1");
            Color[] whole = Ship_Game.Data.Texture.ImageUtils.DecompressDxt1(tex);
            var blocks = new byte[tex.Width * tex.Height / 2];
            tex.GetData(blocks);
            int differ = 0;
            for (int y = 0; y < tex.Height; ++y)
            {
                for (int x = 0; x < tex.Width; ++x)
                {
                    Color expected = whole[y * tex.Width + x];
                    if (Ship_Game.Data.Texture.ImageUtils.Dxt1Texel(blocks, tex.Width, x, y) != (expected.A == 0 ? Color.Transparent : expected))
                        ++differ;
                }
            }
            Assert.AreEqual(0, differ, $"{tex.Name}: texels decoded differently");
        }
    }

    static float MarkedShare(Microsoft.Xna.Framework.Graphics.Texture2D mask)
    {
        Color[] texels;
        if (mask.Format == Microsoft.Xna.Framework.Graphics.SurfaceFormat.Dxt1)
        {
            texels = Ship_Game.Data.Texture.ImageUtils.DecompressDxt1(mask);
        }
        else
        {
            texels = new Color[mask.Width * mask.Height];
            mask.GetData(texels);
        }
        int count = mask.Width * mask.Height; // the decoder's array runs past the image
        int marked = 0;
        for (int i = 0; i < count; ++i)
            if (texels[i].R > 127 && texels[i].A > 127) // a transparent DXT1 texel samples as black
                ++marked;
        return marked / (float)count;
    }

    [TestMethod]
    public void EachBoxColorsItsOwnSidesShipsWhenTheirRaceColorsThem()
    {
        CreateUniverseAndPlayerEmpire("Vulfen", "Cordrazine");
        Assert.AreEqual(HullColorParts.Paint, Player.data.HullColors, "setup: Vulfen paint their hulls");
        Assert.AreEqual(HullColorParts.Paint, Enemy.data.HullColors, "setup: and so do the Cordrazine");
        UState.P.OpponentHullColors = false; // as in a new game
        Assert.AreEqual(0f, Player.HullTint.W, "the player's box is off in a new game, so the player's ships keep their look");
        Assert.AreEqual(0f, Enemy.HullTint.W, "and so is the opponents' box, so theirs keep theirs");

        UState.P.PlayerHullColors = true;
        Assert.AreEqual(new Vector4(Player.EmpireColor.ToVector3(), 1f), Player.HullTint, "with it on the player's ships take the empire color");
        Assert.AreEqual(0f, Enemy.HullTint.W, "the opponents' keep their look");

        UState.P.OpponentHullColors = true;
        Assert.AreEqual(new Vector4(Enemy.EmpireColor.ToVector3(), 1f), Enemy.HullTint, "with the opponents' box on an opponent takes its color, picked or not");
        AssertEqual(0.001f, 1f, Player.HullTint.W, "and the player's keep the color");

        UState.P.PlayerHullColors = false;
        Assert.AreEqual(0f, Player.HullTint.W, "with the player's box off the player's are bare");
        AssertEqual(0.001f, 1f, Enemy.HullTint.W, "and the AI empire's keep theirs");

        UState.P.PlayerHullColors = true;
        Player.data.HullColors = HullColorParts.None;
        Assert.AreEqual(0f, Player.HullTint.W, "a race that paints no hulls keeps them bare");
    }

    [TestMethod]
    public void TheOpponentsBoxColorsPiratesButNoOtherFaction()
    {
        CreateUniverseAndPlayerEmpire("Vulfen", "Cordrazine");
        CreateAMinorFaction("Corsairs");
        Empire pirates = Faction;
        Empire rebels = UState.CreateRebelsFromEmpireData(Enemy.data, Enemy);
        Assert.IsTrue(pirates.WeArePirates && pirates.IsFaction, "setup: the Corsairs are pirates");
        Assert.AreEqual(HullColorParts.Paint, pirates.data.HullColors, "setup: the Corsairs race file paints their hulls");
        Assert.IsTrue(rebels.IsFaction && !rebels.WeArePirates, "setup: rebels are a faction, not pirates");
        Assert.AreEqual(HullColorParts.Paint, rebels.data.HullColors, "setup: and paint their hulls, as the Cordrazine they rose from do");
        UState.P.OpponentHullColors = true;

        Assert.AreEqual(new Vector4(pirates.EmpireColor.ToVector3(), 1f), pirates.HullTint, "pirates wear their color");
        Assert.AreEqual(0f, rebels.HullTint.W, "rebels keep their look");
    }

    // the Remnant hulls mark paint, so only the faction rule keeps the Remnants' own ships in their look
    [TestMethod]
    public void TheRemnantsOwnShipsKeepTheirLookWithTheOpponentsBoxOn()
    {
        CreateUniverseAndPlayerEmpire("Vulfen", "Cordrazine");
        CreateAMinorFaction("Remnant");
        UState.Remnants = Faction;
        Empire remnants = Faction;
        remnants.data.HullColors = HullColorParts.Paint; // as a mod's Remnant race could have it
        Assert.IsTrue(remnants.WeAreRemnants && remnants.IsFaction, "setup: the Remnants are a faction");
        UState.P.OpponentHullColors = true;

        Assert.AreEqual(0f, remnants.HullTint.W, "the Remnants' own ships keep their look");
    }

    [TestMethod]
    public void ANewGameStartsWithTheOpponentsBoxOffAndPickingAnOpponentAColorTicksIt()
    {
        CreateUniverseAndPlayerEmpire("Vulfen", "Kulrathi");
        IEmpireData race = ResourceManager.MajorRaces.First(e => e.ArchetypeName.Contains("Cordrazine"));
        UniverseParams p = UniverseParams.NewGame();
        Assert.IsFalse(p.OpponentHullColors, "a new game starts with the opponents' box off");

        p.PickOpponentLook(race, race.Traits.Color, 12);
        Assert.IsFalse(p.OpponentHullColors, "a flag alone does not tick it");

        p.PickOpponentLook(race, new Color(0, 200, 255), 12);
        Assert.IsTrue(p.OpponentHullColors, "a color other than the race's own ticks it");

        p.PickOpponentLook(race, race.Traits.Color, 12);
        Assert.IsTrue(p.OpponentHullColors, "taking the race's own color back does not untick it");
    }

    // the race file's setting is never saved, so a loaded game takes it from the player's race; the boxes load as saved, at their defaults or not
    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void ALoadedGameTakesItsHullColorsFromThePlayersRace(bool playerBox, bool opponentsBox)
    {
        CreateUniverseAndPlayerEmpire("Vulfen");
        UState.P.PlayerHullColors = playerBox;
        UState.P.OpponentHullColors = opponentsBox;
        Player.data.HullColors = HullColorParts.None;

        UState.StarDate = 1001f; // past the start, so the load adds no starting ships
        SavedGame save = Universe.Save("UnitTest.HullColors", throwOnError: true);
        UniverseState loaded = LoadSave(save.SaveFile).UState;

        Assert.AreEqual(HullColorParts.Paint, loaded.Player.data.HullColors, "the Vulfen race file paints their hulls");
        Assert.AreEqual(playerBox, loaded.P.PlayerHullColors, "the player's box is saved with the game");
        Assert.AreEqual(opponentsBox, loaded.P.OpponentHullColors, "and the opponents' box");
    }

    [TestMethod]
    public void AGameSavedBeforeTheHullColorBoxesLoadsWithBothOff()
    {
        var p = new UniverseParams(); // a load builds it, then sets the fields its save has
        Assert.IsTrue(p.OpponentHullColors, "setup: built as a load builds it, the opponents' box is on");
        p.OnDeserialized(); // a save from before the boxes has none of their fields
        Assert.IsFalse(p.PlayerHullColors, "a game saved before them loads with the player's box off");
        Assert.IsFalse(p.OpponentHullColors, "and the opponents' box off");
    }

    // a chosen opponent plays in the color and flag picked for it, and dropping it drops them
    [TestMethod]
    public void AChosenOpponentPlaysInTheColorAndFlagPickedForIt()
    {
        CreateUniverseAndPlayerEmpire("Vulfen", "Kulrathi");
        IEmpireData race = ResourceManager.MajorRaces.First(e => e.ArchetypeName.Contains("Cordrazine"));
        Color raceColor = race.Traits.Color;
        int raceFlag = race.Traits.FlagIndex;
        var picked = new Color(0, 200, 255);
        Assert.AreNotEqual(raceColor, picked, "setup: a color the race does not have");
        Assert.AreNotEqual(raceFlag, 12, "setup: and a flag");
        UState.P.SelectedOpponents.Add(race);
        UState.P.OpponentLooks[race] = (picked, 12);

        Empire e = UState.CreateEmpire(UState.P.OpponentData(race), isPlayer: false);
        Assert.AreEqual(picked, e.EmpireColor, "the opponent plays in the picked color");
        Assert.AreEqual(picked, e.data.Traits.Color, "its race data too");
        Assert.AreEqual(12, e.data.Traits.FlagIndex, "under the picked flag");
        Assert.AreEqual(raceColor, race.Traits.Color, "the race keeps its own color for later games");
        Assert.AreEqual(raceFlag, race.Traits.FlagIndex, "and its own flag");

        Assert.IsTrue(UState.P.DropOpponent(race), "setup: the opponent is dropped");
        Assert.AreSame(race, UState.P.OpponentData(race), "chosen again, it plays as its race");
    }

    // a chosen opponent plays in the color and flag picked for it, and one picked just a flag keeps its race's color
    [TestMethod]
    public void ANewGalaxyCreatesAChosenOpponentInTheColorAndFlagPickedForIt()
    {
        LoadAllGameData();
        IEmpireData race = ResourceManager.MajorRaces.First(e => e.ArchetypeName.Contains("Cordrazine"));
        IEmpireData flagOnly = ResourceManager.MajorRaces.First(e => e.ArchetypeName.Contains("Kulrathi"));
        EmpireData playerData = ResourceManager.FindEmpire("United").CreateInstance();
        playerData.DiplomaticPersonality = new DTrait();
        (int numStars, float starsModifier) = RaceDesignScreen.GetNumStars(RaceDesignScreen.StarsAbundance.Normal, GalSize.Tiny, 3);
        var p = new UniverseParams
        {
            PlayerData = playerData,
            Mode = RaceDesignScreen.GameMode.Sandbox,
            GalaxySize = GalSize.Tiny,
            NumSystems = numStars,
            NumOpponents = 3,
            StarsModifier = starsModifier,
        };
        var picked = new Color(0, 200, 255);
        Assert.AreNotEqual(12, flagOnly.Traits.FlagIndex, "setup: a flag the Kulrathi do not have");
        p.SelectedOpponents.Add(race);
        p.OpponentLooks[race] = (picked, 12);
        p.SelectedOpponents.Add(flagOnly);
        p.OpponentLooks[flagOnly] = (flagOnly.Traits.Color, 12);

        CreateCustomUniverse(p);
        Empire opponent = UState.GetEmpireByName(race.Name);
        Assert.IsNotNull(opponent, "setup: the chosen opponent is in the galaxy");
        Assert.AreEqual(picked, opponent.EmpireColor, "in the picked color");
        Assert.AreEqual(12, opponent.data.Traits.FlagIndex, "under the picked flag");

        Empire flagged = UState.GetEmpireByName(flagOnly.Name);
        Assert.IsNotNull(flagged, "setup: the other chosen opponent is in the galaxy");
        Assert.AreEqual(12, flagged.data.Traits.FlagIndex, "the other chosen opponent is under the picked flag");
        Assert.AreEqual(flagOnly.Traits.Color, flagged.EmpireColor, "in its race's own color");
    }

    // a ship's model takes its owner's color as the ship updates, so a captured ship is drawn in its captor's at once
    [TestMethod]
    public void AShipIsDrawnInItsOwnersColorAndACapturedOneInItsCaptorsAtOnce()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire("Vulfen", "Cordrazine");
        UState.P.PlayerHullColors = true;
        UState.P.OpponentHullColors = false;
        AssertEqual(0.001f, 1f, Player.HullTint.W, "setup: the player's ships are colored");
        TestShip ship = SpawnShip("Terran-Prototype", Enemy, new Vector2(1000f, 1000f));
        SceneObject so = GiveSceneObject(ship);
        ship.Update(TestSimStep);
        Assert.AreEqual(0f, so.HullTint.W, "with the opponents' box off an opponent's ship is drawn bare");

        ship.LoyaltyChangeByGift(Player, addNotification: false);
        UState.Objects.Update(TestSimStep);
        Assert.AreEqual(Player, ship.Loyalty, "setup: the player has taken the ship");
        Assert.AreEqual(Player.HullTint, so.HullTint, "it is drawn in the player's color");

        Universe.Particles = new ParticleManager(Content); // a dying ship on screen throws sparks
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        ship.Dying = true;
        ship.SetDieTimer(10f);
        so.HullTint = default;
        ship.InFrustum = true;
        ship.KnownByEmpires.SetSeen(Player);
        ship.Update(TestSimStep);
        Assert.IsTrue(ship.IsVisibleToPlayer, "setup: the dying ship is on screen");
        Assert.AreEqual(Player.HullTint, so.HullTint, "and keeps it while it dies");
    }

    // a space port takes its owner's color like the owner's ships do, built with its race's model or the common one
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ASpacePortTakesItsOwnersColor(bool raceModel)
    {
        CreateUniverseAndPlayerEmpire("Cordrazine");
        UState.P.PlayerHullColors = true;
        if (!raceModel)
            Player.data.SpacePortModel = null;
        Planet home = AddHomeWorldToEmpire(new Vector2(400_000f), Player);
        var station = new SpaceStation();
        station.CreateSceneObject(home, Player);
        var outer = (SceneObject)typeof(SpaceStation).GetField("OuterSO", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(station);
        Assert.IsNotNull(outer, "setup: the port's model loaded");
        AssertEqual(0.001f, 1f, Player.HullTint.W, "setup: the player's ships are colored");
        Assert.AreEqual(Player.HullTint, outer.HullTint, raceModel ? "the Cordrazine port takes the player's color" : "and so does the common port");
        station.RemoveSceneObject();
    }

    // a space port's model follows its owner's color as it updates
    [TestMethod]
    public void ASpacePortFollowsItsOwnersColorAsItUpdates()
    {
        CreateUniverseAndPlayerEmpire("Cordrazine");
        UState.P.PlayerHullColors = true;
        Planet home = AddHomeWorldToEmpire(new Vector2(400_000f), Player);
        var station = new SpaceStation();
        var so = new SceneObject("station");
        typeof(SpaceStation).GetField("OuterSO", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(station, so);

        station.UpdateVisibleStation(home, TestSimStep);
        AssertEqual(0.001f, 1f, so.HullTint.W, "the port takes the player's color");
        UState.P.PlayerHullColors = false;
        station.UpdateVisibleStation(home, TestSimStep);
        Assert.AreEqual(0f, so.HullTint.W, "and drops it with the player's box off");
    }

    [TestCleanup]
    public override void Cleanup()
    {
        Universe?.Particles?.Dispose();
        if (Universe != null)
            Universe.Particles = null;
        base.Cleanup();
    }

    static SceneObject GiveSceneObject(Ship ship)
    {
        var so = new SceneObject(ship.Name);
        typeof(Ship).GetField("ShipSO", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(ship, so);
        return so;
    }
}
