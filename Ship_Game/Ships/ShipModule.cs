using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public sealed class ShipModule : GameplayObject
    {
        //private static int TotalModules = 0;
        //public int ID = ++TotalModules;
        public ShipModuleFlyweight Flyweight; //This is where all the other member variables went. Having this as a member object
                                              //allows me to instance the variables inside it, so they are not duplicated. This
                                              //can offer much better memory usage since ShipModules are so numerous.     -Gretman
        private ParticleEmitter TrailEmitter;
        private ParticleEmitter FireTrailEmitter;
        private ParticleEmitter FlameEmitter;
        private ParticleEmitter SmokeEmitter;
        private ParticleEmitter SparksEmitter;
        private ParticleEmitter LightningEmitter;
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
        public Shield GetShield => shield;
        public string hangarShipUID;
        private Ship hangarShip;
        public Guid HangarShipGuid;
        public float hangarTimer;
        public bool isWeapon;
        public Weapon InstalledWeapon;
        public short OrdinanceCapacity;
        private bool OnFire;
        private bool ReallyFuckedUp;
        private Vector3 Center3D;
        public Vector3 GetCenter3D => Center3D;


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
        public int ModuleTargettingValue         => TargetValue
                                          //+ (isExternal ? -5 : 0)         // external modules are less critical
                                          + (Health < HealthMax ? 1 : 0); // prioritize already damaged modules        
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
        public bool FightersOnly                 => Flyweight.FightersOnly;
        public bool DroneModule                  => Flyweight.DroneModule;
        public bool FighterModule                => Flyweight.FighterModule;
        public bool CorvetteModule               => Flyweight.CorvetteModule;
        public bool FrigateModule                => Flyweight.FrigateModule;
        public bool DestroyerModule              => Flyweight.DestroyerModule;
        public bool CruiserModule                => Flyweight.CruiserModule;
        public bool CarrierModule                => Flyweight.CarrierModule;
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
        public float Cargo_Capacity                => Flyweight.Cargo_Capacity;
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
        public float numberOfColonists           => Flyweight.numberOfColonists;
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
        public bool IndirectPower                => Flyweight.IndirectPower;
        public bool isPowerArmour                => Flyweight.isPowerArmour;
        public bool isBulkhead                   => Flyweight.isBulkhead;
        public int TargetTracking                => Flyweight.TargetTracking;
        public int FixedTracking                 => Flyweight.FixedTracking;
        public int ExplosionDamage               => Flyweight.ExplosionDamage;
        public int ExplosionRadius               => Flyweight.ExplosionRadius;
        public bool IsRotatable                  => (bool)Flyweight.IsRotable;
        public bool IsWeapon                     => ModuleType == ShipModuleType.Spacebomb
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
        public Texture2D ModuleTexture => ResourceManager.Texture(IconTexturePath);
        public bool HasColonyBuilding => ModuleType == ShipModuleType.Colony || DeployBuildingOnColonize.NotEmpty();

        private ShipModule() : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = ShipModuleFlyweight.Empty;
        }

        private ShipModule(ShipModule_Deserialize s) : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight               = new ShipModuleFlyweight(s);
            XSIZE                   = s.XSIZE;
            YSIZE                   = s.YSIZE;
            Mass                    = s.Mass;
            Powered                 = s.Powered;
            FieldOfFire             = s.FieldOfFire;
            Facing                  = s.facing;
            XMLPosition             = s.XMLPosition;
            HealthMax               = s.HealthMax;
            WeaponType              = s.WeaponType;
            NameIndex               = s.NameIndex;
            DescriptionIndex        = s.DescriptionIndex;
            Restrictions            = s.Restrictions;
            ShieldPower             = s.shield_power;
            hangarShipUID           = s.hangarShipUID;
            hangarTimer             = s.hangarTimer;
            isWeapon                = s.isWeapon;
            OrdinanceCapacity       = s.OrdinanceCapacity;
            BombTimer               = s.BombTimer;
            ModuleType              = s.ModuleType;
            IconTexturePath         = s.IconTexturePath;
            //isExternal            = s.isExternal; // I think it's safer to let ship externals init handle this...
            TargetValue             = s.TargetValue;
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
            module.TargetValue += module.ModuleType == ShipModuleType.Armor ? -1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Bomb ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Command ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Countermeasure ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Drone ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Engine ? 2 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.FuelCell ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Hangar ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.MainGun ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.MissileLauncher ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Ordnance ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.PowerPlant ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Sensors ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Shield ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Spacebomb ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Special ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Turret ? 1 : 0;
            module.TargetValue += module.explodes ? 2 : 0;
            module.TargetValue += module.isWeapon ? 1 : 0;
            return module;
        }


        // Fat Bastard - Shield Resistance  is now working
        private float ApplyShieldResistances(Weapon weapon, float damagemodifier)
        {
            if (weapon.Tag_Kinetic)             damagemodifier = damagemodifier * (1f - shield_kinetic_resist);
            else if (weapon.Tag_Energy)         damagemodifier = damagemodifier * (1f - shield_energy_resist);
            else if (weapon.Tag_Beam)           damagemodifier = damagemodifier * (1f - shield_beam_resist);
            else if (weapon.Tag_Missile)        damagemodifier = damagemodifier * (1f - shield_missile_resist);
            //else if (weapon.Tag_Explosive)    damagemodifier = damagemodifier * (1f - shield_explosive_resist);
            //else if (weapon.Tag_Flak)         damage -= damage * shield_flak_resist;
            //else if (weapon.Tag_Hybrid)       damage -= damage * shield_hybrid_resist;
            //else if (weapon.Tag_Railgun)      damage -= damage * shield_railgun_resist;
            //else if (weapon.Tag_Subspace)     damage -= damage * shield_subspace_resist;
            //else if (weapon.Tag_Warp)         damage -= damage * shield_warp_resist;
            return damagemodifier;
        }

        private float ApplyResistances(Weapon weapon, float damagemodifier,bool internalexplosion)
        {
            /* Using else if since every weapon should be tagged with one of the top types of projectiles (Kinetic, Beam, Energy, Missile or Torpedo.
            all the rest simply doesnt matter and wastes time being called every time there is a hit. there is no need to make more methods of this since its rather a simple one.
            Modules will have one or more of the types of resist below.
             */
            if (internalexplosion) // damage from reactor explosion. so only explosive resist applies
            {
                damagemodifier = damagemodifier * (1f - ExplosiveResist);
                return damagemodifier;
            }
            if (weapon.Tag_Explosive)           damagemodifier = damagemodifier * (1f - ExplosiveResist);
            if (weapon.Tag_Kinetic)             damagemodifier = damagemodifier * (1f - KineticResist);
            else if (weapon.Tag_Beam)           damagemodifier = damagemodifier * (1f - BeamResist);
            else if (weapon.Tag_Energy)         damagemodifier = damagemodifier * (1f - EnergyResist);
            else if (weapon.Tag_Missile)        damagemodifier = damagemodifier * (1f - MissileResist);
            else if (weapon.Tag_Torpedo)        damagemodifier = damagemodifier * (1f - TorpedoResist);
            //else if (weapon.Tag_Guided)       damagemodifier = damagemodifier * (1f - GuidedResist);
            //else if (weapon.Tag_Cannon)       damagemodifier = damagemodifier * (1f - CannonResist);
            //else if (weapon.Tag_Hybrid)       damagemodifier = damagemodifier * (1f - HybridResist);
            //else if (weapon.Tag_Intercept)    damagemodifier = damagemodifier * (1f - InterceptResist);
            //else if (weapon.Tag_Railgun)      damagemodifier = damagemodifier * (1f - RailgunResist);
            //else if (weapon.Tag_SpaceBomb)    damagemodifier = damagemodifier * (1f - SpaceBombResist);
            //else if (weapon.Tag_Bomb)         damagemodifier = damagemodifier * (1f - BombResist);
            //else if (weapon.Tag_BioWeapon)    damagemodifier = damagemodifier * (1f - BioWeaponResist);
            //else if (weapon.Tag_Drone)        damagemodifier = damagemodifier * (1f - DroneResist);
            //else if (weapon.Tag_Warp)         damagemodifier = damagemodifier * (1f - WarpResist);
            //else if (weapon.Tag_Subspace)     damagemodifier = damagemodifier * (1f - SubspaceResist);
            //else if (weapon.Tag_PD)           damagemodifier = damagemodifier * (1f - PDResist);
            //else if (weapon.Tag_Flak)         damagemodifier = damagemodifier * (1f - FlakResist);
            return damagemodifier;
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
                Health = Math.Min(Health, HealthMax);     //Gretman (Health bug fix)
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
            offset.X            += XSIZE * 8f - 264f;
            offset.Y            += YSIZE * 8f - 264f;
            Vector2 parentCenter = Parent.Center;
            float cx             = offset.X * cos - offset.Y * sin;
            float cy             = offset.X * sin + offset.Y * cos;
            cx                  += parentCenter.X;
            cy                  += parentCenter.Y;
            Center.X             = cx;
            Center.Y             = cy;
            Center3D.X           = cx;
            Center3D.Y           = cy;
            Center3D.Z           = tan * (256f - XMLPosition.X);

            // this can only happen if onFire is already true
            this.ReallyFuckedUp = Parent.InternalSlotsHealthPercent < 0.5f && Health / HealthMax < 0.25f;

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
            if (dx * dx + dy * dy > r2 * r2)
                return false; // definitely out of radius for SQUARE and non-square modules

            // we are a Square module? since we're already inside radius, collision happened
            if (XSIZE == YSIZE)
                return true;

            int smaller = XSIZE < YSIZE ? XSIZE : YSIZE; // wonder if .NET can optimize this? wanna bet no? :P
            int larger = XSIZE >= YSIZE ? XSIZE : YSIZE;

            // now for more expensive and accurate capsule-line collision testing
            // since we can have 4x1 modules etc, so we need to construct a line+radius
            float diameter = ((float)smaller / larger) * smaller * 16.0f;

            // if high module, use forward vector, if wide module, use right vector
            Vector2 dir = Rotation.AngleToDirection();
            if (XSIZE > YSIZE) dir = dir.LeftVector();

            float offset = (larger * 16.0f - diameter) * 0.5f;
            Vector2 startPos = Position - dir * offset;
            Vector2 endPos = Position + dir * offset;
            float rayWidth = diameter * 1.125f; // approx 18.0x instead of 16.0x
            return worldPos.RayHitTestCircle(radius, startPos, endPos, rayWidth);
        }

        public bool RayHitTestNoShield(Vector2 startPos, Vector2 endPos, float rayRadius)
        {
            Vector2 point = Center.FindClosestPointOnLine(startPos, endPos);
            return HitTestNoShields(point, rayRadius);
        }

        public bool HitTestShield(Vector2 worldPos, float radius)
        {
            if (ShieldPower < 1f) return false;
            ++GlobalStats.DistanceCheckTotal;
            float r2 = radius + ShieldHitRadius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            return dx * dx + dy * dy <= r2 * r2;
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
            return dx * dx + dy * dy - r2 * r2;
        }

        public float SqDistanceTo(Vector2 worldPos)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = Radius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            return dx * dx + dy * dy - r2 * r2;
        }

        public static float DamageFalloff(Vector2 explosionCenter, Vector2 affectedPoint, float damageRadius, float moduleRadius, float minFalloff = 0.4f)
        {
            float splodeDis = explosionCenter.Distance(affectedPoint) - moduleRadius;
            if (splodeDis < moduleRadius) splodeDis = 0;

            return Math.Min(1.0f, (damageRadius - splodeDis) / (damageRadius + minFalloff));
        }

        // return TRUE if all damage was absorbed (damageInOut is less or equal to 0)
        public bool ApplyRadialDamage(GameplayObject damageSource, Vector2 worldHitPos, float damageRadius
            , ref float damageInOut, bool damageReduction = true, bool internalExplosion = false)
        {
            if (damageInOut <= 0f) return true;
            float damage = damageInOut * DamageFalloff(worldHitPos, Center, damageRadius, ShieldPower > 0 ? ShieldHitRadius : Radius, 0f);
            if (damage <= 0.001f)
                return damageInOut <= 0f;
            if (Empire.Universe.DebugWin != null)
                Empire.Universe.DebugWin.DrawCircle(DebugModes.SpatialManager, Center, Radius);
            DamageWithDamageDone(damageSource, damage, out float damageDone, internalExplosion);
            if (damageReduction)
                damageInOut -= damageDone;
            return damageInOut <= 0f;
        }
        public void DebugDamageCircle()
        {
            Empire.Universe?.DebugWin?.DrawGPObjects(DebugModes.Targeting, this, Parent);
        }

        public void Damage(GameplayObject source, float damageAmount, out float damageRemainder)
        {
            float health            = Health + ShieldPower;
            float damageModifier    = Damage(source, damageAmount);

            DebugDamageCircle();
            if ( Health > 0)
            {
                damageRemainder = 0f;
                return;
            }
            damageRemainder = damageAmount * damageModifier - (health - Health - ShieldPower);
            if (damageModifier <= 1f)
                return;
            damageRemainder /= damageModifier;  // undo modifier from the damage remained since the next module might not have these vulnerabilites
            damageRemainder = (int)Math.Round(damageRemainder, 0);
        }

        public void DamageWithDamageDone(GameplayObject source, float damageAmount, out float damageDone, bool internalexplosion = false)
        {
            float health = Health + ShieldPower;
            float damageModifier = Damage(source, damageAmount, internalexplosion);
            if (Health > 0)
            {
                damageDone = health - Health - ShieldPower;
                return;
            }
            if (damageModifier >= 0.01 || damageModifier <= -0.01) damageDone = (health - Health - ShieldPower) / damageModifier; // add the dmg resisted
            else damageDone = damageAmount; // everything was absorbed in this module
        }

        public override float Damage(GameplayObject source, float damageAmount, bool internalexplosion = false)
        {
            if (source != null) Parent.LastDamagedBy = source;
            Parent.InCombatTimer        = 15f;
            Parent.ShieldRechargeTimer  = 0f;
            float damageModifier        = 1f;

            var beam = source as Beam;
            Projectile proj = null; 
            if (beam == null)
                proj = source as Projectile;

            damageModifier = CalcDamageModifier(proj, beam, ShieldPower, damageModifier, internalexplosion);
            if (ShieldPower < 1f || proj?.IgnoresShields == true)
            {
                damageAmount *= damageModifier;
                damageAmount  = CalcDamageThreshold(proj, damageAmount);
                CalcEMPDamage(proj);
                CalcBeamDamageTypes(beam);
                if (Parent.InFrustum)
                {
                    beam?.CreateHitParticles(Center3D.Z);
                    if (proj?.Explodes == false)
                        proj.CreateHitParticles(damageAmount, Center3D);
                }
                DebugPerseveranceNoDamage();
                Health = ApplyModuleDamage(damageAmount, Health, HealthMax);
                //Log.Info($"{Parent.Name} module '{UID}' dmg {damageAmount} hp {ealth} by {proj?.WeaponType}");
            }
            else // damaging shields
            {
                damageAmount *= damageModifier;
                damageAmount  = CalcShieldDamageThreshold(proj, damageAmount);
                ShieldPower   = ApplyShieldDamage(ShieldPower, damageAmount);
                //Log.Info($"{Parent.Name} shields '{UID}' dmg {damageAmount} pwr {ShieldPower} by {proj?.WeaponType}");
                if (source != null) ShieldPower = CalcSiphonDamage(beam, ShieldPower);
                Parent.UpdateShields();
                if (Empire.Universe.viewState > UniverseScreen.UnivScreenState.ShipView || !Parent.InFrustum) return damageModifier;
                if (beam != null) shield.HitShield(this, beam);
                else if (proj != null && !proj.IgnoresShields) shield.HitShield(this, proj);
            }
            return damageModifier;
        }

        private float CalcDamageModifier(Projectile proj, Beam beam, float shieldpower, float damageModifier, bool internalexplosion = false)
        {

            Weapon weapon = beam?.Weapon ?? proj?.Weapon;

            // check for the projectiles effects vs shields or armor
            if (shieldpower >= 1f)
            {
                damageModifier = CalcEffectVsShields(damageModifier, weapon);
                if (weapon != null) damageModifier = ApplyShieldResistances(weapon, damageModifier);
            }
            else
            {
                damageModifier = CalcEffectVsArmor(damageModifier, weapon);
                damageModifier = CalcArmorBonus(damageModifier);
                if (weapon != null) damageModifier = ApplyResistances(weapon, damageModifier, internalexplosion);
            }
            return damageModifier;
        }

        private float CalcEffectVsArmor(float damagemodifier, Weapon weapon)
        {
               
            if (ModuleType != ShipModuleType.Armor) return damagemodifier;
            float effectVsArmor = weapon?.EffectVsArmor ?? 1;
            return damagemodifier * effectVsArmor;
        }

        private float CalcEffectVsShields(float damagemodifier, Weapon weapon)
        {

            float effectVsShields = weapon?.EffectVSShields ?? 1;
            return damagemodifier * effectVsShields;
        }

        private float CalcArmorBonus(float damagemodifier)
        {
            //Added by McShooterz: ArmorBonus Hull Bonus
            //if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useHullBonuses) return damagemodifier;
            if (GlobalStats.ActiveModInfo?.useHullBonuses != true) return damagemodifier;
            if (ResourceManager.HullBonuses.TryGetValue(this.GetParent().shipData.Hull, out HullBonus mod))
                damagemodifier *= (1f - mod.ArmoredBonus);
            return damagemodifier;
        }

        private float CalcDamageThreshold(Projectile proj, float damageamount)
        {
            //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
            if (proj == null) return damageamount; // Fat Bastard: wont work on beams
            if (damageamount <= this.DamageThreshold) damageamount = 0f;
            return damageamount;
        }
        private float CalcShieldDamageThreshold(Projectile proj, float damageamount)
        {
            //Doc: If the resistance-modified damage amount is less than an shield's damage threshold, no damage is applied.
            if (proj == null) return damageamount; // Fat Bastard: wont work on beams
            if (damageamount <= this.shield_threshold) damageamount = 0f;
            return damageamount;
        }

        private void CalcEMPDamage(Projectile proj)
        {
            if (proj?.Weapon.EMPDamage > 0f) Parent.EMPDamage = Parent.EMPDamage + proj.Weapon.EMPDamage;
        }

        private void CalcBeamDamageTypes(Beam beam)
        {
            if (beam == null) return;
            BeamPowerDamage(beam);
            BeamTroopDamage(beam);
            BeamMassDamage(beam);
            BeamRepulsionDamage(beam);
        }

        private void BeamPowerDamage(Beam beam)
        {
            if (!(beam.Weapon.PowerDamage > 0f)) return;
            Parent.PowerCurrent -= beam.Weapon.PowerDamage;
            if (Parent.PowerCurrent < 0f)
                Parent.PowerCurrent = 0f;
        }
        private void BeamTroopDamage(Beam beam)
        {
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
                    Parent.MechanicalBoardingDefense -= 1f;
            }
        }

        private void BeamMassDamage(Beam beam)
        {
            if (!(beam.Weapon.MassDamage > 0f) || Parent.IsTethered() || Parent.EnginesKnockedOut) return;
            Parent.Mass += beam.Weapon.MassDamage;
            Parent.velocityMaximum = Parent.Thrust / Parent.Mass;
            Parent.Speed = Parent.velocityMaximum;
            Parent.rotationRadiansPerSecond = Parent.Speed / 700f;
        }

        private void BeamRepulsionDamage(Beam beam)
        {
            if ((beam?.Weapon?.RepulsionDamage ?? 0f) < 1) return;
            if (Parent.IsTethered() || Parent.EnginesKnockedOut) return;
            if (beam?.Owner != null && beam.Weapon != null)
                Parent.Velocity += ((Center - beam.Owner.Center) * beam.Weapon.RepulsionDamage) / Parent.Mass;
        }

        private void DebugPerseveranceNoDamage()
        {
#if DEBUG
            if (!Empire.Universe.Debug || Parent.VanityName != "Perseverance") return;
            if (Health< 10) // never give up, never surrender! F_F
                Health = 10;
#endif
        }

        private float ApplyModuleDamage(float damageAmount, float health,float healthMax)
        {
            if (damageAmount > health)
                health = 0;
            else health -= damageAmount;

            if (health >= healthMax)
            {
                health = healthMax;
                Active = true;
                this.OnFire = false;
            }
            if (health / healthMax < 0.5f)
                this.OnFire = true;
            if ((Parent.Health / Parent.HealthMax) < 0.5 && health < 0.5 * (healthMax))
                this.ReallyFuckedUp = true;
            return health;
        }

        private float ApplyShieldDamage(float shieldpower, float damageamount)
        {
            if (damageamount > shieldpower)
                shieldpower = 0;
            else
                shieldpower -= damageamount;
            Parent.UpdateShields();
            return shieldpower;
        }

        private float CalcSiphonDamage(Beam beam, float shieldpower)
        {
            if (beam?.Weapon.SiphonDamage > 0f)
            {
                shieldpower -= beam.Weapon.SiphonDamage;
                if (shieldpower < 1f) shieldpower = 0f;
                beam.Owner.PowerCurrent += beam.Weapon.SiphonDamage;
                if (beam.Owner.PowerCurrent > beam.Owner.PowerStoreMax) beam.Owner.PowerCurrent = beam.Owner.PowerStoreMax;

            }
            return shieldpower;
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
            int size = Area;
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
                        ignoreShields: true, damageAmount: ExplosionDamage, damageRadius: ExplosionRadius, internalExplosion:true);
                }            
            }
            if (PowerFlowMax > 0 || PowerRadius > 0)
                Parent.NeedRecalculate = true;
            int debriCount = (int)RandomMath.RandomBetween(0, size / 2 + 1);
            if (debriCount == 0) return;
            float debriScale = size * 0.1f;
            SpaceJunk.SpawnJunk(debriCount, Center, inSystem, this, 1.0f, debriScale);
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
                        hangarShip.Velocity = Vector2.Normalize(hangarShip.Velocity) * hangarShip.Speed;

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
                )
                    return;
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
                    var shipWeCanBuild = ResourceManager.GetShipTemplate(shipsWeCanBuild);
                    if (shipWeCanBuild.Size > MaximumHangarShipSize) continue;

                    if (!PermittedHangarRoles.Contains(shipWeCanBuild.shipData.GetRole()))
                    {
                        continue;
                    }
                    
                    if (ship.BaseStrength < shipWeCanBuild.BaseStrength || ship.Size < shipWeCanBuild.Size)
                        ship = shipWeCanBuild;
                }
                hangarShipUID = ship.Name;
            }
            if (ship == null || (!Parent.loyalty.isFaction && ship.Mass / 5f > Parent.Ordinance))  //fbedard: New spawning cost
                return;

            SetHangarShip(Ship.CreateShipFromHangar(this, Parent.loyalty, Parent.Center + LocalCenter, Parent));

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
            InstalledWeapon.Owner = Parent;
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
                this.OnFire = false;
            }

            BombTimer -= elapsedTime;
            UpdateModuleRadius();

            if (Active && ModuleType == ShipModuleType.Hangar) //(this.hangarShip == null || !this.hangarShip.Active) && 
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

        // This code is a 'hot spot'. avoid any method calls here and duplicate code if needed. Its recommended not to change anything at all here.
        private void HandleDamageFireTrail(float elapsedTime)
        {
            if (Parent.InFrustum && Active && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (ReallyFuckedUp)
                {
                    if (TrailEmitter == null) TrailEmitter = Empire.Universe.projectileTrailParticles.NewEmitter(50f, Center3D);
                    if (FlameEmitter == null) FlameEmitter = Empire.Universe.flameParticles.NewEmitter(80f, Center3D);
                    TrailEmitter.Update(elapsedTime, Center3D);
                    FlameEmitter.Update(elapsedTime, Center3D);
                    // this block is added for more interesting damage effects, hopefully it wont effect performance too much
                    if (XSIZE * YSIZE >= 9)
                    {
                        if (SmokeEmitter == null) SmokeEmitter = Empire.Universe.explosionSmokeParticles.NewEmitter(40f, Center3D);
                        SmokeEmitter.Update(elapsedTime, Center3D);
                    }
                    if (ModuleType != ShipModuleType.PowerPlant) return;
                    if (LightningEmitter == null) LightningEmitter = Empire.Universe.lightning.NewEmitter(1f, Center3D);
                    LightningEmitter.Update(elapsedTime, Center3D);
                }
                else if (OnFire)
                {
                    if (TrailEmitter     == null) TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(50f, Center3D);
                    if (FireTrailEmitter == null) FireTrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(60f, Center3D);
                    TrailEmitter.Update(elapsedTime, Center3D);
                    FireTrailEmitter.Update(elapsedTime, Center3D);
                    // this block is added for more interesting damage effects, hopefully it wont effect performance too much
                    if (XSIZE * YSIZE >= 9)
                    {
                        if (SmokeEmitter == null) SmokeEmitter = Empire.Universe.explosionSmokeParticles.NewEmitter(40f, Center3D);
                        SmokeEmitter.Update(elapsedTime, Center3D);
                    }
                    if (ModuleType != ShipModuleType.PowerPlant) return;
                    if (LightningEmitter == null) LightningEmitter = Empire.Universe.lightning.NewEmitter(10f, Center3D);
                    LightningEmitter.Update(elapsedTime, Center3D);
                }
            }
            else if (TrailEmitter != null) // destroy immediately when out of vision range, tried Disposing these, but got a crash... so just null them
            {
                TrailEmitter     = null;
                FireTrailEmitter = null;
                FlameEmitter     = null;
                SmokeEmitter     = null;
                SparksEmitter    = null;
                LightningEmitter = null;
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
                    case ShipModuleType.Command: return 0;
                    case ShipModuleType.PowerPlant: return 1;
                    case ShipModuleType.PowerConduit: return 2;
                    case ShipModuleType.Engine: return 3;
                    case ShipModuleType.Shield: return 4;
                    case ShipModuleType.Armor: return 6;
                    default: return 5;
                }
            }
        }

        public Color GetHealthStatusColor()
        {
            float healthPercent = Health / HealthMax;

            if (Empire.Universe.Debug && isExternal)
            {
                if (healthPercent >= 0.5f) return Color.Blue;
                if (healthPercent > 0.0f) return Color.DarkSlateBlue;
                return Color.DarkSlateGray;
            }

            if (healthPercent >= 0.90f) return Color.Green;
            if (healthPercent >= 0.65f) return Color.GreenYellow;
            if (healthPercent >= 0.45f) return Color.Yellow;
            if (healthPercent >= 0.15f) return Color.OrangeRed;
            if (healthPercent > 0.00f) return Color.Red;
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
            if (healthPercent > 0.00f) return Color.Red;
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

            def += HealthMax * ((ModuleType == ShipModuleType.Armor ? (XSIZE) : 1f) / (slotCount * 4));

            // FB: Added Shield related calcs
            if (shield_power_max > 0)
            {
                def                 += shield_power_max / 100; 
                float shieldcoverage = ((shield_radius + 8f) * (shield_radius + 8f) * 3.14f) / 256f / slotCount;
                shieldcoverage       = shieldcoverage > 1 ? 1f : shieldcoverage;
                // normalizing for small ships
                if (slotCount < 10)
                    shieldcoverage = shieldcoverage * 0.03125f;
                else if (slotCount < 32)
                    shieldcoverage = shieldcoverage * 0.0625f;
                else if (slotCount < 60)
                    shieldcoverage = shieldcoverage * 0.25f;
                else if (slotCount < 200)
                    shieldcoverage = shieldcoverage * 0.5f;

                def *= shieldcoverage > 0 ? 1 + shieldcoverage : 1f; 
                def *= 1 + shield_kinetic_resist / 5;
                def *= 1 + shield_energy_resist / 5;
                def *= 1 + shield_beam_resist / 5;
                def *= 1 + shield_missile_resist / 5;
                def *= 1 + shield_explosive_resist / 5;

                def *= shield_recharge_rate > 0 ? shield_recharge_rate / (shield_power_max / 100) : 1f;
                def *= shield_recharge_delay > 0 ? 1f / shield_recharge_delay : 1f;
                def *= shield_recharge_combat_rate > 0 ? 1 + shield_recharge_combat_rate / (shield_power_max / 25) : 1f;

                def *= 1 + shield_threshold / 50f; // FB: Shield Threshold is much more effective than Damage threshold as it applys to the shield bubble.
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
            def *= 1 + EMP_Protection / slotCount;

            // Engines
            def += (TurnThrust + WarpThrust + thrust) / 15000f;

            // FB: Reactors should also have some value
            def += PowerFlowMax / 100;

            // Normilize Def based on its area - this is the main stuff which wraps all defence to logical  margins.
            def  = Area > 1 ? def / (Area / 2f) : def;
            def *= 0.5f; // So defense will have more chance to be lower than offense, otherwise defense is not calculated in the total offense of the ship

            return def;
        }

        public float CalculateModuleOffense()
        {
            float off = 0f;
            if (InstalledWeapon != null)
            {
                Weapon w = InstalledWeapon;

                if (w.isBeam)
                {
                    off += w.DamageAmount * 90f * w.BeamDuration * (1f / w.fireDelay);
                    off += w.MassDamage * (1f / w.fireDelay) * .5f;
                    off += w.PowerDamage * (1f / w.fireDelay);
                    off += w.RepulsionDamage * (1f / w.fireDelay);
                    off += w.SiphonDamage * (1f / w.fireDelay);
                    off += w.TroopDamageChance * (1f / w.fireDelay) * .2f;
                }
                else
                {
                    off += w.DamageAmount * w.SalvoCount * w.ProjectileCount * (1f / w.fireDelay);
                    off += w.EMPDamage * w.SalvoCount * w.ProjectileCount * (1f / w.fireDelay) * .5f;
                }

                //Doctor: Guided weapons attract better offensive rating than unguided - more likely to hit. Setting at flat 25% currently.
                off *= w.Tag_Guided ? 1.25f : 1f;

                //FB: Kinetics which does also require more than minimal power to shoot is less effective
                off *= w.Tag_Kinetic && w.PowerRequiredToFire > 10 * Area ? 0.5f : 1f;

                //FB: Kinetics which does also require more than minimal power to maintain is less effective
                off *= w.Tag_Kinetic && PowerDraw > 2 * Area ? 0.5f : 1f;

                //FB: Range margins are less steep for missiles
                off *= !w.Tag_Missile && !w.Tag_Torpedo ?  (w.Range / 4000) * (w.Range / 4000) : (w.Range / 4000);

                // FB: simpler calcs for these. 
                off *= w.EffectVsArmor > 1  ? 1f + (w.EffectVsArmor - 1f) / 2f : 1f;
                off *= w.EffectVsArmor < 1 ? 1f - (1f - w.EffectVsArmor) / 2f : 1f;
                off *= w.EffectVSShields > 1 ? 1f + (w.EffectVSShields - 1f) / 2f : 1f;
                off *= w.EffectVSShields < 1 ? 1f - (1f - w.EffectVSShields) / 2f : 1f;

                off *= w.TruePD ? .2f : 1f;
                off *= w.Tag_Intercept && w.Tag_Missile ? .8f : 1f;
                off *= w.ProjectileSpeed > 1 ? w.ProjectileSpeed / 4000 : 1f;

                // FB: offense calcs for damage radius
                off *= w.DamageRadius > 24 && !w.TruePD ? w.DamageRadius / 24f : 1f;

                // FB: Added shield pen chance
                off *= 1 + w.ShieldPenChance / 100;

                // FB: Turrets get some off
                off *= ModuleType == ShipModuleType.Turret ? 1.25f : 1f;

                // FB: Field of Fire is also important
                off *= FieldOfFire > 60 ? FieldOfFire / 60f : 1f;

                // FB: A weapon which can be installed on Internal slots is quite valuable.
                off *= Restrictions.ToString().Contains("I") ? 2f : 1f;

                int allRoles = 0;
                int restrictedRoles = 0;
                foreach (ShipData.RoleName role in Enum.GetValues(typeof(ShipData.RoleName)))
                {
                    allRoles++;
                    if (!w.TargetValid(role))
                        restrictedRoles++;
                }
                float restrictions = (float)(allRoles - restrictedRoles) / allRoles;
                off *= restrictions;

                //Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
                off *= w.OffPowerMod;
            }
            if (ModuleType == ShipModuleType.Hangar && hangarShipUID.NotEmpty() && hangarShipUID != "NotApplicable" && !IsSupplyBay && !IsTroopBay)
            {
                if (ResourceManager.GetShipTemplate(hangarShipUID, out Ship thangarShip))
                {
                    off += (thangarShip.BaseStrength > 0f) ? thangarShip.BaseStrength : thangarShip.CalculateShipStrength();
                }
                else off += 100f;
            }

            // FB: Normalize offense based on its area - this is the main stuff which wraps all weapons to logical offense margins.
            off = Area > 1 ? off / (Area / 2f) : off;

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

        public bool FighterOut
        {
            get
            {
                if (IsTroopBay || IsSupplyBay) return false;

                return hangarShip?.Active == true && hangarTimer <= 0;
            }
        }

        public override string ToString() => $"{UID}  {Id}  {Position}  World={Center}  Ship={Parent?.Name}";
    }
}