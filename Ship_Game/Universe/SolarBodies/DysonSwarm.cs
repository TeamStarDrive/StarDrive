using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Universe.SolarBodies
{
    [StarDataType]
    public class DysonSwarm
    {
        public const int TotalSwarmControllers = 50;
        [StarData] public readonly Map<Vector2, Ship> Swarm;
        [StarData] public readonly SolarSystem System;
        [StarData] public readonly float SwarmSatProductionCost;
        [StarData] public Empire Owner { get; private set; }
        [StarData] public float ControllerCompletion { get; private set; } // 0.0 to 1.0
        [StarData] public float SwarmCompletion { get; private set; } // 0.0 to 1.0, cannot be reduced unless owner decides to.
        [StarData] public bool Overclock { get; private set; }
        [StarData] public int MaxOverclock { get; private set; } = 50;
        [StarData] public int CurrentOverclock { get; private set; }
        [StarData] public float FertilityPercentLoss { get; private set; }

        public float PercentOverClocked => CurrentOverclock / (float)MaxOverclock;
        public bool AreControllersCompleted => ControllerCompletion.AlmostEqual(1);
        public bool IsSwarmCompleted => SwarmCompletion.AlmostEqual(1);
        public float ProductionBoost => ControllerCompletion.UpperBound(SwarmCompletion)*100 + CurrentOverclock;
        public float SunRadiusMultiplier => 1 - FertilityPercentLoss;
        public int SwarmControllersInTheWorks => System.PlanetList.Count(p => p.Owner == Owner && p.SwarmSatInTheWorks);
        //public bool ShouldBuildMoreSwarmSats => !IsCompleted &&  intheworks and goals targeting build pos

        public DysonSwarm(SolarSystem system, Empire owner) 
        {
            Owner = owner;
            System = system;
            Swarm = [];
            SwarmSatProductionCost = owner.Universe.ProductionPace;
            Init();
        }

        void Init()
        {
            AddControllerPositions(20, 20_000);
            AddControllerPositions(15, 10_000);
            AddControllerPositions(8, 5000);
            AddControllerPositions(6, 2500);
            AddControllerPositions(1, 0); // total of 50

            void AddControllerPositions(int numPerRing, int distance)
            {
                float degrees = 360 / numPerRing;
                for (int i = 0; i < numPerRing; i++)
                {
                    var offset = MathExt.PointOnCircle(i * degrees, distance);
                    Swarm.Add(System.Position + offset, null);
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
            foreach (KeyValuePair<Vector2, Ship> item in Swarm)
            {
                Ship swarmController = item.Value;
                if (swarmController != null)
                {
                    if      (!swarmController.Active)          Swarm[item.Key] = null;
                    else if (swarmController.Loyalty != Owner) swarmController.AI.OrderScuttleShip();
                    else                                       count++;
                }
            }

            ControllerCompletion = count / TotalSwarmControllers;
            CurrentOverclock += ((Overclock ? MaxOverclock : 0) - CurrentOverclock).Clamped(-1, 1);
            FertilityPercentLoss = SwarmCompletion * 0.25f + PercentOverClocked * 0.25f; // 0.0 to 0.5
        }

        public bool TryGetRandomControllerTarget(out Ship controller)
        {
            controller = null;
            var controllers = Swarm.Values.Filter(s => s != null);
            if (controllers.Length > 0)
                controller = Owner.Random.Item(controllers);

            return controller != null;
        }

        public void KillSwarm()
        {
            float scuttleTimer = 1;
            foreach (Ship swarmController in Swarm.Values)
            {
                if (swarmController?.Active == true) 
                {
                    swarmController.ScuttleTimer = scuttleTimer;
                    scuttleTimer += 0.25f;
                }
            }

            SwarmCompletion = 0;
        }
    }
}

