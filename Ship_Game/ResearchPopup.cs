using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private readonly Technology Technology;
        
        public ResearchPopup(UniverseScreen s, string uid) : base(s, 600, 600)
        {
            TechUID = uid;
            fade = true;
            IsPopup = true;
            FromGame = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0f;
            TechEntry techEntry = EmpireManager.Player.GetTechEntry(uid);
            if (techEntry == null)
                return;

            Technology = ResourceManager.Tech(uid);

            string level = RomanNumerals.ToRoman(techEntry.Level);
            string maxlvl = RomanNumerals.ToRoman(techEntry.MaxLevel);
            TitleText  = Localizer.Token(Technology.NameIndex) + (Technology.MaxLevel > 1 ? $" {level}/{maxlvl}" : "");
            MiddleText = Localizer.Token(techEntry.Tech.DescriptionIndex);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (fade) ScreenManager.FadeBackBufferToBlack((TransitionAlpha * 2) / 3);

            base.Draw(batch);

            batch.Begin();
            UnlockSL.Draw(batch);

            foreach (ScrollList.Entry entry in UnlockSL.VisibleExpandedEntries)
            {
                void DrawTitleAndDescr(string title, string descr)
                {
                    string wrappedDescr = Fonts.Arial12.ParseText(descr, entry.W - 100);
                    float textHeight = Fonts.Arial14Bold.LineSpacing + 5 + Fonts.Arial12.MeasureString(wrappedDescr).Y;
                    var pos = new Vector2(entry.X + 100, entry.CenterY - (int)(textHeight / 2f));

                    HelperFunctions.DrawDropShadowText(batch, title, pos, Fonts.Arial14Bold, Color.Orange);
                    batch.DrawString(Fonts.Arial12, wrappedDescr, pos + new Vector2(0f, Fonts.Arial14Bold.LineSpacing + 2), Color.LightGray);
                }

                var unlockItem = (UnlockItem)entry.item;
                switch (unlockItem.Type)
                {
                    case UnlockType.SHIPMODULE:
                    {
                        Rectangle DestinationRect(int width, int height)
                        {
                            return new Rectangle(entry.X + 48 - width / 2, entry.CenterY - height / 2, width, height);
                        }

                        int modW = unlockItem.module.XSIZE, modH = unlockItem.module.YSIZE;
                        Rectangle r = DestinationRect(64, 64);
                        if (modW != 1 || modH != 1)
                        {
                            r = DestinationRect(modW * 16, modH * 16);
                            while (r.Height < entry.H) r = DestinationRect(r.Width + modW, r.Height + modH);
                            while (r.Height > entry.H) r = DestinationRect(r.Width - modW, r.Height - modH);                                    
                        }
                        batch.Draw(ResourceManager.Texture(unlockItem.module.IconTexturePath), r, Color.White);

                        DrawTitleAndDescr(unlockItem.privateName, unlockItem.Description);
                        break;
                    }
                    case UnlockType.TROOP:
                    {
                        var r = new Rectangle(UnlocksRect.X + 16, entry.CenterY - 32, 64, 64);
                        unlockItem.troop.Draw(batch, r);

                        DrawTitleAndDescr(unlockItem.troop.Name, unlockItem.troop.Description);
                        break;
                    }
                    case UnlockType.BUILDING:
                    {
                        var r = new Rectangle(UnlocksRect.X + 16, entry.CenterY - 32, 64, 64);
                        batch.Draw(ResourceManager.Texture($"Buildings/icon_{unlockItem.building.Icon}_64x64"), r, Color.White);

                        string title = Localizer.Token(unlockItem.building.NameTranslationIndex);
                        string descr = Localizer.Token(unlockItem.building.DescriptionIndex);
                        DrawTitleAndDescr(title, descr);
                        break;
                    }
                    case UnlockType.HULL:
                    {
                        var r = new Rectangle(UnlocksRect.X, entry.Y, 96, 96);
                        batch.Draw(ResourceManager.Hull(unlockItem.privateName).Icon, r, Color.White);
                        DrawTitleAndDescr(unlockItem.HullUnlocked, unlockItem.Description);
                        break;
                    }
                    case UnlockType.ADVANCE:
                    {
                        var r = new Rectangle(UnlocksRect.X + 24, entry.Y + 24, 48, 48);
                        batch.Draw(ResourceManager.Texture("TechIcons/star"), r, Color.White);
                        DrawTitleAndDescr(unlockItem.privateName, unlockItem.Description);
                        break;
                    }
                }
            }
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            UnlockSL.HandleInput(input);
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            UnlocksRect = new Rectangle(MidContainer.X + 20, 
                                        MidContainer.Y + MidContainer.Height - 20, 
                                        Rect.Width - 40, 
                                        Rect.Height - MidContainer.Height - TitleRect.Height - 20);
            UnlockSL = new ScrollList(new Submenu(UnlocksRect), 100);

            // @todo What is this?
            bool IsShipType(string type)
            {
                return type == null
                    || type == "ALL"
                    || type == EmpireManager.Player.data.Traits.ShipType
                    || type == EmpireManager.Player.AcquiredFrom(TechUID);
            }

            foreach (Technology.UnlockedMod module in Technology.ModulesUnlocked)
            {
                if (!IsShipType(module.Type))
                    continue;
                ShipModule template = ResourceManager.GetModuleTemplate(module.ModuleUID);
                var unlock = new UnlockItem
                {
                    Type = UnlockType.SHIPMODULE,
                    module = template,
                    Description = Localizer.Token(template.DescriptionIndex),
                    privateName = Localizer.Token(template.NameIndex)
                };
                UnlockSL.AddItem(unlock);
            }
            foreach (Technology.UnlockedTroop troop in Technology.TroopsUnlocked)
            {
                if (!IsShipType(troop.Type))
                    continue;
                var unlock = new UnlockItem
                {
                    Type = UnlockType.TROOP,
                    troop = ResourceManager.GetTroopTemplate(troop.Name)
                };
                UnlockSL.AddItem(unlock);
            }
            foreach (Technology.UnlockedHull hull in Technology.HullsUnlocked)
            {
                if (!IsShipType(hull.ShipType))
                    continue;
                var unlock = new UnlockItem
                {
                    Type = UnlockType.HULL,
                    privateName = hull.Name,
                    HullUnlocked = ResourceManager.Hull(hull.Name).Name
                };
                unlock.Description = Localizer.Token(4042) + " " + Localizer.GetRole(ResourceManager.Hull(hull.Name).Role, EmpireManager.Player);
                UnlockSL.AddItem(unlock);
            }
            foreach (Technology.UnlockedBuilding building in Technology.BuildingsUnlocked)
            {
                if (!IsShipType(building.Type))
                    continue;
                var unlock = new UnlockItem
                {
                    Type = UnlockType.BUILDING,
                    building = ResourceManager.GetBuildingTemplate(building.Name)
                };
                UnlockSL.AddItem(unlock);
            }
            foreach (Technology.UnlockedBonus bonus in Technology.BonusUnlocked)
            {
                if (!IsShipType(bonus.Type))
                    continue;
                var unlock = new UnlockItem
                {
                    Type = UnlockType.ADVANCE,
                    privateName = bonus.Name,
                    Description = Localizer.Token(bonus.BonusIndex)
                };
                UnlockSL.AddItem(unlock);
            }
        }
    }
}