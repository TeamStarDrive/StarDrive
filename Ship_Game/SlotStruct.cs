using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;

namespace Ship_Game
{
	public class SlotStruct
	{
		public Ship_Game.Gameplay.Restrictions Restrictions;

		public PrimitiveQuad pq;

		public float facing;

		public bool CheckedConduits;

		public SlotStruct parent;

		public ShipDesignScreen.ActiveModuleState state;

		public bool Powered;

		public ModuleSlotData slotReference;

		public string ModuleUID;

		public ShipModule module;

		public string SlotOptions;

		public bool isDummy;

		public Texture2D tex;

		public bool ShowValid;

		public bool ShowInvalid;

		public SlotStruct()
		{
		}
	}
}