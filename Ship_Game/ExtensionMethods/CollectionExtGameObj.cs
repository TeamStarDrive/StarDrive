using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Spatial;

namespace Ship_Game;

public static class CollectionExtGameObj
{
    public static void SortByDistance<T>(this T[] array, Vector2 fromPos) where T : SpatialObjectBase
    {
        if (array.Length <= 1)
            return;

        var keys = new float[array.Length];
        for (int i = 0; i < array.Length; ++i)
            keys[i] = array[i].Position.SqDist(fromPos);

        Array.Sort(keys, array, 0, array.Length);
    }

    public static T RandItem<T>(this T[] items)
    {
        return RandomMath.RandItem(items);
    }

    public static T RandItem<T>(this Array<T> items)
    {
        return RandomMath.RandItem(items);
    }

    public static T RandItem<T>(this IReadOnlyList<T> items)
    {
        return RandomMath.RandItem(items);
    }

}
