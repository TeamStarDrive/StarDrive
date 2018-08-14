using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {

    public sealed partial class EmpireAI
    {
        private int FirstDemand = 20;

        private int SecondDemand = 75;

        private void AssessAngerAggressive(KeyValuePair<Empire, Relationship> Relationship,
            Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                AssessDiplomaticAnger(Relationship);
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
                    (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Friends Rejected"
                    };
                    Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                        (bool x) => value.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.Trust >= 20f &&
                     Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >=
                     0.75f * (float) OwnerEmpire.data.DiplomaticPersonality.Territorialism)
            {
                if (Relationship.Value.Trust - usedTrust >
                    (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10 && Relationship.Value.Known &&
                     Relationship.Key == Empire.Universe.PlayerEmpire)
            {
                Relationship r = Relationship.Value;
                if (r.Anger_FromShipsInOurBorders >
                    (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                    !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                {
                    ThreatMatrix.ClearBorders();
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                            Relationship.Key, "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                            Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
                    }
                    r.WarnedAboutShips = true;
                    return;
                }
            }
        }

        private void AssessAngerPacifist(KeyValuePair<Empire, Relationship> Relationship,
            Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                AssessDiplomaticAnger(Relationship);
            }
            else if (!Relationship.Value.Treaty_OpenBorders &&
                     (Relationship.Value.Treaty_Trade || Relationship.Value.Treaty_NAPact) &&
                     !Relationship.Value.HaveRejected_OpenBorders)
            {
                if (Relationship.Value.Trust >= 50f)
                {
                    if (Relationship.Value.Trust - usedTrust >
                        OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2f)
                    {
                        Offer NAPactOffer = new Offer()
                        {
                            OpenBorders = true,
                            AcceptDL = "Open Borders Accepted",
                            RejectDL = "Open Borders Friends Rejected"
                        };
                        Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                            (bool x) => value.HaveRejected_OpenBorders = x);
                        Offer OurOffer = new Offer()
                        {
                            OpenBorders = true
                        };
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI()
                                .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                            return;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                            Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                        return;
                    }
                }
                else if (Relationship.Value.Trust >= 20f &&
                         Relationship.Value.Anger_TerritorialConflict +
                         Relationship.Value.Anger_FromShipsInOurBorders >=
                         0.75f * OwnerEmpire.data.DiplomaticPersonality.Territorialism &&
                         Relationship.Value.Trust - usedTrust >
                         OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2f)
                {
                    var NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                        Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10)
            {
                if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
                {
                    Relationship r = Relationship.Value;
                    if (r.Anger_FromShipsInOurBorders >
                        (float) (OwnerEmpire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar &&
                        !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                    {
                        ThreatMatrix.ClearBorders();
                        if (!r.WarnedAboutColonizing)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                                Relationship.Key, "Warning Ships"));
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
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

        private void AssessDiplomaticAnger(KeyValuePair<Empire, Relationship> Relationship)
        {
            if (!Relationship.Value.Known) return;
            Relationship r = Relationship.Value;
            Empire them = Relationship.Key;
            if (r.Anger_MilitaryConflict >= 5 && !r.AtWar && !r.Treaty_Peace)
            {
                DeclareWarOn(them, WarType.DefensiveWar);
                return;
            }
                
            if (r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >= OwnerEmpire.data.DiplomaticPersonality.Territorialism 
                && !r.AtWar && !r.Treaty_OpenBorders && !r.Treaty_Peace)
            {
                r.PreparingForWar = true;
                r.PreparingForWarType = WarType.BorderConflict;
                return;
            }
            if (r.Anger_FromShipsInOurBorders > OwnerEmpire.data.DiplomaticPersonality.Territorialism / 4f 
                && !r.AtWar && !r.WarnedAboutShips)
            {
                if (Relationship.Key == Empire.Universe.PlayerEmpire && r.turnsSinceLastContact > 10)
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                            "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                            "Warning Colonized then Ships", r.GetContestedSystem()));
                    }

                r.turnsSinceLastContact = 0;
                r.WarnedAboutShips = true;
                return;
            }
            if (!r.WarnedAboutShips || r.AtWar || !OwnerEmpire.IsEmpireAttackable(them)) return;

            if (them.currentMilitaryStrength < OwnerEmpire.currentMilitaryStrength * (1f - OwnerEmpire.data.DiplomaticPersonality.Territorialism * .01f))
            {
                DeclareWarOn(them, WarType.ImperialistWar);

            }
        }
        
    }
}