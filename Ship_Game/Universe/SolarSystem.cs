using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class SolarSystem : Explorable, IDisposable
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
        public Array<Ship> ShipList = new Array<Ship>();
        public bool isVisible;
        public Vector2 Position;
        public Array<Planet> PlanetList = new Array<Planet>();
        public Array<Asteroid> AsteroidsList = new Array<Asteroid>();
        public Array<Moon> MoonList = new Array<Moon>();

        private Empire[] FullyExplored = Empty<Empire>.Array;
        public string SunPath;

        public Array<Ring> RingList = new Array<Ring>();
        private int NumberOfRings;
        public int StarRadius;
        public Array<SolarSystem> FiveClosestSystems = new Array<SolarSystem>();
        public Array<string> ShipsToSpawn = new Array<string>();
        public Array<FleetAndPos> FleetsToSpawn = new Array<FleetAndPos>();
        public Array<Anomaly> AnomaliesList = new Array<Anomaly>();
        public bool isStartingSystem;
        public Array<string> DefensiveFleets = new Array<string>();
        public Map<Empire,PredictionTimeout> predictionTimeout =new Map<Empire,PredictionTimeout>();
        [XmlIgnore] [JsonIgnore] public bool VisibilityUpdated;

        public class PredictionTimeout
        {
            public float prediction;
            public float predictionTimeout;
            public float predictedETA;
            public void Update(float time)
            {
                predictionTimeout -= time;
                predictedETA -= time;
                Log.Info($"Prediction Timeout: {predictionTimeout}");
                Log.Info($"Prediction ETA: {predictedETA}");
                Log.Info($"Prediction: {prediction}");
            }
        }

        public void Update(float elapsedTime, UniverseScreen universe)
        {
            float realTime = (float)Game1.Instance.GameTime.ElapsedRealTime.TotalSeconds;
            var player = EmpireManager.Player;
            DangerTimer -= realTime;            
            DangerUpdater -= realTime;
            if (DangerUpdater < 0.0)
            {
                DangerUpdater = 10f;

                DangerTimer =  player.KnownShips.Any(s => s.Center.InRadius(Position, 150000))
                    ? 120f
                    : 0.0f;
            }

            combatTimer -= realTime;

            if (combatTimer <= 0.0)
                CombatInSystem = false;
            bool viewing = false;
            Vector3 v3SystemPosition = Position.ToVec3();
            universe.Viewport.Project(v3SystemPosition, universe.projection, universe.view, Matrix.Identity);
            if (universe.Frustum.Contains(new BoundingSphere(v3SystemPosition, 100000f)) !=
                ContainmentType.Disjoint)
                viewing = true;
            //WTF is this doing?
            else if (universe.viewState <= UniverseScreen.UnivScreenState.ShipView)
            {
                var rect = new Rectangle((int) Position.X - 100000,
                    (int) Position.Y - 100000, 200000, 200000);
                Vector3 position = universe.Viewport.Unproject(new Vector3(500f, 500f, 0.0f), universe.projection, universe.view, Matrix.Identity);
                Vector3 direction = universe.Viewport.Unproject(new Vector3(500f, 500f, 1f), universe.projection, universe.view, Matrix.Identity) -
                                    position;
                direction.Normalize();
                var ray = new Ray(position, direction);
                float num = -ray.Position.Z / ray.Direction.Z;
                var vector3 = new Vector3(ray.Position.X + num * ray.Direction.X,
                    ray.Position.Y + num * ray.Direction.Y, 0.0f);
                var pos = new Vector2(vector3.X, vector3.Y);
                if (rect.HitTest(pos))
                    viewing = true;
            }

            if (IsExploredBy(player) && viewing)
            {
                isVisible = universe.viewState <= UniverseScreen.UnivScreenState.SectorView;
            }

            if (isVisible && universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                VisibilityUpdated = true;
                for (int i = 0; i < AsteroidsList.Count; i++)
                {
                    Asteroid asteroid = AsteroidsList[i];
                    asteroid.So.Visibility = ObjectVisibility.Rendered;
                    asteroid.Update(elapsedTime);
                }

                for (int i = 0; i < MoonList.Count; i++)
                {
                    Moon moon = MoonList[i];
                    moon.So.Visibility = ObjectVisibility.Rendered;
                    moon.UpdatePosition(elapsedTime);
                }
            }
            else if (VisibilityUpdated)
            {
                VisibilityUpdated = false;
                for (int i = 0; i < AsteroidsList.Count; i++)
                {
                    Asteroid asteroid = AsteroidsList[i];
                    asteroid.So.Visibility = ObjectVisibility.None;
                }

                for (int i = 0; i < MoonList.Count; i++)
                {
                    Moon moon = MoonList[i];
                    moon.So.Visibility = ObjectVisibility.None;
                }
            }
            

            for (int i = 0; i < PlanetList.Count; i++)
            {
                Planet planet = PlanetList[i];
                planet.Update(elapsedTime);
                if (planet.HasShipyard && isVisible)
                    planet.Station.Update(elapsedTime);
            }

            for (int i = ShipList.Count - 1; i >= 0; --i)
            {
                Ship ship = ShipList[i];
                if (!ship.ShipInitialized) continue;
                if (ship.System == null)
                    continue;
                if (!ship.Active || ship.ModuleSlotsDestroyed) // added by gremlin ghost ship killer
                {
                    ship.Die(null, true);
                }
                else
                {
                    if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                    {
                        ship.Inhibited = true;
                        ship.InhibitedTimer = 10f;
                    }
                    ship.Update(elapsedTime);
                    if (ship.PlayerShip)
                        ship.ProcessInput(elapsedTime);
                }
            }
        }

        public bool IsFullyExploredBy(Empire empire) => FullyExplored.IsSet(empire);
        public void UpdateFullyExploredBy(Empire empire)
        {
            if (IsFullyExploredBy(empire))
                return;

            for (int i = 0; i < PlanetList.Count; ++i)
                if (!PlanetList[i].IsExploredBy(empire))
                    return;

            FullyExplored.Set(ref FullyExplored, empire);
            //Log.Info($"The {empire.Name} have fully explored {Name}");
        }

        public Planet FindPlanet(Guid planetGuid)
        {
            foreach (Planet p in PlanetList)
                if (p.guid == planetGuid)
                    return p;
            return null;
        }

        static void AddMajorRemnantPresence(Planet newOrbital)
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

        static void AddMinorRemnantPresence(Planet newOrbital)
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

        static void AddMiniRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Xeno Fighter");
            newOrbital.Guardians.Add("Xeno Fighter");
            newOrbital.Guardians.Add("Heavy Drone");
        }

        static void AddSupportRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Support Drone");
            newOrbital.Guardians.Add("Support Drone");
        }

        static void AddCarrierRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Ancient Carrier");
        }

        static void AddTorpedoRemnantPresence(Planet newOrbital)  //Added by Gretman
        {
            newOrbital.Guardians.Add("Ancient Torpedo Cruiser");
        }

        static void AddRemnantPatrol(Planet newOrbital)
        {
            newOrbital.PlanetFleets.Add("Remnant Patrol");
        }

        static void AddRemnantGarrison(Planet newOrbital)
        {
            newOrbital.PlanetFleets.Add("Remnant Garrison");
        }

        // @todo This method is huge, find a way to generalize the logic, perhaps by changing the logic into something more generic
        static void GenerateRemnantPresence(Planet newOrbital, UniverseData data)
        {
            float quality = newOrbital.Fertility + newOrbital.MineralRichness + newOrbital.MaxPopulation / 1000f;
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.customRemnantElements)
            {
                if (quality > 6f && quality < 10f)
                {
                    int n = RandomMath.IntBetween(0, 100);
                    if (n > 20 && n < 50) AddRemnantPatrol(newOrbital);
                    else if (n >= 50)   AddRemnantGarrison(newOrbital);
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

        public void GenerateCorsairSystem(string systemName)
        {
            SetSunPath(RandomMath.IntBetween(1, 3));
            Name = systemName;
            NumberOfRings = 2;
            StarRadius = RandomMath.IntBetween(250, 500);
            for (int i = 1; i < NumberOfRings + 1; i++)
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
                    var newOrbital = new Planet
                    {
                        Name = systemName + " " + RomanNumerals.ToRoman(i),
                        OrbitalAngle = randomAngle,
                        ParentSystem = this
                    };
                    PlanetTypeInfo type = ResourceManager.RandomPlanet(PlanetCategory.Terran);
                    newOrbital.InitNewMinorPlanet(type);
                    newOrbital.Center      = planetCenter;
                    newOrbital.Scale         = scale;
                    newOrbital.ObjectRadius  = planetRadius;
                    newOrbital.OrbitalRadius = ringRadius;
                    newOrbital.PlanetTilt = RandomMath.RandomBetween(45f, 135f);
                    if (RandomMath.IntBetween(1, 100) < 15)
                    {
                        newOrbital.HasRings = true;
                        newOrbital.RingTilt = RandomMath.RandomBetween(-80f, -45f);
                    }
                    newOrbital.CorsairPresence = true;
                    PlanetList.Add(newOrbital);
                    RandomMath.RandomBetween(0f, 3f);
                    var ring = new Ring
                    {
                        Distance = ringRadius,
                        Asteroids = false,
                        planet = newOrbital
                    };
                    RingList.Add(ring);
                }
            }
        }

        public void GenerateRandomSystem(string name, UniverseData data, float systemScale, Empire owner = null)
        {
            // Changed by RedFox: 3% chance to get a tri-sun star
            SetSunPath(RandomMath.IntBetween(0, 100) < 3 ? (6) : RandomMath.IntBetween(1, 5));

            Name              = name;
            StarRadius        = (int) (RandomMath.IntBetween(250, 500) * systemScale);
            float ringMax     = StarRadius * 300;
            float ringbase    = ringMax * .1f;
            int bonusP        = GlobalStats.ExtraPlanets > 0 ? (int)Math.Ceiling(GlobalStats.ExtraPlanets  / 2f) : 0;            
            int minR          = RandomMath.IntBetween(0 + bonusP > 0 ? 1 : 0, 3 + GlobalStats.ExtraPlanets);
            int maxR          = RandomMath.IntBetween(minR, 6 + minR);
            NumberOfRings     = RandomMath.IntBetween(minR,maxR);
            NumberOfRings += owner != null ? NumberOfRings < 5 ? 5 : 0 : 0;
            RingList.Capacity = NumberOfRings;

            float ringSpace = ringMax / NumberOfRings;

            MarkovNameGenerator markovNameGenerator = null;            
            if (owner != null)            
                markovNameGenerator = ResourceManager.GetRandomNames(owner);
            
            for (int i = 1; i < NumberOfRings + 1; i++)
            {
                ringbase += 5000;
                float ringRadius = ringbase + RandomMath.RandomBetween(0, ringSpace / (1 + NumberOfRings - i));
                if (RandomMath.IntBetween(1, 100) > 80)
                {
                    float spread = ringRadius - ringbase;

                    if (!GlobalStats.DisableAsteroids) GenerateAsteroidRing(ringRadius + spread *.25f, spread: spread *.5f);
                    ringRadius += spread / 2;
                }
                else
                {
                    float randomAngle = RandomMath.RandomBetween(0f, 360f);
                    string planetName = markovNameGenerator?.NextName ?? Name + " " + RomanNumerals.ToRoman(i);
                   
                    Planet newOrbital = new Planet(this, randomAngle, ringRadius, planetName, ringMax, owner);

                    if (owner == null)
                        GenerateRemnantPresence(newOrbital, data);

                    PlanetList.Add(newOrbital);
                    RandomMath.RandomBetween(0f, 3f);
                    ringRadius += newOrbital.ObjectRadius;
                    Ring ring = new Ring
                    {
                        Distance  = ringRadius,
                        Asteroids = false,
                        planet    = newOrbital
                    };
                    RingList.Add(ring);
                }
                ringbase = ringRadius;
            }
        }


        public void GenerateStartingSystem(string name, UniverseData data, float systemScale, Empire owner)
        {
            isStartingSystem = true;
            GenerateRandomSystem(name, data, systemScale, owner);
        }

        public static SolarSystem GenerateSystemFromData(SolarSystemData data, Empire owner)
        {
            var newSys = new SolarSystem
            {
                SunPath = data.SunPath,
                Name    = data.Name
            };
            newSys.RingList.Capacity = data.RingList.Count;
            int numberOfRings = data.RingList.Count;
            int randomBetween = RandomMath.IntBetween(50, 500);

            for (int i = 0; i < numberOfRings; i++)
            {
                SolarSystemData.Ring ringData = data.RingList[i];
                float ringRadius = 10000f + (randomBetween + RandomMath.RandomBetween(10500f, 12000f)) * (i+1);

                if (ringData.Asteroids != null)
                {
                    newSys.GenerateAsteroidRing(ringRadius, spread: 3000f, scaleMin: 1.2f, scaleMax: 4.6f);
                    continue;
                }

                PlanetTypeInfo type = ringData.WhichPlanet > 0
                    ? ResourceManager.PlanetOrRandom(ringData.WhichPlanet)
                    : ResourceManager.RandomPlanet();

                float scale;
                if (ringData.planetScale > 0) scale = ringData.planetScale;
                else scale = RandomMath.RandomBetween(0.9f, 1.8f) + type.Scale;

                float planetRadius = 1000f * (float) (1 + ((Math.Log(scale)) / 1.5));
                float randomAngle = RandomMath.RandomBetween(0f, 360f);

                var newOrbital = new Planet
                {
                    Name = ringData.Planet,
                    OrbitalAngle = randomAngle,
                    ParentSystem = newSys,
                    SpecialDescription = ringData.SpecialDescription,
                    Center = MathExt.PointOnCircle(randomAngle, ringRadius),
                    Scale = scale,
                    ObjectRadius = planetRadius,
                    OrbitalRadius = ringRadius,
                    PlanetTilt = RandomMath.RandomBetween(45f, 135f)
                };

                if (!ringData.HomePlanet || owner == null)
                {
                    if (ringData.UniqueHabitat)
                    {
                        newOrbital.UniqueHab = true;
                        newOrbital.UniqueHabPercent = ringData.UniqueHabPC;
                    }

                    newOrbital.InitNewMinorPlanet(type);
                    if (ringData.MaxPopDefined > 0)
                        newOrbital.MaxPopBase = ringData.MaxPopDefined * 1000f;

                    if (ringData.Owner.NotEmpty())
                    {
                        newOrbital.Owner = EmpireManager.GetEmpireByName(ringData.Owner);
                        newOrbital.Owner.AddPlanet(newOrbital);
                        newOrbital.InitializeWorkerDistribution(newOrbital.Owner);
                        newOrbital.Population = newOrbital.MaxPopulation;
                        newOrbital.MineralRichness = 1f;
                        newOrbital.colonyType = Planet.ColonyType.Core;
                        newOrbital.SetFertility(2f, 2f);
                    }
                }
                else
                {
                    newOrbital.Owner = owner;
                    owner.Capital = newOrbital;
                    owner.AddPlanet(newOrbital);
                    newOrbital.GenerateNewHomeWorld(type);
                    newOrbital.InitializeWorkerDistribution(owner);
                    newOrbital.MineralRichness = 1f + owner.data.Traits.HomeworldRichMod;
                    newOrbital.InitFertilityMinMax(2f + owner.data.Traits.HomeworldFertMod);
                    if (ringData.MaxPopDefined > 0)
                        newOrbital.MaxPopBase =
                            ringData.MaxPopDefined * 1000f * owner.data.Traits.HomeworldSizeMultiplier;
                    else
                        newOrbital.MaxPopBase = 14000f * owner.data.Traits.HomeworldSizeMultiplier;

                    newOrbital.Population = 14000f;
                    newOrbital.FoodHere = 100f;
                    newOrbital.ProdHere = 100f;
                    if (!newSys.OwnerList.Contains(newOrbital.Owner))
                        newSys.OwnerList.Add(newOrbital.Owner);

                    newOrbital.HasShipyard = true;
                    newOrbital.AddGood("ReactorFuel", 1000);
                    ResourceManager.CreateBuilding(Building.CapitalId).SetPlanet(newOrbital);
                    ResourceManager.CreateBuilding(Building.SpacePortId).SetPlanet(newOrbital);
                    if (GlobalStats.HardcoreRuleset)
                    {
                        ResourceManager.CreateBuilding(Building.FissionablesId).SetPlanet(newOrbital);
                        ResourceManager.CreateBuilding(Building.FissionablesId).SetPlanet(newOrbital);
                        ResourceManager.CreateBuilding(Building.MineFissionablesId).SetPlanet(newOrbital);
                        ResourceManager.CreateBuilding(Building.FuelRefineryId).SetPlanet(newOrbital);
                    }
                }

                newOrbital.InitializePlanetMesh(null);

                if (ringData.HasRings != null)
                {
                    newOrbital.HasRings = true;
                    newOrbital.RingTilt = RandomMath.RandomBetween(-80f, -45f);
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
                        float radius = newOrbital.ObjectRadius * 5 +
                                       RandomMath.RandomBetween(1000f, 1500f) * (j + 1);
                        Moon moon = new Moon
                        {
                            orbitTarget = newOrbital.guid,
                            moonType = ringData.Moons[j].WhichMoon,
                            scale = ringData.Moons[j].MoonScale,
                            OrbitRadius = radius,
                            OrbitalAngle = RandomMath.RandomBetween(0f, 360f),
                            Position = newOrbital.Center.GenerateRandomPointOnCircle(radius)
                        };
                        newSys.MoonList.Add(moon);
                    }
                }

                newSys.PlanetList.Add(newOrbital);
                Ring ring = new Ring
                {
                    Distance = ringRadius,
                    Asteroids = false,
                    planet = newOrbital
                };
                newSys.RingList.Add(ring);
            }
            return newSys;
        }

        public float GetActualStrengthPresent(Empire e)
        {
            float strength = 0f;
            foreach (Ship ship in ShipList)
            {                
                if (ship?.Active != true) continue;
                if (ship.loyalty != e)
                    continue;
                strength += ship.GetStrength();
            }
            return strength;
        }

        public int GetPredictedEnemyPresence(float time, Empire us)
        {
             
            float prediction =us.GetEmpireAI().ThreatMatrix.PingRadarStr(Position, 150000 *2,us);
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
                Vector2 pos = Vector2.Zero.GenerateRandomPointOnCircle(ringRadius + RandomMath.RandomBetween(-spread, spread));
                if (NoAsteroidProximity(pos))
                    return new Vector3(pos.X, pos.Y, -500f);
            }
            return Vector3.Zero; // should never reach this point, but if it does... we don't care, just don't crash or freeze
        }

        private void GenerateAsteroidRing(float ringRadius, float spread, float scaleMin=0.75f, float scaleMax=1.6f)
        {
            int numberOfAsteroids = RandomMath.IntBetween(150, 250);
            AsteroidsList.Capacity += numberOfAsteroids;
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
            ShipList = null;
            AsteroidsList = null;
        }

        public override string ToString() => $"System '{Name}' Pos={Position} Combat={CombatInSystem} Rings={NumberOfRings}";
    }
}