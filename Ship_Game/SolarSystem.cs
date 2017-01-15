using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public  sealed class SolarSystem: IDisposable
	{
		public string Name = "Random System";
		public bool CombatInSystem;
		public float combatTimer;
		public Guid guid = Guid.NewGuid();
		public int IndexOfResetEvent;
		public bool DontStartNearPlayer;
		public float DangerTimer;
		public float DangerUpdater = 10f;

		//public Array<Empire> OwnerList = new Array<Empire>();
        public HashSet<Empire> OwnerList = new HashSet<Empire>();
		public BatchRemovalCollection<Ship> ShipList = new BatchRemovalCollection<Ship>();
		public SpatialManager spatialManager = new SpatialManager();
		public bool isVisible;
		public Vector2 Position;
		public int RingsCount;

        //public Vector2 Size = new Vector2(200000f, 200000f);          //Not referenced in code, removing to save memory

        public Array<Planet> PlanetList = new Array<Planet>();
		public BatchRemovalCollection<Asteroid> AsteroidsList = new BatchRemovalCollection<Asteroid>();
        public Array<Moon> MoonList = new Array<Moon>();
		public string SunPath;
		public Map<Empire, bool> ExploredDict = new Map<Empire, bool>();
		public Array<Ring> RingList = new Array<Ring>();
		private int numberOfRings;
		public int StarRadius;
		public Array<SolarSystem> FiveClosestSystems = new Array<SolarSystem>(5);
		public Array<string> ShipsToSpawn = new Array<string>();
		public Array<FleetAndPos> FleetsToSpawn = new Array<FleetAndPos>();
		public Array<Anomaly> AnomaliesList = new Array<Anomaly>();
		public bool isStartingSystem;
		public Array<string> DefensiveFleets = new Array<string>();
        public Map<Empire,PredictionTimeout> predictionTimeout =new Map<Empire,PredictionTimeout>();

        public class PredictionTimeout
        {
            public float prediction;
            public float predictionTimeout;
            public float predictedETA;
            public void update(float time)
            {
                predictionTimeout -= time;
                predictedETA -= time;
                Log.Info("Prediction Timeout: {0}", predictionTimeout);
                Log.Info("Prediction ETA: {0}", predictedETA);
                Log.Info("Prediction: {0}", prediction);
            }
        }

		public SolarSystem()
		{
		}

		private static void AddMajorRemnantPresence(Planet newOrbital)
		{
			if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.customRemnantElements)
            {
                newOrbital.PlanetFleets.Add("Remnant Battlegroup");
            }
            else
            {
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Heavy Drone");
                newOrbital.Guardians.Add("Heavy Drone");
                newOrbital.Guardians.Add("Ancient Assimilator");
            }
		}

		private static void AddMinorRemnantPresence(Planet newOrbital)
		{
			if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.customRemnantElements)
            {
                newOrbital.PlanetFleets.Add("Remnant Vanguard");
            }
            else
            {
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Heavy Drone");
                newOrbital.Guardians.Add("Heavy Drone");
            }
		}

        private static void AddMiniRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Xeno Fighter");
            newOrbital.Guardians.Add("Xeno Fighter");
            newOrbital.Guardians.Add("Heavy Drone");
        }

        private static void AddSupportRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Support Drone");
            newOrbital.Guardians.Add("Support Drone");
        }

        private static void AddCarrierRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Ancient Carrier");
        }

        private static void AddTorpedoRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Ancient Torpedo Cruiser");
        }

        private static void AddRemnantPatrol(Planet newOrbital, UniverseData data)
        {
            newOrbital.PlanetFleets.Add("Remnant Patrol");
        }

        private static void AddRemnantGarrison(Planet newOrbital, UniverseData data)
        {
            newOrbital.PlanetFleets.Add("Remnant Garrison");
        }

        // @todo This method is huge, find a way to generalize the logic, perhaps by changing the logic into something more generic
        private static void GenerateRemnantPresence(Planet newOrbital, UniverseData data)
        {
            float quality = newOrbital.Fertility + newOrbital.MineralRichness + newOrbital.MaxPopulation / 1000f;
			if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.customRemnantElements)
            {
                if (quality > 6f && quality < 10f)
                {
                    int n = RandomMath.IntBetween(0, 100);
                    if (n > 20 && n < 50) AddRemnantPatrol(newOrbital, data);
                    else if (n >= 50)   AddRemnantGarrison(newOrbital, data);
                }
                else if (quality > 10f)
                {
                    int n = RandomMath.IntBetween(0, 100);
                    if (n > 50 && n < 85) AddMinorRemnantPresence(newOrbital);
                    else if (n >= 85)     AddMajorRemnantPresence(newOrbital);
                }
            }
            else
            {   
                //Boost the quality score for planets that are very rich, or very fertile
                if (newOrbital.Fertility > 1.6)      quality += 1;
                if (newOrbital.MineralRichness >1.6) quality += 1;
                        
                //Added by Gretman
                if (GlobalStats.ExtraRemnantGS == 0)  //Rare Remnant
                {
                    if (quality > 8f)
                    {
                        int chance = RandomMath.IntBetween(0, 100);
                        if (chance > 70) AddMajorRemnantPresence(newOrbital); // RedFox, changed the rare remnant to Major
                    }
                }
                else if (GlobalStats.ExtraRemnantGS == 1)  //Normal Remnant (Vanilla)
                {
                    int chance = RandomMath.IntBetween(0, 100);
                    if (quality > 6f && quality < 10f)
                    {
                        if (chance > 50) AddMinorRemnantPresence(newOrbital);
                    }
                    else if (quality >= 10f)
                    {
                        if (chance > 50) AddMajorRemnantPresence(newOrbital);
                    }
                }
                else if (GlobalStats.ExtraRemnantGS == 2)  //More Remnant
                {
                    int chance = RandomMath.IntBetween(0, 100);
                    if (quality > 6f && quality < 9f)
                    {
                        if (chance > 35) AddMinorRemnantPresence(newOrbital);
                        if (chance > 70) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 9f && quality < 12f)
                    {
                        if (chance > 25) AddMinorRemnantPresence(newOrbital);
                        if (chance > 45) AddMajorRemnantPresence(newOrbital);
                        if (chance > 65) AddMiniRemnantPresence(newOrbital);
                        if (chance > 85) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 12f)
                    {
                        if (chance > 15) AddMajorRemnantPresence(newOrbital);
                        if (chance > 30) AddMinorRemnantPresence(newOrbital);
                        if (chance > 45) AddSupportRemnantPresence(newOrbital);
                        if (chance > 65) AddMiniRemnantPresence(newOrbital);
                        if (chance > 75) AddMiniRemnantPresence(newOrbital);
                        if (chance > 85) AddMiniRemnantPresence(newOrbital);
                    }
                }
                else if (GlobalStats.ExtraRemnantGS == 3)  //MuchMore Remnant
                {
                    int chance = RandomMath.IntBetween(0, 100);
                    if (quality > 4f && quality < 6f)
                    {
                        if (chance > 50) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 6f && quality < 8f)
                    {
                        if (chance > 25) AddMinorRemnantPresence(newOrbital);
                        if (chance > 50) AddMiniRemnantPresence(newOrbital);
                        if (chance > 75) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 8f && quality < 10f)
                    {
                        if (chance > 15) AddMinorRemnantPresence(newOrbital);
                        if (chance > 35) AddMajorRemnantPresence(newOrbital);
                        if (chance > 50) AddSupportRemnantPresence(newOrbital);
                        if (chance > 65) AddMinorRemnantPresence(newOrbital);
                        if (chance > 80) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 10f && quality < 12f)
                    {
                        if (chance > 05) AddMajorRemnantPresence(newOrbital);
                        if (chance > 25) AddMinorRemnantPresence(newOrbital);
                        if (chance > 30) AddSupportRemnantPresence(newOrbital);
                        if (chance > 45) AddMinorRemnantPresence(newOrbital);
                        if (chance > 60) AddMiniRemnantPresence(newOrbital);
                        if (chance > 70) AddMiniRemnantPresence(newOrbital);
                        if (chance > 80) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 12f)
                    {
                        if (chance > 00) AddMajorRemnantPresence(newOrbital);
                        if (chance > 10) AddMinorRemnantPresence(newOrbital);
                        if (chance > 20) AddSupportRemnantPresence(newOrbital);
                        if (chance > 40) AddMinorRemnantPresence(newOrbital);
                        if (chance > 55) AddMiniRemnantPresence(newOrbital);
                        if (chance > 70)
                        {
                            if (RandomMath.IntBetween(0, 100) > 50)   //50-50 chance of Carrier or Torpedo Remnant
                                AddCarrierRemnantPresence(newOrbital);
                            else AddTorpedoRemnantPresence(newOrbital);
                        }
                    }
                }
                else if (GlobalStats.ExtraRemnantGS == 4)  //Remnant Everywhere!
                {
                    int chance = RandomMath.IntBetween(0, 100);
                    if (quality > 2f && quality < 4f)
                    {
                        if (chance > 50) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 4f && quality < 6f)
                    {
                        if (chance > 30) AddMiniRemnantPresence(newOrbital);
                        if (chance > 50) AddMiniRemnantPresence(newOrbital);
                        if (chance > 80) AddMiniRemnantPresence(newOrbital);
                    }
                    else if (quality >= 6f && quality < 8f)
                    {
                        if (chance > 10) AddMinorRemnantPresence(newOrbital);
                        if (chance > 30) AddMiniRemnantPresence(newOrbital);
                        if (chance > 50) AddMiniRemnantPresence(newOrbital);
                        if (chance > 70) AddSupportRemnantPresence(newOrbital);
                    }
                    else if (quality >= 8f && quality < 10f)
                    {
                        if (chance > 00) AddMinorRemnantPresence(newOrbital);
                        if (chance > 20) AddMajorRemnantPresence(newOrbital);
                        if (chance > 40) AddMiniRemnantPresence(newOrbital);
                        if (chance > 50) AddSupportRemnantPresence(newOrbital);
                        if (chance > 70)
                        {
                            if (RandomMath.IntBetween(0, 100) > 50)   //50-50 chance of Carrier or Torpedo Remnant
                                AddCarrierRemnantPresence(newOrbital);
                            else AddTorpedoRemnantPresence(newOrbital);
                        }
                    }
                    else if (quality >= 10f && quality < 12f)
                    {
                        if (chance > 00) AddMajorRemnantPresence(newOrbital);
                        if (chance > 00) AddMinorRemnantPresence(newOrbital);
                        if (chance > 20) AddSupportRemnantPresence(newOrbital);
                        if (chance > 40) AddMiniRemnantPresence(newOrbital);
                        if (chance > 60)
                        {
                            if (RandomMath.IntBetween(0, 100) > 50)   //50-50 chance of Carrier or Torpedo Remnant
                                AddCarrierRemnantPresence(newOrbital);
                            else AddTorpedoRemnantPresence(newOrbital);
                        }
                        if (chance > 85)
                        {
                            if (RandomMath.IntBetween(0, 100) > 50)   //50-50 chance of Carrier or Torpedo Remnant
                                AddCarrierRemnantPresence(newOrbital);
                            else AddTorpedoRemnantPresence(newOrbital);
                        }
                    }
                    else if (quality >= 12f)
                    {
                        if (chance > 00) AddMajorRemnantPresence(newOrbital);
                        if (chance > 00) AddMinorRemnantPresence(newOrbital);
                        if (chance > 00) AddSupportRemnantPresence(newOrbital);
                        if (chance > 20) AddMinorRemnantPresence(newOrbital);
                        if (chance > 40)
                        {
                            if (RandomMath.IntBetween(0, 100) > 50)   //50-50 chance of Carrier or Torpedo Remnant
                                AddCarrierRemnantPresence(newOrbital);
                            else AddTorpedoRemnantPresence(newOrbital);
                        }
                        if (chance > 60)
                        {
                            if (RandomMath.IntBetween(0, 100) > 50)   //50-50 chance of Carrier or Torpedo Remnant
                                AddCarrierRemnantPresence(newOrbital);
                            else AddTorpedoRemnantPresence(newOrbital);
                        }
                        if (chance > 80) AddMajorRemnantPresence(newOrbital);
                    }
                }
            }
        }

        private void SetSunPath(int whichSun)
        {
            switch (whichSun)
			{
                default:SunPath = "star_red";     break;
			    case 2: SunPath = "star_yellow";  break;
			    case 3: SunPath = "star_green";   break;
			    case 4: SunPath = "star_blue";    break;
			    case 5: SunPath = "star_yellow2"; break;
			    case 6: SunPath = "star_binary";  break;
			}
        }

		public void GenerateCorsairSystem(string name)
		{
            SetSunPath(RandomMath.IntBetween(1, 3));
			Name = name;
			numberOfRings = 2;
			RingsCount = numberOfRings;
			StarRadius = RandomMath.IntBetween(250, 500);
			for (int i = 1; i < numberOfRings + 1; i++)
			{
				float ringRadius = i * (StarRadius + RandomMath.RandomBetween(10500f, 12000f));
				if (i != 1)
				{
                    GenerateAsteroidRing(ringRadius, spread:3500f);
				}
				else
				{
					float scale = RandomMath.RandomBetween(1f, 2f);
                    float planetRadius = 1000f * scale;// (float)(1 + ((Math.Log(scale)) / 1.5));
					float randomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = Vector2.Zero.PointFromAngle(randomAngle, ringRadius);
					Planet newOrbital = new Planet
					{
						Name = Name + " " + NumberToRomanConvertor.NumberToRoman(i),
						OrbitalAngle = randomAngle,
						ParentSystem = this,
                        system       = this,
						planetType   = 22
					};
					newOrbital.SetPlanetAttributes();
					newOrbital.Position      = planetCenter;
					newOrbital.scale         = scale;
					newOrbital.ObjectRadius  = planetRadius;
					newOrbital.OrbitalRadius = ringRadius;
					newOrbital.planetTilt = RandomMath.RandomBetween(45f, 135f);
					if (RandomMath.IntBetween(1, 100) < 15)
					{
						newOrbital.hasRings = true;
						newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
					}
					float fertility       = newOrbital.Fertility;
					float mineralRichness = newOrbital.MineralRichness;
					float maxPopulation   = newOrbital.MaxPopulation / 1000f;
					newOrbital.CorsairPresence = true;
					PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					Ring ring = new Ring
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					RingList.Add(ring);
				}
			}
		}

		private static Vector2 GenerateRandomPointOnCircle(float radius, Vector2 center)
		{
			float randomAngle = RandomMath.RandomBetween(0f, 360f);
			return center.PointFromAngle(randomAngle, radius);
		}

		public void GenerateRandomSystem(string name, UniverseData data, float systemScale)
		{
            // Changed by RedFox: 3% chance to get a tri-sun star
            SetSunPath(RandomMath.IntBetween(0, 100) < 3 ? (6) : RandomMath.IntBetween(1, 5));

			Name = name;
			numberOfRings = RandomMath.IntBetween(1, 6);
            if (GlobalStats.ExtraPlanets > 0) // ADDED BY SHAHMATT (more planets in system)
            {   
                //Edited by Gretman, so if lots of extra planets are selected, there will definitely be extra
                if      (GlobalStats.ExtraPlanets < 2)  numberOfRings += RandomMath.IntBetween(1, GlobalStats.ExtraPlanets);
                else if (GlobalStats.ExtraPlanets < 4)  numberOfRings += RandomMath.IntBetween(2, GlobalStats.ExtraPlanets);
                else if (GlobalStats.ExtraPlanets == 6) numberOfRings += RandomMath.IntBetween(3, GlobalStats.ExtraPlanets);
                else                                    numberOfRings += RandomMath.IntBetween(0, GlobalStats.ExtraPlanets);

                if (numberOfRings == 0) numberOfRings = 1; // If "Extra Planets" was selected at all, there will always be at least 1 in each system. - Gretman
            }
			RingsCount = numberOfRings;
			StarRadius = RandomMath.IntBetween(250, 500);
            float ringbase = 10500f;
            float ringmax = RingsCount > 0 ? (95000f - StarRadius) / numberOfRings : 0f;

			for (int i = 1; i < numberOfRings + 1; i++)
			{
                if (RingList.Count > 1)
                {
                    ringbase = RingList[RingList.Count - 1].Distance + 5000;
                    Planet p = RingList[RingList.Count - 1].planet;
                    if (p != null)
                        ringbase += p.ObjectRadius;
                }
                
                float ringRadius = ringbase + RandomMath.RandomBetween(0, ringmax);
				ringRadius *= systemScale;
				if (RandomMath.IntBetween(1, 100) > 80)
				{
                    GenerateAsteroidRing(ringRadius, spread:3500f*systemScale);
				}
				else
				{
					float randomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = MathExt.PointOnCircle(randomAngle, ringRadius);
					Planet newOrbital = new Planet
					{
						Name = Name + " " + NumberToRomanConvertor.NumberToRoman(i),
						OrbitalAngle = randomAngle,
						ParentSystem = this,
                        system       = this,
						planetType   = RandomMath.IntBetween(1, 24)
					};
					if ((newOrbital.planetType == 22 || newOrbital.planetType == 13) && RandomMath.RandomBetween(0f, 100f) > 50f)
					{
						newOrbital.planetType = RandomMath.IntBetween(1, 24);
					}

                    float scale = RandomMath.RandomBetween(0.9f, 1.8f);
                    if (newOrbital.planetType == 2  || newOrbital.planetType == 6  || newOrbital.planetType == 10 || 
                        newOrbital.planetType == 12 || newOrbital.planetType == 15 || newOrbital.planetType == 20 || newOrbital.planetType == 26)
                        scale += 2.5f;

                    float planetRadius = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
					newOrbital.SetPlanetAttributes();
					newOrbital.Position = planetCenter;
					newOrbital.scale = scale;
					newOrbital.ObjectRadius = planetRadius;
					newOrbital.OrbitalRadius = ringRadius;
					newOrbital.planetTilt = RandomMath.RandomBetween(45f, 135f);
					if (RandomMath.RandomBetween(1f, 100f) < 15f)
					{
						newOrbital.hasRings = true;
						newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
					}

                    GenerateRemnantPresence(newOrbital, data);

					PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					Ring ring = new Ring
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					RingList.Add(ring);
				}
			}
		}

		public void GenerateStartingSystem(string name, Empire owner, float systemScale)
		{
			isStartingSystem = true;
            SetSunPath(RandomMath.IntBetween(1, 6));

			Name = name;
            numberOfRings = GlobalStats.ExtraPlanets > 3 ? GlobalStats.ExtraPlanets : 3;
            numberOfRings += RandomMath.IntBetween(0, 1) + RandomMath.IntBetween(0, 1) + RandomMath.IntBetween(0, 1);
            if (numberOfRings > 6)
                numberOfRings = 6;
			RingsCount = numberOfRings;
			StarRadius = RandomMath.IntBetween(250, 500);
			for (int i = 1; i < numberOfRings + 1; i++)
			{
				float ringRadius = i * (StarRadius +  RandomMath.RandomBetween(500f, 3500f) + 10000f);
				ringRadius = ringRadius * systemScale;
				if (i == 1 || i > 3)
				{
					float randomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = MathExt.PointOnCircle(randomAngle, ringRadius);
					Planet newOrbital = new Planet
					{
						Name = Name + " " + NumberToRomanConvertor.NumberToRoman(i),
						OrbitalAngle = randomAngle,
						ParentSystem = this,
                        system       = this,
						planetType   = RandomMath.IntBetween(1, 24)
					};
                    float scale = RandomMath.RandomBetween(0.9f, 1.8f);
                    if (newOrbital.planetType == 2  || newOrbital.planetType == 6  || newOrbital.planetType == 10 || 
                        newOrbital.planetType == 12 || newOrbital.planetType == 15 || newOrbital.planetType == 20 || newOrbital.planetType == 26)
                        scale += 2.5f;

                    float planetRadius = 1000f * (float)(1 + Math.Log(scale) / 1.5);
					newOrbital.SetPlanetAttributes();
					newOrbital.Position      = planetCenter;
					newOrbital.scale         = scale;
					newOrbital.ObjectRadius  = planetRadius;
					newOrbital.OrbitalRadius = ringRadius;
					newOrbital.planetTilt    = RandomMath.RandomBetween(45f, 135f);
					if (RandomMath.RandomBetween(1f, 100f) < 15f)
					{
						newOrbital.hasRings = true;
						newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
					}
					PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					Ring ring = new Ring
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					RingList.Add(ring);
				}
				else if (i == 2)
				{
                    GenerateAsteroidRing(ringRadius, spread:3500f*systemScale);
				}
				else if (i == 3)
				{
					float scale = RandomMath.RandomBetween(1f, 2f);
                    float planetRadius   = 1000f * (float)(1 + (Math.Log(scale) / 1.5));
					float randomAngle    = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = MathExt.PointOnCircle(randomAngle, ringRadius);
					Planet newOrbital = new Planet
					{
						Name = Name + " " + NumberToRomanConvertor.NumberToRoman(i),
						OrbitalAngle = randomAngle,
						ParentSystem = this,
                        system       = this,
                        planetType   = RandomMath.IntBetween(0, 1) == 0 ? 27 : 29
					};
					newOrbital.Owner = owner;
                    owner.Capital = newOrbital;
					newOrbital.InitializeSliders(owner);
					owner.AddPlanet(newOrbital);
					newOrbital.SetPlanetAttributes(26f);
					newOrbital.MineralRichness = 1f + owner.data.Traits.HomeworldRichMod;
					newOrbital.Special = "None";
					newOrbital.Fertility = 2f + owner.data.Traits.HomeworldFertMod;
					newOrbital.MaxPopulation = 14000f + 14000f * owner.data.Traits.HomeworldSizeMod;
					newOrbital.Population = 14000f;
					newOrbital.FoodHere = 100f;
					newOrbital.ProductionHere = 100f;
					newOrbital.HasShipyard = true;
					newOrbital.AddGood("ReactorFuel", 1000);
					ResourceManager.CreateBuilding("Capital City").SetPlanet(newOrbital);
					ResourceManager.CreateBuilding("Space Port").SetPlanet(newOrbital);
					if (GlobalStats.HardcoreRuleset)
					{
						ResourceManager.CreateBuilding("Fissionables").SetPlanet(newOrbital);
						ResourceManager.CreateBuilding("Fissionables").SetPlanet(newOrbital);
						ResourceManager.CreateBuilding("Mine Fissionables").SetPlanet(newOrbital);
						ResourceManager.CreateBuilding("Fuel Refinery").SetPlanet(newOrbital);
					}
					newOrbital.Position = planetCenter;
					newOrbital.scale = scale;
					newOrbital.ObjectRadius = planetRadius;
					newOrbital.OrbitalRadius = ringRadius;
					newOrbital.planetTilt = RandomMath.RandomBetween(45f, 135f);
					if (RandomMath.RandomBetween(1f, 100f) < 15f)
					{
						newOrbital.hasRings = true;
						newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
					}
					PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					Ring ring = new Ring
					{
						Distance  = ringRadius,
						Asteroids = false,
						planet    = newOrbital
					};
					RingList.Add(ring);
				}
			}
		}

		public static SolarSystem GenerateSystemFromData(SolarSystemData data, Empire owner)
		{
			SolarSystem newSys = new SolarSystem()
			{
				SunPath = data.SunPath,
				Name = data.Name
			};
			int numberOfRings = data.RingList.Count;
			int randomBetween = RandomMath.IntBetween(50, 500);
			for (int i = 0; i < numberOfRings; i++)
			{
                int ringtype = RandomMath.IntBetween(1, 29);
                float ringRadius = 10000f + (randomBetween + RandomMath.RandomBetween(10500f, 12000f)) * (i+1);
                SolarSystemData.Ring ringData = data.RingList[i];

				if (ringData.Asteroids == null)
				{
                    int whichPlanet = ringData.WhichPlanet > 0 ? ringData.WhichPlanet : ringtype;
                    float scale;
                    if (ringData.planetScale > 0)
                    {
                        scale = ringData.planetScale;
                    }
                    else
                    {
                        scale = RandomMath.RandomBetween(0.9f, 1.8f);
                        if (whichPlanet == 2  || whichPlanet == 6  || whichPlanet == 10 || 
                            whichPlanet == 12 || whichPlanet == 15 || whichPlanet == 20 || whichPlanet == 26)
                            scale += 2.5f;
                    }

                    float planetRadius = 1000f * (float)(1 + ((Math.Log(scale)) / 1.5));
					float randomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = MathExt.PointOnCircle(randomAngle, ringRadius);
                    
                    Planet newOrbital = new Planet
					{
						Name               = ringData.Planet,
						OrbitalAngle       = randomAngle,
						ParentSystem       = newSys,
                        system             = newSys,
						SpecialDescription = ringData.SpecialDescription,
						planetType         = whichPlanet,
						Position           = planetCenter,
                        scale              = scale,
						ObjectRadius       = planetRadius,
						OrbitalRadius      = ringRadius,
						planetTilt         = RandomMath.RandomBetween(45f, 135f)
					};
					newOrbital.InitializeUpdate();
                    if (!ringData.HomePlanet || owner == null)
					{
                        if (ringData.UniqueHabitat)
                        {
                            newOrbital.UniqueHab = true;
                            newOrbital.uniqueHabPercent = ringData.UniqueHabPC;
                        }
                        newOrbital.SetPlanetAttributes();
                        if (ringData.MaxPopDefined > 0)
                            newOrbital.MaxPopulation = ringData.MaxPopDefined * 1000f;
                        if (!string.IsNullOrEmpty(ringData.Owner) && !string.IsNullOrEmpty(ringData.Owner))
                        {
                            newOrbital.Owner = EmpireManager.GetEmpireByName(ringData.Owner);
                            newOrbital.Owner.AddPlanet(newOrbital);
                            newOrbital.InitializeSliders(newOrbital.Owner);
                            newOrbital.Population      = newOrbital.MaxPopulation;
                            newOrbital.MineralRichness = 1f;
                            newOrbital.Fertility       = 2f;
                            newOrbital.colonyType      = Planet.ColonyType.Core;
                            newOrbital.GovernorOn      = true;
                        }
					}
					else
					{
						newOrbital.Owner = owner;
                        owner.Capital = newOrbital;
						newOrbital.InitializeSliders(owner);
						owner.AddPlanet(newOrbital);
						newOrbital.SetPlanetAttributes(26f);
						newOrbital.Special         = "None";
						newOrbital.MineralRichness = 1f + owner.data.Traits.HomeworldRichMod;
						newOrbital.Fertility       = 2f + owner.data.Traits.HomeworldFertMod;

                        if (ringData.MaxPopDefined > 0)
                        {
                            newOrbital.MaxPopulation = ringData.MaxPopDefined*1000f + ringData.MaxPopDefined*1000f*owner.data.Traits.HomeworldSizeMod;
                        }
                        else
                        {
                            newOrbital.MaxPopulation = 14000f + 14000f * owner.data.Traits.HomeworldSizeMod;
                        }
						newOrbital.Population = 14000f;
						newOrbital.FoodHere = 100f;
						newOrbital.ProductionHere = 100f;
						if (!newSys.OwnerList.Contains(newOrbital.Owner))
						{
							newSys.OwnerList.Add(newOrbital.Owner);
						}
						newOrbital.HasShipyard = true;
						newOrbital.AddGood("ReactorFuel", 1000);
						ResourceManager.CreateBuilding("Capital City").SetPlanet(newOrbital);
						ResourceManager.CreateBuilding("Space Port").SetPlanet(newOrbital);
						if (GlobalStats.HardcoreRuleset)
						{
							ResourceManager.CreateBuilding("Fissionables").SetPlanet(newOrbital);
							ResourceManager.CreateBuilding("Fissionables").SetPlanet(newOrbital);
							ResourceManager.CreateBuilding("Mine Fissionables").SetPlanet(newOrbital);
							ResourceManager.CreateBuilding("Fuel Refinery").SetPlanet(newOrbital);
						}
					}
					if (ringData.HasRings != null)
					{
						newOrbital.hasRings = true;
						newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
					}
                    //Add buildings to planet
                    if (ringData.BuildingList.Count > 0)
                        foreach (string building in ringData.BuildingList)
                            ResourceManager.CreateBuilding(building).SetPlanet(newOrbital);
                    //Add ships to orbit
                    if (ringData.Guardians.Count > 0)
                        foreach (string ship in ringData.Guardians)
                            newOrbital.Guardians.Add(ship);
                    //Add moons to planets
                    if (ringData.Moons.Count > 0)
                    {
                        for (int j = 0; j < ringData.Moons.Count; j++)
                        {
                            float radius = newOrbital.ObjectRadius * 5 + RandomMath.RandomBetween(1000f, 1500f) * (j + 1);
                            Moon moon = new Moon
                            {
                                orbitTarget  = newOrbital.guid,
                                moonType     = ringData.Moons[j].WhichMoon,
                                scale        = ringData.Moons[j].MoonScale,
                                OrbitRadius  = radius,
                                OrbitalAngle = RandomMath.RandomBetween(0f, 360f),
                                Position     = GenerateRandomPointOnCircle(radius, newOrbital.Position)
                            };
                            newSys.MoonList.Add(moon);
                        }
                    }
					newSys.PlanetList.Add(newOrbital);
					Ring ring = new Ring
					{
						Distance  = ringRadius,
						Asteroids = false,
						planet    = newOrbital
					};
					newSys.RingList.Add(ring);
				}
				else
				{
                    newSys.GenerateAsteroidRing(ringRadius, spread:3000f, scaleMin:1.2f, scaleMax:4.6f);
				}
			}
			return newSys;
		}

		public float GetActualStrengthPresent(Empire e)
		{
			float strength = 0f;
			foreach (Ship ship in ShipList)
			{
				if (ship.loyalty != e)
					continue;
				strength += ship.GetStrength();
			}
			return strength;
		}

        public int GetPredictedEnemyPresence(float time, Empire us)
        {
             
            float prediction =us.GetGSAI().ThreatMatrix.PingRadarStr(Position, RingList[RingsCount - 1].Distance *2,us);
            return (int)prediction;

        }

		private bool NoAsteroidProximity(Vector2 pos)
        {
            foreach (Asteroid asteroid in AsteroidsList)
                if (new Vector2(asteroid.Position3D.X, asteroid.Position3D.Y).SqDist(pos) < 200.0f*200.0f)
                    return false;
            return true;
        }

        private Vector3 GenerateAsteroidPos(float ringRadius, float spread)
        {
            for (int i = 0; i < 100; ++i) // while (true) would be unsafe, so give up after 100 turns
            {
                Vector2 pos = GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-spread, spread), Vector2.Zero);
                if (NoAsteroidProximity(pos))
                    return new Vector3(pos.X, pos.Y, -500f);
            }
            return Vector3.Zero; // should never reach this point, but if it does... we don't care, just don't crash or freeze
        }

        private void GenerateAsteroidRing(float ringRadius, float spread, float scaleMin=0.75f, float scaleMax=1.6f)
        {
            int numberOfAsteroids = RandomMath.IntBetween(150, 250);
			for (int i = 0; i < numberOfAsteroids; ++i)
			{
				AsteroidsList.Add(new Asteroid
				{
					Scale      = RandomMath.RandomBetween(scaleMin, scaleMax),
					Position3D = GenerateAsteroidPos(ringRadius, spread)
				});
			}
			RingList.Add(new Ring
			{
				Distance  = ringRadius,
				Asteroids = true
			});
        }

		public struct FleetAndPos
		{
			public string fleetname;
			public Vector2 Pos;
		}

		public struct Ring
		{
			public float Distance;
			public bool Asteroids;
			public Planet planet;
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SolarSystem() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            ShipList?.Dispose(ref ShipList);
            AsteroidsList?.Dispose(ref AsteroidsList);
            spatialManager?.Dispose(ref spatialManager);
        }
	}
}