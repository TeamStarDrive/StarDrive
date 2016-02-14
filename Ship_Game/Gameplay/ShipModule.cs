using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Ship_Game.Gameplay
{
    public sealed class ShipModule : GameplayObject
	{
        public ShipModule_Advanced Advanced = null; //This is where all the other member variables went -Gretman

        private ParticleEmitter trailEmitter;
        private ParticleEmitter firetrailEmitter;
        private ParticleEmitter flameEmitter;
		public byte XSIZE = 1;
        public byte YSIZE = 1;
		public List<string> PermittedHangarRoles;
		public bool Powered;
        public bool isDummy;
        public List<ShipModule> LinkedModulesList = new List<ShipModule>();
        public static UniverseScreen universeScreen;
        private float distanceToParentCenter;
		private float offsetAngle;
        public float FieldOfFire;//
        public float facing;
        public Vector2 XMLPosition;
        private Ship Parent;
        public ShipModule ParentOfDummy;
		public float HealthMax;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public Ship_Game.Gameplay.Restrictions Restrictions;
        public float shield_power;
        public bool shieldsOff=false;
		private Shield shield;
		public string hangarShipUID;
		private Ship hangarShip;
        public float hangarTimer;
		public bool isWeapon;
        public Weapon InstalledWeapon;
        public short OrdinanceCapacity;
        private bool onFire;
        private bool reallyFuckedUp;
        private Vector3 Center3D = new Vector3();
        public float BombTimer;
		public ShipModuleType ModuleType;
        public Vector2 moduleCenter = new Vector2(0f, 0f);
        public Vector2 ModuleCenter;
        public string IconTexturePath;
        public string UID;
        public ModuleSlot installedSlot;
        public bool isExternal;
        public int TargetValue=0;
        public sbyte quadrant = -1;

        public bool IsWeapon
		{
			get
			{
				if (this.ModuleType != ShipModuleType.Spacebomb && this.ModuleType != ShipModuleType.Turret && this.ModuleType != ShipModuleType.MainGun && this.ModuleType != ShipModuleType.MissileLauncher && this.ModuleType != ShipModuleType.Drone && this.ModuleType != ShipModuleType.Bomb)
				{
					return false;
				}
				return true;
			}
		}

		public ShipModule()     //Constructor
		{
            System.Diagnostics.Debug.WriteLine("ShipModule Created.");
        }

		public ShipModule(ShipModuleType type)  //Constructor that is not called anywhere
		{
			this.ModuleType = type;
		}

		public void Clear()
		{
			this.LinkedModulesList.Clear();
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
            {

                if (psource.weapon.Tag_Kinetic)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.KineticResist);
                }
                if (psource.weapon.Tag_Energy)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.EnergyResist);
                }
                if (psource.weapon.Tag_Guided)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.GuidedResist);
                }
                if (psource.weapon.Tag_Missile)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.MissileResist);
                }
                if (psource.weapon.Tag_Hybrid)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.HybridResist);
                }
                if (psource.weapon.Tag_Intercept)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.InterceptResist);
                }
                if (psource.weapon.Tag_Explosive)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.ExplosiveResist);
                }
                if (psource.weapon.Tag_Railgun)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.RailgunResist);
                }
                if (psource.weapon.Tag_SpaceBomb)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.SpaceBombResist);
                }
                if (psource.weapon.Tag_Bomb)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BombResist);
                }
                if (psource.weapon.Tag_BioWeapon)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BioWeaponResist);
                }
                if (psource.weapon.Tag_Drone)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.DroneResist);
                }
                if (psource.weapon.Tag_Warp)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.WarpResist);
                }
                if (psource.weapon.Tag_Torpedo)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.TorpedoResist);
                }
                if (psource.weapon.Tag_Cannon)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.CannonResist);
                }
                if (psource.weapon.Tag_Subspace)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.SubspaceResist);
                }
                if (psource.weapon.Tag_PD)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.PDResist);
                }
                if (psource.weapon.Tag_Flak)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.FlakResist);
                } 
            }


            if (bsource !=null && bsource.weapon.Tag_Beam)
            {
                damageAmount = damageAmount - (damageAmount * this.Advanced.BeamResist);

                // most of these simply don't apply to beam weapons, but calculated within this if to ensure different beam types also have resistances/vulnerabilities applied

                if (bsource.weapon.Tag_Kinetic)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.KineticResist);
                }
                if (bsource.weapon.Tag_Energy)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.EnergyResist);
                }
                if (bsource.weapon.Tag_Guided)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.GuidedResist);
                }
                if (bsource.weapon.Tag_Missile)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.MissileResist);
                }
                if (bsource.weapon.Tag_Hybrid)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.HybridResist);
                }
                if (bsource.weapon.Tag_Intercept)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.InterceptResist);
                }
                if (bsource.weapon.Tag_Explosive)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.ExplosiveResist);
                }
                if (bsource.weapon.Tag_Railgun)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.RailgunResist);
                }
                if (bsource.weapon.Tag_SpaceBomb)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.SpaceBombResist);
                }
                if (bsource.weapon.Tag_Bomb)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BombResist);
                }
                if (bsource.weapon.Tag_BioWeapon)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BioWeaponResist);
                }
                if (bsource.weapon.Tag_Drone)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.DroneResist);
                }
                if (bsource.weapon.Tag_Warp)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.WarpResist);
                }
                if (bsource.weapon.Tag_Torpedo)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.TorpedoResist);
                }
                if (bsource.weapon.Tag_Cannon)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.CannonResist);
                }
                if (bsource.weapon.Tag_Subspace)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.SubspaceResist);
                }
                if (bsource.weapon.Tag_PD)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.PDResist);
                }
                if (bsource.weapon.Tag_Flak)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.FlakResist);
                }

            }

            //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
            if (damageAmount <= this.Advanced.DamageThreshold)
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
					Ship parent = this.Parent;
					parent.EMPDamage = parent.EMPDamage + (source as Projectile).weapon.EMPDamage;
				}
				if (this.Advanced.shield_power_max > 0f && (!this.isExternal || this.quadrant <=0))
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
                {
                    if (psource.weapon.Tag_Kinetic)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_kinetic_resist);
                    }
                    if (psource.weapon.Tag_Energy)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_energy_resist);
                    }
                    if (psource.weapon.Tag_Explosive)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_explosive_resist);
                    }
                    if (psource.weapon.Tag_Missile)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_missile_resist);
                    }
                    if (psource.weapon.Tag_Flak)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_flak_resist);
                    }
                    if (psource.weapon.Tag_Hybrid)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_hybrid_resist);
                    }
                    if (psource.weapon.Tag_Railgun)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_railgun_resist);
                    }
                    if (psource.weapon.Tag_Subspace)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_subspace_resist);
                    }
                    if (psource.weapon.Tag_Warp)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_warp_resist);
                    }
                }
                else if (bsource != null)
                {
                    if (bsource.weapon.Tag_Kinetic)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_kinetic_resist);
                    }
                    if (bsource.weapon.Tag_Energy)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_energy_resist);
                    }
                    if (bsource.weapon.Tag_Explosive)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_explosive_resist);
                    }
                    if (bsource.weapon.Tag_Missile)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_missile_resist);
                    }
                    if (bsource.weapon.Tag_Flak)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_flak_resist);
                    }
                    if (bsource.weapon.Tag_Hybrid)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_hybrid_resist);
                    }
                    if (bsource.weapon.Tag_Railgun)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_railgun_resist);
                    }
                    if (bsource.weapon.Tag_Subspace)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_subspace_resist);
                    }
                    if (bsource.weapon.Tag_Warp)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_warp_resist);
                    }
                }

                if (damageAmountvsShields <= this.Advanced.shield_threshold)
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
					base.findAngleToTarget(this.Parent.Center, source.Center);
					this.shield.Rotation = source.Rotation - 3.14159274f;
					this.shield.displacement = 0f;
					this.shield.texscale = 2.8f;
					lock (GlobalStats.ObjectManagerLocker)
					{
						ShipModule.universeScreen.ScreenManager.inter.LightManager.Remove(this.shield.pointLight);
						ShipModule.universeScreen.ScreenManager.inter.LightManager.Submit(this.shield.pointLight);
					}
                    if (psource != null && !psource.IgnoresShields && this.Parent.InFrustum)
					{
						Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
						shieldcue.Apply3D(ShipModule.universeScreen.listener, this.Parent.emitter);
						shieldcue.Play();
						this.shield.Radius = this.radius;
						this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
						this.shield.texscale = 2.8f;
						this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.pointLight.World = psource.GetWorld();
						this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
						this.shield.pointLight.Radius = this.radius;
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
				this.radius = this.Advanced.shield_radius;
			}
			else
			{
				this.radius = 8f;
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
                {
                    Weapon weapon = (source as Projectile).weapon;
                    if (weapon.Tag_Kinetic)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.KineticResist);
                    }
                    if (weapon.Tag_Energy)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.EnergyResist);
                    }
                    if (weapon.Tag_Guided)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.GuidedResist);
                    }
                    if (weapon.Tag_Missile)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.MissileResist);
                    }
                    if (weapon.Tag_Hybrid)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.HybridResist);
                    }
                    if (weapon.Tag_Intercept)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.InterceptResist);
                    }
                    if (weapon.Tag_Explosive)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.ExplosiveResist);
                    }
                    if (weapon.Tag_Railgun)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.RailgunResist);
                    }
                    if (weapon.Tag_SpaceBomb)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.SpaceBombResist);
                    }
                    if (weapon.Tag_Bomb)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.BombResist);
                    }
                    if (weapon.Tag_BioWeapon)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.BioWeaponResist);
                    }
                    if (weapon.Tag_Drone)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.DroneResist);
                    }
                    if (weapon.Tag_Warp)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.WarpResist);
                    }
                    if (weapon.Tag_Torpedo)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.TorpedoResist);
                    }
                    if (weapon.Tag_Cannon)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.CannonResist);
                    }
                    if (weapon.Tag_Subspace)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.SubspaceResist);
                    }
                    if (weapon.Tag_PD)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.PDResist);
                    }
                    if (weapon.Tag_Flak)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.FlakResist);
                    }
                }


                if (source is Beam && (source as Beam).weapon.Tag_Beam)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BeamResist);
                    Weapon weapon = (source as Beam).weapon;
                    // most of these simply don't apply to beam weapons, but calculated within this if to ensure different beam types also have resistances/vulnerabilities applied

                    if (weapon.Tag_Kinetic)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.KineticResist);
                    }
                    if (weapon.Tag_Energy)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.EnergyResist);
                    }
                    if (weapon.Tag_Guided)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.GuidedResist);
                    }
                    if (weapon.Tag_Missile)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.MissileResist);
                    }
                    if (weapon.Tag_Hybrid)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.HybridResist);
                    }
                    if (weapon.Tag_Intercept)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.InterceptResist);
                    }
                    if (weapon.Tag_Explosive)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.ExplosiveResist);
                    }
                    if (weapon.Tag_Railgun)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.RailgunResist);
                    }
                    if (weapon.Tag_SpaceBomb)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.SpaceBombResist);
                    }
                    if (weapon.Tag_Bomb)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.BombResist);
                    }
                    if (weapon.Tag_BioWeapon)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.BioWeaponResist);
                    }
                    if (weapon.Tag_Drone)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.DroneResist);
                    }
                    if (weapon.Tag_Warp)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.WarpResist);
                    }
                    if (weapon.Tag_Torpedo)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.TorpedoResist);
                    }
                    if (weapon.Tag_Cannon)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.CannonResist);
                    }
                    if (weapon.Tag_Subspace)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.SubspaceResist);
                    }
                    if (weapon.Tag_PD)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.PDResist);
                    }
                    if (weapon.Tag_Flak)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.FlakResist);
                    }

                }

                //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
                if (damageAmount <= this.Advanced.DamageThreshold)
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
                    Ship parent = this.Parent;
                    parent.EMPDamage = parent.EMPDamage + (source as Projectile).weapon.EMPDamage;
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
                            if (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, 100f) < (source as Beam).weapon.TroopDamageChance)
                            {
                                Troop item = this.Parent.TroopList[0];
                                item.Strength = item.Strength - 1;
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
                        this.Parent.Mass += (source as Beam).weapon.MassDamage;
                        this.Parent.velocityMaximum = this.Parent.Thrust / this.Parent.Mass;
                        this.Parent.speed = this.Parent.velocityMaximum;
                        this.Parent.rotationRadiansPerSecond = this.Parent.speed / 700f;
                    }
                    if ((source as Beam).weapon.RepulsionDamage > 0f && !this.Parent.IsTethered() && !this.Parent.EnginesKnockedOut)
                    {
                        Vector2 vtt = this.Center - (source as Beam).Owner.Center;
                        Ship velocity = this.Parent;
                        this.Parent.Velocity += (vtt * (source as Beam).weapon.RepulsionDamage) / this.Parent.Mass;
                    }
                }
                if (this.Advanced.shield_power_max > 0f && (!this.isExternal || this.quadrant <=0))
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
                {
                    Weapon weapon = (source as Projectile).weapon;
                    if (weapon.Tag_Kinetic)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_kinetic_resist);
                    }
                    if (weapon.Tag_Energy)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_energy_resist);
                    }
                    if (weapon.Tag_Explosive)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_explosive_resist);
                    }
                    if (weapon.Tag_Missile)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_missile_resist);
                    }
                    if (weapon.Tag_Flak)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_flak_resist);
                    }
                    if (weapon.Tag_Hybrid)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_hybrid_resist);
                    }
                    if (weapon.Tag_Railgun)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_railgun_resist);
                    }
                    if (weapon.Tag_Subspace)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_subspace_resist);
                    }
                    if (weapon.Tag_Warp)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_warp_resist);
                    }
                }
                else if (source is Beam)
                {
                    Weapon weapon = (source as Beam).weapon;
                    if (weapon.Tag_Kinetic)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_kinetic_resist);
                    }
                    if (weapon.Tag_Energy)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_energy_resist);
                    }
                    if (weapon.Tag_Explosive)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_explosive_resist);
                    }
                    if (weapon.Tag_Missile)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_missile_resist);
                    }
                    if (weapon.Tag_Flak)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_flak_resist);
                    }
                    if (weapon.Tag_Hybrid)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_hybrid_resist);
                    }
                    if (weapon.Tag_Railgun)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_railgun_resist);
                    }
                    if (weapon.Tag_Subspace)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_subspace_resist);
                    }
                    if (weapon.Tag_Warp)
                    {
                        damageAmountvsShields = damageAmountvsShields - (damageAmountvsShields * this.Advanced.shield_warp_resist);
                    }
                }

                if (damageAmountvsShields <= this.Advanced.shield_threshold)
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
                    base.findAngleToTarget(this.Parent.Center, source.Center);
                    this.shield.Rotation = source.Rotation - 3.14159274f;
                    this.shield.displacement = 0f;
                    this.shield.texscale = 2.8f;
                    lock (GlobalStats.ObjectManagerLocker)
                    {
                        ShipModule.universeScreen.ScreenManager.inter.LightManager.Remove(this.shield.pointLight);
                        ShipModule.universeScreen.ScreenManager.inter.LightManager.Submit(this.shield.pointLight);
                    }
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
                        this.shield.Rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Center, (source as Beam).Source));
                        this.shield.pointLight.World = Matrix.CreateTranslation(new Vector3((source as Beam).ActualHitDestination, 0f));
                        this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        this.shield.pointLight.Radius = this.Advanced.shield_radius * 2f;
                        this.shield.pointLight.Intensity = RandomMath.RandomBetween(4f, 10f);
                        this.shield.displacement = 0f;
                        this.shield.Radius = this.radius;
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
                            {
                                this.shield_power = 0f;
                            }
                        }
                    }
                    else if (source is Projectile && !(source as Projectile).IgnoresShields && this.Parent.InFrustum)
                    {
                        Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
                        shieldcue.Apply3D(ShipModule.universeScreen.listener, this.Parent.emitter);
                        shieldcue.Play();
                        this.shield.Radius = this.radius;
                        this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.texscale = 2.8f;
                        this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
                        this.shield.pointLight.World = (source as Projectile).GetWorld();
                        this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                        this.shield.pointLight.Radius = this.radius;
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
                this.radius = this.Advanced.shield_radius;
            }
            else
            {
                this.radius = 8f;
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

                if (source is Projectile && (source as Projectile).weapon.Tag_Kinetic)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.KineticResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Energy)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.EnergyResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Guided)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.GuidedResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Missile)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.MissileResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Hybrid)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.HybridResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Intercept)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.InterceptResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Explosive)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.ExplosiveResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Railgun)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.RailgunResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_SpaceBomb)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.SpaceBombResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Bomb)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BombResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_BioWeapon)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BioWeaponResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Drone)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.DroneResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Warp)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.WarpResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Torpedo)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.TorpedoResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Cannon)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.CannonResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Subspace)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.SubspaceResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_PD)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.PDResist);
                }
                if (source is Projectile && (source as Projectile).weapon.Tag_Flak)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.FlakResist);
                }


                if (source is Beam && (source as Beam).weapon.Tag_Beam)
                {
                    damageAmount = damageAmount - (damageAmount * this.Advanced.BeamResist);

                    // most of these simply don't apply to beam weapons, but calculated within this if to ensure different beam types also have resistances/vulnerabilities applied

                    if ((source as Beam).weapon.Tag_Kinetic)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.KineticResist);
                    }
                    if ((source as Beam).weapon.Tag_Energy)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.EnergyResist);
                    }
                    if ((source as Beam).weapon.Tag_Guided)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.GuidedResist);
                    }
                    if ((source as Beam).weapon.Tag_Missile)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.MissileResist);
                    }
                    if ((source as Beam).weapon.Tag_Hybrid)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.HybridResist);
                    }
                    if ((source as Beam).weapon.Tag_Intercept)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.InterceptResist);
                    }
                    if ((source as Beam).weapon.Tag_Explosive)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.ExplosiveResist);
                    }
                    if ((source as Beam).weapon.Tag_Railgun)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.RailgunResist);
                    }
                    if ((source as Beam).weapon.Tag_SpaceBomb)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.SpaceBombResist);
                    }
                    if ((source as Beam).weapon.Tag_Bomb)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.BombResist);
                    }
                    if ((source as Beam).weapon.Tag_BioWeapon)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.BioWeaponResist);
                    }
                    if ((source as Beam).weapon.Tag_Drone)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.DroneResist);
                    }
                    if ((source as Beam).weapon.Tag_Warp)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.WarpResist);
                    }
                    if ((source as Beam).weapon.Tag_Torpedo)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.TorpedoResist);
                    }
                    if ((source as Beam).weapon.Tag_Cannon)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.CannonResist);
                    }
                    if ((source as Beam).weapon.Tag_Subspace)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.SubspaceResist);
                    }
                    if ((source as Beam).weapon.Tag_PD)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.PDResist);
                    }
                    if ((source as Beam).weapon.Tag_Flak)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.FlakResist);
                    }
                }

                //Doc: If the resistance-modified damage amount is less than an armour's damage threshold, no damage is applied.
                if (damageAmount <= this.Advanced.DamageThreshold)
                    damageAmount = 0f;

				if (source is Projectile && (source as Projectile).weapon.EMPDamage > 0f)
				{
					Ship parent = this.Parent;
					parent.EMPDamage = parent.EMPDamage + (source as Projectile).weapon.EMPDamage;
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
						Ship powerCurrent = this.Parent;
						powerCurrent.PowerCurrent = powerCurrent.PowerCurrent - (source as Beam).weapon.PowerDamage;
						if (this.Parent.PowerCurrent < 0f)
						{
							this.Parent.PowerCurrent = 0f;
						}
					}
					if ((source as Beam).weapon.TroopDamageChance > 0f)
					{
						if (this.Parent.TroopList.Count > 0)
						{
							if (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, 100f) < (source as Beam).weapon.TroopDamageChance)
							{
								Troop item = this.Parent.TroopList[0];
								item.Strength = item.Strength - 1;
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
						Ship mass = this.Parent;
						mass.Mass = mass.Mass + (source as Beam).weapon.MassDamage;
						this.Parent.velocityMaximum = this.Parent.Thrust / this.Parent.Mass;
						this.Parent.speed = this.Parent.velocityMaximum;
						this.Parent.rotationRadiansPerSecond = this.Parent.speed / 700f;
					}
					if ((source as Beam).weapon.RepulsionDamage > 0f && !this.Parent.IsTethered() && !this.Parent.EnginesKnockedOut)
					{
						Vector2 vtt = this.Center - (source as Beam).Owner.Center;
						Ship velocity = this.Parent;
						velocity.Velocity = velocity.Velocity + ((vtt * (source as Beam).weapon.RepulsionDamage) / this.Parent.Mass);
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
                {
                    if ((source as Projectile).weapon.Tag_Kinetic)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_kinetic_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Energy)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_energy_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Explosive)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_explosive_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Missile)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_missile_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Flak)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_flak_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Hybrid)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_hybrid_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Railgun)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_railgun_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Subspace)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_subspace_resist);
                    }
                    if ((source as Projectile).weapon.Tag_Warp)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_warp_resist);
                    }
                }
                else if (source is Beam)
                {
                    if ((source as Beam).weapon.Tag_Kinetic)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_kinetic_resist);
                    }
                    if ((source as Beam).weapon.Tag_Energy)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_energy_resist);
                    }
                    if ((source as Beam).weapon.Tag_Explosive)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_explosive_resist);
                    }
                    if ((source as Beam).weapon.Tag_Missile)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_missile_resist);
                    }
                    if ((source as Beam).weapon.Tag_Flak)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_flak_resist);
                    }
                    if ((source as Beam).weapon.Tag_Hybrid)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_hybrid_resist);
                    }
                    if ((source as Beam).weapon.Tag_Railgun)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_railgun_resist);
                    }
                    if ((source as Beam).weapon.Tag_Subspace)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_subspace_resist);
                    }
                    if ((source as Beam).weapon.Tag_Warp)
                    {
                        damageAmount = damageAmount - (damageAmount * this.Advanced.shield_warp_resist);
                    }
                }

                if (damageAmount <= this.Advanced.shield_threshold)
                    damageAmount = 0f;

				this.shield_power = this.shield_power - damageAmount;
				if (ShipModule.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && this.Parent.InFrustum)
				{
					base.findAngleToTarget(this.Parent.Center, source.Center);
					this.shield.Rotation = source.Rotation - 3.14159274f;
					this.shield.displacement = 0f;
					this.shield.texscale = 2.8f;
					lock (GlobalStats.ObjectManagerLocker)
					{
						ShipModule.universeScreen.ScreenManager.inter.LightManager.Remove(this.shield.pointLight);
						ShipModule.universeScreen.ScreenManager.inter.LightManager.Submit(this.shield.pointLight);
					}
					if (source is Beam)
					{
						this.shield.Rotation = MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Center, (source as Beam).Source));
						this.shield.pointLight.World = Matrix.CreateTranslation(new Vector3((source as Beam).ActualHitDestination, 0f));
						this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
						this.shield.pointLight.Radius = this.Advanced.shield_radius * 2f;
						this.shield.pointLight.Intensity = RandomMath.RandomBetween(4f, 10f);
						this.shield.displacement = 0f;
						this.shield.Radius = this.radius;
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
							//ShipModule shieldPower1 = this;
							this.shield_power = this.shield_power - (source as Beam).weapon.SiphonDamage;
							if (this.shield_power < 0f)
							{
								this.shield_power = 0f;
							}
							Ship ship = (source as Beam).owner;
							ship.PowerCurrent = ship.PowerCurrent + (source as Beam).weapon.SiphonDamage;
						}
					}
					else if (source is Projectile && !(source as Projectile).IgnoresShields && this.Parent.InFrustum)
					{
						Cue shieldcue = AudioManager.GetCue("sd_impact_shield_01");
						shieldcue.Apply3D(ShipModule.universeScreen.listener, this.Parent.emitter);
						shieldcue.Play();
						this.shield.Radius = this.radius;
						this.shield.displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
						this.shield.texscale = 2.8f;
						this.shield.texscale = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);
						this.shield.pointLight.World = (source as Projectile).GetWorld();
						this.shield.pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
						this.shield.pointLight.Radius = this.radius;
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
				this.radius = this.Advanced.shield_radius;
			}
			else
			{
				this.radius = 8f;
			}
			if (base.Health <= 0f)
			{
				this.Die(source, true);
			}
		}

		public override void Die(GameplayObject source, bool cleanupOnly)
		{
			DebugInfoScreen.ModulesDied = DebugInfoScreen.ModulesDied + 1;
			if (!this.isDummy)
			{
				foreach (ShipModule link in this.LinkedModulesList)
				{
					if (!link.Active)
					{
						continue;
					}
					link.Die(source, true);
				}
			}
			if (this.Advanced.shield_power_max > 0f)
			{
				base.Health = 0f;
			}
			this.SetNewExternals();
			base.Health = 0f;
			Vector3 center = new Vector3(this.Center.X, this.Center.Y, -100f);
			if (this.Active)
			{
				((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(5f, 15f);
				if (!this.Parent.dying && this.Parent.InFrustum)
				{
					for (int i = 0; i < 30; i++)
					{
						ShipModule.universeScreen.explosionParticles.AddParticleThreadA(center, Vector3.Zero);
					}
				}
				else if (this.Parent.InFrustum)
				{
					for (int i = 0; i < 30; i++)
					{
						ShipModule.universeScreen.explosionParticles.AddParticleThreadA(new Vector3(this.Parent.Center, ((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-25f, 25f)), Vector3.Zero);
					}
				}
			}
			base.Die(source, cleanupOnly);
			if (!cleanupOnly)
			{
				if (this.Parent.Active && this.Parent.InFrustum && ShipModule.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView)
				{
					GameplayObject.audioListener.Position = ShipModule.universeScreen.camPos;
					Cue dieCue = AudioManager.GetCue("sd_explosion_module_small");
					dieCue.Apply3D(GameplayObject.audioListener, this.Parent.emitter);
					dieCue.Play();
				}
                if (this.Advanced.explodes)
				{
					if (this.Parent.GetSystem() == null)
					{
                        UniverseScreen.DeepSpaceManager.ExplodeAtModule(this.Parent.LastDamagedBy, this, (float)(2500 * this.XSIZE * this.YSIZE), (float)(this.XSIZE * this.YSIZE * 64));
					}
					else
					{
                        this.Parent.GetSystem().spatialManager.ExplodeAtModule(this.Parent.LastDamagedBy, this, (float)(2500 * this.XSIZE * this.YSIZE), (float)(this.XSIZE * this.YSIZE * 64));
					}
				}
				if (this.Advanced.PowerFlowMax > 0 || this.Advanced.PowerRadius > 0)
				{
					this.Parent.NeedRecalculate = true;
				}
			}
			if (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(0f, 100f) < 10f)
			{
				List<SpaceJunk> junk = SpaceJunk.MakeJunk(1, this.Center, this.Parent.GetSystem());
				lock (GlobalStats.ObjectManagerLocker)
				{
					foreach (SpaceJunk j in junk)
					{
						j.wasAddedToScene = true;
						ShipModule.universeScreen.ScreenManager.inter.ObjectManager.Submit(j.JunkSO);
						UniverseScreen.JunkList.Add(j);
					}
				}
			}
            //this.SetNewExternals();
		}

		public void Draw(SpriteBatch spriteBatch)
		{
		}

		public Ship GetHangarShip()
		{
			return this.hangarShip;
		}

		public Ship GetParent()
		{
			return this.Parent;
		}

		public void Initialize(Vector2 pos) //Mer
		{
            DebugInfoScreen.ModulesCreated = DebugInfoScreen.ModulesCreated + 1;
			this.XMLPosition = pos;
			this.radius = 8f;
			base.Position = pos;
			base.Dimensions = new Vector2(16f, 16f);
			Vector2 RelativeShipCenter = new Vector2(512f, 512f);
			this.moduleCenter.X = base.Position.X + 256f;
			this.moduleCenter.Y = base.Position.Y + 256f;
			this.distanceToParentCenter = (float)Math.Sqrt((double)((this.moduleCenter.X - RelativeShipCenter.X) * (this.moduleCenter.X - RelativeShipCenter.X) + (this.moduleCenter.Y - RelativeShipCenter.Y) * (this.moduleCenter.Y - RelativeShipCenter.Y)));
			float scaleFactor = 1f;
			//ShipModule shipModule = this;
			this.distanceToParentCenter = this.distanceToParentCenter * scaleFactor;
			this.offsetAngle = (float)Math.Abs(base.findAngleToTarget(RelativeShipCenter, this.moduleCenter));
			//this.offsetAngleRadians = MathHelper.ToRadians(this.offsetAngle);
			this.SetInitialPosition();
			this.SetAttributesByType();
			if (this.Parent != null && this.Parent.loyalty != null)
			{
                //bool flag = false;
                //if (this.HealthMax == base.Health)
                //    flag = true;
                this.HealthMax = this.HealthMax + this.HealthMax * this.Parent.loyalty.data.Traits.ModHpModifier;
				base.Health = base.Health + base.Health * this.Parent.loyalty.data.Traits.ModHpModifier;
                this.Health = base.Health;
                //if (flag)
                //    this.Health = this.HealthMax;
			}
			if (!this.isDummy && (this.installedSlot.state == ShipDesignScreen.ActiveModuleState.Left || this.installedSlot.state == ShipDesignScreen.ActiveModuleState.Right))
			{
				byte xsize = this.YSIZE;
				byte ysize = this.XSIZE;
				this.XSIZE = xsize;
				this.YSIZE = ysize;
			}
			if (this.XSIZE > 1)
			{
				for (int xs = this.XSIZE; xs > 1; xs--)
				{
					ShipModule dummy = new ShipModule()
					{
						XMLPosition = this.XMLPosition
					};
					dummy.XMLPosition.X = dummy.XMLPosition.X + (float)(16 * (xs - 1));
					dummy.isDummy = true;
					dummy.ParentOfDummy = this;
					dummy.Mass = 0f;
					dummy.Parent = this.Parent;
					dummy.Health = base.Health;
					dummy.HealthMax = this.HealthMax;
					dummy.ModuleType = ShipModuleType.Dummy;
					dummy.Initialize();
					this.LinkedModulesList.Add(dummy);
					if (this.YSIZE > 1)
					{
						for (int ys = this.YSIZE; ys > 1; ys--)
						{
							dummy = new ShipModule()
							{
								ParentOfDummy = this,
								XMLPosition = this.XMLPosition
							};
							dummy.XMLPosition.X = dummy.XMLPosition.X + (float)(16 * (xs - 1));
							dummy.XMLPosition.Y = dummy.XMLPosition.Y + (float)(16 * (ys - 1));
							dummy.isDummy = true;
							dummy.Mass = 0f;
							dummy.Health = base.Health;
							dummy.HealthMax = this.HealthMax;
							dummy.ModuleType = ShipModuleType.Dummy;
							dummy.Parent = this.Parent;
							dummy.Initialize();
							this.LinkedModulesList.Add(dummy);
						}
					}
				}
			}
			if (this.YSIZE > 1)
			{
				for (int ys = this.YSIZE; ys > 1; ys--)
				{
					ShipModule dummy = new ShipModule()
					{
						XMLPosition = this.XMLPosition
					};
					dummy.XMLPosition.Y = dummy.XMLPosition.Y + (float)(16 * (ys - 1));
					dummy.isDummy = true;
					dummy.ParentOfDummy = this;
					dummy.Mass = 0f;
					dummy.Parent = this.Parent;
					dummy.Health = base.Health;
					dummy.HealthMax = this.HealthMax;
					dummy.ModuleType = ShipModuleType.Dummy;
					dummy.Initialize();
					this.LinkedModulesList.Add(dummy);
				}
			}
			if (!this.isDummy)
			{
				foreach (ShipModule module in this.LinkedModulesList)
				{
					module.Parent = this.Parent;
					module.system = this.Parent.GetSystem();
					module.Dimensions = base.Dimensions;
					module.IconTexturePath = this.IconTexturePath;
					foreach (ModuleSlot slot in this.Parent.ModuleSlotList)
					{
						if (slot.Position != module.XMLPosition)
						{
							continue;
						}
						slot.module = module;
						break;
					}
					module.Initialize(module.XMLPosition);
				}
			}
            if(this.ModuleType == ShipModuleType.Hangar && !this.Advanced.IsSupplyBay)
            {
                if (this.OrdinanceCapacity == 0)
                {
                    this.OrdinanceCapacity = (short)(this.Advanced.MaximumHangarShipSize / 2);
                    if (this.OrdinanceCapacity < 50)
                        this.OrdinanceCapacity = 50;
                }     

            }
			base.Initialize();
		}
        /// <summary>
        /// Seperate module intialization for ships loaded from save games. 
        /// </summary>
        /// <param name="pos"></param>
        public void InitializeFromSave(Vector2 pos)
        {
            DebugInfoScreen.ModulesCreated = DebugInfoScreen.ModulesCreated + 1;
            this.XMLPosition = pos;
            this.radius = 8f;
            base.Position = pos;
            base.Dimensions = new Vector2(16f, 16f);
            Vector2 RelativeShipCenter = new Vector2(512f, 512f);
            this.moduleCenter.X = base.Position.X + 256f;
            this.moduleCenter.Y = base.Position.Y + 256f;
            this.distanceToParentCenter = (float)Math.Sqrt((double)((this.moduleCenter.X - RelativeShipCenter.X) * (this.moduleCenter.X - RelativeShipCenter.X) + (this.moduleCenter.Y - RelativeShipCenter.Y) * (this.moduleCenter.Y - RelativeShipCenter.Y)));
            float scaleFactor = 1f;
            //ShipModule shipModule = this;
            this.distanceToParentCenter = this.distanceToParentCenter * scaleFactor;
            this.offsetAngle = (float)Math.Abs(base.findAngleToTarget(RelativeShipCenter, this.moduleCenter));
            //this.offsetAngleRadians = MathHelper.ToRadians(this.offsetAngle);
            this.SetInitialPosition();
            this.SetAttributesByType();
            //if (this.Parent != null && this.Parent.loyalty != null)
            //{
            //    //bool flag = false;
            //    //if (this.HealthMax == base.Health)
            //    //    flag = true;
            //    this.HealthMax = this.HealthMax + this.HealthMax * this.Parent.loyalty.data.Traits.ModHpModifier;
            //    base.Health = base.Health + base.Health * this.Parent.loyalty.data.Traits.ModHpModifier;
            //    this.Health = base.Health;
            //    //if (flag)
            //    //    this.Health = this.HealthMax;
            //}
            if (!this.isDummy && (this.installedSlot.state == ShipDesignScreen.ActiveModuleState.Left || this.installedSlot.state == ShipDesignScreen.ActiveModuleState.Right))
            {
                byte xsize = this.YSIZE;
                byte ysize = this.XSIZE;
                this.XSIZE = xsize;
                this.YSIZE = ysize;
            }
            if (this.XSIZE > 1)
            {
                for (int xs = this.XSIZE; xs > 1; xs--)
                {
                    ShipModule dummy = new ShipModule()
                    {
                        XMLPosition = this.XMLPosition
                    };
                    dummy.XMLPosition.X = dummy.XMLPosition.X + (float)(16 * (xs - 1));
                    dummy.isDummy = true;
                    dummy.ParentOfDummy = this;
                    dummy.Mass = 0f;
                    dummy.Parent = this.Parent;
                    dummy.Health = base.Health;
                    dummy.HealthMax = this.HealthMax;
                    dummy.ModuleType = ShipModuleType.Dummy;
                    dummy.Initialize();
                    this.LinkedModulesList.Add(dummy);
                    if (this.YSIZE > 1)
                    {
                        for (int ys = this.YSIZE; ys > 1; ys--)
                        {
                            dummy = new ShipModule()
                            {
                                ParentOfDummy = this,
                                XMLPosition = this.XMLPosition
                            };
                            dummy.XMLPosition.X = dummy.XMLPosition.X + (float)(16 * (xs - 1));
                            dummy.XMLPosition.Y = dummy.XMLPosition.Y + (float)(16 * (ys - 1));
                            dummy.isDummy = true;
                            dummy.Mass = 0f;
                            dummy.Health = base.Health;
                            dummy.HealthMax = this.HealthMax;
                            dummy.ModuleType = ShipModuleType.Dummy;
                            dummy.Parent = this.Parent;
                            dummy.Initialize();
                            this.LinkedModulesList.Add(dummy);
                        }
                    }
                }
            }
            if (this.YSIZE > 1)
            {
                for (int ys = this.YSIZE; ys > 1; ys--)
                {
                    ShipModule dummy = new ShipModule()
                    {
                        XMLPosition = this.XMLPosition
                    };
                    dummy.XMLPosition.Y = dummy.XMLPosition.Y + (float)(16 * (ys - 1));
                    dummy.isDummy = true;
                    dummy.ParentOfDummy = this;
                    dummy.Mass = 0f;
                    dummy.Parent = this.Parent;
                    dummy.Health = base.Health;
                    dummy.HealthMax = this.HealthMax;
                    dummy.ModuleType = ShipModuleType.Dummy;
                    dummy.Initialize();
                    this.LinkedModulesList.Add(dummy);
                }
            }
            if (!this.isDummy)
            {
                foreach (ShipModule module in this.LinkedModulesList)
                {
                    module.Parent = this.Parent;
                    module.system = this.Parent.GetSystem();
                    module.Dimensions = base.Dimensions;
                    module.IconTexturePath = this.IconTexturePath;
                    foreach (ModuleSlot slot in this.Parent.ModuleSlotList)
                    {
                        if (slot.Position != module.XMLPosition)
                        {
                            continue;
                        }
                        slot.module = module;
                        break;
                    }
                    module.Initialize(module.XMLPosition);
                }
            }
            base.Initialize();
            if (this.ModuleType == ShipModuleType.Hangar && !this.Advanced.IsSupplyBay)
            {
                if (this.OrdinanceCapacity == 0)
                {
                    this.OrdinanceCapacity = (short)(this.Advanced.MaximumHangarShipSize / 2);                    
                    if(this.OrdinanceCapacity <50)
                    this.OrdinanceCapacity = 50;
                }              
            }
        }
		public void InitializeLite(Vector2 pos)
		{
			this.XMLPosition = pos;
			this.radius = 8f;
			base.Position = pos;
			base.Dimensions = new Vector2(16f, 16f);
			Vector2 RelativeShipCenter = new Vector2(512f, 512f);
			this.moduleCenter.X = base.Position.X + 256f;
			this.moduleCenter.Y = base.Position.Y + 256f;
			this.distanceToParentCenter = (float)Math.Sqrt((double)((this.moduleCenter.X - RelativeShipCenter.X) * (this.moduleCenter.X - RelativeShipCenter.X) + (this.moduleCenter.Y - RelativeShipCenter.Y) * (this.moduleCenter.Y - RelativeShipCenter.Y)));
			float scaleFactor = 1f;
			//ShipModule shipModule = this;
			this.distanceToParentCenter = this.distanceToParentCenter * scaleFactor;
			this.offsetAngle = (float)Math.Abs(base.findAngleToTarget(RelativeShipCenter, this.moduleCenter));
			//this.offsetAngleRadians = MathHelper.ToRadians(this.offsetAngle);
			this.SetInitialPosition();
			this.SetAttributesByType();
			base.Initialize();
            if (this.ModuleType == ShipModuleType.Hangar && !this.Advanced.IsSupplyBay)
            {
                if (this.OrdinanceCapacity == 0)
                {
                    this.OrdinanceCapacity = (short)(this.Advanced.MaximumHangarShipSize / 2);                    
                    if(this.OrdinanceCapacity <50)
                    this.OrdinanceCapacity = 50;
                }

            }
		}   //Not called anywhere

		public void LaunchBoardingPartyORIG(Troop troop)
		{
			if (this.Advanced.IsTroopBay && this.Powered)
			{
				if (this.hangarShip != null)
				{
					this.hangarShip.DoEscort(this.Parent);
					return;
				}
				if (this.hangarTimer <= 0f && this.hangarShip == null)
				{
                    this.hangarShip = Ship_Game.ResourceManager.CreateTroopShipAtPoint("Assault_Shuttle", this.Parent.loyalty, this.Center, troop);
					this.hangarShip.VanityName = "Assault Shuttle";
					this.hangarShip.Mothership = this.Parent;
					this.hangarShip.DoEscort(this.Parent);
					this.hangarShip.Velocity = (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomDirection() * this.hangarShip.speed) + this.Parent.Velocity;
					if (this.hangarShip.Velocity.Length() > this.hangarShip.velocityMaximum)
					{
						this.hangarShip.Velocity = Vector2.Normalize(this.hangarShip.Velocity) * this.hangarShip.speed;
					}
					this.installedSlot.HangarshipGuid = this.hangarShip.guid;
					this.hangarTimer = this.Advanced.hangarTimerConstant;
				}
			}
		}
        //added by gremlin boarding parties
        public void LaunchBoardingParty(Troop troop)
        {
            if (this.Advanced.IsTroopBay && this.Powered)
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
                    this.hangarShip.Velocity = (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomDirection() * this.hangarShip.speed) + this.Parent.Velocity;
                    if (this.hangarShip.Velocity.Length() > this.hangarShip.velocityMaximum)
                    {
                        this.hangarShip.Velocity = Vector2.Normalize(this.hangarShip.Velocity) * this.hangarShip.speed;
                    }
                    this.installedSlot.HangarshipGuid = this.hangarShip.guid;
                    this.hangarTimer = this.Advanced.hangarTimerConstant;
                    this.Parent.Ordinance -= this.hangarShip.Mass / 5f;
                    //if (this.Parent.GetAI().Target != null && this.Parent.GetAI().Target is Ship && (this.Parent.GetAI().Target as Ship).loyalty != this.Parent.loyalty)
                    //{
                    //    this.hangarShip.GetAI().OrderTroopToBoardShip(this.Parent.GetAI().Target as Ship);
                    //}
                }
            }
        }


		public void LoadContent(ContentManager contentManager)
		{
		}

		public void Move(float elapsedTime, float cos, float sin, float tan)
		{
			GlobalStats.ModulesMoved = GlobalStats.ModulesMoved + 1;
			Vector2 actualVector = this.XMLPosition;
			actualVector.X = actualVector.X - 256f;
			actualVector.Y = actualVector.Y - 256f;
			this.Center.X = actualVector.X * cos - actualVector.Y * sin;
			this.Center.Y = actualVector.X * sin + actualVector.Y * cos;
			//ShipModule center = this;
			this.Center = this.Center + this.Parent.Center;
			float num = 256f - this.XMLPosition.X;
			this.Center3D.X = this.Center.X;
			this.Center3D.Y = this.Center.Y;
			this.Center3D.Z = tan * num;
			if (this.Parent.dying && this.Parent.InFrustum)
			{
				if (this.trailEmitter == null && this.firetrailEmitter == null && this.reallyFuckedUp)
				{
					this.trailEmitter = new ParticleEmitter(ShipModule.universeScreen.projectileTrailParticles, 50f, this.Center3D);
					this.firetrailEmitter = new ParticleEmitter(ShipModule.universeScreen.fireTrailParticles, 60f, this.Center3D);
					this.flameEmitter = new ParticleEmitter(ShipModule.universeScreen.flameParticles, 80f, this.Center3D);
				}
				if (this.trailEmitter != null && this.Active && this.reallyFuckedUp)
				{
					this.trailEmitter.Update(elapsedTime, this.Center3D);
					this.flameEmitter.Update(elapsedTime, this.Center3D);
				}
			}
		}

		public void ScrambleFightersORIG()
		{
			if (!this.Advanced.IsTroopBay && this.Powered && !this.Advanced.IsSupplyBay)
			{
				if (this.hangarShip != null && this.hangarShip.Active)
				{
					this.hangarShip.DoEscort(this.Parent);
					return;
				}
				if (this.hangarTimer <= 0f && (this.hangarShip == null || this.hangarShip != null && !this.hangarShip.Active))
				{
					this.hangarShip = Ship_Game.ResourceManager.CreateShipFromHangar(this.hangarShipUID, this.Parent.loyalty, this.Center, this.Parent);
					if (this.hangarShip != null)
					{
						this.hangarShip.DoEscort(this.Parent);
						this.hangarShip.Velocity = (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomDirection() * this.hangarShip.speed) + this.Parent.Velocity;
						if (this.hangarShip.Velocity.Length() > this.hangarShip.velocityMaximum)
						{
							this.hangarShip.Velocity = Vector2.Normalize(this.hangarShip.Velocity) * this.hangarShip.speed;
						}
						this.hangarShip.Mothership = this.Parent;
						this.installedSlot.HangarshipGuid = this.hangarShip.guid;
						this.hangarTimer = this.Advanced.hangarTimerConstant;
					}
				}
			}
		}
        //added by gremlin fighter rearm fix
        public void ScrambleFighters()
        {
            if (!this.Advanced.IsTroopBay && !this.Advanced.IsSupplyBay && this.Powered)
            {
                if (this.hangarShip != null && this.hangarShip.Active)
                {
                    if (this.hangarShip.GetAI().State == AIState.ReturnToHangar 
                        || this.hangarShip.GetAI().HasPriorityOrder 
                        || this.hangarShip.GetAI().hasPriorityTarget
                        || this.hangarShip.GetAI().IgnoreCombat 
                        || this.hangarShip.GetAI().Target!=null
                        || Vector2.Distance(this.Parent.Center,this.hangarShip.Center) >this.Parent.SensorRange
                        )
                        return;
                    this.hangarShip.DoEscort(this.Parent);
                    return;
                }
                if (this.hangarTimer <= 0f && (this.hangarShip == null || this.hangarShip != null && !this.GetHangarShip().Active))
                {
                    string hangarship = this.hangarShipUID;
                    string startingscout = this.Parent.loyalty.data.StartingShip;

                    Ship temphangarship=null;
                    if (!this.Parent.loyalty.isFaction && (this.hangarShipUID == startingscout 
                        || !this.Parent.loyalty.ShipsWeCanBuild.Contains(this.hangarShipUID)))
                    {
                        temphangarship = ResourceManager.ShipsDict[startingscout];
                        List<Ship> fighters = new List<Ship>();
                        foreach (string shipsWeCanBuild in this.Parent.loyalty.ShipsWeCanBuild)
                        {

                            if (!this.PermittedHangarRoles.Contains(ResourceManager.ShipsDict[shipsWeCanBuild].shipData.GetRole()) 
                                || ResourceManager.ShipsDict[shipsWeCanBuild].Size > this.Advanced.MaximumHangarShipSize)
                            {
                                continue;
                            }
                            Ship tempship =ResourceManager.ShipsDict[shipsWeCanBuild];
                            //fighters.Add(ResourceManager.ShipsDict[shipsWeCanBuild]);

                            //if (temphangarship == null)
                            //{
                            //    temphangarship = tempship;
                            //    continue;
                            //}
                            if(temphangarship.BaseStrength  < tempship.BaseStrength || temphangarship.Size < tempship .Size)
                                temphangarship = tempship;
                            }
                                //temphangarship = fighters.OrderByDescending(fighter => fighter.BaseStrength).FirstOrDefault();
                        this.hangarShipUID = temphangarship.Name;
                        hangarship = this.hangarShipUID;
                    }

                    if (string.IsNullOrEmpty(hangarship) )
                        return;
                   
                        temphangarship = ResourceManager.ShipsDict[hangarship];
                    
                    if (temphangarship.Mass / 5f > this.Parent.Ordinance)  //fbedard: New spawning cost
                        return;
                    this.SetHangarShip(ResourceManager.CreateShipFromHangar(temphangarship.Name, this.Parent.loyalty, this.Center, this.Parent));

                    if (this.hangarShip != null)
                    {
                        this.GetHangarShip().DoEscort(this.Parent);
                        this.GetHangarShip().Velocity = 
                            (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomDirection() * this.GetHangarShip().speed) + this.Parent.Velocity;
                        if (this.GetHangarShip().Velocity.Length() > this.GetHangarShip().velocityMaximum)
                        {
                            this.GetHangarShip().Velocity = Vector2.Normalize(this.GetHangarShip().Velocity) * this.GetHangarShip().speed;
                        }
                        this.GetHangarShip().Mothership = this.Parent;
                        this.installedSlot.HangarshipGuid = this.GetHangarShip().guid;

                        this.hangarTimer = this.Advanced.hangarTimerConstant;
                        this.Parent.Ordinance -= this.hangarShip.Mass / 5f;
                    }
                }
            }
        }

        public void SetAttributesByType()
        {
            switch (this.ModuleType)
            {
                case ShipModuleType.Turret:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    this.InstalledWeapon.isTurret = true;
                    this.Parent.Weapons.Add(this.InstalledWeapon);
                    break;
                case ShipModuleType.MainGun:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.isMainGun = true;
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    this.Parent.Weapons.Add(this.InstalledWeapon);
                    break;
                case ShipModuleType.MissileLauncher:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    this.Parent.Weapons.Add(this.InstalledWeapon);
                    break;
                case ShipModuleType.Colony:
                    this.Parent.isColonyShip = true;
                    break;
                case ShipModuleType.Bomb:
                    this.Parent.BombBays.Add(this);
                    break;
                case ShipModuleType.Drone:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    this.Parent.Weapons.Add(this.InstalledWeapon);
                    break;
                case ShipModuleType.Spacebomb:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    this.Parent.Weapons.Add(this.InstalledWeapon);
                    break;
                //case ShipModuleType.Command:
                //    this.TargetTracking = Convert.ToSByte((this.XSIZE*this.YSIZE) / 3);
                //    break;
            }
            this.Health = this.HealthMax;
            if (this.isDummy) return;
            if (this.Advanced.shield_power_max > 0.0)
            {
                this.shield = new Shield();
                this.shield.World = Matrix.Identity * Matrix.CreateScale(2f) * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(this.Center.X, this.Center.Y, 0.0f);
                this.shield.Owner = (GameplayObject)this;
                this.shield.displacement = 0.0f;
                this.shield.texscale = 2.8f;
                this.shield.Rotation = this.Rotation;
                //lock (GlobalStats.ShieldLocker)
                    ShieldManager.shieldList.Add(this.shield);
            }
            if (this.Advanced.IsSupplyBay)
            {
                this.Parent.IsSupplyShip = true;
            }
        }

        public void SetAttributesNoParent()
        {
            switch (this.ModuleType)
            {
                case ShipModuleType.Turret:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    this.InstalledWeapon.isTurret = true;
                    break;
                case ShipModuleType.MainGun:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.isMainGun = true;
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    break;
                case ShipModuleType.MissileLauncher:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    break;
                case ShipModuleType.Drone:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    break;
                case ShipModuleType.Spacebomb:
                    this.InstalledWeapon = ResourceManager.GetWeapon(ResourceManager.ShipModulesDict[this.UID].WeaponType);
                    this.InstalledWeapon.moduleAttachedTo = this;
                    this.InstalledWeapon.SetOwner(this.Parent);
                    this.InstalledWeapon.Center = this.Center;
                    this.isWeapon = true;
                    break;
                //case ShipModuleType.Command:
                //    this.TargetTracking =  //   Convert.ToSByte((this.XSIZE * this.YSIZE) / 3);
                //    break;
            }
            this.Health = this.HealthMax;
        }

		public void SetHangarShip(Ship ship)
		{
			this.hangarShip = ship;
            if(ship != null)
                this.installedSlot.HangarshipGuid = ship.guid;  //fbedard: save mothership
		}

		public void SetInitialPosition()
		{
			float theta;
			float parentFacing = this.Parent.Rotation;
			if (parentFacing != 0f)
			{
				parentFacing = parentFacing * 180f / 3.14159274f;
			}
			float gamma = this.offsetAngle + parentFacing;
			float D = this.distanceToParentCenter;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			this.ModuleCenter = new Vector2(0f, 0f);
			if (gamma == 0f)
			{
				this.ModuleCenter.X = this.Parent.Center.X;
				this.ModuleCenter.Y = this.Parent.Center.Y - D;
			}
			if (gamma == 90f)
			{
				this.ModuleCenter.X = this.Parent.Center.X + D;
				this.ModuleCenter.Y = this.Parent.Center.Y;
			}
			if (gamma == 180f)
			{
				this.ModuleCenter.X = this.Parent.Center.X;
				this.ModuleCenter.Y = this.Parent.Center.Y + D;
			}
			if (gamma == 270f)
			{
				this.ModuleCenter.X = this.Parent.Center.X - D;
				this.ModuleCenter.Y = this.Parent.Center.Y;
			}
			if (gammaQuadrant == 1)
			{
				this.ModuleCenter.X = this.Parent.Center.X + adjX;
				this.ModuleCenter.Y = this.Parent.Center.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				this.ModuleCenter.X = this.Parent.Center.X + adjX;
				this.ModuleCenter.Y = this.Parent.Center.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				this.ModuleCenter.X = this.Parent.Center.X - adjX;
				this.ModuleCenter.Y = this.Parent.Center.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				this.ModuleCenter.X = this.Parent.Center.X - adjX;
				this.ModuleCenter.Y = this.Parent.Center.Y - oppY;
			}
			base.Position = new Vector2(this.ModuleCenter.X - 8f, this.ModuleCenter.Y - 8f);
			this.Center = this.ModuleCenter;
		}

		public void SetModuleToCenter()
		{
			base.Position = this.Parent.Position;
			this.Center = this.Parent.Center;
			if (this.isWeapon)
			{
				this.InstalledWeapon.Center = this.Center;
			}
		}

		public void SetNewExternals()
		{
            this.quadrant = -1;
            ModuleSlot module;
            Vector2 up = new Vector2(this.XMLPosition.X, this.XMLPosition.Y - 16f);
            
            if (this.Parent.GetMD().TryGetValue(up, out module) && module.module.Active && (!module.module.isExternal || module.module.Advanced.shield_power_max >0 ))
			{
                module.module.isExternal = true;
                module.module.quadrant = 1;
                this.Parent.ExternalSlots.Add(module);
			}
			Vector2 right = new Vector2(this.XMLPosition.X + 16f, this.XMLPosition.Y);
            if (this.Parent.GetMD().TryGetValue(right, out module) && module.module.Active && (!module.module.isExternal || module.module.Advanced.shield_power_max > 0))
			{
				module.module.isExternal = true;
                module.module.quadrant = 2;
                this.Parent.ExternalSlots.Add(module);
			}
			Vector2 left = new Vector2(this.XMLPosition.X - 16f, this.XMLPosition.Y);
            if (this.Parent.GetMD().TryGetValue(left, out module) && module.module.Active && (!module.module.isExternal || module.module.Advanced.shield_power_max > 0))
			{
                module.module.isExternal = true;
                module.module.quadrant = 4;
                this.Parent.ExternalSlots.Add(module);
			}
			Vector2 down = new Vector2(this.XMLPosition.X, this.XMLPosition.Y + 16f);
            if (this.Parent.GetMD().TryGetValue(down, out module) && module.module.Active && (!module.module.isExternal || module.module.Advanced.shield_power_max > 0))
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

		private Color[,] TextureTo2DArray(Texture2D texture)
		{
			Color[] colors1D = new Color[texture.Width * texture.Height];
			texture.GetData<Color>(colors1D);
			Color[,] colors2D = new Color[texture.Width, texture.Height];
			for (int x = 0; x < texture.Width; x++)
			{
				for (int y = 0; y < texture.Height; y++)
				{
					colors2D[x, y] = colors1D[x + y * texture.Width];
				}
			}
			return colors2D;
		}

		public override void Update(float elapsedTime)
		{
            this.BombTimer -= elapsedTime;
			if (base.Health > 0f && !this.Active)
			{
				this.Active = true;
				this.SetNewExternals();
				this.Parent.RecalculatePower();
			}
			else if (this.Advanced.shield_power_max > 0f && !this.isExternal && this.Active )
			{				
                this.quadrant=-1;
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
            //Added by McShooterz: shields keep charge when manually turned off
            if (this.shield_power <= 0f || shieldsOff)
            {
                this.radius = 8f;                
            }
            else
                this.radius = this.Advanced.shield_radius;
            if (this.ModuleType == ShipModuleType.Hangar && this.Active) //(this.hangarShip == null || !this.hangarShip.Active) && 
                this.hangarTimer -= elapsedTime;
            //Shield Recharge
            float shieldMax = this.GetShieldsMax();
            if (this.Active && this.Powered && this.shield_power < shieldMax)
			{
                if (this.Parent.ShieldRechargeTimer > this.Advanced.shield_recharge_delay)
                    this.shield_power += this.Advanced.shield_recharge_rate * elapsedTime;
                else if (this.shield_power > 0)
                    this.shield_power += this.Advanced.shield_recharge_combat_rate * elapsedTime;
                if (this.shield_power > shieldMax)
                    this.shield_power = shieldMax;
			}
			if (this.shield_power < 0f)
			{
				this.shield_power = 0f;
			}
            if (this.Advanced.TransporterTimer > 0)
                this.Advanced.TransporterTimer -= elapsedTime;
			base.Update(elapsedTime);

            //Added by Gretman
            if (this.ParentOfDummy != null && this.isDummy)
            {
                this.Health = this.ParentOfDummy.Health;
                this.HealthMax = this.ParentOfDummy.HealthMax;
            }
        }

		public void UpdateEveryFrame(float elapsedTime, float cos, float sin, float tan)
		{
			this.Move(elapsedTime, cos, sin, tan);
			if (this.Parent.percent >= 0.5 || base.Health >= 0.25 * this.HealthMax)
			{
				this.reallyFuckedUp = false;
			}
			else
			{
				this.reallyFuckedUp = true;
			}
            if (this.Parent.InFrustum && Ship.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (this.Active && this.onFire && this.trailEmitter == null && this.firetrailEmitter == null)
                {
                    this.trailEmitter = new ParticleEmitter(ShipModule.universeScreen.projectileTrailParticles, 50f, this.Center3D);
                    this.firetrailEmitter = new ParticleEmitter(ShipModule.universeScreen.fireTrailParticles, 60f, this.Center3D);
                    this.flameEmitter = new ParticleEmitter(ShipModule.universeScreen.flameParticles, 50f, this.Center3D);
                }
                if (this.trailEmitter != null && this.reallyFuckedUp && this.Active)
                {
                    this.trailEmitter.Update(elapsedTime, this.Center3D);
                    this.flameEmitter.Update(elapsedTime, this.Center3D);
                }
                else if (this.trailEmitter != null && this.onFire && this.Active)
                {
                    this.trailEmitter.Update(elapsedTime, this.Center3D);
                    this.firetrailEmitter.Update(elapsedTime, this.Center3D);
                }
                else if (!this.Active && this.trailEmitter != null)
                {
                    this.trailEmitter = null;
                    this.firetrailEmitter = null;
                } 
            }
			base.Rotation = this.Parent.Rotation;
		}

		public void UpdateWhileDying(float elapsedTime)
		{
			this.Center3D = new Vector3(this.Parent.Center.X, this.Parent.Center.Y, ((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(-25f, 25f));
			if (this.trailEmitter != null && this.reallyFuckedUp && this.Active)
			{
				this.trailEmitter.Update(elapsedTime, this.Center3D);
				this.flameEmitter.Update(elapsedTime, this.Center3D);
				return;
			}
			if (this.trailEmitter != null && this.onFire && this.Active)
			{
				this.trailEmitter.Update(elapsedTime, this.Center3D);
				this.firetrailEmitter.Update(elapsedTime, this.Center3D);
			}
		}

        public void Repair(float repairAmount)
        {
            this.Health += repairAmount;
            if (this.Health >= this.HealthMax)
            {
                this.Health = this.HealthMax;
                foreach (ShipModule dummy in this.LinkedModulesList)
                {
                    dummy.Health = dummy.HealthMax;
                }
            }
        }

        public float GetShieldsMax()
        {
			if (GlobalStats.ActiveModInfo != null)
            {
                float value = this.Advanced.shield_power_max;
                value += (this.Parent.loyalty != null ? this.Advanced.shield_power_max * this.Parent.loyalty.data.ShieldPowerMod : 0);
                if (GlobalStats.ActiveModInfo.useHullBonuses)
                {
                    HullBonus mod;
                    if (ResourceManager.HullBonuses.TryGetValue(this.GetParent().shipData.Hull, out mod))
                        value += this.Advanced.shield_power_max * mod.ShieldBonus;
                }
                return value;
            }
            else
                return this.Advanced.shield_power_max;
        }
	}
}