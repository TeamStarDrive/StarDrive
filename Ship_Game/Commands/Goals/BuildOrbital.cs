using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    class BuildOrbital : Goal
    {
        public const string ID = "BuildOrbital";
        public override string UID => ID;

        public BuildOrbital() : base(GoalType.BuildOrbital)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildConstructor,
                WaitForShipBuilt,
                OrderDeployOrbital,
                WaitForDeployment
            };
        }

        public BuildOrbital(Planet planet, string toBuildName, Empire owner) : this()
        {
            ToBuildUID       = toBuildName;
            PlanetBuildingAt = planet;
            empire           = owner;
            TetherTarget     = planet.guid;
            Evaluate();
        }

        GoalStep BuildConstructor()
        {
            if (PlanetBuildingAt.Owner != empire)
                return GoalStep.GoalFailed;

            if (!ResourceManager.GetShipTemplate(ToBuildUID, out Ship orbital))
            {
                Log.Error($"BuildOrbital: no orbital to build with uid={ToBuildUID ?? "null"}");
                return GoalStep.GoalFailed;
            }

            string constructorId = empire.data.ConstructorShip;
            if (!ResourceManager.GetShipTemplate(constructorId, out ShipToBuild))
            {
                if (!ResourceManager.GetShipTemplate(empire.data.DefaultConstructor, out ShipToBuild))
                {
                    Log.Error($"BuildOrbital: no construction ship with uid={constructorId}");
                    return GoalStep.GoalFailed;
                }
            }

            PlanetBuildingAt.Construction.Enqueue(orbital, ShipToBuild, this);
            return GoalStep.GoToNextStep;
        }

        GoalStep OrderDeployOrbital()
        {
            if (FinishedShip == null)
                return GoalStep.GoalFailed; // Ship was removed or destroyed

            BuildPosition              = FindNewOrbitalLocation();
            FinishedShip.IsConstructor = true;
            FinishedShip.AI.OrderDeepSpaceBuild(this);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            // FB - must keep this goal until the ship deployed it's structure.
            // If the goal is not kept, load game construction ships lose the empire goal and get stuck
            return FinishedShip == null ? GoalStep.GoalComplete : GoalStep.TryAgain;
        }

        Vector2 FindNewOrbitalLocation()
        {
            const int ringLimit = ShipBuilder.OrbitalsLimit / 9 + 1; // FB - limit on rings, based on Orbitals Limit
            //save game compatibility hack to make up for the missing tether target in save.
            //remove this later
            if (TetherTarget == Guid.Empty)
                TetherTarget = PlanetBuildingAt.guid;
            for (int ring = 0; ring < ringLimit; ring++)
            {
                int degrees    = (int)RandomMath.RandomBetween(0f, 9f);
                float distance = 2000 + (1000 * ring * GetTetherPlanet.Scale);
                TetherOffset    = MathExt.PointOnCircle(degrees * 40, distance);
                Vector2 pos = GetTetherPlanet.Center + TetherOffset;
                if (BuildPositionFree(pos))
                    return pos;

                for (int i = 0; i < 9; i++) // FB - 9 orbitals per ring
                {
                    TetherOffset = MathExt.PointOnCircle(i * 40, distance);
                    pos = GetTetherPlanet.Center + TetherOffset;
                    if (BuildPositionFree(pos))
                        return pos;
                }
            }

            return GetTetherPlanet.Center; // There is a limit on orbitals number
        }

        bool BuildPositionFree(Vector2 position)
        {
            return !IsOrbitalAlreadyPresentAt(position) && !IsOrbitalPlannedAt(position);
        }

        bool IsOrbitalAlreadyPresentAt(Vector2 position)
        {
            foreach (Ship orbital in GetTetherPlanet.OrbitalStations)
            {
                Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
                    orbital.Position, 1000, Color.LightCyan, 10.0f);
                if (position.InRadius(orbital.Position, 1000))
                    return true;
            }

            return false;
        }

        // Checks if a Construction Ship is due to deploy a structure at a point
        bool IsOrbitalPlannedAt(Vector2 position)
        {
            foreach (Ship ship in empire.GetShips().Filter(s => s.IsConstructor))
            {
                if (ship.AI.FindGoal(ShipAI.Plan.DeployOrbital, out ShipAI.ShipGoal g) && g.Goal.TetherTarget == TetherTarget)
                {
                    Empire.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
                        g.Goal.BuildPosition, 1000, Color.LightCyan, 10.0f);
                    if (position.InRadius(g.Goal.BuildPosition, 1000))
                        return true;
                }
            }

            return false;
        }
    }
}