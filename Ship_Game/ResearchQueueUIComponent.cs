using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDUtils;
using Ship_Game.UI;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class ResearchQueueUIComponent : UIPanel
    {
        readonly ResearchScreenNew Screen;
        Empire Player => Screen.Player;

        readonly Submenu CurrentResearchPanel;
        readonly UIPanel TimeLeft;
        readonly UILabel TimeLeftLabel;
        readonly UIPanel SpyDisruption;
        readonly UILabel SpyDisruptionLabel;

        ResearchQItem CurrentResearch;
        readonly ScrollList<ResearchQItem> ResearchQueueList;
        readonly UIButton BtnShowQueue;

        public ResearchQueueUIComponent(ResearchScreenNew screen, in Rectangle container)  : base(container, Color.Black)
        {
            Screen = screen;

            BtnShowQueue = Button(ButtonStyle.BigDip, 
                new Vector2(container.Right - 170, screen.ScreenHeight - 55), "", OnBtnShowQueuePressed);

            RectF current = new(container.X, container.Y, container.Width, 150);
            RectF timeLeftRect = new(current.X + current.W - 119, current.Y + current.H - 24, 111, 20);
            TimeLeft = Panel(timeLeftRect, Color.White, ResourceManager.Texture("ResearchMenu/timeleft"));
            
            var labelPos = new Vector2(TimeLeft.X + 26,
                                       TimeLeft.Y + TimeLeft.Height / 2 - Fonts.Verdana14Bold.LineSpacing / 2);
            TimeLeftLabel = TimeLeft.Label(labelPos, "", Fonts.Verdana14Bold, new Color(205, 229, 255));

            CurrentResearchPanel = Add(new Submenu(current, GameText.CurrentResearch, SubmenuStyle.Blue));

            // Disruption indicator inline with the "Current Research" tab
            // title. 80% of the 25px tab height. Added AFTER the Submenu so
            // it draws on top of the tab bar.
            const int spyIconSize = 20;
            float titleW = Fonts.Pirulen12.MeasureString(Localizer.Token(GameText.CurrentResearch)).X;
            var spyIconRect = new Rectangle((int)(current.X + titleW + 50),
                                            (int)current.Y -3 + (25 - spyIconSize) / 2,
                                            spyIconSize, spyIconSize);
            SpyDisruption = Add(new UIPanel(spyIconRect, ResourceManager.Texture("UI/icon_spy")));
            SpyDisruption.Tooltip = GameText.ResearchDisruptedByInfiltrationTip;
            var spyLabelPos = new Vector2(spyIconRect.X + spyIconRect.Width + 4,
                                          spyIconRect.Y + 3 + spyIconRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2);
            SpyDisruptionLabel = Add(new UILabel(spyLabelPos, "", Fonts.Arial12Bold, new Color(255, 96, 96),
                                                 GameText.ResearchDisruptedByInfiltrationTip));
            SpyDisruption.Visible = false;
            SpyDisruptionLabel.Visible = false;
            
            RectF queue = new(current.X, current.Y + 165, container.Width, container.Height - 165);
            var queueSub = Add(new SubmenuScrollList<ResearchQItem>(queue, GameText.ResearchQueue, 125, ListStyle.Blue));
            ResearchQueueList = queueSub.List;

            // FB Disabled due to being able to drag stuff to be before other research mandatory for it.
            //ResearchQueueList.OnDragReorder = OnResearchItemReorder; 
            ReloadResearchQueue();
        }

        // TODO: check if we are moving item up before allowed item
        void OnResearchItemReorder(ResearchQItem item, int relativeChange)
        {
            // we use +1 here, because [0] is the current research item
            // which is not in the ScrollList
            //Screen.Player.Research.ReorderTech(oldIndex+1, newIndex+1);
        }

        void OnBtnShowQueuePressed(UIButton button)
        {
            SetQueueVisible(!ResearchQueueList.Visible);
        }

        void SetQueueVisible(bool visible)
        {
            if (CurrentResearch != null)
            {
                TimeLeft.Visible = visible;
                CurrentResearch.Visible = visible;
            }
            else
            {
                TimeLeft.Visible = false;
            }

            ResearchQueueList.Visible = visible;
            ResearchQueueList.Parent.Visible = visible;
            CurrentResearchPanel.Visible = visible;
            if (!visible)
            {
                SpyDisruption.Visible = false;
                SpyDisruptionLabel.Visible = false;
            }
            BtnShowQueue.Text = ResearchQueueList.Visible ? GameText.HideQueue : GameText.ShowQueue;
        }

        public override bool HandleInput(InputState input)
        {
            if (CurrentResearch != null && CurrentResearch.HandleInput(input))
                return true;

            if (ResearchQueueList.Visible && input.RightMouseClick && ResearchQueueList.Any(item => item.HitTest(input.CursorPosition)))
                return base.HandleInput(input);

            if (input.Escaped || input.RightMouseClick)
            {
                Screen.ExitScreen();
                return true;
            }

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            if (ResearchQueueList.Visible && CurrentResearch != null)
            {
                CurrentResearch.Draw(batch, elapsed);

                float remaining = CurrentResearch.Tech.TechCost - CurrentResearch.Tech.Progress;
                float numTurns = (float)Math.Ceiling(remaining / (0.01f + Screen.Player.Research.NetResearch));
                TimeLeftLabel.Text = (numTurns > 999f) ? ">999 turns" : numTurns.String(0)+" turns";

                float multiplier = Screen.Player.Research.DisruptionMultiplier;
                bool disrupted = multiplier < 1f;
                SpyDisruption.Visible = disrupted;
                SpyDisruptionLabel.Visible = disrupted;
                if (disrupted)
                {
                    int turnsLeft = Screen.Player.Research.DisruptionTurnsLeft;
                    SpyDisruptionLabel.Text = $"({(int)Math.Round(multiplier * 100f)}%, {turnsLeft} {Localizer.Token(TurnsText(turnsLeft))})";
                }
            }
        }

        static GameText TurnsText(int turns)
        {
            if (Localizer.Language is not (Language.Russian or Language.Ukrainian))
                return turns == 1 ? GameText.Turn2 : GameText.Turns2;

            int lastDigit = turns % 10, lastTwoDigits = turns % 100;
            if (lastDigit == 1 && lastTwoDigits != 11)
                return GameText.Turn2;
            if (lastDigit is >= 2 and <= 4 && lastTwoDigits is < 12 or > 14)
                return GameText.TurnsFew;
            return GameText.Turns2;
        }

        ResearchQItem CreateQueueItem(TechEntry tech)
        {
            var defaultPos = new Vector2(CurrentResearchPanel.X + 5, CurrentResearchPanel.Y + 30);
            return new(Screen, tech, defaultPos) { List = ResearchQueueList };
        }

        public void AddToResearchQueue(TechEntry tech)
        {
            if (Player.Research.AddToQueue(tech.UID))
            {
                if (CurrentResearch == null)
                    CurrentResearch = CreateQueueItem(tech);
                else
                    ResearchQueueList.AddItem(CreateQueueItem(tech));

                SetQueueVisible(true);
            }
        }

        public void ReloadResearchQueue()
        {
            CurrentResearch = Player.Research.HasTopic
                            ? CreateQueueItem(Player.Research.Current)
                            : null;

            var items = new Array<ResearchQItem>();
            foreach (string tech in Player.Research.QueuedItems)
            {
                TechEntry queuedTech = Player.GetTechEntry(tech);
                items.Add(CreateQueueItem(queuedTech));
            }
            ResearchQueueList.SetItems(items);

            SetQueueVisible(Player.Research.HasTopic);
        }
    }
}
