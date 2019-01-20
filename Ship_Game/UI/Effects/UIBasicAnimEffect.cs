using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.UI.Effects
{
    public class UIBasicAnimEffect : UIEffect
    {
        public UIBasicAnimEffect(UIElementV2 element) : base(element)
        {
        }

        public override bool Update(float deltaTime)
        {

            return false;
        }
    }
}
