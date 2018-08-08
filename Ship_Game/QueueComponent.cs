using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class QueueComponent
	{
		private Rectangle Current;
		private Rectangle Queue;
		public ScrollList QSL;
		private ScreenManager ScreenManager;
		private Submenu qsub;
		public Submenu csub;
		private ResearchScreenNew screen;
		public bool Visible = true;
		private Rectangle TimeLeft;
		public Rectangle container;
		private DanButton ShowQueue;
		public ResearchQItem CurrentResearch;


		public QueueComponent(Ship_Game.ScreenManager ScreenManager, Rectangle container, ResearchScreenNew screen)
		{
			this.container = container;
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			this.Current = new Rectangle(container.X, container.Y, container.Width, 150);
			this.ShowQueue = new DanButton(new Vector2((float)(container.X + container.Width - 192), (float)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 55)), Localizer.Token(2136));
			this.TimeLeft = new Rectangle(this.Current.X + this.Current.Width - 119, this.Current.Y + this.Current.Height - 24, 111, 20);
			this.csub = new Submenu(true, this.Current);
			this.csub.AddTab(Localizer.Token(1405));
			this.Queue = new Rectangle(this.Current.X, this.Current.Y + 165, container.Width, container.Height - 165);
			this.qsub = new Submenu(true, this.Queue);
			this.qsub.AddTab(Localizer.Token(1404));
			this.QSL = new ScrollList(this.qsub, 125);
		}

		public void AddToQueue(TreeNode researchItem)
		{
			if (this.CurrentResearch == null)
			{
				EmpireManager.Player.ResearchTopic = researchItem.tech.UID;
				this.CurrentResearch = new ResearchQItem(new Vector2((float)(this.csub.Menu.X + 5), (float)(this.csub.Menu.Y + 30)), researchItem, this.screen);
				return;
			}
			if (!EmpireManager.Player.data.ResearchQueue.Contains(researchItem.tech.UID) && EmpireManager.Player.ResearchTopic != researchItem.tech.UID)
			{
				EmpireManager.Player.data.ResearchQueue.Add(researchItem.tech.UID);
				ResearchQItem qi = new ResearchQItem(new Vector2((float)(this.csub.Menu.X + 5), (float)(this.csub.Menu.Y + 30)), researchItem, this.screen);
				this.QSL.AddItem(qi);
			}
		}

		public void Draw()
		{
			if (!this.Visible)
			{
				this.ShowQueue.DrawBlue(this.ScreenManager);
				return;
			}
			this.ShowQueue.DrawBlue(this.ScreenManager);
			this.ScreenManager.SpriteBatch.FillRectangle(this.container, Color.Black);
			this.QSL.DrawBlue(this.ScreenManager.SpriteBatch);
			this.csub.Draw();
            var tech = CurrentResearch?.Node?.tech;
            var complete = tech == null || CurrentResearch.Node.complete == true || tech.TechCost == tech.Progress;
			if ( !complete)
			{
				this.CurrentResearch.Draw(this.ScreenManager);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/timeleft"], this.TimeLeft, Color.White);
				Vector2 Cursor = new Vector2((float)(this.TimeLeft.X + this.TimeLeft.Width - 7), (float)(this.TimeLeft.Y + this.TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2 - 2));
				float cost = tech.TechCost - tech.Progress;
				int numTurns = (int)(cost / (0.01f + EmpireManager.Player.GetProjectedResearchNextTurn()));
				if (cost % (float)numTurns != 0f)
				{
					numTurns++;
				}
				string text = string.Concat(numTurns, " turns");
				if (numTurns > 999)
				{
					text = ">999 turns";
				}
				Cursor.X -= Fonts.Verdana14Bold.MeasureString(text).X;
				ScreenManager.SpriteBatch.DrawString(Fonts.Verdana14Bold, text, Cursor, new Color(205, 229, 255));
			}
            foreach (ScrollList.Entry e in QSL.VisibleExpandedEntries)
			{
                e.Get<ResearchQItem>().Draw(ScreenManager, e.Rect);
			}
			qsub.Draw();
		}

		public void HandleInput(InputState input)
		{
			if (input.RightMouseClick && Visible && Queue.HitTest(input.CursorPosition) || input.Escaped)
			{
				screen.ExitScreen();
				return;
			}
			screen.qcomponent.QSL.HandleInput(input);
			if (Visible)
			{
                foreach (ScrollList.Entry e in QSL.VisibleExpandedEntries)
                {
                    if (((ResearchQItem)e.item).HandleInput(input))
                    {
                        GameAudio.PlaySfxAsync("sd_ui_research_select");
                        break;
                    }
                }
				if (CurrentResearch != null && CurrentResearch.HandleInput(input))
				{
					GameAudio.PlaySfxAsync("sd_ui_research_select");
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

        private ResearchQItem CreateQItem(TreeNode researchItem)
        {
            return new ResearchQItem(new Vector2(csub.Menu.X + 5, csub.Menu.Y + 30), researchItem, screen);
        }

		public void LoadQueue(TreeNode researchItem)
		{
			if (CurrentResearch == null)
			{
				CurrentResearch = CreateQItem(researchItem);
				return;
			}
			QSL.AddItem(CreateQItem(researchItem));
		}

        public void ReloadResearchQueue()
        {
            var items = new Array<ResearchQItem>();
            if (EmpireManager.Player.ResearchTopic.NotEmpty())
            {
                var researchItem = (TreeNode)screen.CompleteSubNodeTree[EmpireManager.Player.ResearchTopic];
                CurrentResearch = CreateQItem(researchItem);
            }
            foreach (string uid in EmpireManager.Player.data.ResearchQueue)
            {
                var researchItem = (TreeNode)screen.CompleteSubNodeTree[uid];
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