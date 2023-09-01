using SDGraphics;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class ConstructionShip  // Created by Fat Bastard in to better deal with consturction ships
    {
        public const float ConstructingDistance = 50;

        [StarData] public readonly float ConstructionNeeded;
        [StarData] public float ConstructionAdded { get; private set; }
        [StarData] bool ConstructionStarted;
        [StarData] Ship Owner;

        ConstructionShip()
        {
        }

        ConstructionShip(Ship owner, float constructionNeeded)
        {
            Owner = owner;
            ConstructionNeeded = constructionNeeded;
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
        }

        static readonly ConstructionShip None = new(null, 0); // NIL object pattern

        public static ConstructionShip Create(Ship owner, float constructionNeeded)
        {
            return owner.IsConstructor ? new ConstructionShip(owner, constructionNeeded) : None;
        }

        public bool NeedBuilders => ConstructionNeeded - ConstructionAdded > GlobalStats.Defaults.BuilderShipConstructionAdded * 3;

        public void AddConstructionFromBuilder()
        {
            if (Owner == null)
                return;

            ConstructionAdded = (ConstructionAdded + GlobalStats.Defaults.BuilderShipConstructionAdded).UpperBound(ConstructionNeeded);
        }

        void AddConstruction()
        {
            if (Owner == null)
                return;

            if (ConstructionStarted)
            {
                ConstructionAdded += GlobalStats.Defaults.ConstructionModuleBuildRate;
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
            if (Owner.Position.InRadius(buildPos, ConstructingDistance))
            {
                AddConstruction();
                return !ConsturctionCompleted;
            }

            return false;
        }

        public bool ConsturctionCompleted => ConstructionNeeded == 0 ? true : ConstructionAdded / ConstructionNeeded == 1;

        public void Dispose()
        {
            Owner = null;
        }
    }
}
