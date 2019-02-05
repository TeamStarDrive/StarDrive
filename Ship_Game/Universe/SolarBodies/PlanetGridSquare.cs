using System.Linq;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public sealed class PlanetGridSquare
	{
		public int x;
		public int y;
		public bool CanAttack;
		public bool CanMoveTo;
		public bool ShowAttackHover;
		public int NumAllowedTroops = 1;
		public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
		public bool Biosphere;
		public Building building;
		public bool Habitable;
		public QueueItem QItem;
		public Rectangle ClickRect      = new Rectangle();
		public Rectangle TroopClickRect = new Rectangle();
		public bool highlighted;

        public bool NoTroopsHere       => TroopsHere.Count <= 0;
	    public bool TroopsAreHere      => TroopsHere.Count > 0;
        public bool NoBuildingHere     => building == null;
	    public bool BuildingIsHere     => building != null;
        public bool NothingHere        => NoTroopsHere && NoBuildingHere;
        public bool AllTroopsDead      => TroopsStrength <= 0;
        public bool BuildingDestroyed  => BuildingIsHere && building.Strength <= 0;
        public bool AllDestroyed       => BuildingDestroyed && AllTroopsDead;
        public Troop SingleTroop       => TroopsHere[0]; //FB -  multiple troops per PGS is not supported yet

        // FB - all these are starting multiple troops per PGS
	    public float TroopsStrength    => TroopsHere.Sum(troop => troop.Strength);
        public int TroopsHardAttack    => TroopsHere.Sum(troop => troop.NetHardAttack);
	    public int TroopsSoftAttack    => TroopsHere.Sum(troop => troop.NetSoftAttack);

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

        public bool ShouldPerformAutoCombat(Planet p)
        {
            return (GlobalStats.AutoCombat // always auto combat
                || p.Owner?.isPlayer == false // or we're AI?
                || !Empire.Universe.IsViewingCombatScreen(p)); // or we're not looking at combat screen
        }

        public bool ShouldBuildingPerformAutoCombat(Planet p)
        {
            return building?.CanAttackThisTurn == true
                && ShouldPerformAutoCombat(p);
        }
    }
}