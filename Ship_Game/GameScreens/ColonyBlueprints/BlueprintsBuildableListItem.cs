using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Graphics;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public class BlueprintsBuildableListItem : ScrollListItem<BlueprintsBuildableListItem>
    {
        public readonly BlueprintsScreen Screen;
        public Building Building;
        string BuildingDescr;
        readonly SubTexture ProdIcon = ResourceManager.Texture("NewUI/icon_production");
        readonly SubTexture CostIcon = ResourceManager.Texture("UI/icon_money_22");
        readonly Font Font8 = Fonts.Arial8Bold;
        readonly Font Font12 = Fonts.Arial12Bold;
        readonly bool LowRes;

        public BlueprintsBuildableListItem(BlueprintsScreen screen, Building b)
        {
            Building = b;
            Screen   = screen;
            LowRes   = screen.LowRes;
        }

        public override bool HandleInput(InputState input)
        {
            return base.HandleInput(input);
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);
        }

        // Give a custom height for this scroll list item
        public override int ItemHeight => LowRes ? 32 : 42;

        float IconSize => LowRes ? 36 : 48;
        float ProdWidth => LowRes ? 90 : 120;
        float TextWidth => Width - IconSize - ProdWidth;
        float TextX => X + IconSize;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            DrawBuilding(batch, Building);
        }

        void DrawProductionInfo(SpriteBatch batch, float maintenance, float prod, int cost = 0)
        {
            Font font = LowRes ? Fonts.Arial10 : Font12;
            float x = Right - ProdWidth;
            float y = Y + 4;
            var iconSize = new Vector2(font.LineSpacing + 2);
            batch.Draw(ProdIcon, new Vector2(x, y), iconSize); // Production Icon
            batch.DrawString(font, prod.String(), x + iconSize.X + 2, y); // Build Production Cost

            string maintString = (-maintenance).String(2) + " BC/Y";
            float maintX = x + iconSize.X + 50;

            if (cost > 0)
            {
                batch.Draw(CostIcon, new Vector2(x, y + iconSize.Y + 5), iconSize); // Credits Icon
                batch.DrawString(font, cost.String(), x + iconSize.X + 2, y + iconSize.Y + 5); // Build Credit Cost
            }

            if (maintenance > 0f)
                batch.DrawString(Font8, maintString, maintX, y + iconSize.Y + 6, Color.DarkRed); // Maintenance

            if (maintenance < 0f)
                batch.DrawString(Font8, maintString, maintX, y + iconSize.Y + 6, Color.Green); // Credits per turn
        }

        void DrawBuilding(SpriteBatch batch, Building b)
        {
            Color buildColor = Hovered ? Color.Gold : Color.Gray;
            if (BuildingDescr == null)
                BuildingDescr = Font8.ParseText(b.GetShortDescrText(), TextWidth);

            batch.Draw(b.IconTex, new Vector2(X, Y - 2), new Vector2(IconSize), Screen.PlanAreaHovered && Hovered ? Color.Green : Color.White); // Icon
            batch.DrawString(Font12, b.TranslatedName.Text, TextX + 2, Y + 2, Color.White); // Title
            batch.DrawString(Font8, BuildingDescr, TextX + 4, Y + 16, Screen.PlanAreaHovered && Hovered ? Color.Green : buildColor); // Description
            int creditCost = b.IsMilitary ? GetCreditCharge((int)b.ActualCost) : 0;
            DrawProductionInfo(batch, GetMaintenance(b), b.ActualCost, creditCost);
        }

        int GetCreditCharge(float cost)
        {
            return Screen.Player.EstimateCreditCost(cost);
        }

        float GetMaintenance(Building b) => b.ActualMaintenance(Screen.Player) - b.Income;
    }
}
