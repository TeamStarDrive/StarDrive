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
        public ShipDesignIssues DesignIssues;

        UIButton BtnDesignIssues;
        UIButton BtnInformation;


        public ShipDesignInfoPanel(ShipDesignScreen screen, in Rectangle rect) : base(rect)
        {
            Screen = screen;

            BtnDesignIssues = Add(new UIButton(ButtonStyle.Text));
            BtnDesignIssues.Pos = new Vector2(Rect.X, Rect.Y);
            BtnDesignIssues.RichText.AddText("D", Fonts.Pirulen20);
            BtnDesignIssues.RichText.AddText("esign Issues", Fonts.Pirulen16);
            BtnDesignIssues.DefaultTextColor = Color.Green;
            BtnDesignIssues.HoverTextColor = Color.White;

            BtnInformation = Add(new UIButton(ButtonStyle.Text));
            BtnInformation.Pos = new Vector2(Rect.X, Rect.Y);
            BtnInformation.RichText.AddText("I", Fonts.Pirulen20);
            BtnInformation.RichText.AddText("nformation", Fonts.Pirulen16);
            BtnInformation.DefaultTextColor = Color.Green;
            BtnInformation.HoverTextColor = Color.White;
        }

        public void SetActiveDesign(DesignShip ship)
        {
            S = ship;
            DesignIssues = new ShipDesignIssues(ship.shipData);
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }
    }
}
