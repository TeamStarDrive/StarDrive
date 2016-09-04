using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class RandomEventManager
	{
		public static RandomEvent ActiveEvent;

		public RandomEventManager()
		{
		}

		public static void TryEventSpawn()
		{
			if (RandomEventManager.ActiveEvent == null)
			{
				float Random = RandomMath.RandomBetween(0f, 1000f);


				if (Random < 1f)        //Hyperspace Flux
				{
					RandomEventManager.ActiveEvent = new RandomEvent()
					{
						TurnTimer = (int)RandomMath.RandomBetween(10f, 40f),
						Name = "Hyperspace Flux",
						NotificationString = Localizer.Token(4010),
						InhibitWarp = true
					};
					Ship.universeScreen.NotificationManager.AddRandomEventNotification(RandomEventManager.ActiveEvent.NotificationString, null, null, null);
				}


				if (Random > 2f && Random < 4f)         //Shifted in orbit (+ Fertility)
				{
					List<Planet> potentials = new List<Planet>();
					foreach (KeyValuePair<Guid, Planet> planet in Ship.universeScreen.PlanetsDict)
					{
						if (!planet.Value.habitable)
						{
							continue;
						}
						potentials.Add(planet.Value);
					}
					if (potentials.Count > 0)
					{
						Planet toImprove = potentials[HelperFunctions.GetRandomIndex(potentials.Count)];
						if (toImprove.ExploredDict[EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty)])
						{
							toImprove.TerraformExternal(0.5f);
							string txt = string.Concat(toImprove.Name, Localizer.Token(4011));
							Ship.universeScreen.NotificationManager.AddRandomEventNotification(txt, string.Concat("Planets/", toImprove.planetType), "SnapToPlanet", toImprove);
						}
					}
				}


				if (Random > 4f && Random < 6f)     //Volcano (- Fertility)
				{
					List<Planet> potentials = new List<Planet>();
					foreach (KeyValuePair<Guid, Planet> planet in Ship.universeScreen.PlanetsDict)
					{
						if (!planet.Value.habitable)
						{
							continue;
						}
						potentials.Add(planet.Value);
					}
					if (potentials.Count > 0)
					{
						Planet toImprove = potentials[HelperFunctions.GetRandomIndex(potentials.Count)];
						if (toImprove.ExploredDict[EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty)])
						{
							toImprove.TerraformExternal(-0.5f);
							toImprove.MaxPopulation = toImprove.MaxPopulation * 0.65f;
							if (toImprove.Population > toImprove.MaxPopulation)
							{
								toImprove.Population = toImprove.MaxPopulation;
							}
							string txt = string.Concat(toImprove.Name, Localizer.Token(4012));
							Ship.universeScreen.NotificationManager.AddRandomEventNotification(txt, string.Concat("Planets/", toImprove.planetType), "SnapToPlanet", toImprove);
						}
					}
				}
                if (Random > 6 && Random < 8)   //Meteor Strike --  Added by Gretman
                {
                    List<Planet> potentials = new List<Planet>();
                    foreach (KeyValuePair<Guid, Planet> planet in Ship.universeScreen.PlanetsDict)
                    {
                        if (!planet.Value.habitable)
                        {
                            continue;
                        }
                        potentials.Add(planet.Value);
                    }

                    if (potentials.Count > 0)
                    {
                        Planet targetplanet = potentials[HelperFunctions.GetRandomIndex(potentials.Count)];
                        if (targetplanet.ExploredDict[EmpireManager.GetEmpireByName(Planet.universeScreen.PlayerLoyalty)])
                        {

                        float sizeofmeteor = RandomMath.RandomBetween(1, 3) / 10;
                        if (targetplanet.Fertility > 0) targetplanet.TerraformExternal(-sizeofmeteor);      //Lose half of the richness gained (if not already 0);
                        targetplanet.MineralRichness += sizeofmeteor * 2;

                        string eventtext = string.Concat(targetplanet.Name, Localizer.Token(4105));
                        Ship.universeScreen.NotificationManager.AddRandomEventNotification(eventtext, string.Concat("Planets/", targetplanet.planetType), "SnapToPlanet", targetplanet);
                        }
                        else System.Diagnostics.Debug.WriteLine("Some shit would have happened to '" + targetplanet.Name + "' but it was on a planet the player hasn't discovered yet.");
                    }

                }
			}
		}

		public static void UpdateEvents()
		{
			if (RandomEventManager.ActiveEvent == null)
			{
				RandomEventManager.TryEventSpawn();
				return;
			}
			RandomEvent activeEvent = RandomEventManager.ActiveEvent;
			activeEvent.TurnTimer = activeEvent.TurnTimer - 1;
			if (RandomEventManager.ActiveEvent.TurnTimer <= 0)
			{
				RandomEventManager.ActiveEvent = null;
				Ship.universeScreen.NotificationManager.AddRandomEventNotification(Localizer.Token(4009), null, null, null);
			}
		}
	}
}