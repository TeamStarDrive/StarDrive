using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class ResearchPopup : PopupWindow, IDisposable
	{
		public bool fade = true;

		public bool FromGame;

		private UniverseScreen screen;

		public string TechUID;

		private ScrollList UnlockSL;

		private Rectangle UnlocksRect;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public ResearchPopup(UniverseScreen s, Rectangle dimensions, string uid)
		{
			if (!GlobalStats.IsEnglish)
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
			this.R = dimensions;
            this.TitleText = string.Concat(Localizer.Token(ResourceManager.TechTree[uid].NameIndex), 
                ResourceManager.TechTree[uid].MaxLevel > 1 ? " " + 
                NumberToRomanConvertor.NumberToRoman(EmpireManager.Player.TechnologyDict[uid].Level) + "/" + NumberToRomanConvertor.NumberToRoman(ResourceManager.TechTree[uid].MaxLevel) : "");
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
                if (unlockItem.Type == UnlockType.SHIPMODULE)
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
                    string text = HelperFunctions.ParseText(Fonts.Arial12, unlockItem.Description, (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, unlockItem.privateName, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                }
                if (unlockItem.Type == UnlockType.TROOP)
                {
                    Rectangle drawRect = new Rectangle((int)vector2.X + 16, (int)vector2.Y + entry.clickRect.Height / 2 - 32, 64, 64);
                    unlockItem.troop.Draw(this.ScreenManager.SpriteBatch, drawRect);
                    string Text = unlockItem.troop.Name;
                    string text = HelperFunctions.ParseText(Fonts.Arial12, unlockItem.troop.Description, (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                }
                if (unlockItem.Type == UnlockType.BUILDING)
                {
                    Rectangle destinationRectangle = new Rectangle((int)vector2.X + 16, (int)vector2.Y + entry.clickRect.Height / 2 - 32, 64, 64);
                    //picture of building
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + unlockItem.building.Icon + "_64x64"], destinationRectangle, Color.White);
                    string Text = Localizer.Token(unlockItem.building.NameTranslationIndex);
                    string text = HelperFunctions.ParseText(Fonts.Arial12, Localizer.Token(unlockItem.building.DescriptionIndex), (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    //name of unlocked building
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                    //description of unlocked building
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                }
                if (unlockItem.Type == UnlockType.HULL)
                {
                    Rectangle destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, 96, 96);
                    this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[unlockItem.privateName].IconPath], destinationRectangle, Color.White);
                    string Text = unlockItem.HullUnlocked;
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(unlockItem.Description).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, unlockItem.Description, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                }
                if (unlockItem.Type == UnlockType.ADVANCE)
                {
                    string text = HelperFunctions.ParseText(Fonts.Arial12, unlockItem.Description, (float)(entry.clickRect.Width - 100));
                    float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                    Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                    Pos.X = (float)(int)Pos.X;
                    Pos.Y = (float)(int)Pos.Y;
                    HelperFunctions.DrawDropShadowText(this.ScreenManager, unlockItem.privateName, Pos, Fonts.Arial14Bold, Color.Orange);
                    this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                }
            }
            this.ScreenManager.SpriteBatch.End();
        }

		public override void ExitScreen()
		{
			base.ExitScreen();
		}

		public override void HandleInput(InputState input)
		{
			this.UnlockSL.HandleInput(input);
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			base.LoadContent();
			this.UnlocksRect = new Rectangle(this.MidContainer.X + 20, this.MidContainer.Y + this.MidContainer.Height - 20, this.R.Width - 40, this.R.Height - this.MidContainer.Height - this.TitleRect.Height - 20);
			Submenu UnlocksSubMenu = new Submenu(base.ScreenManager, this.UnlocksRect);
			this.UnlockSL = new ScrollList(UnlocksSubMenu, 100);
			Technology unlockedTech = ResourceManager.TechTree[this.TechUID];
            foreach (Technology.UnlockedMod UnlockedMod in unlockedTech.ModulesUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == UnlockedMod.Type || UnlockedMod.Type == null || UnlockedMod.Type == EmpireManager.Player.GetTDict()[this.TechUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.SHIPMODULE,
                        module = ResourceManager.ShipModulesDict[UnlockedMod.ModuleUID],
                        Description = Localizer.Token(ResourceManager.ShipModulesDict[UnlockedMod.ModuleUID].DescriptionIndex),
                        privateName = Localizer.Token(ResourceManager.ShipModulesDict[UnlockedMod.ModuleUID].NameIndex)
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
			foreach (Technology.UnlockedTroop troop in unlockedTech.TroopsUnlocked)
			{
                if (troop.Type == EmpireManager.Player.data.Traits.ShipType || troop.Type == "ALL" || troop.Type == null || troop.Type == EmpireManager.Player.GetTDict()[this.TechUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.TROOP,
                        troop = ResourceManager.TroopsDict[troop.Name]
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
			foreach (Technology.UnlockedHull hull in unlockedTech.HullsUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == hull.ShipType || hull.ShipType == null || hull.ShipType == EmpireManager.Player.GetTDict()[this.TechUID].AcquiredFrom)
                {

                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.HULL,
                        privateName = hull.Name,
                        HullUnlocked = ResourceManager.HullsDict[hull.Name].Name
                    };
                    int size = ResourceManager.HullsDict[hull.Name].ModuleSlotList.Count;
                    unlock.Description = string.Concat(Localizer.Token(4042), " ", Localizer.GetRole(ResourceManager.HullsDict[hull.Name].Role, EmpireManager.Player));
                    this.UnlockSL.AddItem(unlock);
                }
			}
            foreach (Technology.UnlockedBuilding UnlockedBuilding in unlockedTech.BuildingsUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == UnlockedBuilding.Type || UnlockedBuilding.Type == null || UnlockedBuilding.Type == EmpireManager.Player.GetTDict()[this.TechUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.BUILDING,
                        building = ResourceManager.BuildingsDict[UnlockedBuilding.Name]
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
            foreach (Technology.UnlockedBonus UnlockedBonus in unlockedTech.BonusUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == UnlockedBonus.Type || UnlockedBonus.Type == null || UnlockedBonus.Type == EmpireManager.Player.GetTDict()[this.TechUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.ADVANCE,
                        privateName = UnlockedBonus.Name,
                        Description = Localizer.Token(UnlockedBonus.BonusIndex)
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ResearchPopup() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.UnlockSL != null)
                        this.UnlockSL.Dispose();

                }
                this.UnlockSL = null;
                this.disposed = true;
            }
        }
	}
}