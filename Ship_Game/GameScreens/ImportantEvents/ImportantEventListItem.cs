using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class ImportantEventListItem : ScrollListItem<ImportantEventListItem>
    {
        public readonly ImportantNotification Event;
        readonly Graphics.Font NormalFont = Fonts.Arial12Bold;
        readonly UIPanel EventIcon;
        readonly Color RowColor;

        public ImportantEventListItem(ImportantNotification importantEvent)
        {
            Event    = importantEvent;
            RowColor = Event.RelevantEmpire?.EmpireColor ?? Color.LightGray;

            if (Event.RelevantEmpire != null)
            {
                EventIcon = Add(new UIPanel(Pos, ResourceManager.Flag(Event.RelevantEmpire.data.Traits.FlagIndex),
                                            Event.RelevantEmpire.EmpireColor));
            }
            else if (Event.IconPath.NotEmpty() && ResourceManager.TextureLoaded(Event.IconPath))
            {
                EventIcon = Add(new UIPanel(Pos, ResourceManager.Texture(Event.IconPath)));
            }

            if (EventIcon != null)
                EventIcon.Size = new Vector2(40, 40);

            AddEventLabel(Event.StarDate.StarDateString(), 120, 60, Colors.Cream);
            AddEventLabel(Event.Title, 230, 190, RowColor);
            AddEventLabel(Event.Message.Replace('\n', ' '), 700, 430, Color.LightGray);
        }

        void AddEventLabel(string text, float sizeX, float relativeX, Color color)
        {
            string parsedText = NormalFont.ParseText(text, sizeX - 30);
            UILabel label     = Add(new UILabel(parsedText, NormalFont, color));
            label.Size        = new Vector2(sizeX, 80);
            label.TextAlign   = TextAlign.VerticalCenter;
            label.SetLocalPos(relativeX, 0);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Color borderColor = DimColor(RowColor, 3);
            batch.FillRectangle(Rect, DimColor(RowColor, 10));
            batch.DrawRectangle(Rect, borderColor);

            int top = Rect.Y;
            int bot = Rect.Y + Rect.Height;
            batch.DrawLine(new Vector2(Rect.X + 180, top), new Vector2(Rect.X + 180, bot), borderColor);
            batch.DrawLine(new Vector2(Rect.X + 420, top), new Vector2(Rect.X + 420, bot), borderColor);

            if (EventIcon != null)
                EventIcon.Pos = new Vector2(Pos.X + 5, Pos.Y + 20);

            base.Draw(batch, elapsed);
        }

        static Color DimColor(Color color, int divider)
        {
            return new Color((byte)(color.R / divider),
                             (byte)(color.G / divider),
                             (byte)(color.B / divider));
        }
    }
}
