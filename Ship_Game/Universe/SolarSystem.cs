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
    using static RandomMath;
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

        public void Update(float elapsedTime, UniverseScreen universe, float realTime)
        {            
            var player = EmpireManager.Player;

            for (int i = 0; i < SunLayers.Length; i++)
            {
                SunLayerState layer = SunLayers[i];
                layer.Update(elapsedTime);
            }

            foreach (var status in Status)
                status.Value.Update(realTime);

            bool systemOnScreen = false;
            if (universe.Frustum.Contains(Position, Radius))
            {
                systemOnScreen = true;
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
                    systemOnScreen = true;
            }

            isVisible = systemOnScreen && (universe.viewState <= UniverseScreen.UnivScreenState.SectorView) && IsExploredBy(player);

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
            if (ShipWithinRadiationRadius(ship, out float distance))
            {
                float damage = SunLayers[0].Intensity * Sun.DamageMultiplier(distance)
                                                      * Sun.RadiationDamage;
                ship.CauseRadiationDamage(damage);
            }
        }

        private bool ShipWithinRadiationRadius(Ship ship, out float distance)
        {
            distance = ship.Center.Distance(Position);

            return distance < Sun.RadiationRadius;
        }

        // overload for ship info UI or AI maybe
        public bool ShipWithinRadiationRadius(Ship ship)
        {
            float distance = ship.Center.Distance(Position);
            return distance < Sun.RadiationRadius;
        }

        public Planet IdentifyGravityWell(Ship ship)
        {
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

        public void GenerateCorsairSystem(string systemName)
        {
            Sun = SunType.RandomHabitableSun(s => s.Id == "star_red"
                                      || s.Id == "star_yellow"
                                      || s.Id == "star_green");
            Name           = systemName;
            NumberOfRings  = 2;
            int starRadius = IntBetween(250, 500);
            for (int i = 1; i < NumberOfRings + 1; i++)
            {
                float ringRadius = i * (starRadius + RandomBetween(10500f, 12000f));
                if (i != 1)
                    GenerateAsteroidRing(ringRadius, spread:3500f);
                else
                {
                    float scale          = RandomBetween(1f, 2f);
                    float planetRadius   = 1000f * scale;
                    float randomAngle    = RandomBetween(0f, 360f);
                    Vector2 planetCenter = Vector2.Zero.PointFromAngle(randomAngle, ringRadius);
                    var newOrbital       = new Planet
                    {
                        Name         = systemName + " " + RomanNumerals.ToRoman(i),
                        OrbitalAngle = randomAngle,
                        ParentSystem = this
                    };
                    PlanetType type          = ResourceManager.RandomPlanet(PlanetCategory.Terran);
                    newOrbital.InitNewMinorPlanet(type, scale);
                    newOrbital.Center        = planetCenter;
                    newOrbital.ObjectRadius  = planetRadius;
                    newOrbital.OrbitalRadius = ringRadius;
                    newOrbital.PlanetTilt    = RandomBetween(45f, 135f);
                    if (RollDice(15))
                    {
                        newOrbital.HasRings = true;
                        newOrbital.RingTilt = RandomBetween(-80f, -45f);
                    }
                    newOrbital.CorsairPresence = true;
                    PlanetList.Add(newOrbital);
                    var ring = new Ring
                    {
                        Distance  = ringRadius,
                        Asteroids = false,
                        planet    = newOrbital
                    };
                    RingList.Add(ring);
                }
            }
        }

        public void GenerateRandomSystem(string name, UniverseData data, float systemScale, Empire owner = null)
        {
            // Changed by RedFox: 2% chance to get a tri-sun "star_binary"
            Sun = RollDice(percent:2)
                ? SunType.FindSun("star_binary")
                : SunType.RandomHabitableSun(s => s.Id != "star_binary");

            Name              = name;
            int starRadius    = (int)(IntBetween(250, 500) * systemScale);
            float ringMax     = starRadius * 300;
            float ringBase    = ringMax * .1f;
            int bonusP        = GlobalStats.ExtraPlanets > 0 ? (int)Math.Ceiling(GlobalStats.ExtraPlanets  / 2f) : 0;
            int minR          = IntBetween(0 + bonusP > 0 ? 1 : 0, 3 + GlobalStats.ExtraPlanets);
            int maxR          = IntBetween(minR, 6 + minR);
            NumberOfRings     = IntBetween(minR,maxR);
            NumberOfRings    += owner != null ? NumberOfRings < 5 ? 5 : 0 : 0;
            RingList.Capacity = NumberOfRings;
            float ringSpace   = ringMax / NumberOfRings;

            MarkovNameGenerator markovNameGenerator = null;
            if (owner != null)
                markovNameGenerator = ResourceManager.GetRandomNames(owner);

            for (int i = 1; i < NumberOfRings + 1; i++)
            {
                ringBase        += 5000;
                float ringRadius = ringBase + RandomBetween(0, ringSpace / (1 + NumberOfRings - i));
                if (!GlobalStats.DisableAsteroids && RollDice(10))
                {
                    float spread = ringRadius - ringBase;
                    GenerateAsteroidRing(ringRadius + spread *.25f, spread: spread *.5f);
                    ringRadius += spread / 2;
                }
                else
                {
                    float randomAngle = RandomBetween(0f, 360f);
                    string planetName = markovNameGenerator?.NextName ?? Name + " " + RomanNumerals.ToRoman(i);
                    var newOrbital    = new Planet(this, randomAngle, ringRadius, planetName, ringMax, owner);

                    if (owner == null)
                        newOrbital.GenerateRemnantPresence();

                    PlanetList.Add(newOrbital);
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
                && RollDice(percent:33))
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
            int numberOfRings        = data.RingList.Count;
            int randomBetween        = IntBetween(50, 500);

            for (int i = 0; i < numberOfRings; i++)
            {
                SolarSystemData.Ring ringData = data.RingList[i];
                float ringRadius = 10000f + (randomBetween + RandomBetween(10500f, 12000f)) * (i+1);

                if (ringData.Asteroids != null)
                {
                    newSys.GenerateAsteroidRing(ringRadius, spread: 3000f, scaleMin: 1.2f, scaleMax: 4.6f);
                    continue;
                }

                PlanetType type = ringData.WhichPlanet > 0
                    ? ResourceManager.Planet(ringData.WhichPlanet)
                    : ResourceManager.RandomPlanet();

                float scale;
                if (ringData.planetScale > 0)
                    scale = ringData.planetScale;
                else
                    scale = RandomBetween(0.9f, 1.8f) + type.Scale;

                float planetRadius = 1000f * (float) (1 + ((Math.Log(scale)) / 1.5));
                float randomAngle  = RandomBetween(0f, 360f);

                var newOrbital = new Planet
                {
                    Name               = ringData.Planet,
                    OrbitalAngle       = randomAngle,
                    ParentSystem       = newSys,
                    SpecialDescription = ringData.SpecialDescription,
                    Center             = MathExt.PointOnCircle(randomAngle, ringRadius),
                    ObjectRadius       = planetRadius,
                    OrbitalRadius      = ringRadius,
                    PlanetTilt         = RandomBetween(45f, 135f)
                };

                if (!ringData.HomePlanet || owner == null)
                    newOrbital.GeneratePlanetFromSystemData(ringData, type, scale);
                else // home planet
                    newOrbital.GenerateNewHomeWorld(owner, ringData.MaxPopDefined);

                newOrbital.InitializePlanetMesh(null);

                if (ringData.HasRings != null)
                {
                    newOrbital.HasRings = true;
                    newOrbital.RingTilt = RandomBetween(-80f, -45f);
                }

                // Add Remnant Presence
                if (owner == null)
                    newOrbital.GenerateRemnantPresence();

                // Add buildings to planet
                foreach (string building in ringData.BuildingList)
                    ResourceManager.CreateBuilding(building).SetPlanet(newOrbital);

                // Add ships to orbit
                foreach (string ship in ringData.Guardians)
                    newOrbital.Guardians.Add(ship);

                // Add moons to planets
                for (int j = 0; j < ringData.Moons.Count; j++)
                {
                    float radius = newOrbital.ObjectRadius * 5 + RandomBetween(1000f, 1500f) * (j + 1);
                    Moon moon    = new Moon
                    {
                        orbitTarget  = newOrbital.guid,
                        moonType     = ringData.Moons[j].WhichMoon,
                        scale        = ringData.Moons[j].MoonScale,
                        OrbitRadius  = radius,
                        OrbitalAngle = RandomBetween(0f, 360f),
                        Position     = newOrbital.Center.GenerateRandomPointOnCircle(radius)
                    };
                    newSys.MoonList.Add(moon);
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
                Vector2 pos = Vector2.Zero.GenerateRandomPointOnCircle(ringRadius + RandomBetween(-spread, spread));
                if (NoAsteroidProximity(pos))
                    return new Vector3(pos.X, pos.Y, -500f);
            }
            return Vector3.Zero; // should never reach this point, but if it does... we don't care, just don't crash or freeze
        }

        private void GenerateAsteroidRing(float ringRadius, float spread, float scaleMin=0.75f, float scaleMax=1.6f)
        {
            int numberOfAsteroids   = IntBetween(150, 250);
            AsteroidsList.Capacity += numberOfAsteroids;
            for (int i = 0; i < numberOfAsteroids; ++i)
            {
                AsteroidsList.Add(new Asteroid
                {
                    Scale      = RandomBetween(scaleMin, scaleMax),
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
            public string FleetName;
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
                    Fertility            = planet.BaseFertility,
                    MaxFertility         = planet.BaseMaxFertility,
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
                    GovMilitia           = planet.GovMilitia,
                    DontScrapBuildings   = planet.DontScrapBuildings,
                    NumShipyards         = planet.NumShipyards,
                    SpecialDescription   = planet.SpecialDescription,
                    IncomingFreighters   = planet.IncomingFreighterIds,
                    OutgoingFreighters   = planet.OutgoingFreighterIds,
                    StationsList         = planet.OrbitalStations.Where(kv => kv.Value.Active)
                                                                 .Select(kv => kv.Key).ToArray(),
                    ExploredBy           = planet.ExploredByEmpires.Select(e => e.data.Traits.Name),
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