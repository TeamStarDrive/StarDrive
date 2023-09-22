using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;

namespace Ship_Game.GameScreens.ShipDesign
{
    public class ShipDesignIssues
    {
        public readonly IShipDesign Hull;
        public readonly RoleName Role;
        public Array<DesignIssueDetails> CurrentDesignIssues { get; } = new Array<DesignIssueDetails>();
        public WarningLevel CurrentWarningLevel { get; private set; }
        private readonly Empire Player;
        private EmpireShipDesignStats EmpireStats;

        public ShipDesignIssues(UniverseScreen screen, IShipDesign hull)
        {
            Hull   = hull;
            Role   = hull.Role;
            Player = screen.Player;
            EmpireStats = new(Player);
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

                foreach (IShipDesign design in empire.ShipsWeCanBuild)
                {
                    float warpSpeed = ShipStats.GetFTLSpeed(design, empire);
                    if (warpSpeed < 2000 || Scout(design.Role))
                        continue;

                    if (Civilian(design.Role))
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

            public float AverageEmpireWarpSpeed(RoleName role) => Civilian(role) ? AverageWarpSpeedCivilian
                                                                                 : AverageWarpSpeedMilitary;
        }

        bool IsPlatform => Hull.HullRole == RoleName.platform;
        bool Stationary => Hull.HullRole is RoleName.station or RoleName.platform;
        bool LargeCraft => Hull.HullRole == RoleName.freighter || Hull.HullRole == RoleName.destroyer
                                                               || Hull.HullRole == RoleName.cruiser
                                                               || Hull.HullRole == RoleName.battleship
                                                               || Hull.HullRole == RoleName.capital;

        public static bool Civilian(RoleName role) => role is RoleName.colony 
            or RoleName.freighter 
            or RoleName.construction;

        public static bool Scout(RoleName role) => role == RoleName.scout;

        public void Reset()
        {
            CurrentDesignIssues.Clear();
            CurrentWarningLevel = WarningLevel.None;
        }

        public void CheckIssueNoCommand(int numCommand)
        {
            if (Role != RoleName.platform && numCommand == 0)
                AddDesignIssue(DesignIssueType.NoCommand, WarningLevel.Critical);
        }

        public void CheckIssueBackupCommand(int numCommand, int size)
        {
            if (Role != RoleName.platform && numCommand == 1 && size >= 500)
                AddDesignIssue(DesignIssueType.BackUpCommand, WarningLevel.Major);
        }

        public void CheckIssueStationaryHoldPositionHangars(int numFighterHangars, CombatState combatState)
        {
            if (numFighterHangars > 0 && Stationary && combatState == CombatState.HoldPosition)
            {
                AddDesignIssue(DesignIssueType.OrbitalCarrierHoldPosition, WarningLevel.Critical);
            }
        }

        public void  CheckIssueUnpoweredModules(ShipModule[] unpoweredModules)
        {
            if (unpoweredModules.Length > 0)
            {
                string text = $" {unpoweredModules.Length} modules detected. For instance - {unpoweredModules.First().NameText.Text}.";
                AddDesignIssue(DesignIssueType.UnpoweredModules, WarningLevel.Major, addRemediationText: text);
            }
        }

        public void CheckIssueOrdnanceBurst(float burst, float cap)
        {
            if (burst.LessOrEqual(0) || burst <= cap)
                return;

            float efficiency = cap / burst;
            WarningLevel level = efficiency switch
            {
                > 0.75f => WarningLevel.Minor,
                > 0.5f  => WarningLevel.Major,
                _       => WarningLevel.Critical
            };

            AddDesignIssue(DesignIssueType.HighBurstOrdnance, level, $" {(efficiency*100).String(0)}%");
        }

        public void CheckIssueOrdnance(float ordnanceUsed, float ordnanceRecovered, float ammoTime, bool hasOrdnance)
        {
            if ((ordnanceUsed - ordnanceRecovered).LessOrEqual(0))
                return;  // Inf ammo

            if (!hasOrdnance)
            {
                AddDesignIssue(DesignIssueType.NoOrdnance, WarningLevel.Critical);
            }
            else if (!IsPlatform)
            {
                int goodAmmoTime = LargeCraft ? 60 : 30;
                float ammoRatio  = ammoTime / goodAmmoTime;

                if (ammoRatio > 1)
                    return;

                WarningLevel level = ammoRatio switch
                {
                    < 0.3f => WarningLevel.Critical,
                    < 0.6f => WarningLevel.Major,
                    < 0.9f => WarningLevel.Minor,
                    _      => WarningLevel.Informative
                };

                AddDesignIssue(DesignIssueType.LowOrdnance, level);
            }
        }

        public void CheckIssuePowerRecharge(bool hasEnergyWeapons, float recharge, 
            float powerCapacity, float excessPowerConsumed, out bool hasRechargeIssues)
        {
            hasRechargeIssues = false;
            if (recharge.Less(0))
            { 
                AddDesignIssue(DesignIssueType.NegativeRecharge, WarningLevel.Critical);
                hasRechargeIssues = true;
                return;
            }

            if (!hasEnergyWeapons || excessPowerConsumed < recharge)
                return;

            float rechargeTime    = powerCapacity / recharge.LowerBound(1);

            WarningLevel severity = rechargeTime switch
            {
                > 25 => WarningLevel.Critical,
                > 20 => WarningLevel.Major,
                > 15 => WarningLevel.Minor,
                > 10 => WarningLevel.Informative,
                _    => WarningLevel.None
            };

            if (severity > WarningLevel.None)
            {
                AddDesignIssue(DesignIssueType.LongRechargeTime, severity);
                hasRechargeIssues = true;
            }
        }

        public void CheckPowerCapacityWithSalvo(Ship s, float rechargePerSecond, bool hasFirePowerIssues, out bool hasSalvoFirePowerIssues)
        {
            hasSalvoFirePowerIssues = false;
            if (hasFirePowerIssues)
                return;

            float powerCapacity = s.PowerStoreMax;
            Weapon[] powerWeapons = s.Weapons.Filter(w => w.PowerRequiredToFire > 0 && !w.IsBeam);
            Weapon[] salvoWeapons = powerWeapons.Filter(w => w.SalvoCount > 1 && w.SalvoDuration > 0);

            if (salvoWeapons.Length == 0)
                return;

            float initialPower = powerWeapons.Sum(w => w.SalvoCount > 1 ? 0 : w.PowerRequiredToFire * w.ProjectileCount);
            float beamPower = s.Weapons.Filter(w => w.IsBeam).Sum(w => w.BeamPowerCostPerSecond);
            float averageSalvoTime = salvoWeapons.Average(w => w.SalvoDuration);
            float netBeamPower = beamPower * averageSalvoTime;

            float totalSalvoPower = salvoWeapons.Sum(w => w.PowerRequiredToFire * w.SalvoCount * w.ProjectileCount * (averageSalvoTime / w.SalvoDuration));
            float netPowerNeeded = initialPower + netBeamPower + totalSalvoPower;
            float netPowerStoreDuringSalvo = powerCapacity + (rechargePerSecond*averageSalvoTime);

            float powerRatio = netPowerNeeded / netPowerStoreDuringSalvo;

            if (powerRatio.LessOrEqual(1.01f))
                return;

            hasSalvoFirePowerIssues = true;
            WarningLevel severity = WarningLevel.Informative;

            if      (powerRatio > 2f)    severity = WarningLevel.Critical;
            else if (powerRatio > 1.5f)  severity = WarningLevel.Major;
            else if (powerRatio > 1.25f) severity = WarningLevel.Minor;

            string powerRequirement = $" {(powerRatio * 100).String(0)}%";
            AddDesignIssue(DesignIssueType.SalvoFirePowerEfficiency, severity, powerRequirement);
        }

        public void CheckPowerRequiredToFireOnce(Ship s, out bool hasFirePowerIssues)
        {
            hasFirePowerIssues = false;
            float powerCapacity = s.PowerStoreMax;
            float[] weaponsPowerPerShot = s.Weapons.FilterSelect(w => !w.IsBeam && w.PowerRequiredToFire > 0, 
                                                                 w => w.PowerRequiredToFire * w.ProjectileCount);
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

            WarningLevel severity = percentCanFire switch
            {
                < 80  => WarningLevel.Critical,
                < 90  => WarningLevel.Major,
                < 100 => WarningLevel.Minor,
                _     => WarningLevel.None
            };

            if (percentCanFire.AlmostZero())
                efficiency = 0;

            string strNumCanFire = $" {(100 - percentCanFire).String(0)}% {Localizer.Token(GameText.OfTheShipssEnergyWeapons)}";
            string strEfficiency = $" {Localizer.Token(GameText.TheEfficiencyOfTheShipss)} {efficiency.String(0)}%.";

            if (severity > WarningLevel.None)
            {
                AddDesignIssue(DesignIssueType.OneTimeFireEfficiency, severity, $"{strNumCanFire}{strEfficiency}");
                hasFirePowerIssues = true;
            }
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

        public void CheckTargetExclusions(bool hasWeapons, bool canTargetFighters,
            bool  canTargetCorvettes, bool canTargetCapitals, HangarOptions designation)
        {
            if (!hasWeapons)
                return;

            WarningLevel severity = LargeCraft ? WarningLevel.Minor : WarningLevel.Major;
            if (designation == HangarOptions.Interceptor)
                severity = WarningLevel.Critical;

            if (!canTargetFighters)
                AddDesignIssue(DesignIssueType.CantTargetFighters, severity);

            if (!canTargetCorvettes)
                AddDesignIssue(DesignIssueType.CantTargetCorvettes, severity);

            severity = LargeCraft ? WarningLevel.Critical : WarningLevel.Minor;
            if (designation == HangarOptions.AntiShip)
                severity = WarningLevel.Critical;

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

            WarningLevel severity = weaponPowerTime switch
            {
                < 2  => WarningLevel.Critical,
                < 4  => WarningLevel.Major,
                < 8  => WarningLevel.Minor,
                < 16 => WarningLevel.Informative,
                _    => WarningLevel.None
            };

            if (severity > WarningLevel.None)
                AddDesignIssue(DesignIssueType.LowWeaponPowerTime, severity);
        }

        public void CheckCombatEfficiency(float excessPowerConsumed, float weaponPowerTime, float netPowerRecharge, 
                                          int numWeapons, int numOrdnanceWeapons)
        {
            if (numWeapons == 0 || numOrdnanceWeapons == numWeapons || excessPowerConsumed.Less(0))
                return;

            float energyWeaponsRatio   = (float)(numWeapons - numOrdnanceWeapons) / numWeapons;
            float efficiencyReduction  = (1 - netPowerRecharge / (excessPowerConsumed + netPowerRecharge));
            efficiencyReduction       *= energyWeaponsRatio;
            float netEfficiency        = (1 - efficiencyReduction) * 100;

            WarningLevel severity = netEfficiency switch
            {
                < 20 => WarningLevel.Critical,
                < 40 => WarningLevel.Major,
                < 60 => WarningLevel.Minor,
                < 80 => WarningLevel.Informative,
                _    => WarningLevel.None
            };

            // Modify level by weapon power time if there is an issue
            if (severity > WarningLevel.None)
            {
                switch (weaponPowerTime)
                {
                    case > 60: severity -= 3; break;
                    case > 40: severity -= 2; break;
                    case > 20: severity -= 1; break;
                }

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
            float excessPowerConsumed, bool hasPowerCells, float recharge, float powerCapacity, bool hasEnergyWeapons)
        {
            if (!hasPowerCells
                || hasBeamWeapons && burstEnergyPowerTime.Less(2.2f) // 10% percent more of required beam time
                || excessPowerConsumed.Greater(0))
            {
                return;
            }

            if (!hasEnergyWeapons)
            {
                if (powerCapacity > recharge)
                    AddDesignIssue(DesignIssueType.ExcessPowerCells, WarningLevel.Minor);
            }
            else
            {
                if (powerCapacity > recharge * 30f)
                    AddDesignIssue(DesignIssueType.ExcessPowerCells, WarningLevel.Minor);
                else if (powerCapacity > recharge * 15f)
                    AddDesignIssue(DesignIssueType.ExcessPowerCells, WarningLevel.Informative);
            }
        }

        public void CheckBurstPowerTime(bool hasBeamWeapons, float burstEnergyPowerTime, float averageBeamDuration)
        {
            if (!hasBeamWeapons || burstEnergyPowerTime.GreaterOrEqual(averageBeamDuration)
                                || burstEnergyPowerTime.Less(0))
            {
                return;
            }

            WarningLevel severity = burstEnergyPowerTime switch
            {
                < 1f    => WarningLevel.Critical,
                < 1.75f => WarningLevel.Major,
                < 2f    => WarningLevel.Minor,
                _       => WarningLevel.None
            };

            if (severity > WarningLevel.None)
                AddDesignIssue(DesignIssueType.LowBurstPowerTime, severity);
        }

        public void CheckOrdnanceVsEnergyWeapons(float weaponsArea, float kineticWeaponsArea, float ordnanceUsed, float ordnanceRecovered)
        {
            if (Stationary || weaponsArea == 0 || kineticWeaponsArea == 0)
                return;

            if (kineticWeaponsArea < weaponsArea && ordnanceUsed > ordnanceRecovered)
                AddDesignIssue(DesignIssueType.NoOrdnanceResupplyPlayerOrder, WarningLevel.Informative);

            float ordnanceToEnergyRatio = kineticWeaponsArea / weaponsArea;
            if (ordnanceToEnergyRatio.LessOrEqual(ShipResupply.KineticRatioThreshold))
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

        public void CheckDedicatedCarrier(bool hasFighterHangars, RoleName role, 
                                          int maxWeaponRange, float sensorRange, bool shortRange)
        {
            if (role != RoleName.carrier  && !Stationary || !hasFighterHangars)
                return;

            bool minCarrier  = false;
            string rangeText = shortRange  // short range or attack runs
                ? $"\n{GetRangeLaunchText(maxWeaponRange, out int minLaunchRangeWeapons, out minCarrier)} " +
                  $"{HelperFunctions.GetNumberString(maxWeaponRange.LowerBound(minLaunchRangeWeapons))}" 
                : $"\n{Localizer.Token(GameText.SensorRangeOf)} {sensorRange.GetNumberString()}";

            if (minCarrier) // too low max weapon range, using default minimum hangar launch from carrier bays
                rangeText = $" {rangeText}{Localizer.Token(GameText.SinceTheShipsMaximumWeapon)} {HelperFunctions.GetNumberString(maxWeaponRange)}.";

            AddDesignIssue(DesignIssueType.DedicatedCarrier, WarningLevel.Informative, rangeText);
        }

        public void CheckSecondaryCarrier(bool hasFighterHangars, RoleName role, int maxWeaponRange)
        {
            if (role == RoleName.carrier || !hasFighterHangars || Stationary)
                return;

            string rangeText = $"\n{GetRangeLaunchText(maxWeaponRange, out int minLaunchRangeWeapons, out bool minCarrier)}" +
                               $" {HelperFunctions.GetNumberString(maxWeaponRange.LowerBound(minLaunchRangeWeapons))}";

            if (minCarrier) // too low max weapon range, using default minimum hangar launch from carrier bays
                rangeText = $" {rangeText}{Localizer.Token(GameText.SinceTheShipsMaximumWeapon)} {HelperFunctions.GetNumberString(maxWeaponRange)}.";

            AddDesignIssue(DesignIssueType.SecondaryCarrier, WarningLevel.Informative, rangeText);
        }

        public void CheckLowResearchTime(float researchTime)
        {
            if (!Hull.IsResearchStation)
                return;

            float minimum = ShipResupply.NumTurnsForGoodResearchSupply;
            WarningLevel severity = WarningLevel.Informative;

            if      (researchTime > minimum * 1.5)   return;
            else if (researchTime < minimum)         severity = WarningLevel.Critical;
            else if (researchTime < minimum * 1.15f) severity = WarningLevel.Major;
            else if (researchTime < minimum * 1.3f)  severity = WarningLevel.Minor;

            AddDesignIssue(DesignIssueType.LowResearchTime, severity, 
                severity == WarningLevel.Critical ? $" {Localizer.Token(GameText.DesignIssueLowResearchTimeNotGood)}" : "");
        }

        public void CheckLowRefiningTime(float refiningTime)
        {
            if (!Hull.IsMiningStation)
                return;

            float minimum = ShipResupply.NumTurnsForGoodRefiningSupply;
            WarningLevel severity = WarningLevel.Informative;

            if (refiningTime > minimum * 1.5) return;
            else if (refiningTime < minimum) severity = WarningLevel.Critical;
            else if (refiningTime < minimum * 1.15f) severity = WarningLevel.Major;
            else if (refiningTime < minimum * 1.3f) severity = WarningLevel.Minor;

            AddDesignIssue(DesignIssueType.LowRefiningTime, severity,
                severity == WarningLevel.Critical ? $" {Localizer.Token(GameText.DesignIssueLowRefiningTimeNotGood)}" : "");
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

        public void CheckTargets(ShipModule[] poweredWeapons, int maxTargets)
        {
            if (poweredWeapons.Length == 0 || maxTargets < 1)
                return;

            Map<int,ShipModule> angleGroups = poweredWeapons.GroupBy(m =>
            {
                int facing = m.TurretAngle;
                facing = facing == 360 ? 0 : facing;
                // round to fixed angles 
                return facing.RoundDownToMultipleOf(15);
            });

            int count = angleGroups.Count;
            float ratioToTargets = count / (float)maxTargets;

            WarningLevel severity;
            switch (ratioToTargets)
            {
                case > 4: severity = WarningLevel.Critical;    break;
                case > 3: severity = WarningLevel.Major;       break;
                case > 2: severity = WarningLevel.Minor;       break;
                case > 1: severity = WarningLevel.Informative; break;
                default: return;
            }

            string target     = $" {Localizer.Token(GameText.FcsPower)}: {maxTargets}, ";
            string fireArcs   = $"{Localizer.Token(GameText.FireArc)}: {count} ";
            string baseString = !IsPlatform ? "" : $" {Localizer.Token(GameText.OrbitalTracking)}";

            AddDesignIssue(DesignIssueType.Targets, severity, baseString + target + fireArcs);
        }

        public void CheckConstructorCost(bool isConstructor, float cost)
        {
            if (!isConstructor || cost <= GlobalStats.Defaults.ConstructionShipOrbitalDiscount) 
                return;

            float excessCost = cost - GlobalStats.Defaults.ConstructionShipOrbitalDiscount;
            AddDesignIssue(DesignIssueType.ConstructorCost, WarningLevel.Minor, $" {excessCost}.");
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
        SalvoFirePowerEfficiency,
        ExcessPowerCells,
        DedicatedCarrier,
        SecondaryCarrier,
        OrbitalCarrierHoldPosition,
        LowResearchTime,
        ConstructorCost,
        LowRefiningTime
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
                case DesignIssueType.SalvoFirePowerEfficiency:
                    Title       = GameText.DesignIssueSalvoFirePowerEfficiencyTitle;
                    Problem     = GameText.DesignIssueSalvoFirePowerEfficiencyProblem;
                    Remediation = GameText.DesignIssueSalvoFirePowerEfficiencyRemidiation;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowSalvoPower");
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
                case DesignIssueType.OrbitalCarrierHoldPosition:
                    Title       = GameText.DesignIssueOrbitalHangarHoldPositionTitle;
                    Problem     = GameText.DesignIssueOrbitalHangarHoldPositionProblem;
                    Remediation = GameText.DesignIssueOrbitalHangarHoldPositionRemidiation;
                    Texture     = ResourceManager.Texture("NewUI/IssueOrbitalHangarHold");
                    break;
                case DesignIssueType.LowResearchTime:
                    Title       = GameText.DesignIssueLowResearchTimeTitle;
                    Problem     = GameText.DesignIssueLowResearchTimeProblem;
                    Remediation = GameText.DesignIssueLowResearchTimeRemidiation;
                    Texture     = ResourceManager.Texture("NewUI/IssueLowResearchTime");
                    break;
                case DesignIssueType.ConstructorCost:
                    Title       = GameText.DesignIssueConstructorCostTitle;
                    Problem     = GameText.DesignIssueConstructorCostProblem;
                    Remediation = GameText.DesignIssueConstructorCostRemidiation;
                    Texture     = ResourceManager.Texture("NewUI/IssueConstructorCost");
                    break;
                case DesignIssueType.LowRefiningTime:
                    Title = GameText.DesignIssueLowRefiningTimeTitle;
                    Problem = GameText.DesignIssueLowRefiningTimeProblem;
                    Remediation = GameText.DesignIssueLowRefiningTimeRemidiation;
                    Texture = ResourceManager.Texture("NewUI/IssueLowRefiingTime");
                    break;
            }
        }
    }
}
