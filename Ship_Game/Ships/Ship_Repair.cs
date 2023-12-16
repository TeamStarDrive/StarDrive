using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Ship_Game.Ships;

public partial class Ship
{
    public float RepairRate = 1f;
    public bool HasRegeneratingModules; // module.Regenerate > 0
    public bool HasRepairModule; // module.IsRepairModule

    public float HealPerTurn; // Troops Healing

    public Array<ShipModule> RepairBeams;
    public bool HasRepairBeam => RepairBeams != null && RepairBeams.NotEmpty;

    public GameObject LastDamagedBy { get; private set; }
    float LastDamagedTime;
    
    public float CurrentRepairPerSecond { get; private set; }

    /// <summary>
    /// Total seconds elapsed since we were last damaged by another GameObject
    /// such as a Projectile
    /// </summary>
    public float TimeSinceLastDamage => GameBase.Base.TotalElapsed - LastDamagedTime;
    
    /// <summary>
    /// If CombatRepair is turned off, then ships in conflict cannot repair themselves
    /// </summary>
    public bool CanRepair => GlobalStats.Defaults.UseCombatRepair || !IsInRecentCombat;

    const float RecentlyDamagedSeconds = 5f;

    /// <summary>
    /// Not the same as InCombat, it checks whether this ship has been in conflict,
    /// which means it might be InCombat, or it might have been recently damaged by an enemy
    /// </summary>
    public bool IsInRecentCombat => InCombat || TimeSinceLastDamage < RecentlyDamagedSeconds;

    void Repair(FixedSimTime timeSinceLastUpdate)
    {
        if (CanRepair)
        {
            int repairLevel = Level;
            float repair = RepairRate;

            // sometimes ships Priority escape from combat, turning InCombat=false
            // even if other ships are attacking us, so we check if we were recently damaged
            // and in that case, the repair penalty still applies
            if (IsInRecentCombat) // reduces repair rate while in combat
            {
                repair *= GlobalStats.Defaults.InCombatSelfRepairModifier;
            }

            float planetRepair = 0f;
            Planet p = GetTether()
                       ?? (AI.IsInOrbit ? AI.OrbitTarget : null);
            if (p != null)
            {
                planetRepair = p.GeodeticManager.RepairRatePerSecond;
                repairLevel = Math.Max(repairLevel, p.Level + p.NumShipyards);

                float empRemovalRate = -planetRepair * GlobalStats.Defaults.BonusColonyEMPRecovery;
                CauseEmpDamage(empRemovalRate); // Reduce EMP damage status from planet repair
            }

            float totalRepair = repair + planetRepair;
            Loyalty.AddExoticConsumption(ExoticBonusType.RepairRate, totalRepair);
            float exoticBonusMultiplier = Loyalty.GetDynamicExoticBonusMuliplier(ExoticBonusType.RepairRate);
            ApplyAllRepair(totalRepair * exoticBonusMultiplier, repairInterval: timeSinceLastUpdate.FixedTime, repairLevel);

            if (AI.State == AIState.Flee && HealthPercent > ShipResupply.DamageThreshold(ShipData.ShipCategory))
                AI.OrderAwaitOrders(); // Stop fleeing and get back into combat if needed
        }

        if (!EMPDisabled)
            PerformRegeneration();
    }

    void PerformRegeneration()
    {
        if (!HasRegeneratingModules)
            return;

        for (int i = 0; i < ModuleSlotList.Length; i++)
        {
            ShipModule module = ModuleSlotList[i];
            module.RegenerateSelf();
        }
    }
    
    /// <summary>
    /// Sets the latest damage causer for this Ship
    /// </summary>
    public void SetLastDamagedBy(GameObject damagedBy)
    {
        LastDamagedBy = damagedBy;
        LastDamagedTime = GameBase.Base.TotalElapsed;
    }

    /// <param name="repairAmount">How many HP-s to repair</param>
    /// <param name="repairInterval">This repair event interval in seconds, important for correct UI estimation</param>
    /// <param name="repairLevel">Level which improves repair decisions</param>
    public void ApplyAllRepair(float repairAmount, float repairInterval, int repairLevel)
    {
        if (HealthPercent >= 1)
        {
            CurrentRepairPerSecond = 0;
            return;
        }

        CurrentRepairPerSecond = repairAmount / repairInterval;
        int damagedModules = ModuleSlotList.Count(module => !module.Health.AlmostEqual(module.ActualMaxHealth));
        if (damagedModules == 0)
            Health = HealthMax; // Align small diffs.

        for (int i = 0; repairAmount > 0 && i < damagedModules; ++i)
        {
            ShipModule moduleToRepair = GetModuleToRepair(repairLevel);
            repairAmount = moduleToRepair.Repair(repairAmount);
        }
    }

    public ShipModule GetModuleToRepair(int repairLevel)
    {
        // Critical module percent allows skilled crews to get modules barely functional
        // before moving on to repairing next critical modules.
        // Above skill 5 there is no more benefit.
        // The default value is 50%, and lowest threshold is 5%
        float criticalModulePercent = (0.5f - (repairLevel * 0.1f)).Clamped(0.05f, 0.5f);
        return ModuleSlotList.FindMax(module => module.GetRepairPriority(criticalModulePercent));
    }

    public Status HealthStatus
    {
        get
        {
            if (engineState == MoveState.Warp
                || AI.State == AIState.Refit
                || AI.State == AIState.Resupply)
            {
                return Status.NotApplicable;
            }

            Health = Health.Clamped(0, HealthMax);
            return ToShipStatus(Health, HealthMax);
        }
    }
}
