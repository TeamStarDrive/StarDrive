using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game
{
	public sealed class SelectedShipEntry
	{
		public Array<SkinnableButton> ShipButtons = new Array<SkinnableButton>();

	    public void Update(Vector2 position)
		{
			Vector2 cursor = position;
			foreach (SkinnableButton button in ShipButtons)
			{
				button.r.X = (int)cursor.X;
				button.r.Y = (int)cursor.Y;
				cursor.X = cursor.X + 24f;
			}
		}

	    public bool AllButtonsActive
	    {
            get
            {
                foreach (SkinnableButton button in ShipButtons)
                {
                    if (((Ship)button.ReferenceObject).Active)
                        continue;
                    return false;
                }
                return true;
            }
	    }
	}
}