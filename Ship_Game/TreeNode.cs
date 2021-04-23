using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using System.Text.RegularExpressions;

namespace Ship_Game
{
    public sealed class TreeNode : Node
    {
        //public Graphics.Font TitleFont = Fonts.Visitor10;
        public Graphics.Font TitleFont = Fonts.Tahoma10;

        public NodeState State;

        public ResearchScreenNew Screen;

        public string TechName;

        public Rectangle TitleRect;

        public Rectangle BaseRect = new Rectangle(0, 0, 92, 98);
        public Vector2 RightPoint => new Vector2(BaseRect.Right - 25,
                                                 BaseRect.CenterY() - 10);

        public bool complete;

        Rectangle IconRect;

        Rectangle UnlocksRect;

        Array<UnlockItem> Unlocks = new Array<UnlockItem>();

        UnlocksGrid grid;

        Rectangle progressRect;

        float TitleWidth = 73f;

        Vector2 CostPos;
        readonly Technology TechTemplate;
        Rectangle PlusRect;

        public TreeNode(Vector2 position, TechEntry theEntry, ResearchScreenNew screen)
        {
            if (GlobalStats.IsRussian)
            {
                TitleFont = Fonts.Arial10;
            }

            Screen   = screen;
            Entry    = theEntry;
            TechName = Localizer.Token(ResourceManager.TechTree[theEntry.UID].NameIndex)+(ResourceManager.TechTree[theEntry.UID].MaxLevel > 1
                           ? " " + RomanNumerals.ToRoman(theEntry.Level) + "/"
                             + RomanNumerals.ToRoman(ResourceManager.TechTree[theEntry.UID].MaxLevel) : "");

            BaseRect.X     = (int)position.X;
            BaseRect.Y     = (int)position.Y;
            progressRect   = new Rectangle(BaseRect.X + 14, BaseRect.Y + 21, 1, 34);
            int numUnlocks = 0;
            TechTemplate   = ResourceManager.TechTree[Entry.UID];

            for (int i = 0; i < TechTemplate.ModulesUnlocked.Count; i++)
            {
                if (numUnlocks > 3) break;
                if (TechTemplate.ModulesUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType ||
                    TechTemplate.ModulesUnlocked[i].Type == null ||
                    EmpireManager.Player.AcquiredFrom(Entry).Contains(TechTemplate.ModulesUnlocked[i].Type))
                {
                    UnlockItem unlock  = new UnlockItem();
                    unlock.module      = ResourceManager.GetModuleTemplate(TechTemplate.ModulesUnlocked[i].ModuleUID);
                    unlock.privateName = Localizer.Token(unlock.module.NameIndex);
                    unlock.Description = Localizer.Token(unlock.module.DescriptionIndex);
                    unlock.Type        = UnlockType.ShipModule;
                    Unlocks.Add(unlock);
                    numUnlocks++;
                }
            }
            for (int i = 0; i < TechTemplate.BonusUnlocked.Count; i++)
            {
                if (numUnlocks > 3) break;
                if (TechTemplate.BonusUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType ||
                    TechTemplate.BonusUnlocked[i].Type == null ||
                    EmpireManager.Player.AcquiredFrom(Entry).Contains(TechTemplate.BonusUnlocked[i].Type))
                {
                    UnlockItem unlock = new UnlockItem
                    {
                        privateName = TechTemplate.BonusUnlocked[i].Name,
                        Description = Localizer.Token(TechTemplate.BonusUnlocked[i].BonusIndex),
                        Type = UnlockType.Advance
                    };
                    numUnlocks++;
                    Unlocks.Add(unlock);
                }
            }
            for (int i = 0; i < TechTemplate.BuildingsUnlocked.Count; i++)
            {
                if (numUnlocks > 3) break;
                if (TechTemplate.BuildingsUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType ||
                    TechTemplate.BuildingsUnlocked[i].Type == null ||
                    EmpireManager.Player.AcquiredFrom(Entry).Contains(TechTemplate.BuildingsUnlocked[i].Type))
                {
                    UnlockItem unlock = new UnlockItem();
                    unlock.building = ResourceManager.BuildingsDict[TechTemplate.BuildingsUnlocked[i].Name];
                    unlock.privateName = unlock.building.TranslatedName.Text;
                    unlock.Description = unlock.building.DescriptionText.Text;
                    unlock.Type = UnlockType.Building;
                    numUnlocks++;
                    Unlocks.Add(unlock);
                }
            }
            for (int i = 0; i < TechTemplate.HullsUnlocked.Count; i++)
            {
                if (numUnlocks > 3) break;
                if (TechTemplate.HullsUnlocked[i].ShipType == EmpireManager.Player.data.Traits.ShipType ||
                    TechTemplate.HullsUnlocked[i].ShipType == null ||
                    EmpireManager.Player.AcquiredFrom(Entry).Contains(TechTemplate.HullsUnlocked[i].ShipType))
                {
                    UnlockItem unlock = new UnlockItem
                    {
                        HullUnlocked = TechTemplate.HullsUnlocked[i].Name,
                        privateName = TechTemplate.HullsUnlocked[i].Name,
                        Description = "",
                        Type = UnlockType.Hull
                    };
                    numUnlocks++;
                    Unlocks.Add(unlock);
                }
            }
            for (int i = 0; i < TechTemplate.TroopsUnlocked.Count; i++)
            {
                if (numUnlocks > 3) break;
                if (TechTemplate.TroopsUnlocked[i].Type == EmpireManager.Player.data.Traits.ShipType ||
                    TechTemplate.TroopsUnlocked[i].Type == "ALL" ||
                    TechTemplate.TroopsUnlocked[i].Type == null ||
                    EmpireManager.Player.AcquiredFrom(Entry).Contains(TechTemplate.TroopsUnlocked[i].Type))
                {
                    UnlockItem unlock = new UnlockItem();
                    unlock.troop       = ResourceManager.GetTroopTemplate(TechTemplate.TroopsUnlocked[i].Name);
                    unlock.privateName = TechTemplate.TroopsUnlocked[i].Name;
                    unlock.Description = unlock.troop.Description;
                    unlock.Type        = UnlockType.Troop;
                    numUnlocks++;
                    Unlocks.Add(unlock);
                }
            }
            int numColumns = numUnlocks / 2 + numUnlocks % 2;
            IconRect = new Rectangle(BaseRect.X + BaseRect.Width / 2 - 29, BaseRect.Y + BaseRect.Height / 2 - 24 - 10, 58, 49);
            if (numUnlocks <= 1)
            {
                UnlocksRect = new Rectangle(IconRect.X + IconRect.Width, IconRect.Y + IconRect.Height - 5, 35, 32);

                UnlocksRect.Y = UnlocksRect.Y - UnlocksRect.Height;

                Rectangle drawRect = UnlocksRect;
                drawRect.X = drawRect.X + 3;
                grid = new UnlocksGrid(Unlocks, drawRect);
            }
            else
            {
                UnlocksRect = new Rectangle(IconRect.X + IconRect.Width, IconRect.Y + IconRect.Height - 5, 13 + numColumns * 32, (numUnlocks == 1 ? 32 : 64));
                UnlocksRect.Y = UnlocksRect.Y - UnlocksRect.Height;

                Rectangle drawRect = UnlocksRect;
                drawRect.X = drawRect.X + 13;
                grid = new UnlocksGrid(Unlocks, drawRect);
            }
            UnlocksRect.X = UnlocksRect.X - 2;
            UnlocksRect.Width = UnlocksRect.Width + 4;
            UnlocksRect.Y = UnlocksRect.Y - 2;
            UnlocksRect.Height = UnlocksRect.Height + 4;
            TitleRect = new Rectangle(BaseRect.X, BaseRect.Y - 20, 90, 36);
            CostPos = new Vector2(62f, 70f) + new Vector2(BaseRect.X, BaseRect.Y);
            float x = CostPos.X;
            Graphics.Font titleFont = TitleFont;
            float cost = Entry.TechCost;
            CostPos.X = x - TitleFont.MeasureString(cost.GetNumberString()).X;
            CostPos.X = (int)CostPos.X;
            CostPos.Y = (int)CostPos.Y - 3;

            complete = EmpireManager.Player.HasUnlocked(Entry);
        }

        SubTexture TechIcon
        {
            get
            {
                string iconPath = Entry.Tech.IconPath;
                if (iconPath == null)
                    return ResourceManager.Texture("TechIcons/" + Entry.UID);
                return ResourceManager.TextureOrDefault("TechIcons/" + iconPath, "TechIcons/" + Entry.UID);
            }
        }

        public void Draw(SpriteBatch batch)
        {
            if (complete)
            {
                DrawGlow(batch, Entry.Tech.Secret ? Color.Green : Color.White );
            }
            switch (State)
            {
                case NodeState.Normal:
                    string texSuffix = "";
                    Color color = Color.Black;
                    if (complete)
                    {
                        texSuffix = "_complete";
                        color = new Color(34, 136, 200);
                    }
                    else if (EmpireManager.Player.Research.IsQueued(Entry.UID))
                    {
                        texSuffix = "_queue";
                        color = Color.Teal;
                    }

                    bool active = complete || EmpireManager.Player.Research.IsQueued(Entry.UID);
                    batch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
                    batch.DrawRectangle(UnlocksRect, color);
                    grid.Draw(batch);
                    batch.Draw(ResourceManager.Texture($"NewUI/new_tech_base{texSuffix}"), BaseRect, Color.White);
                    //Added by McShooterz: Allows non root techs to use IconPath
                    batch.Draw(TechIcon, IconRect, Color.White);
                    batch.Draw(ResourceManager.Texture($"NewUI/new_tech_base_title{texSuffix}"), TitleRect, Color.White);
                    string str1 = TitleFont.ParseText(TechName, TitleWidth);
                    string[] strArray1 = Regex.Split(str1, "\n");
                    Vector2 vector2_1 = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(str1).X / 2f, TitleRect.Y + 18 - TitleFont.MeasureString(str1).Y / 2f);
                    int num1 = 0;
                    foreach (string text in strArray1)
                    {
                        Vector2 position = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(text).X / 2f, vector2_1.Y + num1 * TitleFont.LineSpacing);
                        position = new Vector2((int)position.X, (int)position.Y);
                        batch.DrawString(TitleFont, text, position, complete ? new Color(132, 172, 208) : Color.White);
                        ++num1;
                    }
                    int num2 = (int)(progressRect.Height - EmpireManager.Player.TechProgress(Entry) / EmpireManager.Player.TechCost(Entry) * (double)progressRect.Height);
                    Rectangle destinationRectangle1 = progressRect;
                    destinationRectangle1.Height = num2;
                    batch.Draw(active ? ResourceManager.Texture("ResearchMenu/tech_progress")
                                      : ResourceManager.Texture("ResearchMenu/tech_progress_inactive"), progressRect, Color.White);
                    batch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle1, Color.White);
                    break;
                case NodeState.Hover:
                    batch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
                    batch.DrawRectangle(UnlocksRect, new Color(190, 113, 25));
                    grid.Draw(batch);
                    batch.Draw(ResourceManager.Texture("NewUI/new_tech_base_hover"), BaseRect, Color.White);
                    batch.Draw(TechIcon, IconRect, Color.White);
                    batch.Draw(ResourceManager.Texture("NewUI/new_tech_base_title_hover"), TitleRect, Color.White);
                    string str2 = TitleFont.ParseText(TechName, TitleWidth);
                    string[] strArray2 = Regex.Split(str2, "\n");
                    Vector2 vector2_2 = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(str2).X / 2f, TitleRect.Y + 18 - TitleFont.MeasureString(str2).Y / 2f);
                    int num3 = 0;
                    foreach (string text in strArray2)
                    {
                        Vector2 position = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(text).X / 2f, vector2_2.Y + num3 * TitleFont.LineSpacing);
                        position = new Vector2((int)position.X, (int)position.Y);
                        batch.DrawString(TitleFont, text, position, complete ? new Color(132, 172, 208) : Color.White);
                        ++num3;
                    }
                    int num4 = (int)(progressRect.Height - EmpireManager.Player.TechProgress(Entry) / ResourceManager.Tech(Entry.UID).ActualCost * (double)progressRect.Height);
                    Rectangle destinationRectangle2 = progressRect;
                    destinationRectangle2.Height = num4;
                    batch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress"), progressRect, Color.White);
                    batch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle2, Color.White);
                    break;
                case NodeState.Press:
                    batch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
                    batch.DrawRectangle(UnlocksRect, new Color(190, 113, 25));
                    grid.Draw(batch);
                    batch.Draw(ResourceManager.Texture("NewUI/new_tech_base_hover"), BaseRect, Color.White);
                    batch.Draw(TechIcon, IconRect, Color.White);
                    batch.Draw(ResourceManager.Texture("NewUI/new_tech_base_title_hover"), TitleRect, Color.White);
                    string str3 = TitleFont.ParseText(TechName, TitleWidth);
                    string[] strArray3 = Regex.Split(str3, "\n");
                    Vector2 vector2_3 = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(str3).X / 2f, TitleRect.Y + 18 - TitleFont.MeasureString(str3).Y / 2f);
                    int num5 = 0;
                    foreach (string text in strArray3)
                    {
                        Vector2 position = new Vector2(TitleRect.X + TitleRect.Width / 2 - TitleFont.MeasureString(text).X / 2f, vector2_3.Y + num5 * TitleFont.LineSpacing);
                        position = new Vector2((int)position.X, (int)position.Y);
                        batch.DrawString(TitleFont, text, position, complete ? new Color(163, 198, 236) : Color.White);
                        ++num5;
                    }
                    int num6 = (int)(progressRect.Height - EmpireManager.Player.TechProgress(Entry) / EmpireManager.Player.TechCost(Entry) * (double)progressRect.Height);
                    Rectangle destinationRectangle3 = progressRect;
                    destinationRectangle3.Height = num6;
                    batch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress"), progressRect, Color.White);
                    batch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), destinationRectangle3, Color.White);
                    break;
            }
            var techCost = Entry.TechCost;
            if (!Entry.Unlocked) techCost -= Entry.Progress;
            batch.DrawString(TitleFont, techCost.GetNumberString(), CostPos, Color.SkyBlue);
            if (TechTemplate.NumStuffUnlocked > 4)
            { 
                PlusRect = new Rectangle(UnlocksRect.X + 60, UnlocksRect.Y + UnlocksRect.Height, 20, 20);
                batch.DrawString(Fonts.Arial20Bold, "+", new Vector2(PlusRect.X, PlusRect.Y), Color.Orange);
            }
        }

        public void DrawGlow(SpriteBatch batch)
        {
            DrawGlow(batch, Color.White);
        }

        public void DrawGlow(SpriteBatch batch, Color color)
        {
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_base"), BaseRect, color);
            batch.DrawRectangleGlow(TitleRect);
            batch.DrawRectangleGlow(UnlocksRect);
        }

        public bool HandleInput(InputState input, ScreenManager ScreenManager, Camera2D camera)
        {
            Vector2 RectPos = camera.GetScreenSpaceFromWorldSpace(new Vector2(BaseRect.X, BaseRect.Y));
            Rectangle moddedRect = new Rectangle((int)RectPos.X, (int)RectPos.Y, BaseRect.Width, BaseRect.Height);
            Vector2 RectPos2 = camera.GetScreenSpaceFromWorldSpace(new Vector2(UnlocksRect.X, UnlocksRect.Y));
            Rectangle moddedRect2 = new Rectangle((int)RectPos2.X, (int)RectPos2.Y, UnlocksRect.Width, UnlocksRect.Height);
            Vector2 RectPos3 = camera.GetScreenSpaceFromWorldSpace(new Vector2(IconRect.X, IconRect.Y));
            Rectangle moddedRect3 = new Rectangle((int)RectPos3.X, (int)RectPos3.Y, IconRect.Width, IconRect.Height);

            if (moddedRect.HitTest(input.CursorPosition) || moddedRect2.HitTest(input.CursorPosition))
            {
                if (State != NodeState.Hover)
                {
                    GameAudio.MouseOver();
                }
                State = NodeState.Hover;
                if (input.InGameSelect)
                {
                    State = NodeState.Press;
                    return true;
                }
                if (input.RightMouseClick)
                {
                    Screen.RightClicked = true;
                    ScreenManager.AddScreen(new ResearchPopup(Empire.Universe, Entry.UID));
                    return true; // input captured
                }
            }
            else
            {
                State = NodeState.Normal;
            }

            Vector2 plusPos = camera.GetScreenSpaceFromWorldSpace(new Vector2(PlusRect.X, PlusRect.Y));
            Rectangle moddedPlusRect = new Rectangle((int)plusPos.X, (int)plusPos.Y, PlusRect.Width, PlusRect.Height);
            if (moddedPlusRect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip("This Technology unlocks more than 4 items. Right Click on the title to Expand");
                return false;
            }

            if (!moddedRect3.HitTest(input.CursorPosition))
            {
                foreach (UnlocksGrid.GridItem gi in grid.GridOfUnlocks)
                {
                    Vector2 rectPos4 = camera.GetScreenSpaceFromWorldSpace(gi.Pos);
                    var moddedRect4 = new Rectangle((int)rectPos4.X, (int)rectPos4.Y, gi.rect.Width, gi.rect.Height);
                    if (moddedRect4.HitTest(input.CursorPosition))
                    {
                        string tip = string.Concat(gi.item.privateName, "\n\n", gi.item.Description);
                        if (gi.item.HullUnlocked == null)
                        {
                            ToolTip.CreateTooltip(tip);
                        }
                        else if (ResourceManager.Hull(gi.item.HullUnlocked, out ShipData unlocked))
                        {
                            ToolTip.CreateTooltip($"{unlocked.Name} ({Localizer.GetRole(unlocked.Role, EmpireManager.Player)})");
                        }
                    }
                }
            }
            else
            {
                ToolTip.CreateTooltip($"Right Click to Expand \n\n{Localizer.Token(ResourceManager.TechTree[Entry.UID].DescriptionIndex)}");
            }
            return false;
        }
    }
}