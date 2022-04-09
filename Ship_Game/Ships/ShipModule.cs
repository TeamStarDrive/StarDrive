using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using System;
using System.Diagnostics.Contracts;
using Ship_Game.Universe;

namespace Ship_Game.Ships
{
    public sealed class ShipModule : GameplayObject
    {
        public bool ThisClassMustNotBeAutoSerializedByDotNet =>
            throw new InvalidOperationException(
                $"BUG! ShipModule must not be serialized! Add [XmlIgnore][JsonIgnore] to `public ShipModule XXX;` PROPERTIES/FIELDS. {this}");

        //private static int TotalModules = 0;
        //public int ID = ++TotalModules;
        public ShipModuleFlyweight Flyweight; //This is where all the other member variables went. Having this as a member object
                                              //allows me to instance the variables inside it, so they are not duplicated. This
                                              //can offer much better memory usage since ShipModules are so numerous.     -Gretman

        // @note This is always Normalized to [0; +2PI] by FacingDegrees setter
        public float TurretAngleRads;

        // Turret angle in degrees
        public int TurretAngle
        {
            get => (int)TurretAngleRads.ToDegrees();
            set => TurretAngleRads = ((float)value).ToRadians();
        }

        // This is the Rotation of the Installed Module: Normal, Left, Right, Rear
        public ModuleOrientation ModuleRot;

        public bool Powered;
        public bool IsExternal;
        public int XSize = 1;
        public int YSize = 1;

        // Gets the size of this Module, correctly oriented
        // Required by ModuleGrid
        public Point GetSize() => new Point(XSize, YSize);
        
        // Size of this module in World Coordinates
        public Vector2 WorldSize => new Vector2(XSize * 16, YSize * 16);

        // Slot top-left Grid Position in the Ship design
        public Point Pos;

        // Center of the module in World Coordinates, relative to ship center
        public Vector2 LocalCenter;

        float ZPos;
        public Vector3 Center3D => new Vector3(Position, ZPos);

        public int Area => XSize * YSize;

        public bool CanVisualizeDamage;
        public float ShieldPower { get; private set; }
        public short OrdinanceCapacity;
        bool OnFire; // is this module on fire?
        const float OnFireThreshold = 0.15f;
        ShipModuleDamageVisualization DamageVisualizer;
        public EmpireHullBonuses Bonuses = EmpireHullBonuses.Default;
        Ship Parent;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public LocalizedText NameText => new LocalizedText(NameIndex);
        public LocalizedText DescriptionText => new LocalizedText(DescriptionIndex);
        public Restrictions Restrictions;
        public Shield Shield { get; private set; }
        public string HangarShipUID;
        Ship HangarShip;
        public int HangarShipId;
        public float HangarTimer;
        public bool IsWeapon;
        public Weapon InstalledWeapon;

        public DynamicHangarOptions DynamicHangar { get; private set; }

        public ShipModuleType ModuleType;
        public string IconTexturePath;

        /// Field of fire arc, now in Radians.
        /// Conversion to Radians is done during loading
        /// Game files still use Degrees
        public float FieldOfFire;
        public int TargetValue;
        public float TransporterTimer;
        public const int MaxPriority = 6;

        float TemplateMaxHealth; // this is the health design spec of the module
        float WeaponRotation;
        public float WeaponECM = 0;

        //This wall of text is the 'get' functions for all of the variables that got moved to the 'Flyweight' object.
        //This will allow us to still use the normal "Module.IsCommandModule" even though 'IsCommandModule' actually
        //lives in "Module.Flyweight.IsCommandModule" now.    -Gretman
        public string UID => Flyweight.UID;
        public string DeployBuildingOnColonize   => Flyweight.DeployBuildingOnColonize;
        public string ResourceStored             => Flyweight.ResourceStored;
        public bool IsCommandModule              => Flyweight.IsCommandModule;
        public bool IsRepairModule               => Flyweight.IsRepairModule;
        public string[] PermittedHangarRoles     => Flyweight.PermittedHangarRoles;
        public short MaximumHangarShipSize       => Flyweight.MaximumHangarShipSize;
        public bool DroneModule                  => Flyweight.DroneModule;
        public bool FighterModule                => Flyweight.FighterModule;
        public bool CorvetteModule               => Flyweight.CorvetteModule;
        public bool FrigateModule                => Flyweight.FrigateModule;
        public bool DestroyerModule              => Flyweight.DestroyerModule;
        public bool CruiserModule                => Flyweight.CruiserModule;
        public bool BattleshipModule             => Flyweight.BattleshipModule;
        public bool CapitalModule                => Flyweight.CapitalModule;
        public bool FreighterModule              => Flyweight.FreighterModule;
        public bool PlatformModule               => Flyweight.PlatformModule;
        public bool StationModule                => Flyweight.StationModule;
        public float SensorRange                 => Flyweight.SensorRange;
        public float MechanicalBoardingDefense   => Flyweight.MechanicalBoardingDefense;
        public float EMPProtection               => Flyweight.EMPProtection;
        public int PowerRadius                   => Flyweight.PowerRadius;
        public int TechLevel                     => Flyweight.TechLevel;
        public float OrdnanceAddedPerSecond      => Flyweight.OrdnanceAddedPerSecond;
        public string BombType                   => Flyweight.BombType;
        public float BonusRepairRate             => Flyweight.BonusRepairRate;
        public float CargoCapacity               => Flyweight.CargoCapacity;
        public float ShieldRadius                => Flyweight.ShieldRadius;
        public float ShieldPowerMax              => Flyweight.ShieldPowerMax;
        public float ShieldRechargeRate          => Flyweight.ShieldRechargeRate;
        public float ShieldRechargeCombatRate    => Flyweight.ShieldRechargeCombatRate;
        public float ShieldRechargeDelay         => Flyweight.ShieldRechargeDelay;
        public float ShieldDeflection            => Flyweight.ShieldDeflection;
        public float ShieldKineticResist         => Flyweight.ShieldKineticResist;
        public float ShieldEnergyResist          => Flyweight.ShieldEnergyResist;
        public float ShieldExplosiveResist       => Flyweight.ShieldExplosiveResist;
        public float ShieldMissileResist         => Flyweight.ShieldMissileResist;
        public float ShieldPlasmaResist          => Flyweight.ShieldPlasmaResist;
        public float ShieldBeamResist            => Flyweight.ShieldBeamResist;
        public float NumberOfColonists           => Flyweight.NumberOfColonists; // In Millions!
        public float NumberOfEquipment           => Flyweight.NumberOfEquipment;
        public float NumberOfFood                => Flyweight.NumberOfFood;
        public bool IsSupplyBay                  => Flyweight.IsSupplyBay;
        public bool IsTroopBay                   => Flyweight.IsTroopBay;
        public float HangarTimerConstant         => Flyweight.HangarTimerConstant;
        public float Thrust                      => Flyweight.Thrust;
        public float WarpThrust                  => Flyweight.WarpThrust;
        public float TurnThrust                  => Flyweight.TurnThrust;
        public float PowerFlowMax                => Flyweight.PowerFlowMax;
        public float PowerDraw                   => Flyweight.PowerDraw;
        public float PowerDrawAtWarp             => Flyweight.PowerDrawAtWarp;
        public float PowerStoreMax               => Flyweight.PowerStoreMax;
        public float HealPerTurn                 => Flyweight.HealPerTurn;
        public int TroopCapacity                 => Flyweight.TroopCapacity;
        public int TroopsSupplied                => Flyweight.TroopsSupplied;
        public float Cost                        => Flyweight.Cost;
        public float InhibitionRadius            => Flyweight.InhibitionRadius;
        public float FTLSpoolTime                => Flyweight.FTLSpoolTime;
        public float ECM                         => Flyweight.ECM;
        public float SensorBonus                 => Flyweight.SensorBonus;
        public float TransporterTimerConstant    => Flyweight.TransporterTimerConstant;
        public float TransporterRange            => Flyweight.TransporterRange;
        public float TransporterPower            => Flyweight.TransporterPower;
        public float TransporterOrdnance         => Flyweight.TransporterOrdnance;
        public int TransporterTroopLanding       => Flyweight.TransporterTroopLanding;
        public int TransporterTroopAssault       => Flyweight.TransporterTroopAssault;
        public float KineticResist               => Flyweight.KineticResist;
        public float EnergyResist                => Flyweight.EnergyResist;
        public float MissileResist               => Flyweight.MissileResist;
        public float PlasmaResist                => Flyweight.PlasmaResist;
        public float BeamResist                  => Flyweight.BeamResist;
        public float ExplosiveResist             => Flyweight.ExplosiveResist;
        public float TorpedoResist               => Flyweight.TorpedoResist;
        public float Deflection                  => Flyweight.Deflection;
        public int APResist                      => Flyweight.APResist;
        public bool AlwaysPowered                => Flyweight.IndirectPower;
        public int TargetTracking                => Flyweight.TargetTracking;
        public int TargetingAccuracy             => Flyweight.TargetAccuracy;
        public int ExplosionDamage               => Flyweight.ExplosionDamage;
        public int ExplosionRadius               => Flyweight.ExplosionRadius;
        public float RepairDifficulty            => Flyweight.RepairDifficulty;
        public string ShieldBubbleColor          => Flyweight.ShieldBubbleColor;
        public float Regenerate                  => Flyweight.Regenerate; // Self regenerating modules
        public bool DisableRotation              => Flyweight.DisableRotation;
        public float AmplifyShields              => Flyweight.AmplifyShields;

        /// <summary>
        /// This is an override of default weapon accuracy. <see cref="Weapon.BaseTargetError(int)"/>
        /// it is uniform to all weapons. 50% accuracy creates the same base error for all weapons. 
        /// an accuracy percent of 1 removes all target error.
        /// the default of -1 means ignore this value
        /// the percentage is based on the parent module being an 8x8 module
        /// (8x8 * 16 to the power of 1-accuracy percent. 
        /// 100% = 0 slots
        /// 75% = ~half slot
        /// 50% = 2 slots
        /// 25% = 11 slots
        /// 0%  = 64 slots
        /// </summary>
        public float AccuracyPercent      => Flyweight?.AccuracyPercent ?? -1;
        public float WeaponInaccuracyBase => Flyweight?.WeaponInaccuracyBase ?? 1;

        public bool Explodes => ExplosionDamage > 0;

        // used in module selection category
        public bool IsPowerArmor => Is(ShipModuleType.Armor) && !IsBulkhead && (PowerDraw > 0 || PowerFlowMax > 0);

        // used in module selection category
        public bool IsBulkhead => Is(ShipModuleType.Armor) && 
                                  (Restrictions == Restrictions.I 
                                   || Restrictions == Restrictions.IE 
                                   || Restrictions == Restrictions.xI);


        public bool IsWeaponOrBomb => ModuleType == ShipModuleType.Spacebomb
                                   || ModuleType == ShipModuleType.Turret
                                   || ModuleType == ShipModuleType.MainGun
                                   || ModuleType == ShipModuleType.MissileLauncher
                                   || ModuleType == ShipModuleType.Drone
                                   || ModuleType == ShipModuleType.Bomb;

        public float ActualCost => Cost * CurrentGame.ProductionPace;

        // the actual hit radius is a bit bigger for some legacy reason
        public float ShieldHitRadius => Flyweight.ShieldRadius + 10f;
        public bool ShieldsAreActive => Active && ShieldPower > 1f;

        public float WeaponRotationSpeed
        {
            get => WeaponRotation.AlmostZero() ? (InstalledWeapon?.IsTurret ?? false) ? 2 : 1 : WeaponRotation;
            set => WeaponRotation = value;
        }

        public SubTexture ModuleTexture => ResourceManager.Texture(IconTexturePath);

        public float ActualPowerStoreMax   => Is(ShipModuleType.FuelCell) ? PowerStoreMax * Bonuses.FuelCellMod : PowerStoreMax;
        public float ActualPowerFlowMax    => PowerFlowMax  * Bonuses.PowerFlowMod;
        public float ActualBonusRepairRate => BonusRepairRate * Bonuses.RepairRateMod;
        public float ActualShieldPowerMax { get; private set; }
        public float ActualMaxHealth       => TemplateMaxHealth * Bonuses.HealthMod;

        // NOTE: The way InternalRestrictions is handled, is controlled by
        //       GlobalStats.CountInternalModulesFromHull
        public bool HasInternalRestrictions => Restrictions == Restrictions.I || Restrictions == Restrictions.IO;

        // FB: This method was created to deal with modules which have secondary functionality. Use this whenever you want to check
        // module types for calculations. Dont use it when you are looking for main functionality as defined in the xml (for instance - ship design screen)
        public bool Is(ShipModuleType type)
        {
            switch (type)
            {
                case ShipModuleType.PowerPlant: return PowerFlowMax >= 1f;
                case ShipModuleType.Shield:     return ShieldPowerMax >= 1f;
                case ShipModuleType.Armor:      return ModuleType == type || APResist > 0;
                case ShipModuleType.Ordnance:   return ModuleType == type && OrdinanceCapacity > 0;
                default:                        return ModuleType == type;
            }
        }

        public bool IsFighterHangar => !IsTroopBay && !IsSupplyBay && ModuleType != ShipModuleType.Transporter;

        public float HealthPercent => Health / ActualMaxHealth;

        // Used to configure how good of a target this module is
        public int ModuleTargetingValue => TargetValue + (Health < ActualMaxHealth ? 1 : 0); // prioritize already damaged modules

        public void InitShieldPower(float shieldAmplify)
        {
            UpdateShieldPowerMax(shieldAmplify);
            ShieldPower = ActualShieldPowerMax;
        }

        // @note This should only be used in Testing
        // @todo Is there a way to limit visibility to unit tests only?
        public void SetHealth(float newHealth, bool fromSave = false)
        {
            float maxHealth = ActualMaxHealth;
            newHealth = newHealth.Clamped(0, maxHealth);
            float healthChange = newHealth - Health;
            Health = newHealth;
            OnFire = (newHealth / maxHealth) < OnFireThreshold;
            Parent.OnHealthChange(healthChange);

            if (!fromSave) // do not trigger Die() or Resurrect() during savegame loading
            {
                if (Active && Health < 1f)
                {
                    Die(LastDamagedBy, false);
                }
                else if (!Active && Health > 1f)
                {
                    ResurrectModule();
                }
            }
        }

        public void UpdateShieldPowerMax(float shieldAmplify)
        {
            // only amplify dedicated shields
            float actualAmplify = ModuleType == ShipModuleType.Shield ? shieldAmplify : 0;
            ActualShieldPowerMax = (ShieldPowerMax + actualAmplify) * Bonuses.ShieldMod;
        }

        public bool IsAmplified => ActualShieldPowerMax > ShieldPowerMax * Bonuses.ShieldMod;

        [Pure] public Ship GetParent() => Parent;

        [Pure] public bool TryGetHangarShip(out Ship ship)
        {
            ship = HangarShip;
            return HangarShip != null;
        }

        public bool IsHangarShipActive => TryGetHangarShip(out Ship ship) && ship.Active;
        public bool TryGetHangarShipActive(out Ship ship) => TryGetHangarShip(out ship) && ship.Active;

        ShipModule(int id) : base(id, GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = ShipModuleFlyweight.Empty;
        }

        ShipModule(ShipModule_XMLTemplate template) : base(0, GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = new ShipModuleFlyweight(template);
            XSize                 = template.XSize;
            YSize                 = template.YSize;
            Mass                  = template.Mass;
            Powered               = template.Powered;
            FieldOfFire           = template.FieldOfFire.ToRadians(); // @note Convert to radians for higher PERF
            NameIndex             = template.NameIndex;
            DescriptionIndex      = template.DescriptionIndex;
            Restrictions          = template.Restrictions;
            ShieldPower           = template.ShieldPower;
            HangarShipUID         = template.HangarShipUID;
            HangarTimer           = template.HangarTimer;
            ModuleType            = template.ModuleType;
            WeaponType            = template.WeaponType;
            IsWeapon              = WeaponType.NotEmpty();
            OrdinanceCapacity     = template.OrdinanceCapacity;
            IconTexturePath       = template.IconTexturePath;
            TargetValue           = template.TargetValue;
            TemplateMaxHealth     = template.HealthMax;

            Radius = (XSize > YSize ? XSize : YSize) * CollisionRadiusMultiplier;

            CanVisualizeDamage = ShipModuleDamageVisualization.CanVisualize(this);

            // initialize `isWeapon` and other InstalledWeapon attributes for module template
            InstallModule(null, null, Point.Zero);

            // @todo This might need to be updated with latest ModuleType logic?
            TargetValue += Is(ShipModuleType.Armor)             ? -1 : 0;
            TargetValue += Is(ShipModuleType.Bomb)              ? 1 : 0;
            TargetValue += Is(ShipModuleType.Command)           ? 1 : 0;
            TargetValue += Is(ShipModuleType.Countermeasure)    ? 1 : 0;
            TargetValue += Is(ShipModuleType.Drone)             ? 1 : 0;
            TargetValue += Is(ShipModuleType.Engine)            ? 2 : 0;
            TargetValue += Is(ShipModuleType.FuelCell)          ? 1 : 0;
            TargetValue += Is(ShipModuleType.Hangar)            ? 1 : 0;
            TargetValue += Is(ShipModuleType.MainGun)           ? 1 : 0;
            TargetValue += Is(ShipModuleType.MissileLauncher)   ? 1 : 0;
            TargetValue += Is(ShipModuleType.Ordnance)          ? 1 : 0;
            TargetValue += Is(ShipModuleType.PowerPlant)        ? 1 : 0;
            TargetValue += Is(ShipModuleType.Sensors)           ? 1 : 0;
            TargetValue += Is(ShipModuleType.Shield)            ? 1 : 0;
            TargetValue += Is(ShipModuleType.Spacebomb)         ? 1 : 0;
            TargetValue += Is(ShipModuleType.Special)           ? 1 : 0;
            TargetValue += Is(ShipModuleType.Turret)            ? 1 : 0;
            TargetValue += Explodes                             ? 2 : 0;
            TargetValue += IsWeapon                             ? 1 : 0;
        }

        public static ShipModule CreateTemplate(ShipModule_XMLTemplate template)
        {
            return new ShipModule(template);
        }

        // Called by Create() and ShipDesignScreen.CreateDesignModule
        // LOYALTY can be null
        public static ShipModule CreateNoParent(UniverseState us, ShipModule template, Empire loyalty, ShipHull hull)
        {
            var module = new ShipModule(us?.CreateId() ?? -1)
            {
                Flyweight         = template.Flyweight,
                Bonuses           = EmpireHullBonuses.Get(loyalty, hull),
                DescriptionIndex  = template.DescriptionIndex,
                FieldOfFire       = template.FieldOfFire,
                HangarShipUID     = template.HangarShipUID,
                HangarTimer       = template.HangarTimer,
                TemplateMaxHealth = template.TemplateMaxHealth,
                ModuleType        = template.ModuleType,
                IsWeapon          = template.IsWeapon,
                WeaponType        = template.WeaponType,
                Mass              = template.Mass,
                NameIndex         = template.NameIndex,
                OrdinanceCapacity = template.OrdinanceCapacity,
                ShieldPower       = template.ShieldPowerMax, // This one is strange -Gretman
                XSize             = template.XSize,
                YSize             = template.YSize,
                IconTexturePath   = template.IconTexturePath,
                Restrictions      = template.Restrictions,
                TargetValue       = template.TargetValue,
                Radius            = template.Radius,
                CanVisualizeDamage = template.CanVisualizeDamage,
            };

            module.Health = module.ActualMaxHealth; // this depends on Empire-Hull-Bonuses
            module.UpdateShieldPowerMax(0); // also depends on bonuses
            return module;
        }

        // this is used during Ship creation
        public static ShipModule Create(UniverseState us, DesignSlot slot, Ship parent, bool isTemplate)
        {
            ShipModule template = ResourceManager.GetModuleTemplate(slot.ModuleUID);
            ShipModule m = CreateNoParent(us, template, parent.Loyalty, parent.BaseHull);

            if (m.ModuleType == ShipModuleType.Hangar && !m.IsTroopBay)
                m.HangarShipUID = slot.HangarShipUID;

            m.SetModuleSizeRotAngle(slot.Size, slot.ModuleRot, slot.TurretAngle);
            m.InstallModule(parent, parent.BaseHull, slot.Pos);

            // don't initialize Shield instance for ShipTemplates
            if (!isTemplate && m.ShieldPowerMax > 0f)
                m.Shield = ShieldManager.AddShield(m, m.Rotation, m.Position);

            return m;
        }

        // this is used during Ship loading from save
        public static ShipModule Create(UniverseState us, ModuleSaveData slot, Ship parent)
        {
            ShipModule m = Create(us, slot as DesignSlot, parent, isTemplate:false);

            m.Active = slot.Health > 0.01f;
            m.ShieldPower = slot.ShieldPower;
            m.SetHealth(slot.Health, fromSave: true);
            m.HangarShipId = slot.HangarShipId;
            return m;
        }

        public static ShipModule CreateDesignModule(UniverseState us, string uid, ModuleOrientation moduleRot, 
                                                    int turretAngle, string hangarShipUID, ShipHull hull)
        {
            ShipModule template = ResourceManager.GetModuleTemplate(uid);
            ShipModule m = CreateNoParent(us, template, EmpireManager.Player, hull);

            // don't set HangarShipUID if this isn't actually a Hangar (because Shipyard sets default to DynamicLaunch)
            if (m.ModuleType == ShipModuleType.Hangar)
                m.HangarShipUID = m.IsTroopBay ? EmpireManager.Player.GetAssaultShuttleName() : hangarShipUID;

            m.SetModuleRotation(m.XSize, m.YSize, moduleRot, turretAngle);
            m.InstallModule(null, hull, Point.Zero);
            return m;
        }

        public void InstallModule(Ship parent, ShipHull hull, Point gridPos)
        {
            Parent = parent;
            Pos = gridPos;

            if (hull != null) // for module templates this will be null
            {
                Point gridCenter = hull.GridCenter;
                LocalCenter = new Vector2((gridPos.X - gridCenter.X)*16f + XSize * 8f,
                                          (gridPos.Y - gridCenter.Y)*16f + YSize * 8f);

                // world position of this module, will be overwritten during Ship's Module update
                Position = LocalCenter;
                if (parent != null)
                    Position += parent.Position;
            }

            if (IsWeaponOrBomb)
            {
                bool bomb = ModuleType == ShipModuleType.Bomb;
                string type = bomb ? BombType : WeaponType;
                if (InstalledWeapon == null || InstalledWeapon.UID != type)
                {
                    UninstallWeapon();
                    InstalledWeapon = ResourceManager.CreateWeapon(type, Parent, this, hull);
                }

                if (bomb)
                {
                    parent?.OnBombInstalled(this);
                }
                else
                {
                    IsWeapon = true;
                    parent?.OnWeaponInstalled(this, InstalledWeapon);
                }
            }

            if (ModuleType == ShipModuleType.Hangar)
            {
                InitHangar();
            }
        }

        // Used in Shipyard when a module gets uninstalled from the design
        public void UninstallModule()
        {
            UninstallWeapon();

            // some of this state needs to be cleared to avoid bugs
            Parent = null;
            IsExternal = false;
            Powered = false;
        }

        void UninstallWeapon()
        {
            if (InstalledWeapon != null)
            {
                if (IsWeapon)
                    Parent?.Weapons.Remove(InstalledWeapon);
                else
                    Parent?.BombBays.Remove(this);
                InstalledWeapon = null;
            }
        }

        void InitHangar()
        {
            // for the non faction AI , all hangars are dynamic. It makes the AI carriers better
            if (Parent == null || Parent.Loyalty.isFaction)
                return;

            DynamicHangar = ShipBuilder.GetDynamicHangarOptions(HangarShipUID);
            if (DynamicHangar == DynamicHangarOptions.Static)
            {
                if (!Parent.Loyalty.isPlayer)
                {
                    DynamicHangar = DynamicHangarOptions.DynamicLaunch; // AI will always get dynamic launch.
                }
                else if (!Parent.Loyalty.CanBuildShip(HangarShipUID))
                {
                    // If Player has deleted a Fighter Ship Design, this design would not have a
                    // valid fighter so we check if we can build it, otherwise we set DynamicLaunch
                    Log.Error($"InitHangar: Cannot build '{HangarShipUID}', reverting to DynamicLaunch");
                    DynamicHangar = DynamicHangarOptions.DynamicLaunch;
                    HangarShipUID = DynamicHangarOptions.DynamicLaunch.ToString();
                }
            }
        }

        public RoleName[] HangarRoles
        {
            get
            {
                var tempRoles = new Array<RoleName>();
                foreach (var roleName in PermittedHangarRoles)
                {
                    tempRoles.Add((RoleName)Enum.Parse(typeof(RoleName), roleName));
                }
                return tempRoles.ToArray();
            }
        }

        public string GetHangarShipName()
        {
            if (ShipBuilder.IsDynamicHangar(HangarShipUID))
                return CarrierBays.GetDynamicShipNameShipDesign(this);
            return HangarShipUID;
        }

        public float BayOrdnanceUsagePerSecond
        {
            get
            {
                float ordnancePerSecond = 0;
                if (ModuleType == ShipModuleType.Hangar && HangarTimerConstant > 0)
                {
                    string hangarShipName = GetHangarShipName();
                    if (ResourceManager.GetShipTemplate(hangarShipName, out Ship template))
                        ordnancePerSecond = (template.ShipOrdLaunchCost) / HangarTimerConstant;
                }

                return ordnancePerSecond;
            }
        }

        public bool IsObsolete()
        {
            return EmpireManager.Player.ObsoletePlayerShipModules.Contains(UID);
        }

        public struct UpdateEveryFrameArgs
        {
            public FixedSimTime TimeStep;
            public float ParentX;
            public float ParentY;
            public float ParentZ;
            public float ParentRotation;
            public float ParentScale;
            public float Cos;
            public float Sin;
            public float Tan;
        }

        // Refactored by RedFox - @note This method is called very heavily, so many parts have been inlined by hand
        public void UpdateEveryFrame(in UpdateEveryFrameArgs a)
        {
            Vector2 offset = LocalCenter;
            float cx = a.ParentX + offset.X * a.Cos - offset.Y * a.Sin;
            float cy = a.ParentY + offset.X * a.Sin + offset.Y * a.Cos;
            Position.X = cx;
            Position.Y = cy;
            ZPos = a.ParentZ - a.Tan * offset.X;
            Rotation = a.ParentRotation; // assume parent rotation is already normalized
        }

        // radius padding for collision detection
        const float CollisionRadiusMultiplier = 11.5f;

        // Collision test with this ShipModule. Returns TRUE if point is inside this module
        // The collision bounds are APPROXIMATED by using radius checks. This means corners
        // are not accurately checked.
        // HitTest uses the World scene POSITION. Not module XML location
        public bool HitTestNoShields(Vector2 worldPos, float radius)
        {
            float r2 = radius + Radius;
            float dx = Position.X - worldPos.X;
            float dy = Position.Y - worldPos.Y;
            if ((dx*dx + dy*dy) > (r2*r2))
                return false; // definitely out of radius for SQUARE and non-square modules

            // we are a Square module? since we're already inside radius, collision happened
            if (XSize == YSize)
                return true;

            Capsule capsule = GetModuleCollisionCapsule();
            return capsule.HitTest(worldPos, radius);
        }

        // Gets the collision capsule in World coordinates
        public Capsule GetModuleCollisionCapsule()
        {
            float shorter = XSize <  YSize ? XSize : YSize; // wonder if .NET can optimize this? wanna bet no? :P
            float longer  = XSize >= YSize ? XSize : YSize;

            // if high module, use forward vector, if wide module, use right vector
            Vector2 longerDir = Rotation.RadiansToDirection();
            if (XSize > YSize) longerDir = longerDir.LeftVector();

            // now for more expensive and accurate capsule-line collision testing
            // since we can have 4x1 modules etc, we construct a capsule
            float smallerOffset = (shorter / longer) * longer * 8.0f;
            float longerOffset = longer * 8.0f - smallerOffset;

            return new Capsule(
                Position - longerDir * longerOffset,
                Position + longerDir * longerOffset,
                shorter * CollisionRadiusMultiplier
            );
        }

        public bool HitTestShield(Vector2 worldPos, float radius)
        {
            float r2 = radius + ShieldHitRadius;
            float dx = Position.X - worldPos.X;
            float dy = Position.Y - worldPos.Y;
            return (dx*dx + dy*dy) <= (r2*r2);
        }

        public bool RayHitTestShield(Vector2 startPos, Vector2 endPos, float rayRadius, out float distanceFromStart)
        {
            return Position.RayCircleIntersect(rayRadius + ShieldHitRadius, startPos, endPos, out distanceFromStart);
        }

        public bool RayHitTest(Vector2 startPos, Vector2 endPos, float rayRadius, out float distanceFromStart)
        {
            if (ShieldsAreActive)
            {
                return Position.RayCircleIntersect(rayRadius + ShieldHitRadius, startPos, endPos, out distanceFromStart);
            }

            distanceFromStart = Position.FindClosestPointOnLine(startPos, endPos).Distance(startPos);
            return distanceFromStart > 0f;
        }

        public static float DamageFalloff(Vector2 explosionCenter, Vector2 affectedPoint, float damageRadius, float moduleRadius)
        {
            float explodeDist = Math.Max(0f, explosionCenter.Distance(affectedPoint) - moduleRadius);
            float relativeDist = explodeDist / damageRadius;
            float falloff = (1f - relativeDist).Clamped(0f, 1f);
            return falloff * falloff; // (1-x)^2 where x=[0:1]
        }

        public void DebugDamageCircle()
        {
            Parent.Universe?.DebugWin?.DrawGameObject(DebugModes.Targeting, this, Parent.Universe.Screen);
        }

        public float GetExplosionDamageOnShipExplode()
        {
            if (!Active)
                return 0f;

            float resist = ExplosiveResist > 0f ? Health / (1f - ExplosiveResist) : 0f;
            float dmg = Explodes ? ExplosionDamage : 0f;
            return dmg - resist; // Allow negative damage (damage reducer)
        }

        public void DamageShield(float damageAmount, Projectile proj, out float remainder)
        {
            remainder = 0;
            if (damageAmount < 0.01f)
                return;
            
            remainder   = (damageAmount - ShieldPower).LowerBound(0);
            ShieldPower = (ShieldPower - damageAmount).LowerBound(0);

            if (proj != null && Parent.IsVisibleToPlayer)
                Shield.HitShield(Parent.Universe.Screen, this, proj);

            Parent.UpdateShields();
        }

        // return TRUE if all damage was absorbed (damageInOut is less or equal to 0)
        public bool DamageExplosive(GameplayObject source, Vector2 worldHitPos, float damageRadius, ref float damageInOut)
        {
            float damage = GetExplosiveDamage(worldHitPos, damageRadius, damageInOut);
            if (damage <= 0.1f)
                return true;

            //Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager, Center, Radius, 1.5f);

            Damage(source, damageInOut, out damageInOut);
            return damageInOut <= 0f;
        }

        public void DamageExplosive(GameplayObject source, Vector2 worldHitPos, float damageRadius, float damageAmount)
        {
            float damage = GetExplosiveDamage(worldHitPos, damageRadius, damageAmount);
            if (damage > 0.1f)
            {
                //Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager, Center, Radius, 1.5f);
                Damage(source, damage, out _);
            }
        }

        float GetExplosiveDamage(Vector2 worldHitPos, float damageRadius, float damageAmount)
        {
            float moduleRadius = ShieldsAreActive ? ShieldHitRadius : Radius;
            return damageAmount * DamageFalloff(worldHitPos, Position, damageRadius, moduleRadius);
        }

        void EvtDamageInflicted(GameplayObject source, float amount)
        {
            if      (source is Ship s)       source = s;
            else if (source is Projectile p) source = p.Owner ?? p.Module?.Parent;
            source?.OnDamageInflicted(this, amount);
        }

        public void Damage(GameplayObject source, float damageAmount, out float damageRemainder)
        {
            float damageModifier = 1f;
            if (source != null)
            {
                damageModifier = ShieldsAreActive
                    ? source.DamageMod.GetShieldDamageMod(this)
                    : GetGlobalArmourBonus() * source.DamageMod.GetArmorDamageMod(this);
            }

            float modifiedDamage = damageAmount * damageModifier;
            if (!TryDamageModule(source, modifiedDamage, out float grossRemainder))
            {
                damageRemainder = 0f;
                if (source != null)
                {
                    EvtDamageInflicted(source, 0f);
                }
                Deflect(source); // FB: the projectile was deflected
                return; 
            }

            DebugDamageCircle();

            float absorbedDamage = modifiedDamage - grossRemainder.LowerBound(0);
            if (damageModifier <= 1) // below 1, resistance. above 1, vulnerability.
                absorbedDamage /= damageModifier; // module absorbed more damage because of good resistance

            if (source != null)
                EvtDamageInflicted(source, absorbedDamage);

            damageRemainder = (int)(damageAmount - absorbedDamage);
        }

        void Deflect(GameplayObject source)
        {
            if (!Parent.InFrustum || Parent.Universe.Screen.IsShipViewOrCloser == false)
                return;

            if (!(source is Projectile proj))
                return;

            if (proj.Explodes || proj.Duration < 0)
                return;

            proj.Deflect(Parent.Loyalty, Center3D);
        }

        public void DebugDamage(float percent)
        {
            float health = Health * percent + ShieldPower;
            float damage = health.Clamped(0, Health + ShieldPower);
            Ship source = GetParent();
            Damage(source, damage);
        }

        public void DamageByRecoveredFromCrash(float modifier)
        {
            float percent;
            switch (Restrictions)
            {
                case Restrictions.E:
                case Restrictions.OE:
                case Restrictions.O:
                case Restrictions.xO: percent = 1; break;
                default:              percent = 0.95f; break; // contains I
            }

            if (Is(ShipModuleType.Engine))
                percent = RandomMath.RollDice(20) ? 0.75f : 1;

            if (Is(ShipModuleType.Command)
                || Is(ShipModuleType.PowerPlant)
                || Is(ShipModuleType.Command)
                || Explodes)
            {
                percent = 0.95f;
            }

            Ship source  = GetParent();
            if (ActualShieldPowerMax > 0)
                Damage(source, ActualShieldPowerMax); // Kill shield power first

            Damage(source, Health * percent * modifier);
        }

        public override void Damage(GameplayObject source, float damageAmount)
            => Damage(source, damageAmount, out float _);


        // Note - this assumes that projectile effect of ignore shield was taken into account. 
        bool TryDamageModule(GameplayObject source, float modifiedDamage, out float remainder)
        {
            remainder = modifiedDamage;
            if (source != null)
                Parent.LastDamagedBy = LastDamagedBy = source;

            Parent.ShieldRechargeTimer = 0f;

            var beam = source as Beam;
            var proj = source as Projectile;

            bool damagingShields = ShieldsAreActive;
            if (beam == null) // only for projectiles
            {
                float damageThreshold = damagingShields ? ShieldDeflection : Deflection;
                if (proj?.Weapon.EMPDamage > damageThreshold && !damagingShields)
                    CauseEmpDamage(proj); // EMP damage can be applied if not hitting shields

                if (modifiedDamage < damageThreshold && proj?.WeaponType != "Plasma")
                    return false; // no damage could be done, the projectile was deflected.
            }

            if (damagingShields)
            {
                DamageShield(modifiedDamage, proj, out remainder);
                CauseSpecialBeamDamageToShield(beam);
                //Log.Info($"{Parent.Name} shields '{UID}' dmg {modifiedDamage} shld {ShieldPower} by {proj?.WeaponType}");
            }
            else
            {
                CauseSpecialBeamDamageNoShield(beam);
                float healthBefore = Health;
                SetHealth(Health - modifiedDamage);
                remainder = modifiedDamage - healthBefore;
                DebugPerseveranceNoDamage();

                //Log.Info($"{Parent.Name} module '{UID}' dmg {modifiedDamage}  hp  {Health} by {proj?.WeaponType}");
            }

            if (Parent.IsVisibleToPlayer)
            {
                if      (beam != null)            beam.CreateBeamHitParticles(ZPos, damagingShields);
                else if (proj?.Explodes == false) proj.CreateHitParticles(Center3D, damagingShields);

                CreateHitDebris(proj);
            }

            return true;
        }

        void CreateHitDebris(Projectile proj)
        {
            if (proj != null && (proj.Weapon.Tag_Kinetic || proj.Weapon.Explodes))
            {
                if (RandomMath.RollDice(20)) // X % out of 100 that we spawn debris
                {
                    Vector2 velocity = Parent.Velocity + RandomMath.Vector2D(Parent.Velocity.Length()) * (1 + RandomMath.RollDie(200) / 100);
                    SpawnDebris(velocity, 1, ignite: false);
                }
            }
        }

        // TODO: this should be part of `Bonuses`
        float GetGlobalArmourBonus()
        {
            if (GlobalStats.ActiveModInfo?.UseHullBonuses == true &&
                ResourceManager.HullBonuses.TryGetValue(Parent.ShipData.Hull, out HullBonus mod))
                return (1f - mod.ArmoredBonus);

            return 1f;
        }

        void CauseEmpDamage(Projectile proj)
        {
            if (proj.Weapon.EMPDamage > 0f)
                Parent.CauseEmpDamage(proj.Weapon.EMPDamage);
        }

        void CauseSpecialBeamDamageToShield(Beam beam)
        {
            if (beam != null)
            {
                CauseSiphonDamage(beam);
                BeamTractorDamage(beam, hittingShields: true);
            }
        }

        void CauseSpecialBeamDamageNoShield(Beam beam)
        {
            if (beam != null)
            {
                BeamPowerDamage(beam);
                BeamTroopDamage(beam);
                BeamTractorDamage(beam, hittingShields: false);
                BeamRepulsionDamage(beam);
            }
        }

        void BeamPowerDamage(Beam beam)
        {
            if (beam.Weapon.PowerDamage > 0)
                Parent.CausePowerDamage(beam.Weapon.PowerDamage);
        }

        void BeamTroopDamage(Beam beam)
        {
            if (beam.Weapon.TroopDamageChance > 0f)
                Parent.CauseTroopDamage(beam.Weapon.TroopDamageChance);
        }

        void BeamTractorDamage(Beam beam, bool hittingShields)
        {
            if (beam.Weapon.TractorDamage > 0f)
                Parent.CauseTractorDamage(beam.Weapon.TractorDamage, hittingShields);
        }

        void BeamRepulsionDamage(Beam beam)
        {
            if (beam.Weapon.RepulsionDamage > 0)
                Parent.CauseRepulsionDamage(beam);
        }

        void DebugPerseveranceNoDamage()
        {
        #if DEBUG
            if (!Parent.Universe.Debug || Parent.VanityName != "Perseverance")
                return;
            if (Health < 10) // never give up, never surrender! F_F
                SetHealth(10);
        #endif
        }

        void CauseSiphonDamage(Beam beam)
        {
            if (beam.Weapon.SiphonDamage > 0f)
            {
                ShieldPower = (ShieldPower - beam.Weapon.SiphonDamage).LowerBound(0);
                beam.Owner?.AddPower(beam.Weapon.SiphonDamage);
                Parent.UpdateShields();
            }
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++DebugInfoScreen.ModulesDied;
            ShieldPower = 0f;

            if (Active && Parent.IsVisibleToPlayer)
            {
                var center = new Vector3(Position.X, Position.Y, -100f);
                bool parentAlive = !Parent.Dying;

                var p = Parent.Universe.Screen.Particles;
                if (p != null) // can be null in unit tests
                {
                    for (int i = 0; i < 30; ++i)
                    {
                        Vector3 pos = parentAlive ? center : new Vector3(Parent.Position, UniverseRandom.RandomBetween(-25f, 25f));
                        p.Explosion.AddParticle(pos);
                    }
                }

                SpawnDebris(Parent.Velocity, 0, ignite: true);
            }

            Active = false;
            SetHealth(0f);
            Parent.OnModuleDeath(this);

            if (!cleanupOnly && source != null)
            {
                if (Parent.Active && Parent.IsVisibleToPlayer)
                    GameAudio.PlaySfxAsync("sd_explosion_module_small", Parent.SoundEmitter);

                if (Explodes)
                {
                    Parent.Universe.Spatial.ExplodeAtModule(source, this,
                        ignoresShields: true, damageAmount: ExplosionDamage, damageRadius: ExplosionRadius);
                }
            }
        }

        void ResurrectModule()
        {
            Active = true;
            Parent.OnModuleResurrect(this);
        }

        void SpawnDebris(Vector2 velocity, int count, bool ignite)
        {
            if (count == 0) 
                count = RandomMath.IntBetween(0, (int)(Area*0.5f + 1f));

            if (count != 0)
            {
                SpaceJunk.SpawnJunk(Parent.Universe, count, Position, velocity, this,
                                    maxSize:Math.Max(Radius, 32), ignite:ignite);
            }
        }

        //added by gremlin boarding parties
        public bool LaunchBoardingParty(Troop troop, out Ship ship)
        {
            ship = null;
            if (!IsTroopBay || !Powered)
                return false;

            if (HangarShip != null) // this bay's ship is already out
            {
                if (HangarShip.AI.State == AIState.ReturnToHangar
                    || HangarShip.AI.State == AIState.AssaultPlanet
                    || HangarShip.AI.EscortTarget != null
                    || HangarShip.AI.OrbitTarget != null)
                {
                    return false;
                }
                HangarShip.DoEscort(Parent);
                ship = HangarShip;
                return true;
            }

            if (HangarTimer <= 0f && HangarShip == null) // launch the troopship
            {
                string assaultShuttle = Parent.Loyalty.GetAssaultShuttleName();
                ship = Ship.CreateTroopShipAtPoint(Parent.Universe, assaultShuttle, Parent.Loyalty, Position, troop);
                SetHangarShip(ship);
                HangarShip.Mothership = Parent;
                HangarShip.DoEscort(Parent);
                HangarShip.Velocity = Parent.Velocity;
                HangarTimer = HangarTimerConstant;
                // transfer our troop onto the shuttle we just spawned
                troop.LandOnShip(HangarShip);

                Parent.OnShipLaunched(HangarShip);
                return true;
            }

            return false;
        }

        public void ScrambleFighters()
        {
            if (IsTroopBay || IsSupplyBay || !Powered)
                return;

            var fighter = HangarShip;
            var carrier = Parent;
            if (fighter != null && fighter.Active)
            {
                if (fighter.AI.HasPriorityTarget
                    || fighter.AI.IgnoreCombat
                    || fighter.AI.Target != null
                    || (fighter.Position.InRadius(carrier.Position, Parent.SensorRange) && fighter.AI.State != AIState.ReturnToHangar))
                {
                    return;
                }

                fighter.DoEscort(Parent);
                return;
            }

            if (HangarTimer <= 0f && (fighter == null || !fighter.Active))
            {
                SetHangarShip(Ship.CreateShipFromHangar(Parent.Universe, this, carrier.Loyalty, carrier.Position + LocalCenter, carrier));

                if (HangarShip == null)
                {
                    Log.Warning($"Could not create ship from hangar, UID = {HangarShipUID}");
                    return;
                }

                HangarShip.DoEscort(Parent);
                HangarShip.Velocity = carrier.Velocity + UniverseRandom.RandomDirection() * HangarShip.SpeedLimit;
                HangarShip.Mothership = carrier;
                HangarTimer = HangarTimerConstant;

                carrier.ChangeOrdnance(-HangarShip.ShipOrdLaunchCost);
                carrier.OnShipLaunched(HangarShip);
            }
        }

        public void ResetHangarTimer()
        {
            if (HangarTimerConstant > 0f)
                HangarTimer = HangarTimerConstant;
        }

        public void SetHangarShip(Ship ship)
        {
            HangarShip = ship;
            HangarShipId = ship?.Id ?? 0;
        }

        public void ResetHangarShip(Ship newShipToLink)
        {
            SetHangarShip(newShipToLink);
            newShipToLink.Mothership = Parent;
        }

        public void ResetHangarShipWithReturnToHangar(Ship newShipToLink)
        {
            SetHangarShip(newShipToLink);
            newShipToLink.Mothership = Parent;
            newShipToLink.AI.OrderReturnToHangar();
        }

        public override void Update(FixedSimTime timeStep)
        {
            if (Active && Health < 1f)
            {
                Die(LastDamagedBy, false);
            }
            else if (!Active && Health > 1f)
            {
                ResurrectModule();
            }

            if (Active && ModuleType == ShipModuleType.Hangar)
                HangarTimer -= timeStep.FixedTime;

            // Shield Recharge / Discharge
            if (Is(ShipModuleType.Shield))
            {
                float shieldMax = ActualShieldPowerMax;
                ShieldPower = RechargeShields(ShieldPower, shieldMax, timeStep); // use regular recharge
            }

            if (TransporterTimer > 0)
                TransporterTimer -= timeStep.FixedTime;
        }

        float RechargeShields(float shieldPower, float shieldMax, FixedSimTime timeStep)
        {
            if (!Active || !Powered || shieldPower >= ActualShieldPowerMax)
                return shieldPower;

            if (Parent.ShieldRechargeTimer > ShieldRechargeDelay)
                shieldPower += ShieldRechargeRate * timeStep.FixedTime;
            else 
                shieldPower += ShieldRechargeCombatRate * timeStep.FixedTime;
            return shieldPower.Clamped(0, shieldMax);
        }

        public float GetActualMass(Empire loyalty, float ordnancePercent, bool useMassModifier = true)
        {
            float mass = Mass;
            if (Is(ShipModuleType.Armor))
                mass *= loyalty.data.ArmourMassModifier;

            if (Is(ShipModuleType.Ordnance))
                mass = (mass * ordnancePercent).LowerBound(mass * 0.1f);

            // regular positive mass modules
            float positiveMass = mass * (useMassModifier ? loyalty.data.MassModifier : 1);
            if (mass >= 0f)
                return positiveMass;

            // only allow negative mass modules (mass reduction devices)
            // if we're powered, otherwise return their absolute mass
            return Powered ? mass: Math.Abs(positiveMass);
        }

        // @note This is called every frame for every module for every ship in the universe
        public void UpdateDamageVisualization(FixedSimTime timeStep, float scale, bool visible)
        {
            if (visible && OnFire)
            {
                var p = Parent.Universe.Screen.Particles;
                if (p != null)
                {
                    var vis = DamageVisualizer;
                    if (vis == null)
                    {

                            DamageVisualizer = vis = new ShipModuleDamageVisualization(this, p);
                    }
                    vis.Update(timeStep, Center3D, scale, Active);
                }
            }
            else // destroy immediately when out of vision range or if module is no longer OnFire
            {
                DamageVisualizer = null;
            }
        }

        public void UpdateWhileDying(FixedSimTime timeStep, float scale, bool visible)
        {
            if (CanVisualizeDamage)
            {
                if (visible)
                {
                    UpdateDamageVisualization(timeStep, scale, visible: true);
                }
                else
                {
                    DamageVisualizer = null;
                }
            }
        }

        public float Repair(float repairAmount)
        {
            if (Health >= ActualMaxHealth || repairAmount <= 0)
                return repairAmount;

            float neededRepair    = ActualMaxHealth - Health;
            float maxRepairAmount = ActualMaxHealth.Clamped(0, ActualMaxHealth * (0.03f / (1 + RepairDifficulty)));
            float actualRepair    = maxRepairAmount.Clamped(0, repairAmount);
            actualRepair          = actualRepair.Clamped(0, neededRepair);
            float repairLeft      = (repairAmount - actualRepair).Clamped(0, repairAmount);
            SetHealth(Health + actualRepair);
            VisualizeRepair();

            return repairLeft;
        }

        public void RegenerateSelf()
        {
            if (Regenerate > 0 && HealthPercent < 0.99f)
            {
                if (!Active)
                {
                    // Module is destroyed and might "jump start" its regeneration
                    if (RandomMath.RollDice(TechLevel))
                        SetHealth(Health + Regenerate);
                }
                else
                {
                    // If the module is not powered and needs power, the regeneration is 10%
                    float regeneration = !Powered && PowerDraw > 0f ? Regenerate * 0.1f : Regenerate;
                    SetHealth(Health + regeneration);
                }
            }
        }

        public void VisualizeRepair()
        {
            if (Parent.IsVisibleToPlayer)
            {
                var p = Parent.Universe.Screen.Particles;
                if (p != null) // null in unit tests
                {
                    Vector3 repairEffectOrigin = Position.ToVec3(ZPos - 50f); // -Z is up towards the camera
                    for (int i = 0; i < 2; i++)
                        p.BlueSparks.AddParticle(repairEffectOrigin);
                }
            }
        }

        // Used for picking best repair candidate based on main  module type (disregard secondary module functions)
        public int ModulePriority
        {
            get
            {
                switch (ModuleType)
                {
                    case ShipModuleType.Command:      return 0;
                    case ShipModuleType.PowerConduit: return 1;
                    case ShipModuleType.PowerPlant:   return 2;
                    case ShipModuleType.Engine:       return 3;
                    case ShipModuleType.Shield:       return 4;
                    default:                          return 5;
                    case ShipModuleType.Armor:        return 6;
                }
            }
        }

        public Color GetHealthStatusColor()
        {
            float healthPercent = HealthPercent;

            if (Parent.Universe.Debug && IsExternal)
            {
                if (healthPercent >= 0.5f) return Color.Blue;
                if (healthPercent >  0.0f) return Color.DarkSlateBlue;
                return Color.DarkSlateGray;
            }

            if (healthPercent >= 0.90f) return Color.Green;
            if (healthPercent >= 0.65f) return Color.GreenYellow;
            if (healthPercent >= 0.45f) return Color.Yellow;
            if (healthPercent >= 0.15f) return Color.OrangeRed;
            if (healthPercent >  0.00f) return Color.Red;
            return Color.Black;
        }

        // @todo Find a way to get rid of this duplication ?
        public Color GetHealthStatusColorWhite()
        {
            float healthPercent = HealthPercent;

            if (healthPercent >= 0.90f) return Color.White;
            if (healthPercent >= 0.65f) return Color.GreenYellow;
            if (healthPercent >= 0.45f) return Color.Yellow;
            if (healthPercent >= 0.15f) return Color.OrangeRed;
            if (healthPercent >  0.00f) return Color.Red;
            return Color.Black;
        }

        public float CalculateModuleOffenseDefense(int slotCount)
        {
            return CalculateModuleDefense(slotCount) + CalculateModuleOffense();
        }

        public float CalculateModuleDefense(int slotCount)
        {
            if (slotCount <= 0)
                return 0f;

            float def = 0f;

            def += ActualMaxHealth / 200;

            // FB: Added Shield related calcs
            float shieldsMax = ActualShieldPowerMax;
            if (shieldsMax > 0)
            {
                float shieldDef      = shieldsMax / 50;
                float shieldCoverage = ((ShieldRadius + 8f) * (ShieldRadius + 8f) * 3.14159f) / 256f / slotCount;

                shieldDef *= shieldCoverage < 1 ? shieldCoverage : 1f;
                shieldDef *= 1 + ShieldKineticResist / 5;
                shieldDef *= 1 + ShieldEnergyResist / 5;
                shieldDef *= 1 + ShieldBeamResist / 5;
                shieldDef *= 1 + ShieldMissileResist / 5;
                shieldDef *= 1 + ShieldExplosiveResist / 5;

                shieldDef *= ShieldRechargeRate > 0 ? ShieldRechargeRate / (shieldsMax / 100) : 1f;
                shieldDef *= ShieldRechargeDelay > 0 ? 1f / ShieldRechargeDelay : 1f;
                shieldDef *= ShieldRechargeCombatRate > 0 ? 1 + ShieldRechargeCombatRate / (shieldsMax / 25) : 1f;

                shieldDef *= 1 + ShieldDeflection / 50f; // FB: Shield Threshold is much more effective than Damage threshold as it apples to the shield bubble.
                def       += shieldDef;
            }

            // FB: all resists are divided by 5 since there are 5-6 weapon types
            def *= 1 + BeamResist / 5;
            def *= 1 + KineticResist / 5 ;
            def *= 1 + EnergyResist / 5;
            def *= 1 + MissileResist / 5;
            def *= 1 + PlasmaResist / 5;
            def *= 1 + ExplosiveResist / 5;
            def *= 1 + TorpedoResist / 5;

            def *= 1 + Deflection / 100f;
            def *= ModuleType == ShipModuleType.Armor ? 1 + APResist / 2 : 1f;

            def += ECM;
            def *= 1 + EMPProtection / 500;

            // Engines
            def += (TurnThrust + WarpThrust + Thrust) / 15000f;

            def += ActualPowerFlowMax / 50;
            def += TroopCapacity * 50;
            def += BonusRepairRate / 2f;
            def += AmplifyShields / 20f;
            def += Regenerate / (10 * RepairDifficulty).LowerBound(0.1f);

            return def;
        }

        public float CalculateModuleOffense()
        {
            float off = InstalledWeapon?.CalculateOffense(this) ?? 0f;

            off += IsTroopBay ? 50 : 0;
            if (ModuleType != ShipModuleType.Hangar || HangarShipUID.IsEmpty()
                                                    || HangarShipUID == "NotApplicable"
                                                    || IsSupplyBay
                                                    || IsTroopBay)
                return off;

            if (ShipBuilder.IsDynamicHangar(HangarShipUID))
            {
                off += MaximumHangarShipSize * 100 * PermittedHangarRoles.Length / HangarTimerConstant.LowerBound(1);
            }
            else
            {
                if (ResourceManager.GetShipTemplate(HangarShipUID, out Ship hShip))
                    off += (hShip.BaseStrength > 0f) ? hShip.BaseStrength : hShip.CalculateShipStrength();
                else
                    off += 100f;
            }

            return off;
        }

        public static int DefaultFacingFor(ModuleOrientation orientation)
        {
            switch (orientation)
            {
                default:
                case ModuleOrientation.Normal: return 0;
                case ModuleOrientation.Left:   return 270;
                case ModuleOrientation.Right:  return 90;
                case ModuleOrientation.Rear:   return 180;
            }
        }

        // NEW: `orientedSize` comes from DesignSlot data and is already correct
        //      `ModuleOrientation` is only for visual rotation of the module texture
        //      `turretAngle` is now only the turret's default arc direction
        void SetModuleSizeRotAngle(Point orientedSize, ModuleOrientation moduleRot, int turretAngle)
        {
            XSize = orientedSize.X;
            YSize = orientedSize.Y;
            TurretAngle = turretAngle;
            ModuleRot = moduleRot;
        }

        public void SetModuleRotation(int w, int h, ModuleOrientation moduleRot, int turretAngle)
        {
            TurretAngle = turretAngle;
            ModuleRot = moduleRot;
            switch (moduleRot)
            {
                case ModuleOrientation.Normal:
                case ModuleOrientation.Rear:
                    XSize = w;
                    YSize = h;
                    break;
                case ModuleOrientation.Left:
                case ModuleOrientation.Right:
                    XSize = h; // if the module is facing left or right, then length is now height
                    YSize = w;
                    break;
            }
        }

        // Assuming this is a TEMPLATE, returns the oriented size
        public Point GetOrientedSize(ModuleOrientation orientation)
        {
            if (orientation == ModuleOrientation.Left || orientation == ModuleOrientation.Right)
                return new Point(YSize, XSize);
            return new Point(XSize, YSize);
        }

        public Point GetOrientedSize(string slotOrientation)
        {
            if (Enum.TryParse(slotOrientation, out ModuleOrientation orientation))
                return GetOrientedSize(orientation);
            return new Point(XSize, YSize);
        }

        // For specific cases were non squared icons requires a different texture when oriented,
        // for example 1x2 light thrusters
        public bool GetOrientedModuleTexture(ref SubTexture tex, ModuleOrientation orientation)
        {
            if (DisableRotation)
                return false;

            string defaultTex = IconTexturePath;
            SubTexture t;
            switch (orientation)
            {
                default: return false;
                case ModuleOrientation.Left:  t = ResourceManager.TextureOrNull($"{defaultTex}_270"); break;
                case ModuleOrientation.Right: t = ResourceManager.TextureOrNull($"{defaultTex}_90");  break;
                case ModuleOrientation.Rear:  t = ResourceManager.TextureOrNull($"{defaultTex}_180"); break;
            }

            if (t != null)
            {
                tex = t;
                return true;
            }
            return false;
        }

        public override Vector2 JammingError()
        {
            return Parent?.JammingError() ?? Vector2.Zero;
        }

        public bool FighterOut
        {
            get
            {
                if (IsTroopBay || IsSupplyBay)
                    return false;

                return HangarShip?.Active == true;
            }
        }

        public override string ToString()
        {
            return $"{UID}  {Id}  {XSize}x{YSize} {Restrictions} grid={Pos.X},{Pos.Y} local={LocalCenter.X};{LocalCenter.Y} hp={Health} world={Position} ship={Parent?.Name}";
        }

        public void Dispose()
        {
            // TODO: nulling the parent will cause a big can of worms -_-, we get a lot of null ref exceptions
            //Parent = null;
            Flyweight = null;

            DamageVisualizer = null;
            Bonuses = null;
            Shield = null;
            HangarShip = null;
            InstalledWeapon?.Dispose(ref InstalledWeapon);
            LastDamagedBy = null;
            SetSystem(null);
        }
    }
}