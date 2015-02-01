using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Encounter: IDisposable
	{
		public int Step;

		public string Name;

		public string Faction;

		public string DescriptionText;

		public List<Message> MessageList;

		public int CurrentMessage;

		private Rectangle MainRect;

		private Rectangle ResponseRect;

		private Rectangle TopRect;

		private Rectangle BlackRect;

		private ScrollList ResponseSL;

		private Empire playerEmpire;

		private SolarSystem sysToDiscuss;

		private Empire empToDiscuss;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;


		public Encounter()
		{
		}

		private bool CheckIfWeCanMeetDemand()
		{
			if ((float)this.MessageList[this.CurrentMessage].MoneyDemanded > this.playerEmpire.Money)
			{
				return false;
			}
			return true;
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			Primitives2D.FillRectangle(ScreenManager.SpriteBatch, this.BlackRect, Color.Black);
			Primitives2D.FillRectangle(ScreenManager.SpriteBatch, this.ResponseRect, Color.Black);
			Vector2 TheirTextPos = new Vector2((float)(this.BlackRect.X + 10), (float)(this.BlackRect.Y + 10));
			string theirText = this.parseText(this.MessageList[this.CurrentMessage].text, (float)(this.BlackRect.Width - 20), Fonts.Verdana12Bold);
			TheirTextPos.X = (float)((int)TheirTextPos.X);
			TheirTextPos.Y = (float)((int)TheirTextPos.Y);
			ScreenManager.SpriteBatch.DrawString(Fonts.Verdana12Bold, theirText, TheirTextPos, Color.White);
			if (this.MessageList[this.CurrentMessage].EndTransmission)
			{
				Vector2 ResponsePos = new Vector2((float)(this.ResponseRect.X + 10), (float)(this.ResponseRect.Y + 10));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Escape or Right Click to End Transmission:", ResponsePos, Color.White);
			}
			else
			{
				this.ResponseSL.Draw(ScreenManager.SpriteBatch);
				Vector2 drawCurs = new Vector2((float)(this.ResponseRect.X + 10), (float)(this.ResponseRect.Y + 10));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Your Response:", drawCurs, Color.White);
				drawCurs.X = drawCurs.X + 10f;
				for (int i = this.ResponseSL.indexAtTop; i < this.ResponseSL.Entries.Count; i++)
				{
					if (i >= this.ResponseSL.indexAtTop + this.ResponseSL.entriesToDisplay)
					{
						return;
					}
					ScrollList.Entry e = this.ResponseSL.Entries[i];
					drawCurs.Y = (float)e.clickRect.Y;
					drawCurs.X = (float)((int)drawCurs.X);
					SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
					SpriteFont arial12Bold = Fonts.Arial12Bold;
					int num = i + 1;
					spriteBatch.DrawString(arial12Bold, string.Concat(num.ToString(), ". ", (e.item as Response).Text), drawCurs, (e.clickRectHover == 0 ? Color.LightGray : Color.White));
				}
			}
		}

		public void HandleInput(InputState input, GameScreen caller)
		{
			for (int i = this.ResponseSL.indexAtTop; i < this.ResponseSL.Entries.Count && i < this.ResponseSL.indexAtTop + this.ResponseSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.ResponseSL.Entries[i];
				if (!HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
				{
					e.clickRectHover = 0;
				}
				else
				{
					e.clickRectHover = 1;
					if (input.InGameSelect)
					{
						Response r = e.item as Response;
						if (r.DefaultIndex != -1)
						{
							this.CurrentMessage = r.DefaultIndex;
						}
						else
						{
							bool OK = true;
							if (r.MoneyToThem > 0 && this.playerEmpire.Money < (float)r.MoneyToThem)
							{
								OK = false;
							}
							if (r.RequiredTech != null && !this.playerEmpire.GetTDict()[r.RequiredTech].Unlocked)
							{
								OK = false;
							}
							if (r.FailIfNotAlluring && (double)this.playerEmpire.data.Traits.DiplomacyMod < 0.2)
							{
								OK = false;
							}
							if (!OK)
							{
								this.CurrentMessage = r.FailIndex;
							}
							else
							{
								this.CurrentMessage = r.SuccessIndex;
								if (r.MoneyToThem > 0 && this.playerEmpire.Money >= (float)r.MoneyToThem)
								{
									Empire money = this.playerEmpire;
									money.Money = money.Money - (float)r.MoneyToThem;
								}
							}
						}
						if (this.MessageList[this.CurrentMessage].SetWar)
						{
							this.empToDiscuss.GetGSAI().DeclareWarFromEvent(this.playerEmpire, WarType.SkirmishWar);
						}
						if (this.MessageList[this.CurrentMessage].EndWar)
						{
							this.empToDiscuss.GetGSAI().EndWarFromEvent(this.playerEmpire);
						}
						this.playerEmpire.GetRelations()[this.empToDiscuss].EncounterStep = this.MessageList[this.CurrentMessage].SetEncounterStep;
						this.SetResponses();
					}
				}
			}
			if (this.MessageList[this.CurrentMessage].EndTransmission && (input.Escaped || input.CurrentMouseState.RightButton == ButtonState.Released && input.LastMouseState.RightButton == ButtonState.Pressed))
			{
				caller.ExitScreen();
			}
		}

		public void LoadContent(Ship_Game.ScreenManager ScreenManager, Rectangle FitRect)
		{
			this.MainRect = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 300, 600, 600);
			this.TopRect = new Rectangle(this.MainRect.X, this.MainRect.Y, this.MainRect.Width, 28);
			this.BlackRect = new Rectangle(FitRect.X, FitRect.Y, FitRect.Width, 240);
			this.ResponseRect = new Rectangle(FitRect.X, this.BlackRect.Y + this.BlackRect.Height + 10, FitRect.Width, 180);
			Submenu resp = new Submenu(ScreenManager, this.ResponseRect);
			this.ResponseSL = new ScrollList(resp, 20);
			this.SetResponses();
		}

		private string parseText(string text, float Width, SpriteFont font)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] wordArray = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)wordArray.Length; i++)
			{
				if (wordArray[i] == "SING")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Singular;
				}
				else if (wordArray[i] == "SING.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, ".");
				}
				else if (wordArray[i] == "SING,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, ",");
				}
				else if (wordArray[i] == "SING?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, "?");
				}
				else if (wordArray[i] == "SING!")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, "!");
				}
				if (wordArray[i] == "PLURAL")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Plural;
				}
				else if (wordArray[i] == "PLURAL.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, ".");
				}
				else if (wordArray[i] == "PLURAL,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, ",");
				}
				else if (wordArray[i] == "PLURAL?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, "?");
				}
				else if (wordArray[i] == "PLURAL!")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, "!");
				}
				if (wordArray[i] == "TARSYS")
				{
					wordArray[i] = this.sysToDiscuss.Name;
				}
				else if (wordArray[i] == "TARSYS.")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, ".");
				}
				else if (wordArray[i] == "TARSYS,")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, ",");
				}
				else if (wordArray[i] == "TARSYS?")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, "?");
				}
				else if (wordArray[i] == "TARSYS!")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, "!");
				}
				if (wordArray[i] == "TAREMP")
				{
					wordArray[i] = this.empToDiscuss.data.Traits.Name;
				}
				else if (wordArray[i] == "TAREMP.")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ".");
				}
				else if (wordArray[i] == "TAREMP,")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ",");
				}
				else if (wordArray[i] == "TAREMP?")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, "?");
				}
				else if (wordArray[i] == "TAREMP!")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, "!");
				}
				if (wordArray[i] == "ADJ1")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Adj1;
				}
				else if (wordArray[i] == "ADJ1.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj1, ".");
				}
				else if (wordArray[i] == "ADJ1,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj1, ",");
				}
				else if (wordArray[i] == "ADJ1?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj1, "?");
				}
				else if (wordArray[i] == "ADJ1!")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj1, "!");
				}
				if (wordArray[i] == "ADJ2")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Adj2;
				}
				else if (wordArray[i] == "ADJ2.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj2, ".");
				}
				else if (wordArray[i] == "ADJ2,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj2, ",");
				}
				else if (wordArray[i] == "ADJ2?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj2, "?");
				}
				else if (wordArray[i] == "ADJ2!")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Adj2, "!");
				}
			}
			string[] strArrays = wordArray;
			for (int j = 0; j < (int)strArrays.Length; j++)
			{
				string word = strArrays[j];
				if (font.MeasureString(string.Concat(line, word)).Length() > Width)
				{
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				line = string.Concat(line, word, ' ');
			}
			return string.Concat(returnString, line);
		}

		public void SetPlayerEmpire(Empire e)
		{
			this.playerEmpire = e;
		}

		private void SetResponses()
		{
			this.ResponseSL.Entries.Clear();
			this.ResponseSL.indexAtTop = 0;
			foreach (Response r in this.MessageList[this.CurrentMessage].ResponseOptions)
			{
				this.ResponseSL.AddItem(r);
			}
		}

		public void SetSys(SolarSystem s)
		{
			this.sysToDiscuss = s;
		}

		public void SetTarEmp(Empire e)
		{
			this.empToDiscuss = e;
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
                           if (this.ResponseSL != null)
                               this.ResponseSL.Dispose();
                       }
                       this.ResponseSL = null;
                       this.disposed = true;
                   }
               }
	}
}