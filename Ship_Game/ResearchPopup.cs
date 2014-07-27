using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ResearchPopup : PopupWindow
	{
		public bool fade = true;

		public bool FromGame;

		private UniverseScreen screen;

		public string TechUID;

		private ScrollList UnlockSL;

		private Rectangle UnlocksRect;

		public ResearchPopup(UniverseScreen s, Rectangle dimensions, string uid)
		{
			if (GlobalStats.Config.Language != "English")
			{
				dimensions.X = dimensions.X - 20;
				dimensions.Width = dimensions.Width + 40;
			}
			this.TechUID = uid;
			this.screen = s;
			this.fade = true;
			base.IsPopup = true;
			this.FromGame = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0);
			this.r = dimensions;
			this.TitleText = Localizer.Token(ResourceManager.TechTree[uid].NameIndex);
			this.MiddleText = Localizer.Token(ResourceManager.TechTree[uid].DescriptionIndex);
		}

        public override void Draw(GameTime gameTime)
        {
            if (this.fade)
                this.ScreenManager.FadeBackBufferToBlack((int)this.TransitionAlpha * 2 / 3);
            //draw frame, name and description
            this.DrawBase(gameTime);
            this.ScreenManager.SpriteBatch.Begin();
            //draw some scroll bar? never actually seen
            this.UnlockSL.Draw(this.ScreenManager.SpriteBatch);
            Vector2 vector2 = new Vector2((float)this.UnlocksRect.X, (float)this.UnlocksRect.Y);
            for (int index = this.UnlockSL.indexAtTop; index < this.UnlockSL.Copied.Count && index < this.UnlockSL.indexAtTop + this.UnlockSL.entriesToDisplay; ++index)
            {
                ScrollList.Entry entry = this.UnlockSL.Copied[index];
                UnlockItem unlockItem = entry.item as UnlockItem;
                vector2.Y = (float)entry.clickRect.Y;
                if (unlockItem.Type == "SHIPMODULE")
                {
                    Rectangle destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, 16 * (int)unlockItem.module.XSIZE, 16 * (int)unlockItem.module.YSIZE);
                    destinationRectangle.X = destinationRectangle.X + 48 - destinationRectangle.Width / 2;
                    destinationRectangle.Y = entry.clickRect.Y + entry.clickRect.Height / 2 - destinationRectangle.Height / 2;
                    if ((int)unlockItem.module.XSIZE == 1 && (int)unlockItem.module.YSIZE == 1)
                    {
                        destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, 64, 64);
                        destinationRectangle.X = destinationRectangle.X + 48 - destinationRectangle.Width / 2;
                        destinationRectangle.Y = entry.clickRect.Y + entry.clickRect.Height / 2 - destinationRectangle.Height / 2;
                    }
                    else
                    {
                        while (destinationRectangle.Height < entry.clickRect.Height)
                        {
                            destinationRectangle.Height += (int)unlockItem.module.YSIZE;
                            destinationRectangle.Width += (int)unlockItem.module.XSIZE;
                            destinationRectangle.X = entry.clickRect.X + 48 - destinationRectangle.Width / 2;
                            destinationRectangle.Y = entry.clickRect.Y + entry.clickRect.Height / 2 - destinationRectangle.Height / 2;
                        }
                        while (destinationRectangle.Height > entry.clickRect.Height)
                        {
                            destinationRectangle.Height -= (int)unlockItem.module.YSIZE;
                            destinationRectangle.Width -= (int)unlockItem.module.XSIZE;
                            destinationRectangle.X = entry.clickRect.X + 48 - destinationRectangle.Width / 2;
                            destinationRectangle.Y = entry.clickRect.Y + entry.clickRect.Height / 2 - destinationRectangle.Height / 2;
                        }
                    }
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.ShipModulesDict[unlockItem.module.UID].IconTexturePath], destinationRectangle, Color.White);
                    Localizer.Token((int)unlockItem.module.NameIndex);
                    string text = HelperFunctions.parseText(Fonts.Arial12, unlockItem.Description, (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, unlockItem.privateName, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.White);
                }
                if (unlockItem.Type == "TROOP")
                {
                    Rectangle drawRect = new Rectangle((int)vector2.X + 16, (int)vector2.Y + entry.clickRect.Height / 2 - 32, 64, 64);
                    unlockItem.troop.Draw(this.ScreenManager.SpriteBatch, drawRect);
                    string Text = unlockItem.troop.Name;
                    string text = HelperFunctions.parseText(Fonts.Arial12, unlockItem.troop.Description, (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.White);
                }
                if (unlockItem.Type == "BUILDING")
                {
                    Rectangle destinationRectangle = new Rectangle((int)vector2.X + 16, (int)vector2.Y + entry.clickRect.Height / 2 - 32, 64, 64);
                    //picture of building
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + unlockItem.building.Icon + "_64x64"], destinationRectangle, Color.White);
                    string Text = Localizer.Token(unlockItem.building.NameTranslationIndex);
                    string text = HelperFunctions.parseText(Fonts.Arial12, Localizer.Token(unlockItem.building.DescriptionIndex), (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    //name of unlocked building
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                    //description of unlocked building
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.White);
                }
                if (unlockItem.Type == "HULL")
                {
                    Rectangle destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, 96, 96);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[unlockItem.privateName].IconPath], destinationRectangle, Color.White);
                    string Text = unlockItem.HullUnlocked;
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(unlockItem.Description).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, unlockItem.Description, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.White);
                }
                if (unlockItem.Type == "ADVANCE")
                {
                    string text = HelperFunctions.parseText(Fonts.Arial12, unlockItem.Description, (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, unlockItem.privateName, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.White);
                }
            }
            this.ScreenManager.SpriteBatch.End();
        }

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		~ResearchPopup()
		{
			this.Dispose(false);
		}

		public override void HandleInput(InputState input)
		{
			this.UnlockSL.HandleInput(input);
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			base.LoadContent();
			this.UnlocksRect = new Rectangle(this.MidContainer.X + 20, this.MidContainer.Y + this.MidContainer.Height - 20, this.r.Width - 40, this.r.Height - this.MidContainer.Height - this.TitleRect.Height - 20);
			Submenu UnlocksSubMenu = new Submenu(base.ScreenManager, this.UnlocksRect);
			this.UnlockSL = new ScrollList(UnlocksSubMenu, 100);
			Technology unlockedTech = ResourceManager.TechTree[this.TechUID];
            foreach (Technology.UnlockedMod UnlockedMod in unlockedTech.ModulesUnlocked)
			{
                if (!(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.Traits.ShipType == UnlockedMod.Type) && UnlockedMod.Type != null)
                {
                    continue;
                }
				UnlockItem unlock = new UnlockItem()
				{
					Type = "SHIPMODULE",
                    module = ResourceManager.ShipModulesDict[UnlockedMod.ModuleUID],
                    Description = Localizer.Token(ResourceManager.ShipModulesDict[UnlockedMod.ModuleUID].DescriptionIndex),
                    privateName = Localizer.Token(ResourceManager.ShipModulesDict[UnlockedMod.ModuleUID].NameIndex)
				};
				this.UnlockSL.AddItem(unlock);
			}
			foreach (Technology.UnlockedTroop troop in unlockedTech.TroopsUnlocked)
			{
				if (!(troop.Type == EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.Traits.ShipType) && !(troop.Type == "ALL"))
				{
					continue;
				}
				UnlockItem unlock = new UnlockItem()
				{
					Type = "TROOP",
					troop = ResourceManager.TroopsDict[troop.Name]
				};
				this.UnlockSL.AddItem(unlock);
			}
			foreach (Technology.UnlockedHull hull in unlockedTech.HullsUnlocked)
			{
				if (!(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.Traits.ShipType == hull.ShipType) && hull.ShipType != null)
				{
					continue;
				}
				UnlockItem unlock = new UnlockItem()
				{
					Type = "HULL",
					privateName = hull.Name,
					HullUnlocked = ResourceManager.HullsDict[hull.Name].Name
				};
				int size = 0;
				foreach (ModuleSlotData moduleSlotList in ResourceManager.HullsDict[hull.Name].ModuleSlotList)
				{
					size++;
				}
				unlock.Description = string.Concat(Localizer.Token(4042), " ", ResourceManager.HullsDict[hull.Name].Role);
				this.UnlockSL.AddItem(unlock);
			}
            foreach (Technology.UnlockedBuilding UnlockedBuilding in unlockedTech.BuildingsUnlocked)
			{
                if (!(EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty).data.Traits.ShipType == UnlockedBuilding.Type) && UnlockedBuilding.Type != null)
                {
                    continue;
                }
				UnlockItem unlock = new UnlockItem()
				{
					Type = "BUILDING",
                    building = ResourceManager.BuildingsDict[UnlockedBuilding.Name]
				};
				this.UnlockSL.AddItem(unlock);
			}
			foreach (Technology.UnlockedBonus ub in unlockedTech.BonusUnlocked)
			{
				UnlockItem unlock = new UnlockItem()
				{
					Type = "ADVANCE",
					privateName = ub.Name,
					Description = Localizer.Token(ub.BonusIndex)
				};
				this.UnlockSL.AddItem(unlock);
			}
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}