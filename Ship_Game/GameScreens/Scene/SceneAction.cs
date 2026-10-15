namespace Ship_Game.GameScreens.Scene
{
    public abstract class SceneAction
    {
        // warp lengthens the plume, and the end of the spool scales it up too; heat stays at most 1
        public const float MovingThrusterHeat = 1f;
        public const float WarpThrusterLength = 3f;
        public const float SpoolThrusterScale = 4f;

        protected SceneObj Obj;
        protected readonly float Duration;
        protected float Time;
        protected float RelativeTime => Time / Duration;
        protected float Remaining => Duration - Time;

        protected SceneAction(float duration)
        {
            Duration = duration;
        }

        public virtual void Initialize(SceneObj obj)
        {
            Obj = obj;
            Time = 0f;
        }

        // @return TRUE if lifetime transition is over
        public virtual bool Update(FixedSimTime timeStep)
        {
            Time += timeStep.FixedTime;
            if (Time >= Duration)
            {
                Time = Duration;
                return true;
            }
            return false;
        }

        public virtual float ThrusterHeat => MovingThrusterHeat;
        public virtual float ThrusterLength => 1f;
        public virtual float ThrusterScale => 1f;

        public virtual void Draw()
        {
        }
    }
}
