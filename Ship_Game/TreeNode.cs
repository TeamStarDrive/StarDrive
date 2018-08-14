using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ship_Game
{
	public sealed class TreeNode : Node
	{
		public SpriteFont TitleFont = Fonts.Visitor10;

		public NodeState nodeState;

		public ResearchScreenNew screen;

		public string TechName;

		public Rectangle TitleRect;

		public Rectangle BaseRect = new Rectangle(0, 0, 92, 98);

        //public bool researching;          //Not referenced in code, removing to save memory

        public bool complete;

		private Rectangle IconRect;

		private Rectangle UnlocksRect;

		private Array<UnlockItem> Unlocks = new Array<UnlockItem>();

		private UnlocksGrid grid;

		private Rectangle progressRect = new Rectangle();

		private float TitleWidth = 73f;

		private Vector2 CostPos = new Vector2();

		public TreeNode(Vector2 Position, TechEntry Tech, ResearchScreenNew screen)
		{
			if (GlobalStats.IsRussian || GlobalStats.IsPolish)
			{
				this.TitleFont = Fonts.Arial10;
			}
			this.screen = screen;
			this.tech = Tech;
			this.TechName = string.Concat(Localizer.Token(ResourceManager.TechTree[Tech.UID].NameIndex), ResourceManager.TechTree[Tech.UID].MaxLevel > 1 ? " " + NumberToRomanConvertor.NumberToRoman(Tech.Level) + "/" + NumberToRomanConvertor.NumberToRoman(ResourceManager.TechTree[Tech.UID].MaxLevel) : "");
			this.BaseRect.X = (int)Position.X;
			this.BaseRect.Y = (int)Position.Y;
			this.progressRect = new Rectangle(this.BaseRect.X + 14, this.BaseRect.Y + 21, 1, 34);
			int numUnlocks = 0;
            Technology techTemplate = ResourceManager.TechTree[tech.UID];

            for (int i = 0; i < techTemplate.ModulesUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.ModulesUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.ModulesUnlocked[i].Type == null || 
                    techTemplate.ModulesUnlocked[i].Type == EmpireManager.Player.GetTDict()[tech.UID].AcquiredFrom)
                {
                    UnlockItem unlock  = new UnlockItem();
                    unlock.module      = ResourceManager.GetModuleTemplate(techTemplate.ModulesUnlocked[i].ModuleUID);
                    unlock.privateName = Localizer.Token(unlock.module.NameIndex);
                    unlock.Description = Localizer.Token(unlock.module.DescriptionIndex);
                    unlock.Type        = UnlockType.SHIPMODULE;
                    this.Unlocks.Add(unlock);
                    numUnlocks++;
                }
			}
			for (int i = 0; i < techTemplate.BonusUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.BonusUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType ||
                    techTemplate.BonusUnlocked[i].Type == null ||
                    techTemplate.BonusUnlocked[i].Type == EmpireManager.Player.GetTDict()[tech.UID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        privateName = techTemplate.BonusUnlocked[i].Name,
                        Description = Localizer.Token(techTemplate.BonusUnlocked[i].BonusIndex),
                        Type = UnlockType.ADVANCE
                    };
                    numUnlocks++;
                    this.Unlocks.Add(unlock);
                }
			}
			for (int i = 0; i < techTemplate.BuildingsUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.BuildingsUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.BuildingsUnlocked[i].Type == null || 
                    techTemplate.BuildingsUnlocked[i].Type == EmpireManager.Player.GetTDict()[this.tech.UID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem();
                    unlock.building = ResourceManager.BuildingsDict[techTemplate.BuildingsUnlocked[i].Name];
                    unlock.privateName = Localizer.Token(unlock.building.NameTranslationIndex);
                    unlock.Description = Localizer.Token(unlock.building.DescriptionIndex);
                    unlock.Type = UnlockType.BUILDING;
                    numUnlocks++;
                    this.Unlocks.Add(unlock);
                }
			}
			for (int i = 0; i < techTemplate.HullsUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
				if (techTemplate.HullsUnlocked[i].ShipType == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.HullsUnlocked[i].ShipType == null || 
                    techTemplate.HullsUnlocked[i].ShipType == EmpireManager.Player.GetTDict()[this.tech.UID].AcquiredFrom)
				{
					UnlockItem unlock = new UnlockItem()
					{
						HullUnlocked = techTemplate.HullsUnlocked[i].Name,
						privateName = techTemplate.HullsUnlocked[i].Name,
						Description = "",
						Type = UnlockType.HULL
					};
					numUnlocks++;
					this.Unlocks.Add(unlock);
				}
			}
			for (int i = 0; i < techTemplate.TroopsUnlocked.Count; i++)
			{
                if (numUnlocks > 3) break;
                if (techTemplate.TroopsUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType || 
                    techTemplate.TroopsUnlocked[i].Type == "ALL" || 
                    techTemplate.TroopsUnlocked[i].Type == null || 
                    techTemplate.TroopsUnlocked[i].Type == EmpireManager.Player.GetTDict()[this.tech.UID].AcquiredFrom)
				{
					UnlockItem unlock = new UnlockItem();
					unlock.troop       = ResourceManager.GetTroopTemplate(techTemplate.TroopsUnlocked[i].Name);
					unlock.privateName = techTemplate.TroopsUnlocked[i].Name;
					unlock.Description = unlock.troop.Description;
                    unlock.Type        = UnlockType.TROOP;
					numUnlocks++;
					Unlocks.Add(unlock);
				}
			}
			int numColumns = numUnlocks / 2 + numUnlocks % 2;
			this.IconRect = new Rectangle(this.BaseRect.X + this.BaseRect.Width / 2 - 29, this.BaseRect.Y + this.BaseRect.Height / 2 - 24 - 10, 58, 49);
			if (numUnlocks <= 1)
			{
				this.UnlocksRect = new Rectangle(this.IconRect.X + this.IconRect.Width, this.IconRect.Y + this.IconRect.Height - 5, 35, 32);

                this.UnlocksRect.Y = this.UnlocksRect.Y - this.UnlocksRect.Height;
				
				Rectangle drawRect = this.UnlocksRect;
				drawRect.X = drawRect.X + 3;
				this.grid = new UnlocksGrid(this.Unlocks, drawRect);
			}
			else
			{
                this.UnlocksRect = new Rectangle(this.IconRect.X + this.IconRect.Width, this.IconRect.Y + this.IconRect.Height - 5, 13 + numColumns * 32, (numUnlocks == 1 ? 32 : 64));
				
					this.UnlocksRect.Y = this.UnlocksRect.Y - this.UnlocksRect.Height;
				
				Rectangle drawRect = this.UnlocksRect;
				drawRect.X = drawRect.X + 13;
				this.grid = new UnlocksGrid(this.Unlocks, drawRect);
			}
			this.UnlocksRect.X = this.UnlocksRect.X - 2;
			this.UnlocksRect.Width = this.UnlocksRect.Width + 4;
			this.UnlocksRect.Y = this.UnlocksRect.Y - 2;
			this.UnlocksRect.Height = this.UnlocksRect.Height + 4;
			this.TitleRect = new Rectangle(this.BaseRect.X + 8, this.BaseRect.Y - 15, 82, 29);
			if (GlobalStats.IsGermanOrPolish)
			{
				this.TitleRect.X = this.TitleRect.X - 5;
				this.TitleRect.Width = this.TitleRect.Width + 5;
				TreeNode titleWidth = this;
				titleWidth.TitleWidth = titleWidth.TitleWidth + 10f;
			}
			this.CostPos = new Vector2(65f, 70f) + new Vector2((float)this.BaseRect.X, (float)this.BaseRect.Y);
			float x = this.CostPos.X;
			SpriteFont titleFont = this.TitleFont;                
			float cost = (float)(tech.TechCost) * UniverseScreen.GamePaceStatic;
			this.CostPos.X = x - titleFont.MeasureString(cost.ToString()).X;
			this.CostPos.X = (float)((int)this.CostPos.X);
			this.CostPos.Y = (float)((int)this.CostPos.Y - 3);
		}

        public void Draw(Ship_Game.ScreenManager ScreenManager)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            if (this.complete)
            {
                this.DrawGlow(ScreenManager,tech.Tech.Secret ? Color.Green : Color.White );
            }
            switch (this.nodeState)
            {
                case NodeState.Normal:
                    bool flag = EmpireManager.Player.ResearchTopic == this.tech.UID || EmpireManager.Player.data.ResearchQueue.Contains(this.tech.UID);
                    spriteBatch.FillRectangle(this.UnlocksRect, new Color((byte)26, (byte)26, (byte)28));
                    spriteBatch.DrawRectangle(this.UnlocksRect, this.complete || flag ? new Color((byte)34, (byte)136, (byte)200) : Color.Black);
                    this.grid.Draw(spriteBatch);
                    spriteBatch.Draw(complete || flag ? ResourceManager.Texture("ResearchMenu/tech_base_complete") : 
                        ResourceManager.Texture("ResearchMenu/tech_base"), BaseRect, Color.White);
                    //Added by McShooterz: Allows non root techs to use IconPath
                    var techIcon = ResourceManager.TextureOrNull($"TechIcons/{tech.Tech.IconPath}") 
                        ?? ResourceManager.Texture($"TechIcons/{tech.UID}");
                    
                    spriteBatch.Draw(techIcon, this.IconRect, Color.White);                    
                    spriteBatch.Draw(this.complete || flag ? ResourceManager.Texture("ResearchMenu/tech_base_title_complete") 
                        : ResourceManager.Texture("ResearchMenu/tech_base_title"), this.TitleRect, Color.White);
                    string str1 = HelperFunctions.ParseText(this.TitleFont, this.TechName, this.TitleWidth);
                    string[] strArray1 = Regex.Split(str1, "\n");
                    Vector2 vector2_1 = new Vector2((float)(this.TitleRect.X + this.TitleRect.Width / 2) - this.TitleFont.MeasureString(str1).X / 2f, (float)(this.TitleRect.Y + 14) - this.TitleFont.MeasureString(str1).Y / 2f);
                    int num1 = 0;
                    foreach (string text in strArray1)
                    {
                        Vector2 position = new Vector2((float)(this.TitleRect.X + this.TitleRect.Width / 2) - this.TitleFont.MeasureString(text).X / 2f, vector2_1.Y + (float)(num1 * this.TitleFont.LineSpacing));
                        position = new Vector2((float)(int)position.X, (float)(int)position.Y);
                        spriteBatch.DrawString(this.TitleFont, text, position, this.complete ? new Color((byte)132, (byte)172, (byte)208) : Color.White);
                        ++num1;
                    }
                    int num2 = (int)((double)this.progressRect.Height - (double)(EmpireManager.Player.GetTDict()[this.tech.UID].Progress / EmpireManager.Player.GetTDict()[this.tech.UID].TechCost) * (double)this.progressRect.Height);
                    Rectangle destinationRectangle1 = this.progressRect;
                    destinationRectangle1.Height = num2;
                    spriteBatch.Draw(this.complete || flag ? ResourceManager.Texture("ResearchMenu/tech_progress") 
                        : ResourceManager.Texture("ResearchMenu/tech_progress_inactive"), this.progressRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle1, Color.White);
                    break;
                case NodeState.Hover:
                    spriteBatch.FillRectangle(this.UnlocksRect, new Color((byte)26, (byte)26, (byte)28));
                    spriteBatch.DrawRectangle(this.UnlocksRect, new Color((byte)190, (byte)113, (byte)25));
                    this.grid.Draw(spriteBatch);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_hover"), this.BaseRect, Color.White);
                    //Added by McShooterz: Allows non root techs to use IconPath
                    if (ResourceManager.TextureLoaded("TechIcons/" + this.tech.Tech.IconPath))
                    {
                        spriteBatch.Draw(ResourceManager.Texture("TechIcons/" + this.tech.Tech.IconPath), this.IconRect, Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(ResourceManager.Texture("TechIcons/" + this.tech.UID), this.IconRect, Color.White);
                    }
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_title_hover"), this.TitleRect, Color.White);
                    string str2 = HelperFunctions.ParseText(this.TitleFont, this.TechName, this.TitleWidth);
                    string[] strArray2 = Regex.Split(str2, "\n");
                    Vector2 vector2_2 = new Vector2((float)(this.TitleRect.X + this.TitleRect.Width / 2) - this.TitleFont.MeasureString(str2).X / 2f, (float)(this.TitleRect.Y + 14) - this.TitleFont.MeasureString(str2).Y / 2f);
                    int num3 = 0;
                    foreach (string text in strArray2)
                    {
                        Vector2 position = new Vector2((float)(this.TitleRect.X + this.TitleRect.Width / 2) - this.TitleFont.MeasureString(text).X / 2f, vector2_2.Y + (float)(num3 * this.TitleFont.LineSpacing));
                        position = new Vector2((float)(int)position.X, (float)(int)position.Y);
                        spriteBatch.DrawString(this.TitleFont, text, position, this.complete ? new Color((byte)132, (byte)172, (byte)208) : Color.White);
                        ++num3;
                    }
                    int num4 = (int)((double)this.progressRect.Height - (double)(EmpireManager.Player.GetTDict()[this.tech.UID].Progress / ResourceManager.TechTree[this.tech.UID].Cost) * (double)this.progressRect.Height);
                    Rectangle destinationRectangle2 = this.progressRect;
                    destinationRectangle2.Height = num4;
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress"), this.progressRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle2, Color.White);
                    break;
                case NodeState.Press:
                    spriteBatch.FillRectangle(this.UnlocksRect, new Color((byte)26, (byte)26, (byte)28));
                    spriteBatch.DrawRectangle(this.UnlocksRect, new Color((byte)190, (byte)113, (byte)25));
                    this.grid.Draw(spriteBatch);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_hover"), this.BaseRect, Color.White);
                    //Added by McShooterz: Allows non root techs to use IconPath
                    if (ResourceManager.TextureLoaded("TechIcons/" + this.tech.Tech.IconPath))
                    {
                        spriteBatch.Draw(ResourceManager.Texture("TechIcons/" + this.tech.Tech.IconPath), this.IconRect, Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(ResourceManager.Texture("TechIcons/" + this.tech.UID), this.IconRect, Color.White);
                    }
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_base_title_hover"), this.TitleRect, Color.White);
                    string str3 = HelperFunctions.ParseText(this.TitleFont, this.TechName, this.TitleWidth);
                    string[] strArray3 = Regex.Split(str3, "\n");
                    Vector2 vector2_3 = new Vector2((float)(this.TitleRect.X + this.TitleRect.Width / 2) - this.TitleFont.MeasureString(str3).X / 2f, (float)(this.TitleRect.Y + 14) - this.TitleFont.MeasureString(str3).Y / 2f);
                    int num5 = 0;
                    foreach (string text in strArray3)
                    {
                        Vector2 position = new Vector2((float)(this.TitleRect.X + this.TitleRect.Width / 2) - this.TitleFont.MeasureString(text).X / 2f, vector2_3.Y + (float)(num5 * this.TitleFont.LineSpacing));
                        position = new Vector2((float)(int)position.X, (float)(int)position.Y);
                        spriteBatch.DrawString(this.TitleFont, text, position, this.complete ? new Color((byte)163, (byte)198, (byte)236) : Color.White);
                        ++num5;
                    }
                    int num6 = (int)((double)this.progressRect.Height - (double)(EmpireManager.Player.GetTDict()[this.tech.UID].Progress / EmpireManager.Player.GetTDict()[this.tech.UID].TechCost) * (double)this.progressRect.Height);
                    Rectangle destinationRectangle3 = this.progressRect;
                    destinationRectangle3.Height = num6;
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress"), this.progressRect, Color.White);
                    spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle3, Color.White);
                    break;
            }
            spriteBatch.DrawString(this.TitleFont, ((float)(int)EmpireManager.Player.GetTDict()[this.tech.UID].TechCost).ToString(), this.CostPos, Color.White);
        }

		public void DrawGlow(Ship_Game.ScreenManager ScreenManager) 
		{
            DrawGlow(ScreenManager, Color.White);
		}
	    public void DrawGlow(Ship_Game.ScreenManager ScreenManager, Color color)
	    {
	        SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
	        spriteBatch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_base"), this.BaseRect, color);
	        spriteBatch.DrawRectangleGlow(this.TitleRect);
	        spriteBatch.DrawRectangleGlow(this.UnlocksRect);
        }

        public bool HandleInput(InputState input, Ship_Game.ScreenManager ScreenManager, Camera2D camera)
		{
			Vector2 RectPos = camera.GetScreenSpaceFromWorldSpace(new Vector2((float)this.BaseRect.X, (float)this.BaseRect.Y));
			Rectangle moddedRect = new Rectangle((int)RectPos.X, (int)RectPos.Y, this.BaseRect.Width, this.BaseRect.Height);
			Vector2 RectPos2 = camera.GetScreenSpaceFromWorldSpace(new Vector2((float)this.UnlocksRect.X, (float)this.UnlocksRect.Y));
			Rectangle moddedRect2 = new Rectangle((int)RectPos2.X, (int)RectPos2.Y, this.UnlocksRect.Width, this.UnlocksRect.Height);
			Vector2 RectPos3 = camera.GetScreenSpaceFromWorldSpace(new Vector2((float)this.IconRect.X, (float)this.IconRect.Y));
			Rectangle moddedRect3 = new Rectangle((int)RectPos3.X, (int)RectPos3.Y, this.IconRect.Width, this.IconRect.Height);
			if (moddedRect.HitTest(input.CursorPosition) || moddedRect2.HitTest(input.CursorPosition))
			{
				if (this.nodeState != NodeState.Hover)
				{
					GameAudio.PlaySfxAsync("mouse_over4");
				}
				this.nodeState = NodeState.Hover;
				if (input.InGameSelect)
				{
					this.nodeState = NodeState.Press;
					return true;
				}
				if (input.RightMouseClick)
				{
					this.screen.RightClicked = true;
					ScreenManager.AddScreen(new ResearchPopup(Empire.Universe, this.tech.UID));
					return false;
				}
			}
			else
			{
				this.nodeState = NodeState.Normal;
			}
			if (!moddedRect3.HitTest(input.CursorPosition))
			{
				foreach (UnlocksGrid.GridItem gridItem in this.grid.GridOfUnlocks)
				{
					Vector2 RectPos4 = camera.GetScreenSpaceFromWorldSpace(new Vector2((float)gridItem.rect.X, (float)gridItem.rect.Y));
					Rectangle moddedRect4 = new Rectangle((int)RectPos4.X, (int)RectPos4.Y, gridItem.rect.Width, gridItem.rect.Height);
					if (!moddedRect4.HitTest(input.CursorPosition))
					{
						continue;
					}
					string tip = string.Concat(gridItem.item.privateName, "\n\n", gridItem.item.Description);
					if (gridItem.item.HullUnlocked == null)
					{
						ToolTip.CreateTooltip(tip);
					}
					else
					{
                        var unlocked = ResourceManager.HullsDict[gridItem.item.HullUnlocked];
                        ToolTip.CreateTooltip($"{unlocked.Name} ({Localizer.GetRole(unlocked.Role, EmpireManager.Player)})");
					}
				}
			}
			else
			{
				ToolTip.CreateTooltip($"Right Click to Expand \n\n{Localizer.Token(ResourceManager.TechTree[tech.UID].DescriptionIndex)}");
			}
			return false;
		}
	}
}