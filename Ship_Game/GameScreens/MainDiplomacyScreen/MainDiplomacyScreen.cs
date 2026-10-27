using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Input;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Graphics;
using static Ship_Game.Data.Serialization.Types.RawArraySerializer;
using Color = Microsoft.Xna.Framework.Color;
using Font = Ship_Game.Graphics.Font;

namespace Ship_Game
{
    public sealed class MainDiplomacyScreen : GameScreen
    {
        public override bool HelpKeyOpensCodex => true;

        UniverseScreen Universe;
        public DanButton Contact;

        Menu2 TitleBar;
        Vector2 TitlePos;
        Menu2 DMenu;

        public Rectangle SelectedInfoRect;
        public Rectangle IntelligenceRect;
        public Rectangle OperationsRect;

        public Empire SelectedEmpire;

        Array<RaceEntry> Races = new();
        ScrollList<ArtifactItemListItem> ArtifactsSL;
        UITextBox IntelBox;
        UITextBox BonusBox;

        Empire Player;
        readonly bool UsingNewEspioange;
        Array<Empire> Friends;
        Array<Empire> Traders;
        HashSet<Empire> Moles;

        UIButton DiagramButton;
        Rectangle LeftRect;

        Font Font12 = Fonts.Arial12;
        Font Font12Bold = Fonts.Arial12Bold;
        Font Font20Bold = Fonts.Arial20Bold;


        public MainDiplomacyScreen(UniverseScreen screen) : base(screen, toPause: screen)
        {
            Universe = screen;
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            Player = screen.Player;
            Friends = screen.UState.GetAllies(Player);
            Traders = screen.UState.GetTradePartners(Player);
            UsingNewEspioange = Player.NewEspionageEnabled;

            // find empires where player or friends have moles
            var empires = new HashSet<Empire>();
            foreach(Empire empire in screen.UState.Empires)
            {
                if (empire.isPlayer || empire.IsFaction)
                    continue;

                if (Player.data.MoleList.Any(m => empire.FindPlanet(m.PlanetId) != null))
                {
                    empires.Add(empire);
                }
                else
                {
                    foreach(Empire friend in Friends)
                    {
                        if (friend.data.MoleList.Any(m => empire.FindPlanet(m.PlanetId) != null))
                        {
                            empires.Add(empire);
                            break;
                        }
                    }
                }
            }
            Moles = empires;
        }

        private int IntelligenceLevel(Empire e)
        {
            if (UsingNewEspioange)
                return 0;

            int intelligence = 0;
            if (Friends.Contains(e) || Moles.Contains(e))
                return 2;

            if (Traders.Contains(e) && Player.GetRelations(e).Treaty_Trade_TurnsExisted > 30)
                return 1;

            if (e.isPlayer)
                return 3;

            foreach(Empire empire in Friends)
            {
                if (!empire.GetRelations(e, out Relationship rel))
                    continue;

                if (rel.Treaty_Trade && rel.Treaty_Trade_TurnsExisted > 30)
                    intelligence = 1;

                if (rel.Treaty_Alliance && rel.TurnsAllied > 3)
                    return 2;
            }

            if (intelligence ==0)
            {
                foreach (Empire empire in Traders)
                {
                    if (!empire.GetRelations(e, out Relationship rel))
                        continue;

                    if (rel.Treaty_Trade && rel.Treaty_Trade_TurnsExisted > 60)
                        intelligence = 1;

                    if (rel.Treaty_Alliance && rel.TurnsAllied > 60)
                        return 2;
                }
            }
            
            return intelligence;
        }

        void DrawDiploLine(SpriteBatch batch, Font font, string text, Color color, ref Vector2 textCursor)
        {
            batch.DrawString(font, text, textCursor, color);
            textCursor.Y += (font.LineSpacing + 2);
        }


        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.SafeBegin();
            if (ScreenHeight > 766)
            {
                TitleBar.Draw(batch, elapsed);
                batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.DiplomaticOverview), TitlePos, Colors.Cream);
            }
            DMenu.Draw(batch, elapsed);
            foreach (RaceEntry race in Races)
            {
                if (race.e.IsFaction)
                {
                    continue;
                }
                Vector2 NameCursor = new Vector2(race.container.X + 62 - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, race.container.Y + 148 + 8);
                if (race.e.IsDefeated)
                {
                    if (race.e.data.AbsorbedBy == null)
                    {
                        batch.Draw(ResourceManager.Texture("Portraits/"+race.e.data.PortraitName), race.container, Color.White);
                        batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        batch.DrawDropShadowText1(race.e.data.Traits.Name, NameCursor, Fonts.Arial12Bold, race.e.EmpireColor);
                        batch.Draw(ResourceManager.ErrorTexture, race.container, Color.White);
                    }
                    else
                    {
                        batch.Draw(ResourceManager.Texture("Portraits/"+race.e.data.PortraitName), race.container, Color.White);
                        batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        batch.DrawDropShadowText1(race.e.data.Traits.Name, NameCursor, Fonts.Arial12Bold, race.e.EmpireColor);
                        var r = new Rectangle(race.container.X, race.container.Y, 124, 124);
                        var e = Universe.UState.GetEmpireByName(race.e.data.AbsorbedBy);
                        batch.Draw(ResourceManager.Flag(e.data.Traits.FlagIndex), r, e.EmpireColor);
                    }
                }
                else if (Player != race.e && Player.IsKnown(race.e))
                {
                    if (Player.IsAtWarWith(race.e) && !race.e.IsDefeated)
                    {
                        Rectangle war = new Rectangle(race.container.X - 2, race.container.Y - 2, race.container.Width + 4, race.container.Height + 4);
                        batch.FillRectangle(war, Color.Red);
                    }
                    batch.Draw(ResourceManager.Texture("Portraits/"+race.e.data.PortraitName), race.container, Color.White);
                    batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    batch.DrawDropShadowText1(race.e.data.Traits.Name, NameCursor, Fonts.Arial12Bold, race.e.EmpireColor);
                }
                else if (Player != race.e)
                {
                    batch.Draw(ResourceManager.Texture("Portraits/unknown"), race.container, Color.White);
                }
                else
                {
                    batch.Draw(ResourceManager.Texture("Portraits/"+race.e.data.PortraitName), race.container, Color.White);
                    batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    NameCursor = new Vector2(race.container.X + 62 - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, race.container.Y + 148 + 8);
                    batch.DrawDropShadowText1(race.e.data.Traits.Name, NameCursor, Fonts.Arial12Bold, race.e.EmpireColor);
                }
                if (race.e != SelectedEmpire)
                {
                    continue;
                }
                batch.DrawRectangle(race.container, Color.Orange);
            }
            batch.FillRectangle(SelectedInfoRect, new Color(23, 20, 14));
            batch.FillRectangle(IntelligenceRect, new Color(23, 20, 14));
            batch.FillRectangle(OperationsRect, new Color(23, 20, 14));
            var textCursor = new Vector2(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 10);
            batch.DrawDropShadowText1(SelectedEmpire.data.Traits.Name, textCursor, Fonts.Arial20Bold, SelectedEmpire.EmpireColor);
            var flagRect = new Rectangle(SelectedInfoRect.X + SelectedInfoRect.Width - 60, SelectedInfoRect.Y + 10, 40, 40);
            batch.Draw(ResourceManager.Flag(SelectedEmpire.data.Traits.FlagIndex), flagRect, SelectedEmpire.EmpireColor);
            textCursor.Y += (Fonts.Arial20Bold.LineSpacing + 4);

            if (SelectedEmpire.isPlayer)
            {
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.You), textCursor, Color.White);
                textCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                Rectangle artifactsRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 210, SelectedInfoRect.Width - 40, 130);
                Vector2 artifactsCursor = new Vector2(artifactsRect.X, artifactsRect.Y - 8);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.OwnedArtifacts), artifactsCursor, Color.White);
                artifactsCursor.Y += Fonts.Arial12Bold.LineSpacing;
            }
            else if (SelectedEmpire.IsDefeated)
            {
                if (SelectedEmpire.data.AbsorbedBy != null)
                {
                    Empire absorbingEmpire = Universe.UState.GetEmpireByName(SelectedEmpire.data.AbsorbedBy);
                    DrawDiploLine(batch, Font12Bold, absorbingEmpire.data.Traits.Singular + " Federation", Color.White, ref textCursor);
                }
            }
            else if (!SelectedEmpire.IsDefeated)
            {
                Relationship relation = Player.GetRelations(SelectedEmpire);
                if (UsingNewEspioange && relation.Espionage.CanViewPersonality || IntelligenceLevel(SelectedEmpire) > 0)
                    DrawDiploLine(batch, Font12Bold, $"{SelectedEmpire.data.DiplomaticPersonality.Name} {SelectedEmpire.data.EconomicPersonality.Name}", Color.White, ref textCursor);
                else
                    DrawDiploLine(batch, Font12Bold, $"Unknown Unknown", Color.White, ref textCursor);

                if (relation.AtWar)
                    DrawDiploLine(batch, Font12Bold, Localizer.Token(GameText.AtWar), Color.LightPink, ref textCursor);
                else if (relation.Treaty_Peace)
                    DrawDiploLine(batch, Font12Bold, $"{Localizer.Token(GameText.PeaceTreaty)} ({relation.PeaceTurnsRemaining} {Localizer.Token(GameText.Turns)})", Color.LightGreen, ref textCursor);

                if (relation.Treaty_OpenBorders)
                    DrawDiploLine(batch, Font12Bold, Localizer.Token(GameText.OpenBorders), Color.LightGreen, ref textCursor);

                if (relation.Treaty_Trade)
                    DrawDiploLine(batch, Font12Bold, Localizer.Token(GameText.TradeTreaty2), Color.LightGreen, ref textCursor);

                if (relation.Treaty_NAPact)
                    DrawDiploLine(batch, Font12Bold, Localizer.Token(GameText.NonaggressionPact2), Color.LightGreen, ref textCursor);

                if (relation.Treaty_Alliance)
                    DrawDiploLine(batch, Font12Bold, Localizer.Token(GameText.Alliance), Color.LightGreen, ref textCursor);

                Rectangle artifactsRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 250, SelectedInfoRect.Width - 40, 130);
                Vector2 artifactsCursor = new Vector2(artifactsRect.X, artifactsRect.Y - 8);
                if (!UsingNewEspioange || relation.Espionage.CanViewArtifacts)
                    DrawDiploLine(batch, Font12Bold, Localizer.Token(GameText.OwnedArtifacts), Color.White, ref artifactsCursor);

            }

            if (!SelectedEmpire.isPlayer && Player.IsKnown(SelectedEmpire))
                Contact.Draw(ScreenManager);

            if (SelectedEmpire.isPlayer || !UsingNewEspioange || UsingNewEspioange && Player.GetRelations(SelectedEmpire).Espionage.CanViewRanks)
            {

                Empire[] empireList = UsingNewEspioange
                    ? Universe.UState.ActiveMajorEmpires.Filter(e => e.isPlayer || Player.GetRelations(e).Espionage.CanViewRanks)
                    : Universe.UState.ActiveMajorEmpires.Filter(e => e.isPlayer || Player.IsKnown(e));

                Vector2 columnBCursor = textCursor;
                columnBCursor.X += 190f;
                columnBCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                textCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.EconomicStrength), textCursor, Color.White);
                empireList.Sort(e => -e.GrossIncome);
                batch.DrawString(Fonts.Arial12Bold, $"# {GetRank(SelectedEmpire, empireList)}", columnBCursor, Color.White);
                columnBCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                textCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;

                empireList.Sort(e => -GetScientificStr(e));
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.ScientificStrength), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, $"# {GetRank(SelectedEmpire, empireList)}", columnBCursor, Color.White);
                columnBCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                textCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;

                empireList.Sort(e => -e.CurrentMilitaryStrength);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.MilitaryStrength), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, $"# {GetRank(SelectedEmpire, empireList)}", columnBCursor, Color.White);
                columnBCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                textCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                empireList.Sort(e => -GetPop(e));
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Population), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, $"# {GetRank(SelectedEmpire, empireList)}", columnBCursor, Color.White);

                textCursor.Y += Fonts.Arial12Bold.LineSpacing + 4;
                batch.DrawString(Fonts.Arial12, $"(out of {empireList.Length} empires)", textCursor, Color.Wheat);
            }
            //Added by McShooterz:  intel report
            Espionage espionage = SelectedEmpire.isPlayer || !UsingNewEspioange ? null : Player.GetEspionage(SelectedEmpire);
            textCursor = new Vector2(IntelligenceRect.X + 20, IntelligenceRect.Y + 10);
            string intReport = Localizer.Token(GameText.IntelligenceReport);
            if (UsingNewEspioange && !SelectedEmpire.isPlayer)
                intReport += espionage.EffectiveLevel == 0 ? " (basic)" : $" (level {espionage.EffectiveLevel})";

            batch.DrawDropShadowText(intReport, textCursor, Fonts.Arial20Bold, SelectedEmpire.EmpireColor);

            textCursor = new Vector2(OperationsRect.X + 20, OperationsRect.Y + 10);
            batch.DrawDropShadowText((SelectedEmpire.isPlayer ? Localizer.Token(GameText.YourEmpiresBonuses) : Localizer.Token(GameText.TheirBonuses)), textCursor, Fonts.Arial20Bold, SelectedEmpire.EmpireColor);
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        int GetRank(Empire selectedEmpire, Empire[] empireList)
        {
            for (int i = 0; i < empireList.Length; i++)
            {
                Empire e = empireList[i];
                if (selectedEmpire == e)
                {
                    return i + 1;
                }
            }

            return empireList.Length;
        }

        void ShowEmpire(Empire empire)
        {
            SelectedEmpire = empire;
            if (ListArtifacts())
                GameAudio.EchoAffirmative();
            ListIntelligence();
            ListBonuses();
            ArtifactsSL.ScrollToTop();
            IntelBox.ItemsList.ScrollToTop();
            BonusBox.ItemsList.ScrollToTop();
        }

        bool WasOnTop = true;

        // treaties, gifts and trades made on a screen opened from here show on return
        public override void Update(float fixedDeltaTime)
        {
            bool onTop = ScreenManager.Current == this;
            if (onTop && !WasOnTop && SelectedEmpire != null)
            {
                ListArtifacts();
                ListIntelligence();
                ListBonuses();
            }
            WasOnTop = onTop;
            base.Update(fixedDeltaTime);
        }

        void ListIntelligence()
        {
            IntelBox.Clear();
            Espionage espionage = SelectedEmpire.isPlayer || !UsingNewEspioange ? null : Player.GetEspionage(SelectedEmpire);
            if (UsingNewEspioange || IntelligenceLevel(SelectedEmpire) > 0)
                IntelLine(Localizer.Token(GameText.HomeWorld)+SelectedEmpire.data.Traits.HomeworldName, Color.Wheat);

            if (UsingNewEspioange || IntelligenceLevel(SelectedEmpire) > 0)
            {
                if (SelectedEmpire.Capital != null)
                {
                    string controlsHomeworld = Localizer.Token(GameText.ControlsHomeWorld) + ((SelectedEmpire.Capital.Owner == SelectedEmpire)
                        ? Localizer.Token(GameText.Yes)
                        : Localizer.Token(GameText.No));

                    IntelLine(controlsHomeworld, Color.Wheat);
                }

                bool alwaysShow = SelectedEmpire.isPlayer || !UsingNewEspioange;
                if (alwaysShow || espionage.CanViewNumPlanets)
                    IntelLine($"{Localizer.Token(GameText.TotalPlanets)} {SelectedEmpire.GetPlanets().Count}", Color.Wheat);

                if (alwaysShow || espionage.CanViewNumShips)
                    IntelLine($"{Localizer.Token(GameText.TotalStarships)} {SelectedEmpire.OwnedShips.Count}", Color.Wheat);

                if (alwaysShow || espionage.CanViewMoneyAndMaint)
                {
                    IntelLine($"{Localizer.Token(GameText.Treasury)} {SelectedEmpire.Money.String(2)}", Color.Wheat);
                    IntelLine($"{Localizer.Token(GameText.MaintenanceCosts)} {(SelectedEmpire.BuildingAndShipMaint + SelectedEmpire.TroopCostOnPlanets).String(2)}", Color.Wheat);
                }

                if (SelectedEmpire.Research.HasTopic)
                {
                    if (SelectedEmpire.isPlayer || UsingNewEspioange && espionage.CanViewResearchTopic || IntelligenceLevel(SelectedEmpire) > 1)
                        IntelLine($"Researching: {SelectedEmpire.Research.Current.Tech.Name.Text}", Color.Wheat);
                    else if (UsingNewEspioange && espionage.CanViewTechType || IntelligenceLevel(SelectedEmpire) > 0)
                        IntelLine($"Researching: {SelectedEmpire.Research.Current.TechnologyType}", Color.Wheat);
                    else
                        IntelLine("Researching: Unknown", Color.Wheat);
                }
            }

            if (!UsingNewEspioange)
            {
                if (IntelligenceLevel(SelectedEmpire) > 1)
                    IntelLine($"{Localizer.Token(GameText.TotalSpies)} {SelectedEmpire.data.AgentList.Count}", Color.LightGreen);
                else if (IntelligenceLevel(SelectedEmpire) > 0)
                    IntelLine($"{Localizer.Token(GameText.TotalSpies)} {(SelectedEmpire.data.AgentList.Count >= Player.data.AgentList.Count ? "Many" : "Few")}", Color.Yellow);
                else
                    IntelLine($"{Localizer.Token(GameText.TotalSpies)} Unknown", Color.Pink);
            }
            else if (SelectedEmpire != Player)
            {
                IntelLine($"Their Infiltration Level: {espionage.InfiltrationLevelSummary()}", Color.Wheat);
            }

            IntelLine($"{Localizer.Token(GameText.Population2)} {GetPop(SelectedEmpire).String(1)} {Localizer.Token(GameText.Billion)}", Color.Wheat);

            if (UsingNewEspioange && espionage?.CanViewTheirMoles == true || IntelligenceLevel(SelectedEmpire) > 1)
            {
                IntelLine($"Number Of Moles: {Player.GetNumOfTheirMoles(SelectedEmpire)}", Color.Wheat);
            }

            //Diplomatic Relations
            foreach (Relationship rel in SelectedEmpire.AllRelations)
            {
                if (!rel.Known || rel.Them.IsFaction || rel.Them.IsDefeated)
                    continue;

                if (SelectedEmpire.isPlayer 
                    || UsingNewEspioange && espionage.CanViewTheirTreaties
                    || IntelligenceLevel(SelectedEmpire) > 0)
                {
                    Color color = rel.Them.EmpireColor;
                    string name = rel.Them.data.Traits.Name;
                    string andTrade = rel.Treaty_Trade ? Localizer.Token(GameText.AndTrade) : ""; // "and Trade"

                    if      (rel.Treaty_Alliance)    IntelLine($"{name}: {Localizer.Token(GameText.Alliance)} {andTrade}", color);
                    else if (rel.Treaty_OpenBorders) IntelLine($"{name}: {Localizer.Token(GameText.OpenBorders)} {andTrade}", color);
                    else if (rel.Treaty_NAPact)      IntelLine($"{name}: {Localizer.Token(GameText.NonaggressionPact2)} {andTrade}", color);
                    else if (rel.Treaty_Peace)       IntelLine($"{name}: {Localizer.Token(GameText.PeaceTreaty)} {andTrade}", color);
                    else if (rel.AtWar)              IntelLine($"{name}: {Localizer.Token(GameText.AtWar)} {andTrade}", color);
                }
            }

            if (SelectedEmpire.isPlayer || (UsingNewEspioange && espionage?.CanViewTraitSet == true) || IntelligenceLevel(SelectedEmpire) > 1)
            {
                IntelBox.AddLine("", Font12, Color.Wheat);
                IntelLine($"Racial Traits: {RacialTrait.ShownTraitNames(SelectedEmpire.data.SelectedTraitSet)}", SelectedEmpire.EmpireColor);
            }
        }

        void IntelLine(string text, Color color) => IntelBox.AddLines(text, Font12, color);

        void ListBonuses()
        {
            BonusBox.Clear();
            Espionage espionage = SelectedEmpire.isPlayer || !UsingNewEspioange ? null : Player.GetEspionage(SelectedEmpire);
            //Added by McShooterz: Only display modified bonuses
            if (SelectedEmpire.isPlayer 
                || UsingNewEspioange && espionage.CanViewBonuses 
                || IntelligenceLevel(SelectedEmpire) > 0)
            {
                if (SelectedEmpire.data.Traits.PopGrowthMax > 0f)
                    AddStat(Localizer.Token(GameText.MaximumPopulationGrowth), "+"+SelectedEmpire.data.Traits.PopGrowthMax.ToString(".##"), Color.LightPink);
                if (SelectedEmpire.data.Traits.PopGrowthMin > 0f)
                    AddStat(Localizer.Token(GameText.MinimumPopulationGrowth), "+"+SelectedEmpire.data.Traits.PopGrowthMin.ToString(".##"), Color.LightGreen);
                if (SelectedEmpire.data.Traits.ReproductionMod != 0)
                    AddStat(Localizer.Token(GameText.PopulationGrowthModifier), SelectedEmpire.data.Traits.ReproductionMod, false);
                if (SelectedEmpire.data.Traits.ConsumptionModifier != 0)
                    AddStat(Localizer.Token(GameText.FoodConsumptionModifier), SelectedEmpire.data.Traits.ConsumptionModifier, true);
                if (SelectedEmpire.data.Traits.ProductionMod != 0)
                    AddStat(Localizer.Token(GameText.ProductionModifier), SelectedEmpire.data.Traits.ProductionMod, false);
                if (SelectedEmpire.data.Traits.ResearchMod != 0)
                    AddStat(Localizer.Token(GameText.ResearchModifier), SelectedEmpire.data.Traits.ResearchMod, false);
                if (SelectedEmpire.data.Traits.DiplomacyMod != 0)
                    AddStat(Localizer.Token(GameText.DiplomacyModifier), SelectedEmpire.data.Traits.DiplomacyMod, false);
                if (SelectedEmpire.data.OngoingDiplomaticModifier != 0)
                    AddStat(Localizer.Token(GameText.OngoingDiplomacyModifier), SelectedEmpire.data.OngoingDiplomaticModifier, false);
                if (SelectedEmpire.data.Traits.GroundCombatModifier != 0)
                    AddStat(Localizer.Token(GameText.TroopStrengthModifier), SelectedEmpire.data.Traits.GroundCombatModifier, false);
                if (SelectedEmpire.data.Traits.ShipCostMod != 0)
                    AddStat(Localizer.Token(GameText.ShipCostModifier), SelectedEmpire.data.Traits.ShipCostMod, true);
                if (SelectedEmpire.data.Traits.ModHpModifier != 0)
                    AddStat(Localizer.Token(GameText.ShipHitpointsModifier), SelectedEmpire.data.Traits.ModHpModifier, false);
                //Added by McShooterz: new races stats to display in diplomacy
                if (SelectedEmpire.data.Traits.RepairMod != 0)
                    AddStat(Localizer.Token(GameText.RepairRateModifier), SelectedEmpire.data.Traits.RepairMod, false);
                if (SelectedEmpire.data.PowerFlowMod != 0)
                    AddStat(Localizer.Token(GameText.ReactorPowerModifier), SelectedEmpire.data.PowerFlowMod, false);
                if (SelectedEmpire.data.ShieldPowerMod != 0)
                    AddStat(Localizer.Token(GameText.ShieldStrengthModifier), SelectedEmpire.data.ShieldPowerMod, false);
                if (SelectedEmpire.data.MassModifier != 1)
                    AddStat(Localizer.Token(GameText.ShipMassModifier), SelectedEmpire.data.MassModifier - 1f, true);
                if (SelectedEmpire.data.Traits.TaxMod != 0)
                    AddStat(Localizer.Token(GameText.TaxIncomeModifier), SelectedEmpire.data.Traits.TaxMod, false);
                if (SelectedEmpire.data.Traits.MaintMod != 0 || SelectedEmpire.data.Traits.ShipMaintMultiplier < 1)
                {
                    if (SelectedEmpire.data.Traits.MaintMod != 0 )
                        AddStat(Localizer.Token(GameText.MaintenanceModifier), SelectedEmpire.data.Traits.MaintMod, true);

                    float shipMaintTotal = ((1 + SelectedEmpire.data.Traits.MaintMod) * SelectedEmpire.data.Traits.ShipMaintMultiplier) - 1;
                    AddStat(Localizer.Token(GameText.ShipMaintenanceModifier), shipMaintTotal, true);
                }

                AddStat(Localizer.Token(GameText.InbordersFtlBonus), SelectedEmpire.data.Traits.InBordersSpeedBonus, false);
                if (Universe.UState.P.FTLModifier != 1f)
                {
                    float fTLModifier = Universe.UState.P.FTLModifier * 100f;
                    AddStat(Localizer.Token(GameText.InsystemFtlSpeed), fTLModifier.ToString("##")+"%", Color.LightPink);
                }
                AddStat(Localizer.Token(GameText.FtlSpeedMultiplier), string.Concat(SelectedEmpire.data.FTLModifier, "x"), Color.White);
                AddStat(Localizer.Token(GameText.FtlPowerDrainModifier), string.Concat(SelectedEmpire.data.FTLPowerDrainModifier, "x"), Color.White);
                float passengers = SelectedEmpire.data.Traits.PassengerModifier;
                if (passengers > 1f)
                    AddStat(Localizer.Token(GameText.ColonistsPerCargoUnit), passengers.String(2)+"x", Color.LightGreen);
                else if (passengers < 1f)
                    AddStat(Localizer.Token(GameText.ColonistsPerCargoUnit), passengers.String(2)+"x", Color.LightPink);
                if (SelectedEmpire.data.FuelCellModifier != 0)
                    AddStat(Localizer.Token(GameText.FuelCellModifier), SelectedEmpire.data.FuelCellModifier, false);
                if (SelectedEmpire.data.SubLightModifier != 1)
                    AddStat(Localizer.Token(GameText.SublightSpeedBonus), SelectedEmpire.data.SubLightModifier - 1f, false);
                if (SelectedEmpire.data.SensorModifier != 1)
                    AddStat(Localizer.Token(GameText.SensorRangeModifier), SelectedEmpire.data.SensorModifier - 1f, false);
                if (SelectedEmpire.data.ExperienceMod != 0)
                    AddStat("Ship Experience Modifier", SelectedEmpire.data.ExperienceMod, false);
                if (SelectedEmpire.data.SpyModifier > 0f)
                    AddStat(Localizer.Token(GameText.SpyEffectivenessModifier), "+"+SelectedEmpire.data.SpyModifier.ToString("#"), Color.LightGreen);
                else if (SelectedEmpire.data.SpyModifier < 0f)
                    AddStat(Localizer.Token(GameText.SpyEffectivenessModifier), SelectedEmpire.data.SpyModifier.ToString("#"), Color.LightPink);
                if (SelectedEmpire.data.Traits.Spiritual != 0)
                    AddStat(Localizer.Token(GameText.ArtifactBonusModifier), SelectedEmpire.data.Traits.Spiritual, false);
                if (SelectedEmpire.data.Traits.TargetingModifier != 0)
                    AddStat(Localizer.Token(GameText.CannonAccuracyModifier), SelectedEmpire.data.Traits.TargetingModifier, false);
                if (SelectedEmpire.data.Traits.DodgeMod > 0)
                    AddStat(Localizer.Token(GameText.DodgeModifier), SelectedEmpire.data.Traits.DodgeMod , false);
                if (SelectedEmpire.data.OrdnanceEffectivenessBonus != 0)
                    AddStat(Localizer.Token(GameText.OrdnanceDamageradiusModifier), SelectedEmpire.data.OrdnanceEffectivenessBonus, false);
                if (SelectedEmpire.data.MissileHPModifier != 1)
                    AddStat(Localizer.Token(GameText.MissileHitpointsBonus), SelectedEmpire.data.MissileHPModifier - 1f, false);
                if (SelectedEmpire.data.MissileDodgeChance != 0)
                    AddStat(Localizer.Token(GameText.MissileDodgeChance), SelectedEmpire.data.MissileDodgeChance, false); 
                if (SelectedEmpire.data.ExoticStorageMultiplier != 1)
                    AddStat(Localizer.Token(GameText.EmpireExoticStorage), SelectedEmpire.data.ExoticStorageMultiplier-1, false);
                if (SelectedEmpire.data.MiningSpeedMultiplier != 1)
                    AddStat(Localizer.Token(GameText.EmpireMiningSpeed), SelectedEmpire.data.MiningSpeedMultiplier-1, false);
                if (SelectedEmpire.data.SalvageSpeedMultiplier != 1)
                    AddStat(Localizer.Token(GameText.EmpireSalvageSpeed), SelectedEmpire.data.SalvageSpeedMultiplier-1, false);
                if (SelectedEmpire.data.RefiningRatioMultiplier != 1)
                    AddStat(Localizer.Token(GameText.EmpireRefiningEfficiency), SelectedEmpire.data.RefiningRatioMultiplier-1, false);
            }
        }

        void AddStat(string text, string value, Color color)
        {
            BonusBox.AddElement(new BonusLine(text, value, color));
        }

        void AddStat(string text, float value, bool OppositeBonuses)
        {
            Color color;
            if (value <= 10f)
            {
                value = value * 100f;
            }
            if ((value > 0f && !OppositeBonuses) || (value < 0f && OppositeBonuses))
            {
                color = Color.LightGreen;
            }
            else
            {
                color = (value == 0f ? Color.White : Color.LightPink);
            }
            AddStat(text, value.ToString("#.##")+"%", color);
        }

        // the rows under a panel's title, starting where its text started
        static RectF RowsUnderTitle(in Rectangle panel, float rowGap)
        {
            float firstRow = panel.Y + 10 + Fonts.Arial20Bold.LineSpacing + 5;
            float top = firstRow - 15 - rowGap; // a scroll list leaves 15 px above its first row
            float left = panel.X + 20 - ScrollListBase.PaddingLeft;
            return new RectF(left, top, panel.Right - left, panel.Bottom - top);
        }

        // a bonus with its value lined up on the right
        sealed class BonusLine : UIElementV2
        {
            readonly string Label;
            readonly string Value;
            readonly Color TextColor;

            public BonusLine(string label, string value, Color color)
            {
                Label = label;
                Value = value;
                TextColor = color;
                Size = new Vector2(310f, Fonts.Arial12Bold.LineSpacing);
            }

            public override bool HandleInput(InputState input) => false;

            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                Vector2 pos = Pos.ToFloored();
                batch.DrawString(Fonts.Arial12, Label, pos, TextColor);
                var valuePos = new Vector2(pos.X + 310f - Fonts.Arial12Bold.MeasureString(Value).X, pos.Y);
                batch.DrawString(Fonts.Arial12Bold, Value, valuePos.ToFloored(), TextColor);
            }
        }

        private float GetPop(Empire e)
        {
            if (Traders.Contains(e) || e.isPlayer || UsingNewEspioange && Player.GetRelations(e).Espionage.CanViewPop)
                return e.TotalPopBillion;

            float pop = GetPopInExploredPlanetsFor(Player, e);
            foreach (Empire tradePartner in Traders)
                pop = GetPopInExploredPlanetsFor(tradePartner, e).LowerBound(pop);

            return pop;
        }

        float GetPopInExploredPlanetsFor(Empire exploringEmpire, Empire empire)
        {
            float pop = 0;
            foreach (SolarSystem system in exploringEmpire.Universe.Systems.Filter(s => s.IsExploredBy(exploringEmpire)))
            {
                foreach (Planet p in system.PlanetList)
                {
                    if (p.Owner == empire && p.IsExploredBy(exploringEmpire))
                        pop += p.PopulationBillion;
                }
            }

            return pop;
        }

        float GetScientificStr(Empire e)
        {
            if (UsingNewEspioange && (e.isPlayer || Player.GetRelations(e).Espionage.CanViewRanks))
            {
                var techs = e.UnlockedTechs;
                return techs.Length == 0 ? 0 : techs.Sum(t => t.Tech.Cost);
            }

            if (Friends.Contains(e) || e.isPlayer || IntelligenceLevel(e) > 2)
            {
                var techs = e.UnlockedTechs;
                return techs.Length == 0 ? 0 : techs.Sum(t => t.Tech.Cost);
            }

            float scientificStr = 0f;
            var techList = new HashSet<string>();
            Player.AI.ThreatMatrix.GetTechsFromPins(techList, e);
            foreach (Empire ally in Friends)
                ally.AI.ThreatMatrix.GetTechsFromPins(techList, e);

            foreach (string tech in techList)
                scientificStr += ResourceManager.Tech(tech).Cost;

            return scientificStr;
        }

        // @return false when spies have not seen the empire's artifacts
        bool ListArtifacts()
        {
            ArtifactsSL.Reset();
            if (UsingNewEspioange && SelectedEmpire != Player && !Player.GetRelations(SelectedEmpire).Espionage.CanViewArtifacts)
                return false;

            var entry = new ArtifactEntry();
            for (int i = 0; i < SelectedEmpire.data.OwnedArtifacts.Count; i++)
            {
                Artifact art = SelectedEmpire.data.OwnedArtifacts[i];
                var button = new SkinnableButton(new Rectangle(0, 0, 32, 32), $"Artifact Icons/{art.Name}")
                {
                    IsToggle = false,
                    ReferenceObject = art,
                    BaseColor = Color.White
                };

                if (entry.ArtifactButtons.Count < 5)
                {
                    entry.ArtifactButtons.Add(button);
                }
                if (entry.ArtifactButtons.Count == 5 || i == SelectedEmpire.data.OwnedArtifacts.Count - 1)
                {
                    ArtifactsSL.AddItem(new ArtifactItemListItem(entry));
                    entry = new ArtifactEntry();
                }
            }
            return true;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.I) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            if (SelectedEmpire != Player && !SelectedEmpire.IsDefeated && Contact.HandleInput(input))
            {
                DiplomacyScreen.Show(SelectedEmpire, "Greeting", parent: this);
            }

            foreach (RaceEntry race in Races)
            {
                if (HelperFunctions.ClickedRect(race.container, input))
                {
                    if (Player == race.e || !Player.IsKnown(race.e))
                    {
                        if (Player == race.e)
                            ShowEmpire(race.e);
                    }
                    else
                    {
                        ShowEmpire(race.e);
                    }
                }
            }

            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            float screenWidth = ScreenWidth;
            float screenHeight = ScreenHeight;
            Rectangle titleRect = new Rectangle((int)screenWidth / 2 - 200, 44, 400, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(Localizer.Token(GameText.DiplomaticOverview)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            LeftRect = new Rectangle((int)screenWidth / 2 - 700, (screenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1400, 774);
            Rectangle menuRect = HelperFunctions.WidenForRaceRow(LeftRect, Universe.UState.MajorEmpires.Length);
            DMenu = new Menu2(menuRect);
            Add(new CloseButton(menuRect.Right - 40, menuRect.Y + 20));
            SelectedInfoRect = new Rectangle(LeftRect.X + 60, LeftRect.Y + 250, 368, 450);
            IntelligenceRect = new Rectangle(SelectedInfoRect.X + SelectedInfoRect.Width + 30, SelectedInfoRect.Y, 368, 450);
            OperationsRect = new Rectangle(IntelligenceRect.X + IntelligenceRect.Width + 30, SelectedInfoRect.Y, 368, 450);
            IntelBox = Add(new UITextBox(RowsUnderTitle(IntelligenceRect, 2f), useBorder: false, Font12));
            IntelBox.ItemsList.ItemPadding = new(0f, 2f);
            BonusBox = Add(new UITextBox(RowsUnderTitle(OperationsRect, 0f), useBorder: false));
            BonusBox.ItemsList.ItemPadding = new(0f, 0f);
            
            RectF artifacts = new(SelectedInfoRect.X , SelectedInfoRect.Y + 250, SelectedInfoRect.Width - 40, 130);
            ArtifactsSL = Add(new ScrollList<ArtifactItemListItem>(artifacts));
            
            Contact = new DanButton(new Vector2(SelectedInfoRect.X + SelectedInfoRect.Width / 2 - 91, SelectedInfoRect.Y + SelectedInfoRect.Height - 45), Localizer.Token(GameText.Contact))
            {
                Toggled = true
            };
            foreach (Empire e in Universe.UState.Empires)
            {
                if (e != Player)
                {
                    if (e.IsFaction)
                        continue;
                }
                else
                {
                    ShowEmpire(e);
                }
                Races.Add(new RaceEntry { e = e });
            }
            Vector2 cursor = new Vector2(screenWidth / 2f - 148 * Races.Count / 2, LeftRect.Y + 10);
            int j = 0;
            foreach (RaceEntry re in Races)
            {
                re.container = new Rectangle((int)cursor.X + 10 + j * 148, LeftRect.Y + 40, 124, 148);
                j++;
            }
            GameAudio.MuteRacialMusic();

            DiagramButton = Add(new UIButton(ButtonStyle.Default, new Vector2(LeftRect.X + 70, LeftRect.Bottom - 60), "View Relationships"));
            DiagramButton.OnClick = b => AddRelationShipDiagramScreen();
        }

        void AddRelationShipDiagramScreen()
        {
            Array<EmpireAndIntelLevel> empiresAndIntel = new Array<EmpireAndIntelLevel>();
            foreach (Empire empire in Universe.UState.ActiveMajorEmpires)
            {
                int intel = empire.isPlayer ? 3 
                                            :  UsingNewEspioange ? Player.GetRelations(empire).Espionage.Level
                                                                 : IntelligenceLevel(empire);
                empiresAndIntel.Add(new EmpireAndIntelLevel(empire, intel));
            }

            var diagram = new RelationshipsDiagramScreen(this, Universe, empiresAndIntel);
            ScreenManager.AddScreen(diagram);
        }
    }

    public readonly struct EmpireAndIntelLevel
    {
        public readonly Empire Empire;
        public readonly int IntelLevel;

        public EmpireAndIntelLevel(Empire empire, int level)
        {
            Empire     = empire;
            IntelLevel = level;
        }
    }

}
