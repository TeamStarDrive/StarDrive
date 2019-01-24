using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class ToggleButtonStyle
    {
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public int ContentId { get; private set; }
        string Folder;
        public SubTexture Active   { get; private set; }
        public SubTexture Inactive { get; private set; }
        public SubTexture Hover    { get; private set; }
        public SubTexture Press    { get; private set; }

        public void Reload()
        {
            ContentId = ResourceManager.ContentId;
            Active   = ResourceManager.Texture(Folder + Active.Name);
            Inactive = ResourceManager.Texture(Folder + Inactive.Name);
            Hover    = ResourceManager.Texture(Folder + Hover.Name);
            Press    = ResourceManager.Texture(Folder + Press.Name);
        }

        public static readonly ToggleButtonStyle Formation = new ToggleButtonStyle
        {
            Width  = 24,
            Height = 24,
            ContentId = ResourceManager.ContentId,
            Folder   = "SelectionBox/",
            Active   = ResourceManager.Texture("SelectionBox/button_formation_active"),
            Inactive = ResourceManager.Texture("SelectionBox/button_formation_inactive"),
            Hover    = ResourceManager.Texture("SelectionBox/button_formation_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_formation_pressed")
        };

        public static readonly ToggleButtonStyle Grid = new ToggleButtonStyle
        {
            Width  = 34,
            Height = 24,
            ContentId = ResourceManager.ContentId,
            Folder   = "SelectionBox/",
            Active   = ResourceManager.Texture("SelectionBox/button_grid_active"),
            Inactive = ResourceManager.Texture("SelectionBox/button_grid_inactive"),
            Hover    = ResourceManager.Texture("SelectionBox/button_grid_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_grid_pressed")
        };

        public static readonly ToggleButtonStyle PlayerDesigns = new ToggleButtonStyle
        {
            Width  = 29,
            Height = 20,
            ContentId = ResourceManager.ContentId,
            Folder   = "SelectionBox/",
            Active   = ResourceManager.Texture("SelectionBox/PlayerDesignsPressed"),
            Inactive = ResourceManager.Texture("SelectionBox/PlayerDesignsActive"),
            Hover    = ResourceManager.Texture("SelectionBox/button_grid_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_grid_pressed")
        };

        public static readonly ToggleButtonStyle ArrowLeft = new ToggleButtonStyle
        {
            Width  = 14,
            Height = 35,
            ContentId = ResourceManager.ContentId,
            Folder   = "SelectionBox/",
            Active   = ResourceManager.Texture("SelectionBox/button_arrow_left"),
            Inactive = ResourceManager.Texture("SelectionBox/button_arrow_left"),
            Hover    = ResourceManager.Texture("SelectionBox/button_arrow_left_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_arrow_left_hover")
        };

        public static readonly ToggleButtonStyle ArrowRight = new ToggleButtonStyle
        {
            Width  = 14,
            Height = 35,
            ContentId = ResourceManager.ContentId,
            Folder   = "SelectionBox/",
            Active   = ResourceManager.Texture("SelectionBox/button_arrow_right"),
            Inactive = ResourceManager.Texture("SelectionBox/button_arrow_right"),
            Hover    = ResourceManager.Texture("SelectionBox/button_arrow_right_hover"),
            Press    = ResourceManager.Texture("SelectionBox/button_arrow_right_hover")
        };

        public static readonly ToggleButtonStyle ButtonB = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 22,
            ContentId = ResourceManager.ContentId,
            Folder   = "Minimap/",
            Active   = ResourceManager.Texture("Minimap/button_B_normal"),
            Inactive = ResourceManager.Texture("Minimap/button_B_normal"),
            Hover    = ResourceManager.Texture("Minimap/button_B_hover"),
            Press    = ResourceManager.Texture("Minimap/button_B_normal")
        };

        public static readonly ToggleButtonStyle ButtonC = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 22,
            ContentId = ResourceManager.ContentId,
            Folder   = "Minimap/",
            Active   = ResourceManager.Texture("Minimap/button_C_normal"),
            Inactive = ResourceManager.Texture("Minimap/button_C_normal"),
            Hover    = ResourceManager.Texture("Minimap/button_hover"),
            Press    = ResourceManager.Texture("Minimap/button_C_normal")
        };

        public static readonly ToggleButtonStyle Button = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 22,
            ContentId = ResourceManager.ContentId,
            Folder   = "Minimap/",
            Active   = ResourceManager.Texture("Minimap/button_active"),
            Inactive = ResourceManager.Texture("Minimap/button_normal"),
            Hover    = ResourceManager.Texture("Minimap/button_hover"),
            Press    = ResourceManager.Texture("Minimap/button_normal")
        };

        public static readonly ToggleButtonStyle ButtonDown = new ToggleButtonStyle
        {
            Width  = 25,
            Height = 26,
            ContentId = ResourceManager.ContentId,
            Folder   = "Minimap/",
            Active   = ResourceManager.Texture("Minimap/button_active"),
            Inactive = ResourceManager.Texture("Minimap/button_down_inactive"),
            Hover    = ResourceManager.Texture("Minimap/button_down_hover"),
            Press    = ResourceManager.Texture("Minimap/button_down_inactive")
        };
    }

    public class ToggleButton : UIElementV2
    {
        public string Action = "";

        public bool Active;
        public bool Hover;
        private bool Pressed;

        public int WhichToolTip;
        public bool HasToolTip;
        public Color BaseColor = Color.White;

        readonly ToggleButtonStyle Style;
        SubTexture IconTexture, IconActive;

        readonly Vector2 WordPos;
        readonly string IconPath;
        readonly Rectangle IconRect;

        public delegate void ClickHandler(ToggleButton button);
        public event ClickHandler OnClick;

        public override string ToString() => $"ToggleButton Icon:{IconPath} Action:{Action}";

        public ToggleButton(Vector2 pos, ToggleButtonStyle style, string iconPath = "", UIElementV2 container = null)
            : base(container, new Rectangle((int)pos.X, (int)pos.Y, style.Width, style.Height))
        {
            Style = style;    
            UpdateStyle(iconPath);

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
        
        void UpdateStyle(string iconPath)
        {
            if (Style.ContentId != ResourceManager.ContentId)
            {
                Style.Reload();
            }
            if (iconPath.NotEmpty())
            {
                IconTexture = ResourceManager.TextureOrNull(iconPath);
                IconActive  = ResourceManager.TextureOrNull(iconPath+"_active");
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            UpdateStyle(IconPath);

            if (Pressed)
            {
                batch.Draw(Style.Press, Rect, Color.White);
            }
            else if (Hover)
            {
                batch.Draw(Style.Hover, Rect, Color.White);                
            }
            else if (Active)
            {
                batch.Draw(Style.Active, Rect, Color.White);
            }
            else
            {
                batch.Draw(Style.Inactive, Rect, Color.White);
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
                GameAudio.ButtonMouseOver();
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