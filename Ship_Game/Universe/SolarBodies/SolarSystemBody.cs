using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Ship_Game.Audio;
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
        public PlanetType Type;
        public SubTexture PlanetTexture => ResourceManager.Texture(Type.IconPath);
        public PlanetCategory Category => Type.Category;
        public bool IsBarrenType => Type.Category == PlanetCategory.Barren;
        public bool IsBarrenOrVolcanic => Type.Category == PlanetCategory.Barren
                                       || Type.Category == PlanetCategory.Volcanic;
        public string IconPath => Type.IconPath;
        public bool Habitable => Type.Habitable;

        public SBProduction Construction;
        public BatchRemovalCollection<Combat> ActiveCombats = new BatchRemovalCollection<Combat>();
        public BatchRemovalCollection<OrbitalDrop> OrbitalDropList = new BatchRemovalCollection<OrbitalDrop>();
        public BatchRemovalCollection<Troop> TroopsHere = new BatchRemovalCollection<Troop>();
        public BatchRemovalCollection<Projectile> Projectiles = new BatchRemovalCollection<Projectile>();
        protected readonly Array<Building> BuildingsCanBuild = new Array<Building>();
        public bool IsConstructing => Construction.NotEmpty;
        public bool NotConstructing => Construction.Empty;
        public int NumConstructing => Construction.Count;
        public BatchRemovalCollection<QueueItem> ConstructionQueue => Construction.ConstructionQueue;
        public Array<string> Guardians = new Array<string>();
        public Array<string> PlanetFleets = new Array<string>();
        public Map<Guid, Ship> OrbitalStations = new Map<Guid, Ship>();
        public Matrix RingWorld;
        public SceneObject SO;
        public Guid guid = Guid.NewGuid();
        protected AudioEmitter Emit = new AudioEmitter();
        public Vector2 Center;
        public SolarSystem ParentSystem;
        public Matrix CloudMatrix;
        public string SpecialDescription;
        public bool HasSpacePort;
        public string Name;
        public string Description;
        public Empire Owner;
        public float OrbitalAngle;
        public float OrbitalRadius;
        public bool HasRings;
        public float PlanetTilt;
        public float RingTilt;
        public float Scale;
        public Matrix World;
        protected float Zrotate;
        public bool UniqueHab = false;
        public int UniqueHabPercent;
        public SunZone Zone { get; protected set; }
        protected AudioEmitter Emitter;
        protected float InvisibleRadius;
        public float GravityWellRadius { get; protected set; }
        public Array<PlanetGridSquare> TilesList = new Array<PlanetGridSquare>(35);
        public float Density;
        public float Fertility { get; protected set; }
        public float MaxFertility { get; protected set; }
        public float MineralRichness;

        public Array<Building> BuildingList = new Array<Building>();
        public float ShieldStrengthCurrent;
        public float ShieldStrengthMax;        
        private float PosUpdateTimer = 1f;
        private float ZrotateAmount  = 0.03f;
        public float TerraformPoints { get; protected set; } // FB - terraform process from 0 to 1. 
        public float TerraformToAdd { get; protected set; }  //  FB - a sum of all terraformer efforts
        public Planet.ColonyType colonyType;
        public int TileMaxX { get; private set; } = 7; // FB foundations to variable planet tiles
        public int TileMaxY { get; private set; } = 5; // FB foundations to variable planet tiles
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

        public IReadOnlyList<Building> GetBuildingsCanBuild () { return BuildingsCanBuild; }


        protected void SetTileHabitability(float habChance)
        {
            if (UniqueHab)
            {
                habChance = UniqueHabPercent;
            }
            for (int x = 0; x < TileMaxX; ++x)
            {
                for (int y = 0; y < TileMaxY; ++y)
                {
                    bool habitable = habChance > 0 && RandomMath.RandomBetween(0, 100) < habChance;
                    TilesList.Add(new PlanetGridSquare(x, y, null, habitable));
                }
            }
        }

        protected void AddTileEvents()
        {
            if (Habitable && RandomMath.RandomBetween(0.0f, 100f) <= 15)
            {
                var buildingIds = new Array<int>();
                foreach (var kv in ResourceManager.BuildingsDict)
                {
                    if (!kv.Value.NoRandomSpawn && kv.Value.EventHere)
                        buildingIds.Add(kv.Value.BID);
                }

                int building = RandomMath.RandItem(buildingIds);
                PlanetGridSquare pgs = ResourceManager.CreateBuilding(building).AssignBuildingToRandomTile(this as Planet);
                BuildingList.Add(pgs.building);
                Log.Info($"Event building : {pgs.building.Name} : created on {Name}");
            }
        }

        public void SpawnRandomItem(RandomItem randItem, float chance, float instanceMax)
        {
            if (randItem.HardCoreOnly && !GlobalStats.HardcoreRuleset)
                return; // hardcore is disabled, bail

            if (RandomMath.RandomBetween(0.0f, 100f) < chance)
            {
                Building template = ResourceManager.GetBuildingTemplate(randItem.BuildingID);
                if (template == null)
                    return;
                int itemCount = (int)RandomMath.RandomBetween(1f, instanceMax + 0.95f);
                for (int i = 0; i < itemCount; ++i)
                {
                    PlanetGridSquare pgs = ResourceManager.CreateBuilding(template).AssignBuildingToRandomTile(this as Planet);
                    pgs.Habitable = true;
                    BuildingList.Add(pgs.building);
                    Log.Info($"Resource Created : '{pgs.building.Name}' : on '{Name}' ");
                }
            }
        }

        public string RichnessText
        {
            get
            {
                if (MineralRichness > 2.5) return Localizer.Token(1442);
                if (MineralRichness > 1.5) return Localizer.Token(1443);
                if (MineralRichness > 0.75) return Localizer.Token(1444);
                if (MineralRichness > 0.25) return Localizer.Token(1445);
                return Localizer.Token(1446);
            }
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
                SO.World = Matrix.CreateScale(3f)
                         * Matrix.CreateScale(Scale)
                         * Matrix.CreateRotationZ(-Zrotate)
                         * Matrix.CreateRotationX(-45f.ToRadians())
                         * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                CloudMatrix = Matrix.CreateScale(3f)
                            * Matrix.CreateScale(Scale)
                            * Matrix.CreateRotationZ(-Zrotate / 1.5f)
                            * Matrix.CreateRotationX(-45f.ToRadians())
                            * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                RingWorld = Matrix.CreateRotationX(RingTilt.ToRadians())
                          * Matrix.CreateScale(5f)
                          * Matrix.CreateTranslation(new Vector3(Center, 2500f));
                SO.Visibility = ObjectVisibility.Rendered;
            }
            else
                SO.Visibility = ObjectVisibility.None;
        }

        protected void CreatePlanetSceneObject(GameScreen screen)
        {
            if (SO != null)
                screen?.RemoveObject(SO);
            SO = ResourceManager.GetPlanetarySceneMesh(ResourceManager.RootContent, Type.MeshPath);
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
                Description = Name + " " + Type.Composition.Text + ". ";
                if (MaxFertility > 2)
                {
                    if      (Type.Id == 21) Description += Localizer.Token(1729);
                    else if (Type.Id == 13
                          || Type.Id == 22) Description += Localizer.Token(1730);
                    else                    Description += Localizer.Token(1731);
                }
                else if (MaxFertility > 1)
                {
                    if      (Type.Id == 19) Description += Localizer.Token(1732);
                    else if (Type.Id == 21) Description += Localizer.Token(1733);
                    else if (Type.Id == 13
                          || Type.Id == 22) Description += Localizer.Token(1734);
                    else                       Description += Localizer.Token(1735);
                }
                else if (MaxFertility > 0.6f)
                {
                    if      (Type.Id == 14) Description += Localizer.Token(1736);
                    else if (Type.Id == 21) Description += Localizer.Token(1737);
                    else if (Type.Id == 17) Description += Localizer.Token(1738);
                    else if (Type.Id == 19) Description += Localizer.Token(1739);
                    else if (Type.Id == 18) Description += Localizer.Token(1740);
                    else if (Type.Id == 11) Description += Localizer.Token(1741);
                    else if (Type.Id == 13 
                          || Type.Id == 22) Description += Localizer.Token(1742);
                    else                       Description += Localizer.Token(1743);
                }
                else
                {
                    switch (Type.Id) {
                        case 9:
                        case 23: Description += Localizer.Token(1744); break;
                        case 20:
                        case 15: Description += Localizer.Token(1745); break;
                        case 17: Description += Localizer.Token(1746); break;
                        case 18: Description += Localizer.Token(1747); break;
                        case 11: Description += Localizer.Token(1748); break;
                        case 14: Description += Localizer.Token(1749); break;
                        case 2:
                        case 6:
                        case 10: Description += Localizer.Token(1750); break;
                        case 3:
                        case 4:
                        case 16: Description += Localizer.Token(1751); break;
                        case 1:  Description += Localizer.Token(1752); break;
                        default:
                            if (Habitable)
                                Description = Description ?? "";
                            else
                                Description += Localizer.Token(1753);
                            break;
                    }
                }
                if (MaxFertility < 0.6f && MineralRichness >= 2 && Habitable)
                {
                    Description += Localizer.Token(1754);
                    if      (MineralRichness > 3)  Description += Localizer.Token(1755);
                    else if (MineralRichness >= 2) Description += Localizer.Token(1756);
                    else if (MineralRichness >= 1) Description += Localizer.Token(1757);
                }
                else if (MineralRichness > 3 && Habitable)
                {
                    Description += Localizer.Token(1758);
                }
                else if (MineralRichness >= 2 && Habitable)
                {
                    Description += Name + Localizer.Token(1759);
                }
                else if (MineralRichness >= 1 && Habitable)
                {
                    Description += Name + Localizer.Token(1760);
                }
                else if (MineralRichness < 1 && Habitable)
                {
                    if (Type.Id == 14)
                        Description += Name + Localizer.Token(1761);
                    else
                        Description += Name + Localizer.Token(1762);
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

            foreach (var kv in OrbitalStations)
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
    }
}