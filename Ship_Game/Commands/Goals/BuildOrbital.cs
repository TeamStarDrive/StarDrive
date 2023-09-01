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
    class BuildOrbital : DeepSpaceBuildGoal
    {
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }

        [StarDataConstructor]
        public BuildOrbital(Empire owner) : base(GoalType.BuildOrbital, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                WaitForShipBuilt,
                OrderDeployOrbital,
                WaitForDeployment
            };
        }

        // The Orbital will be deployed on a same planet it is being built on
        public BuildOrbital(Planet planet, string toBuildName, Empire owner) : this(owner)
        {
            Initialize(toBuildName, Vector2.Zero, planet, Vector2.Zero);
            Setup(planet);
        }

        // The Orbital will be deployed on a different planet then the planet it is being built on
        public BuildOrbital(Planet planetBuildingAt, Planet targetPlanet, string toBuildName, 
            Empire owner, Vector2 dynamicBuildPos) : this(owner)
        {
            Initialize(toBuildName, Vector2.Zero, targetPlanet, 
                dynamicBuildPos == Vector2.Zero ? dynamicBuildPos : dynamicBuildPos - targetPlanet.Position);
            Setup(planetBuildingAt);
        }

        // The Orbital will be deployed with a specific target system (like researching Stars)
        public BuildOrbital(Planet planetBuildingAt, SolarSystem targetSystem, string toBuildName, Empire owner, Vector2 buildPos) : this(owner)
        {
            Initialize(toBuildName, buildPos, targetSystem);
            StaticBuildPos = buildPos;
            Setup(planetBuildingAt);
        }

        void Setup(Planet planetBuildingAt)
        {
            float structureCost = ToBuild.GetCost(Owner);
            PlanetBuildingAt = planetBuildingAt;
            IShipDesign constructor = BuildableShip.GetConstructor(Owner, TetherPlanet?.System ?? TargetSystem, ToBuild.GetCost(Owner));
            PlanetBuildingAt.Construction.Enqueue(ToBuild.IsResearchStation || ToBuild.IsShipyard 
                ? QueueItemType.OrbitalUrgent : QueueItemType.Orbital,
                ToBuild, constructor, structureCost, rush: false, this);
        }

        GoalStep OrderDeployOrbital()
        {
            if (!ConstructionShipOk)
                return GoalStep.GoalFailed; // Ship was removed or destroyed

            if (StaticBuildPos == Vector2.Zero && TetherOffset == Vector2.Zero)
                StaticBuildPos = FindNewOrbitalLocation();
            FinishedShip.AI.OrderDeepSpaceBuild(this, ToBuild.GetCost(Owner), ToBuild.Grid.Radius);
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForDeployment()
        {
            if (!ConstructionShipOk)
                return GoalStep.GoalComplete;

            if (FinishedShip.Construction.TryConstruct(BuildPosition) && FinishedShip.System != null)
                FinishedShip.System.TryLaunchBuilderShip(FinishedShip, Owner);

            return GoalStep.TryAgain;
        }

        Vector2 FindNewOrbitalLocation()
        {
            const int ringLimit = ShipBuilder.OrbitalsLimit / 9 + 1; // FB - limit on rings, based on Orbitals Limit
            for (int ring = 0; ring < ringLimit; ring++)
            {
                int degrees = (int)Owner.Random.Float(0f, 9f);
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
                Owner.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
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
                if (ship.AI.FindGoal(ShipAI.Plan.DeployOrbital, out ShipAI.ShipGoal g) &&
                    g.Goal is BuildOrbital bo && bo.TetherPlanet == TetherPlanet)
                {
                    Owner.Universe?.DebugWin?.DrawCircle(DebugModes.SpatialManager,
                        g.Goal.BuildPosition, 1000, Color.LightCyan, 10.0f);
                    if (position.InRadius(g.Goal.BuildPosition, 1000))
                        return true;
                }
            }

            return false;
        }

        bool ConstructionShipOk => FinishedShip?.Active == true;
    }
}