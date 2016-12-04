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
            //System.Diagnostics.Debug.WriteLine("ShipModule_Advanced Created. Total so far: {0}", TotalNumModules);
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
            ShipModule ReturnModule = new ShipModule();
            ReturnModule.Advanced = new ShipModule_Advanced();

            ReturnModule.XSIZE = XSIZE;
            ReturnModule.YSIZE = YSIZE;
            ReturnModule.Mass = Mass;
            ReturnModule.PermittedHangarRoles = PermittedHangarRoles;
            ReturnModule.Powered = Powered;
            ReturnModule.isDummy = isDummy;
            //ReturnModule.LinkedModulesList = LinkedModulesList;
            ReturnModule.FieldOfFire = FieldOfFire;
            ReturnModule.facing = facing;
            ReturnModule.XMLPosition = XMLPosition;
            //ReturnModule.Parent = Parent;
            //ReturnModule.ParentOfDummy = ParentOfDummy;
            ReturnModule.HealthMax = HealthMax;
            ReturnModule.WeaponType = WeaponType;
            ReturnModule.NameIndex = NameIndex;
            ReturnModule.DescriptionIndex = DescriptionIndex;
            ReturnModule.Restrictions = Restrictions;
            ReturnModule.shield_power = shield_power;
            ReturnModule.shieldsOff = shieldsOff;
            //ReturnModule.shield = shield;
            ReturnModule.hangarShipUID = hangarShipUID;
            //ReturnModule.hangarShip = hangarShip;
            ReturnModule.hangarTimer = hangarTimer;
            ReturnModule.isWeapon = isWeapon;
            //ReturnModule.InstalledWeapon = InstalledWeapon;
            ReturnModule.OrdinanceCapacity = OrdinanceCapacity;
            //ReturnModule.Center3D = Center3D;
            ReturnModule.BombTimer = BombTimer;
            ReturnModule.ModuleType = ModuleType;
            ReturnModule.moduleCenter = moduleCenter;
            ReturnModule.ModuleCenter = ModuleCenter;
            ReturnModule.IconTexturePath = IconTexturePath;
            ReturnModule.UID = UID;
            //ReturnModule.installedSlot = installedSlot;
            ReturnModule.isExternal = isExternal;
            ReturnModule.TargetValue = TargetValue;
            ReturnModule.quadrant = quadrant;

            ReturnModule.Advanced.FTLSpeed = FTLSpeed;
            ReturnModule.Advanced.DeployBuildingOnColonize = DeployBuildingOnColonize;
            ReturnModule.Advanced.ResourceStored = ResourceStored;
            ReturnModule.Advanced.ResourceStorageAmount = ResourceStorageAmount;
            ReturnModule.Advanced.IsCommandModule = IsCommandModule;
            ReturnModule.Advanced.IsRepairModule = IsRepairModule;
            ReturnModule.Advanced.MaximumHangarShipSize = MaximumHangarShipSize;
            ReturnModule.Advanced.FightersOnly = FightersOnly;
            ReturnModule.Advanced.DroneModule = DroneModule;
            ReturnModule.Advanced.FighterModule = FighterModule;
            ReturnModule.Advanced.CorvetteModule = CorvetteModule;
            ReturnModule.Advanced.FrigateModule = FrigateModule;
            ReturnModule.Advanced.DestroyerModule = DestroyerModule;
            ReturnModule.Advanced.CruiserModule = CruiserModule;
            ReturnModule.Advanced.CarrierModule = CarrierModule;
            ReturnModule.Advanced.CapitalModule = CapitalModule;
            ReturnModule.Advanced.FreighterModule = FreighterModule;
            ReturnModule.Advanced.PlatformModule = PlatformModule;
            ReturnModule.Advanced.StationModule = StationModule;
            ReturnModule.Advanced.explodes = explodes;
            ReturnModule.Advanced.SensorRange = SensorRange;
            ReturnModule.Advanced.MechanicalBoardingDefense = MechanicalBoardingDefense;
            ReturnModule.Advanced.EMP_Protection = EMP_Protection;
            ReturnModule.Advanced.PowerRadius = PowerRadius;
            ReturnModule.Advanced.TechLevel = TechLevel;
            ReturnModule.Advanced.OrdnanceAddedPerSecond = OrdnanceAddedPerSecond;
            ReturnModule.Advanced.BombType = BombType;
            ReturnModule.Advanced.WarpMassCapacity = WarpMassCapacity;
            ReturnModule.Advanced.BonusRepairRate = BonusRepairRate;
            ReturnModule.Advanced.Cargo_Capacity = Cargo_Capacity;
            ReturnModule.Advanced.shield_radius = shield_radius;
            ReturnModule.Advanced.shield_power_max = shield_power_max;
            ReturnModule.Advanced.shield_recharge_rate = shield_recharge_rate;
            ReturnModule.Advanced.shield_recharge_combat_rate = shield_recharge_combat_rate;
            ReturnModule.Advanced.shield_recharge_delay = shield_recharge_delay;
            ReturnModule.Advanced.shield_threshold = shield_threshold;
            ReturnModule.Advanced.shield_kinetic_resist = shield_kinetic_resist;
            ReturnModule.Advanced.shield_energy_resist = shield_energy_resist;
            ReturnModule.Advanced.shield_explosive_resist = shield_explosive_resist;
            ReturnModule.Advanced.shield_missile_resist = shield_missile_resist;
            ReturnModule.Advanced.shield_flak_resist = shield_flak_resist;
            ReturnModule.Advanced.shield_hybrid_resist = shield_hybrid_resist;
            ReturnModule.Advanced.shield_railgun_resist = shield_railgun_resist;
            ReturnModule.Advanced.shield_subspace_resist = shield_subspace_resist;
            ReturnModule.Advanced.shield_warp_resist = shield_warp_resist;
            ReturnModule.Advanced.shield_beam_resist = shield_beam_resist;
            ReturnModule.Advanced.numberOfColonists = numberOfColonists;
            ReturnModule.Advanced.numberOfEquipment = numberOfEquipment;
            ReturnModule.Advanced.numberOfFood = numberOfFood;
            ReturnModule.Advanced.IsSupplyBay = IsSupplyBay;
            ReturnModule.Advanced.IsTroopBay = IsTroopBay;
            ReturnModule.Advanced.hangarTimerConstant = hangarTimerConstant;
            ReturnModule.Advanced.thrust = thrust;
            ReturnModule.Advanced.WarpThrust = WarpThrust;
            ReturnModule.Advanced.TurnThrust = TurnThrust;
            ReturnModule.Advanced.PowerFlowMax = PowerFlowMax;
            ReturnModule.Advanced.PowerDraw = PowerDraw;
            ReturnModule.Advanced.PowerDrawAtWarp = PowerDrawAtWarp;
            ReturnModule.Advanced.PowerStoreMax = PowerStoreMax;
            ReturnModule.Advanced.HealPerTurn = HealPerTurn;
            ReturnModule.Advanced.TroopCapacity = TroopCapacity;
            ReturnModule.Advanced.TroopsSupplied = TroopsSupplied;
            ReturnModule.Advanced.Cost = Cost;
            ReturnModule.Advanced.InhibitionRadius = InhibitionRadius;
            ReturnModule.Advanced.FTLSpoolTime = FTLSpoolTime;
            ReturnModule.Advanced.ECM = ECM;
            ReturnModule.Advanced.SensorBonus = SensorBonus;
            ReturnModule.Advanced.TransporterTimerConstant = TransporterTimerConstant;
            ReturnModule.Advanced.TransporterRange = TransporterRange;
            ReturnModule.Advanced.TransporterPower = TransporterPower;
            ReturnModule.Advanced.TransporterOrdnance = TransporterOrdnance;
            ReturnModule.Advanced.TransporterTroopLanding = TransporterTroopLanding;
            ReturnModule.Advanced.TransporterTroopAssault = TransporterTroopAssault;
            ReturnModule.Advanced.KineticResist = KineticResist;
            ReturnModule.Advanced.EnergyResist = EnergyResist;
            ReturnModule.Advanced.GuidedResist = GuidedResist;
            ReturnModule.Advanced.MissileResist = MissileResist;
            ReturnModule.Advanced.HybridResist = HybridResist;
            ReturnModule.Advanced.BeamResist = BeamResist;
            ReturnModule.Advanced.ExplosiveResist = ExplosiveResist;
            ReturnModule.Advanced.InterceptResist = InterceptResist;
            ReturnModule.Advanced.RailgunResist = RailgunResist;
            ReturnModule.Advanced.SpaceBombResist = SpaceBombResist;
            ReturnModule.Advanced.BombResist = BombResist;
            ReturnModule.Advanced.BioWeaponResist = BioWeaponResist;
            ReturnModule.Advanced.DroneResist = DroneResist;
            ReturnModule.Advanced.WarpResist = WarpResist;
            ReturnModule.Advanced.TorpedoResist = TorpedoResist;
            ReturnModule.Advanced.CannonResist = CannonResist;
            ReturnModule.Advanced.SubspaceResist = SubspaceResist;
            ReturnModule.Advanced.PDResist = PDResist;
            ReturnModule.Advanced.FlakResist = FlakResist;
            ReturnModule.Advanced.DamageThreshold = DamageThreshold;
            ReturnModule.Advanced.APResist = APResist;
            ReturnModule.Advanced.IndirectPower = IndirectPower;
            ReturnModule.Advanced.isPowerArmour = isPowerArmour;
            ReturnModule.Advanced.isBulkhead = isBulkhead;
            ReturnModule.Advanced.TargetTracking = TargetTracking;
            ReturnModule.Advanced.FixedTracking = FixedTracking;

            return ReturnModule;
        }
    }//class
}//namespace