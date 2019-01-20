using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.UI.Effects
{
    public class UIFadeInEffect : UIEffect
    {
        float CurrentTime;
        readonly float FadeInTime;
        readonly float Delay;

        public float MinAlpha = 0f; // Alpha set before fade in
        public float MaxAlpha = 1f; // Alpha set at the end of fade-in

        public UIFadeInEffect(UIElementV2 element, float fadeInTime, float delay = 0f)
            : base(element)
        {
            FadeInTime = fadeInTime;
            Delay = delay;
        }

        static void SetAlphaRecursive(UIElementV2 e, byte alpha)
        {
            if (e is IColorElement c)
                c.Color = new Color(c.Color, alpha);
            if (!(e is UIElementContainer cont)) return;
            foreach (UIElementV2 child in cont.Children)
                SetAlphaRecursive(child, alpha);
        }

        public override bool Update(float deltaTime)
        {
            CurrentTime += deltaTime;
            if (CurrentTime < Delay)
            {
                SetAlphaRecursive(Element, (byte)(MinAlpha * 255f));
                return false;
            }

            float time = CurrentTime - Delay;
            Animation = (time / FadeInTime).Clamped(0f, 1f);
            if (time < FadeInTime)
            {
                SetAlphaRecursive(Element, (byte)(Animation * MaxAlpha * 255f));
                return false;
            }

            SetAlphaRecursive(Element, (byte)(MaxAlpha * 255f));
            return true;
        }
    }
}
