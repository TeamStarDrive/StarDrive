using System;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public abstract class UIElementContainer : UIElementV2
    {
        // automatic layout spacing between elements
        protected int LayoutMargin = 15;
        protected readonly Array<UIElementV2> Elements = new Array<UIElementV2>();
        protected readonly Array<UIButton>    Buttons  = new Array<UIButton>();

        protected UIElementContainer(Vector2 pos) : base(pos)
        {
        }
        protected UIElementContainer(Vector2 pos, Vector2 size) : base(pos, size)
        {
        }
        protected UIElementContainer(Rectangle rect) : base(rect)
        {
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public override void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < Elements.Count; ++i)
                Elements[i].Draw(spriteBatch);
        }

        public override bool HandleInput(InputState input)
        {
            // iterate input in reverse, so we handle topmost objects before
            for (int i = Elements.Count - 1; i >= 0; --i)
                if (Elements[i].HandleInput(input))
                    return true;
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected T Add<T>(T element) where T : UIElementV2
        {
            Elements.Add(element);
            var button = element as UIButton;
            if (button != null) Buttons.Add(button);
            return element;
        }

        protected T Layout<T>(ref Vector2 pos, T element) where T : UIElementV2
        {
            element.Pos = pos;
            pos.Y += element.Rect.Height + LayoutMargin;
            return element;
        }

        protected T Add<T>(ref Vector2 pos, T element) where T : UIElementV2
        {
            //Layout(ref pos, element);
            pos.Y += element.Rect.Height + LayoutMargin;
            return Add(element);
        }

        protected void Remove<T>(T element) where T : UIElementV2
        {
            if (element == null)
                return;
            Elements.RemoveRef(element);
            var button = element as UIButton;
            if (button != null) Buttons.RemoveRef(button);
        }

        protected void Remove<T>(params T[] elements) where T : UIElementV2
        {
            foreach (T element in elements)
                Remove(element);
        }

        protected void RemoveAll()
        {
            Elements.Clear();
            Buttons.Clear();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        

        // Shared utility functions:
        protected UIButton Button(ref Vector2 pos, string launches, int localization)
            => Button(ref pos, launches, Localizer.Token(localization));
        protected UIButton Button(ref Vector2 pos, string launches, string text)
            => Add(ref pos, new UIButton(pos.X, pos.Y, launches, text));


        protected UIButton Button(float x, float y, string launches, int localization)
            => Button(x, y, launches, Localizer.Token(localization));
        protected UIButton Button(float x, float y, string launches, string text)
            => Add(new UIButton(x, y, launches, text));

        protected UIButton Button(ButtonStyle style, float x, float y, string launches, int localization)
            => Button(style, x, y, launches, Localizer.Token(localization));
        protected UIButton Button(ButtonStyle style, float x, float y, string launches, string text)
            => Add(new UIButton(style, x, y, launches, text));


        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        
        protected UIButton ButtonSmall(float x, float y, string launches, int localization)
            => ButtonSmall(x, y, launches, Localizer.Token(localization));
        protected UIButton ButtonSmall(float x, float y, string launches, string text)
            => Add(new UIButton(ButtonStyle.Small, x, y, launches, text));

        protected UIButton ButtonLow(float x, float y, string launches, int localization)
            => ButtonLow(x, y, launches, Localizer.Token(localization));
        protected UIButton ButtonLow(float x, float y, string launches, string text)
            => Add(new UIButton(ButtonStyle.Low80, x, y, launches, text));

        protected UIButton ButtonMedium(float x, float y, string launches, int localization)
            => ButtonMedium(x, y, launches, Localizer.Token(localization));
        protected UIButton ButtonMedium(float x, float y, string launches, string text)
            => Add(new UIButton(ButtonStyle.Medium, x, y, launches, text));

        protected UIButton ButtonMediumMenu(float x, float y, string launches, int localization)
            => ButtonMediumMenu(x, y, launches, Localizer.Token(localization));
        protected UIButton ButtonMediumMenu(float x, float y, string launches, string text)
            => Add(new UIButton(ButtonStyle.MediumMenu, x, y, launches, text));

        protected UIButton ButtonDip(float x, float y, string launches, int localization)
            => ButtonDip(x, y, launches, Localizer.Token(localization));
        protected UIButton ButtonDip(float x, float y, string launches, string text)
            => Add(new UIButton(ButtonStyle.BigDip, x, y, launches, text));


        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected UICheckBox Checkbox(ref Vector2 pos, Expression<Func<bool>> binding, int title, int tooltip)
            => Add(ref pos, new UICheckBox(pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));

        protected UICheckBox Checkbox(ref Vector2 pos, Expression<Func<bool>> binding, string title, string tooltip)
            => Add(ref pos, new UICheckBox(pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));

        protected UICheckBox Checkbox(ref Vector2 pos, Expression<Func<bool>> binding, string title, int tooltip)
            => Add(ref pos, new UICheckBox(pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));


        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected FloatSlider Slider(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(rect, text, min, max, value));

        protected FloatSlider SliderPercent(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(SliderStyle.Percent, rect, text, min, max, value));


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
            => Add(new DropOptions<T>(rect));

        protected DropOptions<T> DropOptions<T>(int x, int y, int width, int height)
            => Add(new DropOptions<T>(new Rectangle(x, y, width, height)));


        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected UILabel Label(Vector2 pos, string text)                       => Add(new UILabel(pos, text));
        protected UILabel Label(Vector2 pos, string text,      SpriteFont font) => Add(new UILabel(pos, text, font));
        protected UILabel Label(Vector2 pos, int localization)                  => Add(new UILabel(pos, localization));
        protected UILabel Label(Vector2 pos, int localization, SpriteFont font) => Add(new UILabel(pos, localization, font));


        protected UILabel Label(float x, float y, string text)                       => Add(new UILabel(new Vector2(x,y), text));
        protected UILabel Label(float x, float y, string text,      SpriteFont font) => Add(new UILabel(new Vector2(x,y), text, font));
        protected UILabel Label(float x, float y, int localization)                  => Add(new UILabel(new Vector2(x,y), localization));
        protected UILabel Label(float x, float y, int localization, SpriteFont font) => Add(new UILabel(new Vector2(x,y), localization, font));


        /////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
