using Ship_Game.AI;
using Ship_Game.Gameplay;
using Point = SDGraphics.Point;

namespace Ship_Game.Ships;

// The strength formula shared by ship designs and ships
public static class ShipStrength
{
    public static int OffensiveArea(ShipModule m)
        => m.InstalledWeapon != null || m.IsTroopBay || m.IsSupplyBay || m.MaximumHangarShipSize > 0 ? m.Area : 0;

    // support ships with no weapons or bays have no strength
    public static bool IsUnarmedSupport(IShipDesign design, Empire loyalty)
    {
        string name = design.Name;
        bool miningShip = loyalty.data.DefaultMiningShip == name || Empire.DefaultMiningShipName == name;
        bool assaultShuttle = loyalty.data.DefaultAssaultShuttle == name || Empire.DefaultBoardingShuttleName == name;
        bool defaultTroopShip = !assaultShuttle && (loyalty.data.DefaultTroopShip == name || design.Role == RoleName.troop);
        return defaultTroopShip || design.IsSupplyShuttle || design.Role == RoleName.scout
            || design.IsSubspaceProjector || design.IsFreighter || design.IsConstructor || miningShip;
    }

    public static float Combine(int surfaceArea, int offensiveArea, float offense, float defense)
        => ShipBuilder.GetModifiedStrength(surfaceArea, offensiveArea, offense, defense);

    // the strength of a new ship of this design built by the empire; with no empire, without empire bonuses
    public static float OfDesign(IShipDesign design, DesignSlot[] slots, Empire empire)
    {
        var modules = new ShipModule[slots.Length];
        for (int i = 0; i < slots.Length; ++i)
        {
            if (ResourceManager.GetModuleTemplate(slots[i].ModuleUID, out ShipModule template))
                modules[i] = template.ModuleType == ShipModuleType.Hangar ? HangarModule(template, slots[i], empire ?? Empire.Void) : template;
        }
        return OfModules(design, modules, empire, workingOnly: false);
    }

    // a ship's strength from its modules, or from its working ones only
    public static float OfModules(IShipDesign design, ShipModule[] modules, Empire empire, bool workingOnly)
    {
        EmpireHullBonuses bonuses = empire != null ? EmpireHullBonuses.Get(empire) : EmpireHullBonuses.Default;
        int surfaceArea = design.SurfaceArea;

        int mainShields = 0;
        float amplification = 0f;
        for (int i = 0; i < modules.Length; ++i)
        {
            ShipModule m = modules[i];
            if (m == null)
                continue;
            if (IsMainShield(m))
                ++mainShields;
            if (m.AmplifyShields > 0f && (!workingOnly || m.Active))
                amplification += m.AmplifyShields;
        }
        float amplifyPerShield = mainShields > 0 ? amplification / mainShields : 0f;

        float offense = 0f;
        float defense = 0f;
        int offensiveArea = 0;
        for (int i = 0; i < modules.Length; ++i)
        {
            ShipModule m = modules[i];
            if (m == null || workingOnly && !m.Active)
                continue;

            float shieldsMax = m.ShieldPowerMax * bonuses.ShieldMod;
            if (IsMainShield(m))
                shieldsMax += amplifyPerShield;

            offense += m.CalculateModuleOffense(empire);
            defense += m.CalculateModuleDefense(surfaceArea, bonuses, shieldsMax);
            offensiveArea += OffensiveArea(m);
        }

        if (empire != null && offensiveArea == 0 && IsUnarmedSupport(design, empire))
            return 0f;
        return Combine(surfaceArea, offensiveArea, offense, defense);
    }

    // the shields which amplifiers boost, as in ShipStats.UpdateShieldAmplification
    static bool IsMainShield(ShipModule m) => m.ShieldPowerMax > 0f && m.ModuleType == ShipModuleType.Shield;

    // a hangar set up from its design slot, as ShipModule.Create does
    static ShipModule HangarModule(ShipModule template, DesignSlot slot, Empire loyalty)
    {
        ShipModule m = ShipModule.CreateNoParent(null, template, loyalty);
        if (!m.IsTroopBay && !m.IsMiningBay)
            m.HangarShipUID = slot.HangarShipUID;
        m.InstallModule(null, null, null, Point.Zero);
        if (!m.IsSupplyBay && !m.IsTroopBay)
            m.SetHangarLaunch(loyalty);
        return m;
    }
}
