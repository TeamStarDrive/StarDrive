using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public class Entry<T>
    {
        public string Name;
        public bool Hover;
        public Rectangle ClickRect;
        public T Value;
    }

    public class DropOptions<T>
    {
        private readonly RecTexPair[] Border = new RecTexPair[16];
        private int BorderCount;

        public Rectangle Rect;
        private Rectangle OpenRect;
        private Rectangle ClickAbleOpenRect;
        private readonly Array<Entry<T>> Options = new Array<Entry<T>>();
        public bool Open;

        public int ActiveIndex;
        public int Count         => Options.Count;
        public bool NotEmpty     => Options.NotEmpty;
        public Entry<T> Active   => Options[ActiveIndex];
        public T ActiveValue     => Options[ActiveIndex].Value;
        public string ActiveName => Options[ActiveIndex].Name;

        public void Clear()
        {
            ActiveIndex = 0;
            Options.Clear();
        }

        public DropOptions(Rectangle rect)
        {
            Rect = rect;
            Reset();
        }

        public int IndexOfEntry(string name)
        {
            for (int i = 0; i < Options.Count; ++i)
                if (Options[i].Name == name)
                    return i;
            return -1;
        }

        public bool SetActiveEntry(string name)
        {
            int i = IndexOfEntry(name);
            if (i == -1)
                return false;
            ActiveIndex = i;
            return true;
        }

        public void AddOption(string option, T value)
        {
            Options.Add(new Entry<T>
            {
                Name      = option,
                ClickRect = new Rectangle(Rect.X, Rect.Y + Rect.Height * Options.Count + 3, Rect.Width, 18),
                Value     = value
            });
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            bool hover = false;
            Vector2 mousePos = Mouse.GetState().Pos();
            if (Rect.HitTest(mousePos))
            {
                hover = true;
            }
            if (hover)
            {
                spriteBatch.FillRectangle(Rect, new Color(128, 87, 43, 50));
            }
            for (int i = 0; i < BorderCount; ++i)
                Border[i].Draw(spriteBatch, Color.White);

            if (!hover && Options.Count > 0)
            {
                
                string txt = Options[ActiveIndex].Name;
                bool addDots = false;
                while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(Rect.Width - 22))
                {
                    txt = txt.Remove(txt.Length - 1);
                    addDots = true;
                }
                if (addDots)
                {
                    txt = string.Concat(txt, "...");
                }
                spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2(Rect.X + 10, Rect.Y + Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2), new Color(255, 239, 208));
            }
            else if(Options.Count >0)
            {
                string txt = Options[ActiveIndex].Name;
                bool addDots = false;
                while (Fonts.Arial12Bold.MeasureString(txt).X > (Rect.Width - 22))
                {
                    txt = txt.Remove(txt.Length - 1);
                    addDots = true;
                }
                if (addDots)
                {
                    txt = string.Concat(txt, "...");
                }
                spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((Rect.X + 10), (Rect.Y + Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
            }
            if (Open)
            {
                spriteBatch.FillRectangle(OpenRect, new Color(22, 22, 23));
                int i = 1;
                foreach (Entry<T> e in Options)
                {
                    if (e.Name == Options[ActiveIndex].Name)
                    {
                        continue;
                    }
                    Rectangle rectangle = new Rectangle(Rect.X, Rect.Y + Rect.Height * i + 3, Rect.Width, 18);
                    Rectangle rectangle1 = rectangle;
                    e.ClickRect = rectangle;
                    e.ClickRect = rectangle1;
                    if (e.ClickRect.HitTest(mousePos))
                    {
                        var hoverLeft   = new Rectangle(e.ClickRect.X + 5, e.ClickRect.Y + 1, 6, 15);
                        var hoverMiddle = new Rectangle(e.ClickRect.X + 11, e.ClickRect.Y + 1, e.ClickRect.Width - 22, 15);
                        var hoverRight  = new Rectangle(hoverMiddle.X + hoverMiddle.Width, hoverMiddle.Y, 6, 15);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/dropdown_menuitem_hover_left"), hoverLeft, Color.White);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/dropdown_menuitem_hover_middle"), hoverMiddle, Color.White);
                        spriteBatch.Draw(ResourceManager.Texture("NewUI/dropdown_menuitem_hover_right"), hoverRight, Color.White);
                    }
                    string txt = e.Name;
                    bool addDots = false;
                    while (Fonts.Arial12Bold.MeasureString(txt).X > (float)(Rect.Width - 22))
                    {
                        txt = txt.Remove(txt.Length - 1);
                        addDots = true;
                    }
                    if (addDots)
                    {
                        txt = string.Concat(txt, "...");
                    }
                    spriteBatch.DrawString(Fonts.Arial12Bold, txt, new Vector2((float)(Rect.X + 10), (float)(e.ClickRect.Y + e.ClickRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2)), Color.White);
                    i++;
                }
            }
        }

        public void DrawGrayed(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < BorderCount; ++i) Border[i].Draw(spriteBatch, Color.DarkGray);
            spriteBatch.DrawString(Fonts.Arial12Bold, "-", new Vector2(Rect.X + 10, Rect.Y + Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2), Color.DarkGray);
        }

        public virtual void HandleInput(InputState input)
        {
            if (Rect.HitTest(input.CursorPosition) && input.InGameSelect)
            {
                Open = !Open;
                if (Open && Options.Count == 1)
                {
                    Open = false;
                }
                if (Open)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                }
                Reset();
            }
            else if (ClickAbleOpenRect.HitTest(input.CursorPosition))
            {
                if (Open)
                {
                    foreach (Entry<T> e in Options)
                    {
                        if (!e.ClickRect.HitTest(input.CursorPosition) || !input.InGameSelect)
                        {
                            continue;
                        }
                        Options[ActiveIndex].ClickRect = e.ClickRect;
                        e.ClickRect = new Rectangle();
                        ActiveIndex = Options.IndexOf(e);
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        Open = false;
                        Reset();
                        return;
                    }
                }
            }
            else if (input.InGameSelect)
            {
                Open = false;
                Reset();
            }            
        }
        
        public void Reset()
        {
            Array.Clear(Border, 0, Border.Length);

            var ttl = ResourceManager.Texture("NewUI/dropdown_menu_corner_TL");
            var ttr = ResourceManager.Texture("NewUI/dropdown_menu_corner_TR");
            var tbl = ResourceManager.Texture("NewUI/dropdown_menu_corner_BL");
            var tbr = ResourceManager.Texture("NewUI/dropdown_menu_corner_BR");
            var left  = ResourceManager.Texture("NewUI/dropdown_menu_sides_left");
            var right = ResourceManager.Texture("NewUI/dropdown_menu_sides_right");
            var top = ResourceManager.Texture("NewUI/dropdown_menu_sides_top");
            var bot = ResourceManager.Texture("NewUI/dropdown_menu_sides_bottom");

            int x = Rect.X, y = Rect.Y, w = Rect.Width, h = Rect.Height;
            var tl = Border[0] = new RecTexPair(x, y, ttl);
            var tr = Border[1] = new RecTexPair(x+w-ttr.Width, y, ttr);
            var bl = Border[2] = new RecTexPair(x, y+h-tbl.Height, tbl);
            var br = Border[3] = new RecTexPair(x+w-tbl.Width, y+h-tbr.Height, tbr);
            Border[4] = new RecTexPair(x, y+6, h-12, left);
            Border[5] = new RecTexPair(x+w-6, y+6, h-12, right);
            Border[6] = new RecTexPair(x+tl.W, y, top, w-tl.W-tr.W);
            Border[7] = new RecTexPair(x+tl.W, y+h-6, bot, w-bl.W-br.W);
            BorderCount = 8;
            if (Open)
            {
                int height = (Options.Count - 1) * 18;
                OpenRect = new Rectangle(x + 6, y + h + 3 + 6, w - 12, height - 12);
                ClickAbleOpenRect = new Rectangle(x + 6, y + h + 3, w - 12, height - 6);

                tl = Border[8]  = new RecTexPair(x, y+h+3, ttl);
                tr = Border[9]  = new RecTexPair(x+w-ttr.Width, tl.Y, ttr);
                bl = Border[10] = new RecTexPair(x, tl.Y+height-tbl.Height, tbl);
                br = Border[11] = new RecTexPair(x+w-tbl.Width, tl.Y+height-tbr.Height, tbr);
                Border[12] = new RecTexPair(x, tl.Y+6, height-12, left);
                Border[13] = new RecTexPair(x+w-6, tl.Y+6, height-12, right);
                Border[14] = new RecTexPair(x+tl.W, tl.Y, top, w-tl.W-tr.W);
                Border[15] = new RecTexPair(x+tl.W, tl.Y+height-6, bot, w-bl.W-br.W);
                BorderCount = 16;
            }
        }

        private struct RecTexPair
        {
            private readonly Rectangle Rect;
            private readonly Texture2D Tex;
            public int Y => Rect.Y;
            public int W => Rect.Width;

            public RecTexPair(int x, int y, Texture2D t)
            {
                Rect = new Rectangle(x, y, t.Width, t.Height);
                Tex = t;
            }
            public RecTexPair(int x, int y, int h, Texture2D t)
            {
                Rect = new Rectangle(x, y, t.Width, h);
                Tex = t;
            }
            public RecTexPair(int x, int y, Texture2D t, int w)
            {
                Rect = new Rectangle(x, y, w, t.Height);
                Tex = t;
            }
            public void Draw(SpriteBatch spriteBatch, Color color)
            {
                spriteBatch.Draw(Tex, Rect, color);
            }
        }
    }
}