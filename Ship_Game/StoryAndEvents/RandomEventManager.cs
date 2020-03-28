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

            float random = RandomMath.RandomBetween(0f, 1000f);

            if (random < 1f) // Hyperspace Flux
            {
                ActiveEvent = new RandomEvent
                {
                    TurnTimer = (int)RandomMath.RandomBetween(10f, 40f),
                    Name = "Hyperspace Flux",
                    NotificationString = Localizer.Token(4010),
                    InhibitWarp = true
                };
                Empire.Universe.NotificationManager.AddRandomEventNotification(
                    ActiveEvent.NotificationString, null, null, null);
            }


            if (random > 2f && random < 4f) ShiftInOrbit();
            if (random > 4f && random < 6f) Volcano();

            if (random > 6 && random < 8)   //Meteor Strike --  Added by Gretman
            {
                if (GetAffectedPlanet(Potentials.Habitable, out Planet targetPlanet))
                {
                    if (targetPlanet.IsExploredBy(EmpireManager.Player))
                    {
                        float sizeOfMeteor = RandomMath.RandomBetween(1, 3) / 10;
                        targetPlanet.AddMaxBaseFertility(-sizeOfMeteor);
                        targetPlanet.MineralRichness += sizeOfMeteor * 2;

                        string eventText = targetPlanet.Name + Localizer.Token(4105);
                        Empire.Universe.NotificationManager.AddRandomEventNotification(
                            eventText, targetPlanet.Type.IconPath, "SnapToPlanet", targetPlanet);
                    }
                    else Log.Info($"Something horrible would have happened to '{targetPlanet.Name}' but it was on a planet the player hasn't discovered yet.");
                }
            }
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

        enum Potentials
        {
            Habitable
        }

        // ***********
        // Event types
        // ***********

        static void ShiftInOrbit() // Shifted in orbit (+ MaxFertility)
        {
            if (GetAffectedPlanet(Potentials.Habitable, out Planet planet))
            {
                planet.AddMaxBaseFertility(0.5f);
                if (planet.IsExploredBy(EmpireManager.Player))
                {
                    string txt = planet.Name + Localizer.Token(4011);
                    Empire.Universe.NotificationManager.AddRandomEventNotification(
                        txt, planet.Type.IconPath, "SnapToPlanet", planet);
                }
            }
        }

        static void Volcano() // Volcano (- Fertility and pop per tile)
        {
            if (GetAffectedPlanet(Potentials.Habitable, out Planet planet))
            {
                planet.SetBaseFertility(0f, planet.BaseMaxFertility);
                planet.BasePopPerTile *= 0.65f;
                if (planet.IsExploredBy(EmpireManager.Player))
                {
                    string txt = planet.Name + Localizer.Token(4012);
                    Empire.Universe.NotificationManager.AddRandomEventNotification(
                        txt, planet.Type.IconPath, "SnapToPlanet", planet);
                }
            }
        }
    }
}