using System;
using System.Collections.Generic;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

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
        public static Ship FindClosestTo(this Array<Ship> ships, Ship toShip)
        {
            return FindClosestTo(ships.GetInternalArrayItems(), ships.Count, toShip.Position);
        }

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
            float min = to.SqDist(found.Position);
            for (int i = 1; i < count; ++i)
            {
                Ship ship = ships[i];
                float distance = to.SqDist(ship.Position);
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

        public static Planet FindClosestTo(this Planet[] planets, Ship toShip)
        {
            return FindClosestTo(planets, planets.Length, toShip.Position);
        }

        public static Planet FindClosestTo(this Planet[] planets, Vector2 pos)
        {
            return FindClosestTo(planets, planets.Length, pos);
        }

        public static Planet FindClosestTo(this Array<Planet> planets, Ship toShip)
        {
            return FindClosestTo(planets.GetInternalArrayItems(), planets.Count, toShip.Position);
        }

        public static Planet FindClosestTo(this Array<Planet> planets, Vector2 pos)
        {
            return FindClosestTo(planets.GetInternalArrayItems(), planets.Count, pos);
        }

        public static Planet FindClosestTo(this IReadOnlyList<Planet> planets, int count, Vector2 to)
        {
            if (count <= 0)
                return null;

            Planet found = planets[0]; // must return a value
            float min = to.SqDist(found.Center);
            for (int i = 1; i < count; ++i)
            {
                Planet planet = planets[i];
                float distance = to.SqDist(planet.Center);
                if (distance < min)
                {
                    min = distance;
                    found = planet;
                }
            }
            return found;
        }
        
        public static Planet FindClosestTo(this IReadOnlyList<Planet> planets, Ship toShip, Predicate<Planet> filter)
        {
            return FindClosestTo(planets, planets.Count, toShip.Position, filter);
        }

        public static Planet FindClosestTo(this IReadOnlyList<Planet> planets, Vector2 to, Predicate<Planet> filter)
        {
            return FindClosestTo(planets, planets.Count, to, filter);
        }

        public static Planet FindClosestTo(this IReadOnlyList<Planet> planets, int count, Vector2 to, Predicate<Planet> filter)
        {
            if (count <= 0 || !planets.FindFirstValid(count, filter, out int i, out Planet found))
                return null; // no elements passed the filter!

            float min = to.SqDist(found.Center);
            for (; i < count; ++i)
            {
                Planet planet = planets[i];
                if (filter(planet))
                {
                    float distance = to.SqDist(planet.Center);
                    if (distance < min)
                    {
                        min = distance;
                        found = planet; 
                    }
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
            
            float min = to.SqDist(found.Position);
            for (; i < count; ++i)
            {
                Ship ship = ships[i];
                if (filter(ship))
                {
                    float value = to.SqDist(ship.Position);
                    if (value < min)
                    {
                        min = value;
                        found = ship;
                    }
                }
            }
            return found;
        }

        public static SolarSystem FindClosestTo(this IReadOnlyList<SolarSystem> systems, SolarSystem toPlanet)
        {
            return FindClosestTo(systems, systems.Count, toPlanet.Position);
        }

        public static SolarSystem FindClosestTo(this SolarSystem[] systems, SolarSystem toPlanet)
        {
            return FindClosestTo(systems, systems.Length, toPlanet.Position);
        }

        public static SolarSystem FindClosestTo(this SolarSystem[] systems, int count, SolarSystem toPlanet)
        {
            return FindClosestTo(systems, count, toPlanet.Position);
        }

        public static SolarSystem FindClosestTo(this SolarSystem[] systems, Ship toShip)
        {
            return FindClosestTo(systems, systems.Length, toShip.Position);
        }

        public static SolarSystem FindClosestTo(this Array<SolarSystem> systems, Ship toShip)
        {
            return FindClosestTo(systems.GetInternalArrayItems(), systems.Count, toShip.Position);
        }

        public static SolarSystem FindClosestTo(this Array<SolarSystem> systems, Vector2 position)
        {
            return FindClosestTo(systems.GetInternalArrayItems(), systems.Count, position);
        }

        public static SolarSystem FindClosestTo(this IReadOnlyList<SolarSystem> systems, int count, Vector2 to)
        {
            if (count <= 0)
                return null;

            SolarSystem found = systems[0];
            float min = to.SqDist(found.Position);
            for (int i = 1; i < count; ++i)
            {
                SolarSystem target = systems[i];
                float distance = to.SqDist(target.Position);
                if (distance < min)
                {
                    min = distance;
                    found = target;
                }
            }
            return found;
        }

        public static SolarSystem FindFurthestFrom(this IReadOnlyList<SolarSystem> systems, Vector2 position)
        {
            return FindFurthestFrom(systems, systems.Count, position);
        }

        public static SolarSystem FindFurthestFrom(this IReadOnlyList<SolarSystem> systems, int count, Vector2 to)
        {
            if (count <= 0)
                return null;

            SolarSystem found = systems[0];
            float max = to.SqDist(found.Position);
            for (int i = 1; i < count; ++i)
            {
                SolarSystem target = systems[i];
                float distance = to.SqDist(target.Position);
                if (distance < max)
                {
                    max = distance;
                    found = target;
                }
            }
            return found;
        }
    }
}
