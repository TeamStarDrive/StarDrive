using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game
{
    /// <summary>
    /// This contains multiple simple yet useful extension algorithms for different data structures
    /// The goal is to increase performance by specializing for concrete container types,
    /// which helps to eliminate virtual dispatch, greatly speeding up iteration times
    /// 
    /// As much as possible, we try to avoid any kind of IEnumerable or foreach loops, because
    /// they have appalling performance and .NET JIT fails to optimize most of our use cases.
    /// 
    /// We don't benefit from lazy evaluation either, because most of the algorithms are very data-heavy,
    /// with no way to exclude elements.
    /// 
    /// If you find these extensions repetitive, then yes, this is your worst nightmare --- however,
    /// all of this repetitive looping provides the best possible performance on .NET JIT. It's just not good enough.
    /// </summary>
    public static class CollectionFindClosestTo
    {
        public static Ship FindClosestTo(this Array<Ship> ships, Planet toPlanet)
        {
            return FindClosestTo(ships.GetInternalArrayItems(), ships.Count, toPlanet.Center);
        }

        public static Ship FindClosestTo(this Ship[] ships, Planet toPlanet)
        {
            return FindClosestTo(ships, ships.Length, toPlanet.Center);
        }
        
        public static Ship FindClosestTo(this Ship[] ships, int count, Planet toPlanet)
        {
            return FindClosestTo(ships, count, toPlanet.Center);
        }

        public static Ship FindClosestTo(this Ship[] ships, int count, Vector2 to)
        {
            if (count <= 0)
                return null;

            Ship found = ships[0];
            float min = to.SqDist(found.Center);
            for (int i = 1; i < count; ++i)
            {
                Ship ship = ships[i];
                float distance = to.SqDist(ship.Center);
                if (distance < min)
                {
                    min = distance;
                    found = ship;
                }
            }
            return found;
        }


        public static Planet FindClosestTo(this Array<Planet> planets, Planet toPlanet)
        {
            return FindClosestTo(planets.GetInternalArrayItems(), planets.Count, toPlanet.Center);
        }

        public static Planet FindClosestTo(this Planet[] planets, Planet toPlanet)
        {
            return FindClosestTo(planets, planets.Length, toPlanet.Center);
        }

        public static Planet FindClosestTo(this Planet[] planets, int count, Planet toPlanet)
        {
            return FindClosestTo(planets, count, toPlanet.Center);
        }

        public static Planet FindClosestTo(this Planet[] planets, int count, Vector2 to)
        {
            if (count <= 0)
                return null;

            Planet found = planets[0];
            float min = to.SqDist(found.Center);
            for (int i = 1; i < count; ++i)
            {
                Planet ship = planets[i];
                float distance = to.SqDist(ship.Center);
                if (distance < min)
                {
                    min = distance;
                    found = ship;
                }
            }
            return found;
        }

        public static Ship FindClosestTo(this Ship[] ships, Planet to, Predicate<Ship> filter)
        {
            return FindClosestTo(ships, ships.Length, to.Center, filter);
        }

        public static Ship FindClosestTo(this Array<Ship> ships, Planet to, Predicate<Ship> filter)
        {
            return FindClosestTo(ships.GetInternalArrayItems(), ships.Count, to.Center, filter);
        }

        public static Ship FindClosestTo(this Ship[] ships, int count, Planet toPlanet, Predicate<Ship> filter)
        {
            return FindClosestTo(ships, count, toPlanet.Center, filter);
        }

        public static Ship FindClosestTo(this Ship[] ships, int count, Vector2 to, Predicate<Ship> filter)
        {
            if (count <= 0 || !ships.FindFirstValid(count, filter, out int i, out Ship found))
                return null; // no elements passed the filter!
            
            float min = to.SqDist(found.Center);
            for (; i < count; ++i)
            {
                Ship ship = ships[i];
                if (filter(ship))
                {
                    float value = to.SqDist(ship.Center);
                    if (value < min)
                    {
                        min = value;
                        found = ship;
                    }
                }
            }
            return found;
        }
    }
}
