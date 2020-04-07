using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class PirateRaidTransport : Goal
    {
        public const string ID = "PirateRaidTransport";
        public override string UID => ID;
        private Pirates Pirates;

        public PirateRaidTransport() : base(GoalType.PirateRaidTransport)
        {
            Steps = new Func<GoalStep>[]
            {
               DetectAndSpawnRaidForce,
               CheckIfHijacked
            };
        }

        public PirateRaidTransport(Empire owner, Empire targetEmpire) : this()
        {
            empire       = owner;
            TargetEmpire = targetEmpire;

            PostInit();
            Log.Info(ConsoleColor.Green, $"---- Pirates: New {empire.Name} Transport Raid vs. {targetEmpire.Name} ----");
        }

        public sealed override void PostInit()
        {
            Pirates = empire.Pirates;
        }

        GoalStep DetectAndSpawnRaidForce()
        {
            if (Paid || Pirates.VictimIsDefeated(TargetEmpire))
                return GoalStep.GoalFailed; // They paid or dead

            int nearPlanetRaidChange = TargetEmpire.PirateThreatLevel * 5;
            if (RandomMath.RollDice(nearPlanetRaidChange))
            {
                if (ScanFreightersNearPlanets(out Ship freighter))
                {
                    Vector2 where = freighter.Center.GenerateRandomPointOnCircle(1000);
                    if (SpawnBoardingShip(freighter, where, out Ship boardingShip));
                    {
                        SpawnBoardingForce(freighter, boardingShip);
                        return GoalStep.GoToNextStep;
                    }
                }
            }
            else
            {
                if (ScanFreightersAtWarp(out Ship freighter))
                {
                    Vector2 where = freighter.Center.GenerateRandomPointOnCircle(1000);
                    freighter.HyperspaceReturn();
                    if (SpawnBoardingShip(freighter, where, out _))
                        return GoalStep.GoToNextStep;
                }
            }

            // Try locating viable freighters for maximum of 1 year (10 turns), else just give up
            return Empire.Universe.StarDate % 1 > 0 ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep CheckIfHijacked()
        {

            if (!TargetShip.Active || TargetShip.loyalty != Pirates.Owner && !TargetShip.Inhibited)
                return GoalStep.GoalFailed; // Target destroyed or escaped

            return TargetShip.loyalty == Pirates.Owner ? GoalStep.GoalComplete :  GoalStep.TryAgain;
        }

        bool ScanFreightersAtWarp(out Ship freighter)
        {
            freighter       = null;
            var targetShips = TargetEmpire.GetShips();
            for (int i = 0; i < targetShips.Count; i++)
            {
                Ship ship = targetShips[i];
                if (ship.IsFreighter && ship.AI.FindGoal(ShipAI.Plan.DropOffGoods, out _) 
                                     &&  ship.IsInWarp
                                     && !Pirates.RaidingThisShip(ship))
                {
                    freighter = ship;
                    break;
                }
            }

            return freighter != null;
        }

        bool ScanFreightersNearPlanets(out Ship freighter)
        {
            freighter       = null;
            var freighters  = new Array<Ship>();
            var systems     = TargetEmpire.GetOwnedSystems();

            for (int i = 0; i < systems.Count; i++)
            {
                SolarSystem system = systems[i];
                for (int j = 0; j < system.ShipList.Count; j++)
                {
                    Ship ship = system.ShipList[j];
                    if (ship.IsFreighter
                        && !ship.IsInWarp
                        && ship.AI.FindGoal(ShipAI.Plan.DropOffGoods, out _)
                        && !Pirates.RaidingThisShip(ship))
                    {
                        freighters.Add(ship);
                    }
                }
            }

            if (freighters.Count == 0)
                return false;

            freighter = freighters.RandItem();
            return freighter != null;
        }

        bool SpawnBoardingShip(Ship freighter, Vector2 where, out Ship boardingShip)
        {
            TargetShip = freighter; // This is the main target, we want this to arrive to our base
            if (Pirates.SpawnShip(PirateShipType.Boarding, where, out boardingShip))
                boardingShip.AI.OrderAttackSpecificTarget(freighter);

            return boardingShip != null;
        }

        void SpawnBoardingForce(Ship freighter, Ship boardingShip)
        {
            // Todo check for the target ally forces nearby and spawn escort ships 
        }

        bool Paid => !Pirates.GetRelations(TargetEmpire).AtWar;
    }
}