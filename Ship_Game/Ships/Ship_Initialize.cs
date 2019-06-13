using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;
using Ship_Game.Data;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        // You can also call Ship.CreateShip... functions to spawn ships
        protected Ship(Empire empire, ShipData data, bool fromSave, bool isTemplate) : base(GameObjectType.Ship)
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
        }

        protected Ship(Ship template, Empire owner, Vector2 position) : base(GameObjectType.Ship)
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
            if (Empire.Universe != null && CurrentGame.Difficulty > UniverseData.GameDifficulty.Normal &&
                owner != EmpireManager.Player)
            {
                Level += (int)CurrentGame.Difficulty;
            }

            InitializeShip(loadingFromSavegame: false);
            owner.AddShip(this);
            Empire.Universe?.MasterShipList.Add(this);
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

        public bool CreateModuleSlotsFromData(ModuleSlotData[] templateSlots, bool fromSave, bool isTemplate = false)
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
            isSpooling       = save.AfterBurnerOn;
            InCombatTimer    = save.InCombatTimer;
            TetherGuid       = save.TetheredTo;
            TetherOffset     = save.TetherOffset;
            InCombat         = InCombatTimer > 0f;
            TroopsLaunched   = save.TroopsLaunched;
            FightersLaunched = save.FightersLaunched;

            TransportingFood       = save.TransportingFood;
            TransportingProduction = save.TransportingProduction;
            TransportingColonists  = save.TransportingColonists;
            AllowInterEmpireTrade  = save.AllowInterEmpireTrade;
            TradeRoutes            = save.TradeRoutes ?? new Array<Guid>(); // the null check is here in order to not break saves.

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
                    TroopList.Add(t);
                }
            }

            if (save.AreaOfOperation != null)
            {
                foreach (Rectangle aoRect in save.AreaOfOperation)
                    AreaOfOperation.Add(aoRect);
            }

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
            return CreateShipAt(shipName, owner, p, RandomMath.Vector2D(100), doOrbit);
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

            if (ship.SetSupplyShuttleRole(hangar.IsSupplyBay))
                return ship;
            if (ship.SetTroopShuttleRole(hangar.IsTroopBay))
                return ship;

            return ship;
        }

        public static Ship CreateDefenseShip(string shipName, Empire owner, Vector2 p, Planet planet)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p);
            ship.VanityName = "Home Defense";
            ship.UpdateHomePlanet(planet);
            ship.HomePlanet = planet;
            return ship;
        }

        bool SetSupplyShuttleRole(bool isSupplyBay) => SetSpecialRole(ShipData.RoleName.supply, isSupplyBay, "Supply Shuttle");
        bool SetTroopShuttleRole(bool isTroopBay)   => SetSpecialRole(ShipData.RoleName.troop, isTroopBay, "");

        bool SetSpecialRole(ShipData.RoleName roleToset, bool ifTrue, string vanityName)
        {
            if (!ifTrue) return false;
            DesignRole = roleToset;
            if (vanityName.NotEmpty())
                VanityName = vanityName;
            return true;
        }

        public static Ship CreateTroopShipAtPoint(string shipName, Empire owner, Vector2 point, Troop troop)
        {
            Ship ship       = CreateShipAtPoint(shipName, owner, point);
            ship.VanityName = troop.DisplayName;
            ship.TroopList.Add(troop);
            troop.SetShip(ship);
            if (ship.shipData.Role == ShipData.RoleName.troop)
                ship.shipData.ShipCategory = ShipData.Category.Conservative;
            return ship;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        public void InitializeAI()
        {
            AI = new ShipAI(this);
            if (shipData == null)
                return;
            AI.CombatState = shipData.CombatState;
            AI.CombatAI    = new CombatAI(this);
        }

        public void InitializeAIFromAISave(SavedGame.ShipAISave aiSave)
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

        public void CreateSceneObject()
        {
            shipData.LoadModel(out ShipSO, Empire.Universe);

            Radius            = ShipSO.WorldBoundingSphere.Radius;
            ShipSO.Visibility = ObjectVisibility.Rendered;
            ShipSO.World      = Matrix.CreateTranslation(new Vector3(Position, 0f));

            ScreenManager.Instance.AddObject(ShipSO);
        }

        public void InitializeShip(bool loadingFromSavegame = false)
        {
            Center = new Vector2(Position.X + Dimensions.X / 2f, Position.Y + Dimensions.Y / 2f);

            CreateSceneObject();

            if (VanityName.IsEmpty())
                VanityName = Name;

            if (shipData.Role == ShipData.RoleName.platform)
                IsPlatform = true;

            if (ResourceManager.GetShipTemplate(Name, out Ship template))
            {
                IsPlayerDesign = template.IsPlayerDesign;
            }
            else FromSave = true;

            //begin: ShipSubClass Initialization. Put all ship sub class intializations here
            if (AI == null)
            {
                InitializeAI();
                AI.CombatState = shipData.CombatState;
            }
            //end: ship subclass initializations.

            // FB: this IF statement so that ships loaded from save wont initialize twice, causing internalslot issues. This is a Workaround
            // issue link: https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/1538/
            if (!loadingFromSavegame)
                InitializeStatus(false);

            SetSystem(System);
            InitExternalSlots();
            Initialize();

            RecalculatePower();
            ShipStatusChange();
            InitializeThrusters();
            SetmaxFTLSpeed();
            DesignRole = GetDesignRole();
            ShipInitialized = true;
        }

        private void InitDefendingTroopStrength()
        {
            TroopBoardingDefense = 0f;

            foreach (Troop t in TroopList)
            {
                t.SetOwner(loyalty);
                t.SetShip(this);
                TroopBoardingDefense += t.Strength;
            }
        }

        private void SpawnTroopsForNewShip(ShipModule module)
        {
            string troopType    = "Wyvern";
            string tankType     = "Wyvern";
            string redshirtType = "Wyvern";

            IReadOnlyList<Troop> unlockedTroops = loyalty?.GetUnlockedTroops();
            if (unlockedTroops?.Count > 0)
            {
                troopType    = unlockedTroops.FindMax(troop => troop.SoftAttack).Name;
                tankType     = unlockedTroops.FindMax(troop => troop.HardAttack).Name;
                redshirtType = unlockedTroops.FindMin(troop => troop.SoftAttack).Name; // redshirts are weakest
                troopType    = (troopType == redshirtType) ? tankType : troopType;
            }

            for (int i = 0; i < module.TroopsSupplied; ++i) // TroopLoad (?)
            {
                int numHangarsBays = Carrier.AllTroopBays.Length;

                string type = troopType;
                if (numHangarsBays < TroopList.Count + 1) //FB: if you have more troop_capacity than hangars, consider adding some tanks
                {
                    type = troopType; // ex: "Space Marine"
                    if (TroopList.Count(trooptype => trooptype.Name == tankType) <= numHangarsBays)
                        type = tankType;
                    // number of tanks will be up to number of hangars bays you have. If you have 8 barracks and 8 hangar bays
                    // you will get 8 infantry. if you have  8 barracks and 4 bays, you'll get 4 tanks and 4 infantry .
                    // If you have  16 barracks and 4 bays, you'll still get 4 tanks and 12 infantry.
                    // logic here is that tanks needs hangarbays and barracks, and infantry just needs barracks.
                }
                Troop newTroop = ResourceManager.CreateTroop(type, loyalty);
                newTroop.LandOnShip(this);
            }
        }

        public float CalcProjAvgSpeed()
        {
            int nonUtilityCount = 0;
            float avgProjSpeed = 0f;
            for (int i = 0; i < Weapons.Count; ++i)
            {
                Weapon w = Weapons[i];
                if (w.DamageAmount < 0.1f) continue;
                nonUtilityCount++;
                avgProjSpeed += w.isBeam ? w.Range : w.ProjectileSpeed; 
            }
            avgProjSpeed /= (float)nonUtilityCount;
            return (avgProjSpeed > 0) ? avgProjSpeed : 800f;
        }

        public void InitializeStatus(bool fromSave)
        {
            Thrust                   = 0f;
            WarpThrust               = 0f;
            PowerStoreMax            = 0f;
            PowerFlowMax             = 0f;
            shield_max               = 0f;
            shield_power             = 0f;
            armor_max                = 0f;
            velocityMaximum          = 0f;
            Speed                    = 0f;
            SensorRange              = 0f;
            OrdinanceMax             = 0f;
            OrdAddedPerSecond        = 0f;
            rotationRadiansPerSecond = 0f;
            Health                   = 0f;
            TroopCapacity            = 0;
            ECMValue                 = 0f;
            FTLSpoolTime             = 0f;
            SurfaceArea              = shipData.ModuleSlots.Length;
            Mass                     = SurfaceArea;
            BaseCost                 = GetBaseCost();
            MaxBank                  = GetMaxBank(MaxBank);

            CalculateWeaponsRanges(); // set Weapons***Range variables.
            RangeForOverlay = WeaponsMaxRange; // setting once. Some might argue that in theory it can change, so maybe add update in ship.updateshipstatus.
            AvgProjectileSpeed = CalcProjAvgSpeed();
            
            Carrier = Carrier ?? CarrierBays.Create(this, ModuleSlotList);
            Supply  = new ShipResupply(this);
            InitializeStatusFromModules(fromSave);
            InitDefendingTroopStrength();
            ActiveInternalSlotCount  = InternalSlotCount;
            velocityMaximum          = Thrust / Mass;
            Speed                    = velocityMaximum;
            rotationRadiansPerSecond = TurnThrust / Mass / 700f;
            ShipMass                 = Mass;
            BaseStrength             = CalculateShipStrength();
            CurrentStrength          = BaseStrength;

            // @todo Do we need to recalculate this every time? This whole thing looks fishy
            if (shipData.BaseStrength <= 0f)
                shipData.BaseStrength = BaseStrength;

            if (FTLSpoolTime <= 0f)
                FTLSpoolTime = 3f;
            UpdateShields();
            SetmaxFTLSpeed();
        }

        private void InitializeStatusFromModules(bool fromSave)
        {
            if (!fromSave)
                TroopList.Clear();
            RepairBeams.Clear();

            float sensorBonus = 0f;
            foreach (ShipModule module in ModuleSlotList)
            {
                if (module.UID == "Dummy") // ignore legacy dummy modules
                    continue;

                if (!fromSave && module.TroopsSupplied > 0) SpawnTroopsForNewShip(module);
                TroopCapacity += module.TroopCapacity;
                MechanicalBoardingDefense += module.MechanicalBoardingDefense;

                if (module.SensorRange > SensorRange) SensorRange = module.SensorRange;
                if (module.SensorBonus > sensorBonus) sensorBonus = module.SensorBonus;
                if (module.ECM > ECMValue) ECMValue = module.ECM.Clamped(0f, 1f);

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

                float massModifier = 1f;
                if (module.Is(ShipModuleType.Armor) && loyalty != null)
                    massModifier = loyalty.data.ArmourMassModifier;
                Mass += module.Mass * massModifier;

                Thrust     += module.thrust;
                WarpThrust += module.WarpThrust;
                TurnThrust += module.TurnThrust;
                Health     += module.Health;

                // Added by McShooterz: fuel cell modifier apply to all modules with power store
                PowerStoreMax += module.ActualPowerStoreMax;
                PowerCurrent  += module.ActualPowerStoreMax;
                PowerFlowMax  += module.ActualPowerFlowMax;
                shield_max    += module.ActualShieldPowerMax;
                if (module.Is(ShipModuleType.Armor))
                    armor_max += module.ActualMaxHealth;

                CargoSpaceMax   += module.Cargo_Capacity;
                OrdinanceMax    += module.OrdinanceCapacity;
                if (module.FTLSpoolTime > FTLSpoolTime)
                    FTLSpoolTime = module.FTLSpoolTime;

                if (!fromSave)
                {
                    Ordinance += module.OrdinanceCapacity;
                }
            }

            NetPower = Power.Calculate(ModuleSlotList, loyalty, shipData.ShieldsBehavior);
            Carrier.PrepShipHangars(loyalty);

            if (shipData.Role == ShipData.RoleName.troop)
                TroopCapacity         = 1; // set troopship and assault shuttle not to have 0 TroopCapacity since they have no modules with TroopCapacity
            MechanicalBoardingDefense = Math.Max(1, MechanicalBoardingDefense);
            shipStatusChanged         = true;
            SensorRange              += sensorBonus;
            DesignRole                = GetDesignRole();
            //these base values are kinda f'd up. BaseCanWarp isn't being set for the shipdata and so gets passed around a lot but isn't ever properly set.
            //also there appear to be two of them and i think that makes no sense.
            // the shipdata should have the base but the ship should have live values. no sense in having in the ship. Think this has been messed up for a while.
            shipData.BaseCanWarp      = WarpThrust > 0;
            BaseCanWarp               = WarpThrust > 0;

        }

        private float GetBaseCost()
        {
            return ModuleSlotList.Sum(module => module.Cost);
        }

        private float GetMaxBank(float mBank)
        {
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
