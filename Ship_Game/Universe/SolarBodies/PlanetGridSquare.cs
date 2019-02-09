using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public sealed class PlanetGridSquare // Refactored by Fat Bastard, Feb 6, 2019
	{
		public int x;
		public int y;
		public bool CanAttack;
		public bool CanMoveTo;
		public bool ShowAttackHover;
		public int MaxAllowedTroops = 1; //FB - multiple troops per PGS is not supported yet
        public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
		public bool Biosphere;
		public Building building;
		public bool Habitable;
		public QueueItem QItem;
		public Rectangle ClickRect      = new Rectangle();
		public Rectangle TroopClickRect = new Rectangle();
		public bool Highlighted;

        public bool NoTroopsOnTile       => !TroopsAreOnTile;
        public bool TroopsAreOnTile      => TroopsHere.Count > 0;
        public bool NoBuildingOnTile     => !BuildingOnTile;
        public bool BuildingOnTile       => building != null;
        public bool CombatBuildingOnTile => BuildingOnTile && building.IsAttackable;
        public bool NothingOnTile        => NoTroopsOnTile && NoBuildingOnTile;
        public bool AllTroopsDead        => TroopsStrength <= 0;
        public bool BuildingDestroyed    => BuildingOnTile && building.Strength <= 0;
        public bool AllDestroyed         => BuildingDestroyed && AllTroopsDead;
        public Troop SingleTroop         => TroopsHere[0]; //FB - multiple troops per PGS is not supported yet
        public bool FreeForMovement      => !TroopsAreOnTile && !CombatBuildingOnTile;
        public bool EventOnTile          => BuildingOnTile && building.EventHere;

        // FB - all these are starting multiple troops per PGS support
        public float TroopsStrength => TroopsHere.Sum(troop => troop.Strength);
        public int TroopsHardAttack => TroopsHere.Sum(troop => troop.NetHardAttack);
        public int TroopsSoftAttack => TroopsHere.Sum(troop => troop.NetSoftAttack);

        public PlanetGridSquare()
		{
		}

		public PlanetGridSquare(int x, int y, Building b, bool hab)
		{
			this.x = x;
			this.y = y;
			Habitable = hab;
			building = b;
		}

        public bool CanBuildHere(Building b)
        {
            if (QItem != null)
                return false;

            if (b.IsBiospheres && (Biosphere || Habitable))
                return false; // don't allow double biosphere

            return !Habitable && b.CanBuildAnywhere
                 || Habitable && building == null;
        }

        public void PlaceBuilding(Building b)
        {
            if (b.IsBiospheres)
            {
                Habitable = true;
                Biosphere = true;
                building = null;
            }
            else
            {
                building = b;
            }
            QItem = null;
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
	        return (TroopsAreOnTile && SingleTroop.Loyalty != us) || EventOnTile || 
	               (CombatBuildingOnTile && planetOwner != us);  // also event ID needed
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


        public TargetDirection GetDirectionTo(PlanetGridSquare target)
	    {

	        int xDiff = (target.x - x).Clamped(-1, 1);
	        int yDiff = (target.y - y).Clamped(-1, 1);
	        switch (xDiff)
	        {
	            case 0 when yDiff == -1:  return TargetDirection.North;
	            case 0 when yDiff == 1:   return TargetDirection.South;
	            case 1 when yDiff == 0:   return TargetDirection.East;
	            case -1 when yDiff == 0:  return TargetDirection.West;
	            case 1 when yDiff == -1:  return TargetDirection.NorthEast;
	            case -1 when yDiff == -1: return TargetDirection.NorthWest;
	            case 1 when yDiff == 1:   return TargetDirection.SouthEast;
	            case -1 when yDiff == 1:  return TargetDirection.SouthWest;
	            default: return TargetDirection.None;
	        }
	    }

        public void ConvertDirectionToCoordinates(TargetDirection d, out int xNew, out int yNew)
        {
            switch (d)
            {
                case TargetDirection.North:     xNew = x;     yNew = y - 1; break;
                case TargetDirection.South:     xNew = x;     yNew = y + 1; break;
                case TargetDirection.East:      xNew = x + 1; yNew = y;     break;
                case TargetDirection.West:      xNew = x - 1; yNew = y;     break;
                case TargetDirection.NorthEast: xNew = x + 1; yNew = y - 1; break;
                case TargetDirection.NorthWest: xNew = x - 1; yNew = y - 1; break;
                case TargetDirection.SouthEast: xNew = x + 1; yNew = y + 1; break;
                case TargetDirection.SouthWest: xNew = x - 1; yNew = y + 1; break;
                case TargetDirection.None:
                default:                        xNew = x;     yNew = y;     break;
            }
        }
    }
    public enum TargetDirection
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