using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class ResearchPopup : PopupWindow
    {
        public bool fade;
        public bool FromGame;
        public string TechUID;
        ScrollList2<UnlockListItem> UnlockSL;
        readonly Technology Technology;
        
        public ResearchPopup(UniverseScreen s, string uid) : base(s, 600, 600)
        {
            TechUID = uid;
            fade = true;
            IsPopup = true;
            FromGame = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0f;
            TechEntry techEntry = s.Player.GetTechEntry(uid);
            if (techEntry == null)
                return;

            Technology = ResourceManager.Tech(uid);

            string level = RomanNumerals.ToRoman(techEntry.Level);
            string maxlvl = RomanNumerals.ToRoman(techEntry.MaxLevel);
            TitleText  = Technology.Name.Text + (Technology.MaxLevel > 1 ? $" {level}/{maxlvl}" : "");
            MiddleText = techEntry.Tech.Description.Text;
        }
        
        public override void LoadContent()
        {
            base.LoadContent();

            var rect = new Rectangle(MidContainer.X + 20, 
                                     MidContainer.Y + MidContainer.Height - 20, 
                                     Rect.Width - 40, 
                                     Rect.Height - MidContainer.Height - TitleRect.Height - 20);

            UnlockSL = Add(new ScrollList2<UnlockListItem>(rect, 100));

            Array<UnlockItem> unlocks = UnlockItem.CreateUnlocksList(Technology);
            UnlockSL.SetItems(unlocks.Select(u => new UnlockListItem(u)));
        }

        class UnlockListItem : ScrollListItem<UnlockListItem>
        {
            readonly UnlockItem Unlock;
            public UnlockListItem(UnlockItem unlock)
            {
                Unlock = unlock;
            }

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                Rectangle iconRect = default;
                string comment = "";
                string summary = "";

                switch (Unlock.Type)
                {
                    case UnlockType.ShipModule:
                        iconRect = Unlock.GetModuleRect((int)X + 16, (int)Y + 16, 64, 64);
                        comment = $" ({Unlock.ModW}x{Unlock.ModH})";
                        break;
                    case UnlockType.Troop:
                        iconRect = new Rectangle((int)X + 16, (int)CenterY - 32, 64, 64);
                        break;
                    case UnlockType.Building:
                        iconRect = new Rectangle((int)X + 16, (int)CenterY - 32, 64, 64);
                        summary = Unlock.building?.GetShortDescrText() ?? "";
                        break;
                    case UnlockType.Hull:
                        iconRect = new Rectangle((int)X, (int)CenterY - 32, 96, 96);
                        break;
                    case UnlockType.Advance:
                        iconRect = new Rectangle((int)X + 24, (int)Y + 24, 48, 48);
                        break;
                }

                batch.Draw(Unlock.Icon, iconRect, Color.White);

                string wrappedDescr = Fonts.Arial12.ParseText(Unlock.Description, Width - 100);
                float textHeight = Fonts.Arial14Bold.LineSpacing + 5 + Fonts.Arial12.MeasureString(wrappedDescr).Y;
                var pos = new Vector2(X + 100, CenterY - (int)(textHeight / 2f));

                batch.DrawDropShadowText(Unlock.Title, pos, Fonts.Arial14Bold, Color.Orange);
                if (comment.NotEmpty())
                {
                    var commentPos = Fonts.Arial14Bold.MeasureString(Unlock.Title);
                    commentPos.X += pos.X;
                    commentPos.Y  = pos.Y + 2;
                    batch.DrawString(Fonts.Arial12, comment, commentPos, Color.Gray);
                }

                batch.DrawString(Fonts.Arial12, wrappedDescr, pos + new Vector2(0f, Fonts.Arial14Bold.LineSpacing + 2), Color.LightGray);
                if (summary.NotEmpty())
                {
                    string wrappedSummary = Fonts.Arial12.ParseText(summary, Width - 100);
                    int lines = wrappedDescr.Split('\n').Length + 2;
                    batch.DrawString(Fonts.Arial12, wrappedSummary, pos + new Vector2(0f, Fonts.Arial12.LineSpacing * lines - 3), Color.SteelBlue);
                }
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (fade) ScreenManager.FadeBackBufferToBlack((TransitionAlpha * 2) / 3);

            base.Draw(batch, elapsed);
        }
    }
}
