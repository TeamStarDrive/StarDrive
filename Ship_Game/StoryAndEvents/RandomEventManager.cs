namespace Ship_Game
{
    public sealed class RandomEvent
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
            else if (random <= 7) MeteorStrike();
        }

        static bool GetAffectedPlanet(Potentials potential, out Planet affectedPlanet)
        {
            affectedPlanet = null;
            var potentials = new Array<Planet>();
            foreach (Planet planet in Empire.Universe.PlanetsDict.Values.ToArray())
            {
                switch (potential)
                {
                    case Potentials.Habitable when planet.Habitable: potentials.Add(planet); break;
                    case Potentials.Volcano   when planet.Habitable: potentials.Add(planet); break;
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

        static void NotifyPlayerIfAffected(Planet planet, int token)
        {
            string fullText = $"{planet.Name} {Localizer.Token(token)}";
            if (planet.IsExploredBy(EmpireManager.Player))
            {
                Empire.Universe.NotificationManager.AddRandomEventNotification(
                    fullText, planet.Type.IconPath, "SnapToPlanet", planet);
            }
        }

        enum Potentials
        {
            Habitable,
            Volcano
        }

        // ***********
        // Event types
        // ***********

        static void HyperSpaceFlux()
        {
            ActiveEvent = new RandomEvent
            {
                TurnTimer          = (int)RandomMath.RandomBetween(10f, 40f),
                Name               = "Hyperspace Flux",
                NotificationString = Localizer.Token(4010),
                InhibitWarp        = true
            };

            Empire.Universe.NotificationManager.AddRandomEventNotification(
                ActiveEvent.NotificationString, null, null, null);
        }

        static void ShiftInOrbit() // Shifted in orbit (+ MaxFertility)
        {
            if (GetAffectedPlanet(Potentials.Habitable, out Planet planet))
            {
                planet.AddMaxBaseFertility(0.5f);
                NotifyPlayerIfAffected(planet, 4011);
            }

            Log.Info($"Event Notification: Orbit Shift at {planet}");
        }

        static void Volcano() // Volcano (- Fertility and pop per tile)
        {
            if (GetAffectedPlanet(Potentials.Habitable, out Planet planet))
            {
                planet.SetBaseFertility(0f, planet.BaseMaxFertility);
                planet.BasePopPerTile *= 0.65f;
                NotifyPlayerIfAffected(planet, 4012);
            }

            Log.Info($"Event Notification: Volcano at {planet}");
        }

        static void MeteorStrike() // Meteor Strike (- MaxFertility and pop)  -- Added by Gretman
        {
            if (GetAffectedPlanet(Potentials.Habitable, out Planet planet))
            {
                float sizeOfMeteor      = RandomMath.RandomBetween(-0.3f, 0.9f).LowerBound(0.1f);
                planet.Population      *= (1 - sizeOfMeteor);
                planet.MineralRichness += sizeOfMeteor;
                planet.AddMaxBaseFertility(-sizeOfMeteor);
                NotifyPlayerIfAffected(planet, 4015);
                Log.Info($"Event Notification: Meteor Strike at {planet}");
            }
        }
    }
}