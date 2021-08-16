﻿using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace Ship_Game.Gameplay
{
    //These simple classes are the member Variable holding sub_classes used to hold the less used variables
    //for the game objects that are extremely numerous. They are essentially just groups of member variables
    //that will allow a parent object to have all of these variables available to them without having them
    //all allocated in memory unless they are needed. By my calculations, this is 313 bytes.       -Gretman

    public sealed class ShipModuleFlyweight
    {
        public static int TotalNumModules { get; private set; } //To track how many advanced modules are being created.

        public readonly float FTLSpeed;
        public readonly string DeployBuildingOnColonize;
        public readonly string ResourceStored;
        public readonly float ResourceStorageAmount;
        public readonly bool IsCommandModule;
        public readonly bool IsRepairModule;
        public readonly string[] PermittedHangarRoles;
        public readonly short MaximumHangarShipSize;
        public readonly bool DroneModule      = true;
        public readonly bool FighterModule    = true;
        public readonly bool CorvetteModule   = true;
        public readonly bool FrigateModule    = true;
        public readonly bool DestroyerModule  = true;
        public readonly bool CruiserModule    = true;
        public readonly bool BattleshipModule = true;
        public readonly bool CapitalModule    = true;
        public readonly bool FreighterModule  = true;
        public readonly bool PlatformModule   = true;
        public readonly bool StationModule    = true;
        public readonly bool explodes;
        public readonly float SensorRange;
        public readonly float MechanicalBoardingDefense;
        public readonly float EMP_Protection;
        public readonly int PowerRadius;
        public readonly int TechLevel;
        public readonly float OrdnanceAddedPerSecond;
        public readonly string BombType;
        public readonly float WarpMassCapacity;
        public readonly float BonusRepairRate;
        public readonly float Cargo_Capacity;
        public readonly float shield_radius;
        public readonly float shield_power_max;
        public readonly float shield_recharge_rate;
        public readonly float shield_recharge_combat_rate;
        public readonly float shield_recharge_delay;
        public readonly float shield_threshold;
        public readonly float shield_kinetic_resist;
        public readonly float shield_energy_resist;
        public readonly float shield_explosive_resist;
        public readonly float shield_missile_resist;
        public readonly float shield_flak_resist;
        public readonly float shield_hybrid_resist;
        public readonly float shield_railgun_resist;
        public readonly float shield_subspace_resist;
        public readonly float shield_warp_resist;
        public readonly float shield_beam_resist;
        public readonly float numberOfColonists;
        public readonly float numberOfEquipment;
        public readonly float numberOfFood;
        public readonly bool IsSupplyBay;
        public readonly bool IsTroopBay;
        public readonly float hangarTimerConstant = 30f;
        public readonly float thrust;
        public readonly float WarpThrust;
        public readonly float TurnThrust;
        public readonly float PowerFlowMax;
        public readonly float PowerDraw;
        public readonly float PowerDrawAtWarp;
        public readonly float PowerStoreMax;
        public readonly float HealPerTurn;
        public readonly int TroopCapacity;
        public readonly int TroopsSupplied;
        public readonly float Cost;
        public readonly float InhibitionRadius;
        public readonly float FTLSpoolTime;
        public readonly float ECM;
        public readonly float SensorBonus;
        public readonly float TransporterTimerConstant;
        public readonly float TransporterRange;
        public readonly float TransporterPower;
        public readonly float TransporterOrdnance;
        public readonly int TransporterTroopLanding;
        public readonly int TransporterTroopAssault;

        //Doctor: these were floats for a reason: they're decimal fractions to define damage resistance. Loading them with sbytes caused crashes from xml and broke the resistance mechanics.
        //Likewise, DamageThreshold is a set damage amount which armour disregards damage value under - immediately broke as a byte as some armours had values higher than 255.
        public readonly float KineticResist;
        public readonly float EnergyResist;
        public readonly float GuidedResist;
        public readonly float MissileResist;
        public readonly float HybridResist;
        public readonly float BeamResist;
        public readonly float ExplosiveResist;
        public readonly float InterceptResist;
        public readonly float RailgunResist;
        public readonly float SpaceBombResist;
        public readonly float BombResist;
        public readonly float BioWeaponResist;
        public readonly float DroneResist;
        public readonly float WarpResist;
        public readonly float TorpedoResist;
        public readonly float CannonResist;
        public readonly float SubspaceResist;
        public readonly float PDResist;
        public readonly float FlakResist;
        public readonly float DamageThreshold;
        public readonly int APResist;
        public readonly bool IndirectPower;
        public readonly bool IsPowerArmor;
        public readonly bool IsBulkhead;
        public readonly int TargetTracking;
        public readonly int TargetAccuracy;
        public readonly bool DisableRotation;
        public readonly float AmplifyShields;
        public readonly int ExplosionDamage;
        public readonly int ExplosionRadius;
        public readonly float RepairDifficulty = 1f;
        public readonly string ShieldBubbleColor;
        public readonly float Regenerate;
        public float AccuracyPercent;
        public float WeaponInaccuracyBase;

        public static readonly ShipModuleFlyweight Empty = new ShipModuleFlyweight();    //A static instance to be assigned to leftover modules
        public readonly string UID = string.Empty;

        public ShipModuleFlyweight()
        {
            ++TotalNumModules;
        }

        public ShipModuleFlyweight(ShipModule_XMLTemplate s)
        {
            //This should only be called once per module XML file -Gretman
            ++TotalNumModules;
            FTLSpeed                    = s.FTLSpeed;
            DeployBuildingOnColonize    = s.DeployBuildingOnColonize;
            ResourceStored              = s.ResourceStored;
            ResourceStorageAmount       = s.ResourceStorageAmount;
            IsCommandModule             = s.IsCommandModule;
            IsRepairModule              = s.IsRepairModule;
            PermittedHangarRoles        = s.PermittedHangarRoles ?? Empty<string>.Array;
            MaximumHangarShipSize       = s.MaximumHangarShipSize;
            DroneModule                 = s.DroneModule;
            FighterModule               = s.FighterModule;
            CorvetteModule              = s.CorvetteModule;
            FrigateModule               = s.FrigateModule;
            DestroyerModule             = s.DestroyerModule;
            CruiserModule               = s.CruiserModule;
            BattleshipModule            = s.BattleshipModule;
            CapitalModule               = s.CapitalModule;
            FreighterModule             = s.FreighterModule;
            PlatformModule              = s.PlatformModule;
            StationModule               = s.StationModule;
            explodes                    = s.explodes;
            SensorRange                 = s.SensorRange;
            MechanicalBoardingDefense   = s.MechanicalBoardingDefense;
            EMP_Protection              = s.EMP_Protection;
            PowerRadius                 = s.PowerRadius;
            TechLevel                   = s.TechLevel;
            OrdnanceAddedPerSecond      = s.OrdnanceAddedPerSecond;
            BombType                    = s.BombType;
            WarpMassCapacity            = s.WarpMassCapacity;
            BonusRepairRate             = s.BonusRepairRate;
            Cargo_Capacity              = s.Cargo_Capacity;
            shield_radius               = s.shield_radius;
            shield_power_max            = s.shield_power_max;
            AmplifyShields              = s.AmplifyShields;
            shield_recharge_rate        = s.shield_recharge_rate;
            shield_recharge_combat_rate = s.shield_recharge_combat_rate;
            shield_recharge_delay       = s.shield_recharge_delay;
            shield_threshold            = s.shield_threshold;
            shield_kinetic_resist       = s.shield_kinetic_resist;
            shield_energy_resist        = s.shield_energy_resist;
            shield_explosive_resist     = s.shield_explosive_resist;
            shield_missile_resist       = s.shield_missile_resist;
            shield_flak_resist          = s.shield_flak_resist;
            shield_hybrid_resist        = s.shield_hybrid_resist;
            shield_railgun_resist       = s.shield_railgun_resist;
            shield_subspace_resist      = s.shield_subspace_resist;
            shield_warp_resist          = s.shield_warp_resist;
            shield_beam_resist          = s.shield_beam_resist;
            numberOfColonists           = s.numberOfColonists;
            numberOfEquipment           = s.numberOfEquipment;
            numberOfFood                = s.numberOfFood;
            IsSupplyBay                 = s.IsSupplyBay;
            IsTroopBay                  = s.IsTroopBay;
            hangarTimerConstant         = s.hangarTimerConstant;
            thrust                      = s.thrust;
            WarpThrust                  = s.WarpThrust;
            TurnThrust                  = s.TurnThrust;
            PowerFlowMax                = s.PowerFlowMax;
            PowerDraw                   = s.PowerDraw;
            PowerDrawAtWarp             = s.PowerDrawAtWarp;
            PowerStoreMax               = s.PowerStoreMax;
            HealPerTurn                 = s.HealPerTurn;
            TroopCapacity               = s.TroopCapacity;
            TroopsSupplied              = s.TroopsSupplied;
            Cost                        = s.Cost;
            InhibitionRadius            = s.InhibitionRadius;
            FTLSpoolTime                = s.FTLSpoolTime;
            ECM                         = s.ECM;
            SensorBonus                 = s.SensorBonus;
            TransporterTimerConstant    = s.TransporterTimerConstant;
            TransporterRange            = s.TransporterRange;
            TransporterPower            = s.TransporterPower;
            TransporterOrdnance         = s.TransporterOrdnance;
            TransporterTroopLanding     = s.TransporterTroopLanding;
            TransporterTroopAssault     = s.TransporterTroopAssault;
            KineticResist               = s.KineticResist;
            EnergyResist                = s.EnergyResist;
            GuidedResist                = s.GuidedResist;
            MissileResist               = s.MissileResist;
            HybridResist                = s.HybridResist;
            BeamResist                  = s.BeamResist;
            ExplosiveResist             = s.ExplosiveResist;
            InterceptResist             = s.InterceptResist;
            RailgunResist               = s.RailgunResist;
            SpaceBombResist             = s.SpaceBombResist;
            BombResist                  = s.BombResist;
            BioWeaponResist             = s.BioWeaponResist;
            DroneResist                 = s.DroneResist;
            WarpResist                  = s.WarpResist;
            TorpedoResist               = s.TorpedoResist;
            CannonResist                = s.CannonResist;
            SubspaceResist              = s.SubspaceResist;
            PDResist                    = s.PDResist;
            FlakResist                  = s.FlakResist;
            DamageThreshold             = s.DamageThreshold;
            APResist                    = s.APResist;
            ExplosionDamage             = s.ExplosionDamage > 0 ? s.ExplosionDamage : s.XSIZE * s.YSIZE * 2500;
            ExplosionRadius             = s.ExplosionRadius > 0 ? s.ExplosionRadius: s.XSIZE * s.YSIZE * 64;
            RepairDifficulty            = s.RepairDifficulty;
            Regenerate                  = s.Regenerate;
            ShieldBubbleColor           = s.ShieldBubbleColor;
            IndirectPower               = s.IndirectPower;
            IsPowerArmor                = s.IsPowerArmor;
            IsBulkhead                  = s.IsBulkhead;
            TargetTracking              = s.TargetTracking;
            TargetAccuracy              = s.TargetAccuracy;
            DisableRotation             = s.DisableRotation;
            UID                         = s.UID;
            AccuracyPercent             = s.AccuracyPercent;
            WeaponInaccuracyBase        = Weapon.GetWeaponInaccuracyBase(s.XSIZE * s.YSIZE, s.AccuracyPercent);
        }
    }

    /// <summary>
    /// This is used for ShipModule XML Templates
    /// </summary>
    [XmlType("ShipModule")]
    public sealed class ShipModule_XMLTemplate : GameplayObject
    {
        public ShipModule_XMLTemplate() : base(GameObjectType.ShipModule)
        {
        }

        public float FTLSpeed;
        public string DeployBuildingOnColonize;
        public int XSIZE = 1;
        public int YSIZE = 1;
        public string ResourceStored;
        public float ResourceStorageAmount;
        public string ResourceRequired;
        public float ResourcePerSecond;
        public float ResourcePerSecondWarp;
        public bool IsCommandModule;
        public bool IsRepairModule;
        public string[] PermittedHangarRoles;
        public short MaximumHangarShipSize;
        public bool FightersOnly;
        public bool DroneModule      = true;
        public bool FighterModule    = true;
        public bool CorvetteModule   = true;
        public bool FrigateModule    = true;
        public bool DestroyerModule  = true;
        public bool CruiserModule    = true;
        public bool BattleshipModule = true;
        public bool CapitalModule    = true;
        public bool FreighterModule  = true;
        public bool PlatformModule   = true;
        public bool StationModule    = true;
        public bool explodes;
        public float SensorRange;
        public float MechanicalBoardingDefense;
        public float EMP_Protection;
        public float TroopBoardingDefense;
        public int PowerRadius;
        public bool Powered;
        public int TechLevel;
        public float OrdnanceAddedPerSecond;
        public string BombType;
        public float WarpMassCapacity;
        public float FieldOfFire;
        public float BonusRepairRate;
        public float Cargo_Capacity;
        public float HealthMax;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public Restrictions Restrictions;
        public float shield_power;
        public float shield_radius;
        public float shield_power_max;
        public float shield_recharge_rate;
        public float shield_recharge_combat_rate;
        public float shield_recharge_delay;
        public float shield_threshold;
        public float shield_kinetic_resist;
        public float shield_energy_resist;
        public float shield_explosive_resist;
        public float shield_missile_resist;
        public float shield_flak_resist;
        public float shield_hybrid_resist;
        public float shield_railgun_resist;
        public float shield_subspace_resist;
        public float shield_warp_resist;
        public float shield_beam_resist;
        public float numberOfColonists;
        public float numberOfEquipment;
        public float numberOfFood;
        public string hangarShipUID;
        public bool IsSupplyBay;
        public bool IsTroopBay;
        public float hangarTimerConstant = 30f;
        public float hangerTimerConstant = -1;
        public float hangarTimer;
        public float thrust;
        public int WarpThrust;
        public int TurnThrust;
        public float PowerFlowMax;
        public float PowerDraw;
        public float PowerDrawAtWarp;
        public float PowerStoreMax;
        public float HealPerTurn;
        public bool isWeapon;
        public bool MountLeft;
        public bool MountRight;
        public bool MountRear;
        public short OrdinanceCapacity;
        public float BombTimer;
        public int TroopCapacity;
        public int TroopsSupplied;
        public float Cost;
        public ShipModuleType ModuleType;
        public string IconTexturePath;
        public string UID;
        public bool isExternal;
        public bool TrulyExternal;
        public float InhibitionRadius;
        public float FTLSpoolTime;
        public float ECM;
        public float SensorBonus;
        public float TransporterTimerConstant;
        public float TransporterTimer;
        public float TransporterRange;
        public float TransporterPower;
        public float TransporterOrdnance;
        public int TransporterTroopLanding;
        public int TransporterTroopAssault;
        public int TargetValue;
        public float KineticResist;
        public float EnergyResist;
        public float GuidedResist;
        public float MissileResist;
        public float HybridResist;
        public float BeamResist;
        public float ExplosiveResist;
        public float InterceptResist;
        public float RailgunResist;
        public float SpaceBombResist;
        public float BombResist;
        public float BioWeaponResist;
        public float DroneResist;
        public float WarpResist;
        public float TorpedoResist;
        public float CannonResist;
        public float SubspaceResist;
        public float PDResist;
        public float FlakResist;
        public float DamageThreshold;
        public int APResist;
        public bool IndirectPower;
        public bool IsPowerArmor;
        public bool IsBulkhead;
        public int quadrant = -1;
        public int TargetTracking;
        public int TargetAccuracy;
        public bool DisableRotation;
        public int ExplosionRadius;
        public int ExplosionDamage;
        public float RepairDifficulty = 1;
        public string ShieldBubbleColor;
        public float AmplifyShields;
        public float Regenerate;
        public float AccuracyPercent = -1;
    }
}
