using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    // Permanent log of Important notifications (empire defeat, merge/surrender,
    // remnant story progression), opened from the minimap. Styled after ShipDesignIssuesScreen.
    public sealed class ImportantEventsScreen : GameScreen
    {
        readonly Menu2 Window;
        readonly Color Cream = Colors.Cream;
        readonly ImportantNotification[] Events;
        readonly ScrollList<ImportantEventListItem> EventList;
        readonly Graphics.Font LargeFont = Fonts.Arial20Bold;

        public ImportantEventsScreen(UniverseScreen screen) : base(screen, toPause: null)
        {
            Events            = screen.UState.GetImportantEvents();
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;

            Window = Add(new Menu2(new Rectangle(ScreenWidth / 2 - 600, ScreenHeight / 2 - 300, 1200, 540)));
            int x  = (int)Window.X + 20;
            int y  = (int)Window.Y + 70;
            int w  = (int)Window.Width - 30;
            int h  = (int)Window.Height - 80;

            EventList = Add(new ScrollList<ImportantEventListItem>(new RectF(x, y, w, h), 80));
            EventList.EnableItemHighlight = true;

            UILabel starDateLabel    = Add(new UILabel("Star Date", LargeFont, Cream));
            UILabel titleLabel       = Add(new UILabel("Title", LargeFont, Cream));
            UILabel descriptionLabel = Add(new UILabel("Description", LargeFont, Cream));
            starDateLabel.Size       = new Vector2(120, 20);
            titleLabel.Size          = new Vector2(230, 20);
            descriptionLabel.Size    = new Vector2(700, 20);
            starDateLabel.Pos        = new Vector2(x + 60, y - 10);
            titleLabel.Pos           = new Vector2(x + 190, y - 10);
            descriptionLabel.Pos     = new Vector2(x + 430, y - 10);
            starDateLabel.TextAlign    = TextAlign.HorizontalCenter;
            titleLabel.TextAlign       = TextAlign.HorizontalCenter;
            descriptionLabel.TextAlign = TextAlign.HorizontalCenter;
        }

        void PopulateEvents()
        {
            // newest first
            for (int i = Events.Length - 1; i >= 0; --i)
                EventList.AddItem(new ImportantEventListItem(Events[i]));
        }

        public override void LoadContent()
        {
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);
            string title    = "Important Events";
            Vector2 menuPos = new Vector2(Window.Menu.CenterTextX(title, Fonts.Laserian14), Window.Menu.Y + 30);
            Label(menuPos, title, Fonts.Laserian14, Cream);
            PopulateEvents();
            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped || input.RightMouseClick)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }
    }
}
