using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.GameScreens.LoadGame;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Serialization
{
    [TestClass]
    public class SavedBuildingTextIdTests : StarDriveTest
    {
        [TestMethod]
        public void ALoadedBuildingTakesItsTextIdsFromTheCurrentTemplate()
        {
            CreateUniverseAndPlayerEmpire();
            UState.StarDate = 1042.5f; // past the start, so loading does not hand out starting ships
            Planet home = AddHomeWorldToEmpire(new Vector2(2000), Player);
            Assert.IsTrue(home.NumBuildings > 0, "setup: the homeworld must have a building");
            Building saved = home.Buildings[0];
            Building template = ResourceManager.GetBuildingTemplate(saved.Name);

            // the ids an older save would carry after a mod renumbered its text
            saved.NameTranslationIndex = template.NameTranslationIndex + 12345;
            saved.DescriptionIndex = template.DescriptionIndex + 12345;
            saved.ShortDescriptionIndex = template.ShortDescriptionIndex + 12345;

            SavedGame save = Universe.Save("UnitTest.BuildingTextIds", throwOnError: true);
            UniverseScreen loaded = LoadGame.Load(save.SaveFile, noErrorDialogs: true, startSimThread: false);
            Assert.IsNotNull(loaded, "the save must load");
            Planet loadedHome = loaded.UState.Player.GetPlanets().FirstOrDefault(p => p.Name == home.Name);
            Assert.IsNotNull(loadedHome, "setup: the homeworld must survive the round trip");
            Building restored = loadedHome.FindBuilding(b => b.Name == saved.Name);

            Assert.IsNotNull(restored, "the building must survive the round trip");
            AssertEqual(template.NameTranslationIndex, restored.NameTranslationIndex, "name id comes from the template");
            AssertEqual(template.DescriptionIndex, restored.DescriptionIndex, "description id comes from the template");
            AssertEqual(template.ShortDescriptionIndex, restored.ShortDescriptionIndex, "short description id comes from the template");
        }
    }
}
