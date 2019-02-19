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
            return FindClosestTo(ships.GetInternalArrayItems(), ships.Count, toPlanet);
        }

        public static Ship FindClosestTo(this Ship[] ships, Planet toPlanet)
        {
            return FindClosestTo(ships, ships.Length, toPlanet);
        }

        public static Ship FindClosestTo(this Ship[] ships, int count, Planet toPlanet)
        {
            if (count <= 0)
                return null;

            Vector2 to = toPlanet.Center;
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
            return FindClosestTo(planets.GetInternalArrayItems(), planets.Count, toPlanet);
        }

        public static Planet FindClosestTo(this Planet[] planets, Planet toPlanet)
        {
            return FindClosestTo(planets, planets.Length, toPlanet);
        }

        public static Planet FindClosestTo(this Planet[] planets, int count, Planet toPlanet)
        {
            if (count <= 0)
                return null;

            Vector2 to = toPlanet.Center;
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
            return ships.FindMinFiltered(filter, s => s.Center.SqDist(to.Center));
        }

        public static Ship FindClosestTo(this Array<Ship> ships, Planet to, Predicate<Ship> filter)
        {
            return ships.FindMinFiltered(filter, s => s.Center.SqDist(to.Center));
        }

        public static Ship FindClosestTo(this Ship[] ships, int count, Planet toPlanet, Predicate<Ship> filter)
        {
            if (count <= 0) return null;

            Ship found = ships.FindFirstValid(count, filter, out int i);
            if (found == null) // no elements passed the filter!
                return null;
            
            Vector2 to = toPlanet.Center;
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
