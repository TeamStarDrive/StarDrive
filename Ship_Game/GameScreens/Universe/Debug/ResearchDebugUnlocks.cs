using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.UI;

namespace Ship_Game.GameScreens.Universe.Debug
{
    public class ResearchDebugUnlocks : UIElementContainer
    {
        Vector2 BtnSize = new Vector2(98, 64);
        const float BtnSpacing = 98 + 2;

        Action OnResearchChanged;

        public ResearchDebugUnlocks(Action onResearchChanged)
            : base(0, 0, w:BtnSpacing*5, h:64)
        {
            OnResearchChanged = onResearchChanged;

            InputState input = ScreenManager.Instance.input;

            Add(new UIPanel(LocalPos.Zero, Size, Color.TransparentBlack.Alpha(0.2f)));
            var style = new UIButton.StyleTextures("NewUI/Debug/tech_button.png", ButtonStyle.DanButton);

            AddCustomBtn(BtnSpacing*0, style,
                () => "Reset ALL\nTechs",
                "Resets all unlocked techs and undos their effects.",
                InputBindings.FromString("Ctrl+F1"),
                (b) => OnResetAllTechsClicked()
            );
            AddCustomBtn(BtnSpacing*1, style,
                () => !input.IsShiftKeyDown ? "ALL Tech\nWith Bonus" : "ALL Tech\nNo Bonus",
                "Unlocks ALL existing techs with Bonuses, hold down Shift for No Bonuses.",
                InputBindings.FromString("Ctrl+F2"),
                (b) => OnUnlockAllTechsClicked(unlockBonuses: !input.IsShiftKeyDown)
            );
            AddCustomBtn(BtnSpacing*2, style,
                () => !input.IsShiftKeyDown ? "Race Tech\nWith Bonus" : "Race Tech\nNo Bonus",
                "Unlocks only techs exclusive to this race with Bonuses, hold down Shift for No Bonuses.",
                InputBindings.FromString("Ctrl+F3"),
                (b) => OnUnlockRaceTechClicked(unlockBonuses: !input.IsShiftKeyDown)
            );
            AddCustomBtn(BtnSpacing*3, style,
                () => "Run AI\nPlanner",
                "Runs AI Research Planner and unlocks several techs.",
                InputBindings.FromString("Ctrl+F4"),
                (b) => OnRunAIPlannerClicked()
            );
            AddCustomBtn(BtnSpacing*4, style,
                () => "Current\nTech",
                "Finishes currently researching technology only.",
                InputBindings.FromString("Ctrl+F5"),
                (b) => OnUnlockCurrentResearchClicked()
            );
        }

        void AddCustomBtn(float localX, UIButton.StyleTextures style,
                          Func<string> label, in LocalizedText tooltip,
                          InputBindings.IBinding hotkey, Action<UIButton> onClick)
        {
            var btn = Add(new UIButton(style, BtnSize, LocalizedText.None)
            {
                Tooltip = tooltip,
                OnClick = onClick,
                Hotkey = hotkey,
                DynamicText = label,
            });
            btn.SetLocalPos(localX, 0);
        }

        void OnResearchChange()
        {
            OnResearchChanged?.Invoke();
            EmpireManager.Player.UpdateForNewTech();
        }

        void OnResetAllTechsClicked()
        {
            ResetAllTechsAndBonuses(EmpireManager.Player);
            OnResearchChange();
        }

        void OnUnlockAllTechsClicked(bool unlockBonuses)
        {
            UnlockAllResearch(EmpireManager.Player, unlockBonuses);
            OnResearchChange();
        }

        void OnUnlockRaceTechClicked(bool unlockBonuses)
        {
            UnlockCurrentTechTree(EmpireManager.Player, unlockBonuses);
            OnResearchChange();
        }

        void OnRunAIPlannerClicked()
        {
            RunAIResearchPlanner(EmpireManager.Player);
            OnResearchChange();
        }

        void OnUnlockCurrentResearchClicked()
        {
            UnlockCurrentResearchTopic(EmpireManager.Player);
            OnResearchChange();
        }

        public static void ResetAllTechsAndBonuses(Empire empire)
        {
            empire.Research.Reset(); // clear research progress bar and queue
            empire.Research.SetNoResearchLeft(false);
            foreach (TechEntry techEntry in empire.TechEntries)
            {
                techEntry.ResetUnlockedTech();
            }
        }

        public static void UnlockAllResearch(Empire empire, bool unlockBonuses)
        {
            // Unlock an empire at each press of ctrl-F1
            int totalTechs = empire.TechEntries.Count;
            int techUnlocked = empire.TechEntries.Count(t => t.Unlocked);
            float ratioUnlocked = techUnlocked / (float)totalTechs;
            empire.Research.SetNoResearchLeft(true);

            foreach (TechEntry techEntry in empire.TechEntries)
            {
                if (!techEntry.Unlocked)
                {
                    techEntry.DebugUnlockFromTechScreen(empire, empire, unlockBonuses);
                    GameAudio.EchoAffirmative();
                }
                else if (ratioUnlocked > 0.9f)
                {
                    foreach (var them in EmpireManager.Empires)
                    {
                        if (them != empire && !techEntry.SpiedFrom(them))
                        {
                            techEntry.DebugUnlockFromTechScreen(empire, them, unlockBonuses);
                            GameAudio.AffirmativeClick();
                            break;
                        }
                    }
                }
            }
            empire.UpdateShipsWeCanBuild();
        }

        public static void UnlockCurrentTechTree(Empire empire, bool unlockBonuses)
        {
            foreach (TechEntry techEntry in empire.TechEntries)
            {
                techEntry.UnlockWithBonus(empire, empire, unlockBonuses);
                if (!unlockBonuses && techEntry.Tech.BonusUnlocked.NotEmpty)
                    techEntry.Unlocked = false;
            }
            empire.UpdateShipsWeCanBuild();
        }

        public static void RunAIResearchPlanner(Empire empire)
        {
            empire.AutoResearch = true;
            empire.Research.Reset();
            empire.GetEmpireAI().DebugRunResearchPlanner();
        }

        public static void UnlockCurrentResearchTopic(Empire empire)
        {
            if (empire.Research.HasTopic)
            {
                empire.Research.Current.Unlock(EmpireManager.Player);
                empire.UpdateShipsWeCanBuild();
            }
            else
            {
                GameAudio.NegativeClick();
            }
        }
    }
}
