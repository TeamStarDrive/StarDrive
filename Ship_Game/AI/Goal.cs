// Type: Ship_Game.Goal
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public enum GoalType
    {
        Colonize,
        DeepSpaceConstruction,
        BuildTroop,
        BuildShips,
        BuildScout,
        FleetRequisition,
    }

    public abstract class Goal
    {
        public Guid guid = Guid.NewGuid();
        public Empire empire;
        public GoalType type;
        public int Step;
        protected Fleet fleet;
        public Vector2 TetherOffset;
        public Guid TetherTarget;
        public bool Held;
        public Vector2 BuildPosition;
        public string ToBuildUID;
        protected Planet PlanetBuildingAt;
        protected Planet markedPlanet;
        public Ship beingBuilt;
        protected Ship colonyShip;
        protected Ship freighter;
        protected Ship passTran;

        public abstract string UID { get; }

        public override string ToString() => $"Goal.{UID} {ToBuildUID}";

        public static Goal CreateInstance(string uid)
        {
            switch (uid)
            {
                case BuildConstructionShip.ID:  return new BuildConstructionShip();
                case BuildDefensiveShips.ID:    return new BuildDefensiveShips();
                case BuildOffensiveShips.ID:    return new BuildOffensiveShips();
                case BuildScout.ID:             return new BuildScout();
                case BuildTroop.ID:             return new BuildTroop();
                case FleetRequisition.ID:       return new FleetRequisition();
                case IncreaseFreighters.ID:     return new IncreaseFreighters();
                case IncreasePassengerShips.ID: return new IncreasePassengerShips();
                case MarkForColonization.ID:    return new MarkForColonization();
                default: throw new ArgumentException($"Unrecognized Goal UID: {uid}");
            }
        }

        protected Goal(GoalType type)
        {
            this.type = type;
        }

        //public Goal(Planet toColonize, Empire e)
        //{
        //    empire = e;
        //    GoalName = "MarkForColonization";
        //    type = GoalType.Colonize;
        //    markedPlanet = toColonize;
        //    colonyShip = (Ship)null;
        //}

        //public Goal(string shipType, string forWhat, Empire e)
        //{
        //    ToBuildUID = shipType;
        //    empire = e;
        //    beingBuilt = ResourceManager.GetShipTemplate(shipType);
        //    GoalName = forWhat;
        //    this.Evaluate();
        //}

        //public Goal(GoalType goalType, string goalName, Empire empire)
        //{
        //    type = goalType;
        //    GoalName = goalName;
        //    this.empire = empire;
        //}

        //public Goal()
        //{
        //}

        //public Goal(Empire e)
        //{
        //    empire = e;
        //}

        public void SetFleet(Fleet f)
        {
            fleet = f;
        }

        public Fleet GetFleet()
        {
            return fleet;
        }

        // Each subclass must implement this behaviour:
        //
        // if (Held)
        //     return;
        // 
        public abstract void Evaluate();

        public Planet GetPlanetWhereBuilding()
        {
            return PlanetBuildingAt;
        }

        public void SetColonyShip(Ship s)
        {
            colonyShip = s;
        }

        public void SetPlanetWhereBuilding(Planet p)
        {
            PlanetBuildingAt = p;
        }

        public void SetBeingBuilt(Ship s)
        {
            beingBuilt = s;
        }

        public void SetMarkedPlanet(Planet p)
        {
            markedPlanet = p;
        }

        public void ReportShipComplete(Ship ship)
        {
            beingBuilt = ship;
            ++Step;
        }

        public Ship GetColonyShip()
        {
            return this.colonyShip;
        }

        public Planet GetMarkedPlanet()
        {
            return this.markedPlanet;
        }

        public struct PlanetRanker
        {
            public Planet Planet;
            public float Value;
            public float Distance;
            public float JumpRange;
            public bool OutOfRange;
        }
    }
}
