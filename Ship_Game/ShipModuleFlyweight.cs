using System.Xml.Serialization;

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
        public readonly bool Explodes;
        public readonly float SensorRange;
        public readonly float MechanicalBoardingDefense;
        public readonly float EMPProtection;
        public readonly int PowerRadius;
        public readonly int TechLevel;
        public readonly float OrdnanceAddedPerSecond;
        public readonly string BombType;
        public readonly float WarpMassCapacity;
        public readonly float BonusRepairRate;
        public readonly float CargoCapacity;
        public readonly float ShieldRadius;
        public readonly float ShieldPowerMax;
        public readonly float ShieldRechargeRate;
        public readonly float ShieldRechargeCombatRate;
        public readonly float ShieldRechargeDelay;
        public readonly float ShieldThreshold;
        public readonly float ShieldKineticResist;
        public readonly float ShieldEnergyResist;
        public readonly float ShieldExplosiveResist;
        public readonly float ShieldMissileResist;
        public readonly float ShieldHybridResist;
        public readonly float ShieldBeamResist;
        public readonly float NumberOfColonists;
        public readonly float NumberOfEquipment;
        public readonly float NumberOfFood;
        public readonly bool IsSupplyBay;
        public readonly bool IsTroopBay;
        public readonly float HangarTimerConstant = 30f;
        public readonly float Thrust;
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
        public readonly float TorpedoResist;
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
            Explodes                    = s.Explodes;
            SensorRange                 = s.SensorRange;
            MechanicalBoardingDefense   = s.MechanicalBoardingDefense;
            EMPProtection               = s.EMPProtection;
            PowerRadius                 = s.PowerRadius;
            TechLevel                   = s.TechLevel;
            OrdnanceAddedPerSecond      = s.OrdnanceAddedPerSecond;
            BombType                    = s.BombType;
            WarpMassCapacity            = s.WarpMassCapacity;
            BonusRepairRate             = s.BonusRepairRate;
            CargoCapacity               = s.CargoCapacity;
            ShieldRadius                = s.ShieldRadius;
            ShieldPowerMax              = s.ShieldPowerMax;
            AmplifyShields              = s.AmplifyShields;
            ShieldRechargeRate          = s.ShieldRechargeRate;
            ShieldRechargeCombatRate    = s.ShieldRechargeCombatRate;
            ShieldRechargeDelay         = s.ShieldRechargeDelay;
            ShieldThreshold             = s.ShieldThreshold;
            ShieldKineticResist         = s.ShieldKineticResist;
            ShieldEnergyResist          = s.ShieldEnergyResist;
            ShieldExplosiveResist       = s.ShieldExplosiveResist;
            ShieldMissileResist         = s.ShieldMissileResist;
            ShieldHybridResist          = s.ShieldHybridResist;
            ShieldBeamResist            = s.ShieldBeamResist;
            NumberOfColonists           = s.NumberOfColonists;
            NumberOfEquipment           = s.NumberOfEquipment;
            NumberOfFood                = s.NumberOfFood;
            IsSupplyBay                 = s.IsSupplyBay;
            IsTroopBay                  = s.IsTroopBay;
            HangarTimerConstant         = s.HangarTimerConstant;
            Thrust                      = s.Thrust;
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
            TorpedoResist               = s.TorpedoResist;
            DamageThreshold             = s.DamageThreshold;
            APResist                    = s.APResist;
            ExplosionDamage             = s.ExplosionDamage > 0 ? s.ExplosionDamage : s.XSize * s.YSize * 2500;
            ExplosionRadius             = s.ExplosionRadius > 0 ? s.ExplosionRadius: s.XSize * s.YSize * 64;
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
            WeaponInaccuracyBase        = Weapon.GetWeaponInaccuracyBase(s.XSize * s.YSize, s.AccuracyPercent);
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
        public int XSize = 1;
        public int YSize = 1;
        public string ResourceStored;
        public float ResourceStorageAmount;
        public string ResourceRequired;
        public bool IsCommandModule;
        public bool IsRepairModule;
        public string[] PermittedHangarRoles;
        public short MaximumHangarShipSize;
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
        public bool Explodes;
        public float SensorRange;
        public float MechanicalBoardingDefense;
        public float EMPProtection;
        public int PowerRadius;
        public bool Powered;
        public int TechLevel;
        public float OrdnanceAddedPerSecond;
        public string BombType;
        public float WarpMassCapacity;
        public float FieldOfFire;
        public float BonusRepairRate;
        public float CargoCapacity;
        public float HealthMax;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public Restrictions Restrictions;
        public float ShieldPower;
        public float ShieldRadius;
        public float ShieldPowerMax;
        public float ShieldRechargeRate;
        public float ShieldRechargeCombatRate;
        public float ShieldRechargeDelay;
        public float ShieldThreshold;
        public float ShieldKineticResist;
        public float ShieldEnergyResist;
        public float ShieldExplosiveResist;
        public float ShieldMissileResist;
        public float ShieldHybridResist;
        public float ShieldBeamResist;
        public float NumberOfColonists;
        public float NumberOfEquipment;
        public float NumberOfFood;
        public string HangarShipUID;
        public bool IsSupplyBay;
        public bool IsTroopBay;
        public float HangarTimerConstant = 30f;
        public float HangarTimer;
        public float Thrust;
        public int WarpThrust;
        public int TurnThrust;
        public float PowerFlowMax;
        public float PowerDraw;
        public float PowerDrawAtWarp;
        public float PowerStoreMax;
        public float HealPerTurn;
        public bool MountLeft;
        public bool MountRight;
        public bool MountRear;
        public short OrdinanceCapacity;
        public int TroopCapacity;
        public int TroopsSupplied;
        public float Cost;
        public ShipModuleType ModuleType;
        public string IconTexturePath;
        public string UID;
        public float InhibitionRadius;
        public float FTLSpoolTime;
        public float ECM;
        public float SensorBonus;
        public float TransporterTimerConstant;
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
        public float TorpedoResist;
        public float DamageThreshold;
        public int APResist;
        public bool IndirectPower;
        public bool IsPowerArmor;
        public bool IsBulkhead;
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
