using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;


namespace Ship_Game
{
    public class WeaponListItem : ScrollList<WeaponListItem>.Entry
    {
        public ModuleHeader Header;
        public ShipModule Module;
        readonly WeaponScrollList WSL;

        public WeaponListItem(WeaponScrollList list)
        {
            WSL = list;
        }

        public override bool HandleInput(InputState input)
        {
            bool captured = base.HandleInput(input);
            if (Clicked)
            {
                if (Module != null)
                {
                    WSL.Screen.SetActiveModule(Module, ModuleOrientation.Normal, 0f);
                }
            }
            return captured;
        }

        public override void Update(float deltaTime)
        {
            if (Header != null)
            {
                Header.Hover = Hovered;
                Header.Open = Expanded;
            }
            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            if (Header != null)
            {
                Header.Pos = new Vector2(WSL.Screen.ModSel.X + 10, Y);
                Header.Draw(batch);
            }
            else if (Module != null)
            {
                DrawModule(batch, Module);
            }
        }

        void DrawModule(SpriteBatch batch, ShipModule mod)
        {
            var bCursor = new Vector2(WSL.Screen.ModSel.X + 15, Y);
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
            if (Fonts.Arial12Bold.MeasureString(moduleName).X + 90 < WSL.Screen.ModSel.Width)
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
