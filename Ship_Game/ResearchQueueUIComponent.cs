using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class ResearchQueueUIComponent : UIElementContainer
	{
        readonly ResearchScreenNew Screen;
        readonly Submenu ActiveResearchPanel;
        readonly Submenu ResearchQueuePanel;

        readonly Rectangle TimeLeft;
        public ScrollList QSL;
        bool IsQueueVisible;
        readonly UIButton BtnShowQueue;

		public ResearchQueueUIComponent(ResearchScreenNew screen, in Rectangle container)  : base(screen, container)
		{
            Screen = screen;

            BtnShowQueue = Button(ButtonStyle.DanButtonBlue, 
                new Vector2(container.Right - 192, screen.ScreenHeight - 55), "", OnBtnShowQueuePressed);
            BtnShowQueue.TextAlign = ButtonTextAlign.Left;
            SetQueueVisible(EmpireManager.Player.HasResearchTopic);

            var current = new Rectangle(container.X, container.Y, container.Width, 150);
			TimeLeft = new Rectangle(current.X + current.Width - 119, current.Y + current.Height - 24, 111, 20);
			
            ActiveResearchPanel = new Submenu(true, current);
			ActiveResearchPanel.AddTab(Localizer.Token(1405));
			
            var queue = new Rectangle(current.X, current.Y + 165, container.Width, container.Height - 165);
            ResearchQueuePanel = new Submenu(true, queue);
			ResearchQueuePanel.AddTab(Localizer.Token(1404));
            QSL = new ScrollList(ResearchQueuePanel, 125) {AutoHandleInput = true};
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

        bool GetCurrentResearchItem(out ResearchQItem current)
        {
            current = null;
            return QSL.NumEntries > 0 && QSL.EntryAt(0).TryGet(out current);
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
                if (GetCurrentResearchItem(out ResearchQItem current) && current.HandleInput(input))
                    return true;

                if (QSL.HandleInput(input))
                    return true;
            }

            return base.HandleInput(input);
        }

        public override void Update(float deltaTime)
        {
            if (QSL.NumEntries > 0)
            {
                QSL.EntryAt(0).Visible = false;
                for (int i = 1; i < QSL.NumEntries; ++i)
                    QSL.EntryAt(i).Visible = true;

                QSL.UpdateListElements();
            }
            base.Update(deltaTime);
        }
        
        public override void Draw(SpriteBatch batch)
        {
            BtnShowQueue.Draw(batch);

            if (!IsQueueVisible)
                return;

            batch.FillRectangle(Rect, Color.Black);

            QSL.DrawBlue(batch);
            ActiveResearchPanel.Draw(batch);

            if (QSL.NumEntries > 0)
            {
                var activeResearchPos = new Vector2(ActiveResearchPanel.Menu.X + 5, ActiveResearchPanel.Menu.Y + 30);
                var currentResearch = QSL.EntryAt(0).Get<ResearchQItem>();
                currentResearch.Draw(batch, activeResearchPos);

                batch.Draw(ResourceManager.Texture("ResearchMenu/timeleft"), TimeLeft, Color.White);
                var cursor = new Vector2(TimeLeft.X + TimeLeft.Width - 7, TimeLeft.Y + TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2 - 2);
				
                TechEntry tech = currentResearch.Node.Entry;
                float cost = tech.TechCost - tech.Progress;
                float numTurns = (float)Math.Ceiling(cost / (0.01f + EmpireManager.Player.GetProjectedResearchNextTurn()));
                string text = (numTurns > 999f) ? ">999 turns" : numTurns.String(0)+" turns";

                cursor.X -= Fonts.Verdana14Bold.MeasureString(text).X;
                batch.DrawString(Fonts.Verdana14Bold, text, cursor, new Color(205, 229, 255));
            }

            if (QSL.NumEntries > 1)
            {
                ScrollList.Entry[] entries = QSL.VisibleExpandedEntries;
                for (int i = 0; i < entries.Length; ++i)
                {
                    ScrollList.Entry e = entries[i];
                    e.Get<ResearchQItem>().Draw(batch, e.Rect.PosVec());
                }
            }

            ResearchQueuePanel.Draw(batch);
        }

        ResearchQItem CreateQItem(TreeNode researchItem)
        {
            return new ResearchQItem(new Vector2(ActiveResearchPanel.Menu.X + 5, ActiveResearchPanel.Menu.Y + 30), researchItem, Screen);
        }

        public void AddToResearchQueue(TreeNode researchItem)
        {
            if (EmpireManager.Player.AddToResearchQueue(researchItem.Entry.UID))
                QSL.AddItem(CreateQItem(researchItem));
        }

        public void ReloadResearchQueue()
        {
            var items = new Array<ResearchQItem>();
            foreach (string uid in EmpireManager.Player.data.ResearchQueue)
            {
                var researchItem = (TreeNode)Screen.AllTechNodes[uid];
                items.Add(CreateQItem(researchItem));
            }
            QSL.SetItems(items);
        }
	}
}