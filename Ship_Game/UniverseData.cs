using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class UniverseData : IDisposable
	{
		public string loadFogPath;

		public List<SolarSystem> SolarSystemsList = new List<SolarSystem>();

		public Vector2 Size;

		public UniverseData.GameDifficulty difficulty = UniverseData.GameDifficulty.Normal;

		public float FTLSpeedModifier = 1f;
        public float EnemyFTLSpeedModifier = 1f;

		public bool GravityWells;

		public BatchRemovalCollection<Ship> MasterShipList = new BatchRemovalCollection<Ship>();

		public Ship playerShip;

		public List<Empire> EmpireList = new List<Empire>();
        public static float UniverseWidth;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.MasterShipList != null)
                        this.MasterShipList.Dispose();

                }
                this.MasterShipList = null;
                this.disposed = true;
            }
        }
	}
}