using System.Xml.Serialization;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Gameplay
{
    //These simple classes are the member Variable holding sub_classes used to hold the less used variables
    //for the game objects that are extremely numerous. They are essentially just groups of member variables
    //that will allow a parent object to have all of these variables available to them without having them
    //all allocated in memory unless they are needed. By my calculations, this is 313 bytes.       -Gretman

    [StarDataType]
    public sealed class ShipModuleFlyweight
    {
        public static int TotalNumModules { get; private set; } //To track how many advanced modules are being created.

        [StarData] public readonly float FTLSpeed;
        [StarData] public readonly string DeployBuildingOnColonize;
        [StarData] public readonly string ResourceStored;
        [StarData] public readonly float ResourceStorageAmount;
        [StarData] public readonly bool IsCommandModule;
        [StarData] public readonly bool IsRepairModule;
        [StarData] public readonly string[] PermittedHangarRoles;
        [StarData] public readonly short MaximumHangarShipSize;
        [StarData] public readonly bool DroneModule      = true;
        [StarData] public readonly bool FighterModule    = true;
        [StarData] public readonly bool CorvetteModule   = true;
        [StarData] public readonly bool FrigateModule    = true;
        [StarData] public readonly bool DestroyerModule  = true;
        [StarData] public readonly bool CruiserModule    = true;
        [StarData] public readonly bool BattleshipModule = true;
        [StarData] public readonly bool CapitalModule    = true;
        [StarData] public readonly bool FreighterModule  = true;
        [StarData] public readonly bool PlatformModule   = true;
        [StarData] public readonly bool StationModule    = true;
        [StarData] public readonly float SensorRange;
        [StarData] public readonly float MechanicalBoardingDefense;
        [StarData] public readonly float EMPProtection;
        [StarData] public readonly int PowerRadius;
        [StarData] public readonly int TechLevel;
        [StarData] public readonly float OrdnanceAddedPerSecond;
        [StarData] public readonly string BombType;
        [StarData] public readonly float WarpMassCapacity;
        [StarData] public readonly float BonusRepairRate;
        [StarData] public readonly float CargoCapacity;
        [StarData] public readonly float ShieldRadius;
        [StarData] public readonly float ShieldPowerMax;
        [StarData] public readonly float ShieldRechargeRate;
        [StarData] public readonly float ShieldRechargeCombatRate;
        [StarData] public readonly float ShieldRechargeDelay;
        [StarData] public readonly float ShieldDeflection;
        [StarData] public readonly float ShieldKineticResist;
        [StarData] public readonly float ShieldEnergyResist;
        [StarData] public readonly float ShieldExplosiveResist;
        [StarData] public readonly float ShieldMissileResist;
        [StarData] public readonly float ShieldPlasmaResist;
        [StarData] public readonly float ShieldBeamResist;
        [StarData] public readonly float NumberOfColonists;
        [StarData] public readonly float NumberOfEquipment;
        [StarData] public readonly float NumberOfFood;
        [StarData] public readonly bool IsSupplyBay;
        [StarData] public readonly bool IsTroopBay;
        [StarData] public readonly float HangarTimerConstant = 30f;
        [StarData] public readonly float Thrust;
        [StarData] public readonly float WarpThrust;
        [StarData] public readonly float TurnThrust;
        [StarData] public readonly float PowerFlowMax;
        [StarData] public readonly float PowerDraw;
        [StarData] public readonly float PowerDrawAtWarp;
        [StarData] public readonly float PowerStoreMax;
        [StarData] public readonly float HealPerTurn;
        [StarData] public readonly int TroopCapacity;
        [StarData] public readonly int TroopsSupplied;
        [StarData] public readonly float Cost;
        [StarData] public readonly float InhibitionRadius;
        [StarData] public readonly float FTLSpoolTime;
        [StarData] public readonly float ECM;
        [StarData] public readonly float SensorBonus;
        [StarData] public readonly float TransporterTimerConstant;
        [StarData] public readonly float TransporterRange;
        [StarData] public readonly float TransporterPower;
        [StarData] public readonly float TransporterOrdnance;
        [StarData] public readonly int TransporterTroopLanding;
        [StarData] public readonly int TransporterTroopAssault;

        //Doctor: these were floats for a reason: they're decimal fractions to define damage resistance. Loading them with sbytes caused crashes from xml and broke the resistance mechanics.
        //Likewise, DamageThreshold is a set damage amount which armour disregards damage value under - immediately broke as a byte as some armours had values higher than 255.
        [StarData] public readonly float KineticResist;
        [StarData] public readonly float EnergyResist;
        [StarData] public readonly float GuidedResist;
        [StarData] public readonly float MissileResist;
        [StarData] public readonly float PlasmaResist;
        [StarData] public readonly float BeamResist;
        [StarData] public readonly float ExplosiveResist;
        [StarData] public readonly float TorpedoResist;
        [StarData] public readonly float Deflection;
        [StarData] public readonly int APResist;
        [StarData] public readonly bool IndirectPower;
        [StarData] public readonly int TargetTracking;
        [StarData] public readonly int TargetAccuracy;
        [StarData] public readonly bool DisableRotation;
        [StarData] public readonly float AmplifyShields;
        [StarData] public readonly int ExplosionDamage;
        [StarData] public readonly int ExplosionRadius;
        [StarData] public readonly float RepairDifficulty = 1f;
        [StarData] public readonly string ShieldBubbleColor;
        [StarData] public readonly float Regenerate;
        [StarData] public float AccuracyPercent;
        [StarData] public readonly float WeaponInaccuracyBase;
        [StarData] public readonly string UID = string.Empty;
        [StarData] public float ResearchPerTurn;
        [StarData] public float ProcessingPerTurn;

        public static readonly ShipModuleFlyweight Empty = new ShipModuleFlyweight();    //A static instance to be assigned to leftover modules

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
            ShieldDeflection            = s.ShieldDeflection;
            ShieldKineticResist         = s.ShieldKineticResist;
            ShieldEnergyResist          = s.ShieldEnergyResist;
            ShieldExplosiveResist       = s.ShieldExplosiveResist;
            ShieldMissileResist         = s.ShieldMissileResist;
            ShieldPlasmaResist          = s.ShieldPlasmaResist;
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
            PlasmaResist                = s.PlasmaResist;
            BeamResist                  = s.BeamResist;
            ExplosiveResist             = s.ExplosiveResist;
            TorpedoResist               = s.TorpedoResist;
            Deflection                  = s.Deflection;
            APResist                    = s.APResist;
            ExplosionDamage             = s.ExplosionDamage;
            ExplosionRadius             = s.ExplosionRadius > 0 ? s.ExplosionRadius : 16;
            RepairDifficulty            = s.RepairDifficulty;
            Regenerate                  = s.Regenerate;
            ShieldBubbleColor           = s.ShieldBubbleColor;
            IndirectPower               = s.IndirectPower;
            TargetTracking              = s.TargetTracking;
            TargetAccuracy              = s.TargetAccuracy;
            DisableRotation             = s.DisableRotation;
            UID                         = s.UID;
            AccuracyPercent             = s.AccuracyPercent;
            ResearchPerTurn             = s.ResearchPerTurn;
            ProcessingPerTurn           = s.ProcessingPerTurn;
            WeaponInaccuracyBase        = WeaponTemplate.GetWeaponInaccuracyBase(s.XSize * s.YSize, s.AccuracyPercent);

            if (s.ExplosionDamage > 0)
            {
                // Consider the module size to extend the explosion a little, if its bigger than 1x1;
                // Adding 8 since the explosion is the in the center of the module, which is 8 radius.
                float averageModuleSize = (s.XSize + s.YSize - 2) * 0.5f;
                if (averageModuleSize > 0)
                    ExplosionRadius = (int)(8 + ExplosionRadius + averageModuleSize * 16);
            }
        }
    }

    /// <summary>
    /// This is used for ShipModule XML Templates
    /// </summary>
    [XmlType("ShipModule")]
    public sealed class ShipModule_XMLTemplate
    {
        public float Mass;
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
        public float ShieldDeflection;
        public float ShieldKineticResist;
        public float ShieldEnergyResist;
        public float ShieldExplosiveResist;
        public float ShieldMissileResist;
        public float ShieldPlasmaResist;
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
        public float PlasmaResist;
        public float BeamResist;
        public float ExplosiveResist;
        public float TorpedoResist;
        public float Deflection;
        public int APResist;
        public bool IndirectPower;
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
        public float ResearchPerTurn;
        public float ProcessingPerTurn;
    }
}
