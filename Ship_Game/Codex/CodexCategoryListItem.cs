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
        // 0 for a top-level category, +1 per level of Children below it. Drives the
        // indent and color so a header inside a header reads as one.
        public readonly int Depth;
        const int IndentPerLevel = 20;
        // where the title text starts, shared by topics and nested headers so a
        // header sits flush with the topics beside it; topics under a top-level
        // category (depth 1) keep the list's original 15px inset
        float TextInset => 15f + Math.Max(0, Depth - 1) * IndentPerLevel;

        public CodexCategoryListItem(CodexEntry entry, int depth)
        {
            Entry = entry;
            Depth = depth;
            // Categories (entries with children) act as expandable headers; the base
            // class requires IsHeader=true before AddSubItem will accept children.
            // Hidden children are never added, so a category with nothing visible
            // under it must not become a header, or it would draw an empty bar that
            // swallows clicks.
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

        // A nested header is indented under its parent and drawn smaller and in its
        // own color, so it cannot be mistaken for the top-level category above it.
        protected override void DrawHeader(SpriteBatch batch, DrawTimes elapsed)
        {
            if (Depth == 0)
            {
                base.DrawHeader(batch, elapsed);
                return;
            }

            // the bar leads the text by a few pixels; the text itself lands on the
            // same inset as the sibling topics
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
