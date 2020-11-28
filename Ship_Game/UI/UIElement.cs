using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public abstract class UIElement
    {
        public enum ElementState
        {
            TransitionOn,
            Open,
            TransitionOff,
            Closed
        }

        public Rectangle ElementRect;

        public ScreenManager ScreenManager;

        public Color tColor = Colors.Cream;

        public bool IsExiting { get; protected set; }
        public ElementState State { get; protected set; } = ElementState.Closed;
        public TimeSpan TransitionOffTime { get; protected set; } = TimeSpan.Zero;
        public TimeSpan TransitionOnTime { get; protected set; } = TimeSpan.Zero;
        public float TransitionPosition { get; protected set; } = 1f;
        public byte TransitionAlpha => (byte)(255f - TransitionPosition * 255f);

        public abstract void Draw(SpriteBatch batch, DrawTimes elapsed);

        public virtual bool HandleInput(InputState input)
        {
            return false;
        }

        public virtual void Update(UpdateTimes elapsed)
        {
            if (State == ElementState.TransitionOn)
            {
                if (UpdateTransition(elapsed, TransitionOnTime, -1))
                {
                    State = ElementState.TransitionOn;
                    return;
                }
                State = ElementState.Open;
                return;
            }
            if (State == ElementState.TransitionOff)
            {
                if (UpdateTransition(elapsed, TransitionOffTime, 1))
                {
                    IsExiting = false;
                    return;
                }
                State = ElementState.Closed;
            }
        }

        bool UpdateTransition(UpdateTimes elapsed, TimeSpan time, int direction)
        {
            float transitionDelta = (time != TimeSpan.Zero ? (float)(elapsed.RealTime.Seconds / time.TotalSeconds) : 1f);
            TransitionPosition += transitionDelta * direction;
            if (TransitionPosition > 0f && TransitionPosition < 1f)
                return true;
            TransitionPosition = TransitionPosition.Clamped(0f, 1f);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawCircle(Vector2 center, float radius, Color color, float thickness = 1.0f)
            => ScreenManager.SpriteBatch.DrawCircle(center, radius, color, thickness);

    }
}