using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;


namespace Ship_Game
{
    public class ModuleSelectListItem : ScrollListItem<ModuleSelectListItem>
    {
        public ShipModule Module;
        public ModuleSelectListItem(string headerText) : base(headerText) {}
        public ModuleSelectListItem(ShipModule module) { Module = module; }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            if (Module != null)
            {
                DrawModule(batch, Module);
            }
        }

        void DrawModule(SpriteBatch batch, ShipModule mod)
        {
            var bCursor = new Vector2(List.X + 15, Y);
            SubTexture modTexture = mod.ModuleTexture;
            var modRect = new Rectangle((int)bCursor.X, (int)bCursor.Y, modTexture.Width, modTexture.Height);
            float aspectRatio = (float)modTexture.Width / modTexture.Height;
            float w = modRect.Width;
            float h;
            for (h = modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
            {
                w -= aspectRatio * 1.6f;
            }
            modRect.Width  = (int)w;
            modRect.Height = (int)h;
            batch.Draw(modTexture, modRect, Color.White);

            var tCursor = new Vector2(bCursor.X + 35f, bCursor.Y + 3f);

            string moduleName = Localizer.Token(mod.NameIndex);
            if (Fonts.Arial12Bold.MeasureString(moduleName).X + 90 < List.Width)
            {
                batch.DrawString(Fonts.Arial12Bold, moduleName, tCursor, Color.White);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing;
            }
            else
            {
                batch.DrawString(Fonts.Arial11Bold, moduleName, tCursor, Color.White);
                tCursor.Y += Fonts.Arial11Bold.LineSpacing;
            }

            string restriction = mod.Restrictions.ToString();
            batch.DrawString(Fonts.Arial8Bold, restriction, tCursor, Color.Orange);
            tCursor.X += Fonts.Arial8Bold.MeasureString(restriction).X;
            string size = $" ({mod.XSIZE}x{mod.YSIZE})";
            batch.DrawString(Fonts.Arial8Bold, size, tCursor, Color.Gray);
            tCursor.X += Fonts.Arial8Bold.MeasureString(size).X;

            if (mod.IsRotatable)
            {
                var rotateRect = new Rectangle((int)bCursor.X + 240, (int)bCursor.Y + 3, 20, 22);
                batch.Draw(ResourceManager.Texture("UI/icon_can_rotate"), rotateRect, Color.White);
                if (rotateRect.HitTest(GameBase.ScreenManager.input.CursorPosition))
                {
                    ToolTip.CreateTooltip("Indicates that this module can be rotated using the arrow keys");
                }
            }

        }
    }
}
