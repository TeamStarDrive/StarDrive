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
        public const int TotalSwarmSats = 50;
        [StarData] public readonly Map<Vector2, Ship> Swarm;
        [StarData] public readonly SolarSystem System;
        [StarData] public Empire Owner { get; private set; }
        [StarData] public float Completion { get; private set; } // 0.0 to 1.0
        [StarData] public bool Overclock { get; private set; }
        [StarData] public int MaxOverclock { get; private set; } = 50;
        [StarData] public int CurrentOverclock { get; private set; }
        [StarData] public float FertilityPercentLoss { get; private set; }

        public float PercentOverClocked => CurrentOverclock / (float)MaxOverclock;
        public bool IsCompleted => Completion.AlmostEqual(1);
        public float ProductionBoost => Completion*3 + CurrentOverclock/100f;
        public float SunRadiusMultiplier => 1 - FertilityPercentLoss;
        public int SwarmSatsInTheWorks => System.PlanetList.Count(p => p.Owner == Owner && p.SwarmSatInTheWorks);
        //public bool ShouldBuildMoreSwarmSats => !IsCompleted &&  intheworks and goals targeting build pos

        public DysonSwarm(SolarSystem system, Empire owner) 
        {
            Owner = owner;
            System = system;
            Swarm = new Map<Vector2, Ship>();
            Init();
        }


        void Init()
        {
            AddSwarmPositions(20, 20_000);
            AddSwarmPositions(15, 10_000);
            AddSwarmPositions(8, 5000);
            AddSwarmPositions(6, 2500);
            AddSwarmPositions(1, 0); // total of 50

            void AddSwarmPositions(int numPerRing, int distance)
            {
                float degrees = 360 / numPerRing;
                for (int i = 0; i < numPerRing; i++)
                {
                    var offset = MathExt.PointOnCircle(i * degrees, distance);
                    Swarm.Add(System.Position + offset, null);
                }
            }
        }

        public void Update() // Once per turn or when a new Dyson Swarm Sat is deployed
        {
            // todo check if owner is still eligible to have dyson swarm in this system


            int count = 0;
            foreach (KeyValuePair<Vector2, Ship> item in Swarm)
            {
                Ship swarmSat = item.Value;
                if (swarmSat != null)
                {
                    if      (!swarmSat.Active)          Swarm[item.Key] = null;
                    else if (swarmSat.Loyalty != Owner) swarmSat.AI.OrderScuttleShip();
                    else                                count++;
                }
            }

            Completion = count / TotalSwarmSats;
            CurrentOverclock += ((Overclock ? MaxOverclock : 0) - CurrentOverclock).Clamped(-1, 1);
            FertilityPercentLoss = Completion * 0.25f + PercentOverClocked * 0.25f; // 0.0 to 0.5
        }

        public void KillSwarm()
        {
            float scuttleTimer = 1;
            foreach (Ship swarmSat in Swarm.Values)
            {
                if (swarmSat?.Active == true) 
                {
                    swarmSat.ScuttleTimer = scuttleTimer;
                    scuttleTimer += 0.25f;
                }
            }
        }
    }
}

