using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Codex
{
    // Replaces UITextBox for Codex body content: walks a StyledRun[] from
    // StyledTextParser, lays out word-wrapped text and atomic image runs into
    // a flat list of draw tokens, and handles url-tag click testing.
    public sealed class StyledTextRenderer
    {
        public RectF Bounds;

        struct Token
        {
            public string Text;       // null for image tokens
            public Font Font;         // null for image tokens
            public Color Color;
            public Vector2 Pos;       // top-left in screen space
            public float Width;
            public float Height;
            public string Url;        // non-null marks a clickable url
            public SubTexture Image;  // non-null marks an image token
        }

        readonly List<Token> Tokens = new();
        // Texture paths we've already warned about; keeps the log from spamming
        // if the same broken path is referenced from multiple entries.
        static readonly HashSet<string> WarnedPaths = new();

        // Vertical scroll offset and total content height for clamping.
        public float ScrollY { get; private set; }
        public float ContentHeight { get; private set; }

        // Right-edge reservation for the scrollbar thumb. Text wraps against
        // (Bounds.Right - ScrollBarWidth) so it never runs under the bar.
        const float ScrollBarWidth = 12f;
        float WrapRight => Bounds.Right - ScrollBarWidth;

        const float BulletIndent = 14f;
        const string BulletGlyph = "-";
        float LineStartX;

        public StyledTextRenderer(RectF bounds)
        {
            Bounds = bounds;
        }

        // Scroll by `delta` pixels; positive = scroll down (reveal lower content).
        // Clamped to [0, max(0, ContentHeight - Bounds.H)].
        public void Scroll(float delta)
        {
            float max = Math.Max(0f, ContentHeight - Bounds.H);
            ScrollY = Math.Clamp(ScrollY + delta, 0f, max);
        }

        public void SetText(StyledRun[] runs)
        {
            Tokens.Clear();
            ScrollY = 0;
            ContentHeight = 0;
            if (runs == null || runs.Length == 0)
                return;

            LineStartX = Bounds.X;
            float x = Bounds.X;
            float y = Bounds.Y;
            float lineHeight = CodexStyles.DefaultFont.LineSpacing;
            int lineStart = 0;

            void CommitLine()
            {
                // Vertically align tokens on the current line to a shared baseline.
                // Tokens that are shorter than the line height get their pos.Y
                // pushed down so their bottom edges align (images vs text mix).
                float maxH = lineHeight;
                for (int k = lineStart; k < Tokens.Count; ++k)
                    if (Tokens[k].Height > maxH) maxH = Tokens[k].Height;

                for (int k = lineStart; k < Tokens.Count; ++k)
                {
                    var t = Tokens[k];
                    t.Pos.Y = y + (maxH - t.Height);
                    Tokens[k] = t;
                }

                y += maxH;
                x = LineStartX;
                lineHeight = CodexStyles.DefaultFont.LineSpacing;
                lineStart = Tokens.Count;
            }

            void BreakLine()
            {
                CommitLine();
                LineStartX = Bounds.X;
                x = LineStartX;
            }

            foreach (StyledRun run in runs)
            {
                if (run.IsImage)
                {
                    SubTexture tex = ResourceManager.TextureOrNull(run.ImagePath);
                    if (tex == null)
                    {
                        if (WarnedPaths.Add(run.ImagePath))
                            Log.Warning($"StyledTextRenderer: missing texture '{run.ImagePath}'");
                        // Use the global error texture (red X) inline so authors
                        // notice the typo at a glance without breaking layout.
                        tex = ResourceManager.ErrorTexture;
                    }

                    float w = tex.Width;
                    float h = tex.Height;
                    if (x + w > WrapRight && x > LineStartX)
                        CommitLine();

                    Tokens.Add(new Token
                    {
                        Image = tex,
                        Pos = new Vector2(x, y),
                        Width = w,
                        Height = h,
                        Color = Color.White,
                    });
                    x += w;
                    if (h > lineHeight) lineHeight = h;
                    continue;
                }

                if (run.IsLineBreak)
                {
                    BreakLine();
                    continue;
                }

                if (run.IsBullet)
                {
                    if (x > LineStartX)
                        CommitLine();
                    Font dashFont = CodexStyles.BoldFont;
                    Tokens.Add(new Token
                    {
                        Text = BulletGlyph,
                        Font = dashFont,
                        Color = CodexStyles.Default,
                        Pos = new Vector2(Bounds.X, y),
                        Width = dashFont.TextWidth(BulletGlyph),
                        Height = dashFont.LineSpacing,
                    });
                    LineStartX = Bounds.X + BulletIndent;
                    x = LineStartX;
                    continue;
                }

                Font font = run.IsCaption ? CodexStyles.CaptionFont
                          : run.Bold      ? CodexStyles.BoldFont
                          :                 CodexStyles.DefaultFont;
                if (font.LineSpacing > lineHeight) lineHeight = font.LineSpacing;

                foreach (string token in SplitWords(run.Text))
                {
                    if (token == "\n")
                    {
                        BreakLine();
                        continue;
                    }
                    EmitText(token, font, run.Color, run.Url, ref x, ref y, ref lineHeight, CommitLine);
                }
            }

            CommitLine();
            ContentHeight = y - Bounds.Y;
        }

        void EmitText(string token, Font font, Color color, string url,
                      ref float x, ref float y, ref float lineHeight, System.Action commitLine)
        {
            float w = font.TextWidth(token);
            // Wrap only when this token genuinely overflows AND there's already
            // something on the line — otherwise a single overflowing word would
            // loop forever on empty lines.
            if (x + w > WrapRight && x > LineStartX)
            {
                // Suppress leading whitespace on a wrapped line so we don't show
                // an awkward leading gap where the wrap occurred.
                commitLine();
                if (token == " ") return;
            }

            Tokens.Add(new Token
            {
                Text = token,
                Font = font,
                Color = color,
                Pos = new Vector2(x, y),
                Width = w,
                Height = font.LineSpacing,
                Url = url,
            });
            x += w;
            if (font.LineSpacing > lineHeight) lineHeight = font.LineSpacing;
        }

        // Splits a text run into word + space + newline tokens. Whitespace runs
        // collapse to single space tokens, except newlines which round-trip as
        // their own "\n" tokens so the renderer can break the line cleanly.
        static IEnumerable<string> SplitWords(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    if (sb.Length > 0) { yield return sb.ToString(); sb.Clear(); }
                    yield return "\n";
                }
                else if (c == ' ' || c == '\t' || c == '\r')
                {
                    if (sb.Length > 0) { yield return sb.ToString(); sb.Clear(); }
                    yield return " ";
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0) yield return sb.ToString();
        }

        public void Draw(SpriteBatch batch)
        {
            if (Tokens.Count == 0)
                return;

            // Scissor-clip to Bounds so partially-visible tokens at the top/bottom
            // of the scroll region don't bleed past the body panel.
            batch.SafeEnd();
            RenderStates.EnableScissorTest(batch.GraphicsDevice, Bounds);
            batch.SafeBegin(SpriteBlendMode.AlphaBlend, RenderStates.ScissorEnabled);

            for (int i = 0; i < Tokens.Count; ++i)
            {
                Token t = Tokens[i];
                float drawY = t.Pos.Y - ScrollY;
                if (drawY + t.Height < Bounds.Y || drawY > Bounds.Bottom)
                    continue;

                var pos = new Vector2(t.Pos.X, drawY);
                if (t.Image != null)
                {
                    batch.Draw(t.Image, new RectF(pos.X, pos.Y, t.Width, t.Height), Color.White);
                }
                else
                {
                    batch.DrawString(t.Font, t.Text, pos, t.Color);
                    if (t.Url != null)
                    {
                        var p1 = new Vector2(pos.X, pos.Y + t.Height - 1f);
                        var p2 = new Vector2(pos.X + t.Width, pos.Y + t.Height - 1f);
                        batch.DrawLine(p1, p2, t.Color, thickness: 1f);
                    }
                }
            }

            batch.SafeEnd();
            RenderStates.DisableScissorTest(batch.GraphicsDevice);
            batch.SafeBegin();

            DrawScrollBar(batch);
        }

        // Reuses NewUI ScrollListStyleTextures so the bar matches the left
        // category list visually.
        void DrawScrollBar(SpriteBatch batch)
        {
            float overflow = ContentHeight - Bounds.H;
            if (overflow <= 0f)
                return;

            ScrollListStyleTextures s = ScrollListStyleTextures.Get(ListStyle.Default);
            float thumbH = Math.Max(20f, Bounds.H * (Bounds.H / ContentHeight));
            float thumbY = Bounds.Y + (ScrollY / overflow) * (Bounds.H - thumbH);
            float midH  = s.ScrollBarMid.Normal.Height;
            float capH  = Math.Max(0f, (thumbH - midH) / 2f);

            var thumbX = Bounds.Right - ScrollBarWidth;
            var up  = new RectF(thumbX, thumbY,                ScrollBarWidth, capH);
            var mid = new RectF(thumbX, thumbY + capH,         ScrollBarWidth, midH);
            var bot = new RectF(thumbX, thumbY + capH + midH,  ScrollBarWidth, capH);

            s.ScrollBarUpDown.Draw(batch, up,  parentHovered: false, controlItemHovered: false);
            s.ScrollBarMid   .Draw(batch, mid, parentHovered: false, controlItemHovered: false);
            s.ScrollBarUpDown.Draw(batch, bot, parentHovered: false, controlItemHovered: false);
        }

        // Returns true if the click landed on a url token, opening that URL.
        public bool HandleClick(Vector2 mousePos)
        {
            for (int i = 0; i < Tokens.Count; ++i)
            {
                Token t = Tokens[i];
                if (t.Url == null) continue;
                float drawY = t.Pos.Y - ScrollY;
                if (mousePos.X >= t.Pos.X && mousePos.X <= t.Pos.X + t.Width
                    && mousePos.Y >= drawY && mousePos.Y <= drawY + t.Height)
                {
                    Log.OpenURL(t.Url);
                    return true;
                }
            }
            return false;
        }
    }
}
