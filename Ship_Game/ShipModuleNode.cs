using Ship_Game.Gameplay;
using System;

namespace Ship_Game
{
	public class ShipModuleNode
	{
		public ShipModuleNode Next;

		public Ship_Game.Gameplay.ModuleSlot ModuleSlot;

		public ShipModuleNode()
		{
		}
	}
}