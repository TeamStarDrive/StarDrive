using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    [StarDataType]
    public sealed class RandomEvent // Refactored by Fat Bastard - March 29, 2020
    {
        [StarData] public string Name;
        [StarData] public string NotificationString;
        [StarData] public int TurnTimer;
        [StarData] public bool InhibitWarp;
    }

    public sealed class RandomEventManager
    {
        public static RandomEvent ActiveEvent;

        public static void TryEventSpawn(UniverseScreen u)
        {
            int random = RandomMath.RollDie(2000);
            if      (random == 1) HyperSpaceFlux(u);
            else if (random <= 3) ShiftInOrbit(u);
            else if (random <= 5) FoundMinerals(u);
            else if (random <= 7) VolcanicToHabitable(u);
            else if (random <= 15) Meteors(u);
        }

        static bool GetAffectedPlanet(UniverseScreen u, Potentials potential, out Planet affectedPlanet, bool allowCapital = true)
        {
            affectedPlanet = null;
            var planetList = allowCapital ? u.Planets.ToArray()
                                          : u.Planets.Filter(p => !p.HasCapital);

            var potentials = new Array<Planet>();
            foreach (Planet planet in planetList)
            {
                switch (potential)
                {
                    case Potentials.Habitable when planet.Habitable:                           potentials.Add(planet); break;
                    case Potentials.Improved  when planet.Category == PlanetCategory.Volcanic: potentials.Add(planet); break;
                    case Potentials.HasOwner  when planet.Owner != null:                       potentials.Add(planet); break;
                }
            }

            if (potentials.Count > 0)
                affectedPlanet = potentials.RandItem();

            return affectedPlanet != null;
        }

        public static void UpdateEvents(UniverseScreen u)
        {
            if (ActiveEvent == null)
            {
                TryEventSpawn(u);
                return;
            }
            RandomEvent activeEvent = ActiveEvent;
            activeEvent.TurnTimer = activeEvent.TurnTimer - 1;
            if (ActiveEvent.TurnTimer <= 0)
            {
                ActiveEvent = null;
                Empire.Universe.NotificationManager.AddRandomEventNotification(Localizer.Token(GameText.TheHyperspaceFluxHasAbatednships), null, null, null);
            }
        }

        static void NotifyPlayerIfAffected(Planet planet, GameText message, string postText = "")
        {
            if (planet.Owner == null)
            {
                var ships = EmpireManager.Player.OwnedShips;
                if (!planet.ParentSystem.HasPlanetsOwnedBy(EmpireManager.Player)
                    && !ships.Any(s => planet.Center.InRadius(s.Position, s.SensorRange)))
                {
                    return;
                }
            }
            else
            {
                if (!planet.Owner.isPlayer 
                    && !planet.Owner.IsAlliedWith(EmpireManager.Player)
                    && !planet.Owner.IsTradeOrOpenBorders(EmpireManager.Player))
                {
                    return;
                }
            }

            string fullText = $"{planet.Name} {Localizer.Token(message)} {postText}";
            Empire.Universe.NotificationManager.AddRandomEventNotification(
                fullText, planet.Type.IconPath, "SnapToPlanet", planet);
        }

        enum Potentials
        {
            Habitable,
            Improved,
            HasOwner
        }

        // ***********
        // Event types
        // ***********

        static void HyperSpaceFlux(UniverseScreen u)
        {
            ActiveEvent = new RandomEvent
            {
                TurnTimer          = (int)RandomMath.AvgRandomBetween(1f, 30f),
                Name               = "Hyperspace Flux",
                NotificationString = Localizer.Token(GameText.AMassiveHyperspaceFluxnisInhibiting),
                InhibitWarp        = true
            };
            u.NotificationManager.AddRandomEventNotification(ActiveEvent.NotificationString, null, null, null);
        }

        static void ShiftInOrbit(UniverseScreen u) // Shifted in orbit (+ MaxFertility)
        {
            if (!GetAffectedPlanet(u, Potentials.Habitable, out Planet planet)) 
                return;

            planet.AddMaxBaseFertility(RandomMath.RollDie(5) / 10f); // 0.1 to 0.5 max base fertility
            NotifyPlayerIfAffected(planet, GameText.HasSuddenlyShiftedInIts);
            Log.Info($"Event Notification: Orbit Shift at {planet}");
        }

        static void Meteors(UniverseScreen u)
        {
            if (!GetAffectedPlanet(u, Potentials.Habitable, out Planet planet))
                return;

            CreateMeteors(planet);

            if (planet.Owner == EmpireManager.Player)
                Empire.Universe.NotificationManager.AddMeteorShowerTargetingOurPlanet(planet);
            else if (planet.ParentSystem.HasPlanetsOwnedBy(EmpireManager.Player))
                Empire.Universe.NotificationManager.AddMeteorShowerInSystem(planet);
        }

        public static void CreateMeteors(Planet p)
        {
            int rand = RandomMath.RollDie(12);
            int numMeteors = RandomMath.IntBetween(rand * 3, rand * 10).Clamped(3, (int)Empire.Universe.StarDate - 1000);

            int baseSpeed = RandomMath.RollDie(1000, 500);
            Vector2 origin = GetMeteorOrigin(p);
            
            // all meteors get the same direction, so some will miss the planet
            Vector2 direction = origin.DirectionToTarget(p.Center);
            float rotation = direction.ToDegrees();

            const string METEOR_VARIANTS = "ABCDEFG";

            for (int i = 0; i < numMeteors; i++)
            {
                Vector2 pos = origin.GenerateRandomPointInsideCircle(p.GravityWellRadius);

                string meteorName = "Meteor " + METEOR_VARIANTS[RandomMath.RollDie(7) - 1];
                var meteor = Ship.CreateShipAtPoint(Empire.Universe, meteorName, EmpireManager.Unknown, pos);
                if (meteor != null)
                {
                    float speed = RandomMath.IntBetween(baseSpeed-100, baseSpeed+100);
                    meteor.AI.AddMeteorGoal(p, rotation, direction, speed);
                }
                else
                {
                    Log.Warning($"Meteors: Could not create {meteorName} in random event");
                }
            }
            
            Log.Info($"{numMeteors} Meteors Created in {p.ParentSystem.Name} targeting {p.Name}");
        }

        static Vector2 GetMeteorOrigin(Planet p)
        {
            SolarSystem system = p.ParentSystem;
            var asteroidsRings = system.RingList.Filter(r => r.Asteroids);
            float originRadius;

            if (asteroidsRings.Length > 0 && RandomMath.RollDice(50))
                originRadius = asteroidsRings.RandItem().OrbitalDistance;
            else
                originRadius = system.Radius * 0.7f;

            return system.Position.GenerateRandomPointOnCircle(originRadius);
        }

        static void VolcanicToHabitable(UniverseScreen u)
        {
            if (!GetAffectedPlanet(u, Potentials.Improved, out Planet planet)) 
                return;

            PlanetCategory category = RandomMath.RollDice(75) ? PlanetCategory.Barren : PlanetCategory.Desert;
            PlanetType newType = ResourceManager.RandomPlanet(category);
            planet.GenerateNewFromPlanetType(newType, planet.Scale);
            planet.RecreateSceneObject();
            NotifyPlayerIfAffected(planet, GameText.HasExperiencedAMassiveVolcanic);
            int numVolcanoes = category == PlanetCategory.Barren ? RandomMath.RollDie(15) : RandomMath.RollDie(7);
            for (int i = 0; i < numVolcanoes; i++)
            {
                var potentialTiles = planet.TilesList.Filter(t => !t.VolcanoHere);
                if (potentialTiles.Length == 0)
                    break;

                PlanetGridSquare tile = potentialTiles.RandItem();
                tile.CreateVolcano(planet);
            }

            Log.Info($"Event Notification: Volcanic to Habitable at {planet} with {numVolcanoes} wanted");
        }

        static void FoundMinerals(UniverseScreen u) // Increase Mineral Richness
        {
            if (!GetAffectedPlanet(u, Potentials.HasOwner, out Planet planet)) 
                return;

            float size = RandomMath.RandomBetween(-0.25f, 0.75f).LowerBound(0.2f);
            planet.MineralRichness += (float)Math.Round(size, 2);
            string postText = $" {size.String(2)}";
            NotifyPlayerIfAffected(planet, GameText.RawMineralsWereDiscoverednmineralRichness, postText);
            Log.Info($"Event Notification: Minerals Found at {planet}");
        }
    }
}
