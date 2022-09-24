using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;


namespace Ship_Game.Commands.Goals  // Created by Fat Bastard
{
    [StarDataType]
    class BuildOrbital : Goal
    {
        [StarData] public IShipDesign ShipToBuild;

        [StarDataConstructor]
        public BuildOrbital(Empire owner) : base(GoalType.BuildOrbital, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildConstructor,
                WaitForShipBuilt,
                OrderDeployOrbital,
                WaitForDeployment
            };
        }

        public BuildOrbital(Planet planet, string toBuildName, Empire owner) : this(owner)
        {
            ToBuildUID = toBuildName;
            PlanetBuildingAt = planet;
            TetherPlanet = planet;
        }

        GoalStep BuildConstructor()
        {
            if (PlanetBuildingAt.Owner != Owner)
                return GoalStep.GoalFailed;

            if (!ResourceManager.GetShipTemplate(ToBuildUID, out Ship orbital))
            {
                Log.Error($"BuildOrbital: no orbital to build with uid={ToBuildUID ?? "null"}");
                return GoalStep.GoalFailed;
            }

            string constructorId = Owner.data.ConstructorShip;
            if (!ResourceManager.Ships.GetDesign(constructorId, out ShipToBuild))
            {
                if (!ResourceManager.Ships.GetDesign(Owner.data.DefaultConstructor, out ShipToBuild))
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

            BuildPosition = FindNewOrbitalLocation();
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
            for (int ring = 0; ring < ringLimit; ring++)
            {
                int degrees = (int)RandomMath.Float(0f, 9f);
                float distance = 2000 + (1000 * ring * TetherPlanet.Scale);
                TetherOffset = MathExt.PointOnCircle(degrees * 40, distance);
                Vector2 pos = TetherPlanet.Position + TetherOffset;
                if (BuildPositionFree(pos))
                    return pos;

                for (int i = 0; i < 9; i++) // FB - 9 orbitals per ring
                {
                    TetherOffset = MathExt.PointOnCircle(i * 40, distance);
                    pos = TetherPlanet.Position + TetherOffset;
                    if (BuildPositionFree(pos))
                        return pos;
                }
            }

            return TetherPlanet.Position; // There is a limit on orbitals number
        }

        bool BuildPositionFree(Vector2 position)
        {
            return !IsOrbitalAlreadyPresentAt(position) && !IsOrbitalPlannedAt(position);
        }

        bool IsOrbitalAlreadyPresentAt(Vector2 position)
        {
            foreach (Ship orbital in TetherPlanet.OrbitalStations)
            {
                Owner.Universum?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
                    orbital.Position, 1000, Color.LightCyan, 10.0f);
                if (position.InRadius(orbital.Position, 1000))
                    return true;
            }

            return false;
        }

        // Checks if a Construction Ship is due to deploy a structure at a point
        bool IsOrbitalPlannedAt(Vector2 position)
        {
            var ships = Owner.OwnedShips;
            foreach (Ship ship in ships.Filter(s => s.IsConstructor))
            {
                if (ship.AI.FindGoal(ShipAI.Plan.DeployOrbital, out ShipAI.ShipGoal g) && g.Goal.TetherPlanet == TetherPlanet)
                {
                    Owner.Universum?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
                        g.Goal.BuildPosition, 1000, Color.LightCyan, 10.0f);
                    if (position.InRadius(g.Goal.BuildPosition, 1000))
                        return true;
                }
            }

            return false;
        }
    }
}