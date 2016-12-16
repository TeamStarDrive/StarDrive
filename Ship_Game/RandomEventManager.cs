using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using MsgPack.Serialization;

namespace Ship_Game
{
    public sealed class RandomEvent
    {
        [MessagePackMember(0)] public string Name;
        [MessagePackMember(1)] public string NotificationString;
        [MessagePackMember(2)] public int TurnTimer;
        [MessagePackMember(3)] public bool InhibitWarp;
    }

    public sealed class RandomEventManager
	{
		public static RandomEvent ActiveEvent;

		public RandomEventManager()
		{
		}

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
		        var potentials = new List<Planet>();
		        foreach (var planet in Empire.Universe.PlanetsDict)
		        {
		            if (planet.Value.habitable)
		                potentials.Add(planet.Value);
		        }
		        if (potentials.Count > 0)
		        {
		            Planet toImprove = potentials[RandomMath.InRange(potentials.Count)];
		            if (toImprove.ExploredDict[EmpireManager.Player])
		            {
		                toImprove.TerraformExternal(0.5f);
		                string txt = toImprove.Name + Localizer.Token(4011);
		                Empire.Universe.NotificationManager.AddRandomEventNotification(
                            txt, "Planets/"+toImprove.planetType, "SnapToPlanet", toImprove);
		            }
		        }
		    }

		    if (random > 4f && random < 6f)     //Volcano (- Fertility)
		    {
		        var potentials = new List<Planet>();
		        foreach (var planet in Empire.Universe.PlanetsDict)
		        {
		            if (planet.Value.habitable)
		                potentials.Add(planet.Value);
		        }
		        if (potentials.Count > 0)
		        {
		            Planet toImprove = potentials[RandomMath.InRange(potentials.Count)];
		            if (toImprove.ExploredDict[EmpireManager.Player])
		            {
		                toImprove.TerraformExternal(-0.5f);
		                toImprove.MaxPopulation = toImprove.MaxPopulation * 0.65f;
		                if (toImprove.Population > toImprove.MaxPopulation)
		                {
		                    toImprove.Population = toImprove.MaxPopulation;
		                }
		                string txt = toImprove.Name + Localizer.Token(4012);
                        Empire.Universe.NotificationManager.AddRandomEventNotification(
                            txt, "Planets/"+toImprove.planetType, "SnapToPlanet", toImprove);
		            }
		        }
		    }
		    if (random > 6 && random < 8)   //Meteor Strike --  Added by Gretman
		    {
		        var potentials = new List<Planet>();
		        foreach (var planet in Empire.Universe.PlanetsDict)
		        {
		            if (planet.Value.habitable)
		                potentials.Add(planet.Value);
		        }

		        if (potentials.Count > 0)
		        {
		            Planet targetplanet = potentials[RandomMath.InRange(potentials.Count)];
		            if (targetplanet.ExploredDict[EmpireManager.Player])
		            {
		                float sizeofmeteor = RandomMath.RandomBetween(1, 3) / 10;
		                if (targetplanet.Fertility > 0) targetplanet.TerraformExternal(-sizeofmeteor);      //Lose half of the richness gained (if not already 0);
		                targetplanet.MineralRichness += sizeofmeteor * 2;

		                string eventtext = targetplanet.Name + Localizer.Token(4105);
                        Empire.Universe.NotificationManager.AddRandomEventNotification(
                            eventtext, "Planets/"+targetplanet.planetType, "SnapToPlanet", targetplanet);
		            }
		            else System.Diagnostics.Debug.WriteLine("Something horrible would have happened to '" + targetplanet.Name + "' but it was on a planet the player hasn't discovered yet.");
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