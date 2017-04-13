using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {

    public sealed partial class GSAI
    {
        private int FirstDemand = 20;

        private int SecondDemand = 75;

        private void AssessAngerAggressive(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship,
            Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                this.AssessDiplomaticAnger(Relationship);
            }
            else if (Relationship.Value.Treaty_OpenBorders ||
                     !Relationship.Value.Treaty_Trade && !Relationship.Value.Treaty_NAPact ||
                     Relationship.Value.HaveRejected_OpenBorders)
            {
                if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f &&
                    Relationship.Value.Trust < Relationship.Value.TotalAnger)
                {
                    Relationship.Value.Posture = Posture.Neutral;
                    return;
                }
            }
            else if (Relationship.Value.Trust >= 50f)
            {
                if (Relationship.Value.Trust - usedTrust >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Friends Rejected"
                    };
                    Ship_Game.Gameplay.Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                        (bool x) => value.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.Trust >= 20f &&
                     Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >=
                     0.75f * (float) this.empire.data.DiplomaticPersonality.Territorialism)
            {
                if (Relationship.Value.Trust - usedTrust >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10 && Relationship.Value.Known &&
                     Relationship.Key == Empire.Universe.PlayerEmpire)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                if (r.Anger_FromShipsInOurBorders >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                    !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                {
                    this.ThreatMatrix.ClearBorders();
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                            Relationship.Key, "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                            Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
                    }
                    r.WarnedAboutShips = true;
                    return;
                }
            }
        }

        private void AssessAngerPacifist(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship,
            Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                this.AssessDiplomaticAnger(Relationship);
            }
            else if (!Relationship.Value.Treaty_OpenBorders &&
                     (Relationship.Value.Treaty_Trade || Relationship.Value.Treaty_NAPact) &&
                     !Relationship.Value.HaveRejected_OpenBorders)
            {
                if (Relationship.Value.Trust >= 50f)
                {
                    if (Relationship.Value.Trust - usedTrust >
                        (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                    {
                        Offer NAPactOffer = new Offer()
                        {
                            OpenBorders = true,
                            AcceptDL = "Open Borders Accepted",
                            RejectDL = "Open Borders Friends Rejected"
                        };
                        Ship_Game.Gameplay.Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                            (bool x) => value.HaveRejected_OpenBorders = x);
                        Offer OurOffer = new Offer()
                        {
                            OpenBorders = true
                        };
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI()
                                .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                            return;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                            Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                        return;
                    }
                }
                else if (Relationship.Value.Trust >= 20f &&
                         Relationship.Value.Anger_TerritorialConflict +
                         Relationship.Value.Anger_FromShipsInOurBorders >=
                         0.75f * (float) this.empire.data.DiplomaticPersonality.Territorialism &&
                         Relationship.Value.Trust - usedTrust >
                         (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10)
            {
                if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
                {
                    Ship_Game.Gameplay.Relationship r = Relationship.Value;
                    if (r.Anger_FromShipsInOurBorders >
                        (float) (this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                        !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                    {
                        this.ThreatMatrix.ClearBorders();
                        if (!r.WarnedAboutColonizing)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                                Relationship.Key, "Warning Ships"));
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire,
                                Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
                        }
                        r.WarnedAboutShips = true;
                        return;
                    }
                }
            }
            else if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f &&
                     Relationship.Value.Trust < Relationship.Value.TotalAnger)
            {
                Relationship.Value.Posture = Posture.Neutral;
                return;
            }
        }

        private void AssessDiplomaticAnger(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship)
        {
            if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                Empire them = Relationship.Key;
                if (r.Anger_MilitaryConflict >= 5 && !r.AtWar)
                {
                    this.DeclareWarOn(them, WarType.DefensiveWar);
                }
                if (r.Anger_FromShipsInOurBorders >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                    !r.WarnedAboutShips && !r.Treaty_Peace && !r.Treaty_OpenBorders)
                {
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, them,
                            "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, them,
                            "Warning Colonized then Ships", r.GetContestedSystem()));
                    }
                    r.turnsSinceLastContact = 0;
                    r.WarnedAboutShips = true;
                    return;
                }
                if (r.Threat < 25f &&
                    r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >=
                    (float) this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar &&
                    !r.Treaty_OpenBorders && !r.Treaty_Peace)
                {
                    r.PreparingForWar = true;
                    r.PreparingForWarType = WarType.BorderConflict;
                    return;
                }
                if (r.PreparingForWar && r.PreparingForWarType == WarType.BorderConflict)
                {
                    r.PreparingForWar = false;
                    return;
                }
            }
            else if (Relationship.Value.Known)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                Empire them = Relationship.Key;
                if (r.Anger_MilitaryConflict >= 5 && !r.AtWar && !r.Treaty_Peace)
                {
                    this.DeclareWarOn(them, WarType.DefensiveWar);
                }
                if (r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >=
                    (float) this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar &&
                    !r.Treaty_OpenBorders && !r.Treaty_Peace)
                {
                    r.PreparingForWar = true;
                    r.PreparingForWarType = WarType.BorderConflict;
                }
                if (r.Anger_FromShipsInOurBorders >
                    (float) (this.empire.data.DiplomaticPersonality.Territorialism / 2) && !r.AtWar &&
                    !r.WarnedAboutShips)
                {
                    r.turnsSinceLastContact = 0;
                    r.WarnedAboutShips = true;
                }
            }
        }
        
    }
}