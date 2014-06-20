using Microsoft.Xna.Framework;
using Ship_Game;
using System;

namespace Ship_Game.Gameplay
{
	public class TraitEntry
	{
		public bool Selected;

		public RacialTrait trait;

		public Rectangle rect;

		public bool Excluded;

		public TraitEntry()
		{
		}
	}
}