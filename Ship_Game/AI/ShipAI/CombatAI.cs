using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.AI.ShipMovement;
using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Debug;
using static Ship_Game.AI.ShipAI;
using static Ship_Game.AI.ShipAI.TargetParameterTotals;
using SynapseGaming.LightingSystem.Shadows;

namespace Ship_Game.AI
{
    public sealed class CombatAI
    {
        private float AssistWeight = 0.5f;
        public Ship Owner;
        ShipAIPlan CombatTactic;
        CombatState CurrentCombatStance;

        public TargetParameterTotals EnemyGroupData;
        public TargetParameterTotals FriendlyGroupData;

        public CombatAI()
        {
        }

        public CombatAI(Ship ship)
        {
            Owner = ship;
            CurrentCombatStance = ship.AI.CombatState;
            SetCombatTactics(ship.AI.CombatState);
        }

        public void ClearTargets()
        {
            Owner.AI.Target = null;
            Owner.AI.PotentialTargets.Clear();
        }

        public ShipWeight ShipCommandTargeting(ShipWeight weight, TargetParameterTotals targetPrefs)
        {
            // target prefs is a collection of averages from all targets. 

            Vector2 friendlyCenter = Owner.AI.FriendliesSwarmCenter;
            Vector2 ownerCenter = Owner.Center;
            TargetParameterTotals fleetPrefs = Owner.fleet?.AverageFleetAttributes ?? new TargetParameterTotals();
            Vector2 battleCenter = BattleCenters(targetPrefs, ref friendlyCenter, ref ownerCenter);

            Ship target                  = weight.Ship;
            float distanceToTarget       = ownerCenter.Distance(target.Center).LowerBound(1);
            float distanceToMass         = friendlyCenter.Distance(targetPrefs.DPSCenter);
            float distanceToBattleCenter = friendlyCenter.Distance(battleCenter);

            float chanceToHit           = -0.5f + (target.Radius - Owner.MaxWeaponError) / target.Radius;
            bool inTheirRange           = distanceToTarget < target.WeaponsMaxRange;
            bool inOurRange             = distanceToTarget < Owner.WeaponsMaxRange;
            bool inOurMass              = target.Center.InRadius(friendlyCenter, distanceToBattleCenter);
            bool tooFar                 = target.Center.OutsideRadius(friendlyCenter, distanceToMass / 2);

            
            // more agile than us the less they are valued. 
            float turnRatio        = (Owner.RotationRadiansPerSecond - target.RotationRadiansPerSecond).Clamped(-1, 1);
            float stlRatio         = (Owner.MaxSTLSpeed - target.MaxSTLSpeed).Clamped(-1,0);
            float massDPSValue     = (target.TotalDps - targetPrefs.DPS).Clamped(-1, 1);
            float targetDPSValue   = Owner.TotalDps < target.TotalDps  ? -1 : 0;
            float massTargetValue  = distanceToTarget > distanceToMass? -1 : 1;
            float ownerTargetValue = Owner.WeaponsMaxRange > distanceToTarget  ? 1 : 0;
            bool weAreAScreenShip  = fleetPrefs.ScreenShip(Owner);


            float targetValue = 0;

            targetValue += HangarShips(targetPrefs, distanceToTarget, chanceToHit, target);
            targetValue += chanceToHit < 0.25f ? -1 : 0;
            targetValue += tooFar ? -1 :0;
            targetValue += weAreAScreenShip && inOurMass ? 0 : -1;
            targetValue += inOurMass ? 1 : 0;
            targetValue += turnRatio;
            targetValue += stlRatio;
            targetValue += massDPSValue;
            targetValue += targetDPSValue;
            targetValue += massTargetValue;
            targetValue += ownerTargetValue;
            targetValue += inTheirRange ? 1 : 0;
            targetValue += inOurRange ? 1 : 0;
            targetValue += target == Owner.AI.Target ? 0.5f : 0;
            targetValue += target.LastDamagedBy == Owner ? 0.25f : 0;
            targetValue += Owner.loyalty.WeArePirates && target.shipData.ShipCategory == ShipData.Category.Civilian ? 1 : 0;
            targetValue += target.AI.State == AIState.Resupply ? -1 : 0;
            targetValue += target.IsHangarShip && chanceToHit < 0.5f ? -1 : 0;
            targetValue += target.IsHomeDefense && chanceToHit < 0.5f ? -1 : 0;
            targetValue += target.MaxSTLSpeed == 0 ? -1 : 0;
            targetValue += target.TotalDps < 1 ? -1 : 0;
            targetValue += target.Carrier.AllFighterHangars.Length;

            weight.SetWeight(targetValue);

            if (float.IsNaN(weight.Weight))
                Log.Error($"ship weight NaN for {weight.Ship}");
            if (float.IsInfinity(weight.Weight))
                Log.Error($"ship weight infinite for {weight.Ship}");
            
            if (Empire.Universe.SelectedShip == Owner && Empire.Universe.DebugWin != null)
            {
                Vector2 debugOffset = new Vector2(target.Radius + 50);
                Empire.Universe.DebugWin?.DrawText(DebugModes.Targeting, target.Center + debugOffset, $"TargetValue : {targetValue.ToString()}", Color.Yellow, 0.3f);
                Empire.Universe.DebugWin?.DrawText(targetPrefs.Center, $"Enemy Center", Color.Red, 0.3f);
                Empire.Universe.DebugWin?.DrawText(targetPrefs.DPSCenter, $"DPS Center", Color.Red, 0.3f);
                Empire.Universe.DebugWin?.DrawText(friendlyCenter, $"FriendlyCenter", Color.Green, 0.3f);
                Empire.Universe.DebugWin?.DrawText(ownerCenter, $"FleetPosCenter", Color.Green, 0.3f);
                Empire.Universe.DebugWin?.DrawText(battleCenter, $"Battle Center", Color.Yellow, 0.3f);
            }
            return weight;
        }

        private Vector2 BattleCenters(TargetParameterTotals targetPrefs, ref Vector2 friendlyCenter, ref Vector2 ownerCenter)
        {
            if (Owner.fleet != null)
            {
                Vector2 dir          = friendlyCenter.DirectionToTarget(targetPrefs.DPSCenter);
                Vector2 fleetPos     = Owner.fleet.GetPositionFromDirection(Owner, dir);
                ownerCenter          = friendlyCenter + fleetPos;
            }
            else if (Owner.IsHangarShip)
            {
                friendlyCenter = Owner.Mothership.Center;
                ownerCenter = friendlyCenter;
            }

            Vector2 battleCenter = (friendlyCenter + targetPrefs.DPSCenter) / 2f;
            return battleCenter;
        }

        private float HangarShips(TargetParameterTotals targetPrefs, float distanceToTarget, float chanceToHit, Ship target)
        {
            float targetValue = 0;
            Ship motherShip   = Owner.Mothership ?? Owner.AI.EscortTarget;

            if (motherShip != null)
            {
                bool targetingMothership      = target.AI.Target == motherShip;
                bool targetOfMothership       = target == motherShip.AI.Target;
                bool damagingMotherShip       = motherShip.LastDamagedBy == target;
                float motherShipDistanceValue = (motherShip.Center.Distance(target.Center) - distanceToTarget).Clamped(-1, 1);

                targetValue += motherShipDistanceValue;
                switch (Owner.shipData.HangarDesignation)
                {
                    case ShipData.HangarOptions.General:
                        {
                            targetValue += targetOfMothership ? 0 : -1;
                            targetValue += targetingMothership ? 0 : -1;
                            targetValue += damagingMotherShip ? 0 : -1;
                            break;
                        }
                    case ShipData.HangarOptions.AntiShip:
                        {
                            targetValue += targetOfMothership ? 0 : -1;
                            targetValue += target.IsHangarShip ? -1 : 0;
                            targetValue += chanceToHit > 0.5f ? 0 : -1;
                            break;
                        }
                    case ShipData.HangarOptions.Interceptor:
                        {
                            targetValue += motherShip.Carrier.AllFighterHangars.Any(h => h.HangarShipGuid == target.AI.Target?.guid) ? 0 : -1;
                            targetValue += target.shipData.HangarDesignation == ShipData.HangarOptions.AntiShip ? 0 : -1;
                            targetValue += chanceToHit < 0.5f ? 0 : -1;
                            targetValue += target.IsHangarShip ? 0 : -1;
                            targetValue += target.DesignRoleType == ShipData.RoleType.Troop ? 0 : -1;
                            targetValue += target.DesignRoleType == ShipData.RoleType.EmpireSupport ? 0 : -1;
                            targetValue += target.DesignRole == ShipData.RoleName.colony ? 0 : -1;
                            break;
                        }
                    default:
                        break;
                }
            }
            else
            {
                if (Owner.MaxSTLSpeed > targetPrefs.Speed)
                {
                    targetValue += target.DesignRoleType == ShipData.RoleType.Troop ? 1 : 0;
                    targetValue += target.DesignRole == ShipData.RoleName.colony ? 1 : 0;
                    targetValue += target.AI.State == AIState.Bombard ? 1 : 0;
                }
            }

            return targetValue;
        }

        public void SetCombatTactics(CombatState combatState)
        {
            if (CurrentCombatStance != combatState)
            {
                CurrentCombatStance = combatState;
                CombatTactic = null;
                Owner.shipStatusChanged = true; // FIX: force DesiredCombatRange update
            }

            if (CombatTactic == null)
            {
                switch (combatState)
                {
                    case CombatState.Artillery:
                        CombatTactic = new CombatTactics.Artillery(Owner.AI);
                        break;
                    case CombatState.BroadsideLeft:
                        CombatTactic = new CombatTactics.BroadSides(Owner.AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.BroadsideRight:
                        CombatTactic = new CombatTactics.BroadSides(Owner.AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.OrbitLeft:
                        CombatTactic = new CombatTactics.OrbitTarget(Owner.AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.OrbitRight:
                        CombatTactic = new CombatTactics.OrbitTarget(Owner.AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.AttackRuns:
                        CombatTactic = new CombatTactics.AttackRun(Owner.AI);
                        break;
                    case CombatState.HoldPosition:
                        CombatTactic = new CombatTactics.HoldPosition(Owner.AI);
                        break;
                    case CombatState.Evade:
                        CombatTactic = new CombatTactics.Evade(Owner.AI);
                        break;
                    case CombatState.AssaultShip:
                        CombatTactic = new CombatTactics.AssaultShipCombat(Owner.AI);
                        break;
                    case CombatState.OrbitalDefense:
                        break;
                    case CombatState.ShortRange:
                        CombatTactic = new CombatTactics.Artillery(Owner.AI);
                        break;
                }

            }
        }

        public void ExecuteCombatTactic(FixedSimTime timeStep) => CombatTactic?.Execute(timeStep, null);

    }
}