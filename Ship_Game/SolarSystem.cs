using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

namespace Ship_Game
{
	public class SolarSystem
	{
		public string Name = "Random System";

		public bool CombatInSystem;

		public float combatTimer;

		public Guid guid = Guid.NewGuid();

		public int IndexOfResetEvent;

		public bool DontStartNearPlayer;

		public float DangerTimer;

		public float DangerUpdater = 10f;

		public List<Empire> OwnerList = new List<Empire>();

		public BatchRemovalCollection<Ship> ShipList = new BatchRemovalCollection<Ship>();

		public RandomThreadMath RNG = new RandomThreadMath();

		public SpatialManager spatialManager = new SpatialManager();

		public bool isVisible;

		public Vector2 Position;

		public int RingsCount;

		public Vector2 Size = new Vector2(200000f, 200000f);

		public List<Planet> PlanetList = new List<Planet>();

		public BatchRemovalCollection<Asteroid> AsteroidsList = new BatchRemovalCollection<Asteroid>();

        public List<Moon> MoonList = new List<Moon>();

		public string SunPath;

		public Dictionary<Empire, bool> ExploredDict = new Dictionary<Empire, bool>();

		public List<SolarSystem.Ring> RingList = new List<SolarSystem.Ring>();

		private int numberOfRings;

		public int StarRadius;

		public List<SolarSystem> FiveClosestSystems = new List<SolarSystem>();

		public List<string> ShipsToSpawn = new List<string>();

		public List<SolarSystem.FleetAndPos> FleetsToSpawn = new List<SolarSystem.FleetAndPos>();

		public List<Anomaly> AnomaliesList = new List<Anomaly>();

		public bool isStartingSystem;

		public List<string> DefensiveFleets = new List<string>();
        
        public Dictionary<Empire,PredictionTimeout> predictionTimeout =new Dictionary<Empire,PredictionTimeout>();

            public class PredictionTimeout{
                public float prediction;
                public float predictionTimeout;
                public float predictedETA;
                public void update(float time)
                {
                    this.predictionTimeout -= time;
                    this.predictedETA -= time;
                    System.Diagnostics.Debug.WriteLine("Prediction Timeout: " + this.predictionTimeout);
                    System.Diagnostics.Debug.WriteLine("Prediction ETA: " + this.predictedETA);
                    System.Diagnostics.Debug.WriteLine("Prediction: " + this.prediction);
                    
                }
            }
		public SolarSystem()
		{
		}

		private void AddMajorRemnantPresence(Planet newOrbital, UniverseData data)
		{
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.customRemnantElements)
            {
                newOrbital.PlanetFleets.Add("Remnant Battlegroup");
            }
            else
            {
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                this.AddRemnantGunship(newOrbital, data);
                this.AddRemnantGunship(newOrbital, data);
                newOrbital.Guardians.Add("Ancient Assimilator");
            }
		}

		private void AddMinorRemnantPresence(Planet newOrbital, UniverseData data)
		{
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.customRemnantElements)
            {
                newOrbital.PlanetFleets.Add("Remnant Vanguard");
            }
            else
            {
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                newOrbital.Guardians.Add("Xeno Fighter");
                this.AddRemnantGunship(newOrbital, data);
                this.AddRemnantGunship(newOrbital, data);
            }
		}

        private void AddRemnantPatrol(Planet newOrbital, UniverseData data)
        {
            newOrbital.PlanetFleets.Add("Remnant Patrol");
        }

        private void AddRemnantGarrison(Planet newOrbital, UniverseData data)
        {
            newOrbital.PlanetFleets.Add("Remnant Garrison");
        }

		private void AddRemnantGunship(Planet newOrbital, UniverseData data)
		{
			newOrbital.Guardians.Add("Heavy Drone");
		}

		private void AddSlaverGroup()
		{
			this.DefensiveFleets.Add("Slaver Fleet");
		}

		private Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = angle;
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - D;
			}
			if (gamma == 90f)
			{
				TargetPosition.X = position.X + D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y + D;
			}
			if (gamma == 270f)
			{
				TargetPosition.X = position.X - D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			return TargetPosition;
		}

		public void GenerateCorsairSystem(string name)
		{
			int WhichSun = (int)RandomMath.RandomBetween(1f, 3f);
			if (WhichSun == 1)
			{
				this.SunPath = "star_red";
			}
			else if (WhichSun == 2)
			{
				this.SunPath = "star_yellow";
			}
			else if (WhichSun == 3)
			{
				this.SunPath = "star_green";
			}
			else if (WhichSun == 4)
			{
				this.SunPath = "star_blue";
			}
			else if (WhichSun == 5)
			{
				this.SunPath = "star_neutron";
			}
			else if (WhichSun == 6)
			{
				this.SunPath = "star_binary";
			}
			this.Name = name;
			this.numberOfRings = 2;
			this.RingsCount = this.numberOfRings;
			this.StarRadius = (int)RandomMath.RandomBetween(250f, 500f);
			for (int i = 1; i < this.numberOfRings + 1; i++)
			{
				float ringRadius = (float)i * ((float)this.StarRadius + RandomMath.RandomBetween(10500f, 12000f));
				if (i != 1)
				{
					float numberOfAsteroids = RandomMath.RandomBetween(150f, 250f);
					for (int k = 0; (float)k < numberOfAsteroids; k++)
					{
						Vector3 asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f, 3500f), Vector2.Zero), 0f);
						while (!this.RoidPosOK(asteroidCenter))
						{
							asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f, 3500f), Vector2.Zero), 0f);
						}
						Asteroid newRoid = new Asteroid()
						{
							scale = RandomMath.RandomBetween(0.75f, 1.6f),
							Position3D = asteroidCenter
						};
						int whichRoid = 0;
						while (whichRoid == 0 || whichRoid == 3)
						{
							whichRoid = (int)RandomMath.RandomBetween(1f, 9f);
						}
						newRoid.whichRoid = whichRoid;
						newRoid.Radius = RandomMath.RandomBetween(30f, 90f);
						this.AsteroidsList.Add(newRoid);
					}
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = true
					};
					this.RingList.Add(ring);
				}
				else
				{
					float scale = RandomMath.RandomBetween(1f, 2f);
					float planetRadius = 100f * scale;
					float RandomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = this.findPointFromAngleAndDistance(Vector2.Zero, RandomAngle, ringRadius);
					Planet newOrbital = new Planet()
					{
						Name = string.Concat(this.Name, " ", NumberToRomanConvertor.NumberToRoman(i)),
						OrbitalAngle = RandomAngle,
						ParentSystem = this,
						planetType = 22
					};
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
					float fertility = newOrbital.Fertility;
					float mineralRichness = newOrbital.MineralRichness;
					float maxPopulation = newOrbital.MaxPopulation / 1000f;
					newOrbital.CorsairPresence = true;
					this.PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					this.RingList.Add(ring);
				}
			}
		}

		private Vector2 GenerateRandomPointOnCircle(float radius, Vector2 center)
		{
			float RandomAngle = RandomMath.RandomBetween(0f, 360f);
			return this.findPointFromAngleAndDistance(center, RandomAngle, radius);
		}

		public void GenerateRandomSystem(string name, UniverseData data, float systemScale)
		{
            int WhichSun = (int)RandomMath.RandomBetween(1f, 6f);
            // CHANGED BY SHAHMATT
            if (WhichSun > 5)
            {
                if ((int)RandomMath.RandomBetween(0f, 100f) < 10)   // 10% for binary star (so 1/6 * 10% = 1/60 for binary star in system)
                {
                    WhichSun = 6;
                }
                else
                {
                    WhichSun = 5;
                }

            }
            // END OF CHANGED BY SHAHMATT
            
            //int WhichSun = (int)RandomMath.RandomBetween(1f, 6f);
            //if (WhichSun > 5)
            //{
            //    WhichSun = 5;
            //}
			if (WhichSun == 1)
			{
				this.SunPath = "star_red";
			}
			else if (WhichSun == 2)
			{
				this.SunPath = "star_yellow";
			}
			else if (WhichSun == 3)
			{
				this.SunPath = "star_green";
			}
			else if (WhichSun == 4)
			{
				this.SunPath = "star_blue";
			}
			else if (WhichSun == 5)
			{
				this.SunPath = "star_yellow2";
			}
			else if (WhichSun == 6)
			{
				this.SunPath = "star_binary";
			}
			this.Name = name;
			this.numberOfRings = (int)RandomMath.RandomBetween(1f, 6f);
            // ADDED BY SHAHMATT (more planets in system)
            if (GlobalStats.ExtraPlanets >0)
            {
                this.numberOfRings = this.numberOfRings + (int)RandomMath.RandomBetween(0f, (float)GlobalStats.ExtraPlanets);
            }
            // END OF ADDED BY SHAHMATT
			this.RingsCount = this.numberOfRings;
			this.StarRadius = (int)RandomMath.RandomBetween(250f, 500f);
			for (int i = 1; i < this.numberOfRings + 1; i++)
			{
				float ringRadius = (float)i * ((float)this.StarRadius + RandomMath.RandomBetween(10500f, 12000f) + 10000f);
				ringRadius = ringRadius * systemScale;
				if ((int)RandomMath.RandomBetween(1f, 100f) > 80)
				{
					float numberOfAsteroids = RandomMath.RandomBetween(150f, 250f);
					for (int k = 0; (float)k < numberOfAsteroids; k++)
					{
						Vector3 asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f * systemScale, 3500f * systemScale), Vector2.Zero), 0f);
						while (!this.RoidPosOK(asteroidCenter))
						{
							asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f * systemScale, 3500f * systemScale), Vector2.Zero), 0f);
						}
						Asteroid newRoid = new Asteroid()
						{
							scale = RandomMath.RandomBetween(0.75f, 1.6f),
							Position3D = asteroidCenter
						};
						int whichRoid = 0;
						while (whichRoid == 0 || whichRoid == 3)
						{
							whichRoid = (int)RandomMath.RandomBetween(1f, 9f);
						}
						newRoid.whichRoid = whichRoid;
						newRoid.Radius = RandomMath.RandomBetween(30f, 90f);
						this.AsteroidsList.Add(newRoid);
					}
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = true
					};
					this.RingList.Add(ring);
				}
				else
				{

					float RandomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = this.findPointFromAngleAndDistance(Vector2.Zero, RandomAngle, ringRadius);
					Planet newOrbital = new Planet()
					{
						Name = string.Concat(this.Name, " ", NumberToRomanConvertor.NumberToRoman(i)),
						OrbitalAngle = RandomAngle,
						ParentSystem = this,
						planetType = (int)RandomMath.RandomBetween(1f, 24f)
					};
					if ((newOrbital.planetType == 22 || newOrbital.planetType == 13) && RandomMath.RandomBetween(0f, 100f) > 50f)
					{
						newOrbital.planetType = (int)RandomMath.RandomBetween(1f, 24f);
					}

                    float scale = RandomMath.RandomBetween(0.9f, 1.8f);
                    if (newOrbital.planetType == 2 || newOrbital.planetType == 6 || newOrbital.planetType == 10 || newOrbital.planetType == 12 || newOrbital.planetType == 15 || newOrbital.planetType == 20 || newOrbital.planetType == 26)
                    {
                        scale += 2.5f;
                    }
                    float planetRadius = 100f * scale;
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
					float quality = newOrbital.Fertility + newOrbital.MineralRichness + newOrbital.MaxPopulation / 1000f;
                    if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.customRemnantElements)
                    {
                        if (quality > 6f && quality < 10f)
                        {
                            int iRandom = (int)RandomMath.RandomBetween(0f, 100f);
                            if (iRandom > 20 && iRandom < 50)
                            {
                                this.AddRemnantPatrol(newOrbital, data);
                            }
                            else if (iRandom >= 50)
                            {
                                this.AddRemnantGarrison(newOrbital, data);
                            }
                        }
                        else if (quality > 10f)
                        {
                            int iRandom = (int)RandomMath.RandomBetween(0f, 100f);
                            if (iRandom > 50 && iRandom < 85)
                            {
                                this.AddMinorRemnantPresence(newOrbital, data);
                            }
                            else if (iRandom >= 85)
                            {
                                this.AddMajorRemnantPresence(newOrbital, data);
                            }
                        }
                    }
                    else
                    {
                        if (quality > 6f && quality < 10f)
                        {
                            if ((int)RandomMath.RandomBetween(0f, 100f) > 50)
                            {
                                this.AddMinorRemnantPresence(newOrbital, data);
                            }
                        }
                        else if (quality > 10f && (int)RandomMath.RandomBetween(0f, 100f) < 50)
                        {
                            this.AddMajorRemnantPresence(newOrbital, data);
                        }
                    }
					this.PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					this.RingList.Add(ring);
				}
			}
		}

		public void GenerateRevoranSystem(string name, Empire Owner)
		{
			int WhichSun = (int)RandomMath.RandomBetween(1f, 5f);
			if (WhichSun == 1)
			{
				this.SunPath = "star_red";
			}
			else if (WhichSun == 2)
			{
				this.SunPath = "star_yellow";
			}
			else if (WhichSun == 3)
			{
				this.SunPath = "star_green";
			}
			else if (WhichSun == 4)
			{
				this.SunPath = "star_blue";
			}
			else if (WhichSun == 5)
			{
				this.SunPath = "star_neutron";
			}
			else if (WhichSun == 6)
			{
				this.SunPath = "star_binary";
			}
			this.Name = "Revoran";
			this.numberOfRings = 2;
			this.RingsCount = this.numberOfRings;
			this.StarRadius = (int)RandomMath.RandomBetween(250f, 500f);
			for (int i = 1; i < this.numberOfRings + 1; i++)
			{
				float ringRadius = (float)i * ((float)this.StarRadius + RandomMath.RandomBetween(10500f, 12000f));
				if (i != 1)
				{
					float numberOfAsteroids = RandomMath.RandomBetween(150f, 250f);
					for (int k = 0; (float)k < numberOfAsteroids; k++)
					{
						Vector3 asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f, 3500f), Vector2.Zero), 0f);
						while (!this.RoidPosOK(asteroidCenter))
						{
							asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f, 3500f), Vector2.Zero), 0f);
						}
						Asteroid newRoid = new Asteroid()
						{
							scale = RandomMath.RandomBetween(0.75f, 1.6f),
							Position3D = asteroidCenter
						};
						int whichRoid = 0;
						while (whichRoid == 0 || whichRoid == 3)
						{
							whichRoid = (int)RandomMath.RandomBetween(1f, 9f);
						}
						newRoid.whichRoid = whichRoid;
						newRoid.Radius = RandomMath.RandomBetween(30f, 90f);
						this.AsteroidsList.Add(newRoid);
					}
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = true
					};
					this.RingList.Add(ring);
				}
				else
				{
					float scale = RandomMath.RandomBetween(1f, 2f);
					float planetRadius = 100f * scale;
					float RandomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = this.findPointFromAngleAndDistance(Vector2.Zero, RandomAngle, ringRadius);
					Planet newOrbital = new Planet()
					{
						Name = string.Concat(this.Name, " ", NumberToRomanConvertor.NumberToRoman(i)),
						OrbitalAngle = RandomAngle,
						ParentSystem = this,
						planetType = 22
					};
					newOrbital.SetPlanetAttributes();
					newOrbital.Position = planetCenter;
					newOrbital.scale = scale;
					newOrbital.ObjectRadius = planetRadius;
					newOrbital.OrbitalRadius = ringRadius;
					newOrbital.planetTilt = RandomMath.RandomBetween(45f, 135f);
					newOrbital.Owner = Owner;
					newOrbital.InitializeSliders(Owner);
					Owner.AddPlanet(newOrbital);
					if (RandomMath.RandomBetween(1f, 100f) < 15f)
					{
						newOrbital.hasRings = true;
						newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
					}
					ResourceManager.GetBuilding("Capital City").SetPlanet(newOrbital);
					float fertility = newOrbital.Fertility;
					float mineralRichness = newOrbital.MineralRichness;
					float maxPopulation = newOrbital.MaxPopulation / 1000f;
					newOrbital.Population = newOrbital.MaxPopulation;
					newOrbital.Name = "Revoran";
					Troop t = ResourceManager.CreateTroop(ResourceManager.TroopsDict["Walker"], EmpireManager.GetEmpireByName("Revoran"));
					newOrbital.AssignTroopToTile(t);
					t = ResourceManager.CreateTroop(ResourceManager.TroopsDict["Walker"], EmpireManager.GetEmpireByName("Revoran"));
					newOrbital.AssignTroopToTile(t);
					t = ResourceManager.CreateTroop(ResourceManager.TroopsDict["Walker"], EmpireManager.GetEmpireByName("Revoran"));
					newOrbital.AssignTroopToTile(t);
					t = ResourceManager.CreateTroop(ResourceManager.TroopsDict["Walker"], EmpireManager.GetEmpireByName("Revoran"));
					newOrbital.AssignTroopToTile(t);
					this.PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					this.RingList.Add(ring);
				}
			}
		}

		public void GenerateStartingSystem(string name, Empire Owner, float systemScale)
		{
			this.isStartingSystem = true;
			int WhichSun = (int)RandomMath.RandomBetween(1f, 6f);
			if (WhichSun > 5)
			{
				WhichSun = 5;
			}
			if (WhichSun == 1)
			{
				this.SunPath = "star_red";
			}
			else if (WhichSun == 2)
			{
				this.SunPath = "star_yellow";
			}
			else if (WhichSun == 3)
			{
				this.SunPath = "star_green";
			}
			else if (WhichSun == 4)
			{
				this.SunPath = "star_blue";
			}
			else if (WhichSun == 5)
			{
				this.SunPath = "star_yellow2";
			}
			else if (WhichSun == 6)
			{
				this.SunPath = "star_binary";
			}
			this.Name = name;
			this.numberOfRings = 3;
			this.RingsCount = this.numberOfRings;
			this.StarRadius = (int)RandomMath.RandomBetween(250f, 500f);
			for (int i = 1; i < this.numberOfRings + 1; i++)
			{
				float ringRadius = (float)i * ((float)this.StarRadius + RandomMath.RandomBetween(10500f, 12000f) + 10000f);
				ringRadius = ringRadius * systemScale;
				if (i == 1)
				{
					float RandomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = this.findPointFromAngleAndDistance(Vector2.Zero, RandomAngle, ringRadius);
					Planet newOrbital = new Planet()
					{
						Name = string.Concat(this.Name, " ", NumberToRomanConvertor.NumberToRoman(i)),
						OrbitalAngle = RandomAngle,
						ParentSystem = this,
						planetType = (int)RandomMath.RandomBetween(1f, 24f)
					};
                    float scale = RandomMath.RandomBetween(0.9f, 1.8f);
                    if (newOrbital.planetType == 2 || newOrbital.planetType == 6 || newOrbital.planetType == 10 || newOrbital.planetType == 12 || newOrbital.planetType == 15 || newOrbital.planetType == 20 || newOrbital.planetType == 26)
                    {
                        scale += 2.5f;
                    }
                    float planetRadius = 100f * scale;
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
					this.PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					this.RingList.Add(ring);
				}
				else if (i == 2)
				{
					float numberOfAsteroids = RandomMath.RandomBetween(150f, 250f);
					for (int k = 0; (float)k < numberOfAsteroids; k++)
					{
						Vector3 asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f * systemScale, 3500f * systemScale), Vector2.Zero), 0f);
						while (!this.RoidPosOK(asteroidCenter))
						{
							asteroidCenter = new Vector3(this.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3500f * systemScale, 3500f * systemScale), Vector2.Zero), 0f);
						}
						Asteroid newRoid = new Asteroid()
						{
							scale = RandomMath.RandomBetween(0.75f, 1.6f),
							Position3D = asteroidCenter
						};
						int whichRoid = 0;
						while (whichRoid == 0 || whichRoid == 3)
						{
							whichRoid = (int)RandomMath.RandomBetween(1f, 9f);
						}
						newRoid.whichRoid = whichRoid;
						newRoid.Radius = RandomMath.RandomBetween(30f, 90f);
					}
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = true
					};
					this.RingList.Add(ring);
				}
				else if (i == 3)
				{
					float scale = RandomMath.RandomBetween(1f, 2f);
					float planetRadius = 100f * scale;
					float RandomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = this.findPointFromAngleAndDistance(Vector2.Zero, RandomAngle, ringRadius);
					Planet newOrbital = new Planet()
					{
						Name = string.Concat(this.Name, " ", NumberToRomanConvertor.NumberToRoman(i)),
						OrbitalAngle = RandomAngle,
						ParentSystem = this
					};
					int random = (int)RandomMath.RandomBetween(1f, 3f);
					if (random == 1)
					{
						newOrbital.planetType = 27;
					}
					else if (random == 2)
					{
						newOrbital.planetType = 29;
					}
					newOrbital.Owner = Owner;
                    Owner.Capital = newOrbital;
					newOrbital.InitializeSliders(Owner);
					Owner.AddPlanet(newOrbital);
					newOrbital.SetPlanetAttributes(26f);
					newOrbital.MineralRichness = 1f + Owner.data.Traits.HomeworldRichMod;
					newOrbital.Special = "None";
					newOrbital.Fertility = 2f + Owner.data.Traits.HomeworldFertMod;
					newOrbital.MaxPopulation = 14000f + 14000f * Owner.data.Traits.HomeworldSizeMod;
					newOrbital.Population = 14000f;
					newOrbital.FoodHere = 100f;
					newOrbital.ProductionHere = 100f;
					newOrbital.HasShipyard = true;
					newOrbital.AddGood("ReactorFuel", 1000);
					ResourceManager.GetBuilding("Capital City").SetPlanet(newOrbital);
					ResourceManager.GetBuilding("Space Port").SetPlanet(newOrbital);
					if (GlobalStats.HardcoreRuleset)
					{
						ResourceManager.GetBuilding("Fissionables").SetPlanet(newOrbital);
						ResourceManager.GetBuilding("Fissionables").SetPlanet(newOrbital);
						ResourceManager.GetBuilding("Mine Fissionables").SetPlanet(newOrbital);
						ResourceManager.GetBuilding("Fuel Refinery").SetPlanet(newOrbital);
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
					this.PlanetList.Add(newOrbital);
					RandomMath.RandomBetween(0f, 3f);
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					this.RingList.Add(ring);
				}
			}
		}

		public static SolarSystem GenerateSystemFromData(SolarSystemData data, Empire Owner)
		{
			SolarSystem newSys = new SolarSystem()
			{
				SunPath = data.SunPath,
				Name = data.Name
			};
			int numberOfRings = data.RingList.Count;
			int StarRadius = (int)RandomMath.RandomBetween(50f, 500f);
			for (int i = 1; i < numberOfRings + 1; i++)
			{
				float ringRadius = (float)((i * ((float)StarRadius + RandomMath.RandomBetween(10500f, 12000f))) + 10000f);
				if (data.RingList[i - 1].Asteroids == null)
				{
                    float scale = 1f;
                    if (data.RingList[i - 1].planetScale > 0)
                    {
                        scale = data.RingList[i - 1].planetScale;
                    }
                    else
                    {
                        scale = RandomMath.RandomBetween(0.9f, 1.8f);
                        if (data.RingList[i - 1].WhichPlanet == 2 || data.RingList[i - 1].WhichPlanet == 6 || data.RingList[i - 1].WhichPlanet == 10 || data.RingList[i - 1].WhichPlanet == 12 || data.RingList[i - 1].WhichPlanet == 15 || data.RingList[i - 1].WhichPlanet == 20 || data.RingList[i - 1].WhichPlanet == 26)
                        {
                            scale += 2.5f;
                        }
                    }
					float planetRadius = 100f * scale;
					float RandomAngle = RandomMath.RandomBetween(0f, 360f);
					Vector2 planetCenter = newSys.findPointFromAngleAndDistance(Vector2.Zero, RandomAngle, ringRadius);
					Planet newOrbital = new Planet()
					{
						Name = data.RingList[i - 1].Planet,
						OrbitalAngle = RandomAngle,
						ParentSystem = newSys,
						SpecialDescription = data.RingList[i - 1].SpecialDescription,
						planetType = data.RingList[i - 1].WhichPlanet,
						Position = planetCenter,
                        scale = scale,
						ObjectRadius = planetRadius,
						OrbitalRadius = ringRadius,
						planetTilt = RandomMath.RandomBetween(45f, 135f)
					};
                    if (data.RingList[i - 1].Moons.Count > 0)
                    {
                        for (int j = 0; j < data.RingList[i - 1].Moons.Count; j++)
                        {
                            float radius = (newOrbital.ObjectRadius * 10) + (1000 * (j + 1));
                            Moon moon = new Moon()
                            {
                                planet = newOrbital,
                                moonType = data.RingList[i - 1].Moons[j].WhichMoon,
                                scale = data.RingList[i - 1].Moons[j].MoonScale,
                                OrbitRadius = radius,
                                OrbitalAngle = RandomMath.RandomBetween(0f, 360f),
                                Position = newSys.GenerateRandomPointOnCircle(radius, newOrbital.Position)
                            };
                            newSys.MoonList.Add(moon);
                        }
                    }
					newOrbital.InitializeUpdate();
					if (!data.RingList[i - 1].HomePlanet)
					{
                        newOrbital.SetPlanetAttributes();
                        if (data.RingList[i - 1].MaxPopDefined > 0)
                        {
                            newOrbital.MaxPopulation = data.RingList[i - 1].MaxPopDefined * 1000f;
                        }						
					}
					else
					{
						newOrbital.Owner = Owner;
                        Owner.Capital = newOrbital;
						newOrbital.InitializeSliders(Owner);
						Owner.AddPlanet(newOrbital);
						newOrbital.SetPlanetAttributes(26f);
						newOrbital.MineralRichness = 1f + Owner.data.Traits.HomeworldRichMod;
						newOrbital.Special = "None";
						newOrbital.Fertility = 2f + Owner.data.Traits.HomeworldFertMod;

                        if (data.RingList[i - 1].MaxPopDefined > 0)
                        {
                            newOrbital.MaxPopulation = (data.RingList[i - 1].MaxPopDefined * 1000f) + ((data.RingList[i - 1].MaxPopDefined * 1000f) *  Owner.data.Traits.HomeworldSizeMod);
                        }
                        else
                        {
                            newOrbital.MaxPopulation = 14000f + 14000f * Owner.data.Traits.HomeworldSizeMod;
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
						ResourceManager.GetBuilding("Capital City").SetPlanet(newOrbital);
						ResourceManager.GetBuilding("Space Port").SetPlanet(newOrbital);
						if (GlobalStats.HardcoreRuleset)
						{
							ResourceManager.GetBuilding("Fissionables").SetPlanet(newOrbital);
							ResourceManager.GetBuilding("Fissionables").SetPlanet(newOrbital);
							ResourceManager.GetBuilding("Mine Fissionables").SetPlanet(newOrbital);
							ResourceManager.GetBuilding("Fuel Refinery").SetPlanet(newOrbital);
						}
					}
					if (data.RingList[i - 1].HasRings != null)
					{
						newOrbital.hasRings = true;
						newOrbital.ringTilt = RandomMath.RandomBetween(-80f, -45f);
					}
					newSys.PlanetList.Add(newOrbital);
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = false,
						planet = newOrbital
					};
					newSys.RingList.Add(ring);
				}
				else
				{
					float numberOfAsteroids = RandomMath.RandomBetween(150f, 250f);
					for (int k = 0; (float)k < numberOfAsteroids; k++)
					{
						Vector3 asteroidCenter = new Vector3(newSys.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3000f, 3000f), Vector2.Zero), 0f);
						while (!newSys.RoidPosOK(asteroidCenter))
						{
							asteroidCenter = new Vector3(newSys.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-3000f, 3000f), Vector2.Zero), 0f);
						}
						Asteroid newRoid = new Asteroid()
						{
							scale = RandomMath.RandomBetween(1.2f, 4.6f),
							Position3D = asteroidCenter
						};
						int whichRoid = 0;
						while (whichRoid == 0 || whichRoid == 3)
						{
							whichRoid = (int)RandomMath.RandomBetween(1f, 9f);
						}
						newRoid.whichRoid = whichRoid;
						newRoid.Radius = RandomMath.RandomBetween(30f, 90f);
						newSys.AsteroidsList.Add(newRoid);
					}
					SolarSystem.Ring ring = new SolarSystem.Ring()
					{
						Distance = ringRadius,
						Asteroids = true
					};
					newSys.RingList.Add(ring);
				}
			}
			return newSys;
		}

		public float GetActualStrengthPresent(Empire e)
		{
			float StrHere = 0f;
			foreach (Ship ship in this.ShipList)
			{
				if (ship.loyalty != e)
				{
					continue;
				}
				StrHere = StrHere + ship.GetStrength();
			}
			return StrHere;
		}

        public float GetPredictedEnemyPresence(float time, Empire us)
        {
            float prediction = 0f;
            foreach (Ship ship in this.ShipList)
            {
                if (ship == null || ship.loyalty == us || !ship.loyalty.isFaction && !us.GetRelations()[ship.loyalty].AtWar)
                {
                    continue;
                }
                prediction = prediction + ship.GetStrength();
            }
            List<GameplayObject> nearby = UniverseScreen.ShipSpatialManager.GetNearby(this.Position);
            for (int i = 0; i < nearby.Count; i++)
            {
                Ship ship = nearby[i] as Ship;
                if (ship != null && ship.loyalty != us && !this.ShipList.Contains(ship) && (ship.loyalty.isFaction || us.GetRelations()[ship.loyalty].AtWar) && HelperFunctions.IntersectCircleSegment(this.Position, 100000f * UniverseScreen.GameScaleStatic, ship.Center, ship.Center + (ship.Velocity * 60f)))
                {
                    prediction = prediction + ship.GetStrength();
                }
            }
            return prediction;
        }

		private bool RoidPosOK(Vector3 roidPos)
        {
            Vector2 vector2_1 = new Vector2(roidPos.X, roidPos.Y);
            foreach (Asteroid asteroid in (List<Asteroid>)this.AsteroidsList)
            {
                Vector2 vector2_2 = new Vector2(asteroid.Position3D.X, asteroid.Position3D.Y);
                if (vector2_2 != vector2_1 && (double)Vector2.Distance(vector2_1, vector2_2) < 200.0)
                    return false;
            }
            return true;
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
	}
}