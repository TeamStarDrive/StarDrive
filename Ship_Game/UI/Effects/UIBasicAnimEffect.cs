using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public enum AnimPattern
    {
        None,
        Sine, // Generates a sine wave pattern on the animation percent
        Cosine, // Cosine wave pattern
    }

    public class UIBasicAnimEffect : UIEffect
    {
        public float CurrentTime { get; private set; }
        public float EndTime     { get; private set; }
        public bool  Looping     { get; private set; }
        public bool  Started     { get; private set; }

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

        // if TRUE, then position 
        public bool AnimatePosition;
        public Vector2 StartPos;
        public Vector2 EndPos;

        public AnimPattern AnimPattern = AnimPattern.None;

        public bool SoundEffects;
        public string StartSfx;
        public string EndSfx;

        public UIBasicAnimEffect(UIElementV2 element) : base(element)
        {
        }

        /// <param name="delay">Start animation fadeIn/stay/fadeOut after seconds</param>
        /// <param name="duration">Duration of fadeIn/stay/fadeOut</param>
        /// <param name="fadeIn">Fade in time</param>
        /// <param name="fadeOut">Fade out time</param>
        ///
        /// Examples:
        ///   -- total time is 2 seconds
        ///   -- animation starts after 1.0s
        ///   -- there is a 0.25s fade in period, 0.5s stay period and 0.25s fade out period
        ///   Time(1.0f, 1.0f, 0.25f, 0.25f);
        ///
        ///   -- total time is 0.5s
        ///   -- entire animation consists of 0.5s fade-in
        ///   Time(0, 0.5f, 0.5f, 0);
        /// 
        ///   -- total time is 0.5s
        ///   -- entire animation consists of 0.5s fade-in
        ///   Time(0, 0.5f, 0, 0.5f);
        public UIBasicAnimEffect Time(float delay, 
                                      float duration = 1f, 
                                      float fadeIn   = 0.25f, 
                                      float fadeOut  = 0.25f)
        {
            Delay       = delay;
            Duration    = duration;
            DurationIn  = fadeIn;
            DurationOut = fadeOut;
            EndTime     = delay + duration;
            return this;
        }

        // Only fades in the animation, no fade out or stay
        public UIBasicAnimEffect FadeIn(float delay, float duration = 1f)
        {
            return Time(delay, duration, duration, 0);
        }

        // Only fades out the animation, no fade in or stay
        public UIBasicAnimEffect FadeOut(float delay, float duration = 1f)
        {
            return Time(delay, duration, 0, duration);
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
            Delay       = 0f;
            Duration    = EndTime = duration;
            DurationIn  = fadeIn;
            DurationOut = fadeOut;
            Looping     = true;
            return this;
        }

        // enable animating alpha
        public UIBasicAnimEffect Alpha(float min=0f, float max=1f) // [0.0 - 1.0]
        {
            AnimateAlpha = true;
            MinAlpha     = min;
            MaxAlpha     = max;
            return this;
        }

        // just enable color fade-in / fade-out
        public UIBasicAnimEffect Color(Color minColor, Color maxColor)
        {
            AnimateColor = true;
            MinColor     = minColor;
            MaxColor     = maxColor;
            return this;
        }

        // Generates a sine wave pattern on the animation percent
        public UIBasicAnimEffect Sine()
        {
            AnimPattern = AnimPattern.Sine;
            return this;
        }

        // Enable position transition animation
        // @note This will PERMANENTLY reposition the UIElement to @endPos
        public UIBasicAnimEffect Pos(Vector2 startPos, Vector2 endPos)
        {
            AnimatePosition = true;
            Element.Pos     = startPos;
            StartPos        = startPos;
            EndPos          = endPos;
            return this;
        }

        public UIBasicAnimEffect Sfx(string startSfx, string endSfx)
        {
            SoundEffects = true;
            StartSfx     = startSfx;
            EndSfx       = endSfx;
            return this;
        }

        void OnAnimationDelayed()
        {
            UpdateAnimatedProperties(0f);
        }
        
        void OnAnimationStart()
        {
            Started = true;
            if (SoundEffects && StartSfx.NotEmpty())
            {
                GameAudio.PlaySfxAsync(StartSfx);
            }
        }

        void OnAnimationProgress()
        {
            UpdateAnimatedProperties(CurrentAnimPercent());
        }

        void OnAnimationEnd()
        {
            UpdateAnimatedProperties(CurrentAnimPercent());

            if (SoundEffects && EndSfx.NotEmpty())
            {
                GameAudio.PlaySfxAsync(EndSfx);
            }
        }

        void UpdateAnimatedProperties(float ratio)
        {
            float relativeTime = CurrentTime - Delay;
            if (AnimPattern == AnimPattern.Sine)
            {
                ratio *= RadMath.Sin(relativeTime);
            }
            else if (AnimPattern == AnimPattern.Cosine)
            {
                ratio *= RadMath.Cos(relativeTime);
            }

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
            if (AnimatePosition)
            {
                Element.Pos = StartPos.LerpTo(EndPos, Animation);
            }
        }

        public override bool Update(float deltaTime)
        {
            CurrentTime += deltaTime;
            if (CurrentTime > EndTime)
            {
                OnAnimationEnd();
                Started = false;
                if (!Looping)
                    return true; // remove FX!
                CurrentTime -= EndTime; // wrap around
            }

            if (CurrentTime < Delay)
            {
                OnAnimationDelayed();
                return false;
            }

            if (!Started)
                OnAnimationStart();

            OnAnimationProgress();
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
