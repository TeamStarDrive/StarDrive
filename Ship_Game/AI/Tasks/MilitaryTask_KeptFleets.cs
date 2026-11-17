namespace Ship_Game.AI.Tasks
{
    public partial class MilitaryTask
    {
        // war and defense tasks keep their fleet when done; expansion forces break up
        public static bool UsesKeptFleets(TaskType type)
        {
            switch (type)
            {
                case TaskType.StrikeForce:
                case TaskType.AssaultPlanet:
                case TaskType.GlassPlanet:
                case TaskType.StageFleet:
                case TaskType.ReclaimPlanet:
                case TaskType.DefendPostInvasion:
                case TaskType.ClearAreaOfEnemies:
                case TaskType.DefendVsRemnants:
                case TaskType.AssaultPirateBase:
                case TaskType.InhibitorInvestigate: return true;
                default:                            return false;
            }
        }

        bool KeepsItsFleet => Fleet.Kept || UsesKeptFleets(Type);

        // a task ended again must not take back a fleet that has moved on to another task
        bool StillHoldsItsFleet => Fleet.FleetTask == this;
    }
}
