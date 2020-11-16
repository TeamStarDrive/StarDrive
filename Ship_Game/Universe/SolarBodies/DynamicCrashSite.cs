using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Universe.SolarBodies
{
    public struct DynamicCrashSite
    {
        public Empire Empire;
        public string ShipName;
        public int NumTroopsSurvived;
        public bool Active;
        public string TroopName;

        public DynamicCrashSite(bool active)
        {
            Active            = active;
            Empire            = null;
            ShipName          = "";
            TroopName         = "";
            NumTroopsSurvived = 0;
        }

        public void CrashShip(Empire empire, string shipName, string troopName, int numTroopsSurvived,
            Planet p, PlanetGridSquare tile, bool fromSave = false)
        {
            if (!TryCreateCrashSite(p, tile, out string message))
                return;

            Active            = true;
            Empire            = empire;
            ShipName          = shipName;
            TroopName         = troopName;
            NumTroopsSurvived = numTroopsSurvived;

            if (!fromSave)
                NotifyPlayerAndAi(p, message);
        }

        bool TryCreateCrashSite(Planet p, PlanetGridSquare tile, out string message)
        {
            message    = $"A ship has crashed landed on {p.Name}.";
            Building b = ResourceManager.CreateBuilding("Dynamic Crash Site");

            if (b == null)
                return false;

            if (tile.BuildingOnTile)
            {
                message = $"{message}\n Unfortunately, it crashed on {tile.building.TranslatedName.Text}.";
                p.DestroyBuildingOn(tile);
            }

            if (tile.TroopsAreOnTile)
            {
                message = $"{message}\nTroops were killed.";
                tile.KillAllTroops(p);
            }

            tile.PlaceBuilding(b, p);
            return true;
        }

        void NotifyPlayerAndAi(Planet p, string message)
        {
            if (p.Owner == null && p.IsExploredBy(EmpireManager.Player) || p.Owner == EmpireManager.Player)
                Empire.Universe.NotificationManager.AddShipCrashed(p, message);

            foreach (Empire e in EmpireManager.ActiveNonPlayerEmpires)
            {
                if (p.Owner == null && p.IsExploredBy(e))
                    e.GetEmpireAI().TrySendExplorationFleetToCrashSite(p);
            }
        }

        public void ActivateSite(Planet p, Empire activatingEmpire, PlanetGridSquare tile)
        {
            Active       = false;
            Empire owner = p.Owner ?? activatingEmpire;

            SpawnShip(p, activatingEmpire, owner, out string message);
            SpawnSurvivingTroops(p, owner, tile, out string troopMessage);
            p.DestroyBuildingOn(tile);

            if (owner.isPlayer || !owner.isPlayer && Empire.isPlayer && NumTroopsSurvived > 0)
                Empire.Universe.NotificationManager.AddShipRecovered(p, $"{message}{troopMessage}");
        }

        void SpawnShip(Planet p, Empire activatingEmpire, Empire owner, out string message)
        {
            Ship template       = ResourceManager.GetShipTemplate(ShipName);
            float recoverChance = CalcRecoverChance(template, activatingEmpire);

            if (RandomMath.RollDice(100))
            {
                string otherOwners = owner.isPlayer ? ".\n" : $" by {owner.Name}.\n";
                Ship ship = Ship.CreateShipAt(ShipName, activatingEmpire, p, true);

                ship.DamageByRecoveredFromCrash();
                message = $"Ship ({ShipName}) was recovered from the\nsurface of {p.Name}{otherOwners}";
            }
            else
            {
                if (owner == activatingEmpire)
                {
                    p.ProdHere = (p.ProdHere + template.BaseCost / 10).UpperBound(p.Storage.Max);
                    message = "We were able to recover some production from\n" +
                                 $"a crashed ships on {p.Name}.\n";
                }
                else
                {
                    activatingEmpire.AddMoney(template.BaseCost / 10);
                    message = "We were able to recover some credit worth scrap metal\n" +
                                $"from a crashed ships on {p.Name}.\n";
                }
            }
        }

        void SpawnSurvivingTroops(Planet p, Empire owner, PlanetGridSquare tile, out string message)
        {
            Relationship rel = null;
            message          = "The Crew was perished.";

            if (Empire != owner)
                rel = owner.GetRelations(Empire);

            if (rel?.AtWar == false && rel.CanAttack && !Empire.isFaction)
            {
                NumTroopsSurvived = 0;
                return; // Dont spawn troops, risking war
            }

            bool shouldLandTroop = Empire == owner || rel?.AtWar == true || Empire.isFaction;
            for (int i = 1; i <= NumTroopsSurvived; i++)
            {
                Troop t = ResourceManager.CreateTroop(TroopName, Empire);
                t.SetOwner(Empire);
                if (!shouldLandTroop || !t.TryLandTroop(p, tile))
                {
                    Ship ship = t.Launch(p);
                    ship.AI.OrderRebaseToNearest();
                }
            }

            if (NumTroopsSurvived == 0)
                return;

            bool playerTroopsRecovered = Empire == EmpireManager.Player && owner != EmpireManager.Player;
            if (Empire == owner)
            {
                message = "Friendly Troops have Survived.";
            }
            else if (rel?.AtWar == true)
            {
                message = playerTroopsRecovered
                    ? "Our Troops are in combat there!."
                    : "Hostile troops survived and are\nattacking!";
            }
            else
            {
                message = playerTroopsRecovered
                    ? "Our Troops and are heading home."
                    : "Neutral troops survived and are\nheading home.";
            }
        }

        float CalcRecoverChance(Ship template, Empire activatingEmpire)
        {
            float chance = 20 * (1 + activatingEmpire.data.Traits.ModHpModifier);
            if (Empire == activatingEmpire)
                chance += 5; // Familiarity with our ships
            else if (Empire.WeAreRemnants)
                chance -= template.SurfaceArea / 15f; // Remnants tend to self destruct

            return chance.Clamped(1, 50);
        }
    }
}