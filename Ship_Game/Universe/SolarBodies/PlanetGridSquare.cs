using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    // Refactored by Fat Bastard, Feb 6, 2019
    // Converted to 2 troops per tile support by Fat Bastard, Feb 28, 2020
    public sealed class PlanetGridSquare
    {
        public int x;
        public int y;
        public bool ShowAttackHover;
        public int MaxAllowedTroops = 2; // FB allow 2 troops of different loyalties
        public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
        public bool Biosphere;
        public bool Terraformable; // This tile can be habitable if terraformed
        public Building building;
        public bool Habitable; // FB - this also affects max population (because of pop per habitable tile)
        public QueueItem QItem;
        public Rectangle ClickRect = new Rectangle();
        public bool Highlighted;
        public bool NoTroopsOnTile       => TroopsHere.IsEmpty;
        public bool TroopsAreOnTile      => TroopsHere.NotEmpty;
        public bool NoBuildingOnTile     => building == null;
        public bool BuildingOnTile       => building != null;
        public bool CombatBuildingOnTile => BuildingOnTile && building.IsAttackable;
        public bool NothingOnTile        => NoTroopsOnTile && NoBuildingOnTile;
        public bool BuildingDestroyed    => BuildingOnTile && building.Strength <= 0;
        public bool EventOnTile          => BuildingOnTile && (building.EventHere || DynamicCrash.Active);
        public bool BioCanTerraform      => Biosphere && Terraformable;
        public bool CanTerraform         => Terraformable && (!Habitable || Habitable && Biosphere);

        public DynamicCrashSite DynamicCrash = new DynamicCrashSite(false);

        public bool IsTileFree(Empire empire)
        {
            if (TroopsHere.Count >= MaxAllowedTroops || CombatBuildingOnTile)
                return false;

            using (TroopsHere.AcquireReadLock())
            {
                for (int i = 0; i < TroopsHere.Count; ++i)
                {
                    Troop t = TroopsHere[i];
                    if (t.Loyalty == empire)
                        return false;
                }
            }

            return true;
        } 

        // Get a troop that is not ours
        public bool LockOnEnemyTroop(Empire us, out Troop troop)
        {
            troop = null;
            using (TroopsHere.AcquireReadLock())
            {
                for (int i = 0; i < TroopsHere.Count; ++i)
                {
                    Troop t = TroopsHere[i];
                    if (t.Loyalty != us)
                    {
                        troop = t;
                        return true;
                    }
                }
            }

            return false;
        }

        // Get a troop that is ours
        public bool LockOnOurTroop(Empire us, out Troop troop)
        {
            troop = null;
            using (TroopsHere.AcquireReadLock())
            {
                for (int i = 0; i < TroopsHere.Count; ++i)
                {
                    Troop t = TroopsHere[i];
                    if (t.Loyalty == us)
                    {
                        troop = t;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool LockOnPlayerTroop(out Troop playerTroop)
        {
            return LockOnOurTroop(EmpireManager.Player, out playerTroop);
        }

        public bool EnemyTroopsHere(Empire us)
        {
            return LockOnEnemyTroop(us, out _);
        }

        public PlanetGridSquare(int x, int y, Building b, bool hab, bool terraformable)
        {
            this.x        = x;
            this.y        = y;
            Habitable     = hab;
            building      = b;
            Terraformable = terraformable;
        }

        public void AddTroop(Troop troop)
        {
            TroopsHere.Add(troop);
        }

        public bool CanBuildHere(Building b)
        {
            if (QItem != null)
                return false;

            if (b.IsBiospheres)
                return !Habitable; // don't allow biospheres on habitable tiles (including tiles with biospheres)

            return !Habitable && b.CanBuildAnywhere && !BuildingOnTile
                 || Habitable && NoBuildingOnTile;
        }

        public void PlaceBuilding(Building b, Planet p)
        {
            if (b.IsBiospheres)
            {
                if (Habitable)
                {
                    QItem = null;
                    return; // Tile was Habitable when Biospheres completed. Probably due to Terraforming
                }

                Habitable = true;
                Biosphere = true;
            }
            else
                building = b;

            QItem = null;
            b.OnBuildingBuiltAt(p);
        }

        public bool PerformAutoCombat(Planet p)
        {
            return (GlobalStats.AutoCombat // always auto combat
                || p.Owner?.isPlayer == false // or we're AI?
                || !Empire.Universe.IsViewingCombatScreen(p)); // or we're not looking at combat screen
        }

        public bool BuildingPerformsAutoCombat(Planet p)
        {
            return building?.CanAttack == true
                && PerformAutoCombat(p);
        }

        public bool HostilesTargetsOnTile(Empire us, Empire planetOwner)
        {
            if (CombatBuildingOnTile && planetOwner != null && planetOwner != us || EventOnTile)
                return true;

            return LockOnEnemyTroop(us, out _);
        }

        public int CalculateNearbyTileScore(Troop troop, Empire planetOwner)
        {
            int score = 0;
            if (CombatBuildingOnTile)
            {
                if (troop.Loyalty != planetOwner) // hostile building
                {
                    score += building.CanAttack ? -1 : 1;
                    if (building.Strength > troop.Strength)
                        score -= 1; // Stay away from stronger buildings

                    if (building.PlanetaryShieldStrengthAdded > 0)
                        score += 10; // Land near shields to destroy them

                    if (building.InvadeInjurePoints > 0)
                        score += 6; // Land near AA anti troop defense to destroy it
                }
                else // friendly building
                {
                    score += building.CanAttack ? 3 : 2;
                    if (building.Strength < troop.Strength)
                        score += 1; // Defend friendly building
                }

                return score;
            }

            if (LockOnOurTroop(troop.Loyalty, out Troop friendly))
            {
                score += friendly.CanAttack ? 3 : 2;
                if (friendly.Strength < troop.Strength)
                    score += 1; // Aid friends in need
            }

            if (LockOnEnemyTroop(troop.Loyalty, out Troop enemy))
            {
                score += enemy.CanAttack ? -1 : 1;
                if (enemy.Strength > troop.Strength)
                    score -= 1; // Stay away from stronger enemy
            }

            return score;
        }

        public int CalculateTargetValue(Troop t, Planet planet)
        {
            if (EventOnTile)
                return 0;

            if (t.Loyalty != planet.Owner && CombatBuildingOnTile)
            {
                if (planet.ShieldStrengthCurrent > 0 && building.PlanetaryShieldStrengthAdded > 0)
                    return 3;

                if (building.InvadeInjurePoints > 0)
                    return 2;

                return 1;
            }
            else if (HostilesTargetsOnTile(t.Loyalty, planet.Owner))
            {
                return 1;
            }

            return 0;
        }

        public bool InRangeOf(PlanetGridSquare tileToCheck, int range)
        {
            return Math.Abs(x - tileToCheck.x) <= range && Math.Abs(y - tileToCheck.y) <= range;
        }

        public void DirectionToTarget(PlanetGridSquare target, out int xDiff, out int yDiff)
        {
            xDiff = (target.x - x).Clamped(-1, 1);
            yDiff = (target.y - y).Clamped(-1, 1);
        }

        public void CheckAndTriggerEvent(Planet planet, Empire empire)
        {
            if (EventOnTile && LockOnOurTroop(empire, out _))
            {
                if (DynamicCrash.Active)
                    DynamicCrash.ActivateSite(planet, empire, this);
                else
                    ResourceManager.Event(building.EventTriggerUID).TriggerPlanetEvent(planet, empire, this, Empire.Universe);
            }
        }

        public TileDirection GetDirectionTo(PlanetGridSquare target)
        {

            int xDiff = (target.x - x).Clamped(-1, 1);
            int yDiff = (target.y - y).Clamped(-1, 1);
            switch (xDiff)
            {
                case 0  when yDiff == -1: return TileDirection.North;
                case 0  when yDiff ==  1: return TileDirection.South;
                case 1  when yDiff ==  0: return TileDirection.East;
                case -1 when yDiff ==  0: return TileDirection.West;
                case 1  when yDiff == -1: return TileDirection.NorthEast;
                case -1 when yDiff == -1: return TileDirection.NorthWest;
                case 1  when yDiff ==  1: return TileDirection.SouthEast;
                case -1 when yDiff ==  1: return TileDirection.SouthWest;
                default: return TileDirection.None;
            }
        }

        public Point ConvertDirectionToCoordinates(TileDirection d)
        {
            Point p;
            switch (d)
            {
                case TileDirection.North:     p.X = x;     p.Y = y - 1; break;
                case TileDirection.South:     p.X = x;     p.Y = y + 1; break;
                case TileDirection.East:      p.X = x + 1; p.Y = y;     break;
                case TileDirection.West:      p.X = x - 1; p.Y = y;     break;
                case TileDirection.NorthEast: p.X = x + 1; p.Y = y - 1; break;
                case TileDirection.NorthWest: p.X = x - 1; p.Y = y - 1; break;
                case TileDirection.SouthEast: p.X = x + 1; p.Y = y + 1; break;
                case TileDirection.SouthWest: p.X = x - 1; p.Y = y + 1; break;
                case TileDirection.None:
                default:                        p.X = x;     p.Y = y;     break;
            }
            return p;
        }

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
                Planet p, PlanetGridSquare tile,  bool fromSave = false)
            {
                if (!TryCreateCrashSite(p, tile))
                    return;

                Active            = true;
                Empire            = empire;
                ShipName          = shipName;
                TroopName         = troopName;
                NumTroopsSurvived = numTroopsSurvived;

                if (!fromSave)
                    NotifyPlayerAndAi(p);
            }

            bool TryCreateCrashSite(Planet p, PlanetGridSquare tile)
            {
                Building b = ResourceManager.CreateBuilding("Dynamic Crash Site");
                if (b == null)
                    return false;

                tile.PlaceBuilding(b, p);
                return true;
            }

            void NotifyPlayerAndAi(Planet p)
            {
                if (p.Owner == null && p.IsExploredBy(EmpireManager.Player) || p.Owner == EmpireManager.Player)
                    Empire.Universe.NotificationManager.AddShipCrashed(p);

                foreach (Empire e in EmpireManager.ActiveNonPlayerEmpires)
                {
                    if (p.Owner == null && p.IsExploredBy(e))
                        e.GetEmpireAI().SendExplorationFleet(p);
                }
            }

            public void ActivateSite(Planet p, Empire activatingEmpire, PlanetGridSquare tile)
            {
                Active = false;
                Empire owner = p.Owner ?? activatingEmpire;
                SpawnShip(p, activatingEmpire, owner, out string message);
                SpawnSurvivingTroops(p, owner, tile, out string troopMessage);
                p.ScrapBuilding(tile.building);

                if (owner.isPlayer || !owner.isPlayer && Empire.isPlayer && NumTroopsSurvived > 0)
                    Empire.Universe.NotificationManager.AddShipRecovered(p, $"{message}{troopMessage}");
            }

            void SpawnShip(Planet p, Empire activatingEmpire, Empire owner, out string message)
            {
                float recoverChance = 20 * (1 + activatingEmpire.data.Traits.ModHpModifier);
                Ship ship = Ship.CreateShipAt(ShipName, activatingEmpire, p, true);
                if (RandomMath.RollDice(recoverChance))
                {
                    string otherOwners = owner.isPlayer ? ".\n" : $" by {owner.Name}.\n";
                    ship.DamageByRecoveredFromCrash();
                    message = $"Ship ({ship.Name}) was recovered from the\nsurface of {p.Name}{otherOwners}";
                }
                else
                {
                    if (owner == activatingEmpire)
                    {
                        p.ProdHere = (p.ProdHere + ship.BaseCost / 10).UpperBound(p.Storage.Max);
                        message = "We were able to recover some scrap metal from\n" +
                                     $"a crashed ships on {p.Name}.\n";
                    }
                    else
                    {
                        activatingEmpire.AddMoney(ship.BaseCost / 10);
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

                if (rel?.AtWar == false && rel.CanAttack)
                {
                    NumTroopsSurvived = 0;
                    return; // Dont spawn troops, risking war
                }

                bool shouldLandTroop = Empire == owner || rel?.AtWar == true;
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
        }


        public SavedGame.PGSData Serialize()
        {
            return new SavedGame.PGSData
            {
                x             = x,
                y             = y,
                Habitable     = Habitable,
                Biosphere     = Biosphere,
                building      = building,
                TroopsHere    = TroopsHere,
                Terraformable = Terraformable,
                CrashSiteActive    = DynamicCrash.Active,
                CrashSiteShipName  = DynamicCrash.ShipName,
                CrashSiteTroopName = DynamicCrash.TroopName,
                CrashSiteTroops    = DynamicCrash.NumTroopsSurvived,
                CrashSiteEmpireId  = DynamicCrash.Empire?.Id ?? -1
            };
        }
    }

    public enum TileDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest,
        None
    }
}