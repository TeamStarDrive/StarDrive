namespace Ship_Game.AI.CombatTactics
{
    public abstract class CombatMovement
    {
        float ErrorAdjustTimer;
        float RandomDirectionTimer;
        float RandomNozzleDirection;
        float InitialPhaseTimer;
        float InitialPhaseDirection;

        public enum CombatMoveState
        {
            Disengage,
            Hold,
            Approach,
            Charge
        }

        public enum CombatMoveType
        {
            Face,
            Retrograde,
            Flank,
            Oblique,
            AttackRun,
            OverRun,
            Feint,
            Taunt
        }

        protected CombatMovement(float errorAdjustTimer, float randomDirectionTime, float initialPhaseTimer)
        {

            InitialPhaseDirection = RandomMath.RollDice(50) ? -1f : +1f;
            RandomNozzleDirection = RandomMath.RandomBetween(-0.5f, +0.5f);
            ErrorAdjustTimer = errorAdjustTimer;
            RandomDirectionTimer = randomDirectionTime;
            InitialPhaseTimer = initialPhaseTimer;
        }

        public abstract void Execute();

        public abstract void UpdateTimers(float elapsedTime);
    }
}