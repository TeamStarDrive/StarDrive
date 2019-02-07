using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    enum AnimPattern
    {
        None,
        Sine, // Generates a sine wave pattern on the animation percent
    }

    public class UIBasicAnimEffect : UIEffect
    {
        public float CurrentTime { get; private set; }
        public float EndTime     { get; private set; }
        public bool  Looping     { get; private set; }

        public float Delay       = 0f;
        public float Duration    = 1f;
        public float DurationIn  = 0.25f;
        public float DurationOut = 0.25f;

        public bool AnimateAlpha; // Are we animating ALPHA?
        public float MinAlpha = 0f; // min/max alpha [0.0 - 1.0] range
        public float MaxAlpha = 1f;

        // if TRUE, then original color will be animated instead
        // by default, animates from black to white, no alpha
        public bool AnimateColor;
        public Color MinColor = Microsoft.Xna.Framework.Graphics.Color.Black;
        public Color MaxColor = Microsoft.Xna.Framework.Graphics.Color.White;

        public bool GenerateSineWave;

        public UIBasicAnimEffect(UIElementV2 element) : base(element)
        {
        }

        /// <param name="delay">Start animation fadeIn/stay/fadeOut after seconds</param>
        /// <param name="duration">Duration of fadeIn/stay/fadeOut</param>
        /// <param name="fadeIn">Fade in time</param>
        /// <param name="fadeOut">Fade out time</param>
        public UIBasicAnimEffect Time(float delay, 
                                  float duration = 1f, 
                                  float fadeIn   = 0.25f, 
                                  float fadeOut  = 0.25f)
        {
            Delay    = delay;
            Duration = duration;
            DurationIn  = fadeIn;
            DurationOut = fadeOut;
            EndTime = delay + duration;
            return this;
        }

        // Always loop with loopTime
        public UIBasicAnimEffect Loop(float loopTime = 0f)
        {
            EndTime = loopTime > 0f ? loopTime : Delay + Duration;
            Looping = true;
            return this;
        }

        // a simplified loop animation, always starts with 0 delay
        public UIBasicAnimEffect Loop(float duration, float fadeIn, float fadeOut)
        {
            Delay = 0f;
            Duration = EndTime = duration;
            DurationIn = fadeIn;
            DurationOut = fadeOut;
            Looping = true;
            return this;
        }

        // enable animating alpha
        public UIBasicAnimEffect Alpha(float min=0f, float max=1f) // [0.0 - 1.0]
        {
            AnimateAlpha = true;
            MinAlpha = min;
            MaxAlpha = max;
            return this;
        }

        // just enable color fade-in / fade-out
        public UIBasicAnimEffect Color(Color minColor, Color maxColor)
        {
            AnimateColor = true;
            MinColor = minColor;
            MaxColor = maxColor;
            return this;
        }

        // Generates a sine wave pattern on the animation percent
        public UIBasicAnimEffect Sine()
        {
            GenerateSineWave = true;
            return this;
        }

        void UpdateAnimatedProperties(float ratio)
        {
            if (GenerateSineWave)
                ratio *= (float)Math.Sin(CurrentTime);

            Animation = ratio;

            if ((AnimateAlpha || AnimateColor) && Element is IColorElement ce)
            {
                Color color = ce.Color;
                if (AnimateAlpha)
                {
                    color = new Color(color, MinAlpha.LerpTo(MaxAlpha, ratio));
                }
                if (AnimateColor)
                {
                    color = new Color(MinColor.LerpTo(MaxColor, ratio), color.A);
                }
                ce.Color = color;
            }
        }

        public override bool Update(float deltaTime)
        {
            CurrentTime += deltaTime;
            if (CurrentTime > EndTime)
            {
                if (!Looping)
                    return true; // remove FX!
                CurrentTime -= EndTime; // wrap around
            }

            if (CurrentTime < Delay)
            {
                UpdateAnimatedProperties(0f);
                return false;
            }

            float anim = CurrentAnimPercent();
            UpdateAnimatedProperties(anim);
            return false;
        }


        float CurrentAnimPercent() // [0.0 - 1.0]
        {
            float relativeTime = CurrentTime - Delay;
            if (DurationIn > 0f)
            {
                if (relativeTime < DurationIn) // fading in?
                    return relativeTime / DurationIn;
            }
            if (DurationOut > 0f)
            {
                float fadeOutStart = Duration - DurationOut;
                if (relativeTime > fadeOutStart) // fading out?
                {
                    if (relativeTime >= Duration)
                        return 0f; // we've fully faded out
                    return 1f - (relativeTime - fadeOutStart)/DurationOut;
                }
            }
            return 1.0f; // stay
        }
    }
}
