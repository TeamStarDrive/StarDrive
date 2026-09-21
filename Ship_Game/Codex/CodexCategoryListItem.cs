using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Codex
{
    public class CodexCategoryListItem : ScrollListItem<CodexCategoryListItem>
    {
        public CodexEntry Entry;
        public readonly int Depth;
        const int IndentPerLevel = 20;
        float TextInset => 15f + Math.Max(0, Depth - 1) * IndentPerLevel;

        public CodexCategoryListItem(CodexEntry entry, int depth)
        {
            Entry = entry;
            Depth = depth;
            // Categories (entries with children) act as expandable headers; the base
            // class requires IsHeader=true before AddSubItem will accept children.
            if (entry is { HasVisibleChildren: true })
            {
                IsHeader = true;
                HeaderText = Localizer.Token(entry.TitleId);
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            if (Entry == null || IsHeader)
                return; // header text is drawn by ScrollListItem<T>.base.Draw via HeaderText

            Vector2 cursor = Pos;
            cursor.X += TextInset;
            Color title = Hovered   ? CodexStyles.Caption
                        : Depth > 1 ? CodexStyles.NestedTitle
                                    : Color.White;
            batch.DrawString(Fonts.Arial12Bold, Localizer.Token(Entry.TitleId), cursor, title);

            if (!string.IsNullOrEmpty(Entry.ShortDescId))
            {
                cursor.Y += Fonts.Arial12Bold.LineSpacing;
                batch.DrawString(Fonts.Arial12,
                    Localizer.Token(Entry.ShortDescId), cursor, Color.Orange);
            }
        }

        protected override void DrawHeader(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Depth == 0)
            {
                base.DrawHeader(batch, elapsed);
                return;
            }

            const int barLead = 5;
            int indent = (int)TextInset - barLead;
            int width  = Math.Min(HeaderMaxWidth, (int)Width) - indent;
            var r = new Rectangle((int)X + indent, (int)Y + 4, width, (int)Height - 10);

            if (HeaderText != null)
            {
                new Selector(r, HeaderBackground).Draw(batch, elapsed);

                var textPos = new Vector2(r.X + barLead, r.CenterY() - Fonts.Arial12Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial12Bold, HeaderText, textPos, CodexStyles.NestedHeader);
            }

            DrawExpandMarker(batch, r);
        }
    }
}
