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
	public class ShipModule : GameplayObject
	{
		private ParticleEmitter trailEmitter;

		private ParticleEmitter firetrailEmitter;

		private ParticleEmitter flameEmitter;

		public float FTLSpeed;

		public string DeployBuildingOnColonize;

		public byte XSIZE = 1;

		public byte YSIZE = 1;

		public string ResourceStored;

		public float ResourceStorageAmount;

		public string ResourceRequired;

		public float ResourcePerSecond;

		public float ResourcePerSecondWarp;

		public float ResourcePerSecondAfterburner;

		public bool IsCommandModule;

		public bool IsRepairModule;

		public List<string> PermittedHangarRoles;

		public short MaximumHangarShipSize;

		public bool FightersOnly;

        public bool DroneModule;

        public bool FighterModule;

        public bool CorvetteModule;

        public bool FrigateModule;

        public bool DestroyerModule;

        public bool CruiserModule;

        public bool CarrierModule;

        public bool CapitalModule;

        public bool FreighterModule;

        public bool PlatformModule;

        public bool StationModule;

		public bool explodes;

		public float SensorRange;

		public float MechanicalBoardingDefense;

		public float EMP_Protection;

		public float TroopBoardingDefense;

		public byte PowerRadius;

		public bool Powered;

		public byte TechLevel;

		public float OrdnanceAddedPerSecond;

		public bool isDummy;

		public List<ShipModule> LinkedModulesList = new List<ShipModule>();

		public static UniverseScreen universeScreen;

		private float distanceToParentCenter;

		public string BombType;

		public float WarpMassCapacity;

		private float offsetAngle;

		public float FieldOfFire;

		public float facing;

		public Vector2 XMLPosition;

		private Ship Parent;

		public ShipModule ParentOfDummy;

		public float BonusRepairRate;

		public float Cargo_Capacity;

		public float HealthMax;

		public string WeaponType;

		public short NameIndex;

		public short DescriptionIndex;

		public Ship_Game.Gameplay.Restrictions Restrictions;

		public float shield_power;

        //Added by McShooterz: shields keep charge when manually turned off
        public bool shieldsOff=false;

		public float shield_radius;

		public float shield_power_max;

		public float shield_recharge_rate;

		public float shield_recharge_combat_rate;

		private Shield shield;

		public float shield_recharge_delay;

		public float numberOfColonists;

		public float numberOfEquipment;

		public float numberOfFood;

		public string hangarShipUID;

		public bool IsSupplyBay;

		public bool IsTroopBay;

		private Ship hangarShip;

		public float hangarTimerConstant = 30f;

		public float hangarTimer;

		public float thrust;

		public int WarpThrust;

		public int TurnThrust;

		public float PowerFlowMax;

		public float PowerDraw;

		public float PowerDrawAtWarp;

		public float PowerDrawWithAfterburner;

		public float AfterburnerThrust;

		public float PowerStoreMax;

		public int HealPerTurn;

		public bool isWeapon;

		public Weapon InstalledWeapon;

		public bool MountLeft;

		public bool MountRight;

		public bool MountRear;

		public short OrdinanceCapacity;

		private bool onFire;

		private bool reallyFuckedUp;

		private Vector3 Center3D = new Vector3();

		private float damagedLastTimer;

		public float BombTimer;

		public byte TroopCapacity;

		public byte TroopsSupplied;

		public float Cost;

		public bool CanRotate;

		public ShipModuleType ModuleType;

		public Vector2 moduleCenter = new Vector2(0f, 0f);

		public Vector2 ModuleCenter;

		public string IconTexturePath;

		public string UID;

		public ModuleSlot installedSlot;

		private float offsetAngleRadians;

		public bool isExternal;

		public bool TrulyExternal;

		public float InhibitionRadius;

        public float FTLSpoolTime;

        public float ECM;

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

		public ShipModule()
		{
		}

		public ShipModule(ShipModuleType type)
		{
			this.ModuleType = type;
		}

		public void Clear()
		{
			this.LinkedModulesList.Clear();
		}

		public override bool Damage(GameplayObject source, float damageAmount)
		{
			this.Parent.LastHitTimer = 15f;
			this.Parent.InCombat = true;
			this.Parent.InCombatTimer = 15f;
			this.damagedLastTimer = -5f;
			this.Parent.ShieldRechargeTimer = 0f;
            //Added by McShooterz: Fix for Ponderous, now negative dodgemod increases damage taken.
			if (source is Projectile)
			{
				this.Parent.LastDamagedBy = source;
				if (this.Parent.Role == "fighter" && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
				{
					damageAmount += damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
				}
			}
			if (source is Ship && (source as Ship).Role == "fighter" && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
			{
				damageAmount += damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
			}
            //Added by McShooterz: ArmorBonus Hull Bonus
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.useHullBonuses && this.Parent.GetShipData().ArmoredBonus != 0 && this.Parent.GetShipData().ArmoredBonus < 100)
            {
                damageAmount *= ((float)(100 - this.Parent.GetShipData().ArmoredBonus)) / 100f;
            }
			this.Parent.InCombatTimer = 15f;
			this.Parent.UnderAttackTimer = 5f;
			if (this.ModuleType == ShipModuleType.Dummy)
			{
				this.ParentOfDummy.Damage(source, damageAmount);
				return true;
			}
            //Added by McShooterz: shields keep charge when manually turned off
			if (this.shield_power <= 0f || shieldsOff)
			{
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
							Ship mechanicalBoardingDefense = this.Parent;
							mechanicalBoardingDefense.MechanicalBoardingDefense = mechanicalBoardingDefense.MechanicalBoardingDefense - 1f;
						}
					}
					if ((source as Beam).weapon.SiphonDamage > 0f)
					{
						Ship ship = this.Parent;
						ship.PowerCurrent = ship.PowerCurrent - (source as Beam).weapon.SiphonDamage;
						if (this.Parent.PowerCurrent < 0f)
						{
							this.Parent.PowerCurrent = 0f;
						}
						Ship powerCurrent1 = (source as Beam).owner;
						powerCurrent1.PowerCurrent = powerCurrent1.PowerCurrent + (source as Beam).weapon.SiphonDamage;
						if ((source as Beam).owner.PowerCurrent > (source as Beam).owner.PowerStoreMax)
						{
							(source as Beam).owner.PowerCurrent = (source as Beam).owner.PowerStoreMax;
						}
					}
					if ((source as Beam).weapon.MassDamage > 0f)
					{
						Ship mass = this.Parent;
						mass.Mass = mass.Mass + (source as Beam).weapon.MassDamage;
						this.Parent.velocityMaximum = this.Parent.Thrust / this.Parent.Mass;
						this.Parent.speed = this.Parent.velocityMaximum;
						this.Parent.rotationRadiansPerSecond = this.Parent.speed / 700f;
					}
					if ((source as Beam).weapon.RepulsionDamage > 0f)
					{
						Vector2 vtt = this.Center - (source as Beam).Owner.Center;
						Ship velocity = this.Parent;
						velocity.Velocity = velocity.Velocity + ((vtt * (source as Beam).weapon.RepulsionDamage) / this.Parent.Mass);
					}
				}
				if (this.shield_power_max > 0f && !this.isExternal)
				{
					return false;
				}
				ShipModule health = this;
				health.Health = health.Health - damageAmount;
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
				if ((double)(this.Parent.Health / this.Parent.HealthMax) < 0.5 && (double)base.Health < 0.5 * (double)this.HealthMax)
				{
					this.reallyFuckedUp = true;
				}
				foreach (ShipModule dummy in this.LinkedModulesList)
				{
					dummy.DamageDummy(source, damageAmount);
				}
			}
			else
			{
				ShipModule shieldPower = this;
				shieldPower.shield_power = shieldPower.shield_power - damageAmount;
				if (this.shield_power <= 0f)
				{
					this.shield_power = 0f;
				}
				if (ShipModule.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && this.Parent.InFrustum)
				{
					float single = damageAmount / this.shield_power_max;
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
						this.shield.pointLight.Radius = this.shield_radius * 2f;
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
							ShipModule shipModule = this;
							shipModule.shield_power = shipModule.shield_power - (source as Beam).weapon.SiphonDamage;
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
			bool moduleType = this.ModuleType == ShipModuleType.PowerPlant & this.Parent.isPlayerShip();
            //Added by McShooterz: shields keep charge when manually turned off
			if (this.shield_power > 0f && !shieldsOff)
			{
				this.radius = this.shield_radius;
			}
			else
			{
				this.radius = 8f;
			}
			return true;
		}

		public bool DamageDummy(GameplayObject source, float damageAmount)
		{
			ShipModule health = this;
			health.Health = health.Health - damageAmount;
			this.Parent.LastDamagedBy = source;
			return true;
		}

		public void DamageInvisible(GameplayObject source, float damageAmount)
		{
			this.Parent.LastHitTimer = 15f;
			if (source is Projectile)
			{
				this.Parent.LastDamagedBy = source;
				if (this.Parent.Role == "fighter" && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
				{
					damageAmount = damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
				}
			}
			if (source is Ship && (source as Ship).Role == "fighter" && this.Parent.loyalty.data.Traits.DodgeMod < 0f)
			{
				damageAmount = damageAmount * Math.Abs(this.Parent.loyalty.data.Traits.DodgeMod);
			}
			this.Parent.InCombatTimer = 15f;
			this.Parent.UnderAttackTimer = 5f;
			this.damagedLastTimer = 0f;
			if (this.ModuleType == ShipModuleType.Dummy)
			{
				this.ParentOfDummy.DamageInvisible(source, damageAmount);
				return;
			}
            //Added by McShooterz: shields keep charge when manually turned off
			if (this.shield_power <= 0f || shieldsOff)
			{
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
							Ship mechanicalBoardingDefense = this.Parent;
							mechanicalBoardingDefense.MechanicalBoardingDefense = mechanicalBoardingDefense.MechanicalBoardingDefense - 1f;
						}
					}
					if ((source as Beam).weapon.MassDamage > 0f)
					{
						Ship mass = this.Parent;
						mass.Mass = mass.Mass + (source as Beam).weapon.MassDamage;
						this.Parent.velocityMaximum = this.Parent.Thrust / this.Parent.Mass;
						this.Parent.speed = this.Parent.velocityMaximum;
						this.Parent.rotationRadiansPerSecond = this.Parent.speed / 700f;
					}
					if ((source as Beam).weapon.RepulsionDamage > 0f)
					{
						Vector2 vtt = this.Center - (source as Beam).Owner.Center;
						Ship velocity = this.Parent;
						velocity.Velocity = velocity.Velocity + ((vtt * (source as Beam).weapon.RepulsionDamage) / this.Parent.Mass);
					}
				}
                //Added by McShooterz: shields keep charge when manually turned off
				if (this.shield_power <= 0f || shieldsOff)
				{
					ShipModule health = this;
					health.Health = health.Health - damageAmount;
				}
				else
				{
					ShipModule shieldPower = this;
					shieldPower.shield_power = shieldPower.shield_power - damageAmount;
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
				if (base.Health / this.HealthMax < 0.5f)
				{
					this.onFire = true;
				}
				if ((double)(this.Parent.Health / this.Parent.HealthMax) < 0.5 && (double)base.Health < 0.5 * (double)this.HealthMax)
				{
					this.reallyFuckedUp = true;
				}
				foreach (ShipModule dummy in this.LinkedModulesList)
				{
					dummy.DamageDummy(source, damageAmount);
				}
			}
			else
			{
				ShipModule shipModule = this;
				shipModule.shield_power = shipModule.shield_power - damageAmount;
				if (ShipModule.universeScreen.viewState == UniverseScreen.UnivScreenState.ShipView && this.Parent.InFrustum)
				{
					float single = damageAmount / this.shield_power_max;
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
						this.shield.pointLight.Radius = this.shield_radius * 2f;
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
							ShipModule shieldPower1 = this;
							shieldPower1.shield_power = shieldPower1.shield_power - (source as Beam).weapon.SiphonDamage;
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
				this.radius = this.shield_radius;
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
			if (this.shield_power_max > 0f)
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
				//if (this.ModuleType == ShipModuleType.PowerPlant)
                if (this.explodes)
				{
					if (this.Parent.GetSystem() == null)
					{
                        UniverseScreen.DeepSpaceManager.ExplodeAtModule(this.Parent.LastDamagedBy, this, (float)(2500 * this.XSIZE * this.YSIZE), (float)(this.XSIZE * this.YSIZE * 64));
					}
					else
					{
                        this.Parent.GetSystem().spatialManager.ExplodeAtModule(this.Parent.LastDamagedBy, this, (float)(2500 * this.XSIZE * this.YSIZE), (float)(this.XSIZE * this.YSIZE * 64));
					}
					this.Parent.NeedRecalculate = true;
				}
				if (this.ModuleType == ShipModuleType.PowerConduit)
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

		public void Initialize(Vector2 pos)
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
			ShipModule shipModule = this;
			shipModule.distanceToParentCenter = shipModule.distanceToParentCenter * scaleFactor;
			this.offsetAngle = (float)Math.Abs(base.findAngleToTarget(RelativeShipCenter, this.moduleCenter));
			this.offsetAngleRadians = MathHelper.ToRadians(this.offsetAngle);
			this.SetInitialPosition();
			this.SetAttributesByType();
			if (this.Parent != null && this.Parent.loyalty != null)
			{
				this.HealthMax = this.HealthMax + this.HealthMax * this.Parent.loyalty.data.Traits.ModHpModifier;
				base.Health = base.Health + base.Health * this.Parent.loyalty.data.Traits.ModHpModifier;
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
			base.Initialize();
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
			ShipModule shipModule = this;
			shipModule.distanceToParentCenter = shipModule.distanceToParentCenter * scaleFactor;
			this.offsetAngle = (float)Math.Abs(base.findAngleToTarget(RelativeShipCenter, this.moduleCenter));
			this.offsetAngleRadians = MathHelper.ToRadians(this.offsetAngle);
			this.SetInitialPosition();
			this.SetAttributesByType();
			base.Initialize();
		}

		public void LaunchBoardingPartyORIG(Troop troop)
		{
			if (this.IsTroopBay && this.Powered)
			{
				if (this.hangarShip != null)
				{
					this.hangarShip.DoEscort(this.Parent);
					return;
				}
				if (this.hangarTimer <= 0f && this.hangarShip == null)
				{
					this.hangarShip = Ship_Game.ResourceManager.CreateTroopShipAtPoint(this.Parent.loyalty.data.StartingScout, this.Parent.loyalty, this.Center, troop);
					this.hangarShip.VanityName = "Assault Ship";
					this.hangarShip.Mothership = this.Parent;
					this.hangarShip.DoEscort(this.Parent);
					this.hangarShip.Velocity = (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomDirection() * this.hangarShip.speed) + this.Parent.Velocity;
					if (this.hangarShip.Velocity.Length() > this.hangarShip.velocityMaximum)
					{
						this.hangarShip.Velocity = Vector2.Normalize(this.hangarShip.Velocity) * this.hangarShip.speed;
					}
					this.installedSlot.HangarshipGuid = this.hangarShip.guid;
					this.hangarTimer = this.hangarTimerConstant;
				}
			}
		}
        //added by gremlin boarding parties
        public void LaunchBoardingParty(Troop troop)
        {
            if (this.IsTroopBay && this.Powered)
            {
                if (this.hangarShip != null)
                {
                    //this.hangarShip.GetAI().State == AIState.AssaultPlanet || this.hangarShip.GetAI().State == AIState.Boarding ||
                    if (this.hangarShip.GetAI().State == AIState.ReturnToHangar || this.hangarShip.GetAI().EscortTarget != null || this.hangarShip.GetAI().OrbitTarget != null) return;
                    this.hangarShip.DoEscort(this.Parent);
                    return;
                }
                if (this.hangarTimer <= 0f && this.hangarShip == null)
                {
                    this.hangarShip = ResourceManager.CreateTroopShipAtPoint(this.Parent.loyalty.data.StartingScout, this.Parent.loyalty, this.Center, troop);
                    this.hangarShip.VanityName = "Assault Ship";
                    this.hangarShip.Mothership = this.Parent;
                    this.hangarShip.DoEscort(this.Parent);
                    this.hangarShip.Velocity = (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomDirection() * this.hangarShip.speed) + this.Parent.Velocity;
                    if (this.hangarShip.Velocity.Length() > this.hangarShip.velocityMaximum)
                    {
                        this.hangarShip.Velocity = Vector2.Normalize(this.hangarShip.Velocity) * this.hangarShip.speed;
                    }
                    this.installedSlot.HangarshipGuid = this.hangarShip.guid;
                    this.hangarTimer = this.hangarTimerConstant;
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
			ShipModule center = this;
			center.Center = center.Center + this.Parent.Center;
			float num = 256f - this.XMLPosition.X;
			this.Center3D.X = this.Center.X;
			this.Center3D.Y = this.Center.Y;
			this.Center3D.Z = tan * num;
			if (this.Parent.dying)
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

		private void OldMove(float elapsedTime)
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

		public void ScrambleFightersORIG()
		{
			if (!this.IsTroopBay && this.Powered && !this.IsSupplyBay)
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
						this.hangarTimer = this.hangarTimerConstant;
					}
				}
			}
		}
        //added by gremlin fighter rearm fix
        public void ScrambleFighters()
        {
            if (!this.IsTroopBay && !this.IsSupplyBay && this.Powered)
            {
                if (this.hangarShip != null && this.hangarShip.Active)
                {
                    if (this.hangarShip.GetAI().State == AIState.ReturnToHangar) return;
                    this.hangarShip.DoEscort(this.Parent);
                    return;
                }
                if (this.hangarTimer <= 0f && (this.hangarShip == null || this.hangarShip != null && !this.GetHangarShip().Active))
                {
                    string hangarship = this.hangarShipUID;
                    string startingscout = this.Parent.loyalty.data.StartingShip;


                    if (!this.Parent.loyalty.isFaction && (this.hangarShipUID == startingscout || !this.Parent.loyalty.ShipsWeCanBuild.Contains(this.hangarShipUID)))
                    {

                        List<Ship> fighters = new List<Ship>();
                        foreach (string shipsWeCanBuild in this.Parent.loyalty.ShipsWeCanBuild)
                        {

                            if (!this.PermittedHangarRoles.Contains(ResourceManager.ShipsDict[shipsWeCanBuild].Role) || ResourceManager.ShipsDict[shipsWeCanBuild].Size > this.MaximumHangarShipSize)
                            {
                                continue;
                            }
                            fighters.Add(ResourceManager.ShipsDict[shipsWeCanBuild]);
                        }

                        hangarship = fighters.OrderByDescending(fighter => fighter.BaseStrength).Select(fighter => fighter.Name).FirstOrDefault();
                    }


                    this.SetHangarShip(ResourceManager.CreateShipFromHangar(hangarship, this.Parent.loyalty, this.Center, this.Parent));

                    if (this.hangarShip != null)
                    {
                        this.GetHangarShip().DoEscort(this.Parent);
                        this.GetHangarShip().Velocity = (((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomDirection() * this.GetHangarShip().speed) + this.Parent.Velocity;
                        if (this.GetHangarShip().Velocity.Length() > this.GetHangarShip().velocityMaximum)
                        {
                            this.GetHangarShip().Velocity = Vector2.Normalize(this.GetHangarShip().Velocity) * this.GetHangarShip().speed;
                        }
                        this.GetHangarShip().Mothership = this.Parent;
                        this.installedSlot.HangarshipGuid = this.GetHangarShip().guid;
                        this.hangarTimer = this.hangarTimerConstant;
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
            }
            if ((double)this.shield_power_max > 0.0)
            {
                this.shield = new Shield();
                this.shield.World = Matrix.Identity * Matrix.CreateScale(2f) * Matrix.CreateRotationZ(this.Rotation) * Matrix.CreateTranslation(this.Center.X, this.Center.Y, 0.0f);
                this.shield.Owner = (GameplayObject)this;
                this.shield.displacement = 0.0f;
                this.shield.texscale = 2.8f;
                this.shield.Rotation = this.Rotation;
                lock (GlobalStats.ShieldLocker)
                    ShieldManager.shieldList.Add(this.shield);
            }
            if (this.IsSupplyBay)
            {
                if (this.Parent.Role == "freighter")
                    this.Parent.Role = "supply";
                this.Parent.IsSupplyShip = true;
            }
            this.Health = this.HealthMax;
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
            }
            this.Health = this.HealthMax;
        }

		public void SetHangarShip(Ship ship)
		{
			this.hangarShip = ship;
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
			Vector2 up = new Vector2(this.XMLPosition.X, this.XMLPosition.Y - 16f);
			if (this.Parent.GetMD().ContainsKey(up) && this.Parent.GetMD()[up].module.Active && !this.Parent.GetMD()[up].module.isExternal)
			{
				this.Parent.GetMD()[up].module.isExternal = true;
				this.Parent.ExternalSlots.Add(this.Parent.GetMD()[up]);
			}
			Vector2 right = new Vector2(this.XMLPosition.X + 16f, this.XMLPosition.Y);
			if (this.Parent.GetMD().ContainsKey(right) && this.Parent.GetMD()[right].module.Active && !this.Parent.GetMD()[right].module.isExternal)
			{
				this.Parent.GetMD()[right].module.isExternal = true;
				this.Parent.ExternalSlots.Add(this.Parent.GetMD()[right]);
			}
			Vector2 left = new Vector2(this.XMLPosition.X - 16f, this.XMLPosition.Y);
			if (this.Parent.GetMD().ContainsKey(left) && this.Parent.GetMD()[left].module.Active && !this.Parent.GetMD()[left].module.isExternal)
			{
				this.Parent.GetMD()[left].module.isExternal = true;
				this.Parent.ExternalSlots.Add(this.Parent.GetMD()[left]);
			}
			Vector2 down = new Vector2(this.XMLPosition.X, this.XMLPosition.Y + 16f);
			if (this.Parent.GetMD().ContainsKey(down) && this.Parent.GetMD()[down].module.Active && !this.Parent.GetMD()[down].module.isExternal)
			{
				this.Parent.GetMD()[down].module.isExternal = true;
				this.Parent.ExternalSlots.Add(this.Parent.GetMD()[down]);
			}
		}

		public void SetParent(Ship p)
		{
			this.Parent = p;
		}

		public void ShipDie(GameplayObject source, bool cleanupOnly)
		{
			if (this.shield != null)
			{
				lock (GlobalStats.ObjectManagerLocker)
				{
					ShipModule.universeScreen.ScreenManager.inter.LightManager.Remove(this.shield.pointLight);
				}
				lock (GlobalStats.ShieldLocker)
				{
					ShieldManager.shieldList.QueuePendingRemoval(this.shield);
				}
			}
			base.Health = 0f;
			Vector3 vector3 = new Vector3(this.Center.X, this.Center.Y, -100f);
			if (this.Active)
			{
				((this.Parent.GetSystem() != null ? this.Parent.GetSystem().RNG : Ship.universeScreen.DeepSpaceRNG)).RandomBetween(5f, 15f);
			}
			base.Die(source, cleanupOnly);
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

		public override bool Touch(GameplayObject target)
		{
			ShipModule testMod = target as ShipModule;
			int test = 0;
			if (testMod != null && testMod.Parent.loyalty != this.Parent.loyalty)
			{
				this.Damage(target, 10f);
				target.Damage(this, 10f);
				test++;
			}
			return base.Touch(target);
		}

		public override void Update(float elapsedTime)
		{
			ShipModule bombTimer = this;
			bombTimer.BombTimer = bombTimer.BombTimer - elapsedTime;
			ShipModule shipModule = this;
			shipModule.damagedLastTimer = shipModule.damagedLastTimer + elapsedTime;
			if (base.Health > 0f && !this.Active)
			{
				this.Active = true;
				this.SetNewExternals();
				this.Parent.RecalculatePower();
			}
			else if (this.shield_power_max > 0f && !this.isExternal && this.Active)
			{
				this.isExternal = true;
			}
			if (base.Health <= 0f && this.Active)
			{
				this.Die(base.LastDamagedBy, false);
			}
			if (this.OrdnanceAddedPerSecond > 0f && this.Powered)
			{
				Ship parent = this.Parent;
				parent.Ordinance = parent.Ordinance + this.OrdnanceAddedPerSecond * elapsedTime;
				if (this.Parent.Ordinance > this.Parent.OrdinanceMax)
				{
					this.Parent.Ordinance = this.Parent.OrdinanceMax;
				}
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
			{
				this.radius = this.shield_radius;
			}
			if ((this.hangarShip == null || !this.hangarShip.Active) && this.ModuleType == ShipModuleType.Hangar && this.Active)
			{
				ShipModule shipModule1 = this;
				shipModule1.hangarTimer = shipModule1.hangarTimer - elapsedTime;
			}
            if (this.Active && this.Powered && this.shield_power < this.shield_power_max && this.Parent.ShieldRechargeTimer >= this.shield_recharge_delay)
			{
                this.shield_power += this.shield_recharge_rate * elapsedTime;
				if (this.shield_power > this.shield_power_max)
				{
					this.shield_power = this.shield_power_max;
				}
			}
            //Combat shield recharge only works until shields fail, then they only come back by normal recharge
            else if (this.Active && this.Powered && this.shield_power < this.shield_power_max && this.Parent.ShieldRechargeTimer < this.shield_recharge_delay && this.shield_power > 1)
			{
                this.shield_power += this.shield_recharge_combat_rate * elapsedTime;
				if (this.shield_power > this.shield_power_max)
				{
					this.shield_power = this.shield_power_max;
				}
			}
			if (this.shield_power < 0f)
			{
				this.shield_power = 0f;
			}
			base.Update(elapsedTime);
		}

		public void UpdateEveryFrame(float elapsedTime, float cos, float sin, float tan)
		{
			this.Move(elapsedTime, cos, sin, tan);
			if ((double)this.Parent.percent >= 0.5 || (double)base.Health >= 0.25 * (double)this.HealthMax)
			{
				this.reallyFuckedUp = false;
			}
			else
			{
				this.reallyFuckedUp = true;
			}
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
	}
}