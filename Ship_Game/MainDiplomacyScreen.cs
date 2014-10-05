using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public class MainDiplomacyScreen : GameScreen, IDisposable
	{
		private UniverseScreen screen;

		public DanButton Contact;

		private Menu2 TitleBar;

		private Vector2 TitlePos;

		private Menu2 DMenu;

		public bool LowRes;

		public Rectangle SelectedInfoRect;

		public Rectangle IntelligenceRect;

		public Rectangle OperationsRect;

		public Empire SelectedEmpire;

		private List<RaceEntry> Races = new List<RaceEntry>();

		//private ProgressBar Penetration;

		private Rectangle ArtifactsRect;

		private ScrollList ArtifactsSL;

		private CloseButton close;

		private float TransitionElapsedTime;

		//private bool showExecuteButton;

		//private string fmt = "0.#";

		//private Rectangle PenRect;

		public MainDiplomacyScreen(UniverseScreen screen)
		{
			this.screen = screen;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 766)
			{
				this.TitleBar.Draw();
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(1600), this.TitlePos, new Color(255, 239, 208));
			}
			this.DMenu.Draw();
			Color color = new Color(118, 102, 67, 50);
			foreach (RaceEntry race in this.Races)
			{
				if (race.e.isFaction || race.e.MinorRace)
				{
					continue;
				}
				Vector2 NameCursor = new Vector2((float)(race.container.X + 62) - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, (float)(race.container.Y + 148 + 8));
				if (race.e.data.Defeated)
				{
					if (race.e.data.AbsorbedBy == null)
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/x_red"], race.container, Color.White);
					}
					else
					{
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
						Rectangle r = new Rectangle(race.container.X, race.container.Y, 124, 124);
						SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
						KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).data.Traits.FlagIndex];
						spriteBatch.Draw(item.Value, r, EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy).EmpireColor);
					}
				}
				else if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) != race.e && EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[race.e].Known)
				{
					if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[race.e].AtWar && !race.e.data.Defeated)
					{
						Rectangle war = new Rectangle(race.container.X - 2, race.container.Y - 2, race.container.Width + 4, race.container.Height + 4);
						Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, war, Color.Red);
					}
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
				}
				else if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) != race.e)
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/unknown"], race.container, Color.White);
				}
				else
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", race.e.data.PortraitName)], race.container, Color.White);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Portraits/portrait_shine"], race.container, Color.White);
					NameCursor = new Vector2((float)(race.container.X + 62) - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, (float)(race.container.Y + 148 + 8));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
				}
				if (race.e != this.SelectedEmpire)
				{
					continue;
				}
				Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, race.container, Color.Orange);
			}
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.SelectedInfoRect, new Color(23, 20, 14));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.IntelligenceRect, new Color(23, 20, 14));
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.OperationsRect, new Color(23, 20, 14));
			Vector2 TextCursor = new Vector2((float)(this.SelectedInfoRect.X + 20), (float)(this.SelectedInfoRect.Y + 10));
			HelperFunctions.DrawDropShadowText(base.ScreenManager, this.SelectedEmpire.data.Traits.Name, TextCursor, Fonts.Arial20Bold);
			Rectangle FlagRect = new Rectangle(this.SelectedInfoRect.X + this.SelectedInfoRect.Width - 60, this.SelectedInfoRect.Y + 10, 40, 40);
			SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
			KeyValuePair<string, Texture2D> keyValuePair = ResourceManager.FlagTextures[this.SelectedEmpire.data.Traits.FlagIndex];
			spriteBatch1.Draw(keyValuePair.Value, FlagRect, this.SelectedEmpire.EmpireColor);
			TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 4);
			if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) == this.SelectedEmpire && !this.SelectedEmpire.data.Defeated)
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1601), TextCursor, Color.White);
				Vector2 ColumnBCursor = TextCursor;
				ColumnBCursor.X = ColumnBCursor.X + 190f;
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				List<Empire> Sortlist = new List<Empire>();
				foreach (Empire e in EmpireManager.EmpireList)
				{
					if (e.isFaction || e.data.Defeated || e.MinorRace)
					{
						if (this.SelectedEmpire != e)
						{
							continue;
						}
						Sortlist.Add(e);
					}
					else if (e != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
					{
						if (!EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetRelations()[e].Known)
						{
							continue;
						}
						Sortlist.Add(e);
					}
					else
					{
						Sortlist.Add(e);
					}
				}
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1613), TextCursor, Color.White);
				IOrderedEnumerable<Empire> MoneySortedList = 
					from empire in Sortlist
					orderby empire.Money + empire.GetAverageNetIncome() descending
					select empire;
				int rank = 1;
				foreach (Empire e in MoneySortedList)
				{
					if (e == this.SelectedEmpire)
					{
						break;
					}
					rank++;
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				IOrderedEnumerable<Empire> ResSortedList = 
					from empire in Sortlist
					orderby this.GetScientificStr(empire) descending
					select empire;
				rank = 1;
				foreach (Empire e in ResSortedList)
				{
					if (e == this.SelectedEmpire)
					{
						break;
					}
					rank++;
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1602), TextCursor, Color.White);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				IOrderedEnumerable<Empire> MilSorted = 
					from empire in Sortlist
					orderby this.GetMilitaryStr(empire) descending
					select empire;
				rank = 1;
				foreach (Empire e in MilSorted)
				{
					if (e == this.SelectedEmpire)
					{
						break;
					}
					rank++;
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1605), TextCursor, Color.White);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				IOrderedEnumerable<Empire> PopSortedList = 
					from empire in Sortlist
					orderby this.GetPop(empire) descending
					select empire;
				rank = 1;
				foreach (Empire e in PopSortedList)
				{
					if (e == this.SelectedEmpire)
					{
						break;
					}
					rank++;
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(385), TextCursor, Color.White);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				Rectangle ArtifactsRect = new Rectangle(this.SelectedInfoRect.X + 20, this.SelectedInfoRect.Y + 210, this.SelectedInfoRect.Width - 40, 130);
				Vector2 ArtifactsCursor = new Vector2((float)ArtifactsRect.X, (float)(ArtifactsRect.Y - 8));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1607), ArtifactsCursor, Color.White);
				ArtifactsCursor.Y = ArtifactsCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ArtifactsSL.Draw(base.ScreenManager.SpriteBatch);
				int i = this.ArtifactsSL.indexAtTop;
				while (i < this.ArtifactsSL.Entries.Count)
				{
					if (i < this.ArtifactsSL.indexAtTop + this.ArtifactsSL.entriesToDisplay)
					{
						ScrollList.Entry e = this.ArtifactsSL.Entries[i];
						ArtifactsCursor.Y = (float)e.clickRect.Y;
						ArtifactEntry art = e.item as ArtifactEntry;
						art.Update(ArtifactsCursor);
						foreach (SkinnableButton button in art.ArtifactButtons)
						{
							button.Draw(base.ScreenManager);
						}
						i++;
					}
					else
					{
						break;
					}
				}
			}
			else if (this.SelectedEmpire.data.Defeated)
			{
				if (this.SelectedEmpire.data.AbsorbedBy != null)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(EmpireManager.GetEmpireByName(this.SelectedEmpire.data.AbsorbedBy).data.Traits.Singular, " Federation"), TextCursor, Color.White);
					TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
			}
			else if (!this.SelectedEmpire.data.Defeated)
			{
				float intelligencePenetration = EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].IntelligencePenetration;
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(this.SelectedEmpire.data.DiplomaticPersonality.Name, " ", this.SelectedEmpire.data.EconomicPersonality.Name), TextCursor, Color.White);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].AtWar)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1608), TextCursor, Color.LightPink);
				}
				else if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].Treaty_Peace)
				{
					SpriteBatch spriteBatch2 = base.ScreenManager.SpriteBatch;
					SpriteFont arial12Bold = Fonts.Arial12Bold;
					object[] objArray = new object[] { Localizer.Token(1213), " (", EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].PeaceTurnsRemaining, " ", Localizer.Token(2200), ")" };
					spriteBatch2.DrawString(arial12Bold, string.Concat(objArray), TextCursor, Color.LightGreen);
					TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
				if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].Treaty_OpenBorders)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1609), TextCursor, Color.LightGreen);
					TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
				if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].Treaty_Trade)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1610), TextCursor, Color.LightGreen);
					TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
				if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].Treaty_NAPact)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1611), TextCursor, Color.LightGreen);
					TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
				if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[this.SelectedEmpire].Treaty_Alliance)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1612), TextCursor, Color.LightGreen);
				}
				Rectangle ArtifactsRect = new Rectangle(this.SelectedInfoRect.X + 20, this.SelectedInfoRect.Y + 210, this.SelectedInfoRect.Width - 40, 130);
				Vector2 ArtifactsCursor = new Vector2((float)ArtifactsRect.X, (float)(ArtifactsRect.Y - 8));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1607), ArtifactsCursor, Color.White);
				ArtifactsCursor.Y = ArtifactsCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				this.ArtifactsSL.Draw(base.ScreenManager.SpriteBatch);
				for (int i = this.ArtifactsSL.indexAtTop; i < this.ArtifactsSL.Entries.Count && i < this.ArtifactsSL.indexAtTop + this.ArtifactsSL.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.ArtifactsSL.Entries[i];
					ArtifactsCursor.Y = (float)e.clickRect.Y;
					ArtifactEntry art = e.item as ArtifactEntry;
					art.Update(ArtifactsCursor);
					foreach (SkinnableButton button in art.ArtifactButtons)
					{
						button.Draw(base.ScreenManager);
					}
				}
				List<Empire> Sortlist = new List<Empire>();
				foreach (Empire e in EmpireManager.EmpireList)
				{
					if (e.isFaction || e.data.Defeated || e.MinorRace)
					{
						if (this.SelectedEmpire != e)
						{
							continue;
						}
						Sortlist.Add(e);
					}
					else if (e != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
					{
						if (!EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetRelations()[e].Known)
						{
							continue;
						}
						Sortlist.Add(e);
					}
					else
					{
						Sortlist.Add(e);
					}
				}
				this.Contact.Draw(base.ScreenManager);
				Vector2 ColumnBCursor = TextCursor;
				ColumnBCursor.X = ColumnBCursor.X + 190f;
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1613), TextCursor, Color.White);
				IOrderedEnumerable<Empire> MoneySortedList = 
					from empire in Sortlist
					orderby empire.Money + empire.GetAverageNetIncome() descending
					select empire;
				int rank = 1;
				foreach (Empire e in MoneySortedList)
				{
					if (e == this.SelectedEmpire)
					{
						break;
					}
					rank++;
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				IOrderedEnumerable<Empire> ResSortedList = 
					from empire in Sortlist
					orderby this.GetScientificStr(empire) descending
					select empire;
				rank = 1;
				foreach (Empire e in ResSortedList)
				{
					if (e == this.SelectedEmpire)
					{
						break;
					}
					rank++;
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1602), TextCursor, Color.White);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
				ColumnBCursor.Y = ColumnBCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				IOrderedEnumerable<Empire> MilSorted = 
					from empire in Sortlist
					orderby this.GetMilitaryStr(empire) descending
					select empire;
				rank = 1;
				foreach (Empire e in MilSorted)
				{
					if (e == this.SelectedEmpire)
					{
						break;
					}
					rank++;
				}
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1605), TextCursor, Color.White);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
			}
			TextCursor = new Vector2((float)(this.IntelligenceRect.X + 20), (float)(this.IntelligenceRect.Y + 10));
            HelperFunctions.DrawDropShadowText(base.ScreenManager, Localizer.Token(6091), TextCursor, Fonts.Arial20Bold);
			TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6094), this.SelectedEmpire.data.Traits.HomeworldName), TextCursor, Color.White);
            TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            //Added by McShooterz:  intel report
            if (this.SelectedEmpire.Capital != null)
            {
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6106), (this.SelectedEmpire.Capital.Owner == this.SelectedEmpire) ? Localizer.Token(6107) : Localizer.Token(1508)), TextCursor, Color.White);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            }
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6095), this.SelectedEmpire.GetPlanets().Count), TextCursor, Color.White);
            TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6096), this.SelectedEmpire.GetShips().Count), TextCursor, Color.White);
            TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6097), this.SelectedEmpire.Money.ToString("0.0")), TextCursor, Color.White);
            TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6098), this.SelectedEmpire.totalMaint.ToString("0.0")), TextCursor, Color.White);
            TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            if (this.SelectedEmpire.ResearchTopic != "")
            {
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat("Researching: ", Localizer.Token( ResourceManager.TechTree[this.SelectedEmpire.ResearchTopic].NameIndex)), TextCursor, Color.White);
                TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            }
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6099), this.SelectedEmpire.data.AgentList.Count), TextCursor, Color.White);
            TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6100), this.SelectedEmpire.GetPopulation().ToString("0.0"), Localizer.Token(6101)), TextCursor, Color.White);
            //Diplomatic Relations
            foreach (KeyValuePair<Empire, Relationship> Relation in this.SelectedEmpire.GetRelations())
            {
                if (!Relation.Value.Known || Relation.Key.isFaction)
                    continue;
                if (Relation.Key.data.Defeated)
                {
                    TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, Localizer.Token(6102)), TextCursor, Color.White);
                    continue;
                }
                if (Relation.Value.Treaty_Alliance)
                {
                    TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1612), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), TextCursor, Color.White);
                }
                else if (Relation.Value.Treaty_OpenBorders)
                {
                    TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1609), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), TextCursor, Color.White);
                }
                else if (Relation.Value.Treaty_NAPact)
                {
                    TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1611), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), TextCursor, Color.White);
                }
                else if (Relation.Value.Treaty_Peace)
                {
                    TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1213), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), TextCursor, Color.White);
                }
                else if (Relation.Value.AtWar)
                {
                    TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1608)), TextCursor, Color.White);
                }
                else
                {
                    TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial12.LineSpacing + 2);
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, (Relation.Value.Treaty_Trade) ? Localizer.Token(6104) : Localizer.Token(6105)), TextCursor, Color.White);
                }
            }
            //End of intel report
			TextCursor = new Vector2((float)(this.OperationsRect.X + 20), (float)(this.OperationsRect.Y + 10));
			HelperFunctions.DrawDropShadowText(base.ScreenManager, (this.SelectedEmpire == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) ? Localizer.Token(2181) : Localizer.Token(2212)), TextCursor, Fonts.Arial20Bold);
			TextCursor.Y = TextCursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 5);
            //Added by McShooterz: Only display modified bonuses
            if (this.SelectedEmpire.data.Traits.PopGrowthMax > 0f)
                this.DrawBadStat(Localizer.Token(4041), string.Concat("+", this.SelectedEmpire.data.Traits.PopGrowthMax.ToString(".##")), ref TextCursor);
            if (this.SelectedEmpire.data.Traits.PopGrowthMin > 0f)
                this.DrawGoodStat(Localizer.Token(4040), string.Concat("+", this.SelectedEmpire.data.Traits.PopGrowthMin.ToString(".##")), ref TextCursor);
            if (this.SelectedEmpire.data.Traits.ReproductionMod != 0)
                this.DrawStat(Localizer.Token(4017), this.SelectedEmpire.data.Traits.ReproductionMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.Traits.ConsumptionModifier != 0)
                this.DrawStat(Localizer.Token(6140), this.SelectedEmpire.data.Traits.ConsumptionModifier, ref TextCursor, true);
            if (this.SelectedEmpire.data.Traits.ProductionMod != 0)
                this.DrawStat(Localizer.Token(4018), this.SelectedEmpire.data.Traits.ProductionMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.Traits.ResearchMod != 0)
                this.DrawStat(Localizer.Token(4019), this.SelectedEmpire.data.Traits.ResearchMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.Traits.DiplomacyMod != 0)
                this.DrawStat(Localizer.Token(4020), this.SelectedEmpire.data.Traits.DiplomacyMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.Traits.GroundCombatModifier != 0)
                this.DrawStat(Localizer.Token(4021), this.SelectedEmpire.data.Traits.GroundCombatModifier, ref TextCursor, false);
            if (this.SelectedEmpire.data.Traits.ShipCostMod != 0)
                this.DrawStat(Localizer.Token(4022), this.SelectedEmpire.data.Traits.ShipCostMod, ref TextCursor, true);
            if (this.SelectedEmpire.data.Traits.ModHpModifier != 0)
                this.DrawStat(Localizer.Token(4023), this.SelectedEmpire.data.Traits.ModHpModifier, ref TextCursor, false);
            //Added by McShooterz: new races stats to display in diplomacy
            if (this.SelectedEmpire.data.Traits.RepairMod != 0)
                this.DrawStat(Localizer.Token(6012), this.SelectedEmpire.data.Traits.RepairMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.PowerFlowMod != 0)
                this.DrawStat(Localizer.Token(6014), this.SelectedEmpire.data.PowerFlowMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.ShieldPowerMod != 0)
                this.DrawStat(Localizer.Token(6141), this.SelectedEmpire.data.ShieldPowerMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.MassModifier != 1)
                this.DrawStat(Localizer.Token(4036), this.SelectedEmpire.data.MassModifier - 1f, ref TextCursor, true);
            if (this.SelectedEmpire.data.Traits.TaxMod != 0)
                this.DrawStat(Localizer.Token(4024), this.SelectedEmpire.data.Traits.TaxMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.Traits.MaintMod != 0)
                this.DrawStat(Localizer.Token(4037), this.SelectedEmpire.data.Traits.MaintMod, ref TextCursor, true);
            this.DrawStat(Localizer.Token(4025), this.SelectedEmpire.data.Traits.InBordersSpeedBonus, ref TextCursor, false);
            if (Ship.universeScreen.FTLModifier != 1f)
            {
                float fTLModifier = Ship.universeScreen.FTLModifier * 100f;
                this.DrawBadStat(Localizer.Token(4038), string.Concat(fTLModifier.ToString("##"), "%"), ref TextCursor);
            }
            this.DrawStat(Localizer.Token(4026), string.Concat(this.SelectedEmpire.data.FTLModifier, "x"), ref TextCursor);
            this.DrawStat(Localizer.Token(4027), string.Concat(this.SelectedEmpire.data.FTLPowerDrainModifier, "x"), ref TextCursor);
            if (this.SelectedEmpire.data.FuelCellModifier != 0)
                this.DrawStat(Localizer.Token(4039), this.SelectedEmpire.data.FuelCellModifier, ref TextCursor, false);
            if (this.SelectedEmpire.data.SubLightModifier != 1)
                this.DrawStat(Localizer.Token(4028), this.SelectedEmpire.data.SubLightModifier - 1f, ref TextCursor, false);
            if (this.SelectedEmpire.data.SensorModifier != 1)
                this.DrawStat(Localizer.Token(4029), this.SelectedEmpire.data.SensorModifier - 1f, ref TextCursor, false);
            if (this.SelectedEmpire.data.SpyModifier > 0f)
            {
                this.DrawGoodStat(Localizer.Token(4030), string.Concat("+", this.SelectedEmpire.data.SpyModifier.ToString("#")), ref TextCursor);
            }
            else if (this.SelectedEmpire.data.SpyModifier < 0f)
            {
                this.DrawBadStat(Localizer.Token(4030), string.Concat("-", this.SelectedEmpire.data.SpyModifier.ToString("#")), ref TextCursor);
            }
            if (this.SelectedEmpire.data.Traits.Spiritual != 0)
                this.DrawStat(Localizer.Token(4031), this.SelectedEmpire.data.Traits.Spiritual, ref TextCursor, false);
            if (this.SelectedEmpire.data.Traits.EnergyDamageMod != 0)
                this.DrawStat(Localizer.Token(4032), this.SelectedEmpire.data.Traits.EnergyDamageMod, ref TextCursor, false);
            if (this.SelectedEmpire.data.OrdnanceEffectivenessBonus != 0)
                this.DrawStat(Localizer.Token(4033), this.SelectedEmpire.data.OrdnanceEffectivenessBonus, ref TextCursor, false);
            if (this.SelectedEmpire.data.MissileHPModifier != 1)
                this.DrawStat(Localizer.Token(4034), this.SelectedEmpire.data.MissileHPModifier - 1f, ref TextCursor, false);
            if (this.SelectedEmpire.data.MissileDodgeChance != 0)
                this.DrawStat(Localizer.Token(4035), this.SelectedEmpire.data.MissileDodgeChance, ref TextCursor, false);
			this.close.Draw(base.ScreenManager);
			if (base.IsActive)
			{
				ToolTip.Draw(base.ScreenManager);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		private void DrawBadStat(string text, string text2, ref Vector2 Position)
		{
			HelperFunctions.ClampVectorToInt(ref Position);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.LightPink);
			Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.LightPink);
			Position.Y = Position.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
		}

		private void DrawGoodStat(string text, string text2, ref Vector2 Position)
		{
			HelperFunctions.ClampVectorToInt(ref Position);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.LightGreen);
			Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.LightGreen);
			Position.Y = Position.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
		}

        private void DrawStat(string text, float value, ref Vector2 Position, bool OppositeBonuses)
		{
			Color color;
			if (value <= 10f)
			{
				value = value * 100f;
			}
            if ((value > 0f && !OppositeBonuses) || (value < 0f && OppositeBonuses))
            {
                color = Color.LightGreen;
            }
            else
            {
                color = (value == 0f ? Color.White : Color.LightPink);
            }
			HelperFunctions.ClampVectorToInt(ref Position);
			SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
			SpriteFont arial12 = Fonts.Arial12;
			string str = text;
			Vector2 position = Position;
			spriteBatch.DrawString(arial12, str, position, color);
			Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(string.Concat(value.ToString("#.##"), "%")).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			string str1 = string.Concat(value.ToString("#.##"), "%");
			Vector2 vector2 = nPos;
			spriteBatch1.DrawString(arial12Bold, str1, vector2, color);
			Position.Y = Position.Y + (float)Fonts.Arial12Bold.LineSpacing;
		}

		private void DrawStat(string text, string text2, ref Vector2 Position)
		{
			HelperFunctions.ClampVectorToInt(ref Position);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.White);
            Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
			//{
				nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
			//};
			HelperFunctions.ClampVectorToInt(ref nPos);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.White);
			Position.Y = Position.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
		}

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~MainDiplomacyScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		private float GetMilitaryStr(Empire e)
		{
			float single;
			float str = 0f;
			try
			{
				foreach (Ship ship in e.GetShips())
				{
					str = str + ship.GetStrength();
				}
				return str;
			}
			catch
			{
				single = str;
			}
			return single;
		}

		private float GetPop(Empire e)
		{
			float pop = 0f;
			foreach (Planet p in e.GetPlanets())
			{
				pop = pop + p.Population;
			}
			return pop;
		}

		private float GetScientificStr(Empire e)
		{
			float scientificStr = 0f;
			foreach (KeyValuePair<string, TechEntry> Technology in e.GetTDict())
			{
				if (!Technology.Value.Unlocked)
				{
					continue;
				}
				scientificStr = scientificStr + ResourceManager.TechTree[Technology.Key].Cost;
			}
			return scientificStr;
		}

		public override void HandleInput(InputState input)
		{
			if (this.close.HandleInput(input))
			{
				this.ExitScreen();
				return;
			}
			//this.showExecuteButton = false;
			if (this.SelectedEmpire != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) && !this.SelectedEmpire.data.Defeated && this.Contact.HandleInput(input))
			{
				base.ScreenManager.AddScreen(new DiplomacyScreen(this.SelectedEmpire, EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty), "Greeting"));
			}
			foreach (RaceEntry race in this.Races)
			{
				if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) == race.e || !EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).GetRelations()[race.e].Known)
				{
					if (EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty) != race.e || !HelperFunctions.ClickedRect(race.container, input))
					{
						continue;
					}
					this.SelectedEmpire = race.e;
					this.ArtifactsSL.Entries.Clear();
					this.ArtifactsSL.indexAtTop = 0;
					ArtifactEntry entry = new ArtifactEntry();
					for (int i = 0; i < this.SelectedEmpire.data.OwnedArtifacts.Count; i++)
					{
						Artifact art = this.SelectedEmpire.data.OwnedArtifacts[i];
						SkinnableButton button = new SkinnableButton(new Rectangle(0, 0, 32, 32), string.Concat("Artifact Icons/", art.Name))
						{
							IsToggle = false,
							ReferenceObject = art,
							BaseColor = Color.White
						};
						if (entry.ArtifactButtons.Count < 5)
						{
							entry.ArtifactButtons.Add(button);
						}
						if (entry.ArtifactButtons.Count == 5 || i == this.SelectedEmpire.data.OwnedArtifacts.Count - 1)
						{
							this.ArtifactsSL.AddItem(entry);
							entry = new ArtifactEntry();
						}
					}
					AudioManager.PlayCue("echo_affirm");
				}
				else
				{
					if (!HelperFunctions.ClickedRect(race.container, input))
					{
						continue;
					}
					this.SelectedEmpire = race.e;
					this.ArtifactsSL.Entries.Clear();
					this.ArtifactsSL.indexAtTop = 0;
					ArtifactEntry entry = new ArtifactEntry();
					for (int i = 0; i < this.SelectedEmpire.data.OwnedArtifacts.Count; i++)
					{
						Artifact art = this.SelectedEmpire.data.OwnedArtifacts[i];
						SkinnableButton button = new SkinnableButton(new Rectangle(0, 0, 32, 32), string.Concat("Artifact Icons/", art.Name))
						{
							IsToggle = false,
							ReferenceObject = art,
							BaseColor = Color.White
						};
						if (entry.ArtifactButtons.Count < 5)
						{
							entry.ArtifactButtons.Add(button);
						}
						if (entry.ArtifactButtons.Count == 5 || i == this.SelectedEmpire.data.OwnedArtifacts.Count - 1)
						{
							this.ArtifactsSL.AddItem(entry);
							entry = new ArtifactEntry();
						}
					}
				}
			}
			for (int i = this.ArtifactsSL.indexAtTop; i < this.ArtifactsSL.Entries.Count && i < this.ArtifactsSL.indexAtTop + this.ArtifactsSL.entriesToDisplay; i++)
			{
				foreach (SkinnableButton button in (this.ArtifactsSL.Entries[i].item as ArtifactEntry).ArtifactButtons)
				{
					if (!HelperFunctions.CheckIntersection(button.r, input.CursorPosition))
					{
						continue;
					}
					string Text = string.Concat(Localizer.Token((button.ReferenceObject as Artifact).NameIndex), "\n\n");
					Text = string.Concat(Text, Localizer.Token((button.ReferenceObject as Artifact).DescriptionIndex));
					ToolTip.CreateTooltip(Text, base.ScreenManager);
				}
			}
			if (input.Escaped || input.CurrentMouseState.RightButton == ButtonState.Pressed)
			{
				this.ExitScreen();
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			float screenWidth = (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			float screenHeight = (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
			Rectangle titleRect = new Rectangle((int)screenWidth / 2 - 200, 44, 400, 80);
			this.TitleBar = new Menu2(base.ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString(Localizer.Token(1600)).X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			Rectangle leftRect = new Rectangle((int)screenWidth / 2 - 640, (screenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1280, 660);
			this.DMenu = new Menu2(base.ScreenManager, leftRect);
			this.close = new CloseButton(new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
			this.SelectedInfoRect = new Rectangle(leftRect.X + 60, leftRect.Y + 250, 368, 376);
			this.IntelligenceRect = new Rectangle(this.SelectedInfoRect.X + this.SelectedInfoRect.Width + 30, this.SelectedInfoRect.Y, 368, 376);
			this.OperationsRect = new Rectangle(this.IntelligenceRect.X + this.IntelligenceRect.Width + 30, this.SelectedInfoRect.Y, 368, 376);
			this.ArtifactsRect = new Rectangle(this.SelectedInfoRect.X + 20, this.SelectedInfoRect.Y + 180, this.SelectedInfoRect.Width - 40, 130);
			Submenu ArtifactsSub = new Submenu(base.ScreenManager, this.ArtifactsRect);
			this.ArtifactsSL = new ScrollList(ArtifactsSub, 40);
			this.Contact = new DanButton(new Vector2((float)(this.SelectedInfoRect.X + this.SelectedInfoRect.Width / 2 - 91), (float)(this.SelectedInfoRect.Y + this.SelectedInfoRect.Height - 45)), Localizer.Token(1644))
			{
				Toggled = true
			};
			foreach (Empire e in EmpireManager.EmpireList)
			{
				if (e != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
				{
					if (e.isFaction || e.MinorRace)
					{
						continue;
					}
					RaceEntry re = new RaceEntry()
					{
						e = e
					};
					this.Races.Add(re);
				}
				else
				{
					RaceEntry re = new RaceEntry()
					{
						e = e
					};
					this.SelectedEmpire = e;
					this.ArtifactsSL.Entries.Clear();
					this.ArtifactsSL.indexAtTop = 0;
					ArtifactEntry entry = new ArtifactEntry();
					for (int i = 0; i < e.data.OwnedArtifacts.Count; i++)
					{
						Artifact art = e.data.OwnedArtifacts[i];
						SkinnableButton button = new SkinnableButton(new Rectangle(0, 0, 32, 32), string.Concat("Artifact Icons/", art.Name))
						{
							IsToggle = false,
							ReferenceObject = art,
							BaseColor = Color.White
						};
						if (entry.ArtifactButtons.Count < 5)
						{
							entry.ArtifactButtons.Add(button);
						}
						if (entry.ArtifactButtons.Count == 5 || i == e.data.OwnedArtifacts.Count - 1)
						{
							this.ArtifactsSL.AddItem(entry);
							entry = new ArtifactEntry();
						}
					}
					this.Races.Add(re);
				}
			}
			Vector2 Cursor = new Vector2(screenWidth / 2f - (float)(148 * this.Races.Count / 2), (float)(leftRect.Y + 10));
			int j = 0;
			foreach (RaceEntry re in this.Races)
			{
				re.container = new Rectangle((int)Cursor.X + 10 + j * 148, leftRect.Y + 40, 124, 148);
				j++;
			}
			base.ScreenManager.racialMusic.SetVolume(0f);
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			MainDiplomacyScreen transitionElapsedTime = this;
			transitionElapsedTime.TransitionElapsedTime = transitionElapsedTime.TransitionElapsedTime + elapsedTime;
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}