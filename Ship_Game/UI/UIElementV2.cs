using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    // UIElement alignment values used for Axis and ParentAlign
    public enum Align
    {
        TopLeft,     // "topleft"      x=0.0  y=0.0
        TopCenter,   // "topcenter"    x=0.5  y=0.0
        TopRight,    // "topright"     x=1.0  y=0.0

        CenterLeft,  // "centerleft"   x=0.0  y=0.5
        Center,      // "center"       x=0.5  y=0.5
        CenterRight, // "centerright"  x=1.0  y=0.5

        BottomLeft,  // "bottomleft"   x=0.0  y=1.0
        BottomCenter,// "bottomcenter" x=0.5  y=1.0
        BottomRight  // "bottomright"  x=1.0  y=1.0
    }

    public enum DrawDepth
    {
        Foreground, // draw 2D on top of 3D objects -- default value
        Background, // draw 2D behind 3D objects
        ForeAdditive, // Foreground + Additive alpha blend
        BackAdditive, // Background + Additive alpha blend
    }

    public interface IColorElement
    {
        Color Color { get; set; }
    }

    public interface ISpriteElement
    {
        DrawableSprite Sprite { get; set; }
    }

    public abstract class UIElementV2 : IInputHandler
    {
        public UIElementV2 Parent;
        public string Name = string.Empty;

        public Vector2 Pos;    // absolute position in the UI
        public Vector2 Size;   // absolute size in the UI

        public Vector2 RelPos; // relative position on parent, in absolute coordinates
        protected bool UseRelPos; // if TRUE, uses RelPos during PerformLayout()

        //protected Vector2 AxisOffset = Vector2.Zero;
        //protected Vector2 ParentOffset = Vector2.Zero;

        // If set TRUE, this.PerformLayout() will be triggered during next Update()
        // After layout is complete, RequiresLayout should be set false
        public bool RequiresLayout;

        // Elements are sorted by ZOrder during EndLayout()
        // Changing this will allow you to bring your UI elements to top
        public int ZOrder;
        
        public bool Visible = true; // If TRUE, this UIElement is rendered
        public bool Enabled = true; // If TRUE, this UIElement can receive input events
        protected internal bool DeferredRemove; // If TRUE, this UIElement will be deleted during update

        // This controls the layer ordering of 2D UI Elements
        public DrawDepth DrawDepth;

        // Nullable to save memory
        Array<UIEffect> Effects;

        public void Show() => Visible = true;
        public void Hide() => Visible = false;

        public Rectangle Rect
        {
            get => new Rectangle((int)Pos.X, (int)Pos.Y, (int)Size.X, (int)Size.Y);
            set
            {
                Pos.X = value.X;
                Pos.Y = value.Y;
                Size.X = value.Width;
                Size.Y = value.Height;
            }
        }

        public float X { get => Pos.X; set => Pos.X = value; }
        public float Y { get => Pos.Y; set => Pos.Y = value; }
        public float Width  { get => Size.X; set => Size.X = value; }
        public float Height { get => Size.Y; set => Size.Y = value; }
        public float Right  { get => Pos.X + Size.X; set => Size.X = (value - Pos.X); }
        public float Bottom { get => Pos.Y + Size.Y; set => Size.Y = (value - Pos.Y); }
        
        public Vector2 TopLeft  => new Vector2(Pos.X, Pos.Y);
        public Vector2 TopRight => new Vector2(Pos.X + Size.X, Rect.Y);
        public Vector2 BotRight => new Vector2(Pos.X + Size.X, Pos.Y + Size.Y);
        public Vector2 BotLeft  => new Vector2(Pos.X,          Pos.Y + Size.Y);
        public float CenterX => Pos.X + Size.X*0.5f;
        public float CenterY => Pos.Y + Size.Y*0.5f;
        public Vector2 Center => Pos + Size*0.5f;

        protected string TypeName => GetType().GetTypeName();
        protected string ElementDescr => $"{Name} {{{Pos.X},{Pos.Y} {Size.X}x{Size.Y}}} {(Visible?"Vis":"Hid")}";

        public override string ToString() => $"{TypeName} {ElementDescr}";

        static Vector2 RelativeToAbsolute(UIElementV2 parent, float x, float y)
        {
            if      (x < 0f) x += parent.Size.X;
            else if (x <=1f) x *= parent.Size.X;
            if      (y < 0f) y += parent.Size.Y;
            else if (y <=1f) y *= parent.Size.Y;
            return new Vector2(x, y);
        }

        public Vector2 RelativeToAbsolute(float x, float y)
        {
            return RelativeToAbsolute(Parent ?? this, x, y);
        }

        // This has a special behaviour,
        // if x < 0 or y < 0, then it will be evaluated as Parent.Size.X - x
        public void SetAbsPos(float x, float y)
        {
            Pos = new Vector2(x, y);
        }
        public void SetSize(float width, float height)
        {
            Size = new Vector2(width, height);
        }

        public void SetRelPos(float x, float y)
        {
            SetRelPos(new Vector2(x, y));
        }

        public void SetRelPos(in Vector2 relPos)
        {
            RelPos = relPos;
            UseRelPos = true;
            RequiresLayout = true;
        }

        /// <summary>
        /// Using this element size, moves the element to
        /// the center of the target element.
        /// NOTE: Coordinates are rounded to pixel boundary
        /// </summary>
        public UIElementV2 SetPosToCenterOf(UIElementV2 target)
        {
            Vector2 centered = (target.Center - Size*0.5f).Rounded();
            SetAbsPos(centered.X, centered.Y);
            return this;
        }

        /// <summary>
        /// Moves this element Y pos to the bottom of target.
        /// Perfect for aligning elements a few pixels offset from a Panel's bottom.
        /// NOTE: Coordinates are rounded to pixel boundary
        /// </summary>
        public UIElementV2 SetDistanceFromBottomOf(UIElementV2 target, float distance)
        {
            float y = (float)Math.Round(target.Bottom - distance - Height);
            SetAbsPos(X, y);
            return this;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        protected UIElementV2()
        {
        }
        protected UIElementV2(in Vector2 pos)
        {
            Pos = pos;
        }
        protected UIElementV2(in Vector2 pos, in Vector2 size)
        {
            Pos = pos;
            Size = size;
        }
        protected UIElementV2(in Rectangle rect)
        {
            Pos = new Vector2(rect.X, rect.Y);
            Size = new Vector2(rect.Width, rect.Height);
        }
        protected UIElementV2(in RectF rect)
        {
            Pos = new Vector2(rect.X, rect.Y);
            Size = new Vector2(rect.W, rect.H);
        }
        protected UIElementV2(float x, float y, float w, float h)
        {
            Pos = new Vector2(x, y);
            Size = new Vector2(w, h);
        }
        // TODO: deprecated
        protected UIElementV2(UIElementV2 parent, in Rectangle rect) : this(rect)
        {
        }

        protected virtual int NextZOrder() { return ZOrder + 1; }

        // 0. Perform Layout operations on demand
        public virtual void PerformLayout()
        {
            RequiresLayout = false;
            if (UseRelPos && Parent != null)
            {
                Pos = Parent.Pos + RelPos;
            }
        }

        // 1. we handle input
        public abstract bool HandleInput(InputState input);

        // 2. then we update
        public virtual void Update(float fixedDeltaTime)
        {
            UpdateEffects(fixedDeltaTime);
            if (RequiresLayout)
                PerformLayout();
        }

        // 3. finally we draw
        public abstract void Draw(SpriteBatch batch, DrawTimes elapsed);

        public void RunOnEmpireThread(Action action) => ScreenManager.Instance.RunOnEmpireThread(action);

        public void RemoveFromParent(bool deferred = false)
        {
            if (deferred)
                DeferredRemove = true;
            else if (Parent is UIElementContainer container)
                container.Remove(this);
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////

            
        public T AddEffect<T>(T effect) where T : UIEffect
        {
            Log.Assert(effect != null, "UIEffect cannot be null");
            if (Effects == null)
                Effects = new Array<UIEffect>();
            Effects.Add(effect);
            return effect;
        }

        public void ClearEffects()
        {
            Effects = null;
        }

        protected void UpdateEffects(float deltaTime)
        {
            Log.Assert(Visible, "UpdateEffects should only be called when Visible");
            if (Effects == null)
                return;
            for (int i = 0; i < Effects.Count;)
            {
                if (Effects[i].Update(deltaTime)) 
                    Effects.RemoveAt(i);
                else ++i;
            }
            if (Effects.Count == 0)
                Effects = null;
        }
        
        public UIBasicAnimEffect Anim() => AddEffect(new UIBasicAnimEffect(this));

        /// <param name="delay">Start animation fadeIn/stay/fadeOut after seconds</param>
        /// <param name="duration">Duration of fadeIn/stay/fadeOut</param>
        /// <param name="fadeIn">Fade in time</param>
        /// <param name="fadeOut">Fade out time</param>
        public UIBasicAnimEffect Anim(
            float delay, 
            float duration = 1.0f, 
            float fadeIn   = 0.25f, 
            float fadeOut  = 0.25f)
        {
            return Anim().Time(delay, duration, fadeIn, fadeOut);
        }

        public UIBasicAnimEffect StartFadeIn(float fadeInTime, float delay = 0f) => Anim().FadeIn(delay, fadeInTime);

        // Starts transition from [start] to [end]
        public UIBasicAnimEffect StartTransition(Vector2 start, Vector2 end, float time = 1f) => Anim().FadeIn(0f, time).Pos(start, end);

        // Starts transition from Pos to [end]
        public UIBasicAnimEffect StartTransitionTo(Vector2 end, float time = 1f) => Anim().FadeIn(0f, time).Pos(Pos, end);

        // Starts transition from [from] to Pos
        public UIBasicAnimEffect StartTransitionFrom(Vector2 from, float time = 1f) => Anim().FadeIn(0f, time).Pos(from, Pos);

        /////////////////////////////////////////////////////////////////////////////////////////////////
        
        public bool HitTest(Vector2 pos)
        {
            return pos.X > Pos.X && pos.Y > Pos.Y && pos.X < Pos.X + Size.X && pos.Y < Pos.Y + Size.Y;
        }

        public GameContentManager ContentManager
        {
            get
            {
                if (this is GameScreen screen)
                    return screen.TransientContent;
                return Parent == null ? ResourceManager.RootContent : Parent.ContentManager;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
    }

    public static class UIElementExt
    {
        public static T InBackground<T>(this T element) where T : UIElementV2
        {
            element.DrawDepth = DrawDepth.Background;
            return element;
        }
        public static T InBackAdditive<T>(this T element) where T : UIElementV2
        {
            element.DrawDepth = DrawDepth.BackAdditive;
            return element;
        }
        public static T InForeAdditive<T>(this T element) where T : UIElementV2
        {
            element.DrawDepth = DrawDepth.ForeAdditive;
            return element;
        }
    }
}