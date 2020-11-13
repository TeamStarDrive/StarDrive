using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.AI.ShipMovement;
using System;
using static Ship_Game.AI.ShipAI;
using static Ship_Game.AI.ShipAI.TargetParameterTotals;
using SynapseGaming.LightingSystem.Shadows;

namespace Ship_Game.AI
{
    public sealed class CombatAI
    {
        public float VultureWeight = 0.5f;
        public float SelfDefenseWeight = 0.5f;
        public float SmallAttackWeight;
        public float MediumAttackWeight;
        public float LargeAttackWeight;
        public float PirateWeight;
        private float AssistWeight = 0.5f;
        public Ship Owner;
        ShipAIPlan CombatTactic;
        CombatState CurrentCombatStance;

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
            // standard ship targeting:
            // within max weapons range
            // within desired range
            // pirate scavenging
            // Size desire / hit chance
            // speed / turnrate difference
            // damaged by

            // Target of opportunity
            // target is threat. 
            // target is objective

            Vector2 center = Owner.fleet != null ? Owner.fleet.AveragePosition() : Owner.AI.FriendliesSwarmCenter;
            Ship target            = weight.Ship;
            float theirDps         = target.TotalDps;
            float distanceToTarget = center.Distance(weight.Ship.Center).LowerBound(1);
            float errorRatio       = (target.Radius - Owner.MaxWeaponError) / target.Radius;
            bool inTheirRange      = distanceToTarget < target.WeaponsMaxRange;
            bool inOurRange        = distanceToTarget < Owner.WeaponsMaxRange;

            // more agile than us the less they are valued. 
            float turnRatio = 0;
            if (target.RotationRadiansPerSecond > 0 && Owner.RotationRadiansPerSecond > 0)
                turnRatio = (Owner.RotationRadiansPerSecond ) / target.RotationRadiansPerSecond;

            float stlRatio = 0;
            if (target.MaxSTLSpeed > 0)
                stlRatio = (Owner.MaxSTLSpeed / target.MaxSTLSpeed);

            float baseThreat = theirDps / targetPrefs.DPS.LowerBound(1);
            baseThreat += turnRatio;
            baseThreat += stlRatio;
            baseThreat += errorRatio;

            float weaponsRange = Owner.WeaponsMaxRange * 2;
            float targetValue = 0;

            if (inTheirRange || inOurRange)
            {
                targetValue = baseThreat;
                targetValue += weight.Ship.AI.Target == Owner ? 0.25f : 0;
                targetValue += weight.Ship == Owner.LastDamagedBy ? 0.25f : 0;
                targetValue += 0.25f;

            }
            else
            {
                targetValue += turnRatio + stlRatio + errorRatio;
                targetValue += (float)Math.Round((weaponsRange - distanceToTarget) / weaponsRange, 1);
            }

            float rangeToEnemyCenter = 0;
            Ship motherShip = Owner.Mothership ?? Owner.AI.EscortTarget;
            if (motherShip != null)
            {
                targetValue += target.AI.Target == motherShip ? 0.1f : 0;
                targetValue += motherShip.AI.Target == target ? 0.1f : 0;
                targetValue += motherShip.LastDamagedBy == target ? 0.25f : 0;
                targetValue += motherShip.Center.InRadius(target.Center, target.WeaponsMaxRange) ? 0.1f :0;
                rangeToEnemyCenter = motherShip.Center.SqDist(targetPrefs.Center) - motherShip.Center.SqDist(target.Center);
                float rangeValue = (float)Math.Round((rangeToEnemyCenter / motherShip.WeaponsMaxRange), 1);
                targetValue += rangeValue;
            }
            else
            {
                rangeToEnemyCenter = center.Distance(targetPrefs.Center);
                float rangeValue   = (float)Math.Round((1 - (rangeToEnemyCenter / Owner.WeaponsMaxRange)), 1);
                targetValue       += rangeValue;
            }
            
            targetValue += Owner.loyalty.WeArePirates && target.shipData.ShipCategory == ShipData.Category.Civilian ? 1 : 0;

            weight.SetWeight(targetValue);
            return weight;
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