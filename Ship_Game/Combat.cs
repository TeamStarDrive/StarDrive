namespace Ship_Game
{
	public sealed class Combat
	{
		public float Timer = 4f;

		public PlanetGridSquare AttackTile;

		public PlanetGridSquare DefenseTile;

		public int phase = 1;
	}
}