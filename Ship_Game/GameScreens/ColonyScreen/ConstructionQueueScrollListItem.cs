using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public class ConstructionQueueScrollListItem : ScrollListItem<ConstructionQueueScrollListItem>
    {
        readonly Planet Planet;
        public readonly QueueItem Item;

        public ConstructionQueueScrollListItem(QueueItem item)
        {
            Planet = item.Planet;
            Item = item;
            AddUp(new Vector2(-120, 0), /*Queue up*/63, OnUpClicked);
            AddDown(new Vector2(-90, 0), /*Queue down*/64, OnDownClicked);
            AddApply(new Vector2(-60, 0), /*Cancel production*/50, OnApplyClicked);
            AddCancel(new Vector2(-30, 0), /*Cancel production*/53, OnCancelClicked);
        }

        void OnUpClicked()
        {
            int index = Planet.ConstructionQueue.IndexOf(Item);
            InputState input = GameBase.ScreenManager.input;
            if (input.IsCtrlKeyDown) MoveToConstructionQueuePosition(0, index); // move to top
            else                     SwapConstructionQueueItems(index - 1, index); // move up by one
        }

        void OnDownClicked()
        {
            int index = Planet.ConstructionQueue.IndexOf(Item);
            InputState input = GameBase.ScreenManager.input;
            if (input.IsCtrlKeyDown) MoveToConstructionQueuePosition(Planet.ConstructionQueue.Count-1, index); // move to bottom
            else                     SwapConstructionQueueItems(index + 1, index); // move down by one
        }

        void OnApplyClicked()
        {
            int index = Planet.ConstructionQueue.IndexOf(Item);
            InputState input = GameBase.ScreenManager.input;

            float maxAmount = input.IsCtrlKeyDown ? 10000f : 10f;
            if (Planet.Construction.RushProduction(index, maxAmount))
                GameAudio.AcceptClick();
            else
                GameAudio.NegativeClick();
        }

        void OnCancelClicked()
        {
            Planet.Construction.Cancel(Item);
            GameAudio.AcceptClick();
        }

        void SwapConstructionQueueItems(int swapTo, int currentIndex)
        {
            swapTo       = swapTo.Clamped(0, Planet.ConstructionQueue.Count-1);
            currentIndex = currentIndex.Clamped(0, Planet.ConstructionQueue.Count-1);

            QueueItem item = Planet.ConstructionQueue[swapTo];
            Planet.ConstructionQueue[swapTo] = Planet.ConstructionQueue[currentIndex];
            Planet.ConstructionQueue[currentIndex] = item;
            GameAudio.AcceptClick();
        }

        void MoveToConstructionQueuePosition(int moveTo, int currentIndex)
        {
            QueueItem item = Planet.ConstructionQueue[currentIndex];
            Planet.ConstructionQueue.RemoveAt(currentIndex);
            Planet.ConstructionQueue.Insert(moveTo, item);
            GameAudio.AcceptClick();
        }
        
        public override void Draw(SpriteBatch batch)
        {
            Item.DrawAt(batch, Pos);
            base.Draw(batch);
        }
    }
}
