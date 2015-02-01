using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class QueueComponent: IDisposable
	{
		private Rectangle Current;

		private Rectangle Queue;

		public ScrollList QSL;

		private Ship_Game.ScreenManager ScreenManager;

		private Submenu qsub;

		public Submenu csub;

		private ResearchScreenNew screen;

		public bool Visible = true;

		private Rectangle TimeLeft;

		public Rectangle container;

		private DanButton ShowQueue;

		public ResearchQItem CurrentResearch;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;


		public QueueComponent(Ship_Game.ScreenManager ScreenManager, Rectangle container, ResearchScreenNew screen)
		{
			this.container = container;
			this.screen = screen;
			this.ScreenManager = ScreenManager;
			this.Current = new Rectangle(container.X, container.Y, container.Width, 150);
			this.ShowQueue = new DanButton(new Vector2((float)(container.X + container.Width - 192), (float)(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 55)), Localizer.Token(2136));
			this.TimeLeft = new Rectangle(this.Current.X + this.Current.Width - 119, this.Current.Y + this.Current.Height - 24, 111, 20);
			this.csub = new Submenu(true, ScreenManager, this.Current);
			this.csub.AddTab(Localizer.Token(1405));
			this.Queue = new Rectangle(this.Current.X, this.Current.Y + 165, container.Width, container.Height - 165);
			this.qsub = new Submenu(true, ScreenManager, this.Queue);
			this.qsub.AddTab(Localizer.Token(1404));
			this.QSL = new ScrollList(this.qsub, 125);
		}

		public void AddToQueue(TreeNode researchItem)
		{
			if (this.CurrentResearch == null)
			{
				EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic = researchItem.tech.UID;
				this.CurrentResearch = new ResearchQItem(new Vector2((float)(this.csub.Menu.X + 5), (float)(this.csub.Menu.Y + 30)), researchItem, this.screen);
				return;
			}
			if (!EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Contains(researchItem.tech.UID) && EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic != researchItem.tech.UID)
			{
				EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Add(researchItem.tech.UID);
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
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, this.container, Color.Black);
			this.QSL.DrawBlue(this.ScreenManager.SpriteBatch);
			this.csub.Draw();
			if (this.CurrentResearch != null)
			{
				this.CurrentResearch.Draw(this.ScreenManager);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/timeleft"], this.TimeLeft, Color.White);
				Vector2 Cursor = new Vector2((float)(this.TimeLeft.X + this.TimeLeft.Width - 7), (float)(this.TimeLeft.Y + this.TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2 - 2));
				float cost = ResourceManager.TechTree[this.CurrentResearch.Node.tech.UID].Cost * UniverseScreen.GamePaceStatic - EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).GetTDict()[EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic].Progress;
				int numTurns = (int)(cost / (0.01f + EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).GetProjectedResearchNextTurn()));
				if (cost % (float)numTurns != 0f)
				{
					numTurns++;
				}
				string text = string.Concat(numTurns, " turns");
				if (numTurns > 999)
				{
					text = ">999 turns";
				}
				Cursor.X = Cursor.X - Fonts.Verdana14Bold.MeasureString(text).X;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Verdana14Bold, text, Cursor, new Color(205, 229, 255));
			}
			Vector2 vector2 = new Vector2((float)(this.qsub.Menu.X + 10), (float)(this.qsub.Menu.Y + 10));
			for (int i = this.QSL.indexAtTop; i < this.QSL.Entries.Count && i < this.QSL.indexAtTop + this.QSL.entriesToDisplay; i++)
			{
				Rectangle r = this.QSL.Copied[i].clickRect;
				(this.QSL.Copied[i].item as ResearchQItem).Draw(r, this.ScreenManager);
			}
			this.qsub.Draw();
		}

		public void HandleInput(InputState input)
		{
			if (input.RightMouseClick && this.Visible && HelperFunctions.CheckIntersection(this.Queue, input.CursorPosition) || input.Escaped)
			{
				this.screen.ExitScreen();
				return;
			}
			this.screen.qcomponent.QSL.Update();
			this.screen.qcomponent.QSL.HandleInput(input);
			if (this.Visible)
			{
				int i = this.QSL.indexAtTop;
				while (i < this.QSL.Entries.Count && i < this.QSL.indexAtTop + this.QSL.entriesToDisplay)
				{
					if (!(this.QSL.Copied[i].item as ResearchQItem).HandleInput(input))
					{
						i++;
					}
					else
					{
						AudioManager.PlayCue("sd_ui_research_select");
						break;
					}
				}
				if (this.CurrentResearch != null && this.CurrentResearch.HandleInput(input))
				{
					AudioManager.PlayCue("sd_ui_research_select");
				}
				if (this.ShowQueue.HandleInput(input))
				{
					this.ShowQueue = new DanButton(this.ShowQueue.Pos, Localizer.Token(2135));
					this.Visible = false;
					return;
				}
			}
			else if (this.ShowQueue.HandleInput(input))
			{
				this.ShowQueue = new DanButton(this.ShowQueue.Pos, Localizer.Token(2136));
				this.Visible = true;
			}
		}

		public void LoadQueue(TreeNode researchItem)
		{
			if (this.CurrentResearch == null)
			{
				this.CurrentResearch = new ResearchQItem(new Vector2((float)(this.csub.Menu.X + 5), (float)(this.csub.Menu.Y + 30)), researchItem, this.screen);
				return;
			}
			ResearchQItem qi = new ResearchQItem(new Vector2((float)(this.csub.Menu.X + 5), (float)(this.csub.Menu.Y + 30)), researchItem, this.screen);
			this.QSL.AddItem(qi);
		}

		public void SetInvisible()
		{
			this.ShowQueue = new DanButton(this.ShowQueue.Pos, Localizer.Token(2135));
			this.Visible = false;
		}

		public void SetVisible()
		{
			this.ShowQueue = new DanButton(this.ShowQueue.Pos, Localizer.Token(2136));
			this.Visible = true;
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.QSL != null)
                        this.QSL.Dispose();
            
                }
                this.QSL = null;
                this.disposed = true;
            }
        }
	}
}