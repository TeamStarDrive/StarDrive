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
        ScrollList2<UnlockListItem> UnlockSL;
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

        class UnlockListItem : ScrollListItem<UnlockListItem>
        {
            readonly UnlockItem Unlock;
            public UnlockListItem(UnlockItem unlock)
            {
                Unlock = unlock;
            }

            void DrawTitleAndDescr(SpriteBatch batch, string title, string descr, string comment = "", string summary = "")
            {
                string wrappedDescr = Fonts.Arial12.ParseText(descr, Width - 100);
                float textHeight = Fonts.Arial14Bold.LineSpacing + 5 + Fonts.Arial12.MeasureString(wrappedDescr).Y;
                var pos = new Vector2(X + 100, CenterY - (int)(textHeight / 2f));

                batch.DrawDropShadowText(title, pos, Fonts.Arial14Bold, Color.Orange);
                if (comment.NotEmpty())
                {
                    var commentPos = Fonts.Arial14Bold.MeasureString(title);
                    commentPos.X  += pos.X;
                    commentPos.Y   = pos.Y + 2;
                    batch.DrawString(Fonts.Arial12, comment, commentPos, Color.Gray);
                }

                batch.DrawString(Fonts.Arial12, wrappedDescr, pos + new Vector2(0f, Fonts.Arial14Bold.LineSpacing + 2), Color.LightGray);
                if (summary.NotEmpty())
                {
                    string wrappedSummary = Fonts.Arial12.ParseText(summary, Width - 100);
                    int lines = wrappedDescr.Split('\n').Length + 2;
                    batch.DrawString(Fonts.Arial12, wrappedSummary, pos + new Vector2(0f, Fonts.Arial12.LineSpacing * lines - 3), Color.SteelBlue);
                }

            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                switch (Unlock.Type)
                {
                    case UnlockType.ShipModule:
                    {
                        Rectangle DestinationRect(int width, int height)
                        {
                            return new Rectangle((int)X + 48 - width / 2, (int)CenterY - height / 2, width, height);
                        }

                        int modW = Unlock.module.XSIZE, modH = Unlock.module.YSIZE;
                        Rectangle r = DestinationRect(64, 64);
                        if (modH > modW)
                        {
                            float ratio = (float)modW / modH * Height;
                            r = DestinationRect((int)ratio, (int)Height);
                        }
                        else if (modW > modH)
                        {
                            float ratio = (float)modH / modW * (Height-20);
                            r = DestinationRect((int)(Height-20), (int)ratio);
                        }

                        batch.Draw(Unlock.module.ModuleTexture, r, Color.White);
                        string size = $" ({modW}x{modH})";
                        DrawTitleAndDescr(batch, Unlock.privateName, Unlock.Description, size);
                        break;
                    }
                    case UnlockType.Troop:
                    {
                        var r = new Rectangle((int)X + 16, (int)CenterY - 32, 64, 64);
                        Unlock.troop.Draw(batch, r);

                        DrawTitleAndDescr(batch, Unlock.troop.Name, Unlock.troop.Description);
                        break;
                    }
                    case UnlockType.Building:
                    {
                        var r = new Rectangle((int)X + 16, (int)CenterY - 32, 64, 64);
                        batch.Draw(ResourceManager.Texture($"Buildings/icon_{Unlock.building.Icon}_64x64"), r, Color.White);

                        string title   = new LocalizedText(Unlock.building.NameTranslationIndex).Text;
                        string descr   = new LocalizedText(Unlock.building.DescriptionIndex).Text;
                        string summary = new LocalizedText(Unlock.building.ShortDescriptionIndex).Text;
                        DrawTitleAndDescr(batch, title, descr, summary: summary);
                        break;
                    }
                    case UnlockType.Hull:
                    {
                        if (ResourceManager.Hull(Unlock.privateName, out ShipData hull))
                        {
                            var r = new Rectangle((int)X, (int)CenterY - 32, 96, 96);
                            batch.Draw(hull.Icon, r, Color.White);
                            DrawTitleAndDescr(batch, Unlock.HullUnlocked, Unlock.Description);
                        }
                        break;
                    }
                    case UnlockType.Advance:
                    {
                        var r = new Rectangle((int)X + 24, (int)Y + 24, 48, 48);
                        batch.Draw(ResourceManager.Texture("TechIcons/star"), r, Color.White);
                        DrawTitleAndDescr(batch, Unlock.privateName, Unlock.Description);
                        break;
                    }
                }
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (fade) ScreenManager.FadeBackBufferToBlack((TransitionAlpha * 2) / 3);

            base.Draw(batch, elapsed);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var rect = new Rectangle(MidContainer.X + 20, 
                                     MidContainer.Y + MidContainer.Height - 20, 
                                     Rect.Width - 40, 
                                     Rect.Height - MidContainer.Height - TitleRect.Height - 20);
            UnlockSL = Add(new ScrollList2<UnlockListItem>(rect, 100));

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
                        Type = UnlockType.ShipModule,
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
                        Type = UnlockType.Troop,
                        troop = ResourceManager.GetTroopTemplate(troop.Name)
                    };
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
            foreach (Technology.UnlockedHull hull in Technology.HullsUnlocked)
            {
                if (IsShipType(hull.ShipType) && ResourceManager.Hull(hull.Name, out ShipData hullData))
                {
                    var unlock = new UnlockItem
                    {
                        Type = UnlockType.Hull,
                        privateName = hull.Name,
                        HullUnlocked = hullData.Name
                    };
                    unlock.Description = Localizer.Token(4042) + " " +
                                         Localizer.GetRole(hullData.Role, EmpireManager.Player);
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
            foreach (Technology.UnlockedBuilding building in Technology.BuildingsUnlocked)
            {
                if (IsShipType(building.Type))
                {
                    var unlock = new UnlockItem
                    {
                        Type = UnlockType.Building,
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
                        Type = UnlockType.Advance,
                        privateName = bonus.Name,
                        Description = Localizer.Token(bonus.BonusIndex)
                    };
                    UnlockSL.AddItem(new UnlockListItem(unlock));
                }
            }
        }
    }
}