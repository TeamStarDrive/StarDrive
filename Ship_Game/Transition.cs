using Microsoft.Xna.Framework;
using System;

namespace Ship_Game
{
    public sealed class Transition
    {
        private float currentPosition;

        private Direction direction;

        private float speed;

        public Action OnTransitionEnd;

        public float CurrentPosition => MathHelper.Lerp(0f, 1f, this.timeLerp());

        public bool Finished
        {
            get
            {
                if (this.direction == Direction.Ascending)
                {
                    return this.currentPosition >= 1f;
                }
                return this.currentPosition <= 0f;
            }
        }

        public TransitionCurve InterpolationCurve { get; set; }

        public Transition(Direction direction, TransitionCurve transitionCurve, float transitionDuration)
        {
            this.currentPosition = 0f;
            this.InterpolationCurve = transitionCurve;
            this.Reset(direction, transitionDuration);
        }

        public void Reset(Direction direction, float transitionLength)
        {
            this.direction = direction;
            this.speed = 1f / transitionLength;
            this.Reset();
        }

        public void Reset(Direction direction)
        {
            this.direction = direction;
            this.Reset();
        }

        public void Reset()
        {
            if (this.direction == Direction.Ascending)
            {
                this.currentPosition = 0f;
                return;
            }
            this.currentPosition = 1f;
        }

        private float timeLerp()
        {
            if (this.direction != Direction.Ascending)
            {
                if (this.currentPosition < 0f)
                {
                    return 1f;
                }
                if (this.currentPosition > 1f)
                {
                    return 0f;
                }
            }
            else
            {
                if (this.currentPosition < 0f)
                {
                    return 0f;
                }
                if (this.currentPosition > 1f)
                {
                    return 1f;
                }
            }
            double timelerp = (double)this.currentPosition;
            switch (this.InterpolationCurve)
            {
                case TransitionCurve.Linear:
                {
                    return (float)timelerp;
                }
                case TransitionCurve.SmoothStep:
                {
                    timelerp = (double)((float)MathHelper.SmoothStep(0f, 1f, (float)timelerp));
                    return (float)timelerp;
                }
                case TransitionCurve.Exponential:
                {
                    timelerp = Math.Pow(timelerp, 2);
                    return (float)timelerp;
                }
                case TransitionCurve.Sqrt:
                {
                    timelerp = Math.Sqrt(timelerp);
                    return (float)timelerp;
                }
                default:
                {
                    return (float)timelerp;
                }
            }
        }

        public void Update(double elapsedTime)
        {
            float sign = direction == Direction.Ascending ? +1.0f : -1.0f;
            currentPosition += sign * speed * (float)elapsedTime;

            if (Finished)
            {
                OnTransitionEnd?.Invoke();
            }
        }
    }
}