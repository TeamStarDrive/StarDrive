using Microsoft.Xna.Framework;
using Particle3DSample;
using System;
using System.Collections.Generic;

namespace Ship_Game.Gameplay
{
    //These simple classes are the member Variable holding sub_classes used to hold the less used variables
    //for the game objects that are extremely numerous. They are essentially just groups of member variables
    //that will allow a parent object to have all of these variables available to them without having them
    //all allocated in memory unless they are needed.      -Gretman

    public sealed class ShipModule : ShipModule_Slim
    {
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
        public bool IsSupplyBay;
        public bool IsTroopBay;
        public float hangarTimerConstant = 30f;
        public float thrust;
        public int WarpThrust;
        public int TurnThrust;
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
        public float TransporterTimer = 0f;
        public float TransporterRange;
        public float TransporterPower;
        public float TransporterOrdnance;
        public byte TransporterTroopLanding;
        public byte TransporterTroopAssault;
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
    }
}