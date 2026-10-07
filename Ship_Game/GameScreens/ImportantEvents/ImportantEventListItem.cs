using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDUtils;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class ImportantEventListItem : ScrollListItem<ImportantEventListItem>
    {
        public const int RowHeight = 80;
        const float DescriptionWidth = 700;

        public readonly ImportantNotification Event;
        public bool Selected;
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

            AddEventLabel(NormalFont.ParseText(Event.StarDate.StarDateString(), 90), 120, 60, Colors.Cream);
            AddEventLabel(NormalFont.ParseText(Event.Title, 200), 230, 190, RowColor);
            AddEventLabel(FitDescription(Event.Message), DescriptionWidth, 430, Color.LightGray);
        }

        // the full text is in the details box below the list
        internal string FitDescription(string text)
        {
            text = text.Replace('\n', ' ').Replace("\\n", " "); // event texts write paragraph breaks as a literal \n
            string[] lines = NormalFont.ParseTextToLines(text, DescriptionWidth - 30);
            int maxLines = (RowHeight - 8) / NormalFont.LineSpacing;
            if (lines.Length > maxLines)
            {
                Array.Resize(ref lines, maxLines);
                lines[maxLines - 1] = WithEllipsis(lines[maxLines - 1]);
            }
            return string.Join("\n", lines);
        }

        internal string WithEllipsis(string line)
        {
            const string ellipsis = "...";
            while (line.Length > 0 && NormalFont.MeasureString(line + ellipsis).X > DescriptionWidth - 30)
            {
                int space = line.LastIndexOf(' ');
                line = space > 0 ? line.Substring(0, space) : "";
            }
            return line.TrimEnd(' ', ',', '.', ';', ':') + ellipsis;
        }

        void AddEventLabel(string parsedText, float sizeX, float relativeX, Color color)
        {
            UILabel label   = Add(new UILabel(parsedText, NormalFont, color));
            label.Size      = new Vector2(sizeX, RowHeight);
            label.TextAlign = TextAlign.VerticalCenter;
            label.SetLocalPos(relativeX, 0);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Color borderColor = DimColor(RowColor, 3);
            batch.FillRectangle(Rect, DimColor(RowColor, Selected ? 5 : 10));
            batch.DrawRectangle(Rect, Selected ? RowColor : borderColor);

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
