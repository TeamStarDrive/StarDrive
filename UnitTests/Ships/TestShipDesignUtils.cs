using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class TestShipDesignUtils : StarDriveTest
    {
        [TestMethod]
        public void TestMarkUnlockableMatchesOldBehavior()
        {
            ShipDesignUtilsOld.MarkDesignsUnlockable(new ProgressCounter());

            var map = new Dictionary<string, ShipData>();
            foreach (var tOld in ResourceManager.GetShipTemplates())
            {
                map.Add(tOld.Name, tOld.shipData.GetClone()); //Because it gets disposed later on and we get a NPE
            }
            
            StarDriveTestContext.ReloadStarterShips();
            var templates = ResourceManager.GetShipTemplates();
            ShipDesignUtils.MarkDesignsUnlockable(new ProgressCounter());
            
            foreach (var template in templates)
            {
                if (map.TryGetValue(template.Name, out var legacy)) {

                    //if (template.Name == "Prototype Frigate") continue;
                    ShipData shipData = template.shipData;
                    
                    Assert.AreEqual(legacy.UnLockable, shipData.UnLockable, $"{template.Name} Not same Unlockable");
                    Assert.AreEqual(legacy.AllModulesUnlockable, shipData.AllModulesUnlockable, $"{template.Name} Not same AllModulesUnlockable");
                    Assert.AreEqual(legacy.HullUnlockable, shipData.HullUnlockable,$"{template.Name} Not same HullUnlockable");

                    Assert.AreEqual(legacy.TechScore, shipData.TechScore,$"{template.Name} Not same TechScore");
                    Assert.AreEqual(legacy.BaseStrength, shipData.BaseStrength,$"{template.Name} Not same BaseStrength");
                    
                    //${string.Join("",oldShipData.TechsNeeded)} : ${string.Join("",template.shipData.TechsNeeded)}
                    Assert.IsTrue(legacy.TechsNeeded.SetEquals(shipData.TechsNeeded), $"{template.Name} Not SetEquals TechsNeeded");
                } 
                else
                {
                    throw new AssertFailedException($"{template.Name} Not found after ReloadStarterShips");
                }
                


            }
            
        }
    }
}
