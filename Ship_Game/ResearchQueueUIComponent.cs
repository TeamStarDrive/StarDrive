using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class ResearchQueueUIComponent : UIElementContainer
	{
        readonly ResearchScreenNew Screen;
        readonly Submenu CurrentResearchPanel;
        readonly Submenu ResearchQueuePanel;

        readonly Rectangle TimeLeft;

        public ResearchQItem CurrentResearch { get; private set; }
        readonly ScrollList QSL;
        bool IsQueueVisible;
        readonly UIButton BtnShowQueue;

		public ResearchQueueUIComponent(ResearchScreenNew screen, in Rectangle container)  : base(screen, container)
		{
            Screen = screen;

            BtnShowQueue = Button(ButtonStyle.DanButtonBlue, 
                new Vector2(container.Right - 192, screen.ScreenHeight - 55), "", OnBtnShowQueuePressed);
            BtnShowQueue.TextAlign = ButtonTextAlign.Left;
            SetQueueVisible(EmpireManager.Player.Research.HasTopic);

            var current = new Rectangle(container.X, container.Y, container.Width, 150);
			TimeLeft = new Rectangle(current.X + current.Width - 119, current.Y + current.Height - 24, 111, 20);
			
            CurrentResearchPanel = new Submenu(true, current);
			CurrentResearchPanel.AddTab(Localizer.Token(1405));
			
            var queue = new Rectangle(current.X, current.Y + 165, container.Width, container.Height - 165);
            ResearchQueuePanel = new Submenu(true, queue);
			ResearchQueuePanel.AddTab(Localizer.Token(1404));
            QSL = new ScrollList(ResearchQueuePanel, 125, ListControls.All, ListStyle.Blue) {AutoHandleInput = true};
        }

        void OnBtnShowQueuePressed(UIButton button)
        {
            SetQueueVisible(!IsQueueVisible);
        }

        public void SetQueueVisible(bool visible)
        {
            IsQueueVisible = visible;
            BtnShowQueue.Text = Localizer.Token(IsQueueVisible ? 2136 : 2135);
        }

        public override bool HandleInput(InputState input)
        {
            if ((IsQueueVisible && input.RightMouseClick && ResearchQueuePanel.Menu.HitTest(input.CursorPosition))
                || input.Escaped)
            {
                Screen.ExitScreen();
                return true;
            }

            if (IsQueueVisible)
            {
                if (CurrentResearch != null && CurrentResearch.HandleInput(input))
                    return true;

                if (QSL.HandleInput(input))
                    return true;
            }

            return base.HandleInput(input);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
        }
        
        public override void Draw(SpriteBatch batch)
        {
            BtnShowQueue.Draw(batch);

            if (!IsQueueVisible)
                return;

            batch.FillRectangle(Rect, Color.Black);

            QSL.Draw(batch);
            CurrentResearchPanel.Draw(batch);

            if (CurrentResearch != null)
            {
                var currentResPos = new Vector2(CurrentResearchPanel.Menu.X + 5, CurrentResearchPanel.Menu.Y + 30);
                CurrentResearch.Draw(batch, currentResPos);

                batch.Draw(ResourceManager.Texture("ResearchMenu/timeleft"), TimeLeft, Color.White);
                var cursor = new Vector2(TimeLeft.X + TimeLeft.Width - 7, TimeLeft.Y + TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2 - 2);
				
                TechEntry tech = CurrentResearch.Tech;
                float cost = tech.TechCost - tech.Progress;
                float numTurns = (float)Math.Ceiling(cost / (0.01f + EmpireManager.Player.Research.NetResearch));
                string text = (numTurns > 999f) ? ">999 turns" : numTurns.String(0)+" turns";

                cursor.X -= Fonts.Verdana14Bold.MeasureString(text).X;
                batch.DrawString(Fonts.Verdana14Bold, text, cursor, new Color(205, 229, 255));
            }

            foreach (ScrollList.Entry e in QSL.VisibleExpandedEntries)
            {
                e.Get<ResearchQItem>().Draw(batch, e.Rect.PosVec());
            }

            ResearchQueuePanel.Draw(batch);
        }

        public void AddToResearchQueue(TreeNode researchItem)
        {
            if (EmpireManager.Player.Research.AddToQueue(researchItem.Entry.UID))
            {
                if (CurrentResearch == null)
                    CurrentResearch = new ResearchQItem(Screen, researchItem);
                else
                    QSL.AddItem(new ResearchQItem(Screen, researchItem));
            }
        }

        public void ReloadResearchQueue()
        {
            CurrentResearch = EmpireManager.Player.Research.HasTopic
                            ? new ResearchQItem(Screen, (TreeNode)Screen.AllTechNodes[EmpireManager.Player.Research.Topic])
                            : null;

            Array<string> techIds = EmpireManager.Player.Research.Queue;
            var items = new Array<ResearchQItem>();

            for (int i = 1; i < techIds.Count; ++i)
            {
                items.Add(new ResearchQItem(Screen, (TreeNode)Screen.AllTechNodes[techIds[i]]));
            }
            QSL.SetItems(items);
        }
	}
}