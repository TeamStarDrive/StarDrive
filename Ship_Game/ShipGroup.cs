using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.AI;

namespace Ship_Game
{
    public class ShipGroup : IDisposable
    {
        public BatchRemovalCollection<Ship> Ships = new BatchRemovalCollection<Ship>();
        public float ProjectedFacing;

        public override string ToString() => $"FleetGroup size={Ships.Count}";

        public ShipGroup()
        {
        }

        public virtual void ProjectPos(Vector2 position, float facing, Array<Fleet.Squad> flank)
        {//This is basically here so it can be overridden in fleet.cs -Gretman
        }
        public virtual void AddShip(Ship ship)
        {
            using (Ships.AcquireWriteLock())
            {
                Ships.Add(ship);
            }
        }
        public virtual void ProjectPos(Vector2 position, float facing, Vector2 fVec)
        {
            ProjectedFacing = facing;
            Ships[0].projectedPosition = position;
            for (int i = 1; i < Ships.Count; i++)
            {
                float facingRandomizer = (i % 2 == 0) ? -1.57079637f : +1.57079637f;
                Ships[i].projectedPosition = position.PointFromRadians(facing + facingRandomizer, i * 500);
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        ~ShipGroup() { Destroy(); }

        protected virtual void Destroy()
        {
            Ships?.Dispose(ref Ships);
        }
    }
}