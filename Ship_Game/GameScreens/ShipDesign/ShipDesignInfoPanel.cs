using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.GameScreens.ShipDesign
{
    /// <summary>
    /// Displays STATS information of the currently active design
    /// </summary>
    public class ShipDesignInfoPanel : UIElementContainer
    {
        readonly ShipDesignScreen Screen;
        DesignShip S;
        ShipDesignStats Ds;
        public ShipDesignIssues DesignIssues;


        public ShipDesignInfoPanel(ShipDesignScreen screen, in Rectangle rect) : base(rect)
        {
            Screen = screen;
            DebugDraw = true;
        }

        public void SetActiveDesign(DesignShip ship)
        {
            S = ship;
            Ds = ship.DesignStats;
            Elements.Clear();
            CreateElements();
        }

        void CreateElements()
        {

        }

        public override void Update(float fixedDeltaTime)
        {
            if (!Visible)
                return;
            
            base.Update(fixedDeltaTime);
        }
    }
}
