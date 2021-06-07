using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            LoadStarterShips(ResourceManager.TestOptions.AllStarterShips);
        }
        
        [TestMethod]
        public void EnsureSaveGameIntegrity()
        {
            CreateDeveloperSandboxUniverse("United", numOpponents:1);

            
        }
    }
}
