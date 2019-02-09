using Microsoft.Xna.Framework;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class UITransitionEffect : UIEffect
    {
        readonly Vector2 Start;
        readonly Vector2 End;
        float DelayTimer;
        readonly float TransitionTime;

        public UITransitionEffect(UIElementV2 e, 
            Vector2 start, Vector2 end, float delay = 0f, float transitionTime = 1.0f) : base(e)
        {
            Start = start;
            End = end;
            DelayTimer = delay;
            TransitionTime = transitionTime;
        }

        public override bool Update(float deltaTime)
        {
            if (DelayTimer > 0f)
            {
                DelayTimer -= deltaTime;
                return false;
            }

            Animation = (Animation + (deltaTime / TransitionTime)).Clamped(0f, 1f);
            Element.Pos = Start.LerpTo(End, Animation);

            if (Element.Pos.AlmostEqual(End))
            {
                GameAudio.BlipClick(); // effect finished!

                return true;
            }
            return false;
        }
    }
}
