using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Linq;

namespace Ship_Game.ShipDesignIssues
{
    public class ShipDesignIssues
    {
        public readonly ShipData Hull;
        public readonly ShipData.RoleName Role;
        public Array<DesignIssueDetails> CurrentDesignIssues { get; }
        public WarningLevel CurrentWarningLevel { get; private set; }
        private readonly Empire Player;
        private EmpireShipDesignStats EmpireStats;

        public ShipDesignIssues(ShipData hull)
        {
            Hull   = hull;
            Role   = hull.Role;
            Player = EmpireManager.Player;
            CurrentDesignIssues = new Array<DesignIssueDetails>();
            EmpireStats         = new EmpireShipDesignStats(Player);
        }

        void AddDesignIssue(DesignIssueType type, WarningLevel severity, string addRemediationText = "")
        {
            DesignIssueDetails details = new DesignIssueDetails(type, severity, addRemediationText);
            CurrentDesignIssues.Add(details);
            UpdateCurrentWarningLevel(details.Severity);
        }

        void UpdateCurrentWarningLevel(WarningLevel level)
        {
            if (level > CurrentWarningLevel)
                CurrentWarningLevel = level;
        }

        public struct EmpireShipDesignStats
        {
            private float AverageWarpSpeedCivilian;
            private float AverageWarpSpeedMilitary;

            public EmpireShipDesignStats(Empire empire)
            {
                AverageWarpSpeedCivilian = 0;
                AverageWarpSpeedMilitary = 0;
                RecalculateEmpireStats(empire);
            }

            public void RecalculateEmpireStats(Empire empire)
            {
                float totalWarpSpeedCivilian = 0;
                float totalWarpSpeedMilitary = 0;
                int totalWarpShipsCivilian   = 0;
                int totalWarpShipsMilitary   = 0;

                foreach (string name in empire.ShipsWeCanBuild)
                {
                    Ship ship = ResourceManager.GetShipTemplate(name);
                    float warpSpeed = ShipStats.GetFTLSpeed(ship.WarpThrust, ship.Mass, empire);
                    if (warpSpeed < 2000 || Scout(ship.shipData.Role))
                        continue;

                    if (Civilian(ship.shipData.Role))
                    {
                        totalWarpShipsCivilian += 1;
                        totalWarpSpeedCivilian += warpSpeed;
                    }
                    else
                    {
                        totalWarpShipsMilitary += 1;
                        totalWarpSpeedMilitary += warpSpeed;
                    }
                }

                AverageWarpSpeedCivilian = (totalWarpSpeedCivilian / totalWarpShipsCivilian).RoundTo10();
                AverageWarpSpeedMilitary = (totalWarpSpeedMilitary / totalWarpShipsMilitary).RoundTo10();
            }

            public float AverageEmpireWarpSpeed(ShipData.RoleName role) => Civilian(role) ? AverageWarpSpeedCivilian
                                                                                          : AverageWarpSpeedMilitary;
        }

        bool IsPlatform => Hull.HullRole == ShipData.RoleName.platform;
        bool Stationary => Hull.HullRole == ShipData.RoleName.station || Hull.HullRole == ShipData.RoleName.platform;
        bool LargeCraft => Hull.HullRole == ShipData.RoleName.freighter || Hull.HullRole == ShipData.RoleName.destroyer
                                                                        || Hull.HullRole == ShipData.RoleName.cruiser 
                                                                        || Hull.HullRole == ShipData.RoleName.capital;

        public static bool Civilian(ShipData.RoleName role) => role == ShipData.RoleName.colony 
                                                               || role == ShipData.RoleName.freighter 
                                                               || role == ShipData.RoleName.construction;

        public static bool Scout(ShipData.RoleName role) => role == ShipData.RoleName.scout;

        public void Reset()
        {
            CurrentDesignIssues.Clear();
            CurrentWarningLevel = WarningLevel.None;
        }

        public void CheckIssueNoCommand(int numCommand)
        {
            if (Role != ShipData.RoleName.platform && numCommand == 0)
                AddDesignIssue(DesignIssueType.NoCommand, WarningLevel.Critical);
        }

        public void CheckIssueBackupCommand(int numCommand, int size)
        {
            if (Role != ShipData.RoleName.platform && numCommand == 1 && size >= 500)
                AddDesignIssue(DesignIssueType.BackUpCommand, WarningLevel.Major);
        }

        public void  CheckIssueUnpoweredModules(bool unpoweredModules)
        {
            if (unpoweredModules)
                AddDesignIssue(DesignIssueType.UnpoweredModules, WarningLevel.Major);
        }

        public void CheckIssueOrdnanceBurst(float burst, float cap)
        {
            if (burst.LessOrEqual(0) || burst < cap)
                return;

            WarningLevel level;
            float efficiency = cap / burst;
            if (efficiency > 0.75f)     level = WarningLevel.Minor;
            else if (efficiency > 0.5f) level = WarningLevel.Major;
            else                        level = WarningLevel.Critical;

            AddDesignIssue(DesignIssueType.HighBurstOrdnance, level, $" {(efficiency*100).String(0)}%");
        }

        public void CheckIssueOrdnance(float ordnanceUsed, float ordnanceRecovered, float ammoTime)
        {
            if ((ordnanceUsed - ordnanceRecovered).LessOrEqual(0))
                return;  // Inf ammo

            if (ammoTime < 5)
            {
                AddDesignIssue(DesignIssueType.NoOrdnance, WarningLevel.Critical);
            }
            else if (!IsPlatform)
            {
                int goodAmmoTime = LargeCraft ? 50 : 25;
                if (ammoTime < goodAmmoTime)
                    AddDesignIssue(DesignIssueType.LowOrdnance, WarningLevel.Minor);
            }
        }

        public void CheckIssuePowerRecharge(bool hasEnergyWeapons, float recharge, float powerCapacity, float excessPowerConsumed)
        {
            if (recharge.Less(0))
            { 
                AddDesignIssue(DesignIssueType.NegativeRecharge, WarningLevel.Critical);
                return;
            }

            if (!hasEnergyWeapons || excessPowerConsumed < recharge)
                return;

            float rechargeTime    = powerCapacity / recharge.LowerBound(1);
            WarningLevel severity = WarningLevel.None;

            if (rechargeTime > 20)      severity = WarningLevel.Critical;
            else if (rechargeTime > 16) severity = WarningLevel.Major;
            else if (rechargeTime > 12) severity = WarningLevel.Minor;
            else if (rechargeTime > 8)  severity = WarningLevel.Informative;

            if (severity > WarningLevel.None)
                AddDesignIssue(DesignIssueType.LongRechargeTime, severity);
        }

        public void CheckIssueLowWarpTime(float warpDraw, float ftlTime, float warpSpeed)
        {
            if (Stationary || warpSpeed.AlmostZero() || warpDraw.GreaterOrEqual(0) || ftlTime > 900)
                return;

            WarningLevel severity = ftlTime < 60 ? WarningLevel.Critical : WarningLevel.Major;
            AddDesignIssue(DesignIssueType.LowWarpTime, severity);
        }

        public void CheckIssueNoWarp(float speed, float warpSpeed)
        {
            if (Stationary || speed.AlmostZero())
                return;

            if (warpSpeed.LessOrEqual(0))
            {
                WarningLevel severity = LargeCraft ? WarningLevel.Critical : WarningLevel.Informative;
                AddDesignIssue(DesignIssueType.NoWarp, severity);
            }
        }

        public void CheckIssueSlowWarp(float warpSpeed)
        {
            if (Stationary || warpSpeed.AlmostZero())
                return;

            float averageWarpSpeed = EmpireStats.AverageEmpireWarpSpeed(Hull.Role);
            if (warpSpeed.GreaterOrEqual(averageWarpSpeed * 0.9f))
                return;

            WarningLevel severity = WarningLevel.Informative;
            if      (warpSpeed.Less(averageWarpSpeed / 2)) severity = WarningLevel.Major;
            else if (warpSpeed.Less(averageWarpSpeed / 2)) severity = WarningLevel.Minor;


            string civilianOrMilitary = Civilian(Role) ? new LocalizedText(2556).Text
                                                       : new LocalizedText(2557).Text;

            float averageEmpireWarpSpeed = EmpireStats.AverageEmpireWarpSpeed(Role);
            string averageWarpString = $" {civilianOrMilitary} ({averageEmpireWarpSpeed.GetNumberString()}).";
            AddDesignIssue(DesignIssueType.SlowWarp, severity, averageWarpString);
        }

        public void CheckIssueNoSpeed(float speed)
        {
            if (speed.Greater(0) || Stationary)
                return;

            AddDesignIssue(DesignIssueType.NoSpeed, WarningLevel.Critical);
        }

        public void CheckTargetExclusions(bool hasWeapons, bool canTargetFighters, bool  canTargetCorvettes, bool canTargetCapitals)
        {
            if (!hasWeapons)
                return;

            WarningLevel severity = LargeCraft ? WarningLevel.Major : WarningLevel.Critical;
            if (!canTargetFighters)
                AddDesignIssue(DesignIssueType.CantTargetFighters, severity);

            if (!canTargetCorvettes)
                AddDesignIssue(DesignIssueType.CantTargetCorvettes, severity);

            severity = LargeCraft ? WarningLevel.Critical : WarningLevel.Minor;
            if (!canTargetCapitals)
                AddDesignIssue(DesignIssueType.CantTargetCapitals, severity);
        }

        public void CheckTruePD(int size, int pointDefenseValue)
        {
            int threshold = (size / 60);
            if (size < 500 || pointDefenseValue > threshold)
                return;

            WarningLevel severity = pointDefenseValue < threshold / 2 ? WarningLevel.Major
                                                                      : WarningLevel.Minor;

            AddDesignIssue(DesignIssueType.LowPdValue, severity);
        }

        public void CheckWeaponPowerTime(bool hasEnergyWeapons, bool excessPowerConsumed, float weaponPowerTime)
        {
            if (!hasEnergyWeapons || !excessPowerConsumed)
                return;

            WarningLevel severity = WarningLevel.None;
            if      (weaponPowerTime < 2)  severity = WarningLevel.Critical;
            else if (weaponPowerTime < 4)  severity = WarningLevel.Major;
            else if (weaponPowerTime < 8) severity = WarningLevel.Minor;
            else if (weaponPowerTime < 16) severity = WarningLevel.Informative;

            if (severity > WarningLevel.None)
                AddDesignIssue(DesignIssueType.LowWeaponPowerTime, severity);
        }

        public void CheckCombatEfficiency(float excessPowerConsumed, float weaponPowerTime, float netPowerRecharge, 
                                          int numWeapons, int numOrdnanceWeapons)
        {
            if (numWeapons == 0 || numOrdnanceWeapons == numWeapons || excessPowerConsumed.Less(0))
                return;

            WarningLevel severity      = WarningLevel.None;
            float energyWeaponsRatio   = (float)(numWeapons - numOrdnanceWeapons) / numWeapons;
            float efficiencyReduction  = (1 - netPowerRecharge / (excessPowerConsumed + netPowerRecharge));
            efficiencyReduction       *= energyWeaponsRatio;
            float netEfficiency        = (1 - efficiencyReduction) * 100;

            if      (netEfficiency < 25) severity = WarningLevel.Critical;
            else if (netEfficiency < 50) severity = WarningLevel.Major;
            else if (netEfficiency < 75) severity = WarningLevel.Minor;
            else if (netEfficiency < 95) severity = WarningLevel.Informative;

            // Modify level by weapon power time if there is an issue
            if (severity > WarningLevel.None) 
            {
                if      (weaponPowerTime > 120) severity -= 3;
                else if (weaponPowerTime > 60)  severity -= 2;
                else if (weaponPowerTime > 30)  severity -= 1;

                if (severity < WarningLevel.Informative)
                    severity = WarningLevel.Informative;
            }

            if (severity > WarningLevel.None)
            {
                string efficiencyText = $" {new LocalizedText(2565).Text} {netEfficiency.String(0)}%.";
                AddDesignIssue(DesignIssueType.NotIdealCombatEfficiency, severity, efficiencyText);
            }
        }

        public void CheckBurstPowerTime(bool hasBeamWeapons, float burstEnergyPowerTime)
        {
            if (!hasBeamWeapons || burstEnergyPowerTime.GreaterOrEqual(2) || burstEnergyPowerTime.Less(0))
                return;

            WarningLevel severity = burstEnergyPowerTime < 1 ? WarningLevel.Critical : WarningLevel.Major;
            AddDesignIssue(DesignIssueType.LowBurstPowerTime, severity);
        }

        public void CheckOrdnanceVsEnergyWeapons(int numWeapons, int numOrdnanceWeapons)
        {
            if (Stationary || numWeapons == 0 || numOrdnanceWeapons == 0)
                return;

            if (numOrdnanceWeapons < numWeapons)
                AddDesignIssue(DesignIssueType.NoOrdnanceResupplyPlayerOrder, WarningLevel.Informative);

            float ordnanceToEnergyRatio = (float)numOrdnanceWeapons / numWeapons;
            if (ordnanceToEnergyRatio.LessOrEqual(ShipResupply.KineticToEnergyRatio))
                AddDesignIssue(DesignIssueType.NoOrdnanceResupplyCombat, WarningLevel.Informative);
        }

        public void CheckTroopsVsBays(int numTroops, int numTroopBays)
        {
            if (numTroops >= numTroopBays)
                return;

            int diff             = numTroopBays - numTroops;
            string troopsMissing = $" {diff} {new LocalizedText(2564).Text}";
            AddDesignIssue(DesignIssueType.LowTroopsForBays, WarningLevel.Major, troopsMissing);
        }

        public void CheckTroops(int numTroops, int size)
        {
            if (size < 500)
                return;

            int expectedTroops    = (size / 500).RoundUpToMultipleOf(1);
            int diff              = expectedTroops - numTroops;
            WarningLevel severity;

            if      (diff >= 3) severity = WarningLevel.Major;
            else if (diff == 2) severity = WarningLevel.Minor;
            else if (diff == 1) severity = WarningLevel.Informative;
            else                return;

            string troopsMissing = $" {diff} {new LocalizedText(2564).Text}";
            AddDesignIssue(DesignIssueType.LowTroops, severity, troopsMissing);
        }

        public void CheckAccuracy(Map<ShipModule,float> accuracyList)
        {
            if (accuracyList.Count == 0)
                return;

            var average = accuracyList.Average(kv=> kv.Value);
            WarningLevel severity;
            if      (average > 7)    severity = WarningLevel.Critical;
            else if (average > 5)    severity = WarningLevel.Major;
            else if (average > 3.5f) severity = WarningLevel.Minor;
            else if (average > 2.5f) severity = WarningLevel.Informative;
            else                     return;

            string remediation = $" {new LocalizedText(GameText.Average).Text}" +
                                 $" {new LocalizedText(GameText.Accuracy).Text}:" +
                                 $" {Math.Round(average, 1)}";

            AddDesignIssue(DesignIssueType.Accuracy, severity, remediation);
        }

        public void CheckTargets(Map<ShipModule, float> accuracyList, float maxTargets)
        {
            if (accuracyList.Count == 0 || maxTargets <1)
                return;

            var facings = accuracyList.GroupBy(kv=>
            {
                int facing = (int)kv.Key.FacingDegrees;
                facing     = facing == 360 ? 0 : facing;
                return (float)facing;
            });
            float count          = facings.Count();
            float ratioToTargets = count / maxTargets;

            WarningLevel severity;
            if      (ratioToTargets > 4) severity = WarningLevel.Critical;
            else if (ratioToTargets > 3) severity = WarningLevel.Major;
            else if (ratioToTargets > 2) severity = WarningLevel.Minor;
            else if (ratioToTargets > 1) severity = WarningLevel.Informative;
            else return;

            string target     = $" {new LocalizedText(GameText.FcsPower).Text}: {maxTargets.String(1)}, ";
            string fireArcs   = $"{new LocalizedText(GameText.FireArc).Text}: {count.String(1)} ";
            string baseString = !IsPlatform ? "" : $" {new LocalizedText(GameText.OrbitalTracking).Text}";

            AddDesignIssue(DesignIssueType.Targets, severity, baseString + target + fireArcs);
        }

        public Color CurrentWarningColor => IssueColor(CurrentWarningLevel);

        public static Color IssueColor(WarningLevel severity)
        {
            switch (severity)
            {
                default:
                case WarningLevel.None:        return Color.DarkGray;
                case WarningLevel.Informative: return Color.Green;
                case WarningLevel.Minor:       return Color.Yellow;
                case WarningLevel.Major:       return Color.Orange;
                case WarningLevel.Critical:    return Color.Red;
            }
        }
    }

    public enum DesignIssueType
    {
        NoCommand,
        BackUpCommand,
        UnpoweredModules,
        NoOrdnance,
        LowOrdnance,
        LowWarpTime,
        NoWarp,
        SlowWarp,
        NegativeRecharge,
        NoSpeed,
        CantTargetFighters,
        CantTargetCorvettes,
        CantTargetCapitals,
        LowPdValue,
        LowWeaponPowerTime,
        LowBurstPowerTime,
        NoOrdnanceResupplyCombat,
        NoOrdnanceResupplyPlayerOrder,
        LowTroops,
        LowTroopsForBays,
        NotIdealCombatEfficiency,
        HighBurstOrdnance,
        Accuracy,
        Targets,
        LongRechargeTime
    }

    public enum WarningLevel
    {
        None,
        Informative,
        Minor,
        Major,
        Critical
    }

    public struct DesignIssueDetails
    {
        public readonly DesignIssueType Type;
        public readonly WarningLevel Severity;
        public readonly Color Color;
        public readonly string Title;
        public readonly string Problem;
        public readonly string Remediation;
        public readonly SubTexture Texture;

        public DesignIssueDetails(DesignIssueType issueType, WarningLevel severity, string addToRemediationText)
        {
            Type     = issueType;
            Severity = severity;
            Color    = ShipDesignIssues.IssueColor(severity);
            switch (issueType)
            {
                default:
                case DesignIssueType.NoCommand:
                    Title       = new LocalizedText(2501).Text;
                    Problem     = new LocalizedText(2502).Text;
                    Remediation = new LocalizedText(2503).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.BackUpCommand:
                    Title       = new LocalizedText(2504).Text;
                    Problem     = new LocalizedText(2505).Text;
                    Remediation = new LocalizedText(2506).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueBackupCommand");
                    break;
                case DesignIssueType.UnpoweredModules:
                    Title       = new LocalizedText(2507).Text;
                    Problem     = new LocalizedText(2508).Text;
                    Remediation = new LocalizedText(2509).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueUnpowered");
                    break;
                case DesignIssueType.NoOrdnance:
                    Title       = new LocalizedText(2510).Text;
                    Problem     = new LocalizedText(2511).Text;
                    Remediation = new LocalizedText(2512).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoOrdnance");
                    break;
                case DesignIssueType.LowOrdnance:
                    Title       = new LocalizedText(2513).Text;
                    Problem     = new LocalizedText(2514).Text;
                    Remediation = new LocalizedText(2515).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowOrdnance");
                    break;
                case DesignIssueType.LowWarpTime:
                    Title       = new LocalizedText(2516).Text;
                    Problem     = new LocalizedText(2517).Text;
                    Remediation = new LocalizedText(2518).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowWarpTime");
                    break;
                case DesignIssueType.NoWarp:
                    Title       = new LocalizedText(2522).Text;
                    Problem     = new LocalizedText(2523).Text;
                    Remediation = new LocalizedText(2524).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoWarp");
                    break;
                case DesignIssueType.SlowWarp:
                    Title       = new LocalizedText(2525).Text;
                    Problem     = new LocalizedText(2526).Text;
                    Remediation = new LocalizedText(2527).Text; 
                    Texture     = ResourceManager.Texture("NewUI/IssueSlowWarp");
                    break;
                case DesignIssueType.NegativeRecharge:
                    Title       = new LocalizedText(2519).Text;
                    Problem     = new LocalizedText(2520).Text;
                    Remediation = new LocalizedText(2521).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueNegativeRecharge");
                    break;
                case DesignIssueType.NoSpeed:
                    Title       = new LocalizedText(2528).Text;
                    Problem     = new LocalizedText(2529).Text;
                    Remediation = new LocalizedText(2530).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoSublight");
                    break;
                case DesignIssueType.CantTargetFighters:
                    Title       = new LocalizedText(2531).Text;
                    Problem     = new LocalizedText(2532).Text;
                    Remediation = new LocalizedText(2533).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueCantTargetFighters");
                    break;
                case DesignIssueType.CantTargetCorvettes:
                    Title       = new LocalizedText(2534).Text;
                    Problem     = new LocalizedText(2535).Text;
                    Remediation = new LocalizedText(2536).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueCantTargetCorvettes");
                    break;
                case DesignIssueType.CantTargetCapitals:
                    Title       = new LocalizedText(2537).Text;
                    Problem     = new LocalizedText(2538).Text;
                    Remediation = new LocalizedText(2539).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueCantTargetCapitals");
                    break;
                case DesignIssueType.LowPdValue:
                    Title       = new LocalizedText(2540).Text;
                    Problem     = new LocalizedText(2541).Text;
                    Remediation = new LocalizedText(2542).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowPD");
                    break;
                case DesignIssueType.LowWeaponPowerTime:
                    Title       = new LocalizedText(2543).Text;
                    Problem     = new LocalizedText(2544).Text;
                    Remediation = new LocalizedText(2545).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowEnergyWeaponTime");
                    break;
                case DesignIssueType.LowBurstPowerTime:
                    Title       = new LocalizedText(2547).Text;
                    Problem     = new LocalizedText(2548).Text;
                    Remediation = new LocalizedText(2549).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowEnergyBurstTime");
                    break;
                case DesignIssueType.NoOrdnanceResupplyCombat:
                    Title       = new LocalizedText(2550).Text;
                    Problem     = new LocalizedText(2551).Text;
                    Remediation = new LocalizedText(2552).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoAmmoResupplyCombat");
                    break;
                case DesignIssueType.NoOrdnanceResupplyPlayerOrder:
                    Title       = new LocalizedText(2553).Text;
                    Problem     = new LocalizedText(2554).Text;
                    Remediation = new LocalizedText(2555).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoAmmoResupplyPlayer");
                    break;
                case DesignIssueType.LowTroopsForBays:
                    Title       = new LocalizedText(2558).Text;
                    Problem     = new LocalizedText(2559).Text;
                    Remediation = new LocalizedText(2560).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowTroopsForBays");
                    break;
                case DesignIssueType.LowTroops:
                    Title       = new LocalizedText(2561).Text;
                    Problem     = new LocalizedText(2562).Text;
                    Remediation = new LocalizedText(2563).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowTroops");
                    break;
                case DesignIssueType.NotIdealCombatEfficiency:
                    Title       = new LocalizedText(2566).Text;
                    Problem     = new LocalizedText(2567).Text;
                    Remediation = new LocalizedText(2568).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowWeaponPowerEfficiency");
                    break;
                case DesignIssueType.HighBurstOrdnance:
                    Title       = new LocalizedText(2569).Text;
                    Problem     = new LocalizedText(2570).Text;
                    Remediation = new LocalizedText(2571).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueHighBurstOrdnance");
                    break;
                case DesignIssueType.Accuracy:
                    Title       = new LocalizedText(GameText.LowAccuracy).Text;
                    Problem     = new LocalizedText(GameText.WeaponAccuracy).Text;
                    Remediation = new LocalizedText(GameText.ImproveAccuracy).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssuesLowAccuracy");
                    break;
                case DesignIssueType.Targets:
                    Title       = new LocalizedText(GameText.LowTracking).Text;
                    Problem     = new LocalizedText(GameText.TrackingTargets).Text;
                    Remediation = new LocalizedText(GameText.ImproveTracking).Text;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowTracking");
                    break;
                case DesignIssueType.LongRechargeTime:
                    Title       = new LocalizedText(1462).Text;
                    Problem     = new LocalizedText(1463).Text;
                    Remediation = new LocalizedText(1464).Text;
                    Texture     = ResourceManager.Texture("NewUI/issueLongRechargeTime");
                    break;
            }

            Remediation += addToRemediationText;
        }
    }
}