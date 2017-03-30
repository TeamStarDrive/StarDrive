using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Particle3DSample;
using System;
using Ship_Game.AI;
using Ship_Game.Debug;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
    [DebuggerDisplay("UID = {Flyweight.UID} InternalPos={XMLPosition} WorldPos={Position}")]
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
        private float distanceToParentCenter;
        private float offsetAngle;
        public float FieldOfFire;
        public float Facing;
        public Vector2 XMLPosition; // module slot location in the ship design
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
        public Vector2 moduleCenter;
        public Vector2 ModuleCenter;
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
                                          + (isExternal ? -5 : 0)         // external modules are less critical
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

        private ShipModule()     //Constructor
        {
            Flyweight = ShipModuleFlyweight.Empty;
        }

        private ShipModule(ShipModule_Deserialize s)
        {
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
            moduleCenter         = s.moduleCenter;
            ModuleCenter         = s.ModuleCenter;
            IconTexturePath      = s.IconTexturePath;
            isExternal           = s.isExternal;
            TargetValue          = s.TargetValue;
        }

        public static bool CanCreate(string uid)
        {
            return uid != "Dummy" // dummy modules are deprecated, so disallow creation
                && ResourceManager.ShipModules.ContainsKey(uid);
        }

        public static ShipModule CreateTemplate(ShipModule_Deserialize template)
        {
            return new ShipModule(template);
        }

        public static ShipModule Create(string uid, Ship parent, Vector2 xmlPos, float facing)
        {
            ShipModule module = CreateNoParent(uid);
            module.SetParent(parent);
            module.Facing = facing;
            module.Initialize(xmlPos);
            return module;
        }

        public static ShipModule CreateNoParent(string uid)
        {
            ShipModule template = ResourceManager.GetModuleTemplate(uid);
            var module = new ShipModule
            {
                // All complex properties here have been replaced by this single reference to 'ShipModuleFlyweight' which now contains them all - Gretman
                Flyweight            = template.Flyweight,
                DescriptionIndex     = template.DescriptionIndex,
                FieldOfFire          = template.FieldOfFire,
                hangarShipUID        = template.hangarShipUID,
                hangarTimer          = template.hangarTimer,
                Health               = template.HealthMax,
                HealthMax            = template.HealthMax,
                isWeapon             = template.isWeapon,
                Mass                 = template.Mass,
                ModuleType           = template.ModuleType,
                NameIndex            = template.NameIndex,
                OrdinanceCapacity    = template.OrdinanceCapacity,
                ShieldPower          = template.shield_power_max, //Hmmm... This one is strange -Gretman
                XSIZE                = template.XSIZE,
                YSIZE                = template.YSIZE
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

        // Collision test with this ShipModule. Returns TRUE if point is inside this module's
        // The collision bounds are APPROXIMATED by using radius checks. This means corners
        // are not accurately checked.
        // HitTest uses the World scene POSITION. Not module XML location
        public bool HitTest(Vector2 point, float radius, bool ignoreShields = false)
        {
            int larger = XSIZE >= YSIZE ? XSIZE : YSIZE;

            float r2 = radius + larger * 8.0f * 1.125f; // approximated, slightly bigger radius
            float dx = Center.X - point.X;
            float dy = Center.Y - point.Y;

            if (!ignoreShields && ShieldPower > 0.0f) // if module is shielded, then radius check is always circular
            {
                r2 += Radius; // Radius is the Exact shield radius, no extra scaling
                return dx * dx + dy * dy <= r2 * r2;
            }

            if (dx * dx + dy * dy > r2 * r2)
                return false; // definitely out of radius for SQUARE and non-square modules

            // we are a Square module? since we're already inside radius, collision happened
            int smaller = XSIZE < YSIZE ? XSIZE : YSIZE;
            if (larger == smaller)
                return true;

            // now for more expensive and accurate capsule-line collision testing
            // since we can have 4x1 modules etc, so we need to construct a line+radius
            float diameter   = ((float)smaller / larger) * smaller * 16.0f;

            // if high module, use forward vector, if wide module, use right vector
            Vector2 dir = Rotation.AngleToDirection();
            if (XSIZE > YSIZE) dir = dir.RightVector();

            float offset = (larger*16.0f - diameter) * 0.5f;
            Vector2 startPos = Position - dir * offset;
            Vector2 endPos   = Position + dir * offset;
            float rayWidth = diameter * 1.125f; // approx 18.0x instead of 16.0x
            return point.RayHitTestCircle(radius, startPos, endPos, rayWidth);
        }

        // gives the approximate radius of the module, depending on module XSIZE & YSIZE
        public float ApproxRadius => 8.0f * 1.125f * (XSIZE > YSIZE ? XSIZE : YSIZE);

        public bool Damage(GameplayObject source, float damageAmount, out float damageRemainder)
        {
            float health = Health;
            bool result = Damage(source, damageAmount);
            damageRemainder = damageAmount - (health - Health);
            return result;
        }

        // @todo Unify with DamageInvisible
        public override bool Damage(GameplayObject source, float damageAmount)
        {
            if (source != null)
                Parent.LastDamagedBy = source;

            Parent.InCombatTimer = 15f;
            Parent.ShieldRechargeTimer = 0f;
            //Added by McShooterz: Fix for Ponderous, now negative dodgemod increases damage taken.

            var proj = source as Projectile;
            var beam = source as Beam;
            
            if (proj != null)
            {
                if (Parent.shipData.Role == ShipData.RoleName.fighter && Parent.loyalty.data.Traits.DodgeMod < 0f)
                {
                    damageAmount += damageAmount * Math.Abs(Parent.loyalty.data.Traits.DodgeMod);
                }
            }

            if (source is Ship ship && ship.shipData.Role == ShipData.RoleName.fighter && Parent.loyalty.data.Traits.DodgeMod < 0f)
                damageAmount += damageAmount * Math.Abs(Parent.loyalty.data.Traits.DodgeMod);

            // Vulnerabilities and resistances for modules, XML-defined.
            if (proj != null)
                damageAmount = ApplyResistances(proj.weapon, damageAmount);

            if (ShieldPower <= 0f || proj != null && proj.IgnoresShields)
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
                if (proj?.weapon.EMPDamage > 0f)
                {
                    Parent.EMPDamage = Parent.EMPDamage + proj.weapon.EMPDamage;
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
                    if (beam.weapon.PowerDamage > 0f)
                    {
                        Parent.PowerCurrent -= beam.weapon.PowerDamage;
                        if (Parent.PowerCurrent < 0f)
                        {
                            Parent.PowerCurrent = 0f;
                        }
                    }
                    if (beam.weapon.TroopDamageChance > 0f)
                    {
                        if (Parent.TroopList.Count > 0)
                        {
                            if (UniverseRandom.RandomBetween(0f, 100f) < beam.weapon.TroopDamageChance)
                            {
                                Parent.TroopList[0].Strength = Parent.TroopList[0].Strength - 1;
                                if (Parent.TroopList[0].Strength <= 0)
                                    Parent.TroopList.RemoveAt(0);
                            }
                        }
                        else if (Parent.MechanicalBoardingDefense > 0f && RandomMath.RandomBetween(0f, 100f) < beam.weapon.TroopDamageChance)
                        {
                            Parent.MechanicalBoardingDefense -= 1f;
                        }
                    }
                    if (beam.weapon.MassDamage > 0f && !Parent.IsTethered() && !Parent.EnginesKnockedOut)
                    {
                        Parent.Mass += beam.weapon.MassDamage;
                        Parent.velocityMaximum = Parent.Thrust / Parent.Mass;
                        Parent.speed = Parent.velocityMaximum;
                        Parent.rotationRadiansPerSecond = Parent.speed / 700f;
                    }
                    if (beam.weapon.RepulsionDamage > 0f && !Parent.IsTethered() && !Parent.EnginesKnockedOut)
                    {
                        Parent.Velocity += ((Center - beam.Owner.Center) * beam.weapon.RepulsionDamage) / Parent.Mass;
                    }
                }
                if (shield_power_max > 0f && (!isExternal || quadrant <= 0))
                {
                    return false;
                }

                if (ModuleType == ShipModuleType.Armor && source is Projectile)
                {
                    if (proj.isSecondary)
                    {
                        Weapon secondary = ResourceManager.GetWeaponTemplate(proj.weapon.SecondaryFire);
                        damageAmount *= secondary.EffectVsArmor; 
                    }
                    else
                    {
                        damageAmount *= proj.weapon.EffectVsArmor;
                    }
                }
                else if (ModuleType == ShipModuleType.Armor && beam != null)
                {
                    damageAmount *= beam.weapon.EffectVsArmor;
                }

                if (damageAmount > Health)
                {
                    Health = 0;
                }
                else
                {
                    Health -= damageAmount;
                }
                if (Health >= HealthMax)
                {
                    Health = HealthMax;
                    Active = true;
                    onFire = false;
                }
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
                    damageAmount *= proj.weapon.EffectVSShields;

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

                if (Empire.Universe.viewState == UniverseScreen.UnivScreenState.ShipView && Parent.InFrustum)
                {
                    if (source != null)
                        shield.Rotation = source.Rotation - 3.14159274f;
                    shield.displacement = 0f;
                    shield.texscale = 2.8f;
                    shield.pointLight.Refresh(Empire.Universe);

                    if (beam != null)
                    {
                        if (beam.weapon.SiphonDamage > 0f)
                        {
                            ShieldPower -= beam.weapon.SiphonDamage;
                            if (ShieldPower < 0f)
                            {
                                ShieldPower = 0f;
                            }
                            beam.owner.PowerCurrent += beam.weapon.SiphonDamage;
                            if (beam.owner.PowerCurrent > beam.owner.PowerStoreMax)
                            {
                                beam.owner.PowerCurrent = beam.owner.PowerStoreMax;
                            }
                        }
                        shield.Rotation = Center.RadiansToTarget(beam.Source);
                        shield.pointLight.World = Matrix.CreateTranslation(new Vector3(beam.ActualHitDestination, 0f));
                        shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        shield.pointLight.Radius = shield_radius * 2f;
                        shield.pointLight.Intensity = RandomMath.RandomBetween(4f, 10f);
                        shield.displacement       = 0f;
                        shield.Radius             = Radius;
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
                        if (beam.weapon.SiphonDamage > 0f)
                        {
                            ShieldPower -= beam.weapon.SiphonDamage;
                            if (ShieldPower < 0f)
                                ShieldPower = 0f;
                        }
                    }
                    else if (proj != null && !proj.IgnoresShields && Parent.InFrustum)
                    {
                        Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
                        shieldcue.Apply3D(Empire.Universe.listener, Parent.emitter);
                        shieldcue.Play();
                        shield.Radius       = Radius;
                        shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        shield.texscale     = 2.8f;
                        shield.texscale     = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        shield.pointLight.World        = proj.GetWorld();
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
            Radius = ShieldPower > 0f ? shield_radius : 8f;
            return true;
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++DebugInfoScreen.ModulesDied;
            if (shield_power_max > 0f)
                Health = 0f;

            SetNewExternals();
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

            int size = XSIZE * YSIZE;
            if (!cleanupOnly)
            {
                if (Parent.Active && Parent.InFrustum && Empire.Universe.viewState == UniverseScreen.UnivScreenState.ShipView)
                {
                    audioListener.Position = Empire.Universe.camPos;
                    AudioManager.PlayCue("sd_explosion_module_small", audioListener, Parent.emitter);
                }
                if (explodes)
                {
                    GameplayObject damageCauser = Parent.LastDamagedBy;
                    if (damageCauser == null)
                        Log.Error("LastDamagedBy is not properly set. Please check projectile damage code!");
                    SpatialManagerForSystem(inSystem).ExplodeAtModule(damageCauser, this, damageAmount:size*2500, damageRadius:size*64);
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

        private void Initialize(Vector2 pos)
        {
            ++DebugInfoScreen.ModulesCreated;

            XMLPosition = pos;
            Radius = 8f;
            Position = pos;
            Dimensions = new Vector2(16f, 16f);
            var relativeShipCenter = new Vector2(512f, 512f);
            moduleCenter.X = pos.X + 256f;
            moduleCenter.Y = pos.Y + 256f;

            distanceToParentCenter = relativeShipCenter.Distance(moduleCenter);
            offsetAngle = relativeShipCenter.AngleToTarget(moduleCenter);
            SetInitialPosition();
            SetAttributesByType();

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
                if (ResourceManager.ShipsDict["Assault_Shuttle"].Mass / 5f > Parent.Ordinance)  //fbedard: New spawning cost
                    return;
                if (hangarTimer <= 0f && hangarShip == null)
                {
                    hangarShip = ResourceManager.CreateTroopShipAtPoint("Assault_Shuttle", Parent.loyalty, Center, troop);
                    hangarShip.VanityName = "Assault Shuttle";
                    hangarShip.Mothership = Parent;
                    hangarShip.DoEscort(Parent);
                    hangarShip.Velocity = UniverseRandom.RandomDirection() * hangarShip.speed + Parent.Velocity;
                    if (hangarShip.Velocity.Length() > hangarShip.velocityMaximum)
                    {
                        hangarShip.Velocity = Vector2.Normalize(hangarShip.Velocity) * hangarShip.speed;
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
                    || hangarShip.AI.hasPriorityTarget
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

            Ship ship = null;
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

            SetHangarShip(ResourceManager.CreateShipFromHangar(ship.Name, Parent.loyalty, Center, Parent));

            hangarShip.DoEscort(Parent);
            hangarShip.Velocity = UniverseRandom.RandomDirection() * GetHangarShip().speed + Parent.Velocity;
            if (hangarShip.Velocity.Length() > hangarShip.velocityMaximum)
            {
                hangarShip.Velocity = Vector2.Normalize(hangarShip.Velocity) * hangarShip.speed;
            }
            hangarShip.Mothership = Parent;
            HangarShipGuid = GetHangarShip().guid;

            hangarTimer = hangarTimerConstant;
            Parent.Ordinance -= hangarShip.Mass / 5f;
        }

        public void SetAttributesByType()
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
            if (shield_power_max > 0.0)
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
            InstalledWeapon.moduleAttachedTo = this;
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

        public void SetInitialPosition()
        {
            float angle = offsetAngle + Parent.Rotation.ToDegrees();

            ModuleCenter = Parent.Center.PointFromAngle(angle, distanceToParentCenter);

            Position = new Vector2(ModuleCenter.X - 8f, ModuleCenter.Y - 8f);
            Center = ModuleCenter;
        }

        private void AddExternalModule(ShipModule module, int moduleQuadrant)
        {
            module.isExternal = true;
            module.quadrant   = moduleQuadrant;
            Parent.ExternalSlots.Add(module);
        }

        public void SetNewExternals()
        {
            quadrant = -1;

            Vector2 pos = XMLPosition;
            if (Parent.TryGetModule(new Vector2(pos.X, pos.Y - 16f), out ShipModule module) && module.Active && !module.isExternal)
            {
                AddExternalModule(module, 1);
            }
            else if (Parent.TryGetModule(new Vector2(pos.X + 16f, pos.Y), out module) && module.Active && !module.isExternal)
            {
                AddExternalModule(module, 2);
            }
            else if (Parent.TryGetModule(new Vector2(pos.X, pos.Y + 16f), out module) && module.Active && !module.isExternal)
            {
                AddExternalModule(module, 3);
            }
            else if (Parent.TryGetModule(new Vector2(pos.X - 16f, pos.Y), out module) && module.Active && !module.isExternal)
            {
                AddExternalModule(module, 4);
            }
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
                SetNewExternals();
                Parent.RecalculatePower();
            }
            else if (shield_power_max > 0f && !isExternal && Active)
            {
                quadrant = -1;
                isExternal = true;
            }
            if (Health <= 0f && Active)
            {
                Die(LastDamagedBy, false);
            }
            if (Health >= HealthMax)
            {
                Health = HealthMax;
                onFire = false;
            }

            BombTimer -= elapsedTime;
            //Added by McShooterz: shields keep charge when manually turned off
            if (ShieldPower <= 0f)
            {
                Radius = 8f;
            }
            else
                Radius = shield_radius;
            if (ModuleType == ShipModuleType.Hangar && Active) //(this.hangarShip == null || !this.hangarShip.Active) && 
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

        // Refactored by RedFox - @note This method is called very heavily, so many parts have been inlined by hand
        public void UpdateEveryFrame(float elapsedTime, float cos, float sin, float tan)
        {
            // Move the module, this part is optimized according to profiler data
            GlobalStats.ModulesMoved += 1;
            Vector2 actualVector = XMLPosition; // huge cache miss here
            actualVector.X -= 256f;
            actualVector.Y -= 256f;
            float cx = actualVector.X * cos - actualVector.Y * sin;
            float cy = actualVector.X * sin + actualVector.Y * cos;
            Vector2 parentCenter = Parent.Center;
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

        private void HandleDamageFireTrail(float elapsedTime)
        {
            if (Parent.InFrustum && Active && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (reallyFuckedUp)
                {
                    if (trailEmitter == null) trailEmitter = new ParticleEmitter(Empire.Universe.projectileTrailParticles, 50f, Center3D);
                    if (flameEmitter == null) flameEmitter = new ParticleEmitter(Empire.Universe.flameParticles, 80f, Center3D);
                    trailEmitter.Update(elapsedTime, Center3D);
                    flameEmitter.Update(elapsedTime, Center3D);
                }
                else if (onFire)
                {
                    if (trailEmitter == null)     trailEmitter     = new ParticleEmitter(Empire.Universe.projectileTrailParticles, 50f, Center3D);
                    if (firetrailEmitter == null) firetrailEmitter = new ParticleEmitter(Empire.Universe.fireTrailParticles, 60f, Center3D);
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

        public override void Draw(UniverseScreen screen)
        {

        }
    }
}