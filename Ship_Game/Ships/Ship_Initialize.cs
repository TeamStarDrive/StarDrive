using Ship_Game.AI;
using Ship_Game.Gameplay;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Point = SDGraphics.Point;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public readonly ShipStats Stats;

        // Universe where this ship was added
        // If this is null, the Ship is not in any universe
        public UniverseState Universe;

        // Create a NEW ship from an existing template
        // You should call Ship.CreateShip() functions to spawn ships
        protected Ship(UniverseState us, int id, Ship template, Empire owner, Vector2 position)
            : base(id, GameObjectType.Ship)
        {
            Universe = us;
            Position     = position;
            Name         = template.Name;
            BaseStrength = template.BaseStrength;
            ShipData     = template.ShipData;

            // loyalty must be set before modules are initialized
            LoyaltyTracker = new Components.LoyaltyChanges(this, owner);

            if (!CreateModuleSlotsFromData(template.ShipData.GetOrLoadDesignSlots()))
                return; // return and crash again...

            // ship must not be added to empire ship list until after modules are validated.
            LoyaltyChangeAtSpawn(owner);

            Stats = new ShipStats(this);
            KnownByEmpires = new Components.KnownByEmpire();
            HasSeenEmpires = new Components.KnownByEmpire();

            VanityName = ResourceManager.ShipNames.GetName(owner.data.Traits.ShipType, ShipData.Role);

            InitializeThrusters(template.ShipData.BaseHull);
            InitializeShip();
            SetInitialCrewLevel();
        }

        // Create a ship from a SavedGame
        // You should call Ship.CreateShip() functions to spawn ships
        protected Ship(UniverseState us, int id, Empire empire, IShipDesign data, SavedGame.ShipSaveData save, ModuleSaveData[] savedModules)
            : base(id, GameObjectType.Ship)
        {
            Universe = us;
            Name       = save.Name;
            VanityName = save.VanityName;
            Level      = save.Level;
            Experience = save.Experience;
            ShipData   = data;

            // loyalty must be set before modules are initialized
            LoyaltyTracker = new Components.LoyaltyChanges(this, empire);

            if (!CreateModuleSlotsFromData(savedModules))
                return;

            // ship must not be added to empire ship list until after modules are validated.
            LoyaltyChangeAtSpawn(empire);

            Stats = new ShipStats(this);
            KnownByEmpires = new Components.KnownByEmpire();
            HasSeenEmpires = new Components.KnownByEmpire();

            InitializeThrusters(data.BaseHull);
            InitializeStatus(fromSave:true);
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
            LoyaltyTracker = new Components.LoyaltyChanges(this, empire);

            if (!CreateModuleSlotsFromData(data.GetOrLoadDesignSlots(), isTemplate, shipyardDesign))
                return;

            // ship must not be added to empire ship list until after modules are validated.
            if (!isTemplate && !shipyardDesign) // don't trigger adding to empire lists for template designs
                LoyaltyChangeAtSpawn(empire);

            Stats = new ShipStats(this);
            KnownByEmpires = new Components.KnownByEmpire();
            HasSeenEmpires = new Components.KnownByEmpire();

            InitializeThrusters(data.BaseHull);
            InitializeStatus(fromSave: false);

            if (isTemplate && !shipyardDesign && !BaseCanWarp &&
                DesignRoleType == RoleType.Warship && !Name.Contains("STL"))
            {
                Log.Warning($"Ship.BaseCanWarp is false: {this}");
            }
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
                Log.Warning($"Ship spawn failed failed '{Name}' due to all empty Modules");
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
                m.InstallModule(this, BaseHull, m.Pos);
                ModuleSlotList[i] = m;
            }

            design.SetDesignSlots(DesignSlot.FromModules(placedModules));
            CreateModuleGrid(ShipData, isTemplate:true, shipyardDesign:true);
        }

        // initialize modules from saved game
        protected bool CreateModuleSlotsFromData(ModuleSaveData[] moduleSaves)
        {
            ResetSlots(moduleSaves.Length);

            if (ModuleSlotList.Length == 0)
            {
                Log.Warning($"Ship spawn failed failed '{Name}' due to all empty Modules");
                return false;
            }

            for (int i = 0; i < moduleSaves.Length; ++i)
            {
                ModuleSaveData slot = moduleSaves[i];
                if (!ResourceManager.ModuleExists(slot.ModuleUID))
                {
                    Log.Warning($"Invalid Module '{slot.ModuleUID}' in '{Name}'");
                    // replace it with a simple module
                    var ds = new DesignSlot(slot.Pos, "OrdnanceLockerSmall", new Point(1,1), 0, ModuleOrientation.Normal, null);
                    slot = new ModuleSaveData(ds, slot.Health, 0, 0);
                }
                ModuleSlotList[i] = ShipModule.Create(Universe, slot, this);
            }

            CreateModuleGrid(ShipData, isTemplate: false, shipyardDesign: false);
            return true;
        }

        void InitializeFromSaveData(SavedGame.ShipSaveData save)
        {
            Position     = save.Position;
            Experience   = save.Experience;
            Kills        = save.Kills;
            PowerCurrent = save.Power;
            YRotation    = save.YRotation;
            Rotation     = save.Rotation;
            Velocity     = save.Velocity;
            IsSpooling   = save.AfterBurnerOn;
            TetheredId   = save.TetheredTo;
            TetherOffset = save.TetherOffset;
            InCombat     = save.InCombat;
            ScuttleTimer = save.ScuttleTimer;

            TransportingFood          = save.TransportingFood;
            TransportingProduction    = save.TransportingProduction;
            TransportingColonists     = save.TransportingColonists;
            AllowInterEmpireTrade     = save.AllowInterEmpireTrade;
            TradeRoutes               = save.TradeRoutes ?? new Array<int>(); // the null check is here in order to not break saves.
            MechanicalBoardingDefense = save.MechanicalBoardingDefense;

            VanityName = ShipData.Role == RoleName.troop && save.TroopList.NotEmpty
                            ? save.TroopList[0].Name : save.VanityName;

            HealthMax = RecalculateMaxHealth();
            CalcTroopBoardingDefense();
            ChangeOrdnance(save.Ordnance);

            if (save.HomePlanetId != 0)
                HomePlanet = Loyalty.FindPlanet(save.HomePlanetId);

            if (Loyalty.WeAreRemnants)
                IsGuardian = true;

            if (save.TroopList != null)
            {
                foreach (Troop t in save.TroopList)
                {
                    t.SetOwner(EmpireManager.GetEmpireByName(t.OwnerString));
                    AddTroop(t);
                }
            }

            if (save.AreaOfOperation != null)
            {
                foreach (Rectangle aoRect in save.AreaOfOperation)
                    AreaOfOperation.Add(aoRect);
            }

            Carrier = Carrier ?? CarrierBays.Create(this, ModuleSlotList);
            Carrier.InitFromSave(save);

            InitializeAIFromAISave(save.AISave);
            LoadFood(save.FoodCount);
            LoadProduction(save.ProdCount);
            LoadColonists(save.PopCount);
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

        public static Ship CreateShipFromSave(UniverseState us, Empire empire,
                                              SavedGame.UniverseSaveData uSave,
                                              SavedGame.ShipSaveData save)
        {
            ModuleSaveData[] savedModules;
            string[] moduleUIDs;
            try
            {
                (savedModules, moduleUIDs) = ShipDesign.GetModuleSaveFromBytes(save.ModuleSaveData);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Failed to deserialize ShipSave Name='{save.Name}'");
                return null;
            }

            IShipDesign design = uSave.GetDesign(save.Name);

            // add the design to ships list if it doesn't exist
            // if there is a newer version of this design, it will not be overwritten
            if (!ResourceManager.Ships.Exists(design.Name))
            {
                ResourceManager.AddShipTemplate((ShipDesign)design, playerDesign: true, readOnly: true);
            }

            Ship ship;
            try
            {
                ship = new Ship(us, save.Id, empire, design, save, savedModules);
            }
            catch (Exception e)
            {
                Log.Error(e);
                // fallback, recreate the ship from design template
                if (!ResourceManager.GetShipTemplate(save.Name, out Ship template))
                    return null;
                ship = new Ship(us, save.Id, template, empire, save.Position);
            }

            if (ship.HasModules)
            {
                ship.InitializeFromSaveData(save);
                us.AddShip(ship);
                return ship;
            }
            return null; // module creation failed
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

            us?.AddShip(ship);
            return ship;
        }

        public static Ship CreateShipAt(UniverseState us, string shipName, Empire owner, Planet p, Vector2 deltaPos, bool doOrbit)
        {
            Ship ship = CreateShipAtPoint(us, shipName, owner, p.Position + deltaPos);
            if (doOrbit && ship != null)
                ship.OrderToOrbit(p, clearOrders:true);
            return ship;
        }

        // Refactored by RedFox - Normal Shipyard ship creation
        public static Ship CreateShipAt(UniverseState us, string shipName, Empire owner, Planet p, bool doOrbit)
        {
            return CreateShipAt(us, shipName, owner, p, RandomMath.Vector2D(300), doOrbit);
        }

        // Hangar Ship Creation
        public static Ship CreateShipFromHangar(UniverseState us, ShipModule hangar, Empire owner, Vector2 p, Ship parent)
        {
            Ship ship = null;
            if (parent.Carrier.PrepHangarShip(owner, hangar, out string shipName))
                ship = CreateShipAtPoint(us, shipName, owner, p);

            if (ship == null)
                return null;

            ship.Mothership = parent;
            ship.Velocity   = parent.Velocity;

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
            return ship;
        }

        public static Ship CreateTroopShipAtPoint(UniverseState us, string shipName, Empire owner, Vector2 point, Troop troop)
        {
            Ship ship = CreateShipAtPoint(us, shipName, owner, point);
            ship.VanityName = troop.DisplayName;
            troop.LandOnShip(ship);
            return ship;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        void InitializeAI()
        {
            AI = new ShipAI(this);
            if (ShipData != null)
                AI.CombatState = ShipData.DefaultCombatState;
        }

        void InitializeAIFromAISave(SavedGame.ShipAISave aiSave)
        {
            InitializeAI();
            AI.State          = aiSave.State;
            AI.DefaultAIState = aiSave.DefaultState;
            AI.CombatState    = aiSave.CombatState;
            AI.StateBits      = aiSave.StateBits;
            AI.MovePosition   = aiSave.MovePosition;
        }

        // This should be called when a Ship is ready to enter the universe
        // Before this call, the ship doesn't have an AI instance
        public void InitializeShip(bool loadingFromSaveGame = false)
        {
            if (VanityName.IsEmpty())
                VanityName = Name;

            if (ShipData.Role == RoleName.platform)
                IsPlatform = true;

            // Begin: ShipSubClass Initialization. Put all ship sub class initializations here
            if (AI == null)
            {
                InitializeAI();
            }
            // End: ship subclass initializations.

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
            Carrier     = CarrierBays.Create(this, ModuleSlotList);
            Supply      = new ShipResupply(this);
            ShipEngines = new ShipEngines(this, ModuleSlotList);

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
            RepairBeams.Clear();
            MaxBank = GetMaxBank();
            if (!fromSave)
                KillAllTroops();
            InitDefendingTroopStrength();

            if (!fromSave)
                Ordinance = 0;

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule module = ModuleSlotList[i];
                if (module.UID == "Dummy") // ignore legacy dummy modules
                    continue;

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
                        RepairBeams.Add(module);
                        HasRepairBeam = true;
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

            if (InhibitionRadius.Greater(0))
                Loyalty.Inhibitors.Add(this); // Start inhibiting at spawn

            MechanicalBoardingDefense = MechanicalBoardingDefense.LowerBound(1);
        }

        void InitShieldsPower(float shieldAmplify)
        {
            foreach (ShipModule shield in Grid.GetShields(ModuleSlotList))
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
