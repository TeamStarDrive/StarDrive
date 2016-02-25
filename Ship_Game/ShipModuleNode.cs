using Ship_Game.Gameplay;
using System;

namespace Ship_Game
{
	public sealed class ShipModuleNode
	{
		public ShipModuleNode Next;

        //public Ship_Game.Gameplay.ModuleSlot ModuleSlot;          //Not referenced in code, removing to save memory -Gretman

        public ShipModuleNode()
		{
		}
	}
}