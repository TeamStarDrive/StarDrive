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
