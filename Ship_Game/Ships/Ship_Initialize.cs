using Ship_Game.AI;
using Ship_Game.Gameplay;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;
using Ship_Game.ExtensionMethods;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public ShipStats Stats;

        // Universe where this ship was added
        // If this is null, the Ship is not in any universe
        public UniverseState Universe;

        // Create a NEW ship from an existing template
        // You should call Ship.CreateShip() functions to spawn ships
        protected Ship(UniverseState us, int id, Ship template, Empire owner, Vector2 position)
            : base(id, GameObjectType.Ship)
        {
            Active = true;
            Universe = us;
            Position     = position;
            Name         = template.Name;
            BaseStrength = template.BaseStrength;
            ShipData     = template.ShipData;

            // loyalty must be set before modules are initialized
            LoyaltyTracker = new(this, owner);

            if (!CreateModuleSlotsFromData(template.ShipData.GetOrLoadDesignSlots()))
                return; // return and crash again...

            // ship must not be added to empire ship list until after modules are validated.
            LoyaltyChangeAtSpawn(owner);

            VanityName = ResourceManager.ShipNames.GetName(owner.data.Traits.ShipType, ShipData.Role);

            Active = true;
            InitializeStats(us);
            InitializeThrusters(template.ShipData.BaseHull);
            InitializeShip();
            SetInitialCrewLevel();
        }

        // Create a ship as a new template or shipyard WIP design
        // You should call Ship.CreateShip() functions to spawn ships
        // @param shipyardDesign This is a WIP design from Shipyard
        protected Ship(UniverseState us, int id, Empire empire, IShipDesign data, bool isTemplate, bool shipyardDesign = false)
            : base(id, GameObjectType.Ship)
        {
            if (!data.IsValidForCurrentMod)
            {
                Log.Info($"Design '{data.Name}' [Mod:{data.ModName}] ignored for [{GlobalStats.ModOrVanillaName}]");
                return;
            }

            Universe = us;
            Name     = data.Name;
            ShipData = data;

            // loyalty must be set before modules are initialized
            LoyaltyTracker = new(this, empire);

            if (!CreateModuleSlotsFromData(data.GetOrLoadDesignSlots(), isTemplate, shipyardDesign))
                return;

            // ship must not be added to empire ship list until after modules are validated.
            if (!isTemplate && !shipyardDesign) // don't trigger adding to empire lists for template designs
                LoyaltyChangeAtSpawn(empire);

            Active = true;
            InitializeStats(us);
            InitializeThrusters(data.BaseHull);
            InitializeStatus(fromSave: false);

            if (isTemplate && !shipyardDesign && !BaseCanWarp &&
                DesignRoleType == RoleType.Warship && !Name.Contains("STL"))
            {
                Log.Warning($"Ship.BaseCanWarp is false: {this}");
            }
        }

        void InitializeStats(UniverseState us)
        {
            Stats = new(this);
            KnownByEmpires = new(us);
            PlayerProjectorHasSeenEmpires = new(us);
        }

        protected static Ship GetShipTemplate(string shipName)
        {
            if (ResourceManager.GetShipTemplate(shipName, out Ship template))
                return template;
            Log.Warning($"Failed to create new ship '{shipName}'. This is a bug caused by mismatched or missing ship designs");
            return ResourceManager.GetShipTemplate("Vulcan Scout", out template) ? template : null;
        }

        void ResetSlots(int length)
        {
            Weapons.Clear();
            BombBays.Clear();
            ModuleSlotList = new ShipModule[length];
        }

        // initialize fresh modules from a new template
        protected bool CreateModuleSlotsFromData(DesignSlot[] templateSlots,
                                                 bool isTemplate = false,
                                                 bool shipyardDesign = false)
        {
            ResetSlots(templateSlots.Length);

            // ignore invalid modules for templates
            if (!isTemplate && ShipData.InvalidModules != null)
            {
                Log.Warning($"Ship spawn failed '{Name}' InvalidModules='{ShipData.InvalidModules}'");
                return false;
            }

            ModuleSlotList = new ShipModule[templateSlots.Length];
            
            if (isTemplate && !shipyardDesign && ModuleSlotList.Length == 0)
            {
                Log.Warning($"Create ShipTemplate failed '{Name}' due to all empty Modules");
                return false;
            }

            for (int i = 0; i < templateSlots.Length; ++i)
            {
                DesignSlot slot = templateSlots[i];
                if (ResourceManager.ModuleExists(slot.ModuleUID))
                {
                    ModuleSlotList[i] = ShipModule.Create(Universe, slot, this, isTemplate);
                }
                else if (!isTemplate) // we already reported it when loading template from files
                {
                    Log.Warning($"Invalid Module '{slot.ModuleUID}' in '{Name}'");
                }
            }

            // if this is a template and we have invalid slots, clear them out
            if (isTemplate && ShipData.InvalidModules != null)
            {
                ModuleSlotList = ModuleSlotList.Filter(s => s != null);
            }

            CreateModuleGrid(ShipData, isTemplate, shipyardDesign);
            return true;
        }

        // use placed modules from ModuleGrid in Shipyard
        protected void CreateModuleSlotsFromShipyardModules(Array<ShipModule> placedModules, ShipDesign design)
        {
            ShipData = design;
            ResetSlots(placedModules.Count);

            for (int i = 0; i < placedModules.Count; ++i)
            {
                ShipModule m = placedModules[i];
                m.UninstallModule();
                m.InstallModule(Universe, this, BaseHull, m.Pos);
                ModuleSlotList[i] = m;
            }

            design.SetDesignSlots(DesignSlot.FromModules(placedModules));
            CreateModuleGrid(ShipData, isTemplate:true, shipyardDesign:true);
        }

        void SetInitialCrewLevel()
        {
            Level = 0;
            if (ShipData.Role is RoleName.fighter or RoleName.corvette or RoleName.drone)
                Level += Loyalty.data.BonusFighterLevels;

            Level += Loyalty.data.BaseShipLevel;
            Level += Loyalty.data.RoleLevels[(int)DesignRole - 1];

            if (!Loyalty.isPlayer)
                Level += Loyalty.DifficultyModifiers.ShipLevel;
        }

        public void InitializeThrusters(ShipHull hull)
        {
            DestroyThrusters();
            ThrusterList = hull.Thrusters.Select(t => new Thruster(this, t.Scale, t.Position));

            if (StarDriveGame.Instance == null) // allows creating ship templates in Unit Tests
                return;

            GameContentManager content = ResourceManager.RootContent;
            foreach (Thruster t in ThrusterList)
            {
                t.LoadAndAssignDefaultEffects(content);
            }
        }

        void DestroyThrusters()
        {
            foreach (Thruster t in ThrusterList)
            {
                 /*todo Dispose*/
            }
            ThrusterList = Empty<Thruster>.Array;
        }
        
        public static Ship CreateNewShipTemplate(Empire voidEmpire, ShipDesign data)
        {
            var ship = new Ship(null, 0, voidEmpire, data, isTemplate:true);
            return ship.HasModules ? ship : null;
        }

        [StarDataConstructor] // for Deserializer
        protected Ship() : base(0, GameObjectType.Ship)
        {
        }

        [StarData] ModuleSaveData[] SavedModules;

        [StarDataSerialize]
        StarDataDynamicField[] OnSerialize()
        {
            return new StarDataDynamicField[]
            {
                new(nameof(SavedModules), GetModuleSaveData())
            };
        }

        // Create a ship from a SavedGame: Ship.OnDeserialized
        [StarDataDeserialized(typeof(ShipDesign), typeof(UniverseParams))]
        public void OnDeserialized(UniverseState us)
        {
            Universe = us;
            var moduleSaves = SavedModules ?? Empty<ModuleSaveData>.Array;
            SavedModules = null;

            // use ShipData from ResourceManager if it exists
            if (ShipData.IsAnExistingSavedDesign)
                ShipData = ResourceManager.Ships.GetDesign(ShipData.Name);

            ResetSlots(moduleSaves.Length);
            for (int i = 0; i < moduleSaves.Length; ++i)
            {
                ModuleSaveData slot = moduleSaves[i];
                if (!ResourceManager.ModuleExists(slot.ModuleUID))
                {
                    Log.Warning($"Invalid Module '{slot.ModuleUID}' in '{Name}'");
                    // replace it with a simple module
                    var ds = new DesignSlot(slot.Pos, "OrdnanceLockerSmall", new Point(1,1), 0, ModuleOrientation.Normal, null);
                    slot = new ModuleSaveData(ds, slot.Health, 0, null);
                }
                ModuleSlotList[i] = ShipModule.Create(Universe, slot, this);
            }

            CreateModuleGrid(ShipData, isTemplate: false, shipyardDesign: false);

            HealthMax = RecalculateMaxHealth();
            CalcTroopBoardingDefense();

            IsGuardian = Loyalty.WeAreRemnants;
            // loyalty must be set before modules are initialized
            LoyaltyTracker = new(this, Loyalty);

            InitializeStats(us);
            InitializeThrusters(ShipData.BaseHull);
            InitializeStatus(fromSave:true);
            SetOrdnance(Ordinance);
        }

        public static Ship CreateShipAtShipyard(UniverseState us, string shipName, Empire owner, Vector2 position)
        {
            Ship ship = CreateShipAtPoint(us, shipName, owner, position);
            if (ship != null)
            {
                float facing = owner.Random.RollDice(50) ? 135 : 315;
                ship.InitLaunch(LaunchPlan.ShipyardBig, facing);
            }
            return ship;
        }

        public static Ship CreateShipAtPoint(UniverseState us, string shipName, Empire owner, Vector2 position)
        {
            Ship template = GetShipTemplate(shipName);
            if (template == null)
            {
                Log.Warning($"CreateShip failed, no such design: {shipName}");
                return null;
            }
            return CreateShipAtPoint(us, template, owner, position);
        }

        // Added by RedFox - Debug, Hangar Ship, and Platform creation
        public static Ship CreateShipAtPoint(UniverseState us, Ship template, Empire owner, Vector2 position)
        {
            if (!template.ShipData.IsValidForCurrentMod)
            {
                Log.Info($"Design {template.ShipData.Name} [Mod:{template.ShipData.ModName}] is not valid for [{GlobalStats.ModOrVanillaName}]");
                return null;
            }

            var ship = new Ship(us, us.CreateId(), template, owner, position);
            if (!ship.HasModules)
                return null;

            us.AddShip(ship);
            return ship;
        }

        public static Ship CreateShipAt(UniverseState us, string shipName, Empire owner, Planet p, Vector2 pos, bool doOrbit)
        {
            Ship ship = CreateShipAtPoint(us, shipName, owner, pos);
            if (ship != null)
            {
                if (ship.IsPlatformOrStation || ship.IsShipyard)
                {
                    ship.TetherToPlanet(p);
                    p.OrbitalStations.Add(ship);
                }
                else if (doOrbit)
                {
                    ship.OrderToOrbit(p, clearOrders: true);
                }
            }
            return ship;
        }

        // Refactored by RedFox
        public static Ship CreateShipNearPlanet(UniverseState us, string shipName, Empire owner, Planet p, bool doOrbit, bool initLaunch = true)
        {
            float randomRadius = owner.Random.Float(p.Radius - 100, p.Radius + 100);
            Ship ship = CreateShipAt(us, shipName, owner, p, p.Position.GenerateRandomPointOnCircle(randomRadius, owner.Random), doOrbit);
            if (initLaunch && ship != null && !ship.IsPlatformOrStation)
                ship.InitLaunch(LaunchPlan.Planet, p);

            return ship;
        }

        // Hangar Ship Creation
        public static Ship CreateShipFromHangar(UniverseState us, ShipModule hangar, Empire owner, Vector2 p, Ship parent)
        {
            Ship ship = null;
            if (parent.Carrier.PrepHangarShip(owner, hangar, out string shipName))
            {  
                Ship template = GetShipTemplate(shipName);
                if (parent.Ordinance >= template?.ShipOrdLaunchCost)
                    ship = CreateShipAtPoint(us, template, owner, p);
                else
                    return null;
            }

            if (ship == null)
            {
                Log.Warning($"Could not create ship from hangar, UID = {hangar.HangarShipUID}");
                return null;
            }

            ship.Mothership = parent;
            ship.InitLaunch(LaunchPlan.Hangar, hangar.ActualRotationDegrees);

            if (hangar.IsSupplyBay)
            {
                if (!ship.IsSupplyShuttle)
                    Log.Error("Expected ship to be a SupplyShuttle !");
                ship.VanityName = "Supply Shuttle";
            }
            else if (hangar.IsTroopBay)
            {
                if (!ship.IsSingleTroopShip)
                    Log.Error("Expected ship to be a SingleTroopShip !");
                ship.VanityName = "";
            }
            return ship;
        }



        public static Ship CreateDefenseShip(UniverseState us, string shipName, Empire owner, Vector2 p, Planet planet)
        {
            Ship ship = CreateShipAtPoint(us, shipName, owner, p);
            ship.VanityName = "Home Defense";
            ship.UpdateHomePlanet(planet);
            ship.HomePlanet = planet;
            ship.InitLaunch(LaunchPlan.Planet, planet);
            return ship;
        }

        public static Ship CreateTroopShipAtPoint(UniverseState us, string shipName, Empire owner, 
            Vector2 point, Troop troop, LaunchPlan launchPlan, float rotationDeg = -1f)
        {
            Ship ship = CreateShipAtPoint(us, shipName, owner, point);
            ship.VanityName = troop.DisplayName;
            troop.LandOnShip(ship);
            ship.InitLaunch(launchPlan, rotationDeg);
            return ship;
        }

        // Note - ship with launch plan cannot enter combat until plan is finished.
        // For testing we have Universe.P.DebugDisableShipLaunch
        public void InitLaunch(LaunchPlan launchPlan, float startingRotationDegrees = -1f)
        {
            if (!Universe.P.DebugDisableShipLaunch)
                LaunchShip = new(this, launchPlan, startingRotationDegrees);
        }

        void InitLaunch(LaunchPlan launchPlan, Planet planet)
        {
            if (!Universe.P.DebugDisableShipLaunch)
            {
                float startingRotationZ = (Position.DirectionToTarget(planet.Position) * -1).ToDegrees();
                LaunchShip = new(this, launchPlan, startingRotationZ);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        void InitializeAI()
        {
            AI = new ShipAI(this);
            if (ShipData != null)
                AI.CombatState = ShipData.DefaultCombatState;
        }

        // This should be called when a Ship is ready to enter the universe
        // Before this call, the ship doesn't have an AI instance
        public void InitializeShip(bool loadingFromSaveGame = false)
        {
            if (VanityName.IsEmpty())
                VanityName = Name;

            if (ShipData.Role == RoleName.platform)
                IsPlatform = true;

            if (AI == null)
            {
                InitializeAI();
            }

            RecalculatePower(); // NOTE: Must be before InitializeStatus

            // when loading savegames, just do a regular ship status update
            // because we already did InitializeStatus in the constructor
            if (loadingFromSaveGame)
            {
                ShipStatusChange();
            }
            else
            {
                InitializeStatus(fromSave: false);
            }

            UpdateModulePositions(FixedSimTime.Zero, isSystemView: false, forceUpdate: true);
        }

        void InitDefendingTroopStrength()
        {
            TroopBoardingDefense = 0f;

            for (int i = 0; i < OurTroops.Count; i++)
            {
                Troop troop = OurTroops[i];
                troop.SetOwner(Loyalty);
                troop.SetShip(this);
                TroopBoardingDefense += troop.Strength;
            }
        }

        void InitializeStatus(bool fromSave)
        {
            Carrier = CarrierBays.Create(this, ModuleSlotList);
            Supply = new(this);
            ShipEngines = new();
            TroopUpdateTimer = Universe?.P.TurnTimer ?? 0; // null for Templates

            // power calc needs to be the first thing
            // otherwise stats update below will fail
            RecalculatePower();
            UpdateStatus(initConstants:true, fromSave);

            BaseStrength = CurrentStrength; // save base strength for later
            UpdateOrdnancePercentage();
        }

        public void ShipStatusChange()
        {
            ShipStatusChanged = false;
            UpdateStatus(initConstants:false, fromSave:false);
        }

        void UpdateStatus(bool initConstants, bool fromSave)
        {
            if (initConstants)
            {
                InitConstantsBeforeUpdate(fromSave);
            }

            Stats.UpdateCoreStats();
            UpdateMassRelated();

            if (initConstants)
            {
                InitConstantsAfterUpdate(fromSave);
            }

            UpdateWeaponRanges();
            CurrentStrength = CalculateShipStrength();

            if (TetheredTo != null)
            {
                var planet = TetheredTo;
                if (planet?.Owner != null && (planet.Owner == Loyalty || Loyalty.IsAlliedWith(planet.Owner)))
                {
                    TrackingPower = TrackingPower.LowerBound(planet.Level);
                    TargetingAccuracy = TargetingAccuracy.LowerBound(planet.Level);
                }
            }
        }

        void UpdateMassRelated()
        {
            Stats.UpdateMassRelated();
            Mass = Stats.Mass;

            UpdateVelocityMax();
        }

        void InitConstantsBeforeUpdate(bool fromSave)
        {
            ArmorMax = 0f;
            Health = 0f;
            TroopCapacity = 0;
            RepairBeams?.Clear();
            MaxBank = GetMaxBank();
            if (!fromSave)
                KillAllTroops();
            InitDefendingTroopStrength();

            if (!fromSave)
                Ordinance = 0;

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule module = ModuleSlotList[i];

                if (!fromSave && module.TroopsSupplied > 0)
                    SpawnTroopsForNewShip(module);

                TroopCapacity += module.TroopCapacity;
                MechanicalBoardingDefense += module.MechanicalBoardingDefense;

                switch (module.ModuleType)
                {
                    case ShipModuleType.PowerConduit:
                        module.IconTexturePath = PwrGrid.GetConduitGraphic(module);
                        break;
                }

                if (module.InstalledWeapon != null)
                {
                    if (module.InstalledWeapon.IsRepairBeam)
                    {
                        RepairBeams ??= new();
                        RepairBeams.Add(module);
                    }

                    if (!module.InstalledWeapon.TruePD && !module.InstalledWeapon.Tag_PD)
                        OrdnanceMin = Math.Max(module.InstalledWeapon.OrdinanceRequiredToFire,OrdnanceMin);
                }

                HasRepairModule |= module.IsRepairModule;
                Health += module.Health;

                if (module.Is(ShipModuleType.Armor))
                    ArmorMax += module.ActualMaxHealth;

                if (!fromSave)
                    Ordinance += module.OrdinanceCapacity; // WARNING: do not use ChangeOrdnance() here!

                if (module.Regenerate > 0)
                    HasRegeneratingModules = true;

                if (module.UID == "MeteorPart")
                    IsMeteor = true;
            }

            HealthMax = Health;
        }

        void InitConstantsAfterUpdate(bool fromSave)
        {
            if (!fromSave)
            {
                PowerCurrent = PowerStoreMax;
                InitShieldsPower(Stats.ShieldAmplifyPerShield);
            }

            UpdateShields();
            if (ShipData.Role == RoleName.troop)
                TroopCapacity = 1; // set troopship and assault shuttle not to have 0 TroopCapacity since they have no modules with TroopCapacity

            MechanicalBoardingDefense = MechanicalBoardingDefense.LowerBound(1);
        }

        void InitShieldsPower(float shieldAmplify)
        {
            foreach (ShipModule shield in GetShields())
            {
                shield.InitShieldPower(shieldAmplify);
            }
        }

        public float GetMaxBank()
        {
            const float mBank = 0.5236f;
            switch (ShipData.Role)
            {
                default:
                    return mBank;
                case RoleName.drone:
                case RoleName.scout:
                case RoleName.fighter:
                    return mBank * 2.1f;
                case RoleName.corvette:
                    return mBank * 1.5f;
            }
        }
    }
}
