using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Xml;

namespace Ship_Game.Gameplay
{
	public sealed class ModuleSlotData
	{
		public Vector2 Position;
		public string InstalledModuleUID;
		public Guid HangarshipGuid;
		public float Health;
		public float Shield_Power;
		public float facing;
		public ShipDesignScreen.ActiveModuleState state;
		public Restrictions Restrictions;
		public string SlotOptions;

		public ModuleSlotData()
		{
		}

	}
}