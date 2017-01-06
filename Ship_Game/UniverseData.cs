using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class UniverseData : IDisposable
	{
		public string loadFogPath;

		public Array<SolarSystem> SolarSystemsList = new Array<SolarSystem>();

		public Vector2 Size;

		public UniverseData.GameDifficulty difficulty = UniverseData.GameDifficulty.Normal;

		public float FTLSpeedModifier = 1f;
        public float EnemyFTLSpeedModifier = 1f;
        public float FTLInSystemModifier = 1f;
        public bool FTLinNeutralSystem = true;

		public bool GravityWells;

		public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();

		public Ship playerShip;

		public Array<Empire> EmpireList = new Array<Empire>();
        public static float UniverseWidth;

		public UniverseData()
		{
            UniverseWidth = this.Size.X;
		}

		public enum GameDifficulty
		{
			Easy,
			Normal,
			Hard,
			Brutal
		}
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UniverseData() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            MasterShipList?.Dispose(ref MasterShipList);
        }
    }
}