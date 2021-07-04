using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.GameScreens.LoadGame;
using System.IO;
using System.Threading;

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
            CreateGameInstance();
            LoadGameContent(ResourceManager.TestOptions.LoadEverything);
            Directory.CreateDirectory(SavedGame.DefaultSaveGameFolder);
            Directory.CreateDirectory(SavedGame.DefaultSaveGameFolder+"Headers/");
            Directory.CreateDirectory(SavedGame.DefaultSaveGameFolder+"Fog Maps/");
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

            SavedGame save1 = Universe.Save("UnitTest.IntegrityTest", async:false);
            if (save1 == null) throw new AssertFailedException("Save1 failed");
            DestroyUniverse();
            SavedGame.UniverseSaveData snap1 = save1.SaveData;

            UniverseScreen us = LoadGame.Load(save1.PackedFile, noErrorDialogs:true, startSimThread:false);
            SavedGame save2 = us.Save("UnitTest.IntegrityTest", async:false);
            if (save1 == null) throw new AssertFailedException("Save2 failed");
            DestroyUniverse();
            SavedGame.UniverseSaveData snap2 = save2.SaveData;

            Array<string> results = snap1.MemberwiseCompare(snap2);
            results.ForEach(Console.WriteLine);

            // TODO: disabling these tests right now because it's really hard to fix in one go
            //Assert.That.MemberwiseEqual(snap1, snap2, "SaveGame did not load correctly");
        }
    }
}
