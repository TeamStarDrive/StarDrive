using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public sealed class SolarSystem : Explorable
    {
        public string Name = "Random System";
        public Guid guid = Guid.NewGuid();
        public bool DontStartNearPlayer;
        
        //public Array<Empire> OwnerList = new Array<Empire>();
        public HashSet<Empire> OwnerList = new HashSet<Empire>();
        public Array<Ship> ShipList = new Array<Ship>();
        public bool isVisible;
        public Vector2 Position;
        public float Radius = 150000f; // solar system radius
        public Array<Planet> PlanetList = new Array<Planet>();
        public Array<Asteroid> AsteroidsList = new Array<Asteroid>();
        public Array<Moon> MoonList = new Array<Moon>();

        Empire[] FullyExplored = Empty<Empire>.Array;

        SunType TheSunType;
        public SunLayerState[] SunLayers;

        public SunType Sun
        {
            get => TheSunType;
            set
            {
                TheSunType = value;
                SunLayers = value.CreateLayers();
            }
        }

        public Array<Ring> RingList = new Array<Ring>();
        private int NumberOfRings;
        public Array<SolarSystem> FiveClosestSystems = new Array<SolarSystem>();
        public Array<string> ShipsToSpawn = new Array<string>();
        public Array<FleetAndPos> FleetsToSpawn = new Array<FleetAndPos>();
        public Array<Anomaly> AnomaliesList = new Array<Anomaly>();
        public bool isStartingSystem;
        public Array<string> DefensiveFleets = new Array<string>();
        [XmlIgnore][JsonIgnore] public bool VisibilityUpdated;

        public void Update(float elapsedTime, UniverseScreen universe)
        {
            float realTime = (float)StarDriveGame.Instance.GameTime.ElapsedRealTime.TotalSeconds;
            var player = EmpireManager.Player;

            foreach (SunLayerState layer in SunLayers)
                layer.Update(elapsedTime);

            foreach (var status in Status)
                status.Value.Update(realTime);

            bool viewing = false;
            if (universe.Frustum.Contains(Position, Radius))
            {
                viewing = true;
            }
            else if (universe.viewState <= UniverseScreen.UnivScreenState.ShipView) // WTF is this doing?
            {
                var rect = new Rectangle((int) Position.X - (int)Radius,
                                         (int) Position.Y - (int)Radius, (int)Radius*2, (int)Radius*2);
                Vector3 position = universe.Viewport.Unproject(new Vector3(500f, 500f, 0f), universe.Projection, universe.View, Matrix.Identity);
                Vector3 direction = universe.Viewport.Unproject(new Vector3(500f, 500f, 1f), universe.Projection, universe.View, Matrix.Identity) -
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
                isVisible = (universe.viewState <= UniverseScreen.UnivScreenState.SectorView);
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
                if (planet.HasSpacePort && isVisible)
                    planet.Station.Update(elapsedTime);
            }

            bool radiation = ShouldApplyRadiationDamage(elapsedTime);
            if (Sun.RadiationDamage > 0f)
                UpdateSolarRadiationDebug(elapsedTime);

            for (int i = 0; i < ShipList.Count; ++i)
            {
                Ship ship = ShipList[i];
                if (radiation && ship.Active)
                {
                    ApplySolarRadiationDamage(ship);
                }
                ship.Update(elapsedTime);
            }
        }


        float RadiationTimer;
        const float RadiationInterval = 0.5f;

        bool ShouldApplyRadiationDamage(float elapsedTime)
        {
            if (Sun.RadiationDamage > 0f)
            {
                RadiationTimer += elapsedTime;
                if (RadiationTimer >= RadiationInterval)
                {
                    RadiationTimer -= RadiationInterval;
                    return true;
                }
            }
            return false;
        }

        void UpdateSolarRadiationDebug(float elapsedTime)
        {
            // some debugging for us developers
            if (Empire.Universe.Debug && Debug.DebugInfoScreen.Mode == Debug.DebugModes.Solar)
            {
                for (float r = 0.03f; r < 0.5f; r += 0.03f)
                {
                    float dist = Sun.RadiationRadius*r;
                    var color = new Color(Color.Red, Sun.DamageMultiplier(dist));
                    Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Solar, 
                        Position, dist, color, 0f);
                }
                Empire.Universe.DebugWin?.DrawCircle(Debug.DebugModes.Solar, 
                    Position, Sun.RadiationRadius, Color.Brown, 0f);
            }
        }

        void ApplySolarRadiationDamage(Ship ship)
        {
            float distance = ship.Center.Distance(Position);
            if (distance < Sun.RadiationRadius)
            {
                float damage = SunLayers[0].Intensity * Sun.DamageMultiplier(distance)
                                                      * Sun.RadiationDamage;
                ship.CauseRadiationDamage(damage);
            }
        }

        public Planet IdentifyGravityWell(Ship ship)
        {
            // FB - should add an option in rules option for friendlies to not ignore gravity wells
            // for instance - (|| IsInFriendlySpace && !FriendliesIgnoreWells)  or something like that
            if (!Empire.Universe.GravityWells || ship.IsInFriendlySpace)
                return null;

            for (int i = 0; i < PlanetList.Count; i++)
            {
                Planet planet = PlanetList[i];
                if (ship.Position.InRadius(planet.Center, planet.GravityWellRadius))
                    return planet;
            }

            return null;
        }

        readonly Map<Empire, EmpireSolarSystemStatus> Status = new Map<Empire, EmpireSolarSystemStatus>();

        EmpireSolarSystemStatus GetStatus(Empire empire)
        {
            if (!Status.TryGetValue(empire, out EmpireSolarSystemStatus status))
            {
                status = new EmpireSolarSystemStatus(this, empire);
                Status.Add(empire, status);
            }
            return status;
        }

        public float GetCombatTimer(Empire empire)
        {
            if (empire == null)
                return 0f;
            return GetStatus(empire).CombatTimer;
        }

        public bool HostileForcesPresent(Empire empire)
        {
            if (empire == null)
                return false;
            return GetStatus(empire).HostileForcesPresent;
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

        public Planet FindPlanet(in Guid planetGuid)
        {
            if (planetGuid != Guid.Empty)
            {
                foreach (Planet p in PlanetList)
                    if (p.guid == planetGuid)
                        return p;
            }
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

        public void GenerateCorsairSystem(string systemName)
        {
            Sun = SunType.RandomHabitableSun(s => s.Id == "star_red" 
                                      || s.Id == "star_yellow" 
                                      || s.Id == "star_green");
            Name = systemName;
            NumberOfRings = 2;
            int starRadius = RandomMath.IntBetween(250, 500);
            for (int i = 1; i < NumberOfRings + 1; i++)
            {
                float ringRadius = i * (starRadius + RandomMath.RandomBetween(10500f, 12000f));
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
                    PlanetType type = ResourceManager.RandomPlanet(PlanetCategory.Terran);
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
            // Changed by RedFox: 2% chance to get a tri-sun "star_binary"
            Sun = RandomMath.RollDice(percent:2)
                ? SunType.FindSun("star_binary")
                : SunType.RandomHabitableSun(s => s.Id != "star_binary");

            Name              = name;
            int starRadius    = (int)(RandomMath.IntBetween(250, 500) * systemScale);
            float ringMax     = starRadius * 300;
            float ringBase    = ringMax * .1f;
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
                ringBase += 5000;
                float ringRadius = ringBase + RandomMath.RandomBetween(0, ringSpace / (1 + NumberOfRings - i));
                if (RandomMath.IntBetween(1, 100) > 80)
                {
                    float spread = ringRadius - ringBase;

                    if (!GlobalStats.DisableAsteroids) GenerateAsteroidRing(ringRadius + spread *.25f, spread: spread *.5f);
                    ringRadius += spread / 2;
                }
                else
                {
                    float randomAngle = RandomMath.RandomBetween(0f, 360f);
                    string planetName = markovNameGenerator?.NextName ?? Name + " " + RomanNumerals.ToRoman(i);
                   
                    var newOrbital = new Planet(this, randomAngle, ringRadius, planetName, ringMax, owner);

                    if (owner == null)
                        GenerateRemnantPresence(newOrbital, data);

                    PlanetList.Add(newOrbital);
                    RandomMath.RandomBetween(0f, 3f);
                    ringRadius += newOrbital.ObjectRadius;
                    var ring = new Ring
                    {
                        Distance  = ringRadius,
                        Asteroids = false,
                        planet    = newOrbital
                    };
                    RingList.Add(ring);
                }
                ringBase = ringRadius;
            }

            // now, if number of planets is <= 2 and they are barren,
            // then 33% chance to have neutron star:
            if (PlanetList.Count <= 2 && PlanetList.All(p => p.IsBarrenOrVolcanic)
                && RandomMath.RollDice(percent:33))
            {
                Sun = SunType.RandomBarrenSun();
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
                Sun  = SunType.FindSun(data.SunPath),
                Name = data.Name
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

                PlanetType type = ringData.WhichPlanet > 0
                    ? ResourceManager.Planet(ringData.WhichPlanet)
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
                    newOrbital.SetFertilityMinMax(2f + owner.data.Traits.HomeworldFertMod);
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

                    newOrbital.HasSpacePort = true;
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

            public SavedGame.RingSave Serialize()
            {
                var ringSave = new SavedGame.RingSave
                {
                    Asteroids = Asteroids,
                    OrbitalDistance = Distance
                };

                if (planet == null)
                    return ringSave;

                var pdata = new SavedGame.PlanetSaveData
                {
                    Crippled_Turns       = planet.CrippledTurns,
                    guid                 = planet.guid,
                    FoodState            = planet.FS,
                    ProdState            = planet.PS,
                    FoodLock             = planet.Food.PercentLock,
                    ProdLock             = planet.Prod.PercentLock,
                    ResLock              = planet.Res.PercentLock,
                    Name                 = planet.Name,
                    Scale                = planet.Scale,
                    ShieldStrength       = planet.ShieldStrengthCurrent,
                    Population           = planet.Population,
                    PopulationMax        = planet.MaxPopBase,
                    Fertility            = planet.Fertility,
                    MaxFertility         = planet.MaxFertility,
                    Richness             = planet.MineralRichness,
                    Owner                = planet.Owner?.data.Traits.Name ?? "",
                    WhichPlanet          = planet.Type.Id,
                    OrbitalAngle         = planet.OrbitalAngle,
                    OrbitalDistance      = planet.OrbitalRadius,
                    HasRings             = planet.HasRings,
                    Radius               = planet.ObjectRadius,
                    farmerPercentage     = planet.Food.Percent,
                    workerPercentage     = planet.Prod.Percent,
                    researcherPercentage = planet.Res.Percent,
                    foodHere             = planet.FoodHere,
                    TerraformPoints      = planet.TerraformPoints,
                    prodHere             = planet.ProdHere,
                    ColonyType           = planet.colonyType,
                    GovOrbitals          = planet.GovOrbitals,
                    SpecialDescription   = planet.SpecialDescription,
                    IncomingFreighters = planet.IncomingFreighterIds,
                    OutgoingFreighters = planet.OutgoingFreighterIds,
                    StationsList       = planet.OrbitalStations.Where(kv => kv.Value.Active)
                                                                    .Select(kv => kv.Key).ToArray(),
                    ExploredBy = planet.ExploredByEmpires.Select(e => e.data.Traits.Name),
                };

                if (planet.Owner != null)
                {
                    pdata.QISaveList = planet.ConstructionQueue.Select(item => item.Serialize());
                }

                pdata.PGSList = planet.TilesList.Select(tile => tile.Serialize());

                ringSave.Planet = pdata;
                return ringSave;
            }
        }

        public override string ToString() => $"System '{Name}' Pos={Position} Rings={NumberOfRings}";
    }
}