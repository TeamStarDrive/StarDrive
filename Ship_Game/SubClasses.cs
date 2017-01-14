using Microsoft.Xna.Framework;
using Particle3DSample;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
    //These simple classes are the member Variable holding sub_classes used to hold the less used variables
    //for the game objects that are extremely numerous. They are essentially just groups of member variables
    //that will allow a parent object to have all of these variables available to them without having them
    //all allocated in memory unless they are needed. By my calculations, this is 313 bytes.       -Gretman

    public sealed class ShipModule_Advanced
    {
        private static int TotalNumModules; //To track how many advanced modules are being created.

        public float FTLSpeed;
        public string DeployBuildingOnColonize;
        public string ResourceStored;
        public float ResourceStorageAmount;
        public bool IsCommandModule;
        public bool IsRepairModule;
        public short MaximumHangarShipSize;
        public bool FightersOnly;
        public bool DroneModule;
        public bool FighterModule = true;
        public bool CorvetteModule = true;
        public bool FrigateModule = true;
        public bool DestroyerModule = true;
        public bool CruiserModule = true;
        public bool CarrierModule = true;
        public bool CapitalModule = true;
        public bool FreighterModule = true;
        public bool PlatformModule = true;
        public bool StationModule = true;
        public bool explodes;
        public float SensorRange;
        public float MechanicalBoardingDefense;
        public float EMP_Protection;
        public byte PowerRadius;
        public byte TechLevel;
        public float OrdnanceAddedPerSecond;
        public string BombType;
        public float WarpMassCapacity;
        public float BonusRepairRate;
        public float Cargo_Capacity;
        public float shield_radius;
        public float shield_power_max;
        public float shield_recharge_rate;
        public float shield_recharge_combat_rate;
        public float shield_recharge_delay;
        public float shield_threshold;
        public sbyte shield_kinetic_resist;
        public sbyte shield_energy_resist;
        public sbyte shield_explosive_resist;
        public sbyte shield_missile_resist;
        public sbyte shield_flak_resist;
        public sbyte shield_hybrid_resist;
        public sbyte shield_railgun_resist;
        public sbyte shield_subspace_resist;
        public sbyte shield_warp_resist;
        public sbyte shield_beam_resist;
        public float numberOfColonists;
        public float numberOfEquipment;
        public float numberOfFood;
        public bool IsSupplyBay;
        public bool IsTroopBay;
        public float hangarTimerConstant = 30f;
        public float thrust;
        public float WarpThrust;
        public float TurnThrust;
        public float PowerFlowMax;
        public float PowerDraw;
        public float PowerDrawAtWarp;
        public float PowerStoreMax;
        public float HealPerTurn;
        public byte TroopCapacity;
        public byte TroopsSupplied;
        public float Cost;
        public float InhibitionRadius;
        public float FTLSpoolTime;
        public float ECM;
        public float SensorBonus;
        public float TransporterTimerConstant;
        public float TransporterRange;
        public float TransporterPower;
        public float TransporterOrdnance;
        public byte TransporterTroopLanding;
        public byte TransporterTroopAssault;

        //Doctor: these were floats for a reason: they're decimal fractions to define damage resistance. Loading them with sbytes caused crashes from xml and broke the resistance mechanics.
        //Likewise, DamageThreshold is a set damage amount which armour disregards damage value under - immediately broke as a byte as some armours had values higher than 255.
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
        public bool isPowerArmour;
        public bool isBulkhead;
        public sbyte TargetTracking;
        public sbyte FixedTracking;

        public static ShipModule_Advanced Empty = new ShipModule_Advanced();    //A static instance to be assigned to leftover modules

        //CG Causing trouble going to use this for some other purposes! like storing the techs needed for this module. 
        HashSet<string> TechRequired = new HashSet<string>();
        public string UID="";

        public ShipModule_Advanced()    //Constructor
        {
            //This should only be called once per module XML file -Gretman
            ++TotalNumModules;
            Log.Info("ShipModule_Advanced Created. Total so far: {0}", TotalNumModules);
        }
    }

    [XmlType("ShipModule")]
    public sealed class ShipModule_Deserialize : GameplayObject
    {
        public float FTLSpeed;
        public string DeployBuildingOnColonize;
        public byte XSIZE = 1;
        public byte YSIZE = 1;
        public string ResourceStored;
        public float ResourceStorageAmount;
        public string ResourceRequired;
        public float ResourcePerSecond;
        public float ResourcePerSecondWarp;
        public bool IsCommandModule;
        public bool IsRepairModule;
        public Array<string> PermittedHangarRoles;
        public short MaximumHangarShipSize;
        public bool FightersOnly;
        public bool DroneModule = false;
        public bool FighterModule = true;
        public bool CorvetteModule = true;
        public bool FrigateModule = true;
        public bool DestroyerModule = true;
        public bool CruiserModule = true;
        public bool CarrierModule = true;
        public bool CapitalModule = true;
        public bool FreighterModule = true;
        public bool PlatformModule = true;
        public bool StationModule = true;
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
        public string BombType;
        public float WarpMassCapacity;
        public float FieldOfFire;
        public float facing;
        public Vector2 XMLPosition;
        public float BonusRepairRate;
        public float Cargo_Capacity;
        public float HealthMax;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public Restrictions Restrictions;
        public float shield_power;
        public bool shieldsOff = false;
        public float shield_radius;
        public float shield_power_max;
        public float shield_recharge_rate;
        public float shield_recharge_combat_rate;
        public float shield_recharge_delay;
        public float shield_threshold;
        public sbyte shield_kinetic_resist;
        public sbyte shield_energy_resist;
        public sbyte shield_explosive_resist;
        public sbyte shield_missile_resist;
        public sbyte shield_flak_resist;
        public sbyte shield_hybrid_resist;
        public sbyte shield_railgun_resist;
        public sbyte shield_subspace_resist;
        public sbyte shield_warp_resist;
        public sbyte shield_beam_resist;
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
        public byte TroopCapacity;
        public byte TroopsSupplied;
        public float Cost;
        public ShipModuleType ModuleType;
        public Vector2 moduleCenter = new Vector2(0f, 0f);
        public Vector2 ModuleCenter;
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
        public byte TransporterTroopLanding;
        public byte TransporterTroopAssault;
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
        public bool isPowerArmour;
        public bool isBulkhead;
        public sbyte quadrant = -1;
        public sbyte TargetTracking;
        public sbyte FixedTracking;

        public ShipModule ConvertToShipModule()
        {                                                           
            //This functions translates from the old ShipModule (eg the XML files)
            //to the new ShipModule, sorting out the variables that now live in the
            //'Advanced' object.        -Gretman
            var module = new ShipModule
            {
                Advanced = new ShipModule_Advanced(),
                XSIZE                = XSIZE,
                YSIZE                = YSIZE,
                Mass                 = Mass,
                PermittedHangarRoles = PermittedHangarRoles,
                Powered              = Powered,
                isDummy              = isDummy,
                FieldOfFire          = FieldOfFire,
                facing               = facing,
                XMLPosition          = XMLPosition,
                HealthMax            = HealthMax,
                WeaponType           = WeaponType,
                NameIndex            = NameIndex,
                DescriptionIndex     = DescriptionIndex,
                Restrictions         = Restrictions,
                shield_power         = shield_power,
                shieldsOff           = shieldsOff,
                hangarShipUID        = hangarShipUID,
                hangarTimer          = hangarTimer,
                isWeapon             = isWeapon,
                OrdinanceCapacity    = OrdinanceCapacity,
                BombTimer            = BombTimer,
                ModuleType           = ModuleType,
                moduleCenter         = moduleCenter,
                ModuleCenter         = ModuleCenter,
                IconTexturePath      = IconTexturePath,
                UID                  = UID,
                isExternal           = isExternal,
                TargetValue          = TargetValue,
                quadrant             = quadrant
            };


            module.Advanced.FTLSpeed                    = FTLSpeed;
            module.Advanced.DeployBuildingOnColonize    = DeployBuildingOnColonize;
            module.Advanced.ResourceStored              = ResourceStored;
            module.Advanced.ResourceStorageAmount       = ResourceStorageAmount;
            module.Advanced.IsCommandModule             = IsCommandModule;
            module.Advanced.IsRepairModule              = IsRepairModule;
            module.Advanced.MaximumHangarShipSize       = MaximumHangarShipSize;
            module.Advanced.FightersOnly                = FightersOnly;
            module.Advanced.DroneModule                 = DroneModule;
            module.Advanced.FighterModule               = FighterModule;
            module.Advanced.CorvetteModule              = CorvetteModule;
            module.Advanced.FrigateModule               = FrigateModule;
            module.Advanced.DestroyerModule             = DestroyerModule;
            module.Advanced.CruiserModule               = CruiserModule;
            module.Advanced.CarrierModule               = CarrierModule;
            module.Advanced.CapitalModule               = CapitalModule;
            module.Advanced.FreighterModule             = FreighterModule;
            module.Advanced.PlatformModule              = PlatformModule;
            module.Advanced.StationModule               = StationModule;
            module.Advanced.explodes                    = explodes;
            module.Advanced.SensorRange                 = SensorRange;
            module.Advanced.MechanicalBoardingDefense   = MechanicalBoardingDefense;
            module.Advanced.EMP_Protection              = EMP_Protection;
            module.Advanced.PowerRadius                 = PowerRadius;
            module.Advanced.TechLevel                   = TechLevel;
            module.Advanced.OrdnanceAddedPerSecond      = OrdnanceAddedPerSecond;
            module.Advanced.BombType                    = BombType;
            module.Advanced.WarpMassCapacity            = WarpMassCapacity;
            module.Advanced.BonusRepairRate             = BonusRepairRate;
            module.Advanced.Cargo_Capacity              = Cargo_Capacity;
            module.Advanced.shield_radius               = shield_radius;
            module.Advanced.shield_power_max            = shield_power_max;
            module.Advanced.shield_recharge_rate        = shield_recharge_rate;
            module.Advanced.shield_recharge_combat_rate = shield_recharge_combat_rate;
            module.Advanced.shield_recharge_delay       = shield_recharge_delay;
            module.Advanced.shield_threshold            = shield_threshold;
            module.Advanced.shield_kinetic_resist       = shield_kinetic_resist;
            module.Advanced.shield_energy_resist        = shield_energy_resist;
            module.Advanced.shield_explosive_resist     = shield_explosive_resist;
            module.Advanced.shield_missile_resist       = shield_missile_resist;
            module.Advanced.shield_flak_resist          = shield_flak_resist;
            module.Advanced.shield_hybrid_resist        = shield_hybrid_resist;
            module.Advanced.shield_railgun_resist       = shield_railgun_resist;
            module.Advanced.shield_subspace_resist      = shield_subspace_resist;
            module.Advanced.shield_warp_resist          = shield_warp_resist;
            module.Advanced.shield_beam_resist          = shield_beam_resist;
            module.Advanced.numberOfColonists           = numberOfColonists;
            module.Advanced.numberOfEquipment           = numberOfEquipment;
            module.Advanced.numberOfFood                = numberOfFood;
            module.Advanced.IsSupplyBay                 = IsSupplyBay;
            module.Advanced.IsTroopBay                  = IsTroopBay;
            module.Advanced.hangarTimerConstant         = hangarTimerConstant;
            module.Advanced.thrust                      = thrust;
            module.Advanced.WarpThrust                  = WarpThrust;
            module.Advanced.TurnThrust                  = TurnThrust;
            module.Advanced.PowerFlowMax                = PowerFlowMax;
            module.Advanced.PowerDraw                   = PowerDraw;
            module.Advanced.PowerDrawAtWarp             = PowerDrawAtWarp;
            module.Advanced.PowerStoreMax               = PowerStoreMax;
            module.Advanced.HealPerTurn                 = HealPerTurn;
            module.Advanced.TroopCapacity               = TroopCapacity;
            module.Advanced.TroopsSupplied              = TroopsSupplied;
            module.Advanced.Cost                        = Cost;
            module.Advanced.InhibitionRadius            = InhibitionRadius;
            module.Advanced.FTLSpoolTime                = FTLSpoolTime;
            module.Advanced.ECM                         = ECM;
            module.Advanced.SensorBonus                 = SensorBonus;
            module.Advanced.TransporterTimerConstant    = TransporterTimerConstant;
            module.Advanced.TransporterRange            = TransporterRange;
            module.Advanced.TransporterPower            = TransporterPower;
            module.Advanced.TransporterOrdnance         = TransporterOrdnance;
            module.Advanced.TransporterTroopLanding     = TransporterTroopLanding;
            module.Advanced.TransporterTroopAssault     = TransporterTroopAssault;
            module.Advanced.KineticResist               = KineticResist;
            module.Advanced.EnergyResist                = EnergyResist;
            module.Advanced.GuidedResist                = GuidedResist;
            module.Advanced.MissileResist               = MissileResist;
            module.Advanced.HybridResist                = HybridResist;
            module.Advanced.BeamResist                  = BeamResist;
            module.Advanced.ExplosiveResist             = ExplosiveResist;
            module.Advanced.InterceptResist             = InterceptResist;
            module.Advanced.RailgunResist               = RailgunResist;
            module.Advanced.SpaceBombResist             = SpaceBombResist;
            module.Advanced.BombResist                  = BombResist;
            module.Advanced.BioWeaponResist             = BioWeaponResist;
            module.Advanced.DroneResist                 = DroneResist;
            module.Advanced.WarpResist                  = WarpResist;
            module.Advanced.TorpedoResist               = TorpedoResist;
            module.Advanced.CannonResist                = CannonResist;
            module.Advanced.SubspaceResist              = SubspaceResist;
            module.Advanced.PDResist                    = PDResist;
            module.Advanced.FlakResist                  = FlakResist;
            module.Advanced.DamageThreshold             = DamageThreshold;
            module.Advanced.APResist                    = APResist;
            module.Advanced.IndirectPower               = IndirectPower;
            module.Advanced.isPowerArmour               = isPowerArmour;
            module.Advanced.isBulkhead                  = isBulkhead;
            module.Advanced.TargetTracking              = TargetTracking;
            module.Advanced.FixedTracking               = FixedTracking;

            return module;
        }
    }
}
