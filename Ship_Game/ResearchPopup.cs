using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Graphics;

namespace Ship_Game;

public sealed class ResearchPopup : PopupWindow
{
    UniverseScreen Universe;
    public bool fade;
    public bool FromGame;
    public string TechUID;
    ScrollList<UnlockListItem> UnlockSL;
    readonly Technology Technology;
        
    public ResearchPopup(UniverseScreen us, string uid) : base(us, 600, 600)
    {
        Universe = us;
        TechUID = uid;
        fade = true;
        IsPopup = true;
        FromGame = true;
        TransitionOnTime = 0.25f;
        TransitionOffTime = 0f;
        TechEntry techEntry = us.Player.GetTechEntry(uid);
        if (techEntry != null)
        {
            Technology = techEntry.Tech;
            TitleText = Technology.Name.Text;
            if (Technology.MaxLevel > 1)
            {
                string level = RomanNumerals.ToRoman(techEntry.Level);
                string maxlvl = RomanNumerals.ToRoman(techEntry.MaxLevel);
                TitleText += $" {level}/{maxlvl}";
            }
            MiddleText = techEntry.Tech.Description.Text;
        }
    }

    public override void LoadContent()
    {
        base.LoadContent();

        RectF rect = new(MidContainer.X + 20, 
            MidContainer.Y + MidContainer.Height - 20, 
            Rect.Width - 40, 
            Rect.Height - MidContainer.Height - TitleRect.Height - 20);
        UnlockSL = Add(new ScrollList<UnlockListItem>(rect, 100));

        Array<UnlockItem> unlocks = UnlockItem.CreateUnlocksList(Technology, Universe.Player);
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
            try
            {
                RectF iconRect = default;
                string comment = "";
                string summary = "";

                switch (Unlock.Type)
                {
                    case UnlockType.ShipModule:
                        iconRect = Unlock.GetModuleRect(X + 16, Y + 16, 64, 64);
                        comment = $" ({Unlock.ModW}x{Unlock.ModH})";
                        break;
                    case UnlockType.Troop:
                        iconRect = new(X + 16, CenterY - 32, 64, 64);
                        break;
                    case UnlockType.Building:
                        iconRect = new(X + 16, CenterY - 32, 64, 64);
                        if (Unlock.building != null)
                        {
                            comment = $"    Production Cost: {Unlock.building.ActualCost}";
                        }
                        summary = Unlock.building?.GetShortDescrText() ?? "";
                        break;
                    case UnlockType.Hull:
                        iconRect = new(X, CenterY - 32, 96, 96);
                        break;
                    case UnlockType.Advance:
                        iconRect = new(X + 24, Y + 24, 48, 48);
                        break;
                }

                batch.Draw(Unlock.Icon, iconRect, Color.White);

                Font titleFont = Fonts.Arial14Bold;
                Font textFont = Fonts.Arial12;

                string wrappedDescr = textFont.ParseText(Unlock.Description, Width - 100);
                Vector2 descrSize = textFont.MeasureString(wrappedDescr);
                float textHeight = titleFont.LineSpacing + 5 + descrSize.Y;
                    
                // title text for the item
                Vector2 titlePos = new(X + 100, CenterY - (int)(textHeight / 2f));
                batch.DrawDropShadowText(Unlock.Title, titlePos, titleFont, Color.Orange);

                // small comment text right next to the Title
                if (comment.NotEmpty())
                {
                    float titleWidth = titleFont.TextWidth(Unlock.Title);
                    Vector2 commentPos = titlePos + new Vector2(titleWidth, 2);
                    batch.DrawString(textFont, comment, commentPos, Color.Gray);
                }

                // long description text
                Vector2 descrPos = titlePos + new Vector2(0, titleFont.LineSpacing + 2);
                batch.DrawString(textFont, wrappedDescr, descrPos, Color.LightGray);

                if (summary.NotEmpty())
                {
                    string wrappedSummary = textFont.ParseText(summary, Width - 100);
                    Vector2 summaryPos = descrPos + new Vector2(0, descrSize.Y + 2);
                    batch.DrawString(textFont, wrappedSummary, summaryPos, Color.SteelBlue);
                }
            }
            catch (Exception ex)
            {
                Visible = false; // hide it, so we don't crash the game
                Log.Error(ex, $"UnlockListItem draw failed: Type={Unlock.Type} Title={Unlock.Title} Description={Unlock.Description}");
            }
        }
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (fade) ScreenManager.FadeBackBufferToBlack((TransitionAlpha * 2) / 3);

        base.Draw(batch, elapsed);
    }
}