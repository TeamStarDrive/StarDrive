using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.SpriteSystem;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

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
        /// <summary>
        /// If TRUE, every HandleInput() call which returns True will be logged into console
        /// </summary>
        public static bool DebugInputCapture;

        public UIElementV2 Parent;
        public string Name = string.Empty;

        public Vector2 Pos;    // absolute position in the UI
        public Vector2 Size;   // absolute size in the UI

        // these are all custom types to prevent type-conversion bugs and mixing
        // relative coordinates with absolute coordinates
        // ABSOLUTE COORDINATES: Vector2, Point
        // LOCAL COORDINATES:    LocalPos
        // RELATIVE COORDINATES: RelPos, RelSize
        public LocalPos LocalPos; // local pos on parent, in absolute coordinates
        public RelPos RelPos;     // relative pos  on parent, in RELATIVE coordinates [0.0, 1.0]
        public RelSize RelSize;   // relative size on parent, in RELATIVE coordinates [0.0, 1.0]

        // Absolute size of the Parent (or Screen if no parent)
        public Vector2 ParentSize => Parent?.Size ?? GameBase.ScreenSize;

        [Flags]
        protected enum StateFlags
        {
            None = 0,

            // local pos on parent, in absolute coordinates
            // if set, use LocalPos during PerformLayout() which means it will move with Parent
            UseLocalPos = (1<<1), 

            // relative pos on parent, in relative coordinates [0.0, 1.0]
            // if set, uses RelPos during PerformLayout() which means it will move with Parent
            UseRelPos = (1<<2),

            // relative size on parent, in RELATIVE coordinates [0.0, 1.0]
            // if set, RelSize during PerformLayout() which means it will resize with Parent
            UseRelSize = (1<<3),
        }

        protected StateFlags StateBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Bit(StateFlags tag, bool value) => StateBits = value ? StateBits|tag : StateBits & ~tag;

        public bool UseLocalPos
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (StateBits & StateFlags.UseLocalPos) != 0;
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(StateFlags.UseLocalPos, value); }
        
        public bool UseRelPos
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (StateBits & StateFlags.UseRelPos) != 0;
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(StateFlags.UseRelPos, value); }

        public bool UseRelSize
        { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (StateBits & StateFlags.UseRelSize) != 0;
          [MethodImpl(MethodImplOptions.AggressiveInlining)] set => Bit(StateFlags.UseRelSize, value); }

        // When using Local or Relative positions, where is the parent's center?
        // PRECONDITION: UseLocalPos || UseRelPos
        public Align ParentAlign;

        // When using Local or Relative positions, where is this UIElement's Position axis?
        // Default is TOPLEFT
        // PRECONDITION: UseLocalPos || UseRelPos
        public Align LocalAxis;

        // Sets both ParentAlign and LocalAxis to the same value
        public Align AxisAlign
        {
            set { ParentAlign = LocalAxis = value; }
        }

        // If set TRUE, this.PerformLayout() will be triggered during next Update()
        // After layout is complete, RequiresLayout should be set false
        public bool RequiresLayout;

        // Elements are sorted by ZOrder during EndLayout()
        // Changing this will allow you to bring your UI elements to top
        public int ZOrder;
        
        public bool Visible = true; // If TRUE, this UIElement is rendered
        public bool Hidden { get => !Visible; set => Visible = !value; }
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
            get => new((int)Pos.X, (int)Pos.Y, (int)Size.X, (int)Size.Y);
            set
            {
                Pos.X = value.X;
                Pos.Y = value.Y;
                Size.X = value.Width;
                Size.Y = value.Height;
            }
        }

        public RectF RectF
        {
            get => new(Pos, Size);
            set
            {
                Pos.X = value.X;
                Pos.Y = value.Y;
                Size.X = value.W;
                Size.Y = value.H;
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
        
        public override string ToString() => $"{TypeName} {ElementDescr}";

        protected string TypeName => GetType().GetTypeName();
        protected string ElementDescr => $"{Name} {{{PosDescr} {SizeDescr}}} {(Visible?"Vis":"Hid")}";
        
        protected string PosDescr
        {
            get
            {
                if (UseRelPos)
                    return $"Rel {RelPos.X},{RelPos.Y}";
                if (UseLocalPos)
                    return $"Local {LocalPos.X},{LocalPos.Y}";
                return $"{Pos.X},{Pos.Y}";
            }
        }

        protected string SizeDescr
        {
            get
            {
                if (UseRelSize)
                    return $"Rel {RelSize.W}x{RelSize.H}";
                return $"{Size.X}x{Size.Y}";
            }
        }

        // This has a special behaviour,
        // if x < 0 or y < 0, then it will be evaluated as Parent.Size.X - x
        public void SetAbsPos(float x, float y) => SetAbsPos(new Vector2(x, y));
        public void SetAbsPos(in Vector2 absPos)
        {
            UseLocalPos = false;
            UseRelPos = false;
            Pos = absPos;
        }

        public void SetLocalPos(float x, float y)    => SetLocalPos(new LocalPos(x, y));
        public void SetLocalPos(in Vector2 localPos) => SetLocalPos(new LocalPos(localPos));
        public void SetLocalPos(in LocalPos localPos)
        {
            LocalPos = localPos;
            UseLocalPos = true;
            UseRelPos = false;
            RequiresLayout = true;
        }

        public void SetRelPos(float x, float y)  => SetRelPos(new RelPos(x, y));
        public void SetRelPos(in Vector2 relPos) => SetRelPos(new RelPos(relPos));
        public void SetRelPos(in RelPos relPos)
        {
            RelPos = relPos;
            UseLocalPos = false;
            UseRelPos = true;
            RequiresLayout = true;
        }

        // depending on current element configuration, sets RelPos/LocalPos/AbsPos
        public void SetAutoPos(float x, float y)
        {
            if (UseRelPos)        SetRelPos(x, y);
            else if (UseLocalPos) SetLocalPos(x, y);
            else                  SetAbsPos(x, y);
        }
        
        // depending on current element configuration, gets RelPos/LocalPos/AbsPos
        public Vector2 GetAutoPos()
        {
            if (UseRelPos)        return new Vector2(RelPos.X, RelPos.Y);
            else if (UseLocalPos) return new Vector2(LocalPos.X, LocalPos.Y);
            else                  return Pos;
        }
        
        public void SetAbsSize(float w, float h) => SetAbsSize(new Vector2(w, h));
        public void SetAbsSize(in Vector2 absSize)
        {
            UseRelSize = false;
            Size = absSize;
        }

        public void SetRelSize(float w, float h)   => SetRelSize(new RelSize(w, h));
        public void SetRelSize(in Vector2 relSize) => SetRelSize(new RelSize(relSize));
        public void SetRelSize(in RelSize relSize)
        {
            RelSize = relSize;
            UseRelSize = true;
            RequiresLayout = true;
        }
        
        // depending on current element configuration, sets RelSize/AbsSize
        public void SetAutoSize(float w, float h)
        {
            if (UseRelSize) SetRelSize(w, h);
            else            SetAbsSize(w, h);
        }

        // depending on current element configuration, gets RelSize/AbsSize
        public Vector2 GetAutoSize()
        {
            if (UseRelSize) return new Vector2(RelSize.W, RelSize.H);
            else            return Size;
        }

        /// <summary>
        /// Using this element size, moves the element to
        /// the center of the target element.
        /// NOTE: Coordinates are rounded to pixel boundary
        /// </summary>
        public UIElementV2 SetPosToCenterOf(UIElementV2 target)
        {
            if (target == Parent)
            {
                SetLocalPos((target.Size * 0.5f).Rounded());
                LocalAxis = Align.Center;
                UpdatePosAndSize();
            }
            else
            {
                Vector2 centered = (target.Center - Size * 0.5f).Rounded();
                SetAbsPos(centered.X, centered.Y);
            }
            return this;
        }

        /// <summary>
        /// Moves this element Y pos to the bottom of target.
        /// Perfect for aligning elements a few pixels offset from a Panel's bottom.
        /// NOTE: Coordinates are rounded to pixel boundary
        /// </summary>
        public UIElementV2 SetDistanceFromBottomOf(UIElementV2 target, float distance)
        {
            if (target == Parent)
            {
                SetLocalPos(LocalPos.X, target.Size.Y - distance - Height);
                UpdatePosAndSize();
            }
            else
            {
                float y = (float)Math.Round(target.Bottom - distance - Height);
                SetAbsPos(X, y);
            }
            return this;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public Vector2 ParentAlignOffset => AlignValue(ParentAlign);
        public Vector2 LocalAxisOffset => AlignValue(LocalAxis);

        public static Vector2 AlignValue(Align align)
        {
            switch (align)
            {
                default:
                case Align.TopLeft:      return new Vector2(0.0f, 0.0f);
                case Align.TopCenter:    return new Vector2(0.5f, 0.0f);
                case Align.TopRight:     return new Vector2(1.0f, 0.0f);
                case Align.CenterLeft:   return new Vector2(0.0f, 0.5f);
                case Align.Center:       return new Vector2(0.5f, 0.5f);
                case Align.CenterRight:  return new Vector2(1.0f, 0.5f);
                case Align.BottomLeft:   return new Vector2(0.0f, 1.0f);
                case Align.BottomCenter: return new Vector2(0.5f, 1.0f);
                case Align.BottomRight:  return new Vector2(1.0f, 1.0f);
            }
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
        protected UIElementV2(in LocalPos localPos)
        {
            SetLocalPos(localPos);
        }
        protected UIElementV2(in LocalPos localPos, in Vector2 size)
        {
            SetLocalPos(localPos);
            Size = size;
        }
        protected UIElementV2(in LocalPos localPos, in RelSize size)
        {
            SetLocalPos(localPos);
            SetRelSize(size);
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

        protected virtual int NextZOrder() { return ZOrder + 1; }

        // 0. Perform Layout operations on demand
        public virtual void PerformLayout()
        {
            RequiresLayout = false;
            UpdatePosAndSize();
        }

        public void UpdatePosAndSize()
        {
            // using relpos, relsize or localpos?
            if (StateBits == StateFlags.None)
                return;

            UIElementV2 parent = Parent;
            Vector2 parentPos = parent?.Pos ?? Vector2.Zero;
            Vector2 parentSize = parent?.Size ?? GameBase.ScreenSize;
            
            if (UseRelSize)
            {
                Size.X = parentSize.X * RelSize.W;
                Size.Y = parentSize.Y * RelSize.H;
            }

            if (UseRelPos)
            {
                // default for both is TopLeft [0.0, 0.0]
                Vector2 parentAlign = AlignValue(ParentAlign);
                Vector2 localAxis = AlignValue(LocalAxis);
                Pos.X = (parentPos.X + parentSize.X * parentAlign.X) + (parentSize.X * RelPos.X - Size.X * localAxis.X);
                Pos.Y = (parentPos.Y + parentSize.Y * parentAlign.Y) + (parentSize.Y * RelPos.Y - Size.Y * localAxis.Y);
            }
            else if (UseLocalPos)
            {
                Vector2 parentAlign = AlignValue(ParentAlign);
                Vector2 localAxis = AlignValue(LocalAxis);
                Pos.X = (parentPos.X + parentSize.X * parentAlign.X) + (LocalPos.X - Size.X * localAxis.X);
                Pos.Y = (parentPos.Y + parentSize.Y * parentAlign.Y) + (LocalPos.Y - Size.Y * localAxis.Y);
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

        public void RemoveFromParent(bool deferred = false)
        {
            if (deferred)
                DeferredRemove = true;
            else if (Parent is UIElementContainer container)
                container.Remove(this);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////
        /// Events
        
        /// EVT: Added to a parent container
        public virtual void OnAdded(UIElementContainer parent)
        {
        }

        /// EVT: Removed from a parent.
        ///      However be aware that this UIElement may be Added again to another parent!
        public virtual void OnRemoved()
        {
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        public IReadOnlyList<UIEffect> GetEffects() => Effects;

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