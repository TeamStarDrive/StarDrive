using System;
using System.Collections.Generic;

namespace Ship_Game.Gameplay
{
	public class Store
	{
		public string Name;

		public string Description;

		public List<Store.Item> Inventory = new List<Store.Item>();

		public Store()
		{
		}

		public struct Item
		{
			public string Type;

			public int Cost;

			public int Quantity;
		}
	}
}