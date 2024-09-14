using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Universe;
using Ship_Game.Universe.SolarBodies;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

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

    public class OrbitalDrop
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public PlanetGridSquare TargetTile;
        public Planet Surface;

        public void DamageColonySurface(Bomb bomb)
        {
            int softDamage  = (int)bomb.Owner.Random.Float(bomb.TroopDamageMin, bomb.TroopDamageMax);
            int hardDamage  = (int)bomb.Owner.Random.Float(bomb.HardDamageMin, bomb.HardDamageMax);
            float popKilled = bomb.PopKilled;
            float envDamage = bomb.FertilityDamage;

            if (!TargetTile.Habitable)
            {
                popKilled *= 0.25f;
                envDamage *= 0.1f;
            }

            DamageTile(hardDamage);
            DamageTroops(softDamage, bomb.Owner);
            DamageBuildings(hardDamage, bomb.ShipLevel);
            TryCreateVolcano(hardDamage);
            Surface.ApplyBombEnvEffects(popKilled, envDamage, bomb.Owner); // Fertility and pop loss
            Surface.AddBombingIntensity(hardDamage);
        }

        void TryCreateVolcano(int hardDamage)
        {
            if (Surface.Random.RollDice((hardDamage / 10f).UpperBound(0.4f * Surface.Universe.P.VolcanicActivity)))
                TargetTile.CreateVolcano(Surface);
        }

        private void DamageTile(int hardDamage)
        {
            // Damage biospheres first
            if (TargetTile.Biosphere)
            {
                DamageBioSpheres(hardDamage);
            }
            else if (TargetTile.Habitable)
            {
                float destroyThreshold = TargetTile.BuildingOnTile ? 0.25f : 0.5f; // Lower chance to destroy a tile if there is a building on it
                if (Surface.Random.RollDice(hardDamage * destroyThreshold))
                    Surface.DestroyTile(TargetTile); // Tile becomes un-habitable and any building on it is destroyed immediately
            }
        }

        private void DamageBioSpheres(int damage)
        {
            if (TargetTile.Biosphere && Surface.Random.RollDice(damage * 20))
            {
                // Biospheres could not withstand damage
                TargetTile.Highlighted = false;
                Surface.DestroyBioSpheres(TargetTile);
            }
        }

        private void DamageTroops(int damage, Empire bombOwner)
        {
            if (!TargetTile.TroopsAreOnTile)
                return;

            for (int i = 0; i < TargetTile.TroopsHere.Count; ++i)
            {
                // Try to hit the troop, high level troops have better chance to evade
                Troop troop = TargetTile.TroopsHere[i];
                int troopHitChance = 50 - troop.Level*4;

                // Reduce friendly fire chance (25%) if bombing a tile with multiple troops
                if (troop.Loyalty == bombOwner)
                    troopHitChance = (int)(troopHitChance * 0.25f);

                if (Surface.Random.RollDice(troopHitChance))
                    troop.DamageTroop(damage, Surface, TargetTile, out _);
            }
        }

        void DamageBuildings(int damage, int shipLevel)
        {
            if (!TargetTile.BuildingOnTile || TargetTile.Building.CannotBeBombed)
                return;

            Building building = TargetTile.Building;
            int hitChance = 50 + shipLevel * 5;
            hitChance = (hitChance - building.Defense).Clamped(10, 95);

            if (Surface.Random.RollDice(hitChance))
            {
                building.ApplyDamageAndRemoveIfDestroyed(Surface, damage);
            }
        }
    }

    public enum DevelopmentLevel
    {
        Solitary=1, Meager=2, Vibrant=3, CoreWorld=4, MegaWorld=5
    }

    [StarDataType]
    public class SolarSystemBody : ExplorableGameObject
    {
        public Vector3 Position3D => new(Position, 2500);

        public PlanetType PType;

        // this is used only for serializing the PlanetType PType
        [StarData]
        public int WhichPlanet
        {
            get => PType.Id;
            set => PType = ResourceManager.Planets.Planet(value);
        }

        public SubTexture PlanetTexture => ResourceManager.Texture(PType.IconPath);
        public PlanetCategory Category => PType.Category;
        public bool IsBarrenType => PType.Category == PlanetCategory.Barren;
        public bool IsBarrenGasOrVolcanic => PType.Category is PlanetCategory.Barren or PlanetCategory.Volcanic or PlanetCategory.GasGiant;

        public string IconPath => PType.IconPath;
        public bool Habitable => PType.Habitable;

        public UniverseState Universe => System.Universe;

        [StarData] public SBProduction Construction;
        public Array<Combat> ActiveCombats = new();
        protected Array<Building> BuildingsCanBuild = new();
        public bool IsConstructing => Construction.NotEmpty;
        public bool NotConstructing => Construction.Empty;
        public int NumConstructing => Construction.Count;
        [StarData] public Array<Ship> OrbitalStations = new();

        // this is only here for SaveGame backwards compatibility
        [StarData] public SolarSystem ParentSystem
        {
            get => System;
            set => SetSystem(value);
        }

        public SceneObject SO;

        [StarData] public string SpecialDescription;
        [StarData] public bool HasSpacePort;
        [StarData] public string Name;
        public string Description;
        [StarData] public Empire Owner;
        public bool OwnerIsPlayer => Owner != null && Owner.isPlayer;
        [StarData] public float OrbitalAngle; // OrbitalAngle in DEGREES
        [StarData] public float OrbitalRadius { get; protected set; }
        [StarData] public bool HasRings;
        [StarData] public float PlanetTilt;
        [StarData] public float RingTilt; // tilt in Radians
        [StarData] public float Scale;
        public Matrix World;
        public float Zrotate;
        public bool UniqueHab = false;
        public int UniqueHabPercent;
        protected Audio.AudioEmitter Emitter;
        public float GravityWellRadius { get; protected set; }

        // TODO: replace TilesList with a raw array
        [StarData] public Array<PlanetGridSquare> TilesList = new(35);
        public float Density;
        [StarData] public float BaseFertility { get; protected set; } // This is clamped to a minimum of 0, cannot be negative
        [StarData] public float BaseMaxFertility { get; protected set; } // Natural Fertility, this is clamped to a minimum of 0, cannot be negative
        [StarData] public float BuildingsFertility { get; protected set; }  // Fertility change by all relevant buildings. Can be negative
        [StarData] public float MineralRichness; // Mineable Gas giants get the richness of the exotic resource

        [StarData] protected Array<Building> BuildingList = new();
        public int NumBuildings => BuildingList.Count;

        public int NumMilitaryBuildings => BuildingList.Count(b => b.IsMilitary);
        public ReadOnlySpan<Building> Buildings => BuildingList.AsReadOnlySpan();

        [StarData] public float ShieldStrengthCurrent { get; private set; }
        public float ShieldStrengthMax { get; private set; }
        float PosUpdateTimer = 1f;
        float ZrotateAmount  = 0.03f;
        [StarData] public float TerraformPoints { get; protected set; } // FB - terraform process from 0 to 1. 
        [StarData] public float BaseFertilityTerraformRatio { get; protected set; } // A value to add to base fertility during Terraform. 
        public float TerraformToAdd { get; protected set; }  //  FB - a sum of all terraformer efforts
        [StarData] public Planet.ColonyType CType;
        public static int TileMaxX { get; private set; } = 7; // FB foundations to variable planet tiles
        public static int TileMaxY { get; private set; } = 5; // FB foundations to variable planet tiles

        // bomb impacts, shield impacts
        public void PlayPlanetSfx(string sfx, Vector3 position)
        {
            Emitter ??= new(maxDistance: GameAudio.PlanetSfxDistance);
            Emitter.Position = position;
            GameAudio.PlaySfxAsync(sfx, Emitter);
        }

        public int TurnsSinceTurnover { get; protected set; }
        public Shield Shield { get; protected set; }
        public IReadOnlyList<Building> GetBuildingsCanBuild() => BuildingsCanBuild;
        
        // per-planet pseudo-random source
        public readonly RandomBase Random = new ThreadSafeRandom();

        public SolarSystemBody(int id, GameObjectType type) : base(id, type)
        {
            DisableSpatialCollision = true;
        }

        protected void AddTileEvents()
        {
            if (!Habitable)
                return;

            var potentialEvents = ResourceManager.BuildingsDict.FilterValues(b => b.EventHere && !b.NoRandomSpawn);
            if (potentialEvents.Length == 0)
                return;

            Building selectedBuilding = Random.Item(potentialEvents);
            if (selectedBuilding.IsBadCacheResourceBuilding)
            {
                Log.Warning($"{selectedBuilding.Name} is FoodCache with no PlusFlatFood or ProdCache with no PlusProdPerColonist." +
                            " Cannot use it for events.");
                return;
            }

            if (Random.RollDice(selectedBuilding.EventSpawnChance))
            {
                if (!(this is Planet thisPlanet))
                    return;
                Building building = ResourceManager.CreateBuilding(thisPlanet, selectedBuilding.BID);
                if (building.AssignBuildingToTilePlanetCreation(thisPlanet, out PlanetGridSquare tile))
                {
                    if (!tile.SetEventOutComeNum(thisPlanet, building))
                        thisPlanet.DestroyBuildingOn(tile);

                    //Log.Info($"Event building : {tile.Building.Name} : created on {Name}");
                }
            }
        }

        public void SpawnRandomItem(RandomItem randItem, float chance, int instanceMin, int instanceMax)
        {
            if (randItem.HardCoreOnly)
                return; // hardcore is disabled, bail

            if (Random.RollDice(chance))
            {
                Building template = ResourceManager.GetBuildingTemplate(randItem.BuildingID);
                if (template == null)
                    return;

                int itemCount = Random.RollDie(instanceMax).LowerBound(instanceMin);
                for (int i = 0; i < itemCount; ++i)
                {
                    if (template.BID == Building.VolcanoId)
                    {
                        Random.Item(TilesList.Filter(t => !t.BuildingOnTile)).CreateVolcano(this as Planet);
                        //Log.Info($"Volcano Created on '{Name}' ");
                    }
                    else
                    {
                        Building b = ResourceManager.CreateBuilding(this as Planet, template);
                        b.AssignBuildingToRandomTile(this as Planet);
                        //Log.Info($"Resource Created : '{b.Name}' : on '{Name}' ");
                    }
                }
            }
        }

        public string RichnessText
        {
            get
            {
                if (this is Planet p && p.Category == PlanetCategory.GasGiant)
                {
                    if (p.IsMineable) 
                    {
                        float richness = p.Mining.Richness;
                        string text;
                        if (richness > 7.5)      text = $"{Localizer.Token(GameText.UltraRich)} ({richness})";
                        else if (richness > 5)   text = $"{Localizer.Token(GameText.Rich)} ({richness})";
                        else if (richness > 2.5) text = $"{Localizer.Token(GameText.Average)} ({richness})";
                        else                     text = $"{Localizer.Token(GameText.Poor)} ({richness})";

                        return text;
                    }
                    else
                    {
                        return Localizer.Token(GameText.UltraPoor);
                    }
                }
                else
                {
                    if (MineralRichness > 2.5)  return Localizer.Token(GameText.UltraRich);
                    if (MineralRichness > 1.5)  return Localizer.Token(GameText.Rich);
                    if (MineralRichness > 0.75) return Localizer.Token(GameText.Average);
                    if (MineralRichness > 0.25) return Localizer.Token(GameText.Poor);
                    return Localizer.Token(GameText.UltraPoor);
                }
            }
        }

        public string GetOwnerName()
        {
            if (Owner != null)
                return Owner.data.Traits.Singular;
            return Habitable ? " None" : " Uninhabitable";
        }

        public void InitializePlanetMesh()
        {
            UpdateDescription();
            CreatePlanetSceneObject();

            GravityWellRadius = (float)(Universe.P.GravityWellRange * (1 + ((Math.Log(Scale)) / 1.5)));
        }

        protected void UpdatePositionOnly()
        {
            Position = System.Position.PointFromAngle(OrbitalAngle, OrbitalRadius);
        }

        protected void UpdatePosition(FixedSimTime timeStep)
        {
            PosUpdateTimer -= timeStep.FixedTime;
            if (!Universe.Paused && (PosUpdateTimer <= 0.0f || System.InFrustum))
            {
                PosUpdateTimer = 5f;
                OrbitalAngle += (float) Math.Asin(15.0 / OrbitalRadius);
                if (OrbitalAngle >= 360f)
                    OrbitalAngle -= 360f;
                UpdatePositionOnly();
            }

            bool visible = System.InFrustum;
            if (visible)
            {
                Zrotate += ZrotateAmount * timeStep.FixedTime;
            }

            if (visible && ShouldCreateSO)
            {
                CreatePlanetSceneObject();
            }
            else if (SO != null)
            {
                UpdateSO(visible);
            }
        }

        public Matrix ScaleMatrix => Matrix.CreateScale(PType.Types.PlanetScale * Scale);

        void UpdateSO(bool visible)
        {
            if (visible)
            {
                var pos3d = Matrix.CreateTranslation(Position3D);
                var tilt = Matrix.CreateRotationX(-RadMath.Deg45AsRads);
                var baseScale = ScaleMatrix;
                SO.World = baseScale * Matrix.CreateRotationZ(-Zrotate) * tilt * pos3d;
                SO.Visibility = ObjectVisibility.Rendered;
            }
            else
            {
                SO.Visibility = ObjectVisibility.None;
            }
        }

        bool ShouldCreateSO => !PType.Types.NewRenderer && !GlobalStats.IsUnitTest;

        protected void CreatePlanetSceneObject()
        {
            if (Universe == null)
            {
                Log.Warning("CreatePlanetSceneObject failed: Universe was null!");
                return;
            }

            RemoveSceneObject();

            if (ShouldCreateSO)
            {
                SO = PType.CreatePlanetSO();
                UpdateSO(visible: true);
                Universe.Screen.AddObject(SO);
            }
        }

        public void RemoveSceneObject()
        {
            if (SO != null)
            {
                Universe.Screen.RemoveObject(SO);
                SO = null;
            }
        }

        protected void UpdateDescription()
        {
            if (SpecialDescription != null)
            {
                Description = SpecialDescription;
            }
            else
            {
                Description = Name + " " + PType.Composition.Text + ". ";
                if (BaseMaxFertility > 2)
                {
                    switch (PType.Id)
                    {
                        case 21: Description += Localizer.Token(GameText.TheLushVibranceOfThis); break;
                        case 13:
                        case 22: Description += Localizer.Token(GameText.ItIsAnExtremelyFertile); break;
                        default: Description += Localizer.Token(GameText.ItIsAnExtremelyFertile2); break;
                    }
                }
                else if (BaseMaxFertility > 1)
                {
                    switch (PType.Id)
                    {
                        case 19: Description += Localizer.Token(GameText.TheCombinationOfExtremeHeat); break;
                        case 21: Description += Localizer.Token(GameText.WhileThisIsUnquestionablyA); break;
                        case 13:
                        case 22: Description += Localizer.Token(GameText.MountainsDesertsTundrasForestsOceans); break;
                        default: Description += Localizer.Token(GameText.ItHasAmpleNaturalResources); break;
                    }
                }
                else if (BaseMaxFertility > 0.6f)
                {
                    switch (PType.Id)
                    {
                        case 14: Description += Localizer.Token(GameText.DunesOfSunscorchedSandRise); break;
                        case 21: Description += Localizer.Token(GameText.ScansRevealThatThisPlanet); break;
                        case 17: Description += Localizer.Token(GameText.HoweverScansRevealGeothermalActivity); break;
                        case 19: Description += Localizer.Token(GameText.ThisPlanetAppearsLushAnd); break;
                        case 18: Description += Localizer.Token(GameText.ThisPlanetsEcosystemIsDivided); break;
                        case 11: Description += Localizer.Token(GameText.ACoolPlanetaryTemperatureAnd); break;
                        case 13:
                        case 22: Description += Localizer.Token(GameText.ItAppearsThatSomeEcological); break;
                        default: Description += Localizer.Token(GameText.ItHasADifficultBut); break;
                    }
                }
                else
                {
                    switch (PType.Id) {
                        case 9:
                        case 23: Description += Localizer.Token(GameText.ToxicGasesPermeateTheAtmosphere); break;
                        case 20:
                        case 15: Description += Localizer.Token(GameText.ItsAtmosphereIsComprisedLargely); break;
                        case 17: Description += Localizer.Token(GameText.WithNoAtmosphereToSpeak); break;
                        case 18: Description += Localizer.Token(GameText.ThisPlanetsRoughTerrainAnd); break;
                        case 11: Description += Localizer.Token(GameText.LargeLifelessPlainsDominateThe); break;
                        case 14: Description += Localizer.Token(GameText.DunesOfSunscorchedSandTower); break;
                        case 2:
                        case 6:
                        case 10: Description += Localizer.Token(GameText.GasGiantsLikeThisPlanet); break;
                        case 3:
                        case 4:
                        case 16: Description += Localizer.Token(GameText.TheAtmosphereHereIsVery); break;
                        case 1:  Description += Localizer.Token(GameText.TheLifeCycleOnThis); break;
                        default:
                            if (Habitable)
                                Description = Description ?? "";
                            else
                                Description += Localizer.Token(GameText.ColonizationOfThisPlanetIs);
                            break;
                    }
                }
                if (BaseMaxFertility < 0.6f && MineralRichness >= 2 && Habitable)
                {
                    Description += Localizer.Token(GameText.However2);
                    if      (MineralRichness > 3)  Description += Localizer.Token(GameText.ScansRevealThatThisIs);
                    else if (MineralRichness >= 2) Description += Localizer.Token(GameText.ScansRevealThatThisPlanet2);
                    else if (MineralRichness >= 1) Description += Localizer.Token(GameText.ScansRevealThatThisPlanet3);
                }
                else if (MineralRichness > 3 && Habitable)
                {
                    Description += Localizer.Token(GameText.ScansRevealThatThisIs2);
                }
                else if (MineralRichness >= 2 && Habitable)
                {
                    Description += Name + Localizer.Token(GameText.IsRelativelyMineralRichAnd);
                }
                else if (MineralRichness >= 1 && Habitable)
                {
                    Description += Name + Localizer.Token(GameText.HasAnAverageAbundanceOf);
                }
                else if (MineralRichness < 1 && Habitable)
                {
                    if (PType.Id == 14)
                        Description += Name + Localizer.Token(GameText.SuffersFromALackOf);
                    else
                        Description += Name + Localizer.Token(GameText.LacksSignificantVeinsOfValuable);
                }
            }
        }

        static float GetTraitMax(float invader, float owner) => invader.LowerBound(owner);
        static float GetTraitMin(float invader, float owner) => invader.UpperBound(owner);

        public void ChangeOwnerByInvasion(Empire newOwner, int planetLevel) // TODO: FB - this code needs refactor
        {
            Empire oldOwner = Owner;
            newOwner.DecreaseFleetStrEmpireMultiplier(Owner);
            var thisPlanet = (Planet)this;

            thisPlanet.Construction.ClearQueue();
            thisPlanet.TerraformPoints = 0;
            thisPlanet.SetHomeworld(false);
            thisPlanet.SetOwner(newOwner, newOwner);

            if (IsExploredBy(Universe.Player) && oldOwner != null)
            {
                Universe.Notifications.AddConqueredNotification(thisPlanet, newOwner, oldOwner);
            }

            if (newOwner.data.Traits.Assimilators && planetLevel >= 3)
            {
                RacialTrait ownerTraits = oldOwner.data.Traits;
                newOwner.data.Traits.ConsumptionModifier  = GetTraitMin(newOwner.data.Traits.ConsumptionModifier, ownerTraits.ConsumptionModifier);
                newOwner.data.Traits.PopGrowthMax         = GetTraitMin(newOwner.data.Traits.PopGrowthMax, ownerTraits.PopGrowthMax);
                newOwner.data.Traits.MaintMod             = GetTraitMin(newOwner.data.Traits.MaintMod, ownerTraits.MaintMod);
                newOwner.data.Traits.ShipMaintMultiplier  = GetTraitMin(newOwner.data.Traits.ShipMaintMultiplier, ownerTraits.ShipMaintMultiplier);
                newOwner.data.Traits.DiplomacyMod         = GetTraitMax(newOwner.data.Traits.DiplomacyMod, ownerTraits.DiplomacyMod);
                newOwner.data.Traits.DodgeMod             = GetTraitMax(newOwner.data.Traits.DodgeMod, ownerTraits.DodgeMod);
                newOwner.data.Traits.TargetingModifier    = GetTraitMax(newOwner.data.Traits.TargetingModifier, ownerTraits.TargetingModifier);
                newOwner.data.Traits.GroundCombatModifier = GetTraitMax(newOwner.data.Traits.GroundCombatModifier, ownerTraits.GroundCombatModifier);
                newOwner.data.Traits.Mercantile           = GetTraitMax(newOwner.data.Traits.Mercantile, ownerTraits.Mercantile);
                newOwner.data.Traits.PassengerModifier    = GetTraitMax(newOwner.data.Traits.PassengerModifier, ownerTraits.PassengerModifier);
                newOwner.data.Traits.RepairMod            = GetTraitMax(newOwner.data.Traits.RepairMod, ownerTraits.RepairMod);
                newOwner.data.Traits.PopGrowthMin         = GetTraitMax(newOwner.data.Traits.PopGrowthMin, ownerTraits.PopGrowthMin);
                newOwner.data.Traits.SpyModifier          = GetTraitMax(newOwner.data.Traits.SpyModifier, ownerTraits.SpyModifier);
                newOwner.data.Traits.Spiritual            = GetTraitMax(newOwner.data.Traits.Spiritual, ownerTraits.Spiritual);
                newOwner.data.Traits.TerraformingLevel    = (int)GetTraitMax(newOwner.data.Traits.TerraformingLevel, ownerTraits.TerraformingLevel);

                newOwner.data.Traits.EnemyPlanetInhibitionPercentCounter =
                    GetTraitMax(newOwner.data.Traits.EnemyPlanetInhibitionPercentCounter, ownerTraits.EnemyPlanetInhibitionPercentCounter);

                // Do not add AI difficulty modifiers for the below
                float realProductionMod = Owner.isPlayer && oldOwner.DifficultyModifiers.ProductionMod.NotZero()
                    ? ownerTraits.ProductionMod/oldOwner.DifficultyModifiers.ProductionMod - 1
                    : ownerTraits.ProductionMod;

                float realResearchMod = newOwner.isPlayer && oldOwner.DifficultyModifiers.ResearchMod.NotZero()
                    ? ownerTraits.ResearchMod/oldOwner.DifficultyModifiers.ResearchMod - 1
                    : ownerTraits.ResearchMod;

                float realTaxMod = newOwner.isPlayer && oldOwner.DifficultyModifiers.TaxMod.NotZero()
                    ? ownerTraits.TaxMod/oldOwner.DifficultyModifiers.TaxMod - 1 
                    : ownerTraits.TaxMod;

                float realShipCostMod   = newOwner.isPlayer ? ownerTraits.ShipCostMod - oldOwner.DifficultyModifiers.ShipCostMod : ownerTraits.ShipCostMod;
                float realModHpModifer  = newOwner.isPlayer ? ownerTraits.ModHpModifier - oldOwner.DifficultyModifiers.ModHpModifier : ownerTraits.ModHpModifier;


                newOwner.data.Traits.ShipCostMod   = GetTraitMin(newOwner.data.Traits.ShipCostMod, realShipCostMod); // min
                newOwner.data.Traits.ProductionMod = GetTraitMax(newOwner.data.Traits.ProductionMod, realProductionMod);
                newOwner.data.Traits.ResearchMod   = GetTraitMax(newOwner.data.Traits.ResearchMod, realResearchMod);
                newOwner.data.Traits.ModHpModifier = GetTraitMax(newOwner.data.Traits.ModHpModifier, realModHpModifer);
                newOwner.data.Traits.TaxMod        = GetTraitMax(newOwner.data.Traits.TaxMod, realTaxMod);
            }

            foreach (Ship station in OrbitalStations)
            {
                if (station.Loyalty != newOwner)
                {
                    station.LoyaltyChangeByGift(newOwner);
                    Log.Info($"Owner of platform tethered to {Name} changed from {oldOwner.PortraitName} to {newOwner.PortraitName}");
                }
            }

            thisPlanet.LaunchNonOwnerTroops();
            thisPlanet.AbortLandingPlayerFleets();
            thisPlanet.ResetGarrisonSize();
            thisPlanet.ResetFoodAfterInvasionSuccess();
            Construction.ClearQueue();
            TurnsSinceTurnover        = 0;
            thisPlanet.Quarantine     = false;
            thisPlanet.ManualOrbitals = false;
            thisPlanet.Station?.RemoveSceneObject(); // remove current SO, so it can get reloaded properly

            if (newOwner.isPlayer && !newOwner.AutoColonize)
                CType = Planet.ColonyType.Colony;
            else
                CType = newOwner.AssessColonyNeeds(thisPlanet);

            newOwner.TryTransferCapital(thisPlanet);
        }

        protected void GenerateMoons(SolarSystem system, Planet newOrbital, SolarSystemData.Ring data)
        {
            if (data != null)
            {
                // Add moons to planets
                for (int j = 0; j < data.Moons.Count; j++)
                {
                    float orbitRadius = newOrbital.Radius * 5 + Random.Float(1000f, 1500f) * (j + 1);
                    var moon = new Moon(System,
                                        newOrbital,
                                        data.Moons[j].WhichMoon,
                                        data.Moons[j].MoonScale,
                                        orbitRadius,
                                        Random.Float(0f, 360f),
                                        newOrbital.Position.GenerateRandomPointOnCircle(orbitRadius, Random));
                    System.MoonList.Add(moon);
                }
            }
            else if (newOrbital.PType.MoonTypes.Length != 0)
            {
                int moonCount = (int)Math.Ceiling(Radius * 0.004f);
                moonCount = (int)Math.Round(Random.AvgFloat(-moonCount * 0.75f, moonCount));
                for (int j = 0; j < moonCount; j++)
                {
                    PlanetType moonType = ResourceManager.Planets.RandomMoon(newOrbital.PType);
                    float orbitRadius = newOrbital.Radius + 1500 + Random.Float(1000f, 1500f) * (j + 1);
                    var moon = new Moon(system,
                                        newOrbital,
                                        moonType.Id,
                                        1f, orbitRadius,
                                        Random.Float(0f, 360f),
                                        newOrbital.Position.GenerateRandomPointOnCircle(orbitRadius, Random));
                    System.MoonList.Add(moon);
                }
            }
        }

        public void SetShieldStrengthMax(float value)
        {
            ShieldStrengthMax = value;
        }

        public void ChangeCurrentplanetaryShield(float value)
        {
            ShieldStrengthCurrent = (ShieldStrengthCurrent + value).Clamped(0, ShieldStrengthMax);
        }

        // Used only for Unit tests!
        public void TestSetOrbitalRadius(float value)
        {
            OrbitalRadius = value;
        }

        public Building FindBuilding(Predicate<Building> predicate)
        {
            return BuildingList.Find(predicate);
        }

        public bool HasBuilding(Predicate<Building> predicate)
        {
            return BuildingList.Any(predicate);
        }

        public int CountBuildings(Predicate<Building> predicate)
        {
            return BuildingList.Count(predicate);
        }

        public float SumBuildings(Func<Building, float> selector)
        {
            return BuildingList.Sum(selector);
        }

        public int SumBuildings(Func<Building, int> selector)
        {
            return BuildingList.Sum(selector);
        }

        /// <summary>
        /// Finds the building with the Maximum selected value, example:
        /// Building mostExpensive = planet.FindMaxBuilding(b => b.Cost);
        /// </summary>
        public Building FindMaxBuilding(Func<Building, float> selector)
        {
            return BuildingList.FindMax(selector);
        }

        protected float GetgDysonFertilityMultiplier()
        {
            if (System.HasDysonSwarm)
            {
                float percentLoss = System.DysonSwarm.FertilityPercentLoss;
                if (percentLoss > 0)
                    return percentLoss > 0 ? (1 - percentLoss).LowerBound(0.01f) : 1;
            }

            return 1;
        }
    }
}
