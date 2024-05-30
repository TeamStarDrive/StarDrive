using System;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe.SolarBodies;
using SDUtils;
using Rectangle = SDGraphics.Rectangle;
using Point = SDGraphics.Point;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Ship_Game
{
    // Refactored by Fat Bastard, Feb 6, 2019
    // Converted to 2 troops per tile support by Fat Bastard, Feb 28, 2020
    [StarDataType]
    public sealed class PlanetGridSquare
    {
        [StarData] public Planet P;
        [StarData] public int X;
        [StarData] public int Y;
        public bool ShowAttackHover;
        public int MaxAllowedTroops = 2; // FB allow 2 troops of different loyalties
        [StarData] public Array<Troop> TroopsHere = new();
        [StarData] public bool Biosphere;
        [StarData] public bool Terraformable; // This tile can be habitable if terraformed
        [StarData] public Building Building { get; private set; }
        [StarData] public bool Habitable; // FB - this also affects max population (because of pop per habitable tile)
        [StarData] public QueueItem QItem { get; private set; }
        public Rectangle ClickRect = new();
        [StarData] public short EventOutcomeNum { get; private set; }
        public bool Highlighted;

        public bool NoTroopsOnTile       => TroopsHere.IsEmpty;
        public bool TroopsAreOnTile      => TroopsHere.NotEmpty;
        public bool NoBuildingOnTile     => Building == null;
        public bool BuildingOnTile       => Building != null;
        public bool CombatBuildingOnTile => BuildingOnTile && Building.IsAttackable;
        public bool NothingOnTile        => NoTroopsOnTile && NoBuildingOnTile;
        public bool BuildingDestroyed    => BuildingOnTile && Building.IsDestroyed;
        public bool EventOnTile          => BuildingOnTile && (Building.EventHere || IsCrashSiteActive);
        public bool BioCanTerraform      => Biosphere && Terraformable;
        public bool CanTerraform         => Terraformable && (!Habitable || Habitable && Biosphere);
        public bool VolcanoHere          => Volcano != null;
        public bool LavaHere             => BuildingOnTile && Building.IsLava;
        public bool CraterHere           => BuildingOnTile && Building.IsCrater;
        public bool CapitalHere          => BuildingOnTile && Building.IsCapital;
        public bool CanCrashHere         => !IsCrashSiteActive && !CraterHere && !VolcanoHere && (!BuildingOnTile || !CapitalHere);
        public bool IsCrashSiteActive => CrashSite?.Active == true;
        public bool TerrainCanBeTerraformed => BuildingOnTile && Building.CanBeTerraformed;
        public bool ImmuneToLava => LavaHere || VolcanoHere || BuildingOnTile && Building.ImmueToLava;

        public bool NoQueuedBuildings => QItem == null;

        [StarData] public DynamicCrashSite CrashSite;
        [StarData] public Volcano Volcano;
        
        [StarDataConstructor] PlanetGridSquare() { }

        public PlanetGridSquare(Planet p, int x, int y, Building b, bool hab, bool terraformable)
        {
            P = p;
            X = x;
            Y = y;
            Habitable = hab;
            Building = b;
            Terraformable = terraformable;
        }

        public override string ToString()
        {
            string stuffOnTile = "";
            if (BuildingOnTile)
                stuffOnTile += "building="+Building.Name;
            if (TroopsAreOnTile)
                stuffOnTile += ":troops="+TroopsHere.Count;
            if (Biosphere)
                stuffOnTile += ":biosphere";
            return $"Tile X:{X} Y:{Y} {stuffOnTile} Planet={P}";
        }

        public bool IsTileFree(Empire empire)
        {
            if (TroopsHere.Count >= MaxAllowedTroops || CombatBuildingOnTile)
                return false;

            for (int i = 0; i < TroopsHere.Count; ++i)
            {
                Troop t = TroopsHere[i];
                if (t.Loyalty == empire)
                    return false;
            }

            return true;
        } 

        public void RemoveQueueItem()
        {
            QItem = null;
        }

        public void SetQueueItem(QueueItem qItem)
        {
            QItem = qItem;
        }

        // Get a troop that is not ours
        public bool LockOnEnemyTroop(Empire us, out Troop troop)
        {
            troop = null;
            for (int i = 0; i < TroopsHere.Count; ++i)
            {
                Troop t = TroopsHere[i];
                if (t.Loyalty.IsAtWarWith(us))
                {
                    troop = t;
                    return true;
                }
            }
            return false;
        }

        // Get a troop that is ours
        public bool LockOnOurTroop(Empire us, out Troop troop)
        {
            troop = null;
            for (int i = 0; i < TroopsHere.Count; ++i)
            {
                Troop t = TroopsHere[i];
                if (t.Loyalty == us)
                {
                    troop = t;
                    return true;
                }
            }
            return false;
        }

        public Troop TryGetFirstTroop()
        {
            if (TroopsAreOnTile)
                return TroopsHere[0];
            return null;
        }

        public void KillAllTroops(Planet p)
        {
            for (int i = TroopsHere.Count - 1; i >= 0; --i)
            {
                Troop t = TroopsHere[i];
                t.KillTroop(p, this);
            }
        }

        public bool EnemyTroopsHere(Empire us)
        {
            return LockOnEnemyTroop(us, out _);
        }

        public void AddTroop(Troop troop)
        {
            TroopsHere.AddUniqueRef(troop);
        }

        public void CreateVolcano(Planet p)
        {
            Volcano = new Volcano(this, p);
        }

        public bool CanEnqueueBuildingHere(Building b)
        {
            return NoQueuedBuildings && CanPlaceBuildingHere(b);
        }

        public bool CanPlaceBuildingHere(Building b)
        {
            // Don't allow biospheres on habitable tiles (including tiles with biospheres or specific buildings)
            if (b.IsBiospheres)
                return !Habitable && !VolcanoHere && !LavaHere;

            return NoBuildingOnTile && (Habitable || b.CanBuildAnywhere);
        }

        public void PlaceBuilding(Building b, Planet p)
        {
            RemoveQueueItem();
            if (b.IsBiospheres)
            {
                if (Habitable)
                    return; // Tile was Habitable when Biospheres completed. Probably due to Terraforming

                Habitable = true;
                Biosphere = true;
            }
            else
            {
                if (Building != null)
                {
                    Log.Write($"Planet {p.Name} destroy Building={Building.Name} ReplacedBy={b.Name}");
                    p.DestroyBuildingOn(this);
                }
                Building = b;
            }

            p.PlaceBuildingAt(b);
        }

        // don't call this directly, always use Planet.DestroyBuilding()/ScrapBuilding() instead
        public void ClearBuilding()
        {
            Building = null;
        }

        public bool HostilesTargetsOnTileToBuilding(Empire us, Empire planetOwner, bool spaceCombat)
        {
            // buildings only see troops on tile as potential hostiles
            return TroopsAreOnTile && HostilesTargetsOnTile(us, planetOwner, spaceCombat);
        }

        public bool HostilesTargetsOnTile(Empire us, Empire planetOwner, bool spaceCombat)
        {
            // Events will not be targeted if there is a space battle near the planet, since its
            // useless to potentially recover damaged ships right into battle.
            if (CombatBuildingOnTile && planetOwner != null && planetOwner != us
                || EventOnTile && !spaceCombat && !us.IsFaction) // factions wont explore events
            {
                return true;
            }

            return LockOnEnemyTroop(us, out _);
        }

        public int CalculateNearbyTileScore(Troop troop, Empire planetOwner)
        {
            int score = 0;
            if (CombatBuildingOnTile) // is attackable building on tile?
            {
                if (troop.Loyalty != planetOwner) // hostile building
                {
                    score += Building.CanAttack ? -1 : 1;
                    if (Building.Strength > troop.Strength)
                        score -= 1; // Stay away from stronger buildings

                    if (Building.IsCapital)
                        score = -2; // never land on capital directly

                    if (Building.PlanetaryShieldStrengthAdded > 0)
                        score += 2; // Land near shields to destroy them

                    if (Building.InvadeInjurePoints > 0)
                        score += 2; // Land near AA anti troop defense to destroy it
                }
                else // friendly building
                {
                    score += Building.CanAttack ? 3 : 2;
                    if (Building.Strength < troop.Strength)
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
                if (planet.ShieldStrengthCurrent > 0 && Building.PlanetaryShieldStrengthAdded > 0)
                    return 3;

                if (Building.InvadeInjurePoints > 0)
                    return 2;

                return 1;
            }

            return HostilesTargetsOnTile(t.Loyalty, planet.Owner, planet.SpaceCombatNearPlanet) ? 1 : 0;
        }

        public bool InRangeOf(PlanetGridSquare tileToCheck, int range)
        {
            return Math.Abs(X - tileToCheck.X) <= range
                && Math.Abs(Y - tileToCheck.Y) <= range;
        }

        public void CrashShip(Empire empire, string shipName, string troopName, int numTroopsSurvived,
                              Planet p, int shipSize)
        {
            if (CrashSite == null)
                CrashSite = new(false);
            CrashSite.CrashShip(empire, shipName, troopName, numTroopsSurvived, p, this, shipSize);
        }

        public void CheckAndTriggerEvent(Planet planet, Empire empire)
        {
            if (EventOnTile && LockOnOurTroop(empire, out _))
            {
                if (IsCrashSiteActive)
                {
                    if (!planet.SpaceCombatNearPlanet)
                        CrashSite.ActivateSite(planet.Universe, planet, empire, this);
                }
                else
                {
                    ResourceManager.Event(Building.EventTriggerUID)
                        .TriggerPlanetEvent(planet, EventOutcomeNum , empire, this, planet.Universe.Screen);
                }
            }
        }

        public bool SetEventOutComeNum(Planet p, Building b)
        {
            EventOutcomeNum = ResourceManager.Event(b.EventTriggerUID).SetOutcomeNum(p);
            return EventOutcomeNum != 0;
        }

        public TileDirection GetDirectionTo(PlanetGridSquare target)
        {

            int xDiff = (target.X - X).Clamped(-1, 1);
            int yDiff = (target.Y - Y).Clamped(-1, 1);
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
                default:                  return TileDirection.None;
            }
        }

        public Point ConvertDirectionToCoordinates(TileDirection d)
        {
            Point p;
            switch (d)
            {
                case TileDirection.North:     p.X = X;     p.Y = Y - 1; break;
                case TileDirection.South:     p.X = X;     p.Y = Y + 1; break;
                case TileDirection.East:      p.X = X + 1; p.Y = Y;     break;
                case TileDirection.West:      p.X = X - 1; p.Y = Y;     break;
                case TileDirection.NorthEast: p.X = X + 1; p.Y = Y - 1; break;
                case TileDirection.NorthWest: p.X = X - 1; p.Y = Y - 1; break;
                case TileDirection.SouthEast: p.X = X + 1; p.Y = Y + 1; break;
                case TileDirection.SouthWest: p.X = X - 1; p.Y = Y + 1; break;
                case TileDirection.None:
                default:                        p.X = X;     p.Y = Y;     break;
            }
            return p;
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

    public sealed class BlueprintsTile
    {
        public readonly UIPanel Panel;
        public Building Building { get; private set; }

        public bool Unlocked { get; private set; }
        public bool IsFree => Building == null;
        public bool HasBuilding => !IsFree;

        public void UpdatePanelColor(bool isHovered)
        {
            if      (!HasBuilding)       Panel.Color = Color.White;
            else if (Building.IsOutpost) Panel.Color = Color.Gray;
            else if (isHovered)          Panel.Color = Color.Red;
            else                         Panel.Color = Unlocked ? Color.White : Color.Yellow;
        }


        public BlueprintsTile(UIPanel uiPanel)
        {
            Panel = uiPanel;
            Panel.Visible = false;
        }

        public void AddBuilding(Building b, bool unlocked = false)
        {
            Building = b;
            Panel.Sprite = new SpriteSystem.DrawableSprite(ResourceManager.Texture("Buildings/icon_" + b.Icon + "_64x64"));
            Panel.Visible = true;
            Unlocked = unlocked;
            if (b.IsOutpost)
                Panel.Tooltip = GameText.RightClickToRemove;
            else 
                Panel.Tooltip = unlocked ? GameText.RightClickToRemove : GameText.RightClickToRemove;
        }

        public void RemoveBuilding()
        {
            Building = null;
            Panel.Sprite = null;
            Panel.Visible = false;
            Unlocked = false;
        }

        public bool BuildingNameHereIS(string name)
        {
            return HasBuilding && Building.Name == name;
        }
    }

}
