using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class MainDiplomacyScreen : GameScreen
    {
        public DanButton Contact;

        private Menu2 TitleBar;

        private Vector2 TitlePos;

        private Menu2 DMenu;

        public Rectangle SelectedInfoRect;

        public Rectangle IntelligenceRect;

        public Rectangle OperationsRect;

        public Empire SelectedEmpire;

        private Array<RaceEntry> Races = new Array<RaceEntry>();

        //private ProgressBar Penetration;

        private Rectangle ArtifactsRect;

        private ScrollList2<ArtifactItemListItem> ArtifactsSL;

        //Added by CG: player empire
        Empire PlayerEmpire;
        Array<Empire> Friends;
        Array<Empire> Traders;
        HashSet<Empire> Moles;


        public MainDiplomacyScreen(UniverseScreen screen) : base(screen)
        {
            IsPopup = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
            PlayerEmpire = EmpireManager.Player;
            Friends = EmpireManager.GetPlayerAllies();
            Traders = EmpireManager.GetTradePartners(PlayerEmpire);
            HashSet<Empire> empires = new HashSet<Empire>();            
            foreach(Empire empire in EmpireManager.Empires)
            {
                bool flag =false;
                if(empire.isPlayer || empire.isFaction)
                {
                    continue;
                }
                foreach(Planet p in empire.GetPlanets())
                {                    
                    foreach(Mole mole in PlayerEmpire.data.MoleList)
                    {
                        if(p.guid == mole.PlanetGuid)
                        {
                            flag =true;
                            empires.Add(empire);                            
                            break;
                        }


                    }
                    if(flag)
                        break;
                    foreach(Empire friend in Friends)
                    {
                        foreach (Mole mole in friend.data.MoleList)
                        {
                            if (p.guid == mole.PlanetGuid)
                            {
                                flag = true;
                                empires.Add(empire);
                                break;
                            }


                        }
                        if (flag)
                            break;
                    }
                }
            }
            Moles = empires;
        }

        private int IntelligenceLevel(Empire e)
        {
            int intelligence = 0;
            if (Friends.Contains(e) || Moles.Contains(e))
                return 2;

            if (Traders.Contains(e))
            {
                if (PlayerEmpire.GetRelations(e).Treaty_Trade_TurnsExisted > 3)
                    return 1;
            }

            if (e == PlayerEmpire)
                return 3;

            foreach(Empire empire in Friends)
            {
                if (!empire.GetRelations(e, out Relationship rel))
                    continue;
                if (rel.Treaty_Trade && rel.Treaty_Trade_TurnsExisted >3)
                {
                    intelligence = 1;                    
                }
                if (rel.Treaty_Alliance && rel.TurnsAllied >3)
                {
                    return 2;
                }
            }

            if (intelligence ==0)
            {
                foreach (Empire empire in Traders)
                {
                    if (!empire.GetRelations(e, out Relationship rel))
                        continue;
                    if (rel.Treaty_Trade && rel.Treaty_Trade_TurnsExisted > 6)
                    {
                        intelligence = 1;
                    }
                    if (rel.Treaty_Alliance && rel.TurnsAllied > 6)
                    {
                        intelligence = 2;
                        return 2;
                    }
                }
            }
            
            return intelligence;
        }
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            if (ScreenHeight > 766)
            {
                TitleBar.Draw(batch, elapsed);
                batch.DrawString(Fonts.Laserian14, Localizer.Token(GameText.DiplomaticOverview), TitlePos, Colors.Cream);
            }
            DMenu.Draw(batch, elapsed);
            foreach (RaceEntry race in Races)
            {
                if (race.e.isFaction)
                {
                    continue;
                }
                Vector2 NameCursor = new Vector2(race.container.X + 62 - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, race.container.Y + 148 + 8);
                if (race.e.data.Defeated)
                {
                    if (race.e.data.AbsorbedBy == null)
                    {
                        batch.Draw(ResourceManager.Texture("Portraits/"+race.e.data.PortraitName), race.container, Color.White);
                        batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        batch.DrawDropShadowText1(race.e.data.Traits.Name, NameCursor, Fonts.Arial12Bold, race.e.EmpireColor);
                        batch.Draw(ResourceManager.Texture("NewUI/x_red"), race.container, Color.White);
                    }
                    else
                    {
                        batch.Draw(ResourceManager.Texture("Portraits/"+race.e.data.PortraitName), race.container, Color.White);
                        batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        batch.DrawDropShadowText1(race.e.data.Traits.Name, NameCursor, Fonts.Arial12Bold, race.e.EmpireColor);
                        var r = new Rectangle(race.container.X, race.container.Y, 124, 124);
                        var e = EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy);
                        batch.Draw(ResourceManager.Flag(e.data.Traits.FlagIndex), r, e.EmpireColor);
                    }
                }
                else if (EmpireManager.Player != race.e && EmpireManager.Player.IsKnown(race.e))
                {
                    if (EmpireManager.Player.IsAtWarWith(race.e) && !race.e.data.Defeated)
                    {
                        Rectangle war = new Rectangle(race.container.X - 2, race.container.Y - 2, race.container.Width + 4, race.container.Height + 4);
                        batch.FillRectangle(war, Color.Red);
                    }
                    batch.Draw(ResourceManager.Texture("Portraits/"+race.e.data.PortraitName), race.container, Color.White);
                    batch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    batch.DrawDropShadowText1(race.e.data.Traits.Name, NameCursor, Fonts.Arial12Bold, race.e.EmpireColor);
                }
                else if (EmpireManager.Player != race.e)
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
            if (EmpireManager.Player == SelectedEmpire && !SelectedEmpire.data.Defeated)
            {
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.You), textCursor, Color.White);
                Vector2 ColumnBCursor = textCursor;
                ColumnBCursor.X = ColumnBCursor.X + 190f;
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                var Sortlist = new Array<Empire>();
                foreach (Empire e in EmpireManager.Empires)
                {
                    if (e.isFaction || e.data.Defeated)
                    {
                        if (SelectedEmpire == e)
                            Sortlist.Add(e);
                    }
                    else if (e != EmpireManager.Player)
                    {
                        if (EmpireManager.Player.IsKnown(e))
                            Sortlist.Add(e);
                    }
                    else
                    {
                        Sortlist.Add(e);
                    }
                }
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.EconomicStrength), textCursor, Color.White);
                IOrderedEnumerable<Empire> MoneySortedList = 
                    from empire in Sortlist
                    orderby empire.GrossIncome descending
                    select empire;
                int rank = 1;
                foreach (Empire e in MoneySortedList)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }
                batch.DrawString(Fonts.Arial12Bold, "# "+rank.ToString(), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> ResSortedList = 
                    from empire in Sortlist
                    orderby GetScientificStr(empire) descending
                    select empire;
                rank = 1;
                foreach (Empire e in ResSortedList)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.ScientificStrength), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> MilSorted = 
                    from empire in Sortlist
                    orderby empire.CurrentMilitaryStrength descending
                    select empire;
                rank = 1;
                foreach (Empire e in MilSorted)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.MilitaryStrength), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, "# "+rank.ToString(), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> PopSortedList = 
                    from empire in Sortlist
                    orderby GetPop(empire) descending
                    select empire;
                rank = 1;
                foreach (Empire e in PopSortedList)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }

                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Population), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                Rectangle ArtifactsRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 210, SelectedInfoRect.Width - 40, 130);
                Vector2 ArtifactsCursor = new Vector2(ArtifactsRect.X, ArtifactsRect.Y - 8);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.OwnedArtifacts), ArtifactsCursor, Color.White);
                ArtifactsCursor.Y += Fonts.Arial12Bold.LineSpacing;
            }
            else if (SelectedEmpire.data.Defeated)
            {
                if (SelectedEmpire.data.AbsorbedBy != null)
                {
                    Empire absorbingEmpire = EmpireManager.GetEmpireByName(SelectedEmpire.data.AbsorbedBy);
                    batch.DrawString(Fonts.Arial12Bold, absorbingEmpire.data.Traits.Singular+" Federation", textCursor, Color.White);
                    textCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                }
            }
            else if (!SelectedEmpire.data.Defeated)
            {
                Relationship relation = EmpireManager.Player.GetRelations(SelectedEmpire);
                float intelligencePenetration = relation.IntelligencePenetration;
                if (IntelligenceLevel(SelectedEmpire) > 0)
                {
                    batch.DrawString(Fonts.Arial12Bold, string.Concat(SelectedEmpire.data.DiplomaticPersonality.Name, " ", SelectedEmpire.data.EconomicPersonality.Name), textCursor, Color.White);
                    textCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                }
                else
                {
                    batch.DrawString(Fonts.Arial12Bold, string.Concat("Unknown", " ", "Unknown"), textCursor, Color.White);
                    textCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (relation.AtWar)
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.AtWar), textCursor, Color.LightPink);
                }
                else if (relation.Treaty_Peace)
                {
                    SpriteBatch spriteBatch2 = batch;
                    SpriteFont arial12Bold = Fonts.Arial12Bold;
                    object[] objArray = { Localizer.Token(GameText.PeaceTreaty), " (", relation.PeaceTurnsRemaining, " ", Localizer.Token(GameText.Turns), ")" };
                    spriteBatch2.DrawString(arial12Bold, string.Concat(objArray), textCursor, Color.LightGreen);
                    textCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (relation.Treaty_OpenBorders)
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.OpenBorders), textCursor, Color.LightGreen);
                    textCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (relation.Treaty_Trade)
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.TradeTreaty2), textCursor, Color.LightGreen);
                    textCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (relation.Treaty_NAPact)
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.NonaggressionPact2), textCursor, Color.LightGreen);
                    textCursor.Y += (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (relation.Treaty_Alliance)
                {
                    batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Alliance), textCursor, Color.LightGreen);
                }
                Rectangle ArtifactsRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 210, SelectedInfoRect.Width - 40, 130);
                Vector2 ArtifactsCursor = new Vector2(ArtifactsRect.X, ArtifactsRect.Y - 8);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.OwnedArtifacts), ArtifactsCursor, Color.White);
                ArtifactsCursor.Y += Fonts.Arial12Bold.LineSpacing;

                var Sortlist = new Array<Empire>();
                foreach (Empire e in EmpireManager.Empires)
                {
                    if (e.isFaction || e.data.Defeated)
                    {
                        if (SelectedEmpire == e)
                            Sortlist.Add(e);
                    }
                    else if (e != EmpireManager.Player)
                    {
                        if (EmpireManager.Player.IsKnown(e))
                            Sortlist.Add(e);
                    }
                    else
                    {
                        Sortlist.Add(e);
                    }
                }
                Contact.Draw(ScreenManager);
                Vector2 ColumnBCursor = textCursor;
                ColumnBCursor.X = ColumnBCursor.X + 190f;
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.EconomicStrength), textCursor, Color.White);
                IOrderedEnumerable<Empire> MoneySortedList = 
                    from empire in Sortlist
                    orderby empire.GrossIncome descending
                    select empire;
                int rank = 1;

                foreach (Empire e in MoneySortedList)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }
                batch.DrawString(Fonts.Arial12Bold, "# "+rank.ToString(), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> ResSortedList = 
                    from empire in Sortlist
                    orderby GetScientificStr(empire) descending
                    select empire;
                rank = 1;
                foreach (Empire e in ResSortedList)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.ScientificStrength), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> MilSorted = 
                    from empire in Sortlist
                    orderby empire.CurrentMilitaryStrength descending
                    select empire;
                rank = 1;
                foreach (Empire e in MilSorted)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.MilitaryStrength), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, "# "+rank.ToString(), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> PopSortedList =
                    from empire in Sortlist
                    orderby GetPop(empire) descending
                    select empire;
                rank = 1;
                foreach (Empire e in PopSortedList)
                {
                    if (e == SelectedEmpire)
                    {
                        break;
                    }
                    rank++;
                }

                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Population), textCursor, Color.White);
                batch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
            }
            textCursor = new Vector2(IntelligenceRect.X + 20, IntelligenceRect.Y + 10);
            batch.DrawDropShadowText(Localizer.Token(GameText.IntelligenceReport), textCursor, Fonts.Arial20Bold);
            textCursor.Y += (Fonts.Arial20Bold.LineSpacing + 5);
            if (IntelligenceLevel(SelectedEmpire) > 0)
            {
                batch.DrawString(Fonts.Arial12, Localizer.Token(GameText.HomeWorld)+SelectedEmpire.data.Traits.HomeworldName, textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
            }
            //Added by McShooterz:  intel report
            if (IntelligenceLevel(SelectedEmpire) > 0)
            {
                if (SelectedEmpire.Capital != null)
                {
                    batch.DrawString(Fonts.Arial12, Localizer.Token(GameText.ControlsHomeWorld)+((SelectedEmpire.Capital.Owner == SelectedEmpire) ? Localizer.Token(GameText.Yes) : Localizer.Token(GameText.No)), textCursor, Color.Wheat);
                    textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                }
                batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(GameText.TotalPlanets), SelectedEmpire.GetPlanets().Count), textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(GameText.TotalStarships), SelectedEmpire.GetShips().Count), textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                batch.DrawString(Fonts.Arial12, Localizer.Token(GameText.Treasury)+SelectedEmpire.Money.String(2), textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                batch.DrawString(Fonts.Arial12, Localizer.Token(GameText.MaintenanceCosts)+SelectedEmpire.BuildingAndShipMaint.String(2), textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);

                if (SelectedEmpire.Research.HasTopic)
                {
                    if (IntelligenceLevel(SelectedEmpire)>1)
                    {
                        batch.DrawString(Fonts.Arial12, "Researching: "+Localizer.Token(SelectedEmpire.Research.Current.Tech.NameIndex), textCursor, Color.Wheat);
                    }
                    else if (IntelligenceLevel(SelectedEmpire) >0)
                    {
                        batch.DrawString(Fonts.Arial12, "Researching: "+SelectedEmpire.Research.Current.TechnologyType, textCursor, Color.Wheat);
                    }
                    else
                    {
                        batch.DrawString(Fonts.Arial12, "Researching: Unknown", textCursor, Color.Wheat);
                    }
                    textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                }
            }
            if (IntelligenceLevel(SelectedEmpire)>1)
            {
                batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(GameText.TotalSpies), SelectedEmpire.data.AgentList.Count), textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
            }
            else if (IntelligenceLevel(SelectedEmpire)>0)
            {
                batch.DrawString(Fonts.Arial12, Localizer.Token(GameText.TotalSpies)+(SelectedEmpire.data.AgentList.Count >=PlayerEmpire.data.AgentList.Count ? "Many":"Few" ), textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
            }
            else 
            {
                batch.DrawString(Fonts.Arial12, Localizer.Token(GameText.TotalSpies)+"Unknown", textCursor, Color.Wheat);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
            }
            batch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(GameText.Population2), GetPop(SelectedEmpire).String(1), Localizer.Token(GameText.Billion)) + " ", textCursor, Color.Wheat);
            //Diplomatic Relations
            foreach ((Empire other, Relationship rel) in SelectedEmpire.AllRelations)
            {
                if (!rel.Known || other.isFaction)
                    continue;

                Color color = other.EmpireColor;
                string name = other.data.Traits.Name;
                if (other.data.Defeated)
                {
                    textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                    batch.DrawString(Fonts.Arial12, name+Localizer.Token(GameText.GovernmentDestroyed), textCursor, color);
                    continue;
                }
                if (IntelligenceLevel(SelectedEmpire) > 0)
                {
                    // "and Trade"
                    string andTrade = rel.Treaty_Trade ? Localizer.Token(GameText.AndTrade) : "";
                    if (rel.Treaty_Alliance)
                    {
                        textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                        batch.DrawString(Fonts.Arial12, string.Concat(name, ": ", Localizer.Token(GameText.Alliance), andTrade), textCursor, color);
                    }
                    else if (rel.Treaty_OpenBorders)
                    {
                        textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                        batch.DrawString(Fonts.Arial12, string.Concat(name, ": ", Localizer.Token(GameText.OpenBorders), andTrade), textCursor, color);
                    }
                    else if (rel.Treaty_NAPact)
                    {
                        textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                        batch.DrawString(Fonts.Arial12, string.Concat(name, ": ", Localizer.Token(GameText.NonaggressionPact2), andTrade), textCursor, color);
                    }
                    else if (rel.Treaty_Peace)
                    {
                        textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                        batch.DrawString(Fonts.Arial12, string.Concat(name, ": ", Localizer.Token(GameText.PeaceTreaty), andTrade), textCursor, color);
                    }
                    else if (rel.AtWar)
                    {
                        textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                        batch.DrawString(Fonts.Arial12, string.Concat(name, ": ", Localizer.Token(GameText.AtWar)), textCursor, color);
                    }
                    else
                    {
                        textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                        batch.DrawString(Fonts.Arial12, name + " " + (rel.Treaty_Trade ? Localizer.Token(GameText.None2) : Localizer.Token(GameText.MilitaryStrength)), textCursor, color);
                    }
                }
            }
            //End of intel report
            textCursor = new Vector2(OperationsRect.X + 20, OperationsRect.Y + 10);
            batch.DrawDropShadowText((SelectedEmpire == EmpireManager.Player ? Localizer.Token(GameText.YourEmpiresBonuses) : Localizer.Token(GameText.TheirBonuses)), textCursor, Fonts.Arial20Bold);
            textCursor.Y = textCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
            //Added by McShooterz: Only display modified bonuses
            if (IntelligenceLevel(SelectedEmpire)>0)
            {
                if (SelectedEmpire.data.Traits.PopGrowthMax > 0f)
                    DrawBadStat(Localizer.Token(GameText.MaximumPopulationGrowth), "+"+SelectedEmpire.data.Traits.PopGrowthMax.ToString(".##"), ref textCursor);
                if (SelectedEmpire.data.Traits.PopGrowthMin > 0f)
                    DrawGoodStat(Localizer.Token(GameText.MinimumPopulationGrowth), "+"+SelectedEmpire.data.Traits.PopGrowthMin.ToString(".##"), ref textCursor);
                if (SelectedEmpire.data.Traits.ReproductionMod != 0)
                    DrawStat(Localizer.Token(GameText.PopulationGrowthModifier), SelectedEmpire.data.Traits.ReproductionMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.ConsumptionModifier != 0)
                    DrawStat(Localizer.Token(GameText.FoodConsumptionModifier), SelectedEmpire.data.Traits.ConsumptionModifier, ref textCursor, true);
                if (SelectedEmpire.data.Traits.ProductionMod != 0)
                    DrawStat(Localizer.Token(GameText.ProductionModifier), SelectedEmpire.data.Traits.ProductionMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.ResearchMod != 0)
                    DrawStat(Localizer.Token(GameText.ResearchModifier), SelectedEmpire.data.Traits.ResearchMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.DiplomacyMod != 0)
                    DrawStat(Localizer.Token(GameText.DiplomacyModifier), SelectedEmpire.data.Traits.DiplomacyMod, ref textCursor, false);
                if (SelectedEmpire.data.OngoingDiplomaticModifier != 0)
                    DrawStat(Localizer.Token(GameText.OngoingDiplomacyModifier), SelectedEmpire.data.OngoingDiplomaticModifier, ref textCursor, false);
                if (SelectedEmpire.data.Traits.GroundCombatModifier != 0)
                    DrawStat(Localizer.Token(GameText.TroopStrengthModifier), SelectedEmpire.data.Traits.GroundCombatModifier, ref textCursor, false);
                if (SelectedEmpire.data.Traits.ShipCostMod != 0)
                    DrawStat(Localizer.Token(GameText.ShipCostModifier), SelectedEmpire.data.Traits.ShipCostMod, ref textCursor, true);
                if (SelectedEmpire.data.Traits.ModHpModifier != 0)
                    DrawStat(Localizer.Token(GameText.ShipHitpointsModifier), SelectedEmpire.data.Traits.ModHpModifier, ref textCursor, false);
                //Added by McShooterz: new races stats to display in diplomacy
                if (SelectedEmpire.data.Traits.RepairMod != 0)
                    DrawStat(Localizer.Token(GameText.RepairRateModifier), SelectedEmpire.data.Traits.RepairMod, ref textCursor, false);
                if (SelectedEmpire.data.PowerFlowMod != 0)
                    DrawStat(Localizer.Token(GameText.ReactorPowerModifier), SelectedEmpire.data.PowerFlowMod, ref textCursor, false);
                if (SelectedEmpire.data.ShieldPowerMod != 0)
                    DrawStat(Localizer.Token(GameText.ShieldStrengthModifier), SelectedEmpire.data.ShieldPowerMod, ref textCursor, false);
                if (SelectedEmpire.data.MassModifier != 1)
                    DrawStat(Localizer.Token(GameText.ShipMassModifier), SelectedEmpire.data.MassModifier - 1f, ref textCursor, true);
                if (SelectedEmpire.data.Traits.TaxMod != 0)
                    DrawStat(Localizer.Token(GameText.TaxIncomeModifier), SelectedEmpire.data.Traits.TaxMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.MaintMod != 0)
                    DrawStat(Localizer.Token(GameText.MaintenanceModifier), SelectedEmpire.data.Traits.MaintMod, ref textCursor, true);
                DrawStat(Localizer.Token(GameText.InbordersFtlBonus), SelectedEmpire.data.Traits.InBordersSpeedBonus, ref textCursor, false);
                if (Empire.Universe.FTLModifier != 1f)
                {
                    float fTLModifier = Empire.Universe.FTLModifier * 100f;
                    DrawBadStat(Localizer.Token(GameText.InsystemFtlSpeed), fTLModifier.ToString("##")+"%", ref textCursor);
                }
                DrawStat(Localizer.Token(GameText.FtlSpeedMultiplier), string.Concat(SelectedEmpire.data.FTLModifier, "x"), ref textCursor);
                DrawStat(Localizer.Token(GameText.FtlPowerDrainModifier), string.Concat(SelectedEmpire.data.FTLPowerDrainModifier, "x"), ref textCursor);
                if (SelectedEmpire.data.FuelCellModifier != 0)
                    DrawStat(Localizer.Token(GameText.FuelCellModifier), SelectedEmpire.data.FuelCellModifier, ref textCursor, false);
                if (SelectedEmpire.data.SubLightModifier != 1)
                    DrawStat(Localizer.Token(GameText.SublightSpeedBonus), SelectedEmpire.data.SubLightModifier - 1f, ref textCursor, false);
                if (SelectedEmpire.data.SensorModifier != 1)
                    DrawStat(Localizer.Token(GameText.SensorRangeModifier), SelectedEmpire.data.SensorModifier - 1f, ref textCursor, false);
                if (SelectedEmpire.data.ExperienceMod != 0)
                    DrawStat("Ship Experience Modifier", SelectedEmpire.data.ExperienceMod, ref textCursor, false);
                if (SelectedEmpire.data.SpyModifier > 0f)
                    DrawGoodStat(Localizer.Token(GameText.SpyEffectivenessModifier), "+"+SelectedEmpire.data.SpyModifier.ToString("#"), ref textCursor);
                else if (SelectedEmpire.data.SpyModifier < 0f)
                    DrawBadStat(Localizer.Token(GameText.SpyEffectivenessModifier), "-"+SelectedEmpire.data.SpyModifier.ToString("#"), ref textCursor);
                if (SelectedEmpire.data.Traits.Spiritual != 0)
                    DrawStat(Localizer.Token(GameText.ArtifactBonusModifier), SelectedEmpire.data.Traits.Spiritual, ref textCursor, false);
                if (SelectedEmpire.data.Traits.EnergyDamageMod != 0)
                    DrawStat(Localizer.Token(GameText.CannonAccuracyModifier), SelectedEmpire.data.Traits.EnergyDamageMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.DodgeMod > 0)
                    DrawStat(Localizer.Token(GameText.DodgeModifier), SelectedEmpire.data.Traits.DodgeMod , ref textCursor, false);
                if (SelectedEmpire.data.OrdnanceEffectivenessBonus != 0)
                    DrawStat(Localizer.Token(GameText.OrdnanceDamageradiusModifier), SelectedEmpire.data.OrdnanceEffectivenessBonus, ref textCursor, false);
                if (SelectedEmpire.data.MissileHPModifier != 1)
                    DrawStat(Localizer.Token(GameText.MissileHitpointsBonus), SelectedEmpire.data.MissileHPModifier - 1f, ref textCursor, false);
                if (SelectedEmpire.data.MissileDodgeChance != 0)
                    DrawStat(Localizer.Token(GameText.MissileDodgeChance), SelectedEmpire.data.MissileDodgeChance, ref textCursor, false); 
            }
            base.Draw(batch, elapsed);
            batch.End();
        }

        private void DrawBadStat(string text, string text2, ref Vector2 Position)
        {
            HelperFunctions.ClampVectorToInt(ref Position);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.LightPink);
            Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
            //{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
            //};
            HelperFunctions.ClampVectorToInt(ref nPos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.LightPink);
            Position.Y = Position.Y + (Fonts.Arial12Bold.LineSpacing + 2);
        }

        private void DrawGoodStat(string text, string text2, ref Vector2 Position)
        {
            HelperFunctions.ClampVectorToInt(ref Position);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.LightGreen);
            Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
            //{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
            //};
            HelperFunctions.ClampVectorToInt(ref nPos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.LightGreen);
            Position.Y = Position.Y + (Fonts.Arial12Bold.LineSpacing + 2);
        }

        private void DrawStat(string text, float value, ref Vector2 Position, bool OppositeBonuses)
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
            HelperFunctions.ClampVectorToInt(ref Position);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, color);

            string valuePercent = value.ToString("#.##")+"%";
            var nPos = new Vector2(Position.X + 310f, Position.Y);
            nPos.X -= Fonts.Arial12Bold.MeasureString(valuePercent).X;

            HelperFunctions.ClampVectorToInt(ref nPos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, valuePercent, nPos, color);
            Position.Y += Fonts.Arial12Bold.LineSpacing;
        }

        private void DrawStat(string text, string text2, ref Vector2 Position)
        {
            HelperFunctions.ClampVectorToInt(ref Position);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, text, Position, Color.White);
            Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
            //{
                nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(text2).X;
            //};
            HelperFunctions.ClampVectorToInt(ref nPos);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text2, nPos, Color.White);
            Position.Y = Position.Y + (Fonts.Arial12Bold.LineSpacing + 2);
        }

        private float GetPop(Empire e)
        {
            if (Traders.Contains(e) || e.isPlayer)
                return e.TotalPopBillion;

            float pop = GetPopInExploredPlanetsFor(PlayerEmpire, e);
            foreach (Empire tradePartner in Traders)
                pop = GetPopInExploredPlanetsFor(tradePartner, e).LowerBound(pop);

            return pop;
        }

        float GetPopInExploredPlanetsFor(Empire exploringEmpire, Empire empire)
        {
            float pop = 0;
            foreach (SolarSystem system in UniverseScreen.SolarSystemList.Filter(s => s.IsExploredBy(exploringEmpire)))
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
            float scientificStr = 0f;
            if (Friends.Contains(e) || e.isPlayer)
            {
                var techs = e.TechEntries.Filter(t => t.Unlocked);
                return techs.Length == 0 ? 0 : techs.Sum(t => t.Tech.Cost);
            }

            var techList = new HashSet<string>();
            PlayerEmpire.GetEmpireAI().ThreatMatrix.GetTechsFromPins(techList, e);
            foreach (Empire ally in Friends)
                ally.GetEmpireAI().ThreatMatrix.GetTechsFromPins(techList, e);

            foreach (string tech in techList)
                scientificStr += ResourceManager.Tech(tech).Cost;

            return scientificStr;
        }

        void CreateArtifactsScrollList(Empire empire)
        {
            SelectedEmpire = empire;
            ArtifactsSL.Reset();

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
            GameAudio.EchoAffirmative();
        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeyPressed(Keys.I) && !GlobalStats.TakingInput)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            if (SelectedEmpire != EmpireManager.Player && !SelectedEmpire.data.Defeated && Contact.HandleInput(input))
            {
                DiplomacyScreen.Show(SelectedEmpire, "Greeting", parent: this);
            }

            foreach (RaceEntry race in Races)
            {
                if (HelperFunctions.ClickedRect(race.container, input))
                {
                    if (EmpireManager.Player == race.e || !EmpireManager.Player.IsKnown(race.e))
                    {
                        if (EmpireManager.Player == race.e)
                            CreateArtifactsScrollList(race.e);
                    }
                    else
                    {
                        CreateArtifactsScrollList(race.e);
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
            Rectangle leftRect = new Rectangle((int)screenWidth / 2 - 700, (screenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1400, 660);
            DMenu = new Menu2(leftRect);
            Add(new CloseButton(leftRect.Right - 40, leftRect.Y + 20));
            SelectedInfoRect = new Rectangle(leftRect.X + 60, leftRect.Y + 250, 368, 376);
            IntelligenceRect = new Rectangle(SelectedInfoRect.X + SelectedInfoRect.Width + 30, SelectedInfoRect.Y, 368, 376);
            OperationsRect = new Rectangle(IntelligenceRect.X + IntelligenceRect.Width + 30, SelectedInfoRect.Y, 368, 376);
            ArtifactsRect = new Rectangle(SelectedInfoRect.X , SelectedInfoRect.Y + 190, SelectedInfoRect.Width - 40, 130);
            ArtifactsSL = new ScrollList2<ArtifactItemListItem>(ArtifactsRect);
            Add(ArtifactsSL);
            Contact = new DanButton(new Vector2(SelectedInfoRect.X + SelectedInfoRect.Width / 2 - 91, SelectedInfoRect.Y + SelectedInfoRect.Height - 45), Localizer.Token(GameText.Contact))
            {
                Toggled = true
            };
            foreach (Empire e in EmpireManager.Empires)
            {
                if (e != EmpireManager.Player)
                {
                    if (e.isFaction)
                        continue;
                }
                else
                {
                    CreateArtifactsScrollList(e);
                }
                Races.Add(new RaceEntry { e = e });
            }
            Vector2 Cursor = new Vector2(screenWidth / 2f - 148 * Races.Count / 2, leftRect.Y + 10);
            int j = 0;
            foreach (RaceEntry re in Races)
            {
                re.container = new Rectangle((int)Cursor.X + 10 + j * 148, leftRect.Y + 40, 124, 148);
                j++;
            }
            GameAudio.MuteRacialMusic();
        }
    }
}
