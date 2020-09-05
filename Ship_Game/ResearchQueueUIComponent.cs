using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class ResearchQueueUIComponent : UIPanel
    {
        readonly ResearchScreenNew Screen;

        readonly Submenu CurrentResearchPanel;
        readonly UIPanel TimeLeft;
        readonly UILabel TimeLeftLabel;

        ResearchQItem CurrentResearch;
        readonly ScrollList2<ResearchQItem> ResearchQueueList;
        readonly UIButton BtnShowQueue;

        public ResearchQueueUIComponent(ResearchScreenNew screen, in Rectangle container)  : base(container, Color.Black)
        {
            Screen = screen;

            BtnShowQueue = Button(ButtonStyle.DanButtonBlue, 
                new Vector2(container.Right - 192, screen.ScreenHeight - 55), "", OnBtnShowQueuePressed);
            BtnShowQueue.TextAlign = ButtonTextAlign.Left;

            var current = new Rectangle(container.X, container.Y, container.Width, 150);
            var timeLeftRect = new Rectangle(current.X + current.Width - 119, current.Y + current.Height - 24, 111, 20);
            TimeLeft = Panel(timeLeftRect, Color.White, ResourceManager.Texture("ResearchMenu/timeleft"));
            
            var labelPos = new Vector2(TimeLeft.X + 26,
                                       TimeLeft.Y + TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2);
            TimeLeftLabel = TimeLeft.Label(labelPos, "", Fonts.Verdana14Bold, new Color(205, 229, 255));

            CurrentResearchPanel = Add(new Submenu(current, SubmenuStyle.Blue));
            CurrentResearchPanel.AddTab(Localizer.Token(1405));
            
            var queue = new Rectangle(current.X, current.Y + 165, container.Width, container.Height - 165);
            var queuePanel = new Submenu(queue, SubmenuStyle.Blue);
            queuePanel.AddTab(Localizer.Token(1404));
            ResearchQueueList = Add(new ScrollList2<ResearchQItem>(queuePanel, 125, ListStyle.Blue));
            // FB Disabled due to being able to drag stuff to be before other research mandatory for it.
            //ResearchQueueList.OnDragReorder = OnResearchItemReorder; 
            ReloadResearchQueue();
        }

        void OnResearchItemReorder(ResearchQItem item, int oldIndex, int newIndex)
        {
            // we use +1 here, because [0] is the current research item
            // which is not in the ScrollList
            EmpireManager.Player.Research.ReorderTech(oldIndex+1, newIndex+1);
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
            BtnShowQueue.Text = ResearchQueueList.Visible ? 2136 : 2135;
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
                float numTurns = (float)Math.Ceiling(remaining / (0.01f + EmpireManager.Player.Research.NetResearch));
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
            if (EmpireManager.Player.Research.AddToQueue(node.Entry.UID))
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
            CurrentResearch = EmpireManager.Player.Research.HasTopic
                            ? CreateQueueItem((TreeNode)Screen.AllTechNodes[EmpireManager.Player.Research.Topic])
                            : null;

            var items = new Array<ResearchQItem>();
            foreach (string tech in EmpireManager.Player.Research.QueuedItems)
            {
                items.Add(CreateQueueItem( (TreeNode)Screen.AllTechNodes[tech] ));
            }
            ResearchQueueList.SetItems(items);

            SetQueueVisible(EmpireManager.Player.Research.HasTopic);
        }
    }
}