using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using System;
using System.Collections.Generic;
using Ship_Game.AI;
using Ship_Game.Debug;
using System.Diagnostics;

namespace Ship_Game.Gameplay
{
    [DebuggerDisplay("UID = {Advanced.UID} InternalPos={XMLPosition} WorldPos={Position}")]
    public sealed class ShipModule : GameplayObject
    {
        //private static int TotalModules = 0;
        //public int ID = ++TotalModules;
        public ShipModule_Advanced Advanced; //This is where all the other member variables went. Having this as a member object
                                             //allows me to instance the variables inside it, so they are not duplicated. This
                                             //can offer much better memory usage since ShipModules are so numerous.     -Gretman
        private ParticleEmitter trailEmitter;
        private ParticleEmitter firetrailEmitter;
        private ParticleEmitter flameEmitter;
        public byte XSIZE = 1;
        public byte YSIZE = 1;
        public Array<string> PermittedHangarRoles;
        public bool Powered;
        public bool isDummy;
        public Array<ShipModule> LinkedModulesList = new Array<ShipModule>();
        public static UniverseScreen universeScreen;
        private float distanceToParentCenter;
        private float offsetAngle;
        public float FieldOfFire;
        public float facing;
        public Vector2 XMLPosition;
        private Ship Parent;
        public ShipModule ParentOfDummy;
        public float HealthMax;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public Restrictions Restrictions;
        public float shield_power;
        public bool shieldsOff;
        private Shield shield;
        public string hangarShipUID;
        private Ship hangarShip;
        public float hangarTimer;
        public bool isWeapon;
        public Weapon InstalledWeapon;
        public short OrdinanceCapacity;
        private bool onFire;
        private bool reallyFuckedUp;
        private Vector3 Center3D;
        public float BombTimer;
        public ShipModuleType ModuleType;
        public Vector2 moduleCenter = new Vector2(0f, 0f);
        public Vector2 ModuleCenter;
        public string IconTexturePath;

        public string UID
        {
            get { return Advanced.UID; }
            set { Advanced.UID = value; }
        }
        
        public ModuleSlot installedSlot;
        public bool isExternal;
        public int TargetValue=0;
        public sbyte quadrant = -1;
        public float TransporterTimer = 0f;

        // Used to configure how good of a target this module is
        public int ModuleTargettingValue => TargetValue 
                                          + (isExternal ? -5 : 0)         // external modules are less critical
                                          + (Health < HealthMax ? 1 : 0); // prioritize already damaged modules

        //This wall of text is the 'get' functions for all of the variables that got moved to the 'Advanced' object.
        //This will allow us to still use the normal "Module.IsCommandModule" even though 'IsCommandModule' actually
        //lives in "Module.Advanced.IsCommandModule" now.    -Gretman
        public float FTLSpeed                   => Advanced.FTLSpeed;
        public string DeployBuildingOnColonize  => Advanced.DeployBuildingOnColonize;
        public string ResourceStored            => Advanced.ResourceStored;
        public float ResourceStorageAmount      => Advanced.ResourceStorageAmount;
        public bool IsCommandModule             => Advanced.IsCommandModule;
        public bool IsRepairModule              => Advanced.IsRepairModule;
        public short MaximumHangarShipSize      => Advanced.MaximumHangarShipSize;
        public bool FightersOnly                => Advanced.FightersOnly;
        public bool DroneModule                 => Advanced.DroneModule;
        public bool FighterModule               => Advanced.FighterModule;
        public bool CorvetteModule              => Advanced.CorvetteModule;
        public bool FrigateModule               => Advanced.FrigateModule;
        public bool DestroyerModule             => Advanced.DestroyerModule;
        public bool CruiserModule               => Advanced.CruiserModule;
        public bool CarrierModule               => Advanced.CarrierModule;
        public bool CapitalModule               => Advanced.CapitalModule;
        public bool FreighterModule             => Advanced.FreighterModule;
        public bool PlatformModule              => Advanced.PlatformModule;
        public bool StationModule               => Advanced.StationModule;
        public bool explodes                    => Advanced.explodes;
        public float SensorRange                => Advanced.SensorRange;
        public float MechanicalBoardingDefense  => Advanced.MechanicalBoardingDefense;
        public float EMP_Protection             => Advanced.EMP_Protection;
        public byte PowerRadius                 => Advanced.PowerRadius;
        public byte TechLevel                   => Advanced.TechLevel;
        public float OrdnanceAddedPerSecond     => Advanced.OrdnanceAddedPerSecond;
        public string BombType                  => Advanced.BombType;
        public float WarpMassCapacity           => Advanced.WarpMassCapacity;
        public float BonusRepairRate            => Advanced.BonusRepairRate;
        public float Cargo_Capacity             => Advanced.Cargo_Capacity;
        public float shield_radius              => Advanced.shield_radius;
        public float shield_power_max           => Advanced.shield_power_max;
        public float shield_recharge_rate       => Advanced.shield_recharge_rate;
        public float shield_recharge_combat_rate=> Advanced.shield_recharge_combat_rate;
        public float shield_recharge_delay      => Advanced.shield_recharge_delay;
        public float shield_threshold           => Advanced.shield_threshold;
        public float shield_kinetic_resist      => Advanced.shield_kinetic_resist;
        public float shield_energy_resist       => Advanced.shield_energy_resist;
        public float shield_explosive_resist    => Advanced.shield_explosive_resist;
        public float shield_missile_resist      => Advanced.shield_missile_resist;
        public float shield_flak_resist         => Advanced.shield_flak_resist;
        public float shield_hybrid_resist       => Advanced.shield_hybrid_resist;
        public float shield_railgun_resist      => Advanced.shield_railgun_resist;
        public float shield_subspace_resist     => Advanced.shield_subspace_resist;
        public float shield_warp_resist         => Advanced.shield_warp_resist;
        public float shield_beam_resist         => Advanced.shield_beam_resist;
        public float numberOfColonists          => Advanced.numberOfColonists;
        public float numberOfEquipment          => Advanced.numberOfEquipment;
        public float numberOfFood               => Advanced.numberOfFood;
        public bool IsSupplyBay                 => Advanced.IsSupplyBay;
        public bool IsTroopBay                  => Advanced.IsTroopBay;
        public float hangarTimerConstant        => Advanced.hangarTimerConstant;
        public float thrust                     => Advanced.thrust;
        public float WarpThrust                 => Advanced.WarpThrust;
        public float TurnThrust                 => Advanced.TurnThrust;
        public float PowerFlowMax               => Advanced.PowerFlowMax;
        public float PowerDraw                  => Advanced.PowerDraw;
        public float PowerDrawAtWarp            => Advanced.PowerDrawAtWarp;
        public float PowerStoreMax              => Advanced.PowerStoreMax;
        public float HealPerTurn                => Advanced.HealPerTurn;
        public byte TroopCapacity               => Advanced.TroopCapacity;
        public byte TroopsSupplied              => Advanced.TroopsSupplied;
        public float Cost                       => Advanced.Cost;
        public float InhibitionRadius           => Advanced.InhibitionRadius;
        public float FTLSpoolTime               => Advanced.FTLSpoolTime;
        public float ECM                        => Advanced.ECM;
        public float SensorBonus                => Advanced.SensorBonus;
        public float TransporterTimerConstant   => Advanced.TransporterTimerConstant;
        public float TransporterRange           => Advanced.TransporterRange;
        public float TransporterPower           => Advanced.TransporterPower;
        public float TransporterOrdnance        => Advanced.TransporterOrdnance;
        public byte TransporterTroopLanding     => Advanced.TransporterTroopLanding;
        public byte TransporterTroopAssault     => Advanced.TransporterTroopAssault;
        public float KineticResist              => Advanced.KineticResist;
        public float EnergyResist               => Advanced.EnergyResist;
        public float GuidedResist               => Advanced.GuidedResist;
        public float MissileResist              => Advanced.MissileResist;
        public float HybridResist               => Advanced.HybridResist;
        public float BeamResist                 => Advanced.BeamResist;
        public float ExplosiveResist            => Advanced.ExplosiveResist;
        public float InterceptResist            => Advanced.InterceptResist;
        public float RailgunResist              => Advanced.RailgunResist;
        public float SpaceBombResist            => Advanced.SpaceBombResist;
        public float BombResist                 => Advanced.BombResist;
        public float BioWeaponResist            => Advanced.BioWeaponResist;
        public float DroneResist                => Advanced.DroneResist;
        public float WarpResist                 => Advanced.WarpResist;
        public float TorpedoResist              => Advanced.TorpedoResist;
        public float CannonResist               => Advanced.CannonResist;
        public float SubspaceResist             => Advanced.SubspaceResist;
        public float PDResist                   => Advanced.PDResist;
        public float FlakResist                 => Advanced.FlakResist;
        public float DamageThreshold            => Advanced.DamageThreshold;
        public int APResist                     => Advanced.APResist;
        public bool IndirectPower               => Advanced.IndirectPower;
        public bool isPowerArmour               => Advanced.isPowerArmour;
        public bool isBulkhead                  => Advanced.isBulkhead;
        public sbyte TargetTracking             => Advanced.TargetTracking;
        public sbyte FixedTracking              => Advanced.FixedTracking;
        public bool IsWeapon    => ModuleType == ShipModuleType.Spacebomb 
                                || ModuleType == ShipModuleType.Turret 
                                || ModuleType == ShipModuleType.MainGun 
                                || ModuleType == ShipModuleType.MissileLauncher 
                                || ModuleType == ShipModuleType.Drone 
                                || ModuleType == ShipModuleType.Bomb;

        public ShipModule()     //Constructor
        {
            Advanced = ShipModule_Advanced.Empty;
        }

        public void Clear()
        {
            this.LinkedModulesList.Clear();
        }

        private float ApplyShieldResistances(Weapon weapon, float damage)
        {
            if (weapon.Tag_Kinetic)
                damage -= damage * this.shield_kinetic_resist;
            if (weapon.Tag_Energy)
                damage -= damage * this.shield_energy_resist;
            if (weapon.Tag_Explosive)
                damage -= damage * this.shield_explosive_resist;
            if (weapon.Tag_Missile)
                damage -= damage * this.shield_missile_resist;
            if (weapon.Tag_Flak)
                damage -= damage * this.shield_flak_resist;
            if (weapon.Tag_Hybrid)
                damage -= damage * this.shield_hybrid_resist;
            if (weapon.Tag_Railgun)
                damage -= damage * this.shield_railgun_resist;
            if (weapon.Tag_Subspace)
                damage -= damage * this.shield_subspace_resist;
            if (weapon.Tag_Warp)
                damage -= damage * this.shield_warp_resist;
            if (weapon.Tag_Beam)
                damage -= damage * this.shield_beam_resist;

            return damage;
        }

        private float ApplyResistances(Weapon weapon, float damage)
        {
            if (weapon.Tag_Beam)
                damage -= damage * this.BeamResist;
            if (weapon.Tag_Kinetic)
                damage -= damage * this.KineticResist;
            if (weapon.Tag_Energy)
                damage -= damage * this.EnergyResist;
            if (weapon.Tag_Guided)
                damage -= damage * this.GuidedResist;
            if (weapon.Tag_Missile)
                damage -= damage * this.MissileResist;
            if (weapon.Tag_Hybrid)
                damage -= damage * this.HybridResist;
            if (weapon.Tag_Intercept)
                damage -= damage * this.InterceptResist;
            if (weapon.Tag_Explosive)
                damage -= damage * this.ExplosiveResist;
            if (weapon.Tag_Railgun)
                damage -= damage * this.RailgunResist;
            if (weapon.Tag_SpaceBomb)
                damage -= damage * this.SpaceBombResist;
            if (weapon.Tag_Bomb)
                damage -= damage * this.BombResist;
            if (weapon.Tag_BioWeapon)
                damage -= damage * this.BioWeaponResist;
            if (weapon.Tag_Drone)
                damage -= damage * this.DroneResist;
            if (weapon.Tag_Warp)
                damage -= damage * this.WarpResist;
            if (weapon.Tag_Torpedo)
                damage -= damage * this.TorpedoResist;
            if (weapon.Tag_Cannon)
                damage -= damage * this.CannonResist;
            if (weapon.Tag_Subspace)
                damage -= damage * this.SubspaceResist;
            if (weapon.Tag_PD)
                damage -= damage * this.PDResist;
            if (weapon.Tag_Flak)
                damage -= damage * this.FlakResist;

            return damage;
        }

        public bool Damage(GameplayObject source, float damageAmount, ref float damageRemainder)
        {
            if (this.ModuleType == ShipModuleType.Dummy)
            {
                this.ParentOfDummy.Damage(source, damageAmount, ref damageRemainder);
                return true;
            }
            Projectile psource = source as Projectile;
            Beam bsource = source as Beam;
            this.Parent.InCombatTimer = 15f;
            this.Parent.ShieldRechargeTimer = 0f;
            //Added by McShooterz: Fix for Ponderous, now negative dodgemod increases damage taken.
            if (psource !=null)
            {
                this.Parent.LastDamagedBy = source;
                if (this.Parent.shipData.Role == ShipData.RoleName.fighter && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
                {
                    damageAmount += damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
                }
            }
            if (source is Ship && (source as Ship).shipData.Role == ShipData.RoleName.fighter && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
            {
                damageAmount += damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
            }

            if (psource != null)
                damageAmount = ApplyResistances(psource.weapon, damageAmount);

            if (bsource !=null)
                damageAmount = ApplyResistances(psource.weapon, damageAmount);

            //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
            if (damageAmount <= this.DamageThreshold)
                damageAmount = 0f;

            //Added by McShooterz: shields keep charge when manually turned off
            if (this.shield_power <= 0f || shieldsOff || psource !=null && psource.IgnoresShields)
            {
                //Added by McShooterz: ArmorBonus Hull Bonus
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                {
                    HullBonus mod;
                    if (ResourceManager.HullBonuses.TryGetValue(this.GetParent().shipData.Hull, out mod))
                        damageAmount *= (1f - mod.ArmoredBonus);
                }
                if (psource !=null && psource.weapon.EMPDamage > 0f)
                {
                    this.Parent.EMPDamage = this.Parent.EMPDamage + (source as Projectile).weapon.EMPDamage;
                }
                if (this.shield_power_max > 0f && (!this.isExternal || this.quadrant <=0))
                {
                    return false;
                }
                if (this.ModuleType == ShipModuleType.Armor && source is Projectile)
                {
                    if ((source as Projectile).isSecondary)
                    {
                        Weapon shooter = (source as Projectile).weapon;
                        ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                        damageAmount *= shooter.EffectVsArmor;
                        //damageAmount *= (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVsArmor);
                    }
                    else
                    {
                        damageAmount *= (source as Projectile).weapon.EffectVsArmor;
                    }
                }
                if (damageAmount > this.Health)
                {
                    damageRemainder = damageAmount - this.Health;
                    this.Health = 0;
                }
                else
                {
                    damageRemainder = 0;
                    this.Health -= damageAmount;
                }
                if (base.Health >= this.HealthMax)
                {
                    base.Health = this.HealthMax;
                    this.Active = true;
                    this.onFire = false;
                }
                if (base.Health / this.HealthMax < 0.5f)
                {
                    this.onFire = true;
                }
                if ((this.Parent.Health / this.Parent.HealthMax) < 0.5 && base.Health < 0.5 * this.HealthMax)
                {
                    this.reallyFuckedUp = true;
                }
                foreach (ShipModule dummy in this.LinkedModulesList)
                {
                    dummy.DamageDummy(damageAmount);
                }
            }
            else
            {
                float damageAmountvsShields = damageAmount;
                if (psource!=null)
                {
                    if (psource.isSecondary)
                    {
                        Weapon shooter = psource.weapon;
                        ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                        damageAmountvsShields *= shooter.EffectVSShields; 
                        //damageAmountvsShields *= (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVSShields);
                    }
                    else
                    {
                        damageAmountvsShields *= (source as Projectile).weapon.EffectVSShields;
                    }
                }

                if (psource !=null)
                    damageAmountvsShields = ApplyShieldResistances(psource.weapon, damageAmountvsShields);
                else if (bsource != null)
                    damageAmountvsShields = ApplyShieldResistances(psource.weapon, damageAmountvsShields);

                if (damageAmountvsShields <= this.shield_threshold)
                    damageAmountvsShields = 0f;

                if (damageAmountvsShields > this.shield_power)
                {
                    damageRemainder = damageAmountvsShields - this.shield_power;
                    this.shield_power = 0;
                }
                else
                {
                    damageRemainder = 0;
                    this.shield_power -= damageAmountvsShields;
                }

                if (ShipModule.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && this.Parent.InFrustum)
                {
                    this.shield.Rotation = source.Rotation - 3.14159274f;
                    this.shield.displacement = 0f;
                    this.shield.texscale = 2.8f;
                    shield.pointLight.Refresh(Empire.Universe);

                    if (psource != null && !psource.IgnoresShields && this.Parent.InFrustum)
                    {
                        Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
                        shieldcue.Apply3D(ShipModule.universeScreen.listener, this.Parent.emitter);
                        shieldcue.Play();
                        this.shield.Radius = this.Radius;
                        this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.texscale = 2.8f;
                        this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.pointLight.World = psource.GetWorld();
                        this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        this.shield.pointLight.Radius = this.Radius;
                        this.shield.pointLight.Intensity = 8f;
                        this.shield.pointLight.Enabled = true;
                        Vector2 vel = Vector2.Normalize(psource.Center - this.Center);
                        ShipModule.universeScreen.flash.AddParticleThreadB(new Vector3(psource.Center, this.Center3D.Z), Vector3.Zero);
                        for (int i = 0; i < 20; i++)
                        {
                            ShipModule.universeScreen.sparks.AddParticleThreadB(new Vector3(psource.Center, this.Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                        }
                    }
                }
            }
            //Added by McShooterz: shields keep charge when manually turned off
            if (this.shield_power > 0f && !shieldsOff)
            {
                this.Radius = this.shield_radius;
            }
            else
            {
                this.Radius = 8f;
            }
            return true;
        }

        public override bool Damage(GameplayObject source, float damageAmount)
        {
            if (source == null)  //fbedard: prevent a crash
                return false;
            if (this.ModuleType == ShipModuleType.Dummy)
            {
                this.ParentOfDummy.Damage(source, damageAmount);
                return true;
            }
            this.Parent.InCombatTimer = 15f;
            this.Parent.ShieldRechargeTimer = 0f;
            //Added by McShooterz: Fix for Ponderous, now negative dodgemod increases damage taken.
            if (source is Projectile)
            {
                this.Parent.LastDamagedBy = source;
                if (this.Parent.shipData.Role == ShipData.RoleName.fighter && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
                {
                    damageAmount += damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
                }
            }

            if (source is Ship && (source as Ship).shipData.Role == ShipData.RoleName.fighter && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
            {
                damageAmount += damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
            }
            //Added by McShooterz: shields keep charge when manually turned off
            if (this.shield_power <= 0f || shieldsOff || source is Projectile && (source as Projectile).IgnoresShields)
            {
                // Vulnerabilities and resistances for modules, XML-defined.
                if (source is Projectile)
                    damageAmount = ApplyResistances((source as Projectile).weapon, damageAmount);

                if (source is Beam)
                    damageAmount = ApplyResistances((source as Beam).weapon, damageAmount);

                //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
                if (damageAmount <= this.DamageThreshold)
                    damageAmount = 0f;

                //Added by McShooterz: ArmorBonus Hull Bonus
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses)
                {
                    HullBonus mod;
                    if (ResourceManager.HullBonuses.TryGetValue(this.GetParent().shipData.Hull, out mod))
                        damageAmount *= (1f - mod.ArmoredBonus);
                }
                if (source is Projectile && (source as Projectile).weapon.EMPDamage > 0f)
                {
                    this.Parent.EMPDamage = this.Parent.EMPDamage + (source as Projectile).weapon.EMPDamage;
                }
                if (source is Beam)
                {
                    Vector2 vel = Vector2.Normalize((source as Beam).Source - this.Center);
                    if (RandomMath.RandomBetween(0f, 100f) > 90f && this.Parent.InFrustum)
                    {
                        ShipModule.universeScreen.flash.AddParticleThreadB(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), Vector3.Zero);
                    }
                    if (this.Parent.InFrustum)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            ShipModule.universeScreen.sparks.AddParticleThreadB(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                        }
                    }
                    if ((source as Beam).weapon.PowerDamage > 0f)
                    {
                        this.Parent.PowerCurrent -= (source as Beam).weapon.PowerDamage;
                        if (this.Parent.PowerCurrent < 0f)
                        {
                            this.Parent.PowerCurrent = 0f;
                        }
                    }
                    if ((source as Beam).weapon.TroopDamageChance > 0f)
                    {
                        if (this.Parent.TroopList.Count > 0)
                        {
                            if (UniverseRandom.RandomBetween(0f, 100f) < (source as Beam).weapon.TroopDamageChance)
                            {
                                this.Parent.TroopList[0].Strength = this.Parent.TroopList[0].Strength - 1;
                                if (this.Parent.TroopList[0].Strength <= 0)
                                    this.Parent.TroopList.RemoveAt(0);
                            }
                        }
                        else if (this.Parent.MechanicalBoardingDefense > 0f && RandomMath.RandomBetween(0f, 100f) < (source as Beam).weapon.TroopDamageChance)
                        {
                            this.Parent.MechanicalBoardingDefense -= 1f;
                        }
                    }
                    if ((source as Beam).weapon.MassDamage > 0f && !this.Parent.IsTethered() && !this.Parent.EnginesKnockedOut)
                    {
                        this.Parent.Mass += (source as Beam).weapon.MassDamage;
                        this.Parent.velocityMaximum = this.Parent.Thrust / this.Parent.Mass;
                        this.Parent.speed = this.Parent.velocityMaximum;
                        this.Parent.rotationRadiansPerSecond = this.Parent.speed / 700f;
                    }
                    if ((source as Beam).weapon.RepulsionDamage > 0f && !this.Parent.IsTethered() && !this.Parent.EnginesKnockedOut)
                    {
                        this.Parent.Velocity += ((this.Center - (source as Beam).Owner.Center) * (source as Beam).weapon.RepulsionDamage) / this.Parent.Mass;
                    }
                }
                if (this.shield_power_max > 0f && (!this.isExternal || this.quadrant <=0))
                {
                    return false;
                }

                if (this.ModuleType == ShipModuleType.Armor && source is Projectile)
                {
                    if ((source as Projectile).isSecondary)
                    {
                        Weapon shooter = (source as Projectile).weapon;
                        ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                        damageAmount *= shooter.EffectVsArmor; 
                        //damageAmount *= (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVsArmor);
                    }
                    else
                    {
                        damageAmount *= (source as Projectile).weapon.EffectVsArmor;
                    }
                }
                else if (this.ModuleType == ShipModuleType.Armor && source is Beam)
                {
                    damageAmount *= (source as Beam).weapon.EffectVsArmor;
                }

                if (damageAmount > this.Health)
                {
                    this.Health = 0;
                }
                else
                {
                    this.Health -= damageAmount;
                }
                if (base.Health >= this.HealthMax)
                {
                    base.Health = this.HealthMax;
                    this.Active = true;
                    this.onFire = false;
                }
                if (base.Health / this.HealthMax < 0.5f)
                {
                    this.onFire = true;
                }
                if ((this.Parent.Health / this.Parent.HealthMax) < 0.5 && base.Health < 0.5 * (this.HealthMax))
                {
                    this.reallyFuckedUp = true;
                }
                foreach (ShipModule dummy in this.LinkedModulesList)
                {
                    dummy.DamageDummy(damageAmount);
                }
            }
            else
            {
                //Damage module health if shields fail from damage
                float damageAmountvsShields = damageAmount;

                if (source is Projectile)
                {
                    if ((source as Projectile).isSecondary)
                    {
                        Weapon shooter = (source as Projectile).weapon;
                        ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                        damageAmountvsShields *= shooter.EffectVSShields; 
                        //damageAmountvsShields *= (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVSShields);
                    }
                    else
                    {
                        damageAmountvsShields *= (source as Projectile).weapon.EffectVSShields;
                    }
                }
                else if (source is Beam)
                {
                    damageAmountvsShields *= (source as Beam).weapon.EffectVSShields;
                }


                if (source is Projectile)
                    damageAmount = ApplyResistances((source as Projectile).weapon, damageAmount);
                else if (source is Beam)
                    damageAmount = ApplyResistances((source as Beam).weapon, damageAmount);

                if (damageAmountvsShields <= this.shield_threshold)
                    damageAmountvsShields = 0f;

                if (damageAmountvsShields > this.shield_power)
                {
                    this.shield_power = 0;
                    this.Parent.UpdateShields();
                }
                else
                {
                    this.shield_power -= damageAmountvsShields;
                    this.Parent.UpdateShields();
                }

                if (ShipModule.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && this.Parent.InFrustum)
                {
                    this.shield.Rotation = source.Rotation - 3.14159274f;
                    this.shield.displacement = 0f;
                    this.shield.texscale = 2.8f;
                    shield.pointLight.Refresh(Empire.Universe);

                    if (source is Beam)
                    {
                        if ((source as Beam).weapon.SiphonDamage > 0f)
                        {
                            this.shield_power -= (source as Beam).weapon.SiphonDamage;
                            if (this.shield_power < 0f)
                            {
                                this.shield_power = 0f;
                            }
                            (source as Beam).owner.PowerCurrent += (source as Beam).weapon.SiphonDamage;
                            if ((source as Beam).owner.PowerCurrent > (source as Beam).owner.PowerStoreMax)
                            {
                                (source as Beam).owner.PowerCurrent = (source as Beam).owner.PowerStoreMax;
                            }
                        }
                        this.shield.Rotation = Center.RadiansToTarget((source as Beam).Source);
                        this.shield.pointLight.World = Matrix.CreateTranslation(new Vector3((source as Beam).ActualHitDestination, 0f));
                        this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        this.shield.pointLight.Radius = this.shield_radius * 2f;
                        this.shield.pointLight.Intensity = RandomMath.RandomBetween(4f, 10f);
                        this.shield.displacement = 0f;
                        this.shield.Radius = this.Radius;
                        this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.texscale = 2.8f;
                        this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.pointLight.Enabled = true;
                        Vector2 vel = (source as Beam).Source - this.Center;
                        vel = Vector2.Normalize(vel);
                        if (RandomMath.RandomBetween(0f, 100f) > 90f && this.Parent.InFrustum)
                        {
                            ShipModule.universeScreen.flash.AddParticleThreadA(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), Vector3.Zero);
                        }
                        if (this.Parent.InFrustum)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                ShipModule.universeScreen.sparks.AddParticleThreadA(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                            }
                        }
                        if ((source as Beam).weapon.SiphonDamage > 0f)
                        {
                            this.shield_power -= (source as Beam).weapon.SiphonDamage;
                            if (this.shield_power < 0f)
                                this.shield_power = 0f;
                        }
                    }
                    else if (source is Projectile && !(source as Projectile).IgnoresShields && this.Parent.InFrustum)
                    {
                        Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
                        shieldcue.Apply3D(ShipModule.universeScreen.listener, this.Parent.emitter);
                        shieldcue.Play();
                        this.shield.Radius = this.Radius;
                        this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.texscale = 2.8f;
                        this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.pointLight.World = (source as Projectile).GetWorld();
                        this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        this.shield.pointLight.Radius = this.Radius;
                        this.shield.pointLight.Intensity = 8f;
                        this.shield.pointLight.Enabled = true;
                        Vector2 vel = Vector2.Normalize((source as Projectile).Center - this.Center);
                        ShipModule.universeScreen.flash.AddParticleThreadB(new Vector3((source as Projectile).Center, this.Center3D.Z), Vector3.Zero);
                        for (int i = 0; i < 20; i++)
                        {
                            ShipModule.universeScreen.sparks.AddParticleThreadB(new Vector3((source as Projectile).Center, this.Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                        }
                    }
                }
            }
            //Added by McShooterz: shields keep charge when manually turned off
            if (this.shield_power > 0f && !shieldsOff)
            {
                this.Radius = this.shield_radius;
            }
            else
            {
                this.Radius = 8f;
            }
            return true;
        }

        public bool DamageDummy(float damageAmount)
        {
            this.Health -= damageAmount;
            return true;
        }

        public void DamageInvisible(GameplayObject source, float damageAmount)
        {
            if (source is Projectile)
            {
                this.Parent.LastDamagedBy = source;
                if (this.Parent.shipData.Role == ShipData.RoleName.fighter && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
                {
                    damageAmount = damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
                }
            }
            if (source is Ship && (source as Ship).shipData.Role == ShipData.RoleName.fighter && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
            {
                damageAmount = damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
            }
            
            this.Parent.InCombatTimer = 15f;
            if (this.ModuleType == ShipModuleType.Dummy)
            {
                this.ParentOfDummy.DamageInvisible(source, damageAmount);
                return;
            }
            //Added by McShooterz: shields keep charge when manually turned off
            if (this.shield_power <= 0f || shieldsOff)
            {
                if (source is Projectile)
                    damageAmount = ApplyResistances((source as Projectile).weapon, damageAmount);

                if (source is Beam)
                    damageAmount = ApplyResistances((source as Beam).weapon, damageAmount);

                //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
                if (damageAmount <= this.DamageThreshold)
                    damageAmount = 0f;

                if (source is Projectile && (source as Projectile).weapon.EMPDamage > 0f)
                {
                    this.Parent.EMPDamage = this.Parent.EMPDamage + (source as Projectile).weapon.EMPDamage;
                }
                if (source is Beam)
                {
                    Vector2 vel = (source as Beam).Source - this.Center;
                    vel = Vector2.Normalize(vel);
                    if (RandomMath.RandomBetween(0f, 100f) > 90f && this.Parent.InFrustum)
                    {
                        ShipModule.universeScreen.flash.AddParticleThreadB(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), Vector3.Zero);
                    }
                    if (this.Parent.InFrustum)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            ShipModule.universeScreen.sparks.AddParticleThreadB(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                        }
                    }
                    if ((source as Beam).weapon.PowerDamage > 0f)
                    {
                        this.Parent.PowerCurrent = this.Parent.PowerCurrent - (source as Beam).weapon.PowerDamage;
                        if (this.Parent.PowerCurrent < 0f)
                        {
                            this.Parent.PowerCurrent = 0f;
                        }
                    }
                    if ((source as Beam).weapon.TroopDamageChance > 0f)
                    {
                        if (this.Parent.TroopList.Count > 0)
                        {
                            if (UniverseRandom.RandomBetween(0f, 100f) < (source as Beam).weapon.TroopDamageChance)
                            {
                                this.Parent.TroopList[0].Strength = this.Parent.TroopList[0].Strength - 1;
                                if (this.Parent.TroopList[0].Strength <= 0)
                                {
                                    this.Parent.TroopList.RemoveAt(0);
                                }
                            }
                        }
                        else if (this.Parent.MechanicalBoardingDefense > 0f && RandomMath.RandomBetween(0f, 100f) < (source as Beam).weapon.TroopDamageChance)
                        {
                            this.Parent.MechanicalBoardingDefense -= 1f;
                        }
                    }
                    if ((source as Beam).weapon.MassDamage > 0f && !this.Parent.IsTethered() && !this.Parent.EnginesKnockedOut)
                    {
                        this.Parent.Mass = this.Parent.Mass + (source as Beam).weapon.MassDamage;
                        this.Parent.velocityMaximum = this.Parent.Thrust / this.Parent.Mass;
                        this.Parent.speed = this.Parent.velocityMaximum;
                        this.Parent.rotationRadiansPerSecond = this.Parent.speed / 700f;
                    }
                    if ((source as Beam).weapon.RepulsionDamage > 0f && !this.Parent.IsTethered() && !this.Parent.EnginesKnockedOut)
                    {
                        this.Parent.Velocity = this.Parent.Velocity + (((this.Center - (source as Beam).Owner.Center) * (source as Beam).weapon.RepulsionDamage) / this.Parent.Mass);
                    }
                }
                //Added by McShooterz: shields keep charge when manually turned off

                if (this.shield_power <= 0f || shieldsOff)
                {
                    if (source is Projectile && this.ModuleType == ShipModuleType.Armor)
                    {
                        if ((source as Projectile).isSecondary)
                        {
                            Weapon shooter = (source as Projectile).weapon;
                            ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                            damageAmount *= shooter.EffectVsArmor;  // (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVsArmor);
                        }
                        else
                        {
                            damageAmount *= (source as Projectile).weapon.EffectVsArmor;
                        }
                    }
                    //ShipModule health = this;
                    this.Health = this.Health - damageAmount;
                }
                else
                {
                    //ShipModule shieldPower = this;
                    this.shield_power = this.shield_power - damageAmount;
                    if (this.shield_power < 0f)
                    {
                        this.shield_power = 0f;
                    }
                }
                if (base.Health >= this.HealthMax)
                {
                    base.Health = this.HealthMax;
                    this.Active = true;
                    this.onFire = false;
                }
                foreach (ShipModule dummy in this.LinkedModulesList)
                {
                    dummy.DamageDummy(damageAmount);
                }
            }
            else
            {
                //ShipModule shipModule = this;
                if (source is Projectile)
                {
                    if ((source as Projectile).isSecondary)
                    {
                        Weapon shooter = (source as Projectile).weapon;
                        ResourceManager.WeaponsDict.TryGetValue(shooter.SecondaryFire, out shooter);
                        damageAmount *= shooter.EffectVSShields; 
                        //damageAmount *= (ResourceManager.GetWeapon(shooter.SecondaryFire).EffectVSShields);
                    }
                    else
                    {
                        damageAmount *= (source as Projectile).weapon.EffectVSShields;
                    }
                }
                else if (source is Beam)
                {
                    damageAmount *= (source as Beam).weapon.EffectVSShields;
                }

                if (source is Projectile)
                    damageAmount = ApplyShieldResistances((source as Projectile).weapon, damageAmount);
                else if (source is Beam)
                    damageAmount = ApplyShieldResistances((source as Beam).weapon, damageAmount);

                if (damageAmount <= this.shield_threshold)
                    damageAmount = 0f;

                this.shield_power = this.shield_power - damageAmount;
                if (Empire.Universe.viewState == UniverseScreen.UnivScreenState.ShipView && this.Parent.InFrustum)
                {
                    this.shield.Rotation = source.Rotation - 3.14159274f;
                    this.shield.displacement = 0f;
                    this.shield.texscale = 2.8f;
                    shield.pointLight.Refresh(Empire.Universe);

                    if (source is Beam)
                    {
                        this.shield.Rotation = Center.RadiansToTarget((source as Beam).Source);
                        this.shield.pointLight.World = Matrix.CreateTranslation(new Vector3((source as Beam).ActualHitDestination, 0f));
                        this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        this.shield.pointLight.Radius = this.shield_radius * 2f;
                        this.shield.pointLight.Intensity = RandomMath.RandomBetween(4f, 10f);
                        this.shield.displacement = 0f;
                        this.shield.Radius = this.Radius;
                        this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.texscale = 2.8f;
                        this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.pointLight.Enabled = true;
                        Vector2 vel = (source as Beam).Source - this.Center;
                        vel = Vector2.Normalize(vel);
                        if (RandomMath.IntBetween(0, 100) > 90 && Parent.InFrustum)
                        {
                            Empire.Universe.flash.AddParticleThreadA(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), Vector3.Zero);
                        }
                        if (this.Parent.InFrustum)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Empire.Universe.sparks.AddParticleThreadA(new Vector3((source as Beam).ActualHitDestination, this.Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                            }
                        }
                        if ((source as Beam).weapon.SiphonDamage > 0f)
                        {
                            //ShipModule shieldPower1 = this;
                            this.shield_power = this.shield_power - (source as Beam).weapon.SiphonDamage;
                            if (this.shield_power < 0f)
                            {
                                this.shield_power = 0f;
                            }
                            (source as Beam).owner.PowerCurrent += (source as Beam).weapon.SiphonDamage;
                        }
                    }
                    else if (source is Projectile && !(source as Projectile).IgnoresShields && this.Parent.InFrustum)
                    {
                        Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
                        shieldcue.Apply3D(ShipModule.universeScreen.listener, this.Parent.emitter);
                        shieldcue.Play();
                        this.shield.Radius = this.Radius;
                        this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.texscale = 2.8f;
                        this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.pointLight.World = (source as Projectile).GetWorld();
                        this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        this.shield.pointLight.Radius = this.Radius;
                        this.shield.pointLight.Intensity = 8f;
                        this.shield.pointLight.Enabled = true;
                        Vector2 vel = Vector2.Normalize((source as Projectile).Center - this.Center);
                        ShipModule.universeScreen.flash.AddParticleThreadB(new Vector3((source as Projectile).Center, this.Center3D.Z), Vector3.Zero);
                        for (int i = 0; i < 20; i++)
                        {
                            ShipModule.universeScreen.sparks.AddParticleThreadB(new Vector3((source as Projectile).Center, this.Center3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                        }
                    }
                }
            }
            //Added by McShooterz: shields keep charge when manually turned off
            if (this.shield_power > 0f && !shieldsOff)
            {
                this.Radius = this.shield_radius;
            }
            else
            {
                this.Radius = 8f;
            }
            if (base.Health <= 0f)
            {
                this.Die(source, true);
            }
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            DebugInfoScreen.ModulesDied += 1;
            if (!isDummy)
            {
                foreach (ShipModule link in LinkedModulesList)
                {
                    if (!link.Active)
                        continue;
                    link.Die(source, true);
                }
            }
            if (shield_power_max > 0f)
                Health = 0f;

            SetNewExternals();
            Health = 0f;
            Vector3 center = new Vector3(Center.X, Center.Y, -100f);

            SolarSystem inSystem = Parent.System;
            if (Active && Parent.InFrustum)
            {
                bool parentAlive = !Parent.dying;
                for (int i = 0; i < 30; ++i)
                {
                    Vector3 pos = parentAlive ? center : new Vector3(Parent.Center, UniverseRandom.RandomBetween(-25f, 25f));
                    universeScreen.explosionParticles.AddParticleThreadA(pos, Vector3.Zero);
                }
            }
            base.Die(source, cleanupOnly);

            int size = XSIZE * YSIZE;
            if (!cleanupOnly)
            {
                if (Parent.Active && Parent.InFrustum && universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
                {
                    audioListener.Position = universeScreen.camPos;
                    AudioManager.PlayCue("sd_explosion_module_small", audioListener, Parent.emitter);
                }
                if (explodes)
                {
                    var mgr = inSystem?.spatialManager ?? UniverseScreen.DeepSpaceManager;
                    mgr.ExplodeAtModule(Parent.LastDamagedBy, this, damageAmount:size*2500, damageRadius:size*64);
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

        public void Draw(SpriteBatch spriteBatch)
        {
        }

        public Ship GetHangarShip()
        {
            return hangarShip;
        }

        public Ship GetParent()
        {
            return Parent;
        }

        public void Initialize(Vector2 pos)
        {
            DebugInfoScreen.ModulesCreated++;
            XMLPosition = pos;
            Radius = 8f;
            Position = pos;
            Dimensions = new Vector2(16f, 16f);
            Vector2 relativeShipCenter = new Vector2(512f, 512f);
            moduleCenter.X = pos.X + 256f;
            moduleCenter.Y = pos.Y + 256f;

            distanceToParentCenter = relativeShipCenter.Distance(moduleCenter);
            offsetAngle = relativeShipCenter.AngleToTarget(moduleCenter);
            SetInitialPosition();
            SetAttributesByType();

            if (Parent?.loyalty != null)
            {
                float max = HealthMax;
                if (ModuleType != ShipModuleType.Dummy)
                    max = ResourceManager.GetModuleTemplate(UID).HealthMax;
                else if(!string.IsNullOrEmpty(ParentOfDummy?.UID))
                    max = ResourceManager.GetModuleTemplate(ParentOfDummy.UID).HealthMax;
                HealthMax = max + max * Parent.loyalty.data.Traits.ModHpModifier;
                Health    = Math.Min(Health, HealthMax);     //Gretman (Health bug fix)
            }

            LinkedModulesList.Capacity = XSIZE * YSIZE;
            if (XSIZE > 1)
            {
                for (int xs = XSIZE; xs > 1; xs--)
                {
                    ShipModule dummy = new ShipModule();
                    dummy.XMLPosition   = XMLPosition;
                    dummy.XMLPosition.X = dummy.XMLPosition.X + (16 * (xs - 1));
                    dummy.isDummy       = true;
                    dummy.ParentOfDummy = this;
                    dummy.Mass          = 0f;
                    dummy.Parent        = Parent;
                    dummy.Health        = Health;
                    dummy.HealthMax     = HealthMax;
                    dummy.ModuleType    = ShipModuleType.Dummy;
                    dummy.Initialize();
                    LinkedModulesList.Add(dummy);
                    if (YSIZE > 1)
                    {
                        for (int ys = YSIZE; ys > 1; ys--)
                        {
                            dummy = new ShipModule();
                            dummy.ParentOfDummy = this;
                            dummy.XMLPosition.X = XMLPosition.X + (16 * (xs - 1));
                            dummy.XMLPosition.Y = XMLPosition.Y + (16 * (ys - 1));
                            dummy.isDummy       = true;
                            dummy.Mass          = 0f;
                            dummy.Parent        = Parent;
                            dummy.Health        = Health;
                            dummy.HealthMax     = HealthMax;
                            dummy.ModuleType    = ShipModuleType.Dummy;
                            dummy.Initialize();
                            LinkedModulesList.Add(dummy);
                        }
                    }
                }
            }
            if (YSIZE > 1)
            {
                for (int ys = YSIZE; ys > 1; ys--)
                {
                    ShipModule dummy = new ShipModule();
                    dummy.XMLPosition   = XMLPosition;
                    dummy.XMLPosition.Y = dummy.XMLPosition.Y + (16 * (ys - 1));
                    dummy.isDummy       = true;
                    dummy.ParentOfDummy = this;
                    dummy.Mass          = 0f;
                    dummy.Parent        = Parent;
                    dummy.Health        = Health;
                    dummy.HealthMax     = HealthMax;
                    dummy.ModuleType    = ShipModuleType.Dummy;
                    dummy.Initialize();
                    LinkedModulesList.Add(dummy);
                }
            }
            if (!isDummy)
            {
                foreach (ShipModule module in LinkedModulesList)
                {
                    module.Parent          = Parent;
                    module.System          = Parent.System;
                    module.Dimensions      = Dimensions;
                    module.IconTexturePath = IconTexturePath;
                    foreach (ModuleSlot slot in Parent.ModuleSlotList)
                    {
                        if (slot.Position != module.XMLPosition)
                            continue;
                        slot.module = module;
                        break;
                    }
                    module.Initialize(module.XMLPosition);
                }
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
            if (this.IsTroopBay && this.Powered)
            {
                if (this.hangarShip != null)
                {
                    //this.hangarShip.GetAI().State == AIState.AssaultPlanet || this.hangarShip.GetAI().State == AIState.Boarding ||
                    if (this.hangarShip.GetAI().State == AIState.ReturnToHangar || this.hangarShip.GetAI().EscortTarget != null || this.hangarShip.GetAI().OrbitTarget != null)
                        return;
                    this.hangarShip.DoEscort(this.Parent);
                    return;
                }
                if (Ship_Game.ResourceManager.ShipsDict["Assault_Shuttle"].Mass / 5f > this.Parent.Ordinance)  //fbedard: New spawning cost
                    return;
                if (this.hangarTimer <= 0f && this.hangarShip == null)
                {
                    this.hangarShip = ResourceManager.CreateTroopShipAtPoint("Assault_Shuttle", this.Parent.loyalty, this.Center, troop);
                    this.hangarShip.VanityName = "Assault Shuttle";
                    this.hangarShip.Mothership = this.Parent;
                    this.hangarShip.DoEscort(this.Parent);
                    this.hangarShip.Velocity = UniverseRandom.RandomDirection() * hangarShip.speed + Parent.Velocity;
                    if (this.hangarShip.Velocity.Length() > this.hangarShip.velocityMaximum)
                    {
                        this.hangarShip.Velocity = Vector2.Normalize(this.hangarShip.Velocity) * this.hangarShip.speed;
                    }
                    this.installedSlot.HangarshipGuid = this.hangarShip.guid;
                    this.hangarTimer = this.hangarTimerConstant;
                    this.Parent.Ordinance -= this.hangarShip.Mass / 5f;
                }
            }
        }


        public void LoadContent(GameContentManager contentManager)
        {
        }

        //added by gremlin fighter rearm fix
        public void ScrambleFighters()
        {
            if (IsTroopBay || IsSupplyBay || !Powered)
                return;
            if (hangarShip != null && hangarShip.Active)
            {
                if (hangarShip.GetAI().State == AIState.ReturnToHangar 
                    || hangarShip.GetAI().HasPriorityOrder 
                    || hangarShip.GetAI().hasPriorityTarget
                    || hangarShip.GetAI().IgnoreCombat 
                    || hangarShip.GetAI().Target != null
                    || hangarShip.Center.InRadius(Parent.Center, Parent.SensorRange)
                ) return;
                hangarShip.DoEscort(Parent);
                return;
            }
            if (hangarTimer > 0f || (hangarShip != null && (hangarShip == null || hangarShip.Active)))
                return;

            string hangarship    = hangarShipUID;
            string startingscout = Parent.loyalty.data.StartingShip;

            Ship temphangarship=null;
            if (!Parent.loyalty.isFaction && (hangarShipUID == startingscout 
                                                   || !Parent.loyalty.ShipsWeCanBuild.Contains(hangarShipUID)))
            {
                temphangarship = ResourceManager.ShipsDict[startingscout];
                Array<Ship> fighters = new Array<Ship>();
                foreach (string shipsWeCanBuild in this.Parent.loyalty.ShipsWeCanBuild)
                {

                    if (!this.PermittedHangarRoles.Contains(ResourceManager.ShipsDict[shipsWeCanBuild].shipData.GetRole()) 
                        || ResourceManager.ShipsDict[shipsWeCanBuild].Size > this.MaximumHangarShipSize)
                    {
                        continue;
                    }
                    Ship tempship =ResourceManager.ShipsDict[shipsWeCanBuild];
                    if(temphangarship.BaseStrength  < tempship.BaseStrength || temphangarship.Size < tempship .Size)
                        temphangarship = tempship;
                }
                this.hangarShipUID = temphangarship.Name;
                hangarship = this.hangarShipUID;
            }

            if (string.IsNullOrEmpty(hangarship) )
                return;
                   
            temphangarship = ResourceManager.ShipsDict[hangarship];
                    
            if (temphangarship.Mass / 5f > Parent.Ordinance)  //fbedard: New spawning cost
                return;
            SetHangarShip(ResourceManager.CreateShipFromHangar(temphangarship.Name, Parent.loyalty, Center, Parent));

            if (hangarShip != null)
            {
                hangarShip.DoEscort(Parent);
                hangarShip.Velocity = UniverseRandom.RandomDirection() * GetHangarShip().speed + Parent.Velocity;
                if (hangarShip.Velocity.Length() > hangarShip.velocityMaximum)
                {
                    hangarShip.Velocity = Vector2.Normalize(hangarShip.Velocity) * hangarShip.speed;
                }
                hangarShip.Mothership = Parent;
                installedSlot.HangarshipGuid = GetHangarShip().guid;

                hangarTimer = hangarTimerConstant;
                Parent.Ordinance -= hangarShip.Mass / 5f;
            }
        }

        public void SetAttributesByType()
        {
            switch (this.ModuleType)
            {
                case ShipModuleType.Turret:
                    ConfigWeapon(true);
                    this.InstalledWeapon.isTurret = true;
                    break;
                case ShipModuleType.MainGun:
                    ConfigWeapon(true);
                    this.InstalledWeapon.isMainGun = true;
                    break;
                case ShipModuleType.MissileLauncher:
                    ConfigWeapon(true);
                    break;
                case ShipModuleType.Colony:
                    this.Parent.isColonyShip = true;
                    break;
                case ShipModuleType.Bomb:
                    this.Parent.BombBays.Add(this);
                    break;
                case ShipModuleType.Drone:
                    ConfigWeapon(true);
                    break;
                case ShipModuleType.Spacebomb:
                    ConfigWeapon(true);
                    break;
            }
            this.Health = this.HealthMax;
            if (this.isDummy) return;
            if (this.shield_power_max > 0.0)
                shield = ShieldManager.AddShield(this, Rotation, Center);
            if (this.IsSupplyBay)
                this.Parent.IsSupplyShip = true;
        }

        public void SetAttributesNoParent()
        {
            switch (this.ModuleType)
            {
                case ShipModuleType.Turret:
                    ConfigWeapon(false);
                    this.InstalledWeapon.isTurret = true;
                    break;
                case ShipModuleType.MainGun:
                    ConfigWeapon(false);
                    this.InstalledWeapon.isMainGun = true;
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
            this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.GetModuleTemplate(UID).WeaponType);
            this.InstalledWeapon.moduleAttachedTo = this;
            this.InstalledWeapon.Owner = Parent;
            this.InstalledWeapon.Center = this.Center;
            this.isWeapon = true;
            if (addToParent)
                this.Parent.Weapons.Add(this.InstalledWeapon);
        }

        public void SetHangarShip(Ship ship)
        {
            hangarShip = ship;
            if(ship != null)
                installedSlot.HangarshipGuid = ship.guid;  //fbedard: save mothership
        }

        public void SetInitialPosition()
        {
            float parentFacing = Parent.Rotation * 180f / 3.14159274f;

            Vector2 position = Parent.Center;
            float angle = offsetAngle + parentFacing;
            float distance = distanceToParentCenter;
            ModuleCenter = position.PointFromAngle(angle, distance);

            Position = new Vector2(ModuleCenter.X - 8f, ModuleCenter.Y - 8f);
            Center = ModuleCenter;
        }

        public void SetModuleToCenter()
        {
            Position = Parent.Position;
            Center = Parent.Center;
            if (isWeapon)
            {
                InstalledWeapon.Center = Center;
            }
        }

        public void SetNewExternals()
        {
            this.quadrant = -1;
            ModuleSlot module;
            Vector2 up = new Vector2(this.XMLPosition.X, this.XMLPosition.Y - 16f);
            
            if (this.Parent.GetMD().TryGetValue(up, out module) && module.module.Active && (!module.module.isExternal || module.module.shield_power_max >0 ))
            {
                module.module.isExternal = true;
                module.module.quadrant = 1;
                this.Parent.ExternalSlots.Add(module);
            }
            Vector2 right = new Vector2(this.XMLPosition.X + 16f, this.XMLPosition.Y);
            if (this.Parent.GetMD().TryGetValue(right, out module) && module.module.Active && (!module.module.isExternal || module.module.shield_power_max > 0))
            {
                module.module.isExternal = true;
                module.module.quadrant = 2;
                this.Parent.ExternalSlots.Add(module);
            }
            Vector2 left = new Vector2(this.XMLPosition.X - 16f, this.XMLPosition.Y);
            if (this.Parent.GetMD().TryGetValue(left, out module) && module.module.Active && (!module.module.isExternal || module.module.shield_power_max > 0))
            {
                module.module.isExternal = true;
                module.module.quadrant = 4;
                this.Parent.ExternalSlots.Add(module);
            }
            Vector2 down = new Vector2(this.XMLPosition.X, this.XMLPosition.Y + 16f);
            if (this.Parent.GetMD().TryGetValue(down, out module) && module.module.Active && (!module.module.isExternal || module.module.shield_power_max > 0))
            {
                module.module.isExternal = true;
                module.module.quadrant = 3;
                this.Parent.ExternalSlots.Add(module);
            }
        }

        public void SetParent(Ship p)
        {
            this.Parent = p;
        }

        public override void Update(float elapsedTime)
        {
            if (base.Health > 0f && !this.Active)
            {
                this.Active = true;
                this.SetNewExternals();
                this.Parent.RecalculatePower();
            }
            else if (this.shield_power_max > 0f && !this.isExternal && this.Active)
            {
                this.quadrant = -1;
                this.isExternal = true;
            }
            if (base.Health <= 0f && this.Active)
            {
                this.Die(base.LastDamagedBy, false);
            }
            if (base.Health >= this.HealthMax)
            {
                base.Health = this.HealthMax;
                this.onFire = false;
            }

            //Added by Gretman
            if (this.ParentOfDummy != null && this.isDummy)
            {
                this.Health = this.ParentOfDummy.Health;
                this.HealthMax = this.ParentOfDummy.HealthMax;
            }

            if (!this.isDummy)
            {
                this.BombTimer -= elapsedTime;
                //Added by McShooterz: shields keep charge when manually turned off
                if (this.shield_power <= 0f || shieldsOff)
                {
                    this.Radius = 8f;
                }
                else
                    this.Radius = this.shield_radius;
                if (this.ModuleType == ShipModuleType.Hangar && this.Active) //(this.hangarShip == null || !this.hangarShip.Active) && 
                    this.hangarTimer -= elapsedTime;
                //Shield Recharge
                float shieldMax = this.GetShieldsMax();
                if (this.Active && this.Powered && this.shield_power < shieldMax)
                {
                    if (this.Parent.ShieldRechargeTimer > this.shield_recharge_delay)
                        this.shield_power += this.shield_recharge_rate * elapsedTime;
                    else if (this.shield_power > 0)
                        this.shield_power += this.shield_recharge_combat_rate * elapsedTime;
                    if (this.shield_power > shieldMax)
                        this.shield_power = shieldMax;
                }
                if (this.shield_power < 0f)
                {
                    this.shield_power = 0f;
                }
                if (this.TransporterTimer > 0)
                    this.TransporterTimer -= elapsedTime;
            }
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
            reallyFuckedUp = Parent.percent < 0.5f && Health / HealthMax < 0.25f;

            HandleDamageFireTrail(elapsedTime);
            Rotation = Parent.Rotation;
        }

        private void HandleDamageFireTrail(float elapsedTime)
        {
            if (Parent.InFrustum && Active && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (reallyFuckedUp)
                {
                    if (trailEmitter == null) trailEmitter = new ParticleEmitter(universeScreen.projectileTrailParticles, 50f, Center3D);
                    if (flameEmitter == null) flameEmitter = new ParticleEmitter(universeScreen.flameParticles, 80f, Center3D);
                    trailEmitter.Update(elapsedTime, Center3D);
                    flameEmitter.Update(elapsedTime, Center3D);
                }
                else if (onFire)
                {
                    if (trailEmitter == null)     trailEmitter     = new ParticleEmitter(universeScreen.projectileTrailParticles, 50f, Center3D);
                    if (firetrailEmitter == null) firetrailEmitter = new ParticleEmitter(universeScreen.fireTrailParticles, 60f, Center3D);
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
            foreach (ShipModule dummy in LinkedModulesList)
                dummy.Health = dummy.HealthMax;
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
    }
}