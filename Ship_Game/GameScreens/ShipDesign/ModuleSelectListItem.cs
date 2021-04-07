using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;


namespace Ship_Game
{
    public class ModuleSelectListItem : ScrollListItem<ModuleSelectListItem>
    {
        public ShipModule Module;
        public bool IsObsolete { get; private set; }
        public ModuleSelectListItem(string headerText) : base(headerText) {}

        public ModuleSelectListItem(ShipModule module)
        {
            Module     = module;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            if (Module != null)
            {
                IsObsolete = Module.IsObsolete();
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

            var tCursor       = new Vector2(bCursor.X + 35f, bCursor.Y + 3f);
            Color nameColor   = IsObsolete ? Color.Red : Color.White; 
            string moduleName = Localizer.Token(mod.NameIndex);
            if (Fonts.Arial12Bold.MeasureString(moduleName).X + 90 < List.Width)
            {
                batch.DrawString(Fonts.Arial12Bold, moduleName, tCursor, nameColor);
                tCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
            }
            else
            {
                batch.DrawString(Fonts.Arial11Bold, moduleName, tCursor, nameColor);
                tCursor.Y += Fonts.Arial11Bold.LineSpacing + 3;
            }

            string restriction = mod.Restrictions.ToString();
            batch.DrawString(Fonts.Arial8Bold, restriction, tCursor, Color.Orange);
            tCursor.X += Fonts.Arial8Bold.MeasureString(restriction).X;
            string size = $" ({mod.XSIZE}x{mod.YSIZE})";
            batch.DrawString(Fonts.Arial8Bold, size, tCursor, Color.Gray);
            tCursor.X += Fonts.Arial8Bold.MeasureString(size).X;

            if (mod.InstalledWeapon?.isTurret == true && !mod.DisableRotation)
            {
                var rotateRect = new Rectangle((int)bCursor.X + 240, (int)bCursor.Y + 3, 15, 16);
                var turretRect = new Rectangle((int)bCursor.X + 238, (int)bCursor.Y + 20, 18, 20);
                batch.Draw(ResourceManager.Texture("UI/icon_can_rotate"), rotateRect, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/icon_turret"), turretRect, Color.White);
                if (rotateRect.HitTest(GameBase.ScreenManager.input.CursorPosition) || turretRect.HitTest(GameBase.ScreenManager.input.CursorPosition))
                    ToolTip.CreateTooltip(GameText.ThisModuleCanBeRotated);
            }
            else if (!mod.DisableRotation)
            {
                var rotateRect = new Rectangle((int)bCursor.X + 240, (int)bCursor.Y + 3, 20, 22);
                batch.Draw(ResourceManager.Texture("UI/icon_can_rotate"), rotateRect, Color.White);
                if (rotateRect.HitTest(GameBase.ScreenManager.input.CursorPosition))
                    ToolTip.CreateTooltip(GameText.IndicatesThatThisModuleCan);
            }
            else if (mod.InstalledWeapon?.isTurret == true)
            {
                var turretRect = new Rectangle((int)bCursor.X + 235, (int)bCursor.Y + 3, 25, 23);
                batch.Draw(ResourceManager.Texture("NewUI/icon_turret"), turretRect, Color.White);
                if (turretRect.HitTest(GameBase.ScreenManager.input.CursorPosition))
                    ToolTip.CreateTooltip(GameText.IndicatesThisModuleHasA);
            }

            if (IsObsolete)
            {
                var obsoleteRect = new Rectangle((int)bCursor.X + 220, (int)bCursor.Y + 22, 17, 17);
                batch.Draw(ResourceManager.Texture("NewUI/icon_queue_delete"), obsoleteRect, Color.Red);
                if (obsoleteRect.HitTest(GameBase.ScreenManager.input.CursorPosition))
                    ToolTip.CreateTooltip(4188);
            }
        }
    }
}
