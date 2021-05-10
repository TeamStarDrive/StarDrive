using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class RandomEvent // Refactored by Fat Bastard - March 29, 2020
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public string NotificationString;
        [Serialize(2)] public int TurnTimer;
        [Serialize(3)] public bool InhibitWarp;
    }

    public sealed class RandomEventManager
    {
        public static RandomEvent ActiveEvent;

        public static void TryEventSpawn()
        {
            if (ActiveEvent != null)
                return;

            int random = RandomMath.RollDie(2000);

            if      (random == 1) HyperSpaceFlux();
            else if (random <= 3) ShiftInOrbit();
            else if (random <= 5) FoundMinerals();
            else if (random <= 7) VolcanicToHabitable();
            else if (random <= 15) Meteors();
        }

        static bool GetAffectedPlanet(Potentials potential, out Planet affectedPlanet, bool allowCapital = true)
        {
            affectedPlanet = null;
            var planetList = allowCapital ? Empire.Universe.PlanetsDict.Values.ToArray() 
                                          : Empire.Universe.PlanetsDict.Values.ToArray().Filter(p => !p.HasCapital);

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

        public static void UpdateEvents()
        {
            if (ActiveEvent == null)
            {
                TryEventSpawn();
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
                if (!planet.ParentSystem.HasPlanetsOwnedBy(EmpireManager.Player)
                    && !EmpireManager.Player.GetShips().Any(s => planet.Center.InRadius(s.Center, s.SensorRange)))
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

        static void HyperSpaceFlux()
        {
            ActiveEvent = new RandomEvent
            {
                TurnTimer          = (int)RandomMath.AvgRandomBetween(1f, 30f),
                Name               = "Hyperspace Flux",
                NotificationString = Localizer.Token(GameText.AMassiveHyperspaceFluxnisInhibiting),
                InhibitWarp        = true
            };

            Empire.Universe.NotificationManager.AddRandomEventNotification(
                ActiveEvent.NotificationString, null, null, null);
        }

        static void ShiftInOrbit() // Shifted in orbit (+ MaxFertility)
        {
            if (!GetAffectedPlanet(Potentials.Habitable, out Planet planet)) 
                return;

            planet.AddMaxBaseFertility(RandomMath.RollDie(5) / 10f); // 0.1 to 0.5 max base fertility
            NotifyPlayerIfAffected(planet, GameText.HasSuddenlyShiftedInIts);
            Log.Info($"Event Notification: Orbit Shift at {planet}");
        }

        static void Meteors()
        {
            if (Empire.Universe.StarDate < 1050 || !GetAffectedPlanet(Potentials.Habitable, out Planet planet))
                return;

            int rand       = RandomMath.RollDie(10);
            int numMeteors = RandomMath.IntBetween(rand * 3, rand * 10);
            CreateMeteors(planet, numMeteors);
            Log.Info($"{numMeteors} Meteors Created in {planet.ParentSystem.Name} targeting {planet.Name}");

            // todo notify player
        }

        static void CreateMeteors(Planet p, int numMeteors)
        {
            Vector2 origin    = GetMeteorOrigin(p);
            Vector2 direction = origin.DirectionToTarget(p.Center);
            float rotation    = direction.ToDegrees();
            int speed         = RandomMath.RollDie(1000, 500);
            for (int i = 0; i < numMeteors; i++)
            {
                string meteorName;
                switch (RandomMath.RollDie(7))
                {
                    default:
                    case 1: meteorName = "Meteor A"; break;
                    case 2: meteorName = "Meteor B"; break;
                    case 3: meteorName = "Meteor C"; break;
                    case 4: meteorName = "Meteor D"; break;
                    case 5: meteorName = "Meteor E"; break;
                    case 6: meteorName = "Meteor F"; break;
                    case 7: meteorName = "Meteor G"; break;
                }

                Vector2 pos = origin.GenerateRandomPointInsideCircle(p.GravityWellRadius);
                Ship meteor = Ship.CreateShipAtPoint(meteorName, EmpireManager.Unknown, pos);
                if (meteor == null)
                {
                    Log.Warning($"Meteors: Could not create {meteorName} is random event");
                    continue;
                }

                meteor.AI.AddMeteorGoal(p, rotation, direction, speed);
            }
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

        static void VolcanicToHabitable()
        {
            if (!GetAffectedPlanet(Potentials.Improved, out Planet planet)) 
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

        static void FoundMinerals() // Increase Mineral Richness
        {
            if (!GetAffectedPlanet(Potentials.HasOwner, out Planet planet)) 
                return;

            float size = RandomMath.RandomBetween(-0.25f, 0.75f).LowerBound(0.2f);
            planet.MineralRichness += (float)Math.Round(size, 2);
            string postText = $" {size.String(2)}";
            NotifyPlayerIfAffected(planet, GameText.RawMineralsWereDiscoverednmineralRichness, postText);
            Log.Info($"Event Notification: Minerals Found at {planet}");
        }
    }
}
