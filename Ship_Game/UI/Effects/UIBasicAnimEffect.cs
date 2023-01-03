using System;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public enum AnimPattern
    {
        None,
        Sine, // Generates a sine wave pattern on the animation percent
        Cosine, // Cosine wave pattern
    }

    /// <summary>
    /// Contains a series of composable animation effects that can
    /// give patterned behaviors to UI Elements, such as pulsating color
    /// or transitioning in from outside of the screen
    /// </summary>
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
        public Range AlphaRange = new Range(0,1); // min/max alpha [0.0 - 1.0] range

        // if TRUE, then original color will be animated instead
        // by default, animates from black to white, no alpha
        public bool AnimateColor;
        public Color MinColor = Microsoft.Xna.Framework.Graphics.Color.Black;
        public Color MaxColor = Microsoft.Xna.Framework.Graphics.Color.White;

        // if TRUE, then position is being animated
        public bool AnimatePosition;
        public Vector2 StartPos;
        public Vector2 EndPos;
        
        // Are we animating SCALE
        public bool AnimateScale;
        public Range CenterScaleRange;
        
        // Are we animating SIZE
        public bool AnimateSize;
        public Vector2 StartSize;
        public Vector2 EndSize;

        public AnimPattern AnimPattern = AnimPattern.None;

        public bool SoundEffects;
        public string StartSfx;
        public string EndSfx;

        public UIBasicAnimEffect FollowedByAnim; // Added when this animation finishes
        public Action FollowedByAction; // Run when this animation finishes

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

        /// <summary>
        /// Only fades in the animation, no fade out or stay
        /// </summary>
        public UIBasicAnimEffect FadeIn(float delay, float duration = 1f)
        {
            return Time(delay, duration, duration, 0);
        }

        /// <summary>
        /// Only fades out the animation, no fade in or stay
        /// </summary>
        public UIBasicAnimEffect FadeOut(float delay, float duration = 1f)
        {
            return Time(delay, duration, 0, duration);
        }

        /// <summary>
        /// Always loop with loopTime
        /// </summary>
        public UIBasicAnimEffect Loop(float loopTime = 0f)
        {
            EndTime = loopTime > 0f ? loopTime : Delay + Duration;
            Looping = true;
            return this;
        }

        /// <summary>
        /// a simplified loop animation, always starts with 0 delay
        /// </summary>
        public UIBasicAnimEffect Loop(float duration, float fadeIn, float fadeOut)
        {
            Delay       = 0f;
            Duration    = EndTime = duration;
            DurationIn  = fadeIn;
            DurationOut = fadeOut;
            Looping     = true;
            return this;
        }

        /// <summary>
        /// enable animating alpha
        /// </summary>
        public UIBasicAnimEffect Alpha(Range alphaRange)
        {
            AnimateAlpha = true;
            AlphaRange = alphaRange;
            return this;
        }

        /// <summary>
        /// just enable color fade-in / fade-out
        /// </summary>
        public UIBasicAnimEffect Color(Color minColor, Color maxColor)
        {
            AnimateColor = true;
            MinColor     = minColor;
            MaxColor     = maxColor;
            return this;
        }

        /// <summary>
        /// Generates a sine wave pattern on the animation percent
        /// </summary>
        public UIBasicAnimEffect Sine()
        {
            AnimPattern = AnimPattern.Sine;
            return this;
        }

        /// <summary>
        /// Enable position transition animation
        /// @note This will PERMANENTLY reposition the UIElement to @endPos
        /// </summary>
        public UIBasicAnimEffect Pos(Vector2 startPos, Vector2 endPos)
        {
            AnimatePosition = true;
            Element.SetAutoPos(startPos.X, startPos.Y);
            StartPos = startPos;
            EndPos   = endPos;
            return this;
        }

        /// <summary>
        /// Enable size change animation
        /// @note The UIElement will default back to startSize after end of animation
        /// </summary>
        public UIBasicAnimEffect Size(Vector2 startSize, Vector2 endSize)
        {
            AnimateSize = true;
            Element.SetAutoSize(startSize.X, startSize.Y);
            StartSize = startSize;
            EndSize   = endSize;
            return this;
        }

        /// <summary>
        /// Enable scale change animation
        /// The UIElement will default back to min scale after end of animation
        /// WARNING: Size must be initialized for this UI element
        /// </summary>
        public UIBasicAnimEffect CenterScale(Range scaleRange)
        {
            AnimateScale = true;
            CenterScaleRange = scaleRange;
            StartPos = Element.GetAutoPos();
            StartSize = Element.GetAutoSize();
            if (StartSize.AlmostZero())
                Log.Error($"UIBasicAnimEffect.Scale {Element.Name} Element.Size={Element.Size} cannot be Zero!");
            return this;
        }

        /// <summary>
        /// Takes the position at the start of this anim sequence
        /// and generates a small bounce based on the magnitude given with parameter `bounce`
        /// </summary>
        /// <param name="bounce">The bounce magnitude and direction as a Vector</param>
        /// <param name="delay">Delay before the bounce</param>
        /// <param name="duration">Duration of the whole bounce, if this is bigger than fadeIn+fadeOut,
        /// then bounced object will "stay" at zenith for the additional duration</param>
        /// <param name="fadeIn">Duration of first bounce movement</param>
        /// <param name="fadeOut">Duration to return back to final position</param>
        /// <returns>Reference to the Bounce animation</returns>
        public UIBasicAnimEffect Bounce(Vector2 bounce,
                                        float delay = 0f, float duration = 0.4f, 
                                        float fadeIn = 0.1f, float fadeOut = 0.2f)
        {
            // in order to actually do the bounce, we need to chain a Then() action
            // because the Position at the start of this sequence is not guaranteed to be final yet
            // (there might be other animations in the works)
            return ThenAnim(anim => anim.Time(delay, duration, fadeIn, fadeOut)
                                        .Pos(Element.Pos, Element.Pos+bounce));
        }

        /// <summary>
        /// Plays a sound effect at start or end of animation
        /// </summary>
        public UIBasicAnimEffect Sfx(string startSfx = null, string endSfx = null)
        {
            SoundEffects = startSfx != null || endSfx != null;
            StartSfx = startSfx;
            EndSfx = endSfx;
            return this;
        }

        /// <summary>
        /// After this animation ends, Add this new animation to the Parent UIElementV2.
        /// This can be used to implement a bounce, after an UI Element has finished sliding in.
        /// Doesn't have any effect on looping effects, since they never finish.
        /// </summary>
        public UIBasicAnimEffect ThenAnim()
        {
            FollowedByAnim = new(Element);
            return FollowedByAnim;
        }

        /// <summary>
        /// Defers the anim definition to the time when this current animation finishes.
        /// Upon which, this `deferredAnimDefine` callback is called to generate the new animation.
        /// This cannot be used at the same time as `Then()`
        /// </summary>
        /// <returns>Reference to the Deferred animation</returns>
        public UIBasicAnimEffect ThenAnim(Action<UIBasicAnimEffect> deferredAnimDefine)
        {
            if (FollowedByAction != null) throw new InvalidOperationException("Then() function has already been called");
            
            UIBasicAnimEffect effect = new(Element);
            FollowedByAction = () =>
            {
                deferredAnimDefine(effect);
                Element.AddEffect(effect);
            };
            return effect;
        }

        /// <summary>
        /// After this animation ends, run this callback to perform additional tasks
        /// </summary>
        public void Then(Action action)
        {
            if (FollowedByAction != null) throw new InvalidOperationException("Then() function has already been called");
            FollowedByAction = action;
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

            if (FollowedByAnim != null)
            {
                Element.AddEffect(FollowedByAnim);
            }

            FollowedByAction?.Invoke();
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
                    color = new Color(color, AlphaRange.Min.LerpTo(AlphaRange.Max, ratio));
                }
                if (AnimateColor)
                {
                    color = new Color(MinColor.LerpTo(MaxColor, ratio), color.A);
                }
                ce.Color = color;
            }

            Vector2 pos = Element.GetAutoPos();
            Vector2 size = Element.GetAutoSize();
            Vector2 newPos = pos;
            Vector2 newSize = size;

            if (AnimatePosition)
            {
                newPos = StartPos.LerpTo(EndPos, Animation);
            }

            if (AnimateSize)
            {
                newSize = StartSize.LerpTo(EndSize, Animation);
            }

            if (AnimateScale)
            {
                float scale = CenterScaleRange.Min.LerpTo(CenterScaleRange.Max, Animation);
                Vector2 startSize = AnimateSize ? newSize : StartSize;
                newSize = startSize * scale;

                Vector2 startPos = AnimatePosition ? newPos : StartPos;
                Vector2 centerOffset = Element.LocalAxisOffset - new Vector2(0.5f, 0.5f);
                Vector2 offset = startSize * (1f - scale) * centerOffset;
                newPos = startPos - offset; // center to current
            }

            if (newPos.NotEqual(pos) || newSize.NotEqual(size))
            {
                Element.SetAutoPos(newPos.X, newPos.Y);
                Element.SetAutoSize(newSize.X, newSize.Y);
                Element.PerformLayout();
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

        public override string ToString()
        {
            var sb = new StringBuilder("UIEffect");
            sb.Append(" Delay:").Append(Delay);
            sb.Append(" Duration:").Append(Duration);
            sb.Append(" EndT:").Append(EndTime);
            sb.Append(" In:").Append(DurationIn);
            sb.Append(" Out:").Append(DurationOut);

            if (Looping)
                sb.Append(" Looping");

            if (AnimateAlpha)
                sb.Append(" Alpha:").Append(AlphaRange.Min).Append(",").Append(AlphaRange.Max);

            if (AnimateColor)
                sb.Append(" Color:").Append(MinColor).Append(",").Append(MaxColor);

            if (AnimatePosition)
                sb.Append(" Position:").Append(StartPos).Append(EndPos);

            if (AnimateScale)
                sb.Append(" CenterScale:").Append(CenterScaleRange.Min).Append(",").Append(CenterScaleRange.Max);

            if (AnimateSize)
                sb.Append(" Size:").Append(StartSize).Append(",").Append(EndSize);

            if (SoundEffects)
                sb.Append(" SFX:").Append(StartSfx).Append(",").Append(EndSfx);

            return sb.ToString();
        }
    }
}
