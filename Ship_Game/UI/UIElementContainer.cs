using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.UI.Effects;

namespace Ship_Game
{
    public enum LayoutStyle
    {
        HorizontalEven,   // elements are spaced evenly
        HorizontalPacked, // elements are packed tightly
        VerticalEven,     // 
        VerticalPacked   //
    }

    public abstract class UIElementContainer : UIElementV2
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected Vector2     Margin        = new Vector2(15f, 15f);
        protected LayoutStyle CurrentLayout = LayoutStyle.HorizontalEven;

        protected readonly Array<UIElementV2> Elements = new Array<UIElementV2>();
        public IReadOnlyList<UIElementV2> Children => Elements;
        protected bool LayoutStarted;
        protected Vector2 LayoutCursor = Vector2.Zero;
        protected Vector2 LayoutStep   = Vector2.Zero;

        /// <summary>
        /// If enabled, UI elements will be drawn with a fixed delay
        /// in their appropriate ZOrder
        /// </summary>
        public bool DebugDraw;
        int DebugDrawIndex;
        float DebugDrawTimer;
        const float DebugDrawInterval = 0.5f;

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
        protected UIElementContainer(UIElementV2 parent, Rectangle rect) : base(parent, rect)
        {
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;

            if (!DebugDraw)
            {
                for (int i = 0; i < Elements.Count; ++i)
                {
                    UIElementV2 e = Elements[i];
                    if (e.Visible) e.Draw(batch);
                }
            }
            else
            {
                for (int i = 0; i <= DebugDrawIndex && i < Elements.Count; ++i)
                {
                    UIElementV2 e = Elements[i];
                    if (!e.Visible) continue;
                    e.Draw(batch);
                    if (i == DebugDrawIndex)
                        batch.DrawRectangle(e.Rect, Color.Orange);
                }
            }

            if (ToolTip.Hotkey.IsEmpty())
                ToolTip.Draw(batch);
        }

        public override bool HandleInput(InputState input)
        {
            if (Visible && Enabled)
            {
                // iterate input in reverse, so we handle topmost objects before
                for (int i = Elements.Count - 1; i >= 0; --i)
                {
                    UIElementV2 e = Elements[i];
                    if (e.Visible && e.Enabled && e.HandleInput(input))
                        return true;
                }
            }
            return false;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            for (int i = 0; i < Elements.Count; ++i)
            {
                UIElementV2 e = Elements[i];
                if (e.Visible)
                {
                    e.Update(deltaTime);
                    if (e.DeferredRemove) { Elements.RemoveAtSwapLast(i--); }
                    // Update directly modified Elements array?
                    else if (Elements[i] != e) { --i; }
                }
            }

            if (DebugDraw)
            {
                DebugDrawTimer -= deltaTime;
                if (DebugDrawTimer <= 0f)
                {
                    DebugDrawTimer = DebugDrawInterval;
                    ++DebugDrawIndex;
                    if (DebugDrawIndex >= Elements.Count)
                        DebugDrawIndex = 0;
                    else if (DebugDrawIndex == Elements.Count - 1)
                        DebugDrawTimer *= 5f; // freeze the UI now
                }
            }
        }

        private Vector2 LayoutDirection()
        {
            switch (CurrentLayout)
            {
                default:
                case LayoutStyle.HorizontalEven:
                case LayoutStyle.HorizontalPacked: return new Vector2(1f, 0f);
                case LayoutStyle.VerticalEven:
                case LayoutStyle.VerticalPacked:   return new Vector2(0f, 1f);
            }
        }

        public override void PerformLayout()
        {
            if (!Visible)
                return;

            base.PerformLayout(); // layout self first
            LayoutChildElements(Pos);
        }

        private void LayoutChildElements(Vector2 pos)
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
                    if (!e.Visible) continue;
                    e.Pos = cursor;
                    e.PerformLayout();
                    cursor += evenSpacing * direction;
                }
            }
            else
            {
                for (int i = 0; i < Elements.Count; ++i)
                {
                    UIElementV2 e = Elements[i];
                    if (!e.Visible) continue;
                    e.Pos = cursor;
                    e.PerformLayout();
                    cursor += (e.Size + Margin) * direction;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public T Add<T>(T element) where T : UIElementV2
        {
            Elements.Add(element);
            return element;
        }

        public void Remove<T>(T element) where T : UIElementV2
        {
            if (element == null)
                return;
            Elements.RemoveRef(element);
        }

        public void Remove<T>(params T[] elements) where T : UIElementV2
        {
            foreach (T element in elements)
                Remove(element);
        }

        public void RemoveAll()
        {
            Elements.Clear();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public void BeginLayout(float x, float y)
        {
            LayoutStarted = true;
            LayoutCursor  = new Vector2(x, y);
            LayoutStep    = new Vector2(15f, 15f);
        }

        // Begin vertical layout of elements;
        // you can now call specific Button(), Checkbox(), etc. to create UI elements
        public void BeginVLayout(float x, float y, float ystep = 15f)
            => BeginVLayout(new Vector2(x,y), ystep);

        public void BeginHLayout(float x, float y, float xstep = 50f)
            => BeginHLayout(new Vector2(x,y), xstep);

        public void BeginVLayout(Vector2 pos, float ystep = 15f)
        {
            LayoutStarted = true;
            LayoutCursor  = pos;
            LayoutStep    = new Vector2(0f, ystep);
        }

        public void BeginHLayout(Vector2 pos, float xstep = 50f)
        {
            LayoutStarted = true;
            LayoutCursor  = pos;
            LayoutStep    = new Vector2(xstep, 0f);
        }

        public void SkipLayoutStep() // skips a single layout step during V and H layout
        {
            LayoutCursor += LayoutStep;
        }

        static int ElementSorter(UIElementV2 a, UIElementV2 b)
        {
            return a.ZOrder - b.ZOrder;
        }

        // ends the layout process and sorts all elements by their ZOrder values
        // In case the end of the layout position needs to be tracked return it on layout end. 
        public Vector2 EndLayout()
        {
            LayoutStarted = false;
            Elements.Sort(ElementSorter);
            return LayoutCursor;
        }

        protected Vector2 LayoutNext()
        {
            if (!LayoutStarted)
                throw new InvalidOperationException("You must call BeginLayout befor calling auto-layout methods");
            
            Vector2 result = LayoutCursor;
            LayoutCursor += LayoutStep;            
            return result;
        }

        Rectangle LayoutNextRect(int width, int height)
        {
            Vector2 next = LayoutNext();
            return new Rectangle((int)next.X, (int)next.Y, width, height);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        // Shared utility functions:
        protected UIButton Button(Vector2 pos, string launches, int titleId)
            => Add(new UIButton(this, pos, Localizer.Token(titleId)));
        protected UIButton Button(Vector2 pos, string launches, string text)
            => Add(new UIButton(this, pos, text));

        protected UIButton ButtonMediumMenu(float x, float y, string text)
            => Add(new UIButton(this, ButtonStyle.MediumMenu, x, y, text));

        // @note CloseButton automatically calls ExitScreen() on this screen
        protected CloseButton CloseButton(float x, float y)
            => Add(new CloseButton(this, new Rectangle((int)x, (int)y, 20, 20)));

        /////////////////////////////////////////////////////////////////////////////////////////////////

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

        protected UIButton Button(ButtonStyle style, Vector2 pos, string text, UIButton.ClickHandler click, string clickSfx = null)
        {
            var button = new UIButton(this, style, pos, text);
            if (click != null)       button.OnClick += click;
            if (clickSfx.NotEmpty()) button.ClickSfx = clickSfx;
            return Add(button);
        }

        protected UIButton Button(ButtonStyle style, float x, float y, string text, UIButton.ClickHandler click, string clickSfx = null)
            => Button(style, new Vector2(x, y), text, click, clickSfx);


        protected UIButton Button(float x, float y, string text, UIButton.ClickHandler click)
            => Button(ButtonStyle.Default, new Vector2(x, y), text, click);
        protected UIButton Button(float x, float y, int titleId, UIButton.ClickHandler click)
            => Button(ButtonStyle.Default, new Vector2(x, y), Localizer.Token(titleId), click);


        protected UIButton ButtonLow(float x, float y, string text, UIButton.ClickHandler click)
            => Button(ButtonStyle.Low80, new Vector2(x, y), text, click);
        protected UIButton ButtonLow(float x, float y, int titleId, UIButton.ClickHandler click)
            => Button(ButtonStyle.Low80, new Vector2(x, y), Localizer.Token(titleId), click);


        protected UIButton ButtonSmall(float x, float y, string text, UIButton.ClickHandler click)
            => Button(ButtonStyle.Small, new Vector2(x, y), text, click);
        protected UIButton ButtonSmall(float x, float y, int titleId, UIButton.ClickHandler click)
            => Button(ButtonStyle.Small, new Vector2(x, y), Localizer.Token(titleId), click);


        protected UIButton ButtonMedium(int titleId, UIButton.ClickHandler click)
            => Button(ButtonStyle.Medium, LayoutNext(), Localizer.Token(titleId), click);
        protected UIButton ButtonMedium(int titleId, string clickSfx, UIButton.ClickHandler click)
            => Button(ButtonStyle.Medium, LayoutNext(), Localizer.Token(titleId), click, clickSfx);


        protected UIButton ButtonMedium(float x, float y, int titleId, UIButton.ClickHandler click)
            => Button(ButtonStyle.Medium, new Vector2(x, y), Localizer.Token(titleId), click);
        protected UIButton ButtonMedium(float x, float y, string title, UIButton.ClickHandler click)
            => Button(ButtonStyle.Medium, new Vector2(x, y), title, click);


        protected ToggleButton ToggleButton(ToggleButtonStyle style, string icon)
            => Add(new ToggleButton(LayoutNext(), style, icon, this));

        protected ToggleButton ToggleButton(ToggleButtonStyle style, string icon, ToggleButton.ClickHandler onClick)
        {
            var button = new ToggleButton(LayoutNext(), style, icon, this);
            button.OnClick += onClick;
            return Add(button);
        }
        
        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, int title, int tooltip)
            => Add(new UICheckBox(this, pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, string title, string tooltip)
            => Add(new UICheckBox(this, pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, string title, int tooltip)
            => Add(new UICheckBox(this, pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));

        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, string title, int tooltip)
            => Add(new UICheckBox(this, x, y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, int title, int tooltip)
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

        protected FloatSlider Slider(Vector2 pos, int w, int h, string text, float min, float max, float value)
            => Slider(new Rectangle((int)pos.X, (int)pos.Y, w, h), text, min, max, value);

        protected FloatSlider Slider(int w, int h, string text, float min, float max, float value)
            => Slider(LayoutNextRect(w, h), text, min, max, value);

        protected FloatSlider SliderPercent(int w, int h, string text, float min, float max, float value)
            => SliderPercent(LayoutNextRect(w, h), text, min, max, value);

        /////////////////////////////////////////////////////////////////////////////////////////////////


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
        protected UILabel Label(Vector2 pos, string text, SpriteFont font, Color color) => Add(new UILabel(this, pos, text, font,color));
        protected UILabel Label(Vector2 pos, int titleId, SpriteFont font, Color color) => Add(new UILabel(this, pos, titleId, font, color));

        protected UILabel Label(float x, float y, string text) => Label(new Vector2(x, y), text);
        protected UILabel Label(float x, float y, int titleId) => Label(new Vector2(x, y), titleId);
        protected UILabel Label(float x, float y, string text, SpriteFont font) => Label(new Vector2(x, y), text, font);
        protected UILabel Label(float x, float y, int titleId, SpriteFont font) => Label(new Vector2(x, y), titleId, font);


        protected UILabel Label(string text) => Add(new UILabel(this, LayoutNext(), text));
        protected UILabel Label(int titleId) => Add(new UILabel(this, LayoutNext(), titleId));
        protected UILabel Label(string text, Color color) => Add(new UILabel(this, LayoutNext(), text, color));
        protected UILabel Label(int titleId, Color color) => Add(new UILabel(this, LayoutNext(), titleId, color));
        protected UILabel Label(string text, SpriteFont font, Color color) => Add(new UILabel(this, LayoutNext(), text, font, color));
        protected UILabel Label(int titleId, SpriteFont font, Color color) => Add(new UILabel(this, LayoutNext(), titleId, font, color));
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
        protected UILabel Label(Func<UILabel, string> dynamicText, bool alignRight = false)
        {
            UILabel label = Add(new UILabel(this, LayoutNext(), ""));
            label.DynamicText = dynamicText;
            label.AlignRight = alignRight;
            return label;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        protected internal UIPanel Panel(Rectangle rect, Color color)          => Add(new UIPanel(this, rect, color));
        protected internal UIPanel Panel(SubTexture tex, Rectangle r)          => Add(new UIPanel(this, tex, r));
        protected internal UIPanel Panel(SubTexture tex, Rectangle r, Color c) => Add(new UIPanel(this, tex, r, c));
        protected internal UIPanel Panel(string tex, int x, int y) => Add(new UIPanel(this, tex, x, y));
        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public UIBasicAnimEffect Anim() => AddEffect(new UIBasicAnimEffect(this));

        /// <param name="delay">Start animation fadeIn/stay/fadeOut after seconds</param>
        /// <param name="duration">Duration of fadeIn/stay/fadeOut</param>
        /// <param name="fadeIn">Fade in time</param>
        /// <param name="fadeOut">Fade out time</param>
        public UIBasicAnimEffect Anim(
            float delay, 
            float duration = 1.0f, 
            float fadeIn   = 0.25f, 
            float fadeOut  = 0.25f) => Anim().Time(delay, duration, fadeIn, fadeOut);

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public void StartTransition<T>(float distance, float direction) where T : UIElementV2
        {
            var candidates = new Array<UIElementV2>();
            for (int i = 0; i < Elements.Count; ++i)
            {
                UIElementV2 e = Elements[i];
                if (e is T) candidates.Add(e);
            }

            for (int i = candidates.Count - 1; i >= 0; --i)
            {
                UIElementV2 e = candidates[i];
                float modifier = i / (float)candidates.Count;
                e.AddEffect(new UITransitionEffect(e, distance, modifier, direction));
            }
        }

        public UIFadeInEffect StartFadeIn(float fadeInTime, float delay = 0f)
        {
            var fx = new UIFadeInEffect(this, fadeInTime, delay);
            AddEffect(fx);
            return fx;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
