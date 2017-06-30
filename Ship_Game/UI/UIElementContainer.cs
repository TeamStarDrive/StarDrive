﻿using System;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public enum LayoutStyle
    {
        HorizontalEven,   // elements are spaced evenly
        HorizontalPacked, // elements are packed tightly
        VerticalEven,     // 
        VerticalPacked,   //
    }

    public abstract class UIElementContainer : UIElementV2
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected Vector2 Margin = new Vector2(15, 15);
        protected LayoutStyle CurrentLayout = LayoutStyle.HorizontalEven;

        protected readonly Array<UIElementV2> Elements = new Array<UIElementV2>();

        // @todo Remove this list of buttons. It's purely for backwards compatibility
        protected readonly Array<UIButton> Buttons = new Array<UIButton>();

        protected bool LayoutStarted = false;
        protected Vector2 LayoutCursor = Vector2.Zero;
        protected Vector2 LayoutStep = Vector2.Zero;

        public LayoutStyle Layout
        {
            get => CurrentLayout;
            set
            {
                if (CurrentLayout == value)
                    return;
                CurrentLayout = value;
                RequiresLayout = true;
            }
        }

        public bool IsEvenLayout => CurrentLayout == LayoutStyle.HorizontalEven
                                 || CurrentLayout == LayoutStyle.VerticalEven;

        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected UIElementContainer(UIElementV2 parent, Vector2 pos) : base(parent, pos)
        {
        }
        protected UIElementContainer(UIElementV2 parent, Vector2 pos, Vector2 size) : base(parent, pos, size)
        {
        }
        protected UIElementContainer(UIElementV2 parent, Rectangle rect) : base(parent, rect)
        {
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public override void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < Elements.Count; ++i)
                Elements[i].Draw(spriteBatch);

            ToolTip.Draw(spriteBatch);
        }

        public override bool HandleInput(InputState input)
        {
            // iterate input in reverse, so we handle topmost objects before
            for (int i = Elements.Count - 1; i >= 0; --i)
                if (Elements[i].HandleInput(input))
                    return true;
            return false;
        }

        private Vector2 LayoutDirection()
        {
            switch (CurrentLayout)
            {
                default:
                case LayoutStyle.HorizontalEven:
                case LayoutStyle.HorizontalPacked: return new Vector2(1f, 0f);
                case LayoutStyle.VerticalEven:
                case LayoutStyle.VerticalPacked: return new Vector2(0f, 1f);
            }
        }

        public override void PerformLegacyLayout(Vector2 pos)
        {
            LayoutChildElements(pos);
        }

        public override void Update()
        {
            base.Update(); // layout self first
            LayoutChildElements(Pos);
        }

        public void LayoutChildElements(Vector2 pos)
        {
            if (Elements.IsEmpty)
                return;

            Vector2 direction = LayoutDirection();
            Vector2 cursor = pos;

            if (IsEvenLayout)
            {
                Vector2 adjustedSize = Size - Margin * Elements.Count;
                Vector2 evenSpacing = (adjustedSize / Elements.Count) + Margin;
                for (int i = 0; i < Elements.Count; ++i)
                {
                    UIElementV2 e = Elements[i];
                    e.Pos = cursor;
                    e.Update();
                    cursor += evenSpacing * direction;
                }
            }
            else
            {
                for (int i = 0; i < Elements.Count; ++i)
                {
                    UIElementV2 e = Elements[i];
                    e.Pos = cursor;
                    e.Update();
                    cursor += (e.Size + Margin) * direction;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public T Add<T>(T element) where T : UIElementV2
        {
            Elements.Add(element);
            var button = element as UIButton;
            if (button != null) Buttons.Add(button);
            return element;
        }

        public void Remove<T>(T element) where T : UIElementV2
        {
            if (element == null)
                return;
            Elements.RemoveRef(element);
            var button = element as UIButton;
            if (button != null) Buttons.RemoveRef(button);
        }

        public void Remove<T>(params T[] elements) where T : UIElementV2
        {
            foreach (T element in elements)
                Remove(element);
        }

        public void RemoveAll()
        {
            Elements.Clear();
            Buttons.Clear();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public void BeginLayout(float x, float y)
        {
            LayoutStarted = true;
            LayoutCursor = new Vector2(x, y);
            LayoutStep = new Vector2(15f, 15f);
        }

        public void BeginVLayout(float x, float y, float ystep = 15f)
        {
            LayoutStarted = true;
            LayoutCursor = new Vector2(x, y);
            LayoutStep = new Vector2(0f, ystep);
        }

        public void BeginVLayout(Vector2 pos, float ystep = 15f)
        {
            LayoutStarted = true;
            LayoutCursor = pos;
            LayoutStep = new Vector2(0f, ystep);
        }

        public void EndLayout()
        {
            LayoutStarted = false;
            Elements.Sort((a,b) => a.ZOrder - b.ZOrder);
        }

        private Vector2 LayoutNext()
        {
            if (!LayoutStarted)
                throw new InvalidOperationException("You must call BeginLayout befor calling auto-layout methods");

            Vector2 result = LayoutCursor;
            LayoutCursor += LayoutStep;
            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        // Shared utility functions:
        protected UIButton Button(Vector2 pos, string launches, int titleId)
            => Add(new UIButton(this, pos, launches, Localizer.Token(titleId)));
        protected UIButton Button(Vector2 pos, string launches, string text)
            => Add(new UIButton(this, pos, launches, text));

        protected UIButton Button(int titleId, UIButton.ClickHandler click)
        {
            return Button(Localizer.Token(titleId), click);
        }
        protected UIButton Button(string text, UIButton.ClickHandler click)
        {
            UIButton button = Add(new UIButton(this, LayoutNext(), text));
            button.OnClick += click;
            button.ClickSfx = "sd_ui_tactical_pause";
            return button;
        }


        protected UIButton Button(float x, float y, string launches, int titleId)
            => Add(new UIButton(this, x, y, launches, Localizer.Token(titleId)));
        protected UIButton Button(float x, float y, string launches, string text)
            => Add(new UIButton(this, x, y, launches, text));

        protected UIButton Button(ButtonStyle style, float x, float y, string launches, int titleId)
            => Add(new UIButton(this, style, x, y, launches, Localizer.Token(titleId)));
        protected UIButton Button(ButtonStyle style, float x, float y, string launches, string text)
            => Add(new UIButton(this, style, x, y, launches, text));

        protected UIButton Button(ButtonStyle style, float x, float y)
            => Add(new UIButton(this, style, x, y));

        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected UIButton ButtonSmall(float x, float y, string launches, int titleId)
            => Add(new UIButton(this, ButtonStyle.Small, x, y, launches, Localizer.Token(titleId)));
        protected UIButton ButtonSmall(float x, float y, string launches, string text)
            => Add(new UIButton(this, ButtonStyle.Small, x, y, launches, text));

        protected UIButton ButtonLow(float x, float y, string launches, int titleId)
            => Add(new UIButton(this, ButtonStyle.Low80, x, y, launches, Localizer.Token(titleId)));
        protected UIButton ButtonLow(float x, float y, string launches, string text)
            => Add(new UIButton(this, ButtonStyle.Low80, x, y, launches, text));

        protected UIButton ButtonMedium(float x, float y, string launches, int titleId)
            => Add(new UIButton(this, ButtonStyle.Medium, x, y, launches, Localizer.Token(titleId)));
        protected UIButton ButtonMedium(float x, float y, string launches, string text)
            => Add(new UIButton(this, ButtonStyle.Medium, x, y, launches, text));

        protected UIButton ButtonMediumMenu(float x, float y, string launches, int titleId)
            => Add(new UIButton(this, ButtonStyle.MediumMenu, x, y, launches, Localizer.Token(titleId)));
        protected UIButton ButtonMediumMenu(float x, float y, string launches, string text)
            => Add(new UIButton(this, ButtonStyle.MediumMenu, x, y, launches, text));

        protected UIButton ButtonDip(float x, float y, string launches, int titleId)
            => Add(new UIButton(this, ButtonStyle.BigDip, x, y, launches, Localizer.Token(titleId)));
        protected UIButton ButtonDip(float x, float y, string launches, string text)
            => Add(new UIButton(this, ButtonStyle.BigDip, x, y, launches, text));

        protected CloseButton CloseButton(float x, float y)
            => Add(new CloseButton(this, new Rectangle((int)x, (int)y, 20, 20)));

        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, int title, int tooltip)
            => Add(new UICheckBox(this, pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, string title, string tooltip)
            => Add(new UICheckBox(this, pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, string title, int tooltip)
            => Add(new UICheckBox(this, pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));

        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, int title, int tooltip)
            => Add(new UICheckBox(this, x, y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, string title, string tooltip)
            => Add(new UICheckBox(this, x, y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, string title, int tooltip)
            => Add(new UICheckBox(this, x, y, binding, Fonts.Arial12Bold, title, tooltip));

        protected UICheckBox Checkbox(Expression<Func<bool>> binding, int title, int tooltip)
            => Checkbox(LayoutNext(), binding, title, tooltip);
        protected UICheckBox Checkbox(Expression<Func<bool>> binding, string title, string tooltip)
            => Checkbox(LayoutNext(), binding, title, tooltip);
        protected UICheckBox Checkbox(Expression<Func<bool>> binding, string title, int tooltip)
            => Checkbox(LayoutNext(), binding, title, tooltip);


        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected FloatSlider Slider(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(this, rect, text, min, max, value));

        protected FloatSlider SliderPercent(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(this, SliderStyle.Percent, rect, text, min, max, value));


        protected FloatSlider Slider(int x, int y, int w, int h, string text, float min, float max, float value)
            => Slider(new Rectangle(x, y, w, h), text, min, max, value);

        protected FloatSlider SliderPercent(int x, int y, int w, int h, string text, float min, float max, float value)
            => SliderPercent(new Rectangle(x, y, w, h), text, min, max, value);

        protected FloatSlider Slider(Vector2 pos, int w, int h, string text, float min, float max, float value)
            => Slider(new Rectangle((int)pos.X, (int)pos.Y, w, h), text, min, max, value);

        protected FloatSlider SliderPercent(Vector2 pos, int w, int h, string text, float min, float max, float value)
            => SliderPercent(new Rectangle((int)pos.X, (int)pos.Y, w, h), text, min, max, value);


        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected DropOptions<T> DropOptions<T>(Rectangle rect)
            => Add(new DropOptions<T>(this, rect));

        protected DropOptions<T> DropOptions<T>(int x, int y, int width, int height)
            => Add(new DropOptions<T>(this, new Rectangle(x, y, width, height)));

        protected DropOptions<T> DropOptions<T>(int width, int height, int zorder = 0)
        {
            DropOptions<T> option = Add(new DropOptions<T>(this, LayoutNext(), width, height));
            option.ZOrder = zorder;
            return option;
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected UILabel Label(Vector2 pos, string text) => Add(new UILabel(this, pos, text));
        protected UILabel Label(Vector2 pos, int titleId) => Add(new UILabel(this, pos, titleId));
        protected UILabel Label(Vector2 pos, string text, SpriteFont font) => Add(new UILabel(this, pos, text, font));
        protected UILabel Label(Vector2 pos, int titleId, SpriteFont font) => Add(new UILabel(this, pos, titleId, font));


        protected UILabel Label(float x, float y, string text) => Label(new Vector2(x, y), text);
        protected UILabel Label(float x, float y, int titleId) => Label(new Vector2(x, y), titleId);
        protected UILabel Label(float x, float y, string text, SpriteFont font) => Label(new Vector2(x, y), text, font);
        protected UILabel Label(float x, float y, int titleId, SpriteFont font) => Label(new Vector2(x, y), titleId, font);


        protected UILabel Label(string text) => Add(new UILabel(this, LayoutNext(), text));
        protected UILabel Label(int titleId) => Add(new UILabel(this, LayoutNext(), titleId));


        protected UILabel Label(int titleId, UILabel.ClickHandler click)
        {
            return Label(Localizer.Token(titleId), click);
        }
        protected UILabel Label(string text, UILabel.ClickHandler click)
        {
            UILabel label = Add(new UILabel(this, LayoutNext(), text));
            label.OnClick += click;
            return label;
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
