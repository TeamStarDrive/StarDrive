using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public int XSIZE = 1;
        public int YSIZE = 1;
        public float Facing;        // the firing arc direction of the module, used to rotate the module overlay 90, 180 or 270 degs
        public Vector2 XMLPosition; // module slot location in the ship design; the coordinate system axis is {256,256}
        private bool CanVisualizeDamage;
        private ShipModuleDamageVisualization DamageVisualizer;
        private EmpireShipBonuses Bonuses = EmpireShipBonuses.Default;
        private bool OnFire;
        private const float OnFireThreshold = 0.15f;
        private Vector3 Center3D;
        public Vector3 GetCenter3D => Center3D;

        private Ship Parent;
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


        public float BombTimer;
        public ShipModuleType ModuleType;
        public string IconTexturePath;

        public string UID => Flyweight.UID;

        public float FieldOfFire;
        public int TargetValue;
        public float TransporterTimer;
        public bool CheckedConduits;
        public bool Powered;
        public int quadrant = -1;
        public bool isExternal;
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
        public bool AlwaysPowered                => Flyweight.IndirectPower;
        public bool isPowerArmour                => Flyweight.isPowerArmour;
        public bool isBulkhead                   => Flyweight.isBulkhead;
        public int TargetTracking                => Flyweight.TargetTracking;
        public int FixedTracking                 => Flyweight.FixedTracking;
        public int ExplosionDamage               => Flyweight.ExplosionDamage;
        public int ExplosionRadius               => Flyweight.ExplosionRadius;
        public float RepairDifficulty            => Flyweight.RepairDifficulty;
        public bool IsRotatable                  => Flyweight.IsRotable;
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

        private float WeaponRotation;
        public float WeaponRotationSpeed
        {
            get => WeaponRotation == 0f ? (InstalledWeapon?.isTurret ?? false) ? 2 : 1 : WeaponRotation;
            set => WeaponRotation = value;
        }
        public float WeaponECM = 0;
        
        public Texture2D ModuleTexture => ResourceManager.Texture(IconTexturePath);
        public bool HasColonyBuilding => ModuleType == ShipModuleType.Colony || DeployBuildingOnColonize.NotEmpty();

        public float ActualPowerStoreMax   => PowerStoreMax * Bonuses.FuelCellMod;
        public float ActualPowerFlowMax    => PowerFlowMax  * Bonuses.PowerFlowMod;
        public float ActualBonusRepairRate => BonusRepairRate * Bonuses.RepairRateMod;
        public float ActualShieldPowerMax  => shield_power_max * Bonuses.ShieldMod;
        public float ActualMaxHealth       => TemplateMaxHealth * Bonuses.HealthMod;


        // this is the design spec of the module
        private float TemplateMaxHealth;


        public float HealthPercent => Health / ActualMaxHealth;

        // Used to configure how good of a target this module is
        public int ModuleTargettingValue => TargetValue + (Health < ActualMaxHealth ? 1 : 0); // prioritize already damaged modules        


        private void SetHealth(float newHealth)
        {
            float maxHealth = ActualMaxHealth;
            newHealth = newHealth.Clamp(0, maxHealth);
            float healthChange = newHealth - Health;
            Health = newHealth;
            OnFire = (newHealth / maxHealth) < OnFireThreshold;
            Parent.AddShipHealth(healthChange);
        }
        
        public Ship GetHangarShip() => hangarShip;
        public Ship GetParent()     => Parent;


        private ShipModule() : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = ShipModuleFlyweight.Empty;
        }

        private ShipModule(ShipModule_Deserialize template) : base(GameObjectType.ShipModule)
        {
            DisableSpatialCollision = true;
            Flyweight = new ShipModuleFlyweight(template);
            XSIZE             = template.XSIZE;
            YSIZE             = template.YSIZE;
            Mass              = template.Mass;
            Powered           = template.Powered;
            FieldOfFire       = template.FieldOfFire;
            Facing            = template.facing;
            XMLPosition       = template.XMLPosition;
            NameIndex         = template.NameIndex;
            DescriptionIndex  = template.DescriptionIndex;
            Restrictions      = template.Restrictions;
            ShieldPower       = template.shield_power;
            hangarShipUID     = template.hangarShipUID;
            hangarTimer       = template.hangarTimer;
            ModuleType        = template.ModuleType;
            WeaponType        = template.WeaponType;
            isWeapon          = WeaponType.NotEmpty();
            OrdinanceCapacity = template.OrdinanceCapacity;
            BombTimer         = template.BombTimer;
            IconTexturePath   = template.IconTexturePath;
            TargetValue       = template.TargetValue;
            TemplateMaxHealth = template.HealthMax;
            UpdateModuleRadius();
        }

        public static ShipModule CreateTemplate(ShipModule_Deserialize template)
        {
            return new ShipModule(template);
        }

        // Called by Create() and ShipDesignScreen.CreateDesignModule
        // LOYALTY can be null
        public static ShipModule CreateNoParent(string uid, Empire loyalty, ShipData hull)
        {
            ShipModule template = ResourceManager.GetModuleTemplate(uid);
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
                ShieldPower       = template.shield_power_max, //Hmmm... This one is strange -Gretman
                XSIZE             = template.XSIZE,
                YSIZE             = template.YSIZE,
                IconTexturePath   = template.IconTexturePath,
                Restrictions      = template.Restrictions
            };

            module.Health = module.ActualMaxHealth;
            module.UpdateModuleRadius();

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

        // this is used during Ship creation, Ship template creation or Ship loading from save
        public static ShipModule Create(string uid, Ship parent, ModuleSlotData slot, bool isTemplate, bool fromSave)
        {
            ShipModule module = CreateNoParent(uid, parent.loyalty, parent.shipData);
            module.Parent = parent;
            module.ApplyModuleOrientation(module.XSIZE, module.YSIZE, slot.GetOrientation());
            // Orientation and Facing are related. But always override Facing with given ModuleSlotData.Facing
            module.Facing = slot.Facing;
            module.Initialize(slot.Position, isTemplate);
            if (fromSave)
            {
                module.Active      = slot.Health > 0.01f;
                module.Health      = slot.Health;
                module.ShieldPower = slot.ShieldPower;;
            }
            return module;
        }

        private void Initialize(Vector2 pos, bool isTemplate)
        {
            if (Parent == null)
                Log.Error("module parent cannot be null!");

            ++DebugInfoScreen.ModulesCreated;

            XMLPosition = pos;

            // center of the top left 1x1 slot of this module 
            //Vector2 topLeftCenter = pos - new Vector2(256f, 256f);

            // top left position of this module
            Position = new Vector2(pos.X - 264f, pos.Y - 264f);

            // center of this module            
            Center.X = Position.X + XSIZE * 8f;
            Center.Y = Position.Y + YSIZE * 8f;
            CanVisualizeDamage = ShipModuleDamageVisualization.CanVisualize(this);

            SetAttributes();
            
            if (!isTemplate)
            {
                if (shield_power_max > 0.0f)
                    shield = ShieldManager.AddShield(this, Rotation, Center);
            }

            if (ModuleType == ShipModuleType.Hangar && !IsSupplyBay)
            {
                if (OrdinanceCapacity == 0)
                {
                    OrdinanceCapacity = (short)(MaximumHangarShipSize / 2);
                    if (OrdinanceCapacity < 50)
                        OrdinanceCapacity = 50;
                }
            }
        }

        // Refactored by RedFox - @note This method is called very heavily, so many parts have been inlined by hand
        public void UpdateEveryFrame(float elapsedTime, float cos, float sin, float tan)
        {
            // Move the module, this part is optimized according to profiler data
            ++GlobalStats.ModulesMoved;

            Vector2 offset = XMLPosition; // huge cache miss here
            offset.X       += XSIZE * 8f - 264f;
            offset.Y       += YSIZE * 8f - 264f;
            Vector2 pcenter = Parent.Center;
            float cx        = offset.X * cos - offset.Y * sin;
            float cy        = offset.X * sin + offset.Y * cos;
            cx             += pcenter.X;
            cy             += pcenter.Y;
            Center.X        = cx;
            Center.Y        = cy;
            Center3D.X      = cx;
            Center3D.Y      = cy;
            Center3D.Z      = tan * (256f - XMLPosition.X);

            UpdateDamageVisualization(elapsedTime);
            Rotation = Parent.Rotation;
        }

        private void UpdateModuleRadius()
        {
            // slightly bigger radius for better collision detection
            //Replaced [8f * 1.125f] with 9f. This is calculated for every module on every call of update() so this might add up -Gretman
            Radius = 9f * (XSIZE > YSIZE ? XSIZE : YSIZE);
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

        public bool HitTestShield(Vector2 worldPos, float radius)
        {
            ++GlobalStats.DistanceCheckTotal;
            float r2 = radius + ShieldHitRadius;
            float dx = Center.X - worldPos.X;
            float dy = Center.Y - worldPos.Y;
            return dx * dx + dy * dy <= r2 * r2;
        }
        
        public bool RayHitTestNoShield(Vector2 startPos, Vector2 endPos, float rayRadius)
        {
            Vector2 point = Center.FindClosestPointOnLine(startPos, endPos);
            return HitTestNoShields(point, rayRadius);
        }

        public bool RayHitTestShield(Vector2 startPos, Vector2 endPos, float rayRadius, out float dist)
        {
            ++GlobalStats.DistanceCheckTotal;
            dist = Center.RayCircleIntersect(rayRadius + ShieldHitRadius, startPos, endPos);
            return dist > 0f;
        }

        public static float DamageFalloff(Vector2 explosionCenter, Vector2 affectedPoint, float damageRadius, float moduleRadius, float minFalloff = 0.4f)
        {
            float explodeDist = explosionCenter.Distance(affectedPoint) - moduleRadius;
            if (explodeDist < moduleRadius) explodeDist = 0;

            return Math.Min(1.0f, (damageRadius - explodeDist) / (damageRadius + minFalloff));
        }

        public void DebugDamageCircle()
        {
            Empire.Universe?.DebugWin?.DrawGPObjects(DebugModes.Targeting, this, Parent);
        }

        // return TRUE if all damage was absorbed (damageInOut is less or equal to 0)
        public bool DamageExplosive(GameplayObject source, Vector2 worldHitPos, float damageRadius, ref float damageInOut)
        {
            float moduleRadius = ShieldPower > 0 ? ShieldHitRadius : Radius;
            float damage = damageInOut * DamageFalloff(worldHitPos, Center, damageRadius, moduleRadius);
            if (damage <= 0.001f)
                return true;

            Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager, Center, Radius);

            Damage(source, damage, out damageInOut);
            return damageInOut <= 0f;
        }

        public void Damage(GameplayObject source, float damageAmount, out float damageRemainder)
        {
            float damageModifier = ShieldPower >= 1f
                                 ? source.DamageMod.GetShieldDamageMod(this)
                                 : GetGlobalArmourBonus() * source.DamageMod.GetArmorDamageMod(this);

            float healthBefore = Health + ShieldPower;
            if (!TryDamageModule(source, damageAmount * damageModifier)) 
            {
                damageRemainder = 0;
                return; // damage was deflected
            }

            DebugDamageCircle();

            float absorbedDamage = healthBefore - (Health + ShieldPower);
            if (damageModifier <= 1) // below 1, resistance. above 1, vulnerability.
                absorbedDamage /= damageModifier; // module absorbed more dam because of good resistance
            // else: extra dam already calculated

            damageRemainder = (int)(damageAmount - absorbedDamage); 
        }

        public void DebugDamage(float percent)
        {
            float health = Health * percent + ShieldPower ;
            float damage = health.Clamp(0, Health + ShieldPower);
            var source   = GetParent();
            Damage(source, damage);            
        }

        public override void Damage(GameplayObject source, float damageAmount) => Damage(source, damageAmount, out float _);

        private bool TryDamageModule(GameplayObject source, float modifiedDamage)
        {
            if (source != null)
                Parent.LastDamagedBy = source;

            Parent.InCombatTimer        = 15f;
            Parent.ShieldRechargeTimer  = 0f;

            var beam = source as Beam;
            var proj = source as Projectile;

            bool damagingShields = ShieldPower > 1f && proj?.IgnoresShields != true;
            if (beam == null) // only for projectiles
            {
                float damageThreshold = damagingShields ? shield_threshold : DamageThreshold;
                if (modifiedDamage <= damageThreshold)
                    return false; // no damage could be done, the projectile was deflected.
            }

            //BUG: So this makes it so that if shieldpower is greater than zero the modeule wont be damaged.
            //even if the damage is greater than the shield amount. 
            if (damagingShields)
            {
                ShieldPower = (ShieldPower - modifiedDamage).Clamp(0, ShieldPower);
                if (proj != null)
                {
                    if (beam != null) CauseSiphonDamage(beam);

                    if (Parent.InFrustum && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView)
                        shield.HitShield(this, proj);
                }

                Parent.UpdateShields();

                //Log.Info($"{Parent.Name} shields '{UID}' dmg {modifiedDamage} shld {ShieldPower} by {proj?.WeaponType}");
            }
            else
            {
                if (proj != null)
                {
                    CauseEmpDamage(proj);
                    if (beam != null) CauseSpecialBeamDamage(beam);
                }
                SetHealth(Health - modifiedDamage);
                DebugPerseveranceNoDamage();

                //Log.Info($"{Parent.Name} module '{UID}' dmg {modifiedDamage}  hp  {Health} by {proj?.WeaponType}");
            }
            if (Parent.InFrustum && Empire.Universe.viewState <= UniverseScreen.UnivScreenState.ShipView)
            {
                if (beam != null) beam.CreateHitParticles(Center3D.Z);
                else if (proj?.Explodes == false) proj.CreateHitParticles(modifiedDamage, Center3D);
            }
            return true;
        }

        private float GetGlobalArmourBonus()
        {
            if (GlobalStats.ActiveModInfo?.useHullBonuses == true &&
                ResourceManager.HullBonuses.TryGetValue(Parent.shipData.Hull, out HullBonus mod))
                return (1f - mod.ArmoredBonus);
            return 1f;
        }

        private void CauseEmpDamage(Projectile proj)
        {
            if (proj.Weapon.EMPDamage > 0f)
                Parent.EMPDamage += proj.Weapon.EMPDamage;
        }

        private void CauseSpecialBeamDamage(Beam beam)
        {
            BeamPowerDamage(beam);
            BeamTroopDamage(beam);
            BeamMassDamage(beam);
            BeamRepulsionDamage(beam);
        }

        private void BeamPowerDamage(Beam beam)
        {
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
            if (beam.Weapon.MassDamage <= 0f || Parent.IsTethered() || Parent.EnginesKnockedOut)
                return;
            Parent.Mass += beam.Weapon.MassDamage;
            Parent.velocityMaximum = Parent.Thrust / Parent.Mass;
            Parent.Speed = Parent.velocityMaximum;
            Parent.rotationRadiansPerSecond = Parent.Speed / 700f;
        }

        private void BeamRepulsionDamage(Beam beam)
        {
            if (beam.Weapon.RepulsionDamage < 1)
                return;
            if (Parent.IsTethered() || Parent.EnginesKnockedOut) return;
            if (beam.Owner != null && beam.Weapon != null)
                Parent.Velocity += ((Center - beam.Owner.Center) * beam.Weapon.RepulsionDamage) / Parent.Mass;
        }

        private void DebugPerseveranceNoDamage()
        {
        #if DEBUG
            if (!Empire.Universe.Debug || Parent.VanityName != "Perseverance")
                return;
            if (Health < 10) // never give up, never surrender! F_F
                SetHealth(10);
        #endif
        }

        private void CauseSiphonDamage(Beam beam)
        {
            float shieldPower = ShieldPower;
            if (beam.Weapon.SiphonDamage > 0f)
            {
                shieldPower -= beam.Weapon.SiphonDamage;
                if (shieldPower < 1f)
                    shieldPower = 0f;

                beam.Owner.PowerCurrent += beam.Weapon.SiphonDamage;
                if (beam.Owner.PowerCurrent > beam.Owner.PowerStoreMax)
                    beam.Owner.PowerCurrent = beam.Owner.PowerStoreMax;
            }

            ShieldPower = shieldPower;
        }

        public override void Die(GameplayObject source, bool cleanupOnly)
        {
            ++DebugInfoScreen.ModulesDied;

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

            //base.Die(source, cleanupOnly); // @note This is probably not needed
            Active = false;
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
                        ignoresShields: true, damageAmount: ExplosionDamage, damageRadius: ExplosionRadius);
                }            
            }
            if (ActualPowerFlowMax > 0 || PowerRadius > 0)
                Parent.NeedRecalculate = true;
            int debriCount = (int)RandomMath.RandomBetween(0, size / 2 + 1);
            if (debriCount == 0) return;
            float debriScale = size * 0.033f;
            SpaceJunk.SpawnJunk(debriCount, Center, inSystem, this, 1.0f, debriScale);
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

        public void SetAttributes()
        {
            switch (ModuleType)
            {
                case ShipModuleType.Turret:
                    ConfigWeapon();
                    InstalledWeapon.isTurret = true;
                    break;
                case ShipModuleType.MainGun:
                    ConfigWeapon();
                    InstalledWeapon.isMainGun = true;
                    break;
                case ShipModuleType.MissileLauncher:
                    ConfigWeapon();
                    break;
                case ShipModuleType.Colony:
                    if (Parent != null)
                        Parent.isColonyShip = true;
                    break;
                case ShipModuleType.Bomb:
                    Parent?.BombBays.Add(this);
                    break;
                case ShipModuleType.Drone:
                    ConfigWeapon();
                    break;
                case ShipModuleType.Spacebomb:
                    ConfigWeapon();
                    break;
            }
            
            if (IsSupplyBay && Parent != null)
                Parent.IsSupplyShip = true;
        }

        private void ConfigWeapon()
        {
            if (InstalledWeapon != null && InstalledWeapon.WeaponType == WeaponType)
                return;
            InstalledWeapon = ResourceManager.CreateWeapon(WeaponType);
            InstalledWeapon.Module = this;
            InstalledWeapon.Owner  = Parent;
            InstalledWeapon.Center = Center;
            isWeapon = true;
            Parent?.Weapons.Add(InstalledWeapon);
        }

        public void SetHangarShip(Ship ship)
        {
            hangarShip = ship;
            if (ship != null)
                HangarShipGuid = ship.guid;  //fbedard: save mothership
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

            //SetHealth(Health); // Update and validate Health

            BombTimer -= elapsedTime;

            if (Active && ModuleType == ShipModuleType.Hangar) //(this.hangarShip == null || !this.hangarShip.Active) && 
                hangarTimer -= elapsedTime;

            // Shield Recharge
            float shieldMax = ActualShieldPowerMax;
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


        // @note This is called every frame for every module for every ship in the universe
        private void UpdateDamageVisualization(float elapsedTime)
        {
            if (!CanVisualizeDamage)
                return; // bail out for modules that are never visualized

            if (OnFire && Parent.InFrustum && 
                Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView)
            {
                if (DamageVisualizer == null)
                    DamageVisualizer = new ShipModuleDamageVisualization(this);

                DamageVisualizer.Update(elapsedTime, Center3D, Active);
            }
            else // destroy immediately when out of vision range or if module is no longer OnFire 
            {
                DamageVisualizer = null;
            }
        }

        public void UpdateWhileDying(float elapsedTime)
        {
            Center3D = Parent.Center.ToVec3(UniverseRandom.RandomBetween(-25f, 25f));
            UpdateDamageVisualization(elapsedTime);
        }

        public float Repair(float repairAmount)
        {
            if (Health >= ActualMaxHealth)
                return repairAmount;

            repairAmount = RepairDifficulty  <= 0 ? repairAmount : repairAmount / RepairDifficulty; //Some modules mightbe more difficult to repiar
            float repairLeft = (repairAmount - (ActualMaxHealth - Health)).Clamp(0, repairAmount);
            SetHealth(Health + repairAmount );
            return repairLeft;
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

            def += ActualMaxHealth * ((ModuleType == ShipModuleType.Armor ? (XSIZE) : 1f) / (slotCount * 4));

            // FB: Added Shield related calcs
            float shieldsMax = ActualShieldPowerMax;
            if (shieldsMax > 0)
            {
                def                 += shieldsMax / 100; 
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

                def *= shield_recharge_rate > 0 ? shield_recharge_rate / (shieldsMax / 100) : 1f;
                def *= shield_recharge_delay > 0 ? 1f / shield_recharge_delay : 1f;
                def *= shield_recharge_combat_rate > 0 ? 1 + shield_recharge_combat_rate / (shieldsMax / 25) : 1f;

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
            def += ActualPowerFlowMax / 100;

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

        public void ApplyModuleOrientation(int w, int h, ShipDesignScreen.ActiveModuleState state)
        {
            switch (state)
            {
                case ShipDesignScreen.ActiveModuleState.Normal:
                    XSIZE = w;
                    YSIZE = h;
                    Facing = 0f;
                    break;
                case ShipDesignScreen.ActiveModuleState.Left:
                    XSIZE = h; // @todo Why are these swapped? Please comment.
                    YSIZE = w; // if the module is facing left or right, then length is now height, and vice versa    
                    Facing = 270f;
                    return;
                case ShipDesignScreen.ActiveModuleState.Right:
                    XSIZE = h; // @todo Why are these swapped? Please comment.
                    YSIZE = w; // if the module is facing left or right, then length is now height, and vice versa
                    Facing = 90f;
                    break;
                case ShipDesignScreen.ActiveModuleState.Rear:
                    XSIZE = w;
                    YSIZE = h;
                    Facing = 180f;
                    break;
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