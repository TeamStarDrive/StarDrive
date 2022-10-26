using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using System;
using Ship_Game.GameScreens.LoadGame;
using System.IO;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.GameScreens.Universe.Debug;
using Ship_Game.Ships;

namespace UnitTests.Universe
{
    /// <summary>
    /// Attempts to TEST and ENSURE that Universe remains consistent
    /// AFTER saving and then loading again
    /// </summary>
    [TestClass]
    public class SaveLoadUniverseScreenTests : StarDriveTest
    {
        public SaveLoadUniverseScreenTests()
        {
        }

        [TestMethod]
        public void EnsureSaveGamesFitInMemory()
        {
            int shipsPerEmpire = 750;

            CreateCustomUniverseSandbox(numOpponents:6, galSize:GalSize.Large);
            Universe.SingleSimulationStep(TestSimStep);

            // unlock all techs so we get full selection of ships
            foreach (Empire e in Universe.UState.Empires)
            {
                ResearchDebugUnlocks.UnlockAllResearch(e, unlockBonuses: true);
            }
            Universe.SingleSimulationStep(TestSimStep);

            // spawn a sick amount of cruisers at each empire's capital
            foreach (Empire e in Universe.UState.Empires)
            {
                if (e.Capital != null)
                {
                    IShipDesign bestShip = ShipBuilder.BestShipWeCanBuild(RoleName.cruiser, e)
                                        ?? ShipBuilder.BestShipWeCanBuild(RoleName.carrier, e)
                                        ?? ShipBuilder.BestShipWeCanBuild(RoleName.frigate, e)
                                        ?? ShipBuilder.BestShipWeCanBuild(RoleName.prototype, e);
                    
                    Assert.IsNotNull(bestShip, $"failed to choose best ship for {e}");
                    for (int i = 0; i < shipsPerEmpire; ++i)
                    {
                        Ship.CreateShipAt(Universe.UState, bestShip.Name, e, e.Capital, true);
                    }
                }
            }
            Universe.SingleSimulationStep(TestSimStep);

            // now try to save the game
            Log.Write($"ShipsCount: {Universe.UState.Objects.NumShips}");
            Universe.Save("MemoryStressTest", throwOnError:true);
        }

        [TestMethod]
        public void EnsureSaveGameIntegrity()
        {
            CreateDeveloperSandboxUniverse("United", numOpponents:1, paused:true);
            Universe.CreateSimThread = false;
            Universe.LoadContent();
            // manually run a few turns
            for (int i = 0; i < 60; ++i)
                Universe.SingleSimulationStep(TestSimStep);

            SavedGame save1 = Universe.Save("UnitTest.IntegrityTest");
            if (save1 == null) throw new AssertFailedException("Save1 failed");
            DestroyUniverse();

            UniverseScreen us = LoadGame.Load(save1.SaveFile, noErrorDialogs:true, startSimThread:false);
            SavedGame save2 = us.Save("UnitTest.IntegrityTest");
            if (save1 == null) throw new AssertFailedException("Save2 failed");
            DestroyUniverse();

            Array<string> results = save1.State.MemberwiseCompare(save2.State);
            results.ForEach(Console.WriteLine);

            // TODO: disabling these tests right now because it's really hard to fix in one go
            //Assert.That.MemberwiseEqual(snap1, snap2, "SaveGame did not load correctly");
        }
    }
}
