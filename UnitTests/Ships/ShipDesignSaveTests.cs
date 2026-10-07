using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
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

    [TestMethod]
    public void AReservedDesignFileIsNamedInTheOverwriteQuestion()
    {
        IShipDesign design = ResourceManager.Ships.GetDesign("Terran-Prototype");
        Assert.IsNotNull(design.Source);

        string question = ShipDesignSaveScreen.OverwriteQuestion(design.Name, hull: false, null, reserved: true, design.Source);
        StringAssert.Contains(question, design.Source.Name);
    }
}
