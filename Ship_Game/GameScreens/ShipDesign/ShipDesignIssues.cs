using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Linq;

namespace Ship_Game.GameScreens.ShipDesign
{
    public class ShipDesignIssues
    {
        public readonly ShipData Hull;
        public readonly ShipData.RoleName Role;
        public Array<DesignIssueDetails> CurrentDesignIssues { get; } = new Array<DesignIssueDetails>();
        public WarningLevel CurrentWarningLevel { get; private set; }
        private readonly Empire Player;
        private EmpireShipDesignStats EmpireStats;

        public ShipDesignIssues(ShipData hull)
        {
            Hull   = hull;
            Role   = hull.Role;
            Player = EmpireManager.Player;
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
                    float warpSpeed = ship.Stats.GetFTLSpeed(ship.Mass, empire);
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
                                                                        || Hull.HullRole == ShipData.RoleName.battleship
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

            if      (rechargeTime > 20) severity = WarningLevel.Critical;
            else if (rechargeTime > 16) severity = WarningLevel.Major;
            else if (rechargeTime > 12) severity = WarningLevel.Minor;
            else if (rechargeTime > 8)  severity = WarningLevel.Informative;

            if (severity > WarningLevel.None)
                AddDesignIssue(DesignIssueType.LongRechargeTime, severity);
        }

        public void CheckPowerRequiredToFireOnce(Ship s)
        {
            float powerCapacity = s.PowerStoreMax;

            float[] weaponsPowerPerShot = s.Weapons.FilterSelect(w => !w.isBeam && w.PowerRequiredToFire > 0, 
                                                                 w => w.PowerRequiredToFire);
            if (weaponsPowerPerShot.Length == 0)
                return;

            Array.Sort(weaponsPowerPerShot);
            int numCanFire = 0;
            for (int i = 0; i < weaponsPowerPerShot.Length; i++)
            {
                float weaponPower = weaponsPowerPerShot[i];
                if (weaponPower.LessOrEqual(powerCapacity))
                    numCanFire += 1;
                else
                    break;
            }

            float percentCanFire  = 100f * numCanFire / weaponsPowerPerShot.Length;
            float efficiency      = 100 * powerCapacity / weaponsPowerPerShot.Sum().LowerBound(1);
            WarningLevel severity = WarningLevel.None;

            if      (percentCanFire < 50)  severity = WarningLevel.Critical;
            else if (percentCanFire < 70)  severity = WarningLevel.Major;
            else if (percentCanFire < 90)  severity = WarningLevel.Minor;
            else if (percentCanFire < 100) severity = WarningLevel.Informative;

            if (percentCanFire.AlmostZero())
                efficiency = 0;

            string strNumCanFire = $" {(100 - percentCanFire).String(0)}% {Localizer.Token(GameText.OfTheShipssEnergyWeapons)}";
            string strEfficiency = $" {Localizer.Token(GameText.TheEfficiencyOfTheShipss)} {efficiency.String(0)}%.";

            if (severity > WarningLevel.None)
                AddDesignIssue(DesignIssueType.OneTimeFireEfficiency, severity, $"{strNumCanFire}{strEfficiency}");
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


            string civilianOrMilitary = Civilian(Role) ? Localizer.Token(GameText.CivilianShips)
                                                       : Localizer.Token(GameText.MilitaryShips);

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
                string efficiencyText = $" {Localizer.Token(GameText.CombatEfficiencyAfterPowerReserves)} {netEfficiency.String(0)}%.";
                AddDesignIssue(DesignIssueType.NotIdealCombatEfficiency, severity, efficiencyText);
            }
        }

        public void CheckExcessPowerCells(bool hasBeamWeapons, float burstEnergyPowerTime, 
            float excessPowerConsumed, bool hasPowerCells, float recharge, float powerCapacity)
        {
            if (!hasPowerCells
                || hasBeamWeapons && burstEnergyPowerTime.Less(2.2f) // 10% percent more of required beam time
                || excessPowerConsumed.Greater(0))
            {
                return;
            }

            if (powerCapacity > recharge * 1.5f)
                AddDesignIssue(DesignIssueType.ExcessPowerCells, WarningLevel.Informative);
            else if (powerCapacity > recharge)
                AddDesignIssue(DesignIssueType.ExcessPowerCells, WarningLevel.Minor);
        }

        public void CheckBurstPowerTime(bool hasBeamWeapons, float burstEnergyPowerTime)
        {
            if (!hasBeamWeapons || burstEnergyPowerTime.GreaterOrEqual(2) || burstEnergyPowerTime.Less(0))
                return;

            WarningLevel severity = burstEnergyPowerTime < 1 ? WarningLevel.Critical : WarningLevel.Major;
            AddDesignIssue(DesignIssueType.LowBurstPowerTime, severity);
        }

        public void CheckOrdnanceVsEnergyWeapons(int numWeapons, int numOrdnanceWeapons, float ordnanceUsed, float ordnanceRecovered)
        {
            if (Stationary || numWeapons == 0 || numOrdnanceWeapons == 0)
                return;

            if (numOrdnanceWeapons < numWeapons && ordnanceUsed > ordnanceRecovered)
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
            string troopsMissing = $" {diff} {Localizer.Token(GameText.MoreTroopsNeeded)}";
            AddDesignIssue(DesignIssueType.LowTroopsForBays, WarningLevel.Major, troopsMissing);
        }

        public void CheckDedicatedCarrier(bool hasFighterHangars, ShipData.RoleName role, 
                                          int maxWeaponRange, float sensorRange, bool shortRange)
        {
            if (role != ShipData.RoleName.carrier  && !Stationary || !hasFighterHangars)
                return;

            bool minCarrier  = false;
            string rangeText = shortRange  // short range or attack runs
                ? $"\n{GetRangeLaunchText(maxWeaponRange, out int minLaunchRangeWeapons, out minCarrier)} " +
                  $"{HelperFunctions.GetNumberString(maxWeaponRange.LowerBound(minLaunchRangeWeapons))}" 
                : $"\n{Localizer.Token(GameText.SensorRangeOf)} {HelperFunctions.GetNumberString(sensorRange)}";

            if (minCarrier) // too low max weapon range, using default minimum hangar launch from carrier bays
                rangeText = $" {rangeText}{Localizer.Token(GameText.SinceTheShipsMaximumWeapon)} {HelperFunctions.GetNumberString(maxWeaponRange)}.";

            AddDesignIssue(DesignIssueType.DedicatedCarrier, WarningLevel.Informative, rangeText);
        }

        public void CheckSecondaryCarrier(bool hasFighterHangars, ShipData.RoleName role, int maxWeaponRange)
        {
            if (role == ShipData.RoleName.carrier || !hasFighterHangars || Stationary)
                return;

            string rangeText = $"\n{GetRangeLaunchText(maxWeaponRange, out int minLaunchRangeWeapons, out bool minCarrier)}" +
                               $" {HelperFunctions.GetNumberString(maxWeaponRange.LowerBound(minLaunchRangeWeapons))}";

            if (minCarrier) // too low max weapon range, using default minimum hangar launch from carrier bays
                rangeText = $" {rangeText}{Localizer.Token(GameText.SinceTheShipsMaximumWeapon)} {HelperFunctions.GetNumberString(maxWeaponRange)}.";

            AddDesignIssue(DesignIssueType.SecondaryCarrier, WarningLevel.Informative, rangeText);
        }

        string GetRangeLaunchText(int maxWeaponRange, out int minLaunchRangeWeapons, out bool usingMinimumCarrierRange)
        {
            usingMinimumCarrierRange = false;
            minLaunchRangeWeapons    = (int)CarrierBays.DefaultHangarRange;
            if (maxWeaponRange < minLaunchRangeWeapons)
            {
                usingMinimumCarrierRange = true;
                return Localizer.Token(GameText.MinimumLaunchRangeOf);
            }

            return Localizer.Token(GameText.MaximumWeaponRangeOf);
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

            string troopsMissing = $" {diff} {Localizer.Token(GameText.MoreTroopsNeeded)}";
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

            string remediation = $" {Localizer.Token(GameText.Average)}" +
                                 $" {Localizer.Token(GameText.Accuracy)}:" +
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

            string target     = $" {Localizer.Token(GameText.FcsPower)}: {maxTargets.String(1)}, ";
            string fireArcs   = $"{Localizer.Token(GameText.FireArc)}: {count.String(1)} ";
            string baseString = !IsPlatform ? "" : $" {Localizer.Token(GameText.OrbitalTracking)}";

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
        LongRechargeTime,
        OneTimeFireEfficiency,
        ExcessPowerCells,
        DedicatedCarrier,
        SecondaryCarrier
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
        public readonly LocalizedText Title;
        public readonly LocalizedText Problem;
        public readonly LocalizedText Remediation;
        public readonly SubTexture Texture;
        public readonly string AdditionalText;

        public DesignIssueDetails(DesignIssueType issueType, WarningLevel severity, string addToRemediationText)
        {
            Type           = issueType;
            Severity       = severity;
            Color          = ShipDesignIssues.IssueColor(severity);
            AdditionalText = addToRemediationText;
            switch (issueType)
            {
                default:
                case DesignIssueType.NoCommand:
                    Title       = GameText.MissingCommandModule;
                    Problem     = GameText.TheShipDoesNotHave;
                    Remediation = GameText.AddACommandModuleTo;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoCommand");
                    break;
                case DesignIssueType.BackUpCommand:
                    Title       = GameText.NoBackupCommandModule;
                    Problem     = GameText.TheShipCurrentlyHasOnly;
                    Remediation = GameText.AddABackupCommandModule;
                    Texture     = ResourceManager.Texture("NewUI/IssueBackupCommand");
                    break;
                case DesignIssueType.UnpoweredModules:
                    Title       = GameText.UnpoweredModulesDetected;
                    Problem     = GameText.SomeOfThePowerConsuming;
                    Remediation = GameText.YouCanAddAReactor;
                    Texture     = ResourceManager.Texture("NewUI/IssueUnpowered");
                    break;
                case DesignIssueType.NoOrdnance:
                    Title       = GameText.NoOrdnanceDetected;
                    Problem     = GameText.ThisShipDesignNeedsOrdnance;
                    Remediation = GameText.YouMustAddOrdnanceStores;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoOrdnance");
                    break;
                case DesignIssueType.LowOrdnance:
                    Title       = GameText.LowOrdnanceTime;
                    Problem     = GameText.TheShipHasLowEffective;
                    Remediation = GameText.AddOrdnanceStoresOrOrdnance;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowOrdnance");
                    break;
                case DesignIssueType.LowWarpTime:
                    Title       = GameText.LowWarpTime;
                    Problem     = GameText.TheShipsAbilityToSustain;
                    Remediation = GameText.AddReactorsToIncreaseThe;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowWarpTime");
                    break;
                case DesignIssueType.NoWarp:
                    Title       = GameText.NoWarpSpeed;
                    Problem     = GameText.TheShipIsNotWarp;
                    Remediation = GameText.AddWarpCapableModulesUsually;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoWarp");
                    break;
                case DesignIssueType.SlowWarp:
                    Title       = GameText.SlowWarpSpeed;
                    Problem     = GameText.TheShipsWarpSpeedBelow;
                    Remediation = GameText.AddWarpCapableModulesUsually2; 
                    Texture     = ResourceManager.Texture("NewUI/IssueSlowWarp");
                    break;
                case DesignIssueType.NegativeRecharge:
                    Title       = GameText.NegativePowerRecharge;
                    Problem     = GameText.TheShipDoesNotHave2;
                    Remediation = GameText.AddReactorsToIncreaseThe2;
                    Texture     = ResourceManager.Texture("NewUI/IssueNegativeRecharge");
                    break;
                case DesignIssueType.NoSpeed:
                    Title       = GameText.NoSubLightSpeed;
                    Problem     = GameText.TheShipCannotMoveAt;
                    Remediation = GameText.AddSubLightEnginesTo;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoSublight");
                    break;
                case DesignIssueType.CantTargetFighters:
                    Title       = GameText.CannotTargetFighters;
                    Problem     = GameText.NoWeaponOnBoardThe;
                    Remediation = GameText.AddWeaponSystemsWhichCan;
                    Texture     = ResourceManager.Texture("NewUI/IssueCantTargetFighters");
                    break;
                case DesignIssueType.CantTargetCorvettes:
                    Title       = GameText.CannotTargetCorvettes;
                    Problem     = GameText.NoWeaponOnBoardThe2;
                    Remediation = GameText.AddWeaponSystemsWhichCan2;
                    Texture     = ResourceManager.Texture("NewUI/IssueCantTargetCorvettes");
                    break;
                case DesignIssueType.CantTargetCapitals:
                    Title       = GameText.CannotTargetCapitals;
                    Problem     = GameText.NoWeaponOnBoardThe3;
                    Remediation = GameText.AddWeaponSystemsWhichCan3;
                    Texture     = ResourceManager.Texture("NewUI/IssueCantTargetCapitals");
                    break;
                case DesignIssueType.LowPdValue:
                    Title       = GameText.LowNumberOfPdWeapons;
                    Problem     = GameText.TheShipDoesNotHave3;
                    Remediation = GameText.AddPointDefenseCapableWeapons;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowPD");
                    break;
                case DesignIssueType.LowWeaponPowerTime:
                    Title       = GameText.LowWeaponTime;
                    Problem     = GameText.TheShipCanFireAll;
                    Remediation = GameText.AddEnergyStorageToIncrease;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowEnergyWeaponTime");
                    break;
                case DesignIssueType.LowBurstPowerTime:
                    Title       = GameText.LowBeamBurstTime;
                    Problem     = GameText.TheShipCannotSustainAll;
                    Remediation = GameText.AddReactorsOrEnergyStorage;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowEnergyBurstTime");
                    break;
                case DesignIssueType.NoOrdnanceResupplyCombat:
                    Title       = GameText.NoAmmoResupplyInCombat;
                    Problem     = GameText.DueToHighRatioOf;
                    Remediation = GameText.GenerallyTheShipWillStay;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoAmmoResupplyCombat");
                    break;
                case DesignIssueType.NoOrdnanceResupplyPlayerOrder:
                    Title       = GameText.EnergyAndKineticBlend;
                    Problem     = GameText.ThisShipHasABlend;
                    Remediation = GameText.TheShipMightHaveReduced;
                    Texture     = ResourceManager.Texture("NewUI/IssueNoAmmoResupplyPlayer");
                    break;
                case DesignIssueType.LowTroopsForBays:
                    Title       = GameText.NotEnoughTroopsToLaunch;
                    Problem     = GameText.TheCurrentNumberOfTroops;
                    Remediation = GameText.AddMoreBarracksModulesTo;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowTroopsForBays");
                    break;
                case DesignIssueType.LowTroops:
                    Title       = GameText.LowGarrison;
                    Problem     = GameText.TheShipHasLessThan;
                    Remediation = GameText.AddMoreBarracksModulesTo2;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowTroops");
                    break;
                case DesignIssueType.NotIdealCombatEfficiency:
                    Title       = GameText.PartialWeaponEfficiency;
                    Problem     = GameText.TheShipConsumesMoreEnergy;
                    Remediation = GameText.AddReactorsToOffsetThe;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowWeaponPowerEfficiency");
                    break;
                case DesignIssueType.HighBurstOrdnance:
                    Title       = GameText.BurstOrdnanceHigherThanStorage;
                    Problem     = GameText.TheShipConsumesMoreOrdnance;
                    Remediation = GameText.AddOrdnanceStorageToSupport;
                    Texture     = ResourceManager.Texture("NewUI/IssueHighBurstOrdnance");
                    break;
                case DesignIssueType.Accuracy:
                    Title       = GameText.LowAccuracy;
                    Problem     = GameText.WeaponAccuracy;
                    Remediation = GameText.ImproveAccuracy;
                    Texture     = ResourceManager.Texture("NewUI/IssuesLowAccuracy");
                    break;
                case DesignIssueType.Targets:
                    Title       = GameText.LowTracking;
                    Problem     = GameText.TrackingTargets;
                    Remediation = GameText.ImproveTracking;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowTracking");
                    break;
                case DesignIssueType.LongRechargeTime:
                    Title       = GameText.LongRechargeTime;
                    Problem     = GameText.TheShipTakesLongTime;
                    Remediation = GameText.AddReactorsWhichWillIncrease;
                    Texture     = ResourceManager.Texture("NewUI/issueLongRechargeTime");
                    break;
                case DesignIssueType.OneTimeFireEfficiency:
                    Title       = GameText.LowPowerCapacity;
                    Problem     = GameText.TheShipsPowerCapacityIt;
                    Remediation = GameText.AddPowerCellsToThe;
                    Texture     = ResourceManager.Texture("NewUI/IssueNegativeRecharge");
                    break;
                case DesignIssueType.ExcessPowerCells:
                    Title       = GameText.ExcessPowerCells;
                    Problem     = GameText.TheShipHasExcessiveNumber;
                    Remediation = GameText.ReplaceSomeOfYourPower;
                    Texture     = ResourceManager.Texture("NewUI/IssueExcessPowerCells");
                    break;
                case DesignIssueType.DedicatedCarrier:
                    Title       = GameText.DedicatedCarrier;
                    Problem     = GameText.ThisShipIsADedicated;
                    Remediation = GameText.CurrentSelectedFighterLaunchRange;
                    Texture     = ResourceManager.Texture("NewUI/IssueDedicatedCarrier");
                    break;
                case DesignIssueType.SecondaryCarrier:
                    Title       = GameText.SecondaryCarrier;
                    Problem     = GameText.ThisShipHasSomeFighter;
                    Remediation = GameText.CurrentSelectedFighterLaunchRange;
                    Texture     = ResourceManager.Texture("NewUI/IssueSecondaryCarrier");
                    break;
            }
        }
    }
}
