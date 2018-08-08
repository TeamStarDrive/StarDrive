using Microsoft.Xna.Framework;

namespace Ship_Game
{
	public sealed class CloseButton : UIButton
	{
		public CloseButton(UIElementV2 parent, Rectangle r)
            : base(parent, ButtonStyle.Close, new Vector2(r.X, r.Y), "")
		{
            Tooltip = "Exit Screen";
            OnClick += CloseButton_OnClick;
		}

        private void CloseButton_OnClick(UIButton button)
        {
            if (Parent is GameScreen screen && !screen.IsExiting)
                screen.ExitScreen();
        }
    }
}