using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// the chance to recover a derelict ship, by its hull's role, raised or lowered by the recovering race's engineers and lowered by the share of it already stripped, the share of the failed recoveries that explode, and the damage a recovered ship comes back with
public static class DerelictRecovery
{
    // percent of the failed recoveries that explode, before the engineers
    public const float CatastropheChance = 10f;

    // a station can only be salvaged
    public static bool CanRecover(Derelict d) => !d.Design.IsStation;

    // percent, as reverse engineering's: a destroyer and any other hull take the middle one
    public static float HullChance(RoleName hullRole) => hullRole switch
    {
        RoleName.frigate    => 60f,
        RoleName.cruiser    => 40f,
        RoleName.battleship => 30f,
        RoleName.capital    => 20f,
        _                   => 50f,
    };

    // percent, from any thread
    public static float Chance(Empire empire, Derelict d)
    {
        float unstripped = d.Production > 0f ? d.ProductionLeft / d.Production : 0f;
        return (HullChance(d.Design.HullRole) * (1f + Engineers(empire)) * unstripped).Clamped(0f, 100f);
    }

    // percent of the empire's failed recoveries that explode
    public static float CatastropheChanceOf(Empire empire) => (CatastropheChance * (1f - Engineers(empire))).Clamped(0f, 100f);

    public enum Outcome { Recovered, BrokenApart, Exploded }

    // as the last of the work is done: the empire's first draw against the chance as it stands then, and for a failure its second for how bad it is; a draw under the percent passes, as a dice roll does
    public static Outcome Roll(Empire empire, Derelict d)
    {
        (float recovery, float catastrophe) = Draws(empire, d);
        if (recovery * 100f < Chance(empire, d))
            return Outcome.Recovered;
        return catastrophe * 100f < CatastropheChanceOf(empire) ? Outcome.Exploded : Outcome.BrokenApart;
    }

    // the empire's own two draws from 0 to 1 for the derelict, its luck keyed by the derelict's id: the same after every load, and each empire's its own
    public static (float Recovery, float Catastrophe) Draws(Empire empire, Derelict d) => (empire.Luck(d.Id, 0), empire.Luck(d.Id, 1));

    // its hull role's biggest death blast
    public static float BlastDamage(Derelict d) => d.Radius * Ship.ExplosionDamageCapPerRadius(d.Design.HullRole);

    // a platform works a recovery's slots at twice the salvage speed, which the salvage techs raise for both
    public const float SpeedPerSalvageSpeed = 2f;

    public static float Speed => GlobalStats.Defaults.SalvageSpeed * SpeedPerSalvageSpeed;

    // the turns a platform breaks into the derelict before the work starts, whatever its size and the game's pace
    public static int BreakInTurns => GlobalStats.Defaults.SalvageRecoveryBreakInTurns.LowerBound(0);

    // from any thread: the time it takes, nothing paid: the share of health the ship had lost at its death, times its slots at the game's pace, the work factor and 1 minus the engineers
    public static float Work(Empire empire, Derelict d)
        => ((1f - d.HealthAtDeath) * d.Design.SurfaceArea * empire.Universe.ProductionPace
            * GlobalStats.Defaults.SalvageRecoveryWorkFactor * (1f - Engineers(empire)).LowerBound(0f)).LowerBound(0f);

    // from any thread: the share of the work done, all of it when there is none
    public static float Progress(Empire empire, Derelict d, float workDone)
    {
        float work = Work(empire, d);
        return work > 0f ? (workDone / work).Clamped(0f, 1f) : 1f;
    }

    // whole turns left: those of the break-in, then the work's at the recovery speed, the last of which rolls; with no work left, the roll's own
    public static int TurnsLeft(Empire empire, Derelict d, int breakInDone, float workDone)
    {
        int breakIn = (BreakInTurns - breakInDone).LowerBound(0);
        float left = Work(empire, d) - workDone;
        float speed = Speed;
        if (left > 0f && speed <= 0f)
            return int.MaxValue;
        int work = left > 0f ? (int)Math.Ceiling(left / speed) : 1;
        return breakIn + work;
    }

    // a recovered ship's modules come back at these shares of their health: its armor inside the hull's edge, and every module neither dead nor armor, command modules too
    public const float InternalArmorHealth = 0.02f;
    public const float ModuleHealth = 0.04f;
    // the engines of a design with no repair at all, which come back at this share instead of dead, so it can still fly home
    public const float EnginesHealthWithoutRepair = 0.01f;

    // a module with a repair value other than a command module's, such as Main Engineering, a Construction Array or a repair drone bay
    public static bool IsRepairModule(ShipModule m) => m.IsRepairModule || m.BonusRepairRate > 0f && !m.IsCommandModule;

    // a design repairs when any of its modules has a repair value, a command module's too
    public static bool DesignRepairs(ShipModule[] modules)
    {
        for (int i = 0; i < modules.Length; ++i)
            if (modules[i].BonusRepairRate > 0f)
                return true;
        return false;
    }

    // the share of its health a recovered ship's module comes back at: its repair modules, engines and the armor on the hull's edge as designed dead, its other armor and every other module barely alive
    public static float HealthOnRecovery(ShipModule m, bool designRepairs)
    {
        if (m.Is(ShipModuleType.Engine))
            return designRepairs ? 0f : EnginesHealthWithoutRepair;
        if (IsRepairModule(m))
            return 0f;
        if (m.Is(ShipModuleType.Armor))
            return m.IsExternal ? 0f : InternalArmorHealth;
        return ModuleHealth;
    }

    // the nearest of the empire's planets that repairs ships, a safe one first, as a damaged ship resupplies at the nearest safe planet; none with no such planet
    public static Planet RepairPlanet(Empire empire, Vector2 position)
        => empire.SafePlanets.FindClosestTo(position, Repairs) ?? empire.UnsafePlanets.FindClosestTo(position, Repairs);

    static bool Repairs(Planet p) => p.TotalRepair > 0f;

    static float Engineers(Empire empire) => empire.RaceEngineers;

    // the module health bonus of the race traits named, Skilled or Haphazard Engineers, without what research, artifacts or a difficulty add
    public static float EngineersOf(string selectedTraits)
    {
        Array<RacialTraitOption> traits = ResourceManager.RaceTraits?.TraitList;
        if (selectedTraits.IsEmpty() || traits == null)
            return 0f;

        string[] names = selectedTraits.Split(", ");
        float engineers = 0f;
        for (int i = 0; i < traits.Count; ++i)
            if (Array.IndexOf(names, traits[i].TraitName) >= 0)
                engineers += traits[i].ModHpModifier;
        return engineers;
    }
}
