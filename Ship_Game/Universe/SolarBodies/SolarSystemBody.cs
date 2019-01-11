using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Universe.SolarBodies.AI;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public enum SunZone
    {
        Near,
        Habital,
        Far,
        VeryFar,
        Any
    }

    public enum PlanetCategory
    {
        Other,
        Barren,
        Desert,
        Steppe,
        Tundra,
        Terran,
        Volcanic,
        Ice,
        Swamp,
        Oceanic,
        GasGiant,
    }
    public enum Richness
    {
        UltraPoor,
        Poor,
        Average,
        Rich,
        UltraRich
    }
    public class OrbitalDrop
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public PlanetGridSquare Target;
    }

    public enum DevelopmentLevel
    {
        Solitary=1, Meager=2, Vibrant=3, CoreWorld=4, MegaWorld=5
    }

    public class SolarSystemBody : Explorable
    {
        public SBProduction SbProduction;
        public BatchRemovalCollection<Combat> ActiveCombats = new BatchRemovalCollection<Combat>();
        public BatchRemovalCollection<OrbitalDrop> OrbitalDropList = new BatchRemovalCollection<OrbitalDrop>();
        public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
        public BatchRemovalCollection<Ship> BasedShips = new BatchRemovalCollection<Ship>();
        public BatchRemovalCollection<Projectile> Projectiles = new BatchRemovalCollection<Projectile>();
        protected readonly Array<Building> BuildingsCanBuild = new Array<Building>();
        public BatchRemovalCollection<QueueItem> ConstructionQueue => SbProduction.ConstructionQueue;
        public Array<string> Guardians = new Array<string>();
        public Array<string> PlanetFleets = new Array<string>();
        public Map<Guid, Ship> Shipyards = new Map<Guid, Ship>();
        public Matrix RingWorld;
        public SceneObject SO;
        public Guid guid = Guid.NewGuid();
        protected AudioEmitter Emit = new AudioEmitter();
        public Vector2 Center;
        public SolarSystem ParentSystem;
        public Matrix CloudMatrix;
        public bool HasEarthLikeClouds;
        public string SpecialDescription;
        public bool HasShipyard; // This is terribly named. This should be 'HasStarPort'
        public string Name;
        public string Description;
        public Empire Owner;
        public float OrbitalAngle;
        public float OrbitalRadius;
        public int PlanetType;
        public bool HasRings;
        public float PlanetTilt;
        public float RingTilt;
        public float Scale;
        public Matrix World;
        public bool Habitable;
        public string PlanetComposition;
        protected float Zrotate;
        public bool UniqueHab = false;
        public int UniqueHabPercent;
        public SunZone Zone { get; protected set; }
        protected AudioEmitter Emitter;
        protected float InvisibleRadius;
        public float GravityWellRadius { get; protected set; }
        public Array<PlanetGridSquare> TilesList = new Array<PlanetGridSquare>(35);
        protected float HabitalTileChance = 10;        
        public float Density;
        public float Fertility { get; protected set; }
        public float MaxFertility { get; protected set; }
        public float MineralRichness;

        public Array<Building> BuildingList = new Array<Building>();
        public float ShieldStrengthCurrent;
        public float ShieldStrengthMax;        
        private float PosUpdateTimer = 1f;
        private float ZrotateAmount = 0.03f;
        public float TerraformPoints;
        public float TerraformToAdd;
        public Planet.ColonyType colonyType;        
        public void PlayPlanetSfx(string sfx, Vector3 position)
        {
            if (Emitter == null)
                Emitter = new AudioEmitter();
            Emitter.Position = position;
            GameAudio.PlaySfxAsync(sfx, Emitter);
        }
        public float ObjectRadius
        {
            get => SO?.WorldBoundingSphere.Radius ?? InvisibleRadius;
            set => InvisibleRadius = SO?.WorldBoundingSphere.Radius ?? value;
        }
        public int TurnsSinceTurnover { get; protected set; }
        public Shield Shield { get; protected set;}

        public Array<Building> GetBuildingsCanBuild () { return BuildingsCanBuild; }




        protected void SetTileHabitability(float habChance)
        {
            {
                if (UniqueHab)
                {
                    habChance = UniqueHabPercent;
                }
                bool habitable = false;
                for (int x = 0; x < 7; ++x)
                {
                    for (int y = 0; y < 5; ++y)
                    {
                        if (habChance > 0)
                            habitable = RandomMath.RandomBetween(0, 100) < habChance;

                        TilesList.Add(new PlanetGridSquare(x, y, null, habitable));
                    }
                }
            }
        }

        protected void AddTileEvents()
        {
            if (RandomMath.RandomBetween(0.0f, 100f) <= 15 && Habitable)
            {
                Array<string> list = new Array<string>();
                foreach (var kv in ResourceManager.BuildingsDict)
                {
                    if (!string.IsNullOrEmpty(kv.Value.EventTriggerUID) && !kv.Value.NoRandomSpawn)
                        list.Add(kv.Key);
                }
                int index = (int)RandomMath.RandomBetween(0f, list.Count + 0.85f);
                if (index >= list.Count)
                    index = list.Count - 1;
                PlanetGridSquare b = ResourceManager.CreateBuilding(list[index]).AssignBuildingToRandomTile(this);
                BuildingList.Add(b.building);
                Log.Info($"Event building : {b.building.Name} : created on {Name}");
            }
        }
        public void SpawnRandomItem(RandomItem randItem, float chance, float instanceMax)
        {
            if ((GlobalStats.HardcoreRuleset || !randItem.HardCoreOnly) && RandomMath.RandomBetween(0.0f, 100f) < chance)
            {
                int itemCount = (int)RandomMath.RandomBetween(1f, instanceMax + 0.95f);
                for (int i = 0; i < itemCount; ++i)
                {
                    if (!ResourceManager.BuildingsDict.ContainsKey(randItem.BuildingID)) continue;
                    var pgs = ResourceManager.CreateBuilding(randItem.BuildingID).AssignBuildingToRandomTile(this);
                    pgs.Habitable = true;
                    Log.Info($"Resource Created : '{pgs.building.Name}' : on '{Name}' ");
                    BuildingList.Add(pgs.building);
                }
            }
        }
        
        public string GetRichness()
        {
            if (MineralRichness > 2.5)  return Localizer.Token(1442);
            if (MineralRichness > 1.5)  return Localizer.Token(1443);
            if (MineralRichness > 0.75) return Localizer.Token(1444);
            if (MineralRichness > 0.25) return Localizer.Token(1445);
            return Localizer.Token(1446);
        }

        public string GetOwnerName()
        {
            if (Owner != null)
                return Owner.data.Traits.Singular;
            return Habitable ? " None" : " Uninhabitable";
        }

        public void InitializePlanetMesh(GameScreen screen)
        {
            Shield = ShieldManager.AddPlanetaryShield(Center);
            UpdateDescription();
            CreatePlanetSceneObject(screen);

            GravityWellRadius = (float)(GlobalStats.GravityWellRange * (1 + ((Math.Log(Scale)) / 1.5)));
        }

        protected void UpdatePosition(float elapsedTime)
        {
            
        
            PosUpdateTimer -= elapsedTime;
            if (!Empire.Universe.Paused && (PosUpdateTimer <= 0.0f || ParentSystem.isVisible))
            {
                PosUpdateTimer = 5f;
                OrbitalAngle += (float) Math.Asin(15.0 / OrbitalRadius);
                if (OrbitalAngle >= 360.0f)
                    OrbitalAngle -= 360f;
                Center = ParentSystem.Position.PointOnCircle(OrbitalAngle, OrbitalRadius);
            }

            if (ParentSystem.isVisible)
            {
                Zrotate += ZrotateAmount * elapsedTime;
                SO.World = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(Scale) *
                           Matrix.CreateRotationZ(-Zrotate) * Matrix.CreateRotationX(-45f.ToRadians()) *
                           Matrix.CreateTranslation(new Vector3(Center, 2500f));
                CloudMatrix = Matrix.Identity * Matrix.CreateScale(3f) * Matrix.CreateScale(Scale) *
                              Matrix.CreateRotationZ((float) (-Zrotate / 1.5)) *
                              Matrix.CreateRotationX(-45f.ToRadians()) *
                              Matrix.CreateTranslation(new Vector3(Center, 2500f));
                RingWorld = Matrix.Identity * Matrix.CreateRotationX(RingTilt.ToRadians()) *
                            Matrix.CreateScale(5f) * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                SO.Visibility = ObjectVisibility.Rendered;
            }
            else
                SO.Visibility = ObjectVisibility.None;
        }

        protected void CreatePlanetSceneObject(GameScreen screen)
        {
            if (SO != null)
                screen?.RemoveObject(SO);
            var contentManager =  ResourceManager.RootContent;
            SO = ResourceManager.GetPlanetarySceneMesh(contentManager, "Model/SpaceObjects/planet_" + PlanetType);
            SO.World = Matrix.CreateScale(Scale * 3)
                       * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            RingWorld = Matrix.CreateRotationX(RingTilt.ToRadians())
                        * Matrix.CreateScale(5f)
                        * Matrix.CreateTranslation(new Vector3(Center, 2500f));

            screen?.AddObject(SO);
        }

        protected void UpdateDescription()
        {
            if (SpecialDescription != null)
            {
                Description = SpecialDescription;
            }
            else
            {
                Description = "";
                var planet1 = this;
                string str1 = planet1.Description + Name + " " + PlanetComposition + ". ";
                planet1.Description = str1;
                if (MaxFertility > 2)
                {
                    if (PlanetType == 21)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1729);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1730);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1731);
                        planet2.Description = str2;
                    }
                }
                else if (MaxFertility > 1)
                {
                    if (PlanetType == 19)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1732);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 21)
                        Description += Localizer.Token(1733);
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1734);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1735);
                        planet2.Description = str2;
                    }
                }
                else if (MaxFertility > 0.6f)
                {
                    if (PlanetType == 14)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1736);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 21)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1737);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 17)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1738);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 19)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1739);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 18)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1740);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 11)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1741);
                        planet2.Description = str2;
                    }
                    else if (PlanetType == 13 || PlanetType == 22)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1742);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Localizer.Token(1743);
                        planet2.Description = str2;
                    }
                }
                else
                {
                    switch (PlanetType) {
                        case 9:
                        case 23:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1744);
                            planet2.Description = str2;
                            break;
                        }
                        case 20:
                        case 15:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1745);
                            planet2.Description = str2;
                            break;
                        }
                        case 17:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1746);
                            planet2.Description = str2;
                            break;
                        }
                        case 18:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1747);
                            planet2.Description = str2;
                            break;
                        }
                        case 11:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1748);
                            planet2.Description = str2;
                            break;
                        }
                        case 14:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1749);
                            planet2.Description = str2;
                            break;
                        }
                        case 2:
                        case 6:
                        case 10:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1750);
                            planet2.Description = str2;
                            break;
                        }
                        case 3:
                        case 4:
                        case 16:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1751);
                            planet2.Description = str2;
                            break;
                        }
                        case 1:
                        {
                            var planet2 = this;
                            string str2 = planet2.Description + Localizer.Token(1752);
                            planet2.Description = str2;
                            break;
                        }
                        default:
                            if (Habitable)
                            {
                                var planet2 = this;
                                string str2 = planet2.Description ?? "";
                                planet2.Description = str2;
                            }
                            else
                            {
                                var planet2 = this;
                                string str2 = planet2.Description + Localizer.Token(1753);
                                planet2.Description = str2;
                            }
                            break;
                    }
                }
                if (MaxFertility < 0.6f && MineralRichness >= 2 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1754);
                    planet2.Description = str2;
                    if (MineralRichness > 3)
                    {
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1755);
                        planet3.Description = str3;
                    }
                    else if (MineralRichness >= 2)
                    {
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1756);
                        planet3.Description = str3;
                    }
                    else
                    {
                        if (MineralRichness < 1)
                            return;
                        var planet3 = this;
                        string str3 = planet3.Description + Localizer.Token(1757);
                        planet3.Description = str3;
                    }
                }
                else if (MineralRichness > 3 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Localizer.Token(1758);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 2 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1759);
                    planet2.Description = str2;
                }
                else if (MineralRichness >= 1 && Habitable)
                {
                    var planet2 = this;
                    string str2 = planet2.Description + Name + Localizer.Token(1760);
                    planet2.Description = str2;
                }
                else
                {
                    if (MineralRichness >= 1 || !Habitable)
                        return;
                    if (PlanetType == 14)
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1761);
                        planet2.Description = str2;
                    }
                    else
                    {
                        var planet2 = this;
                        string str2 = planet2.Description + Name + Localizer.Token(1762);
                        planet2.Description = str2;
                    }
                }
            }
        }

        static void TraitLess(ref float invaderValue, ref float ownerValue) => invaderValue = Math.Max(invaderValue, ownerValue);
        static void TraitMore(ref float invaderValue, ref float ownerValue) => invaderValue = Math.Min(invaderValue, ownerValue);

        public void ChangeOwnerByInvasion(Empire newOwner)
        {
            var thisPlanet = (Planet)this;

            if (newOwner.TryGetRelations(Owner, out Relationship rel) && rel.AtWar && rel.ActiveWar != null)
                ++rel.ActiveWar.ColoniesWon;
            if (Owner.TryGetRelations(newOwner, out Relationship rel2) && rel2.AtWar && rel2.ActiveWar != null)
                ++rel2.ActiveWar.ColoniesLost;

            ConstructionQueue.Clear();
            foreach (PlanetGridSquare planetGridSquare in TilesList)
                planetGridSquare.QItem = null;

            Owner.RemovePlanet(thisPlanet);
            if (newOwner.isPlayer && Owner == EmpireManager.Cordrazine)
                GlobalStats.IncrementCordrazineCapture();

            if (IsExploredBy(Empire.Universe.PlayerEmpire))
            {
                if (!newOwner.isFaction)
                    Empire.Universe.NotificationManager.AddConqueredNotification(thisPlanet, newOwner, Owner);
                else
                {
                    lock (GlobalStats.OwnedPlanetsLock)
                    {
                        Empire.Universe.NotificationManager.AddPlanetDiedNotification(thisPlanet, Empire.Universe.PlayerEmpire);

                        if (Owner != null)
                        {
                            // check if Owner still has planets in this system:
                            bool hasPlanetsInSystem = ParentSystem.PlanetList
                                                    .Any(p => p != thisPlanet && p.Owner == Owner);
                            if (!hasPlanetsInSystem)
                                ParentSystem.OwnerList.Remove(Owner);
                            Owner = null;
                        }
                    }
                    ConstructionQueue.Clear();
                    return;
                }
            }

            if (newOwner.data.Traits.Assimilators)
            {
                TraitLess(ref newOwner.data.Traits.DiplomacyMod, ref Owner.data.Traits.DiplomacyMod);
                TraitLess(ref newOwner.data.Traits.DodgeMod, ref Owner.data.Traits.DodgeMod);
                TraitLess(ref newOwner.data.Traits.EnergyDamageMod, ref Owner.data.Traits.EnergyDamageMod);
                TraitMore(ref newOwner.data.Traits.ConsumptionModifier, ref Owner.data.Traits.ConsumptionModifier);
                TraitLess(ref newOwner.data.Traits.GroundCombatModifier, ref Owner.data.Traits.GroundCombatModifier);
                TraitLess(ref newOwner.data.Traits.Mercantile, ref Owner.data.Traits.Mercantile);
                TraitLess(ref newOwner.data.Traits.PassengerModifier, ref Owner.data.Traits.PassengerModifier);
                TraitLess(ref newOwner.data.Traits.ProductionMod, ref Owner.data.Traits.ProductionMod);
                TraitLess(ref newOwner.data.Traits.RepairMod, ref Owner.data.Traits.RepairMod);
                TraitLess(ref newOwner.data.Traits.ResearchMod, ref Owner.data.Traits.ResearchMod);
                TraitLess(ref newOwner.data.Traits.ShipCostMod, ref Owner.data.Traits.ShipCostMod);
                TraitLess(ref newOwner.data.Traits.PopGrowthMin, ref Owner.data.Traits.PopGrowthMin);
                TraitMore(ref newOwner.data.Traits.PopGrowthMax, ref Owner.data.Traits.PopGrowthMax);
                TraitLess(ref newOwner.data.Traits.ModHpModifier, ref Owner.data.Traits.ModHpModifier);
                TraitLess(ref newOwner.data.Traits.TaxMod, ref Owner.data.Traits.TaxMod);
                TraitMore(ref newOwner.data.Traits.MaintMod, ref Owner.data.Traits.MaintMod);
                TraitLess(ref newOwner.data.SpyModifier, ref Owner.data.SpyModifier);
                TraitLess(ref newOwner.data.Traits.Spiritual, ref Owner.data.Traits.Spiritual);

            }
            if (newOwner.isFaction)
                return;

            foreach (var kv in Shipyards)
            {
                if (kv.Value.loyalty != newOwner && kv.Value.TroopList.Any(loyalty => loyalty.GetOwner() != newOwner))
                    continue;
                kv.Value.ChangeLoyalty(newOwner);             
                Log.Info($"Owner of platform tethered to {Name} changed from {Owner.PortraitName} to {newOwner.PortraitName}");
            }
            Owner = newOwner;
            TurnsSinceTurnover = 0;
            Owner.AddPlanet(thisPlanet);
            ConstructionQueue.Clear();
            ParentSystem.OwnerList.Clear();

            foreach (Planet planet in ParentSystem.PlanetList)
            {
                if (planet.Owner != null && !ParentSystem.OwnerList.Contains(planet.Owner))
                    ParentSystem.OwnerList.Add(planet.Owner);                
            }
            thisPlanet.TradeAI.ClearHistory();

            if (newOwner.isPlayer && !newOwner.AutoColonize)
                colonyType = Planet.ColonyType.Colony;
            else
                colonyType = Owner.AssessColonyNeeds(thisPlanet);
        }

        protected void GenerateMoons(Planet newOrbital)
        {
            int moonCount = (int)Math.Ceiling(ObjectRadius * .004f);
            moonCount = (int)Math.Round(RandomMath.AvgRandomBetween(-moonCount * .75f, moonCount));
            for (int j = 0; j < moonCount; j++)
            {
                float radius = newOrbital.ObjectRadius + 1500 + RandomMath.RandomBetween(1000f, 1500f) * (j + 1);
                var moon = new Moon
                {
                    orbitTarget = newOrbital.guid,
                    moonType = RandomMath.IntBetween(1, 29),
                    scale = 1,
                    OrbitRadius = radius,
                    OrbitalAngle = RandomMath.RandomBetween(0f, 360f),
                    Position = newOrbital.Center.GenerateRandomPointOnCircle(radius)
                };
                ParentSystem.MoonList.Add(moon);
            }
        }

        public void AddBasedShip(Ship ship)
        {
            BasedShips.Add(ship);
        }
    }
}