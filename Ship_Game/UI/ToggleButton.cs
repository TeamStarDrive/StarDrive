using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
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

    // TODO: Replace with UIButton
    public class ToggleButton : UIElementV2
    {
        // If TRUE, this ToggleButton is Toggled Active [x], if false, it is inactive [ ]
        public bool IsToggled;

        // user defined metadata
        public CombatState CombatState; // TODO Move this somewhere else
        public bool Hover;
        bool WasClicked; // purely visual

        public LocalizedText Tooltip;

        readonly ToggleButtonStyle Style;
        SubTexture IconTexture, IconActive;

        Vector2 WordPos;
        protected string IconPath;
        Rectangle IconRect;

        public Action<ToggleButton> OnClick;
        public Action<ToggleButton> OnHover;

        public override string ToString() => $"{TypeName} [{(IsToggled?"x":" ")}] {ElementDescr} Icon:{IconPath}";

        public ToggleButton(Vector2 pos, ToggleButtonStyle style, string iconPath = "")
        {
            Pos = pos;
            Size = new Vector2(style.Width, style.Height);
            Style = style;
            IconPath = iconPath;
            UpdateStyle();
            this.PerformLayout();
        }

        public ToggleButton(ToggleButtonStyle style, string iconPath, Action<ToggleButton> onClick)
        {
            Size = new Vector2(style.Width, style.Height);
            Style = style;
            IconPath = iconPath;
            OnClick = onClick;
            UpdateStyle();
            this.PerformLayout();
        }

        public override void PerformLayout()
        {
            if (IconTexture == null)
            {
                WordPos = new Vector2(X + 12 - Fonts.Arial12Bold.MeasureString(IconPath).X / 2f,
                                      Y + 12 - Fonts.Arial12Bold.LineSpacing / 2f);             
            }
            else
            {
                IconRect = new Rectangle((int)CenterX - IconTexture.Width  / 2,
                                         (int)CenterY - IconTexture.Height / 2,
                                         IconTexture.Width, IconTexture.Height);
            }
        }

        void UpdateStyle()
        {
            if (Style.ContentId != ResourceManager.ContentId)
            {
                Style.Reload();
            }
            if (IconPath.NotEmpty())
            {
                IconTexture = ResourceManager.TextureOrNull(IconPath);
                IconActive  = ResourceManager.TextureOrNull(IconPath+"_active");
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            UpdateStyle();

            if (WasClicked)
            {
                WasClicked = false;
                batch.Draw(Style.Press, Rect, Color.White);
            }
            if (IsToggled)
            {
                batch.Draw(Style.Active, Rect, Color.White);
            }
            else if (Hover)
            {
                batch.Draw(Style.Hover, Rect, Color.White);                
            }
            else
            {
                batch.Draw(Style.Inactive, Rect, Color.White);
            }

            if (IconTexture == null)
            {
                batch.DrawString(Fonts.Arial12Bold, IconPath, WordPos, IsToggled ? Color.White : Color.Gray);
            }
            else
            {
                Rectangle iconRect = IconActive == null ? IconRect : Rect;
                batch.Draw(IconActive ?? IconTexture, iconRect, Color.White);            
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (!Visible || !Enabled)
                return false;

            bool wasHovered = Hover;
            Hover = base.HitTest(input.CursorPosition);
            if (Hover)
            {
                if (!wasHovered)
                    GameAudio.ButtonMouseOver();

                if (Tooltip.IsValid)
                        ToolTip.CreateTooltip(Tooltip);

                OnHover?.Invoke(this);

                if (input.LeftMouseClick)
                {
                    GameAudio.AcceptClick();
                    IsToggled = !IsToggled;
                    WasClicked = true;
                    OnClick?.Invoke(this);
                    return true;
                }

                // edge case: capture mouse release events
                return input.LeftMouseReleased;
            }
            return false;
        }
    }
}