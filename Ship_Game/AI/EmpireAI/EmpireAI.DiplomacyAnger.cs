using System.Collections.Generic;
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
                }
            }
            else if (Relationship.Value.Trust >= 50f)
            {
                if (Relationship.Value.Trust - usedTrust >
                    OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2)
                {
                    var NAPactOffer = new Offer
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Friends Rejected"
                    };
                    Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                        x => value.HaveRejected_OpenBorders = x);
                    var OurOffer = new Offer
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key == Empire.Universe.PlayerEmpire)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer);
                    }
                    else
                    {
                        Relationship.Key.GetEmpireAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                    }
                }
            }
            else if (Relationship.Value.Trust >= 20f &&
                     Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >=
                     0.75f * OwnerEmpire.data.DiplomaticPersonality.Territorialism)
            {
                if (Relationship.Value.Trust - usedTrust >
                    OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2)
                {
                    Offer NAPactOffer = new Offer
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        x => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer { OpenBorders = true };
                    if (Relationship.Key == Empire.Universe.PlayerEmpire)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, "Offer Open Borders", OurOffer, NAPactOffer);
                    }
                    else
                    {
                        Relationship.Key.GetEmpireAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                    }
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10 && Relationship.Value.Known &&
                     Relationship.Key == Empire.Universe.PlayerEmpire)
            {
                Relationship r = Relationship.Value;
                if (r.Anger_FromShipsInOurBorders >
                    OwnerEmpire.data.DiplomaticPersonality.Territorialism / 4f && !r.AtWar &&
                    !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                {
                    ThreatMatrix.ClearBorders();
                    if (!r.WarnedAboutColonizing)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, Relationship.Key, "Warning Ships");
                    }
                    else if (r.GetContestedSystem(out SolarSystem contested))
                    {
                        DiplomacyScreen.Show(OwnerEmpire, Relationship.Key, "Warning Colonized then Ships", contested);
                    }
                    r.WarnedAboutShips = true;
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
                        Offer NAPactOffer = new Offer
                        {
                            OpenBorders = true,
                            AcceptDL = "Open Borders Accepted",
                            RejectDL = "Open Borders Friends Rejected"
                        };
                        Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders,
                            x => value.HaveRejected_OpenBorders = x);
                        Offer OurOffer = new Offer
                        {
                            OpenBorders = true
                        };
                        if (Relationship.Key == Empire.Universe.PlayerEmpire)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer);
                        }
                        else
                        {
                            Relationship.Key.GetEmpireAI()
                                .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                        }
                    }
                }
                else if (Relationship.Value.Trust >= 20f &&
                         Relationship.Value.Anger_TerritorialConflict +
                         Relationship.Value.Anger_FromShipsInOurBorders >=
                         0.75f * OwnerEmpire.data.DiplomaticPersonality.Territorialism &&
                         Relationship.Value.Trust - usedTrust >
                         OwnerEmpire.data.DiplomaticPersonality.Territorialism / 2f)
                {
                    var NAPactOffer = new Offer
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders,
                        x => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key == Empire.Universe.PlayerEmpire)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, "Offer Open Borders", OurOffer, NAPactOffer);
                    }
                    else
                    {
                        Relationship.Key.GetEmpireAI()
                            .AnalyzeOffer(OurOffer, NAPactOffer, OwnerEmpire, Offer.Attitude.Pleading);
                    }
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10)
            {
                if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
                {
                    Relationship r = Relationship.Value;
                    if (r.Anger_FromShipsInOurBorders >
                        OwnerEmpire.data.DiplomaticPersonality.Territorialism / 4 && !r.AtWar &&
                        !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                    {
                        ThreatMatrix.ClearBorders();
                        if (!r.WarnedAboutColonizing)
                        {
                            DiplomacyScreen.Show(OwnerEmpire, Relationship.Key, "Warning Ships");
                        }
                        else if (r.GetContestedSystem(out SolarSystem contested))
                        {
                            DiplomacyScreen.Show(OwnerEmpire, Relationship.Key, "Warning Colonized then Ships", contested);
                        }
                        r.WarnedAboutShips = true;
                    }
                }
            }
            else if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f &&
                     Relationship.Value.Trust < Relationship.Value.TotalAnger)
            {
                Relationship.Value.Posture = Posture.Neutral;
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
                        DiplomacyScreen.Show(OwnerEmpire, them, "Warning Ships");
                    }
                    else if (r.GetContestedSystem(out SolarSystem contested))
                    {
                        DiplomacyScreen.Show(OwnerEmpire, them, "Warning Colonized then Ships", contested);
                    }

                r.turnsSinceLastContact = 0;
                r.WarnedAboutShips = true;
                return;
            }
            if (!r.WarnedAboutShips || r.AtWar || !OwnerEmpire.IsEmpireAttackable(them)) return;

            if (them.CurrentMilitaryStrength < OwnerEmpire.CurrentMilitaryStrength * (1f - OwnerEmpire.data.DiplomaticPersonality.Territorialism * .01f))
            {
                DeclareWarOn(them, WarType.ImperialistWar);
            }
        }
        
    }
}