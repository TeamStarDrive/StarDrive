using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class ResearchQueueUIComponent
	{
        Rectangle Current;
        Rectangle Queue;
		public ScrollList QSL;
        ScreenManager ScreenManager;
        Submenu qsub;
		public Submenu csub;
        ResearchScreenNew screen;
		public bool Visible = true;
        Rectangle TimeLeft;
		public Rectangle container;
        DanButton ShowQueue;


		public ResearchQueueUIComponent(ScreenManager ScreenManager, Rectangle container, ResearchScreenNew screen)
		{
			this.container = container;
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			Current = new Rectangle(container.X, container.Y, container.Width, 150);
			ShowQueue = new DanButton(new Vector2(container.X + container.Width - 192, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 55), Localizer.Token(2136));
			TimeLeft = new Rectangle(Current.X + Current.Width - 119, Current.Y + Current.Height - 24, 111, 20);
			csub = new Submenu(true, Current);
			csub.AddTab(Localizer.Token(1405));
			Queue = new Rectangle(Current.X, Current.Y + 165, container.Width, container.Height - 165);
			qsub = new Submenu(true, Queue);
			qsub.AddTab(Localizer.Token(1404));
			QSL = new ScrollList(qsub, 125);
		}

        public void Draw(SpriteBatch batch)
		{
			if (!Visible)
			{
				ShowQueue.DrawBlue(batch);
				return;
			}

			ShowQueue.DrawBlue(batch);
			batch.FillRectangle(container, Color.Black);
			QSL.DrawBlue(batch);
			csub.Draw(batch);

            ScrollList.Entry[] entries = QSL.VisibleExpandedEntries;

			if (entries.Length > 0)
			{
                var currentResearch = entries[0].Get<ResearchQItem>();
                TechEntry tech = currentResearch.Node.Entry;
                currentResearch.Draw(batch);
				batch.Draw(ResourceManager.Texture("ResearchMenu/timeleft"), TimeLeft, Color.White);
				var cursor = new Vector2(TimeLeft.X + TimeLeft.Width - 7, TimeLeft.Y + TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2 - 2);
				float cost = tech.TechCost - tech.Progress;
				int numTurns = (int)(cost / (0.01f + EmpireManager.Player.GetProjectedResearchNextTurn()));
				if (cost % numTurns != 0f)
				{
					numTurns++;
				}
				string text = string.Concat(numTurns, " turns");
				if (numTurns > 999)
				{
					text = ">999 turns";
				}
				cursor.X -= Fonts.Verdana14Bold.MeasureString(text).X;
				batch.DrawString(Fonts.Verdana14Bold, text, cursor, new Color(205, 229, 255));
			}

            for (int i = 1; i < entries.Length; ++i)
            {
                ScrollList.Entry e = entries[i];
                e.Get<ResearchQItem>().Draw(batch, e.Rect);
            }

			qsub.Draw(batch);
		}

		public void HandleInput(InputState input)
		{
			if ((Visible && input.RightMouseClick && Queue.HitTest(input.CursorPosition))
                || input.Escaped)
			{
				screen.ExitScreen();
				return;
			}

			screen.qcomponent.QSL.HandleInput(input);
			if (Visible)
			{
                foreach (ScrollList.Entry e in QSL.VisibleExpandedEntries)
                {
                    if (e.Get<ResearchQItem>().HandleInput(input))
                    {
                        GameAudio.ResearchSelect();
                        break;
                    }
                }

				if (ShowQueue.HandleInput(input))
				{
					ShowQueue = new DanButton(ShowQueue.Pos, Localizer.Token(2135));
					Visible = false;
				}
			}
			else if (ShowQueue.HandleInput(input))
			{
				ShowQueue = new DanButton(ShowQueue.Pos, Localizer.Token(2136));
				Visible = true;
			}
		}

        ResearchQItem CreateQItem(TreeNode researchItem)
        {
            return new ResearchQItem(new Vector2(csub.Menu.X + 5, csub.Menu.Y + 30), researchItem, screen);
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
                var researchItem = (TreeNode)screen.AllTechNodes[uid];
                items.Add(CreateQItem(researchItem));
            }
            QSL.SetItems(items);
        }

		public void SetInvisible()
		{
			ShowQueue = new DanButton(ShowQueue.Pos, Localizer.Token(2135));
			Visible = false;
		}

		public void SetVisible()
		{
			ShowQueue = new DanButton(ShowQueue.Pos, Localizer.Token(2136));
			Visible = true;
		}
	}
}