using Microsoft.Xna.Framework;
using Particle3DSample;
using System;
using System.Collections.Generic;

namespace Ship_Game.Gameplay
{
    //These simple classes are the member Variable holding sub_classes used to hold the less used variables
    //for the game objects that are extremely numerous. They are essentially just groups of member variables
    //that will allow a parent object to have all of these variables available to them without having them
    //all allocated in memory unless they are needed. By my calculations, this is 313 bytes.       -Gretman

    public sealed class ShipModule_Advanced
    {
        private static int TotalNumModules = 0; //To track how many advanced modules are being created.

        public float FTLSpeed;
        public string DeployBuildingOnColonize;
        public string ResourceStored;
        public float ResourceStorageAmount;
        public bool IsCommandModule;
        public bool IsRepairModule;
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
        public float KineticResist = 0f;
        public float EnergyResist = 0f;
        public float GuidedResist = 0f;
        public float MissileResist = 0f;
        public float HybridResist = 0f;
        public float BeamResist = 0f;
        public float ExplosiveResist = 0f;
        public float InterceptResist = 0f;
        public float RailgunResist = 0f;
        public float SpaceBombResist = 0f;
        public float BombResist = 0f;
        public float BioWeaponResist = 0f;
        public float DroneResist = 0f;
        public float WarpResist = 0f;
        public float TorpedoResist = 0f;
        public float CannonResist = 0f;
        public float SubspaceResist = 0f;
        public float PDResist = 0f;
        public float FlakResist = 0f;
        public float DamageThreshold = 0f;
        public int APResist = 0;
        public bool IndirectPower = false;
        public bool isPowerArmour = false;
        public bool isBulkhead = false;
        public sbyte TargetTracking = 0;

        public static ShipModule_Advanced Empty = new ShipModule_Advanced();    //A static instance to be assigned to leftover modules

        //CG Causing trouble going to use this for some other purposes! like storing the techs needed for this module. 
        HashSet<string> TechRequired = new HashSet<string>();
        public string UID="";

        public ShipModule_Advanced()    //Constructor
        {
            //This should only be called once per module XML file -Gretman
            ++TotalNumModules;
            System.Diagnostics.Debug.WriteLine("ShipModule_Advanced Created. Total so far: " + TotalNumModules);
        }
    }

    [System.Xml.Serialization.XmlType("ShipModule")]
    public sealed class ShipModule_Deserialize : GameplayObject
    {

        //This is for the Deserialization process, when reading the XML files.  -Gretman
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
        public List<string> PermittedHangarRoles;
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
        //public List<ShipModule> LinkedModulesList = new List<ShipModule>();       //Caused Deserialization issue
        public string BombType;
        public float WarpMassCapacity;
        public float FieldOfFire;
        public float facing;
        public Vector2 XMLPosition;
        //public ShipModule ParentOfDummy;       //Caused Deserialization issue
        public float BonusRepairRate;
        public float Cargo_Capacity;
        public float HealthMax;
        public string WeaponType;
        public ushort NameIndex;
        public ushort DescriptionIndex;
        public Ship_Game.Gameplay.Restrictions Restrictions;
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
        //public Weapon InstalledWeapon;       //Caused Deserialization issue
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
        //public ModuleSlot installedSlot;       //Caused Deserialization issue
        public bool isExternal;
        public bool TrulyExternal;
        public float InhibitionRadius;
        public float FTLSpoolTime;
        public float ECM;
        public float SensorBonus;
        public float TransporterTimerConstant;
        public float TransporterTimer = 0f;
        public float TransporterRange;
        public float TransporterPower;
        public float TransporterOrdnance;
        public byte TransporterTroopLanding;
        public byte TransporterTroopAssault;
        public int TargetValue = 0;
        public float KineticResist = 0;
        public float EnergyResist = 0;
        public float GuidedResist = 0;
        public float MissileResist = 0;
        public float HybridResist = 0;
        public float BeamResist = 0;
        public float ExplosiveResist = 0;
        public float InterceptResist = 0;
        public float RailgunResist = 0;
        public float SpaceBombResist = 0;
        public float BombResist = 0;
        public float BioWeaponResist = 0;
        public float DroneResist = 0;
        public float WarpResist = 0;
        public float TorpedoResist = 0;
        public float CannonResist = 0;
        public float SubspaceResist = 0;
        public float PDResist = 0;
        public float FlakResist = 0;
        public float DamageThreshold = 0;
        public int APResist = 0;
        public bool IndirectPower = false;
        public bool isPowerArmour = false;
        public bool isBulkhead = false;
        public sbyte quadrant = -1;
        public sbyte TargetTracking = 0;

        public ShipModule ConvertToShipModule()
        {                                                           //This functions translates from the old ShipModule (eg the XML files)
                                                                    //to the new ShipModule, sorting out the variables that now live in the
                                                                    //'Advanced' object.        -Gretman
            ShipModule ReturnModule = new ShipModule();
            ReturnModule.Advanced = new ShipModule_Advanced();

            ReturnModule.XSIZE = this.XSIZE;
            ReturnModule.YSIZE = this.YSIZE;
            ReturnModule.Mass = this.Mass;
            ReturnModule.PermittedHangarRoles = this.PermittedHangarRoles;
            ReturnModule.Powered = this.Powered;
            ReturnModule.isDummy = this.isDummy;
            //ReturnModule.LinkedModulesList = this.LinkedModulesList;
            ReturnModule.FieldOfFire = this.FieldOfFire;
            ReturnModule.facing = this.facing;
            ReturnModule.XMLPosition = this.XMLPosition;
            //ReturnModule.Parent = this.Parent;
            //ReturnModule.ParentOfDummy = this.ParentOfDummy;
            ReturnModule.HealthMax = this.HealthMax;
            ReturnModule.WeaponType = this.WeaponType;
            ReturnModule.NameIndex = this.NameIndex;
            ReturnModule.DescriptionIndex = this.DescriptionIndex;
            ReturnModule.Restrictions = this.Restrictions;
            ReturnModule.shield_power = this.shield_power;
            ReturnModule.shieldsOff = this.shieldsOff;
            //ReturnModule.shield = this.shield;
            ReturnModule.hangarShipUID = this.hangarShipUID;
            //ReturnModule.hangarShip = this.hangarShip;
            ReturnModule.hangarTimer = this.hangarTimer;
            ReturnModule.isWeapon = this.isWeapon;
            //ReturnModule.InstalledWeapon = this.InstalledWeapon;
            ReturnModule.OrdinanceCapacity = this.OrdinanceCapacity;
            //ReturnModule.Center3D = this.Center3D;
            ReturnModule.BombTimer = this.BombTimer;
            ReturnModule.ModuleType = this.ModuleType;
            ReturnModule.moduleCenter = this.moduleCenter;
            ReturnModule.ModuleCenter = this.ModuleCenter;
            ReturnModule.IconTexturePath = this.IconTexturePath;
            ReturnModule.UID = this.UID;
            //ReturnModule.installedSlot = this.installedSlot;
            ReturnModule.isExternal = this.isExternal;
            ReturnModule.TargetValue = this.TargetValue;
            ReturnModule.quadrant = this.quadrant;

            ReturnModule.Advanced.FTLSpeed = this.FTLSpeed;
            ReturnModule.Advanced.DeployBuildingOnColonize = this.DeployBuildingOnColonize;
            ReturnModule.Advanced.ResourceStored = this.ResourceStored;
            ReturnModule.Advanced.ResourceStorageAmount = this.ResourceStorageAmount;
            ReturnModule.Advanced.IsCommandModule = this.IsCommandModule;
            ReturnModule.Advanced.IsRepairModule = this.IsRepairModule;
            ReturnModule.Advanced.MaximumHangarShipSize = this.MaximumHangarShipSize;
            ReturnModule.Advanced.FightersOnly = this.FightersOnly;
            ReturnModule.Advanced.DroneModule = this.DroneModule;
            ReturnModule.Advanced.FighterModule = this.FighterModule;
            ReturnModule.Advanced.CorvetteModule = this.CorvetteModule;
            ReturnModule.Advanced.FrigateModule = this.FrigateModule;
            ReturnModule.Advanced.DestroyerModule = this.DestroyerModule;
            ReturnModule.Advanced.CruiserModule = this.CruiserModule;
            ReturnModule.Advanced.CarrierModule = this.CarrierModule;
            ReturnModule.Advanced.CapitalModule = this.CapitalModule;
            ReturnModule.Advanced.FreighterModule = this.FreighterModule;
            ReturnModule.Advanced.PlatformModule = this.PlatformModule;
            ReturnModule.Advanced.StationModule = this.StationModule;
            ReturnModule.Advanced.explodes = this.explodes;
            ReturnModule.Advanced.SensorRange = this.SensorRange;
            ReturnModule.Advanced.MechanicalBoardingDefense = this.MechanicalBoardingDefense;
            ReturnModule.Advanced.EMP_Protection = this.EMP_Protection;
            ReturnModule.Advanced.PowerRadius = this.PowerRadius;
            ReturnModule.Advanced.TechLevel = this.TechLevel;
            ReturnModule.Advanced.OrdnanceAddedPerSecond = this.OrdnanceAddedPerSecond;
            ReturnModule.Advanced.BombType = this.BombType;
            ReturnModule.Advanced.WarpMassCapacity = this.WarpMassCapacity;
            ReturnModule.Advanced.BonusRepairRate = this.BonusRepairRate;
            ReturnModule.Advanced.Cargo_Capacity = this.Cargo_Capacity;
            ReturnModule.Advanced.shield_radius = this.shield_radius;
            ReturnModule.Advanced.shield_power_max = this.shield_power_max;
            ReturnModule.Advanced.shield_recharge_rate = this.shield_recharge_rate;
            ReturnModule.Advanced.shield_recharge_combat_rate = this.shield_recharge_combat_rate;
            ReturnModule.Advanced.shield_recharge_delay = this.shield_recharge_delay;
            ReturnModule.Advanced.shield_threshold = this.shield_threshold;
            ReturnModule.Advanced.shield_kinetic_resist = this.shield_kinetic_resist;
            ReturnModule.Advanced.shield_energy_resist = this.shield_energy_resist;
            ReturnModule.Advanced.shield_explosive_resist = this.shield_explosive_resist;
            ReturnModule.Advanced.shield_missile_resist = this.shield_missile_resist;
            ReturnModule.Advanced.shield_flak_resist = this.shield_flak_resist;
            ReturnModule.Advanced.shield_hybrid_resist = this.shield_hybrid_resist;
            ReturnModule.Advanced.shield_railgun_resist = this.shield_railgun_resist;
            ReturnModule.Advanced.shield_subspace_resist = this.shield_subspace_resist;
            ReturnModule.Advanced.shield_warp_resist = this.shield_warp_resist;
            ReturnModule.Advanced.shield_beam_resist = this.shield_beam_resist;
            ReturnModule.Advanced.numberOfColonists = this.numberOfColonists;
            ReturnModule.Advanced.numberOfEquipment = this.numberOfEquipment;
            ReturnModule.Advanced.numberOfFood = this.numberOfFood;
            ReturnModule.Advanced.IsSupplyBay = this.IsSupplyBay;
            ReturnModule.Advanced.IsTroopBay = this.IsTroopBay;
            ReturnModule.Advanced.hangarTimerConstant = this.hangarTimerConstant;
            ReturnModule.Advanced.thrust = this.thrust;
            ReturnModule.Advanced.WarpThrust = this.WarpThrust;
            ReturnModule.Advanced.TurnThrust = this.TurnThrust;
            ReturnModule.Advanced.PowerFlowMax = this.PowerFlowMax;
            ReturnModule.Advanced.PowerDraw = this.PowerDraw;
            ReturnModule.Advanced.PowerDrawAtWarp = this.PowerDrawAtWarp;
            ReturnModule.Advanced.PowerStoreMax = this.PowerStoreMax;
            ReturnModule.Advanced.HealPerTurn = this.HealPerTurn;
            ReturnModule.Advanced.TroopCapacity = this.TroopCapacity;
            ReturnModule.Advanced.TroopsSupplied = this.TroopsSupplied;
            ReturnModule.Advanced.Cost = this.Cost;
            ReturnModule.Advanced.InhibitionRadius = this.InhibitionRadius;
            ReturnModule.Advanced.FTLSpoolTime = this.FTLSpoolTime;
            ReturnModule.Advanced.ECM = this.ECM;
            ReturnModule.Advanced.SensorBonus = this.SensorBonus;
            ReturnModule.Advanced.TransporterTimerConstant = this.TransporterTimerConstant;
            ReturnModule.Advanced.TransporterRange = this.TransporterRange;
            ReturnModule.Advanced.TransporterPower = this.TransporterPower;
            ReturnModule.Advanced.TransporterOrdnance = this.TransporterOrdnance;
            ReturnModule.Advanced.TransporterTroopLanding = this.TransporterTroopLanding;
            ReturnModule.Advanced.TransporterTroopAssault = this.TransporterTroopAssault;
            ReturnModule.Advanced.KineticResist = this.KineticResist;
            ReturnModule.Advanced.EnergyResist = this.EnergyResist;
            ReturnModule.Advanced.GuidedResist = this.GuidedResist;
            ReturnModule.Advanced.MissileResist = this.MissileResist;
            ReturnModule.Advanced.HybridResist = this.HybridResist;
            ReturnModule.Advanced.BeamResist = this.BeamResist;
            ReturnModule.Advanced.ExplosiveResist = this.ExplosiveResist;
            ReturnModule.Advanced.InterceptResist = this.InterceptResist;
            ReturnModule.Advanced.RailgunResist = this.RailgunResist;
            ReturnModule.Advanced.SpaceBombResist = this.SpaceBombResist;
            ReturnModule.Advanced.BombResist = this.BombResist;
            ReturnModule.Advanced.BioWeaponResist = this.BioWeaponResist;
            ReturnModule.Advanced.DroneResist = this.DroneResist;
            ReturnModule.Advanced.WarpResist = this.WarpResist;
            ReturnModule.Advanced.TorpedoResist = this.TorpedoResist;
            ReturnModule.Advanced.CannonResist = this.CannonResist;
            ReturnModule.Advanced.SubspaceResist = this.SubspaceResist;
            ReturnModule.Advanced.PDResist = this.PDResist;
            ReturnModule.Advanced.FlakResist = this.FlakResist;
            ReturnModule.Advanced.DamageThreshold = this.DamageThreshold;
            ReturnModule.Advanced.APResist = this.APResist;
            ReturnModule.Advanced.IndirectPower = this.IndirectPower;
            ReturnModule.Advanced.isPowerArmour = this.isPowerArmour;
            ReturnModule.Advanced.isBulkhead = this.isBulkhead;
            ReturnModule.Advanced.TargetTracking = this.TargetTracking;

            return ReturnModule;
        }
    }//class
}//namespace