using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    /// <summary>
    /// Encapsulates all important ship stats separate from Ship itself.
    /// This is reused in multiple places such as:
    ///   Ship, ShipDesignScreen, ShipDesignIssues
    /// </summary>
    public class ShipStats
    {
        public float Cost { get; private set; }
        public float Mass { get; private set; }
        
        public float Thrust { get; private set; }
        public float WarpThrust { get; private set; }
        public float TurnThrust { get; private set; }

        public float VelocityMax { get; private set; }
        public float TurnRadsPerSec { get; private set; }

        public float MaxFTLSpeed { get; private set; }
        public float MaxSTLSpeed { get; private set; }

        public float FTLSpoolTime { get; private set; }

        public void Update(ShipModule[] modules, ShipData hull, Empire e, int level)
        {
            Cost = GetCost(GetBaseCost(modules), hull, e);
            Mass = GetMass(modules, e);
            Thrust = GetThrust(modules, hull);
            WarpThrust = GetWarpThrust(modules, hull);
            TurnThrust = GetTurnThrust(modules);
            VelocityMax = GetVelocityMax(Thrust, Mass);
            TurnRadsPerSec = GetTurnRadsPerSec(TurnThrust, Mass, level);
            MaxFTLSpeed = GetFTLSpeed(WarpThrust, Mass, e);
            MaxSTLSpeed = GetSTLSpeed(Thrust, Mass, e);
            FTLSpoolTime = GetFTLSpoolTime(modules, e);
        }
        
        public static float GetBaseCost(ShipModule[] modules)
        {
            float baseCost = 0f;
            for (int i = 0; i < modules.Length; i++)
                baseCost += modules[i].Cost;
            return baseCost;
        }

        public static float GetCost(float baseCost, ShipData hull, Empire e)
        {
            if (hull.HasFixedCost)
                return hull.FixedCost * CurrentGame.Pace;
            float cost = baseCost * CurrentGame.Pace;
            cost += hull.Bonuses.StartingCost;
            cost += cost * e.data.Traits.ShipCostMod;
            cost *= 1f - hull.Bonuses.CostBonus; // @todo Sort out (1f - CostBonus) weirdness
            return (int)cost;
        }

        public static float GetMass(ShipModule[] modules, Empire loyalty)
        {
            float mass = 0f;
            for (int i = 0; i < modules.Length; i++)
                mass += modules[i].GetActualMass(loyalty);
            mass *= loyalty.data.MassModifier; // apply overall mass modifier

            float surfaceArea = modules.Length;
            return Math.Max(1, Math.Max(surfaceArea*0.5f, mass));
        }

        public static float GetThrust(ShipModule[] modules, ShipData hull)
        {
            float thrust = 0f;
            for (int i = 0; i < modules.Length; i++)
                thrust += modules[i].thrust;
            return thrust * hull.Bonuses.SpeedModifier;
        }

        public static float GetWarpThrust(ShipModule[] modules, ShipData hull)
        {
            float warpThrust = 0f;
            for (int i = 0; i < modules.Length; i++)
                warpThrust += modules[i].WarpThrust;
            return warpThrust * hull.Bonuses.SpeedModifier;
        }

        public static float GetTurnThrust(ShipModule[] modules)
        {
            float turnThrust = 0f;
            for (int i = 0; i < modules.Length; i++)
                turnThrust += modules[i].TurnThrust;
            return turnThrust;
        }

        public static float GetTurnRadsPerSec(float turnThrust, float mass, int level)
        {
            float radsPerSec = turnThrust / mass / 700f;
            if (level > 0)
                radsPerSec += radsPerSec * level * 0.05f;
            return radsPerSec;
        }

        public static float GetVelocityMax(float thrust, float mass)
        {
            return thrust / mass;
        }

        public static float GetFTLSpeed(float warpThrust, float mass, Empire e)
        {
            return (warpThrust / mass) * e.data.FTLModifier;
        }

        public static float GetSTLSpeed(float thrust, float mass, Empire e)
        {
            float thrustWeightRatio = thrust / mass;
            float speed = thrustWeightRatio * e.data.SubLightModifier;
            return Math.Min(speed, Ship.LightSpeedConstant);
        }

        public static float GetFTLSpoolTime(ShipModule[] modules, Empire e)
        {
            float spoolTime = 0f;
            for (int i = 0; i < modules.Length; i++)
                spoolTime = Math.Max(spoolTime, modules[i].FTLSpoolTime);

            spoolTime *= e.data.SpoolTimeModifier;
            if (spoolTime <= 0f)
                spoolTime = 3f;
            return spoolTime;
        }
    }
}
