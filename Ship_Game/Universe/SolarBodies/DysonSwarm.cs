using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Ship_Game.UniverseScreen;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class DysonSwarm
    {
        const int TotalSwarmControllers = 50;
        const int BaseRequiredSwarmSats = 100_000;
        public const string DysonSwarmLauncherTemplate = "DysonSwarmLauncher";
        public const string DysonSwarmControllerName = "Dyson Swarm Controller";

        readonly int RequiredSwarmSats;
        readonly byte SwarmType; // 1 or 2

        [StarData] public readonly Map<Vector2, Ship> SwarmControllers;
        [StarData] public readonly SolarSystem System;
        [StarData] public readonly float SwarmSatProductionCost;
        [StarData] public Empire Owner { get; private set; }
        [StarData] public float ControllerCompletion { get; private set; } // 0.0 to 1.0
        [StarData] public bool EnableOverclock { get; private set; }
        [StarData] public int MaxOverclock { get; private set; } = 50;
        [StarData] public int CurrentOverclock { get; private set; }
        [StarData] public int NumSwarmSats { get; private set; }
        [StarData] public float FertilityPercentLoss { get; private set; }
        Array<SunLayerState> DysonSwarmRings = [];

        public float PercentOverClocked => CurrentOverclock / (float)MaxOverclock;
        public bool AreControllersCompleted => ControllerCompletion.AlmostEqual(1);
        public float SwarmCompletion => NumSwarmSats / (float)RequiredSwarmSats; // 0.0 to 1.0
        public bool IsSwarmCompleted => SwarmCompletion.AlmostEqual(1);
        public float ProductionBoost => ControllerCompletion.UpperBound(SwarmCompletion)*100 + CurrentOverclock;
        public float ProductionNotAffectingDecay => ControllerCompletion.UpperBound(SwarmCompletion) * 100;
        public int NunSwarmControllersInTheWorks => System.PlanetList.Count(p => p.Owner == Owner && p.SwarmSatInTheWorks);
        public bool ShouldBuildMoreSwarmControllers => !AreControllersCompleted 
            && NunSwarmControllersInTheWorks + SwarmControllers.Values.Count(s => s != null) < TotalSwarmControllers;
        bool NeedDysonRingsChange => (int)(SwarmCompletion * 100) / 10 != DysonSwarmRings.Count;

        public DysonSwarm(SolarSystem system, Empire owner) 
        {
            Owner = owner;
            System = system;
            SwarmControllers = [];
            SwarmType = system.DysonSwarmType.LowerBound(1); // log error if type is 0
            RequiredSwarmSats = BaseRequiredSwarmSats / SwarmType;
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
            // todo check if owner is still eligible to have dyson swarm in this system


            int count = 0;
            foreach (KeyValuePair<Vector2, Ship> item in SwarmControllers)
            {
                Ship swarmController = item.Value;
                if (swarmController != null)
                {
                    if      (!swarmController.Active)          SwarmControllers[item.Key] = null;
                    else if (swarmController.Loyalty != Owner) swarmController.AI.OrderScuttleShip();
                    else                                       count++;
                }
            }

            ControllerCompletion = count / (float)TotalSwarmControllers;
            CurrentOverclock += ((EnableOverclock ? MaxOverclock : 0) - CurrentOverclock).Clamped(-1, 1);
            float completionLimit = ControllerCompletion.UpperBound(SwarmCompletion);
            CurrentOverclock = CurrentOverclock.UpperBound((int)(completionLimit * MaxOverclock));
            FertilityPercentLoss = SwarmCompletion * 0.25f + PercentOverClocked * 0.25f; // 0.0 to 0.5
            if (NeedDysonRingsChange)
                LoadDysonRings();
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
            int neededRings = (int)(SwarmCompletion * 100) / 10;
            lock (DysonSwarmRings) 
            {
                float startRotation = DysonSwarmRings.Count > 0 ? DysonSwarmRings[0].Sprite.Rotation : -1;
                DysonSwarmRings.Clear();
                if (neededRings == 0)
                    return;

                Array<SunLayerState> frontLayers = [];
                for (int i = 0; i < neededRings; i++) 
                {
                    if (i % 2 == 0)
                        DysonSwarmRings.Add(new SunLayerState(ResourceManager.RootContent, DysonRings.Rings[0], startRotation + i * 0.15f));
                    else
                        frontLayers.Add(new SunLayerState(ResourceManager.RootContent, DysonRings.Rings[1], startRotation + (i-1) * 0.15f));
                }

                DysonSwarmRings.AddRange(frontLayers);
            }
        }

        public void DrawDysonRings(SpriteBatch batch, Vector2 pos, float sizeScaleOnScreen)
        {
            if (DysonSwarmRings.Count == 0) 
                return;

            float alphaRange = ((int)UnivScreenState.PlanetView - (int)UnivScreenState.ShipView);
            float alpha = (float)((Owner.Universe.CamPos.Z - (double)(UnivScreenState.ShipView)) / alphaRange);
            alpha = alpha.Clamped(0, 1);
            if (alpha == 0)
                return;

            for (int i = 0; i < DysonSwarmRings.Count; i++)
            {
                SunLayerState ring = DysonSwarmRings[i];
                ring.Draw(batch, pos, sizeScaleOnScreen, alpha);
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
    }
}

