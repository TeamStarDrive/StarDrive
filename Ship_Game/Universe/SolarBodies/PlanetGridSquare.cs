using System;
using Microsoft.Xna.Framework;
using Ship_Game.Universe.SolarBodies;

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
        public bool EventOnTile          => BuildingOnTile && (building.EventHere || CrashSite.Active);
        public bool BioCanTerraform      => Biosphere && Terraformable;
        public bool CanTerraform         => Terraformable && (!Habitable || Habitable && Biosphere);
        public bool CanCrashHere         => NoBuildingOnTile || !CrashSite.Active || BuildingOnTile && !building.IsCapital;

        public DynamicCrashSite CrashSite = new DynamicCrashSite(false);

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
                    if (t.Loyalty.IsAtWarWith(us))
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

        public void KillAllTroops(Planet p)
        {
            using (TroopsHere.AcquireWriteLock())
            {
                for (int i = TroopsHere.Count - 1; i >= 0; --i)
                {
                    Troop t = TroopsHere[i];
                    t.KillTroop(p, this);
                }
            }
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

        public bool HostilesTargetsOnTileToBuilding(Empire us, Empire planetOwner, bool warZone)
        {
            // buildings only see troops on tile as potential hostiles
            return TroopsAreOnTile && HostilesTargetsOnTile(us, planetOwner, warZone);
        }

        public bool HostilesTargetsOnTile(Empire us, Empire planetOwner, bool warZone)
        {
            if (CombatBuildingOnTile && planetOwner != null && planetOwner != us || EventOnTile && !warZone)
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
                        score += 2; // Land near shields to destroy them

                    if (building.InvadeInjurePoints > 0)
                        score += 2; // Land near AA anti troop defense to destroy it
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

            return HostilesTargetsOnTile(t.Loyalty, planet.Owner, planet.MightBeAWarZone(t.Loyalty)) ? 1 : 0;
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
                if (CrashSite.Active)
                {
                    if (!planet.MightBeAWarZone(empire))
                        CrashSite.ActivateSite(planet, empire, this);
                }
                else
                {
                    ResourceManager.Event(building.EventTriggerUID).TriggerPlanetEvent(planet, empire, this, Empire.Universe);
                }
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
                CrashSiteActive    = CrashSite.Active,
                CrashSiteShipName  = CrashSite.ShipName,
                CrashSiteTroopName = CrashSite.TroopName,
                CrashSiteTroops    = CrashSite.NumTroopsSurvived,
                CrashSiteEmpireId  = CrashSite.Loyalty?.Id ?? -1
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