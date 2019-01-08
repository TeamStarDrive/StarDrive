using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    class ColonySliderGroup : UIElementContainer
    {
        readonly ColonySlider[] Sliders = new ColonySlider[3];
        ColonySlider Food, Prod, Res;
        Planet P;

        public ColonySliderGroup(UIElementV2 parent, Rectangle rect) : base(parent, rect)
        {
        }

        public void Create(int x, int y, int width, int spacingY, bool drawIcons=true)
        {
            for (int i = 0; i < 3; ++i)
            {
                Sliders[i] = Add(new ColonySlider(this, (SliderType)i, null, x, y + (spacingY * (i+1)), width, drawIcons)
                {
                    OnSliderChange = OnSliderChange
                });
            }
            Food = Sliders[(int)SliderType.Food];
            Prod = Sliders[(int)SliderType.Prod];
            Res  = Sliders[(int)SliderType.Res];
        }

        public void UpdatePos(int x, int y)
        {
            int spacingY = (int)(Sliders[1].Y - Sliders[0].Y);
            for (int i = 0; i < 3; ++i)
            {
                Sliders[i].UpdatePos(x, y + spacingY * (i+1));
            }
        }

        public void SetPlanet(Planet p)
        {
            P = p;
            foreach (ColonySlider s in Sliders)
                s.P = p;
            if (p != null)
            {
                Food.IsDisabled = p.IsCybernetic;
            }
        }

        // solve 3-way slider change
        void OnSliderChange(ColonySlider a, float delta)
        {
            delta = (float)Math.Round(delta, 2); // round to ~0.01 precision
            if (delta.AlmostZero()) return; // only allow 0.01 increments

            ColonySlider b = Sliders.Find(s => s != a && !s.Locked); // always unlocked
            ColonySlider c = Sliders.Find(s => s != a && s != b);    // maybe locked

            if (c.Locked) // only one is locked, eaaasy and accurate
            {
                float max = (1f - c.Percent) / 2;
                a.Percent += delta.Clamped(-max, +max);
            }
            else // all 3 unlocked
            {
                // this approach avoids math related precision errors
                // + if we reach [0,1] boundary, we don't move other sliders
                float oldValue = a.Percent; 
                a.Percent = oldValue + delta;
                float halfDelta = (a.Percent - oldValue) / 2;
                b.Percent -= halfDelta;

                // now re-balance b and c, while `a` remains constant
                c.Percent = 1f - (a.Percent + b.Percent);
            }

            b.Percent = 1f - (a.Percent + c.Percent); // final re-balance


            float sum = Sliders.Sum(s => s.Percent);
            if (!sum.AlmostEqual(1f))
                Log.Warning($"ColonySlider bad sum {sum} ==> F:{Food.Percent} P:{Prod.Percent} R:{Res.Percent}");
        }

        public override bool HandleInput(InputState input)
        {
            if (P == null)
            {
                Log.Error("ColonySliderGroup Planet not initialized!");
                return false;
            }

            int numLocked = Sliders.Count(s => s.Locked);
            foreach (ColonySlider s in Sliders)
            {
                s.CanDrag = !s.Locked && numLocked <= 1;
            }

            Prod.IsCrippled = P.CrippledTurns > 0;
            Prod.IsInvasion = P.RecentCombat;

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);
        }
    }
}
