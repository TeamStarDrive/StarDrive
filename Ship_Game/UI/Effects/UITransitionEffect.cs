using Microsoft.Xna.Framework;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class UITransitionEffect : UIEffect
    {
        readonly float TransitionTime = 1.0f;
        readonly float Offset; // offset multiplier
        readonly float Direction;

        readonly Rectangle AnimStart;
        readonly Rectangle AnimEnd;

        public UITransitionEffect(UIElementV2 e, 
            float distance, float animOffset, float direction, float transitionTime = 1.0f) : base(e)
        {
            Offset = animOffset;
            Direction  = direction;
            Animation = direction < 0 ? +1f : 0f;
            TransitionTime = transitionTime;
            AnimStart = e.Rect;
            AnimEnd = AnimStart;
            AnimEnd.X += (int)distance;
        }

        public override bool Update(float deltaTime)
        {
            Animation += (Direction * deltaTime) / TransitionTime;
            float animWithOffset = ((Animation - 0.5f * Offset) / 0.5f).Clamped(0f, 1f);

            int dx = (AnimEnd.X - AnimStart.X);
            Element.X = AnimStart.X + animWithOffset * dx;

            if (Direction < 0f && animWithOffset.AlmostEqual(0f) ||
                Direction > 0f && animWithOffset.AlmostEqual(1f))
            {
                GameAudio.BlipClick(); // effect finished!

                return true;
            }
            return false;
        }
    }
}
