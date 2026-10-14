using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using Ship_Game.Ships;
using UnitTests.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships;

[TestClass]
public class ShipDesignSaveTests : StarDriveTest
{
    public ShipDesignSaveTests()
    {
        CreateUniverseAndPlayerEmpire();
        UState.StarDate = 1001f; // a game saved at 1000 creates the starting ships when loaded, and no empire here has a planet
    }

    [TestMethod]
    public void ADesignLoadedFromASaveGetsAnOverwriteQuestionWithoutAFile()
    {
        const string name = "ZzSaveOnlyDesign";
        try
        {
            ShipDesign design = ResourceManager.Ships.GetDesign("Terran-Prototype").GetClone(name);
            SpawnShip(Ship.CreateNewShipTemplate(Empire.Void, design), Player, Vector2.Zero);
            BinarySerializerTests.SerDes(UState);

            Assert.IsTrue(ResourceManager.Ships.GetDesign(name, out IShipDesign fromSave));
            Assert.IsTrue(fromSave.IsReadonlyDesign);
            Assert.IsNull(fromSave.Source);

            string question = ShipDesignSaveScreen.OverwriteQuestion(name, hull: false, null, fromSave.IsReadonlyDesign, fromSave.Source);
            StringAssert.Contains(question, "was loaded from this save");
        }
        finally
        {
            ResourceManager.Ships.Delete(name);
        }
    }

    // saves a design, the game, then another design under that name; returns their sources, as a replaced template is disposed
    (ShipDesign Saved, ShipDesign Changed) SaveADesignThenChangeIt(string name, out SavedGame save, bool inUse, bool playerDesign = true)
    {
        ShipDesign[] buildable = Player.ShipsWeCanBuildSnapshot.OfType<ShipDesign>().Where(d => Player.WeCanBuildThis(d)).ToArray();
        ShipDesign savedFrom = buildable[0];
        ShipDesign changedFrom = buildable.First(d => !d.AreModulesEqual(savedFrom));
        ResourceManager.AddShipTemplate(savedFrom.GetClone(name), playerDesign: playerDesign, readOnly: !playerDesign);
        Player.UpdateShipsWeCanBuild();
        Assert.IsTrue(Player.CanBuildShip(name), "setup: the player can build the design when the game is saved");
        if (inUse)
            SpawnShip(name, Player, Vector2.Zero);

        save = Universe.Save("UnitTest.ChangedDesign", throwOnError: true);
        ResourceManager.AddShipTemplate(changedFrom.GetClone(name), playerDesign: playerDesign, readOnly: !playerDesign);
        return (savedFrom, changedFrom);
    }

    [TestMethod]
    public void AnOlderCopyOfAShippedDesignInASaveStaysAShippedDesign()
    {
        const string name = "ZzShippedChangedAfterSave";
        try
        {
            (ShipDesign saved, _) = SaveADesignThenChangeIt(name, out SavedGame save, inUse: false, playerDesign: false);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

            IShipDesign design = ResourceManager.Ships.GetDesign(name);
            Assert.IsTrue(design.AreModulesEqual(saved), "setup: the save keeps the shipped design the game was played with");
            Assert.IsFalse(design.IsPlayerDesign, "its copy is still a shipped design, not one of the player's");
            Assert.IsTrue(loaded.UState.Player.CanBuildShip(design), "and the empire can still build it");
        }
        finally
        {
            ResourceManager.Ships.Delete(name);
        }
    }

    [TestMethod]
    public void APlayerDesignChangedAfterASaveStaysEditableAfterLoadingThatSave()
    {
        const string name = "ZzChangedAfterSave";
        try
        {
            (_, ShipDesign changed) = SaveADesignThenChangeIt(name, out SavedGame save, inUse: false);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

            IShipDesign design = ResourceManager.Ships.GetDesign(name);
            Assert.IsFalse(design.IsReadonlyDesign, "the save does not turn the player's own design into a reserved one");
            Assert.IsTrue(design.AreModulesEqual(changed), "the design is the one the player saved last");
            Assert.IsTrue(loaded.UState.Player.CanBuildShip(design), "and the player can still build it");
        }
        finally
        {
            ResourceManager.Ships.Delete(name);
        }
    }

    [TestMethod]
    public void APlayerDesignInUseIsLoadedAsItsShipsWereBuilt()
    {
        const string name = "ZzInUseChangedAfterSave";
        try
        {
            (ShipDesign saved, _) = SaveADesignThenChangeIt(name, out SavedGame save, inUse: true);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

            IShipDesign design = ResourceManager.Ships.GetDesign(name);
            Assert.IsTrue(design.AreModulesEqual(saved), "the save keeps the design its ships were built from");
            Assert.IsTrue(loaded.UState.Player.CanBuildShip(design), "and the player can build it");
        }
        finally
        {
            ResourceManager.Ships.Delete(name);
        }
    }

    [TestMethod]
    public void AQueuedPlayerDesignIsBuiltFromTheDesignAfterALoad()
    {
        const string name = "ZzQueuedDesign";
        try
        {
            Planet home = AddHomeWorldToEmpire(new Vector2(200_000), Player);
            ShipDesign source = Player.ShipsWeCanBuildSnapshot.OfType<ShipDesign>().First(d => Player.WeCanBuildThis(d));
            ResourceManager.AddShipTemplate(source.GetClone(name), playerDesign: true, readOnly: false);
            Player.UpdateShipsWeCanBuild();
            home.Construction.Enqueue(ResourceManager.Ships.GetDesign(name), QueueItemType.CombatShip);

            SavedGame save = Universe.Save("UnitTest.QueuedDesign", throwOnError: true);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

            QueueItem queued = loaded.UState.GetPlanet(home.Id).ConstructionQueue.First(qi => qi.isShip);
            Assert.AreSame(ResourceManager.Ships.GetDesign(name), queued.ShipData, "the queue builds the design, not a copy the save brought");
        }
        finally
        {
            ResourceManager.Ships.Delete(name);
        }
    }

    [TestMethod]
    public void TheEmpiresCanBuildAfterALoadWhatTheyCouldBuildBefore()
    {
        IShipDesign given = ResourceManager.Ships.Designs.First(d => !d.IsPlayerDesign && !Enemy.WeCanBuildThis(d));
        Enemy.AddBuildableShip(given);
        string[] player = Player.ShipsWeCanBuildIds, enemy = Enemy.ShipsWeCanBuildIds;
        Assert.IsTrue(player.Length > 0, "setup: the player can build ships");

        SavedGame save = Universe.Save("UnitTest.BuildLists", throwOnError: true);
        UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);

        CollectionAssert.IsSubsetOf(player, loaded.UState.Player.ShipsWeCanBuildIds, "the player's build list");
        CollectionAssert.IsSubsetOf(enemy, loaded.UState.GetEmpireByName(Enemy.Name).ShipsWeCanBuildIds,
                                    "the AI's build list, with a design it was given that its technology does not unlock");
    }

    [TestMethod]
    public void AReservedDesignFileIsNamedInTheOverwriteQuestion()
    {
        IShipDesign design = ResourceManager.Ships.GetDesign("Terran-Prototype");
        Assert.IsNotNull(design.Source);

        string question = ShipDesignSaveScreen.OverwriteQuestion(design.Name, hull: false, null, reserved: true, design.Source);
        StringAssert.Contains(question, design.Source.Name);
    }
}
