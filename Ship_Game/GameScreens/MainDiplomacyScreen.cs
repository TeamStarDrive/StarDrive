using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class MainDiplomacyScreen : GameScreen
    {
        private UniverseScreen screen;

        public DanButton Contact;

        private Menu2 TitleBar;

        private Vector2 TitlePos;

        private Menu2 DMenu;

        public bool LowRes;

        public Rectangle SelectedInfoRect;

        public Rectangle IntelligenceRect;

        public Rectangle OperationsRect;

        public Empire SelectedEmpire;

        private Array<RaceEntry> Races = new Array<RaceEntry>();

        //private ProgressBar Penetration;

        private Rectangle ArtifactsRect;

        private ScrollList ArtifactsSL;

        private CloseButton close;

        private float TransitionElapsedTime;

        //Added by CG: player empire
        Empire PlayerEmpire;
        Array<Empire> Friends;
        Array<Empire> Traders;
        HashSet<Empire> Moles;


        public MainDiplomacyScreen(UniverseScreen screen) : base(screen)
        {			
            this.screen = screen;                      

            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            PlayerEmpire = EmpireManager.Player;
            Friends = EmpireManager.GetAllies(PlayerEmpire);
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
                Relationship rel;
                if (!empire.TryGetRelations(e, out rel))
                    continue;
                if(rel.Treaty_Trade && rel.Treaty_Trade_TurnsExisted >3)
                {
                    intelligence = 1;                    
                }
                if (rel.Treaty_Alliance && rel.TurnsAllied >3)
                {
                    return 2;
                }
            }
            if(intelligence ==0)
            foreach (Empire empire in Traders)
            {
                Relationship rel;
                if (!empire.TryGetRelations(e, out rel))
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


            
            
            return intelligence;
        }
        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            ScreenManager.SpriteBatch.Begin();
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 766)
            {
                TitleBar.Draw(batch);
                ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, Localizer.Token(1600), TitlePos, new Color(255, 239, 208));
            }
            DMenu.Draw(batch);
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
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/x_red"), race.container, Color.White);
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                        var r = new Rectangle(race.container.X, race.container.Y, 124, 124);
                        var e = EmpireManager.GetEmpireByName(race.e.data.AbsorbedBy);
                        batch.Draw(ResourceManager.Flag(e.data.Traits.FlagIndex), r, e.EmpireColor);
                    }
                }
                else if (EmpireManager.Player != race.e && EmpireManager.Player.GetRelations(race.e).Known)
                {
                    if (EmpireManager.Player.GetRelations(race.e).AtWar && !race.e.data.Defeated)
                    {
                        Rectangle war = new Rectangle(race.container.X - 2, race.container.Y - 2, race.container.Width + 4, race.container.Height + 4);
                        ScreenManager.SpriteBatch.FillRectangle(war, Color.Red);
                    }
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                }
                else if (EmpireManager.Player != race.e)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/unknown"), race.container, Color.White);
                }
                else
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(string.Concat("Portraits/", race.e.data.PortraitName)), race.container, Color.White);
                    ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Portraits/portrait_shine"), race.container, Color.White);
                    NameCursor = new Vector2(race.container.X + 62 - Fonts.Arial12Bold.MeasureString(race.e.data.Traits.Name).X / 2f, race.container.Y + 148 + 8);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, race.e.data.Traits.Name, NameCursor, Color.White);
                }
                if (race.e != SelectedEmpire)
                {
                    continue;
                }
                ScreenManager.SpriteBatch.DrawRectangle(race.container, Color.Orange);
            }
            ScreenManager.SpriteBatch.FillRectangle(SelectedInfoRect, new Color(23, 20, 14));
            ScreenManager.SpriteBatch.FillRectangle(IntelligenceRect, new Color(23, 20, 14));
            ScreenManager.SpriteBatch.FillRectangle(OperationsRect, new Color(23, 20, 14));
            var textCursor = new Vector2(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 10);
            HelperFunctions.DrawDropShadowText(batch, SelectedEmpire.data.Traits.Name, textCursor, Fonts.Arial20Bold);
            var flagRect = new Rectangle(SelectedInfoRect.X + SelectedInfoRect.Width - 60, SelectedInfoRect.Y + 10, 40, 40);
            batch.Draw(ResourceManager.Flag(SelectedEmpire.data.Traits.FlagIndex), flagRect, SelectedEmpire.EmpireColor);
            textCursor.Y += (Fonts.Arial20Bold.LineSpacing + 4);
            if (EmpireManager.Player == SelectedEmpire && !SelectedEmpire.data.Defeated)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1601), textCursor, Color.White);
                Vector2 ColumnBCursor = textCursor;
                ColumnBCursor.X = ColumnBCursor.X + 190f;
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                var Sortlist = new Array<Empire>();
                foreach (Empire e in EmpireManager.Empires)
                {
                    if (e.isFaction || e.data.Defeated)
                    {
                        if (SelectedEmpire != e)
                        {
                            continue;
                        }
                        Sortlist.Add(e);
                    }
                    else if (e != EmpireManager.Player)
                    {
                        if (!EmpireManager.Player.GetRelations(e).Known)
                        {
                            continue;
                        }
                        Sortlist.Add(e);
                    }
                    else
                    {
                        Sortlist.Add(e);
                    }
                }
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1613), textCursor, Color.White);
                IOrderedEnumerable<Empire> MoneySortedList = 
                    from empire in Sortlist
                    orderby empire.GrossIncome() descending
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1602), textCursor, Color.White);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> MilSorted = 
                    from empire in Sortlist
                    orderby empire.currentMilitaryStrength descending
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1605), textCursor, Color.White);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(385), textCursor, Color.White);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                Rectangle ArtifactsRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 210, SelectedInfoRect.Width - 40, 130);
                Vector2 ArtifactsCursor = new Vector2(ArtifactsRect.X, ArtifactsRect.Y - 8);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1607), ArtifactsCursor, Color.White);
                ArtifactsCursor.Y = ArtifactsCursor.Y + Fonts.Arial12Bold.LineSpacing;
                ArtifactsSL.Draw(ScreenManager.SpriteBatch);
                foreach (ScrollList.Entry e in ArtifactsSL.VisibleEntries)
                {
                    ArtifactsCursor.Y = e.Y;
                    var art = e.Get<ArtifactEntry>();
                    art.Update(ArtifactsCursor);
                    foreach (SkinnableButton button in art.ArtifactButtons)
                    {
                        button.Draw(ScreenManager);
                    }
                }
            }
            else if (SelectedEmpire.data.Defeated)
            {
                if (SelectedEmpire.data.AbsorbedBy != null)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(EmpireManager.GetEmpireByName(SelectedEmpire.data.AbsorbedBy).data.Traits.Singular, " Federation"), textCursor, Color.White);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                }
            }
            else if (!SelectedEmpire.data.Defeated)
            {
                float intelligencePenetration = EmpireManager.Player.GetRelations(SelectedEmpire).IntelligencePenetration;
                if (IntelligenceLevel(SelectedEmpire) > 0)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(SelectedEmpire.data.DiplomaticPersonality.Name, " ", SelectedEmpire.data.EconomicPersonality.Name), textCursor, Color.White);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                }
                else
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("Unknown", " ", "Unknown"), textCursor, Color.White);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (EmpireManager.Player.GetRelations(SelectedEmpire).AtWar)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1608), textCursor, Color.LightPink);
                }
                else if (EmpireManager.Player.GetRelations(SelectedEmpire).Treaty_Peace)
                {
                    SpriteBatch spriteBatch2 = ScreenManager.SpriteBatch;
                    SpriteFont arial12Bold = Fonts.Arial12Bold;
                    object[] objArray = { Localizer.Token(1213), " (", EmpireManager.Player.GetRelations(SelectedEmpire).PeaceTurnsRemaining, " ", Localizer.Token(2200), ")" };
                    spriteBatch2.DrawString(arial12Bold, string.Concat(objArray), textCursor, Color.LightGreen);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (EmpireManager.Player.GetRelations(SelectedEmpire).Treaty_OpenBorders)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1609), textCursor, Color.LightGreen);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (EmpireManager.Player.GetRelations(SelectedEmpire).Treaty_Trade)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1610), textCursor, Color.LightGreen);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (EmpireManager.Player.GetRelations(SelectedEmpire).Treaty_NAPact)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1611), textCursor, Color.LightGreen);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (EmpireManager.Player.GetRelations(SelectedEmpire).Treaty_Alliance)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1612), textCursor, Color.LightGreen);
                }
                Rectangle ArtifactsRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 210, SelectedInfoRect.Width - 40, 130);
                Vector2 ArtifactsCursor = new Vector2(ArtifactsRect.X, ArtifactsRect.Y - 8);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1607), ArtifactsCursor, Color.White);
                ArtifactsCursor.Y = ArtifactsCursor.Y + Fonts.Arial12Bold.LineSpacing;
                ArtifactsSL.Draw(ScreenManager.SpriteBatch);
                foreach (ScrollList.Entry e in ArtifactsSL.VisibleEntries)
                {
                    ArtifactsCursor.Y = e.Y;
                    var art = e.Get<ArtifactEntry>();
                    art.Update(ArtifactsCursor);
                    foreach (SkinnableButton button in art.ArtifactButtons)
                    {
                        button.Draw(ScreenManager);
                    }
                }
                Array<Empire> Sortlist = new Array<Empire>();
                foreach (Empire e in EmpireManager.Empires)
                {
                    if (e.isFaction || e.data.Defeated)
                    {
                        if (SelectedEmpire != e)
                        {
                            continue;
                        }
                        Sortlist.Add(e);
                    }
                    else if (e != EmpireManager.Player)
                    {
                        if (!EmpireManager.Player.GetRelations(e).Known)
                        {
                            continue;
                        }
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1613), textCursor, Color.White);
                IOrderedEnumerable<Empire> MoneySortedList = 
                    from empire in Sortlist
                    orderby empire.GrossIncome() descending
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1602), textCursor, Color.White);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank), ColumnBCursor, Color.White);
                ColumnBCursor.Y = ColumnBCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                textCursor.Y = textCursor.Y + (Fonts.Arial12Bold.LineSpacing + 2);
                IOrderedEnumerable<Empire> MilSorted = 
                    from empire in Sortlist
                    orderby empire.currentMilitaryStrength descending
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
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token(1605), textCursor, Color.White);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat("# ", rank.ToString()), ColumnBCursor, Color.White);
            }
            textCursor = new Vector2(IntelligenceRect.X + 20, IntelligenceRect.Y + 10);
            HelperFunctions.DrawDropShadowText(batch, Localizer.Token(6091), textCursor, Fonts.Arial20Bold);
            textCursor.Y = textCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
            if (IntelligenceLevel(SelectedEmpire) > 0)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6094), SelectedEmpire.data.Traits.HomeworldName), textCursor, Color.White);
                textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
            }
            //Added by McShooterz:  intel report
            if (IntelligenceLevel(SelectedEmpire) > 0)
            {
                if (SelectedEmpire.Capital != null)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6106), (SelectedEmpire.Capital.Owner == SelectedEmpire) ? Localizer.Token(6107) : Localizer.Token(1508)), textCursor, Color.White);
                    textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                }
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6095), SelectedEmpire.GetPlanets().Count), textCursor, Color.White);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6096), SelectedEmpire.GetShips().Count), textCursor, Color.White);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6097), SelectedEmpire.Money.String(2)), textCursor, Color.White);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6098), SelectedEmpire.BuildingAndShipMaint.String(2)), textCursor, Color.White);
                textCursor.Y += (Fonts.Arial12.LineSpacing + 2);

                if (!string.IsNullOrEmpty(SelectedEmpire.ResearchTopic))
                {
                    if (IntelligenceLevel(SelectedEmpire)>1)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat("Researching: ", Localizer.Token(ResourceManager.TechTree[SelectedEmpire.ResearchTopic].NameIndex)), textCursor, Color.White);
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                    }
                    else if (IntelligenceLevel(SelectedEmpire) >0)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat("Researching: ", ResourceManager.TechTree[SelectedEmpire.ResearchTopic].TechnologyType.ToString()), textCursor, Color.White);
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                    }
                    else
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat("Researching: ", "Unknown"), textCursor, Color.White);
                    textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);

                }
            }
            if (IntelligenceLevel(SelectedEmpire)>1)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6099), SelectedEmpire.data.AgentList.Count), textCursor, Color.White);
                textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
            }
            else if (IntelligenceLevel(SelectedEmpire)>0)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6099), (SelectedEmpire.data.AgentList.Count >=PlayerEmpire.data.AgentList.Count ? "Many":"Few" )), textCursor, Color.White);
                textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
            }
            else 
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6099), "Unknown"), textCursor, Color.White);
                textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
            }
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Localizer.Token(6100), GetPop(SelectedEmpire).ToString("0.0"), Localizer.Token(6101)), textCursor, Color.White);
            //Diplomatic Relations
            foreach (KeyValuePair<Empire, Relationship> Relation in SelectedEmpire.AllRelations)
            {
                if (!Relation.Value.Known || Relation.Key.isFaction)
                    continue;
                if (Relation.Key.data.Defeated)
                {
                    textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, Localizer.Token(6102)), textCursor, Color.White);
                    continue;
                }
                if (IntelligenceLevel(SelectedEmpire) >0)
                {
                    if (Relation.Value.Treaty_Alliance)
                    {
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1612), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), textCursor, Color.White);
                    }
                    else if (Relation.Value.Treaty_OpenBorders)
                    {
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1609), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), textCursor, Color.White);
                    }
                    else if (Relation.Value.Treaty_NAPact)
                    {
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1611), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), textCursor, Color.White);
                    }
                    else if (Relation.Value.Treaty_Peace)
                    {
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1213), (Relation.Value.Treaty_Trade) ? Localizer.Token(6103) : ""), textCursor, Color.White);
                    }
                    else if (Relation.Value.AtWar)
                    {
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, ": ", Localizer.Token(1608)), textCursor, Color.White);
                    }
                    else
                    {
                        textCursor.Y = textCursor.Y + (Fonts.Arial12.LineSpacing + 2);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, string.Concat(Relation.Key.data.Traits.Name, (Relation.Value.Treaty_Trade) ? Localizer.Token(6104) : Localizer.Token(6105)), textCursor, Color.White);
                    }
                }
            }
            //End of intel report
            textCursor = new Vector2(OperationsRect.X + 20, OperationsRect.Y + 10);
            HelperFunctions.DrawDropShadowText(batch, (SelectedEmpire == EmpireManager.Player ? Localizer.Token(2181) : Localizer.Token(2212)), textCursor, Fonts.Arial20Bold);
            textCursor.Y = textCursor.Y + (Fonts.Arial20Bold.LineSpacing + 5);
            //Added by McShooterz: Only display modified bonuses
            if (IntelligenceLevel(SelectedEmpire)>0)
            {
                if (SelectedEmpire.data.Traits.PopGrowthMax > 0f)
                    DrawBadStat(Localizer.Token(4041), string.Concat("+", SelectedEmpire.data.Traits.PopGrowthMax.ToString(".##")), ref textCursor);
                if (SelectedEmpire.data.Traits.PopGrowthMin > 0f)
                    DrawGoodStat(Localizer.Token(4040), string.Concat("+", SelectedEmpire.data.Traits.PopGrowthMin.ToString(".##")), ref textCursor);
                if (SelectedEmpire.data.Traits.ReproductionMod != 0)
                    DrawStat(Localizer.Token(4017), SelectedEmpire.data.Traits.ReproductionMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.ConsumptionModifier != 0)
                    DrawStat(Localizer.Token(6140), SelectedEmpire.data.Traits.ConsumptionModifier, ref textCursor, true);
                if (SelectedEmpire.data.Traits.ProductionMod != 0)
                    DrawStat(Localizer.Token(4018), SelectedEmpire.data.Traits.ProductionMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.ResearchMod != 0)
                    DrawStat(Localizer.Token(4019), SelectedEmpire.data.Traits.ResearchMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.DiplomacyMod != 0)
                    DrawStat(Localizer.Token(4020), SelectedEmpire.data.Traits.DiplomacyMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.GroundCombatModifier != 0)
                    DrawStat(Localizer.Token(4021), SelectedEmpire.data.Traits.GroundCombatModifier, ref textCursor, false);
                if (SelectedEmpire.data.Traits.ShipCostMod != 0)
                    DrawStat(Localizer.Token(4022), SelectedEmpire.data.Traits.ShipCostMod, ref textCursor, true);
                if (SelectedEmpire.data.Traits.ModHpModifier != 0)
                    DrawStat(Localizer.Token(4023), SelectedEmpire.data.Traits.ModHpModifier, ref textCursor, false);
                //Added by McShooterz: new races stats to display in diplomacy
                if (SelectedEmpire.data.Traits.RepairMod != 0)
                    DrawStat(Localizer.Token(6012), SelectedEmpire.data.Traits.RepairMod, ref textCursor, false);
                if (SelectedEmpire.data.PowerFlowMod != 0)
                    DrawStat(Localizer.Token(6014), SelectedEmpire.data.PowerFlowMod, ref textCursor, false);
                if (SelectedEmpire.data.ShieldPowerMod != 0)
                    DrawStat(Localizer.Token(6141), SelectedEmpire.data.ShieldPowerMod, ref textCursor, false);
                if (SelectedEmpire.data.MassModifier != 1)
                    DrawStat(Localizer.Token(4036), SelectedEmpire.data.MassModifier - 1f, ref textCursor, true);
                if (SelectedEmpire.data.Traits.TaxMod != 0)
                    DrawStat(Localizer.Token(4024), SelectedEmpire.data.Traits.TaxMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.MaintMod != 0)
                    DrawStat(Localizer.Token(4037), SelectedEmpire.data.Traits.MaintMod, ref textCursor, true);
                DrawStat(Localizer.Token(4025), SelectedEmpire.data.Traits.InBordersSpeedBonus, ref textCursor, false);
                if (Empire.Universe.FTLModifier != 1f)
                {
                    float fTLModifier = Empire.Universe.FTLModifier * 100f;
                    DrawBadStat(Localizer.Token(4038), string.Concat(fTLModifier.ToString("##"), "%"), ref textCursor);
                }
                DrawStat(Localizer.Token(4026), string.Concat(SelectedEmpire.data.FTLModifier, "x"), ref textCursor);
                DrawStat(Localizer.Token(4027), string.Concat(SelectedEmpire.data.FTLPowerDrainModifier, "x"), ref textCursor);
                if (SelectedEmpire.data.FuelCellModifier != 0)
                    DrawStat(Localizer.Token(4039), SelectedEmpire.data.FuelCellModifier, ref textCursor, false);
                if (SelectedEmpire.data.SubLightModifier != 1)
                    DrawStat(Localizer.Token(4028), SelectedEmpire.data.SubLightModifier - 1f, ref textCursor, false);
                if (SelectedEmpire.data.SensorModifier != 1)
                    DrawStat(Localizer.Token(4029), SelectedEmpire.data.SensorModifier - 1f, ref textCursor, false);
                if (SelectedEmpire.data.ExperienceMod != 0)
                    DrawStat("Ship Experience Modifier", SelectedEmpire.data.ExperienceMod, ref textCursor, false);
                if (SelectedEmpire.data.SpyModifier > 0f)
                    DrawGoodStat(Localizer.Token(4030), string.Concat("+", SelectedEmpire.data.SpyModifier.ToString("#")), ref textCursor);
                else if (SelectedEmpire.data.SpyModifier < 0f)
                    DrawBadStat(Localizer.Token(4030), string.Concat("-", SelectedEmpire.data.SpyModifier.ToString("#")), ref textCursor);
                if (SelectedEmpire.data.Traits.Spiritual != 0)
                    DrawStat(Localizer.Token(4031), SelectedEmpire.data.Traits.Spiritual, ref textCursor, false);
                if (SelectedEmpire.data.Traits.EnergyDamageMod != 0)
                    DrawStat(Localizer.Token(4032), SelectedEmpire.data.Traits.EnergyDamageMod, ref textCursor, false);
                if (SelectedEmpire.data.Traits.DodgeMod > 0)
                    DrawStat(Localizer.Token(1977), SelectedEmpire.data.Traits.DodgeMod , ref textCursor, false);
                if (SelectedEmpire.data.OrdnanceEffectivenessBonus != 0)
                    DrawStat(Localizer.Token(4033), SelectedEmpire.data.OrdnanceEffectivenessBonus, ref textCursor, false);
                if (SelectedEmpire.data.MissileHPModifier != 1)
                    DrawStat(Localizer.Token(4034), SelectedEmpire.data.MissileHPModifier - 1f, ref textCursor, false);
                if (SelectedEmpire.data.MissileDodgeChance != 0)
                    DrawStat(Localizer.Token(4035), SelectedEmpire.data.MissileDodgeChance, ref textCursor, false); 
            }
            close.Draw(batch);
            if (IsActive)
            {
                ToolTip.Draw(ScreenManager.SpriteBatch);
            }
            ScreenManager.SpriteBatch.End();
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
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont arial12 = Fonts.Arial12;
            string str = text;
            Vector2 position = Position;
            spriteBatch.DrawString(arial12, str, position, color);
            Vector2 nPos = new Vector2(Position.X + 310f, Position.Y);
            //{
            nPos.X = nPos.X - Fonts.Arial12Bold.MeasureString(string.Concat(value.ToString("#.##"), "%")).X;
            //};
            HelperFunctions.ClampVectorToInt(ref nPos);
            SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            string str1 = string.Concat(value.ToString("#.##"), "%");
            Vector2 vector2 = nPos;
            spriteBatch1.DrawString(arial12Bold, str1, vector2, color);
            Position.Y = Position.Y + Fonts.Arial12Bold.LineSpacing;
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

        private float GetMilitaryStr(Empire e)
        {
            float single;
            float str = 0f;
            try
            {
                HashSet<Ship> knownShips = new HashSet<Ship>();
                if (Friends.Contains(e))
                {
                    foreach (Ship ship in e.GetShips())
                    {
                        str += ship.GetStrength();
                    }
                    return str;
                }
                foreach(ThreatMatrix.Pin pins in PlayerEmpire.GetEmpireAI().ThreatMatrix.Pins.Values)
                {
                    if (pins.Ship == null || pins.Ship.loyalty != e)
                        continue;
                    knownShips.Add(pins.Ship);
                }

                foreach(Empire ally in Friends)
                {
                    foreach (ThreatMatrix.Pin pins in ally.GetEmpireAI().ThreatMatrix.Pins.Values)
                    {
                        if (pins.Ship == null || pins.Ship.loyalty != e)
                            continue;
                        knownShips.Add(pins.Ship);
                    }
                }
                foreach(Ship ship in knownShips)
                
                {
                    str = str + ship.GetStrength();
                }
                return str;
            }
            catch
            {
                single = str;
            }
            return single;
        }

        private float GetPop(Empire e)
        {
            float pop = 0f;
            var planets = new HashSet<Planet>();

            if (Traders.Contains(e))
            {
                foreach (Planet p in e.GetPlanets())
                    pop += p.Population;
                return pop / 1000f;
            }
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                if (!system.IsExploredBy(PlayerEmpire))
                    continue;
                foreach (Planet p in system.PlanetList)
                {
                    if (p.Owner == e && p.IsExploredBy(PlayerEmpire))
                        planets.Add(p);
                }
            }
            foreach (Empire ally in Traders)
            {
                foreach (SolarSystem system in UniverseScreen.SolarSystemList)
                {
                    if (!system.IsExploredBy(ally))
                        continue;
                    foreach (Planet p in system.PlanetList)
                    {
                        if (p.Owner == e && p.IsExploredBy(ally))
                            planets.Add(p);
                    }
                }
            }

            foreach (Planet p in planets)
                pop += p.Population;
            return pop / 1000f;
        }

        private int ShipCount(Empire e)
        {
            int num = 0;            
            
            try
            {
                if(Friends.Contains(e))
                {
                    foreach (Ship ship in e.GetShips())
                    {
                        num++;
                    }
                    return num;
                }
                HashSet<Ship> knownShips = new HashSet<Ship>();

                foreach (ThreatMatrix.Pin pins in PlayerEmpire.GetEmpireAI().ThreatMatrix.Pins.Values)
                {
                    if (pins.Ship == null || pins.Ship.loyalty != e)
                        continue;
                    knownShips.Add(pins.Ship);
                }

                foreach (Empire ally in Friends)
                {
                    foreach (ThreatMatrix.Pin pins in ally.GetEmpireAI().ThreatMatrix.Pins.Values)
                    {
                        if (pins.Ship == null || pins.Ship.loyalty != e)
                            continue;
                        knownShips.Add(pins.Ship);
                    }
                }
                foreach (Ship ship in knownShips)
                {
                    num++;
                }
                return num;
            }
            catch
            {               
            }
            return num;
        
        }

        private static void GetTechsFromPins(HashSet<string> techs, Dictionary<Guid, ThreatMatrix.Pin>.ValueCollection pins, Empire empire )
        {
            
            var shipTechs = new Array<string>();
            if (empire == null) return;
            var threatArray = pins.ToArray();
            foreach (ThreatMatrix.Pin pin in threatArray)
            {
                if (pin.Ship?.loyalty != empire) continue;

                shipTechs.AddRange(pin.Ship.shipData.TechsNeeded);
            }
            foreach (string tech in shipTechs)
                techs.Add(tech);


        }

        private float GetScientificStr(Empire e)
        {
            float scientificStr = 0f;
            HashSet<Planet> planets = new HashSet<Planet>();

            if (Traders.Contains(e) || Friends.Contains(e))
            {
                foreach (KeyValuePair<string, TechEntry> Technology in e.GetTDict())
                {
                    if (!Technology.Value.Unlocked)
                    {
                        continue;
                    }
                    scientificStr = scientificStr + ResourceManager.TechTree[Technology.Key].Cost;
                }
                return scientificStr;
            }
            var techs = new HashSet<string>();
            GetTechsFromPins(techs, PlayerEmpire.GetEmpireAI().ThreatMatrix.Pins.Values, e);
            foreach (Empire ally in Friends)
            {
                GetTechsFromPins(techs, ally.GetEmpireAI().ThreatMatrix.Pins.Values, e);
            }
            foreach (string tech in techs)
            {
                scientificStr = scientificStr + ResourceManager.TechTree[tech].Cost;
            }
            return scientificStr;



        }

        public override bool HandleInput(InputState input)
        {
            if (input.KeysCurr.IsKeyDown(Keys.I) && !input.KeysPrev.IsKeyDown(Keys.I) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
                return true;
            }
            if (close.HandleInput(input))
            {
                ExitScreen();
                return true;
            }
            //showExecuteButton = false;
            if (SelectedEmpire != EmpireManager.Player && !SelectedEmpire.data.Defeated && Contact.HandleInput(input))
            {
                ScreenManager.AddScreen(new DiplomacyScreen(this, SelectedEmpire, EmpireManager.Player, "Greeting"));
            }
            foreach (RaceEntry race in Races)
            {
                if (EmpireManager.Player == race.e || !EmpireManager.Player.GetRelations(race.e).Known)
                {
                    if (EmpireManager.Player != race.e || !HelperFunctions.ClickedRect(race.container, input))
                    {
                        continue;
                    }
                    SelectedEmpire = race.e;
                    ArtifactsSL.Reset();
                    var entry = new ArtifactEntry();
                    for (int i = 0; i < SelectedEmpire.data.OwnedArtifacts.Count; i++)
                    {
                        Artifact art = SelectedEmpire.data.OwnedArtifacts[i];
                        SkinnableButton button = new SkinnableButton(new Rectangle(0, 0, 32, 32), string.Concat("Artifact Icons/", art.Name))
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
                            ArtifactsSL.AddItem(entry);
                            entry = new ArtifactEntry();
                        }
                    }
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else
                {
                    if (!HelperFunctions.ClickedRect(race.container, input))
                    {
                        continue;
                    }
                    SelectedEmpire = race.e;
                    ArtifactsSL.Reset();
                    var entry = new ArtifactEntry();
                    for (int i = 0; i < SelectedEmpire.data.OwnedArtifacts.Count; i++)
                    {
                        Artifact art = SelectedEmpire.data.OwnedArtifacts[i];
                        var button = new SkinnableButton(new Rectangle(0, 0, 32, 32), string.Concat("Artifact Icons/", art.Name))
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
                            ArtifactsSL.AddItem(entry);
                            entry = new ArtifactEntry();
                        }
                    }
                }
            }
            foreach (ScrollList.Entry e in ArtifactsSL.VisibleEntries)
            {
                var arte = (ArtifactEntry)e.item;
                foreach (SkinnableButton button in arte.ArtifactButtons)
                {
                    if (!button.r.HitTest(input.CursorPosition))
                        continue;
                    var art = (Artifact)button.ReferenceObject;
                    string text = $"{Localizer.Token(art.NameIndex)}\n\n{Localizer.Token(art.DescriptionIndex)}";
                    ToolTip.CreateTooltip(text);
                }
            }
            if (input.Escaped || input.MouseCurr.RightButton == ButtonState.Pressed)
            {
                ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            float screenWidth = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            float screenHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            Rectangle titleRect = new Rectangle((int)screenWidth / 2 - 200, 44, 400, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2 - Fonts.Laserian14.MeasureString(Localizer.Token(1600)).X / 2f, titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            Rectangle leftRect = new Rectangle((int)screenWidth / 2 - 640, (screenHeight > 768f ? titleRect.Y + titleRect.Height + 5 : 44), 1280, 660);
            DMenu = new Menu2(leftRect);
            close = new CloseButton(this, new Rectangle(leftRect.X + leftRect.Width - 40, leftRect.Y + 20, 20, 20));
            SelectedInfoRect = new Rectangle(leftRect.X + 60, leftRect.Y + 250, 368, 376);
            IntelligenceRect = new Rectangle(SelectedInfoRect.X + SelectedInfoRect.Width + 30, SelectedInfoRect.Y, 368, 376);
            OperationsRect = new Rectangle(IntelligenceRect.X + IntelligenceRect.Width + 30, SelectedInfoRect.Y, 368, 376);
            ArtifactsRect = new Rectangle(SelectedInfoRect.X + 20, SelectedInfoRect.Y + 180, SelectedInfoRect.Width - 40, 130);
            Submenu ArtifactsSub = new Submenu(ArtifactsRect);
            ArtifactsSL = new ScrollList(ArtifactsSub, 40);
            Contact = new DanButton(new Vector2(SelectedInfoRect.X + SelectedInfoRect.Width / 2 - 91, SelectedInfoRect.Y + SelectedInfoRect.Height - 45), Localizer.Token(1644))
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
                    SelectedEmpire = e;
                    ArtifactsSL.Reset();
                    var entry = new ArtifactEntry();
                    for (int i = 0; i < e.data.OwnedArtifacts.Count; i++)
                    {
                        Artifact art = e.data.OwnedArtifacts[i];
                        var button = new SkinnableButton(new Rectangle(0, 0, 32, 32), string.Concat("Artifact Icons/", art.Name))
                        {
                            IsToggle = false,
                            ReferenceObject = art,
                            BaseColor = Color.White
                        };
                        if (entry.ArtifactButtons.Count < 5)
                        {
                            entry.ArtifactButtons.Add(button);
                        }
                        if (entry.ArtifactButtons.Count == 5 || i == e.data.OwnedArtifacts.Count - 1)
                        {
                            ArtifactsSL.AddItem(entry);
                            entry = new ArtifactEntry();
                        }
                    }
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

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            TransitionElapsedTime += elapsedTime;
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }
    }
}