using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class ConstructionShip  // Created by Fat Bastard in to better deal with consturction ships
    {
        public const float ConstructingDistance = 50;

        [StarData] public readonly float ConstructionNeeded;
        [StarData] readonly float  ConstructionPerTurn;
        [StarData] readonly float BuildRadius; // Approximate radius if the structre to be built
        [StarData] public float ConstructionAdded { get; private set; }
        [StarData] bool ConstructionStarted;
        [StarData] Ship Owner;
        ConstructionShip()
        {
        }

        ConstructionShip(Ship owner, float constructionNeeded, float buildRadius)
        {
            Owner = owner;
            ConstructionNeeded = constructionNeeded;
            int buildRate = GlobalStats.Defaults.ConstructionModuleBuildRate;
            ConstructionPerTurn = (buildRate * owner?.ShipData.NumConstructionModules ?? 0).LowerBound(buildRate);
            BuildRadius = buildRadius;
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
        }

        static readonly ConstructionShip None = new(null, 0, 0); // NIL object pattern

        public static ConstructionShip Create(Ship owner, float constructionNeeded, float buildRadius)
        {
            return owner.IsConstructor ? new ConstructionShip(owner, constructionNeeded,  buildRadius) : None;
        }

        public bool NeedBuilders => ConstructionNeeded - ConstructionAdded > BuilderShipConstructionAdded * 3;
        public float ActualConstructionPerTurn => ConstructionPerTurn * Owner?.Loyalty.data.Traits.ConstructionRateMultiplier ?? 1;
        int BuilderShipConstructionAdded => (int)(GlobalStats.Defaults.BuilderShipConstructionAdded 
                                            * Owner?.Loyalty.data.Traits.BuilderShipConstructionMultiplier ?? 1);

        public void AddConstructionFromBuilder()
        {
            if (Owner == null)
                return;

            ConstructionAdded = (ConstructionAdded + BuilderShipConstructionAdded).UpperBound(ConstructionNeeded);
        }

        void AddConstruction()
        {
            if (Owner == null)
                return;

            if (ConstructionStarted)
            {
                ConstructionAdded += ActualConstructionPerTurn;
            }
            else // Initial Construction
            {
                ConstructionAdded += (Owner.CargoSpaceMax + Owner.GetCost(Owner.Loyalty)*0.5f) * Owner.HealthPercent;
                ConstructionStarted = true;
            }

            ConstructionAdded = ConstructionAdded.UpperBound(ConstructionNeeded);
        }

        /// <summary>
        /// Will retrun false is not close enough or construction is completed
        /// </summary>
        /// <param name="buildPos"></param>
        /// <returns></returns>
        public bool TryConstruct(Vector2 buildPos)
        {
            if (Owner == null)
                return false;

            if (Owner.Position.InRadius(buildPos, ConstructingDistance))
            {
                AddConstruction();
                return !ConsturctionCompleted;
            }

            return false;
        }

        public void AddConstructionEffects()
        {
            if (Owner == null || !ConstructionStarted)
                return;

            float percentCompleted = ConstructionPercentage;
            var universe = Owner.Universe;
            if (Owner.InFrustum && universe.IsShipViewOrCloser)
            {
                // visualize construction efforts
                Vector3 center = RandomPoint();
                for (int j = 0; j < percentCompleted; j++)
                    universe.Screen.Particles.BlueSparks.AddParticle(center);

                if (percentCompleted > 25)
                {
                    center = RandomPoint();
                    for (int j = 0; j < percentCompleted * 0.2; j++)
                        universe.Screen.Particles.Sparks.AddParticle(center);
                }

                if (percentCompleted > 50 && universe.Random.RollDice(percentCompleted))
                {
                    center = RandomPoint();
                    for (int j = 0; j < percentCompleted * 0.01; j++)
                        universe.Screen.Particles.Lightning.AddParticle(center);
                }

                if (percentCompleted > 75 && universe.Random.RollDice(percentCompleted * 0.3f))
                {
                    center = RandomPoint();
                    universe.Screen.Particles.Flash.AddParticle(center);
                }

                if (percentCompleted > 90 && universe.Random.RollDice(10))
                {
                    SpaceJunk.SpawnJunk(universe, 1, Owner.Position, Vector2.Zero, Owner,
                        maxSize: percentCompleted*0.5f, ignite: false);
                }
            }

            Vector3 RandomPoint()
            {
                return Owner.Position.GenerateRandomPointInsideCircle(BuildRadius * percentCompleted * 0.01f, Owner.Universe.Random).ToVec3();
            }
        }

        float ConstructionPercentage => ConstructionAdded / ConstructionNeeded * 100;

        public bool ConsturctionCompleted => ConstructionNeeded == 0 ? true : ConstructionAdded / ConstructionNeeded == 1;

        public void Dispose()
        {
            Owner = null;
        }
    }
}
