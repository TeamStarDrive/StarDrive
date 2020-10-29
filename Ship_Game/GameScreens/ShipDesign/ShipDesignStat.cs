using System;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.GameScreens.ShipDesign
{
    public class ShipDesignStat : ScrollListItem<ShipDesignStat>
    {
        string Title;
        readonly Func<string> DynamicText;
        string GetText(UILabel label)
        {
            if (DynamicText != null)
                return DynamicText();
            return "";
        }
        public override int ItemHeight => IsHeader ? 40 : 16;
        public ShipDesignStat(string title, AggregatePerfTimer perfTimer, bool isHeader)
            : base(null)
        {
            Title = title;
            HeaderMaxWidth = 800;
            Init(title, 0, 80);
        }
        public ShipDesignStat(string title, Func<string> dynamicText)
        {
            DynamicText = dynamicText;
            Init(title, 0, 80);
        }
        public ShipDesignStat(string title, AggregatePerfTimer perfTimer, AggregatePerfTimer master)
        {
            Init(title, 10, 80);
        }
        void Init(string title, float titleX, float valueX)
        {
            Title = title;
            UILabel lblTitle = Add(new UILabel(title));
            UILabel lblValue = Add(new UILabel(GetText));
            lblTitle.SetRelPos(titleX, 0);
            lblValue.SetRelPos(valueX, 0);
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (IsHeader)
            {
                // dark blueish transparent background for Headers
                var edgeColor = new Color(75, 99, 125, 100);
                Color bkgColor = Hovered ? edgeColor : new Color(35, 59, 85, 50);
                new Selector(Rect, bkgColor, edgeColor).Draw(batch, elapsed);
            }
            base.Draw(batch, elapsed);
        }
    }

}