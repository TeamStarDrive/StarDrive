using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ShipGroup: IDisposable
	{
		public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();

		public float ProjectedFacing;
        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public ShipGroup()
		{
		}

		public virtual void ProjectPos(Vector2 position, float facing, List<Fleet.Squad> flank)
		{
		}

		public virtual void ProjectPos(Vector2 position, float facing, Vector2 fVec)
		{
		    ProjectedFacing = facing;
			Ships[0].projectedPosition = position;
			for (int i = 1; i < Ships.Count; i++)
			{
                float facingRandomizer = (i % 2 == 0) ? -1.57079637f : +1.57079637f;
				Ships[i].projectedPosition = MathExt.PointFromRadians(position, facing + facingRandomizer, i * 500);
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShipGroup() { Dispose(false); }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.Ships != null)
                        this.Ships.Dispose();

                }
                this.Ships = null;
                this.disposed = true;
            }
        }
	}
}