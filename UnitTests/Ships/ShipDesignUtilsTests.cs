using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    [TestClass]
    public class ShipDesignUtilsTests : StarDriveTest
    {
        void LoadShips()
        {
            ResourceManager.LoadHullData();
            ReloadStarterShips();
            ReloadTechTree();
            LoadStarterShips("Excalibur-Class Supercarrier", "Medium Freighter", "Flak Corvette", "Laserclaw",
                             "Ving Defender", "Corsair", "Alliance-Class Mk Ia Hvy Assault", "Assault Shuttle");
            //OnlyLoadShips("Vulcan Scout");
        }

        [ClassCleanup]
        public static void Teardown()
        {
            ReloadStarterShips();
        }

        static string[] TechsNeeded(ShipData data)
        {
            string[] techs = data.TechsNeeded.ToArray();
            Array.Sort(techs);
            return techs;
        }

        static Ship GetFirstShipTemplate() => ResourceManager.GetShipTemplates().ToArray()[0];
        static ShipData GetFirstShipData() => GetFirstShipTemplate().shipData;
        static ShipData GetFirstBaseHull() => GetFirstShipData().BaseHull;
        static string GetTechsNeededStr(ShipData data) => string.Join(",", TechsNeeded(data));

        void PrintInfo(string prefix)
        {
            ShipData firstBase = GetFirstBaseHull();
            ShipData firstShip = GetFirstShipData();
            Log.Info($"{prefix} Hull {firstBase.Name} UnLockable: {firstBase.UnLockable} TechsNeeded: {GetTechsNeededStr(firstBase)}");
            Log.Info($"{prefix} Ship {firstShip.Name} UnLockable: {firstShip.UnLockable} TechsNeeded: {GetTechsNeededStr(firstShip)}");
        }

        [TestMethod]
        public void MarkDesignsUnlockableMatchesLegacyBehaviour()
        {
            // NOTE: This 'redundant' step here exposes a rare issue
            //       where Root techs are accidentally added to ShipData.TechsNeeded
            LoadShips();
            ShipDesignUtils.MarkDesignsUnlockable();
            PrintInfo("1");

            // mark unlockable using the old legacy utility which we are imitating
            LoadShips();
            ShipDesignUtilsOld.MarkDesignsUnlockable();
            PrintInfo("2");

            var legacyUnlockable = new Dictionary<string, ShipData>();
            foreach (Ship tOld in ResourceManager.GetShipTemplates())
            {
                legacyUnlockable.Add(tOld.Name, tOld.shipData.GetClone()); //Because it gets disposed later on and we get a NPE
            }

            // now try with the new optimized algorithm and make sure it matches
            LoadShips();
            ShipDesignUtils.MarkDesignsUnlockable();
            PrintInfo("3");

            foreach (Ship template in ResourceManager.GetShipTemplates())
            {
                if (legacyUnlockable.TryGetValue(template.Name, out ShipData legacy))
                {
                    ShipData @new = template.shipData;

                    Assert.That.Equal(TechsNeeded(legacy), TechsNeeded(@new),
                                      $"{template.Name} TechsNeeded must be equal");
                    
                    Assert.AreEqual(legacy.UnLockable, @new.UnLockable, $"{template.Name} Not same Unlockable");
                    Assert.AreEqual(legacy.AllModulesUnlockable, @new.AllModulesUnlockable, $"{template.Name} Not same AllModulesUnlockable");
                    Assert.AreEqual(legacy.HullUnlockable, @new.HullUnlockable,$"{template.Name} Not same HullUnlockable");

                    Assert.AreEqual(legacy.BaseStrength, @new.BaseStrength,$"{template.Name} Not same BaseStrength");
                    
                    //${string.Join("",oldShipData.TechsNeeded)} : ${string.Join("",template.shipData.TechsNeeded)}
                    Assert.IsTrue(legacy.TechsNeeded.SetEquals(@new.TechsNeeded), $"{template.Name} Not SetEquals TechsNeeded");
                } 
                else
                {
                    throw new AssertFailedException($"{template.Name} Not found after ReloadStarterShips");
                }
            }
        }
    }
}
