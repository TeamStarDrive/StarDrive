using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public class AssignLaborComponent : UIElementContainer
    {
        Planet Planet;
        ColonySliderGroup Sliders;
        Submenu Title;
        bool UseTitle;

        public AssignLaborComponent(Planet p, RectF rect, bool useTitleFrame) : base(rect)
        {
            Planet = p;
            UseTitle = useTitleFrame;

            Sliders = Add(new ColonySliderGroup(p, SlidersHousing, drawIcons: useTitleFrame)
            {
                OnSlidersChanged = OnSlidersChanged
            });

            if (useTitleFrame)
            {
                Title = Add(new Submenu(rect));
                Title.AddTab(title:GameText.AssignLabor);
            }

            RequiresLayout = true;
        }

        void OnSlidersChanged()
        {
            Planet.UpdateIncomes(false);
        }

        Rectangle SlidersHousing
        {
            get
            {
                int sliderX = (int)X + (UseTitle ? 60 : 10);
                int sliderY = (int)Y + 25;
                int sliderW = (Width * 0.6f).RoundTo10();
                int sliderH = (int)Height - 25;
                return new Rectangle(sliderX, sliderY, sliderW, sliderH);
            }
        }

        public override void PerformLayout()
        {
            if (Title != null) Title.Rect = Rect;
            Sliders.Rect = SlidersHousing;
            base.PerformLayout();
        }
    }
}
