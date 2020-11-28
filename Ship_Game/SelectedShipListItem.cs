using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class SelectedShipListItem : ScrollListItem<SelectedShipListItem>
    {
        readonly ShipListInfoUIElement ShipListInfo;
        public Array<SkinnableButton> ShipButtons = new Array<SkinnableButton>();
        public Action<SkinnableButton> OnShipButtonClick;

        public SelectedShipListItem(ShipListInfoUIElement parent, Action<SkinnableButton> onShipBtnClick)
        {
            ShipListInfo = parent;
            OnShipButtonClick = onShipBtnClick;
        }

        public override void PerformLayout()
        {
            Vector2 cursor = Pos;
            foreach (SkinnableButton button in ShipButtons)
            {
                button.r.X = (int)cursor.X;
                button.r.Y = (int)cursor.Y;
                cursor.X += 24f;
            }
            base.PerformLayout();
        }

        public bool AllButtonsActive
        {
            get
            {
                foreach (SkinnableButton button in ShipButtons)
                {
                    if (((Ship)button.ReferenceObject).Active == false)
                        return false;
                }
                return true;
            }
        }

        public override bool HandleInput(InputState input)
        {
            foreach (SkinnableButton button in ShipButtons)
            {
                if (!button.r.HitTest(input.CursorPosition))
                {
                    button.Hover = false;
                }
                else
                {
                    button.Hover = true;

                    if (ShipListInfo.HoveredShipLast != (Ship)button.ReferenceObject)
                        GameAudio.ButtonMouseOver();
                    ShipListInfo.HoveredShip = (Ship)button.ReferenceObject;

                    if (input.InGameSelect)
                    {
                        OnShipButtonClick(button);
                        return true;
                    }
                }
            }
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            
            foreach (SkinnableButton button in ShipButtons)
            {
                button.Draw(batch);
            }
        }
    }
}