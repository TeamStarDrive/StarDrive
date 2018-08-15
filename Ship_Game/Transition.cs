using System;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public sealed class Transition
    {
        private float currentPosition;

        private Direction direction;

        private float speed;

        public Action OnTransitionEnd;

        public float CurrentPosition => MathHelper.Lerp(0f, 1f, timeLerp());

        public bool Finished
        {
            get
            {
                if (direction == Direction.Ascending)
                {
                    return currentPosition >= 1f;
                }
                return currentPosition <= 0f;
            }
        }

        public TransitionCurve InterpolationCurve { get; set; }

        public Transition(Direction direction, TransitionCurve transitionCurve, float transitionDuration)
        {
            currentPosition = 0f;
            InterpolationCurve = transitionCurve;
            Reset(direction, transitionDuration);
        }

        public void Reset(Direction direction, float transitionLength)
        {
            this.direction = direction;
            speed = 1f / transitionLength;
            Reset();
        }

        public void Reset(Direction direction)
        {
            this.direction = direction;
            Reset();
        }

        public void Reset()
        {
            if (direction == Direction.Ascending)
            {
                currentPosition = 0f;
                return;
            }
            currentPosition = 1f;
        }

        private float timeLerp()
        {
            if (direction != Direction.Ascending)
            {
                if (currentPosition < 0f)
                {
                    return 1f;
                }
                if (currentPosition > 1f)
                {
                    return 0f;
                }
            }
            else
            {
                if (currentPosition < 0f)
                {
                    return 0f;
                }
                if (currentPosition > 1f)
                {
                    return 1f;
                }
            }
            double timelerp = currentPosition;
            switch (InterpolationCurve)
            {
                case TransitionCurve.Linear:
                {
                    return (float)timelerp;
                }
                case TransitionCurve.SmoothStep:
                {
                    timelerp = MathHelper.SmoothStep(0f, 1f, (float)timelerp);
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