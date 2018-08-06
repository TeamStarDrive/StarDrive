using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.UI
{
    public class ToggleButtonStyle
    {
        public int Width  { get; private set; }
        public int Height { get; private set; }

        public Texture2D Active   { get; private set; }
        public Texture2D Inactive { get; private set; }
        public Texture2D Hover    { get; private set; }
        public Texture2D Press    { get; private set; }

        public static readonly ToggleButtonStyle Formation = new ToggleButtonStyle
        {
            Width  = 24,
            Height = 24,
            Active   = ResourceManager.Texture("SelectionBox/button_formation_active"),
            Inactive = ResourceManager.Texture("SelectionBox/button_formation_inactive"),
            Hover    = ResourceManager.Texture("SelectionBox/button_formation_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_formation_pressed"),
        };

        public static readonly ToggleButtonStyle Grid = new ToggleButtonStyle
        {
            Width  = 34,
            Height = 24,
            Active   = ResourceManager.Texture("SelectionBox/button_grid_active"),
            Inactive = ResourceManager.Texture("SelectionBox/button_grid_inactive"),
            Hover    = ResourceManager.Texture("SelectionBox/button_grid_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_grid_pressed"),
        };

        public static readonly ToggleButtonStyle PlayerDesigns = new ToggleButtonStyle
        {
            Width  = 29,
            Height = 20,
            Active   = ResourceManager.Texture("SelectionBox/PlayerDesignsPressed"),
            Inactive = ResourceManager.Texture("SelectionBox/PlayerDesignsActive"),
            Hover    = ResourceManager.Texture("SelectionBox/button_grid_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_grid_pressed"),
        };

        public static readonly ToggleButtonStyle ArrowLeft = new ToggleButtonStyle
        {
            Width  = 14,
            Height = 35,
            Active   = ResourceManager.Texture("SelectionBox/button_arrow_left"),
            Inactive = ResourceManager.Texture("SelectionBox/button_arrow_left"),
            Hover    = ResourceManager.Texture("SelectionBox/button_arrow_left_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_arrow_left_hover"),
        };

        public static readonly ToggleButtonStyle ArrowRight = new ToggleButtonStyle
        {
            Width  = 14,
            Height = 35,
            Active   = ResourceManager.Texture("SelectionBox/button_arrow_right"),
            Inactive = ResourceManager.Texture("SelectionBox/button_arrow_right"),
            Hover    = ResourceManager.Texture("SelectionBox/button_arrow_right_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_arrow_right_hover"),
        };

        public static readonly ToggleButtonStyle ButtonB = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 22,
            Active   = ResourceManager.Texture("Minimap/button_B_normal"),
            Inactive = ResourceManager.Texture("Minimap/button_B_normal"),
            Hover    = ResourceManager.Texture("Minimap/button_B_hover"),
            Press    = ResourceManager.Texture("Minimap/button_B_normal"),
        };

        public static readonly ToggleButtonStyle ButtonC = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 22,
            Active   = ResourceManager.Texture("Minimap/button_C_normal"),
            Inactive = ResourceManager.Texture("Minimap/button_C_normal"),
            Hover    = ResourceManager.Texture("Minimap/button_hover"),
            Press    = ResourceManager.Texture("Minimap/button_C_normal"),
        };

        public static readonly ToggleButtonStyle Button = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 22,
            Active   = ResourceManager.Texture("Minimap/button_active"),
            Inactive = ResourceManager.Texture("Minimap/button_normal"),
            Hover    = ResourceManager.Texture("Minimap/button_hover"),
            Press    = ResourceManager.Texture("Minimap/button_normal"),
        };

        public static readonly ToggleButtonStyle ButtonDown = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 26,
            Active   = ResourceManager.Texture("Minimap/button_active"),
            Inactive = ResourceManager.Texture("Minimap/button_down_inactive"),
            Hover    = ResourceManager.Texture("Minimap/button_down_hover"),
            Press    = ResourceManager.Texture("Minimap/button_down_inactive"),
        };
    }

    public class ToggleButton : UIElementV2
    {
        public object ReferenceObject;

        public string Action = "";

        public bool Active;
        public bool Hover;
        private bool Pressed;

        public int WhichToolTip;
        public bool HasToolTip;
        public Color BaseColor = Color.White;

        private readonly Texture2D PressTexture;
        private readonly Texture2D HoverTexture;
        private readonly Texture2D ActiveTexture;
        private readonly Texture2D InactiveTexture;
        private readonly Texture2D IconTexture;
        private readonly Vector2 WordPos;
        private readonly string IconPath;
        private readonly Texture2D IconActive;
        private readonly Rectangle IconRect;

        public delegate void ClickHandler(ToggleButton button);
        public event ClickHandler OnClick;

        public ToggleButton(Vector2 pos, ToggleButtonStyle style, string iconPath = "", UIElementV2 container = null)
            : base(container, new Rectangle((int)pos.X, (int)pos.Y, style.Width, style.Height))
        {
            PressTexture    = style.Press;
            HoverTexture    = style.Hover;
            ActiveTexture   = style.Active;
            InactiveTexture = style.Inactive;      
            
            if (iconPath.NotEmpty())
            {
                IconTexture = ResourceManager.Texture(iconPath, "");
                IconActive  = ResourceManager.Texture(iconPath+"_active", "");
            }

            if (IconTexture == null)
            {
                IconPath = iconPath;
                WordPos = new Vector2(Rect.X + 12 - Fonts.Arial12Bold.MeasureString(IconPath).X / 2f,
                    Rect.Y + 12 - Fonts.Arial12Bold.LineSpacing / 2);             
            }
            else
            {
                IconRect = new Rectangle(Rect.X + Rect.Width  / 2 - IconTexture.Width  / 2,
                    Rect.Y + Rect.Height / 2 - IconTexture.Height / 2,
                    IconTexture.Width, IconTexture.Height);
            }
        }

        //hack... until this is all straightend out to allow override of base draw.
        public void Draw(ScreenManager screenManager) => Draw(screenManager.SpriteBatch);
        
        public override void Draw(SpriteBatch batch)
        {
            if (Pressed)
            {
                batch.Draw(PressTexture, Rect, Color.White);
            }
            else if (Hover)
            {
                batch.Draw(HoverTexture, Rect, Color.White);                
            }
            else if (Active)
            {
                batch.Draw(ActiveTexture, Rect, Color.White);
            }
            else
            {
                batch.Draw(InactiveTexture, Rect, Color.White);
            }

            if (IconTexture == null)
            {
                if (Active)
                {
                    batch.DrawString(Fonts.Arial12Bold, IconPath, WordPos, Color.White);
                    return;
                }

                batch.DrawString(Fonts.Arial12Bold, IconPath, WordPos, Color.Gray);
            }
            else
            {
                Rectangle iconRect = IconActive == null ? IconRect : Rect;
                batch.Draw(IconActive ?? IconTexture, iconRect, Color.White);            
            }
        }

        public override bool HandleInput(InputState input)
        {
            Pressed = false;
            if (!Rect.HitTest(input.CursorPosition))
            {
                if (Hover)
                {
                    if (ToolTip.TipTimer > 3)
                    {
                        ToolTip.LastWhich = 0;
                        ToolTip.TextLast = string.Empty;
                    }

                    ToolTip.TipTimer = 0;
                }
                Hover = false;
                return false;
            }
            if (!Hover)
            {
                GameAudio.MiniMapMouseOver();
                if (WhichToolTip != 0)
                    ToolTip.CreateTooltip(WhichToolTip);
            }
            Hover = true;

            if (input.LeftMouseClick)
            {
                OnClick?.Invoke(this);
                Pressed = true;
                return true;
            }

            // edge case: capture mouse release events
            return input.LeftMouseReleased;
        }
    }
}