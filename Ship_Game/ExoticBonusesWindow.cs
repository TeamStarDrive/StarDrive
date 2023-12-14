using System;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;
using SDUtils;
using System.Runtime.InteropServices;

namespace Ship_Game
{
    public sealed class ExoticBonusesWindow : GameScreen
    {
        public bool IsOpen { get; private set; }
        readonly UniverseScreen Screen;
        Submenu ConstructionSubMenu;
        Map<ExoticBonusType, EmpireExoticBonuses> ExoticBonuses;
        bool CompactMode;

        Empire Player => Screen.Player;

        public ExoticBonusesWindow(UniverseScreen screen) : base(screen, toPause: null)
        {
            Screen = screen;
            const int windowWidth = 650;
            int windowHeight = (ResourceManager.GetNumExoticGoods() * (Fonts.Arial12Bold.LineSpacing+20));
            Rect = new Rectangle((int)Screen.Minimap.X - 5 - windowWidth, (int)Screen.Minimap.Y + 
                (int)Screen.Minimap.Height - windowHeight - 10, windowWidth, windowHeight);
        }

        class ExoticStats : UIElementV2
        {
            readonly Empire Owner;
            readonly UILabel BonusInfo;
            readonly UILabel ResourceName;
            readonly UIPanel Icon;
            readonly UILabel OutputInfo;
            readonly ProgressBar ConsumptionBar;
            readonly ProgressBar StorageBar;
            readonly EmpireExoticBonuses ExoticResource;
            readonly UILabel PotentialInfo;
            readonly UILabel ActiveVsTotalOps;
            public ExoticStats(Empire owner, EmpireExoticBonuses bonus)
            {
                Owner = owner;
                ExoticResource = Owner.GetExoticResource(bonus.Good.ExoticBonusType);
                BonusInfo = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.White) { Tooltip = new LocalizedText(bonus.Good.DescriptionIndex) };
                Icon = new UIPanel(new Rectangle(-100, -100, 20, 20), ResourceManager.Texture($"Goods/{bonus.Good.UID}"));
                Icon.Tooltip = new LocalizedText(bonus.Good.DescriptionIndex);
                ResourceName = new UILabel(new Vector2(-100, -100), new LocalizedText(bonus.Good.RefinedNameIndex), Fonts.Arial12Bold, Color.Wheat, new LocalizedText(bonus.Good.DescriptionIndex));
                OutputInfo = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);
                ConsumptionBar = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0) { Fraction10Values = true };
                StorageBar = new ProgressBar(new Rectangle(-100, -100, 150, 18), 0, 0) { color = "blue", Fraction10Values = true };
                PotentialInfo = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Wheat);
                ActiveVsTotalOps = new UILabel(new Vector2(-100, -100), GameText.HullBonus, Fonts.Arial12Bold, Color.Gray);

            }
            public override void PerformLayout()
            {
                BonusInfo.Pos = new Vector2(Pos.X, Pos.Y+2);
                BonusInfo.PerformLayout();
                Icon.Pos = new Vector2(Pos.X+50, Pos.Y);
                Icon.PerformLayout();
                ResourceName.Pos = new Vector2(Pos.X+ 80, Pos.Y+2);
                ResourceName.PerformLayout();
                OutputInfo.Pos = new Vector2(Pos.X + 170, Pos.Y);
                OutputInfo.PerformLayout();
                ConsumptionBar.SetRect(new Rectangle((int)Pos.X + 230, (int)Pos.Y, 150, 18));
                StorageBar.SetRect(new Rectangle((int)Pos.X + 385, (int)Pos.Y, 150, 18));
                PotentialInfo.Pos = new Vector2(Pos.X + 545, Pos.Y);
                PotentialInfo.PerformLayout();
                ActiveVsTotalOps.Pos = new Vector2(Pos.X + 590, Pos.Y);
                ActiveVsTotalOps.PerformLayout();
            }
            public override bool HandleInput(InputState input)
            {
                Icon.HandleInput(input);
                ResourceName.HandleInput(input);
                return false;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                BonusInfo.Draw(batch, elapsed);
                Icon.Draw(batch, elapsed);
                ResourceName.Draw(batch, elapsed);
                OutputInfo.Draw(batch, elapsed);

                if (ActiveVsTotalOps.Text == "0/0/0" && StorageBar.Progress == 0)
                {
                    StorageBar.DrawGrayed(batch);
                    ConsumptionBar.DrawGrayed(batch);
                }
                else
                {
                    StorageBar.Draw(batch);
                    if (StorageBar.Progress < StorageBar.Max)
                        ConsumptionBar.Draw(batch);
                    else
                        ConsumptionBar.DrawGrayed(batch);
                }                   

                PotentialInfo.Draw(batch, elapsed);
                ActiveVsTotalOps.Draw(batch, elapsed);
            }

            public override void Update(float fixedDeltaTime)
            {
                var exoticResource = ExoticResource;
                bool storagefull = exoticResource.CurrentStorage == Owner.MaxExoticStorage;
                UpdateBonus(exoticResource.DynamicBonus, exoticResource.PreviousBonus, 
                    exoticResource.DynamicBonusString, exoticResource.TotalBuiltMiningOps, exoticResource.MaxBonus);
                UpdateOutput(exoticResource.OutputThisTurn, exoticResource.TotalBuiltMiningOps,
                    exoticResource.CurrentPercentageOutput, exoticResource.Consumption, exoticResource.RefinedPerTurnForConsumption, storagefull);

                UpdateConsumptionBar(exoticResource.Consumption, exoticResource.TotalRefinedPerTurn, exoticResource.TotalBuiltMiningOps, storagefull);
                UpdateStorage(exoticResource.CurrentStorage, Owner.MaxExoticStorage, exoticResource.TotalBuiltMiningOps, 
                    exoticResource.TotalRefinedPerTurn >= exoticResource.Consumption);
                UpdatePotential(exoticResource.MaxPotentialRefinedPerTurn, exoticResource.Consumption);
                UpdateOps(exoticResource.ActiveMiningOps, exoticResource.TotalBuiltMiningOps, exoticResource.ActiveVsTotalOps, storagefull);

               // base.Update(fixedDeltaTime);
            }

            void UpdateBonus(float currentBonus, float previousBonus, string bonusString, int miningOps, float maxBonus)
            {
                currentBonus = currentBonus.RoundToFractionOf10();
                previousBonus = previousBonus.RoundToFractionOf10();
                BonusInfo.Text = bonusString;
                if (currentBonus == maxBonus)                BonusInfo.Color = Color.Gold;
                else if (currentBonus > previousBonus)       BonusInfo.Color = Color.Green;
                else if (currentBonus < previousBonus)       BonusInfo.Color = Color.Red;
                else if (miningOps > 0)                      BonusInfo.Color = Color.White;
                else                                         BonusInfo.Color = Color.Gray;
            }

            void UpdateConsumptionBar(float consumption, float refining, float totalMiningOps, bool storageFull)
            {
                ConsumptionBar.Max = consumption;
                ConsumptionBar.Progress = refining;
                float ratio = consumption > 0 ? refining / consumption 
                                              : refining > consumption ? 1 
                                                                       : 0;
                if      (storageFull)        ConsumptionBar.color = "brown";
                else if (ratio > 0.95f)      ConsumptionBar.color = "green";
                else if (ratio > 0)          ConsumptionBar.color = "yellow";
                else if (totalMiningOps > 0) ConsumptionBar.color = "red";
                else                         ConsumptionBar.color = "brown";
            }

            void UpdateOutput(float output, int activeOps, string outputPercent, float consumption, float refining, bool storageFull)
            {
                OutputInfo.Text = outputPercent;
                if      (storageFull)                   OutputInfo.Color = Color.Wheat;
                else if (output == 0 && activeOps == 0) OutputInfo.Color = Color.Gray;
                else if (output == 0 && activeOps > 0)  OutputInfo.Color = Color.Red;
                else if (refining < consumption*0.95f)  OutputInfo.Color = Color.Yellow;
                else                                    OutputInfo.Color = Color.Wheat;
            }

            void UpdatePotential(float maxPotentialRefinedPerTurn, float consumption)
            {
                PotentialInfo.Text = maxPotentialRefinedPerTurn.String(1);
                if      (maxPotentialRefinedPerTurn == 0)          PotentialInfo.Color = Color.Gray;
                else if (consumption > maxPotentialRefinedPerTurn) PotentialInfo.Color = Color.Red;
                else                                               PotentialInfo.Color = Color.Wheat;
            }

            void UpdateOps(int activeMiningOps, int totalMiningOps, string activeVsTotalOps, bool storageFull)
            {
                ActiveVsTotalOps.Text = activeVsTotalOps;
                Color color = Color.Yellow;

                if      (storageFull)                       color = Color.Wheat;
                else if (totalMiningOps == 0)               color = Color.Gray;
                else if (activeMiningOps == 0)              color = Color.Red;
                else if (totalMiningOps == activeMiningOps) color = Color.Green;

                ActiveVsTotalOps.Color = color;
            }

            void UpdateStorage(float currentStorage, float maxStorage, int totalOps, bool positiveRefinning) 
            {
                StorageBar.Max = maxStorage;
                StorageBar.Progress = currentStorage;
                if      (positiveRefinning)                   StorageBar.color = "green";
                else if (totalOps > 0 && currentStorage == 0) StorageBar.color = "red";
                else                                          StorageBar.color = "blue";
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RemoveAll();
            ExoticBonuses = Player.GetExoticBonuses();

            RectF win = new(Rect);
            ConstructionSubMenu = new(win, GameText.ExoticResourcesMenu);

            float titleOffset = win.Y + 40;
            Add(new UILabel(new Vector2(win.X+5,   titleOffset), GameText.ExoticResourcesBonus, Fonts.Arial12Bold, Color.Gold, GameText.ExoticResourceBonusTip));
            Add(new UILabel(new Vector2(win.X+60,  titleOffset), GameText.ExoticResourcesName, Fonts.Arial12Bold, Color.White));
            Add(new UILabel(new Vector2(win.X+170, titleOffset), GameText.ExoticResourcesOutput, Fonts.Arial12Bold, Color.White, GameText.ExoticResourceOutputTip));
            Add(new UILabel(new Vector2(win.X+240, titleOffset), GameText.ExoticRefiningVsConsumption, Fonts.Arial12Bold, Color.White, GameText.ExoticResourceRefineConsumeTip));
            Add(new UILabel(new Vector2(win.X+430, titleOffset), GameText.Storage, Fonts.Arial12Bold, Color.White, GameText.ExoticResourceStorageTip));
            Add(new UILabel(new Vector2(win.X+550, titleOffset), GameText.ExoticRefiningMaxPotential, Fonts.Arial12Bold, Color.White, GameText.ExoticResourceMaxRefineTip));
            Add(new UILabel(new Vector2(win.X+595, titleOffset), GameText.ExoticNumOps, Fonts.Arial12Bold, Color.White, GameText.ExoticResourceOpsTip));

            UIList bonusData = AddList(new(win.X + 5f, win.Y + 40));
            bonusData.Padding = new(2f, 25f);
            foreach (EmpireExoticBonuses type in ExoticBonuses.Values)
                bonusData.Add(new ExoticStats(Player, type));
        }

        public void ToggleVisibility()
        {
            GameAudio.AcceptClick();
            IsOpen = !IsOpen;
            if (IsOpen)
                LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            Rectangle r = ConstructionSubMenu.Rect;
            r.Y += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch, elapsed);
            ConstructionSubMenu.Draw(batch, elapsed);
            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if (!IsOpen)
                return false;

            if (input.RightMouseClick || input.Escaped)
            {
                IsOpen = false;
                return false;
            }

            base.HandleInput(input);
            return false;
        }
    }
}
