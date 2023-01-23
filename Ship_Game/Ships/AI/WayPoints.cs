using System;
using Ship_Game.Data.Serialization;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships.AI;

[StarDataType]
public readonly struct WayPoint
{
    [StarData] public readonly Vector2 Position;
    [StarData] public readonly Vector2 Direction; // direction we should be facing at the way point
    public WayPoint(Vector2 pos, Vector2 dir)
    {
        Position = pos;
        Direction = dir;
    }
}

public sealed class WayPoints : IDisposable
{
    readonly SafeQueue<WayPoint> ActiveWayPoints = new();
    public int Count => ActiveWayPoints.Count;

    public void Clear()
    {
        ActiveWayPoints.Clear();
    }
    public WayPoint Dequeue()
    {
        return ActiveWayPoints.Dequeue();
    }
    public void Enqueue(WayPoint point)
    {
        ActiveWayPoints.Enqueue(point);
    }
    public WayPoint[] EnqueueAndToArray(WayPoint point)
    {
        using (ActiveWayPoints.AcquireWriteLock())
        {
            ActiveWayPoints.Enqueue(point);
            return ActiveWayPoints.ToArray();
        }
    }
    public WayPoint ElementAt(int element)
    {
        return ActiveWayPoints.ElementAt(element);
    }
    public void Set(WayPoint[] wayPoints)
    {
        for (int i = 0; i < wayPoints.Length; i++)
            ActiveWayPoints.Enqueue(wayPoints[i]);
    }
    public WayPoint[] ToArray()
    {
        return ActiveWayPoints.ToArray();
    }

    public void Dispose()
    {
        ActiveWayPoints.Dispose();
    }
}
