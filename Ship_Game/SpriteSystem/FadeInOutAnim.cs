using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class FadeInOutAnim : UIElementV2
    {
        readonly SubTexture Texture;
        public float CurrentTime { get; private set; }
        public float EndTime     { get; private set; }
        public bool  Looping     { get; private set; }
        public float Delay       = 0f;
        public float Duration    = 1f;
        public float DurationIn  = 0.25f;
        public float DurationOut = 0.25f;

        public float MinAlpha = 0f; // min/max alpha [0.0 - 1.0] range
        public float MaxAlpha = 1f;

        // if TRUE, then instead of varying alpha, main RGB channel intensity
        // will be animated instead
        public bool ColorNotAlpha = false;

        public FadeInOutAnim(UIElementV2 parent, string textureName, int x, int y)
                           : base(parent, new Vector2(x,y))
        {
            Texture = ((GameScreen)parent).TransientContent
                    .Load<SubTexture>("Textures/"+textureName);
            Size = Texture.SizeF;
        }

        /// <param name="delay">Start animation fadeIn/stay/fadeOut after seconds</param>
        /// <param name="duration">Duration of fadeIn/stay/fadeOut</param>
        /// <param name="fadeIn">Fade in time</param>
        /// <param name="fadeOut">Fade out time</param>
        /// <param name="loop">Looping time of the animation</param>
        public FadeInOutAnim Time(float delay, 
                                  float duration = 1f, 
                                  float fadeIn   = 0.25f, 
                                  float fadeOut  = 0.25f, 
                                  float loop     = 0f)
        {
            Delay    = delay;
            Duration = duration;
            DurationIn  = fadeIn;
            DurationOut = fadeOut;
            EndTime = loop > 0f ? loop : delay + duration;
            Looping = loop > 0f;
            return this;
        }

        // a simplified loop animation, always starts with 0 delay
        public FadeInOutAnim Loop(float duration, float fadeIn, float fadeOut)
        {
            Delay = 0f;
            Duration = EndTime = duration;
            DurationIn = fadeIn;
            DurationOut = fadeOut;
            Looping = true;
            return this;
        }

        public FadeInOutAnim Alpha(float min, float max) // [0.0 - 1.0]
        {
            MinAlpha = min;
            MaxAlpha = max;
            return this;
        }

        public FadeInOutAnim AnimateColor()
        {
            ColorNotAlpha = true;
            return this;
        }

        public override void Update(float deltaTime)
        {
            CurrentTime += deltaTime;
            if (CurrentTime > EndTime)
            {
                CurrentTime = Looping ? 0f : EndTime;
            }
            base.Update(deltaTime);
        }

        float CurrentFadeValue() // [0.0 - 1.0]
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
                    return 1f - (relativeTime - fadeOutStart)/DurationOut;
            }
            return 1.0f; // stay
        }

        public override void Draw(SpriteBatch batch)
        {
            if (CurrentTime < Delay || EndTime < CurrentTime)
                return;

            float alpha = CurrentFadeValue();
            float range = MaxAlpha - MinAlpha;
            float value = (MinAlpha + alpha*range).Clamped(0f, 1f);

            Color color = ColorNotAlpha
                ? new Color(value, value, value, 1.0f)
                : new Color(1.0f, 1.0f, 1.0f, value);

            batch.Draw(Texture, Rect, color);
        }

        public override bool HandleInput(InputState input)
        {
            return false; // no input stuff here
        }
    }
}
