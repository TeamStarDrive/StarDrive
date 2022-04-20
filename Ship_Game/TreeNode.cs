using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using System.Text.RegularExpressions;

namespace Ship_Game
{
    public sealed class TreeNode : Node
    {
        public Graphics.Font TitleFont = Fonts.Tahoma10; // Fonts.Visitor10;
        public NodeState State;
        public ResearchScreenNew Screen;
        public string TechName;
        public Rectangle TitleRect;
        public Rectangle BaseRect = new Rectangle(0, 0, 92, 98);
        public Vector2 RightPoint => new Vector2(BaseRect.Right - 25, BaseRect.CenterY() - 10);
        public bool complete;
        Rectangle IconRect;
        Rectangle UnlocksRect;
        readonly Array<UnlockItem> UnlocksGridItems;
        UnlocksGrid UnlocksGrid;
        Rectangle progressRect;
        float TitleWidth = 73f;

        readonly Technology TechTemplate;
        Rectangle PlusRect;

        const int MaxUnlockItems = 4;

        public TreeNode(Vector2 pos, TechEntry theEntry, ResearchScreenNew screen)
        {
            if (GlobalStats.IsRussian)
                TitleFont = Fonts.Arial10;

            Screen = screen;
            Entry = theEntry;
            Technology tech = ResourceManager.TechTree[theEntry.UID];
            TechName = tech.Name.Text + (tech.MaxLevel > 1 ? " " + RomanNumerals.ToRoman(theEntry.Level) + "/" + RomanNumerals.ToRoman(tech.MaxLevel) : "");
            TechTemplate = ResourceManager.TechTree[Entry.UID];
            complete = EmpireManager.Player.HasUnlocked(Entry);
            UnlocksGridItems = UnlockItem.CreateUnlocksList(TechTemplate, maxUnlocks: MaxUnlockItems);
            SetPos(pos);
        }

        public void SetPos(Vector2 pos)
        {
            BaseRect.X = (int)pos.X;
            BaseRect.Y = (int)pos.Y;
            progressRect = new Rectangle(BaseRect.X + 14, BaseRect.Y + 21, 1, 34);
            IconRect = new Rectangle(BaseRect.X + BaseRect.Width / 2 - 29, BaseRect.Y + BaseRect.Height / 2 - 24 - 10, 58, 49);

            int numColumns = UnlocksGridItems.Count / 2 + UnlocksGridItems.Count % 2;
            if (UnlocksGridItems.Count <= 1)
            {
                UnlocksRect = new Rectangle(IconRect.X + IconRect.Width, IconRect.Y + IconRect.Height - 5, 35, 32);
                UnlocksRect.Y -= UnlocksRect.Height;
                UnlocksGrid = new UnlocksGrid(UnlocksGridItems, UnlocksRect.Move(3, 0));
            }
            else
            {
                UnlocksRect = new Rectangle(IconRect.X + IconRect.Width, IconRect.Y + IconRect.Height - 5, 13 + numColumns * 32, (UnlocksGridItems.Count == 1 ? 32 : 64));
                UnlocksRect.Y -= UnlocksRect.Height;
                UnlocksGrid = new UnlocksGrid(UnlocksGridItems, UnlocksRect.Move(13, 0));
            }
            UnlocksRect = UnlocksRect.Bevel(2);

            TitleRect = new Rectangle(BaseRect.X, BaseRect.Y - 20, 90, 36);
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

            bool queued = EmpireManager.Player.Research.IsQueued(Entry.UID);
            bool active = complete || queued;

            string techBaseRectSuffix = "";
            string progressIcon = "ResearchMenu/tech_progress";
            var unlocksRectBorderColor = new Color(190, 113, 25);
            var completeTitleColor = new Color(132, 172, 208);

            if (State == NodeState.Normal)
            {
                unlocksRectBorderColor = Color.Black;
                if (complete)
                {
                    techBaseRectSuffix = "_complete";
                    unlocksRectBorderColor = new Color(34, 136, 200);
                }
                else if (queued)
                {
                    techBaseRectSuffix = "_queue";
                    unlocksRectBorderColor = Color.Teal;
                }
                if (!active)
                {
                    progressIcon = "ResearchMenu/tech_progress_inactive";
                }
            }
            else
            {
                techBaseRectSuffix = "_hover";
            }
            if (State == NodeState.Press)
            {
                completeTitleColor = new Color(163, 198, 236);
            }

            batch.FillRectangle(UnlocksRect, new Color(26, 26, 28));
            batch.DrawRectangle(UnlocksRect, unlocksRectBorderColor);
            UnlocksGrid.Draw(batch);

            batch.Draw(ResourceManager.Texture($"NewUI/new_tech_base{techBaseRectSuffix}"), BaseRect, Color.White);
            batch.Draw(TechIcon, IconRect, Color.White);
            batch.Draw(ResourceManager.Texture($"NewUI/new_tech_base_title{techBaseRectSuffix}"), TitleRect, Color.White);

            // Draw the Title as multi-line centered text
            // TODO: Maybe Use UILabel MultiLine text with automatic centering??
            string title = TitleFont.ParseText(TechName, TitleWidth);
            string[] titleLines = title.Split('\n');
            float textHeight = titleLines.Length * TitleFont.LineSpacing;
            float textStartY = TitleRect.Y + 18 - textHeight/2f;

            for (int i = 0; i < titleLines.Length; ++i)
            {
                var pos = new Vector2(TitleRect.CenterX() - TitleFont.TextWidth(titleLines[i]) * 0.5f,
                                      textStartY + i * TitleFont.LineSpacing);
                batch.DrawString(TitleFont, titleLines[i], pos.Rounded(), complete ? completeTitleColor : Color.White);
            }

            batch.Draw(ResourceManager.Texture(progressIcon), progressRect, Color.White);

            int progress = (int)(progressRect.Height - EmpireManager.Player.TechProgress(Entry) / EmpireManager.Player.TechCost(Entry) * (double)progressRect.Height);
            Rectangle progressRect2 = progressRect;
            progressRect2.Height = progress;
            batch.Draw(ResourceManager.Texture("ResearchMenu/tech_progress_bgactive"), progressRect2, Color.White);

            // draw tech cost if tech can still be research (such as multi-level techs) display the cost

            LocalizedText costText = LocalizedText.None;
            var costFont = TitleFont;
            if (Entry.CanBeResearched)
            {
                costText = (Entry.TechCost - Entry.Progress).GetNumberString();
            }
            else if (Entry.Unlocked)
            {
                costText = GameText.Unlocked;
                costFont = Fonts.Arial8Bold;
            }

            if (costText.NotEmpty)
            {
                Vector2 textSize = costFont.MeasureString(costText);
                var costPos = new Vector2(BaseRect.X + BaseRect.Width/2 - textSize.X/2 - 2, BaseRect.Y + 67).Rounded();
                batch.DrawString(costFont, costText, costPos, Color.SkyBlue);
                //batch.DrawRectangle(new RectF(costPos, textSize), Color.Red); // for debugging
            }

            // draw an orange + if there are more techs unlocked
            if (TechTemplate.NumStuffUnlocked(EmpireManager.Player) > MaxUnlockItems)
            { 
                PlusRect = new Rectangle(UnlocksRect.X + 60, UnlocksRect.Y + UnlocksRect.Height, 20, 20);
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
                    ScreenManager.AddScreen(new ResearchPopup(u, Entry.UID));
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
                foreach (UnlocksGrid.GridItem gi in UnlocksGrid.GridOfUnlocks)
                {
                    Vector2 rectPos4 = camera.GetScreenSpaceFromWorldSpace(gi.Pos);
                    var moddedRect4 = new Rectangle((int)rectPos4.X, (int)rectPos4.Y, gi.rect.Width, gi.rect.Height);
                    if (moddedRect4.HitTest(input.CursorPosition))
                    {
                        ShipHull unlocked = gi.item.hull;
                        ToolTip.CreateTooltip(unlocked == null
                            ? $"{gi.item.Title}\n\n{gi.item.Description}"
                            : $"{unlocked.VisibleName} ({Localizer.GetRole(unlocked.Role, EmpireManager.Player)})");
                    }
                }
            }
            else
            {
                ToolTip.CreateTooltip($"Right Click to Expand \n\n{ResourceManager.TechTree[Entry.UID].Description.Text}");
            }
            return false;
        }
    }
}