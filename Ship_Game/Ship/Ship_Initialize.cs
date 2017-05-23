using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.AI;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System.Linq;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship
    {
        // The only way to spawn instances of Ship is to call Ship.CreateShip... overloads
        private Ship() : base(GameObjectType.Ship)
        {
        }

        public bool CreateModuleSlotsFromData(ModuleSlotData[] templateSlots, bool fromSave, bool addToShieldManager = true)
        {
            int count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                string uid = templateSlots[i].InstalledModuleUID;
                if (uid == "Dummy") // @note Backwards savegame compatibility for ship designs, dummy modules are deprecated
                    continue;
                if (!ResourceManager.ShipModules.ContainsKey(uid))
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
                string uid = slotData.InstalledModuleUID;
                if (uid == "Dummy")
                    continue;
                ShipModule module = ShipModule.Create(uid, this, slotData.Position, slotData.Facing, addToShieldManager);
                if (fromSave)
                {
                    module.Health      = slotData.Health;
                    module.ShieldPower = slotData.ShieldPower;
                }
                module.HangarShipGuid = slotData.HangarshipGuid;
                module.hangarShipUID  = slotData.SlotOptions;
                ModuleSlotList[count++] = module;
            }

            CreateModuleGrid();
            return true;
        }

        public static Ship CreateShipFromShipData(ShipData data, bool fromSave, bool addToShieldManager = true)
        {
            var ship = new Ship
            {
                Position   = new Vector2(200f, 200f),
                Name       = data.Name,
                Level      = data.Level,
                experience = data.experience,
                shipData   = data
            };

            if (!ship.CreateModuleSlotsFromData(data.ModuleSlots, fromSave, addToShieldManager))
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

            ship.SetShipData(data);
            ship.InitializeThrusters();
            return ship;
        }

        // Added by RedFox - Debug, Hangar Ship, and Platform creation
        public static Ship CreateShipAtPoint(string shipName, Empire owner, Vector2 position)
        {
            if (!ResourceManager.ShipsDict.TryGetValue(shipName, out Ship template))
            {
                var stackTrace = new Exception();
                MessageBox.Show($"Failed to create new ship '{shipName}'. This is a bug caused by mismatched or missing ship designs\n\n{stackTrace.StackTrace}",
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

            // during new game creation, universeScreen can still be null
            if (Empire.Universe != null && Empire.Universe.GameDifficulty > UniverseData.GameDifficulty.Normal)
                ship.Level += (int)Empire.Universe.GameDifficulty;

            ship.InitializeShip(loadingFromSavegame: false);

            owner.AddShip(ship);
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

            ship.SetSystem(p.system);
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
        public static Ship CreateShipFromHangar(string key, Empire owner, Vector2 p, Ship parent)
        {
            Ship ship = CreateShipAtPoint(key, owner, p);
            if (ship == null) return null;
            ship.Mothership = parent;
            ship.Velocity = parent.Velocity;
            return ship;
        }

        public static Ship CreateTroopShipAtPoint(string shipName, Empire owner, Vector2 point, Troop troop)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, point);
            ship.VanityName = troop.Name;
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
            AI.FoodOrProd         = aiSave.FoodOrProd;
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
            Model model;
            if (shipData.Animated)
            {
                SkinnedModel skinned = ResourceManager.GetSkinnedModel(shipData.ModelPath);
                ShipMeshAnim = new AnimationController(skinned.SkeletonBones);
                ShipMeshAnim.StartClip(skinned.AnimationClips["Take 001"]);
                model = skinned.Model;
            }
            else
            {
                model = ResourceManager.GetModel(shipData.ModelPath);
            }

            ShipSO = new SceneObject(model.Meshes[0])
            {
                ObjectType = ObjectType.Dynamic,
                Visibility = ObjectVisibility.Rendered
            };

            Radius = ShipSO.WorldBoundingSphere.Radius;
            Center = new Vector2(Position.X + Dimensions.X / 2f, Position.Y + Dimensions.Y / 2f);
            ShipSO.Visibility = ObjectVisibility.Rendered;
            ShipSO.World = Matrix.CreateTranslation(new Vector3(Position, 0f));

            // Universe will be null during loading, so we need to grab the Global ScreenManager instance from somewhere else
            var manager = Empire.Universe?.ScreenManager ?? ResourceManager.ScreenManager;
            manager.AddObject(ShipSO);
        }

        public void InitializeShip(bool loadingFromSavegame)
        {
            CreateSceneObject();
            SetShipData(GetShipData());

            if (VanityName.IsEmpty())
                VanityName = Name;

            if (shipData.Role == ShipData.RoleName.platform)
                IsPlatform = true;

            if (ResourceManager.GetShipTemplate(Name, out Ship template))
            {
                IsPlayerDesign = template.IsPlayerDesign;
            }
            else FromSave = true;

            if (AI == null)
            {
                InitializeAI();
                AI.CombatState = shipData.CombatState;
            }

            InitializeStatus(loadingFromSavegame);

            SetSystem(System);
            InitExternalSlots();
            base.Initialize();

            RecalculatePower();        
            ShipStatusChange();
            InitializeThrusters();
            RecalculateMaxHP();

            ShipInitialized = true;
        }

        private void InitDefendingTroopStrength()
        {
            TroopBoardingDefense      = 0f;
            MechanicalBoardingDefense = 0f;
            foreach (Troop troopList in TroopList)
            {
                troopList.SetOwner(loyalty);
                troopList.SetShip(this);
                TroopBoardingDefense += troopList.Strength;
            }
            MechanicalBoardingDefense *= (1 + TroopList.Count / 10);
            if (MechanicalBoardingDefense < 1f)
                MechanicalBoardingDefense = 1f;
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
                int numTroopHangars = ModuleSlotList.Count(hangarbay => hangarbay.IsTroopBay);

                string type = redshirtType;
                if (numTroopHangars < TroopList.Count)
                {
                    type = troopType; // ex: "Space Marine"
                    if (TroopList.Count(trooptype => trooptype.Name == tankType) <= numTroopHangars / 2)
                        type = tankType;
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
            ModulePowerDraw           = 0f;
            ShieldPowerDraw           = 0f;
            shield_max                = 0f;
            shield_power              = 0f;
            armor_max                 = 0f;
            velocityMaximum           = 0f;
            speed                     = 0f;
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

            foreach (Weapon w in Weapons)
            {
                float weaponRange = w.GetModifiedRange();
                if (weaponRange > RangeForOverlay)
                    RangeForOverlay = weaponRange;
            }

            InitializeStatusFromModules(fromSave);
            InitDefendingTroopStrength();

            HealthMax                = Health;
            ActiveInternalSlotCount  = InternalSlotCount;
            velocityMaximum          = Thrust / Mass;
            speed                    = velocityMaximum;
            rotationRadiansPerSecond = speed / Size;
            ShipMass                 = Mass;
            if (FTLSpoolTime <= 0f)
                FTLSpoolTime = 3f;
            UpdateShields();
        }

        private void InitializeStatusFromModules(bool fromSave)
        {
            if (!fromSave)
                TroopList.Clear();
            Hangars.Clear();
            Transporters.Clear();
            RepairBeams.Clear();

            float sensorBonus = 0f;
            foreach (ShipModule module in ModuleSlotList)
            {
                if (module.UID == "Dummy") // ignore legacy dummy modules
                    continue;

                if (!fromSave && module.TroopsSupplied > 0)
                    SpawnTroopsForNewShip(module);

                TroopCapacity += module.TroopCapacity;
                MechanicalBoardingDefense += module.MechanicalBoardingDefense;
                if (MechanicalBoardingDefense < 1f)
                    MechanicalBoardingDefense = 1f;

                if (module.SensorRange > SensorRange) SensorRange = module.SensorRange;
                if (module.SensorBonus > sensorBonus) sensorBonus = module.SensorBonus;
                if (module.ECM > ECMValue) ECMValue = module.ECM.Clamp(0f, 1f);

                if (module.ModuleType == ShipModuleType.Construction)
                {
                    isConstructor = true;
                    shipData.Role = ShipData.RoleName.construction;
                }
                else if (module.ModuleType == ShipModuleType.PowerConduit)
                {
                    module.IconTexturePath = GetConduitGraphic(module);
                }
                else if (module.ModuleType == ShipModuleType.Hangar)
                {
                    Hangars.Add(module);
                    HasTroopBay |= module.IsTroopBay;
                }
                else if (module.ModuleType == ShipModuleType.Transporter)
                {
                    Transporters.Add(module);
                    hasTransporter = true;
                    hasOrdnanceTransporter |= module.TransporterOrdnance > 0;
                    hasAssaultTransporter  |= module.TransporterTroopAssault > 0;
                }
                else if (module.ModuleType == ShipModuleType.Colony)
                {
                    isColonyShip = true;
                }

                if (module.InstalledWeapon?.isRepairBeam == true)
                {
                    RepairBeams.Add(module);
                    hasRepairBeam = true;
                }

                InternalSlotCount += module.Restrictions == Restrictions.I ? 1 : 0;
                HasRepairModule |= module.IsRepairModule;

                float massModifier = 1f;
                if (module.ModuleType == ShipModuleType.Armor && loyalty != null)
                    massModifier = loyalty.data.ArmourMassModifier;
                Mass += module.Mass * massModifier;

                Thrust     += module.thrust;
                WarpThrust += module.WarpThrust;

                // Added by McShooterz: fuel cell modifier apply to all modules with power store
                PowerStoreMax += module.PowerStoreMax * (loyalty?.data.FuelCellModifier ?? 0);
                PowerCurrent  += module.PowerStoreMax;
                PowerFlowMax  += module.PowerFlowMax     * (loyalty?.data.PowerFlowMod   ?? 0);
                shield_max    += module.shield_power_max * (loyalty?.data.ShieldPowerMod ?? 0);
                if (module.ModuleType == ShipModuleType.Armor)
                    armor_max += module.HealthMax;

                CargoSpaceMax += module.Cargo_Capacity;
                OrdinanceMax  += module.OrdinanceCapacity;
                if (module.ModuleType == ShipModuleType.Shield) ShieldPowerDraw += module.PowerDraw;
                else                                            ModulePowerDraw += module.PowerDraw;
                if (module.FTLSpoolTime > FTLSpoolTime)
                    FTLSpoolTime = module.FTLSpoolTime;

                if (!fromSave)
                {
                    Ordinance     += module.OrdinanceCapacity;
                    Health += module.Health;
                }
            }

            SensorRange += sensorBonus;
        }
    }
}
