using System;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.UI;

namespace Ship_Game
{
    public class UIElementContainer : UIElementV2
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected readonly Array<UIElementV2> Elements = new Array<UIElementV2>();

        /// <summary>
        /// If enabled, UI elements will be drawn with a fixed delay
        /// in their appropriate ZOrder
        /// </summary>
        public bool DebugDraw;
        int DebugDrawIndex;
        float DebugDrawTimer;
        const float DebugDrawInterval = 0.5f;

        /// <summary>
        /// Hack: NEW Multi-Layered draw mode disables child element drawing
        /// </summary>
        public bool NewMultiLayeredDrawMode;

        public override string ToString() => $"Element {ElementDescr} Elements={Elements.Count}";

        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected UIElementContainer()
        {
        }
        protected UIElementContainer(UIElementV2 parent, Vector2 pos) : base(parent, pos)
        {
        }
        protected UIElementContainer(UIElementV2 parent, Vector2 pos, Vector2 size) : base(parent, pos, size)
        {
        }
        protected UIElementContainer(UIElementV2 parent, in Rectangle rect) : base(parent, rect)
        {
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public int GetInternalElementsUnsafe(out UIElementV2[] elements)
        {
            elements = Elements.GetInternalArrayItems();
            return Elements.Count;
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
                return;

            if (!DebugDraw)
            {
                if (!NewMultiLayeredDrawMode) // DON'T DRAW CHILD ELEMENTS IN MULTI-LAYER MODE
                {
                    for (int i = 0; i < Elements.Count; ++i)
                    {
                        UIElementV2 child = Elements[i];
                        if (child.Visible) child.Draw(batch);
                    }
                }
            }
            else
            {
                DrawWithDebugOverlay(batch);
            }

            if (ToolTip.Hotkey.IsEmpty())
                ToolTip.Draw(batch);
        }

        void DrawWithDebugOverlay(SpriteBatch batch)
        {
            for (int i = 0; i <= DebugDrawIndex && i < Elements.Count; ++i)
            {
                UIElementV2 child = Elements[i];
                if (child.Visible)
                {
                    if (!NewMultiLayeredDrawMode) // DON'T DRAW CHILD ELEMENTS IN MULTI-LAYER MODE
                        child.Draw(batch);

                    if (i == DebugDrawIndex)
                        batch.DrawRectangle(child.Rect, Color.Orange);
                }
            }

            Color debugColor = Color.Red.Alpha(0.75f);
            batch.DrawRectangle(Rect, debugColor);
            batch.DrawString(Fonts.Arial12Bold, ToString(), Pos, debugColor);
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
            if (!Visible)
                return;

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
            RequiresLayout = false;
            for (int i = 0; i < Elements.Count; ++i)
                Elements[i].PerformLayout();
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

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public bool Find<T>(string name, out T found) where T : UIElementV2
        {
            for (int i = 0; i < Elements.Count; ++i) // first find immediate children
            {
                UIElementV2 e = Elements[i];
                if (e.Name == name && e is T elem)
                {
                    found = elem;
                    return true;
                }
            }

            for (int i = 0; i < Elements.Count; ++i) // then perform recursive scan of child containers
            {
                UIElementV2 e = Elements[i];
                if (e is UIElementContainer c && c.Find(name, out found))
                    return true; // yay
            }

            found = null;
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public void RefreshZOrder()
        {
            Elements.Sort(ZOrderSorter);
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

        protected UIButton Button(UIButton btn, Action<UIButton> click, string clickSfx)
        {
            if (click != null)       btn.OnClick += click;
            if (clickSfx.NotEmpty()) btn.ClickSfx = clickSfx;
            return Add(btn);
        }

        protected UIButton Button(ButtonStyle style, Action<UIButton> click, string clickSfx = null)
            => Button(new UIButton(this, style), click, clickSfx);

        protected UIButton Button(ButtonStyle style, Vector2 pos, string text, Action<UIButton> click, string clickSfx = null)
            => Button(new UIButton(this, style, pos, text), click, clickSfx);

        protected UIButton Button(ButtonStyle style, float x, float y, string text, Action<UIButton> click, string clickSfx = null)
            => Button(style, new Vector2(x, y), text, click, clickSfx);

        protected UIButton Button(ButtonStyle style, in Rectangle rect, Action<UIButton> click, string clickSfx = null)
            => Button(new UIButton(this, style, rect), click, clickSfx);

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



        protected UIButton ButtonMedium(float x, float y, int titleId, Action<UIButton> click)
            => Button(ButtonStyle.Medium, new Vector2(x, y), Localizer.Token(titleId), click);
        protected UIButton ButtonMedium(float x, float y, string title, Action<UIButton> click)
            => Button(ButtonStyle.Medium, new Vector2(x, y), title, click);

        /////////////////////////////////////////////////////////////////////////////////////////////////


        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, string title, int tooltip)
            => Add(new UICheckBox(this, pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));

        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, string title, int tooltip)
            => Add(new UICheckBox(this, x, y, binding, Fonts.Arial12Bold, title, tooltip));
        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, int title, int tooltip)
            => Add(new UICheckBox(this, x, y, binding, Fonts.Arial12Bold, title, tooltip));

        /////////////////////////////////////////////////////////////////////////////////////////////////


        public FloatSlider Slider(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(this, rect, text, min, max, value));

        public FloatSlider SliderPercent(Rectangle rect, string text, float min, float max, float value)
            => Add(new FloatSlider(this, SliderStyle.Percent, rect, text, min, max, value));

        public FloatSlider Slider(int x, int y, int w, int h, string text, float min, float max, float value)
            => Slider(new Rectangle(x, y, w, h), text, min, max, value);

        public FloatSlider Slider(Vector2 pos, int w, int h, string text, float min, float max, float value)
            => Slider(new Rectangle((int)pos.X, (int)pos.Y, w, h), text, min, max, value);


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

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public UIPanel Panel(in Rectangle r, Color c) => Add(new UIPanel(this, r, c));

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public UIList List(Vector2 pos, Vector2 size) => Add(new UIList(this, pos, size));

        public UIList List(Vector2 pos)
        {
            UIList list = Add(new UIList(this, pos, new Vector2(100f, 100f)));
            list.LayoutStyle = ListLayoutStyle.Resize;
            return list;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public void StartTransition<T>(Vector2 offset, float direction, float time = 1f) where T : UIElementV2
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
                float delay = time * (i / (float)candidates.Count);
                Vector2 start = direction > 0f ? e.Pos : e.Pos + offset;
                Vector2 end   = direction < 0f ? e.Pos : e.Pos + offset;
                e.AddEffect(new UIBasicAnimEffect(e)
                    .FadeIn(delay, time)
                    .Pos(start, end)
                    .Sfx(null, "blip_click"));
            }
        }

        public UIBasicAnimEffect StartFadeIn(float fadeInTime, float delay = 0f)
        {
            var fx = new UIBasicAnimEffect(this).FadeIn(delay, fadeInTime);
            AddEffect(fx);
            return fx;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
