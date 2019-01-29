using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.UI;
using Ship_Game.UI.Effects;

namespace Ship_Game
{
    public abstract class UIElementContainer : UIElementV2
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////

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
                    if (e.DeferredRemove) { Remove(e); }
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

        public override void PerformLayout()
        {
            if (!RequiresLayout || !Visible)
                return;
            RequiresLayout = false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual T Add<T>(T element) where T : UIElementV2
        {
            Elements.Add(element);
            element.Parent = this;
            element.ZOrder = NextZOrder();
            return element;
        }

        public T Add<T>() where T : UIElementV2, new()
        {
            return Add(new T());
        }

        public SplitElement AddSplit(UIElementV2 a, UIElementV2 b)
        {
            var split = new SplitElement(a, b);
            a.Parent = split;
            b.Parent = split;
            return Add(split);
        }

        public virtual void Remove(UIElementV2 element)
        {
            if (element == null)
                return;
            Elements.RemoveRef(element);
        }

        public virtual void RemoveAll()
        {
            Elements.Clear();
        }

        public void RefreshZOrder()
        {
            Elements.Sort(ZOrderSorter);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        // Begin vertical layout of elements;
        // you can now call specific Button(), Checkbox(), etc. to create UI elements
        // @note Coordinates are relative to parent TopLeft X,Y
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

        protected override int NextZOrder()
        {
            if (Elements.NotEmpty)
                return Elements.Last.ZOrder + 1;
            return ZOrder + 1;
        }

        static int ZOrderSorter(UIElementV2 a, UIElementV2 b)
        {
            return a.ZOrder - b.ZOrder;
        }

        // ends the layout process and sorts all elements by their ZOrder values
        // In case the end of the layout position needs to be tracked return it on layout end. 
        public Vector2 EndLayout()
        {
            LayoutStarted = false;
            Elements.Sort(ZOrderSorter);
            return LayoutCursor;
        }

        protected Vector2 LayoutNext()
        {
            if (!LayoutStarted)
                throw new InvalidOperationException("You must call BeginLayout before calling auto-layout methods");
            
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
            => Add(new UIButton(this, ButtonStyle.MediumMenu, new Vector2(x, y), text));

        // @note CloseButton automatically calls ExitScreen() on this screen
        protected CloseButton CloseButton(float x, float y)
            => Add(new CloseButton(this, new Rectangle((int)x, (int)y, 20, 20)));

        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected UIButton Button(int titleId, Action<UIButton> click)
        {
            return Button(Localizer.Token(titleId), click);
        }

        protected UIButton Button(string text, Action<UIButton> click)
        {
            UIButton button = Add(new UIButton(this, LayoutNext(), text));
            button.OnClick = click;
            button.ClickSfx = "sd_ui_tactical_pause";
            return button;
        }

        protected UIButton Button(ButtonStyle style, Vector2 pos, string text, Action<UIButton> click, string clickSfx = null)
        {
            var button = new UIButton(this, style, pos, text);
            if (click != null)       button.OnClick += click;
            if (clickSfx.NotEmpty()) button.ClickSfx = clickSfx;
            return Add(button);
        }

        protected UIButton Button(ButtonStyle style, float x, float y, string text, Action<UIButton> click, string clickSfx = null)
            => Button(style, new Vector2(x, y), text, click, clickSfx);


        protected UIButton Button(float x, float y, string text, Action<UIButton> click)
            => Button(ButtonStyle.Default, new Vector2(x, y), text, click);
        protected UIButton Button(float x, float y, int titleId, Action<UIButton> click)
            => Button(ButtonStyle.Default, new Vector2(x, y), Localizer.Token(titleId), click);


        protected UIButton ButtonLow(float x, float y, string text, Action<UIButton> click)
            => Button(ButtonStyle.Low80, new Vector2(x, y), text, click);
        protected UIButton ButtonLow(float x, float y, int titleId, Action<UIButton> click)
            => Button(ButtonStyle.Low80, new Vector2(x, y), Localizer.Token(titleId), click);


        protected UIButton ButtonSmall(float x, float y, string text, Action<UIButton> click)
            => Button(ButtonStyle.Small, new Vector2(x, y), text, click);
        protected UIButton ButtonSmall(float x, float y, int titleId, Action<UIButton> click)
            => Button(ButtonStyle.Small, new Vector2(x, y), Localizer.Token(titleId), click);


        protected UIButton ButtonMedium(int titleId, Action<UIButton> click)
            => Button(ButtonStyle.Medium, LayoutNext(), Localizer.Token(titleId), click);
        protected UIButton ButtonMedium(int titleId, string clickSfx, Action<UIButton> click)
            => Button(ButtonStyle.Medium, LayoutNext(), Localizer.Token(titleId), click, clickSfx);


        protected UIButton ButtonMedium(float x, float y, int titleId, Action<UIButton> click)
            => Button(ButtonStyle.Medium, new Vector2(x, y), Localizer.Token(titleId), click);
        protected UIButton ButtonMedium(float x, float y, string title, Action<UIButton> click)
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


        public FloatSlider Slider(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(this, rect, text, min, max, value));

        public FloatSlider SliderPercent(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(this, SliderStyle.Percent, rect, text, min, max, value));

        public FloatSlider Slider(int x, int y, int w, int h, string text, float min, float max, float value)
            => Slider(new Rectangle(x, y, w, h), text, min, max, value);

        public FloatSlider Slider(Vector2 pos, int w, int h, string text, float min, float max, float value)
            => Slider(new Rectangle((int)pos.X, (int)pos.Y, w, h), text, min, max, value);

        public FloatSlider Slider(int w, int h, string text, float min, float max, float value)
            => Slider(LayoutNextRect(w, h), text, min, max, value);

        public FloatSlider SliderPercent(int w, int h, string text, float min, float max, float value)
            => SliderPercent(LayoutNextRect(w, h), text, min, max, value);


        /////////////////////////////////////////////////////////////////////////////////////////////////
        

        public UILabel Label(Vector2 pos, string text) => Add(new UILabel(this, pos, text));
        public UILabel Label(Vector2 pos, int titleId) => Add(new UILabel(this, pos, titleId));
        public UILabel Label(Vector2 pos, string text, SpriteFont font) => Add(new UILabel(this, pos, text, font));
        public UILabel Label(Vector2 pos, int titleId, SpriteFont font) => Add(new UILabel(this, pos, titleId, font));
        public UILabel Label(Vector2 pos, string text, SpriteFont font, Color color) => Add(new UILabel(this, pos, text, font,color));
        public UILabel Label(Vector2 pos, int titleId, SpriteFont font, Color color) => Add(new UILabel(this, pos, titleId, font, color));

        public UILabel Label(float x, float y, string text) => Label(new Vector2(x, y), text);
        public UILabel Label(float x, float y, int titleId) => Label(new Vector2(x, y), titleId);
        public UILabel Label(float x, float y, string text, SpriteFont font) => Label(new Vector2(x, y), text, font);
        public UILabel Label(float x, float y, int titleId, SpriteFont font) => Label(new Vector2(x, y), titleId, font);


        public UILabel Label(string text) => Add(new UILabel(this, LayoutNext(), text));
        public UILabel Label(int titleId) => Add(new UILabel(this, LayoutNext(), titleId));
        public UILabel Label(string text, Color color) => Add(new UILabel(this, LayoutNext(), text, color));
        public UILabel Label(int titleId, Color color) => Add(new UILabel(this, LayoutNext(), titleId, color));
        public UILabel Label(string text, SpriteFont font, Color color) => Add(new UILabel(this, LayoutNext(), text, font, color));
        public UILabel Label(int titleId, SpriteFont font, Color color) => Add(new UILabel(this, LayoutNext(), titleId, font, color));
        public UILabel Label(int titleId, Action<UILabel> click)
        {
            return Label(Localizer.Token(titleId), click);
        }
        public UILabel Label(string text, Action<UILabel> click)
        {
            UILabel label = Add(new UILabel(this, LayoutNext(), text));
            label.OnClick = click;
            return label;
        }
        public UILabel Label(Func<UILabel, string> dynamicText, bool alignRight = false)
        {
            UILabel label = Add(new UILabel(this, LayoutNext(), ""));
            label.DynamicText = dynamicText;
            label.AlignRight = alignRight;
            return label;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public UIPanel Panel(in Rectangle r, Color c)               => Add(new UIPanel(this, r, c));
        public UIPanel Panel(SubTexture t, in Rectangle r)          => Add(new UIPanel(this, t, r));
        public UIPanel Panel(SubTexture t, in Rectangle r, Color c) => Add(new UIPanel(this, t, r, c));
        public UIPanel Panel(string t, int x, int y)   => Add(new UIPanel(this, t, x, y));
        public UIPanel Panel(string t, in Rectangle r) => Add(new UIPanel(this, t, r));
        public UIPanel Panel(string t)                 => Add(new UIPanel(this, t));

        // special Panel overload, parse relative position instead of absolute pos
        public UIPanel PanelRel(string t, Vector2 relPos)
        {
            UIPanel p = Add(new UIPanel(this, t));
            p.SetRelPos(relPos);
            return p;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public UIList List(Vector2 pos, Vector2 size) => Add(new UIList(this, pos, size));

        public UIList List(Vector2 pos)
        {
            UIList list = Add(new UIList(this, pos, new Vector2(100f, 100f)));
            list.LayoutStyle = ListLayoutStyle.Resize;
            return list;
        }


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
        
        public void StartTransition<T>(float distance, float direction, float time = 1f) where T : UIElementV2
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
                float modifier = time * (i / (float)candidates.Count);
                e.AddEffect(new UITransitionEffect(e, distance, modifier, direction, time));
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
