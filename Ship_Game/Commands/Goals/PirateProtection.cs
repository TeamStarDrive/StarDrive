﻿using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateProtection : Goal
    {
        public const string ID = "PirateRaidProtection";
        public override string UID => ID;
        private Pirates Pirates;
        private Ship ShipToProtect;
        private Empire EmpireToProtect;

        public PirateProtection() : base(GoalType.PirateProtection)
        {
            Steps = new Func<GoalStep>[]
            {
               SpawnProtectionForce
,              CheckIfHijacked,
               ReturnTargetToOriginalOwner
            };
        }

        public PirateProtection(Empire owner, Empire targetEmpire, Ship targetShip) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;
            TargetShip   = targetShip;

            PostInit();
            Evaluate();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Protection for {targetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates         = empire.Pirates;
            ShipToProtect   = TargetShip;
            EmpireToProtect = TargetEmpire;
        }

        GoalStep SpawnProtectionForce()
        {
            if (!Pirates.PaidBy(TargetEmpire) || ShipToProtect == null || !ShipToProtect.Active)
                return GoalStep.GoalFailed; // They stopped the contract or the ship is dead

            Vector2 where = ShipToProtect.Center.GenerateRandomPointOnCircle(1000);
            if (Pirates.SpawnBoardingShip(ShipToProtect, where, out Ship boardingShip))
            {
                ShipToProtect.HyperspaceReturn();
                if (Pirates.SpawnForce(TargetShip, boardingShip.Center, 5000, out Array<Ship> force))
                   Pirates.OrderEscortShip(boardingShip, force);

                return GoalStep.GoToNextStep;
            }

            // Could not spawn required stuff for this goal
            return GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {
            if (TargetShip == null
                || !TargetShip.Active
                || TargetShip.loyalty != Pirates.Owner && !TargetShip.InCombat)
            {
                return GoalStep.GoalFailed; // Target or our forces were destroyed 
            }

            return TargetShip.loyalty == Pirates.Owner ? GoalStep.GoToNextStep : GoalStep.TryAgain;
        }

        GoalStep ReturnTargetToOriginalOwner()
        {
            if (TargetShip == null || !TargetShip.Active || TargetShip.loyalty != Pirates.Owner)
                return GoalStep.GoalFailed; // Target destroyed or they took it from us

            TargetShip.AI.OrderPirateFleeHome(signalRetreat: true); // Retreat our forces before returning the ship to the rightful owner
            TargetShip.DisengageExcessTroops(TargetShip.TroopCount);
            TargetShip.ChangeLoyalty(EmpireToProtect);
            TargetShip.AI.ClearOrders();
            if (EmpireToProtect == EmpireManager.Player)
                Empire.Universe.NotificationManager.AddWeProtectedYou(Pirates.Owner);

            return GoalStep.GoalComplete;
        }
    }
}