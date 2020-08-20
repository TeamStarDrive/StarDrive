using System;

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

            int random = RandomMath.IntBetween(1, 1000);

            if      (random == 1) HyperSpaceFlux();
            else if (random <= 3) ShiftInOrbit();
            else if (random <= 5) Volcano();
            else if (random <= 6) MeteorStrike();
            else if (random <= 7) VolcanicToHabitable();
            else if (random <= 9) FoundMinerals();
        }

        static bool GetAffectedPlanet(Potentials potential, out Planet affectedPlanet)
        {
            affectedPlanet = null;
            var potentials = new Array<Planet>();
            foreach (Planet planet in Empire.Universe.PlanetsDict.Values.ToArray())
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
                Empire.Universe.NotificationManager.AddRandomEventNotification(Localizer.Token(4009), null, null, null);
            }
        }

        static void NotifyPlayerIfAffected(Planet planet, int token, string postText = "")
        {
            if (!planet.IsExploredBy(EmpireManager.Player)) 
                return;

            string fullText = $"{planet.Name} {Localizer.Token(token)} {postText}";
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
                TurnTimer          = (int)RandomMath.AvgRandomBetween(1f, 40f),
                Name               = "Hyperspace Flux",
                NotificationString = Localizer.Token(4010),
                InhibitWarp        = true
            };

            Empire.Universe.NotificationManager.AddRandomEventNotification(
                ActiveEvent.NotificationString, null, null, null);
        }

        static void ShiftInOrbit() // Shifted in orbit (+ MaxFertility)
        {
            if (!GetAffectedPlanet(Potentials.Habitable, out Planet planet)) 
                return;

            planet.AddMaxBaseFertility(0.5f);
            NotifyPlayerIfAffected(planet, 4011);
            Log.Info($"Event Notification: Orbit Shift at {planet}");
        }

        static void Volcano() // Volcano (- Fertility and pop per tile)
        {
            if (!GetAffectedPlanet(Potentials.Habitable, out Planet planet)) 
                return;

            planet.SetBaseFertility(0f, planet.BaseMaxFertility);
            planet.BasePopPerTile *= 0.65f;
            NotifyPlayerIfAffected(planet, 4012);
            Log.Info($"Event Notification: Volcano at {planet}");
        }

        static void MeteorStrike() // Meteor Strike (- MaxFertility and pop)  -- Added by Gretman
        {
            if (!GetAffectedPlanet(Potentials.Habitable, out Planet planet)) 
                return;

            float sizeOfMeteor      = RandomMath.RandomBetween(-0.3f, 0.9f).LowerBound(0.1f);
            int token               = planet.Population > 0 ? 4105 : 4113;
            planet.Population      *= (1 - sizeOfMeteor);
            planet.MineralRichness += sizeOfMeteor;
            planet.AddMaxBaseFertility(-sizeOfMeteor);
            NotifyPlayerIfAffected(planet, token);
            Log.Info($"Event Notification: Meteor Strike at {planet}");
        }

        static void VolcanicToHabitable()
        {
            if (!GetAffectedPlanet(Potentials.Improved, out Planet planet)) 
                return;

            PlanetCategory category = RandomMath.RollDice(75) ? PlanetCategory.Barren 
                                                              : PlanetCategory.Desert;

            PlanetType newType = ResourceManager.RandomPlanet(category);
            planet.GenerateNewFromPlanetType(newType, planet.Scale);
            planet.RecreateSceneObject();
            NotifyPlayerIfAffected(planet, 4112);
            Log.Info($"Event Notification: Volcanic to Habitable at {planet}");
        }

        static void FoundMinerals() // Increase Mineral Richness
        {
            if (!GetAffectedPlanet(Potentials.HasOwner, out Planet planet)) 
                return;

            float size              = RandomMath.RandomBetween(-0.25f, 0.75f).LowerBound(0.2f);
            planet.MineralRichness += (float)Math.Round(size, 2);
            string postText         = $" {size.String(2)}";
            NotifyPlayerIfAffected(planet, 1867, postText);
            Log.Info($"Event Notification: Minerals Found at {planet}");
        }
    }
}