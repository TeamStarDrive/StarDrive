using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Particle3DSample;
using System;
using Ship_Game.AI;
using Ship_Game.Debug;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
    public sealed class ShipModule : GameplayObject
    {
        //private static int TotalModules = 0;
        //public int ID = ++TotalModules;
        public ShipModuleFlyweight Flyweight; //This is where all the other member variables went. Having this as a member object
                                              //allows me to instance the variables inside it, so they are not duplicated. This
                                              //can offer much better memory usage since ShipModules are so numerous.     -Gretman
        private ParticleEmitter trailEmitter;
        private ParticleEmitter firetrailEmitter;
        private ParticleEmitter flameEmitter;
        public int XSIZE = 1;
        public int YSIZE = 1;
        public bool Powered;
        public float FieldOfFire;
        public float Facing;        // the firing arc direction of the module, used to rotate the module overlay 90, 180 or 270 degs
        public Vector2 XMLPosition; // module slot location in the ship design; the coordinate system axis is {256,256}
        private Ship Parent;
        public float HealthMax;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public Restrictions Restrictions;
        public float ShieldPower;
        private Shield shield;
        public string hangarShipUID;
        private Ship hangarShip;
        public Guid HangarShipGuid;
        public float hangarTimer;
        public bool isWeapon;
        public Weapon InstalledWeapon;
        public short OrdinanceCapacity;
        private bool onFire;
        private bool reallyFuckedUp;
        private Vector3 Center3D;
        public float BombTimer;
        public ShipModuleType ModuleType;
        public string IconTexturePath;

        public string UID => Flyweight.UID;
        
        public bool isExternal;
        public int TargetValue;
        public int quadrant = -1;
        public float TransporterTimer;

        // This is used to calculate whether this module has power or not
        private int ActivePowerSources;
        public bool HasPower => ActivePowerSources > 0;
        public bool CheckedConduits;

        // Used to configure how good of a target this module is
        public int ModuleTargettingValue => TargetValue 
                                          //+ (isExternal ? -5 : 0)         // external modules are less critical
                                          + (Health < HealthMax ? 1 : 0); // prioritize already damaged modules        
        //This wall of text is the 'get' functions for all of the variables that got moved to the 'Flyweight' object.
        //This will allow us to still use the normal "Module.IsCommandModule" even though 'IsCommandModule' actually
        //lives in "Module.Flyweight.IsCommandModule" now.    -Gretman
        public float FTLSpeed                   => Flyweight.FTLSpeed;
        public string DeployBuildingOnColonize  => Flyweight.DeployBuildingOnColonize;
        public string ResourceStored            => Flyweight.ResourceStored;
        public float ResourceStorageAmount      => Flyweight.ResourceStorageAmount;
        public bool IsCommandModule             => Flyweight.IsCommandModule;
        public bool IsRepairModule              => Flyweight.IsRepairModule;
        public string[] PermittedHangarRoles    => Flyweight.PermittedHangarRoles;
        public short MaximumHangarShipSize      => Flyweight.MaximumHangarShipSize;
        public bool FightersOnly                => Flyweight.FightersOnly;
        public bool DroneModule                 => Flyweight.DroneModule;
        public bool FighterModule               => Flyweight.FighterModule;
        public bool CorvetteModule              => Flyweight.CorvetteModule;
        public bool FrigateModule               => Flyweight.FrigateModule;
        public bool DestroyerModule             => Flyweight.DestroyerModule;
        public bool CruiserModule               => Flyweight.CruiserModule;
        public bool CarrierModule               => Flyweight.CarrierModule;
        public bool CapitalModule               => Flyweight.CapitalModule;
        public bool FreighterModule             => Flyweight.FreighterModule;
        public bool PlatformModule              => Flyweight.PlatformModule;
        public bool StationModule               => Flyweight.StationModule;
        public bool explodes                    => Flyweight.explodes;
        public float SensorRange                => Flyweight.SensorRange;
        public float MechanicalBoardingDefense  => Flyweight.MechanicalBoardingDefense;
        public float EMP_Protection             => Flyweight.EMP_Protection;
        public int PowerRadius                  => Flyweight.PowerRadius;
        public int TechLevel                    => Flyweight.TechLevel;
        public float OrdnanceAddedPerSecond     => Flyweight.OrdnanceAddedPerSecond;
        public string BombType                  => Flyweight.BombType;
        public float WarpMassCapacity           => Flyweight.WarpMassCapacity;
        public float BonusRepairRate            => Flyweight.BonusRepairRate;
        public int Cargo_Capacity               => Flyweight.Cargo_Capacity;
        public float shield_radius              => Flyweight.shield_radius;
        public float shield_power_max           => Flyweight.shield_power_max;
        public float shield_recharge_rate       => Flyweight.shield_recharge_rate;
        public float shield_recharge_combat_rate=> Flyweight.shield_recharge_combat_rate;
        public float shield_recharge_delay      => Flyweight.shield_recharge_delay;
        public float shield_threshold           => Flyweight.shield_threshold;
        public float shield_kinetic_resist      => Flyweight.shield_kinetic_resist;
        public float shield_energy_resist       => Flyweight.shield_energy_resist;
        public float shield_explosive_resist    => Flyweight.shield_explosive_resist;
        public float shield_missile_resist      => Flyweight.shield_missile_resist;
        public float shield_flak_resist         => Flyweight.shield_flak_resist;
        public float shield_hybrid_resist       => Flyweight.shield_hybrid_resist;
        public float shield_railgun_resist      => Flyweight.shield_railgun_resist;
        public float shield_subspace_resist     => Flyweight.shield_subspace_resist;
        public float shield_warp_resist         => Flyweight.shield_warp_resist;
        public float shield_beam_resist         => Flyweight.shield_beam_resist;
        public float numberOfColonists          => Flyweight.numberOfColonists;
        public float numberOfEquipment          => Flyweight.numberOfEquipment;
        public float numberOfFood               => Flyweight.numberOfFood;
        public bool IsSupplyBay                 => Flyweight.IsSupplyBay;
        public bool IsTroopBay                  => Flyweight.IsTroopBay;
        public float hangarTimerConstant        => Flyweight.hangarTimerConstant;
        public float thrust                     => Flyweight.thrust;
        public float WarpThrust                 => Flyweight.WarpThrust;
        public float TurnThrust                 => Flyweight.TurnThrust;
        public float PowerFlowMax               => Flyweight.PowerFlowMax;
        public float PowerDraw                  => Flyweight.PowerDraw;
        public float PowerDrawAtWarp            => Flyweight.PowerDrawAtWarp;
        public float PowerStoreMax              => Flyweight.PowerStoreMax;
        public float HealPerTurn                => Flyweight.HealPerTurn;
        public int TroopCapacity                => Flyweight.TroopCapacity;
        public int TroopsSupplied               => Flyweight.TroopsSupplied;
        public float Cost                       => Flyweight.Cost;
        public float InhibitionRadius           => Flyweight.InhibitionRadius;
        public float FTLSpoolTime               => Flyweight.FTLSpoolTime;
        public float ECM                        => Flyweight.ECM;
        public float SensorBonus                => Flyweight.SensorBonus;
        public float TransporterTimerConstant   => Flyweight.TransporterTimerConstant;
        public float TransporterRange           => Flyweight.TransporterRange;
        public float TransporterPower           => Flyweight.TransporterPower;
        public float TransporterOrdnance        => Flyweight.TransporterOrdnance;
        public int TransporterTroopLanding      => Flyweight.TransporterTroopLanding;
        public int TransporterTroopAssault      => Flyweight.TransporterTroopAssault;
        public float KineticResist              => Flyweight.KineticResist;
        public float EnergyResist               => Flyweight.EnergyResist;
        public float GuidedResist               => Flyweight.GuidedResist;
        public float MissileResist              => Flyweight.MissileResist;
        public float HybridResist               => Flyweight.HybridResist;
        public float BeamResist                 => Flyweight.BeamResist;
        public float ExplosiveResist            => Flyweight.ExplosiveResist;
        public float InterceptResist            => Flyweight.InterceptResist;
        public float RailgunResist              => Flyweight.RailgunResist;
        public float SpaceBombResist            => Flyweight.SpaceBombResist;
        public float BombResist                 => Flyweight.BombResist;
        public float BioWeaponResist            => Flyweight.BioWeaponResist;
        public float DroneResist                => Flyweight.DroneResist;
        public float WarpResist                 => Flyweight.WarpResist;
        public float TorpedoResist              => Flyweight.TorpedoResist;
        public float CannonResist               => Flyweight.CannonResist;
        public float SubspaceResist             => Flyweight.SubspaceResist;
        public float PDResist                   => Flyweight.PDResist;
        public float FlakResist                 => Flyweight.FlakResist;
        public float DamageThreshold            => Flyweight.DamageThreshold;
        public int APResist                     => Flyweight.APResist;
        public bool IndirectPower               => Flyweight.IndirectPower;
        public bool isPowerArmour               => Flyweight.isPowerArmour;
        public bool isBulkhead                  => Flyweight.isBulkhead;
        public int TargetTracking               => Flyweight.TargetTracking;
        public int FixedTracking                => Flyweight.FixedTracking;
        public bool IsWeapon    => ModuleType == ShipModuleType.Spacebomb 
                                || ModuleType == ShipModuleType.Turret 
                                || ModuleType == ShipModuleType.MainGun 
                                || ModuleType == ShipModuleType.MissileLauncher 
                                || ModuleType == ShipModuleType.Drone 
                                || ModuleType == ShipModuleType.Bomb;

        public Vector2 LocalCenter => new Vector2(Position.X + XSIZE * 8f, Position.Y + XSIZE * 8f);
        public int Area => XSIZE * YSIZE;

        // the actual hit radius is a bit bigger for some legacy reason
        public float ShieldHitRadius => Flyweight.shield_radius + 10f;

        
        public float AccuracyPercent = -1;
        //private float SwivelSpeed;
        private float WeaponRotation = 0;
        public float WeaponRotationSpeed
        {
            get { return
                WeaponRotation == 0 ? (InstalledWeapon?.isTurret ?? false) ? 2 : 1 : WeaponRotation; }
            set { WeaponRotation = value; }
        }
        public float WeaponECM = 0;
        public float WeaponECCM = 0;


        private ShipModule() : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = ShipModuleFlyweight.Empty;
        }

        private ShipModule(ShipModule_Deserialize s) : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = new ShipModuleFlyweight(s);
            XSIZE                = s.XSIZE;
            YSIZE                = s.YSIZE;
            Mass                 = s.Mass;
            Powered              = s.Powered;
            FieldOfFire          = s.FieldOfFire;
            Facing               = s.facing;
            XMLPosition          = s.XMLPosition;
            HealthMax            = s.HealthMax;
            WeaponType           = s.WeaponType;
            NameIndex            = s.NameIndex;
            DescriptionIndex     = s.DescriptionIndex;
            Restrictions         = s.Restrictions;
            ShieldPower          = s.shield_power;
            hangarShipUID        = s.hangarShipUID;
            hangarTimer          = s.hangarTimer;
            isWeapon             = s.isWeapon;
            OrdinanceCapacity    = s.OrdinanceCapacity;
            BombTimer            = s.BombTimer;
            ModuleType           = s.ModuleType;
            IconTexturePath      = s.IconTexturePath;
            //isExternal           = s.isExternal; // I think it's safer to let ship externals init handle this...
            TargetValue          = s.TargetValue;
        }

        public static ShipModule CreateTemplate(ShipModule_Deserialize template)
        {
            return new ShipModule(template);
        }

        public static ShipModule Create(string uid, Ship parent, Vector2 xmlPos, float facing, bool addToShieldManager = true
            , ShipDesignScreen.ActiveModuleState orientation = ShipDesignScreen.ActiveModuleState.Normal)
        {
            ShipModule module = CreateNoParent(uid);
            module.SetParent(parent);
            module.Facing = facing;
            module.ApplyModuleOrientation(orientation);
            module.Initialize(xmlPos, addToShieldManager);            
            return module;
        }

        public static ShipModule CreateNoParent(string uid)
        {
            ShipModule template = ResourceManager.GetModuleTemplate(uid);
            var module = new ShipModule
            {
                // All complex properties here have been replaced by this single reference to 'ShipModuleFlyweight' which now contains them all - Gretman
                Flyweight         = template.Flyweight,
                DescriptionIndex  = template.DescriptionIndex,
                FieldOfFire       = template.FieldOfFire,
                hangarShipUID     = template.hangarShipUID,
                hangarTimer       = template.hangarTimer,
                Health            = template.HealthMax,
                HealthMax         = template.HealthMax,
                isWeapon          = template.isWeapon,
                Mass              = template.Mass,
                ModuleType        = template.ModuleType,
                NameIndex         = template.NameIndex,
                OrdinanceCapacity = template.OrdinanceCapacity,
                ShieldPower       = template.shield_power_max, //Hmmm... This one is strange -Gretman
                XSIZE             = template.XSIZE,
                YSIZE             = template.YSIZE,
                IconTexturePath   = template.IconTexturePath,
                Restrictions      = template.Restrictions
            };
            // @todo This might need to be updated with latest ModuleType logic?
            module.TargetValue += module.ModuleType == ShipModuleType.Armor           ? -1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Bomb            ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Command         ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Countermeasure  ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Drone           ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Engine          ? 2 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.FuelCell        ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Hangar          ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.MainGun         ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.MissileLauncher ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Ordnance        ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.PowerPlant      ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Sensors         ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Shield          ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Spacebomb       ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Special         ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Turret          ? 1 : 0;
            module.TargetValue += module.explodes ? 2 : 0;
            module.TargetValue += module.isWeapon ? 1 : 0;
            return module;
        }

        // @todo Why isn't this used? A bug?
        // Nah, this was added by The Doctor a few centries ago, and to my knowledge was never completed.
        private float ApplyShieldResistances(Weapon weapon, float damage)
        {
            if (weapon.Tag_Kinetic)   damage -= damage * shield_kinetic_resist;
            if (weapon.Tag_Energy)    damage -= damage * shield_energy_resist;
            if (weapon.Tag_Explosive) damage -= damage * shield_explosive_resist;
            if (weapon.Tag_Missile)   damage -= damage * shield_missile_resist;
            if (weapon.Tag_Flak)      damage -= damage * shield_flak_resist;
            if (weapon.Tag_Hybrid)    damage -= damage * shield_hybrid_resist;
            if (weapon.Tag_Railgun)   damage -= damage * shield_railgun_resist;
            if (weapon.Tag_Subspace)  damage -= damage * shield_subspace_resist;
            if (weapon.Tag_Warp)      damage -= damage * shield_warp_resist;
            if (weapon.Tag_Beam)      damage -= damage * shield_beam_resist;
            return damage;
        }

        private float ApplyResistances(Weapon weapon, float damage)
        {
            if (weapon.Tag_Beam)      damage -= damage * BeamResist;
            if (weapon.Tag_Kinetic)   damage -= damage * KineticResist;
            if (weapon.Tag_Energy)    damage -= damage * EnergyResist;
            if (weapon.Tag_Guided)    damage -= damage * GuidedResist;
            if (weapon.Tag_Missile)   damage -= damage * MissileResist;
            if (weapon.Tag_Hybrid)    damage -= damage * HybridResist;
            if (weapon.Tag_Intercept) damage -= damage * InterceptResist;
            if (weapon.Tag_Explosive) damage -= damage * ExplosiveResist;
            if (weapon.Tag_Railgun)   damage -= damage * RailgunResist;
            if (weapon.Tag_SpaceBomb) damage -= damage * SpaceBombResist;
            if (weapon.Tag_Bomb)      damage -= damage * BombResist;
            if (weapon.Tag_BioWeapon) damage -= damage * BioWeaponResist;
            if (weapon.Tag_Drone)     damage -= damage * DroneResist;
            if (weapon.Tag_Warp)      damage -= damage * WarpResist;
            if (weapon.Tag_Torpedo)   damage -= damage * TorpedoResist;
            if (weapon.Tag_Cannon)    damage -= damage * CannonResist;
            if (weapon.Tag_Subspace)  damage -= damage * SubspaceResist;
            if (weapon.Tag_PD)        damage -= damage * PDResist;
            if (weapon.Tag_Flak)      damage -= damage * FlakResist;
            return damage;
        }

        private void Initialize(Vector2 pos, bool addToShieldManager = true)
        {
            ++DebugInfoScreen.ModulesCreated;

            XMLPosition = pos;
            // center of the top left 1x1 slot of this module 
            //Vector2 topLeftCenter = pos - new Vector2(256f, 256f);

            // top left position of this module
            Position = new Vector2(pos.X - 264f, pos.Y - 264f);

            // center of this module            
            Center.X = Position.X + XSIZE * 8f;
            Center.Y = Position.Y + YSIZE * 8f;

            UpdateModuleRadius();
            SetAttributesByType(addToShieldManager);

            if (Parent?.loyalty != null)
            {
                float max = ResourceManager.GetModuleTemplate(UID).HealthMax;
                HealthMax = max + max * Parent.loyalty.data.Traits.ModHpModifier;
                Health    = Math.Min(Health, HealthMax);     //Gretman (Health bug fix)
            }

            base.Initialize();
            if (ModuleType == ShipModuleType.Hangar && !IsSupplyBay)
            {
                if (OrdinanceCapacity == 0)
                {
                    OrdinanceCapacity = (short)(MaximumHangarShipSize / 2);
                    if (OrdinanceCapacity < 50)
                        OrdinanceCapacity = 50;
                }     
            }
            if (Parent == null)
                Log.Error("module parent is null");
        }

        // Refactored by RedFox - @note This method is called very heavily, so many parts have been inlined by hand
        public void UpdateEveryFrame(float elapsedTime, float cos, float sin, float tan)
        {
            // Move the module, this part is optimized according to profiler data
            ++GlobalStats.ModulesMoved;

            Vector2 offset = XMLPosition; // huge cache miss here
            offset.X += XSIZE*8f - 264f;
            offset.Y += YSIZE*8f - 264f;

            Vector2 parentCenter = Parent.Center;
            float cx = offset.X * cos - offset.Y * sin;
            float cy = offset.X * sin + offset.Y * cos;
            cx += parentCenter.X;
            cy += parentCenter.Y;
            Center.X = cx;
            Center.Y = cy;
            Center3D.X = cx;
            Center3D.Y = cy;
            Center3D.Z = tan * (256f - XMLPosition.X);

            // this can only happen if onFire is already true
            reallyFuckedUp = Parent.InternalSlotsHealthPercent < 0.5f && Health / HealthMax < 0.25f;

            HandleDamageFireTrail(elapsedTime);
            Rotation = Parent.Rotation;
        }

        private void UpdateModuleRadius()
        {
            // slightly bigger radius for better collision detection
            //Replaced [8f * 1.125f] with 9f. This is calculated for every module on every call of update() so this might add up -Gretman
            Radius = 9f * (XSIZE > YSIZE ? XSIZE : YSIZE);
        }
        // Collision test with this ShipModule. Returns TRUE if point is inside this module's
        // The collision bounds are APPROXIMATED by using radius checks. This means corners
        // are not accurately checked.
        // HitTest uses the World scene POSITION. Not module XML location
        public bool HitTestNoShields(Vector2 worldPos, float radius)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = radius + Radius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            if (dx*dx + dy*dy > r2*r2)
                return false; // definitely out of radius for SQUARE and non-square modules

            // we are a Square module? since we're already inside radius, collision happened
            if (XSIZE == YSIZE)
                return true; 

            int smaller = XSIZE <  YSIZE ? XSIZE : YSIZE; // wonder if .NET can optimize this? wanna bet no? :P
            int larger  = XSIZE >= YSIZE ? XSIZE : YSIZE;

            // now for more expensive and accurate capsule-line collision testing
            // since we can have 4x1 modules etc, so we need to construct a line+radius
            float diameter = ((float)smaller / larger) * smaller * 16.0f;

            // if high module, use forward vector, if wide module, use right vector
            Vector2 dir = Rotation.AngleToDirection();
            if (XSIZE > YSIZE) dir = dir.LeftVector();

            float offset = (larger*16.0f - diameter) * 0.5f;
            Vector2 startPos = Position - dir * offset;
            Vector2 endPos   = Position + dir * offset;
            float rayWidth   = diameter * 1.125f; // approx 18.0x instead of 16.0x
            return worldPos.RayHitTestCircle(radius, startPos, endPos, rayWidth);
        }

        public bool RayHitTestNoShield(Vector2 startPos, Vector2 endPos, float rayRadius)
        {
            Vector2 point = Center.FindClosestPointOnLine(startPos, endPos);
            return HitTestNoShields(point, rayRadius);
        }

        public bool HitTestShield(Vector2 worldPos, float radius)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = radius + ShieldHitRadius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            return dx*dx + dy*dy <= r2*r2;
        }

        public bool RayHitTestShield(Vector2 startPos, Vector2 endPos, float rayRadius, out float dist)
        {
            ++GlobalStats.DistanceCheckTotal;
            dist = Center.RayCircleIntersect(rayRadius + ShieldHitRadius, startPos, endPos);
            return dist > 0f;
        }

        public float SqDistanceToShields(Vector2 worldPos)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = ShieldHitRadius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            return dx*dx + dy*dy - r2*r2;
        }

        public float SqDistanceTo(Vector2 worldPos)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = Radius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            return dx*dx + dy*dy - r2*r2;
        }

        public static float DamageFalloff(Vector2 explosionCenter, Vector2 affectedPoint, float damageRadius, float moduleRadius, float minFalloff = 0.4f)
        {
            float splodeDis = explosionCenter.Distance(affectedPoint) - moduleRadius;
            if (splodeDis < moduleRadius) splodeDis = 0;

            return Math.Min(1.0f, (damageRadius - splodeDis) / (damageRadius + minFalloff));
        }

        // return TRUE if all damage was absorbed (damageInOut is less or equal to 0)
        public bool ApplyRadialDamage(GameplayObject damageSource, Vector2 worldHitPos, float damageRadius
            , ref float damageInOut, bool damageReduction = true)
        {
            if (damageInOut <= 0f) return true;
            float damage = damageInOut * DamageFalloff(worldHitPos, Center, damageRadius, ShieldPower >0 ? ShieldHitRadius : Radius, 0f);
            if (damage <= 0.001f)
                return damageInOut <= 0f;
            if (Empire.Universe.DebugWin != null)
                Empire.Universe.DebugWin.DrawCircle(DebugModes.SpatialManager, Center, Radius);
            DamageWithDamageDone(damageSource, damage, out float damageDone);
            if (damageReduction)
                damageInOut -= damageDone;
            return damageInOut <= 0f;
        }


        public bool Damage(GameplayObject source, float damageAmount, out float damageRemainder)
        {
            float health = Health + ShieldPower;
            bool result = Damage(source, damageAmount);
            damageRemainder = damageAmount - (health - Health - ShieldPower);
            return result;
        }

        public bool DamageWithDamageDone(GameplayObject source, float damageAmount, out float damageDone)
        {
            float health = Health + ShieldPower;
            bool result = Damage(source, damageAmount);
            damageDone = health - Health - ShieldPower;
            return result;
        }

        public override bool Damage(GameplayObject source, float damageAmount)
        {
            if (source != null)
                Parent.LastDamagedBy = source;

            Parent.InCombatTimer = 15f;
            Parent.ShieldRechargeTimer = 0f;
            //Added by McShooterz: Fix for Ponderous, now negative dodgemod increases damage taken.

            var proj = source as Projectile;
            var beam = source as Beam;
            
            //if (proj != null)
            //{
            //    if (Parent.shipData.Role == ShipData.RoleName.fighter && Parent.loyalty.data.Traits.DodgeMod < 0f)
            //    {
            //        damageAmount += damageAmount * Math.Abs(Parent.loyalty.data.Traits.DodgeMod);
            //    }
            //}
            //should not need this with targetting changes.
            //if (source is Ship ship && ship.shipData.Role == ShipData.RoleName.fighter && Parent.loyalty.data.Traits.DodgeMod < 0f)
            //    damageAmount += damageAmount * Math.Abs(Parent.loyalty.data.Traits.DodgeMod);

            // Vulnerabilities and resistances for modules, XML-defined.
            if (proj != null)
                damageAmount = ApplyResistances(proj.Weapon, damageAmount);

            if (ShieldPower < 1f || proj?.IgnoresShields == true)
            {
                //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
                if (damageAmount <= DamageThreshold)
                    damageAmount = 0f;

                //Added by McShooterz: ArmorBonus Hull Bonus
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                {
                    if (ResourceManager.HullBonuses.TryGetValue(GetParent().shipData.Hull, out HullBonus mod))
                        damageAmount *= (1f - mod.ArmoredBonus);
                }
                if (proj?.Weapon.EMPDamage > 0f)
                {
                    Parent.EMPDamage = Parent.EMPDamage + proj.Weapon.EMPDamage;
                }
                if (beam != null)
                {
                    Vector2 vel = Vector2.Normalize(beam.Source - Center);
                    if (RandomMath.RandomBetween(0f, 100f) > 90f && Parent.InFrustum)
                    {
                        Empire.Universe.flash.AddParticleThreadB(new Vector3(beam.ActualHitDestination, Center3D.Z), Vector3.Zero);
                    }
                    if (Parent.InFrustum)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Empire.Universe.sparks.AddParticleThreadB(new Vector3(beam.ActualHitDestination, Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                        }
                    }
                    if (beam.Weapon.PowerDamage > 0f)
                    {
                        Parent.PowerCurrent -= beam.Weapon.PowerDamage;
                        if (Parent.PowerCurrent < 0f)
                        {
                            Parent.PowerCurrent = 0f;
                        }
                    }
                    if (beam.Weapon.TroopDamageChance > 0f)
                    {
                        if (Parent.TroopList.Count > 0)
                        {
                            if (UniverseRandom.RandomBetween(0f, 100f) < beam.Weapon.TroopDamageChance)
                            {
                                Parent.TroopList[0].Strength = Parent.TroopList[0].Strength - 1;
                                if (Parent.TroopList[0].Strength <= 0)
                                    Parent.TroopList.RemoveAt(0);
                            }
                        }
                        else if (Parent.MechanicalBoardingDefense > 0f && RandomMath.RandomBetween(0f, 100f) < beam.Weapon.TroopDamageChance)
                        {
                            Parent.MechanicalBoardingDefense -= 1f;
                        }
                    }
                    if (beam.Weapon.MassDamage > 0f && !Parent.IsTethered() && !Parent.EnginesKnockedOut)
                    {
                        Parent.Mass += beam.Weapon.MassDamage;
                        Parent.velocityMaximum = Parent.Thrust / Parent.Mass;
                        Parent.Speed = Parent.velocityMaximum;
                        Parent.rotationRadiansPerSecond = Parent.Speed / 700f;
                    }
                    if (beam.Weapon.RepulsionDamage > 0f && !Parent.IsTethered() && !Parent.EnginesKnockedOut)
                    {
                        Parent.Velocity += ((Center - beam.Owner.Center) * beam.Weapon.RepulsionDamage) / Parent.Mass;
                    }
                }
                if (shield_power_max > 0f && ShieldPower >=1f) // && (!isExternal || quadrant <= 0))
                {
                    return false;
                }

                if (ModuleType == ShipModuleType.Armor)
                {
                    if      (beam != null) damageAmount *= beam.Weapon.EffectVsArmor;
                    else if (proj != null) damageAmount *= proj.Weapon.EffectVsArmor;
                }

                if (damageAmount > Health) Health = 0;
                else                       Health -= damageAmount;

            #if DEBUG
                if (Empire.Universe.Debug && Parent.VanityName == "Perseverance")
                {
                    if (Health < 10) // never give up, never surrender! F_F
                        Health = 10;
                }
            #endif

                if (Health >= HealthMax)
                {
                    Health = HealthMax;
                    Active = true;
                    onFire = false;
                }

                //Log.Info($"{Parent.Name} module '{UID}' dmg {damageAmount} hp {Health} by {proj?.WeaponType}");

                if (Health / HealthMax < 0.5f)
                {
                    onFire = true;
                }
                if ((Parent.Health / Parent.HealthMax) < 0.5 && Health < 0.5 * (HealthMax))
                {
                    reallyFuckedUp = true;
                }
            }
            else
            {
                if (proj != null)
                    damageAmount *= proj.Weapon.EffectVSShields;

                if (damageAmount <= shield_threshold)
                    damageAmount = 0f;

                if (damageAmount > ShieldPower)
                {
                    ShieldPower = 0;
                    Parent.UpdateShields();
                }
                else
                {
                    ShieldPower -= damageAmount;
                    Parent.UpdateShields();
                }

                //Log.Info($"{Parent.Name} shields '{UID}' dmg {damageAmount} pwr {ShieldPower} by {proj?.WeaponType}");

                if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView && Parent.InFrustum)
                {
                    if (source != null)
                        shield.Rotation = source.Rotation - 3.14159274f;
                    shield.displacement = 0f;
                    shield.texscale = 2.8f;
                    Empire.Universe.AddLight(shield.pointLight);

                    if (beam != null)
                    {
                        if (beam.Weapon.SiphonDamage > 0f)
                        {
                            ShieldPower -= beam.Weapon.SiphonDamage;
                            if (ShieldPower < 1f)
                            {
                                ShieldPower = 0f;
                            }
                            beam.Owner.PowerCurrent += beam.Weapon.SiphonDamage;
                            if (beam.Owner.PowerCurrent > beam.Owner.PowerStoreMax)
                            {
                                beam.Owner.PowerCurrent = beam.Owner.PowerStoreMax;
                            }
                        }
                        shield.Rotation = Center.RadiansToTarget(beam.Source);
                        shield.pointLight.World = Matrix.CreateTranslation(new Vector3(beam.ActualHitDestination, 0f));
                        shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        shield.pointLight.Radius = shield_radius * 2f;
                        shield.pointLight.Intensity = RandomMath.RandomBetween(4f, 10f);
                        shield.displacement       = 0f;
                        shield.Radius             = ShieldHitRadius;
                        shield.displacement       = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        shield.texscale           = 2.8f;
                        shield.texscale           = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        shield.pointLight.Enabled = true;
                        if (RandomMath.RandomBetween(0f, 100f) > 90f && Parent.InFrustum)
                        {
                            Empire.Universe.flash.AddParticleThreadA(new Vector3(beam.ActualHitDestination, Center3D.Z), Vector3.Zero);
                        }
                        if (Parent.InFrustum)
                        {
                            Vector2 vel = (beam.Source - Center).Normalized();
                            for (int i = 0; i < 20; i++)
                            {
                                Empire.Universe.sparks.AddParticleThreadA(new Vector3(beam.ActualHitDestination, Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                            }
                        }
                        if (beam.Weapon.SiphonDamage > 0f)
                        {
                            ShieldPower -= beam.Weapon.SiphonDamage;
                            if (ShieldPower < 0f)
                                ShieldPower = 0f;
                        }
                    }
                    else if (proj != null && !proj.IgnoresShields && Parent.InFrustum)
                    {
                        GameAudio.PlaySfxAsync("sd_impact_shield_01", Parent.SoundEmitter);                        
                        shield.Radius       = ShieldHitRadius;
                        shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        shield.texscale     = 2.8f;
                        shield.texscale     = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        shield.pointLight.World        = proj.WorldMatrix;
                        shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        shield.pointLight.Radius       = Radius;
                        shield.pointLight.Intensity    = 8f;
                        shield.pointLight.Enabled      = true;
                        Vector2 vel = proj.Center - Center.Normalized();
                        Empire.Universe.flash.AddParticleThreadB(new Vector3(proj.Center, Center3D.Z), Vector3.Zero);
                        for (int i = 0; i < 20; i++)
                        {
                            Empire.Universe.sparks.AddParticleThreadB(new Vector3(proj.Center, Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                        }
                    }
                }
            }
            return true;
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++DebugInfoScreen.ModulesDied;
            if (shield_power_max > 0f)
                Health = 0f;

            
            Health = 0f;
            var center = new Vector3(Center.X, Center.Y, -100f);

            SolarSystem inSystem = Parent.System;
            if (Active && Parent.InFrustum)
            {
                bool parentAlive = !Parent.dying;
                for (int i = 0; i < 30; ++i)
                {
                    Vector3 pos = parentAlive ? center : new Vector3(Parent.Center, UniverseRandom.RandomBetween(-25f, 25f));
                    Empire.Universe.explosionParticles.AddParticleThreadA(pos, Vector3.Zero);
                }
            }
            base.Die(source, cleanupOnly);
            Parent.UpdateExternalSlots(this, becameActive: false);
            int size = XSIZE * YSIZE;
            if (!cleanupOnly)
            {
                if (Parent.Active && Parent.InFrustum && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView)
                {
                    GameAudio.PlaySfxAsync("sd_explosion_module_small", Parent.SoundEmitter);
                }
                if (explodes)
                {
                    GameplayObject damageCauser = Parent.LastDamagedBy;
                    if (damageCauser == null)
                        Log.Error("LastDamagedBy is not properly set. Please check projectile damage code!");
                    UniverseScreen.SpaceManager.ExplodeAtModule(damageCauser, this, 
                        ignoreShields:true, damageAmount:size*2500, damageRadius:size*64);
                }
                if (PowerFlowMax > 0 || PowerRadius > 0)
                    Parent.NeedRecalculate = true;
            }

            int debriCount = (int)RandomMath.RandomBetween(0, size/2 + 1);
            if (debriCount != 0)
            {
                float debriScale = size * 0.1f;
                SpaceJunk.SpawnJunk(debriCount, Center, inSystem, this, 1.0f, debriScale);
            }
        }

        public Ship GetHangarShip() => hangarShip;
        public Ship GetParent()     => Parent;


        //added by gremlin boarding parties
        public void LaunchBoardingParty(Troop troop)
        {
            if (IsTroopBay && Powered)
            {
                if (hangarShip != null)
                {
                    //this.hangarShip.GetAI().State == AIState.AssaultPlanet || this.hangarShip.GetAI().State == AIState.Boarding ||
                    if (hangarShip.AI.State == AIState.ReturnToHangar || hangarShip.AI.EscortTarget != null || hangarShip.AI.OrbitTarget != null)
                        return;
                    hangarShip.DoEscort(Parent);
                    return;
                }
                if (Parent.loyalty.BoardingShuttle.Mass / 5f > Parent.Ordinance)  //fbedard: New spawning cost
                    return;
                if (hangarTimer <= 0f && hangarShip == null)
                {                    
                    hangarShip = Ship.CreateTroopShipAtPoint(Parent.loyalty.BoardingShuttle.Name, Parent.loyalty, Center, troop);
                    hangarShip.VanityName = "Assault Shuttle";
                    hangarShip.Mothership = Parent;
                    hangarShip.DoEscort(Parent);
                    hangarShip.Velocity = UniverseRandom.RandomDirection() * hangarShip.Speed + Parent.Velocity;
                    if (hangarShip.Velocity.Length() > hangarShip.velocityMaximum)
                    {
                        hangarShip.Velocity = Vector2.Normalize(hangarShip.Velocity) * hangarShip.Speed;
                    }
                    HangarShipGuid = hangarShip.guid;
                    hangarTimer = hangarTimerConstant;
                    Parent.Ordinance -= hangarShip.Mass / 5f;
                }
            }
        }

        //added by gremlin fighter rearm fix
        public void ScrambleFighters()
        {
            if (IsTroopBay || IsSupplyBay || !Powered)
                return;
            if (hangarShip != null && hangarShip.Active)
            {
                if (hangarShip.AI.State == AIState.ReturnToHangar 
                    || hangarShip.AI.HasPriorityOrder 
                    || hangarShip.AI.HasPriorityTarget
                    || hangarShip.AI.IgnoreCombat 
                    || hangarShip.AI.Target != null
                    || hangarShip.Center.InRadius(Parent.Center, Parent.SensorRange)
                ) return;
                hangarShip.DoEscort(Parent);
                return;
            }
            if (hangarTimer > 0f || (hangarShip != null && (hangarShip == null || hangarShip.Active)))
                return;

            string startingscout = Parent.loyalty.data.StartingShip;
           
            Ship ship = ResourceManager.GetShipTemplate(hangarShipUID, false); 
            if (!Parent.loyalty.isFaction && (hangarShipUID == startingscout 
                                                   || !Parent.loyalty.ShipsWeCanBuild.Contains(hangarShipUID)))
            {
                ship = ResourceManager.GetShipTemplate(startingscout);
                foreach (string shipsWeCanBuild in Parent.loyalty.ShipsWeCanBuild)
                {

                    if (!PermittedHangarRoles.Contains(ResourceManager.GetShipTemplate(shipsWeCanBuild).shipData.GetRole()) 
                        || ResourceManager.GetShipTemplate(shipsWeCanBuild).Size > MaximumHangarShipSize)
                    {
                        continue;
                    }
                    Ship tempship = ResourceManager.GetShipTemplate(shipsWeCanBuild);
                    if (ship.BaseStrength  < tempship.BaseStrength || ship.Size < tempship .Size)
                        ship = tempship;
                }
                hangarShipUID = ship.Name;
            }
            if (ship == null || ship.Mass / 5f > Parent.Ordinance)  //fbedard: New spawning cost
                return;

            SetHangarShip(Ship.CreateShipFromHangar(ship.Name, Parent.loyalty, Center, Parent));

            hangarShip.DoEscort(Parent);
            hangarShip.Velocity = UniverseRandom.RandomDirection() * GetHangarShip().Speed + Parent.Velocity;
            if (hangarShip.Velocity.Length() > hangarShip.velocityMaximum)
            {
                hangarShip.Velocity = Vector2.Normalize(hangarShip.Velocity) * hangarShip.Speed;
            }
            hangarShip.Mothership = Parent;
            HangarShipGuid = GetHangarShip().guid;

            hangarTimer = hangarTimerConstant;
            Parent.Ordinance -= hangarShip.Mass / 5f;
        }

        public void SetAttributesByType(bool addToShieldManager = true)
        {
            switch (ModuleType)
            {
                case ShipModuleType.Turret:
                    ConfigWeapon(true);
                    InstalledWeapon.isTurret = true;
                    break;
                case ShipModuleType.MainGun:
                    ConfigWeapon(true);
                    InstalledWeapon.isMainGun = true;
                    break;
                case ShipModuleType.MissileLauncher:
                    ConfigWeapon(true);
                    break;
                case ShipModuleType.Colony:
                    Parent.isColonyShip = true;
                    break;
                case ShipModuleType.Bomb:
                    Parent.BombBays.Add(this);
                    break;
                case ShipModuleType.Drone:
                    ConfigWeapon(true);
                    break;
                case ShipModuleType.Spacebomb:
                    ConfigWeapon(true);
                    break;
            }
            Health = HealthMax;
            if (shield_power_max > 0.0 && addToShieldManager)
                shield = ShieldManager.AddShield(this, Rotation, Center);
            if (IsSupplyBay)
                Parent.IsSupplyShip = true;
        }

        public void SetAttributesNoParent()
        {
            switch (ModuleType)
            {
                case ShipModuleType.Turret:
                    ConfigWeapon(false);
                    InstalledWeapon.isTurret = true;
                    break;
                case ShipModuleType.MainGun:
                    ConfigWeapon(false);
                    InstalledWeapon.isMainGun = true;
                    break;
                case ShipModuleType.MissileLauncher:
                    ConfigWeapon(false);
                    break;
                case ShipModuleType.Drone:
                    ConfigWeapon(false);
                    break;
                case ShipModuleType.Spacebomb:
                    ConfigWeapon(false);
                    break;
            }
            Health = HealthMax;
        }

        private void ConfigWeapon(bool addToParent)
        {
            InstalledWeapon = ResourceManager.CreateWeapon(ResourceManager.GetModuleTemplate(UID).WeaponType);
            InstalledWeapon.Module = this;
            InstalledWeapon.Owner  = Parent;
            InstalledWeapon.Center = Center;
            isWeapon = true;
            if (addToParent)
                Parent.Weapons.Add(InstalledWeapon);
        }

        public void SetHangarShip(Ship ship)
        {
            hangarShip = ship;
            if (ship != null)
                HangarShipGuid = ship.guid;  //fbedard: save mothership
        }

        public void SetParent(Ship p)
        {
            Parent = p;
        }

        public override void Update(float elapsedTime)
        {
            if (Health > 0f && !Active)
            {
                Active = true;
                Parent.shipStatusChanged = true;
                Parent.UpdateExternalSlots(this, becameActive: true);
                Parent.NeedRecalculate = true;
            }
            if (Health <= 0f && Active)
            {
                Die(LastDamagedBy, false);
                Parent.shipStatusChanged = true;
            }
            if (Health >= HealthMax)
            {
                Health = HealthMax;
                onFire = false;
            }

            BombTimer -= elapsedTime;
            UpdateModuleRadius();

            if (Active && ModuleType == ShipModuleType.Hangar ) //(this.hangarShip == null || !this.hangarShip.Active) && 
                hangarTimer -= elapsedTime;
            //Shield Recharge
            float shieldMax = GetShieldsMax();
            if (Active && Powered && ShieldPower < shieldMax)
            {
                if (Parent.ShieldRechargeTimer > shield_recharge_delay)
                    ShieldPower += shield_recharge_rate * elapsedTime;
                else if (ShieldPower > 0)
                    ShieldPower += shield_recharge_combat_rate * elapsedTime;
                if (ShieldPower > shieldMax)
                    ShieldPower = shieldMax;
            }
            if (ShieldPower < 0f)
            {
                ShieldPower = 0f;
            }
            if (TransporterTimer > 0)
                TransporterTimer -= elapsedTime;

            base.Update(elapsedTime);
        }

        private void HandleDamageFireTrail(float elapsedTime)
        {
            if (Parent.InFrustum && Active && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (reallyFuckedUp)
                {
                    if (trailEmitter == null) trailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(50f, Center3D);
                    if (flameEmitter == null) flameEmitter = Empire.Universe.flameParticles.NewEmitter(80f, Center3D);
                    trailEmitter.Update(elapsedTime, Center3D);
                    flameEmitter.Update(elapsedTime, Center3D);
                }
                else if (onFire)
                {
                    if (trailEmitter == null)     trailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(50f, Center3D);
                    if (firetrailEmitter == null) firetrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(60f, Center3D);
                    trailEmitter.Update(elapsedTime, Center3D);
                    firetrailEmitter.Update(elapsedTime, Center3D);
                }
            }
            else if (trailEmitter != null) // destroy immediately when out of vision range
            {
                trailEmitter     = null; // tried Disposing these, but got a crash... so just null them
                firetrailEmitter = null;
                flameEmitter     = null;
            }
        }

        public void UpdateWhileDying(float elapsedTime)
        {
            Center3D = Parent.Center.ToVec3(UniverseRandom.RandomBetween(-25f, 25f));
            HandleDamageFireTrail(elapsedTime);
        }

        public void Repair(float repairAmount)
        {
            Health += repairAmount;
            if (Health < HealthMax) return;

            Health = HealthMax;
        }

        public float GetShieldsMax()
        {
            if (GlobalStats.ActiveModInfo == null)
                return shield_power_max;

            float value = shield_power_max + shield_power_max * Parent.loyalty?.data.ShieldPowerMod ?? 0;
            if (!GlobalStats.ActiveModInfo.useHullBonuses)
                return value;

            if (ResourceManager.HullBonuses.TryGetValue(GetParent().shipData.Hull, out HullBonus mod))
                value += shield_power_max * mod.ShieldBonus;
            return value;
        }

        // Used for picking best repair candidate
        public int ModulePriority
        {
            get
            {
                switch (ModuleType)
                {
                    case ShipModuleType.Command:      return 0;
                    case ShipModuleType.PowerPlant:   return 1;
                    case ShipModuleType.PowerConduit: return 2;
                    case ShipModuleType.Engine:       return 3;
                    case ShipModuleType.Shield:       return 4;
                    case ShipModuleType.Armor:        return 6;
                    default:                          return 5;
                }
            }
        }

        public Color GetHealthStatusColor()
        {
            float healthPercent = Health / HealthMax;

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
            float healthPercent = Health / HealthMax;

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
            def += shield_power_max * ((shield_radius * .05f) / slotCount);
            //(module.shield_power_max+  module.shield_radius +module.shield_recharge_rate) / slotCount ;
            def += HealthMax * ((ModuleType == ShipModuleType.Armor ? (XSIZE) : 1f) / (slotCount * 4));
            return def;
        }

        public float CalculateModuleOffense()
        {
            float off = 0f;
            if (InstalledWeapon != null)
            {
                //weapons = true;
                Weapon w = InstalledWeapon;

                //Doctor: The 25% penalty to explosive weapons was presumably to note that not all the damage is applied to a single module - this isn't really weaker overall, though
                //and unfairly penalises weapons with explosive damage and makes them appear falsely weaker.
                off += (!w.isBeam ? (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) : w.DamageAmount * 18f);

                //Doctor: Guided weapons attract better offensive rating than unguided - more likely to hit. Setting at flat 25% currently.
                if (w.Tag_Guided)
                    off *= 1.25f;

                //Doctor: Higher range on a weapon attracts a small bonus to offensive rating. E.g. a range 2000 weapon gets 5% uplift vs a 5000 range weapon 12.5% uplift. 
                off *= (1 + (w.Range / 40000));

                //Doctor: Here follows multipliers which modify the perceived offensive value of weapons based on any modifiers they may have against armour and shields
                //Previously if e.g. a rapid-fire cannon only did 20% damage to armour, it could have amuch higher off rating than a railgun that had less technical DPS but did double armour damage.
                if (w.EffectVsArmor < 1)
                {
                    if (w.EffectVsArmor > 0.75f)      off *= 0.9f;
                    else if (w.EffectVsArmor > 0.5f)  off *= 0.85f;
                    else if (w.EffectVsArmor > 0.25f) off *= 0.8f;
                    else                              off *= 0.75f;
                }
                if (w.EffectVsArmor > 1)
                {
                    if (w.EffectVsArmor > 2.0f)      off *= 1.5f;
                    else if (w.EffectVsArmor > 1.5f) off *= 1.3f;
                    else                             off *= 1.1f;
                }
                if (w.EffectVSShields < 1)
                {
                    if (w.EffectVSShields > 0.75f)      off *= 0.9f;
                    else if (w.EffectVSShields > 0.5f)  off *= 0.85f;
                    else if (w.EffectVSShields > 0.25f) off *= 0.8f;
                    else                                off *= 0.75f;
                }
                if (w.EffectVSShields > 1)
                {
                    if (w.EffectVSShields > 2f)        off *= 1.5f;
                    else if (w.EffectVSShields > 1.5f) off *= 1.3f;
                    else                               off *= 1.1f;
                }

                //Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
                off *= w.OffPowerMod;

                if (off > 0f && (w.TruePD || w.Range < 1000))
                {
                    float range = 0f;
                    if (w.Range < 1000)
                        range = (1000f - w.Range) * .01f;
                    off /= (2 + range);
                }
                if (w.EMPDamage > 0) off += w.EMPDamage * (1f / w.fireDelay) * .2f;
            }
            if (hangarShipUID != null && !IsSupplyBay && !IsTroopBay)
            {
                if (ResourceManager.GetShipTemplate(hangarShipUID, out Ship thangarShip))
                {
                    off += (thangarShip.BaseStrength > 0f) ? thangarShip.BaseStrength : thangarShip.CalculateBaseStrength();
                }
                else off += 100f;
            }
            return off;
        }

        private void ApplyModuleOrientation(ShipDesignScreen.ActiveModuleState state)
        {
            ShipModule activeModule = this;
          
            int x = activeModule.XSIZE;
            int y = activeModule.YSIZE;
            switch (state)
            {

                case ShipDesignScreen.ActiveModuleState.Right:
                    activeModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    activeModule.YSIZE = x;
                    return;
                case ShipDesignScreen.ActiveModuleState.Left:
                {
                    activeModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    activeModule.YSIZE = x; // These are swapped because if the module is facing left or right, then the length is now the height, and vice versa                                            
                    return;
                }

                case ShipDesignScreen.ActiveModuleState.Normal:
                    break;
                case ShipDesignScreen.ActiveModuleState.Rear:                    
                    break;
                default:
                {
                    return;
                }
            }
        }

        public override Vector2 JitterPosition() => Parent?.JitterPosition() ?? base.JitterPosition(); 
        
        public override string ToString() => $"{UID}  {Id}  {Position}  World={Center}  Ship={Parent?.Name}";
    }
}