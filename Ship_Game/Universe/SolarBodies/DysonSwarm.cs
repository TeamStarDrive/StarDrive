﻿using Microsoft.Xna.Framework.Graphics;////////
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using System.Collections.Generic;
using static Ship_Game.UniverseScreen;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class DysonSwarm
    {
        const int TotalSwarmControllers = 50;
        const int BaseRequiredSwarmSats = 30_000;
        public const int BaseSwarmProductionBoost = 100;
        public const string DysonSwarmLauncherTemplate = "DysonSwarmLauncher";
        public const string DysonSwarmControllerName = "Dyson Swarm Controller";

        Array<SunLayerState> DysonSwarmRings = [];

        [StarData] public readonly int RequiredSwarmSats;
        [StarData] readonly byte SwarmType; // 1 or 2

        [StarData] public readonly Map<Vector2, Ship> SwarmControllers;
        [StarData] public readonly SolarSystem System;
        [StarData] public readonly float SwarmSatProductionCost;
        [StarData] public Empire Owner { get; private set; }
        [StarData] public float ControllerCompletion { get; private set; } // 0.0 to 1.0
        [StarData] public int MaxOverclock { get; private set; }
        [StarData] public int CurrentOverclock { get; private set; }  
        [StarData] public int NumSwarmSats { get; private set; }
        [StarData] public bool OverclockEnabled { get; private set; }

        bool NeedDysonRingsChange       => (int)(SwarmCompletion * 100) / 10 != DysonSwarmRings.Count;
        bool AreControllersCompleted    => ControllerCompletion.AlmostEqual(1);
        public float PercentOverClocked => MaxOverclock != 0 ? CurrentOverclock / (float)MaxOverclock : 0;
        public float SwarmCompletion    => NumSwarmSats / (float)RequiredSwarmSats; // 0.0 to 1.0
        public bool IsSwarmCompleted    => SwarmCompletion.AlmostEqual(1);
        public bool IsCompleted         => IsSwarmCompleted && AreControllersCompleted;
        public float ProductionBoost    => ControllerCompletion.UpperBound(SwarmCompletion)* BaseSwarmProductionBoost + CurrentOverclock;
        public int MaxProductionBoost   => BaseSwarmProductionBoost + (OverclockEnabled ?  MaxOverclock : 0);
        public float ProductionNotAffectingDecay => ControllerCompletion.UpperBound(SwarmCompletion) * BaseSwarmProductionBoost;
        public int NunSwarmControllersInTheWorks => System.PlanetList.Count(p => p.Owner == Owner && p.SwarmSatInTheWorks);
        public bool ShouldBuildMoreSwarmControllers => !AreControllersCompleted 
            && NunSwarmControllersInTheWorks + SwarmControllers.Values.Count(s => s != null) < TotalSwarmControllers;

        static public LocalizedText DysonSwarmTypeTitle(byte swarmType) => 
            swarmType == 1 ? GameText.DysonSwarmType1 : GameText.DysonSwarmType2;
        static public int GetRequiredSwarmSats(byte swarmType) => BaseRequiredSwarmSats / swarmType.LowerBound(1);

        public DysonSwarm(SolarSystem system, Empire owner) 
        {
            Owner = owner;
            System = system;
            SwarmControllers = [];
            SwarmType = system.DysonSwarmType.LowerBound(1); // log error if type is 0
            RequiredSwarmSats = GetRequiredSwarmSats(SwarmType);
            SwarmSatProductionCost = owner.Universe.ProductionPace;
            if (SwarmType == 2)
                SwarmSatProductionCost *= 3;

            Init();
        }

        [StarDataConstructor]
        public DysonSwarm() { }

        void Init()
        {
            AddControllerPositions(24, 5000);
            AddControllerPositions(12, 3750);
            AddControllerPositions(9,  2500);
            AddControllerPositions(5,  1250);  // total of 50

            void AddControllerPositions(int numPerRing, int distance)
            {
                float degrees = 360 / numPerRing;
                for (int i = 0; i < numPerRing; i++)
                {
                    var offset = MathExt.PointOnCircle(i * degrees, distance);
                    SwarmControllers.Add(System.Position + offset, null);
                }
            }

            foreach (Planet p in System.PlanetList) 
            {
                if (p.Owner == Owner) 
                    p.SetDysonSwarmWeapon(loadWeapon: true);
            }
        }

        public void Update() // Once per turn or when a new Dyson Swarm Sat is deployed
        {
            int count = 0;
            List<Vector2> keysToUpdate = new List<Vector2>();
            foreach (KeyValuePair<Vector2, Ship> item in SwarmControllers)
            {
                Ship swarmController = item.Value;
                if (swarmController != null)
                {
                    if      (!swarmController.Active)           keysToUpdate.Add(item.Key);
                    else if (swarmController.Loyalty != Owner) swarmController.AI.OrderScuttleShip();
                    else                                       count++;
                }
            }

            for (int i = 0; i < keysToUpdate.Count; i++)
                SwarmControllers[keysToUpdate[i]] = null;

            UpdateMaxOverclock();
            ControllerCompletion = count / (float)TotalSwarmControllers;
            int desiredOverclock = OverclockEnabled ? MaxOverclock : 0;
            CurrentOverclock += (desiredOverclock - CurrentOverclock).Clamped(-1, 1);
            float completionLimit = ControllerCompletion.UpperBound(SwarmCompletion);
            CurrentOverclock = CurrentOverclock.UpperBound((int)(completionLimit * MaxOverclock));
            if (DysonSwarmRings.Count == 0)
                LoadDysonRings();
        }

        public void UpdateMaxOverclock()
        {
            MaxOverclock = Owner.data.Traits.DysonSwarmMaxOverclock;
        }

        public void KillSwarm()
        {
            float scuttleTimer = 1;
            foreach (Ship swarmController in SwarmControllers.Values)
            {
                if (swarmController?.Active == true) 
                {
                    swarmController.ScuttleTimer = scuttleTimer;
                    scuttleTimer += 0.25f;
                }
            }

            NumSwarmSats = 0;
        }

        public void DeploySwarmSat()
        {
            NumSwarmSats = (NumSwarmSats + 1).UpperBound(RequiredSwarmSats);
        }
      
        void LoadDysonRings()
        {
            lock (DysonSwarmRings) 
            {
                var dysonRings = DysonRings.GetDysonRings(SwarmType);
                for (int i = 0; i < dysonRings.Rings.Count; i++)
                {
                    DysonSwarmRings.Add(new SunLayerState(ResourceManager.RootContent, dysonRings.Rings[i], -1));
                }
            }
        }

        public void DrawDysonRings(SpriteBatch batch, Vector2 pos, float sizeScaleOnScreen, bool drawBackRings = false)
        {
            if (DysonSwarmRings.Count == 0) 
                return;

            float alphaRange = ((int)UnivScreenState.PlanetView - (int)UnivScreenState.ShipView);
            float zoomAlpha = (float)((Owner.Universe.CamPos.Z - (double)(UnivScreenState.ShipView)) / alphaRange);
            zoomAlpha = zoomAlpha.Clamped(0, 1);
            if (zoomAlpha == 0)
                return;

            for (int i = 0; i < (drawBackRings ? (DysonRings.NumBacktRings*2)-1 : DysonSwarmRings.Count); i++)
            {
                float alpha = DysonRings.GetRingAlpha(i, SwarmCompletion);
                if (alpha > 0)
                {
                    SunLayerState ring = DysonSwarmRings[i];
                    ring.Draw(batch, pos, sizeScaleOnScreen, alpha.UpperBound(zoomAlpha));
                }
            }
        }

        public void UpdateDysonRings(FixedSimTime timeStep)
        {
            for (int i = 0; i < DysonSwarmRings.Count; i++)
            {
                SunLayerState ring = DysonSwarmRings[i];
                ring.Update(timeStep);
            }
        }

        public bool TryGetAvailablePosForController(out Vector2 pos)
        {
            pos = Vector2.Zero;
            var currentGoals = Owner.AI.Goals.Filter(g => g.IsBuildingOrbitalFor(System) && g.ToBuild.Name == DysonSwarmControllerName);
            Array<Vector2> potentialVectors = [];
            foreach (KeyValuePair<Vector2, Ship> item in SwarmControllers)
            {
                if (item.Value == null && (currentGoals.Length == 0 || !currentGoals.Any(g=> g.BuildPosition.InRadius(item.Key, 25))))
                   potentialVectors.Add(item.Key);
            }

            if (potentialVectors.Count > 0)
                pos = Owner.Random.Item(potentialVectors);

            return pos != Vector2.Zero;
        }

        public bool TryConnectControllerToGrid(Ship controller)
        {
            foreach (KeyValuePair<Vector2, Ship> item in SwarmControllers)
            {
                if (controller.Position.InRadius(item.Key, 25))
                {
                    SwarmControllers[item.Key] = controller;
                    return true;
                }
            }

            return false;
        }

        public void SetOverclock(bool value)
        {
            OverclockEnabled = value;
            UpdateMaxOverclock();
        }
    }
}

