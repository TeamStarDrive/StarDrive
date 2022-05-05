using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Gameplay
{
	public sealed class TraitEntry
	{
		public bool Selected;

		public RacialTrait trait;

		public Rectangle rect;

		public bool Excluded;
	}
}