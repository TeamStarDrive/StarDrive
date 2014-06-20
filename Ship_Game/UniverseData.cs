using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class UniverseData
	{
		public string loadFogPath;

		public List<SolarSystem> SolarSystemsList = new List<SolarSystem>();

		public Vector2 Size;

		public UniverseData.GameDifficulty difficulty = UniverseData.GameDifficulty.Normal;

		public float FTLSpeedModifier = 1f;

		public bool GravityWells;

		public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();

		public Ship playerShip;

		public List<Empire> EmpireList = new List<Empire>();

		public UniverseData()
		{
		}

		public enum GameDifficulty
		{
			Easy,
			Normal,
			Hard,
			Brutal
		}
	}
}