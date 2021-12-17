namespace Ship_Game.GameScreens.Scene
{
    public abstract class SceneAction
    {
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

        public virtual void Draw()
        {
        }
    }
}
