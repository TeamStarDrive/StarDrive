using System.Linq;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies
{
    public struct DynamicCrashSite // Created by Fat Bastard, Nov 2020
    {
        public Empire Loyalty { get; private set; }
        public string ShipName { get; private set; }
        public int NumTroopsSurvived { get; private set; }
        public bool Active { get; private set; }
        public string TroopName { get; private set; }
        public bool RecoverShip { get; private set; }

        public DynamicCrashSite(bool active)
        {
            Active            = active;
            Loyalty           = null;
            ShipName          = "";
            TroopName         = "";
            NumTroopsSurvived = 0;
            RecoverShip       = false;
        }

        public void CrashShip(Empire empire, string shipName, string troopName, int numTroopsSurvived,
            Planet p, PlanetGridSquare tile, int shipSize,  bool fromSave = false)
        {
            if (!TryCreateCrashSite(p, tile, out string message))
                return;

            Active            = true;
            Loyalty           = empire;
            ShipName          = shipName;
            TroopName         = troopName;
            NumTroopsSurvived = numTroopsSurvived;

            if (!fromSave)
            {
                p.SetInGroundCombat(p.Owner);
                NotifyPlayerAndAi(p, message, shipSize);
                RecoverShip = RecoverChance();
            }
        }

        public void CrashShip(SavedGame.PGSData d, Planet p, PlanetGridSquare tile)
        {
            Empire e = EmpireManager.GetEmpireById(d.CrashSiteEmpireId);
            // ShipSize 0 since it is not relevant when called from save
            CrashShip(e, d.CrashSiteShipName, d.CrashSiteTroopName, d.CrashSiteTroops, p, tile, 0, true);
            RecoverShip = d.CrashSiteRecoverShip;
        }

        bool TryCreateCrashSite(Planet p, PlanetGridSquare tile, out string message)
        {
            message    = $"A ship has crashed landed on {p.Name}.";
            Building b = ResourceManager.CreateBuilding("Dynamic Crash Site");

            if (b == null)
                return false;

            if (tile.BuildingOnTile)
            {
                message = $"{message}\n Unfortunately, it crashed on the {tile.Building.TranslatedName.Text}.";
                p.DestroyBuildingOn(tile);
            }

            if (tile.QItem?.isBuilding == true)
            {
                message = $"{message}\nUnfortunately, it crashed on the {tile.QItem.Building.TranslatedName.Text}\n" +
                          "Construction site.";

                p.Construction.Cancel(tile.QItem, refund: false);
            }

            if (tile.TroopsAreOnTile)
            {
                message = $"{message}\nTroops were killed.";
                tile.KillAllTroops(p);
            }

            tile.PlaceBuilding(b, p);
            return true;
        }

        void NotifyPlayerAndAi(Planet p, string message, int shipSize)
        {
            foreach (Empire e in EmpireManager.ActiveMajorEmpires)
            {
                var ships = e.OwnedShips;
                if (p.Owner == e 
                    || p.Owner == null && p.IsExploredBy(e)
                                       && ships.Any(s => s?.Center.InRadius(p.Center, s.SensorRange) == true))
                {
                    if (e.isPlayer)
                        Empire.Universe.NotificationManager.AddShipCrashed(p, message);
                    else
                        AiProcessCrashSite(p, e, shipSize);
                }
            }
        }

        void AiProcessCrashSite(Planet p, Empire e, int shipSize)
        {
            if (p.Owner == null)
            {
                if (!p.ParentSystem.OwnerList.ToArray().Any(empire => empire.IsNAPactWith(e)) && shipSize >= 100)
                    e.GetEmpireAI().SendExplorationFleet(p);
            }
            else if (!p.TroopsInTheWorks && !p.AnyOfOurTroops(e) && !p.SpaceCombatNearPlanet) // owner is this empire
            {
                SendTroop(e, p);
            }
        }

        void SendTroop(Empire e, Planet p)
        {
            if (e.GetTroopShipForRebase(out Ship troopShip, p.Center, p.Name))
                troopShip.AI.OrderLandAllTroops(p);
            else
                e.GetEmpireAI().SendExplorationFleet(p); // Create a task to be processed normally
        }

        public void ActivateSite(Planet p, Empire activatingEmpire, PlanetGridSquare tile)
        {
            Active              = false;
            Empire owner        = p.Owner ?? activatingEmpire;
            string troopMessage = "";
            Ship ship           = SpawnShip(p, activatingEmpire, owner, out string message);

            if (ship != null)
                SpawnSurvivingTroops(p, owner, tile, out troopMessage);

            if (owner.isPlayer || !owner.isPlayer && Loyalty.isPlayer && NumTroopsSurvived > 0)
                Empire.Universe.NotificationManager.AddShipRecovered(p, ship, $"{message}{troopMessage}");

            p.DestroyBuildingOn(tile);
        }

        Ship SpawnShip(Planet p, Empire activatingEmpire, Empire owner, out string message)
        {
            message = $"Recover efforts of a crashed ship on {p.Name} were futile.\n" +
                      "It was completely wrecked.";

            Ship template = ResourceManager.GetShipTemplate(ShipName, false);
            if (template == null)
                return null;

            if (RecoverShip)
            {
                string otherOwners   = owner.isPlayer ? ".\n" : $" by {owner.Name}.\n";
                Ship ship            = Ship.CreateShipAt(ShipName, activatingEmpire, p, true);
                message              = $"Ship ({ShipName}) was recovered from the\nsurface of {p.Name}{otherOwners}";
                float damageModifier = activatingEmpire == Loyalty ? 0.8f : 1; // If it was our ship, spawn with less damage.
                ship.DamageByRecoveredFromCrash(damageModifier);
                return ship;
            }

            float recoverAmount = template.BaseCost / 10;
            if (owner == activatingEmpire)
            {
                p.ProdHere  = (p.ProdHere + recoverAmount).UpperBound(p.Storage.Max);
                message     = $"We were able to recover {recoverAmount.String(0)} production\n" +
                              $"from a crashed ship on {p.Name}.\n";
            }
            else
            {
                activatingEmpire.AddMoney(template.BaseCost / 10);
                message = $"We were able to recover {recoverAmount.String(0)} credits\n" +
                          $"from a crashed ship on {p.Name}.\n";
            }

            return null;
        }

        void SpawnSurvivingTroops(Planet p, Empire owner, PlanetGridSquare tile, out string message)
        {
            Relationship rel = null;
            message          = "The Crew was perished.";

            if (Loyalty != owner)
                rel = owner.GetRelations(Loyalty);

            if (rel?.AtWar == false && rel.CanAttack && !Loyalty.isFaction)
            {
                NumTroopsSurvived = 0;
                return; // Dont spawn troops, risking war
            }

            bool shouldLandTroop = Loyalty == owner || rel?.AtWar == true || Loyalty.isFaction;
            for (int i = 1; i <= NumTroopsSurvived; i++)
            {
                Troop t = ResourceManager.CreateTroop(TroopName, Loyalty);
                t.SetOwner(Loyalty);
                if (!shouldLandTroop || !t.TryLandTroop(p, tile))
                {
                    Ship ship = t.Launch(p);
                    ship.AI.OrderRebaseToNearest();
                }
            }

            if (NumTroopsSurvived == 0)
                return;

            bool playerTroopsRecovered = Loyalty == EmpireManager.Player && owner != EmpireManager.Player;
            if (Loyalty == owner)
            {
                message = "Friendly Troops have Survived.";
            }
            else if (rel?.AtWar == true)
            {
                message = playerTroopsRecovered
                    ? "Our Troops are in combat there!."
                    : "Hostile troops survived and are attacking!";
            }
            else
            {
                message = playerTroopsRecovered
                    ? "Our Troops and are heading home."
                    : "Neutral troops survived and are\nheading home.";
            }
        }

        bool RecoverChance()
        {
            Ship template = ResourceManager.GetShipTemplate(ShipName, false);
            if (template == null || template.IsPlatformOrStation)
                return false;

            float chance = 20 * (1 + Loyalty.data.Traits.ModHpModifier);
            if (Loyalty.WeAreRemnants)
                chance -= template.SurfaceArea / 15f; // Remnants tend to self destruct

            return RandomMath.RollDice(chance.Clamped(1, 50))
                && !template.IsConstructor
                && !template.IsDefaultTroopTransport;
        }

        public static void UpdateCrater(PlanetGridSquare tile, Planet planet)
        {
            if (!RandomMath.RollDice(2))
                return;

            // Remove the Crater
            string path = tile.BuildingOnTile ? tile.Building.IconPath64 : "";
            if (planet.Owner == EmpireManager.Player)
                Empire.Universe.NotificationManager.AddMeteorRelated(planet, Localizer.Token(GameText.AMeteorCraterWasFlattened), path);

            planet.DestroyBuildingOn(tile);
        }
    }
}
