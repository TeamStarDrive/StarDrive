using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game
{
    public class BlueprintsChainListItem : ScrollListItem<BlueprintsChainListItem>
    {
        static SubTexture BlueprintsIcon = ResourceManager.Texture("NewUI/blueprints");
        string Info = "";
        readonly string BlueprintsName;
        readonly Color IconColor;

        public BlueprintsChainListItem(BlueprintsTemplate template)
        {
            BlueprintsName = template.Name;
            Info = template.Exclusive ? Localizer.Token(GameText.ExclusiveBlueprints) : "";
            IconColor = BlueprintsScreen.GetBlueprintsIconColor(template.ColonyType);
            if (template.ColonyType != Planet.ColonyType.Colony)
            {
                Info = Info.NotEmpty() ? $"{Info} | Switch to: {template.ColonyType}"
                                       : $"Switch to: {template.ColonyType}";
            }
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            float iconHeight = (int)(Height * 0.89f);
            float iconWidth = (int)BlueprintsIcon.GetWidthFromHeightAspect(iconHeight);
            batch.Draw(BlueprintsIcon, Pos, new Vector2(iconWidth, iconHeight), IconColor);

            var tCursor = new Vector2(X + 50f, Y);
            batch.DrawString(Fonts.Arial20Bold, BlueprintsName, tCursor, Color.Orange);

            tCursor.Y += Fonts.Arial20Bold.LineSpacing;
            batch.DrawString(Fonts.Arial12Bold, Info, tCursor, Color.White);
        }
    }
}
