using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
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
            LoadStarterShips("Heavy Carrier mk5-b",
                             "Medium Freighter",
                             "Fang Strafer",
                             "Ving Defender",
                             "Corsair",
                             "TEST_Heavy Carrier mk1",
                             "Assault Shuttle");
            //OnlyLoadShips("Vulcan Scout");
        }

        [ClassCleanup]
        public static void Teardown()
        {
            ReloadStarterShips();
        }

        static string[] ToArray(HashSet<string> techsNeeded)
        {
            string[] techs = techsNeeded.ToArr();
            Array.Sort(techs);
            return techs;
        }

        static IShipDesign GetFirstShipData() => ResourceManager.Ships.Designs[0];
        static ShipHull GetFirstBaseHull() => GetFirstShipData().BaseHull;
        static string ToString(HashSet<string> techsNeeded) => string.Join(",", ToArray(techsNeeded));

        void PrintInfo(string prefix)
        {
            ShipHull firstBase = GetFirstBaseHull();
            IShipDesign firstShip = GetFirstShipData();
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

            var legacyUnlockable = new Dictionary<string, ShipDesign>();
            foreach (IShipDesign tOld in ResourceManager.Ships.Designs)
            {
                legacyUnlockable.Add(tOld.Name, tOld.GetClone(null)); //Because it gets disposed later on and we get a NPE
            }

            // now try with the new optimized algorithm and make sure it matches
            LoadShips();
            ShipDesignUtils.MarkDesignsUnlockable();
            PrintInfo("3");

            foreach (IShipDesign @new in ResourceManager.Ships.Designs)
            {
                if (legacyUnlockable.TryGetValue(@new.Name, out ShipDesign legacy))
                {
                    AssertEqual(ToArray(legacy.TechsNeeded), ToArray(@new.TechsNeeded),
                                      $"{@new.Name} TechsNeeded must be equal");
                    AssertEqual(legacy.Unlockable, @new.Unlockable, $"{@new.Name} Not same Unlockable");
                } 
                else
                {
                    throw new AssertFailedException($"{@new.Name} Not found after ReloadStarterShips");
                }
            }
        }
    }
}
