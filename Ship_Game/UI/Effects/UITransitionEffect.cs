using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public class UITransitionEffect : UIEffect
    {
        private readonly float Modifier;
        private readonly float Direction;
        private float Transition; // 0f means the intended position, 1f means max distance

        public UITransitionEffect(UIElementV2 element, float modifier, bool transitionIn) : base(element)
        {
            Modifier  = modifier;
            Direction  = transitionIn ? -1f : 1f;
            Offset     = transitionIn ? +1f : 0f;
            Transition = transitionIn ? +1f : 0f;
        }

        public override bool Update(ref Rectangle r, float effectSpeed)
        {
            Transition += effectSpeed * Direction * 0.016f;
            Offset = ((Transition - 0.5f*Modifier) / 0.5f).Clamped(0f, 1f);

            r.X += (int)(Offset * 512f);

            if (Direction < 0f && Offset.AlmostEqual(0f) ||
                Direction > 0f && Offset.AlmostEqual(1f))
            {
                GameAudio.PlaySfxAsync("blip_click"); // effect finished!
                return true;
            }
            return false;
        }
    }
}
