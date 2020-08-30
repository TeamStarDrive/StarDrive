﻿using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;
using Ship_Game.Data;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // You can also call Ship.CreateShip... functions to spawn ships
        Ship(Empire empire, ShipData data, bool fromSave, bool isTemplate) : base(GameObjectType.Ship)
        {
            if (!data.IsValidForCurrentMod)
            {
                Log.Info($"Design {data.Name} [Mod:{data.ModName}] ignored for [{GlobalStats.ModOrVanillaName}]");
                return;
            }

            Position   = new Vector2(200f, 200f);
            Name       = data.Name;
            Level      = data.Level;
            experience = data.experience;
            loyalty    = empire;
            SetShipData(data);

            if (!CreateModuleSlotsFromData(data.ModuleSlots, fromSave, isTemplate))
            {
                Log.Warning(ConsoleColor.DarkRed, $"Unexpected failure while spawning ship '{Name}'. Is the module list corrupted??");
                return;
            }

            foreach (ShipToolScreen.ThrusterZone t in data.ThrusterList)
                ThrusterList.Add(new Thruster(this, t.Scale, t.Position));
            InitializeStatus(fromSave);
            InitializeThrusters();
            DesignRole = GetDesignRole();
            KnownByEmpires = new DataPackets.KnownByEmpire(this);
            HasSeenEmpires = new DataPackets.KnownByEmpire(this);
        }

        Ship(Ship template, Empire owner, Vector2 position) : base(GameObjectType.Ship)
        {
            if (template == null)
                return; // Aaarghhh!!

            if (!template.shipData.IsValidForCurrentMod)
            {
                Log.Info($"Design {template.shipData.Name} [Mod:{template.shipData.ModName}] is not valid for [{GlobalStats.ModOrVanillaName}]");
                return;
            }

            Position     = position;
            Name         = template.Name;
            BaseStrength = template.BaseStrength;
            BaseCanWarp  = template.BaseCanWarp;
            loyalty      = owner;
            SetShipData(template.shipData);

            if (!CreateModuleSlotsFromData(template.shipData.ModuleSlots, fromSave: false))
            {
                Log.Warning(ConsoleColor.DarkRed, $"Unexpected failure while spawning ship '{Name}'. Is the module list corrupted??");
                return; // return and crash again...
            }

            KnownByEmpires = new DataPackets.KnownByEmpire(this);
            HasSeenEmpires = new DataPackets.KnownByEmpire(this);

            ThrusterList.Capacity = template.ThrusterList.Count;
            foreach (Thruster t in template.ThrusterList)
                ThrusterList.Add(new Thruster(this, t.tscale, t.XMLPos));

            // Added by McShooterz: add automatic ship naming
            if (GlobalStats.HasMod)
                VanityName = ResourceManager.ShipNames.GetName(owner.data.Traits.ShipType, shipData.Role);

            if (shipData.Role == ShipData.RoleName.fighter)
                Level += owner.data.BonusFighterLevels;

            Level += owner.data.BaseShipLevel;
            // during new game creation, universeScreen can still be null its not supposed to work on players.
            if (Empire.Universe != null && !owner.isPlayer)
                Level += owner.DifficultyModifiers.ShipLevel;

            InitializeShip(loadingFromSaveGame: false);
            if (!BaseCanWarp && DesignRoleType == ShipData.RoleType.Warship)
                Log.Warning($"Warning: Ship base warp is false: {this}");

            owner.AddShip(this);
            Empire.Universe?.MasterShipList.Add(this);
            if (owner.GetEmpireAI() != null && !owner.isPlayer)
                owner.Pool.ForcePoolAdd(this);
        }

        protected Ship(string shipName, Empire owner, Vector2 position)
                : this(GetShipTemplate(shipName), owner, position)
        {
        }

        static Ship GetShipTemplate(string shipName)
        {
            if (ResourceManager.GetShipTemplate(shipName, out Ship template))
                return template;
            Log.Warning($"Failed to create new ship '{shipName}'. This is a bug caused by mismatched or missing ship designs");
            return ResourceManager.GetShipTemplate("Vulcan Scout", out template) ? template : null;
        }

        bool CreateModuleSlotsFromData(ModuleSlotData[] templateSlots, bool fromSave, bool isTemplate = false)
        {
            bool hasLegacyDummySlots = false;
            int count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slot = templateSlots[i];
                string uid = slot.InstalledModuleUID;
                if (uid == null || uid == "Dummy") // @note Backwards savegame compatibility for ship designs, dummy modules are deprecated
                {
                    hasLegacyDummySlots = true;
                    continue;
                }
                if (!ResourceManager.ModuleExists(uid))
                {
                    Log.Warning($"Failed to load ship '{Name}' due to invalid Module '{uid}'!");
                    return false;
                }
                ++count;
            }

            if (count == 0)
            {
                Log.Warning($"Failed to load ship '{Name}' due to all dummy modules!");
                return false;
            }

            ModuleSlotList = new ShipModule[count];

            count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slotData = templateSlots[i];
                string uid = slotData.InstalledModuleUID;
                if (uid == "Dummy" || uid == null)
                    continue;

                ShipModule module = ShipModule.Create(uid, this, slotData, isTemplate, fromSave);
                module.HangarShipGuid   = slotData.HangarshipGuid;
                module.hangarShipUID    = slotData.SlotOptions;
                ModuleSlotList[count++] = module;
            }

            CreateModuleGrid();
            if (hasLegacyDummySlots)
                FixLegacyInternalRestrictions(templateSlots);
            return true;
        }

        public static Ship CreateShipFromShipData(Empire empire, ShipData data, bool fromSave, bool isTemplate = false)
        {
            var ship = new Ship(empire, data, fromSave, isTemplate);
            return ship.HasModules ? ship : null;
        }

        public static Ship CreateShipFromSave(Empire empire, SavedGame.ShipSaveData save)
        {
            save.data.Hull = save.Hull; // @todo Why is this modified here?
            var ship = new Ship(empire, save.data, fromSave: true, isTemplate: false);
            if (!ship.HasModules)
                return null; // module creation failed
            ship.InitializeFromSaveData(save);
            return ship;
        }

        void InitializeFromSaveData(SavedGame.ShipSaveData save)
        {

            guid             = save.guid;
            Position         = save.Position;
            experience       = save.experience;
            kills            = save.kills;
            PowerCurrent     = save.Power;
            yRotation        = save.yRotation;
            Ordinance        = save.Ordnance;
            Rotation         = save.Rotation;
            Velocity         = save.Velocity;
            IsSpooling       = save.AfterBurnerOn;
            InCombatTimer    = save.InCombatTimer;
            TetherGuid       = save.TetheredTo;
            TetherOffset     = save.TetherOffset;
            InCombat         = InCombatTimer > 0f;

            TransportingFood         = save.TransportingFood;
            TransportingProduction   = save.TransportingProduction;
            TransportingColonists    = save.TransportingColonists;
            AllowInterEmpireTrade    = save.AllowInterEmpireTrade;
            TradeRoutes              = save.TradeRoutes ?? new Array<Guid>(); // the null check is here in order to not break saves.

            VanityName = shipData.Role == ShipData.RoleName.troop && save.TroopList.NotEmpty
                            ? save.TroopList[0].Name : save.VanityName;

            HealthMax = RecalculateMaxHealth();

            if (save.HomePlanetGuid != Guid.Empty)
                HomePlanet = loyalty.FindPlanet(save.HomePlanetGuid);

            if (!ResourceManager.ShipTemplateExists(save.Name))
            {
                save.data.Hull = save.Hull;
                ResourceManager.AddShipTemplate(save.data, fromSave: true);
            }

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

            foreach (SavedGame.ProjectileSaveData pdata in save.Projectiles)
            {
                Projectile.Create(this, pdata);
            }
        }


        void InitializeThrusters()
        {
            if (ThrusterList.IsEmpty || ThrusterList.First.model != null)
                return;

            if (StarDriveGame.Instance == null) // allows creating ship templates in Unit Tests
                return;

            GameContentManager content = ResourceManager.RootContent;
            foreach (Thruster t in ThrusterList)
            {
                t.LoadAndAssignDefaultEffects(content);
                t.InitializeForViewing();
            }
        }

        // Added by RedFox - Debug, Hangar Ship, and Platform creation
        public static Ship CreateShipAtPoint(string shipName, Empire owner, Vector2 position)
        {
            var ship = new Ship(shipName, owner, position);
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

        // Added by McShooterz: for refit to keep name
        // Refactored by RedFox
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit,
                                        string refitName, int refitLevel)
        {
            Ship ship = CreateShipAt(shipName, owner, p, doOrbit);

            // Added by McShooterz: add automatic ship naming
            ship.VanityName = refitName;
            ship.Level      = refitLevel;
            return ship;
        }

        // Hangar Ship Creation
        public static Ship CreateShipFromHangar(ShipModule hangar, Empire owner, Vector2 p, Ship parent)
        {
            Ship ship = CreateShipAtPoint(hangar.hangarShipUID, owner, p);
            if (ship == null)
                return null;

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
            if (shipData == null)
                return;
            AI.CombatState = shipData.CombatState;
            AI.CombatAI    = new CombatAI(this);
        }

        void InitializeAIFromAISave(SavedGame.ShipAISave aiSave)
        {
            InitializeAI();
            AI.State              = aiSave.State;
            AI.DefaultAIState     = aiSave.DefaultState;
            AI.MovePosition       = aiSave.MovePosition;
            AI.OrbitTargetGuid    = aiSave.OrbitTarget;
            AI.TargetGuid         = aiSave.AttackTarget;
            AI.SystemToDefendGuid = aiSave.SystemToDefend;
            AI.EscortTargetGuid   = aiSave.EscortTarget;
        }

        public void InitializeShip(bool loadingFromSaveGame = false)
        {
            Center = Position;
            Empire.Universe?.QueueSceneObjectCreation(this);

            if (VanityName.IsEmpty())
                VanityName = Name;

            if (shipData.Role == ShipData.RoleName.platform)
                IsPlatform = true;

            if (ResourceManager.GetShipTemplate(Name, out Ship template))
            {
                IsPlayerDesign = template.IsPlayerDesign;
            }
            else FromSave = true;

            // Begin: ShipSubClass Initialization. Put all ship sub class initializations here
            if (AI == null)
            {
                InitializeAI();
                AI.CombatState = shipData.CombatState;
            }
            // End: ship subclass initializations.
            
            RecalculatePower();

            // FB: this IF statement so that ships loaded from save wont initialize twice, causing internalslot issues. This is a Workaround
            // issue link: https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/1538/
            if (!loadingFromSaveGame)
                InitializeStatus(false);

            SetSystem(System);
            InitExternalSlots();
            Initialize();

            ShipStatusChange();
            InitializeThrusters();
            DesignRole = GetDesignRole();
            ShipInitialized = true;
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
            PowerStoreMax            = 0f;
            PowerFlowMax             = 0f;
            shield_max               = 0f;
            shield_power             = 0f;
            armor_max                = 0f;
            SensorRange              = 0f;
            OrdinanceMax             = 0f;
            OrdAddedPerSecond        = 0f;
            Health                   = 0f;
            TroopCapacity            = 0;
            ECMValue                 = 0f;
            SurfaceArea              = shipData.ModuleSlots.Length;
            BaseCost                 = ShipStats.GetBaseCost(ModuleSlotList);
            MaxBank                  = GetMaxBank();

            Carrier     = Carrier ?? CarrierBays.Create(this, ModuleSlotList);
            Supply      = new ShipResupply(this);
            ShipEngines = new ShipEngines(this, ModuleSlotList);

            InitializeStatusFromModules(fromSave);
            ActiveInternalSlotCount = InternalSlotCount;

            UpdateWeaponRanges();

            InitDefendingTroopStrength();
            UpdateMaxVelocity();
            SetMaxFTLSpeed();
            SetMaxSTLSpeed();
            UpdateShields();

            // initialize strength for our empire:
            CurrentStrength = CalculateShipStrength();
            BaseStrength = CurrentStrength; // save base strength for later
            if (shipData.BaseStrength <= 0f)
                shipData.BaseStrength = BaseStrength;
        }

        void InitializeStatusFromModules(bool fromSave)
        {
            RepairBeams.Clear();

            float sensorBonus = 0f;
            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule module = ModuleSlotList[i];
                if (module.UID == "Dummy") // ignore legacy dummy modules
                    continue;

                if (!fromSave && module.TroopsSupplied > 0)
                    SpawnTroopsForNewShip(module);

                TroopCapacity += module.TroopCapacity;
                MechanicalBoardingDefense += module.MechanicalBoardingDefense;

                if (module.SensorRange > SensorRange) SensorRange = module.SensorRange;
                if (module.SensorBonus > sensorBonus) sensorBonus = module.SensorBonus;
                if (module.ECM > ECMValue)            ECMValue    = module.ECM.Clamped(0f, 1f);
                if (module.Regenerate > 0) HasRegeneratingModules = true;

                switch (module.ModuleType)
                {
                    case ShipModuleType.Construction:
                        IsConstructor = true;
                        shipData.Role = ShipData.RoleName.construction;
                        break;
                    case ShipModuleType.PowerConduit:
                        module.IconTexturePath = GetConduitGraphic(module);
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

                if (module.HasInternalRestrictions)
                    InternalSlotCount += module.XSIZE * module.YSIZE;
                HasRepairModule |= module.IsRepairModule;

                Health     += module.Health;

                // Added by McShooterz: fuel cell modifier apply to all modules with power store
                PowerStoreMax += module.ActualPowerStoreMax;
                PowerCurrent  += module.ActualPowerStoreMax;
                PowerFlowMax  += module.ActualPowerFlowMax;
                if (module.Is(ShipModuleType.Armor))
                    armor_max += module.ActualMaxHealth;

                CargoSpaceMax   += module.Cargo_Capacity;
                OrdinanceMax    += module.OrdinanceCapacity;

                if (!fromSave)
                {
                    Ordinance += module.OrdinanceCapacity;
                }
            }

            if (!fromSave)
                InitShieldsPower();

            RecalculatePower();
            NetPower = Power.Calculate(ModuleSlotList, loyalty);
            Carrier.PrepShipHangars(loyalty);

            if (shipData.Role == ShipData.RoleName.troop)
                TroopCapacity = 1; // set troopship and assault shuttle not to have 0 TroopCapacity since they have no modules with TroopCapacity

            (Thrust, WarpThrust, TurnThrust) = ShipStats.GetThrust(ModuleSlotList, shipData);
            Mass         = ShipStats.GetMass(ModuleSlotList, loyalty);
            FTLSpoolTime = ShipStats.GetFTLSpoolTime(ModuleSlotList, loyalty);

            MechanicalBoardingDefense = MechanicalBoardingDefense.LowerBound(1);
            shipStatusChanged         = true;
            SensorRange              += sensorBonus;
            DesignRole                = GetDesignRole();
            //these base values are kinda f'd up. BaseCanWarp isn't being set for the shipdata and so gets passed around a lot but isn't ever properly set.
            //also there appear to be two of them and i think that makes no sense.
            // the shipdata should have the base but the ship should have live values. no sense in having in the ship. Think this has been messed up for a while.
            shipData.BaseCanWarp      = WarpThrust > 0;
            BaseCanWarp               = WarpThrust > 0;
        }

        void InitShieldsPower()
        {
            float shieldAmplify = ShipUtils.GetShieldAmplification(Amplifiers, Shields);
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
