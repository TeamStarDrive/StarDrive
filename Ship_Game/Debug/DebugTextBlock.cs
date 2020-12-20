using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Debug
{
    public struct  DebugTextBlock
    {
        public Array<string> Lines;
        public string Header;
        public Color HeaderColor;
        public string Footer;
        public Array<Color> LineColor;

        public void AddRange(Array<string> lines)
        {            
            foreach (string line in lines)            
                AddLine(line);                            
        }

        public void AddRange(Array<string> lines, Color color)
        {
            foreach (string line in lines)
            {
                AddLine(line);
                LineColor.Add(color);
            }
        }

        public Array<string> GetFormattedLines()
        {
            var text = new Array<string>();
            if (Header.NotEmpty()) text.Add(Header);
            text.AddRange(Lines);
            if (Footer.NotEmpty()) text.Add(Footer);
            return text;
        }

        public void AddLine(string text) => AddLine(text, GetLastColor());

        public void AddLine(string text, Color color)
        {
            Lines = Lines ?? new Array<string>();
            LineColor = LineColor ?? new Array<Color>();
            Lines.Add(text);
            LineColor.Add(color);
            
        }

        Color GetLastColor()
        {
            if (LineColor?.IsEmpty ?? true) return Color.White;
            return LineColor.Last;
        }

    }
}