using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    // Static list of elements, not scrollable
    public class UIList : UIPanel
    {
        // so we can do quick layout
        readonly Array<UIElementV2> Items = new Array<UIElementV2>();

        public int Count => Items.Count;
        public UIElementV2 this[int index] => Items[index];
        
        // By default, 2px from edges is padded
        public Vector2 Padding = new Vector2(2f, 2f);

        UIElementV2 TitleElement, FooterElement;

        public UIElementV2 Title
        {
            get => TitleElement;
            set
            {
                if (TitleElement == value) return;
                base.Remove(TitleElement);
                TitleElement = value;
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

        public UIList(UIElementV2 parent, in Rectangle rect) : base(parent, in rect, Color.TransparentBlack)
        {
        }

        public UIList(UIElementV2 parent, in Rectangle rect, Color color) : base(parent, in rect, color)
        {
        }

        public new T Add<T>(T element) where T : UIElementV2
        {
            Items.Add(element);
            return base.Add(element);
        }

        public void RemoveAt(int index)
        {
            Remove(Items[index]); // go through the virtual call hierarchy for removing items
        }

        public override void Remove(UIElementV2 element)
        {
            if (element == null) return;
            Items.Remove(element);
            base.Remove(element);
        }

        public override void RemoveAll()
        {
            Items.Clear();
            base.RemoveAll();
        }

        static void LayoutItem(UIElementV2 item, Vector2 pos, float width)
        {
            if (item.Pos.NotEqual(pos) || item.Width.NotEqual(width))
            {
                item.Pos = pos;
                item.Width = width;
                item.PerformLayout();
            }
        }

        static void LayoutItem(UIElementV2 item, ref Vector2 pos, float width, float padY)
        {
            LayoutItem(item, pos, width);
            pos.Y += item.Height + padY;
        }

        public override void Update(float deltaTime)
        {
            // first perform layout:
            Vector2 pos = Pos + Padding;
            float width = Width - Padding.X*2f;

            if (TitleElement != null)
                LayoutItem(TitleElement, ref pos, width, Padding.Y+2f);

            for (int i = 0; i < Items.Count; ++i)
            {
                UIElementV2 item = Items[i];
                if (item.Visible) LayoutItem(item, ref pos, width, Padding.Y);
            }

            if (FooterElement != null)
            {
                pos.Y = Bottom - (FooterElement.Height + Padding.Y + 2f);
                LayoutItem(FooterElement, pos, width);
            }

            // then call update on child items
            base.Update(deltaTime);
        }
    }
}
