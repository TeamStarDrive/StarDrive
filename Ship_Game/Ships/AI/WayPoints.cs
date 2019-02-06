using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

namespace Ship_Game.Ships.AI
{
    public class WayPoints
    {
        readonly SafeQueue<Vector2> ActiveWayPoints = new SafeQueue<Vector2>();
        
        public void Clear()
        {
            ActiveWayPoints.Clear();
        }

        public int Count => ActiveWayPoints.Count;

        public Vector2 Dequeue()
        {
            return ActiveWayPoints.Dequeue();
        }
        public void Enqueue(Vector2 point)
        {
            ActiveWayPoints.Enqueue(point);
        }
        public Vector2 ElementAt(int element)
        {
            return ActiveWayPoints.ElementAt(element);
        }
        public Vector2[] ToArray()
        {
            return ActiveWayPoints.ToArray();
        }
    }        

}