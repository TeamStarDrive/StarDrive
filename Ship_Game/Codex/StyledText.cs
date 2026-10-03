using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Ship_Game.Codex
{
    // A single styled fragment of body text, an inline image, or a bullet
    // marker. Text and image runs are mutually exclusive: ImagePath != null
    // marks an image run, in which case Text/Color/Bold/Url are ignored. A text
    // run with Text == "\n" is a forced line break. A bullet run carries no text:
    // it starts a list item, which the renderer indents until the next line break.
    public readonly struct StyledRun
    {
        public readonly string Text;
        public readonly Color Color;
        public readonly bool Bold;
        public readonly bool IsCaption; // bigger heading font + auto double-break after span
        public readonly string Url;
        public readonly string ImagePath;
        public readonly bool IsBullet;

        public StyledRun(string text, Color color, bool bold, string url, bool caption = false)
        {
            Text = text;
            Color = color;
            Bold = bold;
            IsCaption = caption;
            Url = url;
            ImagePath = null;
            IsBullet = false;
        }

        public static StyledRun Image(string path) => new(image: path, bullet: false);
        public static StyledRun Bullet() => new(image: null, bullet: true);
        StyledRun(string image, bool bullet)
        {
            Text = null;
            Color = default;
            Bold = false;
            IsCaption = false;
            Url = null;
            ImagePath = image;
            IsBullet = bullet;
        }

        public bool IsImage => ImagePath != null;
        public bool IsLineBreak => !IsImage && Text == "\n";
    }

    // Parses a string with embedded markup into a flat sequence of StyledRun.
    //
    // Supported tags (case-sensitive, must not nest within their own class):
    //   <color=Name>...</color>     color span; Name resolves via CodexStyles.TryGetColor
    //   <b>...</b>                  bold span
    //   <url=https://...>text</url> clickable hyperlink
    //   <img>path/to/texture</img>  inline atomic image
    //   <li>item text</li>          list item: a dash at the margin, and every
    //                               wrapped line of the item indented past it;
    //                               the item ends at the next line break, so
    //                               </li> is optional
    //
    // Tags of different classes (color + bold + url) may combine. Same-class
    // nesting is refused — the inner open tag is emitted as literal text.
    // Unknown tag names pass through literally, so a missing close tag never
    // crashes the parser.
    //
    // After parsing, a forced "\n" run is inserted after the last image of any
    // contiguous img group when followed by a non-image run (the inline-image
    // flow rule from docs/codex-plan.md Phase 4).
    public static class StyledTextParser
    {
        public static StyledRun[] Parse(string source)
        {
            if (string.IsNullOrEmpty(source))
                return System.Array.Empty<StyledRun>();

            var runs = new List<StyledRun>();
            var buffer = new StringBuilder();

            Color color = CodexStyles.Default;
            bool bold = false;
            bool caption = false;
            string url = null;
            bool inColor = false;
            bool inBold = false;
            bool inUrl = false;
            // Color is forced to CodexStyles.Url while a url span is active; this
            // remembers the outer color so we can restore it on </url>.
            Color preUrlColor = CodexStyles.Default;

            void Flush()
            {
                if (buffer.Length == 0)
                    return;
                runs.Add(new StyledRun(buffer.ToString(), color, bold, url, caption));
                buffer.Clear();
            }

            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];
                if (c != '<')
                {
                    buffer.Append(c);
                    i++;
                    continue;
                }

                int tagEnd = source.IndexOf('>', i + 1);
                if (tagEnd < 0)
                {
                    buffer.Append(c);
                    i++;
                    continue;
                }

                string content = source.Substring(i + 1, tagEnd - i - 1);
                bool isClose = content.Length > 0 && content[0] == '/';
                string nameAndValue = isClose ? content.Substring(1) : content;
                int eq = nameAndValue.IndexOf('=');
                string tagName = eq >= 0 ? nameAndValue.Substring(0, eq) : nameAndValue;
                string tagValue = !isClose && eq >= 0 ? nameAndValue.Substring(eq + 1) : null;

                bool consumed = false;
                switch (tagName)
                {
                    case "color":
                        if (isClose)
                        {
                            if (inColor)
                            {
                                Flush();
                                // While inside a <url> span, the body color must
                                // remain CodexStyles.Url after a nested </color>
                                // closes — otherwise the rest of the link span
                                // renders as Default and the "url forces blue"
                                // guarantee silently breaks.
                                color = inUrl ? CodexStyles.Url : CodexStyles.Default;
                                caption = false;
                                inColor = false;
                                consumed = true;
                            }
                        }
                        else if (!inColor)
                        {
                            Flush();
                            CodexStyles.TryGetColor(tagValue ?? "", out color);
                            // <color=Caption> is special — bumps to the heading font
                            // and emits two trailing line breaks on close.
                            caption = (tagValue == "Caption");
                            inColor = true;
                            consumed = true;
                        }
                        break;

                    case "b":
                        if (isClose)
                        {
                            if (inBold)
                            {
                                Flush();
                                bold = false;
                                inBold = false;
                                consumed = true;
                            }
                        }
                        else if (!inBold)
                        {
                            Flush();
                            bold = true;
                            inBold = true;
                            consumed = true;
                        }
                        break;

                    case "url":
                        if (isClose)
                        {
                            if (inUrl)
                            {
                                Flush();
                                url = null;
                                inUrl = false;
                                color = preUrlColor;
                                consumed = true;
                            }
                        }
                        else if (!inUrl)
                        {
                            Flush();
                            url = tagValue ?? "";
                            inUrl = true;
                            preUrlColor = color;
                            color = CodexStyles.Url;
                            consumed = true;
                        }
                        break;

                    case "li":
                        Flush();
                        if (!isClose)
                            runs.Add(StyledRun.Bullet());
                        consumed = true;
                        break;

                    case "img":
                        if (!isClose)
                        {
                            int imgClose = source.IndexOf("</img>", tagEnd + 1, System.StringComparison.Ordinal);
                            if (imgClose >= 0)
                            {
                                string path = source.Substring(tagEnd + 1, imgClose - tagEnd - 1).Trim();
                                if (path.Length > 0)
                                {
                                    Flush();
                                    runs.Add(StyledRun.Image(path));
                                    i = imgClose + "</img>".Length;
                                    consumed = true;
                                    continue;
                                }
                            }
                        }
                        break;
                }

                if (consumed)
                {
                    i = tagEnd + 1;
                }
                else
                {
                    buffer.Append(c);
                    i++;
                }
            }

            Flush();
            return InsertSemanticBreaks(runs);
        }

        // Insert synthetic "\n" runs at semantic boundaries so the renderer's
        // simple word-wrap can produce the intended layout:
        //   - One "\n" after the last image of a contiguous img group when
        //     followed by non-image content.
        //   - Two "\n"s after a caption span ends, so caption text reads as a
        //     heading with a blank line below the body that follows.
        // End-of-source transitions don't add trailing breaks.
        static StyledRun[] InsertSemanticBreaks(List<StyledRun> runs)
        {
            var output = new List<StyledRun>(runs.Count + 8);
            bool prevWasImage = false;
            bool prevWasCaption = false;
            foreach (StyledRun r in runs)
            {
                if (prevWasCaption && !r.IsCaption && !r.IsLineBreak)
                {
                    output.Add(LineBreak());
                    output.Add(LineBreak());
                }
                else if (prevWasImage && !r.IsImage && !r.IsLineBreak)
                {
                    output.Add(LineBreak());
                }
                output.Add(r);
                prevWasImage = r.IsImage;
                prevWasCaption = r.IsCaption;
            }
            return output.ToArray();
        }

        static StyledRun LineBreak() => new("\n", CodexStyles.Default, bold: false, url: null);
    }
}
