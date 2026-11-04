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
/// The player's ships take the empire color while the player's hull color box is on, and the AI major empires' while
/// the opponents' box is, as both are by default: on the hull paint their model's tint map marks when the race paints
/// its ships, or on the lights of the race's own hulls.
/// </summary>
[TestClass]
public class HullColorTests : StarDriveTest
{
    [TestMethod]
    [DataRow("Model/Ships/Vulfen/ship14_d.dds", "Model/Ships/Vulfen/ship14_t.dds")]
    [DataRow("Model/Ships/Vulfen/ship14_d2.dds", "Model/Ships/Vulfen/ship14_t2.dds")]
    [DataRow("mod models/Vulfar/ship13_d_0.dds", "mod models/Vulfar/ship13_t_0.dds")]
    [DataRow("Model/Ships/Terran/ship07_altglow_d.dds", "Model/Ships/Terran/ship07_altglow_t.dds")]
    [DataRow("Model/Ships/Kulrathi/SciFi_Ship_Escort-Diffuse.dds", null)]
    [DataRow("Model/Ships/my_dir/hull.dds", null)]
    [DataRow("", null)]
    public void ATintMapSitsNextToItsDiffuseMapWithTForD(string diffuseMap, string tintMap)
    {
        Assert.AreEqual(tintMap, MeshInterface.TintMapFileOf(diffuseMap));
    }

    // the shipped tint maps load with their models, and a model with none has none
    [TestMethod]
    [DataRow("Model/Ships/Vulfen/TypeXIII.fbx", true)]
    [DataRow("Model/Ships/Vulfen/TypeXIX.fbx", true)]
    [DataRow("Model/Ships/Cordrazine/ship16e.fbx", false)]
    [DataRow("Model/Ships/Terran/Battleship/Battleship.fbx", false)]
    public void AShipModelTakesTheTintMapNextToItsDiffuseMap(string model, bool hasTintMap)
    {
        StaticMesh mesh = Content.LoadStaticMesh(model);
        LightingEffect fx = mesh.GetFirstEffect<LightingEffect>();
        Assert.IsNotNull(fx?.DiffuseMapTexture, $"setup: {model} loads with its diffuse map");
        Assert.AreEqual(hasTintMap, fx.TintMapTexture != null, $"{model} tint map {fx.TintMapFile}");
    }

    [TestMethod]
    public void EachBoxColorsItsOwnSidesShipsWhenTheirRaceColorsThem()
    {
        CreateUniverseAndPlayerEmpire("Vulfen", "Cordrazine");
        Assert.AreEqual(HullColorParts.Paint, Player.data.HullColors, "setup: Vulfen paint their hulls");
        Assert.AreEqual(HullColorParts.Lights, Enemy.data.HullColors, "setup: the Cordrazine color their lights");
        Assert.AreEqual(new Vector4(Player.EmpireColor.ToVector3(), 1f), Player.HullTint, "both boxes are on by default: the player's ships take the empire color");
        Assert.AreEqual(0f, Player.LightsTint("Vulfen").W, "on their paint, not their lights");
        Assert.AreEqual(new Vector4(Enemy.EmpireColor.ToVector3(), 1f), Enemy.LightsTint("Cordrazine"), "and an AI empire's ships take its own");

        UState.P.OpponentHullColors = false;
        Assert.AreEqual(0f, Enemy.LightsTint("Cordrazine").W, "with the opponents' box off theirs are bare");
        AssertEqual(0.001f, 1f, Player.HullTint.W, "and the player's keep the color");

        UState.P.OpponentHullColors = true;
        UState.P.PlayerHullColors = false;
        Assert.AreEqual(0f, Player.HullTint.W, "with the player's box off the player's are bare");
        AssertEqual(0.001f, 1f, Enemy.LightsTint("Cordrazine").W, "and the AI empire's keep theirs");

        UState.P.PlayerHullColors = true;
        Player.data.HullColors = HullColorParts.None;
        Assert.AreEqual(0f, Player.HullTint.W, "a race that paints no hulls keeps them bare");

        Enemy.IsFaction = true;
        Assert.AreEqual(0f, Enemy.LightsTint("Cordrazine").W, "and a faction's ships keep their look");
    }

    // a race that colors its lights does so only on its own race's hulls, and leaves their paint alone
    [TestMethod]
    public void TheCordrazineColorTheLightsOfTheirOwnHullsOnly()
    {
        CreateUniverseAndPlayerEmpire("Cordrazine", "Vulfen");
        Assert.AreEqual(HullColorParts.Lights, Player.data.HullColors, "setup: the Cordrazine color their lights");
        Assert.AreEqual(new Vector4(Player.EmpireColor.ToVector3(), 1f), Player.LightsTint("Cordrazine"), "their own hulls' lights take the empire color");
        Assert.AreEqual(0f, Player.LightsTint("Vulfen").W, "a captured Vulfen hull keeps its lights");
        Assert.AreEqual(0f, Player.HullTint.W, "and no hull's paint changes");

        UState.P.PlayerHullColors = false;
        Assert.AreEqual(0f, Player.LightsTint("Cordrazine").W, "with the box off their own lights keep their color too");
    }

    // the race file's setting is never saved, so a loaded game takes it from the player's race
    [TestMethod]
    public void ALoadedGameTakesItsHullColorsFromThePlayersRace()
    {
        CreateUniverseAndPlayerEmpire("Vulfen");
        UState.P.PlayerHullColors = false;
        Player.data.HullColors = HullColorParts.None;

        UState.StarDate = 1001f; // past the start, so the load adds no starting ships
        SavedGame save = Universe.Save("UnitTest.HullColors", throwOnError: true);
        UniverseState loaded = LoadSave(save.SaveFile).UState;

        Assert.AreEqual(HullColorParts.Paint, loaded.Player.data.HullColors, "the Vulfen race file paints their hulls");
        Assert.IsFalse(loaded.P.PlayerHullColors, "the player's box is saved with the game, here off");
        Assert.IsTrue(loaded.P.OpponentHullColors, "and the opponents' box, here on");
    }

    [TestMethod]
    public void AGameSavedBeforeTheHullColorBoxesLoadsWithBothOff()
    {
        var p = new UniverseParams(); // a load builds it as a new game does, then sets the fields its save has
        Assert.IsTrue(p.PlayerHullColors && p.OpponentHullColors, "setup: built as for a new game, both boxes are on");
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

    [TestMethod]
    public void ANewGalaxyCreatesAChosenOpponentInTheColorAndFlagPickedForIt()
    {
        LoadAllGameData();
        IEmpireData race = ResourceManager.MajorRaces.First(e => e.ArchetypeName.Contains("Cordrazine"));
        EmpireData playerData = ResourceManager.FindEmpire("United").CreateInstance();
        playerData.DiplomaticPersonality = new DTrait();
        (int numStars, float starsModifier) = RaceDesignScreen.GetNumStars(RaceDesignScreen.StarsAbundance.Normal, GalSize.Tiny, 1);
        var p = new UniverseParams
        {
            PlayerData = playerData,
            Mode = RaceDesignScreen.GameMode.Sandbox,
            GalaxySize = GalSize.Tiny,
            NumSystems = numStars,
            NumOpponents = 1,
            StarsModifier = starsModifier,
        };
        var picked = new Color(0, 200, 255);
        p.SelectedOpponents.Add(race);
        p.OpponentLooks[race] = (picked, 12);

        CreateCustomUniverse(p);
        Empire opponent = UState.GetEmpireByName(race.Name);
        Assert.IsNotNull(opponent, "setup: the chosen opponent is in the galaxy");
        Assert.AreEqual(picked, opponent.EmpireColor, "in the picked color");
        Assert.AreEqual(12, opponent.data.Traits.FlagIndex, "under the picked flag");
    }

    // a ship's model takes its owner's color as the ship updates, so a captured ship is drawn in its captor's at once
    [TestMethod]
    public void AShipIsDrawnInItsOwnersColorAndACapturedOneInItsCaptorsAtOnce()
    {
        LoadStarterShips("Terran-Prototype");
        CreateUniverseAndPlayerEmpire("Vulfen", "Cordrazine");
        AssertEqual(0.001f, 1f, Player.HullTint.W, "setup: the player's ships are colored");
        TestShip ship = SpawnShip("Terran-Prototype", Enemy, new Vector2(1000f, 1000f));
        SceneObject so = GiveSceneObject(ship);
        ship.Update(TestSimStep);
        Assert.AreEqual(0f, so.HullTint.W, "the Cordrazine paint no hulls, so their ship is drawn bare");

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

    // a ship passes its hull's race to its owner's colors, so only the owner's own race's hulls light up in them
    [TestMethod]
    public void AShipOfTheOwnersRaceDrawsItsLightsInItsColorAndAnotherRacesDoesNot()
    {
        LoadStarterShips("Cordrazine Small Station Mk1", "Terran-Prototype");
        CreateUniverseAndPlayerEmpire("Cordrazine");
        TestShip own = SpawnShip("Cordrazine Small Station Mk1", Player, new Vector2(1000f, 1000f));
        TestShip other = SpawnShip("Terran-Prototype", Player, new Vector2(5000f, 1000f));
        Assert.AreEqual("Cordrazine", own.ShipData.BaseHull.Style, "setup: a Cordrazine hull");
        SceneObject ownSO = GiveSceneObject(own), otherSO = GiveSceneObject(other);
        own.Update(TestSimStep);
        other.Update(TestSimStep);
        Assert.AreEqual(Player.LightsTint("Cordrazine"), ownSO.LightsTint, "the Cordrazine hull's lights take the player's color");
        AssertEqual(0.001f, 1f, ownSO.LightsTint.W, "setup: the player's color is on");
        Assert.AreEqual(0f, otherSO.LightsTint.W, "the Terran hull's keep theirs");
    }

    // a wreck of the owner's own race keeps its last owner's color on its lights, on a model handed over at death or loaded later
    [TestMethod]
    public void ADerelictOfTheOwnersRaceKeepsItsLightsColor()
    {
        LoadStarterShips("Cordrazine Small Station Mk1");
        CreateUniverseAndPlayerEmpire("Cordrazine");
        Universe.Particles = new ParticleManager(Content);
        Universe.Frustum = new BoundingFrustum(Matrix.CreateOrthographicOffCenter(0, 1_000_000, 1_000_000, 0, -10_000, 10_000));
        UState.ViewState = UniverseScreen.UnivScreenState.PlanetView;
        Vector4 color = Player.LightsTint("Cordrazine");
        AssertEqual(0.001f, 1f, color.W, "setup: the player's Cordrazine hulls are colored");

        TestShip ship = SpawnShip("Cordrazine Small Station Mk1", Player, new Vector2(200_000f));
        var so = new SceneObject(ship.Name);
        var wreck = new Derelict(ship, so, salvageable: false, seen: true);
        wreck.Update(TestSimStep);
        AssertEqual(GlobalStats.ShipVisibility, so.Visibility, "setup: on screen, it is drawn");
        Assert.AreEqual(color, so.LightsTint, "its lights keep the player's color");

        AssertTrue(UState.Derelicts.TryAdd(SpawnShip("Cordrazine Small Station Mk1", Player, new Vector2(220_000f)), salvageable: false, seen: false),
                   "setup: another wreck of the player's, with no model");
        var loaded = new SceneObject("loaded");
        AssertTrue(UState.Derelicts.TryAttachModel(UState.Derelicts.ToArray().Last(), loaded), "setup: a model loaded later is attached");
        Assert.AreEqual(color, loaded.LightsTint, "so do the lights of one whose model loads later");
    }

    // a space port built with its race's model lights up as a hull of that race, and with the common model it does not
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ASpacePortBuiltWithItsRacesModelLightsUpInTheOwnersColor(bool raceModel)
    {
        CreateUniverseAndPlayerEmpire("Cordrazine");
        if (!raceModel)
            Player.data.SpacePortModel = null;
        Planet home = AddHomeWorldToEmpire(new Vector2(400_000f), Player);
        var station = new SpaceStation();
        station.CreateSceneObject(home, Player);
        var outer = (SceneObject)typeof(SpaceStation).GetField("OuterSO", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(station);
        Assert.IsNotNull(outer, "setup: the port's model loaded");
        AssertEqual(0.001f, raceModel ? 1f : 0f, outer.LightsTint.W,
                    raceModel ? "the Cordrazine port's lights take the color" : "the common port's keep theirs");
        station.RemoveSceneObject();
    }

    // a space port takes its owner's colors as a hull of the race whose model it shows
    [TestMethod]
    [DataRow("Cordrazine", 1f)]
    [DataRow(null, 0f)]
    public void ASpacePortTakesItsOwnersColorsAsAHullOfItsModelsRace(string modelStyle, float lit)
    {
        CreateUniverseAndPlayerEmpire("Cordrazine");
        Planet home = AddHomeWorldToEmpire(new Vector2(400_000f), Player);
        var station = new SpaceStation();
        var so = new SceneObject("station");
        typeof(SpaceStation).GetField("OuterSO", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(station, so);
        typeof(SpaceStation).GetField("ModelStyle", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(station, modelStyle);

        station.UpdateVisibleStation(home, TestSimStep);
        AssertEqual(0.001f, lit, so.LightsTint.W, modelStyle == null ? "the common model keeps its lights" : "the Cordrazine model's lights take the color");
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
