using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;

namespace Ship_Game.Ships
{
    public sealed partial class Ship
    {
        // The only way to spawn instances of Ship is to call Ship.CreateShip... overloads
        private Ship() : base(GameObjectType.Ship)
        {
        }

        public bool CreateModuleSlotsFromData(ModuleSlotData[] templateSlots, bool fromSave, bool isTemplate = false)
        {
            var internalPosistions = new Array<Vector2>();
            int count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slot = templateSlots[i];
                if (slot.Restrictions == Restrictions.I)
                    internalPosistions.Add(slot.Position);
                string uid = slot.InstalledModuleUID;
                if (uid == null || uid == "Dummy") // @note Backwards savegame compatibility for ship designs, dummy modules are deprecated
                    continue;
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
                for (float x = module.XMLPosition.X; x < module.XMLPosition.X + module.XSIZE * 16; x+=16)
                {
                    for (float y = module.XMLPosition.Y; y < module.XMLPosition.Y + module.YSIZE * 16; y += 16)
                    {
                        if (internalPosistions.Contains(new Vector2(x, y)))
                        {
                            module.Restrictions = Restrictions.I;
                            break;
                        }
                    }
                }


                module.HangarShipGuid = slotData.HangarshipGuid;
                module.hangarShipUID  = slotData.SlotOptions;
                ModuleSlotList[count++] = module;
            }

            CreateModuleGrid();
            return true;
        }

        public static Ship CreateShipFromShipData(Empire empire, ShipData data, bool fromSave, bool isTemplate = false)
        {
            var ship = new Ship
            {
                Position   = new Vector2(200f, 200f),
                Name       = data.Name,
                Level      = data.Level,
                experience = data.experience,
                shipData   = data,
                loyalty    = empire
            };

            ship.SetShipData(data);

            if (!ship.CreateModuleSlotsFromData(data.ModuleSlots, fromSave, isTemplate))
                return null;

            foreach (ShipToolScreen.ThrusterZone t in data.ThrusterList)
            {
                ship.ThrusterList.Add(new Thruster
                {
                    Parent = ship,
                    tscale = t.Scale,
                    XMLPos = t.Position
                });
            }

            ship.InitializeStatus(fromSave);
            ship.InitializeThrusters();
            return ship;
        }

        public static Ship CreateShipFromSave(Empire empire, SavedGame.ShipSaveData save)
        {
            save.data.Hull = save.Hull; // @todo Why is this modified here?
            Ship ship = CreateShipFromShipData(empire, save.data, fromSave: true);
            if (ship == null) // happens if module creation failed
                return null;
            ship.InitializeFromSaveData(save);
            return ship;
        }

        private void InitializeFromSaveData(SavedGame.ShipSaveData save)
        {
            guid             = save.guid;
            Position         = save.Position;
            PlayerShip       = save.IsPlayerShip;
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

            VanityName = shipData.Role == ShipData.RoleName.troop && save.TroopList.NotEmpty 
                            ? save.TroopList[0].Name : save.Name;
            
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

            switch (AI.State)
            {
                case AIState.SystemTrader:
                    bool hasCargo = save.FoodCount > 0f || save.ProdCount > 0f;
                    AI.OrderTradeFromSave(hasCargo, save.AISave.startGuid, save.AISave.endGuid);
                    break;
                case AIState.PassengerTransport:
                    AI.OrderTransportPassengersFromSave();
                    break;
            }

            foreach (SavedGame.ProjectileSaveData pdata in save.Projectiles)
                Projectile.Create(this, pdata);
        }

        // Added by RedFox - Debug, Hangar Ship, and Platform creation
        public static Ship CreateShipAtPoint(string shipName, Empire owner, Vector2 position)
        {
            if (!ResourceManager.ShipsDict.TryGetValue(shipName, out Ship template))
            {
                var stackTrace = new Exception();
                MessageBox.Show(
                    $"Failed to create new ship '{shipName}'. This is a bug caused by mismatched or missing ship designs\n\n{stackTrace.StackTrace}",
                    "Ship spawn failed!", MessageBoxButtons.OK);
                return null;
            }

            var ship = new Ship
            {
                shipData     = template.shipData,
                Name         = template.Name,
                BaseStrength = template.BaseStrength,
                BaseCanWarp  = template.BaseCanWarp,
                loyalty      = owner,
                Position     = position
            };

            if (!ship.CreateModuleSlotsFromData(template.shipData.ModuleSlots, fromSave: false))
            {
                Log.Error($"Unexpected failure while spawning ship '{shipName}'. Is the module list corrupted??");
                return null; // return and crash again...
            }

            ship.ThrusterList.Capacity = template.ThrusterList.Count;
            foreach (Thruster t in template.ThrusterList)
            {
                ship.ThrusterList.Add(new Thruster
                {
                    Parent = ship,
                    tscale = t.tscale,
                    XMLPos = t.XMLPos
                });
            }

            // Added by McShooterz: add automatic ship naming
            if (GlobalStats.HasMod)
                ship.VanityName = ResourceManager.ShipNames.GetName(owner.data.Traits.ShipType, ship.shipData.Role);

            if (ship.shipData.Role == ShipData.RoleName.fighter)
                ship.Level += owner.data.BonusFighterLevels;
            ship.Level += owner.data.BaseShipLevel;
            // during new game creation, universeScreen can still be null its not supposed to work on players. 
            if (Empire.Universe != null && Empire.Universe.GameDifficulty > UniverseData.GameDifficulty.Normal &&
                owner != EmpireManager.Player)
                ship.Level += (int) Empire.Universe.GameDifficulty;

            ship.InitializeShip(loadingFromSavegame: false);
            owner.AddShip(ship);
            Empire.Universe?.MasterShipList.Add(ship);
            return ship;
        }

        private void InitializeThrusters()
        {
            if (ThrusterList.IsEmpty || ThrusterList.First.model != null)
                return;

            GameContentManager content = ResourceManager.ContentManager;
            foreach (Thruster t in ThrusterList)
            {
                t.LoadAndAssignDefaultEffects(content);
                t.InitializeForViewing();
            }
        }

        // @bug #1002  cant add a ship to a system in readlock. 
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, Vector2 deltaPos, bool doOrbit)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p.Center + deltaPos);
            if (doOrbit)
                ship.DoOrbit(p);

            //ship.SetSystem(p.ParentSystem);
            return ship;
        }

        // Refactored by RedFox - Normal Shipyard ship creation
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit)
        {
            return CreateShipAt(shipName, owner, p, Vector2.Zero, doOrbit);
        }

        // Added by McShooterz: for refit to keep name
        // Refactored by RedFox
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit, string refitName, int refitLevel)
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
            if (ship == null) return null;
            ship.Mothership = parent;
            ship.Velocity = parent.Velocity;            

            if (ship.SetSupplyShuttleRole(hangar.IsSupplyBay))
                return ship;
            if (ship.SetTroopShuttleRole(hangar.IsTroopBay))
                return ship;
            
            return ship;
        }

        private bool SetSupplyShuttleRole(bool isSupplyBay) => SetSpecialRole(ShipData.RoleName.supply, isSupplyBay, "Supply Shuttle");
        private bool SetTroopShuttleRole(bool isTroopBay) => SetSpecialRole(ShipData.RoleName.troop, isTroopBay, "");

        private bool SetSpecialRole(ShipData.RoleName roleToset, bool ifTrue, string vanityName)
        {
            if (!ifTrue) return false;            
            DesignRole = roleToset;
            if (vanityName.NotEmpty())
                VanityName = vanityName;
            return true;
            
        }

        public static Ship CreateTroopShipAtPoint(string shipName, Empire owner, Vector2 point, Troop troop)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, point);
            ship.VanityName = troop.DisplayName;
            ship.TroopList.Add(ResourceManager.CopyTroop(troop));
            if (ship.shipData.Role == ShipData.RoleName.troop)
                ship.shipData.ShipCategory = ShipData.Category.Combat;
            return ship;
        }



        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        public void InitializeAI()
        {
            AI = new ShipAI(this) { State = AIState.AwaitingOrders };
            if (shipData == null)
                return;
            AI.CombatState = shipData.CombatState;
            AI.CombatAI = new CombatAI(this);
        }

        public void InitializeAIFromAISave(SavedGame.ShipAISave aiSave)
        {
            InitializeAI();
            AI.SetTradeType(aiSave.FoodOrProd);
            AI.State              = aiSave.state;
            AI.DefaultAIState     = aiSave.defaultstate;
            AI.GotoStep           = aiSave.GoToStep;
            AI.MovePosition       = aiSave.MovePosition;
            AI.OrbitTargetGuid    = aiSave.OrbitTarget;
            AI.TargetGuid         = aiSave.AttackTarget;
            AI.SystemToDefendGuid = aiSave.SystemToDefend;
            AI.EscortTargetGuid   = aiSave.EscortTarget;
        }

        public void CreateSceneObject()
        {
            shipData.LoadModel(out ShipSO, out ShipMeshAnim);

            Radius            = ShipSO.WorldBoundingSphere.Radius;                       
            ShipSO.Visibility = ObjectVisibility.Rendered;
            ShipSO.World      = Matrix.CreateTranslation(new Vector3(Position, 0f));


            // Universe will be null during loading, so we need to grab the Global ScreenManager instance from somewhere else
            ScreenManager manager = Empire.Universe?.ScreenManager ?? ResourceManager.ScreenManager;
            manager.AddObject(ShipSO);
        }
        
        public void InitiizeShipScene()
        {
            CreateSceneObject();
            ShipInitialized = true;
        }

        public void InitializeShip(bool loadingFromSavegame = false)
        {
            Center = new Vector2(Position.X + Dimensions.X / 2f, Position.Y + Dimensions.Y / 2f);

            bool worldInit = loadingFromSavegame || Empire.Universe == null;
            if (worldInit)
                CreateSceneObject();
            else
                Empire.Universe.QueueShipToWorldScene(this);
            

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

            // FB: this IF statement so that ships loaded from save wont initialize twice, causing internlslot issues. This is a Workaround
            // issue link: https://bitbucket.org/CrunchyGremlin/sd-blackbox/issues/1538/
            if (!loadingFromSavegame)
                InitializeStatus(loadingFromSavegame); 
            
            SetSystem(System);
            InitExternalSlots();
            base.Initialize();

            RecalculatePower();        
            ShipStatusChange();
            InitializeThrusters();
            SetmaxFTLSpeed();
            DesignRole = GetDesignRole();
            if (worldInit)
                ShipInitialized = true;
        }

        private void InitDefendingTroopStrength()
        {
            TroopBoardingDefense      = 0f;
            
            foreach (Troop troopList in TroopList)
            {
                troopList.SetOwner(loyalty);
                troopList.SetShip(this);
                TroopBoardingDefense += troopList.Strength;
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
                if (numHangarsBays  < TroopList.Count + 1) //FB: if you have more troop_capcacity than hangars, consider adding some tanks
                {
                    type = troopType; // ex: "Space Marine"
                    if (TroopList.Count(trooptype => trooptype.Name == tankType) <= numHangarsBays)
                        type = tankType; 
                    // number of tanks will be up to number of hangars bays you have. If you have 8 barracks and 8 hangar bays
                    // you will get 8 infentry. if you have  8 barracks and 4 bays, you'll get 4 tanks and 4 infantry .
                    // If you have  16 barracks and 4 bays, you'll still get 4 tanks and 12 infantry. 
                    // logic here is that tanks needs hangarbays and barracks, and infantry just needs barracks.
                }
                TroopList.Add(ResourceManager.CreateTroop(type, loyalty));
            }
        }


        public void InitializeStatus(bool fromSave)
        {
            Mass                      = 0f;
            Thrust                    = 0f;
            WarpThrust                = 0f;
            PowerStoreMax             = 0f;
            PowerFlowMax              = 0f;
            shield_max                = 0f;
            shield_power              = 0f;
            armor_max                 = 0f;
            velocityMaximum           = 0f;
            Speed                     = 0f;
            SensorRange               = 0f;
            OrdinanceMax              = 0f;
            OrdAddedPerSecond         = 0f;
            rotationRadiansPerSecond  = 0f;
            Health                    = 0f;
            TroopCapacity             = 0;
            ECMValue                  = 0f;
            FTLSpoolTime              = 0f;
            RangeForOverlay           = 0f;
            Size                      = Calculatesize();
            BaseCost                  = GetBaseCost();
            MaxBank                   = GetMaxBank(MaxBank);

            foreach (Weapon w in Weapons)
            {
                float weaponRange = w.GetModifiedRange();
                if (weaponRange > RangeForOverlay)
                    RangeForOverlay = weaponRange;
            }
            Carrier = CarrierBays.Create(this, ModuleSlotList);
            Supply  = new ShipResupply(this);
            InitializeStatusFromModules(fromSave);
            InitDefendingTroopStrength();
            ActiveInternalSlotCount  = InternalSlotCount;
            velocityMaximum          = Thrust / Mass;
            Speed                    = velocityMaximum;
            rotationRadiansPerSecond = Speed / Size;
            ShipMass                 = Mass;
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
                TroopCapacity             += module.TroopCapacity;
                MechanicalBoardingDefense += module.MechanicalBoardingDefense;

                if (module.SensorRange > SensorRange) SensorRange = module.SensorRange;
                if (module.SensorBonus > sensorBonus) sensorBonus = module.SensorBonus;
                if (module.ECM > ECMValue)            ECMValue    = module.ECM.Clamped(0f, 1f);

                switch (module.ModuleType)
                {
                    case ShipModuleType.Construction:
                        isConstructor = true;
                        shipData.Role = ShipData.RoleName.construction;
                        break;
                    case ShipModuleType.PowerConduit:
                        module.IconTexturePath = GetConduitGraphic(module);
                        break;
                    case ShipModuleType.Colony:
                        isColonyShip = true;
                        break;
                }

                if (module.InstalledWeapon?.isRepairBeam == true)
                {
                    RepairBeams.Add(module);
                    hasRepairBeam = true;
                }

                if (module.HasInternalRestrictions) InternalSlotCount += module.XSIZE * module.YSIZE;
                HasRepairModule |= module.IsRepairModule;

                float massModifier = 1f;
                if (module.Is(ShipModuleType.Armor) && loyalty != null)
                    massModifier = loyalty.data.ArmourMassModifier;
                Mass += module.Mass * massModifier;

                Thrust     += module.thrust;
                WarpThrust += module.WarpThrust;
                Health     += module.Health;

                // Added by McShooterz: fuel cell modifier apply to all modules with power store
                PowerStoreMax += module.ActualPowerStoreMax;
                PowerCurrent  += module.ActualPowerStoreMax;
                PowerFlowMax  += module.ActualPowerFlowMax;
                shield_max    += module.ActualShieldPowerMax;
                if (module.Is(ShipModuleType.Armor))
                    armor_max += module.ActualMaxHealth;

                CargoSpaceMax += module.Cargo_Capacity;
                OrdinanceMax  += module.OrdinanceCapacity;
                if (module.FTLSpoolTime > FTLSpoolTime)
                    FTLSpoolTime = module.FTLSpoolTime;

                if (!fromSave)
                {
                    Ordinance += module.OrdinanceCapacity;
                }
            }

            NetPower = Power.Calculate(ModuleSlotList, loyalty, shipData.ShieldsBehavior);

            if (shipData.Role == ShipData.RoleName.troop)
                TroopCapacity = 1; // set troopship and assault shuttle not to have 0 TroopCapcacity since they have no modules with TroopCapacity 
            MechanicalBoardingDefense = Math.Max(1, MechanicalBoardingDefense);
            shipStatusChanged = true;
            SensorRange += sensorBonus;            
            DesignRole = GetDesignRole();
            //these base values are kinda f'd up. BaseCanWarp isnt being set for the shipdata and so gets passed around alot but isnt ever properly set.
            //also there appear to be two of them and i think that makes no sense.
            // the shipdata should have the base but the ship should have live values. no sense in having in the ship. Think this has been messed up for a while. 
            shipData.BaseCanWarp = WarpThrust > 0;
            BaseCanWarp = WarpThrust > 0;
            BaseStrength = CalculateShipStrength();
            CurrentStrength = BaseStrength;

            // @todo Do we need to recalculate this every time? This whole thing looks fishy
            if (shipData.BaseStrength <= 0f)
                shipData.BaseStrength = BaseStrength;
        }

        private float GetBaseCost()
        {
            return ModuleSlotList.Sum(t => t.Cost);
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
