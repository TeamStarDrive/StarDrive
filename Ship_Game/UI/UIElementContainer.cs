using System;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;
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

        // This is for debugging
        int LastInputFrameId = -1;
        int LastUpdateFrameId = -1;

        /// <summary>
        /// Hack: NEW Multi-Layered draw mode disables child element drawing
        /// </summary>
        public bool NewMultiLayeredDrawMode;

        public override string ToString() => $"{TypeName} {ElementDescr} Elements={Elements.Count}";

        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected UIElementContainer()
        {
        }
        protected UIElementContainer(in Vector2 pos) : base(pos)
        {
        }
        protected UIElementContainer(in Vector2 pos, in Vector2 size) : base(pos, size)
        {
        }
        protected UIElementContainer(in Rectangle rect) : base(rect)
        {
        }
        protected UIElementContainer(in RectF rect) : base(rect)
        {
        }
        protected UIElementContainer(float x, float y, float w, float h) : base(x, y, w, h)
        {
        }
        // TODO: deprecated
        protected UIElementContainer(UIElementV2 parent, in Rectangle rect) : base(rect)
        {
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public int GetInternalElementsUnsafe(out UIElementV2[] elements)
        {
            elements = Elements.GetInternalArrayItems();
            return Elements.Count;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
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
                        if (child.Visible) child.Draw(batch, elapsed);
                    }
                }
            }
            else
            {
                DrawWithDebugOverlay(batch, elapsed);
            }
        }

        void DrawWithDebugOverlay(SpriteBatch batch, DrawTimes elapsed)
        {
            for (int i = 0; i <= DebugDrawIndex && i < Elements.Count; ++i)
            {
                UIElementV2 child = Elements[i];
                if (child.Visible)
                {
                    if (!NewMultiLayeredDrawMode) // DON'T DRAW CHILD ELEMENTS IN MULTI-LAYER MODE
                        child.Draw(batch, elapsed);

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
                if (LastInputFrameId != GameBase.Base.FrameId)
                    LastInputFrameId = GameBase.Base.FrameId;
                else
                    Empire.Universe.DebugWin?.DebugLogText("UIElement.HandleInput called twice per frame. This is a potential bug: " + this
                    , Debug.DebugModes.input);

                // iterate input in reverse, so we handle topmost objects before
                for (int i = Elements.Count - 1; i >= 0; --i)
                {
                    UIElementV2 child = Elements[i];
                    if (child.Visible && child.Enabled && child.HandleInput(input))
                        return true;
                }
            }
            return false;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (!Visible)
                return;

            if (LastUpdateFrameId != GameBase.Base.FrameId)
                LastUpdateFrameId = GameBase.Base.FrameId;
            else
                Log.Warning(ConsoleColor.DarkRed, 
                    "UIElement.Update called twice per frame. This is a potential bug: "+this);

            base.Update(fixedDeltaTime);

            for (int i = 0; i < Elements.Count; ++i)
            {
                UIElementV2 element = Elements[i];
                if (element.Visible)
                {
                    element.Update(fixedDeltaTime);
                    if (element.DeferredRemove) { Remove(element); }
                    // Update directly modified Elements array?
                    else if (Elements[i] != element) { --i; }
                }
            }

            if (DebugDraw)
            {
                DebugDrawTimer -= fixedDeltaTime;
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

        // UIElementContainer default implementation performs layout on all child elements
        public override void PerformLayout()
        {
            RequiresLayout = false;
            for (int i = 0; i < Elements.Count; ++i)
                Elements[i].PerformLayout();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual T Add<T>(T element) where T : UIElementV2
        {
            RequiresLayout = true;
            if (element.Parent != null)
                element.RemoveFromParent();
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

        public UIButton ButtonMediumMenu(float x, float y, in LocalizedText text)
            => Add(new UIButton(ButtonStyle.MediumMenu, new Vector2(x, y), text));

        // @note CloseButton automatically calls ExitScreen() on this screen
        public CloseButton CloseButton(float x, float y) => Add(new CloseButton(x, y));

        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected UIButton Button(UIButton btn, Action<UIButton> click, string clickSfx)
        {
            if (click != null)       btn.OnClick += click;
            if (clickSfx.NotEmpty()) btn.ClickSfx = clickSfx;
            return Add(btn);
        }

        public UIButton Button(ButtonStyle style, Action<UIButton> click, string clickSfx = null)
            => Button(new UIButton(style), click, clickSfx);


        public UIButton Button(ButtonStyle style, Vector2 pos, in LocalizedText text, Action<UIButton> click, string clickSfx = null)
            => Button(new UIButton(style, pos, text), click, clickSfx);


        public UIButton Button(ButtonStyle style, float x, float y, in LocalizedText text, Action<UIButton> click, string clickSfx = null)
            => Button(style, new Vector2(x, y), text, click, clickSfx);


        public UIButton Button(ButtonStyle style, in Rectangle rect, Action<UIButton> click, string clickSfx = null)
            => Button(new UIButton(style, rect), click, clickSfx);


        public UIButton Button(float x, float y, in LocalizedText text, Action<UIButton> click)
            => Button(ButtonStyle.Default, new Vector2(x, y), text, click);


        public UIButton ButtonLow(float x, float y, in LocalizedText text, Action<UIButton> click)
            => Button(ButtonStyle.Low80, new Vector2(x, y), text, click);

        public UIButton ButtonBigDip(float x, float y, in LocalizedText text, Action<UIButton> click)
            => Button(ButtonStyle.BigDip, new Vector2(x, y), text, click);


        public UIButton ButtonSmall(float x, float y, in LocalizedText text, Action<UIButton> click)
            => Button(ButtonStyle.Small, new Vector2(x, y), text, click);

        public UIButton ButtonMedium(float x, float y, in LocalizedText text, Action<UIButton> click)
            => Button(ButtonStyle.Medium, new Vector2(x, y), text, click);

        public UIButton Button(ButtonStyle style, in LocalizedText text, Action<UIButton> click, string clickSfx = null)

            => Button(new UIButton(style, text), click, clickSfx);

        public UIButton ButtonMedium(in LocalizedText text, Action<UIButton> click, string clickSfx = null)
            => Button(ButtonStyle.Medium, text, click, clickSfx);


        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected UICheckBox Checkbox(Vector2 pos, Expression<Func<bool>> binding, in LocalizedText title, in LocalizedText tooltip)
            => Add(new UICheckBox(pos.X, pos.Y, binding, Fonts.Arial12Bold, title, tooltip));

        protected UICheckBox Checkbox(float x, float y, Expression<Func<bool>> binding, in LocalizedText title, in LocalizedText tooltip)
            => Add(new UICheckBox(x, y, binding, Fonts.Arial12Bold, title, tooltip));

        /////////////////////////////////////////////////////////////////////////////////////////////////


        public FloatSlider Slider(Rectangle rect, in LocalizedText text, float min, float max, float value)
            => Add(new FloatSlider(rect, text, min, max, value));

        public FloatSlider SliderPercent(Rectangle rect, in LocalizedText text, float min, float max, float value)
            => Add(new FloatSlider(SliderStyle.Percent, rect, text, min, max, value));

        public FloatSlider SliderDecimal1(Rectangle rect, in LocalizedText text, float min, float max, float value)
            => Add(new FloatSlider(SliderStyle.Decimal1, rect, text, min, max, value));

        public FloatSlider Slider(int x, int y, int w, int h, in LocalizedText text, float min, float max, float value)
            => Slider(new Rectangle(x, y, w, h), text, min, max, value);

        public FloatSlider Slider(Vector2 pos, int w, int h, in LocalizedText text, float min, float max, float value)
            => Slider(new Rectangle((int)pos.X, (int)pos.Y, w, h), text, min, max, value);


        /////////////////////////////////////////////////////////////////////////////////////////////////

        public UILabel Label(Vector2 pos, in LocalizedText text) => Add(new UILabel(pos, text));
        public UILabel Label(Vector2 pos, in LocalizedText text, SpriteFont font) => Add(new UILabel(pos, text, font));
        public UILabel Label(Vector2 pos, in LocalizedText text, SpriteFont font, Color color) => Add(new UILabel(pos, text, font,color));

        public UILabel Label(float x, float y, in LocalizedText text) => Label(new Vector2(x, y), text);
        public UILabel Label(float x, float y, in LocalizedText text, SpriteFont font) => Label(new Vector2(x, y), text, font);

        public UILabel LabelRel(in LocalizedText text, SpriteFont font, float x, float y)
            => LabelRel(text, font, Color.White, x, y);

        public UILabel LabelRel(in LocalizedText text, SpriteFont font, Color color, float x, float y)
        {
            UILabel label = Add(new UILabel(text, font, color));
            label.SetRelPos(x, y);
            return label;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public UIPanel Panel(in Rectangle r, Color c, DrawableSprite s = null)
            => Add(new UIPanel(r, c, s));     
        
        public UIPanel Panel(in Rectangle r, DrawableSprite s = null)
            => Add(new UIPanel(r, s));

        public UIPanel Panel(in Rectangle r, Color c, SubTexture s)
            => Panel(r, c, new DrawableSprite(s));

        public UIPanel Panel(in Rectangle r, SubTexture s)
            => Panel(r, new DrawableSprite(s));

        public UIPanel Panel(float x, float y, SubTexture s)
            => Add(new UIPanel(new Vector2(x,y), s));

        public UIPanel PanelRel(in Rectangle r, SubTexture s)
        {
            var panel = Add(new UIPanel(r, s));
            panel.SetRelPos(r.X, r.Y);
            return panel;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////


        public UIList AddList(Vector2 pos, Vector2 size) => Add(new UIList(pos, size));

        public UIList AddList(float x, float y) => AddList(new Vector2(x, y));
        public UIList AddList(Vector2 pos)
        {
            UIList list = Add(new UIList(pos, new Vector2(100f, 100f)));
            list.LayoutStyle = ListLayoutStyle.ResizeList;
            return list;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public void StartGroupTransition<T>(Vector2 offset, float direction, float time = 1f) where T : UIElementV2
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


        /////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
