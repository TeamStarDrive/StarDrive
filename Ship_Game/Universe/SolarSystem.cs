using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public bool PiratePresence { get; private set; }

        // this is the minimum solar system radius
        // needs to be big enough to properly trigger system-radius related events
        const float MinRadius = 150000f;

        // solar system radius
        public float Radius = MinRadius;

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
        int NumberOfRings;
        public Array<SolarSystem> FiveClosestSystems = new Array<SolarSystem>();
        public Array<string> ShipsToSpawn = new Array<string>();
        public Array<FleetAndPos> FleetsToSpawn = new Array<FleetAndPos>();
        public Array<Anomaly> AnomaliesList = new Array<Anomaly>();
        public bool IsStartingSystem;
        public Array<string> DefensiveFleets = new Array<string>();
        [XmlIgnore][JsonIgnore] bool WasVisibleLastFrame;

        public static SolarSystem GetSolarSystemFromGuid(Guid guid)
        {
            return UniverseScreen.SolarSystemList.Find(s => s.guid == guid);
        }

        public static Array<SolarSystem> GetSolarSystemsFromGuids(Array<Guid> guids)
        {
            var systems = new Array<SolarSystem>();
            for (int i = 0; i < guids.Count; i++)
            {
                var guid = guids[i];
                var system = GetSolarSystemFromGuid(guid);
                if (system != null)
                    systems.Add(system);
            }

            return systems;
        }

        public void Update(FixedSimTime timeStep, UniverseScreen universe)
        {            
            var player = EmpireManager.Player;

            for (int i = 0; i < SunLayers.Length; i++)
            {
                SunLayerState layer = SunLayers[i];
                layer.Update(timeStep);
            }
            var solarStatus = Status.Values.ToArray();
            for (int i = 0; i < solarStatus.Length; i++)
            {
                var status = solarStatus[i];
                status.Update(timeStep);
            }

            isVisible = universe.Frustum.Contains(Position, Radius)
                    && (universe.IsSectorViewOrCloser)
                    && IsExploredBy(player);

            if (isVisible && universe.IsSystemViewOrCloser)
            {
                WasVisibleLastFrame = true;
                for (int i = 0; i < AsteroidsList.Count; i++)
                {
                    AsteroidsList[i].UpdateVisibleAsteroid(timeStep);
                }
                for (int i = 0; i < MoonList.Count; i++)
                {
                    MoonList[i].UpdateVisibleMoon(timeStep);
                }
            }
            else if (WasVisibleLastFrame)
            {
                WasVisibleLastFrame = false;
                for (int i = 0; i < AsteroidsList.Count; i++)
                {
                    AsteroidsList[i].DestroySceneObject();
                }

                for (int i = 0; i < MoonList.Count; i++)
                {
                    MoonList[i].DestroySceneObject();
                }
            }

            for (int i = 0; i < PlanetList.Count; i++)
            {
                Planet planet = PlanetList[i];
                planet.Update(timeStep);
                if (planet.HasSpacePort && isVisible)
                    planet.Station.Update(timeStep);
            }

            if (Sun.RadiationDamage > 0f)
                UpdateSolarRadiationDebug();

            bool radiation = ShouldApplyRadiationDamage(timeStep);
            if (radiation)
            {
                for (int i = 0; i < ShipList.Count; ++i)
                {
                    Ship ship = ShipList[i];
                    if (ship.Active)
                    {
                        ApplySolarRadiationDamage(ship);
                    }
                }
            }
        }

        public void SetPiratePresence(bool value)
        {
            PiratePresence = value;
        }

        /// <summary>
        /// Checks if the empire has planets owned in this system. It might be the only owner here as well.
        /// </summary>
        public bool HasPlanetsOwnedBy(Empire empire)
        {
            return OwnerList.Contains(empire);
        }

        public bool IsExclusivelyOwnedBy(Empire empire)
        {
            return HasPlanetsOwnedBy(empire) && OwnerList.Count == 1;
        }

        float RadiationTimer;
        const float RadiationInterval = 0.5f;

        bool ShouldApplyRadiationDamage(FixedSimTime timeStep)
        {
            if (Sun.RadiationDamage > 0f)
            {
                RadiationTimer += timeStep.FixedTime;
                if (RadiationTimer >= RadiationInterval)
                {
                    RadiationTimer -= RadiationInterval;
                    return true;
                }
            }
            return false;
        }

        void UpdateSolarRadiationDebug()
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
            if (!ship.IsGuardian && ShipWithinRadiationRadius(ship, out float distance))
            {
                float damage = SunLayers[0].Intensity * Sun.DamageMultiplier(distance)
                                                      * Sun.RadiationDamage;
                ship.CauseRadiationDamage(damage);
            }
        }

        bool ShipWithinRadiationRadius(Ship ship, out float distance)
        {
            distance = ship.Center.Distance(Position);
            return distance < Sun.RadiationRadius;
        }
        
        public bool InSafeDistanceFromRadiation(Vector2 center)
        {
            return Sun.RadiationDamage.AlmostZero() || center.Distance(Position) > Sun.RadiationRadius + 10000;
        }

        public bool InSafeDistanceFromRadiation(float distance)
        {
            return Sun.RadiationDamage.AlmostZero() || distance > Sun.RadiationRadius + 10000;
        }

        // overload for ship info UI or AI maybe
        public bool ShipWithinRadiationRadius(Ship ship)
        {
            float distance = ship.Center.Distance(Position);
            return distance < Sun.RadiationRadius;
        }

        public Planet IdentifyGravityWell(Ship ship)
        {
            if (Empire.Universe.GravityWells)
            {
                // @todo QuadTree
                for (int i = 0; i < PlanetList.Count; i++)
                {
                    Planet planet = PlanetList[i];
                    if (ship.Position.InRadius(planet.Center, planet.GravityWellRadius))
                        return planet;
                }
            }
            return null;
        }

        public float AverageValueForEmpires(Array<Empire> empireList)
        {
            float totalValue = 0;
            float numOpponents = empireList.Count(e => !e.isFaction);
            for (int i = 0; i < empireList.Count; i++)
            {
                Empire empire = empireList[i];
                if (!empire.isFaction)
                    totalValue += RawValue(empire);
            }

            return totalValue / numOpponents;
        }

        float RawValue(Empire empire)
        {
            return PlanetList.Sum(p => p.ColonyRawValue(empire));
        }

        public float WarValueTo(Empire empire)
        { 
            return PlanetList.Sum(p => p.ColonyWarValueTo(empire));
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

        /// <summary>
        /// Forces present can not cause damage to ships but can be destroyed. 
        /// </summary>
        public bool HostileForcesPresent(Empire empire)
        {
            if (empire == null)
                return false;
            return GetStatus(empire).HostileForcesPresent;
        }

        /// <summary>
        /// Forces present can destroy friendly ships. 
        /// </summary>
        public bool DangerousForcesPresent(Empire empire)
        {
            if (empire == null)
                return false;
            return GetStatus(empire).DangerousForcesPresent;
        }

        public bool IsFullyExploredBy(Empire empire) => FullyExplored.FlatMapIsSet(empire);
        public void UpdateFullyExploredBy(Empire empire)
        {
            if (IsFullyExploredBy(empire))
                return;

            for (int i = 0; i < PlanetList.Count; ++i)
                if (!PlanetList[i].IsExploredBy(empire))
                    return;

            FullyExplored.FlatMapSet(ref FullyExplored, empire);
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

        public void GenerateRandomSystem(string name, float systemScale, Empire owner = null)
        {
            // Changed by RedFox: 2% chance to get a tri-sun "star_binary"
            Sun = RollDice(percent:2)
                ? SunType.FindSun("star_binary")
                : SunType.RandomHabitableSun(s => s.Id != "star_binary");

            Name              = name;
            int starRadius    = (int)(IntBetween(250, 500) * systemScale);
            float ringMax     = starRadius * 300;
            float ringBase    = ringMax * .1f;
            int minR          = IntBetween(GlobalStats.ExtraPlanets, 3);
            int maxR          = IntBetween(minR, 7 + minR);
            NumberOfRings     = IntBetween(minR, maxR);
            if (owner != null && NumberOfRings < 5)
                NumberOfRings = 5;

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

                    PlanetList.Add(newOrbital);
                    ringRadius += newOrbital.ObjectRadius;
                    var ring = new Ring
                    {
                        OrbitalDistance  = ringRadius,
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

            UpdateSystemRadius();
        }

        public void GenerateStartingSystem(string name, float systemScale, Empire owner)
        {
            IsStartingSystem = true;
            GenerateRandomSystem(name, systemScale, owner);
        }

        void GenerateFromData(SolarSystemData data, Empire owner)
        {
            int numberOfRings = data.RingList.Count;
            int fixedSpacing  = IntBetween(50, 500);
            int nextDistance  = 10000 + GetRingWidth(0);

            int GetRingWidth(int orbitalWidth)
            {
                return orbitalWidth > 0 ? orbitalWidth : fixedSpacing + IntBetween(10500, 12000);
            }

            if (owner != null)
                IsStartingSystem = true;

            for (int i = 0; i < numberOfRings; i++)
            {
                SolarSystemData.Ring ringData = data.RingList[i];

                int orbitalDistance = ringData.OrbitalDistance > 0 ? ringData.OrbitalDistance : nextDistance;
                nextDistance = orbitalDistance + GetRingWidth(ringData.OrbitalWidth);

                if (ringData.Asteroids != null)
                {
                    GenerateAsteroidRing(orbitalDistance, spread: 3000f, scaleMin: 1.2f, scaleMax: 4.6f);
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
                    ParentSystem       = this,
                    SpecialDescription = ringData.SpecialDescription,
                    Center             = MathExt.PointOnCircle(randomAngle, orbitalDistance),
                    ObjectRadius       = planetRadius,
                    OrbitalRadius      = orbitalDistance,
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

                // Add buildings to planet
                foreach (string building in ringData.BuildingList)
                    ResourceManager.CreateBuilding(building).SetPlanet(newOrbital);

                // Add moons to planets
                for (int j = 0; j < ringData.Moons.Count; j++)
                {
                    float orbitRadius = newOrbital.ObjectRadius * 5 + RandomBetween(1000f, 1500f) * (j + 1);
                    var moon = new Moon(newOrbital.guid,
                                    ringData.Moons[j].WhichMoon,
                                    ringData.Moons[j].MoonScale,
                                    orbitRadius,
                                    RandomBetween(0f, 360f),
                                    newOrbital.Center.GenerateRandomPointOnCircle(orbitRadius));
                    MoonList.Add(moon);
                }

                PlanetList.Add(newOrbital);
                RingList.Add(new Ring
                {
                    OrbitalDistance = orbitalDistance,
                    Asteroids = false,
                    planet = newOrbital
                });
            }
            
            UpdateSystemRadius();
        }

        public static SolarSystem GenerateSystemFromData(SolarSystemData data, Empire owner)
        {
            var newSys = new SolarSystem
            {
                Sun  = SunType.FindSun(data.SunPath),
                Name = data.Name
            };
            newSys.GenerateFromData(data, owner);
            return newSys;
        }

        void UpdateSystemRadius()
        {
            Radius = MinRadius;
            if (!RingList.IsEmpty)
            {
                int enclosingRadius = ((int)RingList.Last.OrbitalDistance + 10000).RoundUpToMultipleOf(10000);
                Radius = Math.Max(MinRadius, enclosingRadius);
            }
        }

        public void AddSystemExploreSuccessMessage(Empire empire)
        {
            if (!empire.isPlayer)
                return; // Message only the player

            //added by gremlin  add shamatts notification here
            var message = new StringBuilder(Name); //@todo create global string builder
            message.Append(" system explored.");

            if (Sun.RadiationDamage > 0)
                message.Append("\nThis Star emits radiation which will damage your ship's\nexternal modules or shields if they get close to it.");

            var planetsTypesNumber = new Map<string, int>();
            if (PlanetList.Count > 0)
            {
                foreach (Planet planet in PlanetList)
                    planetsTypesNumber.AddToValue(planet.CategoryName, 1);

                foreach (var pair in planetsTypesNumber)
                    message.Append('\n').Append(pair.Value).Append(' ').Append(pair.Key);
            }

            foreach (Planet planet in PlanetList)
            {
                Building tile = planet.BuildingList.Find(t => t.IsCommodity);
                if (tile != null)
                    message.Append('\n').Append(tile.Name).Append(" on ").Append(planet.Name);
            }

            if (DangerousForcesPresent(empire))
                message.Append("\nCombat in system!!!");

            if (OwnerList.Count > 0 && !OwnerList.Contains(empire))
                message.Append("\nContested system!!!");

            Empire.Universe.NotificationManager.AddNotification(new Notification
            {
                Pause           = false,
                Message         = message.ToString(),
                ReferencedItem1 = this,
                Icon            = Sun.Icon,
                Action          = "SnapToExpandSystem"
            }, "sd_ui_notification_warning");
        }

        public float GetActualStrengthPresent(Empire e)
        {
            float strength = 0f;
            for (int i = 0; i < ShipList.Count; i++)
            {
                Ship ship = ShipList[i];
                if (ship?.Active != true) continue;
                if (ship.loyalty != e)
                    continue;
                strength += ship.GetStrength();
            }

            return strength;
        }

        public float GetKnownStrengthHostileTo(Empire e)
        {
            float strength = 0f;
            for (int i = 0; i < ShipList.Count; i++)
            {
                Ship ship = ShipList[i];
                if (ship?.Active != true || !ship.KnownByEmpires.KnownBy(e)) continue;
                if (!ship.loyalty.IsAtWarWith(e))
                    continue;
                strength += ship.GetStrength();
            }

            return strength;
        }


        bool NoAsteroidProximity(Vector2 pos)
        {
            for (int i = 0; i < AsteroidsList.Count; i++)
                if (pos.InRadius(AsteroidsList[i].Position, 200.0f))
                    return false;
            return true;
        }

        Vector2 GenerateAsteroidPos(float ringRadius, float spread)
        {
            for (int i = 0; i < 100; ++i) // while (true) would be unsafe, so give up after 100 turns
            {
                Vector2 pos = Vector2.Zero.GenerateRandomPointOnCircle(ringRadius + RandomBetween(-spread, spread));
                if (NoAsteroidProximity(pos))
                    return pos;
            }
            return Vector2.Zero.GenerateRandomPointOnCircle(ringRadius + RandomBetween(-spread, spread));
        }

        void GenerateAsteroidRing(float orbitalDistance, float spread, float scaleMin=0.75f, float scaleMax=1.6f)
        {
            int numberOfAsteroids = IntBetween(150, 250);
            AsteroidsList.Capacity += numberOfAsteroids;
            for (int i = 0; i < numberOfAsteroids; ++i)
            {
                AsteroidsList.Add(new Asteroid(scaleMin, scaleMax,
                    GenerateAsteroidPos(orbitalDistance, spread)));
            }
            RingList.Add(new Ring
            {
                OrbitalDistance = orbitalDistance,
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
            public float OrbitalDistance;
            public bool Asteroids;
            public Planet planet;

            public SavedGame.RingSave Serialize()
            {
                var ringSave = new SavedGame.RingSave
                {
                    Asteroids = Asteroids,
                    OrbitalDistance = OrbitalDistance
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
                    BasePopPerTile       = planet.BasePopPerTile,
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
                    GovGroundDefense     = planet.GovGroundDefense,
                    GovMilitia           = planet.AutoBuildTroops,
                    GarrisonSize         = planet.GarrisonSize,
                    Quarantine           = planet.Quarantine,
                    ManualOrbitals       = planet.ManualOrbitals,
                    WantedPlatforms      = planet.WantedPlatforms,
                    WantedShipyards      = planet.WantedShipyards,
                    WantedStations       = planet.WantedStations,
                    DontScrapBuildings   = planet.DontScrapBuildings,
                    NumShipyards         = planet.NumShipyards,
                    SpecialDescription   = planet.SpecialDescription,
                    IncomingFreighters   = planet.IncomingFreighterIds,
                    OutgoingFreighters   = planet.OutgoingFreighterIds,
                    StationsList         = planet.OrbitalStations.Where(s => s.Active).Select(s => s.guid).ToArray(),
                    ExploredBy           = planet.ExploredByEmpires.Select(e => e.data.Traits.Name),
                    BaseFertilityTerraformRatio = planet.BaseFertilityTerraformRatio
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