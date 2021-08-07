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
            ResourceManager.LoadHulls();
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

        static string[] ToArray(HashSet<string> techsNeeded)
        {
            string[] techs = techsNeeded.ToArray();
            Array.Sort(techs);
            return techs;
        }

        static Ship GetFirstShipTemplate() => ResourceManager.GetShipTemplates().ToArray()[0];
        static ShipData GetFirstShipData() => GetFirstShipTemplate().shipData;
        static ShipHull GetFirstBaseHull() => GetFirstShipData().BaseHull;
        static string ToString(HashSet<string> techsNeeded) => string.Join(",", ToArray(techsNeeded));

        void PrintInfo(string prefix)
        {
            ShipHull firstBase = GetFirstBaseHull();
            ShipData firstShip = GetFirstShipData();
            Log.Info($"{prefix} Hull {firstBase.HullName} Unlockable: {firstBase.Unlockable} TechsNeeded: {ToString(firstBase.TechsNeeded)}");
            Log.Info($"{prefix} Ship {firstShip.Name} Unlockable: {firstShip.Unlockable} TechsNeeded: {ToString(firstShip.TechsNeeded)}");
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
                    Assert.That.Equal(ToArray(legacy.TechsNeeded), ToArray(@new.TechsNeeded),
                                      $"{template.Name} TechsNeeded must be equal");
                    Assert.AreEqual(legacy.Unlockable, @new.Unlockable, $"{template.Name} Not same Unlockable");
                } 
                else
                {
                    throw new AssertFailedException($"{template.Name} Not found after ReloadStarterShips");
                }
            }
        }
    }
}
