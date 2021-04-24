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

        UIList StatsList;
        float TitleWidth;
        float ValueWidth = 60;
        Graphics.Font StatsFont = Fonts.Arial12Bold;

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
            StatsList = AddList(Vector2.Zero);
            StatsList.SetRelPos(0, 18);
            StatsList.Width = Width;
            TitleWidth = Width - ValueWidth;

            Value(() => S.GetCost(), GameText.ProductionCost, GameText.IndicatesTheTotalProductionValue, Tint.GoodBad);
            //Value(() => S.GetMaintCost(), )

            Line();
        }

        void Value(Func<float> dynamicValue, LocalizedText title, LocalizedText tooltip, Tint tint)
        {
            var lbl = new UI.UIKeyValueLabel(title, "11.11k");
            lbl.Key.TextAlign = TextAlign.Right;
            lbl.Key.Width = TitleWidth;
            lbl.Separator = ":   ";
            lbl.Width = Width;
            lbl.Split = TitleWidth;
            lbl.DynamicValue = dynamicValue;
            lbl.Tooltip = tooltip;
            lbl.Key.Font = lbl.Value.Font = StatsFont;
            lbl.DynamicColor = GetColor(tint);
            //lbl.DebugDraw = true;
            StatsList.Add(lbl);
        }

        void Line()
        {
            StatsList.Add(new UILabel(" ", StatsFont));
        }

        enum Tint
        {
            None,
            Bad,
            GoodBad,
            BadLowerThan2,
            BadPLessThan1,
            CompareValue
        }

        Func<float, Color> GetColor(Tint tint, float compareValue = 0f)
        {
            return (float value) =>
            {
                switch (tint)
                {
                    case Tint.GoodBad:       return value > 0f ? Color.LightGreen : Color.LightPink;
                    case Tint.Bad:           return Color.LightPink;
                    case Tint.BadLowerThan2: return value > 2f ? Color.LightGreen : Color.LightPink;
                    case Tint.BadPLessThan1: return value > 1f ? Color.LightGreen : Color.LightPink;
                    case Tint.CompareValue:  return compareValue < value ? Color.LightGreen : Color.LightPink;
                    case Tint.None:
                    default: return Color.White;
                }
            };
        }

        public override void Update(float fixedDeltaTime)
        {
            if (!Visible)
                return;
            
            base.Update(fixedDeltaTime);
        }
    }
}
