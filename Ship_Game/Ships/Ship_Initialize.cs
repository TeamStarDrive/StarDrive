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

        // Create a ship from a savegame or a template or in shipyard
        // You can also call Ship.CreateShip... functions to spawn ships
        // @param shipyardDesign This is a potentially incomplete design from Shipyard
        protected Ship(Empire empire, ShipData data, bool fromSave, 
                       bool isTemplate, bool shipyardDesign = false) : base(GameObjectType.Ship)
        {
            if (!data.IsValidForCurrentMod)
            {
                Log.Info($"Design '{data.Name}' [Mod:{data.ModName}] ignored for [{GlobalStats.ModOrVanillaName}]");
                return;
            }

            Position   = new Vector2(200f, 200f);
            Name       = data.Name;
            Level      = data.Level;
            experience = data.experience;
            loyalty    = empire;
            shipData   = data;
            if (fromSave)
                data.UpdateBaseHull(); // when loading from save, the basehull data might not be set

            if (!CreateModuleSlotsFromData(data.ModuleSlots, fromSave, isTemplate, shipyardDesign))
                return;
            
            Stats = new ShipStats(this);
            KnownByEmpires = new DataPackets.KnownByEmpire(this);
            HasSeenEmpires = new DataPackets.KnownByEmpire(this);

            InitializeThrusters(data);
            InitializeStatus(fromSave);
        }

        // create a NEW ship from template and add it to the universe
        Ship(Ship template, Empire owner, Vector2 position) : base(GameObjectType.Ship)
        {
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
            shipData     = template.shipData;
            SetInitialCrewLevel();

            if (!CreateModuleSlotsFromData(template.shipData.ModuleSlots, fromSave: false))
                return; // return and crash again...
            
            Stats = new ShipStats(this);
            KnownByEmpires = new DataPackets.KnownByEmpire(this);
            HasSeenEmpires = new DataPackets.KnownByEmpire(this);

            VanityName = ResourceManager.ShipNames.GetName(owner.data.Traits.ShipType, shipData.Role);
            
            InitializeThrusters(template.shipData);
            InitializeShip();

            owner.AddShip(this);
            Empire.Universe?.Objects.Add(this);
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

        protected bool CreateModuleSlotsFromData(ModuleSlotData[] templateSlots, bool fromSave, 
                                                 bool isTemplate = false, bool shipyardDesign = false)
        {
            Weapons.Clear();
            BombBays.Clear();

            bool hasLegacyDummySlots = false;
            int count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slot = templateSlots[i];
                string uid = slot.ModuleUID;
                // @note Backwards savegame compatibility for ship designs, dummy modules are deprecated
                if (slot.IsDummy)
                {
                    // incomplete shipyard designs are a new feature, so no legacy dummies to fix
                    if (shipyardDesign)
                        continue;
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

            ModuleSlotList = new ShipModule[count];

            count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slotData = templateSlots[i];
                string uid = slotData.ModuleUID;
                if (uid == "Dummy" || uid == null)
                    continue;

                var module = ShipModule.Create(slotData, this, isTemplate, fromSave);
                ModuleSlotList[count++] = module;
            }

            CreateModuleGrid(shipData.GridInfo, shipyardDesign);

            if ((fromSave || isTemplate) && !shipyardDesign && ModuleSlotList.Length == 0)
            {
                Log.Warning($"Failed to load ship '{Name}' due to all empty Modules");
                return false;
            }

            if (hasLegacyDummySlots)
                FixLegacyInternalRestrictions(templateSlots);
            return true;
        }

        public static Ship CreateNewShipTemplate(ShipData data, bool fromSave)
        {
            var ship = new Ship(EmpireManager.Void, data, fromSave, isTemplate:true);
            return ship.HasModules ? ship : null;
        }

        public static Ship CreateShipFromSave(Empire empire, SavedGame.ShipSaveData save)
        {
            // HACK: This is here to enable loading older saves
            //       It can be removed if we break saves in a major release
            if (save.data.Hull.IsEmpty())
                save.data.Hull = save.Hull;

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
            Rotation         = save.Rotation;
            Velocity         = save.Velocity;
            IsSpooling       = save.AfterBurnerOn;
            InCombatTimer    = save.InCombatTimer;
            TetherGuid       = save.TetheredTo;
            TetherOffset     = save.TetherOffset;
            InCombat         = InCombatTimer > 0f;

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
        }

        void SetInitialCrewLevel()
        {
            Level = shipData.Level;

            if (shipData.Role == ShipData.RoleName.fighter)
                Level += loyalty.data.BonusFighterLevels;

            Level += loyalty.data.BaseShipLevel;

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

        public static Ship ImmediateCreateShipAtPoint(string shipName, Empire owner, Vector2 position)
        {
            var ship = CreateShipAtPoint(shipName, owner, position);
            ship?.loyalty.EmpireShipLists.Update();
            return ship;
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
            AI.HasPriorityTarget  = aiSave.PriorityTarget;

            AI.SetPriorityOrder(aiSave.PriorityOrder);
        }

        // This should be called when a Ship is ready to enter the universe
        // Before this call, the ship doesn't have an AI instance
        public void InitializeShip(bool loadingFromSaveGame = false)
        {
            Center = Position;

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
                InitializeAI();
            AI.CombatState = shipData.CombatState;
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
            Carrier = Carrier ?? CarrierBays.Create(this, ModuleSlotList);
            Supply = new ShipResupply(this);
            ShipEngines = new ShipEngines(this, ModuleSlotList);

            // power calc needs to be the first thing
            // otherwise stats update below will fail
            RecalculatePower();
            UpdateStatus(initConstants:true, fromSave);

            BaseStrength = CurrentStrength; // save base strength for later
            if (shipData.BaseStrength <= 0f)
                shipData.BaseStrength = BaseStrength;

            if (!BaseCanWarp && DesignRoleType == ShipData.RoleType.Warship)
                Log.Warning($"Ship.BaseCanWarp is false: {this}");
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

                HasRepairModule |= module.IsRepairModule;
                Health += module.Health;
                if (module.Is(ShipModuleType.Armor))
                    armor_max += module.ActualMaxHealth;

                
                if (!fromSave)
                    Ordinance += module.OrdinanceCapacity; // WARNING: do not use ChangeOrdnance() here!

                if (module.Regenerate > 0)
                    HasRegeneratingModules = true;
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
