using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using System;
using System.Diagnostics.Contracts;

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
        public float FacingRadians;
        public float FacingDegrees
        {
            get => FacingRadians.ToDegrees();
            set => FacingRadians = value.ToRadians();
        }
        public bool CheckedConduits;
        public bool Powered;
        public bool isExternal;
        public int XSIZE = 1;
        public int YSIZE = 1;
        public Vector2 XMLPosition; // module slot location in the ship design; the coordinate system axis is {256,256}
        bool CanVisualizeDamage;
        public float ShieldPower { get; private set; }
        public short OrdinanceCapacity;
        bool OnFire;
        Vector3 Center3D;
        public Vector3 GetCenter3D => Center3D;
        const float OnFireThreshold = 0.15f;
        ShipModuleDamageVisualization DamageVisualizer;
        public EmpireShipBonuses Bonuses = EmpireShipBonuses.Default;
        Ship Parent;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public LocalizedText NameLocalized => LocalizedText.Parse($"{{{NameIndex}}}");
        public Restrictions Restrictions;
        public Shield Shield { get; private set; }
        public string hangarShipUID;
        Ship hangarShip;
        public Guid HangarShipGuid;
        public float hangarTimer;
        public bool isWeapon;
        public Weapon InstalledWeapon;

        public DynamicHangarOptions DynamicHangar { get; private set; }

        public ShipModuleType ModuleType;
        public string IconTexturePath;

        public string UID => Flyweight.UID;
        
        /// Field of fire arc, now in Radians.
        /// Conversion to Radians is done during loading
        /// Game files still use Degrees
        public float FieldOfFire;
        public int TargetValue;
        public float TransporterTimer;
        public const int MaxPriority =6;

        //This wall of text is the 'get' functions for all of the variables that got moved to the 'Flyweight' object.
        //This will allow us to still use the normal "Module.IsCommandModule" even though 'IsCommandModule' actually
        //lives in "Module.Flyweight.IsCommandModule" now.    -Gretman
        public float FTLSpeed                    => Flyweight.FTLSpeed;
        public string DeployBuildingOnColonize   => Flyweight.DeployBuildingOnColonize;
        public string ResourceStored             => Flyweight.ResourceStored;
        public float ResourceStorageAmount       => Flyweight.ResourceStorageAmount;
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
        public bool explodes                     => Flyweight.explodes;
        public float SensorRange                 => Flyweight.SensorRange;
        public float MechanicalBoardingDefense   => Flyweight.MechanicalBoardingDefense;
        public float EMP_Protection              => Flyweight.EMP_Protection;
        public int PowerRadius                   => Flyweight.PowerRadius;
        public int TechLevel                     => Flyweight.TechLevel;
        public float OrdnanceAddedPerSecond      => Flyweight.OrdnanceAddedPerSecond;
        public string BombType                   => Flyweight.BombType;
        public float WarpMassCapacity            => Flyweight.WarpMassCapacity;
        public float BonusRepairRate             => Flyweight.BonusRepairRate;
        public float Cargo_Capacity              => Flyweight.Cargo_Capacity;
        public float shield_radius               => Flyweight.shield_radius;
        public float shield_power_max            => Flyweight.shield_power_max;
        public float shield_recharge_rate        => Flyweight.shield_recharge_rate;
        public float shield_recharge_combat_rate => Flyweight.shield_recharge_combat_rate;
        public float shield_recharge_delay       => Flyweight.shield_recharge_delay;
        public float shield_threshold            => Flyweight.shield_threshold;
        public float shield_kinetic_resist       => Flyweight.shield_kinetic_resist;
        public float shield_energy_resist        => Flyweight.shield_energy_resist;
        public float shield_explosive_resist     => Flyweight.shield_explosive_resist;
        public float shield_missile_resist       => Flyweight.shield_missile_resist;
        public float shield_flak_resist          => Flyweight.shield_flak_resist;
        public float shield_hybrid_resist        => Flyweight.shield_hybrid_resist;
        public float shield_railgun_resist       => Flyweight.shield_railgun_resist;
        public float shield_subspace_resist      => Flyweight.shield_subspace_resist;
        public float shield_warp_resist          => Flyweight.shield_warp_resist;
        public float shield_beam_resist          => Flyweight.shield_beam_resist;
        public float numberOfColonists           => Flyweight.numberOfColonists; // In Millions!
        public float numberOfEquipment           => Flyweight.numberOfEquipment;
        public float numberOfFood                => Flyweight.numberOfFood;
        public bool IsSupplyBay                  => Flyweight.IsSupplyBay;
        public bool IsTroopBay                   => Flyweight.IsTroopBay;
        public float hangarTimerConstant         => Flyweight.hangarTimerConstant;
        public float thrust                      => Flyweight.thrust;
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
        public float GuidedResist                => Flyweight.GuidedResist;
        public float MissileResist               => Flyweight.MissileResist;
        public float HybridResist                => Flyweight.HybridResist;
        public float BeamResist                  => Flyweight.BeamResist;
        public float ExplosiveResist             => Flyweight.ExplosiveResist;
        public float InterceptResist             => Flyweight.InterceptResist;
        public float RailgunResist               => Flyweight.RailgunResist;
        public float SpaceBombResist             => Flyweight.SpaceBombResist;
        public float BombResist                  => Flyweight.BombResist;
        public float BioWeaponResist             => Flyweight.BioWeaponResist;
        public float DroneResist                 => Flyweight.DroneResist;
        public float WarpResist                  => Flyweight.WarpResist;
        public float TorpedoResist               => Flyweight.TorpedoResist;
        public float CannonResist                => Flyweight.CannonResist;
        public float SubspaceResist              => Flyweight.SubspaceResist;
        public float PDResist                    => Flyweight.PDResist;
        public float FlakResist                  => Flyweight.FlakResist;
        public float DamageThreshold             => Flyweight.DamageThreshold;
        public int APResist                      => Flyweight.APResist;
        public bool AlwaysPowered                => Flyweight.IndirectPower;
        public bool isPowerArmour                => Flyweight.isPowerArmour;
        public bool isBulkhead                   => Flyweight.isBulkhead;
        public int TargetTracking                => Flyweight.TargetTracking;
        public int TargetingAccuracy             => Flyweight.TargetAccuracy;
        public int ExplosionDamage               => Flyweight.ExplosionDamage;
        public int ExplosionRadius               => Flyweight.ExplosionRadius;
        public float RepairDifficulty            => Flyweight.RepairDifficulty;
        public string ShieldBubbleColor          => Flyweight.ShieldBubbleColor;
        public float Regenerate                  => Flyweight.Regenerate; // Self regenerating modules
        public bool IsRotatable                  => Flyweight.IsRotable;
        public float AmplifyShields              => Flyweight.AmplifyShields;
        public bool IsWeapon    => ModuleType == ShipModuleType.Spacebomb
                                || ModuleType == ShipModuleType.Turret
                                || ModuleType == ShipModuleType.MainGun
                                || ModuleType == ShipModuleType.MissileLauncher
                                || ModuleType == ShipModuleType.Drone
                                || ModuleType == ShipModuleType.Bomb;

        public Vector2 LocalCenter;// => new Vector2(Position.X + XSIZE * 8f, Position.Y + XSIZE * 8f);
        public int Area => XSIZE * YSIZE;

        public float ActualCost => Cost * CurrentGame.Pace;

        // the actual hit radius is a bit bigger for some legacy reason
        public float ShieldHitRadius => Flyweight.shield_radius + 10f;
        public bool ShieldsAreActive => Active && ShieldPower > 1f;

        /// <summary>
        /// This is an override of default weapon accuracy. <see cref="Weapon.BaseTargetError(int)"/>
        /// it is uniform to all weapons. 50% accuracy creates the same base error for all weapons. 
        /// an accuracy percent of 1 removes all target error.
        /// the default of -1 means ignore this value
        /// </summary>
        public float AccuracyPercent = -1;

        float WeaponRotation;
        public float WeaponRotationSpeed
        {
            get => WeaponRotation.AlmostZero() ? (InstalledWeapon?.isTurret ?? false) ? 2 : 1 : WeaponRotation;
            set => WeaponRotation = value;
        }
        public float WeaponECM = 0;

        public SubTexture ModuleTexture => ResourceManager.Texture(IconTexturePath);

        public float ActualPowerStoreMax   => Is(ShipModuleType.FuelCell) ? PowerStoreMax * Bonuses.FuelCellMod : PowerStoreMax;
        public float ActualPowerFlowMax    => PowerFlowMax  * Bonuses.PowerFlowMod;
        public float ActualBonusRepairRate => BonusRepairRate * Bonuses.RepairRateMod;
        public float ActualShieldPowerMax { get; private set; }
        public float ActualMaxHealth       => TemplateMaxHealth * Bonuses.HealthMod;

        public bool HasInternalRestrictions => Restrictions == Restrictions.I || Restrictions == Restrictions.IO;

        // FB: This method was created to deal with modules which have secondary functionality. Use this whenever you want to check
        // module types for calculations. Dont use it when you are looking for main functionality as defined in the xml (for instance - ship design screen)
        public bool Is(ShipModuleType type)
        {
            switch (type)
            {
                case ShipModuleType.PowerPlant: return Flyweight.PowerFlowMax >= 1f;
                case ShipModuleType.Shield:     return Flyweight.shield_power_max >= 1f;
                case ShipModuleType.Armor:      return ModuleType == type || APResist > 0;
                case ShipModuleType.Ordnance:   return ModuleType == type && OrdinanceCapacity > 0;
                default:                        return ModuleType == type;
            }
        }

        // this is the design spec of the module
        float TemplateMaxHealth;

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
            Parent.AddShipHealth(healthChange);
        }

        public void UpdateShieldPowerMax(float shieldAmplify)
        {
            // only amplify dedicated shields
            float actualAmplify = ModuleType == ShipModuleType.Shield ? shieldAmplify : 0;
            ActualShieldPowerMax = (shield_power_max + actualAmplify) * Bonuses.ShieldMod;
        }

        public bool IsAmplified => ActualShieldPowerMax > shield_power_max * Bonuses.ShieldMod;

        [Pure] public Ship GetParent() => Parent;

        [Pure] public bool TryGetHangarShip(out Ship ship)
        {
            ship = hangarShip;
            return hangarShip != null;
        }

        public bool IsHangarShipActive => TryGetHangarShip(out Ship ship) && ship.Active;
        public bool TryGetHangarShipActive(out Ship ship) => TryGetHangarShip(out ship) && ship.Active;

        public override bool ParentIsThis(Ship ship) => Parent == ship;

        ShipModule() : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = ShipModuleFlyweight.Empty;
        }

        ShipModule(ShipModule_Deserialize template) : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = new ShipModuleFlyweight(template);
            XSIZE                 = template.XSIZE;
            YSIZE                 = template.YSIZE;
            Mass                  = template.Mass;
            Powered               = template.Powered;
            FieldOfFire           = template.FieldOfFire.ToRadians(); // @note Convert to radians for higher PERF
            FacingDegrees         = template.facing;
            XMLPosition           = template.XMLPosition;
            NameIndex             = template.NameIndex;
            DescriptionIndex      = template.DescriptionIndex;
            Restrictions          = template.Restrictions;
            ShieldPower           = template.shield_power;
            hangarShipUID         = template.hangarShipUID;
            hangarTimer           = template.hangarTimer;
            ModuleType            = template.ModuleType;
            WeaponType            = template.WeaponType;
            isWeapon              = WeaponType.NotEmpty();
            OrdinanceCapacity     = template.OrdinanceCapacity;
            IconTexturePath       = template.IconTexturePath;
            TargetValue           = template.TargetValue;
            TemplateMaxHealth     = template.HealthMax;

            UpdateModuleRadius();
        }


        public static ShipModule CreateTemplate(ShipModule_Deserialize template)
        {
            return new ShipModule(template);
        }

        // Called by Create() and ShipDesignScreen.CreateDesignModule
        // LOYALTY can be null
        public static ShipModule CreateNoParent(ShipModule template, Empire loyalty, ShipData hull)
        {
            var module = new ShipModule
            {
                Flyweight         = template.Flyweight,
                // @note loyalty can be null, in which case it uses hull bonus only
                Bonuses           = EmpireShipBonuses.Get(loyalty, hull),
                DescriptionIndex  = template.DescriptionIndex,
                FieldOfFire       = template.FieldOfFire,
                hangarShipUID     = template.hangarShipUID,
                hangarTimer       = template.hangarTimer,
                TemplateMaxHealth = template.TemplateMaxHealth,
                ModuleType        = template.ModuleType,
                isWeapon          = template.isWeapon,
                WeaponType        = template.WeaponType,
                Mass              = template.Mass,
                NameIndex         = template.NameIndex,
                OrdinanceCapacity = template.OrdinanceCapacity,
                ShieldPower       = template.shield_power_max, // This one is strange -Gretman
                XSIZE             = template.XSIZE,
                YSIZE             = template.YSIZE,
                IconTexturePath   = template.IconTexturePath,
                Restrictions      = template.Restrictions
            };

            module.Health = module.ActualMaxHealth;
            module.UpdateModuleRadius();
            module.UpdateShieldPowerMax(0);

            // @todo This might need to be updated with latest ModuleType logic?
            module.TargetValue += module.Is(ShipModuleType.Armor)             ? -1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Bomb)              ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Command)           ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Countermeasure)    ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Drone)             ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Engine)            ? 2 : 0;
            module.TargetValue += module.Is(ShipModuleType.FuelCell)          ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Hangar)            ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.MainGun)           ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.MissileLauncher)   ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Ordnance)          ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.PowerPlant)        ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Sensors)           ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Shield)            ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Spacebomb)         ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Special)           ? 1 : 0;
            module.TargetValue += module.Is(ShipModuleType.Turret)            ? 1 : 0;
            module.TargetValue += module.explodes                             ? 2 : 0;
            module.TargetValue += module.isWeapon                             ? 1 : 0;
            return module;
        }

        // this is used during Ship creation, Ship template creation or Ship loading from save
        public static ShipModule Create(string uid, Ship parent, ModuleSlotData slot, bool isTemplate, bool fromSave)
        {
            ShipModule template = ResourceManager.GetModuleTemplate(uid);
            ShipModule module = CreateNoParent(template, parent.loyalty, parent.shipData);
            module.Parent = parent;
            module.SetModuleFacing(module.XSIZE, module.YSIZE, slot.GetOrientation(), slot.Facing);
            module.Initialize(slot.Position, isTemplate);
            if (fromSave)
            {
                module.Active                = slot.Health > 0.01f;
                module.ShieldPower           = slot.ShieldPower;
                module.SetHealth(slot.Health, fromSave: true);
            }
            return module;
        }

        void Initialize(Vector2 pos, bool isTemplate)
        {
            if (Parent == null)
                Log.Error("module parent cannot be null!");

            ++DebugInfoScreen.ModulesCreated;

            XMLPosition = pos;

            // center of the top left 1x1 slot of this module
            //Vector2 topLeftCenter = pos - new Vector2(256f, 256f);

            // top left position of this module
            Position = new Vector2(pos.X - 264f, pos.Y - 264f);
            LocalCenter = new Vector2(Position.X + XSIZE * 8f, Position.Y + YSIZE * 8f);
            // center of this module
            Center.X = Position.X + XSIZE * 8f;
            Center.Y = Position.Y + YSIZE * 8f;
            CanVisualizeDamage = ShipModuleDamageVisualization.CanVisualize(this);

            SetAttributes();

            if (!isTemplate)
            {
                if (shield_power_max > 0.0f)
                    Shield = ShieldManager.AddShield(this, Rotation, Center);
            }
        }

        public void InitHangar()
        {
            // for the non faction AI , all hangars are dynamic. It makes the AI carriers better
            if (Parent.loyalty.isFaction)
                return;

            DynamicHangar = ShipBuilder.GetDynamicHangarOptions(hangarShipUID);
            if (DynamicHangar == DynamicHangarOptions.Static && !Parent.loyalty.isPlayer)
                DynamicHangar = DynamicHangarOptions.DynamicLaunch; //AI will always get dynamic launch.
        }

        public ShipData.RoleName[] HangarRoles
        {
            get
            {
                var tempRoles = new Array<ShipData.RoleName>();
                foreach (var roleName in PermittedHangarRoles)
                {
                    tempRoles.Add((ShipData.RoleName)Enum.Parse(typeof(ShipData.RoleName), roleName));
                }
                return tempRoles.ToArray();
            }
        }

        public float BayOrdnanceUsagePerSecond
        {
            get
            {
                float ordnancePerSecond = 0;
                if (ModuleType == ShipModuleType.Hangar && hangarTimerConstant > 0)
                {
                    string hangarShipName = ShipBuilder.IsDynamicHangar(hangarShipUID)
                        ? CarrierBays.GetDynamicShipNameShipDesign(this)
                        : hangarShipUID;

                    if (ResourceManager.ShipsDict.TryGetValue(hangarShipName, out Ship template))
                        ordnancePerSecond = (template.ShipOrdLaunchCost) / hangarTimerConstant;
                }

                return ordnancePerSecond;
            }
        }

        // Refactored by RedFox - @note This method is called very heavily, so many parts have been inlined by hand
        public void UpdateEveryFrame(FixedSimTime timeStep, float parentX, float parentY, float parentRotation,
                                     float cos, float sin, float tan)
        {
            Vector2 offset = LocalCenter;
            float cx = parentX + offset.X * cos - offset.Y * sin;
            float cy = parentY + offset.X * sin + offset.Y * cos;
            Center.X   = cx;
            Center.Y   = cy;
            Center3D.X = cx;
            Center3D.Y = cy;
            Center3D.Z = tan * (256f - XMLPosition.X);
            Rotation = parentRotation; // assume parent rotation is already normalized

            if (CanVisualizeDamage)
                UpdateDamageVisualization(timeStep);
        }

        // radius padding for collision detection
        const float CollisionRadiusMultiplier = 11.5f;

        // this is called once during module creation
        void UpdateModuleRadius()
        {
            Radius = (XSIZE > YSIZE ? XSIZE : YSIZE) * CollisionRadiusMultiplier;
        }

        // Collision test with this ShipModule. Returns TRUE if point is inside this module
        // The collision bounds are APPROXIMATED by using radius checks. This means corners
        // are not accurately checked.
        // HitTest uses the World scene POSITION. Not module XML location
        public bool HitTestNoShields(Vector2 worldPos, float radius)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = radius + Radius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            if ((dx*dx + dy*dy) > (r2*r2))
                return false; // definitely out of radius for SQUARE and non-square modules

            // we are a Square module? since we're already inside radius, collision happened
            if (XSIZE == YSIZE)
                return true;

            Capsule capsule = GetModuleCollisionCapsule();
            return capsule.HitTest(worldPos, radius);
        }

        // Gets the collision capsule in World coordinates
        public Capsule GetModuleCollisionCapsule()
        {
            float shorter = XSIZE <  YSIZE ? XSIZE : YSIZE; // wonder if .NET can optimize this? wanna bet no? :P
            float longer  = XSIZE >= YSIZE ? XSIZE : YSIZE;

            // if high module, use forward vector, if wide module, use right vector
            Vector2 longerDir = Rotation.RadiansToDirection();
            if (XSIZE > YSIZE) longerDir = longerDir.LeftVector();

            // now for more expensive and accurate capsule-line collision testing
            // since we can have 4x1 modules etc, we construct a capsule
            float smallerOffset = (shorter / longer) * longer * 8.0f;
            float longerOffset = longer * 8.0f - smallerOffset;

            return new Capsule(
                Center - longerDir * longerOffset,
                Center + longerDir * longerOffset,
                shorter * CollisionRadiusMultiplier
            );
        }

        public bool HitTestShield(Vector2 worldPos, float radius)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = radius + ShieldHitRadius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            return (dx*dx + dy*dy) <= (r2*r2);
        }

        public bool RayHitTestShield(Vector2 startPos, Vector2 endPos, float rayRadius, out float distanceFromStart)
        {
            ++GlobalStats.DistanceCheckTotal;
            return Center.RayCircleIntersect(rayRadius + ShieldHitRadius, startPos, endPos, out distanceFromStart);
        }

        public bool RayHitTest(Vector2 startPos, Vector2 endPos, float rayRadius, out float distanceFromStart)
        {
            if (ShieldsAreActive)
            {
                return Center.RayCircleIntersect(rayRadius + ShieldHitRadius, startPos, endPos, out distanceFromStart);
            }

            distanceFromStart = Center.FindClosestPointOnLine(startPos, endPos).Distance(startPos);
            return distanceFromStart > 0f;
        }

        public static float DamageFalloff(Vector2 explosionCenter, Vector2 affectedPoint, float damageRadius, float moduleRadius, float minFalloff = 0.4f)
        {
            float explodeDist = explosionCenter.Distance(affectedPoint) - moduleRadius;
            if (explodeDist < moduleRadius) explodeDist = 0;

            return Math.Min(1.0f, (damageRadius - explodeDist) / (damageRadius + minFalloff));
        }

        public void DebugDamageCircle()
        {
            Empire.Universe?.DebugWin?.DrawGameObject(DebugModes.Targeting, this);
        }

        // return TRUE if all damage was absorbed (damageInOut is less or equal to 0)
        public bool DamageExplosive(GameplayObject source, Vector2 worldHitPos, float damageRadius, ref float damageInOut)
        {
            float moduleRadius = ShieldsAreActive ? ShieldHitRadius : Radius;
            float damage = damageInOut * DamageFalloff(worldHitPos, Center, damageRadius, moduleRadius);
            if (damage <= 0.001f)
                return true;

            //Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager, Center, Radius, 1.5f);

            Damage(source, damage, out damageInOut);
            return damageInOut <= 0f;
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

            float healthBefore = Health + ShieldPower;
            if (!TryDamageModule(source, damageAmount * damageModifier))
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

            float absorbedDamage = healthBefore - (Health + ShieldPower);
            if (damageModifier <= 1) // below 1, resistance. above 1, vulnerability.
                absorbedDamage /= damageModifier; // module absorbed more dam because of good resistance
            // else: extra dam already calculated

            if (source != null) EvtDamageInflicted(source, absorbedDamage);

            damageRemainder = (int)(damageAmount - absorbedDamage);
        }

        private void Deflect(GameplayObject source)
        {
            if (!Parent.InFrustum || Empire.Universe?.IsShipViewOrCloser == false)
                return;

            if (!(source is Projectile proj))
                return;

            if (proj.Explodes || proj.Duration < 0)
                return;

            proj.Deflect(Parent.loyalty, Center3D);
        }

        public void DebugDamage(float percent)
        {
            float health = Health * percent + ShieldPower;
            float damage = health.Clamped(0, Health + ShieldPower);
            Ship source = GetParent();
            Damage(source, damage);
        }

        public void DamageByRecoveredFromCrash()
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
                || explodes)
            {
                percent = 0.95f;
            }

            Ship source  = GetParent();
            if (ActualShieldPowerMax > 0)
                Damage(source, ActualShieldPowerMax); // Kill shield power first

            Damage(source, Health * percent);
        }

        public override void Damage(GameplayObject source, float damageAmount)
            => Damage(source, damageAmount, out float _);

        bool TryDamageModule(GameplayObject source, float modifiedDamage)
        {
            if (source != null)
                Parent.LastDamagedBy = LastDamagedBy = source;

            Parent.InCombatTimer       = 15f;
            Parent.ShieldRechargeTimer = 0f;

            var beam = source as Beam;
            var proj = source as Projectile;

            bool damagingShields = ShieldsAreActive && proj?.IgnoresShields != true;
            if (beam == null) // only for projectiles
            {
                float damageThreshold = damagingShields ? shield_threshold : DamageThreshold;
                if (proj?.Weapon.EMPDamage >= damageThreshold && !damagingShields)
                    CauseEmpDamage(proj); // EMP damage can be applied if not hitting shields

                if (modifiedDamage < damageThreshold && proj?.WeaponEffectType != "Plasma")
                    return false; // no damage could be done, the projectile was deflected.
            }

            // BUG: So this makes it so that if ShieldPower is greater than zero the module wont be damaged.
            // even if the damage is greater than the shield amount.
            if (damagingShields)
            {
                ShieldPower = (ShieldPower - modifiedDamage).Clamped(0, ShieldPower);

                if (proj != null)
                {
                    if (beam != null)
                    {
                        CauseSiphonDamage(beam);
                        BeamMassDamage(beam, hittingShields: true);
                    }

                    if (Parent.InFrustum && Empire.Universe?.IsShipViewOrCloser == true)
                        Shield.HitShield(this, proj);
                }

                Parent.UpdateShields();
                //Log.Info($"{Parent.Name} shields '{UID}' dmg {modifiedDamage} shld {ShieldPower} by {proj?.WeaponType}");
            }
            else
            {
                if (beam != null)
                    CauseSpecialBeamDamage(beam);
                
                SetHealth(Health - modifiedDamage);
                DebugPerseveranceNoDamage();

                //Log.Info($"{Parent.Name} module '{UID}' dmg {modifiedDamage}  hp  {Health} by {proj?.WeaponType}");
            }

            if (Parent.InFrustum && Empire.Universe?.IsShipViewOrCloser == true)
            {
                if      (beam != null)            beam.CreateHitParticles(Center3D.Z);
                else if (proj?.Explodes == false) proj.CreateHitParticles(modifiedDamage, Center3D);

                CreateHitDebris(proj);
            }

            return true;
        }

        void CreateHitDebris(Projectile proj)
        {
            if (proj == null || !RandomMath.RollDice(50 + Area))
                return;

            if (proj.Weapon.Tag_Kinetic || proj.Weapon.Tag_Explosive)
            {
                Vector2 velocity = Parent.Velocity + RandomMath.Vector2D(Parent.Velocity.Length()) * (1 + RandomMath.RollDie(200)/100);
                SpawnDebris(1, velocity, 1, ignite: false);
            }
        }
        float GetGlobalArmourBonus()
        {
            if (GlobalStats.ActiveModInfo?.UseHullBonuses == true &&
                ResourceManager.HullBonuses.TryGetValue(Parent.shipData.Hull, out HullBonus mod))
                return (1f - mod.ArmoredBonus);

            return 1f;
        }

        void CauseEmpDamage(Projectile proj)
        {
            if (proj.Weapon.EMPDamage <= 0f)
                return;

            Parent.CauseEmpDamage(proj.Weapon.EMPDamage);
        }

        void CauseSpecialBeamDamage(Beam beam, bool hittingShields = false)
        {
            BeamPowerDamage(beam);
            BeamTroopDamage(beam);
            BeamMassDamage(beam, hittingShields);
            BeamRepulsionDamage(beam);
        }

        void BeamPowerDamage(Beam beam)
        {
            if (beam.Weapon.PowerDamage <= 0)
                return;

            Parent.CausePowerDamage(beam.Weapon.PowerDamage);
        }

        void BeamTroopDamage(Beam beam)
        {
            if (beam.Weapon.TroopDamageChance <= 0f)
                return;

            Parent.CauseTroopDamage(beam.Weapon.TroopDamageChance);
        }

        void BeamMassDamage(Beam beam, bool hittingShields)
        {
            if (beam.Weapon.MassDamage <= 0f)
                return;

            Parent.CauseMassDamage(beam.Weapon.MassDamage, hittingShields);
        }

        void BeamRepulsionDamage(Beam beam)
        {
            if (beam.Weapon.RepulsionDamage < 1)
                return;

            Parent.CauseRepulsionDamage(beam);
        }

        void DebugPerseveranceNoDamage()
        {
        #if DEBUG
            if (!Empire.Universe.Debug || Parent.VanityName != "Perseverance")
                return;
            if (Health < 10) // never give up, never surrender! F_F
                SetHealth(10);
        #endif
        }

        void CauseSiphonDamage(Beam beam)
        {
            if (beam.Weapon.SiphonDamage <= 0f)
                return;
            ShieldPower -= beam.Weapon.SiphonDamage;
            ShieldPower = ShieldPower.Clamped(0, shield_power_max);
            beam.Owner?.AddPower(beam.Weapon.SiphonDamage);
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++DebugInfoScreen.ModulesDied;
            ShieldPower = 0f;

            if (Active && Parent.InFrustum)
            {
                var center = new Vector3(Center.X, Center.Y, -100f);
                bool parentAlive = !Parent.dying;
                for (int i = 0; i < 30; ++i)
                {
                    Vector3 pos = parentAlive ? center : new Vector3(Parent.Center, UniverseRandom.RandomBetween(-25f, 25f));
                    Empire.Universe.explosionParticles.AddParticleThreadA(pos, Vector3.Zero);
                }

                SpawnDebris(Area, Parent.Velocity,0);
            }

            Active = false;
            Parent.shipStatusChanged = true;
            Parent.ShouldRecalculatePower |= ActualPowerFlowMax > 0 || PowerRadius > 0;
            Parent.UpdateExternalSlots(this, becameActive: false);

            if (!cleanupOnly && source != null)
            {
                if (Parent.Active && Parent.InFrustum && Empire.Universe.IsShipViewOrCloser)
                {
                    GameAudio.PlaySfxAsync("sd_explosion_module_small", Parent.SoundEmitter);
                }

                if (explodes)
                {
                    UniverseScreen.Spatial.ExplodeAtModule(source, this,
                        ignoresShields: true, damageAmount: ExplosionDamage, damageRadius: ExplosionRadius);
                }
            }
        }

        void SpawnDebris(int size, Vector2 velocity, int count, bool ignite = true)
        {
            if (count == 0) 
                count = (int)RandomMath.RandomBetween(0, size / 2 + 1);

            if (count != 0)
            {
                float debrisScale = size * 0.05f;
                SpaceJunk.SpawnJunk(count, Center, velocity, this, 1.0f, debrisScale, ignite: ignite);
            }
        }

        //added by gremlin boarding parties
        public bool LaunchBoardingParty(Troop troop, out Ship ship)
        {
            ship = null;
            if (!IsTroopBay || !Powered)
                return false;

            if (hangarShip != null) // this bay's ship is already out
            {
                if (hangarShip.AI.State == AIState.ReturnToHangar
                    || hangarShip.AI.State == AIState.AssaultPlanet
                    || hangarShip.AI.EscortTarget != null
                    || hangarShip.AI.OrbitTarget != null)
                {
                    return false;
                }
                hangarShip.DoEscort(Parent);
                ship = hangarShip;
                return true;
            }

            if (hangarTimer <= 0f && hangarShip == null) // launch the troopship
            {
                hangarShip = Ship.CreateTroopShipAtPoint(Parent.loyalty.GetAssaultShuttleName(), Parent.loyalty, Center, troop);
                hangarShip.Mothership = Parent;
                hangarShip.DoEscort(Parent);
                hangarShip.Velocity = Parent.Velocity;
                HangarShipGuid      = hangarShip.guid;
                hangarTimer         = hangarTimerConstant;
                ship                = hangarShip;
                // transfer our troop onto the shuttle we just spawned
                troop.LandOnShip(hangarShip);
                return true;
            }

            return false;
        }

        //added by gremlin fighter rearm fix
        public void ScrambleFighters()
        {
            if (IsTroopBay || IsSupplyBay || !Powered)
                return;

            if (hangarShip != null && hangarShip.Active)
            {
                if (hangarShip.AI.HasPriorityTarget
                    || hangarShip.AI.IgnoreCombat
                    || hangarShip.AI.Target != null
                    || (hangarShip.Center.InRadius(Parent.Center, Parent.SensorRange) && hangarShip.AI.State != AIState.ReturnToHangar))
                {
                    return;
                }

                hangarShip.DoEscort(Parent);
                return;
            }

            if (hangarTimer <= 0f && (hangarShip == null || hangarShip != null && !hangarShip.Active))
            {
                SetHangarShip(Ship.CreateShipFromHangar(this, Parent.loyalty, Parent.Center + LocalCenter, Parent));
                if (hangarShip == null)
                {
                    Log.Warning($"Could not create ship from hangar, UID = {hangarShipUID}");
                    return;
                }

                hangarShip.DoEscort(Parent);
                hangarShip.Velocity   = Parent.Velocity + UniverseRandom.RandomDirection() * hangarShip.SpeedLimit;
                hangarShip.Mothership = Parent;
                HangarShipGuid        = hangarShip.guid;
                hangarTimer           = hangarTimerConstant;
                Parent.ChangeOrdnance(-hangarShip.ShipOrdLaunchCost);
            }
        }

        public void ResetHangarTimer()
        {
            if (hangarTimerConstant.Greater(0))
                hangarTimer = hangarTimerConstant;
        }

        public void SetAttributes()
        {
            switch (ModuleType)
            {
                case ShipModuleType.Drone:
                case ShipModuleType.Spacebomb:
                case ShipModuleType.MissileLauncher: InstallWeapon();                                   break;
                case ShipModuleType.Turret:          InstallWeapon(); InstalledWeapon.isTurret  = true; break;
                case ShipModuleType.MainGun:         InstallWeapon(); InstalledWeapon.isMainGun = true; break;
                case ShipModuleType.Colony:          if (Parent != null) Parent.isColonyShip    = true; break;
                case ShipModuleType.Bomb:            InstallBomb();                                     break;
            }

            if (IsSupplyBay && Parent != null)
                Parent.IsSupplyShip = true;
        }

        void InstallWeapon()
        {
            if (InstalledWeapon != null && InstalledWeapon.WeaponType == WeaponType)
                return;
            ConfigWeapon(WeaponType);
            Parent?.Weapons.Add(InstalledWeapon);
            isWeapon = true;
        }

        void InstallBomb()
        {
            if (InstalledWeapon != null && InstalledWeapon.UID == BombType)
                return;
            ConfigWeapon(BombType);
            Parent?.BombBays.Add(this);
        }

        void ConfigWeapon(string weaponType)
        {
            InstalledWeapon = ResourceManager.CreateWeapon(weaponType);
            InstalledWeapon.Module = this;
            InstalledWeapon.Owner  = Parent;
        }

        public void SetHangarShip(Ship ship)
        {
            hangarShip = ship;
            if (ship != null)
                HangarShipGuid = ship.guid;  //fbedard: save mothership
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

        void ResurrectModule()
        {
            Active = true;
            Parent.shipStatusChanged = true;
            Parent.ShouldRecalculatePower = true;
            Parent.UpdateExternalSlots(this, becameActive: true);
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
                hangarTimer -= timeStep.FixedTime;

            // Shield Recharge / Discharge
            if (Is(ShipModuleType.Shield))
            {
                float shieldMax = ActualShieldPowerMax;
                ShieldPower = RechargeShields(ShieldPower, shieldMax, timeStep); // use regular recharge
            }

            if (TransporterTimer > 0)
                TransporterTimer -= timeStep.FixedTime;

            base.Update(timeStep);
        }

        float RechargeShields(float shieldPower, float shieldMax, FixedSimTime timeStep)
        {
            if (!Active || !Powered || shieldPower >= ActualShieldPowerMax)
                return shieldPower;

            if (Parent.ShieldRechargeTimer > shield_recharge_delay)
                shieldPower += shield_recharge_rate * timeStep.FixedTime;
            else if (ShieldPower > 0)
                shieldPower += shield_recharge_combat_rate * timeStep.FixedTime;
            return shieldPower.Clamped(0, shieldMax);
        }

        public float GetActualMass(Empire loyalty, float ordnancePercent)
        {
            float mass = Mass;
            if (Is(ShipModuleType.Armor))
                mass *= loyalty.data.ArmourMassModifier;

            if (Is(ShipModuleType.Ordnance))
                mass = (mass * ordnancePercent).LowerBound(mass * 0.1f);

            // regular positive mass modules
            if (mass >= 0f)
                return mass;

            // only allow negative mass modules (mass reduction devices)
            // if we're powered, otherwise return their absolute mass
            return Powered ? mass : Math.Abs(mass);
        }

        // @note This is called every frame for every module for every ship in the universe
        void UpdateDamageVisualization(FixedSimTime timeStep)
        {
            if (OnFire && Parent.InFrustum && Empire.Universe.IsSystemViewOrCloser)
            {
                if (DamageVisualizer == null)
                    DamageVisualizer = new ShipModuleDamageVisualization(this);

                DamageVisualizer.Update(timeStep, Center3D, Active);
            }
            else // destroy immediately when out of vision range or if module is no longer OnFire
            {
                DamageVisualizer = null;
            }
        }

        public void UpdateWhileDying(FixedSimTime timeStep)
        {
            if (!RandomMath.RollDice(10))
                return; 

            Center3D = Parent.Center.ToVec3(UniverseRandom.RandomBetween(-25f, 25f));
            if (CanVisualizeDamage)
                UpdateDamageVisualization(timeStep);
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
            if (Parent.InFrustum && Empire.Universe?.IsShipViewOrCloser == true)
            {
                float modelZ = Parent.BaseHull.ModelZ;
                modelZ = modelZ.Clamped(0, 200) * -1;
                Vector3 repairEffectOrigin = Center.ToVec3(modelZ);
                for (int i = 0; i < 50; i++)
                    Empire.Universe.sparks.AddParticleThreadB(repairEffectOrigin, Vector3.Zero);
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

            if (Empire.Universe.Debug && isExternal)
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
                float shieldCoverage = ((shield_radius + 8f) * (shield_radius + 8f) * 3.14159f) / 256f / slotCount;

                shieldDef *= shieldCoverage < 1 ? shieldCoverage : 1f;
                shieldDef *= 1 + shield_kinetic_resist / 5;
                shieldDef *= 1 + shield_energy_resist / 5;
                shieldDef *= 1 + shield_beam_resist / 5;
                shieldDef *= 1 + shield_missile_resist / 5;
                shieldDef *= 1 + shield_explosive_resist / 5;

                shieldDef *= shield_recharge_rate > 0 ? shield_recharge_rate / (shieldsMax / 100) : 1f;
                shieldDef *= shield_recharge_delay > 0 ? 1f / shield_recharge_delay : 1f;
                shieldDef *= shield_recharge_combat_rate > 0 ? 1 + shield_recharge_combat_rate / (shieldsMax / 25) : 1f;

                shieldDef *= 1 + shield_threshold / 50f; // FB: Shield Threshold is much more effective than Damage threshold as it apples to the shield bubble.
                def       += shieldDef;
            }

            // FB: all resists are divided by 5 since there are 5-6 weapon types
            def *= 1 + BeamResist / 5;
            def *= 1 + KineticResist / 5 ;
            def *= 1 + EnergyResist / 5;
            def *= 1 + GuidedResist / 5;
            def *= 1 + MissileResist / 5;
            def *= 1 + HybridResist / 5;
            def *= 1 + InterceptResist / 5;
            def *= 1 + ExplosiveResist / 5;
            def *= 1 + RailgunResist / 5;
            def *= 1 + SpaceBombResist / 5;
            def *= 1 + BombResist / 5;
            def *= 1 + BioWeaponResist / 5;
            def *= 1 + DroneResist / 5;
            def *= 1 + WarpResist / 5;
            def *= 1 + TorpedoResist / 5;
            def *= 1 + CannonResist / 5;
            def *= 1 + SubspaceResist / 5;
            def *= 1 + PDResist / 5;
            def *= 1 + FlakResist / 5;

            def *= 1 + DamageThreshold / 100f;
            def *= ModuleType == ShipModuleType.Armor ? 1 + APResist / 2 : 1f;

            def += ECM;
            def *= 1 + EMP_Protection / 500;

            // Engines
            def += (TurnThrust + WarpThrust + thrust) / 15000f;

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
            if (ModuleType != ShipModuleType.Hangar || hangarShipUID.IsEmpty()
                                                    || hangarShipUID == "NotApplicable"
                                                    || IsSupplyBay
                                                    || IsTroopBay)
                return off;

            if (ShipBuilder.IsDynamicHangar(hangarShipUID))
            {
                off += MaximumHangarShipSize * 100 * PermittedHangarRoles.Length / hangarTimerConstant.LowerBound(1);
            }
            else
            {
                if (ResourceManager.GetShipTemplate(hangarShipUID, out Ship hShip))
                    off += (hShip.BaseStrength > 0f) ? hShip.BaseStrength : hShip.CalculateShipStrength();
                else
                    off += 100f;
            }

            return off;
        }

        public static float DefaultFacingFor(ModuleOrientation orientation)
        {
            switch (orientation)
            {
                default:
                case ModuleOrientation.Normal: return 0f;
                case ModuleOrientation.Left:   return 270f;
                case ModuleOrientation.Right:  return 90f;
                case ModuleOrientation.Rear:   return 180f;
            }
        }

        public void SetModuleFacing(int w, int h, ModuleOrientation orientation, float facing)
        {
            FacingDegrees = facing;
            switch (orientation)
            {
                case ModuleOrientation.Normal:
                case ModuleOrientation.Rear:
                    XSIZE = w;
                    YSIZE = h;
                    break;
                case ModuleOrientation.Left:
                case ModuleOrientation.Right:
                    XSIZE = h; // if the module is facing left or right, then length is now height
                    YSIZE = w;
                    break;
            }
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

                return hangarShip?.Active == true;
            }
        }

        public override string ToString() => $"{UID}  {Id}  x {Position.X} y {Position.Y}  size {XSIZE}x{YSIZE}  world={Center}  Ship={Parent?.Name}";
    }
}