using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships.AI
{
    public class WayPoints
    {
        public object WayPointLocker;
        public Queue<Vector2> ActiveWayPoints = new Queue<Vector2>();
        public Ship Owner;
        
        public WayPoints(Ship owner)
        {
            Owner = owner;
            WayPointLocker = new object();
        }

        public IReadOnlyCollection<Vector2> GetWayPoints()
        {
            lock(WayPointLocker)
                return ActiveWayPoints.ToList().AsReadOnly();
        }

        public void Clear()
        {
            lock (WayPointLocker)
            {
                ActiveWayPoints.Clear();
            }
        }
        public int Count()
        {
            lock (WayPointLocker)
                return ActiveWayPoints.Count;
        }
        public Vector2 Last()
        {
            lock (WayPointLocker)
                return ActiveWayPoints.Last();
        }
        public Vector2 Dequeue()
        {
            lock (WayPointLocker)
                return ActiveWayPoints.Dequeue();
        }
        public void Enqueue(Vector2 point)
        {
            lock (WayPointLocker)
                ActiveWayPoints.Enqueue(point);
        }
        public bool LastPointEquals(Vector2 point)
        {
            lock (WayPointLocker)
                return ActiveWayPoints.Last().Equals(point);
        }
        public Vector2 ElementAt(int element)
        {
            lock (WayPointLocker)
                return ActiveWayPoints.ElementAt(element);
        }
        public Vector2[] ToArray()
        {
            lock (WayPointLocker)
                return ActiveWayPoints.ToArray();
        }
    }        

}