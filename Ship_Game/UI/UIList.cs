using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.UI;

namespace Ship_Game
{
    public enum ListLayoutStyle
    {
        Fill, // fill to width of the list
        Clip, // clip elements to width of the list
        Resize, // auto resize list itself
    }

    // Static list of elements, not scrollable
    public class UIList : UIPanel
    {
        // so we can do quick layout
        readonly Array<UIElementV2> Items = new Array<UIElementV2>();

        public int Count => Items.Count;
        public UIElementV2 this[int index] => Items[index];
        
        // By default, 2px from edges is padded
        public Vector2 Padding = new Vector2(2f, 2f);

        public ListLayoutStyle LayoutStyle = ListLayoutStyle.Fill;

        public Vector2 Direction = new Vector2(0f, 1f);

        UIElementV2 HeaderElement, FooterElement;

        public UIElementV2 Header
        {
            get => HeaderElement;
            set
            {
                if (HeaderElement == value) return;
                base.Remove(HeaderElement);
                HeaderElement = value;
                base.Add(value);
            }
        }

        public UIElementV2 Footer
        {
            get => FooterElement;
            set
            {
                if (FooterElement == value) return;
                base.Remove(FooterElement);
                FooterElement = value;
                base.Add(value);
            }
        }

        public override string ToString() => $"List {ElementDescr} Items={Items.Count} Header={Header!=null} Footer={Footer!=null}";

        public UIList(UIElementV2 parent, in Rectangle rect) : base(parent, in rect, Color.TransparentBlack)
        {
        }

        public UIList(UIElementV2 parent, Vector2 pos, Vector2 size) : base(parent, pos, size, Color.TransparentBlack)
        {
        }

        public UIList(in Rectangle rect, Color color) : base(null, rect, color)
        {
        }

        public override T Add<T>(T element)
        {
            RequiresLayout = true;
            Items.Add(element);
            return base.Add(element);
        }

        public void RemoveAt(int index)
        {
            RequiresLayout = true;
            UIElementV2 e = Items[index];
            Items.RemoveAt(index);
            base.Remove(e);
        }

        public override void Remove(UIElementV2 element)
        {
            if (element == null) return;
            RequiresLayout = true;
            Items.Remove(element);
            base.Remove(element);
        }

        public override void RemoveAll()
        {
            Items.Clear();
            base.RemoveAll();
        }

        void LayoutItem(UIElementV2 item, Vector2 pos, Vector2 size)
        {
            bool updated = false;
            if (item.Pos.NotEqual(pos))
            {
                item.Pos = pos;
                updated = true;
            }

            if (LayoutStyle == ListLayoutStyle.Clip ||
                LayoutStyle == ListLayoutStyle.Resize)
            {
                size.X = Math.Min(size.X, item.Width);
                size.Y = Math.Min(size.Y, item.Height);
            }

            if (size.X.NotZero() && item.Width.NotEqual(size.X))
            {
                item.Width = size.X;
                updated = true;
            }
            if (size.Y.NotZero() && item.Height.NotEqual(size.Y))
            {
                item.Height = size.Y;
                updated = true;
            }

            if (updated)
            {
                item.PerformLayout();
            }
        }

        void LayoutItem(UIElementV2 item, ref Vector2 pos, Vector2 size, Vector2 padding)
        {
            LayoutItem(item, pos, size);
            pos += Direction * (item.Size + padding);
        }

        Vector2 MaxDimensions()
        {
            var d = new Vector2(Width, Height);
            if (LayoutStyle == ListLayoutStyle.Resize)
            {
                for (int i = 0; i < Items.Count; ++i)
                {
                    d.X = Math.Max(d.X, Items[i].Width);
                    d.Y = Math.Max(d.Y, Items[i].Height);
                }
                d += Padding*2f;
            }
            return d;
        }

        public override void PerformLayout()
        {
            Vector2 pos = Pos + Padding;

            Vector2 dim = MaxDimensions();
            // swap will enforce Width during Vertical and Height during Horizontal
            Vector2 elemSize = Direction.Swapped() * (dim - Padding*2f);
            elemSize = elemSize.AbsVec(); // make sure size is absolute

            if (elemSize.X.NotZero()) // Vertical list
                Width = dim.X;
            if (elemSize.Y.NotZero()) // horizontal list
                Height = dim.Y;

            if (HeaderElement != null)
                LayoutItem(HeaderElement, ref pos, elemSize, Padding + new Vector2(2f));

            for (int i = 0; i < Items.Count; ++i)
            {
                UIElementV2 item = Items[i];
                if (item.Visible) LayoutItem(item, ref pos, elemSize, Padding);
            }

            if (LayoutStyle == ListLayoutStyle.Resize)
            {
                if (pos.Y > Bottom)
                    Bottom = pos.Y;
            }

            if (FooterElement != null)
            {
                pos = (BotLeft + Padding*Direction.Swapped())
                    - Direction * (FooterElement.Size + Padding);
                LayoutItem(FooterElement, pos, elemSize);
            }
            RequiresLayout = false;
        }

        public override void Update(float deltaTime)
        {
            if (RequiresLayout) // first perform layout:
            {
                PerformLayout();
            }
            // then call update on child items
            base.Update(deltaTime);
        }

        public void ReverseZOrder()
        {
            if (Elements.IsEmpty)
                return;
            int max = Elements.Last.ZOrder;
            Elements.Reverse();
            foreach (UIElementV2 e in Elements)
                e.ZOrder = max--;
        }

        /////////////////////////////////////////////////////////////////////////////
        
        public UIButton AddButton(int titleId, Action<UIButton> click)
        {
            return AddButton(Localizer.Token(titleId), click);
        }

        public UIButton AddButton(string text, Action<UIButton> click)
        {
            UIButton button = Add(new UIButton(this, Vector2.Zero, text));
            button.OnClick  = click;
            button.ClickSfx = "sd_ui_tactical_pause";
            return button;
        }

        public UIButton Add(ButtonStyle style, int titleId, Action<UIButton> click)
        {
            return Add(style, Localizer.Token(titleId), click);
        }

        public UIButton Add(ButtonStyle style, string text, Action<UIButton> click)
        {
            UIButton button = Add(new UIButton(this, style, Vector2.Zero, text));
            button.OnClick  = click;
            button.ClickSfx = "sd_ui_tactical_pause";
            return button;
        }

        /////////////////////////////////////////////////////////////////////////////

        public UICheckBox AddCheckbox(Expression<Func<bool>> binding, int title, int tooltip)
            => Add(new UICheckBox(binding, Fonts.Arial12Bold, Localizer.Token(title), Localizer.Token(tooltip)));
        
        public UICheckBox AddCheckbox(Expression<Func<bool>> binding, string title, string tooltip)
            => Add(new UICheckBox(binding, Fonts.Arial12Bold, title, tooltip));
        
        public UICheckBox AddCheckbox(Expression<Func<bool>> binding, string title, int tooltip)
            => Add(new UICheckBox(binding, Fonts.Arial12Bold, title, Localizer.Token(tooltip)));

        /////////////////////////////////////////////////////////////////////////////
    }
}
