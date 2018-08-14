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
		public int number_allowed_troops = 1;
		public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
		public bool Biosphere;
		public Building building;
		public bool Habitable;
		public QueueItem QItem;
		public Rectangle ClickRect = new Rectangle();
		public Rectangle TroopClickRect = new Rectangle();
		public bool highlighted;

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
	}
}