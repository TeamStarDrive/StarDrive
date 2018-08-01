using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace Ship_Game
{
    [DebuggerTypeProxy(typeof(DropOptionsDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class DropOptions<T> : UIElementV2
    {
        private readonly RecTexPair[] Border = new RecTexPair[16];
        private int BorderCount;

        private Rectangle OpenRect;
        private Rectangle ClickAbleOpenRect;
        private readonly Array<Entry> Options = new Array<Entry>();
        public bool Open;

        public int ActiveIndex;
        public int Count         => Options.Count;
        public bool NotEmpty     => Options.NotEmpty;
        public Entry Active      => Options[ActiveIndex];
        public T ActiveValue     => Options[ActiveIndex].Value;
        public string ActiveName => Options[ActiveIndex].Name;

        public Action<T> OnValueChange;

        private Ref<T> PropertyRef;
        public Expression<Func<T>> PropertyBinding
        {
            set => PropertyRef = new Ref<T>(value);
        }

        public class Entry
        {
            public string Name;
            public bool Hover;
            public Rectangle Rect;
            public T Value;

            public Entry(string name, T value)
            {
                Name  = name;
                Value = value;
            }
            public void UpdateRect(UIElementV2 parent, int index)
            {
                Rect = new Rectangle((int)parent.X, (int)parent.Y + (int)parent.Height * index + 3, (int)parent.Width, 18);
            }
            public override string ToString() => $"{Name}: {Value}";
        }


        public DropOptions(UIElementV2 parent, Rectangle rect) : base(parent, rect)
        {
            Reset();
        }
        public DropOptions(UIElementV2 parent, Vector2 pos, int width, int height)
            : base(parent, pos, new Vector2(width, height))
        {
            Reset();
        }

        public void Clear()
        {
            ActiveIndex = 0;
            Options.Clear();
        }

        public void CopyTo(Entry[] items) => Options.CopyTo(items);

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

        private int IndexOfValue(T value)
        {
            for (int i = 0; i < Options.Count; ++i)
                if (Options[i].Value.Equals(value))
                    return i;
            return -1;
        }

        public bool SetActiveValue(T value)
        {
            int i = IndexOfValue(value);
            if (i == -1)
                return false;
            ActiveIndex = i;
            return true;
        }

        public void AddOption(string option, T value)
        {
            var e = new Entry(option, value);
            e.UpdateRect(this, Options.Count);
            Options.Add(e);
        }

        public bool Contains(Func<T, bool> selector)
        {
            for (int i = 0; i < Options.Count; ++i)
                if (selector(Options[i].Value))
                    return true;
            return false;
        }

        private static bool IsMouseHoveringOver(Rectangle rect)
        {
            return rect.HitTest(Game1.Instance.ScreenManager.input.CursorPosition);
        }

        private string WrappedString(string text)
        {
            float maxWidth = Width - 22;
            if (Fonts.Arial12Bold.MeasureString(text).X <= maxWidth)
                return text;

            var sb = new StringBuilder(text, text.Length + 2);
            do {
                sb.Remove(sb.Length-1, 1);
            } while (Fonts.Arial12Bold.MeasureString(sb).X > maxWidth);

            sb.Append("...");
            return sb.ToString();
        }

        private static Vector2 TextPosition(Rectangle rect)
        {
            return new Vector2(rect.X + 10, rect.Y + rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
        }

        public override void Draw(SpriteBatch batch)
        {
            bool hover = IsMouseHoveringOver(Rect);
            if (hover) // draw border if mouse is hovering
                batch.FillRectangle(Rect, new Color(128, 87, 43, 50));

            for (int i = 0; i < BorderCount; ++i) // draw borders
                Border[i].Draw(batch, Color.White);

            if (Count > 0) // draw active item
            {
                Color color = hover ? Color.White : new Color(255, 239, 208);
                batch.DrawString(Fonts.Arial12Bold, WrappedString(ActiveName), TextPosition(Rect), color);
            }

            if (Open) // draw drop options
            {
                DrawOpenOptions(batch);
            }
        }

        private void DrawOpenOptions(SpriteBatch spriteBatch)
        {
            spriteBatch.FillRectangle(OpenRect, new Color(22, 22, 23));

            int drawOffset = 1;
            for (int i = 0; i < Options.Count; ++i)
            {
                if (i == ActiveIndex)
                    continue;

                Entry e = Options[i];
                e.UpdateRect(this, drawOffset);
                if (IsMouseHoveringOver(e.Rect))
                {
                    var hoverLeft   = new Rectangle(e.Rect.X + 5,  e.Rect.Y + 1, 6, 15);
                    var hoverMiddle = new Rectangle(e.Rect.X + 11, e.Rect.Y + 1, e.Rect.Width - 22, 15);
                    var hoverRight  = new Rectangle(hoverMiddle.X + hoverMiddle.Width, hoverMiddle.Y, 6, 15);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/dropdown_menuitem_hover_left"), hoverLeft, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/dropdown_menuitem_hover_middle"), hoverMiddle, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("NewUI/dropdown_menuitem_hover_right"), hoverRight, Color.White);
                }
                spriteBatch.DrawString(Fonts.Arial12Bold, WrappedString(e.Name), TextPosition(e.Rect), Color.White);
                ++drawOffset;
            }
        }

        public override void Update()
        {
            base.Update();
            if (PropertyRef != null) // ensure our drop-down list is in sync with the property binding!
            {
                T bindingValue = PropertyRef.Value;
                if (!bindingValue.Equals(ActiveValue))
                    SetActiveValue(bindingValue);
            }
        }

        public override bool HandleInput(InputState input)
        {
            bool selectPressed = input.InGameSelect;
            if (!selectPressed)
                return false;

            if (HitTest(input.CursorPosition))
            {
                Open = !Open;
                if (Open && Options.Count == 1)
                    Open = false;

                if (Open) GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                Reset();
                return true;
            }
            if (Open && ClickAbleOpenRect.HitTest(input.CursorPosition))
            {
                for (int i = 0; i < Options.Count; ++i)
                {
                    Entry e = Options[i];
                    if (!e.Rect.HitTest(input.CursorPosition))
                        continue;

                    Active.Rect = e.Rect;
                    e.Rect = new Rectangle();
                    ActiveIndex = i;
                    OnValueChange?.Invoke(ActiveValue);

                    if (PropertyRef != null)
                        PropertyRef.Value = ActiveValue;

                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    Open = false;
                    Reset();
                    return true;
                }
                Open = false;
                Reset();
            }
            return false;
        }

        public override void PerformLegacyLayout(Vector2 pos)
        {
            Pos = pos;
            Reset();
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

    internal sealed class DropOptionsDebugView<T>
    {
        private readonly DropOptions<T> Collection;

        public DropOptionsDebugView(DropOptions<T> collection)
        {
            Collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DropOptions<T>.Entry[] Items
        {
            get
            {
                var items = new DropOptions<T>.Entry[Collection.Count];
                Collection.CopyTo(items);
                return items;
            }
        }
    }
}