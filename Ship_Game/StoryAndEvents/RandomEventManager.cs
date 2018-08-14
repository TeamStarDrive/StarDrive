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

            if (random < 1f)        //Hyperspace Flux
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


            if (random > 2f && random < 4f)         //Shifted in orbit (+ Fertility)
            {
                var potentials = new Array<Planet>();
                foreach (var planet in Empire.Universe.PlanetsDict)
                {
                    if (planet.Value.Habitable)
                        potentials.Add(planet.Value);
                }
                if (potentials.Count > 0)
                {
                    Planet toImprove = potentials[RandomMath.InRange(potentials.Count)];
                    if (toImprove.IsExploredBy(EmpireManager.Player))
                    {
                        toImprove.TerraformExternal(0.5f);
                        string txt = toImprove.Name + Localizer.Token(4011);
                        Empire.Universe.NotificationManager.AddRandomEventNotification(
                            txt, "Planets/"+toImprove.PlanetType, "SnapToPlanet", toImprove);
                    }
                }
            }

            if (random > 4f && random < 6f)     //Volcano (- Fertility)
            {
                var potentials = new Array<Planet>();
                foreach (var planet in Empire.Universe.PlanetsDict)
                {
                    if (planet.Value.Habitable)
                        potentials.Add(planet.Value);
                }
                if (potentials.Count > 0)
                {
                    Planet toImprove = potentials[RandomMath.InRange(potentials.Count)];
                    if (toImprove.IsExploredBy(EmpireManager.Player))
                    {
                        toImprove.TerraformExternal(-0.5f);
                        toImprove.MaxPopulation = toImprove.MaxPopulation * 0.65f;
                        if (toImprove.Population > toImprove.MaxPopulation)
                        {
                            toImprove.Population = toImprove.MaxPopulation;
                        }
                        string txt = toImprove.Name + Localizer.Token(4012);
                        Empire.Universe.NotificationManager.AddRandomEventNotification(
                            txt, "Planets/"+toImprove.PlanetType, "SnapToPlanet", toImprove);
                    }
                }
            }
            if (random > 6 && random < 8)   //Meteor Strike --  Added by Gretman
            {
                var potentials = new Array<Planet>();
                foreach (var planet in Empire.Universe.PlanetsDict)
                {
                    if (planet.Value.Habitable)
                        potentials.Add(planet.Value);
                }

                if (potentials.Count > 0)
                {
                    Planet targetplanet = potentials[RandomMath.InRange(potentials.Count)];
                    if (targetplanet.IsExploredBy(EmpireManager.Player))
                    {
                        float sizeofmeteor = RandomMath.RandomBetween(1, 3) / 10;
                        if (targetplanet.Fertility > 0) targetplanet.TerraformExternal(-sizeofmeteor);      //Lose half of the richness gained (if not already 0);
                        targetplanet.MineralRichness += sizeofmeteor * 2;

                        string eventtext = targetplanet.Name + Localizer.Token(4105);
                        Empire.Universe.NotificationManager.AddRandomEventNotification(
                            eventtext, "Planets/"+targetplanet.PlanetType, "SnapToPlanet", targetplanet);
                    }
                    else Log.Info($"Something horrible would have happened to '{targetplanet.Name}' but it was on a planet the player hasn't discovered yet.");
                }

            }
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
    }
}