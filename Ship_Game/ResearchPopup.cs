using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ResearchPopup : PopupWindow
    {
        public bool fade;
        public bool FromGame;
        public string TechUID;
        ScrollList<UnlockListItem> UnlockSL;
        readonly Technology Technology;
        
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

        class UnlockListItem : ScrollListEntry<UnlockListItem>
        {
            readonly UnlockItem Unlock;
            public UnlockListItem(UnlockItem unlock)
            {
                Unlock = unlock;
            }
            void DrawTitleAndDescr(SpriteBatch batch, string title, string descr)
            {
                string wrappedDescr = Fonts.Arial12.ParseText(descr, Width - 100);
                float textHeight = Fonts.Arial14Bold.LineSpacing + 5 + Fonts.Arial12.MeasureString(wrappedDescr).Y;
                var pos = new Vector2(X + 100, CenterY - (int)(textHeight / 2f));

                batch.DrawDropShadowText(title, pos, Fonts.Arial14Bold, Color.Orange);
                batch.DrawString(Fonts.Arial12, wrappedDescr, pos + new Vector2(0f, Fonts.Arial14Bold.LineSpacing + 2), Color.LightGray);
            }
            public override void Draw(SpriteBatch batch)
            {
                switch (Unlock.Type)
                {
                    case UnlockType.SHIPMODULE:
                    {
                        Rectangle DestinationRect(int width, int height)
                        {
                            return new Rectangle((int)X + 48 - width / 2, (int)CenterY - height / 2, width, height);
                        }

                        int modW = Unlock.module.XSIZE, modH = Unlock.module.YSIZE;
                        Rectangle r = DestinationRect(64, 64);
                        if (modW != 1 || modH != 1)
                        {
                            r = DestinationRect(modW * 16, modH * 16);
                            while (r.Height < Height) r = DestinationRect(r.Width + modW, r.Height + modH);
                            while (r.Height > Height) r = DestinationRect(r.Width - modW, r.Height - modH);                                    
                        }
                        batch.Draw(ResourceManager.Texture(Unlock.module.IconTexturePath), r, Color.White);

                        DrawTitleAndDescr(batch, Unlock.privateName, Unlock.Description);
                        break;
                    }
                    case UnlockType.TROOP:
                    {
                        var r = new Rectangle((int)X + 16, (int)CenterY - 32, 64, 64);
                        Unlock.troop.Draw(batch, r);

                        DrawTitleAndDescr(batch, Unlock.troop.Name, Unlock.troop.Description);
                        break;
                    }
                    case UnlockType.BUILDING:
                    {
                        var r = new Rectangle((int)X + 16, (int)CenterY - 32, 64, 64);
                        batch.Draw(ResourceManager.Texture($"Buildings/icon_{Unlock.building.Icon}_64x64"), r, Color.White);

                        string title = Localizer.Token(Unlock.building.NameTranslationIndex);
                        string descr = Localizer.Token(Unlock.building.DescriptionIndex);
                        DrawTitleAndDescr(batch, title, descr);
                        break;
                    }
                    case UnlockType.HULL:
                    {
                        var r = new Rectangle((int)X, (int)Y, 96, 96);
                        batch.Draw(ResourceManager.Hull(Unlock.privateName).Icon, r, Color.White);
                        DrawTitleAndDescr(batch, Unlock.HullUnlocked, Unlock.Description);
                        break;
                    }
                    case UnlockType.ADVANCE:
                    {
                        var r = new Rectangle((int)X + 24, (int)Y + 24, 48, 48);
                        batch.Draw(ResourceManager.Texture("TechIcons/star"), r, Color.White);
                        DrawTitleAndDescr(batch, Unlock.privateName, Unlock.Description);
                        break;
                    }
                }
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            if (fade) ScreenManager.FadeBackBufferToBlack((TransitionAlpha * 2) / 3);

            base.Draw(batch);

            batch.Begin();
            UnlockSL.Draw(batch);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            return UnlockSL.HandleInput(input) && base.HandleInput(input);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var rect = new Rectangle(MidContainer.X + 20, 
                                    MidContainer.Y + MidContainer.Height - 20, 
                                    Rect.Width - 40, 
                                    Rect.Height - MidContainer.Height - TitleRect.Height - 20);
            UnlockSL = new ScrollList<UnlockListItem>(new Submenu(rect), 100);

            // @todo What is this?
            bool IsShipType(string type)
            {
                return type == null
                    || type == "ALL"
                    || type == EmpireManager.Player.data.Traits.ShipType
                    || EmpireManager.Player.AcquiredFrom(TechUID).Contains(type);
            }

            foreach (Technology.UnlockedMod module in Technology.ModulesUnlocked)
            {
                if (IsShipType(module.Type))
                {
                    ShipModule template = ResourceManager.GetModuleTemplate(module.ModuleUID);
                    var unlock = new UnlockItem
                    {
                        Type = UnlockType.SHIPMODULE,
                        module = template,
                        Description = Localizer.Token(template.DescriptionIndex),
                        privateName = Localizer.Token(template.NameIndex)
                    };
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
            foreach (Technology.UnlockedTroop troop in Technology.TroopsUnlocked)
            {
                if (IsShipType(troop.Type))
                {
                    var unlock = new UnlockItem
                    {
                        Type = UnlockType.TROOP,
                        troop = ResourceManager.GetTroopTemplate(troop.Name)
                    };
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
            foreach (Technology.UnlockedHull hull in Technology.HullsUnlocked)
            {
                if (IsShipType(hull.ShipType))
                {
                    var unlock = new UnlockItem
                    {
                        Type = UnlockType.HULL,
                        privateName = hull.Name,
                        HullUnlocked = ResourceManager.Hull(hull.Name).Name
                    };
                    unlock.Description = Localizer.Token(4042) + " " +
                                         Localizer.GetRole(ResourceManager.Hull(hull.Name).Role, EmpireManager.Player);
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
            foreach (Technology.UnlockedBuilding building in Technology.BuildingsUnlocked)
            {
                if (IsShipType(building.Type))
                {
                    var unlock = new UnlockItem
                    {
                        Type = UnlockType.BUILDING,
                        building = ResourceManager.GetBuildingTemplate(building.Name)
                    };
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
            foreach (Technology.UnlockedBonus bonus in Technology.BonusUnlocked)
            {
                if (IsShipType(bonus.Type))
                {
                    var unlock = new UnlockItem
                    {
                        Type = UnlockType.ADVANCE,
                        privateName = bonus.Name,
                        Description = Localizer.Token(bonus.BonusIndex)
                    };
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
        }
    }
}