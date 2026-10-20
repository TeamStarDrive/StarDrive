using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;

namespace Ship_Game.Ships;

// the chance to recover a derelict ship, by its hull's role, raised or lowered by the recovering race's engineers and lowered by the share of it already stripped, and the share of the failed recoveries that explode
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

    // whole turns left: those of the break-in, then the work's at the recovery speed
    public static int TurnsLeft(Empire empire, Derelict d, int breakInDone, float workDone)
    {
        int breakIn = (BreakInTurns - breakInDone).LowerBound(0);
        float left = Work(empire, d) - workDone;
        float speed = Speed;
        return left <= 0f ? breakIn : speed > 0f ? breakIn + (int)Math.Ceiling(left / speed) : int.MaxValue;
    }

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
