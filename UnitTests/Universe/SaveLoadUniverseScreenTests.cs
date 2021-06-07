using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.GameScreens.LoadGame;
using System.IO;

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
        }
        
        [TestMethod]
        public void EnsureSaveGameIntegrity()
        {
            CreateDeveloperSandboxUniverse("United", numOpponents:1);
            SavedGame save1 = Universe.Save("UnitTest.IntegrityTest", async:false);
            if (save1 == null) throw new AssertFailedException("Save1 failed");
            DestroyUniverse();
            SavedGame.UniverseSaveData snap1 = save1.SaveData;

            UniverseScreen us = LoadGame.Load(save1.PackedFile, noErrorDialogs:true);
            SavedGame save2 = us.Save("UnitTest.IntegrityTest", async:false);
            if (save1 == null) throw new AssertFailedException("Save2 failed");
            DestroyUniverse();
            SavedGame.UniverseSaveData snap2 = save2.SaveData;

            Assert.That.MemberwiseEqual(snap1, snap2, "SaveGame did not load correctly");
        }
    }
}
