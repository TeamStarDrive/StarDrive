using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships.AI
{
    [StarDataType]
    public struct WayPoint
    {
        [StarData] public Vector2 Position;
        [StarData] public Vector2 Direction; // direction we should be facing at the way point
        public WayPoint(Vector2 pos, Vector2 dir)
        {
            Position = pos;
            Direction = dir;
        }
    }

    public class WayPoints
    {
        readonly SafeQueue<WayPoint> ActiveWayPoints = new SafeQueue<WayPoint>();
        
        public void Clear()
        {
            ActiveWayPoints.Clear();
        }

        public int Count => ActiveWayPoints.Count;

        public WayPoint Dequeue()
        {
            return ActiveWayPoints.Dequeue();
        }
        public void Enqueue(WayPoint point)
        {
            ActiveWayPoints.Enqueue(point);
        }
        public WayPoint ElementAt(int element)
        {
            return ActiveWayPoints.ElementAt(element);
        }
        public WayPoint[] ToArray()
        {
            return ActiveWayPoints.ToArray();
        }
        public WayPoint PeekFirst => ActiveWayPoints.PeekFirst;
        public WayPoint PeekLast => ActiveWayPoints.PeekLast;
    }        

}