using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public enum ColonyResType
    {
        Food,
        Prod,
        Res
    }

    public class ColonySlider : UIElementV2
    {
        public delegate void SliderChangeEvent(ColonySlider slider, float difference);
        public SliderChangeEvent OnSliderChange;

        readonly ColonyResType Type;
        public Planet P;
        readonly SubTexture Slider, Icon;
        readonly SubTexture Lock           = ResourceManager.Texture("NewUI/icon_lock");
        readonly SubTexture Minute         = ResourceManager.Texture("NewUI/slider_minute");
        readonly SubTexture MinuteHover    = ResourceManager.Texture("NewUI/slider_minute_hover");
        readonly SubTexture Crosshair      = ResourceManager.Texture("NewUI/slider_crosshair");
        readonly SubTexture CrosshairHover = ResourceManager.Texture("NewUI/slider_crosshair_hover");
        Rectangle LockRect;
        static readonly Color DefaultColor = new Color(72, 61, 38);
        static readonly Color HoverColor = new Color(164, 154, 133);

        bool SliderHover;
        bool LockHover;
        readonly bool DrawIcons;

        public bool IsDragging { get; private set; }
        public bool CanDrag;
        public bool IsDisabled;
        public bool IsCrippled; // PRODUCTION resource: are we crippled?
        public bool IsInvasion; // PRODUCTION resource: invasion leaves us crippled as well?

        public ColonySlider(ColonyResType type, Planet p, bool drawIcons = true)
        {
            Height = 6;
            Type = type;
            P = p;
            var sliders = new[]{ "green", "brown", "blue" };
            var icons   = new[]{ "food", "production", "science" };
            Slider    = ResourceManager.Texture($"NewUI/slider_grd_{sliders[(int)type]}");
            Icon      = ResourceManager.Texture($"NewUI/icon_{icons[(int)type]}");
            DrawIcons = drawIcons;
            RequiresLayout = true;
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LockRect = new Rectangle(Rect.Right + 10, 
                                     Rect.Center.Y + 2 - Lock.Height / 2, Lock.Width, Lock.Height);
        }

        ToolTipText Tooltip()
        {
            switch (Type)
            {
                default: return P.IsCybernetic ? 77 : 70;
                case ColonyResType.Prod: return 71;
                case ColonyResType.Res:  return 72;
            }
        }

        public ColonyResource Resource
        {
            get
            {
                switch (Type)
                {
                    default:                 return P.Food;
                    case ColonyResType.Prod: return P.Prod;
                    case ColonyResType.Res:  return P.Res;
                }
            }
        }

        public float Value
        {
            get => Resource.Percent;
            set => Resource.Percent = value.NaNChecked(0f, "ColonySlider.Value");
        }

        public float NetValue => Resource.NetIncome;

        public bool LockedByUser
        {
            get => Resource.PercentLock;
            set => Resource.PercentLock = value;
        }

        bool IsAIGovernor => P.colonyType != Planet.ColonyType.Colony;

        public override bool HandleInput(InputState input)
        {
            if (IsDisabled)
                return false;

            Vector2 mousePos = input.CursorPosition;
            bool mouseOverSlider = !LockedByUser && !IsAIGovernor && Rect.Bevel(5).HitTest(mousePos);

            // slider drag is stateful to give user more convenient slide experience
            if (IsDragging)
            {
                if (!input.LeftMouseHeldDown) // LMB not down anymore?
                    IsDragging = false; // stop sliding
            }
            else if (CanDrag)
            {
                if (mouseOverSlider && input.LeftMouseClick)
                    IsDragging = true;
            }

            // @note No tooltips or other stuff during sliding
            if (IsDragging)
            {
                SliderHover = true;
                HandleDragging((int)mousePos.X);
                return true;
            }

            SliderHover = mouseOverSlider;

            LockHover = false;
            if (!IsAIGovernor)
            {
                LockHover = LockRect.HitTest(mousePos);
                if (LockHover) // hovering over lock?
                {
                    if (input.LeftMouseClick)
                    {
                        LockedByUser = !LockedByUser;
                        GameAudio.AcceptClick();
                    }
                    ToolTip.CreateTooltip(69);
                }
            }
            if (DrawIcons && !LockHover) // maybe hovering over icon?
            {
                if (IconRect().HitTest(input.CursorPosition) && Empire.Universe.IsActive)
                    ToolTip.CreateTooltip(Tooltip());
            }
            return false;
        }

        void HandleDragging(int mouseX)
        {
            float newRelX = (mouseX - Rect.Left) / (float)Rect.Width;
            float difference = newRelX.Clamped(0f, 1f) - Value;
            if (Math.Abs(difference) >= 0.001f)
            {
                OnSliderChange?.Invoke(this, difference);
            }
        }

        Rectangle CursorRect()
        {
            int posX = Rect.RelativeX(Value) - Crosshair.CenterX;
            int posY = Rect.CenterY() - Crosshair.CenterY;
            return new Rectangle(posX, posY, Crosshair.Width, Crosshair.Height);
        }

        Rectangle IconRect()
        {
            return new Rectangle(Rect.X-40, Rect.Center.Y - Icon.CenterY, Icon.Width, Icon.Height);
        }
        
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Color sliderTint = IsDisabled ? Color.DarkGray : Color.White;

            batch.Draw(Slider, new Rectangle(Rect.X, Rect.Y, (int)(Value * Rect.Width), Rect.Height), sliderTint);
            batch.DrawRectangle(Rect, SliderHover ? HoverColor : DefaultColor);

            if (DrawIcons)
            {
                batch.Draw(Icon, IconRect(), sliderTint);
            }

            if (!IsDisabled)
                batch.Draw(SliderHover ? CrosshairHover : Crosshair, CursorRect(), sliderTint);

            SubTexture minute = SliderHover ? MinuteHover : Minute;
            var tickPos = new Vector2(Rect.X, Rect.Bottom + 1);
            for (int i = 0; i < 11; ++i)
            {
                tickPos.X = Rect.X + (int)(((Rect.Width-1) / 10f)*i); // @note Yeah, cast is important
                batch.Draw(minute, tickPos, sliderTint);
            }

            DrawLock(batch);
            DrawValueText(batch);
        }

        void DrawLock(SpriteBatch batch)
        {
            if (IsDisabled) return;

            if (!LockedByUser && !IsAIGovernor)
            {
                Color color = LockHover ? new Color(255, 255, 255, 150) : new Color(255, 255, 255, 50);
                batch.Draw(Lock, LockRect, color);
            }
            else
            {
                batch.Draw(Lock, LockRect, Color.White);
            }
        }

        void DrawValueText(SpriteBatch batch)
        {
            var font = Fonts.Arial12Bold;
            var pos = new Vector2(LockRect.Right + 10, Rect.CenterY() - font.LineSpacing / 2);
            float value = NetValue;
            string text;
            if      (IsDisabled) text = "n/a";
            else if (IsCrippled) text = Localizer.Token(2202);/*sabotaged*/
            else if (IsInvasion) text = Localizer.Token(2257);/*invasion!*/
            else                 text = value.String();
            batch.DrawString(font, text, pos, (value < 0.0f ? Color.LightPink : Colors.Cream));
        }
    }
}
