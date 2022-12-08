using SDGraphics;

namespace Ship_Game
{
	public sealed class BlueButton : UIButton
	{
		public BlueButton(Vector2 pos, in LocalizedText text) : this(text)
		{
			Pos = pos;
        }

        public BlueButton(in LocalizedText text)
            : base(new StyleTextures(
                normal: "NewUI/button_blue_hover1",
                hover: "NewUI/button_blue_hover2",
                pressed: "NewUI/button_blue_hover0"), size: new Vector2(180, 33), text)
        {
            Text = text;
        }
    }
}