using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using System;
using Ship_Game.Data;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public readonly ShipStats Stats;

        // create a NEW ship from template and add it to the universe
        protected Ship(Ship template, Empire owner, Vector2 position) : base(GameObjectType.Ship)
        {
            Position     = position;
            Name         = template.Name;
            BaseStrength = template.BaseStrength;
            BaseCanWarp  = template.BaseCanWarp;
            shipData     = template.shipData;

            // loyalty must be set before modules are initialized
            LoyaltyTracker = new Components.LoyaltyChanges(this, owner);

            if (!CreateModuleSlotsFromData(template.shipData.ModuleSlots))
                return; // return and crash again...

            // ship must not be added to empire ship list until after modules are validated.
            LoyaltyChangeAtSpawn(owner);

            Stats = new ShipStats(this);
            KnownByEmpires = new Components.KnownByEmpire();
            HasSeenEmpires = new Components.KnownByEmpire();

            VanityName = ResourceManager.ShipNames.GetName(owner.data.Traits.ShipType, shipData.Role);

            InitializeThrusters(template.shipData);
            InitializeShip();
            SetInitialCrewLevel();
        }

        // Create a ship from a savegame or a template or in shipyard
        // You can also call Ship.CreateShip... functions to spawn ships
        // @param shipyardDesign This is a potentially incomplete design from Shipyard
        protected Ship(Empire empire, SavedGame.ShipSaveData save, ModuleSaveData[] savedModules) : base(GameObjectType.Ship)
        {
            Position   = new Vector2(200f, 200f);
            Name       = save.Name;
            VanityName = save.VanityName;
            Level      = save.Level;
            experience = save.Experience;
            shipData   = data;

            // loyalty must be set before modules are initialized
            LoyaltyTracker = new Components.LoyaltyChanges(this, empire);

            if (!CreateModuleSlotsFromData(savedModules))
                return;

            // ship must not be added to empire ship list until after modules are validated.
            LoyaltyChangeAtSpawn(empire);

            Stats = new ShipStats(this);
            KnownByEmpires = new Components.KnownByEmpire();
            HasSeenEmpires = new Components.KnownByEmpire();

            InitializeThrusters(data);
            InitializeStatus(fromSave:true);
        }

        // Create a ship as a template in shipyard or from a save
        // You can also call Ship.CreateShip... functions to spawn ships
        // @param shipyardDesign This is a potentially incomplete design from Shipyard
        protected Ship(Empire empire, ShipData data, bool isTemplate, bool shipyardDesign = false)
            : base(GameObjectType.Ship)
        {
            if (!data.IsValidForCurrentMod)
            {
                Log.Info($"Design '{data.Name}' [Mod:{data.ModName}] ignored for [{GlobalStats.ModOrVanillaName}]");
                return;
            }

            Position   = new Vector2(200f, 200f);
            Name       = data.Name;
            shipData   = data;

            // loyalty must be set before modules are initialized
            LoyaltyTracker = new Components.LoyaltyChanges(this, empire);

            if (!CreateModuleSlotsFromData(data.ModuleSlots, isTemplate, shipyardDesign))
                return;

            // ship must not be added to empire ship list until after modules are validated.
            if (!isTemplate && !shipyardDesign) // don't trigger adding to empire lists for template designs
                LoyaltyChangeAtSpawn(empire);

            Stats = new ShipStats(this);
            KnownByEmpires = new Components.KnownByEmpire();
            HasSeenEmpires = new Components.KnownByEmpire();

            InitializeThrusters(data);
            InitializeStatus(fromSave: false);
        }

        protected static Ship GetShipTemplate(string shipName)
        {
            if (ResourceManager.GetShipTemplate(shipName, out Ship template))
                return template;
            Log.Warning($"Failed to create new ship '{shipName}'. This is a bug caused by mismatched or missing ship designs");
            return ResourceManager.GetShipTemplate("Vulcan Scout", out template) ? template : null;
        }

        protected bool CreateModuleSlotsFromData(DesignSlot[] templateSlots,
                                                 bool isTemplate = false,
                                                 bool shipyardDesign = false)
        {
            Weapons.Clear();
            BombBays.Clear();

            ModuleSlotList = new ShipModule[templateSlots.Length];
            if (isTemplate && !shipyardDesign && ModuleSlotList.Length == 0)
            {
                Log.Warning($"Failed to load ship '{Name}' due to all empty Modules");
                return false;
            }

            for (int i = 0; i < templateSlots.Length; ++i)
            {
                DesignSlot slot = templateSlots[i];
                if (!ResourceManager.ModuleExists(slot.ModuleUID))
                {
                    Log.Warning($"Failed to load ship '{Name}' due to invalid Module '{slot.ModuleUID}'!");
                    return false;
                }
                ModuleSlotList[i] = ShipModule.Create(slot, this, isTemplate);
            }

            CreateModuleGrid(shipData.GridInfo, shipyardDesign);
            return true;
        }

        protected bool CreateModuleSlotsFromData(ModuleSaveData[] moduleSaves)
        {
            Weapons.Clear();
            BombBays.Clear();

            ModuleSlotList = new ShipModule[moduleSaves.Length];
            if (ModuleSlotList.Length == 0)
            {
                Log.Warning($"Failed to load ship '{Name}' due to all empty Modules");
                return false;
            }

            for (int i = 0; i < moduleSaves.Length; ++i)
            {
                ModuleSaveData slot = moduleSaves[i];
                if (!ResourceManager.ModuleExists(slot.ModuleUID))
                {
                    Log.Warning($"Failed to load ship '{Name}' due to invalid Module '{slot.ModuleUID}'!");
                    return false;
                }
                ModuleSlotList[i] = ShipModule.Create(slot, this);
            }

            CreateModuleGrid(shipData.GridInfo, shipyardDesign:false);
            return true;
        }

        void InitializeFromSaveData(SavedGame.ShipSaveData save)
        {
            guid         = save.guid;
            Position     = save.Position;
            experience   = save.Experience;
            kills        = save.Kills;
            PowerCurrent = save.Power;
            yRotation    = save.yRotation;
            Rotation     = save.Rotation;
            Velocity     = save.Velocity;
            IsSpooling   = save.AfterBurnerOn;
            TetherGuid   = save.TetheredTo;
            TetherOffset = save.TetherOffset;
            InCombat     = save.InCombat;
            ScuttleTimer = save.ScuttleTimer;

            TransportingFood          = save.TransportingFood;
            TransportingProduction    = save.TransportingProduction;
            TransportingColonists     = save.TransportingColonists;
            AllowInterEmpireTrade     = save.AllowInterEmpireTrade;
            TradeRoutes               = save.TradeRoutes ?? new Array<Guid>(); // the null check is here in order to not break saves.
            MechanicalBoardingDefense = save.MechanicalBoardingDefense;

            VanityName = shipData.Role == ShipData.RoleName.troop && save.TroopList.NotEmpty
                            ? save.TroopList[0].Name : save.VanityName;

            HealthMax = RecalculateMaxHealth();
            CalcTroopBoardingDefense();
            ChangeOrdnance(save.Ordnance);

            if (save.HomePlanetGuid != Guid.Empty)
                HomePlanet = loyalty.FindPlanet(save.HomePlanetGuid);

            if (loyalty.WeAreRemnants)
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
            Level = 1;
            if (shipData.Role == ShipData.RoleName.fighter)
                Level += loyalty.data.BonusFighterLevels;

            Level += loyalty.data.BaseShipLevel;
            Level += loyalty.data.RoleLevels[(int)DesignRole - 1];

            if (!loyalty.isPlayer)
                Level += loyalty.DifficultyModifiers.ShipLevel;
        }

        void InitializeThrusters(ShipData data)
        {
            ThrusterList = data.ThrusterList.Select(t => new Thruster(this, t.Scale, t.Position));

            if (StarDriveGame.Instance == null) // allows creating ship templates in Unit Tests
                return;

            GameContentManager content = ResourceManager.RootContent;
            foreach (Thruster t in ThrusterList)
            {
                t.LoadAndAssignDefaultEffects(content);
                t.InitializeForViewing();
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
        
        public static Ship CreateNewShipTemplate(ShipData data)
        {
            var ship = new Ship(EmpireManager.Void, data, isTemplate:true);
            return ship.HasModules ? ship : null;
        }

        public static Ship CreateShipFromSave(Empire empire, SavedGame.ShipSaveData save)
        {
            ModuleSaveData[] savedModules = ShipData.GetModuleSaveFromBase64String(save.ModulesBase64);

            if (!ResourceManager.ShipTemplateExists(save.Name))
            {
                if (!ResourceManager.Hull(save.Hull, out ShipHull hull))
                {
                    Log.Error($"CreateShipFromSave failed: no hull named {save.Hull}");
                    return null;
                }

                var shipData = new ShipData();
                save.data.Hull = save.Hull;
                ResourceManager.AddShipTemplate(save.data);
            }

            var ship = new Ship(empire, save, savedModules);
            if (!ship.HasModules)
                return null; // module creation failed
            ship.InitializeFromSaveData(save);
            return ship;
        }

        // Added by RedFox - Debug, Hangar Ship, and Platform creation
        public static Ship CreateShipAtPoint(string shipName, Empire owner, Vector2 position)
        {
            Ship template = GetShipTemplate(shipName);
            if (template == null)
            {
                Log.Warning($"CreateShip failed, no such design: {shipName}");
                return null;
            }
            
            if (!template.shipData.IsValidForCurrentMod)
            {
                Log.Info($"Design {template.shipData.Name} [Mod:{template.shipData.ModName}] is not valid for [{GlobalStats.ModOrVanillaName}]");
                return null;
            }

            var ship = new Ship(template, owner, position);
            Empire.Universe?.Objects.Add(ship);
            return ship.HasModules ? ship : null;
        }

        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, Vector2 deltaPos, bool doOrbit)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p.Center + deltaPos);
            if (doOrbit)
                ship.OrderToOrbit(p);
            //ship.SetSystem(p.ParentSystem);
            return ship;
        }

        // Refactored by RedFox - Normal Shipyard ship creation
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit)
        {
            return CreateShipAt(shipName, owner, p, RandomMath.Vector2D(300), doOrbit);
        }

        // Hangar Ship Creation
        public static Ship CreateShipFromHangar(ShipModule hangar, Empire owner, Vector2 p, Ship parent)
        {
            Ship ship = CreateShipAtPoint(hangar.HangarShipUID, owner, p);
            if (ship == null)
                return null;

            //if (ship.DesignRole == ShipData.RoleName.drone || ship.DesignRole == ShipData.RoleName.corvette)
            //    ship.Level += owner.data.BonusFighterLevels;

            ship.Mothership = parent;
            ship.Velocity   = parent.Velocity;

            if (hangar.IsSupplyBay)
                ship.SetSpecialRole(ShipData.RoleName.supply, "Supply Shuttle");
            else if (hangar.IsTroopBay)
                ship.SetSpecialRole(ShipData.RoleName.troop, "");
            return ship;
        }

        void SetSpecialRole(ShipData.RoleName role, string vanityName)
        {
            DesignRole = role;
            if (vanityName.NotEmpty())
                VanityName = vanityName;
        }

        public static Ship CreateDefenseShip(string shipName, Empire owner, Vector2 p, Planet planet)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p);
            ship.VanityName = "Home Defense";
            ship.UpdateHomePlanet(planet);
            ship.HomePlanet = planet;
            return ship;
        }

        public static Ship CreateTroopShipAtPoint(string shipName, Empire owner, Vector2 point, Troop troop)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, point);
            ship.VanityName = troop.DisplayName;
            troop.LandOnShip(ship);
            if (ship.shipData.Role == ShipData.RoleName.troop)
                ship.shipData.ShipCategory = ShipData.Category.Conservative;
            return ship;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        void InitializeAI()
        {
            AI = new ShipAI(this);
            if (shipData != null)
                AI.CombatState = shipData.DefaultCombatState;
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

            if (shipData.Role == ShipData.RoleName.platform)
                IsPlatform = true;

            if (ResourceManager.GetShipTemplate(Name, out Ship template))
                IsPlayerDesign = template.IsPlayerDesign;
            else
                FromSave = true; // this is a design which is only available from the savegame

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
                troop.SetOwner(loyalty);
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

            if (!BaseCanWarp && DesignRoleType == ShipData.RoleType.Warship)
                Log.Warning($"Ship.BaseCanWarp is false: {this}");

            UpdateOrdnancePercentage();
        }

        public void ShipStatusChange()
        {
            shipStatusChanged = false;
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
                if (planet?.Owner != null && (planet.Owner == loyalty || loyalty.IsAlliedWith(planet.Owner)))
                {
                    TrackingPower = TrackingPower.LowerBound(planet.Level);
                    TargetingAccuracy = TargetingAccuracy.LowerBound(planet.Level);
                }
            }
        }

        void UpdateMassRelated()
        {
            Stats.UpdateMassRelated();

            Thrust = Stats.Thrust;
            Mass = Stats.Mass;

            UpdateMaxVelocity();
            SetMaxFTLSpeed();
            SetMaxSTLSpeed();
        }

        void InitConstantsBeforeUpdate(bool fromSave)
        {
            armor_max = 0f;
            Health = 0f;
            TroopCapacity = 0;
            RepairBeams.Clear();
            BaseCost = ShipStats.GetBaseCost(ModuleSlotList);
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
                    case ShipModuleType.Construction:
                        IsConstructor = true;
                        shipData.Role = ShipData.RoleName.construction;
                        break;
                    case ShipModuleType.PowerConduit:
                        module.IconTexturePath = PwrGrid.GetConduitGraphic(module);
                        break;
                    case ShipModuleType.Colony:
                        isColonyShip = true;
                        break;
                    case ShipModuleType.Hangar:
                        module.InitHangar();
                        break;
                }

                if (module.InstalledWeapon?.isRepairBeam == true)
                {
                    RepairBeams.Add(module);
                    hasRepairBeam = true;
                }

                HasRepairModule |= module.IsRepairModule;
                Health += module.Health;
                if (module.Is(ShipModuleType.Armor))
                    armor_max += module.ActualMaxHealth;

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

            Carrier.PrepShipHangars(loyalty);

            if (shipData.Role == ShipData.RoleName.troop)
                TroopCapacity = 1; // set troopship and assault shuttle not to have 0 TroopCapacity since they have no modules with TroopCapacity

            if (InhibitionRadius.Greater(0))
                loyalty.Inhibitors.Add(this); // Start inhibiting at spawn

            MechanicalBoardingDefense = MechanicalBoardingDefense.LowerBound(1);
            DesignRole = GetDesignRole();
            BaseCanWarp = Stats.WarpThrust > 0;
        }

        void InitShieldsPower(float shieldAmplify)
        {
            for (int i = 0; i < Shields.Length; i++)
            {
                ShipModule shield = Shields[i];
                shield.InitShieldPower(shieldAmplify);
            }
        }

        float GetMaxBank()
        {
            const float mBank = 0.5236f;
            switch (shipData.Role)
            {
                default:
                    return mBank;
                case ShipData.RoleName.drone:
                case ShipData.RoleName.scout:
                case ShipData.RoleName.fighter:
                    return mBank * 2.1f;
                case ShipData.RoleName.corvette:
                    return mBank * 1.5f;
            }
        }
    }
}
