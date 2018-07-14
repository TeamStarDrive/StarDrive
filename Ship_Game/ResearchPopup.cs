using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ResearchPopup : PopupWindow
    {
        public bool fade = true;

        public bool FromGame;

        public string TechUID;

        private ScrollList UnlockSL;

        private Rectangle UnlocksRect;
       
        
        public ResearchPopup(UniverseScreen s, string uid) : base(s, 600, 600)
        {
            TechUID = uid;
            fade = true;
            base.IsPopup = true;
            FromGame = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0);
            var techEntry = EmpireManager.Player.GetTechEntry(uid);
            if (techEntry == null) return;
            TitleText = string.Concat(Localizer.Token(ResourceManager.TechTree[uid].NameIndex), 
                ResourceManager.TechTree[uid].MaxLevel > 1 ? " " + 
                NumberToRomanConvertor.NumberToRoman(techEntry.Level) + "/" + NumberToRomanConvertor.NumberToRoman(techEntry.MaxLevel) : "");
            MiddleText = Localizer.Token(techEntry.Tech.DescriptionIndex);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (fade)
                ScreenManager.FadeBackBufferToBlack((int)TransitionAlpha * 2 / 3);


            base.Draw(spriteBatch);


            ScreenManager.SpriteBatch.Begin();
            //draw some scroll bar? never actually seen
            UnlockSL.Draw(ScreenManager.SpriteBatch);
            var vector2 = new Vector2(UnlocksRect.X, UnlocksRect.Y);
            foreach (ScrollList.Entry entry in UnlockSL.VisibleExpandedEntries)
            {
                var unlockItem = entry.item as UnlockItem;
                vector2.Y = (float)entry.clickRect.Y;
                switch (unlockItem.Type)
                {
                    case UnlockType.SHIPMODULE:
                    {
                        var destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, 16 * (int)unlockItem.module.XSIZE, 16 * (int)unlockItem.module.YSIZE);
                        destinationRectangle.X = destinationRectangle.X + 48 - destinationRectangle.Width / 2;
                        destinationRectangle.Y = entry.clickRect.Y + entry.clickRect.Height / 2 - destinationRectangle.Height / 2;
                        if (unlockItem.module.XSIZE == 1 && unlockItem.module.YSIZE == 1)
                        {
                            destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, 64, 64);
                            destinationRectangle.X = destinationRectangle.X + 48 - destinationRectangle.Width / 2;
                            destinationRectangle.Y = entry.clickRect.Y + entry.clickRect.Height / 2 - destinationRectangle.Height / 2;
                        }
                        else
                        {
                            while (destinationRectangle.Height < entry.clickRect.Height)
                            {
                                destinationRectangle = DestinationRectangle(destinationRectangle, entry, unlockItem.module.YSIZE, unlockItem.module.XSIZE);
                            }
                            while (destinationRectangle.Height > entry.clickRect.Height)
                            {
                                destinationRectangle = DestinationRectangle(destinationRectangle, entry, -unlockItem.module.YSIZE, -unlockItem.module.XSIZE);                                    
                            }
                        }
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.GetModuleTemplate(unlockItem.module.UID).IconTexturePath], destinationRectangle, Color.White);
                        Localizer.Token((int)unlockItem.module.NameIndex);
                        string text = HelperFunctions.ParseText(Fonts.Arial12, unlockItem.Description, (float)(entry.clickRect.Width - 100));
                        float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                        Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                        Pos.X = (float)(int)Pos.X;
                        Pos.Y = (float)(int)Pos.Y;
                        HelperFunctions.DrawDropShadowText(ScreenManager, unlockItem.privateName, Pos, Fonts.Arial14Bold, Color.Orange);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                        break;
                    }
                    case UnlockType.TROOP:
                    {
                        Rectangle drawRect = new Rectangle((int)vector2.X + 16, (int)vector2.Y + entry.clickRect.Height / 2 - 32, 64, 64);
                        unlockItem.troop.Draw(ScreenManager.SpriteBatch, drawRect);
                        string Text = unlockItem.troop.Name;
                        string text = HelperFunctions.ParseText(Fonts.Arial12, unlockItem.troop.Description, (float)(entry.clickRect.Width - 100));
                        float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                        Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                        Pos.X = (float)(int)Pos.X;
                        Pos.Y = (float)(int)Pos.Y;
                        HelperFunctions.DrawDropShadowText(ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                        break;
                    }
                    case UnlockType.BUILDING:
                    {
                        Rectangle destinationRectangle = new Rectangle((int)vector2.X + 16, (int)vector2.Y + entry.clickRect.Height / 2 - 32, 64, 64);
                        //picture of building
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Buildings/icon_" + unlockItem.building.Icon + "_64x64"], destinationRectangle, Color.White);
                        string Text = Localizer.Token(unlockItem.building.NameTranslationIndex);
                        string text = HelperFunctions.ParseText(Fonts.Arial12, Localizer.Token(unlockItem.building.DescriptionIndex), (float)(entry.clickRect.Width - 100));
                        float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                        Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                        Pos.X = (float)(int)Pos.X;
                        Pos.Y = (float)(int)Pos.Y;
                        //name of unlocked building
                        HelperFunctions.DrawDropShadowText(ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                        //description of unlocked building
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                        break;
                    }
                    case UnlockType.HULL:
                    {
                        Rectangle destinationRectangle = new Rectangle((int)vector2.X, (int)vector2.Y, 96, 96);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.HullsDict[unlockItem.privateName].Icon, destinationRectangle, Color.White);
                        string Text = unlockItem.HullUnlocked;
                        float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(unlockItem.Description).Y;
                        Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                        Pos.X = (float)(int)Pos.X;
                        Pos.Y = (float)(int)Pos.Y;
                        HelperFunctions.DrawDropShadowText(ScreenManager, Text, Pos, Fonts.Arial14Bold, Color.Orange);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, unlockItem.Description, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                        break;
                    }
                    case UnlockType.ADVANCE:
                    {
                        string text = HelperFunctions.ParseText(Fonts.Arial12, unlockItem.Description, (float)(entry.clickRect.Width - 100));
                        float num = (float)(Fonts.Arial14Bold.LineSpacing + 5) + Fonts.Arial12.MeasureString(text).Y;
                        Vector2 Pos = new Vector2((float)(entry.clickRect.X + 100), (float)(entry.clickRect.Y + entry.clickRect.Height / 2) - num / 2f);
                        Pos.X = (float)(int)Pos.X;
                        Pos.Y = (float)(int)Pos.Y;
                        HelperFunctions.DrawDropShadowText(ScreenManager, unlockItem.privateName, Pos, Fonts.Arial14Bold, Color.Orange);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Pos + new Vector2(0.0f, (float)(Fonts.Arial14Bold.LineSpacing + 2)), Color.LightGray);
                        break;
                    }
                }
            }
            ScreenManager.SpriteBatch.End();
        }

        private Rectangle DestinationRectangle(Rectangle destinationRectangle, ScrollList.Entry entry , int ySize, int xSize)
        {
            destinationRectangle.Height += ySize;
            destinationRectangle.Width += xSize;
            destinationRectangle.X = entry.clickRect.X + 48 - destinationRectangle.Width / 2;
            destinationRectangle.Y = entry.clickRect.Y + entry.clickRect.Height / 2 - destinationRectangle.Height / 2;
            return destinationRectangle;
        }

        public override bool HandleInput(InputState input)
        {
            UnlockSL.HandleInput(input);
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            UnlocksRect = new Rectangle(MidContainer.X + 20, MidContainer.Y + MidContainer.Height - 20, Rect.Width - 40, Rect.Height - MidContainer.Height - TitleRect.Height - 20);
            Submenu UnlocksSubMenu = new Submenu(UnlocksRect);
            UnlockSL = new ScrollList(UnlocksSubMenu, 100);
            Technology unlockedTech = ResourceManager.TechTree[TechUID];
            foreach (Technology.UnlockedMod UnlockedMod in unlockedTech.ModulesUnlocked)
            {
                if (EmpireManager.Player.data.Traits.ShipType == UnlockedMod.Type || UnlockedMod.Type == null || UnlockedMod.Type == EmpireManager.Player.GetTDict()[TechUID].AcquiredFrom)
                {
                    ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(UnlockedMod.ModuleUID);
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.SHIPMODULE,
                        module = moduleTemplate,
                        Description = Localizer.Token(moduleTemplate.DescriptionIndex),
                        privateName = Localizer.Token(moduleTemplate.NameIndex)
                    };
                    UnlockSL.AddItem(unlock);
                }
            }
            foreach (Technology.UnlockedTroop troop in unlockedTech.TroopsUnlocked)
            {
                if (troop.Type == EmpireManager.Player.data.Traits.ShipType || troop.Type == "ALL" || troop.Type == null || troop.Type == EmpireManager.Player.GetTDict()[TechUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.TROOP,
                        troop = ResourceManager.GetTroopTemplate(troop.Name)
                    };
                    UnlockSL.AddItem(unlock);
                }
            }
            foreach (Technology.UnlockedHull hull in unlockedTech.HullsUnlocked)
            {
                if (EmpireManager.Player.data.Traits.ShipType == hull.ShipType || hull.ShipType == null || hull.ShipType == EmpireManager.Player.GetTDict()[TechUID].AcquiredFrom)
                {

                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.HULL,
                        privateName = hull.Name,
                        HullUnlocked = ResourceManager.HullsDict[hull.Name].Name
                    };
                    unlock.Description = string.Concat(Localizer.Token(4042), " ", Localizer.GetRole(ResourceManager.HullsDict[hull.Name].Role, EmpireManager.Player));
                    UnlockSL.AddItem(unlock);
                }
            }
            foreach (Technology.UnlockedBuilding UnlockedBuilding in unlockedTech.BuildingsUnlocked)
            {
                if (EmpireManager.Player.data.Traits.ShipType == UnlockedBuilding.Type || UnlockedBuilding.Type == null || UnlockedBuilding.Type == EmpireManager.Player.GetTDict()[TechUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.BUILDING,
                        building = ResourceManager.BuildingsDict[UnlockedBuilding.Name]
                    };
                    UnlockSL.AddItem(unlock);
                }
            }
            foreach (Technology.UnlockedBonus UnlockedBonus in unlockedTech.BonusUnlocked)
            {
                if (EmpireManager.Player.data.Traits.ShipType == UnlockedBonus.Type || UnlockedBonus.Type == null || UnlockedBonus.Type == EmpireManager.Player.GetTDict()[TechUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = UnlockType.ADVANCE,
                        privateName = UnlockedBonus.Name,
                        Description = Localizer.Token(UnlockedBonus.BonusIndex)
                    };
                    UnlockSL.AddItem(unlock);
                }
            }
        }
    }
}