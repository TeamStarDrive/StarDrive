using System;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

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

    [StarDataType]
    public sealed class RandomEventManager
    {
        [StarData] public RandomEvent ActiveEvent;

        // for UNIT TESTING, allows us to disable random events while tests are running
        public bool Disabled;

        public void TryEventSpawn(UniverseState u)
        {
            if (Disabled)
                return;

            int random = RandomMath.RollDie(2000);
            if      (random == 1) HyperSpaceFlux(u);
            else if (random <= 3) ShiftInOrbit(u);
            else if (random <= 5) FoundMinerals(u);
            else if (random <= 7) VolcanicToHabitable(u);
            else if (random <= 15) Meteors(u);
        }

        bool GetAffectedPlanet(UniverseState u, Potentials potential, out Planet affectedPlanet, bool allowCapital = true)
        {
            affectedPlanet = null;
            var planetList = allowCapital ? u.Planets.ToArr()
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

        public void UpdateEvents(UniverseState u)
        {
            if (ActiveEvent == null)
            {
                TryEventSpawn(u);
                return;
            }
            RandomEvent activeEvent = ActiveEvent;
            activeEvent.TurnTimer--;
            if (ActiveEvent.TurnTimer <= 0)
            {
                ActiveEvent = null;
                u.Notifications?.AddRandomEventNotification(Localizer.Token(GameText.TheHyperspaceFluxHasAbatednships), null, null, null);
            }
        }

        static void NotifyPlayerIfAffected(Planet planet, GameText message, string postText = "")
        {
            if (planet.Owner == null)
            {
                var ships = planet.Universe.Player.OwnedShips;
                if (!planet.ParentSystem.HasPlanetsOwnedBy(planet.Universe.Player)
                    && !ships.Any(s => planet.Position.InRadius(s.Position, s.SensorRange)))
                {
                    return;
                }
            }
            else
            {
                if (!planet.OwnerIsPlayer 
                    && !planet.Owner.IsAlliedWith(planet.Universe.Player)
                    && !planet.Owner.IsTradeOrOpenBorders(planet.Universe.Player))
                {
                    return;
                }
            }

            string fullText = $"{planet.Name} {Localizer.Token(message)} {postText}";
            planet.Universe.Notifications?.AddRandomEventNotification(
                fullText, planet.PType.IconPath, "SnapToPlanet", planet);
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

        void HyperSpaceFlux(UniverseState u)
        {
            ActiveEvent = new RandomEvent
            {
                TurnTimer          = (int)RandomMath.AvgFloat(1f, 30f),
                Name               = "Hyperspace Flux",
                NotificationString = Localizer.Token(GameText.AMassiveHyperspaceFluxnisInhibiting),
                InhibitWarp        = true
            };
            u.Notifications?.AddRandomEventNotification(ActiveEvent.NotificationString, null, null, null);
        }

        void ShiftInOrbit(UniverseState u) // Shifted in orbit (+ MaxFertility)
        {
            if (!GetAffectedPlanet(u, Potentials.Habitable, out Planet planet)) 
                return;

            planet.AddMaxBaseFertility(RandomMath.RollDie(5) / 10f); // 0.1 to 0.5 max base fertility
            NotifyPlayerIfAffected(planet, GameText.HasSuddenlyShiftedInIts);
            Log.Info($"Event Notification: Orbit Shift at {planet}");
        }

        void Meteors(UniverseState u)
        {
            if (!GetAffectedPlanet(u, Potentials.Habitable, out Planet planet))
                return;

            CreateMeteors(planet);

            if (planet.OwnerIsPlayer)
                u.Notifications?.AddMeteorShowerTargetingOurPlanet(planet);
            else if (planet.ParentSystem.HasPlanetsOwnedBy(u.Player))
                u.Notifications?.AddMeteorShowerInSystem(planet);
        }

        public void CreateMeteors(Planet p)
        {
            int rand = RandomMath.RollDie(12);
            int numMeteors = RandomMath.Int(rand * 3, rand * 10).Clamped(3, (int)p.Universe.StarDate - 1000);

            int baseSpeed = RandomMath.RollDie(1000, 500);
            Vector2 origin = GetMeteorOrigin(p);
            
            // all meteors get the same direction, so some will miss the planet
            Vector2 direction = origin.DirectionToTarget(p.Position);
            float rotation = direction.ToDegrees();

            const string METEOR_VARIANTS = "ABCDEFG";

            for (int i = 0; i < numMeteors; i++)
            {
                Vector2 pos = origin.GenerateRandomPointInsideCircle(p.GravityWellRadius);

                string meteorName = "Meteor " + METEOR_VARIANTS[RandomMath.RollDie(7) - 1];
                var meteor = Ship.CreateShipAtPoint(p.Universe, meteorName, p.Universe.Unknown, pos);
                if (meteor != null)
                {
                    float speed = RandomMath.Int(baseSpeed-100, baseSpeed+100);
                    meteor.AI.AddMeteorGoal(p, rotation, direction, speed);
                }
                else
                {
                    Log.Warning($"Meteors: Could not create {meteorName} in random event");
                }
            }
            
            Log.Info($"{numMeteors} Meteors Created in {p.ParentSystem.Name} targeting {p.Name}");
        }

        Vector2 GetMeteorOrigin(Planet p)
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

        void VolcanicToHabitable(UniverseState u)
        {
            if (!GetAffectedPlanet(u, Potentials.Improved, out Planet planet)) 
                return;

            PlanetCategory category = RandomMath.RollDice(75) ? PlanetCategory.Barren : PlanetCategory.Desert;
            PlanetType newType = ResourceManager.Planets.RandomPlanet(category);
            var random = new SeededRandom();
            planet.GenerateNewFromPlanetType(random, newType, planet.Scale);
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

        void FoundMinerals(UniverseState u) // Increase Mineral Richness
        {
            if (!GetAffectedPlanet(u, Potentials.HasOwner, out Planet planet)) 
                return;

            float size = RandomMath.Float(-0.25f, 0.75f).LowerBound(0.2f);
            planet.MineralRichness += (float)Math.Round(size, 2);
            string postText = $" {size.String(2)}";
            NotifyPlayerIfAffected(planet, GameText.RawMineralsWereDiscoverednmineralRichness, postText);
            Log.Info($"Event Notification: Minerals Found at {planet}");
        }
    }
}
