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
            //BtnDesignIssues = AddTextButton("Design Issues");
            //BtnInformation = AddTextButton("Information");
            // TODO:
            //DebugDraw = true;
            //BtnInformation.Visible = BtnDesignIssues.Visible = false;
        }

        UIButton AddTextButton(string text)
        {
            string firstLetter = text[0].ToString().ToUpper();
			string remaining = text.Remove(0, 1);

            var btn = Add(new UIButton(ButtonStyle.Text));
            btn.Pos = new Vector2(Rect.X, Rect.Y);
            btn.RichText.AddText(firstLetter, Fonts.Pirulen20);
            btn.RichText.AddText(remaining, Fonts.Pirulen16);
            btn.DefaultTextColor = Color.Green;
            btn.HoverTextColor = Color.White;
            btn.PressTextColor = Color.Green;
            btn.TextShadows = true;
            return btn;
        }

        public void SetActiveDesign(DesignShip ship)
        {
            S = ship;
            DesignIssues = new ShipDesignIssues(ship.shipData);
        }

        public override void Update(float fixedDeltaTime)
        {
            if (!Visible)
                return;

            
            // TODO:
            //BtnInformation.Visible = BtnDesignIssues.Visible = false;
            //switch (DesignIssues.CurrentWarningLevel)
            //{
            //    case WarningLevel.None:                                        break;
            //    case WarningLevel.Informative: BtnInformation.Visible = true;  break;
            //    default:                       BtnDesignIssues.Visible = true; break;
            //}
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }
    }
}
