using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Spatial;

namespace Ship_Game;

public static class CollectionExtGameObj
{
    public static void SortByDistance<T>(this T[] items, Vector2 fromPos) where T : SpatialObjectBase
    {
        SortByDistance(items, items.Length, fromPos);
    }

    public static void SortByDistance<T>(this T[] items, int count, Vector2 fromPos) where T : SpatialObjectBase
    {
        if (count <= 1)
            return;

        var keys = new float[count];
        for (int i = 0; i < count; ++i)
            keys[i] = items[i].Position.SqDist(fromPos);

        Array.Sort(keys, items, 0, count);
    }
}
