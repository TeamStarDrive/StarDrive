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
        ResizeList, // auto resize list itself
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

        public override string ToString() => $"{TypeName} {ElementDescr} Items={Items.Count} Header={Header!=null} Footer={Footer!=null}";

        public UIList()
        {
            Color = Color.TransparentBlack;
        }

        public UIList(Vector2 pos, Vector2 size) : base(pos, size, Color.TransparentBlack)
        {
        }

        public UIList(in Rectangle rect, Color color) : base(rect, color)
        {
        }

        public override T Add<T>(T element)
        {
            base.Add(element); // NOTE: Must be the first, because base.Add calls element.RemoveFromParent()
            Items.Add(element);
            return element;
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

        void LayoutItem(UIElementV2 item, Vector2 pos, Vector2 itemSize)
        {
            bool updated = false;
            if (item.Pos.NotEqual(pos))
            {
                item.Pos = pos;
                updated = true;
            }

            if (LayoutStyle == ListLayoutStyle.Clip)
            {
                // clip size to list boundary
                itemSize.X = Math.Min(itemSize.X, item.Width);
                itemSize.Y = Math.Min(itemSize.Y, item.Height);
            }
            else if (LayoutStyle == ListLayoutStyle.Fill)
            {
                // expand the size if item doesn't fill to list width
                itemSize.X = Math.Max(itemSize.X, item.Width);
                itemSize.Y = Math.Max(itemSize.Y, item.Height);
            }

            if (itemSize.X.NotZero() && item.Width.NotEqual(itemSize.X))
            {
                item.Width = itemSize.X;
                updated = true;
            }
            if (itemSize.Y.NotZero() && item.Height.NotEqual(itemSize.Y))
            {
                item.Height = itemSize.Y;
                updated = true;
            }

            if (updated)
            {
                item.PerformLayout();
            }
        }

        void LayoutItem(UIElementV2 item, ref Vector2 pos, Vector2 itemSize, Vector2 padding)
        {
            LayoutItem(item, pos, itemSize);
            pos += Direction * (item.Size + padding);
        }

        Vector2 MaxDimensions()
        {
            if (LayoutStyle == ListLayoutStyle.ResizeList)
            {
                var d = new Vector2(8, 8);
                for (int i = 0; i < Items.Count; ++i)
                {
                    d.X = Math.Max(d.X, Items[i].Width);
                    d.Y = Math.Max(d.Y, Items[i].Height);
                }
                d += Padding*2f;
                return d;
            }

            return new Vector2(Width, Height);
        }

        public override void PerformLayout()
        {
            if (UseRelPos && Parent != null)
            {
                Pos = Parent.Pos + RelPos;
            }

            Vector2 pos = Pos + Padding;
            Vector2 maxElemSize = MaxDimensions();

            // swap will enforce Width during Vertical and Height during Horizontal
            Vector2 elemSize = Direction.Swapped() * (maxElemSize - Padding*2f);
            elemSize = elemSize.AbsVec(); // make sure size is absolute

            if (LayoutStyle == ListLayoutStyle.ResizeList)
            {
                // set initialize Width/Height of the list
                if (elemSize.X.NotZero()) // Vertical list
                    Width = maxElemSize.X;
                if (elemSize.Y.NotZero()) // horizontal list
                    Height = maxElemSize.Y;
            }
            
            float maxItemWidth = 0;

            if (HeaderElement != null)
            {
                LayoutItem(HeaderElement, ref pos, elemSize, Padding + new Vector2(2f));
                maxItemWidth = HeaderElement.Width;
            }

            for (int i = 0; i < Items.Count; ++i)
            {
                UIElementV2 item = Items[i];
                if (item.Visible)
                {
                    LayoutItem(item, ref pos, elemSize, Padding);
                    maxItemWidth = Math.Max(maxItemWidth, item.Width);
                }
            }

            if (LayoutStyle == ListLayoutStyle.ResizeList)
            {
                if (pos.Y > Bottom)
                    Bottom = pos.Y;
            }

            if (FooterElement != null)
            {
                pos = (BotLeft + Padding*Direction.Swapped())
                    - Direction * (FooterElement.Size + Padding);
                LayoutItem(FooterElement, pos, elemSize);
                maxItemWidth = Math.Max(maxItemWidth, FooterElement.Width);
            }
            
            if (LayoutStyle == ListLayoutStyle.ResizeList)
            {
                if (maxItemWidth.NotZero())
                    Width = maxItemWidth;
            }

            RequiresLayout = false;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (RequiresLayout) // first perform layout:
            {
                PerformLayout();
            }
            // then call update on child items
            base.Update(fixedDeltaTime);
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
        
        public UIButton AddButton(in LocalizedText text, Action<UIButton> click)
        {
            UIButton button = Add(new UIButton(ButtonStyle.Default, text));
            button.OnClick  = click;
            button.ClickSfx = "sd_ui_tactical_pause";
            return button;
        }

        public UIButton Add(ButtonStyle style, in LocalizedText text, Action<UIButton> click)
        {
            UIButton button = Add(new UIButton(style, text));
            button.OnClick  = click;
            button.ClickSfx = "sd_ui_tactical_pause";
            return button;
        }

        public UICheckBox AddCheckbox(Expression<Func<bool>> binding, 
                                      in LocalizedText title, in LocalizedText tooltip)
            => Add(new UICheckBox(binding, Fonts.Arial12Bold, title, tooltip));

        /////////////////////////////////////////////////////////////////////////////
    }
}
