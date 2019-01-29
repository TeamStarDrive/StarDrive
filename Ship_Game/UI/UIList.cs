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

        public override string ToString() => $"List {Pos} Items:{Items.Count} Header:{Header!=null} Footer:{Footer!=null}";

        public UIList(UIElementV2 parent, in Rectangle rect) : base(parent, in rect, Color.TransparentBlack)
        {
        }

        public UIList(UIElementV2 parent, Vector2 pos, Vector2 size) : base(parent, pos, size, Color.TransparentBlack)
        {
        }

        public UIList(UIElementV2 parent, in Rectangle rect, Color color) : base(parent, in rect, color)
        {
        }

        public T AddItem<T>(T element) where T : UIElementV2
        {
            RequiresLayout = true;
            Items.Add(element);
            return base.Add(element);
        }

        public T AddItem<T>() where T : UIElementV2, new()
        {
            return AddItem(new T());
        }

        public SplitElement AddSplit(UIElementV2 a, UIElementV2 b)
        {
            var split = new SplitElement(a, b);
            a.Parent = split;
            b.Parent = split;
            return AddItem(split);
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

        void LayoutItem(UIElementV2 item, Vector2 pos, float width)
        {
            bool updated = false;
            if (item.Pos.NotEqual(pos))
            {
                item.Pos = pos;
                updated = true;
            }

            if (LayoutStyle == ListLayoutStyle.Clip ||
                LayoutStyle == ListLayoutStyle.Resize)
                width = Math.Min(width, item.Width);

            if (item.Width.NotEqual(width))
            {
                item.Width = width;
                updated = true;
            }

            if (updated)
            {
                item.PerformLayout();
            }
        }

        void LayoutItem(UIElementV2 item, ref Vector2 pos, float width, float padY)
        {
            LayoutItem(item, pos, width);
            pos.Y += item.Height + padY;
        }

        float MaxWidth()
        {
            float width = Width;
            if (LayoutStyle == ListLayoutStyle.Resize)
            {
                for (int i = 0; i < Items.Count; ++i)
                    width = Math.Max(width, Items[i].Width);
                width += Padding.X*2f;
            }
            return width;
        }

        public override void PerformLayout()
        {
            Vector2 pos = Pos + Padding;

            Width = MaxWidth(); // update width if needed
            float elemWidth = Width - Padding.X*2f;

            if (HeaderElement != null)
                LayoutItem(HeaderElement, ref pos, elemWidth, Padding.Y+2f);

            for (int i = 0; i < Items.Count; ++i)
            {
                UIElementV2 item = Items[i];
                if (item.Visible) LayoutItem(item, ref pos, elemWidth, Padding.Y);
            }

            if (LayoutStyle == ListLayoutStyle.Resize)
            {
                if (pos.Y > Bottom)
                    Bottom = pos.Y;
            }

            if (FooterElement != null)
            {
                pos.Y = Bottom - (FooterElement.Height + Padding.Y + 2f);
                LayoutItem(FooterElement, pos, elemWidth);
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
        
        public UIButton AddButton(int titleId, UIButton.ClickHandler click)
        {
            return AddButton(Localizer.Token(titleId), click);
        }

        public UIButton AddButton(string text, UIButton.ClickHandler click)
        {
            UIButton button = AddItem(new UIButton(this, Vector2.Zero, text));
            button.OnClick += click;
            button.ClickSfx = "sd_ui_tactical_pause";
            return button;
        }

        /////////////////////////////////////////////////////////////////////////////

        public UICheckBox AddCheckbox(Expression<Func<bool>> binding, int title, int tooltip)
            => AddItem(new UICheckBox(binding, Fonts.Arial12Bold, Localizer.Token(title), Localizer.Token(tooltip)));
        
        public UICheckBox AddCheckbox(Expression<Func<bool>> binding, string title, string tooltip)
            => AddItem(new UICheckBox(binding, Fonts.Arial12Bold, title, tooltip));
        
        public UICheckBox AddCheckbox(Expression<Func<bool>> binding, string title, int tooltip)
            => AddItem(new UICheckBox(binding, Fonts.Arial12Bold, title, Localizer.Token(tooltip)));

        /////////////////////////////////////////////////////////////////////////////
    }
}
