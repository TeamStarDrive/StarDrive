using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class TreeNode : Node
    {
        public Graphics.Font TitleFont = Fonts.Tahoma10; // Fonts.Visitor10;
        public NodeState State;
        public ResearchScreenNew Screen;
        public string TechName;
        public RectF TitleRect;
        public RectF BaseRect = new(0, 0, 105, 98);
        public Vector2 RightPoint => new(BaseRect.Right - 25, BaseRect.CenterY - 10);
        public bool Complete;
        RectF IconRect;
        RectF UnlocksRect;
        RectF MultiLevelRect;
        readonly Array<UnlockItem> UnlocksGridItems;
        UnlocksGrid UnlocksGrid;
        RectF ProgressRect;
        readonly float TitleWidth = 73f;
        readonly Color MultiLevelColor = new Color(210, 155, 255);

        readonly Technology TechTemplate;
        RectF PlusRect;

        const int MaxUnlockItems = 4;

        public TreeNode(Vector2 pos, TechEntry theEntry, ResearchScreenNew screen)
        {
            if (GlobalStats.IsRussian)
                TitleFont = Fonts.Arial10;

            Screen = screen;
            Entry = theEntry;
            Technology tech = ResourceManager.Tech(theEntry.UID);
            TechName = tech.Name.Text;
            TechTemplate = ResourceManager.Tech(Entry.UID);
            Complete = Entry.Unlocked;
            UnlocksGridItems = UnlockItem.CreateUnlocksList(TechTemplate, Screen.Player, maxUnlocks: MaxUnlockItems);
            SetPos(pos);
        }

        public void SetPos(Vector2 pos)
        {
            BaseRect.X = (int)pos.X;
            BaseRect.Y = (int)pos.Y;
            ProgressRect = new(BaseRect.X + 15, BaseRect.Y + 21, 2, 34);
            IconRect = new(BaseRect.X + BaseRect.W / 2 - 29, BaseRect.Y + BaseRect.H / 2 - 24 - 10, 58, 49);

            int numColumns = UnlocksGridItems.Count / 2 + UnlocksGridItems.Count % 2;
            if (UnlocksGridItems.Count <= 1)
            {
                UnlocksRect = new(IconRect.Right + 7, IconRect.Bottom - 5, 35, 32);
                UnlocksRect.Y -= UnlocksRect.H;
                UnlocksGrid = new UnlocksGrid(UnlocksGridItems, UnlocksRect.Move(3, 0));
            }
            else
            {
                UnlocksRect = new(IconRect.Right, IconRect.Bottom - 5, 13 + numColumns * 32, (UnlocksGridItems.Count == 1 ? 32 : 64));
                UnlocksRect.Y -= UnlocksRect.H;
                UnlocksGrid = new UnlocksGrid(UnlocksGridItems, UnlocksRect.Move(13, 0));
            }
            UnlocksRect = UnlocksRect.Bevel(2);

            TitleRect = new(BaseRect.X, BaseRect.Y - 22, 90, 36);
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
            if (Complete && Entry.MultiLevelComplete)
            {
                DrawGlow(batch, Entry.Tech.Secret ? Color.Green : Color.White);
            }

            bool queued = Screen.Player.Research.IsQueued(Entry.UID);
            bool active = Complete || queued;

            string techBaseRectSuffix = "";
            string progressIcon = "ResearchMenu/tech_progress";
            var unlocksRectBorderColor = new Color(190, 113, 25);
            var completeTitleColor = new Color(57, 116, 143);

            if (State == NodeState.Normal)
            {
                unlocksRectBorderColor = Color.Black;
                if (Complete && !queued)
                {
                    if (!Entry.MultiLevelComplete)
                    {
                        techBaseRectSuffix = "_MultiInProgress";
                        unlocksRectBorderColor = Screen.ApplyCurrentAlphaColor(MultiLevelColor);
                        completeTitleColor = Color.White;
                    }
                    else
                    {
                        techBaseRectSuffix = "_complete";
                        unlocksRectBorderColor = new Color(4, 113, 128);
                    }
                }
                else if (queued)
                {
                    techBaseRectSuffix = "_queue";
                    unlocksRectBorderColor = new Color(94, 239, 255);
                }
                if (!active)
                {
                    progressIcon = "ResearchMenu/tech_progress_inactive";
                }
            }
            else
            {
                techBaseRectSuffix = "_hover";
                completeTitleColor = Color.White;
            }
            if (State == NodeState.Press)
            {
                completeTitleColor = new Color(163, 198, 236);
            }

            batch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
            batch.DrawRectangle(UnlocksRect, unlocksRectBorderColor);

            if (Entry.MaxLevel > 1)
            {
                string multiLevel = RomanNumerals.ToRoman((Entry.Level + 1).UpperBound(Entry.MaxLevel)) + "/" + RomanNumerals.ToRoman(Entry.MaxLevel);
                MultiLevelRect = new(UnlocksRect.X+1, UnlocksRect.Y + 41, TitleFont.TextWidth(multiLevel)+8, 20);
                batch.FillRectangle(MultiLevelRect, new Color(26, 26, 28));
                batch.DrawRectangle(MultiLevelRect, unlocksRectBorderColor);
                Vector2 multiPos = new(MultiLevelRect.X + 5, MultiLevelRect.Y + 2);
                batch.DrawString(TitleFont, multiLevel, multiPos, Complete && Entry.MultiLevelComplete ? completeTitleColor : Color.White);
            }


            UnlocksGrid.Draw(batch);

            Color borderColor = Complete && !Entry.MultiLevelComplete && !queued && State != NodeState.Hover
                ? Screen.ApplyCurrentAlphaColor(MultiLevelColor)
                : Color.White;

            batch.Draw(ResourceManager.Texture($"NewUI/new_tech_base{techBaseRectSuffix}"), BaseRect, borderColor);
            batch.Draw(TechIcon, IconRect, Color.White);
            batch.Draw(ResourceManager.Texture($"NewUI/new_tech_base_title{techBaseRectSuffix}"), TitleRect, borderColor);

            // Draw the Title as multi-line centered text
            // TODO: Maybe Use UILabel MultiLine text with automatic centering??
            string title = TitleFont.ParseText(TechName, TitleWidth);
            string[] titleLines = title.Split('\n');
            float textHeight = titleLines.Length * TitleFont.LineSpacing;
            float textStartY = TitleRect.Y + 18 - textHeight/2f;

            for (int i = 0; i < titleLines.Length; ++i)
            {
                var pos = new Vector2(TitleRect.CenterX - TitleFont.TextWidth(titleLines[i]) * 0.5f,
                                      textStartY + i * TitleFont.LineSpacing);
                batch.DrawString(TitleFont, titleLines[i], pos.Rounded(), Complete && Entry.MultiLevelComplete ? completeTitleColor : Color.White);
            }

            batch.Draw(ResourceManager.Texture(progressIcon), ProgressRect, Color.White);

            int progress = (int)(ProgressRect.H - Entry.PercentResearched * ProgressRect.H);
            var progressRect2 = ProgressRect;
            progressRect2.H = progress;
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), progressRect2, Color.White);

            // draw tech cost if tech can still be research (such as multi-level techs) display the cost

            LocalizedText costText = LocalizedText.None;
            var costFont = TitleFont;
            var costColor = Color.SkyBlue;
            if (Entry.CanBeResearched)
            {
                costText = (Entry.TechCost - Entry.Progress).GetNumberString();
                if (completeTitleColor == MultiLevelColor)
                    costColor = completeTitleColor;
            }
            else if (Entry.Unlocked)
            {
                costText = Entry.TechCost.GetNumberString();
                costColor = Color.Gray;
            }

            if (costText.NotEmpty)
            {
                Vector2 textSize = costFont.MeasureString(costText);
                var costPos = new Vector2(BaseRect.X + BaseRect.W/2 - textSize.X/2 - 2, BaseRect.Y + 67).Rounded();
                batch.DrawString(costFont, costText, costPos, costColor);
                //batch.DrawRectangle(new RectF(costPos, textSize), Color.Red); // for debugging
            }

            // draw an orange + if there are more techs unlocked
            if (TechTemplate.NumStuffUnlocked(Screen.Player) > MaxUnlockItems)
            { 
                PlusRect = new(UnlocksRect.X + 60, UnlocksRect.Y + UnlocksRect.H, 20, 20);
                batch.DrawString(Fonts.Arial20Bold, "+", new Vector2(PlusRect.X, PlusRect.Y), Color.Orange);
            }

            //batch.DrawRectangle(BaseRect, Color.Red); // for debugging
        }

        public void DrawGlow(SpriteBatch batch, Color color)
        {
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_underglow_base"), BaseRect, color);
            batch.DrawRectangleGlow(TitleRect);
            batch.DrawRectangleGlow(UnlocksRect);
        }

        public bool HandleInput(InputState input, ScreenManager ScreenManager, Camera2D camera, UniverseScreen u)
        {
            RectF moddedRect = new(camera.GetScreenSpaceFromWorldSpace(BaseRect.Pos), BaseRect.Size);
            RectF moddedRect2 = new(camera.GetScreenSpaceFromWorldSpace(UnlocksRect.Pos), UnlocksRect.Size);
            RectF moddedRect3 = new(camera.GetScreenSpaceFromWorldSpace(IconRect.Pos), IconRect.Size);

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
                    ScreenManager.AddScreen(new ResearchPopup(u, Entry.UID));
                    return true; // input captured
                }
            }
            else
            {
                State = NodeState.Normal;
            }

            RectF moddedPlusRect = new(camera.GetScreenSpaceFromWorldSpace(PlusRect.Pos), PlusRect.Size);
            if (moddedPlusRect.HitTest(input.CursorPosition))
            {
                ToolTip.CreateTooltip("This Technology unlocks more than 4 items. Right Click on the title to Expand");
                return false;
            }

            if (!moddedRect3.HitTest(input.CursorPosition))
            {
                foreach (UnlocksGrid.GridItem gi in UnlocksGrid.GridOfUnlocks)
                {
                    RectF moddedRect4 = new(camera.GetScreenSpaceFromWorldSpace(gi.Pos), gi.Rect.Size);
                    if (moddedRect4.HitTest(input.CursorPosition))
                    {
                        ShipHull unlocked = gi.Item.hull;
                        ToolTip.CreateTooltip(unlocked == null
                            ? $"{gi.Item.Title}\n\n{gi.Item.Description}"
                            : $"{unlocked.VisibleName} ({Localizer.GetRole(unlocked.Role, Screen.Player)})");
                    }
                }
            }
            else
            {
                string text = $"Right Click to Expand\nCtrl Left Click to move or insert at topmost possible place in queue.\n\n{Entry.Tech.Description.Text}";
                if (Complete && !Entry.MultiLevelComplete && !Screen.Player.Research.IsQueued(Entry.UID))
                    text = $"Left Click to research level {Entry.Level+1} of this tech.\n\n{text}";

                ToolTip.CreateTooltip(text);
            }
            return false;
        }
    }
}