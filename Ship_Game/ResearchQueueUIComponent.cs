using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class ResearchQueueUIComponent : UIPanel
    {
        readonly ResearchScreenNew Screen;

        readonly Submenu CurrentResearchPanel;
        readonly UIPanel TimeLeft;
        readonly UILabel TimeLeftLabel;

        ResearchQItem CurrentResearch;
        readonly ScrollList<ResearchQItem> ResearchQueueList;
        readonly UIButton BtnShowQueue;

        public ResearchQueueUIComponent(ResearchScreenNew screen, in Rectangle container)  : base(container, Color.Black)
        {
            Screen = screen;

            BtnShowQueue = Button(ButtonStyle.BigDip, 
                new Vector2(container.Right - 170, screen.ScreenHeight - 55), "", OnBtnShowQueuePressed);

            RectF current = new(container.X, container.Y, container.Width, 150);
            RectF timeLeftRect = new(current.X + current.W - 119, current.Y + current.H - 24, 111, 20);
            TimeLeft = Panel(timeLeftRect, Color.White, ResourceManager.Texture("ResearchMenu/timeleft"));
            
            var labelPos = new Vector2(TimeLeft.X + 26,
                                       TimeLeft.Y + TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2);
            TimeLeftLabel = TimeLeft.Label(labelPos, "", Fonts.Verdana14Bold, new Color(205, 229, 255));

            CurrentResearchPanel = Add(new Submenu(current, GameText.CurrentResearch, SubmenuStyle.Blue));
            
            RectF queue = new(current.X, current.Y + 165, container.Width, container.Height - 165);
            var queueSub = Add(new SubmenuScrollList<ResearchQItem>(queue, GameText.ResearchQueue, 125, ListStyle.Blue));
            ResearchQueueList = queueSub.List;

            // FB Disabled due to being able to drag stuff to be before other research mandatory for it.
            //ResearchQueueList.OnDragReorder = OnResearchItemReorder; 
            ReloadResearchQueue();
        }

        // TODO: check if we are moving item up before allowed item
        void OnResearchItemReorder(ResearchQItem item, int oldIndex, int newIndex)
        {
            // we use +1 here, because [0] is the current research item
            // which is not in the ScrollList
            Screen.Player.Research.ReorderTech(oldIndex+1, newIndex+1);
        }

        void OnBtnShowQueuePressed(UIButton button)
        {
            SetQueueVisible(!ResearchQueueList.Visible);
        }

        void SetQueueVisible(bool visible)
        {
            if (CurrentResearch != null)
            {
                TimeLeft.Visible = visible;
                CurrentResearch.Visible = visible;
            }
            else
            {
                TimeLeft.Visible = false;
            }

            ResearchQueueList.Visible = visible;
            CurrentResearchPanel.Visible = visible;
            BtnShowQueue.Text = ResearchQueueList.Visible ? GameText.HideQueue : GameText.ShowQueue;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                Screen.ExitScreen();
                return true;
            }

            if (ResearchQueueList.Visible && input.RightMouseClick &&
                ResearchQueueList.HitTest(input.CursorPosition))
            {
                Screen.ExitScreen();
                return true;
            }

            if (CurrentResearch != null && CurrentResearch.HandleInput(input))
                return true;

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            if (ResearchQueueList.Visible && CurrentResearch != null)
            {
                CurrentResearch.Draw(batch, elapsed);

                float remaining = CurrentResearch.Tech.TechCost - CurrentResearch.Tech.Progress;
                float numTurns = (float)Math.Ceiling(remaining / (0.01f + Screen.Player.Research.NetResearch));
                TimeLeftLabel.Text = (numTurns > 999f) ? ">999 turns" : numTurns.String(0)+" turns";
            }
        }

        ResearchQItem CreateQueueItem(TreeNode node)
        {
            var defaultPos = new Vector2(CurrentResearchPanel.X + 5, CurrentResearchPanel.Y + 30);
            return new ResearchQItem(Screen, node, defaultPos) { List = ResearchQueueList };
        }

        public void AddToResearchQueue(TreeNode node)
        {
            if (Screen.Player.Research.AddToQueue(node.Entry.UID))
            {
                if (CurrentResearch == null)
                    CurrentResearch = CreateQueueItem(node);
                else
                    ResearchQueueList.AddItem(CreateQueueItem(node));

                SetQueueVisible(true);
            }
        }

        public void ReloadResearchQueue()
        {
            CurrentResearch = Screen.Player.Research.HasTopic
                            ? CreateQueueItem((TreeNode)Screen.AllTechNodes[Screen.Player.Research.Topic])
                            : null;

            var items = new Array<ResearchQItem>();
            foreach (string tech in Screen.Player.Research.QueuedItems)
            {
                items.Add(CreateQueueItem( (TreeNode)Screen.AllTechNodes[tech] ));
            }
            ResearchQueueList.SetItems(items);

            SetQueueVisible(Screen.Player.Research.HasTopic);
        }
    }
}
