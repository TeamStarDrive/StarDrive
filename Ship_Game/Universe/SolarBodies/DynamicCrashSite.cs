using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class DynamicCrashSite // Created by Fat Bastard, Nov 2020
    {
        [StarData] public Empire Loyalty { get; private set; }
        [StarData] public string ShipName { get; private set; }
        [StarData] public int NumTroopsSurvived { get; private set; }
        [StarData] public bool Active { get; private set; }
        [StarData] public string TroopName { get; private set; }
        [StarData] public bool RecoverShip { get; private set; }

        [StarDataConstructor]
        private DynamicCrashSite() {}

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
                              Planet p, PlanetGridSquare tile, int shipSize)
        {
            if (!TryCreateCrashSite(p, tile, out string message))
                return;

            Active            = true;
            Loyalty           = empire;
            ShipName          = shipName;
            TroopName         = troopName;
            NumTroopsSurvived = numTroopsSurvived;

            p.SetInGroundCombat(p.Owner);
            NotifyPlayerAndAi(p, message, shipSize);
            RecoverShip = RecoverChance();
        }

        bool TryCreateCrashSite(Planet p, PlanetGridSquare tile, out string message)
        {
            message    = $"A ship has crashed landed on {p.Name}.";
            Building b = ResourceManager.CreateBuilding(p, "Dynamic Crash Site");

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
            foreach (Empire e in p.Universe.ActiveMajorEmpires)
            {
                var ships = e.OwnedShips;
                if (p.Owner == e 
                    || p.Owner == null && p.IsExploredBy(e)
                                       && ships.Any(s => s?.Position.InRadius(p.Position, s.SensorRange) == true))
                {
                    if (e.isPlayer)
                        p.Universe.Notifications.AddShipCrashed(p, message);
                    else
                        AiProcessCrashSite(p, e, shipSize);
                }
            }
        }

        void AiProcessCrashSite(Planet p, Empire e, int shipSize)
        {
            if (p.Owner == null)
            {
                if (!p.ParentSystem.OwnerList.Any(empire => empire.IsNAPactWith(e)) && shipSize >= 100)
                    e.AI.SendExplorationFleet(p);
            }
            else if (!p.TroopsInTheWorks && !p.AnyOfOurTroops(e) && !p.SpaceCombatNearPlanet) // owner is this empire
            {
                SendTroop(e, p);
            }
        }

        void SendTroop(Empire e, Planet p)
        {
            if (e.GetTroopShipForRebase(out Ship troopShip, p.Position, p.Name))
                troopShip.AI.OrderLandAllTroops(p, clearOrders:true);
            else
                e.AI.SendExplorationFleet(p); // Create a task to be processed normally
        }

        public void ActivateSite(UniverseState u, Planet p, Empire activatingEmpire, PlanetGridSquare tile)
        {
            Active = false;
            Empire owner = p.Owner ?? activatingEmpire;
            Ship ship = SpawnShip(u, p, activatingEmpire, owner, out string message);
            string troopMessage = "";

            if (ship != null)
                SpawnSurvivingTroops(p, owner, tile, out troopMessage);

            if (owner.isPlayer || !owner.isPlayer && Loyalty.isPlayer && NumTroopsSurvived > 0)
                u.Notifications.AddShipRecovered(p, ship, $"{message}{troopMessage}");

            p.DestroyBuildingOn(tile);
        }

        Ship SpawnShip(UniverseState u, Planet p, Empire activatingEmpire, Empire owner, out string message)
        {
            message = $"Recover efforts of a crashed ship on {p.Name} were futile.\n" +
                      "It was completely wrecked.";

            Ship template = ResourceManager.GetShipTemplate(ShipName, false);
            if (template == null)
                return null;

            if (RecoverShip)
            {
                string otherOwners = owner.isPlayer ? ".\n" : $" by {owner.Name}.\n";
                Ship ship = Ship.CreateShipAt(u, ShipName, activatingEmpire, p, true);
                message = $"Ship ({ShipName}) was recovered from the\nsurface of {p.Name}{otherOwners}";
                float damageModifier = activatingEmpire == Loyalty ? 0.8f : 1; // If it was our ship, spawn with less damage.
                ship.DamageByRecoveredFromCrash(damageModifier);
                return ship;
            }

            float recoverAmount = template.ShipData.BaseCost / 10;
            if (owner == activatingEmpire)
            {
                p.ProdHere  = (p.ProdHere + recoverAmount).UpperBound(p.Storage.Max);
                message     = $"We were able to recover {recoverAmount.String(0)} production\n" +
                              $"from a crashed ship on {p.Name}.\n";
            }
            else
            {
                activatingEmpire.AddMoney(template.ShipData.BaseCost / 10);
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

            if (rel?.AtWar == false && rel.CanAttack && !Loyalty.IsFaction)
            {
                NumTroopsSurvived = 0;
                return; // Dont spawn troops, risking war
            }

            bool shouldLandTroop = Loyalty == owner || rel?.AtWar == true || Loyalty.IsFaction;
            for (int i = 1; i <= NumTroopsSurvived; i++)
            {
                if (ResourceManager.TryCreateTroop(TroopName, Loyalty, out Troop t))
                {
                    if (!shouldLandTroop || !t.TryLandTroop(p, tile))
                    {
                        Ship ship = t.Launch(p);
                        ship.AI.OrderRebaseToNearest();
                    }
                }
            }

            if (NumTroopsSurvived == 0)
                return;

            bool playerTroopsRecovered = Loyalty == p.Universe.Player && owner != p.Universe.Player;
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
            if (planet.OwnerIsPlayer)
                planet.Universe.Notifications.AddMeteorRelated(planet, Localizer.Token(GameText.AMeteorCraterWasFlattened), path);

            planet.DestroyBuildingOn(tile);
        }
    }
}
