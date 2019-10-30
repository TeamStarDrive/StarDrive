using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public sealed class CloseButton : UIButton
    {
        public CloseButton(UIElementV2 parent, float x, float y) 
            : base(parent, ButtonStyle.Close, new Vector2(x, y), "")
        {
            Tooltip = "Close this Screen";
        }

        protected override void OnButtonClicked()
        {
            if (Parent is GameScreen screen && !screen.IsExiting)
                screen.ExitScreen();
            base.OnButtonClicked();
        }
    }
}